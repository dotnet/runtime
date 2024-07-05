# Contract Thread

This contract is for getting information about exceptions in the process.

## APIs of contract

``` csharp
TargetPointer GetExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException);
```

## Version 1

Data descriptors used:
- `ExceptionInfo`

``` csharp
TargetPointer GetExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException)
{
    if (exception == TargetPointer.Null)
        throw new InvalidArgumentException();

    nextNestedException = target.ReadPointer(address + /* ExceptionInfo::PreviousNestedInfo offset*/);
    TargetPointer thrownObjHandle = target.ReadPointer(address + /* ExceptionInfo::ThrownObject offset */);
    return = thrownObjHandle != TargetPointer.Null
        ? target.ReadPointer(thrownObjHandle)
        : TargetPointer.Null;
}
```
