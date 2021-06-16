# Framework version resolution

This document describes .NET Core 3.0 version resolution behavior when the host resolves framework references for framework dependent apps.
It's just a part of the overall framework resolution scenario described in [multilevel-sharedfx-lookup](multilevel-sharedfx-lookup.md).

## Framework references
Application defines its framework dependencies in its `.runtimeconfig.json` file. Each framework then defines its dependencies in its copy of `.runtimeconfig.json`. Each dependency is expressed as a framework reference. Together these form a graph. The host must resolve the references by finding the actual frameworks which are available on the machine. It must also unify references if there are multi references to the same framework.

Each framework reference consists of these values
* Framework name - for example `Microsoft.NETCore.App`
* Version - for example `3.0.1`
* Roll-forward setting - for example `Minor`

*In the code the framework reference is represented by an instance of [`fx_reference_t`](https://github.com/dotnet/runtime/blob/main/src/native/corehost/fx_reference.h).*

In the `.runtimeconfig.json` these values are defined like this:
``` json
{
  "runtimeOptions": {
    "tfm": "netcoreapp3.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "3.0.1",
        "rollForward": "Minor"
      }
    ]
  }
}
```

#### Framework name
Each framework reference identifies the framework by its name. Framework names are case sensitive (since they're used as folder names even on Linux systems).

#### Version
Framework version must be a [SemVer V2](https://semver.org) valid version.
Versions are compared based on SemVer V2 rules which also define ordering semantics.
Each framework reference must specify a version number, which is used as the minimum version needed.
Version of the first framework reference can be overridden by a command line option `--fx-version` in which case that version is used and its roll-forward is set to `Disable`.

#### Roll-forward
The roll-forward setting specifies how to find a matching framework available on the machine. Design for this setting is described in [Runtime Binding Behavior](https://github.com/dotnet/designs/blob/master/accepted/2019/runtime-binding.md).

The value is a string (enum really) which is case insensitive.

Available values are:
* `LatestPatch` -- Roll forward to the highest patch version. If specified, this disables minor version roll forward.
* `Minor` (default) -- Roll forward to the lowest higher minor version, if requested minor version is missing. If the requested minor version is present, then the `LatestPatch` policy is used.
* `Major` -- Roll forward to lowest higher major version, and lowest minor version, if requested major version is missing. If the requested major version is present, then the `Minor` policy is used.
* `LatestMinor` -- Roll forward to highest minor version, even if requested minor version is present.
* `LatestMajor` -- Roll forward to highest major and highest minor version, even if requested major is present.
* `Disable` -- Do not roll forward. Only bind to specified version. This policy is not recommended for general use since it disables the ability to roll forward to the latest patches. It is only recommended for testing.

Roll-forward setting can be specified in these places:
1. `.runtimeconfig.json` - `runtimeOptions` property `rollForward` - at this level the setting applies to all framework references in that `.runtimeconfig.json`. For example:
``` json
{
  "runtimeOptions": {
    "tfm": "netcoreapp3.0",
    "rollForward": "major",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "3.0.1"
      }
    ]
  }
}
```

2. `.runtimeconfig.json` - `framework` property `rollForward` - each framework reference can specify its own setting. For example:
``` json
{
  "runtimeOptions": {
    "tfm": "netcoreapp3.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "3.0.1",
        "rollForward": "major"
      }
    ]
  }
}
```

3. Environment variable `DOTNET_ROLL_FORWARD` - For example:
```
set DOTNET_ROLL_FORWARD=LatestMajor
```

4. Command line argument `--roll-forward` - For example:
```
dotnet --roll-forward LatestMinor app.dll
```

The order above defines precedence. Later scopes take precedence over earlier ones. So command line wins over everything, environment variable wins over `.runtimeconfig.json` and per-framework setting wins over config-wide setting.

*Note: There's no inheritance applied when chaining framework references. So for example if the application references FX1, then if FX1 has a reference to FX2, the roll-forward value used to resolved FX2 will be determined solely by looking at the `.runtimeconfig.json` from FX1 and the CLI and env. variables. Any roll-forward settings in the app's `.runtimeconfig.json` will have no effect on the resolution of FX2.*

## Pre-release versions
Pre-release version is a version which has a pre-release part, for example `3.0.0-preview4-27415-15`. Everything after the `-` (dash) character is a pre-release identifier. Per [semantic versioning rules](https://semver.org/) pre-release versions are ordered before the same version without any pre-release part. So `3.0.0-preview4-27415-15` comes before `3.0.0`.

Note that due to the above described ordering, application which refers framework version `3.0.0` will NOT run on `3.0.0-preview`.

### Behavior before 3.0
#### Release
Release version will prefer to roll forward to release. If no matching release version is available, release will roll forward to pre-release. (AP = ApplyPatches)

| Framework reference       | Available versions             | Resolved framework | Notes                                             |
| ------------------------- | ------------------------------ | ------------------ | ------------------------------------------------- |
| 2.1.0 Minor, AP=true      | 2.1.1-preview, 2.2.0           | 2.2.0              | 2.2.0 is found first, pre-release is ignored      |
| 2.1.0 Minor, AP=true      | 2.0.0, 2.2.0-preview           | 2.2.0-preview      | No matching release found, so pre-release used    |
| 2.1.0 Major, AP=true      | 3.0.0-preview                  | 3.0.0-preview      | Pre-release is the only one available             |
| 2.1.0 Minor, AP=true      | 2.1.1-preview, 2.1.2-preview   | 2.1.2-preview      | Roll forward to latest patch works on pre-release |
| 2.1.0 Minor, AP=false     | 2.1.1-preview, 2.1.2-preview   | 2.1.1-preview      | `ApplyPatches=false` means no roll forward to latest patch, even on pre-release |
| 2.1.0 Disabled, AP=true   | 2.1.1-preview                  | failure            | For some reason we prevent roll forward on patch only to pre-release |
| 2.1.0 Minor, AP=true      | 2.1.1-preview1, 2.1.1-preview2 | 2.1.1-preview2     | Roll forward to latest patch including latest pre-release |

#### Pre-release
Pre-release version will never roll forward to release version. So for example `3.0.0-preview4-27415-15` will not roll forward to `3.0.0`. Also pre-release will only roll forward to the same `major.minor.patch`. So for example `3.0.0-preview4-27415-15` will not roll forward to `3.0.1-preview1-29000-0`. Pre-release only rolls forward if exact match is not available (unlike release, which will roll forward on patches by default). Finally pre-release only rolls forward to the closest higher pre-release (similar to release behavior for minor version). Both `rollForwardOnNoCandidateFx` and `applyPatches` are completely ignored for pre-release versions.

| Framework reference       | Available versions             | Resolved framework | Notes                                             |
| ------------------------- | ------------------------------ | ------------------ | ------------------------------------------------- |
| 2.1.0-preview2            | 2.1.0-preview2, 2.1.0-preview3 | 2.1.0-preview2     | Pre-release doesn't roll forward if exact match is available  |
| 2.1.0-preview             | 2.1.0-preview2, 2.1.0-preview3 | 2.1.0-preview2     | Pre-release only rolls forward to closest higher  |
| 2.1.0-preview             | 2.1.0                          | failure            | Pre-release never rolls forward to release        |
| 2.1.0-preview             | 2.1.1-preview                  | failure            | Pre-release never rolls to different `major.minor.patch` |

### Proposed behavior for 3.0 and forward
When resolving framework reference with a **pre-release** version, treat all versions the same and include both release and pre-release versions in the set. This means pre-release can resolve to both release or pre-release.

When resolving framework reference with a **release** version, prefer release versions. This means that if there's a release version on the machine which satisfies all the requirements it will be chosen, regardless of what pre-release versions are installed. Only if there's no suitable release version, then pre-release versions are also considered (and then they're treated all the same).

Interesting examples:
* `3.0.0 rollForward = Minor` would not-roll forward and choose `3.0.0` even if `3.0.1-preview` is also available on the machine. This means the automatic roll forward to latest patch doesn't work for pre-release versions.
* `3.0.0 rollForward = Minor` would roll forward to `3.1.0` even if `3.0.1-preview` is also available on the machine (and technically is closer to the desired version).
* `2.0.0 rollForward = LatestMajor` would roll forward to `3.0.0` even if `3.0.1-preview` is also available on the machine.
* `3.0.0 rollForward = Minor` would roll forward to `3.0.1-preview` if that's the only version available on the machine.

Pros
* Installing pre-release version doesn't affect apps which use release version (unless it's needed to make the app work).
* Doesn't impose any implicit expectations on the quality of pre-release versions as typically pre-release would only be used when explicitly asked for.
* Seems to match most users' expectations.

Cons
* Testing behavior of new releases with pre-release versions is not fully possible (see below).
* Some special cases don't work.

  One special case which would not work:
  *Component A which asks for `2.0.0 LatestMajor` is loaded first on a machine which has `3.0.0` and also `3.1.0-preview` installed. Because it's the first in the process it will resolve the runtime according to the above rules - that is prefer release version - and thus will select `3.0.0`.*

  *Later on component B is loaded which asks for `3.1.0-preview LatestMajor` (for example the one in active development). This load will fail since `3.0.0` is not enough to run this component.*
  *Loading the components in reverse order (B first and then A) will work since the `3.1.0-preview` runtime will be selected.*

Modification to automatic roll forward to latest patch:
Existing behavior is to find a matching framework based on the above rules and then apply roll forward to latest patch (except if `Disable` is specified). The new behavior should be:
* If the above rules find a matching pre-release version of a framework, then automatic roll forward to latest patch is not applied.
* If the above rules find a matching release version of a framework, automatic roll forward to latest patch is applied.

This is done to adapt to .NET Core's usage of pre-release versions. Per semantic versioning the pre-release part of the version is the least significant - less significant than patch version. Without this modification, automatic roll forward to latest patch would mean that the latest available preview would always be selected. .NET Core usage of previews is more akin to major version - each preview release (Preview 1 to Preview 2 for example) can contain changes which are breaking with respect to the previous preview. Automatic roll forward to latest pre-release would also make it hard to test two preview releases side by side on a single machine.

### Proposed new "pre-release" mode
The above behavior makes sense for most users, but it makes it hard for us to test new versions of frameworks. Let's assume .NET Core 3.0 already shipped and there are apps which target `3.0.0 rollForward = Minor` (the default). The shipped framework is version `3.0.0`. Now the next patch release is being prepared and `3.0.1-preview` is produced. With the proposed (and current) behavior, there's no good way to make the apps use the new preview for testing purposes.

The proposal is to add a new environment variable `DOTNET_ROLL_FORWARD_TO_PRERELEASE`. There would only be the environment variable (no command line or `.runtimeconfig.json` property). By default when it's not set or set to anything but `1` the behavior would be as described above.

If the variable is set to `1` the algorithm would always treat pre-release versions the same as release versions. So in the case of a framework reference with a release version, all versions (even pre-release) would be considered always.

This would mean that with `DOTNET_ROLL_FORWARD_TO_PRERELEASE=1`:
* `3.0.0 rollForward = Minor` would roll forward to `3.0.1-preview` even if `3.0.0` is available on the machine. So automatic roll forward to latest patch is used even for pre-release versions.
* `3.0.0 rollForward = Minor` would roll forward to `3.0.1-preview` even if `3.1.0` is also available on the machine. The pre-release version would be chosen because it's closer then the higher minor version.

*With this behavior the special case described above with `LatestMajor` would also work in both orders.*

It's important for this setting to work "on top" of the roll forward settings described above. We need to be able to test pre-release versions without otherwise changing the roll forward policy chosen by the app/environment and without changing the app's configuration assets (files, command line and so on). The other possibilities:
* `.runtimeconfig.json` version of this setting - this would allow per-framework-reference setting. This may come useful when testing third party framework pre-release versions, but right now there's no such scenario yet. Even then the environment variable might be enough to support testing.
* CLI version of this setting - this would allow per-process setting (unlike the env. variable which is inherited by all child processes). This is not needed for the testing scenario described above. Currently we're not aware of another scenario which would require such behavior.

Also of note is that SemVer2 rules still apply, and thus pre-release versions order before the respective release version counterpart. So even if this setting is on, application referring to `3.0.0` will NOT run `3.0.0-preview` regardless of roll forward settings. This is because per versioning rules it would be a downgrade to run on the pre-release and the framework resolution doesn't allow any downgrades.

Pros
* Enables easy testing of future releases using pre-release versions.
* Its name and behavior is intended to be used only when dealing with pre-release versions, so should not cause confusion.

Cons
* It's not the default behavior, so developers working with pre-release versions need to know about it and must use it.


## Interaction with existing settings
In .NET Core 2.2 there are already two settings which affect framework version resolution.

#### Roll forward on no candidate FX
This setting is described in detail in [roll-forward-on-no-candidate-fx](roll-forward-on-no-candidate-fx.md). It can be specified in the same scopes as roll-forward but the precedence is somewhat different (command line over config over environment variable).

To avoid conflicts these rules will be implemented for combinations of setting `rollForward` and `rollForwardOnNoCandidateFx`:
* If both are specified in `.runtimeconfig.json` (counting both per-config and per-framework reference scopes together) then fail.
* If both are specified as command line arguments then fail.
* It's OK to specify both as environment variables.
* It's OK to specify both across different scopes.

The host will use the `rollForward` setting to determine framework reference resolution behavior. It will convert the `rollForwardOnNoCandidateFx` values into the value of `rollForward` according to this mapping:
* `0` -> `LatestPatch`
* `1` -> `Minor`
* `2` -> `Major`

The behavior of these settings are exactly the same, so switching to using `rollForward` internally will maintain 100% backward compatibility.

To reconcile the various scopes the host will apply the following precedence:
1. environment variable `DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX`
1. `.runtimeconfig.json` global setting - `rollForward` and `rollForwardOnNoCandidateFx` (it's invalid to specify both)
1. `.runtimeconfig.json` per-framework setting - `rollForward` and `rollForwardOnNoCandidateFx` (it's invalid to specify both)
1. environment variable `DOTNET_ROLL_FORWARD`
1. command line arguments - `rollForward` and `rollForwardOnNoCandidateFx` (it's invalid to specify both)

Items lower in the list override those higher in the list. At each precedence scope the host will determine an effective value of `rollForward` by converting any potential `rollForwardOnNoCandidateFx` setting to `rollForward`. *Note that there are never collisions between `rollForward` and `rollForwardOnNoCandidateFx` since both can't appear at the same level.*

#### Apply patches
This setting is also described in [roll-forward-on-no-candidate-fx](roll-forward-on-no-candidate-fx.md). It can be specified as a property either for the entire `.runtimeconfig.json` or per framework reference (it has no environment variable of command line argument). It disables rolling forward to the latest patch.

The host will compute effective value of `applyPatches` for each framework reference.
The `applyPatches` value is only considered if the effective `rollForward` value for a given framework reference is
* `LatestPatch`
* `Minor`
* `Major`

For the other values `applyPatches` is ignored.
*This is to maintain backward compatibility with `rollForwardOnNoCandidateFx`. `applyPatches` is now considered obsolete.*

If `applyPatches` is set to `true` (the default), then the roll-forward rules described above apply fully.
If `applyPatches` is set to `false` then for effective roll-forward setting:
* `LatestPatch` - no roll forward will happen - only exact version match is accepted.
* `Minor` - if the exact `major.minor` is found, then the equal or lowest higher patch version is used. Otherwise the lowest higher minor is found and the lowest patch is used.
* `Major` - if the exact major is found, the rules for `Minor` above are followed. Otherwise the lowest higher `major.minor.patch` is selected (so lowest patch available for a given major.minor).
* Any other value of roll-forward - the `applyPatches` is ignored - the `rollForward` setting then effectively overrides the `applyPatches` setting.

It is illegal to specify both `applyPatches` and `rollForward` in `.runtimeconfig.json` (counting both per-config and per-framework reference scopes together). It is OK to specify `applyPatches` in `.runtimeconfig.json` and `rollForward` through either CLI or env. variable.

In addition to the above any framework reference with a pre-release version will allow roll forward over pre-release (so same `major.minor.patch` but different pre-release part) even if `applyPatches=false`.
*This is to maintain backward compatibility. In 2.\* pre-release never rolled forward to a different `major.minor.patch` and completely ignored `applyPatches`. Starting to honor `applyPatches` would introduce potentially breaking behavior in some corner cases where the resolution might fail when previously it didn't.*

## Framework resolution
The above described format and handling of settings on framework references will in the end produce a graph where the application is a node and each dependent framework is also a node. Each edge in the graph is a framework reference which has these attributes:
* `version` - the minimum version required for the framework
* `version_compatibility_range` - specifies the compatibility range for the framework, this can have values
  * `exact` - only exact match is allowed
  * `patch` - any higher version with the same `major.minor` is allowed
  * `minor` - any higher version with the same `major` is allowed
  * `major` - any higher version is allowed
* `roll_to_highest_version` - specifies how the exact version within the allowed `version_compatibility_range` is selected
  * `false` - select the closest higher available version
  * `true` - select the highest available version

Note that roll forward on all `version_compatibility_range` values except the `exact` will always pick the latest available `patch` version. So `roll_to_highest_version` is ignored for `patch` versions (it's effectively implied to be `true` in that case). One caveat: to maintain backward compatibility with `rollForwardOnNoCandidateFx` and `applyPatches`, the `patch` version range will not roll forward to latest patch if `applyPatches=false`.

The goal of the framework resolution algorithm is to resolve any potentially conflicting framework references and to find the available framework on disk which would satisfy the framework references.

There's a direct mapping from the `rollForward` setting to the internal representation of the framework references:

| `rollForward`         | `version_compatibility_range` | `roll_to_highest_version`                  |
| --------------------- | ----------------------------- | ------------------------------------------ |
| `Disable`             | `exact`                       | `false`                                    |
| `LatestPatch`         | `patch`                       | `false` (always picks latest patch anyway) |
| `Minor`               | `minor`                       | `false`                                    |
| `LatestMinor`         | `minor`                       | `true`                                     |
| `Major`               | `major`                       | `false`                                    |
| `LatestMajor`         | `major`                       | `true`                                     |

### Framework reference conflict resolution
If there are two references to the same framework name, then the host needs to resolve the potential conflict. The rules are:
* Take the higher `version`
* Validate that the reference with the lower `version` allows roll-forward to the higher version. If not, fail.
* Take the more restrictive `version_compatibility_range` from the two
* If `roll_to_highest_version` is true on one of the framework references, apply the `true` value to the merged framework reference as well.

The check for whether the roll-forward is allowed follows the rules described above in the list of available settings for `rollForward`.

For example:
In this example the two framework references are for the same framework name.

| First framework reference                      | Second framework reference | Resolved framework reference               |
| ---------------------------------------------- | -------------------------- | ------------------------------------------ |
| `2.1.0 minor`                                  | `2.2.0 major`              | `2.2.0 minor`                              |
| `2.1.0 minor`                                  | `3.0.0 minor`              | failure                                    |
| `2.1.0 major roll_to_highest_version=true`     | `3.0.0 minor`              | `3.0.0 minor roll_to_highest_version=true` |
| `2.1.0 major roll_to_highest_version=true`     | `3.1.2 exact`              | `3.1.2 exact roll_to_highest_version=true` |

To maintain backward compatibility, each framework reference also carries `applyPatches` setting. In case of two references the more restrictive setting value is used. So if one of the two framework references has `applyPatches=false` then the resolved framework reference also has `applyPatches=false`.

The `roll_to_highest_version` flag is propagated into the referenced frameworks. So if the app has a reference like `Microsoft.AspNet.App 3.0.0 minor highest` then all references from the `Microsoft.AspNet.App` framework will have the `highest` flag applied to them as well (regardless of the settings in the framework).

### Algorithm
Terminology
- `framework reference`: consists of framework `name`, `version`, `rollForward` and optionally `applyPatches`.
- `config fx references`: `framework references` for a single `.runtimeconfig.json`.
- `effective fx references`: dictionary of `framework references` keyed off of framework `name` that contains the highest `version` requested and merged `rollForward` and `applyPatches`. It is used to track the most up to date effective framework reference without reading the disk, it prevents excessive re-tries of the resolution.
- `resolved frameworks`: a list of frameworks that have been resolved, meaning a compatible framework was found on disk.

Steps
1. Determine the `config fx references`:
   * Parse the application's `.runtimeconfig.json` `runtimeOptions.frameworks` section.
   * Insert each `framework reference` into the `config fx references`.
2. For each `framework reference` in `config fx references`:
   * Apply the recursively passed value of `roll_to_highest_version` to the `framework reference`.
   * Then apply the below steps:
3. --> If the framework `name` is not currently in the `effective fx references` list Then add it.
   * By doing this for all `framework references` here, before the next loop, we minimize the number of re-try attempts.
4. For each `framework reference` in `config fx references`:
5. --> If the framework's `name` is not in `resolved frameworks` Then resolve the `framework reference` to the actual framework on disk:
   * If the framework `name` already exists in the `effective fx references` reconcile the currently processed `framework reference` with the one from the `effective fx references` (see above for the algorithm).
   *Term "reconcile framework references" is used for this in the code, this used to be called "soft-roll-forward" as well.*
     * The reconciliation will always pick the higher `version` and will merge the `rollForward` and `applyPatches` settings.
     * The reconciliation may fail if it's not possible to roll forward from one `framework reference` to the other.
     * Update the `effective fx references` with the reconciled `framework reference` (note that this may be a combination of version and settings from the two `framework references` being considered).
   * Resolve the `framework reference` (which by now is the one from `effective fx references`) against the frameworks available on the disk
   *Sometimes this is referred to as "hard-roll-forward".*
     * This follows the roll-forward framework selection rules as describe above.
   * If success add it to `resolved frameworks`
     * Parse the `.runtimeconfig.json` of the resolved framework and create a new `config fx references`. Make a recursive call back to Step 2 with these new `config fx references`. Pass in the value of the `roll_to_highest_version` from the `framework reference` used to resolve the framework.
     * Continue with the next `framework reference` (Step 4).
6. --> Else perform reconcile the `framework reference` with the one from `effective fx references`.
   * We may fail here if not compatible.
   * If the reconciliation results in a different `framework reference` than the one in `effective fx references`
     * Update the `framework reference` in `effective fx references`
     * Re-start the algorithm (goto Step 1) with new/clear state except for `effective fx references` so we attempt to use the newer `framework reference` next time.
   * Else (no need to change the `effective fx references`) - use the already resolved framework and continue with the next `framework reference` (Step 4).

Notes on this algorithm:
* This algorithm for resolving the various framework references assumes the **No Downgrading** best practice explained below in order to prevent loading a newer version of a framework than necessary.
* Probing for the framework on disk never changes the `effective fx references`. This means that the `effective fx references` contains the latest effective framework reference for each framework without considering what frameworks are actually available. This is very important to avoid ordering issues. (See the **Fixing ordering issues** in the sections below.)


### Best practices for a `.runtimeconfig.json`

#### No Redundant References
When a given framework "F1" ships it should not create a case of having more than one reference to the another framework "F2". The reason is that base frameworks already specify "F2" so there is no reason to re-specify it. However, there are potential valid reasons to re-specify the framework:
	* To force a newer version of a given framework which is referenced by lower-level frameworks. However assuming first-party frameworks are coordinated, this reason should not exist for first-party `.runtimeconfig.json` files.
	* To be redundant if there are several "smaller" or "optional" frameworks being used and no guarantee that a base framework will always reference the smaller frameworks over time.
For first-party frameworks, this means that the app should only specify the reference to the highest-level framework. For example, the app should reference `Microsoft.AspNet.App` but should not then also specify a reference to `Microsoft.NETCore.App` as that is already implied by the higher level framework.

#### No Circular References
There should not be any circular dependencies between frameworks.
  * It is not normally a desirable design for the same reasons why circular references in assemblies and packages are not supported or supported well (chicken-egg creation, simultaneous version changes).
  * One potential future case is to allow "pseudo-circular" dependencies where framework "F1" loads a light-up framework which depends on "F2". Internally the F1->lightup reference may be treated as a late-bound framework reference, thus causing a cycle. This potential feature may replace the "additional deps" feature in a way that allows for richer light-up scenarios by allowing the lightup to specify framework dependency(s) and have a small `.deps.json`.

#### No Downgrading
A newer version of a shared framework should keep or increase the version to another shared framework (never decrease the version number).

By following these best practices we have optimal run-time performance (less processing and probing) and less chance of incompatible framework references.


### Scenarios with known issues
#### `LatestMajor` usage with multiple frameworks
For example an app which has runtime config like this:
```
ASP.NET 3.0 rollForward=LatestMajor
ThirdPartyFX 1.0 rollForward=LatestMajor
```

And now assume that both `ASP.NET 3.0` and `ThirdPartyFX 1.0` have references to `Microsoft.NETCore.App 3.0`. And then `ASP.NET 4.0` is released which has a reference to `Microsoft.NETCore.App 4.0`. The above application will break now, since it will pick `ASP.NET 4.0` and thus request `Microsoft.NETCore.App 4.0` but that reference is not compatible with the `Microsoft.NETCoreApp 3.0` reference from `ThirdPartyFX 1.0`.

Right now we don't support 3rd party frameworks and all 1st party framework should ship in sync, so such a situation should not arise. At the same time it will be relatively uncommon to have apps with multiple framework references.

This might be more of an issue for components (COM and such), which we will recommend to use at least `LatestMinor` if not `LatestMajor`. But again it would only happen if the component has references to more than on framework.



## Changes to existing apps
The above proposal will impact behavior of existing apps (because framework resolution is in `hostfxr` which is global on the machine for all frameworks). This is a description of the changes as they apply to apps using either default settings, `rollForwardOnNoCandidateFx` or `applyPatches`.

### Fixing ordering issues
In 2.* the algorithm had a bug in it which caused it to resolve different version depending solely on the order of framework references. Consider this example:

`Microsoft.NETCore.App` is available on the machine with versions `2.1.1` and `2.1.2`.

```
Application
 -> Microsoft.NETCore.App 2.1.0 rollForwardOnNoCandidateFx=0, applyPatches=false
 -> ASP.NET 2.1.0
ASP.NET (2.1.0)
 -> Microsoft.NETCore.App 2.1.0 rollForwardOnNoCandidateFx=0
```

This would resolve `Microsoft.NETCore.App 2.1.1` because the reference from the app with `applyPatches=false` is hard resolved first and the reference from `ASP.NET` can soft roll forward to it.

Now simply change the order of framework reference in the app

```
Application
 -> ASP.NET 2.1.0
 -> Microsoft.NETCore.App 2.1.0 rollForwardOnNoCandidateFx=0, applyPatches=false
ASP.NET (2.1.0)
 -> Microsoft.NETCore.App 2.1.0 rollForwardOnNoCandidateFx=0
```

This one would resolve `Microsoft.NETCore.App 2.1.2` because the reference in `ASP.NET` is hard resolved first and the one in the app can soft roll forward to it.
In 2.* this is not a serious problem since `rollForwardOnNoCandidateFx=0` is used very rarely and more importantly none of the built in frameworks will specify it.

In 3.0 with the addition of `LatestMinor` (and `LatestMajor`) this problem can become a real issue. Consider this example:

`Microsoft.NETCore.App` is available on the machine with versions `3.1.1` and `3.2.0`.

```
Application
 -> Microsoft.NETCore.App 3.1.0 rollForward=LatestMinor
 -> ASP.NET 3.1.0
ASP.NET (3.1.0)
 -> Microsoft.NETCore.App 3.1.0 <default> (i.e. rollForward=Minor)
```

This would resolve `Microsoft.NETCore.App 3.2.0` because the reference from the app with `LatestMinor` is hard resolved first and the reference from `ASP.NET` can soft roll forward to it.

Now simply change the order of framework reference in the app

```
Application
 -> ASP.NET 3.1.0
 -> Microsoft.NETCore.App 3.1.0 rollForward=LatestMinor
ASP.NET (3.1.0)
 -> Microsoft.NETCore.App 3.1.0 <default> (i.e. rollForward=Minor)
```

This one would resolve `Microsoft.NETCore.App 3.1.1` because the reference in `ASP.NET` is hard resolved first (`Minor` will pick the closest available minor version) and the one in the app can soft roll forward to it.

Since the reference in standard framework (`ASP.NET` in this sample) has no settings (defaults) this can be very common. Specifically for COM or other components where we will recommend usage of `LatestMinor` or `LatestMajor` for best compatibility.
Note that with `LatestMajor` the problem is even worse because depending on the order and available framework the references may actually fail to resolve with the old algorithm.

The fixed algorithm doesn't consider the actual hard resolved framework version when computing the effective framework reference. See the algorithm description above. The outcome is that it will effectively always compute the full effective framework reference before hard resolving it and thus is not affected by ordering. The downside is that it may need to retry more often. To avoid unnecessary retries the best practices should be followed, specifically the one about not specifying unnecessary framework references. In the above sample, the app doesn't need to specify a framework reference for `Microsoft.NETCore.App` and so it should not do that.

### Roll on patches-only will now roll from release to pre-release if no release is available
When `rollForwardOnNoCandidateFx` is disabled (set to `0` which is not the default) the existing behavior is to never roll forward to a pre-release version. If the setting is any other value (Minor/Major) it would roll forward to pre-release version if there's no available matching release version.

For example, if the machine has only `3.0.1-preview.1` installed and the application has a reference to `3.0.0`, the existing behavior is:
* Default behavior is to roll forward to the `3.0.1-preview.1` since there's no matching release version, and run the app.
* If `rollForwardOnNoCandidateFx=0` (and only then), the app will fail to run (as it won't roll forward to pre-release version).

The new behavior will be to treat all settings of `rollForwardOnNoCandidateFx` the same with regard to pre-release. That is release version will roll forward to pre-release if there's no release version available. In the above sample, the app would run using the `3.0.1-preview.1` framework.

| Reference                                     | Available versions | Existing behavior | New behavior      |
| --------------------------------------------- | ------------------ | ----------------- | ----------------- |
| 3.0.0                                         | 3.0.1-preview.1    | 3.0.1-preview.1   | 3.0.1-preview.1   |
| 3.0.0 rollForwardOnNoCandidateFx=0            | 3.0.1-preview.1    | failure           | 3.0.1-preview.1   |


### Pre-release will roll forward
The existing behavior is that pre-release only rolls forward to the closest higher pre-release of the same `major.minor.patch`. This also means that if there's an exact match available, pre-release doesn't roll forward. Pre-release also never rolls forward to release.

With the proposed behavior pre-release will be allowed to roll forward to release. If the algorithm looking for closest match finds a pre-release version, it will not apply automatic roll to latest patch.
For backward compatibility reasons `applyPatches=false` will still allow roll forward over pre-release.

| Reference                                     | Available versions                                | Existing behavior | New behavior      | Notes   |
| --------------------------------------------- | ------------------------------------------------- | ----------------- | ----------------- | ------- |
| 2.1.0-preview.2                               | 2.1.0-preview.2, 2.1.0-preview.3, 2.1.1-preview.1 | 2.1.0-preview.2   | 2.1.0-preview.2   | Exact match available, don't roll to latest patch if it's a pre-release |
| 2.1.0-preview.1                               | 2.1.0-preview.2, 2.1.0-preview.3                  | 2.1.0-preview.2   | 2.1.0-preview.2   | Only roll to closest pre-release |
| 2.1.0-preview.1 `rollForwardOnNoCandidateFx=0, applyPatches=false` | 2.1.0-preview.2, 2.1.0-preview.3 | 2.1.0-preview.2   | 2.1.0-preview.2 | Pre-release can still roll on pre-release even if `applyPatches=false` but only to the closest. |
| 2.1.0-preview.1                               | 2.1.0                                             | failure           | 2.1.0             | Pre-release will roll forward to release |
| 2.1.0-preview.1                               | 2.1.1-preview.1                                   | failure           | 2.1.1-preview.1   | Pre-release will roll forward on patches |
| 2.1.0-preview.1                               | 2.2.0-preview.1                                   | failure           | 2.2.0-preview.1   | Pre-release will roll forward on minor by default |
| 2.1.0-preview.1 `rollForwardOnNoCandidateFx=2` | 3.0.0                                            | failure           | 3.0.0             | Pre-release will roll forward on major if enabled |
