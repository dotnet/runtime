# Contract FeatureFlags

This contract exposes whether optional runtime features are enabled in the target.

## APIs of contract

```csharp
public enum RuntimeFeature
{
    COMInterop,
    ComWrappers,
    ObjCMarshal,
    JavaMarshal,
    OnStackReplacement,
    PortableEntrypoints,
    Webcil,
}

// Returns true if the target runtime has the given feature enabled.
// A feature is considered disabled when its global is absent from the
// target descriptor or present with a zero value.
bool IsEnabled(RuntimeFeature feature);
```

## Version 1

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| FeatureCOMInterop | uint8 | Present (nonzero) when COM interop is enabled |
| FeatureComWrappers | uint8 | Present (nonzero) when ComWrappers is enabled |
| FeatureObjCMarshal | uint8 | Present (nonzero) when Objective-C marshalling is enabled |
| FeatureJavaMarshal | uint8 | Present (nonzero) when Java marshalling is enabled |
| FeatureOnStackReplacement | uint8 | Present (nonzero) when on-stack replacement is enabled |
| FeaturePortableEntrypoints | uint8 | Present (nonzero) when portable entrypoints are enabled |
| FeatureWebcil | uint8 | Present (nonzero) when Webcil is enabled |

Feature flag globals are **only present in the descriptor when the feature is enabled**. A missing global and a zero-valued global are both treated as "disabled".

```csharp
bool IsEnabled(RuntimeFeature feature)
{
    string? globalName = feature switch
    {
        RuntimeFeature.COMInterop           => "FeatureCOMInterop",
        RuntimeFeature.ComWrappers          => "FeatureComWrappers",
        RuntimeFeature.ObjCMarshal          => "FeatureObjCMarshal",
        RuntimeFeature.JavaMarshal          => "FeatureJavaMarshal",
        RuntimeFeature.OnStackReplacement   => "FeatureOnStackReplacement",
        RuntimeFeature.PortableEntrypoints  => "FeaturePortableEntrypoints",
        RuntimeFeature.Webcil               => "FeatureWebcil",
        _                                   => null,
    };
    return globalName is not null
        && TryReadGlobal<byte>(globalName, out byte? value)
        && value != 0;
}
```
