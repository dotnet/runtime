
# Using `corerun` To Run a .NET Application

In page [Using your .NET Runtime Build with dotnet cli](../using-dotnet-cli.md), it gives detailed instructions on using the standard
command line host (that is, `dotnet.exe` or `dotnet`), and SDK to run an application with the modified local build of the
.NET Runtime. This is the preferred mechanism for you to officially deploy
your changes to other people since dotnet.exe and NuGet insure that you end up with a consistent
set of binaries that can work together.

However, packing and unpacking the runtime binaries adds extra steps to the deployment process and when
you are in the tight code-build-debug loop these extra steps are cumbersome.

For this situation there is an alternative host to `dotnet` called `corerun` that is well suited
for this. It does not know about NuGet at all, and has very simple rules.  It needs to find the
.NET runtime (for example, `coreclr.dll`) and additionally any class library assemblies (for example, `System.Runtime.dll`, `System.IO.dll`, etc).

It does this using heuristics in the following order:

1. Check if a the `--clr-path` argument is set.
1. See if the `CORE_ROOT` environment variable is defined.
1. Check if the .NET runtime binary is in the same directory as the `corerun` binary.

However, the .NET runtime binary is discovered its location is defined as "Core Root". The Core Root directory
is used to discover not only the .NET runtime binary but also all class library assemblies. Additional
directories to be included in the set of class library assemblies can be provided using the `CORE_LIBRARIES`
environment variable.

The above rules can be used in a number of ways.

## Getting the class library from the shared system-wide runtime

Consider that you already have a .NET application assembly called `HelloWorld.dll` and wish to run it.
You could make such an assembly by using an officially installed .NET runtime with `dotnet new` and `dotnet build` in a `HelloWorld` directory.

If you execute the following on Windows, the `HelloWorld` assembly will be run.

```cmd
set PATH=%PATH%;%CoreCLR%\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root\
set CORE_LIBRARIES=%ProgramFiles%\dotnet\shared\Microsoft.NETCore.App\1.0.0

corerun HelloWorld.dll
```

On non-Windows platforms, setting environment variables is different but the logic is identical. For example, on Linux use `/usr/share` for `%Program Files%`.

The `%CoreCLR%` represents the base of your dotnet/runtime repository. The first line puts the build output directory (your OS, architecture, and buildType
may be different) and thus the `corerun` binary on your path.
The second line tells `corerun` where to find class library assemblies, in this case we tell it
to find them where the installation of `dotnet` placed its copy. Note the version number in the path above may change depending on what is currently installed on your system.

Thus when you run `corerun HelloWorld.dll`, `corerun` knows where to get the assemblies it needs.
Notice that once you set up the path and the `CORE_LIBRARIES` environment, after a rebuild you can simply use `corerun` to run your
application &ndash; you don't have to move any binaries around.

## Using `corerun` to Execute a Published Application

When `dotnet publish` publishes an application it deploys all the class libraries needed as well.
Thus if you simply change the `CORE_LIBRARIES` definition in the previous instructions to point at
that publication directory but run the `corerun` from your build output the effect will be that you
run your new runtime getting all the other code needed from that deployed application. This is
very convenient because you don't need to modify the deployed application in order to test
your new runtime.

## How CoreCLR Tests use `corerun`

When you execute `runtime/src/tests/build.cmd` on Windows one of the things that it does is set up a directory where it
gathers the CoreCLR that has just been built with the pieces of the class library that tests need.
It places this runtime in the directory
`artifacts\tests\coreclr\<OS>.<Arch>.<BuildType>\Tests\Core_Root`
 starting at the repository root. The way the tests are expected to work is that you can set the environment
variable `CORE_ROOT` to this directory &ndash; you don't have to set `CORE_LIBRARIES` since the test environment has copied all base class libraries assemblies to this `Core_Root` directory &ndash; and you can run any test. For example, after building the tests
(running `src\tests\build` from the repository base) and running `src\tests\run`) you can do the following on Windows:

```cmd
set PATH=%PATH%;%CoreCLR%\artifacts\Product\windows.x64.Debug
set CORE_ROOT=%CoreCLR%\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root
```
sets you up so that `corerun` can run any of the tests. For example, the following runs the finalizerio test on Windows.

```cmd
corerun artifacts\tests\coreclr\windows.X64.Debug\GC\Features\Finalizer\finalizeio\finalizeio\finalizeio.exe
```

## Additional `corerun` options

The `corerun` binary is designed to be a platform agnostic tool for quick testing of a locally build .NET runtime.
This means the `corerun` binary must be able to feasibly exercise any scenario the official `dotnet` binary is capable of.
It must also be able to help facilitate .NET runtime development and investigation of test failures.
See `corerun -h` for usage details.

**Options**

`--clr-path <PATH>` - Pass the location of Core Root on the command line.
- For example, `corerun --clr-path /usr/scratch/private_build HelloWorld.dll`

`--property <PROPERTY>` - Supply a property to pass to the .NET runtime during initialization.
- For example, `corerun --property System.GC.Concurrent=true HelloWorld.dll`

`--debug` - Wait for a debugger to attach prior to loading the .NET runtime.
- For example, `corerun --debug HelloWorld.dll`
