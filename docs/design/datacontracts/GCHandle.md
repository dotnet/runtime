# Contract GCHandle

This contract allows decoding and reading of GCHandles. This will also include handle enumeration in the future

## Data structures defined by contract
``` csharp
struct DacGCHandle
{
    DacGCHandle(TargetPointer value) { Value = value; }
    TargetPointer Value;
}
```

## Apis of contract
``` csharp
TargetPointer GetObject(DacGCHandle gcHandle);
```

## Version 1

``` csharp
TargetPointer GetObject(DacGCHandle gcHandle)
{
    if (gcHandle.Value == TargetPointer.Null)
        return TargetPointer.Null;
    return Target.ReadTargetPointer(gcHandle.Value);
}
```
