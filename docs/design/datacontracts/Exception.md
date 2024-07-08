# Contract Exception

This contract is for getting information about exceptions in the process.

## APIs of contract

```csharp
record struct ManagedExceptionData(
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
ManagedExceptionData GetManagedExceptionData(TargetPointer managedException)
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

ManagedExceptionData GetManagedExceptionData(TargetPointer managedException)
{
    return new ManagedExceptionData(
        target.ReadPointer(objectAddress + /* Exception::Message offset */),
        target.ReadPointer(objectAddress + /* Exception::InnerException offset */),
        target.ReadPointer(objectAddress + /* Exception::StackTrace offset */),
        target.ReadPointer(objectAddress + /* Exception::WatsonBuckets offset */),
        target.ReadPointer(objectAddress + /* Exception::StackTraceString offset */),
        target.ReadPointer(objectAddress + /* Exception::RemoteStackTraceString offset */),
        target.Read<int>(objectAddress + /* Exception::HResult offset */),
        target.Read<int>(objectAddress + /* Exception::XCode offset */),
    );
}
```
