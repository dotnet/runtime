# Using your .NET Runtime Build with the Installed SDK

* [Introduction](#introduction)
  * [Acquire the latest development .NET SDK](#acquire-the-latest-development-net-sdk)
* [Create a sample self-contained application](#create-a-sample-self-contained-application)
  * [Publish your App](#publish-your-app)
* [Update CoreCLR and System.Private.CoreLib.dll with your build](#update-coreclr-and-systemprivatecorelibdll-with-your-build)
* [Confirm that the app used your new runtime (Optional)](#confirm-that-the-app-used-your-new-runtime-optional)
* [Troubleshooting](#troubleshooting)
  * [If it's not using your copied binaries](#if-its-not-using-your-copied-binaries)
  * [If you get a consistency check assertion failure](#if-you-get-a-consistency-check-assertion-failure)
  * [If you get a JIT load error](#if-you-get-a-jit-load-error)

This guide will walk you through using your own build from the runtime repo for testing, running apps, and so on. This doc assumes you've already built at least the _clr_ subset of the repo, and have the binaries under `artifacts/bin/coreclr/<OS>.<arch>.<configuration>`. If this is not your case, the [CoreCLR building docs](/docs/workflow/building/coreclr/README.md) have detailed instructions on how to get these artifacts.

## Introduction

To run a .NET app with the runtime you've built, you will need a _host_ program that will load the runtime, as well as all the other .NET libraries that your application might need. There are three main ways to go about this:

* Use your machine's installed .NET SDK and replace the necessary binaries in a self-contained app.
* Use your build's _Dev Shipping Packages_ to run your app.
* Use the _CoreRun_ host generated as part of your build's artifacts.

This guide focuses on the first of the bullet points described above. For the other two, we have docs dedicated to them:

* [Using your build's dev shipping packages](using-dev-shipping-packages.md)
* [Using CoreRun and CoreRoot](using-corerun-and-coreroot.md)

**NOTE**: It's unlikely, but it's possible that the officially released version of `dotnet` may not be compatible with the live repository. If this happens to you, then unfortunately, you will be limited to either installing a nightly build on your machine (not that recommended), or use another of the methods described above to test your build. This is because this method requires a _self-contained_ app, and the portable builds do not support this at the present time.

### Acquire the latest development .NET SDK

The [sdk repo](https://github.com/dotnet/sdk#installing-the-sdk) has downloads to all nightly builds for all the currently supported platforms. Find the one that matches your machine and download it.

To setup the nightly SDK, you can either install it to your machine or use a portable build. If you downloaded the _installer_, then just follow the usual installation instructions, and you're done.

To use a portable build (check the note above though), first extract somewhere the _zip/tar.gz_ you downloaded at the beginning of this section. Then, you can either add the path where you extracted it to your `PATH` environment variable, or always fully qualify the path to the `dotnet` you extracted (e.g. `/path/to/nightly/build/dotnet`).

After setting up dotnet you can verify you are using the newer version by issuing the `dotnet --version` command on it. At the time of writing, the version must be equal or greater than `9.0.100-*`.

<!-- TODO: It feels like this link may or may not be more appropriate elsewhere. Need to dig deeper into the documentation, so leaving it here for the time being. -->
For another small walkthrough see [Dogfooding .NET SDK](/docs/project/dogfooding.md).

## Create a sample self-contained application

First things first. We need a sample app to test our runtime build on. Let's create a quick 'Hello World' app for this example.

```cmd
mkdir HelloWorld
cd HelloWorld
dotnet new console
```

In order to run with your local changes, the application needs to be self-contained, as opposed to running on the installed shared framework. In order to do that, you will need a `RuntimeIdentifier` for your project. You can specify it directly in the command-line later on, or you can write it in your app's `.csproj` file:

```xml
<PropertyGroup>
  ...
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

We are using Windows x64 for this example. Make sure you set it to the platform and configuration you have your build in. The codenames for the most common OS's are:

* _Windows_: `win`
* _macOS_: `osx`
* _Linux_: `linux`

For example, if we were testing a macOS ARM64 build, our `RuntimeIdentifier` would be `osx-arm64`.

### Publish your App

Now is the time to build and publish. This step will trigger _restore_ and _build_.

```cmd
dotnet publish --self-contained
```

**NOTE:** If publish fails to restore runtime packages, then you'll need to configure custom NuGet feeds. This is a side-effect of using a dogfood .NET SDK: Its dependencies are not yet on the regular NuGet feed. To configure this, you have to:

1. Run the command `dotnet new nugetconfig`
2. Go to the newly created `NuGet.Config` file and replace the content with the following template:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
    <add key="dotnet9" value="https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet9/nuget/v3/index.json" />
 </packageSources>
</configuration>
```

After you publish successfully, you will find all the binaries needed to run your application under `bin\Debug\net9.0\win-x64\publish`.

**But we are not done yet, you need to replace the published runtime files with the files from your local build!**

## Update CoreCLR and System.Private.CoreLib.dll with your build

The publishing step described above creates a directory that has all the files necessary to run your app, including the CoreCLR runtime and the required libraries. Out of all these binaries, there are three notable ones that will contain any changes you make to the runtime:

* `coreclr.dll (windows)/libcoreclr.dylib (macos)/libcoreclr.so (linux)`: Most modifications (with the exception of the JIT compiler and tools) that are C++ code update this binary.
* `System.Private.CoreLib.dll`: If you modified managed C# code, it will end up here.
* `clrjit.dll`: The JIT compiler. It is also required you copy this one to your published app.

Now, here comes the main deal to test your build. Once you have your self-contained app published, and CoreCLR built, you will replace the binaries listed above with the generated artifacts. Copy them from `artifacts/bin/coreclr/<OS>.<arch>.<configuration>/` to your app's publication directory, which by default is `your-app-folder/bin/<configuration>/net9.0/<os-code>-<arch>/publish`.

In our previous example this would be:

* From: `artifacts/bin/coreclr/windows.x64.Debug/`
* To: `HelloWorld/bin/Debug/net9.0/win-x64/publish/`

## Confirm that the app used your new runtime (Optional)

Congratulations, you have successfully used your newly built runtime.

If you want to further ensure this is indeed the case before delving into more complex experiments and testing, you can run the following piece of code in your app:

```csharp
using System.Diagnostics;

var coreAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
Console.WriteLine($"Core Runtime Info: {coreAssemblyInfo.ProductVersion}");
Console.WriteLine($"System.Private.CoreLib.dll is located at: {typeof(object).Assembly.Location}");
```

That should tell you the version, and which user and machine built the assembly, as well as the _commit hash_ of the code at the time of building:

```text
Core Runtime Info: 9.0.0-dev
System.Private.CoreLib.dll is located at: /path/to/your/app/bin/Debug/net9.0/win-x64/publish/System.Private.CoreLib.dll
```

What you are looking for here is that the core runtime used is labelled as `-dev`. This means it is indeed using the one you built in the runtime repo. Also, ensure that the picked _System.Private.CoreLib.dll_ is indeed the one in your `publish` folder.

## Troubleshooting

Here are a few very common errors you might encounter, and how to fix them.

### If it's not using your copied binaries

Make sure you are running the executable directly.

```cmd
.\bin\Debug\net9.0\win-x64\publish\HelloWorld.exe
```

If you use `dotnet run` it will overwrite your custom binaries before executing the app.

### If you get a consistency check assertion failure

This failure happens when you only copy `coreclr`, but not `System.Private.Corelib.dll` as well.

```text
Assert failure(PID 13452 [0x0000348c], Thread: 10784 [0x2a20]): Consistency check failed: AV in clr at this callstack:
```

### If you get a JIT load error

If you forget to also copy `clrjit.dll`, you will get the following error message:

```text
Fatal error. Failed to load JIT compiler.
```
