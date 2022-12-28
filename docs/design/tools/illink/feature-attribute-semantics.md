# Feature attribute semantics

This specification defines the semantics of "feature attributes", in terms of a hypothetical `RequiresFeature` attribute type. The rules described here are designed to be applicable to any attribute that describes a feature or capability of the code or the platform.

## Terminology

There's a history in .NET of using the terms "features" and "capabilities" to describe closely related concepts.

In the linker and runtime we often refer to "feature switches", which are toggles that can tell illink and NativeAot to treat specific predefined properties as constants. There are also "runtime features" such as support for new language features like byref fields.

We also often refer to "platform capabilities" such as the ability to run dynamic code, or the ability to create new threads. Sometimes the deployment model may restrict the set of available APIs; in that case we also may refer to "capabilities" (for example the ability to access files on disk, which is not available in single-file apps).

Capabilities will often have associated attributes and/or feature switches. For example, `RequiresDynamicCodeAttribute` is used to annotate code which requires support for running dynamic code, and `RuntimeFeature.IsDynamicCodeSupported` can be used to check for this support and guard access to annotated code. We also have many feature switches for features which depend on the availablility of so-called unreferenced code (that is, code which may be removed by trimming), which is attributed with `RequiresUnreferencedCodeAttribute`.

Feature switch settings may also be determined by explicit configuration (in addition to the choice of target platform or deployment model). For example, the `System.Globalization.Invariant` feature switch can be used to disable support for culture-specific globalization behavior.

This design takes the view that "features" and "capabilities" are essentially the same concept. We use the terminology "features" for these attribute semantics, because it seems to have slightly more general usage. "Features" as used today often includes runtime features which don't have feature switches or attributes, whereas "capabilities" is most often used to refer to capabilities of the platform or deployment model and do usually come with feature switches.

The intention is for this specification to be generally applicable to any feature which would benefit from an attribute used to annotate code that should only be run when a given feature is available. The decision whether to introduce such a feature attribute is of course determined case by case by the feature owners.

## Motivation

Existing attributes like `RequiresUnreferencedCodeAttribute`, `RequiresDynamicCodeAttribute`, and `RequiresAssemblyFilesAttribute` have behavior close to what is described below. The behavior differs slightly between illink and NativeAot in the details, so this is an attempt to specify the semantics as clearly as possible, so that both tools can converge to match this.

The ILLink Roslyn analyzer also produces warnings for these attributes, but doesn't have insight into the compilation strategy used for compiler-generated code. These rules are designed so that the warnings produced by a Roslyn analyzer are matched by the IL analysis, but IL analysis may include additional warnings (specifically for reflection access to compiler-generated code).

There is also the possibility that we will create an attribute-based model which allows users to define their own feature attributes; see this draft for example: https://github.com/dotnet/designs/pull/261. The semantics outlined here could be extended to those attributes if we determine that they are appropriate there.

We would also like to share as much code as possible for this logic between the linker, NativeAot, the corresponding analyzers, and possibly future analyzers.

## Goals

- Define the semantics of feature attributes
- Define the access patterns which are allowed and disallowed by these semantics

## Non-goals

- Specify the warning codes or wording of the specific warnings for disallowed access
- Define a model for defining new feature attributes
- Define an attribute-based model for feature switches
- Specify the relationship between feature switches and feature attributes
- Define the interactions between `RequiresUnreferencedCodeAttribute` and `DynamicallyAccessedMembersAttribute`

## RequiresFeatureAttribute

`RequiresFeatureAttribute` may be used on methods, constructors, or classes only.

