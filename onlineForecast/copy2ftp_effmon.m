%----------------------------------------------------------------
% function errorflag = copy2ftp_effmon(file, ftpfolder, FTPServer, User, Password) 
% 
% Kopiert Datei mit Filenamen "file" if FTP-Ordner "ftpfolder".
%
% FTPServer: default:  'ftp.iosb.fraunhofer.de'
% User, Password:  FTP-Login (default:  User = 'austausch', Password = 'austausch' )
%
% Bnd, 12/2010
%----------------------------------------------------------------

%                                     1         2       3        4      5
function errorflag = copy2ftp_effmon(file, ftpfolder, FTPServer, User, Password) 

if nargin < 3
    FTPServer = 'leute.server.de';
end
if nargin < 4
    User     = 'effmon';
    Password = 'effmon_iosb';
end

try
    f = ftp( FTPServer, User, Password );
 
    if ftpfolder(end)~='\'
	    ftpfolder = [ftpfolder '\'];
	end

	I = [0, strfind(ftpfolder,'\')];
  
    for i = 1:length(I)-1
        
        subfolder = ftpfolder([I(i)+1 : I(i+1)-1]);
        try cd(f, subfolder);
        	%disp(['CD FTP-folder:' subfolder])
        catch mkdir(f, subfolder);
            %disp(['FTP-folder erstellt:' subfolder])
            cd(f, subfolder);
        end
    end
   
    mput(f,file);
    disp(['  ' mfilename ': File "' file '" wurde auf FTP-Server ftp.iosb.fraunhofer.de unter "' ftpfolder '" gespeichert.'])
    errorflag = 1;
    close(f)
   
catch
    if exist('f') && ~isempty(f)  % falls FTP-Verbindung geöffnet wurde
        close(f);   % ... FTP-Verbindung trennen
    end
    [p, fname, ext] = fileparts(file);  % [PATHSTR,NAME,EXT,VERSN]
    subfolder = fullfile(p, 'copy2ftp_failed');
    newfile   = fullfile( subfolder, [fname ext]);
    if ~exist( subfolder,'dir')
        mkdir( subfolder );
    end
    copyfile( file, newfile);
    disp(['  ' mfilename ': ERROR : File "' file '" konnte NICHT auf FTP-Server gespeichert werden.'])
    disp(['  -> File wurde kopiert in Unterordner \' subfolder])

 	errorflag = 0;
end  
