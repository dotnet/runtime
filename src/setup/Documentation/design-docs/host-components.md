# Components of the hosting

The .NET Core default hosting setup consists of several components which are described here.

## Executable
The executable, which is also sometimes refered to as the "host" can be either the
* `dotnet.exe` - which comes from a shared location and is typically the latest version available on the machine
* `apphost.exe` - which is used in case of self-contained apps. The name of the .exe will be app dependent and it comes with the app

The executable does just one thing, it finds the `hostfxr.dll` (for Windows, Linux and Max use their respective naming schemes) and passes control to it.
For `dotnet.exe` host the hostfxr is located next to the host.
For self-contained apps (apphost scenario):
1. The app's folder is searched first
2. If the `DOTNET_ROOT` environment variable is defined, that path is searched
3. The default shared locations are searched

## Host FXR
This library finds and resolves the runtime and all the frameworks the app needs. Then it loads the `hostpolicy.dll` (on Windows) and transfers control to it.

The library reads the `.runtimeconfig.json` of the app (and all it's dependent frameworks) and resolves the frameworks. It implements the algorithm for framework resolution as described in [SharedFX Lookup](multilevel-sharedfx-lookup.md).

In most cases the hostfxr used is the latest version available on the machine (self-contained apps use the one they bring with them).

The main reason to split the executable host and the hostfxr is to allow for servicing the logic in hostfxr without the need to stop all instances of the executable host currently running.

## Host Policy
The host policy library implements all the policies to actually load the runtime, apply configuration, resolve all app's dependencies and actually run the app.

The host policy library lives in the runtime folder and is versioned alongside it. Which version is used is specified by the app as it specifies which version of the .NET Core runtime to use.

The library reads the `.deps.json` file of the app (and the frameworks and any dependent libraries and packages). It resolves all the assemblies specified in the `.deps.json` for the app and creates a list of assembly paths (so called TPA). It does a similar thing for native dependencies as well.

Finally the app loads the runtime - `coreclr.dll` (on Windows) and initializes it (among other things with the TPA). Then it calls the runtime to execute the app.