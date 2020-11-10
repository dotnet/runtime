
# Using corerun To Run .NET Application

In page [Using your .NET Runtime Build with dotnet cli](../using-dotnet-cli.md) gives detailed instructions on using the standard
command line host and SDK, dotnet.exe to run an application with the modified build of the
.NET Runtime built here.   This is the preferred mechanism for you to officially deploy
your changes to other people since dotnet.exe and Nuget insure that you end up with a consistent
set of DLLs that can work together.

However packing and unpacking the runtime DLLs adds extra steps to the deployment process and when
you are in the tight code-build-debug loop these extra steps are an issue.

For this situation there is an alternative host to dotnet.exe called corerun.exe that is well suited
for this.   It does not know about Nuget at all, and has very simple rules.  It needs to find the
.NET runtime (that is coreclr.dll) and additionally any class library DLLs (e.g. System.Runtime.dll  System.IO.dll ...).

It does this by looking at two environment variables.


 * `CORE_ROOT` - The directory where to find the runtime DLLs itself (e.g. CoreCLR.dll).
 Defaults to be next to the corerun.exe host itself.
 * `CORE_LIBRARIES` - A directory to look for DLLS to resolve any assembly references.
 It defaults CORE_ROOT if it is not specified.

These simple rules can be used in a number of ways

## Getting the class library from the shared system-wide runtime

Consider that you already have a .NET application DLL called HelloWorld.dll and wish to run it
(You could make such a DLL by using 'dotnet new' 'dotnet restore' 'dotnet build' in a 'HelloWorld' directory).

If you execute the following
```bat
    set PATH=%PATH%;%CoreCLR%\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root\
    set CORE_LIBRARIES=%ProgramFiles%\dotnet\shared\Microsoft.NETCore.App\1.0.0


    corerun HelloWorld.dll
```

for Linux  use /usr/share for %Program Files%

Where %CoreCLR% is the base of your CoreCLR repository, then it will run your HelloWorld. application.
You can see why this works.  The first line puts build output directory (Your OS, architecture, and buildType
may be different) and thus corerun.exe you just built is on your path.
The second line tells corerun.exe where to find class library files, in this case we tell it
to find them where the installation of dotnet.exe placed its copy.   (Note that version number in the path above may change)

Thus when you run 'corerun HelloWorld.dll' Corerun knows where to get the DLLs it needs.   Notice that once
you set up the path and CORE_LIBRARIES environment, after a rebuild you can simply use corerun to run your
application (you don't have to move DLLs around)

## Using corerun.exe to Execute a Published  Application

When 'dotnet publish' publishes an application it deploys all the class libraries needed as well.
Thus if you simply change the CORE_LIBRARIES definition in the previous instructions to point at
that publication directory but RUN the corerun from your build output the effect will be that you
run your new runtime getting all the other code needed from that deployed application.   This is
very convenient because you don't need to modify the deployed application in order to test
your new runtime.

## How CoreCLR Tests use corerun.exe

When you execute 'runtime/src/tests/build.cmd' one of the things that it does is set up a directory where it
gathers the CoreCLR that has just been built with the pieces of the class library that tests need.
It places this runtime in the directory
```bat
    runtime\artifacts\tests\coreclr\<OS>.<Arch>.<BuildType>\Tests\Core_Root
```
off the CoreCLR Repository.    The way the tests are expected to work is that you set the environment
variable CORE_ROOT to this directory
(you don't have to set CORE_LIBRARIES) and you can run any tests.  For example after building the tests
(running src\tests\build from the repository base) and running 'src\tests\run') you can do the following

```bat
    set PATH=%PATH%;%CoreCLR%\artifacts\Product\windows.x64.Debug
    set CORE_ROOT=%CoreCLR%\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root
```
sets you up so that corerun can run any of the test.   For example
```bat
    corerun artifacts\tests\coreclr\windows.X64.Debug\GC\Features\Finalizer\finalizeio\finalizeio\finalizeio.exe
```
runs the finalizerio test.
