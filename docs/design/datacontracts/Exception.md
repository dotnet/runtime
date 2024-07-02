# Contract Exception

This contract is for getting information about exceptions in the process.

## APIs of contract

```csharp
record struct ExceptionObjectData(
    TargetPointer Message,
    TargetPointer InnerException,
    TargetPointer StackTrace,
    TargetPointer WatsonBuckets,
    TargetPointer StackTraceString,
    TargetPointer RemoteStackTraceString,
    int HResult,
    int XCode);
```

``` csharp
TargetPointer GetExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException);
ExceptionObjectData GetExceptionObjectData(TargetPointer objectAddress);
```

## Version 1

Data descriptors used:
- `ExceptionInfo`
- `ExceptionObject`

``` csharp
TargetPointer GetExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException)
{
    if (exception == TargetPointer.Null)
        throw new InvalidArgumentException();

    nextNestedException = target.ReadPointer(exception + /* ExceptionInfo::PreviousNestedInfo offset*/);
    TargetPointer thrownObjHandle = target.ReadPointer(exception + /* ExceptionInfo::ThrownObject offset */);
    return = thrownObjHandle != TargetPointer.Null
        ? target.ReadPointer(thrownObjHandle)
        : TargetPointer.Null;
}

ExceptionObjectData GetExceptionObjectData(TargetPointer objectAddress)
{
    return new ExceptionObjectData(
        target.ReadPointer(objectAddress + /* ExceptionObject::Message offset */),
        target.ReadPointer(objectAddress + /* ExceptionObject::InnerException offset */),
        target.ReadPointer(objectAddress + /* ExceptionObject::StackTrace offset */),
        target.ReadPointer(objectAddress + /* ExceptionObject::WatsonBuckets offset */),
        target.ReadPointer(objectAddress + /* ExceptionObject::StackTraceString offset */),
        target.ReadPointer(objectAddress + /* ExceptionObject::RemoteStackTraceString offset */),
        target.Read<int>(objectAddress + /* ExceptionObject::HResult offset */),
        target.Read<int>(objectAddress + /* ExceptionObject::XCode offset */),
    );
}
```
