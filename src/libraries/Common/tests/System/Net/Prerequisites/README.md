# System.Net Test Prerequisites
Contains source files for the networking test servers in Azure or a private IIS deployment.

## Deployment Instructions for a private multi-machine environment

### Configuration

1. Open .\Deployment\config.ps1 in an editor.
2. Fill in the _Machine Selection_ section with the names and IP addresses of the target machines. In most cases the default options for _Test Parameters_ should be enough.

Note: the `config.ps1` file has been added to .gitignore to prevent it being updated in the master branch.

### Build the server applications 

Prepare the $COREFX_NET_CLIENT_Machine as any Dev station following the instructions at https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md. Ensure that you can build and test CoreFX on this machine.
In addition, you will also need to install the _Azure development_ workload for Visual Studio 2017.

From a Visual Studio command prompt:

```
    powershell
    cd .\Servers
    .\buildAndPackage.ps1
```

You should now find a folder named `IISApplications` within the Deployment folder.

### (only once) Create the Active Directory and join all machines

Skip this step if previously completed and all machines are already part of a domain to which you have Administrator rights.
This will join all machines to a test Active Directory and enable Windows Remoting.

1. Copy the Deployment folder to each of the machines. 
2. Run the .\setup.ps1 script on the machine designated to become the Domain Controller. Once complete, the machine will reboot.
3. Run the .\setup.ps1 script on all other domain joined machines. Once complete, the machines will reboot.

### Install or Update the environment

Running as the Active Directory Administrator, run .\setup.ps1 from any of the machines within the environment.
The script will use WinRM to connect and update all other roles.

## Deployment Instructions to update the Azure-based environment

1. Create a _Classic_ Azure WebService role.
2. Create a server certificate and add it to the subscription with the name: `CoreFxNetCertificate`
3. Edit `Servers\CoreFxNetCloudService\CoreFxNetCloudService\ServiceConfiguration.Cloud.cscfg` and ensure that the `CoreFxNetCertificate` `thumbprint` and `thumbprintAlgorithm` are correct.
4. Open the solution in Visual Studio and Run the Azure Publishing wizard to create and deploy the application.
