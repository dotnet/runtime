# corehost runtime/assembly resolution

The shared host locates assemblies and native libraries using a combination of: Servicing Index, Files in the application folder (aka "app-local") and files from package caches.

## Definitions and Formats

### Terms/Notes

* The term "Library" (Title Case) is used throughout this document to refer to a NuGet Package. We use this term because it is used in the code to represent multiple things: Packages, Projects and Framework Assemblies are all types of "Library".
* All of the assembly resolution here refers to setting up the default assembly load context for the runtime. Further dynamic loads (plugins, etc.) can still have custom resolution logic provided by using an `AssemblyLoadContext` in the managed code. Essentially, we are setting up the necessary assemblies to launch the `Program.Main` (and all the assemblies that are statically-referenced by that).

### Servicing Index

The servicing index is loaded when the `CORE_SERVICING` environment variable is non-empty. When this variable is non-empty, it points to a directory that will be the **Servicing Root**. There may be platform-specific default locations, to be determined later.

An index file is located at the path defined by `$CORE_SERVICING/servicing_index.txt`. In this file are a series of lines of one of the following formats:

```
# Lines starting with a '#' and blank lines are ignored.

# Identifies an asset from a NuGet Package that has been serviced.
package|[Package ID]|[Package Version]|[Original Asset Relative Path]=[New Asset Path, relative to Servicing Root]
package|System.Threading.Thread|1.2.3.4|lib/dotnet5.4/System.Threading.Thread.dll=patches/abc123/System.Threading.Thread.dll

# TBD: Host/Runtime servicing entries.
```

Paths in this file are **always** specified using `/`, even on Windows. They must be converted to platform-specific directory separators.

This index is loaded when needed during the Resolution Process (see below).

### Runtime Configuration File

The runtime configuration file is used to determine settings to apply to the runtime during initialization and for building the TPA and Native Library Search Path lists. See the [spec for the runtime configuration file](runtime-configuration-file.md) for more information.

### Files in the application folder

Any file with the suffix `.dll` in the same folder as the managed application being loaded (the "Application Base") will be considered a viable assembly during the resolution process. The host **assumes** that the assembly's short name is the same as the file name with the `.dll` suffix removed (yes, this is not technically required by the CLR, but we assume it for use with this host).

### Files from package caches

Only assemblies listed in the dependencies file can be resolved from a package cache. To resolve those assemblies, two environment variables are used:

* `DOTNET_PACKAGES` - The primary package cache. If not set, defaults to `$HOME/.nuget/packages` on Unix or `%LOCALAPPDATA%\NuGet\Packages` (TBD) on Windows. **NOTE**: Currently the host uses different folders as we are still coordinating with NuGet to get the directories right (there are compatibility considerations). Currently we always use `$HOME/.dnx/packages`(Unix)/`%USERPROFILE%\.dnx\packages`(Win).
* `DOTNET_PACKAGES_CACHE` - The secondary cache. This is used by shared hosts (such as Azure) to provide a cache of pre-downloaded common packages on a faster disk. If not set, it is not used.

Given the Package ID, Package Version, Package Hash and Asset Relative Path provided in the runtime configuration file, **and the assembly is not serviced** (see the full resolution algorithm below) resolution proceeds as follows (Unix-style paths will be used for convenience but these variables and paths all apply to Windows as well):

1. If `DOTNET_PACKAGES_CACHE` is non-empty, read the file `$DOTNET_PACKAGES_CACHE/[Package ID]/[Package Version]/[Package Id].[Package Version].nupkg.sha512` if present. If the file is present and the content matches the `[Package Hash]` value from the dependencies file. Use that location as the Package Root and go to 3
2. Using `DOTNET_PACKAGES`, or it's default value, use `$DOTNET_PACKAGES/[Package ID]/[Package Version]` as the Package Root
3. Concatenate the Package Root and the Asset Relative Path. This is the path to the asset (managed assembly or native library).

## Assembly Resolution

During host start-up, the host identifies if a runtime configuration file is present and loads it. It also scans the files located in the Application Base and determines the assembly name for each (using the file name). It builds a set of assembly names to be loaded by the union of the assembly names listed in runtime configuration file and the assemblies located in the Application Base.

A runtime configuration file is **not** required to successfully launch an application, but without it, all the dependent assemblies must be located within the same folder as the application. Also, since servicing is performed on the basis of packages, an application without a runtime configuration file file cannot use the servicing index to override the location of assemblies.

