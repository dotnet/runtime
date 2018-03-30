# Additional-deps

## Summary
This document describes current (2.0) and proposed (2.1) behavior for "light-up" scenarios regarding additional-deps functionality.

The `deps.json` file format specifies assets including managed assemblies, resource assemblies and native libraries to load.

Every applicaton has its own `<app>.deps.json` file which is automatically processed. If an application needs additional deps files, typically for "lightup" extensions, it can specify that by:
- The `--additional-deps` command line option
- If this is not set, the `DOTNET_ADDITIONAL_DEPS` environment variable is used

The value can be a combination of:
- A path to a deps.json file
- A path to a folder which can contain several deps.json files
separated by a path delimiter (e.g. `;` on Windows, `:` otherwise).

When additional-deps specifies a folder:
- The resulting folder can have more than one deps.json files; all will be processed
- If there are several frameworks (e.g. Microsoft.AspNetCore.App, Microsoft.AspNetCore.All, Microsoft.NETCore.App) then each will be processed

## 2.0 behavior
When additional-deps specifies a folder, the subfolder must follow a naming convention of `shared/<framework_name>/<requested_framework_version>`

The semantics of `requested_framework_version` is that it matches exactly the "version" specified by the `runtimeconfig.json` in its "framework" section:
```
{
  "runtimeOptions": {
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "2.0.0"
    }
  }
}
```
So even if a roll-forward on a framework occurred here to "2.0.1", the directory structure must match the "requested" version ("2.0.0" in this case).

Note that the app and each framework has its own `runtimeconfig.json` setting, which can be different because each defines the framework "name" and "version" for the next lowest framework which don't have to have the same "version".

### 2.0 issues
The primary issue is the use of the `requested_framework_version` folder naming convention:
- Since it does not take into account newer framework versions, any "lightup" extensions must co-release with new framework(s) releases which is especially an issue with frequent patch releases. However, this is somewhat mitigated because most applications in their `runtimeconfig.json` do not target an explicit patch version, and just target `major.minor.0`
- Since it does not take into account older framework versions, a "lightup" extensions should install all previous versions of deps files. Note that since some previous versions may require different assets in the deps.json file, for example every minor release, this issue primarily applies to frequent patch versions.

## 2.1 proposal
In order to prevent having to co-release for roll-forward cases, and deploy all past versions, the followng rules are proposed:
1) Instead of `requested_framework_version`, use `found_framework_version`

Where "found" means the version that is being used at run time including roll-forward. For example, if an app requests `2.1.0` of `Microsoft.NETCore.App` in its runtimeconfig.json, but we actually found and are using `2.2.1` (because there were no "compatible" versions installed from 2.1.0 to 2.2.0), then look for the deps folder `shared/Microsoft.NETCore.App/2.1.1` first.

2) If the `found_framework_version` folder does not exist, find the next closest by going "backwards" in versioning
3) The next closest version only includes a lower minor or major if enabled by "roll-forward-by-no-candidate-fx"

The "roll-forward-by-no-candidate-fx" option has values (0=off, 1=minor, 2=minor\major) and is specified by:
- `%DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX%` environment variable
-	`rollForwardOnNoCandidateFx` in runtimeconfig.json
-	`--roll-forward-on-no-candidate-fx` command line option

where 1 (minor) is the default.

Similar to `applyPatches`, the app may or may not want to tighten or loosen the range of supported frameworks. The default of `minor` seems like a good fit for additional-deps.

4) Similar to roll-forward, a release version will only "roll-backwards" to release versions, unless no release versions are found. Then it will attempt to to find a compatible pre-release version.

Note: the "apply patches" functionality that exists in roll-forward doesn't make sense here since we are going "backwards" and the nearest (most compatible) version already has patches applied.

## 2.1 other issues (not covered here)
Currently the additional-deps feature relies on either the store or "additional probing paths" in order for the assets in the additional deps.json to be found.

When using the store, it uses the "tfm" option specified in the runtimeconfig.json in order to find the appropriate folder in the store. During minor or major "roll-forward" cases, the `tfm` will be referencing the older (original) `tfm`; there is no "roll-forward" on `tfm`. This causes issues if the store assets have been previously deleted.

In addition, the lightup's deps.json contains package references which must be found by exact package version number, so again this causes issues if they were previously deleted (from the store or from the additional probing paths).

## Long-term thoughts
A lightup "extension" could be considered an application, and have its own `runtimeconfig.json` and `deps.json` file next to its corresponding assembly(s). In this way, it would specify the target framework version and thus compatibility with the hosting application could be established. Having an app-to-app dependency in this way is not currently supported.

It could be supported by entending the concept of "multi-layered frameworks" like we have with Microsoft.AspNetCore.App, Microsoft.AspNetCore.All, Microsoft.NETCore.App, where they each have their own runtimeconfig.json and deps.json files.

Adding support for app-to-app dependencies would imply adding a "horizontal" hierarchy, and introducing a "graph reconcilation" phase that would need to be able to collapse several references to the same app or framework when they have different versions.

Similar to additional-deps, the extension apps could "light up" by "additional-apps" via host option or environment variable.
