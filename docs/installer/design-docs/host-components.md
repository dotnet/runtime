# Components of the hosting

The .NET Core default hosting setup consists of several components which are described here.

## Entry-point hosts
.NET Core comes with several executables/libraries which act as the main entry-point to start code execution. These are typically referred to as the "host":
* `dotnet` (executable) - which comes from a shared location and is typically the latest version available on the machine. This is also sometimes called the "muxer".
* `apphost` (executable) - which is used to give app an actual executable which can be run directly. The executable will be named using the application name. The advantage of having an app-local executable is that it can be customized for each app (not just the name, but icon, OS behavior and so on).
* `comhost` (library) - which is used to enable COM server hosting. Component which wants to expose COM server objects will be built with this dynamic library in its output. The `comhost` then acts as the main entry point for the OS.

The executable does just one thing, it finds the `hostfxr` library and passes control to it. It also exposes the right entry points for its purpose (so the "main" for `dotnet` and `apphost`, the COM exports for `comhost` and so on).
* `dotnet` host - `hostfxr` is obtained from the `./shared/host/fxr<highestversion>` folder (relative to the location of the `dotnet` host).
* `apphost` and `comhost` - `hostfxr` is located by
    1. The app's folder is searched first. This is either the folder where the `apphost` or `comhost` lives or in case of `apphost` it is the path it has embedded in it as the app path.
    1. If the `DOTNET_ROOT` environment variable is defined, that path is searched
    1. The default shared locations are searched

## Host FXR
This library finds and resolves the runtime and all the frameworks the app needs. Then it loads the `hostpolicy` library and transfers control to it.

The library reads the `.runtimeconfig.json` of the app (and all it's dependent frameworks) and resolves the frameworks. It implements the algorithm for framework resolution as described in [SharedFX Lookup](multilevel-sharedfx-lookup.md).

In most cases the latest available version of `hostfxr` is used. Self-contained apps use `hostfxr` from the app folder.

The main reason to split the entry-point host and the `hostfxr` is to allow for servicing the logic in `hostfxr` without the need to stop all instances of the executable host currently running.

## Host Policy
The host policy library implements all the policies to actually load the runtime, apply configuration, resolve all app's dependencies and calls the runtime to run the app.

The host policy library lives in the runtime folder and is versioned alongside it. Which version is used is specified by the app as it specifies which version of the .NET Core runtime to use (done by directly or indirectly referencing the `Microsoft.NETCore.App` framework, or carrying everything app-local).

The library reads the `.deps.json` file of the app (and the `.deps.json` of all the referenced frameworks). It resolves all the assemblies specified in the `.deps.json` for the app and creates a list of assembly paths (also called TPA). It does a similar thing for native dependencies as well.

Finally the library loads the runtime `coreclr` library and initializes it (among other things with the TPA). The version of the runtime (and its location) is now already determined since the host policy was loaded from it. Then it calls the runtime with the configuration information which runs the app or performs other requested actions (like COM activation).