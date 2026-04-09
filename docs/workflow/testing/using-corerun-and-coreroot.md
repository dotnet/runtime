# Using Corerun To Run a .NET Application

* [Introduction](#introduction)
* [The CoreRun](#the-corerun)
  * [Running Apps with CoreRun](#running-apps-with-corerun)
    * [Using CoreRun with the class library from the shared system-wide .NET installation](#using-corerun-with-the-class-library-from-the-shared-system-wide-net-installation)
    * [Using CoreRun to Execute a Published Self-Contained Application](#using-corerun-to-execute-a-published-self-contained-application)
* [The Core_Root](#the-core_root)
  * [Additional CoreRun Options](#additional-corerun-options)

This guide will walk you through using the Corerun and Core_Root your own build from the runtime repo for testing, running apps, and so on. This doc assumes you've already built at least the _clr_ subset of the repo, and have the binaries under `artifacts/bin/coreclr/<OS>.<arch>.<configuration>`. If this is not your case, the [CoreCLR building docs](/docs/workflow/building/coreclr/README.md) have detailed instructions on how to get these artifacts.

## Introduction

To run a .NET app with the runtime you've built, you will need a _host_ program that will load the runtime, as well as all the other .NET libraries that your application might need. There are three main ways to go about this:

* Use your machine's installed .NET SDK and replace the necessary binaries in a self-contained app.
* Use your build's _Dev Shipping Packages_ to run your app.
* Use the _CoreRun_ host generated as part of your build's artifacts.

This guide focuses on the third of the bullet points described above. For the other two, we have docs dedicated to them:

* [Using your build with your machine's installed SDK](using-your-build-with-installed-sdk.md)
* [Using your build's dev shipping packages](using-dev-shipping-packages.md)

## The CoreRun

The `corerun` binary is designed to be a platform agnostic tool for quick testing of a locally built .NET runtime. It helps facilitate .NET runtime development and investigation of test failures. This method is the most recommended one when you are making lots of changes that you want to keep continually testing and debugging, since it's the fastest way to apply them.

`Corerun` does not know about NuGet at all. It just needs to find the .NET runtime, `coreclr.dll`, `libcoreclr.dylib`, or `libcoreclr.so` depending on your platform, and any class library assemblies like for example, `System.Runtime.dll`, `System.IO.dll`, and so on.

`Corerun` achieves these goals by using heuristics in the following order:

1. Check if the user passed the `--clr-path` argument.
2. Check if the `CORE_ROOT` environment variable is defined.
3. Check if the .NET runtime binary is in the same directory as the `corerun` binary.

Regardless of which method is used to discover the .NET runtime binary, its location is used to also find all of the base class library assemblies. Additional directories can be included in the set of class library assemblies by defining the `CORE_LIBRARIES` environment variable.

The above heuristics can be used in a number of ways, providing you with multiple options to test using your `corerun`.

### Running Apps with CoreRun

In the following subsections, we will describe how to run any apps you might create, but using your built runtime instead of the one installed on your machine.

#### Using CoreRun with the class library from the shared system-wide .NET installation

For this example, let's create a simple _Hello World_ app:

```cmd
mkdir HelloWorld && cd HelloWorld
dotnet new console
dotnet build
```

Now, instead of running our app the usual way, we will use `corerun` to execute it using our build of the runtime. The `corerun` executable is created as part of building the `clr` subset, and it will exist in the `<repo root>/artifacts/bin/coreclr/<OS>.<Arch>.<Configuration>` folder. For this, we will follow the steps denoted below:

* First we will add `corerun`'s folder to the `PATH` environment variable for ease of use. Note that you can always skip this step and fully qualify the name instead.
  * This example assumes you built on the _Debug_ configuration for the _x64_ architecture. Make sure you adjust the path accordingly to your kind of build.
* Then, we also need the libraries. Since we only built the runtime, we will tell `corerun` to use the ones shipped with .NET's default installation on your machine.
  * This example assumes your default .NET installation's version is called "_7.0.0_". Same deal as with your runtime build path, adjust to the version you have installed on your machine.
* Afterwards, we can finally run our app.

On Windows Command Prompt:

```cmd
set PATH=%PATH%;<repo_root>\artifacts\bin\coreclr\windows.x64.Debug
set CORE_LIBRARIES=%ProgramFiles%\dotnet\shared\Microsoft.NETCore.App\7.0.0
corerun HelloWorld.dll
```

On macOS and Linux:

```bash
# Change osx to linux if you're on a Linux machine.
export PATH="$PATH:<repo_root>/artifacts/bin/coreclr/osx.x64.Debug"
export CORE_LIBRARIES="/usr/local/share/dotnet/shared/Microsoft.NETCore.App/7.0.0"
corerun HelloWorld.dll
```

On PowerShell:

```powershell
# Note the '+=' since we're appending to the already existing PATH variable.
# Also, replace the ';' with ':' if on Linux or macOS.
$Env:PATH += ';<repo_root>\artifacts\bin\coreclr\windows.x64.Debug'
$Env:CORE_LIBRARIES = %ProgramFiles%\dotnet\shared\Microsoft.NETCore.App\7.0.0
corerun HelloWorld.dll
```

Once you set the `PATH` and `CORE_LIBRARIES` environment variables, when you issue `corerun HelloWorld.dll` following the snippets above, `corerun` now knows where to get the assemblies it needs. Note that this setup only has to be done once, as long as you stay in the same terminal instance. After a rebuild with more changes you might make, you can simply rerun `corerun` directly to run your application. The stage is set for it to work as expected.

#### Using CoreRun to Execute a Published Self-Contained Application

When an application is published as self-contained (`dotnet publish --self-contained`), it deploys all the class libraries needed as well. Thus if you simply change the `CORE_LIBRARIES` defined in the previous section to point at that publication directory, then the effect will be that your `corerun` will be getting all that libraries' code from your deployed application.

## The Core_Root

The test build script (`src/tests/build.cmd` or `src/tests/build.sh`) sets up a directory where it gathers the CoreCLR that has just been built with the pieces of the class libraries that the tests need. It places these binaries in the directory `artifacts/tests/coreclr/<OS>.<Arch>.<Configuration>/Tests/Core_Root`. Note that the test building process is a lengthy one, so it is recommended to only generate the Core_Root with the `-generatelayoutonly` flag to the tests build script, and build individual tests and/or test trees as you need them.

**NOTE**: In order to generate the Core_Root, you must also have built the libraries beforehand with `-subset libs`. Running the tests build script by default searches the libraries in _Release_ mode, regardless of the runtime configuration you specify. If you built your libraries in another configuration, then you have to pass down the appropriate flag `/p:LibrariesConfiguration=<your_config>`. More details in the [testing CoreCLR doc](/docs/workflow/testing/coreclr/testing.md).

Once you have your Core_Root, it's just a matter of calling it directly or adding it to your `PATH` environment variable, and you're ready to run your apps with it.

On Windows Command Prompt:

```cmd
set PATH=%PATH%;<repo_root>\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root
corerun HelloWorld.dll
```

On macOS and Linux:

```bash
# Change linux to osx if you're on a macOS machine.
export PATH="$PATH:<repo_root>/artifacts/tests/coreclr/linux.x64.Debug/Tests/Core_Root"
corerun HelloWorld.dll
```

On PowerShell:

```powershell
# Note the '+=' since we're appending to the already existing PATH variable.
# Also, replace the ';' with ':' if on Linux or macOS.
$Env:PATH += ';<repo_root>\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root'
corerun HelloWorld.dll
```

The advantage of generating the Core_Root, instead of sticking to the _corerun_ from the _clr_ build, is that you can also test and debug libraries at the same time.

### Additional CoreRun Options

The `corerun` binary has a few optional command-line arguments described when issuing `corerun --help`:

* `--clr-path <PATH>`: Pass the location of Core_Root on the command line. You can omit this flag if either the `corerun` you're using is within the Core_Root folder, or you have set the Core_Root's path by means of the `CORE_ROOT` environment variable.
  * Example: `corerun --clr-path /path/to/core_root HelloWorld.dll`
* `--property <PROPERTY>`: Supply a property to pass to the .NET runtime during initialization.
  * Example: `corerun --property System.GC.Concurrent=true HelloWorld.dll`
* `--debug`: Wait for a debugger to attach prior to loading the .NET runtime.
  * Example: `corerun --debug HelloWorld.dll`
* `--env`: Pass the path to a `.env` file to specify environment variables for the test run. More info about `dotenv` can be found in [their repo](https://github.com/motdotla/dotenv).
  * For example, `corerun --env gcstress.env HelloWorld.dll`
