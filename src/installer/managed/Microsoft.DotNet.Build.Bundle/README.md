.NET Core Bundler
===================================

The Bundler is a tool that embeds an application and its dependencies into the AppHost executable. This tool is used to publish apps as a single-file, as described in this [design document](https://github.com/dotnet/designs/blob/master/accepted/single-file/design.md).

### Why is the Bundler in core-setup repo?

The bundler is an independent tool for merging several files into one. 
The bundler code lives in the core-setup repo because:
* It is closely related to the AppHost code, which facilitates easy development, update, and testing.
* The `dotnet/cli` and `dotnet/sdk` repos were considered unsuitable because of repo ownership and maintainence concerns.
* It is not worth creating an managing an independent repo just for the tool. 

### Why is the Bundler a tool, not a managed library?

Users typically only interact with the bundler via dotnet CLI (`dotnet publish /p:PublishSingleFile=true`). The connection between the bundler tool and msbuild is facilitated by MsBuild artifacts in the SDK.  The Bundler itself is an executable tool with a command-line interface because: 

1. A library hosted in-proc by the MSBuild process needs to carefully address concerns such as target framework / multitargeting requirements, dependency collision with other tasks / libraries, etc. 
2. It forces a crisp contract of inputs and outputs of the MSBuild task.
3. It can be run without spinning up a full build in "ad-hoc" scenarios.
4. It facilitates easy testing.

The [IL Linker](https://github.com/mono/linker) and [Crossgen compiler](https://github.com/dotnet/coreclr/tree/master/src/tools/crossgen) are similarly implemented as an independent command line tools.