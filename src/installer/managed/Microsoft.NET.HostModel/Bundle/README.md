.NET Core Bundler
===================================

The Bundler is a tool that embeds an application and its dependencies into the AppHost executable. This tool is used to publish apps as a single-file, as described in this [design document](https://github.com/dotnet/designs/blob/master/accepted/single-file/design.md).

### Why is the Bundler in core-setup repo?

The bundler is an independent tool for merging several files into one. 
The bundler code lives in the core-setup repo because:
* It is closely related to the AppHost code, which facilitates easy development, update, and testing.
* The `dotnet/cli` and `dotnet/sdk` repos were considered unsuitable because of repo ownership and maintainence concerns.
* It is not worth creating an managing an independent repo just for the tool. 
