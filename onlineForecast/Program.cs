using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;


namespace onlineForecast
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.LongDatePattern = "MMM-dd-yyyy";
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            

            // HEADER
            Console.WriteLine(
                
                " _____________________________________________________________________________ \n\n" +
                "  #####  #######  #####     ######                                            \n" +
                " #     # #       #     #    #     # ######  ####   ####  #####  ###### #####  \n" + 
                " #       #       #          #     # #      #    # #    # #    # #      #    # \n" + 
                " #  #### #####    #####     #     # #####  #      #    # #    # #####  #    # \n" + 
                " #     # #             #    #     # #      #      #    # #    # #      #####  \n" + 
                " #     # #       #     #    #     # #      #    # #    # #    # #      #   #  \n" + 
                "  #####  #        #####     ######  ######  ####   ####  #####  ###### #    # \n\n" +

                "                                                    By: jorgethomasm@ieee.org \n\n" +
                " Last update: Aug. 2021\n" +
                " Forecast publicly available at:\n" +
                " http://www.emc.ncep.noaa.gov/index.php?branch=GFS \n" +
                " For more information open the file \"README.md\"\n" +
                " _____________________________________________________________________________ \n"

                );

            #region ---------- INPUT PARAMETERS---------

            string myWorkingDir = string.Concat(Environment.CurrentDirectory, @"\");
                    

            // read .csv setup files
            var myConfigWeahterVars = ReadConfigWeatherVars("config_weather_variables.csv"); // Extra method - See below...

            var myConfigLocations = ReadConfigLocations("config_locations.csv"); // Extra method - See below...

            // Forecast Horizon (Hour products from 000 - 384)
            int FH = ReadConfigForecastHorizon("config_forecast_horizon_hours.csv"); // e.g.:  until = 72 for 72 hours Forecast Horizon (FH)

            string Res = "0p25"; // Selection of Spatial Resolution

            int aggPeriod = 6; // hours. This is to dissaggregate DSWRF

            
            Console.WriteLine(" Horizontal Resolution: 0.25� (~13 km)");
            Console.WriteLine(" Time Resolution: 1 hour");
            Console.WriteLine(" Forecast Horizon: " + FH + " hours");


            #endregion


            // Get file name termination according to selected FH
            var FFF = new List<string>();
            for (int k = 0; k <= FH; k += 1) //(values every 1 hour)
            {

                if (k < 10)
                    FFF.Add("f" + "00" + k.ToString());
                else
                {
                    FFF.Add("f" + "0" + k.ToString());
                }

            }



            #region Auto-selection of the last Model Cycle Runtime            

            // times are synced until 72 hours in the future
            DateTime CC00Available = DateTime.Today.AddHours(5.5); //4.5
            DateTime CC06Available = DateTime.Today.AddHours(11.5);
            DateTime CC12Available = DateTime.Today.AddHours(17.5);
            DateTime CC18Available = DateTime.Today.AddHours(23.5);

            string FolderEndName;
            string CC; // model cycle runtime (i.e. 00, 06, 12, 18)

            // Available at 4:00 UTC
            if (DateTime.Now >= CC00Available && DateTime.Now < CC06Available)
            {

                Console.WriteLine("Model Cycle Runtime: 00 UTC\n");
                CC = "t00z";
                FolderEndName = "00";

            }
            // Available at 10:00 UTC
            else if (DateTime.Now >= CC06Available && DateTime.Now < CC12Available)
            {

                Console.WriteLine(" Model Cycle Runtime: 06 UTC\n");
                CC = "t06z";
                FolderEndName = "06";

            }
            // Available at 16:00 UTC
            else if (DateTime.Now >= CC12Available && DateTime.Now < CC18Available)
            {

                Console.WriteLine(" Model Cycle Runtime: 12 UTC\n");
                CC = "t12z";
                FolderEndName = "12";

            }
            // Available at 22:00 UTC
            else               // (DateTime.Now >= Modellauf18Available)
            {

                Console.WriteLine(" Model Cycle Runtime: 18 UTC\n");
                CC = "t18z";
                FolderEndName = "18";

            }

            // Build a List of .GRIB2 files to be downloaded:
            var myGFSfiles = new List<string>();

            for (int i = 0; i < FFF.Count - 1; i++)
            {

                myGFSfiles.Add("gfs." + CC + ".pgrb2." + Res + "." + FFF[i + 1]); // Discard f000

            }

            Console.WriteLine(" _____________________________________________________________________________\n\n");
            
            #endregion


            #region Download Action & MSGs Decoding

            // FTP folder autosearch
            var myFtpCCfolder = "gfs." + DateTime.Now.ToString("yyyyMMdd") + "/" + FolderEndName + "/atmos";
            var myFtpString = "ftp://ftp.ncep.noaa.gov/pub/data/nccf/com/gfs/prod/" + myFtpCCfolder + "/";


            var NOAAclient = new WebClient(); // request download
            var degriber = new Process(); // Console (cmd.exe) process
            string DegribCmdLine;
            string outputMSG;
            string[] splitOutputMSGperline;
            DateTime myTimeStamp;
            string fromDate = null;
            string toDate = null;
            var RandomWait = new Random();


            // ------------------- Create a List of Data Tables to Store MSG per Location -------------------
            var myListOfDataTables = new List<DataTable>(myConfigLocations.GetLength(0));

            // Populate List:
            for (int i = 0; i < myConfigLocations.GetLength(0); i++)
            {
                var myDataTable = new DataTable(); // Set Data Table 
                myListOfDataTables.Add(myDataTable);

            }

            
            for (int i = 0; i < myGFSfiles.Count; i++)
            {

                Console.WriteLine(" Downloading GRIB2 file from:\n " + myFtpString + "\n");

                //Action is a DELEGATE. It encapsulates a method that has no parameters and doesn't return a value.
                Action GFSdownloadAction = () => NOAAclient.DownloadFile(myFtpString + myGFSfiles[i], myWorkingDir + myGFSfiles[i]);

                long myFileSize = 0;
                var myExpectedFileSize = 185000000; // in Bytes = 185 MB
                int dnldRetryCount = 0;

                while (myFileSize < myExpectedFileSize)
                {
                    dnldRetryCount++;

                    if (dnldRetryCount > 5)
                    {
                        Console.WriteLine(" WARNING! Possible corrupted file:\n ");
                        break;
                    }

                    // Call TryExecute Mehtod (retry manager, see method developed below)
                    TryExecute(GFSdownloadAction);

                    Console.WriteLine(" Done! File saved in the system folder:\n " + myWorkingDir);

                    int RndSeconds = RandomWait.Next(3000, 11000);
                    Thread.Sleep(RndSeconds); // WaitAsync

                    var myDownloadedFile = new FileInfo(myGFSfiles[i]);

                    myFileSize = myDownloadedFile.Length; // update file size with FileInfo

                    Console.WriteLine(" File size: " + myFileSize / 1048576 + " MB\n");

                }

                int l = 0; // location counter. It resets for the next FH
                foreach (var tableOfLocation in myListOfDataTables)
                {

                    // Prepare table only the first time (for the first FH hour)
                    if (i == 0)
                    {
                        tableOfLocation.Columns.Add("Timestamp (UTC)", typeof(DateTime));
                        tableOfLocation.Columns.Add("GUST (m/s)", typeof(double));
                        tableOfLocation.Columns.Add("TMP (C)", typeof(double));
                        tableOfLocation.Columns.Add("RH (%)", typeof(double));
                        tableOfLocation.Columns.Add("PRES (Pa)", typeof(double));
                        tableOfLocation.Columns.Add("DSWRF_agg (W/m^2)", typeof(double));
                        tableOfLocation.Columns.Add("SUNSD (s)", typeof(double));
                        tableOfLocation.Columns.Add("TSOIL (K)", typeof(double));
                        tableOfLocation.Columns.Add("SNOD (m)", typeof(double));
                        tableOfLocation.Columns.Add("ALBDO (%)", typeof(double));
                        tableOfLocation.Columns.Add("TCDC_ATM (%)", typeof(double));
                        tableOfLocation.Columns.Add("CRAIN", typeof(double));
                    }
                    
                    // Extract more than 1 location:

                    var Loc = myConfigLocations[l, 0];
                    var Lat = myConfigLocations[l, 1];
                    var Lon = myConfigLocations[l, 2];
                    l++;

                    Console.WriteLine(" Decoding data for: \n Location: {0}\n Latitude: {1}\n Longitude: {2}\n", Loc, Lat, Lon);

                    DegribCmdLine = "degrib " + myGFSfiles[i] + " -Unit m -P -pnt " + Lat + "," + Lon;
                    degriber.StartInfo.FileName = "cmd.exe";
                    degriber.StartInfo.Arguments = "/c " + DegribCmdLine; // Command Line to execute
                    degriber.StartInfo.UseShellExecute = false;
                    degriber.StartInfo.RedirectStandardOutput = true;
                    degriber.Start(); // Execute (like pressing ENTER key)

                    // To avoid deadlocks, always read the output stream first and then wait.        
                    outputMSG = degriber.StandardOutput.ReadToEnd();
                    degriber.WaitForExit();

                    // End of degrib Process

                    // Splitting data string
                    splitOutputMSGperline = outputMSG.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    // Direct Download files from:
                    // ftp://ftp.ncep.noaa.gov/pub/data/nccf/com/gfs/prod/ 
                    // Description:
                    // https://www.nco.ncep.noaa.gov/pmb/products/gfs/
                    // MSGs / Forecasted Variables. Invetory available on: 
                    // https://www.nco.ncep.noaa.gov/pmb/products/gfs/gfs.t00z.pgrb2.0p25.f003.shtml
                    // Last Updated: Wed Mar 17 19:37:44 2021

                    var GUST = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[0])].Split(',');
                    var TMP = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[1])].Split(','); // 2 m above ground
                    var RH = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[2])].Split(','); // 2 m above ground
                    var PRES = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[3])].Split(','); // Ground or water surface
                    var DSWRF = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[4])].Split(',');
                    var SUNSD = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[5])].Split(',');
                    var TSOIL = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[6])].Split(','); // 0-0.1 m land depth
                    var SNOD = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[7])].Split(',');
                    var ALBDO = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[8])].Split(',');
                    var TCDC = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[9])].Split(','); // entire atmosphere
                    var CRAIN = splitOutputMSGperline[Convert.ToInt32(myConfigWeahterVars[10])].Split(','); // Categorical Rain

                    // Trimming white spaces
                    for (int q = 0; q < GUST.Length; q++)
                    {
                        GUST[q] = GUST[q].Trim();
                        TMP[q] = TMP[q].Trim();
                        RH[q] = RH[q].Trim();
                        PRES[q] = PRES[q].Trim();
                        DSWRF[q] = DSWRF[q].Trim();
                        SUNSD[q] = SUNSD[q].Trim();
                        TSOIL[q] = TSOIL[q].Trim();
                        SNOD[q] = SNOD[q].Trim();
                        ALBDO[q] = ALBDO[q].Trim();
                        TCDC[q] = TCDC[q].Trim();
                        CRAIN[q] = CRAIN[q].Trim();
                    }

                    myTimeStamp = DateTime.ParseExact(GUST[3], "yyyyMMddHHmm", CultureInfo.InvariantCulture); // GUST[3 (Third Column)] is validTime (UTC) for the forecasted value
                    Console.WriteLine("\n --------- Done! -> Valid Forecast Timestamp: " + myTimeStamp + " UTC ---------\n\n");

                    if (i == 0) fromDate = myTimeStamp.ToString(); // First timestamp
                    if (i == myGFSfiles.Count) toDate = myTimeStamp.ToString(); // Last timestamp = First timestamp + FH

                    // Fill Data Table (fill with a loop of the current Modellauf)

                    tableOfLocation.Rows.Add(new object[]
                    {
                        myTimeStamp,
                        Convert.ToDouble(GUST[4]), // originally it's a string
                        Convert.ToDouble(TMP[4]),
                        Convert.ToDouble(RH[4]),
                        Convert.ToDouble(PRES[4]),
                        Convert.ToDouble(DSWRF[4]),
                        Convert.ToDouble(SUNSD[4]),
                        Convert.ToDouble(TSOIL[4]),
                        Convert.ToDouble(SNOD[4]),
                        Convert.ToDouble(ALBDO[4]),
                        Convert.ToDouble(TCDC[4]),
                        Convert.ToDouble(CRAIN[4]),
                     });

                }

                //Console.WriteLine("\n --------- Done! -> Valid Forecast Timestamp: " + myTimeStamp + " UTC ---------\n\n");

                //Delete Grib2 files               
                TryToDelete(myGFSfiles[i]); // this method is developed at the end
                Console.WriteLine(" The GRIB2 file for the decoded UTC timestamp was deleted\n\n\n");
                Thread.Sleep(300);

            }

            #endregion



            #region Add disaggregated DSWRF / Delete aggregated DSWRF column

            // Dissagregate per 'l' element (location) in populated list: foreach table in 

            // Extract DSWRF aggregated values (RAW)

            foreach (var table_of_location in myListOfDataTables)
            {
                double[] myDswrfRawValues = new double[table_of_location.Rows.Count];
                for (int i = 0; i < myDswrfRawValues.Length; i++)
                {
                    myDswrfRawValues[i] = Convert.ToDouble(table_of_location.Rows[i]["DSWRF_agg (W/m^2)"]);
                }

                // Call helper method
                var myDswrfValues = DoDesaggregation(myDswrfRawValues, aggPeriod);

                table_of_location.Columns.Remove("DSWRF_agg (W/m^2)"); // Delete DSWRF_agg column

                table_of_location.Columns.Add("DSWRF (W/m^2)", typeof(double)); // Add array to existing table

                var myRowCounter = 0;
                foreach (DataRow row in table_of_location.Rows)
                {
                    //need to set value to NewColumn column
                    row["DSWRF (W/m^2)"] = myDswrfValues[myRowCounter];

                    myRowCounter++;
                }

            }

            #endregion


            #region Exporting Data Table to .Csv

            // for each table in list of data tables
            int j = 0; // Location counter
            foreach (var table_of_location in myListOfDataTables)
            {
                StringBuilder myForecastContent = new StringBuilder();

                // Get column names for the first row:
                IEnumerable<string> columnNames = table_of_location.Columns.Cast<DataColumn>().Select(column => column.ColumnName);

                myForecastContent.AppendLine(string.Join(",", columnNames)); // add column names as the 1st row

                foreach (DataRow row in table_of_location.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());

                    myForecastContent.AppendLine(string.Join(",", fields));
                }

                // Get Prefix to identigy a filename per location
                var Loc = myConfigLocations[j, 0];
                
                // Write .dat (Csv) File

                //string myForecastFileName = "GFS" + FH + "h" + Res + ".dat";
                string myForecastFilename = "GFS_"+ Loc + ".csv";
                File.WriteAllText(myForecastFilename, myForecastContent.ToString());

                //string myPastForecastFilename = myWorkingDir + @"\PastForecasts\" + myWorkingDir + @"\PastForecasts\" + "GFS_" + DateTime.Now.ToString("yyyyMMdd") + "_" + CC + ".dat";
                //string test = myWorkingDir + @"PastForecasts\" + "GFS_" + DateTime.Now.ToString("yyyyMMdd") + ".dat";


                // ToDo: Write results to InfluxDB -------

                // if results are written in the InfluxDB the following part can be commented":

                FileInfo myPastForecastFilename = new FileInfo(myWorkingDir + @"PastForecasts\" + "GFS_" + Loc + "_" + DateTime.Now.ToString("yyyyMMdd") + "_FH" + FH + "_" + CC + ".csv");
                myPastForecastFilename.Directory.Create();
                File.WriteAllText(myPastForecastFilename.FullName, myForecastContent.ToString());

                j++;
                Thread.Sleep(1200); // Wait 1 seconds

            }
           
            #endregion

            // Call Batchfile in Command Line (execute matlab scripts)
            
            //BatchFileExecute("execmlabscrpt.bat");


        }


        #region HELPER METHODS


        // 1) File Delete
        static bool TryToDelete(string f)
        {
            try
            {
                File.Delete(f);
                return true;

            }

            catch (IOException)
            {
                return false;
            }

        }


        // 2) Retry Manager (in case of connection time-out)
        private const int RetryCount = 5;
        private const int RetryTimeoutInMillis = 4813;

        public static void TryExecute(Action action)
        {
            if (action == null) return;
            var numberOfRetries = RetryCount;
            do
            {
                try
                {
                    action();
                    return;
                }
                catch (WebException webEx)
                {
                    if (numberOfRetries == 0)
                    {

                        var ShowWebEx = webEx.Message;
                        throw;

                    }
                    Thread.Sleep(RetryTimeoutInMillis);
                }

            }
            while (numberOfRetries-- > 0);
        }


        // 3) Function to dissagregate the DSWRF 
        ///<summary>
        ///This method disaggregates values of a time-series.
        ///Additionaly, it will filter any negative value resulting from the calculation.
        ///</summary>
        static double[] DoDesaggregation(double[] myAggregatedValues, int aggregationPeriod)
        {

            double[] myDisaggregatedValues = new double[myAggregatedValues.Length];

            // Generate multiples of the Aggregation Period
            var aggPeriodMultiples = new List<int>();

            for (int i = 0; i < aggregationPeriod * 100; i++)
            {
                aggPeriodMultiples.Add(aggregationPeriod * i); // 100 multiples of aggPeriod (6 h)
            }

            int j = 0;

            for (int i = 0; i < myAggregatedValues.Length; i++) // take .count instead of FH
            {

                // A) Multiples
                if (aggPeriodMultiples.Contains(i))
                {
                    myDisaggregatedValues[i] = myAggregatedValues[i]; // it stays the same as aggregated
                    j = 0; // reset
                }

                // B) Between multiples
                else
                {
                    j++;
                    myDisaggregatedValues[i] = (j + 1) * myAggregatedValues[i] - j * myAggregatedValues[i - 1];
                }

            }

            // Filter negative values:
            for (int i = 0; i < myDisaggregatedValues.Length; i++)
            {
                if (myDisaggregatedValues[i] < 0)
                {
                    myDisaggregatedValues[i] = 0;
                }
            }

            return myDisaggregatedValues;

        }


        // 4) Function to read the .csv config file
        ///<summary>
        ///This method parses a two column .csv and keeps the values of the secods column only.
        ///</summary>
        static string[] ReadConfigWeatherVars(string configFileName)
        {

            string[] myConfig = File.ReadAllLines(configFileName);

            string[] myConfigVars = new string[myConfig.Length];

            for (int i = 0; i < myConfig.Length; i++)
            {
                var temp = myConfig[i].Split(',');
                myConfigVars[i] = temp[1];
            }

            return myConfigVars;

        }


        static string[,] ReadConfigLocations(string configFileName)
        {

            string[] myConfig = File.ReadAllLines(configFileName);

            string[,] myConfigVars = new string[myConfig.Length, myConfig.Length];

            for (int i = 0; i < myConfig.Length; i++)
            {
                for (int j = 0; j < myConfig.Length; j++)
                {
                    var temp = myConfig[i].Split(',');
                    myConfigVars[i,j] = temp[j];
                }
                
            }

            return myConfigVars;

        }

        static int ReadConfigForecastHorizon(string configFileName)
        {
            
            string[] myConfigVar = File.ReadAllLines(configFileName);

            int myConfigVarInt = Int16.Parse(myConfigVar[0]);
          
            return myConfigVarInt;

        }



        // 5) Function to execute .bat file in cmd. (console)
        ///<summary>
        ///This method executes a batch (.bat) file in the console environment.
        ///</summary>
        public static void BatchFileExecute(string myBatchFilename)
        {

            string myWorkingDir = string.Concat(Environment.CurrentDirectory, @"\");
            string myArguments = string.Concat("/c ", myBatchFilename);


            Process callBatchFile = new Process();

            callBatchFile.StartInfo.WorkingDirectory = myWorkingDir;

            callBatchFile.StartInfo.FileName = "cmd.exe";
            callBatchFile.StartInfo.Arguments = myArguments;
            callBatchFile.StartInfo.UseShellExecute = false;
            callBatchFile.StartInfo.RedirectStandardOutput = true;
            callBatchFile.Start();
            callBatchFile.WaitForExit();

        }
        

        #endregion
        
    }
}
