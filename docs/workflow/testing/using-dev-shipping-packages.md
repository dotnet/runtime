# Using your build's Shipping Packages with the .NET SDK

* [Requirements](#requirements)
  * [Build the Shipping Packages](#build-the-shipping-packages)
  * [Acquire the latest development .NET SDK](#acquire-the-latest-development-net-sdk)
* [Creating and running the app with your build](#creating-and-running-the-app-with-your-build)
  * [Create and Configure the App](#create-and-configure-the-app)
  * [Write a small test](#write-a-small-test)
  * [Publish and Run the App](#publish-and-run-the-app)
* [Making Changes and Consuming Updated Packages](#making-changes-and-consuming-updated-packages)

This guide will walk you through using the shipping packages of your own build from the runtime repo for testing, running apps, and so on. This is the lengthiest but most end-user-like of all the CoreCLR testing methods.

This guided focuses on this scenario, but we also have detailed docs on the other ways of testing:

* [Using your build with your machine's installed SDK](using-your-build-with-installed-sdk.md)
* [Using CoreRun and CoreRoot](using-corerun-and-coreroot.md)

## Requirements

The following subsections will describe the requirements you will need to have ready in advance, and how to prepare them.

### Build the Shipping Packages

The shipping packages are comprised of four subsets of the runtime repo: Clr, Libraries, Packs, and the Host. So, to build all of those, issue the following command from the root of the repo:

For Windows:

```cmd
.\build.cmd -s clr+libs+packs+host
```

For macOS and Linux:

```bash
./build.sh -s clr+libs+packs+host
```

This will place several installers, Nuget packages, compressed archives, and other files within `artifacts/packages/<configuration>/Shipping`. You could actually install your built runtime to your machine using the installers here, but that's not recommended.

### Acquire the latest development .NET SDK

The [sdk repo](https://github.com/dotnet/sdk#installing-the-sdk) has downloads to all nightly builds for all the currently supported platforms. Find the one that matches your machine and download it.

To setup the nightly SDK, you can either install it to your machine or use a portable build. If you downloaded the _installer_, then just follow the usual installation instructions, and you're done.

To use a portable build (recommended way), first extract somewhere the _zip/tar.gz_ you downloaded at the beginning of this section. Then, you can either add the path where you extracted it to your `PATH` environment variable, or always fully qualify the path to the `dotnet` you extracted (e.g. `/path/to/nightly/build/dotnet`).

After setting up the new dotnet you can verify you are using the newer version by issuing the `dotnet --version` command on it. At the time of writing, the version ought to be equal or greater than `8.0.100-*`.

## Creating and running the app with your build

Now that you have your environment set up, let's get the test app prepared.

### Create and Configure the App

First, create a simple console app like you usually do:

```bash
mkdir HelloWorld && cd HelloWorld
dotnet new console
```

**NOTE**: Make sure you're using the nightly SDK you downloaded in the previous step.

Next, we have to somehow tell the SDK that our built NuGet packages exist, and that we want to use them. For this purpose, we will create a `NuGet.Config` file within our app's folder with the `dotnet new nugetconfig` command.

This config file will require a handful of modifications to work as we need it to. Firstly, create a folder where you'll want Nuget to cache its stuff locally for this experiment. We will call it `LocalNugetCache` in this example. Then, edit your config file with the new stuff:

```xml
<configuration>

  <config>
    <!-- Set the "value" here to the folder you will be using for your local Nuget cache. -->
    <add key="globalPackagesFolder" value="Path/To/LocalNugetCache" />
  </config>

  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />

    <!-- Any packages that might be required, but not present in your build, will have to be taken from the latest NuGet feed. -->
    <!-- More info on: https://github.com/dotnet/sdk#installing-the-sdk -->
    <add key="dotnet8" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json" />

    <!-- Set this path to where your Shipping Artifacts are located. Note that we are assuming a 'Debug' build in this example. -->
    <add key="local runtime" value="Path/To/Runtime/artifacts/packages/Debug/Shipping" />
  </packageSources>

</configuration>
```

Once we have your `NuGet.Config` file ready, we have to make our project aware that we will be using a different runtime. Add the following to the `csproj` file of your app:

```xml
<ItemGroup>
  <!-- At the time of writing, '8.0.0-dev' is the version of the runtime repo's shipping packages. -->
  <FrameworkReference Update="Microsoft.NETCore.App" RuntimeFrameworkVersion="8.0.0-dev" />
</ItemGroup>
```

If you're unsure of what version your packages are, it is included as part of their filenames. For example, pick the `nupkg` file that will be used with your app from your shipping folder (`artifacts/packages/<configuration>/Shipping`). It's name is something like `Microsoft.NETCore.App.Runtime.win-x64.8.0.0-dev.nupkg`, depending on the current version and your target platform.

### Write a small test

For illustration purposes, we will have our app print the version of the runtime in this example:

```csharp
using System.Diagnostics;

var coreAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
Console.WriteLine($"Hello World from .NET {coreAssemblyInfo.ProductVersion}.");
Console.WriteLine($"The location of System.Private.CoreLib.dll is '{typeof(object).Assembly.Location}'");
```

### Publish and Run the App

The following command will build and publish your app:

```cmd
dotnet publish -r win-x64
```

Adjust the `win-x64` to match your machine's OS and architecture.

Running this little app should yield an output like the following:

```text
Hello World from .NET 8.0.0-dev
The location of System.Private.CoreLib.dll is '/path/to/your/app/bin/Debug/net8.0/win-x64/publish/System.Private.CoreLib.dll'
```

## Making Changes and Consuming Updated Packages

You've now successfully tested your runtime build. However, more likely than not, you will be making further changes that you'll want to test. The issue here is you can't simply build the repo again and have it work. This is because of the _NuGet Cache_ mentioned earlier. Since the version number doesn't change locally, NuGet doesn't realize changes have been made and thus uses its cached version. To get around this, we have to get rid of such cache. That's why we set a local one using the `globalPackagesFolder` in the `nuget.config` file we created.

So the steps to apply and test changes are the following:

1. Build the runtime again like you did in the "[Build the Shipping Packages section](#build-the-shipping-packages)". Note that if you only make changes to CoreCLR _(clr)_ or the libraries _(libs)_, you can omit the other in the build command.
2. Delete the local NuGet cache.
3. Publish and run your app again.

Now your app will be using the updated package.
