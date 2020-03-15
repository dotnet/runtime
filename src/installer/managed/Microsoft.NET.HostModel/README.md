Host Model
===================================

HostModel is a library used by the [SDK](https://github.com/dotnet/sdk) to perform certain transformations on host executables. The main services implemented in HostModel are:

* AppHost rewriter:  Embeds the App Name into the AppHost executable. On Windows, also copies resources from App.dll to the AppHost.
* ComHost rewriter: Creates a ComHost with an embedded CLSIDMap file to map CLSIDs to .NET Classes.

* Single-file bundler: Embeds an application and its dependencies into the AppHost, to publish a single executable, as described [here](https://github.com/dotnet/designs/blob/master/accepted/2020/single-file/design.md).

The HostModel library is in the Runtime repo because:

* The implementations of the host and HostModel are closely related, which facilitates easy development, update, and testing.
* Separating the HostModel implementation from SDK repo repo aligns with code ownership, and facilitates maintenance. 

The build targets/tasks that use the HostModel library are in the SDK repo because:

* This facilitates the MSBuild tasks to be multi-targeted. 
* It helps generate localized error messages, since SDK repo has the localization infrastructure. 
