
# Using `corerun` To Run a .NET Application

The page [Using your .NET Runtime Build with dotnet cli](../using-dotnet-cli.md) gives detailed instructions on using the standard
command line host (that is, `dotnet.exe` or `dotnet`), and SDK to run an application with a local build of the
.NET Runtime. This is the preferred mechanism for you to officially deploy
your changes to other people since dotnet.exe and NuGet ensure that you end up with a consistent
set of binaries that can work together.

However, packing and unpacking the runtime binaries adds extra steps to the deployment process. When
working in a tight edit-build-debug loop, these extra steps become cumbersome.

For this tight edit-build-debug loop, there is a simplified alternative to `dotnet` called `corerun` which
does not know about NuGet at all. It just needs to find the .NET runtime (for example, `coreclr.dll`)
and any class library assemblies (for example, `System.Runtime.dll`, `System.IO.dll`, etc).

It does this using heuristics in the following order:

1. Check if the user passed the `--clr-path` argument.
1. Check if the `CORE_ROOT` environment variable is defined.
1. Check if the .NET runtime binary is in the same directory as the `corerun` binary.

Regardless of which method is used to discover the .NET runtime binary, its location is used to discover
both the .NET runtime binary and all base class library assemblies. Additional directories can be included
in the set of class library assemblies by defining the `CORE_LIBRARIES` environment variable.

The above heuristics can be used in a number of ways.

## Getting the class library from the shared system-wide runtime

Consider that you already have a .NET application assembly called `HelloWorld.dll` and wish to run it.
You could make such an assembly by using an officially installed .NET runtime with `dotnet new` and `dotnet build` in a `HelloWorld` directory.

If you execute the following on Windows, the `HelloWorld` assembly will be run.

```cmd
set PATH=%PATH%;<repo_root>\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root\
set CORE_LIBRARIES=%ProgramFiles%\dotnet\shared\Microsoft.NETCore.App\1.0.0

corerun HelloWorld.dll
```

On non-Windows platforms, setting environment variables is different but the logic is identical. For example, on macOS use `/usr/local/share` for `%ProgramFiles%`.

The `<repo_root>` represents the base of your dotnet/runtime repository. The first line puts the build output directory
(your OS, architecture, and buildType may be different) and thus the `corerun` binary on your path.
The second line tells `corerun` where to find class library assemblies. In this case we tell it to find them where
the installation of `dotnet` placed its copy. The version number in the path may be different depending on what
is currently installed on your system.

Thus when you run `corerun HelloWorld.dll`, `corerun` knows where to get the assemblies it needs.
Once you set the path and `CORE_LIBRARIES` environment variable, after a rebuild you can simply use
`corerun` to run your application &ndash; you don't have to move any binaries around.

## Using `corerun` to Execute a Published Application

When `dotnet publish` publishes an application, it deploys all the class libraries needed as well.
Thus if you simply change the `CORE_LIBRARIES` definition in the previous instructions to point at
that publication directory, but run the `corerun` from your build output, the effect will be that you
run your new runtime getting all the other code needed from that deployed application. This is
very convenient because you don't need to modify the deployed application in order to test
your new runtime.

## How CoreCLR Tests use `corerun`

The test build script (`src/tests/build.cmd` or `src/tests/build.sh`) sets up a directory where it
gathers the CoreCLR that has just been built with the pieces of the class library that tests need.
It places this runtime in the directory
`artifacts\tests\coreclr\<OS>.<Arch>.<BuildType>\Tests\Core_Root`
 starting at the repository root. The way the tests are expected to work is that you can set the environment
variable `CORE_ROOT` to this directory &ndash; you don't have to set `CORE_LIBRARIES` since the test environment has copied all base class libraries assemblies to this `Core_Root` directory &ndash; and you can run any test. For example, after building the tests
(running `src\tests\build` from the repository base), you can do the following on Windows to set up an environment where `corerun` can run any test.

```cmd
set PATH=%PATH%;<repo_root>\artifacts\Product\windows.x64.Debug
set CORE_ROOT=<repo_root>\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root
```
For example, the following runs the finalizeio test on Windows.

```cmd
corerun artifacts\tests\coreclr\windows.x64.Debug\GC\Features\Finalizer\finalizeio\finalizeio\finalizeio.dll
```

## Additional `corerun` options

The `corerun` binary is designed to be a platform agnostic tool for quick testing of a locally built .NET runtime.
This means the `corerun` binary must be able to feasibly exercise any scenario the official `dotnet` binary is capable
of. It must also be able to help facilitate .NET runtime development and investigation of test failures.
See `corerun --help` for additional details.

**Options**

`--clr-path <PATH>` - Pass the location of Core Root on the command line.
- For example, `corerun --clr-path /usr/scratch/private_build HelloWorld.dll`

`--property <PROPERTY>` - Supply a property to pass to the .NET runtime during initialization.
- For example, `corerun --property System.GC.Concurrent=true HelloWorld.dll`

`--debug` - Wait for a debugger to attach prior to loading the .NET runtime.
- For example, `corerun --debug HelloWorld.dll`

`--env` - Pass the path to a file in the [`dotenv`](https://github.com/motdotla/dotenv) format to specify environment variables for the test run.
- For example, `corerun --env gcstress.env HelloWorld.dll`.
