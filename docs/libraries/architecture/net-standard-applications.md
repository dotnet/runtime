#.NET Standard Applications

NETStandardApp is the [target framework](https://docs.nuget.org/Create/TargetFrameworks) that represents .NET Core applications

Property | Value
---------|---------
Target framework identifier | `.NETStandardApp`
Target framework version | `1.5`
Target framework moniker | `.NETStandardApp,Version=v1.5`
Friendly name | .NET Standard Application
NuGet folder name | `netstandardapp1.5`
NETStandard version supported | `netstandard1.5`

##FAQ
**Q: What is a .NET Standard application?**  
**A:** A .NET Standard application is an application that can run on any .NET Core runtime: CoreCLR (current), .NETNative (future). It can run on one of many .NET core platforms (Windows, OSX, Linux).  It relies on the host provided by the given runtime.  It's a composable framework built from the packages on which the application depends.  Its assembly loading policy permits newer versions of dependencies without any application configuration (eg: BindingRedirects are not required).

**Q: Can I share source between a .NET Standard application, and other target frameworks?**  
**A:** Yes.  Most of the API supported by .NET Standard application is also part of .NET Standard.  That source could be compiled as a .NET Standard library and shared with a .NET Standard application and a .NET Framework application as a binary.  Alternatively, the source could be shared and cross-compiled between a .NET Standard application and a .NET Framework application.

**Q: Can a .NET Standard application depend on more packages than just those in the `NETStandard.Library` package?**  
**A:** Yes

**Q: Why is there only one version of `.NETStandardApp` (1.5), but there are many of `.NETStandard`?**  
**A:** `.NETStandard` is an abstract representation of API that covers all historical platforms that have ever supported that API.  `.NETStandardApp` represents a concrete application type with a runnable implementation.  We are shipping one version of this implementation at this point and it supports `netstandard1.5`.  As we version `.NETStandard` in the future we will update the implementation of `.NETStandardApp` to support the new API and ship a new version of `.NETStandardApp`.

**Q: Can a .NET Standard application depend on platform specific packages like `Microsoft.Win32.Registry`?**  
**A:** Yes, but it will only run on the platforms that support those packages.

**Q: How is this different than `.NETCore`?**  
**A:** The `.NETCore` target framework represents Windows 8, Windows 8.1, and Universal Windows Platform applications.  For compatibility purposes this moniker cannot be reused for “.NET Core applications”.  The branding overlap is unfortunate.

**Q: How is this different than `DNXCore`?**  
**A:** The `DNXCore` target framework represents ASP.NET V5 applications that run in DNX and use XProj.  As such the TFM already had more characteristics associated with it than those we associate with .NET standard application.  .NET standard applications need not be ASP.NET applications and can run on any host and runtime supported by .NET Core.

**Q: How is this different than `.NETStandard`?**  
**A:** The `NETStandard` target framework is an abstract target framework that represents API surface of many frameworks and platforms.  As such `NETStandard` assemblies can run on any platform that supports the `NETStandard` targeted by that assembly, for example: .NET Desktop, Windows Phone, Universal Windows Platform applications, .NET Standard applications, etc.  `NETStandardApplication` is a concrete target framework that represents a single platform with both API surface and implementation.  .NET standard applications are runnable on their own.  .NETStandard libraries must be published-for or consumed-by a specific concrete target framework to be used in that type of application.

**Q: How is this different than `.NETFramework`, AKA: Full .NET, Desktop .NET, Big .NET, old .NET?**  
**A:** `.NETFramework` is another concrete framework like `.NETStandardApp` but has a different implementation and runtime and supports a different set of API.  Both `.NETFramework` and `.NETStandardApp` support a common version of `.NETStandard` and the API it exposes.  As such a `.NETStandard` assembly can be used by both `.NETFramework` and `.NETStandardApp`, but a `.NETStandardApp` assembly is not meant to run on `.NETFramework`, nor is a `.NETFramework` assembly meant to run on `.NETStandardApp`.
