# OpcDaAgent
OpcDaAgent is the Windows service which acts as the OPC DA client and .Net Remoting server to provide data for the OpenHistorian universal adapter (https://github.com/Rotabor/OpenHistorianRemoteDataAdapter).
It uses OPC Foundation Classic OPC .NET libraries.

**Concept**

![openHistorian Web Interface](https://github.com/Rotabor/OpcDaAgent/blob/master/GitHubResources/OpenHistorianDataRemotingAdapter.png)

**Disclaimer**: This code is provided as is, without any warranty or obligation. It requires you to have knowledge of C# programming, openHistorian and other products/libraries/technologies in use. It has to be compiled.

## How to use:
1. Compile source code - refer to properly compiled OPC libraries or appropriate DLLs. The target platform should match OPC DA server's platform in bit numbers (32 or 64). .NET Framework should be available or installed on the target machine.
2. On the target machine, create a folder for service files 
3. Copy all files from bin directory into the folder
4. Adjust parameters in 'OpcDaAgent.exe.config':
    - **logpath** - the path to the log file. Should be accessibale for write for the account under which the service is run
    - **port** - the tcp port number used both for inbound and outbound connections, should match port's number used in the OpenHistorian universal adapter
    - **remotehost** - the of OpenHistorian universal adapter's host 
    - **host** - OPC DA server's host, typically is the local server, but the remote server can be referred 
    - **server** - the name of the OPC DA server.
5. Install the service on the target machine (command-line prompt):
    - Sc create OpcDaAgent binPath= FOLDERPATH\OpcDaAgent.exe DisplayName= OpcDaAgent start= auto obj= .\USERNAME password= PASSWORD
6. Start OpcDaAgent service
7. Examine the log file to see if there are any errors.
