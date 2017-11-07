# Multi-level SharedFX Lookup

## Introduction

There are two possible ways of running .NET Core Applications: through dotnet.exe or through a custom executable appname.exe. The first one is used when the user wants to run a portable app or a .NET Core command while the second one is used for standalone applications. Both executables share exactly the same source code.

The executable is in charge of finding and loading the hostfxr.dll file. The hostfxr, in turn, must find and load the hostpolicy.dll file (it’s also responsible for searching for the SDK when running .NET commands). At last the coreclr.dll file must be found and loaded by the hostpolicy. Standalone apps are supposed to keep all its dependencies in the same location as the executable. Portable apps must have the runtime files inside predefined folders.

## Semantic Versioning 1.0.0

.NET Core uses the Semantic Versioning system to manage its version number. It’s important to understand how this system works because since it’s being proposed to search files from different locations, it’s necessary to establish the software behavior based on compatibility limitations.

The version number must take the form X.Y.Z where X is the major version, Y is the minor version, and Z is the patch version. Bug fixes and modifications that do not affect the API itself must increment the patch version. Changes that affect the API but have backwards compatibility must increment the minor version and reset the patch version to zero. Finally changes that are backwards incompatible must increment the major version and reset both patch and minor versions to zero.

It’s also possible to append a dash followed by a string after the version number to specify a pre-release. The string must be composed of only alphanumeric characters plus dash. Precedence is determined by lexicographic ASCII sort order.

Versions that are not pre-releases are called productions.

	For instance, a valid Semantic Versioning number sort would be:
	1.0.0 -> 1.0.1-alpha -> 1.0.1 -> 1.1.0-alpha -> 1.1.0-rc1 -> 1.1.0 -> 1.1.1 -> 2.0.0.

 ## Executable

The executable’s only task is to find and load the hostfxr.dll file and pass on its arguments.

Portable applications are supposed to have version folders for hostfxr inside host\fxr directory close to dotnet.exe itself. The most recent version folder is picked by following the Semantic Versioning system described above. The hostfxr.dll file is expected to be inside the chosen folder.

If the file cannot be found, then the user is probably trying to run a standalone application. The running program then searches for the hostfxr.dll file in the executable directory.

It’s important to notice that, at this point, the process still does not make a distinction between portable and standalone apps.

## Hostfxr

### Host mode

The hostfxr’s first task is to determine the running host mode. It’s a muxer if invoked as dotnet.exe, a standalone if invoked as appname.exe, or a splitfx if other conditions apply. Since the following changes will not interfere in the way that standalone and splitfx modes are handled, then it’s safe to assume that we will be dealing with a muxer.

### SDK Search

There are two possibilities for a muxer: it can be a portable app or a .NET Core command.

In the first case the app file path should have been specified as an argument to the dotnet.exe.

In the second case the dotnet.dll from SDK must be invoked as a portable app. At first the running program searches for the global.json file which may have specified a CLI version. It starts from the current working directory and looks for it inside all parent folder hierarchy. After that, it searches for the dotnet.dll file inside the sdk\CLI_version subfolder in the executable directory. If the version defined in the global.json file or the specified version folder cannot be found, then it must choose the most appropriate one. The most appropriate version is defined as the latest version according to the Semantic Versioning system.

### Framework search and rolling forward

The hostfxr then searches for the configuration files appname.runtimeconfig.json and appname.runtimeconfig.dev.json in the same folder as the appname.dll file. The first one contains the specified framework name and version that are necessary to find its folder.

The shared\fxname subfolder in the executable directory is expected to contain some framework version folders. If the required version was passed as an argument to appname.exe, then the framework folder path is already decided.

If the desired version was not passed as an argument, then the one in appname.runtimeconfig.json must be used as a starting point to determine which will be chosen. There are two possible scenarios:

- If the version specified in the configuration file is a production, then the default behavior is to pick the latest available production that differs only in patch.
- If the version specified in the configuration file is a pre-release, then it will pick the exact specified version. If its version folder does not exist, then it will search for the smallest pre-release that is greater than the specified one.

This process of choosing the most appropriate available version instead of the specified one is called “rolling forward”.

Hostfxr must then locate the hostpolicy.dll file:

- Portable apps are expected to have a file called fxname.deps.json inside the framework folder. This file contains information about the application’s dependencies and during most of the time it will be used by the hostpolicy. After locating the json file, the hostfxr must search inside it for what the specified hostpolicy version is.
- The pkgs\hostpolicy_version subfolder below the default servicing directory is expected to contain the hostpolicy.dll.
- If for any reason the file cannot be found, then the running program will search for the hostpolicy.dll file inside the framework folder independently of the version.
- Finally, if the file still cannot be found, it will try looking inside the probing paths passed as arguments to the process.

The hostpolicy is then loaded into memory and executed.

### Proposed hostfxr changes for 2.1 (and 2.0.x long-term-servicing)
There can only be one framework in 2.0. That framework is located in the app's runtimeconfig.json:
```javascript
{
  "runtimeOptions": {
    "tfm": "netcoreapp2.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "2.0.0"
    }
  }
}
```

From the framework's `name` and `version` the appropriate framework location is found as explained earlier.

In order for other frameworks (or platforms such as ASP.NET) to get the same benefits of roll-forward and self-containment for serviceability, 2.1 will support multiple frameworks.

For 2.1, a given framework can only depend upon another single framework. An app can still only depend upon a single framework as well. Thus it repesents a "vertical" hierarchy. It is possible to allow additional frameworks in a "horizontal" manner, but that is out of scope for 2.1.

