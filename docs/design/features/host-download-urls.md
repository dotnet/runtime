# URLs host uses to point to download of .NET Core

When an application fails to start because the right .NET Core components are not available, the host includes a URL in the error message which should provide good UX to acquire the necessary installers to make the app work.

The host uses different URLs for different scenarios. These different URLs are described in this document.

## General install link
> https://aka.ms/dotnet-download

This URL is part of the output of `dotnet --info`:
```console
To install additional .NET runtimes or SDKs:
  https://aka.ms/dotnet-download
```

It's also part of the error when an SDK command is executed and there's no SDK installed:
```
  It was not possible to find any installed .NET SDKs
  Did you mean to run .NET SDK commands? Install a .NET SDK from:
      https://aka.ms/dotnet-download
```

## Install prerequisites link
> https://go.microsoft.com/fwlink/?linkid=798306  for Windows
> https://go.microsoft.com/fwlink/?linkid=2063366 for OSX
> https://go.microsoft.com/fwlink/?linkid=2063370 for Linux

This URL is part of the error message when the host fails to load `hostfxr`.


## Install missing runtime/framework

> https://aka.ms/dotnet-core-applaunch

This URL is used in various error cases where the host can't find runtime/framework or the right version of these.

### Common parameters

* `arch=<Architecture>` - `x86`, `x64`, `arm`, `arm64` - the architecture of the host
* `rid=<RID>` - the current most specific RID the host knows - so for example `win10-x64`
* `gui=true` - present if the host is a GUI app (so windows desktop only)

### Missing runtime
This will happen when the host can't find `hostfxr`, typically when there's no .NET Core installed.

In this case the URL will contain these parameters:
* `missing_runtime=true` - this marks the case of missing runtime.
* `apphost_version=<Version>` - the version of the `apphost` which was used to create the executable for the app. For example `apphost_version=5.0.0-preview.2.20155.1`. This is included in all cases in 5.0 and above and it may be included in 3.1 only for GUI apps.

In this case the `apphost_version` parameter could be used by the website to determine the major version of the runtime to offer (using latest minor/patch). Also if there's a `gui=true` we should offer the `WindowsDesktop` runtime installer.

Note that using more of the `apphost_version` than just major version is probably not precise enough. But it should be enough to offer the right major version and latest minor - almost all apps should work then.

### Missing framework or framework version
This will happen when framework resolution fails for the app. This can mean that:
* a required framework is completely missing - for example running winforms app on an ASP.NET install of the runtime
* a matching version of the framework was not found - this typically happens when the app requires a version of .NET Core which is not installed.

In this case the URL will contain these parameters:
* `framework=<framework name>` - the name of the requested framework - for example `framework=Microsoft.NETCore.App` or `framework=Microsoft.WindowsDesktop.App`.
* `framework_version=<version>` - the version of the requested framework - for example `framework_version=3.1.0`.

The website should ideally offer the right runtime installer for the app:
* If the framework name is `Microsoft.AspNetCore.App` - this means that some runtime of a matching version is installed but it doesn't have ASP.NET in it. Offer the ASP.NET runtime installer - of the requested version
* If the framework name is `Microsoft.WindowsDesktop.App` - this means that some runtime of a matching version is installed but it doesn't have Windows Desktop in it. offer the Windows Desktop runtime installer - of the requested version
* If the framework name is `Microsoft.NETCore.App` - in this case it probably means that there's no matching install of .NET Core (for example 3.1 app running on a machine with 3.0 only). The website can use the `gui=true` to determine if it should offer Windows Desktop runtime of the right version. In case it's not a GUI app it could be an ASP.NET app or not. If we offer just the core runtime (no ASP.NET), the app may fail again after it's installed, but this time it would denote a missing `Microsoft.AspNetCore.App` framework.
