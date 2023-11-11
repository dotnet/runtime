# Attribute-based model for feature switches

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

  We will explore what an attribtute-based model for feature switches would look like to ensure that it interacts well with a model for feature guards. It's possible that we would design both in conjunction if they are naturally related.

### Non-goals

- Support branch elimination in the analyzer for all feature switches

  The most important use case for the analyzer is analyzing libraries. Libraries typically don't bake in constants for feature switches, so the analyzer needs to consider all branches. It should support feature guards for features that produce warnings, but doesn't need to consider feature settings passed in from the project file to treat some branches as dead code.

- Teach the ILLink Roslyn Analyzer about the substitution XML

  We don't want to teach the analyzer to read the substitution XML. The analyzer is the first interaction that users typically have with trimming and AOT warnings. This should not be burdened by the XML format. Even if we did teach the analyzer about the XML, it would not solve the problem because the analyzer must not globally assume that `IsDynamicCodeSupported` is false as ILCompiler does.

- Define a model with the full richness of the supported OS platform attributes

  We will focus initially on a model where feature switches are booleans that return `true` if a feature is enabled. We aren't considering supporting version checks, or feature switches of the opposite polarity (where `true` means a feature is disabled/unsupported). We will consider what this might look like just enough to gain confidence that our model could be extended to support these cases in the future, but won't design this fully in the first iteration.

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