Each framework has its own roll-forward semantics. This means ASP.NET can roll-forward independently of NETCore.App even though ASP.NET depends upon the NETCore.App framework.

NETCore.App in 2.0 has its own deps.json file in its own folder that lists its assemblies. In 2.1, other frameworks will also have their own deps.jon. In addition, each framework has an optional runtimeconfig.json that describes its framework dependency as well as other settings that exist today (applyPatches, preReleaseRollForward, rollForwardOnNoCandidateFx). If the runtimeconfig.json file does not exist, or does not have a value for a setting, it uses the value from the higher-level runtimeconfig.json.

For example, an MVC app's runtimeconfig.json would contain (actual framework names TBD):
```javascript
"framework": {
      "name": "Microsoft.AspNetCore.All",
      "version": "2.1.0"
    }
```
and Microsoft.AspNetCore.All's runtimeconfig.json would contain:
```javascript
"framework": {
      "name": "Microsoft.NETCore.App",
      "version": "2.1.0"
    }
```
and Microsoft.NETCore.App would not have a runtimeconfig.json because it doesn't have any framework dependency or need to change other settings.

## Hostpolicy

Hostpolicy is in charge of looking for all dependencies files required for the application. That includes the coreclr.dll file which is necessary to run it.

It will look for the json files that specify the needed assemblies’ filenames:

- If the appname.deps.json file path has not been specified as an argument, then it is expected to be inside the application directory.
- Portable apps are supposed to have an fxname.deps.json file inside the framework folder.

Both files carry the filenames for dependencies that must be found. They can be categorized as runtime, native or resources assemblies. The coreclr.dll file is expected to be found during the native assemblies search.

At last, the coreclr is loaded into memory and called to run the application.

### Proposed hostpolicy changes for 2.1 (and 2.0.x long-term-servicing)
For 2.0, there are several probing paths that are used to find the dependencies. These paths follow a certain order and the first assembly found wins and that location will be passed to the coreclr. For example, the local app location has priority over the shared framework locations and if the same assembly exists in both locations, the coreclr will end up using the local app's copy of that assembly.

These semantics will be unchanged for 2.1 except when a roll-forward is performed at a non-patch version (meaning a change to the major or minor version). For these cases, the highest assembly version wins. This is necessary in run-time scenarios to prevent assembly load exceptions which occur when an assembly is referencing a higher version of another assembly, but a lower version is actually found.

There will be some checks added to hostpolicy to catch these conflicts during development scenarios, but since this conflict scenario should only occur during roll-forward, and on a version of the framework likely not even released yet during development, these checks will focus on correctness of the currently targeted framework. This ennsures that any duplicate versions higher up the framework stack have a newer assembly version than the copies of that assembly in the lower level frameworks.

This situation of having assembly conflicts (or duplicates) is more likely to occur when there are multiple frameworks (as convered in hostfxr's proposed changes for 2.1), so it is important to address in the 2.1 timeframe.

## Global locations

In addition to searching the executable directory, the global .NET location is also searched. The global folders may vary depending on the running operational system. They are defined as follows:

Global .NET location:

	Windows 32-bit: %SystemDrive%\Program Files\dotnet
	Windows 64-bit (32-bit application): %SystemDrive%\Program Files (x86)\dotnet
	Windows 64-bit (64-bit application): %SystemDrive%\Program Files\dotnet
	Unix: none
	
### Framework search

If the specified version is defined through the configuration json file, the search must be conducted as follows:

- For productions:

	1.	In relation to the executable directory: search for the most appropriate version by rolling forward. If it cannot be found, proceed to the next step.
	2.	In relation to the global location: search for the most appropriate version by rolling forward. If it cannot be found, then we were not able to locate any compatible version.

- For pre-releases:
	
	1.	In relation to the executable directory: search for the specified version. If it cannot be found, search for the most appropriate version by rolling forward. If no compatible version can be found, proceed to the next step.
	2.	In relation to the global location: search for the specified version. If it cannot be found, search for the most appropriate version by rolling forward. If no compatible version can be found, then we were not able to locate any compatible version.

In the case that the desired version is defined through an argument, the multi-level lookup will happen as well but it will only consider the exact specified version (it will not roll forward).

#### Tests

To make sure that the changes are working correctly, the following behavior conditions will be verified through tests:

- Folders must be verified in the correct order.
- If production, then a roll forward must happen in a given folder before proceeding to the next one.
- If pre-release, then a roll forward must happen in a given folder only if the specified version is not found. If there is no compatible version available, then it must proceed to the next location.
- If the version is specified through an argument, then roll forwards are not allowed to happen.
- If no compatible version folder is found, then an error message must be returned and the process must end.


### SDK search

Like the Framework search, the SDK is searched for a compatible version. Instead of looking for it only in relation to the executable directory, it is also searched in the folders specified above by following the same priority rank.

The search is conducted as follows:

1.	In relation to the executable directory: search for the specified version. If it cannot be found, choose the most appropriate available version. If there’s no available version, proceed to the next step.
2.	In relation to the global location: search for the specified version. If it cannot be found, choose the most appropriate available version. If there’s no available version, then we were not able to find any version folder and an error message is returned.

Unlike the Framework search, the SDK search does a roll-forward for pre-release versions when the patch version changes. For example, if you install v2.0.1-pre, it will be used over v2.0.0.
