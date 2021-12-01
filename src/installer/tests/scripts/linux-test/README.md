This project has the purpose to automate verification test for .NET Runtime and SDK linux packages.

To have this test running in your local machine do the following steps:
1. Download VerificationTestOnDocker.sh, RuntimeInstallation.sh, SdkInstallation.sh, images.txt in the same folder
2. Update images.txt with images name you want to run the installation test
3. Run $ ./VerificationTestOnDocker.sh \<package> \<version> \<command>

The options are:

* \<package>
   * runtime - verification test for .NET Runtime Linux packages
   * sdk - verification test for .NET SDK Linux packages
* \<version>
  * latest - install the latest available .NET package from our main repository
  * \<number> - install the package corresponding to this version number
* \<command>
  * install - verification test for install
  * install uninstall - verification test for install and uninstall


The script VerificationTestOnDocker.sh is responsible for read a file (images.txt) containing docker images and run a docker container for each image specified in that file. Inside each container it will be executed the script to install .NET Runtime (RuntimeInstallation.sh) or .NET SDK (SdkInstallation.sh).

Both scripts RuntimeInstallation.sh and SdkInstallation.sh automatically identify what distro and version is running in the current machine and can install and uninstall the latest version of .NET Runtime/Sdk packages corresponding to that distro & version. The installation's stdout for all containers is redirected to a single file (logfile.txt). In the end of this file (logfile.txt) it's also displayed the results of the test, printing for each distro and version the result 'failed' or 'passed'.

.NET packages are downloaded from the blob https://dotnetcli.blob.core.windows.net/dotnet

This project takes in account:
  -> dotnet-sdk depends on dotnet-runtime and aspnet-runtime
  -> aspnet-runtime depends on dotnet-runtime (can be different to what dotnet-sdk depends on)
  -> dotnet-runtime-deps depends on system packages
  -> .NET runtime carries: dotnet-runtime-deps, dotnet-host, dotnet-hostfxr and dotnet-runtime.

Changes on how dotnet runtime packages are structured or modification on the packages dependencies may affect the verification test result.

This verification test depends on docker images and the test result can be a false negative if the image doesn't carry some system packages required to have a proper runtime package installation.


The script allows automated test only for the following distro & version:

| Distro | Version |
|--------|---------|
| Ubuntu | 14.04, 16.04, 18.04 |
| Debian | 8, 9 |
| Centos | 7 |
| Fedora | 27 |
| OpenSUSE | 42 |
| Oracle Linux | 7 |
| RHEL | 7 |
| SLES | 12 |
