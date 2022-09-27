# Global Forecast System (GFS) Decoder (by jorgethomasm)

The GFS is a Numeric Weather Prediction (NWP) model developed by the National Centers for Environmental Prediction (NCEP).
The GFS is publicly available.

"GFS data is not copyrighted and is available for free in the public domain under provisions of U.S. law. Because of this, the model serves as the basis for the forecasts of numerous private, commercial, and foreign weather companies." 
https://en.wikipedia.org/wiki/Global_Forecast_System

This software manages the download and decoding (Degrib) of the Grib-formatted files that contain Messages (MSGs) or weather variables forecasted for the next hours, depending on the desired forecast horizon (FH).
If the forecast horizon is 24 hours, then 24 files will be downloaded, decoded and the variables of interest will be extracted and saved in a .dat file with table format. (Actually an US_en-formated .csv file)
Each files is approximately 200 MB. As soon as one is downloaded and processed, the file is deleted in order to optimise hard drive space.

## Current selected MSGs with energy management purposes:

Abkurzung | Beschreibung								| Row Location
------    | ------										| ----------
GUST      |- surface Wind speed in [m/s]                |- @row 14
TMP       |- 2 m above ground temperature [Celsius]     |- @row 581 
RH        |- Relative Humidity 2 m above ground [%]     |- @row 584   
PRES      |- surface level Pressure [Pa]                |- @row 561
DSWRF     |- Downward Short-Wave Radiation Flux [W/m^2] |- @row 653 
SUNSD     |- Sunshine duration [s]                      |- @row 622 
TSOIL     |- 0-0.1 m below ground Soil Temperature [K]  |- @row 564 
SNOD      |- Snow depth [m]							    |- @row 578 
ALBDO     |- surface Albedo [%]						    |- @row 730 
TCDC_ATM  |- Entire atmosphere Total Cloud Coverage [%] |- @row 637 
CRAIN     |- Categorical Rain [-]				        |- @row 604 
														 					   
												
### Invetory available on: https://www.nco.ncep.noaa.gov/pmb/products/gfs/gfs.t00z.pgrb2.0p25.f003.shtml

## Runtime Schedule:
The update cycle of the files in the server is every 6 hours (model cycle runtime), i.e. 00:00, 06:00, 12:00 and 18:00 UTC 
In order to update the forecast, run this program in the following daily schedule:
05:45; 11:45; 17:45 and 23:45 CET/CEST. This will guarantee the availability of the new Grib files in the server. 


## System Requirements:

- Microsoft Windows OS
- Internet Connection (~10 Mbps)
- 500 MB of free hard drive space

Note: Due to heavy traffic, the server will conveniently limit the download speed (server throttling).
Therefore, the whole bandwidth of your connection will not be occupied.

This sofware includes and uses the command line version of degrib available in https://vlab.noaa.gov/web/mdl/degrib-download 

## Contact:
jorgethomasm@ieee.org