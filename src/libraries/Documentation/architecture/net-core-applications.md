# .NET Core Applications

NETCoreApp is the [target framework](https://docs.nuget.org/Create/TargetFrameworks) that represents .NET Core applications

Property | Value
---------|---------
Target framework identifier | `.NETCoreApp`
Target framework version | `1.0`
Target framework moniker | `.NETCoreApp,Version=v1.0`
Friendly name | .NET Core Application
NuGet folder name | `netcoreapp1.0`
NETStandard version supported | `netstandard1.6`

## FAQ
**Q: What is a .NET Core application?**  
**A:** A .NET Core application is an application that can run on any .NET Core runtime: CoreCLR (current), .NETNative (future). It can run on one of many .NET core platforms (Windows, OSX, Linux).  It relies on the host provided by the given runtime.  It's a composable framework built from the packages on which the application depends.  Its assembly loading policy permits newer versions of dependencies without any application configuration (eg: BindingRedirects are not required).

**Q: Can I share source between a .NET Core application, and other target frameworks?**  
**A:** Yes.  Most of the API supported by .NET Core application is also part of .NET Standard.  That source could be compiled as a .NET Standard library and shared with a .NET Core application and a .NET Framework application as a binary.  Alternatively, the source could be shared and cross-compiled between a .NET Core application and a .NET Framework application.

**Q: Can a .NET Core application depend on more packages than just those in the `NETStandard.Library` package?**  
**A:** Yes.  The contents of `NETStandard.Library` represent the standard library and are guaranteed to be supported by all platforms, including .NET Core applications, that support standard library.  Packages outside of this set will be supported so long as their dependencies are provided and none are platform specific.

**Q: Can a .NET Core application depend on more packages than just those in the `Microsoft.NETCore.App` package?**  
**A:** Yes.  The contents of `Microsoft.NETCore.App` at a particular version are guaranteed to run on every platform where that version .NET Core is released.  Packages outside this set can be used but don't come with that guarantee.  For instance, if a package is not part of `Microsoft.NETCore.App` and needs to be cross-compiled specifically for a new OS, there is no guarantee that it will be re-released when .NET Core supports that new OS.

**Q: Why is there only one version of `.NETCoreApp` (1.0), but there are many of `.NETStandard`?**  
**A:** `.NETStandard` is an abstract representation of API that covers all historical platforms that have ever supported that API.  `.NETCoreApp` represents a concrete application type with a runnable implementation.  We are shipping one version of this implementation at this point and it supports `netstandard1.6`.  As we version `.NETStandard` in the future we will update the implementation of `.NETCoreApp` to support the new API and ship a new version of `.NETCoreApp`.

**Q: Can a .NET Core application depend on platform specific packages like `Microsoft.Win32.Registry`?**  
**A:** Yes, but it will only run on the platforms that support those packages.

**Q: How is this different than `.NETCore`?**  
**A:** The `.NETCore` target framework represents Windows 8, Windows 8.1, and Universal Windows Platform applications.  For compatibility purposes this moniker cannot be reused for “.NET Core applications”.  The branding overlap is unfortunate.

**Q: How is this different than `DNXCore`?**  
**A:** The `DNXCore` target framework represents ASP.NET V5 applications that run in DNX and use XProj.  As such the TFM already had more characteristics associated with it than those we associate with .NET Core application.  .NET Core applications need not be ASP.NET applications and can run on any host and runtime supported by .NET Core.

**Q: How is this different than `.NETStandard`?**  
**A:** The `NETStandard` target framework is an abstract target framework that represents API surface of many frameworks and platforms.  As such `NETStandard` assemblies can run on any platform that supports the `NETStandard` targeted by that assembly, for example: .NET Desktop, Windows Phone, Universal Windows Platform applications, .NET Core applications, etc.  `NETCoreApplication` is a concrete target framework that represents a single platform with both API surface and implementation.  .NET Core applications are runnable on their own.  .NETStandard libraries must be published-for or consumed-by a specific concrete target framework to be used in that type of application.

**Q: How is this different than `.NETFramework`, AKA: Full .NET, Desktop .NET, Big .NET, old .NET?**  
**A:** `.NETFramework` is another concrete framework like `.NETCoreApp` but has a different implementation and runtime and supports a different set of API.  Both `.NETFramework` and `.NETCoreApp` support a common version of `.NETStandard` and the API it exposes.  As such a `.NETStandard` assembly can be used by both `.NETFramework` and `.NETCoreApp`, but a `.NETCoreApp` assembly is not meant to run on `.NETFramework`, nor is a `.NETFramework` assembly meant to run on `.NETCoreApp`.
