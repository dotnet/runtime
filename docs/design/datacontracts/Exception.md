# Contract Exception

This contract is for getting information about exceptions in the process.

## APIs of contract

```csharp
record struct ExceptionData(
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
TargetPointer GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo);
ExceptionData GetExceptionData(TargetPointer exceptionAddr)
```

## Version 1

Data descriptors used:
- `ExceptionInfo`
- `Exception`

``` csharp
TargetPointer GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo)
{
    if (exceptionInfo == TargetPointer.Null)
        throw new InvalidArgumentException();

    nextNestedException = target.ReadPointer(exceptionInfo + /* ExceptionInfo::PreviousNestedInfo offset*/);
    TargetPointer thrownObjHandle = target.ReadPointer(exceptionInfo + /* ExceptionInfo::ThrownObject offset */);
    return = thrownObjHandle != TargetPointer.Null
        ? target.ReadPointer(thrownObjHandle)
        : TargetPointer.Null;
}

ExceptionData GetExceptionData(TargetPointer exceptionAddr)
{
    return new ExceptionData(
        target.ReadPointer(exceptionAddr + /* Exception::Message offset */),
        target.ReadPointer(exceptionAddr + /* Exception::InnerException offset */),
        target.ReadPointer(exceptionAddr + /* Exception::StackTrace offset */),
        target.ReadPointer(exceptionAddr + /* Exception::WatsonBuckets offset */),
        target.ReadPointer(exceptionAddr + /* Exception::StackTraceString offset */),
        target.ReadPointer(exceptionAddr + /* Exception::RemoteStackTraceString offset */),
        target.Read<int>(exceptionAddr + /* Exception::HResult offset */),
        target.Read<int>(exceptionAddr + /* Exception::XCode offset */),
    );
}
```
