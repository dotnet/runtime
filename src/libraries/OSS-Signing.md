For reasons listed over on [Strong Naming](https://github.com/dotnet/corefx/wiki/Strong-Naming), all .NET Core assemblies are strong-named.

To enable you to build assemblies that have a matching identity to what Microsoft would build, we leverage a new signing mechanism called _OSS Signing_. This lets you clone the dotnet/corefx repository, build and then drop the resulting assembly in your application with zero changes to consuming libraries. By default, all .NET Core projects build using OSS Signing.

OSS Signing is very similar to [delay signing](http://msdn.microsoft.com/en-us/library/t07a3dye(v=vs.110).aspx) but without the need to add skip verification entries to your machine. This allows you to load the assembly in most contexts, or more precisely in any context that doesn't require validating the strong-name signature. You cannot install OSS Signed assemblies into the GAC or use them in partial trust.

When running on the full .NET Framework we only support using OSS Signed assemblies for debugging and testing purposes. Microsoft does not guarantee that you can successfully load OSS Signed assemblies in all scenarios that are required for production use.

However, in the context of ASP.NET 5 on .NET Core, or .NET Native, Microsoft supports using OSS Signed assemblies for production uses. Make note, however, that while ability to load OSS Signed binaries is supported on these platforms, the API and contents of the assembly itself is unsupported (due to it being privately built).