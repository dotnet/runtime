Debugging CoreCLR on Windows
============================

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

Debugging Mscorlib and/or managed application
=============================================

To step into and debug managed code of Mscorlib.dll (or the managed application being executed by the runtime you built), using Visual Studio, is something that will be supported with Visual Studio 2015. We are actively working to enable this support. 

Until then, you can use [WinDbg](https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx) and [SOS](https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx) (an extension to WinDbg to support managed debugging) to step in and debug the generated managed code. This is what we do on the .NET Runtime team as well :)
