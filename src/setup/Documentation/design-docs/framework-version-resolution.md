# Framework version resolution

This document describes .NET Core 3.0 version resolution behavior when the host resolves framework references for framework dependent apps.
It's just a part of the overall framework resolution scenario described in [multilevel-sharedfx-lookup](multilevel-sharedfx-lookup.md).

## Framework references
Application defines its framework dependencies in its `.runtimeconfig.json` file. Each framework then defines its dependencies in its copy of `.runtimeconfig.json`. Each dependency is expressed as a framework reference. Together these form a graph. The host must resolve the references by finding the actual frameworks which are available on the machine. It must also unify references if there are multi references to the same framework.

Each framework reference consists of these values
* Framework name - for example `Microsoft.NETCore.App`
* Version - for example `3.0.1`
* Roll-forward setting - for example `Minor`

*In the code the framework reference is represented by an instance of [`fx_reference_t`](https://github.com/dotnet/core-setup/blob/master/src/corehost/cli/fx_reference.h).*

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
Each framework reference identifies the framework by its name.  
Framework names are case sensitive (since they're used as folder names even on Linux systems).

#### Version
Framework version must be a [SemVer V2](https://semver.org) valid version.
Versions are compared based on SemVer V2 rules which also define ordering semantics.
Each framework reference must specify a version number, which is used as the minimum version needed.
Version of the first framework reference can be overridden by a command line option `--fx-version` in which case that version is used and its roll-forward is set to `Disable`.

#### Roll-forward
The roll-forward setting specifies how to find a matching framework available on the machine. Design for this setting is described in [Runtime Binding Behavior](https://github.com/dotnet/designs/blob/master/accepted/runtime-binding.md).

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
Before .NET Core 3.0 (so 2.2 and older) the roll-forward behavior for pre-release versions ignored any of the roll-forward related settings, that is both `rollForwardOnNoCandidateFx` as well as `applyPatches`.
The behavior was that:
* Release version will prefer to roll forward to release. If no matching release version is available, release will roll forward to pre-release. (AP = ApplyPatches)

| Framework reference       | Available versions             | Resolved framework | Notes                                             |
| ------------------------- | ------------------------------ | ------------------ | ------------------------------------------------- |
| 2.1.0 Minor, AP=true      | 2.1.1-preview, 2.2.0           | 2.2.0              | 2.2.0 is found first, pre-release is ignored      |
| 2.1.0 Minor, AP=true      | 2.0.0, 2.2.0-preview           | 2.2.0-preview      | No matching release found, so pre-release used    |
| 2.1.0 Major, AP=true      | 3.0.0-preview                  | 3.0.0-preview      | Pre-release is the only one available             |
| 2.1.0 Minor, AP=true      | 2.1.1-preview, 2.1.2-preview   | 2.1.2-preview      | Roll forward to latest patch works on pre-release |
| 2.1.0 Minor, AP=false     | 2.1.1-preview, 2.1.2-preview   | 2.1.1-preview      | `ApplyPatches=false` means no roll forward to latest patch, even on pre-release |
| 2.1.0 Disabled, AP=true   | 2.1.1-preview                  | failure            | For some reason we prevent roll forward on patch only to pre-release |
| 2.1.0 Minor, AP=true      | 2.1.1-preview1, 2.1.1-preview2 | 2.1.1-preview2     | Roll forward to latest patch including latest pre-release |

* Pre-release version will never roll forward to release version. So for example `3.0.0-preview4-27415-15` will not roll forward to `3.0.0`. Also pre-release will only roll forward to the same `major.minor.patch`. So for example `3.0.0-preview4-27415-15` will not roll forward to `3.0.1-preview1-29000-0`. Both `rollForwardOnNoCandidateFx` and `applyPatches` are completely ignored.

| Framework reference       | Available versions             | Resolved framework | Notes                                             |
| ------------------------- | ------------------------------ | ------------------ | ------------------------------------------------- |
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
* Some special cases don't work:
One special case which would not work:  
*Component A which asks for `2.0.0 LatestMajor` is loaded first on a machine which has `3.0.0` and also `3.1.0-preview` installed. Because it's the first in the process it will resolve the runtime according to the above rules - that is prefer release version - and thus will select `3.0.0`.  
Later on component B is loaded which asks for `3.1.0-preview LatestMajor` (for example the one in active development). This load will fail since `3.0.0` is not enough to run this component.  
Loading the components in reverse order (B first and then A) will work since the `3.1.0-preview` runtime will be selected.*

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
1. command line arguments - `rollFoward` and `rollForwardOnNoCandidateFx` (it's invalid to specify both)

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


## Framework resolution
The above described format and handling of settings on framework references will in the end produce a graph where the application is the first node and each dependent framework is a node. Each edge in the graph is a framework reference which has two attributes: the `version` and the `rollForward` setting.
The goal of the framework resolution algorithm is to resolve any potentially conflicting framework references and to find actual available framework on disk which would match the framework references.

### Framework reference conflict resolution
If there are two references to the same framework name, then the host needs to resolve the potential conflict. The rules are:
* Take the higher `version`
* Validate that the reference with the lower `version` allows roll-forward to the higher version. If not, fail.
* Take the more restrictive `rollForward` setting

The check whether the roll-forward is allowed follows the rules described above in the list of available settings for `rollForward`.

To select the more restrictive `rollForward` setting the values are ordered like this:
1. `Disable`
1. `LatestPatch`
1. `Minor`
1. `LatestMinor`
1. `Major`
1. `LatestMajor`

Setting which is higher in this list (lower order number) is more restrictive than the one which is lower. `Disable` is the most restrictive (no roll-forward allowed). `LatestMajor` is the least restrictive (any other setting is compatible with it). `Minor` and `LatestMinor` are more restrictive then `Major` because they don't allow different major version.

For example:
In this example the two framework references are for the same framework name.

| First framework reference | Second framework reference | Resolved framework reference |
| ------------------------- | -------------------------- | ---------------------------- |
| 2.1.0 Minor               | 2.2.0 Major                | 2.2.0 Minor                  |
| 2.1.0 Minor               | 3.0.0 Minor                | failure                      |
| 2.1.0 LatestMajor         | 3.0.0 Minor                | 3.0.0 Minor                  |
| 2.1.0 LatestMajor         | 3.1.2 Disable              | 3.1.2 Disable                |

To maintain backward compatibility, each framework reference will also have to carry `applyPatches` setting. Similar policy will be used to resolve conflicts. The more restrictive setting value will be used. So if one of the two framework references has `applyPatches=false` then the resolved framework reference will also have `applyPatches=false`.

### Algorithm
Terminology
- `config fx references`: framework references for a single `.runtimeconfig.json`, where each reference consists of `name`, `version`, optional `rollForward` and optional `applyPatches`.
- `newest fx references`: framework references keyed off of framework name that contain the highest framework version requested. It is used to perform "soft" roll-forwards to compatible references of the same framework name without reading the disk or performing excessive re-try (Step 7).
- `resolved frameworks`: a list of frameworks that have been resolved, meaning a compatible framework was found on disk.

Steps
1. Determine the `config fx references`:
   * Parse the application's `.runtimeconfig.json` `runtimeOptions.frameworks` section.
   * Insert each framework reference into the `config fx references`.
2. For each framework in `config fx references`:
3. --> If the framework name is not currently in the `newest fx references` list Then add it.
   * By doing this for all references here, before the next loop, we minimize the number of re-try attempts.
4. For each framework reference in `config fx references`:
5. --> If the framework is not in `resolved frameworks` Then resolve the framework reference to the actual framework on disk
   * If the framework `name` already exists in the `newest fx references` resolve the currently processed reference with the one from the `newest fx references` (see above for the algorithm).  
   *Term "soft roll-forward" is used for this in the code*
     * The resolution will always pick the higher `version` and will consolidate the `rollForward` and `applyPatches` settings.
     * The resolution may fail if it's not possible to roll forward from one reference to the other.
     * Update the `newest fx references` with the resolved reference.
   * Probe for the framework on disk  
   *Term "hard roll-forward" is used for this in the code*
     * This follows the roll-forward rules as describe above.
   * If success add it to `resolved frameworks`
     * Parse the `.runtimeconfig.json` of the resolved framework and create a new `config fx references`. Make a recursive call back to Step 2 with this new `config fx references`.
6. --> ElseIf the `version` is < resolved `version` Then perform a "soft roll-forward" to the resolved framework.
   * We may fail here if not compatible.
7. --> Else re-start the algorithm (goto Step 1) with new/clear state except for `newest fx references` so we attempt to use the newer version next time.

This algorithm for resolving the various framework references assumes the **No Downgrading** best practice explained below in order to prevent loading a newer version of a framework than necessary.


### Best practices for a `.runtimeconfig.json`

#### No Restrictive Roll-Forward Overrides
Do not specify `rollForward` (and `applyPatches` and `rollForwardOnNoCandidateFx`) in the `.runtimeconfig.json` unless absolutely necessary. These should be mostly used by the app's `.runtimeconfig.json` and pretty much never in any framework's config.
 * The one exception to this is to use a *less restrictive* setting by using either `LatestMinor`, `Major` or `LatestMajor`.

#### No Redundant References
When a given framework "F1" ships it should not create a case of having more than one reference to the another framework "F2". The reason is that base frameworks already specify "F2" so there is no reason to re-specify it. However, there are potential valid reasons to re-specify the framework:
	* To force a newer version of a given framework which is referenced by lower-level frameworks. However assuming first-party frameworks are coordinated, this reason should not exist for first-party `.runtimeconfig.json` files.
	* To be redundant if there are several "smaller" or "optional" frameworks being used and no guarantee that a base framework will always reference the smaller frameworks over time.

#### No Circular References
There should not be any circular dependencies between frameworks.
  * It is not normally a desirable design for the same reasons why circular references in assemblies and packages are not supported or supported well (chicken-egg creation, simultaneous version changes).
  * One potential future case is to allow "pseudo-circular" dependencies where framework "F1" loads a light-up framework which depends on "F2". Internally the F1->lightup reference may be treated as a late-bound framework reference, thus causing a cycle. This potential feature may replace the "additional deps" feature in a way that allows for richer light-up scenarios by allowing the lightup to specify framework dependency(s) and have a small `.deps.json`.

#### No Downgrading
A newer version of a shared framework should keep or increase the version to another shared framework (never decrease the version number).

By following these best practices we have optimal run-time performance (less processing and probing) and less chance of incompatible framework references.
