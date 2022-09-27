% ----------------------------------

% JThomas - 29.08.2018

% Forecast Plots / Visualisation Script

% ----------------------------------


% Read decoded forecast file:

fid = fopen('GFS_IOSB.dat');

Data = textscan(fid, '%{MM/dd/yyyy HH:mm:ss}D %f %f %f %f %f %f %f %f %f %f', 'delimiter',',', 'Headerlines',1); 

fclose(fid); 

% Extract vectors:
UTC = Data{1};
UTC.TimeZone = 'UTC';

GUST = Data{2};
TMP = Data{3};
RH = Data{4};
PRES = Data{5} ./ 1000; % to kPa
SUNSD = Data{6} ./ 60; % to minutes
TSOIL = Data{7};
SNOD = Data{8};
ALBDO = Data{9};
TCDC = Data{10};
DSWRF = Data{11};

time_EU_Berlin = UTC;
time_EU_Berlin.TimeZone = 'Europe/Berlin';



% Define a Table
dataTable = table(UTC, time_EU_Berlin, GUST, TMP, RH, PRES, SUNSD, TSOIL, SNOD, ALBDO, TCDC, DSWRF);

dataTable.Properties.VariableUnits{'UTC'} = 'Time (UTC)';
dataTable.Properties.VariableDescriptions{'UTC'} = 'Coordinated Universal Time';

dataTable.Properties.VariableUnits{'time_EU_Berlin'} = 'Time (EU/Berlin)';
dataTable.Properties.VariableDescriptions{'time_EU_Berlin'} = 'Europe-Berlin (CET/CEST) Time';

dataTable.Properties.VariableUnits{'GUST'} = 'm/s';
dataTable.Properties.VariableDescriptions{'GUST'} = 'Surface Wind speed';

dataTable.Properties.VariableUnits{'TMP'} = '°C';
dataTable.Properties.VariableDescriptions{'TMP'} = '2 m above ground temperature';

dataTable.Properties.VariableUnits{'RH'} = '%';
dataTable.Properties.VariableDescriptions{'RH'} = 'Relative Humidity 2 m above ground';

dataTable.Properties.VariableUnits{'PRES'} = 'kPa';
dataTable.Properties.VariableDescriptions{'PRES'} = 'Surface level Pressure';

dataTable.Properties.VariableUnits{'SUNSD'} = 'minutes';
dataTable.Properties.VariableDescriptions{'SUNSD'} = 'Sunshine duration';

dataTable.Properties.VariableUnits{'TSOIL'} = 'K';
dataTable.Properties.VariableDescriptions{'TSOIL'} = '0-0.1 m below ground Soil Temperature';

dataTable.Properties.VariableUnits{'SNOD'} = 'm';
dataTable.Properties.VariableDescriptions{'SNOD'} = 'Snow depth';

dataTable.Properties.VariableUnits{'ALBDO'} = '%';
dataTable.Properties.VariableDescriptions{'ALBDO'} = 'Surface Albedo';

dataTable.Properties.VariableUnits{'TCDC'} = '%';
dataTable.Properties.VariableDescriptions{'TCDC'} = 'Entire atmosphere Total Cloud Coverage';

dataTable.Properties.VariableUnits{'DSWRF'} = 'W/m^2';
dataTable.Properties.VariableDescriptions{'DSWRF'} = 'Downward Short-Wave Radiation Flux';

% Plots

for i = 3:width(dataTable)
       
    myFigure = figure('visible', 'off');
    % stairs(dataTable.time_EU_Berlin - hours(1), dataTable{:,2},'LineWidth', 1.5);
    
    % Cope with 1 h aggregated data!
    stairs([dataTable.time_EU_Berlin - hours(1); dataTable.time_EU_Berlin(end)], [dataTable{:,i}; dataTable{end,i}],'LineWidth', 1.5);
    hold on;
    plot(dataTable.time_EU_Berlin, dataTable{:,i},'*');
    grid on;
    grid minor;
    %datetick('x','dd.mm HH:MM','keepticks');
    xtickangle(90);    
    %ylim([floor(min(dataTable{:,i})) ceil(max(dataTable{:,i}))]);
    xlabel(dataTable(:,2).Properties.VariableUnits{1});
    ylabel(dataTable(:,i).Properties.VariableUnits{1});
    title(strcat(dataTable(:,i).Properties.VariableNames{1},{' '}, strcat(dataTable(:,i).Properties.VariableDescriptions{1})));
    
    print(myFigure, dataTable(:,i).Properties.VariableNames{1}, '-dpng');
	
	pause(1.2);
	
    close(myFigure);

end

exit;