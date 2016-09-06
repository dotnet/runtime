Public Signing
===================

For reasons listed over on [Strong Naming](strong-name-signing.md), all .NET Core assemblies are strong-named.

To enable you to build assemblies that have a matching identity to what Microsoft would build, we leverage a new signing mechanism called _Public Signing_. This lets you clone the dotnet/corefx repository, build and then drop the resulting assembly in your application with zero changes to consuming libraries. By default, all .NET Core projects build using public signing.

Public signing is very similar to [delay signing](http://msdn.microsoft.com/en-us/library/t07a3dye(v=vs.110).aspx) but without the need to add skip verification entries to your machine. This allows you to load the assembly in most contexts, or more precisely in any context that doesn't require validating the strong-name signature.

When running on the full .NET Framework we only support using public signed assemblies for debugging and testing purposes. Microsoft does not guarantee that you can successfully load public signed assemblies in all scenarios that are required for production use. For list of known scenarios where public signing does not work when running on .NET Framework, see below.

However, in the context of ASP.NET 5 on .NET Core, or .NET Native, Microsoft supports using public signed assemblies for production uses. Make note, however, that while ability to load public signed binaries is supported on these platforms, the API and contents of the assembly itself is unsupported (due to it being privately built).

Known issues when debugging and testing public signed assemblies on .NET Framework:

- You will not be able to install the assembly to the [Global Assembly Cache (GAC)](https://msdn.microsoft.com/en-us/library/yf1d93sz.aspx)
- You will not be able to load the assembly in an AppDomain where shadow copying is turned on.
- You will not be able to load the assembly in a partially trusted AppDomain
