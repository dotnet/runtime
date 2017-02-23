
# Using your .NET Core Runtime Build

We assume that you have successfully built CoreCLR repository and thus have files of the form
```
    bin\Product\<OS>.<arch>.<flavor>\.nuget\pkg\Microsoft.NETCore.Runtime.CoreCLR.<version>.nupkg
```
And now you wish to try it out.  We will be using Windows OS as an example and thus will use \ rather
than / for directory separators and things like Windows_NT instead of Linux but it should be
pretty obvious how to adapt these instructions for other operating systems.

To run your newly built .NET Core Runtime in addition to the application itself, you will need
a 'host' program that will load the Runtime as well as all the other .NET Core Framework
code that your application needs. The easiest way to get all this other stuff is to simply use the
standard 'dotnet' host that installs with .NET Core SDK.

The released version of 'dotnet' tool may not be compatible with the live CoreCLR repository. The following steps
assume use of a dogfood build of the .NET SDK.

## Acquire the latest nightly .NET Core 2.0 SDK

- [Win 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-win-x64.latest.zip)
- [macOS 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-osx-x64.latest.tar.gz)
- [Others](https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries)

To setup the SDK download the zip and extract it somewhere and add the root folder to your [path](../building/windows-instructions.md#adding-to-the-default-path-variable)
or always fully qualify the path to dotnet in the root of this folder for all the instructions in this document.

After setting up dotnet you can verify you are using the newer version by:

`dotnet --info` -- the version should be greater than 2.0.0-*

For another small walkthrough see [Dogfooding .NET Core 2.0 SDK](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md).

## Create sample self-contained application

At this point you can create a new 'Hello World' program in the standard way.

```bat
mkdir HelloWorld
cd HelloWorld
dotnet new console
```

### Change project to be self-contained

In order to update with your local changes the application needs to be self-contained, as opposed to running on the
shared framework.  In order to do that you will need to add a `RuntimeIdentifier` to your project.

```
  <PropertyGroup>
    ...
    <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
  </PropertyGroup>
```

For windows you will want `win7-x64` but for other OS's you will need to set it to the most appropriate one based
on what you built. You can generally figure that out by looking at the packages you found in your output. In our
example you will see there is a package with the name `runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR.2.0.0-beta-25023-0.nupkg`
so you will want to put whatever id is between `runtime.` and `Microsoft.NETCore.Runtime.CoreCLR`.

Next you need to restore and publish. The publish step will also trigger a build but you can iterate on build by calling `dotnet build` as
needed.

```bat
dotnet restore
dotnet publish
```

After you publish you will find you all the binaries needed to run your application under `bin\Debug\netcoreapp2.0\win7-x64\publish\`.
To run the application simply run the EXE that is in this publish directory (it is the name of the app, or specified in the project file).

```
.\bin\Debug\netcoreapp2.0\win7-x64\publish\HelloWorld.exe
```

Thus at this point publication directory directory has NO dependency outside that directory (including dotnet.exe). You can copy this publication
directory to another machine and run the exe in it and it will 'just work' (assuming you are on the same OS). Note that your managed app's
code is still in the 'app'.dll file, the 'app'.exe file is actually simply a rename of dotnet.exe.

**NOTE**: Normally you would be able to run the application by calling `dotnet run` however there is currently tooling issues which lead to an error similar
to `A fatal error was encountered. The library 'hostpolicy.dll' required to execute the application was not found in ...` so to workaround that for
now you have to manually run the application from the publish directory.


## Update CoreCLR from raw binary output

Updating CoreCLR from raw binary output is easier for quick one-off testing which is what this set of instructions
outline but for consuming in a real .NET Core application you should use the nuget package instructions below.

The 'dotnet publish' step above creates a directory that has all the files necessary to run your app
including the CoreCLR and the parts of CoreFX that were needed. You can use this fact to skip some steps if
you wish to update the DLLs. For example typically when you update CoreCLR you end up updating one of two DLLs

* coreclr.dll - Most modifications (with the exception of the JIT compiler and tools) that are C++ code update
  this DLL.
* System.Private.CoreLib.dll - If you modified C# it will end up here.
* System.Private.CoreLib.ni.dll - the native image (code) for System.Private.Corelib.   If you modify C# code
you will want to update both of these together in the target installation.

Thus after making a change and building, you can simply copy the updated binary from the `bin\Product\<OS>.<arch>.<flavor>`
directory to your publication directory (e.g. `helloWorld\bin\Debug\netcoreapp2.0\win7-x64\publish`) to quickly
deploy your new bits. In a lot of cases it is easiest to just copy everything from here to your publication directory.

You can build just the .NET Library part of the build by doing (debug, for release add 'release' qualifier)
(on Linux / OSX us ./build.sh)
```bat
    .\build skiptests skipnative
```
Which builds System.Private.CoreLib.dll AND System.Private.CoreLib.ni.dll (you will always want both) if you modify
C# code. If you wish to only compile the coreclr.dll you can do
 ```bat
    .\build skiptests skipmscorlib
```
Note that this technique does not work on .NET Apps that have not been published (that is you have not created
a directory with all DLLs needed to run the all)  That is because the runtime is either fetched from the system-wide
location that dotnet.exe installed, OR it is fetched from the local nuget package cache (which is where your
build was put when you did a 'dotnet restore' and had a dependency on your particular runtime).    In theory you
could update these locations in place, but that is not recommended since they are shared more widely.

## Update CoreCLR using runtime nuget package

Updating CoreCLR from raw binary output is easier for quick one-off testing but using the nuget package is better
for referencing your CoreCLR build in your actual application because of it does not require manual copying of files
around each time the application is built and plugs into the rest of the tool chain. This set of instructions will cover
the further steps needed to consume the runtime nuget package.

#### 1 - Get the Version number of the CoreCLR package you built.

This makes a 'standard' hello world application but uses the .NET Core Runtime version that
came with the dotnet.exe tool. First you need to modify your app to ask for the .NET Core
you have built, and to do that, we need to know the version number of what you built.  Get
this by simply listing the name of the Microsoft.NETCore.Runtime.CoreCLR you built.

```bat
    dir bin\Product\Windows_NT.x64.Release\.nuget\pkg
```

and you will get name of the which looks something like this

```
    Microsoft.NETCore.Runtime.CoreCLR.2.0.0-beta-25023-0.nupkg
```

This gets us the version number, in the above case it is 2.0.0-beta-25023-0. We will
use this in the next step.

#### 2 - Add a reference to your runtime package

Add the following lines to your project file:

```
  <ItemGroup>
    <PackageReference Include="Microsoft.NETCore.Runtime.CoreCLR" Version="2.0.0-beta-25023-0" />
  </ItemGroup>
```

In your project you should also see a `RuntimeFrameworkVersion` property which represents the
version of Micorosoft.NETCore.App which is used for all the other dependencies. It is possible
that libraries between your runtime and that package are far enough apart to cause issues, so
it is best to have the latest version of Microsoft.NETCore.App package if you are working on the
latest version of the source in coreclr master branch. You can find the latest package by looking
at https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App.

#### 3 - Place your build directory and beta .NET Core Framework feed on your Nuget source list

By default the dogfooding dotnet SDK will create a Nuget.Config file next to your project, if it doesn't
you can create one. Your config file will need a source for your local coreclr package directory as well
as a reference to our nightly dotnet-core feed on myget:

```xml
<configuration>
  <packageSources>
    <add key="local coreclr" value="D:\git\coreclr\bin\Product\Windows_NT.x64.Debug\.nuget\pkg" />
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
  </packageSources>
</configuration>

```
Obviously **you need to update path in the XML to be the path to output directory for your build**.

On Windows you also have the alternative of modifying the Nuget.Config
at `%HOMEPATH%\AppData\Roaming\Nuget\Nuget.Config` (`~/.nuget/NuGet/NuGet.Config` on Linux) with the new location.
This will allow your new runtime to be used on any 'dotnet restore' run by the current user.
Alternatively you can skip creating this file and pass the path to your package directory using
the -s SOURCE qualifer on the dotnet restore command below.   The important part is that somehow
you have told the tools where to find your new package.

Once have made these modifications you will need to rerun the restore and publish as such.

```
dotnet restore
dotnet publish
```
Now your publication directory should contain your local built CoreCLR builds.

#### 4 - Update BuildNumberMinor Environment Variable

One possible problem with the technique above is that Nuget assumes that distinct builds have distinct version numbers.
Thus if you modify the source and create a new NuGet package you must it a new version number and use that in your
application's project. Otherwise the dotnet.exe tool will assume that the existing version is fine and you
won't get the updated bits. This is what the Minor Build number is all about. By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable.
```bat
    set BuildNumberMinor=3
```
before packaging. You should see this number show up in the version number (e.g. 2.0.0-beta-25023-03).

As an alternative you can delete the existing copy of the package from the Nuget cache.   For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\Microsoft.NETCore.Runtime.CoreCLR\2.0.0-beta-25023-02
```
which should make things work (but is fragile, confirm file timestamps that you are getting the version you expect)

## (Optional) Confirm that the app used your new runtime

Congratulations, you have successfully used your newly built runtime. To confirm that everything worked, you
should compare the file creation timestamps for the CoreCLR.dll and System.Private.Runtime.dll in the publishing
directory and the build output directory. They should be identical. If not, something went wrong and the
dotnet tool picked up a different version of your runtime.

As a hint you could adde some code like:
```
    var coreAssemblyInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
    Console.WriteLine($"Hello World from Core {coreAssemblyInfo.ProductVersion}");
```
That should tell you the version and which user and machine build the assembly as well as the commit hash of the code
at the time of building.


--------------------------
## Using CoreRun to run your .NET Core Application

Generally using dotnet.exe tool to run your .NET Core application is the preferred mechanism to run .NET Core Apps.
However there is a simpler 'host' for .NET Core applications called 'CoreRun' that can also be used.   The value
of this host is that it is simpler (in particular it knows nothing about NuGet), but precisely because of this
it can be harder to use (since you are responsible for insuring all the dependencies you need are gather together)
See [Using CoreRun To Run .NET Core Application](UsingCoreRun.md) for more.
