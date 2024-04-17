Public Signing
===================

For reasons listed over on [Strong Naming](strong-name-signing.md), all .NET Core assemblies are strong-named.

To enable you to build assemblies that have a matching identity to what Microsoft would build, we leverage a new signing mechanism called _Public Signing_. This lets you clone the dotnet/runtime repository, build and then drop the resulting assembly in your application with zero changes to consuming libraries. By default, all .NET Core projects build using public signing.

Public signing is very similar to [delay signing](http://msdn.microsoft.com/en-us/library/t07a3dye(v=vs.110).aspx) but without the need to add skip verification entries to your machine. This allows you to load the assembly in most contexts, or more precisely in any context that doesn't require validating the strong-name signature.

When running on the full .NET Framework we only support using public signed assemblies for debugging and testing purposes. Microsoft does not guarantee that you can successfully load public signed assemblies in all scenarios that are required for production use. For list of known scenarios where public signing does not work when running on .NET Framework, see below.

However, in the context of .NET Core, Microsoft supports using public signed assemblies for production uses. Make note, however, that while ability to load public signed binaries is supported on these platforms, the API and contents of the assembly itself is unsupported (due to it being privately built).

Known issues when debugging and testing public signed assemblies on .NET Framework:

- You will not be able to install the assembly to the [Global Assembly Cache (GAC)](https://msdn.microsoft.com/en-us/library/yf1d93sz.aspx)
- You will not be able to load the assembly in an AppDomain where shadow copying is turned on.
- You will not be able to load the assembly in a partially trusted AppDomain
- You will not be able to pre-compile ASP.NET applications
- You may not be able to load the assembly if the `app.config` file specifies `<configuration>`/`<runtime>`/`<NetFx40_LegacySecurityPolicy enabled="true" />`

The `corflags.exe` tool that ships with the .NET Framework SDK can show whether a binary is delay-signed or strong-named. For a delay-signed assembly it may show:

```
CorFlags  : 0x20003
```

For a strong-named assembly it can show:

```
CorFlags  : 0x2000b
```

The bit that is flipped is 0x8. If the bit is set, the assembly is strong-named. Additionally, the `sn.exe -vf` tool can show the same information. It will output `<assembly> is a delay-signed or test-signed assembly` or `Failed to verify assembly -- Strong name validation failed.` for a public-signed binary.

# MSBuild and Compiler Tooling Support

Starting with Visual Studio 2015 Update 2, the C# and VB compilers support the new `/publicsign` command-line argument. You can also pass it to the compiler from your MSBuild project by setting the `<PublicSign>True</PublicSign>` MSBuild property to true. Note that you have to set `<DelaySign>False</DelaySign>` otherwise you will get an error that DelaySign and PublicSign can't be both specified at the same time.

# Legacy FakeSign tool

If you're not able to use Visual Studio 2015 Update 2 or later, the [FakeSign package on NuGet](https://www.nuget.org/packages/fakesign) contains the `FakeSign.exe` tool that can flip the bit on or off.
