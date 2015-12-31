=================================================================================================
	win2tiz (c) katatunix@gmail.com, Summer 2014 - FIFA World Cup Brazil
	(Since Spring 2013)
=================================================================================================

This tool will help you execute commands faster by distributing the commands to dedicated servers.

Current supported commands:
	- GCC/G++ compile commands
	- PVRTexTool.exe (old version)
	- PVRTexTool34.exe (new version)
	- etcpack.exe (note that currently etcpack.exe itself cannot runs parallel due to the conflict of temporary files,
					waiting for ARM Mali to fix this)

=================================================================================================

HOW TO USE (FOR DEVELOPERS)

- Just open the file win2tiz.sln by Visual Studio 2013+ and build.
- After building, running deploy.bat will copy the full release build to the "build" folder

=================================================================================================

HOW TO USE (FOR USERS)

1. Setup your dedicated severs:
	- Copy the folder win2tiz to each server, the path should be short and MUST NOT contain spaces (e.g C:\win2tiz\)
	- Copy your tool (e.g. PVRTexTool) to a identical path (e.g. Y:/PVRTexTool/PVRTexTool.exe) for all severs
	- In each server, start the service by running: mongcc.exe --start -port <number>
		For example: mongcc.exe --start -port 1909
	- You can specify the temp folder with the argument -tempdir
	- Run mongcc.exe with empty argument to see more usage

2. Clients distribute commands:
	- In each client, set following environment variables (example):
		set MONGCC_ENABLE=1 // if set to 0, then all commands will be executed locally, no need servers
		set MONGCC_MAX_JOBS=4 // how many commands will be executed parallel, typical 4
		set MONGCC_HOSTS=servername1 servername2 servername3 // computer names of the servers, separated by spaces
		set MONGCC_PORT=1909 // the port number, must be matched with the one on servers
	
	- In each client, export all your commands to a text file, each command is on a line, then execute:
		win2tiz.exe --cmd -file <your text file> -jobs <number>
		For example: win2tiz.exe --cmd -file D:\myfile.txt -jobs 4
		
		This will distribute your commmands to the servers and receive the results, in a parallel way.
		The jobs number should not be greater than MONGCC_MAX_JOBS, otherwise you will get an error message.
	
	- An example of the text file that contains PVRTexTool commands:
		Y:/PVRTexTool/PVRTexTool.exe -fETC -iE:\MyTexture1.tga -oE:\MyTexture1.pvr
		Y:/PVRTexTool/PVRTexTool.exe -fETC -iE:\MyTexture2.tga -oE:\MyTexture2.pvr
		//Y:/PVRTexTool/PVRTexTool.exe -fETC -iE:\MyTexture3.tga -oE:\MyTexture3.pvr
		//Commands that are empty or begin with // will be ignored.
	
	- If you are using Visual Studio and want to compile source files declared in VS, you can call:
		win2tiz.exe -i <str> [-t <str>] [-p <str>] [-g <str>] [-v] [-j <num>] [-o <str>]
	
	- Run win2tiz.exe and mongcc.exe with empty argument to see their usage.
	
NOTE

- The path to the tool (Y:/PVRTexTool/PVRTexTool.exe) must be the same for all clients and servers.
- If you are using the new version (3.4) of PVRTexTool, please rename it to PVRTexTool34.exe
