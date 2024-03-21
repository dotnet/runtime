# Contract GCHandle

This contract allows decoding and reading of GCHandles. This will also include handle enumeration in the future

## Data structures defined by contract
``` csharp
```

## Apis of contract
``` csharp
TargetPointer GetObject(TargetPointer gcHandle);
```

## Version 1

``` csharp
TargetPointer GetObject(TargetPointer gcHandle)
{
    if (gcHandle == TargetPointer.Null)
        return TargetPointer.Null;
    return Target.ReadTargetPointer(gcHandle);
}
```