The host looks for the `.deps` file in the Application Base with the file name `[AssemblyName].deps`. The path to the deps file can be overridden by specifying `--depsfile:{path to deps file}` as the first parameter to the application.

Given the set of assembly names, the host performs the following resolution. In some steps, the Package ID/Version/Relative Path data is required. This is only available if the assembly was listed in the deps file. If the assembly comes from the app-local folder, these resolution steps are skipped.

1. If there is an entry in the servicing index for the Package ID/Version/Relative Path associated with the assembly, the Servicing Root is concatenated with the New Asset Path from the index and used as the Assembly Path. This occurs **even if the assembly is also located app-local**, as long as it is also in the runtime configuration file.
2. If there is a file in the Application Base with the file name `[AssemblyName].dll`, `[AssemblyName].ni.dll`, `[AssemblyName].exe`, or `[AssemblyName].ni.exe` (in that order), it its full path is used as the Assembly Path
3. The Assembly Path is resolved out of the Package Caches using the algorithm above (in 'Files from package caches').

A similar process is used to produce a list of Native Library Paths. Native libraries are listed in the runtime configuration file (under the `native` asset section) and searched in the same way as managed assemblies (Servicing, then app-local, then package caches). The main exception is that the app-local file extensions vary by platform (`.dll` on Windows, `.so` on Linux, `.dylib` on Mac OS X)

Once a the list of assemblies and native libraries is produced, the host will check for duplicates. If both an `.ni.dll`/`.ni.exe` image and a `.dll`/`.exe` assembly are found for an assembly, the native image will be preferred and the IL-only assembly will be removed. The presence of two different paths for the same assembly name will be considered an error. The managed assemblies are provided in the Trusted Platform Assemblies (TPA) list for the CoreCLR during startup. The folder paths for each native library are deduplicated and provided in the Native Search Paths list for the CoreCLR during startup.

**NOTE**: The CLR may add support for providing a similar structure as the TPA list for native libraries (i.e. a flat list of full file paths).

### Satellite Assemblies

Satellite Assemblies (assemblies containing only embedded resources used in place of the resources provided by an assembly) are detected by path convention in the host. The convention will be to look at the last two segments of the path (the file name and the immediate parent directory name). If the parent directory matches an [IETF Language Tag](https://en.wikipedia.org/wiki/IETF_language_tag) (or more specifically, a value usable in the Culture field for a CLR Assembly Name), then the assembly is considered culture-specific (for the culture specified in that folder name). Upon determining this, the host will place the culture-neutral assemblies on the TPA list and provide the directories containing the assemblies as Platform Resource Roots to the CLR to allow it to locate the assemblies.

## Runtime Resolution

Runtime resolution is controlled by these environment variables:

* `DOTNET_RUNTIME_SERVICING` -> Global override for runtime
* `DOTNET_PACKAGES_CACHE` -> Secondary cache
* `DOTNET_PACKAGES` -> Package restore location

The runtime is located by searching the following paths in order, where `APP_BASE` refers to the directory containing the managed application assembly and `LIBCORECLR_NAME` refers to the platform-specific name for the CoreCLR library (`libcoreclr.so` on Unix, `libcoreclr.dylib` on Mac OS X, `coreclr.dll` on Windows). The first path that matches is used as the path to load the CoreCLR from.

* `$DOTNET_RUNTIME_SERVICING/runtime/coreclr/LIBCORECLR_NAME`
* `$DOTNET_PACKAGES_CACHE/<Package Id>/<Package Version>/runtimes/<RID>/native/LIBCORECLR_NAME`
* `APP_BASE/LIBCORECLR_NAME`
* `$DOTNET_PACKAGES/<Package Id>/<Package Version>/runtimes/<RID>/native/LIBCORECLR_NAME`
* On Windows:
    * WoW64: `%ProgramFiles(x86)%\dotnet\shared\<Package Name>\<Package Version>\LIBCORECLR_NAME`    
    * Otherwise: `%ProgramFiles%\dotnet\shared\<Package Name>\<Package Version>\LIBCORECLR_NAME`
* On Mac OS X:
    * `/usr/local/share/dotnet/shared/<Package Name>/<Package Version>/LIBCORECLR_NAME`
* On Unix:
    * `/usr/share/dotnet/shared/<Package Name>/<Package Version>/LIBCORECLR_NAME`
