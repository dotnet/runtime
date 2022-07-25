# Additional-deps

## Summary
This document describes current (2.0) and proposed (2.1) behavior for "light-up" scenarios regarding additional-deps functionality. The proposed behavior resolves the following issues:

https://github.com/dotnet/core-setup/issues/3889

https://github.com/dotnet/core-setup/issues/3884

The `deps.json` file format specifies assets including managed assemblies, resource assemblies and native libraries to load.

Every application has its own `<app>.deps.json` file which is automatically processed. If an application needs additional deps files, typically for "lightup" extensions, it can specify that by:
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

#### Lightup components have to co-release with framework

The primary issue is the use of the `requested_framework_version` folder naming convention:
- Since it does not take into account newer framework versions, any "lightup" extensions must co-release with new framework(s) releases which is especially an issue with frequent patch releases. However, this is somewhat mitigated because most applications in their `runtimeconfig.json` do not target an explicit patch version, and just target `major.minor.0`
- Since it does not take into account older framework versions, a "lightup" extensions should install all previous versions of deps files. Note that since some previous versions may require different assets in the deps.json file, for example every minor release, this issue primarily applies to frequent patch versions.

The proposal for this is to "roll-backwards" starting with the "found" version.

#### Roll-forward uses app's TFM

A secondary issue with with the store's naming convention for framework. It contains a path such as: `\dotnet\store\x64\netcoreapp2.0\microsoft.applicationinsights\2.4.0` where 'netcoreapp2.0' is a "tfm" (target framework moniker). During roll-forward cases, the tfm is still the value specified in the app's runtimeconfig. The host only includes store folders that match that tfm, so it may not find packages from other deps files that were generated off a different tfm. In addition, with the advent of multiple frameworks, it makes it cumbersome to be forced to install to every tfm because multiple frameworks may use the same package, and because each package is still identified by an exact version.

The proposal for this is to add an "any" tfm.

#### Downgrading assemblies

Another issue with additional-deps is that it can "downgrade" assemblies that also exist in the framework. This means that if a file (e.g. assembly) is referenced by both the additional-deps.json files and the framework, the additional-deps file will be selected instead of the framework's file. This can cause issues when the additional-deps.json file is referecing an older version of the file, because that will be loaded and cause run-time issues

The proposal for this is to change the ordering of the processing of additional-deps to make it occur after the framework files, but also support "upgrade" scenarios by allowing newer versions.

#### No opt-out from global deps lightup

Finally, a lower-priority issue is there is no way to turn off the global deps lightup (via `%DOTNET_ADDITIONAL_DEPS%`) for a single application if they run into issues with pulling in the additional deps. If the environment variable is set, and an application can't load because of the additional lightup deps, and the lightup isn't needed, there should be a way to turn it off so the app can load. One (poor) workaround would be to specify `--additional-deps` in the command-line to point to any empty file, but that would only work if the command line can be used in this way to launch the application.

## 2.1 proposal (roll-backwards)
In order to prevent having to co-release for roll-forward cases, and deploy all past versions, the following rules are proposed:
1) Instead of `requested_framework_version`, use `found_framework_version`

Where "found" means the version that is being used at run time including roll-forward. For example, if an app requests `2.1.0` of `Microsoft.NETCore.App` in its runtimeconfig.json, but we actually found and are using `2.2.1` (because there were no "compatible" versions installed from 2.1.0 to 2.2.0), then look for the deps folder `shared/Microsoft.NETCore.App/2.2.1` first.

2) If the `found_framework_version` folder does not exist, find the next closest by going "backwards" in versioning
3) Only go backwards to the closest `patch` version, not `major` or `minor`.

## 2.1 proposal (add an "any" tfm to store)
For example,
    `\dotnet\store\x64\any\microsoft.applicationinsights\2.4.0`

The `any` tfm would be used if the specified tfm (e.g. netcoreapp2.0) is not found: `\dotnet\store\x64\netcoreapp2.0\microsoft.applicationinsights\2.4.0`

_Possible risk: doesn't this make "uninstall" more difficult? Because multiple installs may write the same packages and try to remove packages that another installer created?_

_Possible risk: we should not cross-gen these assemblies as cross-gen varies per release\TFM in incompatible ways._

**Update** we are not going to implement this in 2.1. Every minor release of the framework requires an update by the lightup application.

## 2.1 proposal (change additional-deps processing order to avoid "downgrading")
The current ordering for resolving deps files is:
  1) The app's deps file
  2) The additional-deps file(s)
  3) The framework(s) deps file(s)

The order is important because "first-in" wins. Since the additional-deps is before the framework, the additional-deps will "win" in all cases except during a minor\major roll-forward. The reason minor\major roll-forward is different is because the framework has special logic (new in 2.1) to compare assembly and file version numbers from the deps files, and pick the newest.

The proposed ordering change for 2.1 is:
  1) The app's deps file
  2) The framework(s) deps file(s)
  3) The additional-deps file(s)

In addition, the additional-deps will always look for assembly and file version information present in the deps files in order to support "upgrade" scenarios where the additional-deps brings a newer version of a given assembly. Note that these version checks only occur for managed assemblies, not native files nor resource assemblies.

## 2.1 proposal (add runtimeconfig knob to to disable `%DOTNET_ADDITIONAL_DEPS%`)
<strike>
Add an `additionalDepsLookup` option to the runtimeconfig with these values:

  0) The `%DOTNET_ADDITIONAL_DEPS%` is not used
  1) `DOTNET_ADDITIONAL_DEPS` is used (the default)
</strike>

**Update** this is a nice-to-have feature and is no longer planned for 2.1.

## Long-term thoughts
A lightup "extension" could be considered an application, and have its own `runtimeconfig.json` and `deps.json` file next to its corresponding assembly(s). In this way, it would specify the target framework version and thus compatibility with the hosting application could be established. Having an app-to-app dependency in this way is not currently supported.

It could be supported by entending the concept of "multi-layered frameworks" like we have with Microsoft.AspNetCore.App, Microsoft.AspNetCore.All, Microsoft.NETCore.App, where they each have their own runtimeconfig.json and deps.json files.

Adding support for app-to-app dependencies would imply adding a "horizontal" hierarchy, and introducing a "graph reconciliation" phase that would need to be able to collapse several references to the same app or framework when they have different versions.

Similar to additional-deps, the extension apps could "light up" by (for example) an "additional-apps" host option or environment variable.