The use of this attribute establishes a [_feature requirement_](#feature-requirement) for the attributed type or member, which restricts access to the attributed type or member (and in some cases to other related IL) in certain ways. It also establishes a [_feature available_](#feature-available-scope) scope (which includes the attributed member but may also include other related IL) wherein access to members with a _feature requirement_ is allowed.

Access to members with a _feature requirement_ is always allowed from a _feature available_ scope, and never produces feature warnings. The restrictions created by _feature requirement_ only limit access from scopes outside of _feature available_, where certain access patterns produce warnings.

## Feature available scope

The following constructs with a _feature requirement_ are also in a _feature available_ scope:
- Methods
- Constructors (including static constructors)

When a class or struct has a _feature requirement_, the following members are in a _feature available_ scope:
- Methods
- Constructors (including static constructors)
- Fields
- Properties
- Events

Note that the _feature available_ scope for a type does not extend to nested types or to members of base classes or interfaces implemented by the type.

## Feature requirement

### Methods

When `RequiresFeature` is used on a method or constructor (except static constructors), this declares a _feature requirement_ for the method.

`RequiresFeature` on a static constructor is not supported. Note however that static constructors may have a _feature requirement_ due to the declaring type having a _feature requirement_.

### Classes

When `RequiresFeature` is used on a class, this declares a _feature requirement_ for the class.

When a class has a _feature requirement_, this creates a _feature requirement_ for the following members of the class:
  - static methods
  - all constructors (static and instance)
  - static fields
  - static properties
  - static events

Note that this does not create a _feature requirement_ for nested types or for members of base classes or interfaces implemented by the type.
Note also that this may create a _feature requirement_ for fields, properties, and events, which cannot have `RequiresFeature` used on them directly.

### Structs

When a struct has a _feature requirement_, this creates a _feature requirement_ for the following members of the struct:
  - all methods
  - all constructors (static and instance)
  - all fields
  - all properties
  - all events

Note that structs may have _feature requirement_ due to compiler-generated code, even though they can not have `RequiresFeature`.
Note also that this does not create a _feature requirement_ for members of interfaces implemented by the type.

### State machine types

When an iterator or async method is in a _feature available_ scope, the compiler-generated state machine class or type has a _feature requirement_.

### Nested functions

When a method is in a _feature available_ scope, lambdas and local functions declared in the method have a _feature requirement_.

When a lambda or local function is in a _feature available_ scope, lambdas and local functions declared in it have a _feature requirement_.

When a lambda or local function is declared in a method or nested function which is in a _feature available_ scope, then the following compiler-generated type or members have a _feature requirement_:

- The generated closure environment type, if it is unique to the lambda or local function, OR

- The generated method for the lambda or local function, if the compiler does not generate a type for the closure environment, OR

- The generated method and delegate cache field for the lambda or local function, if these are generated into a static closure environment type.

Note that IL analysis tools currently deviate from this specification because the IL does not always contain enough information to reconstruct the original nesting of lambdas and local functions. (For ILLink, lambdas and local functions inherit _feature requirement_ from the enclosing user method, not from an enclosing lambda or local function if one is present.)

## Validation behavior

### RequiresFeature attribute

`RequiresFeature` on a static constructor is disallowed.

`RequiresFeature` on a method that already has a _feature requirement_ due to another attribute is allowed.

`RequiresFeature` on a method that is in a _feature available_ scope is allowed. This establishes a _feature requirement_ for the method even if there was not one previously. (Note: this could be made stricter by warning about redundant `RequiresFeature` on methods that are already in a _feature available_ scope.)

### Virtual methods

- Overriding a method that has a _feature requirement_ with a method outside of a _feature available_ scope is disallowed.
- Overriding a method outside of a _feature available_ scope with a method that has a _feature requirement_ is disallowed.

### Member access

Access to a _feature requirement_ method, constructor, field, property, or event outside of a _feature available_ scope is disallowed.

## Feature checks

Some feature attributes also come with corresponding feature checks that can be evaluated as constant at the time of trimming, with the guarded code removed when a feature is disabled. This effectivtely places the guarded code in a _feature available_ scope for the purposes of this analysis. However, the definition of such feature checks is left unspecified for now.

## Trimming

These semantics have been designed with trimming in mind. When a feature is disabled (by user configuration, or based on limitations of the target platform), trimming an app that will remove most or all of the feature-related code. Specifically, when a feature is disabled and an app has no trim warnings (including suppressed warnings):

- Methods, fields, properties, and events which have a _feature requirement_ for the disabled feature may be removed.

- Methods which are in a _feature available_ scope for the disabled feature, but aren't entirely removed, may have the method body replaced with a throwing instruction sequence.

Thie latter can happen for methods in a type with _feature requirement_ (but that do not themselves have _feature requirement_) that are referenced outside of a _feature available_ scope. The reference to such a method may remain even though the type is never constructed. The callsite would produce a `NullReferenceException` and the method body is unreachable.

## Alternatives

One simplification would be to unify the concepts of _feature requirement_ with _feature available_, and treat both as similar to preprocessor symbols, where _any_ reference to a guarded type or member from an unguarded context is disallowed.

The advantage of the specified model is that it allows some references without warning, giving some extra flexibility and making it easier to migrate existing code. The downside is that it might lead to preserving more code, whereas a simplified model could guarantee that all code related to a disabled feature is removed.

Here is an example of a pattern which does not warn in the current model, but would warn with a simplified model. Assume that the code under `SomeFeatureIsSupported` is removed when the feature is unavailable.

```csharp
class FeatureConsumer {
    static void Run() {
        SomeFeatureProvider? some;
        if (Features.SomeFeatureIsSupported)
            some = new SomeFeatureProvider();
        OtherFeatureProvider other = new();
        Helper(some, other);
    }

    static void Helper(SomeFeatureProvider? some, OtherFeatureProvider other) {
        some?.Use(); // This callsite would warn with the simplified model.
        other.Use();
    }
}

[RequiresSomeFeature]
class SomeFeatureProvider {
    public void Use() {}
}

class OtherFeatureProvider {
    public void Use() {}
}
```

Note that the `SomeFeatureProvider` type and its `Use` method are kept, but the `Use` method will be rewritten to throw.

The simplified model would encourage the above to be rewritten as follows, resulting in the entire type `SomeFeatureProvider` being removed:

```csharp

class FeatureConsumer {
    static void Run() {
        if (Features.SomeFeatureIsSupported) {
            var some = new SomeFeatureProvider();
            some.Use();
        }
        OtherFeatureProvider other = new();
        other.Use();
    }
}
```

Perhaps we could introduce the simplified model as an optional strict mode for people who are interested in rewriting their code for maximal size savings.