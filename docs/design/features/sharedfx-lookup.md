# SharedFX Lookup

## Introduction

There are two main ways of running .NET Applications: through `dotnet` or through the `apphost` executables. The executable is in charge of finding and loading `hostfxr`. `hostfxr`, in turn, must find and load `hostpolicy`. It is also responsible for searching for the SDK when running .NET SDK commands. Finally, `hostpolicy` must find and load the runtime (`coreclr`). See [host components](host-components.md) for details.

An application can either be [framework-dependent](https://docs.microsoft.com/dotnet/core/deploying/#publish-framework-dependent) or [self-contained](https://docs.microsoft.com/dotnet/core/deploying/#publish-self-contained). Framework-dependent apps must have the runtime files inside predefined folders. Self-contained apps are expected to have their dependencies in the same location as the executable.

## Semantic Versioning

.NET Core uses the Semantic Versioning system to manage its version number. It’s important to understand how this system works because since it’s being proposed to search files from different locations, it’s necessary to establish the software behavior based on compatibility limitations.

The version number must take the form X.Y.Z where X is the major version, Y is the minor version, and Z is the patch version. Bug fixes and modifications that do not affect the API itself must increment the patch version. Changes that affect the API but have backwards compatibility must increment the minor version and reset the patch version to zero. Finally changes that are backwards incompatible must increment the major version and reset both patch and minor versions to zero.

It’s also possible to append a dash followed by a string after the version number to specify a pre-release. The string must be composed of only alphanumeric characters plus dash. Precedence is determined by lexicographic ASCII sort order.

Versions that are not pre-releases are called productions.

	For instance, a valid Semantic Versioning number sort would be:
	1.0.0 -> 1.0.1-alpha -> 1.0.1 -> 1.1.0-alpha -> 1.1.0-rc1 -> 1.1.0 -> 1.1.1 -> 2.0.0.

 ## Executable

The executable’s only task is to find and load the hostfxr.dll file and pass on its arguments.

Framework-dependent applications are supposed to have version folders for hostfxr inside host\fxr directory close to dotnet.exe itself. The most recent version folder is picked by following the Semantic Versioning system described above. The hostfxr.dll file is expected to be inside the chosen folder.

If the file cannot be found, then the user is probably trying to run a self-contained application. The running program then searches for the hostfxr.dll file in the executable directory.

It’s important to notice that, at this point, the process still does not make a distinction between framework-dependent and self-contained apps.

## Hostfxr

### Host mode

The hostfxr’s first task is to determine the running host mode. It’s a muxer if invoked as dotnet.exe, a self-contained application if invoked as appname.exe, or a splitfx if other conditions apply. Since the following changes will not interfere in the way that self-contained and splitfx modes are handled, then it’s safe to assume that we will be dealing with a muxer.

### SDK Search

There are two possibilities for a muxer: it can be a framework-dependent app or a .NET Core command.

In the first case the app file path should have been specified as an argument to the dotnet.exe.

In the second case the `dotnet.dll` from SDK must be invoked as a framework-dependent app. At first the running program searches for the `global.json` file which may have specified a CLI version. It starts from the current working directory and looks for it inside all parent folder hierarchy. After that, it searches for the dotnet.dll file inside the `sdk\<CLI_version>` sub-folder in the executable directory.
The exact algorithm how versions as matched is described (with some history) in the [docs](https://docs.microsoft.com/en-us/dotnet/core/tools/global-json#matching-rules)

Note: if the SDK lookup is invoked through `hostfxr_resolve_sdk2` the algorithm is the same, expect that the function can disallow pre-release versions via the `hostfxr_resolve_sdk2_flags_t::disallow_prerelease` flag.

### Framework search and rolling forward

The hostfxr then searches for the configuration files appname.runtimeconfig.json and appname.runtimeconfig.dev.json in the same folder as the appname.dll file. The first one contains the specified framework name and version that are necessary to find its folder.

The shared\fxname subfolder in the executable directory is expected to contain some framework version folders. If the required version was passed as an argument to appname.exe, then the framework folder path is already decided.

If the desired version was not passed as an argument, then the one in appname.runtimeconfig.json must be used as a starting point to determine which will be chosen. There are two possible scenarios:

- If the version specified in the configuration file is a production, then the default behavior is to pick the latest available production that differs only in patch.
- If the version specified in the configuration file is a pre-release, then it will pick the exact specified version. If its version folder does not exist, then it will search for the smallest pre-release that is greater than the specified one.

This process of choosing the most appropriate available version instead of the specified one is called “rolling forward”.

Hostfxr must then locate the hostpolicy.dll file:

- Framework-dependent apps are expected to have a file called fxname.deps.json inside the framework folder. This file contains information about the application’s dependencies and during most of the time it will be used by the hostpolicy. After locating the json file, the hostfxr must search inside it for what the specified hostpolicy version is.
- The pkgs\hostpolicy_version subfolder below the default servicing directory is expected to contain the hostpolicy.dll.
- If for any reason the file cannot be found, then the running program will search for the hostpolicy.dll file inside the framework folder independently of the version.
- Finally, if the file still cannot be found, it will try looking inside the probing paths passed as arguments to the process.

The hostpolicy is then loaded into memory and executed.

### Chained frameworks (2.1+)
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

For 2.1, a given framework can only depend upon another single framework. An app can still only depend upon a single framework as well. Thus it represents a "vertical" hierarchy. It is possible to allow additional frameworks in a "horizontal" manner, but that is out of scope for 2.1.

Each framework has its own roll-forward semantics. This means ASP.NET can roll-forward independently of NETCore.App even though ASP.NET depends upon the NETCore.App framework.

NETCore.App in 2.0 has its own deps.json file in its own folder that lists its assemblies. In 2.1, other frameworks will also have their own deps.json. In addition, each framework has an optional runtimeconfig.json that describes its framework dependency including optional setting overrides (applyPatches, rollForwardOnNoCandidateFx). If the runtimeconfig.json file does not exist, or does not have a value for a setting, it uses the values from the app's runtimeconfig.json or from environment variables.

For example, an MVC app's runtimeconfig.json would contain:
```javascript
"framework": {
      "name": "Microsoft.AspNetCore.App",
      "version": "2.1.0"
    }
```
and Microsoft.AspNetCore.App's runtimeconfig.json would contain:
```javascript
"framework": {
      "name": "Microsoft.NETCore.App",
      "version": "2.1.0"
    }
```
and Microsoft.NETCore.App would not have a runtimeconfig.json because it doesn't have any framework dependency or need to change settings.

### Multiple Frameworks (3.0+)
The 2.1 release added support for a chain of frameworks, where each framework can have one dependent framework. However, with the advent of frameworks for WPF and WinForms it becomes necessary for an application to be able to reference more than one dependent framework.

The runtimeconfig.json will have a new `frameworks` array section that allows more than one framework to be specified:
```javascript
"runtimeOptions": {
	"rollForwardOnNoCandidateFx" : 1,
	"applyPatches" : true,
	"frameworks": [
			{
					"name": "Microsoft.AspNetCore.All",
					"version": "3.0.0"
			},
			{
					"name": "Microsoft.Forms",
					"version": "3.0.0",
					"rollForwardOnNoCandidateFx": 1,
					"applyPatches": true
			}
	]
}
```

If an entry also exists in the `framework` section, it is treated as the first element in the `frameworks` array. Thus the `framework` section is no longer required but is supported for backwards compatibility.

The `applyPatches` and `rollForwardOnNoCandidateFx` continue to be supported globally in the `runtimeOptions` section, but can now also be specified individually for each framework. These per-framework settings override any corresponding values in the `runtimeOptions` section.

By allowing more than one framework reference, we may encounter issues with multiple references to the same framework but with different versions or with different roll-forward settings. The rules to reconcile that include:
- All existing roll-forward rules are applied to each reference to a framework individually, respecting each `version`, `applyPatches` and `rollForwardOnNoCandidateFx` value.
- The *most restrictive* value of every `applyPatches` and `rollForwardOnNoCandidateFx` entry are used when resolving a given framework:
  - `applyPatches` `false` is more restrictive than `true`
  - `rollForwardOnNoCandidateFx` `0` (no roll-forward) is more restrictive than `1` (Patch and Minor) or `2` (Patch, Minor and Major).
  - `rollForwardOnNoCandidateFx` `1` is more restrictive than `2`.
  - Note that if there are no explicit values for `rollForwardOnNoCandidateFx`, then the environment variable `DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX` is used (there is no environment variable for `applyPatches`). If there is no environment or config settings, then the default values are used: `applyPatches=true`, and `rollForwardOnNoCandidateFx=1`.
- The highest `version` value of a given framework is selected.

So, for example, if there are two references:
- `Foo 2.1.0` with `rollForwardOnNoCandidateFx=0`
- `Foo 2.2.0` with `rollForwardOnNoCandidateFx=1`

then that will always fail and result in a framework not found error. This example fails because `2.1.0` does not allow roll-forward on Minor (`rollForwardOnNoCandidateFx=0`) and because of the specified version `2.2.0`.

#### Best practices for a runtimeconfig.json:
- <B>No Restrictive Roll-Forward Overrides:</B> do not specify `applyPatches` and `rollForwardOnNoCandidateFx` in the runtimeconfig.json unless absolutely necessary. These should only be considered to work around issues in the field, by the end user, and not set by default by any framework.
 - The rollForwardOnNoCandidateFx can also be controlled by environment variables, it should be very rare that we need to override these in the runtimeconfig.json, and especially at a per-framework level.
 - The one exception to this is to use a *less restrictive* setting by specifying `rollForwardOnNoCandidateFx=2` which allows roll-forward by Major (in addition to Minor and Patch). The default value is `1` (Minor \ Patch only).
- <B>No Redundant References:</B> when a given framework "foo" ships it should not create a case of having more than one reference to the another framework "bar". The reason is that base frameworks already specify "bar" so there is no reason to re-specify it. However, there are potential valid reasons to re-specify the framework:
	- To force a newer version of a given framework which is referenced by lower-level frameworks. However assuming first-party frameworks are coordinated, this reason should not be exist for first-party runtimeconfig.json files.
	- To be redundant if there are several "smaller" or "optional" frameworks being used and no guarantee that a base framework will always reference the smaller frameworks over time.
	- To provide a hint of the newest framework version. This would likely only be in the app's runtimeconfig.json, and during roll-forward scenarios. This would be used to prevent re-resolving the frameworks (finding the most compatible framework on disk) which can happen when a lower-level framework requires a newer version of a another framework that was already resolved. By providing the hint at a higher-level, the correct framework version will be found the first time.
- <B>No Circular References:</B> there should not be any circular dependencies between frameworks.
  - It is not normally a desirable design for the same reasons why circular references in assemblies and packages are not supported or supported well (chicken-egg creation, simultaneous version changes).
  - One potential future case is to allow "pseudo-circular" dependencies where framework "foo" loads a light-up framework which depends on "foo". Internally the foo->lightup reference may be treated as a late-bound framework reference, thus causing a cycle. This potential feature may replace the "additional deps" feature in a way that allows for richer light-up scenarios by allowing the lightup to specify framework dependency(s) and have a small deps.json.
- <B>No Downgrading:</B> a newer version of a shared framework should keep or increase the version to another shared framework (never decrease the version number).
By following these best practices we have optimal run-time performance (less processing and probing) and less chance of incompatible framework references.

#### Algorithm
Terminology:
- `config list`: entries for a single runtimeconfig.json which consists of framework `name`, `version`, optional `applyPatches`, and optional `rollForwardOnNoCandidateFx`.
- `newest list`: entries keyed off of framework name that contain the highest framework version requested. It is used to perform "soft" roll-forwards to compatible references of the same framework name without reading the disk or performing excessive re-try (Step 7).
- `resolved list`: a list of frameworks that have been resolved, meaning a compatible framework was found on disk.

Algorithm:
1. Determine the `config list`:
  - Parse the application's runtimeconfig.json `runtimeOptions.frameworks` section.
  - If the `runtimeOptions.framework.name` and `runtimeOptions.framework.version` exist, Then insert that framework into the beginning of the `config list`.
2. For each framework in `config list`:
3. --> If the framework is not currently in the `newest list` list Then add it.
  - By doing this here, before the next loop, we minimize the number of re-try attempts.
4. For each framework in `config list`:
5. --> If the framework is not in `resolved list` Then resolve the framework
 - Use the framework version from `newest list` if newer than the reference, otherwise update `newest list` if reference is newer.
 - We may fail here if not compatible.
 - Probe for the framework on disk
 - If success add it to `resolved list` and make a recursive call back to Step 2 but pass in a new `config list` based upon the values from the newly resolved framework's runtimeconfig.json which may reference additional frameworks.
6. --> ElseIf the version is < resolved version  Then perform a "soft" roll-forward.
  - We may fail here if not compatible.
7. --> Else re-start the algorithm (goto Step 1) with new \ clear state except for `newest list` so we attempt to use the newer version next time.

This algorithm for resolving the various framework references assumes the <B>>No Downgrading</B> best practice explained above in order to prevent loading a newer version of a framework than necessary.

#### Discussion points:
- By choosing the "most restrictive" values for `applyPatches` and `rollForwardOnNoCandidateFx` we limit what changes the app developer can do to work around issues without being forced to modify the framework's runtimeconfig.json files.
  - For example, if a framework "foo" depends on framework "bar" version 2.0.0. with an explicit framework setting of `rollForwardOnNoCandidateFx=0` and only 2.1.0 is installed, a framework load error will occur at runtime and the app developer will not be able to force 2.1.0 to be loaded (without modifying the framework's runtimeconfig.json file).
	- According to the best practice "No Restrictive Roll-Forward Overrides", the framework reference to "bar" should not specify `rollForwardOnNoCandidateFx=0`, and thus we would not encounter this issue.
- If we expect this feature to be used to create several smaller-grain or "optional" frameworks, we may want to add a concept of a "private" framework reference so that lower-level references to these optional frameworks are not automatically "lifted" to the app level. This would help with forward-compatibility if lower-level frameworks remove a reference to optional framework, because the app would have its own reference to the optional framework.

## Hostpolicy

Hostpolicy is in charge of looking for all dependencies files required for the application. That includes the coreclr.dll file which is necessary to run it.

It will look for the json files that specify the needed assemblies’ filenames:

- If the appname.deps.json file path has not been specified as an argument, then it is expected to be inside the application directory.
- Framework-dependent apps are supposed to have an fxname.deps.json file inside the framework folder.

Both files carry the filenames for dependencies that must be found. They can be categorized as runtime, native or resources assemblies. The coreclr.dll file is expected to be found during the native assemblies search.

At last, the coreclr is loaded into memory and called to run the application.

### Probing for Assemblies
Assemblies are found by looking through probing paths in a certain order.

* In version 2.0, the local app location has priority over the shared framework locations and if the same assembly exists in both locations, the coreclr will end up using the local app's copy of that assembly.
* In version 2.1 and later, if multiple assemblies with the same name are found, the assembly with the highest version wins. This is necessary to avoid downgrading assembly versions requested by the application.

In order to compare versions of an assembly, the assemblyVersion and fileVersion attributes will be added for each assembly in the deps.json files. The application and every framework contains a <name>.deps.json file. The assemblyVersion is compared first, and if equal, the fileVersion is used as a tie-breaker.


## Global locations

Global install locations are described in the [install locations design](https://github.com/dotnet/designs/blob/main/accepted/2020/install-locations.md) document.

**.NET 7.0 and above**

When running `dotnet`, only the executable directory will be searched and global locations are not searched. For all other [entry-point hosts](host-components.md#entry-point-hosts), if the `DOTNET_ROOT` environment variable is set, that path is searched. If the environment variable is not set, the global location as described in [install locations](https://github.com/dotnet/designs/blob/main/accepted/2020/install-locations.md) is searched.

See [disable multi-level lookup](https://github.com/dotnet/designs/blob/main/accepted/2022/disable-multi-level-lookup-by-default.md) for more details.

**Before .NET 7.0**

In addition to searching the executable directory, the global .NET location is also searched. The global folders may vary depending on the running operational system. They are defined as follows:

Global .NET location:

	Windows 32-bit: %ProgramFiles%\dotnet
	Windows 64-bit (32-bit application): %ProgramFiles(x86)%\dotnet
	Windows 64-bit (64-bit application): %ProgramFiles%\dotnet
	Unix: none
	OSX: none

Default installation location. In certain cases when a framework-dependent apphost (e.g. myapp.exe) is executed (which is new functionality for 2.1) a default location will be used which varies per platform:

	Windows: The same as "Global .NET location" above
	Unix: /usr/share/dotnet
	OSX: /usr/local/share/dotnet


### Framework search

Using the specified version from the `--fx-version` argument or from the runtimeconfig.json file, the search is conducted as follows:

1.	Use the first of following locations to determine the most appropriate version:
 - If the muxer (e.g. dotnet.exe) use the directory of the muxer that is being executed.
 - For 2.1+, if the apphost (e.g. myapp.exe) use the environment variable %DOTNET_ROOT% (empty by default)
 - For 2.1+, if the apphost and %DOTNET_ROOT% is empty, use the `Default installation location`.

2.	Obtain the most appropriate version from the `Global .NET location` and compare it against the most appropriate version from the first step. Select the most appropriate version from the two locations. If a compatible framework cannot be found, then an error will be displayed.

Determine the most appropriate version varies for release (production) and pre-release versions.
- For releases:
1.	Search for the version specified. If it cannot be found, roll-forward to the closest version (behavior is configurable).
2. Once a version has been selected, roll-forward to the latest `patch` version (this functionality is enabled by default, but can be turned off).

- For pre-releases:

1.	Search for the version specified in the runtimeconfig.json. If it cannot be found, roll-forward to the closest pre-release `build` version meaning it must have the same `major`, `minor` and `patch` version.

In the case that the desired version is defined through an argument, the multi-level lookup (`Global .NET location`) will happen as well but it will only consider the exact specified version (it will not roll forward).

#### Tests

To make sure that the changes are working correctly, the following behavior conditions will be verified through tests:

- Folders must be verified in the correct order.
- If release, then a roll forward must happen in a given folder before proceeding to the next one.
- If pre-release, then a roll forward must happen in a given folder only if the specified version is not found. If there is no compatible version available, then it must proceed to the next location.
- If the version is specified through an argument, then roll forwards are not allowed to happen.
- If no compatible version folder is found, then an error message must be returned and the process must end.


### SDK search

Like the Framework search, the SDK is searched for a compatible version.

Unlike the Framework search, the SDK search does a roll-forward for pre-release versions when the patch version changes. For example, if you install v2.0.1-pre, it will be used over v2.0.0.

**.NET 7.0 and above**

Only the executable directory will be searched. See [disable multi-level lookup](https://github.com/dotnet/designs/blob/main/accepted/2022/disable-multi-level-lookup-by-default.md) for more details.

**Before .NET 7.0**

Aside from looking for it in relation to the executable directory, it is also searched in the folders specified above by following the same priority rank.

The search is conducted as follows:

1.	In relation to the executable directory: search for the specified version. If it cannot be found, choose the most appropriate available version. If there’s no available version, proceed to the next step.
2.	In relation to the global location: search for the specified version. If it cannot be found, choose the most appropriate available version. If there’s no available version, then we were not able to find any version folder and an error message is returned.
