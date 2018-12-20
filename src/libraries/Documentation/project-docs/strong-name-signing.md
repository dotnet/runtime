Strong Name Signing
===================

All .NET Core assemblies are [strong-named](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/strong-named-assemblies). We do this for two reasons:

1. _Compatibility_. We want to maintain type identity with previous versions of our assemblies that have shipped across various versions of our platforms. Removing a strong-name from an assembly is a breaking change, and would break the ability to consume and run libraries built against the previous identities.

2. _Serviceability_. When running on .NET Framework some of .NET Core assemblies ship locally ("app-local") with the application, this is in contrast to other framework assemblies that are placed in the [GAC](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/gac). To be able to service these libraries for critical security updates, we make use of the [app-local servicing](https://blogs.msdn.microsoft.com/dotnet/2014/01/22/net-4-5-1-supports-microsoft-security-updates-for-net-nuget-libraries/) feature which requires that assemblies have strong-names.

##  FAQ

### 1. Microsoft strong-names their assemblies, should I?
For the most part, the majority of applications do not need strong-names. Strong-names are left over from previous eras of .NET where [sandboxing](https://en.wikipedia.org/wiki/Sandbox_(computer_security)) needed to differentiate between code that was trusted, versus code that was untrusted. However in recent years, sandboxing via AppDomains, especially to [isolate ASP.NET web applications](https://support.microsoft.com/en-us/help/2698981/asp-net-partial-trust-does-not-guarantee-application-isolation), is no longer guaranteed and is not recommended. 

However, strong-names are still required in applications in some rare situations, most of which are called out on this page: [Strong-Named Assemblies](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/strong-named-assemblies).

Because of the viral nature of strong-naming, it is recommended that publicly published .NET libraries are strong-named. Not strong-naming a .NET library excludes anyone who does need to strong-name their application or library from using it. Read more about .NET libraries and strong-naming in the [.NET Library Guidance](https://docs.microsoft.com/dotnet/standard/library-guidance/strong-naming).

### 2. I really, _really_ need to strong-name, what kinds of issues will I run into?
There are three major problems that developers run into after strong naming their assemblies:

1. _Binding Policy_. When developers talk about strong-names, they are usually conflating it with the strict binding policy of the .NET Framework that kicks in _when_ you strong-name. This binding policy is problematic because it forces, by default, an exact match between reference and version, and requires developers to author complex [binding redirects](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/bindingredirect-element) when they don't. In recent versions of Visual Studio, however, we've added [Automatic Binding Redirection](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/how-to-enable-and-disable-automatic-binding-redirection) as an attempt to reduce pain of this policy on developers. On top of this, all newer platforms, including _Silverlight_, _WinRT-based platforms_ (Phone and Store), _.NET Native_ and _ASP.NET 5_ this policy has been loosened, allowing later versions of an assembly to satisfy earlier references, thereby completely removing the need to ever write binding redirects on those platforms.

2. _Virality_. Once you've strong-named an assembly, you can only statically reference other strong-named assemblies. 

3. _No drop-in replacement_. This is a problem for open source libraries where the strong-name private key is not checked into the repository. This means that developers are unable to build to their own version of the library and then use it as a drop-in replacement without recompiling _all_ consuming libraries up stack to pick up the new identity. This is extremely problematic for libraries, such as Json.NET, which have large incoming dependencies. Firstly, we would recommend that these open source projects check-in their private key (remember, [strong-names are used for identity, and not for security](https://docs.microsoft.com/en-us/dotnet/framework/app-domains/strong-named-assemblies)). Failing that, however, we've introduced a new concept called [Public Signing](public-signing.md) that enables developers to build drop-in replacements without needing access to the strong-name private key. This is the mechanism that .NET Core libraries use by default.
