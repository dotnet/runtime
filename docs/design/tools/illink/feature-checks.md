# Feature checks

The ILLink Roslyn Analyzer, ILLink, and ILCompiler respect [feature attributes](feature-attribute-semantics.md) which indicates that an API has requirements on various "features" or "capabilities" of the platform or toolchain. Some of the attributes are associated with APIs that serve as checks for the feature in question. This document specifies the kinds of check conditions that are supported by the various tools.

Note that "feature checks" are a subset of [feature switches](https://github.com/dotnet/designs/blob/main/accepted/2020/feature-switch.md). A "feature switch" has a read-only static property. A "feature check" has a read-only static `bool` property that is also associated with one of the "feature attributes". The mechanism that creates this association is not defined here (it is currently implementation-defined by each tool).

## Guarding calls to annotated APIs

This document will refer to a hypothetical feature check `Feature.IsSupported` as a stand-in for `RuntimeFeature.IsDynamicCodeSupported` as well as any other feature checks we add in the future. This check is associated with the attribute `RequiresFeatureAttribute`, a stand-in for `RequiresDynamicCodeAttribute` or the attribute associated with the feature check in question. Assume that there is an API `Feature.Run` annotated with `RequiresFeatureAttribute`:

```csharp
class Feature
{
    public static bool IsSupported => // ...

    [RequiresFeature]
    public static bool Run() {
        // ...
    }
}
```

The return type of `Run` is `bool` for illustration purposes in some of the guard patterns below. Calls to this API are only allowed if made in a guarded context, for example:

```csharp
if (Feature.IsSupported)
    Feature.Run(); // ok

Feature.Run(); // NOT ok, produces warning
```

The ILLink Roslyn Analyzer, ILLink, and ILCompiler understand various sets of such feature checks and attributes. The Roslyn analyzer doesn't warn for a call to a `RequiresFeature`-annotated API in a context that's guarded by a feature check, while ILLink and ILCompiler will entirely remove the branches guarded by a feature check for a disabled feature.

This document is meant to describe the supported usages where the warnings are silenced by all of these tools. We recommend using one of these patterns if you need to write code that guards a call to a feature-annotated API with a call to a feature check.

## Supported boolean expressions

The following boolean expressions may be used to guard a call to a `RequiresFeature`-annotated API in a way understood by all of the trimming tools. They can be used with the control-flow patterns described below.

### Direct check

```csharp
if (Feature.IsSupported)
    Feature.Run();
```

### Negation

```csharp
if (!Feature.IsSupported)
    return;

Feature.Run();
```

### Equality comparison with true/false
```csharp
if (Feature.IsSupported == true)
    Feature.Run();
```

```csharp
if (true == Feature.IsSupported)
    Feature.Run();
```

```csharp
if (Feature.IsSupported == false)
    throw null;

Feature.Run();
```

```csharp
if (false == Feature.IsSupported)
    return;

Feature.Run();
```

### Inequality comparison with true/false

```csharp
if (Feature.IsSupported != true)
    return;
    
Feature.Run();
```

```csharp
if (Feature.IsSupported != false)
    Feature.Run();
```

```csharp
if (true != Feature.IsSupported)
    return;

Feature.Run();
```

```csharp
if (false != Feature.IsSupported)
    Feature.Run();
```

### Pattern match is true/false

```csharp
if (Feature.IsSupported is true)
    Feature.Run();
```

```csharp
if (Feature.IsSupported is false)
    return;

Feature.Run();
```

### Pattern match is not true/false

```csharp
if (Feature.IsSupported is not true)
    return;

Feature.Run();
```

```csharp
if (Feature.IsSupported is not false)
    Feature.Run();
```

## Supported control-flow patterns

Any of the above boolean expressions can be used with the following control-flow patterns to guard a call to an annotated API:

### If/else statement guard
```csharp
if (Feature.IsSupported)
    Feature.Run();
```

```csharp
if (!Feature.IsSupported)
{
    // ...
}
else
{
    Feature.Run();
}
```

```csharp
if (OtherCondition)
{
}
else if (Feature.IsSupported)
{
    Feature.Run();
}
```

### Short-circuiting boolean guard
```csharp
var a = Feature.IsSupported && Feature.Run();
```

```csharp
var a = !Feature.IsSupported || Feature.Run();
```

### Ternary operator guard
```csharp
_ = Feature.IsSupported ? Feature.Run() : true;
```

```csharp
_ = !Feature.IsSupported ? true : Feature.Run();
```

### Early throw

```csharp
if (!Feature.IsSupported)
    throw new Exception ();

Feature.Run();
```

### Early return

```csharp
if (!Feature.IsSupported)
    return;

Featuer.Run ();
```

## Unsupported guard patterns

The following guard patterns are not supported by all of the tools. Using one of these patterns will result in warnings being produced by ILLink or ILCompiler. If you need to use one of these, it is safe to suppress the warning by using `UnconditionalSuppressMessageAttribute` as long as the call to the annotated feature is unreachable at runtime, but it is recommended to use one of the supported guard patterns instead.

### Boolean and/or

The short-circuiting boolean operators create branches in the control-flow graph that don't always look straightforward in IL. Sometimes (in Debug mode) these compile into a multiple branches that compute a temporary local which stores the computed condition and is used for the conditional branch. ILLink/ILCompiler aren't guaranteed to do branch removal based on constant propagation of this temporary value, so the following patterns aren't supported guards:

```csharp
if (Feature.IsSupported && OtherCondition)
    Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
```

```csharp
if (OtherCondition && Feature.IsSupported)
    Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
```

```csharp
if (!Feature.IsSupported || OtherCondition)
    return;

Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
```

```csharp
if (OtherCondition || !Feature.IsSupported)
    return;

Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
```

### DoesNotReturnIfAttribute
`DoesNotReturnIfAttribute` influences the analysis done by the Roslyn analyzer, allowing the use of `Debug.Assert` for example.

```csharp
Debug.Assert (Feature.IsSupported);

Feature.Run(); // OK in analyzer, warns in ILLLink/ILCompiler
```

However, ILLink and ILCompiler don't optimize away branches based on `DoesNotReturnIfAttribute`, because the attribute doesn't have runtime semantics, and an incorrectly-applied attribute could result in the guarded branch being reachable at runtime. Or, in the case of `Debug.Assert`, if the assert is used incorrectly, the guarded code may still be reachable at runtime in `Release` builds.

Here are some more examples. These are not recommended because they are not supported by ILLink/ILCompiler:

```csharp
DoesNotReturnIfFalse (Feature.IsSupported);

Feature.Run(); // OK in analyzer, warns in ILLink/ILCompiler

static void DoesNotReturnIfFalse([DoesNotReturnIf(false)] condition) {
    // ...
}
```

```csharp
DoesNotReturnIfTrue (!Feature.IsSupported);

Feature.Run(); // OK in analyzer, warns in ILLink/ILCompiler

static void DoesNotReturnIfTrue([DoesNotReturnIf(true)] condition) {
    // ...
}
```

### DoesNotReturnAttribute

ILLink and ILCompiler dont't optimize branches away based on `DoesNotReturnAttribute`. This is similar to `DoesNotReturnIfAttribute`.

```csharp
if (!Feature.IsSupported)
    DoesNotReturn ();

Feature.Run(); // OK in analyzer, warns in ILLink/ILCompiler

[DoesNotReturn]
static void DoesNotReturn() {
    // ...
}
```

### Compiler-generated state machines

`async` or iterator methods produce state machines that are not understood by ILLink and ILCompiler, so branch removal may not work in such methods even if the analyzer does not produce warnings. It is recommended to avoid such using feature checks in async or iterator methods. For example:

```csharp
async Task AsyncMethod ()
{
    if (!Feature.IsSupported)
        return;

    await Task.Yield();

    Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
}
```

```csharp
IEnumerable<int> StateFlowsAcrossYield ()
{
    if (!Feature.IsSupported)
        yield break;

    yield return 0;

    Feature.Run(); // OK in analyzer, may warn in ILLink/ILCompiler
}
```
