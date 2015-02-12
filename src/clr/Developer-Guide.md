# Building the repository #

CoreCLR repo can be built from a regular, non-admin command prompt. Currently, the repo supports building CoreCLR.dll (and its various native binaries), mscorlib.dll and the accompanying tests for the following platforms and build configurations:

**Windows**

- X64 - Debug and Release

We have work in progress to bring more tests online and support more platforms in the near future. As can be seen from the repo home page (the Linux build status labels), we are bringing up Linux support. Once the engineering support (e.g. build and validation) is functionally complete, we will share the details on how to build and test the product on Linux.

## Prerequisites ##

1. Visual Studio must be installed. Supported versions:
    - [Visual Studio Community 2013](http://go.microsoft.com/fwlink/?LinkId=517284) - **Free** for Open Source development!
    - [Visual Studio 2013](http://www.visualstudio.com/downloads/download-visual-studio-vs) (Pro, Premium, Ultimate)
    - Visual Studio Express isn't supported for building CoreCLR
2. Install [Cmake](http://www.cmake.org/download/ "CMake") 3.0.2 for Windows and make sure it is present in PATH environment variable for the system.
3. Powershell should be installed.
4. Tools required to work with Git are installed (e.g. [Git for Windows](http://msysgit.github.io/), [GitHub for Windows](https://windows.github.com/))

**Known Issues**

If you installed VS 2013 after VS 2012, then DIA SDK gets incorrectly installed to the VS 2012 install folder instead of VS 2013 install folder. This will result in a build break. To workaround this [issue](https://connect.microsoft.com/VisualStudio/feedback/details/814147/dia-sdk-installed-into-wrong-directory), copy `%program files (x86)%\Microsoft Visual Studio 11.0\DIA SDK` to  `%program files (x86)%\Microsoft Visual Studio 12.0\DIA SDK` and restart the build. More details are [here](http://support.microsoft.com/kb/3035999).

**Git Configuration**

The repository is configured to allow Git to make the right decision about handling CRLF. Specifically, if you are working on **Windows**, please ensure that **core.autocrlf** is set to **true**. On **non-Windows** platforms, please set it to **input**.

**Building the repository**

1. Fork the CoreCLR Repo
2. Clone the forked repo on your development machine
3. Open a new command prompt and navigate to the root of the cloned repo.
4. Invoke "build.cmd clean"

This will do a clean x64/Debug build of CoreCLR, its native components, Mscorlib and the tests. 


- Product Binaries will be dropped in `<repo_root>\Binaries\Product\<arch>\<flavor>` folder. 
- A Nuget package, Microsoft.Dotnet.CoreCLR, will also be created under `<repo_root>\Binaries\Product\<arch>\<flavor>\.nuget` folder. 
- Test binaries will be dropped under `<repo_root>\Binaries\Tests\<arch>\<flavor>` folder

Doing **build /?** will give details on the supported parameters.


## Building and running tests ##

**Building Tests**        

In a clean command prompt, issue the following command: 

    <repo_root>\tests\buildtest.cmd x64 release clean

**Note:** The above command (or building from the repo_root) must be done once, at the least, to ensure that all test dependencies are initialized correctly. 

In Visual Studio, open `<repo_root>\tests\src\AllTestProjects.sln`, build all the test projects or the one required.

**Running Tests**

In a clean command prompt: `<repo_root>\tests\runtest.cmd x64 release <Absolute path to previously built product binaries>`

This will generate the report named as `TestRun_<arch>_<flavor>.html` (e.g. `TestRun_x64__release.html`) in the current folder. It will also copy all the test dependencies to the folder passed at the command line.

**Investigating Test Failures**

Upon completing a test run, you may find one or more tests have failed.

The output of the Test will be available in Test reports directory, but the default the directory would be something like is `<repo_root>\binaries\tests\x64\debug\Reports\Exceptions\Finalization`.

There are 2 files of interest: 

- `Finalizer.output.txt` - Contains all the information logged by the test.
- `Finalizer.error.txt`  - Contains the information reported by CoreRun.exe (which executed the test) when the test process crashes.

**Rerunning a failed test**

If you wish to re-run a failed test, please follow the following steps:

1. Set an environment variable, `CORE_ROOT`, pointing to the path to product binaries that was passed to runtest.cmd. The command to set this environment variable is also specified in the test report for a failed test.
2. Next, run the failed test, the command to which is also present in the test report for a failed test. It will be something like `<repo_root>\binaries\tests\x64\debug\Exceptions\Finalization\Finalizer.cmd`.

If you wish to run the test under a debugger (e.g. [WinDbg](http://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx)), append `-debug <debuggerFullPath>` to the test command. For example:


     <repo_root>\binaries\tests\x64\debug\Exceptions\Finalization\Finalizer.cmd -debug <debuggerFullPath>
    
**Modifying a test**

If test changes are needed, make the change and build the test project. This will binplace the binaries in test binaries folder (e.g. `<repo_root>\binaries\tests\x64\debug\Exceptions\Finalization`). At this point, follow the steps to re-run a failed test to re-run the modified test.

**Authoring Tests (in VS)**

1. Use an existing test such as `<repo_root>\tests\src\Exceptions\Finalization\Finalizer.csproj` as a template and copy it to a new folder under `<repo_root>\tests\src`.
2. Add the project of the new test to `<repo_root>\tests\src\AllTestProjects.sln` in VS
3. Add source files to this newly added project.
4. Indicate the success of the test by returning `100`.
5. Add the .NET CoreFX contract references, as required, via the Nuget Package Manager in Visual Studio. *Make sure this does not change the csproj. If it does, then undo the change in the csproj.*
6. Add any other projects as a dependency, if needed.
7. Build the test.
8. Follow the steps to re-run a failed test to validate the new test.


## Debugging ##

**Debugging CoreCLR**



1. Perform a build of the repo.
2. Open <repo_root>\binaries\Cmake\CoreCLR.sln in VS.
3. Right click the INSTALL project and choose ‘Set as StartUp Project’
4. Bring up the properties page for the INSTALL project
5. Select Configuration Properties->Debugging from the left side tree control
6. Set Command=`$(SolutionDir)..\product\$(Platform)\$(Configuration)\corerun.exe`
	1. This points to the folder where the built runtime binaries are present.
7. Set Command Arguments=`<managed app you wish to run>` (e.g. HelloWorld.exe)
8. Set Working Directory=`$(SolutionDir)..\product\$(Platform)\$(Configuration)`
	1. This points to the folder containing CoreCLR binaries.
9. Press F11 to start debugging at wmain in corerun (or set a breakpoint in source and press F5 to run to it)
	1. As an example, set a breakpoint for the EEStartup function in ceemain.cpp to break into CoreCLR startup.

Steps 1-8 only need to be done once, and then (9) can be repeated whenever you want to start debugging. The above can be done with Visual Studio 2013.

**Debugging Mscorlib and/or managed application**

To step into and debug managed code of Mscorlib.dll (or the managed application being executed by the runtime you built), using Visual Studio, is something that will be supported with Visual Studio 2015. We are actively working to enable this support. 

Until then, you can use [WinDbg](https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx) and [SOS](https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx) (an extension to WinDbg to support managed debugging) to step in and debug the generated managed code. This is what we do on the .NET Runtime team as well :)