.NET Core Runtime & host setup repo
===================================

This repo contains the code to build the .NET Core runtime, libraries and shared host (`dotnet`) installers for 
all supported platforms. It **does not** contain the actual sources to .NET Core runtime; this source is split across 
the dotnet/coreclr repo (runtime) and dotnet/corefx repo (libraries). 

## Installation experience
The all-up installation experience is described in the [installation scenarios](https://github.com/dotnet/cli/blob/rel/1.0.0/Documentation/cli-installation-scenarios.md) 
document in the dotnet/cli repo. That is the first step to get acquantied with the overall plan and experience we have
thought up for installing .NET Core bits. 

## Filing issues
This repo should contain issues that are tied to the installation of the "muxer" (the `dotnet` binary) and installation 
of the .NET Core runtime and libraries. 

For other issues, please use the following repos:

- For overall .NET Core SDK issues, file on [dotnet/cli](https://github.com/dotnet/cli) repo
- For class library and framework functioning issues, file on [dotnet/corefx](https://github.com/dotnet/corefx) repo
- For runtime issues, file on [dotnet/coreclr](https://github.com/dotnet/coreclr) issues

