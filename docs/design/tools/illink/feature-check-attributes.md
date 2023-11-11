# Feature switch attributes

.NET has [feature switches](https://github.com/dotnet/designs/blob/main/accepted/2020/feature-switches.md) which can be set to turn on/off areas of functionality in our libraries, with optional support for removing unused features when trimming or native AOT compiling. What we describe overall as "feature switches" have many pieces which fit together to enable this:

- MSBuild property
- RuntimHostConfigurationOption MSBuild ItemGroup
- `runtimeconfig.json` setting
- AppContext feature setting
- **ILLink.Substitutions.xml**
- **static boolean property**
- **Requries attributes**

The bold pieces are the focus of this document. [Feature switches](https://github.com/dotnet/designs/blob/main/accepted/2020/feature-switches.md) describes how settings flow from the MSBuild property through the AppContext (for runtime feature checks) or `ILLink.Substitutions.xml` (for feature settings baked-in when trimming). This document aims to describe an attribute-based model to replace some of the functionality currently implemented via ILLink.Substitutions.xml, used for branch elimination in ILLink and ILCompiler to remove branches that call into `Requires`-annotated code when trimming.

## Background

### Terminology

We'll use the following terms to describe specific bits of functionality related to feature switches:
- Feature switch property: the IL property whose value indicates whether a feature is enabled/supported
  - For example: `RuntimeFeature.IsDynamicCodeSupported`
- Feature switch name: the string that identifies a feature in `RuntimeHostConfigurationOption` and AppContext
  - For example: `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"`
- Feature attribute: an attribute associated with a feature, used to annotate code that is directly related to the feature.
  - For example: `RequiresDynamicCodeAttribute`
- Feature guard property: an IL property whose value _depends_ on a feature being enabled, but isn't necessarily _defined_ by the availability of that feature.
  - For example: `RuntimeFeature.IsDynamicCodeCompiled` depends on `IsDynamicCodeSupported`. It should return `false` when `IsDynamicCodeSupported` returns `false`, but it may return `false` even if `IsDynamicCodeSupported` is `true`.

We'll say that a "feature switch property" is also necessarily a "feature guard property" for its defining feature, but not all "feature guard properties" are "feature switch properties".

A "feature switch property" may also be a "feature guard property" for a feature _other than_ the defining feature. For example, we introduced the feature switch property `StartupHookProvider.IsSupported`, which is defined by the feature switch named `"System.StartupHookProvider.IsSupported"`, but additionally serves as a guard for code in the implementation that has `RequiresUnreferencedCodeAttribute`. Startup hook support is disabled whenever we trim out unreferenced code, but may alse be disabled independently by the feature switch.

Similarly, one could imagine introducing a feature switch for `IsDynamicCodeCompiled`. 


`IsDynamicCodeSupported` is an example of a feature that has an attribute, a feature switch, and a separate feature guard (`IsDynamicCodeCompiled`), but not all features have all of these bits of functionality.

### Warning behavior

Typically, feature switches come with support (via XML substitutions) for treating feature properties and feature guards as constants when publishing with ILLink/ILCompiler. These tools will eliminate guarded branches. This is useful as a code size optimization, and also as a way to prevent producing warnings for features that have attributes designed to produce warnings at callsites:

```csharp
UseDynamicCode(); // warns

if (RuntimeFeature.IsDynamicCodeSupported)
  UseDynamicCode(); // OK, no warning

if (RuntimeFeature.IsDynamicCodeCompiled)
  UseDynamicCode(); // OK, no warning in ILCompiler, but warns in analyzer

[RequiresDynamicCode("Uses dynamically generated code")]
static void UseDynamicCode() { }
```

The ILLink Roslyn analyzer has built-in support for treating `IsDynamicCodeSupported` as a guard for `RequiresDynamicCodeAttribute`, but has no other built-in support.

## Goals

- Teach the ILLink Roslyn analyzer to treat `IsDynamicCodeCompiled` as a guard for `RequiresDynamicCodeAttribute`
- Allow libraries to define their own feature guard properties

  Libraries should be able to introduce their own properties that can act as guards for `RequiresDynamicCodeAttribute`, or for other features that might produce warnings in the analyzer

- Define an attribute-based model for such feature guards

- Take into account how this would interact with an attribute-based model for feature switches

  We don't necessarily aim to introduce a model for feature switches in this proposal, but we will at least explore the potential API shape to ensure that our model for feature guards would work well if we did.

### Non-goals

- Support branch elimination in the analyzer for all feature switches

  The most important use case for the analyzer is analyzing libraries. Libraries typically don't bake in constants for feature switches, so the analyzer needs to consider all branches. It should support feature guards for features that produce warnings, but doesn't need to consider feature settings passed in from the project file to treat some branches as dead code.

- Teach the ILLink Roslyn Analyzer about the substitution XML

  We don't want to teach the analyzer to read the substitution XML. The analyzer is the first interaction that users typically have with trimming and AOT warnings. This should not be burdened by the XML format. Even if we did teach the analyzer about the XML, it would not solve the problem because the analyzer must not globally assume that `IsDynamicCodeSupported` is false as ILCompiler does.

- Define a model with the full richness of the supported OS platform attributes

  We will focus initially on a model where feature switches are booleans that return `true` if a feature is enabled. We aren't considering supporting version checks, or feature switches of the opposite polarity (where `true` means a feature is disabled/unsupported). We will consider what this might look like just enough to gain confcidence that our model could be extended to support these cases in the future, but won't design this fully in the first iteration.


## Feature guard attribute

In order to treat a property as a guard for a feature that has a `Requires` attribute, there must be a semantic tie between the guard property and the attribute. ILLink and ILCompiler don't have this requirement because they run on apps, not libraries, so the desired warning behavior just falls out, thanks to branch elimination and the fact that `IsDynamicCodeSupported` is set to false from MSBuild.


We could allow placing `FeatureGuardAttribute` on the property to indicate that it should act as a guard for a particular feature. The attribute instance needs to reference the feature somehow, whether as:

- a reference to the feature attribute:

  ```csharp
  class Feature {
      [FeatureGuard<RequiresDynamicCodeAttribute>]
      public static bool IsSupported => RuntimeFeature.IsDynamicCodeSupported;
  }
  ```

  This tells the analyzer enough that it can treat this as a guard without any extra information.

  The analyzer wouldn't know about the relationship between this check and `RuntimeFeature.IsDynamicCodeSupported` or `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"`, but encoding this relationship isn't strictly necessary if we are only interested in representing feature _guards_ via attributes.

  On its own this is not enough for ILLink/ILCompiler to do branch elimination, because there's no tie to the feature switch name.

- a reference to the feature name string:

  ```csharp
  class Feature {
      [FeatureGuard("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported")]
      public static bool IsSupported => RuntimeFeature.IsDynamicCodeSupported;
  }
  ```

  The analyzer would need to hard-code the fact that this string corresponds to `RequiresDynamicCodeAttribute`, unless we model this relationship via attributes that represent feature switches.

  This would be sufficient for ILLink/ILCompiler to treat `IsSupported` as a constant based on the feature switch name.

- a reference to the existing feature check property:

  ```csharp
  class Feature {
      [FeatureGuard(typeof(RuntimeFeature), nameof(RuntimeFeature.IsDynamicCodeSupported))]
      public static bool IsSupported => RuntimeFeature.IsDynamicCodeSupported;
  }
  ```

  The analyzer would need to hard-code the fact that `RuntimeFeature.IsDynamicCodeSupported` corresponds to `RequiresDynamicCodeAttribute`, unless we model this relationship via attributes that represent feature switches.

  This would be sufficient for ILLink/ILCompiler to treat `IsSupported` as a constant based on the feature switch name, assuming it has existing knowledge of the fact that `RuntimeFeature.IsDynamicCodeSupported` is contrelled by the feature switch named `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"`, either from a substitution XML or from a separate attribute that encodes this.


The intention with any of these approaches is for the guard to prevent analyzer warnings:

```csharp
if (Feature.IsSupported) {
    APIWhichRequiresDynamicCode(); // No warnings
}

[RequiresDynamicCode("Does something with dynamic codegen")]
static void APIWhichRequiresDynamicCode() {
    // ...
}
```

### Danger of incorrect usage

Since a feature guard silences warnings from the analyzer, there is some danger that `FeatureGuardAttribute` will be used carelessly as a way to silence warnings, even when the definition of the `IsSupported` property doesn't have any tie to the existing feature. For example:

```csharp
class Feature {
    [FeatureGuard<RequiresDynamicCodeAttribute>]
    public static bool SilenceWarnings => true; // BAD
}
```

```csharp
if (Feature.SilenceWarnings) {
    APIWhichRequiresDynamicCode(); // No warnings
}
```

This will silence warnings from the analyzer without any indication that `RequiresDynamicCode`-annotated code might be reached at runtime. ILLink and ILCompiler would still warn in this example, so we do have some guard rails for this.

However, if there is a substitution XML that sets `SilenceWarnings` to `false` whenever `IsDynamicCodeSupported` is `false`, then ILLink and ILCompiler will remove this branch, and it will silently behave differently in "dotnet run" (no trimming/AOT) than in a published app. The published app would have the branch removed, while "dotnet run" would execute the call, without any `RequiresDynamicCode` warnings.

If there is no substitution XML which sets `SilenceWarnings` to `false` when `IsDynamicCodeSupported` is `false`, the feature check is a no-op that just silences the warning from the analyzer, but will still produce warnings when publishing.


We should be extremely clear in our documentation that this is not the intended use.

### Validating correctness of feature guards

Better would be for the analyzer to validate that the implementation of a feature check includes a check for `IsDynamicCodeSupported`. This could be done using the analyzer infrastructure we have in place without much cost for simple cases:

```csharp
class Feature {
    [FeatureGuard<RequiresDynamicCodeAttribute>]
    public static bool IsSupported => RuntimeFeature.IsDynamicCodeSupported && SomeOtherCondition(); // OK
}
```

```csharp
class Feature {
    [FeatureGuard<RequiresDynamicCodeAttribute>]
    public static bool IsSupported => SomeOtherCondition(); // warning
}
```

Note that this analysis does require the analyzer to understand the relationship between `RequiresDynamicCodeAttribute` and `RuntimeFeature.IsDynamicCodeSupported`, so it would still require either hard-coding this relationship in the analyzer, or representing it via an attribute model for feature switches.

There may be more complex implementations of feature guards that the analyzer would not support. In this case, the analyzer would produce a warning on the property definition that can be silenced if the author is confident that the return value of the property reflects the referenced feature.

## Feature switch attributes

Allow placing `FeatureSwitchAttribute` on the property to indicate that it should be treated as a constant if the feature setting is passed to ILLink/ILCompiler at publish time. The Roslyn analyzer could also respect it by not analyzing branches that are unreachable with the feature setting, though we don't have an immediate use case for this. It could be useful when analyzing application code, but the analyzer is most important for libraries, where feature switches are usually not set.

The attribute needs to reference the feature somehow, whether as:

- a reference to the feature name string:

  ```csharp
  class RuntimeFeature {
      [FeatureSwitch("RuntimeFeature.IsDynamicCodeSupported")]
      public static bool IsDynamicCodeSupported => AppContext.TryGetSwitch("RuntimeFeature.IsDynamicCodeSupported", out bool isEnabled) ? isEnabled : false;
  }
  ```

  Since we set `"RuntimeFeature.IsDynamicCodeSupported"` to `false` when running ILCompiler, this is enough for ILCompiler to use it for branch elimination and avoid warning for guarded calls to `RequiresDynamicCodeAttribute`. ILLink would behave similarly if there were a feature switch for `RequiresUnreferencedCodeAttribute`.

  The analyzer would still need to separately encode the fact that `"RuntimeFeature.IsDynamicCodeSupported"` corresponds to `RequiresDynamicCodeAttribute`.

- a reference to the feature attribute, for those feature switches that are associated with attributes:

  ```csharp
  class RuntimeFeature {
      [FeatureSwitch<RequiresDynamicCodeAttribute>]
      public static bool IsDynamicCodeSupported => AppContext.TryGetSwitch("RuntimteFeature.IsDynamicCodeSupported", out bool isEnabled) ? isEnabled : true;
  }
  ```

  In this case, ILCompiler would need to hard-code the fact that `RequiresDynamicCodeAttribute` corresponds to `"RuntimeFeature.IsDynamicCodeSupported"`, and use this knowledge to treat the property as returning a constant.

  However, the Roslyn analyzer would have enough information from this attribute alone.

In either case, the feature switch property would be usable as a guard for calls to `RequiresDynamicCode` APIs:

```csharp
if (RuntimeFeature.IsDynamicCodeSupported) {
    APIWhichRequiresDynamicCode(); // No warnings
}

[RequiresDynamicCode("Does something with dynamic codegen")]
static void APIWhichRequiresDynamicCode() {
    // ...
}
```

## Relationship between feature switches and feature guards

### Feature switches are also guards
A feature switch is also a feature guard for the feature it is defined by. We could encode this in one of two ways:
- `FeatureSwitch` only, with a unified representation that includes the mapping to both the attribute and feature name, or
- Require both `FeatureSwitch` and `FeatureGuard`, where both attributes together provide a mapping to the attribute and the feature name

### Feature guards _may_ also be feature switches

A feature guard might also come with its own independent feature switch. We shoud be careful to avoid violating the assumptions of the feature guard by controlling the property via a feature switch.

For example, if we had a separate feature switch for `IsDynamicCodeCompiled`, this would allow setting `IsDynamicCodeCompiled` to `true` even when `IsDynamicCodeSupported` is `false`. This is essentially what we did for features like `StartupHookSupport`: this feature switch can be set even in trimmed apps.

We rely on trim warnings to alert the app author to the problem, and also default `StartupHookSupport` to `false` for trimmed apps. If we have an attribute-based model for feature guards, we may want to consider inferring these defaults from the `FeatureGuard` instead (so a guard for `RequiresUnreferencedCode` that is also a feature switch would be `false` by default whenever "unreferenced code" is unavailable).

The proposal for now is not to infer any defaults and just take care to set appropriate defaults in the SDK. This means that custom feature guards that are also feature switches will need to do the same. For example, a library that defines a feature switch which guards `RequiresUnreferencedCode` will need to ship with MSBuild targets that disable the feature by default for trimmed apps.

### Referencing features from `FeatureGuard` and `FeatureSwitch`

We saw a few cases that required a link between feature guards and the functionality related to feature switches:
- To support feature guards in the analyzer, there must be a tie to the guarded `Requires` attribute
- To support eliminating branches guarded by feature guards in ILLink/ILCompiler, there must be a tie to the name of the feature setting.
- To support detecting incorrect implementations of the feature guard property in the analyzer, there must be a tie to the feature switch property of the guarded feature.

| How `FeatureGuard` references the guarded feature | Analyzer | ILLink/ILCompiler |
| - | - | - |
| Attribute | OK; needs mapping to feature name/property for validation | needs mapping to feature name |
| Feature switch name | needs mapping to attribute | OK |
| Feature switch property | needs mapping to attribute | needs mapping to feature name |

| How `FeatureSwitch` references the defining feature | Analyzer | ILLink/ILCompiler |
| - | - | - |
| Feature switch name | needs mapping to attribute | OK |
| Feature attribute | OK | needs mapping to feature name |


It seems natural to define a model where all three of these represent the same concept.

## Unified view of features

We take the view that `RequiresDynamicCodeAttribute`, `RuntimeFeature.IsDynamicCodeSupported`, and `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"` all conceptually represent the same "feature".

- The "feature" of dynamic code support is represented as:
  - `RequiresDynamicCodeAttribute`
  - `RuntimeFeature.IsDynamicCodeSupported` property
  - `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"`

Currently, "dynamic code" is the main example of a feature that defines all three of these components.

Not all features come with a feature switch. For example:

- The "feature" of unreferenced code being available is represented as:
  - `RequiresUnreferencedCodeAttribute`

It's easy to imagine adding a feature check property like `RuntimeFeature.IsUnreferencedCodeSupported` that would be set to `false` in trimmed apps. We don't necessarily want to do this, because there is value in organizing features that call into `RequiresUnreferencedCode` APIs in a more granular way, so that they can be toggled independently.

- The "feature" of individual assembly files being available in an app is represented as:
  - `RequiresAssemblyFilesAttribute`

Similarly, not all features come with an attribute. Some features don't tie into functionality which is designed to produce warnings if called. For example:

- The "feature" of verifying that open generics are dynamically instantiated with trim-compatible arguments is represented as:
  - `VerifyOpenGenericServiceTrimmability` property
  - `"Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability"`

- The "feature" of supporting or not supporting globalization is represented as:
  - `GlobalizationMode.Invariant` property
  - `"System.Globalization.Invariant"`

No warnings are produced just because these features are enabled/disabled. Instead, they are designed to be used to change the behavior, and possibly remove unneccessary code when publishing. It's easy to imagine adding an attribute like `RequiresVariantGlobalization` that would be used to annotate APIs that rely on globalization support, such that analysis warnings would prevent accidentally pulling in the globalization stack from code that is supposed to be using invariant globalization. We don't want to do this for every feature; typically we only do this for features that represent large cross-cutting concerns, and are not available with certain app models (trimming, AOT compilation, single-file publishing).

However, any attribute-based model that we pick to represent features should be able to tie in with all three representations.

### Unified attribute model for feature switches and guards

We would like for the attribute model to have a consistent way to refer to a feature in `FeatureGuardAttribute` or `FeatureSwitchAttribute`. The proposal is to support an attribute model for features that have an associated `Requires` attribute, and use that attribute type uniformly to refer to the feature from `FeatureGuardAttribute` and `FeatureSwitchAttribute`.

For example, the feature switch for "dynamic code support" might look like this:
```csharp
public class RuntimeFeature
{
    [FeatureSwitch<RequiresDynamicCode>]
    public static bool IsDynamicCodeSupported => // ...
}

class RequiresDynamicCodeAttribute : Attribute, IFeatureAttribute<RequiresDynamicCodeAttribute> {
    public static string FeatureName => "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported";
}
```
and a feature guard for "dynamic code compilation" might look like this:
```csharp
public class RuntimeFeature
{
    [FeatureGuard<RequiresDynamicCode>]
    public static bool IsDynamicCodeSupported => // ...
}
```

The attribute definitions to support this might look like this:
```csharp
class FeatureGuardAttribute<T> : Attribute
  where T : Attribute, IFeatureAttribute<T> {}

interface IFeatureAttribute<TSelf> where TSelf : Attribute {
    static abstract string FeatureName { get; }
}
```

We could use this model even for feature switches similar to `StartupHookSupport`, which don't currently have a `StartupHoopSupportAttribute`. There's no need to actually annotate related APIs with the attribute if that is overkill for a small feature. In this case the attribute definition would just serve as metadata that allows us to define a feature switch in a uniform way.

This is fundamentally the same idea outlined in https://github.com/dotnet/designs/pull/261. The hope is that this document provides the motivation for using a unified representation for the attributes that are related to feature switches and trim/AOT analysis.

## Comparison with platform compatibility analyzer

The platform compatibility analyzer is semantically very similar to the behavior described here, except that it doesn't come with ILLink/ILCompiler support for removing removing branches that are unreachable when publishing for a given platform.

"Platforms" (instead of "features") are represented as strings, with optional versions.

- `SupportedOSPlatformAttribute` is similar to the `RequiresDynamicCodeAttribute`, etc, and will produce warnings if called in an unguarded context.

  ```csharp
  CallSomeAndroidAPI(); // warns if not targeting android

  [SupportedOSPlatform("android")]
  static void CallSomeAndroidAPI() {
    // ...
  }
  ```

- Platform checks are like feature switch properties, and can guard calls to annotated APIs:

  ```csharp
  if (OperatingSystem.IsAndroid())
    CallSomeAndroidAPI(); // no warning for guarded call
  ```

  The analyzer has built-in knowledge of fact that `IsAndroid` corresponds to the `SupportedOSPlatform("android")`. This is similar to the ILLink analyzer's current hard-coded knowledge of the fact that `IsDynamicCodeSupported` corresponds to `RequiresDynamicCodeAttribute`.

- Platform guards are like feature guards, allowing libraries to incroduce custom guards for existing platforms:

  ```csharp
  class Feature {
      [SupportedOSPlatformGuard("android")]
      public static bool IsSupported => SomeCondition() && OperatingSystem.IsAndroid();
  }
  ```

  ```csharp
  if (Feature.IsSupported)
    CallSomeAndroidAPI(); // no warning for guarded call
  ```

The platform compatibility analyzer also has some additional functionality, such as annotating _unsupported_ APIs, and including version numbers.


## Possible future extensions

We might eventually want to extend the semantics in a few directions:

- Feature switches with inverted polarity (`false` means supported/available)

  `GlobalizationMode.Invariant` is an example of this. `true` means that globalization support is not available.

  This could be done by adding an extra boolean argument to the feature switch attribute constructor:

  ```csharp
  class GlobalizationMode {
      [FeatureSwitch("Globalization.Invariant", negativeCheck: true)]
      public static bool InvariantGlobalization => AppContext.TryGetSwitch("Globalization.Invariant", out bool value) ? value : false;
  }
  ```

  ```csharp
  if (GlobaliazationMode.Invariant) {
      UseInvariantGlobalization();
  } else {
      UseGlobalization(); // no warning
  }

  [RequiresGlobalizationSupport]
  static void UseGlobalization() { }
  ```

- Feature guards with inverted polarity. This could work similarly to feature switches:
  ```csharp
  class Feature {
      [FeatureGuard("RuntimeFeature.IsDynamicCodeSupported", negativeCheck: true)]
      public bool IsDynamicCodeUnsupported => !RuntimeFeature.IsDynamicCodeSupported;
  }
  ```

- Feature attributes with inverted polarity

  It would be possible to define an attribute that indicates _lack_ of support for a feature, similar to the `UnsupportedOSPlatformAttribute`. The attribute-based model should make it possible to differentiate these from the `Requires` attributes, for example with a different base class.

  It's not clear whether we have a use case for such an attribute, so these examples aren't meant to suggest realistic names, but just the semantics:

  ```csharp
  class RequiresNotAttribute : Attribute {}

  class RequiresNoDynamicCodeAttribute : RequiresNotAttribute {}
  ```

- Versioning support for feature attributes/checks/guards

  The model here would extend naturally to include support for version checks the same way that the platform compatibility analyzer does. Versions would likely be represented as strings because they are encodable in custom attributes:

  ```csharp
  class RequiresWithVersionAttribute : Attribute {
      public RequiresWithVersionAttribute(string version) {}
  }

  class RequiresFooVersionAttribute : RequiresWithVersionAttribute {
      public RequiresFooVersionAttribute(string version) : base(version) {}
  }

  class Foo {
      [FeatureSwitch<Requires>]
      public static bool IsSupportedWithVersionAtLeast(string version) => return VersionIsLessThanOrEquals(version, "2.0");

      [RequiresFooVersion("2.0")]
      public static void Impl_2_0() {
          // Do some work
      }

      [RequiresFooVersion("1.0")]
      public static void Impl_1_0() {
        // Breaking change was made in version 2.0, where this API is no longer supported.
          throw new NotSupportedException();
      }
  }
  ```

  Code that was originally built against the 1.0 version, and broken on the upgrade to the 2.0 version, could then be updated with a feature check like this:
  ```csharp
  if (Foo.IsSupportedWithVersionAtLeast("2.0")) {
      Foo.Impl_2_0();
  } else {
      Foo.Impl_1_0();
  }
  ```

  Although it's not clear in practice where this would be useful. This is not meant as a realistic example.



<hr />

# Two models for custom feature checks

In practice, custom feature checks in dotnet/runtime broadly fall into two categories:

- Checks that can be toggled independently via a feature switch
- Checks that simply act as a guard for an existing feature

## Independent feature switch

The IL property gets its value from `AppContext`:

```csharp
class Feature {
    static bool IsSupported => AppContext.TryGetSwitch("Feature.IsSupported", out bool enabled) ? enabled : true;
}
```

This is used to guard code paths which should be trimmed when the feature is disabled:

```csharp
class Feature {
    static void DoSomething () {
        if (IsSupported) {
            CallSomeAPI(); // Should be removed when Feature.IsSupported == false
        } else {
            UseFallback();
        }
    }
}
```

Substitution XMLs ensure that guarded branches get removed by ILLink/ILCompiler when `Feature.IsSupported` is set to `false`. If `CallSomeAPI` is annotated with `RequiresUnreferencedCode` or `RequiresDynamicCode`, no warnings are produced by ILLink/ILCompiler in this case.

However, the ILLink Roslyn analyzer has no way to silence these warnings, because it doesn't see the substitution XMLs.

<details>
<summary>Examples</summary>

#### AllowCustomResourceTypes

- substituted false based on AllowCustomResourceTypes in shared substitutions
- set from AppContext
- set to false for trimmed apps in MSBuild

#### StartupHookSupport

- substituted false based on StartupHookProvider.IsSupported in xml
- gets value from AppContext
- set to false for trimmed apps in MSBuild

#### VerifyOpenGenericServiceTrimmability

- substituted true based on feature in XML
- gets value from appcontext
- set to true for trimmed apps in MSBuild

If you manually set it to false, presumably just get inconsistency between trimmed apps and
"dotnet run". No warning or anything. Just don't get the extra validation. So "dotnet run" might
work, but the trimmed app might fail.

#### DynamicCodeSupport

- set to false for AOT apps in MSBuild
- substituted false always in nativeaot XML
- returns false always in nativeaot corelib
- substituted true/false based on feature in coreclr XML
- substituted false based on feature in iOS XML
- gets value from appcontext on non-nativeaot corelib
  - mono marks as intrinsic, where mono AOT compiler changes to false for FullAOT scenarios

#### JsonSerializer.IsReflectionEnabledByDefault (buggy)

- substituted false _unconditionally_ in xml???
- gets value from AppContext
- set to false for trimmed apps in MSBuild

Really weird that we mark methods RUC/RDC if they have checks to IsReflectionEnabledByDefault...
Looks like you can't ever set it to true for trimmed apps.
Wrong. Would have different behavior for trimmed apps.

Similar to IsDynamicCodeCompiled. Can't be substituted independently (even though it can be set
independently in "dotnet run". Probably a bug.)

#### CanEmitObjectArrayDelegate (buggy)

- set to false in AOT MSBuild
- substituted false based on feature in XML
- returns 'true' in code

Configurable via AppContext for trimmed apps only, but not for "dotnet run". Probably a bug.

### CanInterpret

- always returns true

</details>

## Guard for existing feature

The IL property is based on the value of an existing feature switch:

```csharp
class Feature {
    static bool IsSupported => ExistingFeature.IsSupported;
}
```

It might also be associated with a more specific condition:

```csharp
class Feature {
    static bool IsSupported => ExistingFeature.IsSupported && DayOfWeekEquals("Wednesday");
}
```

Or an instance member:

```csharp
class FeatureInstance {
    bool isSpecialInstance;

    bool IsSupported => ExistingFeature.IsSupported && isSpecialInstance;
}
```

This is used to guard code paths which should be trimmed when the existing feature is disabled:

```csharp
class Feature {
    static void DoSomething() {
        if (IsSupported) {
            UseExistingFeature(); // Should be removed when ExistingFeature.IsSupported == false
        } else {
            UseFallback();
        }
    }
}
```

Constant propagation and/or substitution XMLs ensure that guarded branches get removed by ILLink/ILCompiler when `ExistingFeature.IsSupported` is set to `false`. ILLink performs interprocedural constant propagation, so if there is a substitution XML that sets `ExistingFeature.IsSupported` to `false`, this happens automatically. ILCompiler doesn't perform interprocedural constant propagation, so it instead relies on a separate substitution XML which sets `Feature.IsSupported` to `false` based on the value of `ExistingFeature.IsSupported`.

If `UseExistingFeature` is annotated with `RequiresUnreferencedCode` or `RequiresDynamicCode`, no warnings are produced by ILLink/ILCompiler in this case.

However, the ILLink Roslyn analyzer has no way to silence these warnings, because it doesn't see the XML or perform constant propagation across methods.

<details>
<summary>Examples</summary>

#### IsDynamicCodeCompiled

- substituted false in nativeaot
- set to false in nativeaot corelib
- substituted true/false based on Supported feature in coreclr xml
- set to IsDynamicCodeSupported in non-nativeaot corelib
  - with ifdef MONO that puts [Intrinsic],
    changes to false if FullAOT or interpreted scenario, otherwise
    falls back on IsDynamicCodeSupported too.
- substituted false in iOS substitutions. unconditional.

Summary: may be false in cases where Supported is true. But always false-if-false. And on platforms
where we know Supported is false, we stub it to false.

Weird that we stub it to true based on Supported feature in corelib. Doesn't feel right.
No way to set it to false. Probably because there's no interpreter on coreclr?

Analyzer should treat as guard.

#### CanCompileToIL

- substituted false based on IsDynamicCodeSupported in xml
- returns RuntimeFeature.IsDynamicCodeSupported

Can't be toggled independently. Pretty much same as IsDynamicCodeCompiled.

#### VerifyAotCompatibility

- (ignoring netfx/netstandard) Gets value from !IsDynamicCodeSupported
- Used to insert a check method for AOT, that will probably throw or something before calling MakeGenericType
  with value types.

Similar to IsDynamicCodeCompiled. Can't be substituted independently.

#### CanCreateArbitraryDelegates

- substituted false based on IsDynamicCodeSupported in XML
- code returns RuntimeFeature.IsDynamicCodeSupported

Similar to IsDynamicCodeCompiled. Can't be substituted independently.
</details>

## Goal

We would like the Roslyn analyzer to not produce any warnings in code guarded by `IsDynamicCodeCompiled`, or other feature checks that behave similarly, whether defined in our own code or in third-party library code.

## Solution approaches



### 

### 


# TODO: mention SupportedOSPlatform

This has both an attribute involed in the analysis, and a feature switch with support for replacing calls to the feature check property with a constant when publishing, based on the MSBuild settings.


## Decomposing the problem

There are two related pieces of functionality:
- feature guards for existing features, and
- feature switches for newly defined features.

The most immediate problem we would like to solve is that of feature guards; the analyzer should treat `IsDynamicCodeCompiled` and similar feature checks as guards for APIs marked with `RequiresDynamicCodeAttribute`.

### Feature guards




### Feature switches

A secondary goal is to simplify the way libraries can introduce feature switches of their own. This is currently possible but cumbersome with the XML-based substitutions.

All that a feature switch does is to enable a different code path to be taken when certain settings are passed, with support for branch elimination by ILLink and ILCompiler.

For example:
```csharp
class Feature {
    public static bool LightweightMode => AppContext.TryGetSwitch("Feature.LightweightMode", out bool isEnabled) ? isEnabled : false;

    static void DoSomething() {
        if (LightweightMode)
            LightweightImpl();
        else
            ExpensiveOrLargeImpl();
    }
}
```

This enables the `"Feature.LightweightMode"` setting to control whether the lightweight code path is taken at runtime. If it is set up with a substition XML, ILLink or ILCompiler can also remove the `ExpensiveOrLargeImpl` method in this case.

Sometimes we define different defaults for native AOT or trimmed apps. One reason is just to reduce code size and get the related performance improvement. Another reason is to disable an area of functionality known to be unsupported with the target form factor.

For example, in a trimmed app, the feature `RequiresUnreferencedCodeAttribute` is disabled out of the box without any feature switch, and calls to APIs annotated as such will warn. There are several independent feature switches such as `AllowCustomResourceTypes` or `StartupHookSupport` which are disabled by default to avoid depending on `RequiresUnreferencedCode`-annotated APIs.

In a native AOT app, `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"` is set to `false` by default because native AOT doesn't provide a JIT or interpreter, and calls to APIs annotated with `RequiresDynamicCodeAttribute` will produce warnings. There is also an independent feature switch `CanEmitObjectArrayDelegate` that is disabled by default because it depends on APIs marked `RequiresDynamicCode`.


## Guarding features that don't have feature attributes

We've identified three components that a "feature" may or may not have:
- Attribute
- Feature switch (property and a string name)
- Feature guard (property only)

For each of these, there are examples of features that _only_ define this component:
- Attribute only (`RequiresUnreferencedCodeAttribute`)
- Feature switch only (`GlobalizationMode.Invariant`/`"System.Globalization.Invariant"`)
- Feature guard only (`IsDynamicCodeCompiled`)

Each of these components requires different levels of support in ILLink, ILCompiler, and the analyzer:
#### Attribute
All tools must produce warnings for at least one feature attribute (`RequiresUnreferencedCodeAttribute`)

#### Feature switch
ILCompiler/analyzer must support treating the specific feature switch `IsDynamicCodeSupported` as a guard for the attribute (via branch elimination in ILCompiler).
ILLink/ILCompiler/analyzer must support treating certain feature switches that are designed to avoid calling into `RequiresUnreferencedCode` APIs as guards for the attribute (such as `AllowCustomResourceTypes`).
ILLink/ILCompiler must support branch removal for feature switches unrelated to an attribute, but analyzer doesn't have a particular need for this yet. It may be useful in the future to prevent warnings from effectively dead code in apps.

#### Feature guard
// TODO: does ILLink have this?
ILCompiler/analyzer must support treating `IsDynamicCodeCompiled` as a guard for `RequiresDynamicCode`. ILLink may not need to support guards in the same way.



```csharp
class FeatureFoo {
    [FeatureSwitch("FeatureFoo.IsSupported")]
    public static bool IsSupported => AppContext.TryGetSwitch("FeatureFoo.IsSupported", out bool isEnabled) ? isEnabled : false;

    public static void DoFoo() {
        if (!IsSupported)
            throw new NotSupportedException();

        Impl();
    }

    static void Impl() {
        // Do some work...
    }
}

class FeatureBar {
    [FeatureGuard("FeatureFoo.IsSupported")]
    public static bool IsSupported => FeatureFoo.IsSupported;

    public static void DoBar() {
        if (!IsSupported)
          throw new NotSupportedException();

        FeatureFoo.DoFoo();
        Impl();
    }

    static void Impl() {
        // Do some work...
    }
}
```

If there's a desire to trim out the `Impl` methods, then 

## Unified representation of features for feature guards and feature switches

Notice that both feature switches and feature guards need to reference a feature somehow. However, 


```csharp
class Feature {
    [FeatureGuard<RequiresDynamicCodeAttribute>]
    static bool IsSupported() { return RuntimeFeature.IsDynamicCodeSupported; }
}

class FeatureGuardAttribute<T> : Attribute
  where T : Attribute, IFeatureAttribute<T> {}

interface IFeatureAttribute<TSelf> where TSelf : Attribute {
    static abstract string FeatureName { get; }
}

class RequiresDynamicCodeAttribute : Attribute, IFeatureAttribute<RequiresDynamicCodeAttribute> {
    public static string FeatureName => "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported";
}
```

If using strings, the analyzer will hard-code the fact that the string `"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"` corresponds to `RequiresDynamicCodeAttribute`, so that `Feature.IsSupported` can guard calls to `RequiresDynamicCode`-annotated APIs.

If using a reference to the existing feature check, the analyzer will hard-code the fact that the API `RuntimeFeature.IsDynamicCodceSupported` corresponds to `RequiresDynamicCodeAttribute`, so that `Feature.IsSupported` can guard calls to `RequiresDynamicCode`-annotated APIs.

If using the attribute-based model, the analyzer will see the tie 


<!-- ### Hard-code analyzer support

We could just hard-code the fact that `IsDynamicCodeCompiled` should act as a guard for APIs annotated with `RequiresDynamicCode`. This would solve that particular problem, but isn't extensible. It would not work for other feature checks that behave similarly, such as `CanCompileToIL` or `CanCreateArbitraryDelegates`. It would not work for custom feature checks in other libraries. This doesn't really solve the problem. -->




It is possible via XML to define a new feature that has as string name and a check property, but there is currently no extensibility model for defining a new feature attribute. We might consider adding new feature _attributes_ out of scope for this proposal, but the solution should be compatible with introducing feature attributes for new features.

Should we define an attribute-based model for defining feature switches?



  <hr />

  

## Analogy with `DefineConstants`

Feature switches serve a similar purpose to `DefineConstants`. They behave similarly, except that they are substituted by ILLink/ILCompiler at publish time, instead of by Roslyn at build time.

| Preprocessor symbols | Feature switches |
| - | - |
| `DefineConstants` | `RuntimeHostConfigurationOption` |
| - abc |oe |


For


## Feature check relationship with feature switches

ILLink and ILCompiler respect feature switches that have a corresponding `ILLink.Substitutions.xml` that sets a feature check to return a constant when a feature switch is set. This allows branch elimination for code guarded by a feature check.

The ILLink Roslyn analyzer only understands the built-in feature check `RuntimeFeature.IsDynamicCodeSupported`. It similarly considers branches guarded by this feature check to be reachable only under the assumption that dynamic code is supported, so it doesn't produce warnings about code which requires dynamic code support in such a context.

## Feature check relationship with `Requires` attributes

Some, but not necessarily all, feature checks guard code which is attributed with `Requires` attributes such as `RequiresUnreferencedCodeAttribute` or `RequiresDynamicCodeAttribute`.

When used with `ILLink.Substitutions.xml`, this allows trimming and native AOT to remove code paths which aren't compatible with trimming and native AOT.

The ILLink Roslyn analyzer 



## Branch removal

ILLink and ILCompiler remove unused branches that are guarded by [feature checks](feature-checks.md) when the tools know that the feature check returns a constant, based on `ILLink.Substitutions.xml`. The ILLink Roslyn analyzer similarly considers branches guarded by feature checks to be reachable only under the assumption that the corresponding feature switch is enabled. The only feature switch currently supported by the ILLink Roslyn analyzer is `RuntimeFeature.IsDynamicCodeSupported`, which has built-in support. The analyzer doesn't process `ILLink.Substitutions.xml`.

This may be used for two related purposes:
- Preventing warings in code paths that are incompatible with trimming or native AOT
- 

interaction with feature attributes

ILLink and ILCompiler produce warnings when they encounter calls to APIs annotated with `RequiresUnreferencedCodeAttribute` (both tools) or `RequiresDynamicCodeAttribute` (ILCompiler only). 


## Option 1

Tie feature checks to requires attributes

This would allow libraries to define feature checks which act as guards for `RequiresUnreferencedCode` and `RequiresDynamicCode`. In ILLink and ILCompiler, such feature checks could be set to a constant by default (event without ILLink.Substitutions.xml or any extra feature settings passed in to the tool). ILLink.Substitutions.xml would still be needed:
- to support overriding these defaults, allowing the developer to turn _on_ a feature which is incompatible with trimming/AOT, or
- to support branch removal for features which don't have to do with `RequiresUnreferencedCode` or `RequiresDynamicCode`.

It lets us get rid of default feature settings for ILLink and ILCompiler. The information about which features are incompatible with trimming/AOT now lives in the code, not in the SDK.

Solves the analyzer experience for `IsDynamicCodeCompiled`

Tying a feature to an attribute means it would be disabled by default. Doesn't _necessarily_ mean it is incompatible with trimming. It could be annotated as being a guard for RUC, even if it is never actually used to guard a call to RUC code!

## Option 2

Tie feature checks to feature names

This would allow ILLink and ILCompiler to set feature switches to constants when the feature switch is passed in to the tool. It still requires the SDK to pass in the feature settings for features which are disabled when trimming or native AOT compiling.

The ILLink Roslyn analyzer would also need to be told which features are incompatible with `RequiresUnreferencedCode` or `RequiresDynamicCode`. 

This allows us to replace ILLink.Substitutions.xml with an attribute-based model, but the information about which feature switches are incompatible with trimming/AOT still lives in the SDK.

## Option 3

Do both!

This would allow us to get rid of ILLink.Substitutions.xml entirely.

# Terminology

- "Incompatible with trimming/AOT": means it would produce trim/AOT warnings if enabled.
- Disabled for trimming/AOT: means it is disabled by default, maybe as a size optimization, but can be turned on without trim/AOT warnings.

Revisiting the options:

- Tie feature checks to attributes
  - Can remove trim/AOT default feature settings from the SDK
  - Still need substitutions
  - Lets analyzer respect user-defined guards
  - Possible to define feature check for trimming that is never used as such.
- Tie feature checks to feature names
  - Can remove substitutions
  - Still need to pass feature settings to ILLink/ILCompiler to remove them
  - Would need to define way to tell analyzer "these features are incompatible with RUC/RDC/etc"

A feature that guards a call to RDC can still be turned on. What's the analyzer supposed to do about that? It wouldn't give you any warnings.

IsDynamicCodeSupported can't be turned on for NativeAOT. So anything guarded by IsDynamicCodeSupported won't warn. Same with IsDynamicCodeCompiled. The "base case" for NativeAOT.

But for ILLink, both features can be turned on via a feature switch.

Other features can still be turned on independently, even if off by default.

SO: in "nativeaot" mode, the analyzer should treat IsDynamicCodeCompiled as a guard for RDC.
But in "illink" mode, there are no RDC warnings in the first place.

What about a feature that can be turned ON in illink, which guards a RUC API? StartupHoopSupport. Or binary serialization.

My view: Do both. But unify the concepts. Treat a feature name very similarly to a new Requires* attribute, just that
it's not worth introducing a new one for each feature. And define a way to declare dependencies between features.

StartupHook -> UnreferencedCode.
Then the feature check for startup hook can guard RUC without analyzer warnings. We can still avoid baking in the feature during library build.
Custom -> UnreferencedCode, DynamicCode
DynamicCodeCompilation -> DynamicCode

All we have to tell the tools:
- Turn off "UnreferencedCode" for nativeaot and illink
- Turn off "DynamicCode" for nativeaot
Any features which depend on these will be off-by-default. We can still turn them on (except base cases) via settings. Don't need XML any more.

How to tell the tools that a feature depends on another?


Really need:
- trim-time conditional.
  [Conditional("StartupHook.IsSupported")]

Really want something like conditional compilation.
`UnconditionalConditionalAttribute`

Condition must be inside.

Constant.

```csharp
if (Feature.IsSupported) {

}

class Feature {
    [FeatureCondition("Feature.IsSupported")]
    static bool IsSupported => AppContext.TryGetSwitch("Feature.IsSuported");
}
```

Use strings for feature names.

```
```


# Goals

- IsDynamicCodeCompiled should not warn in the analyzer
- remove SDK's knowledge of which features are disabled when trimming/aot

## Example API usages

```csharp
class Feature {
    [FeatureCondition("Feature.IsSupported")]
    static bool IsSupported => //...
}
```

```csharp
class Feature {
    [FeatureCheck<RequiresUnreferencedCodeAttribute>]
    static bool IsSupported => // ...
}
```

```csharp
if (Feature.IsSupported)
    Featuer.Do();

class Feature {

    [FeatureSwitch("Feature.IsSupported")]
    [FeatureGuard("UnreferencedCode")] // DependsOn, TrueOnlyIf, RequiresFeature, RequiresCheck
    static bool IsSupported => // ...

    [Requires("UnreferencedCode")]
    static void Do() {}
}
```

API shape:
- One is the substitution. There can be only one.
- Another is which features it depends on.
- Another is which attribute describes which feature.

```csharp

if (Consumer.IsSupported) {
    Consumer.DoSomething();
}

[CapabilityCheck(typeof(Feature), nameof(Feature.IsSupported))]
class RequiresFeatureAttribute : RequiresAttribute {}

class Consumer {
    [CapabilityGuard(typeof(RequiresFeatureAttribute))]
    static bool IsSupported => // ...

    [RequiresFeature]
    public static void DoSomething () {}
}

```

Important pieces of the API:
- I don't want a single attribute combining feature name with feature dependencies (RUC)
- Feature guard should be automatically stubbed out by default, with the setting of the feature it guards.
  (if the feature it depends on is disabled, then the guard should be disabled by default)

FeatureCheck/FeatureCondition on attribute? Or on the check? Attribute encoding is more efficient.
- Generics or not?
- Define an attribute for each feature?

Capability attribute uses attribute definition as the abstraction of a feature.

For ease of implementation:
- When visiting property, should be able to tell if it's a feature check.
  OR analyzer needs to have a list of feature attributes to look up. But
  one goal is that the set of supported features isn't static.
- => property can declare itself as a FeatureCheck("")
  causes it to be stubbed with the feature value.

- System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
```csharp
class RuntimeFeature {
    [UnconditionalCondition("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported")]
    public static bool IsDynamicCodeSupported => // ...
}
```

Feature check vs feature guard?
- A method can only be a feature check for one feature.
- Because that feature dictates its value when stubbing it out.
- But a method can be a feature guard for multiple features.
- Feature guards silence warnings from features they depend on...
  UNLESS the features are enabled. Then analyzer and other tools should warn.

You can turn on a feature that depends on RUC and trim. Then library implementation details will start warning.
The analyzer will only warn if it encounters code using the feature check.

m/n relationships

- A property can only be defined by one feature name
- A feature name might have multiple properties

- Feature check vs feature guard?
  Feature guard allows multiple.
  But...

IsDynamicCodeCompiled can be false, even if IsDynamicCodeSupported is true.

Problem: IsDynamicCodeSupported true means IsDynamicCodeCompiled gets set to true.
But what if I don't want that?
