# Contract GCCover

This contract encapsulates support for [GCCover](../coreclr/jit/investigate-stress.md) (GC stress testing) in the runtime.

## APIs of contract

```csharp
public virtual TargetPointer? GetGCCoverageInfo(NativeCodeVersionHandle codeVersionHandle);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| NativeCodeVersionNode | GCCoverageInfo | pointer to GC coverage info |

Contracts used:
| Contract Name |
| --- |
| CodeVersions |
| RuntimeTypeSystem |

### Getting GCCoverageInfo for a NativeCodeVersion
```csharp
public virtual TargetPointer? GetGCCoverageInfo(NativeCodeVersionHandle codeVersionHandle);
```
1. If `codeVersionHandle` is synthetic, attempt to read the GCCoverageInfo off of the MethodDesc using the RuntimeTypeSystem contract.
2. If `codeVersionHandle` is explicit, fetch the `NativeCodeVersionNode` and attempt to read the `GCCoverageInfo` pointer. This value will only exist on targets with `HAVE_GCCOVER` enabled. If this value exists return it. Otherwise return null.
