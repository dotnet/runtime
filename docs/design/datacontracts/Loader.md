# Contract Thread

This contract is for reading and iterating the threads of the process.

## APIs of contract

``` csharp
readonly struct ModuleHandle
{
    // Opaque handle - no public members
}
```

``` csharp
ModuleHandle GetModuleHandle(TargetPointer);

TargetPointer[] GetLookupTables(ModuleHandle handle);
```

## Version 1

Data descriptors used:
- `Module`

``` csharp
ModuleHandle GetModuleHandle(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}
```
