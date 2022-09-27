@echo off

rem ------ Call .m or Matlab App file from cmd line -------

rem pushd P:\dynOptEn\SOFTWARE\GFSdecoder\

rem ------ Command to execute: 

rem matlab -nosplash -nodesktop -minimize -r plotForecast

matlab -nosplash -nodesktop -minimize -r "cd P:\dynOptEn\SOFTWARE\GFSdecoder\; pause(1); copy2ftp_effmon('GFS_IOSB.dat', 'weatherforecast'); pause(1); plotForecast; pause(20); exit;"

TIMEOUT 3

rem matlab -nosplash -nodesktop -minimize -r matlab.apputil.run('appFile')

rem popd