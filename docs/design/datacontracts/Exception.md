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

```csharp
record struct ExceptionStackFrameInfo(
    TargetPointer Ip,
    TargetPointer MethodDesc,
    bool IsLastForeignExceptionFrame);
```

``` csharp
TargetPointer GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo, out TargetPointer thrownObjectHandle);
ExceptionData GetExceptionData(TargetPointer exceptionAddr);
IEnumerable<ExceptionStackFrameInfo> GetExceptionStackFrames(TargetPointer exceptionAddr);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Type | Purpose |
| --- | --- | --- | --- |
| `ExceptionInfo` | `PreviousNestedInfo` | nuint | Pointer to the previous nested `ExInfo` on the thread's exception chain |
| `ExceptionInfo` | `ThrownObject` | nuint | Object pointer of the exception currently being handled by this `ExInfo` |
| `Exception` | `_message` | nuint | `System.Exception._message` field (managed string) |
| `Exception` | `_innerException` | nuint | `System.Exception._innerException` field |
| `Exception` | `_stackTrace` | nuint | Either an `I1Array` of `StackTraceElement` entries or a combined `object[]` whose slot 0 is that `I1Array` |
| `Exception` | `_watsonBuckets` | nuint | `System.Exception._watsonBuckets` field |
| `Exception` | `_stackTraceString` | nuint | `System.Exception._stackTraceString` field |
| `Exception` | `_remoteStackTraceString` | nuint | `System.Exception._remoteStackTraceString` field |
| `Exception` | `_HResult` | int32 | `System.Exception._HResult` field |
| `Exception` | `_xcode` | int32 | Native exception code captured at throw |
| `Array` | `m_NumComponents` | uint32 | Number of components (bytes for `I1Array`, slots for `object[]`); array byte payload starts immediately after this descriptor |
| `StackTraceArrayHeader` | `Size` | uint32 | Number of `StackTraceElement` entries that follow this header in the `I1Array` payload |
| `StackTraceElement` | `Ip` | nuint | Captured native instruction pointer for the frame |
| `StackTraceElement` | `MethodDesc` | nuint | Pointer to the frame method's `MethodDesc` |
| `StackTraceElement` | `Flags` | int32 | `StackTraceElementFlags` bitmask (see Contract Constants below) |

Contracts used:
| Contract Name | Purpose |
| --- | --- |
| `Object` | Resolves the `MethodTable` address of the `_stackTrace` object |
| `RuntimeTypeSystem` | Tests `ContainsGCPointers` on that `MethodTable` to distinguish the combined `object[]` shape from the bare `I1Array` shape |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE` | int | Bit in `StackTraceElement::Flags` marking the last frame copied from a foreign (rethrown) stack trace. | `0x0001` |

``` csharp
TargetPointer GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo, out TargetPointer thrownObjectHandle)
{
    nextNestedExceptionInfo = target.ReadPointer(exceptionInfoAddr + /* ExceptionInfo::PreviousNestedInfo offset*/);
    thrownObjectHandle = target.ReadPointer(exceptionInfoAddr + /* ExceptionInfo::ThrownObject offset */);
    if (thrownObjectHandle == TargetPointer.Null)
    {
        return TargetPointer.Null;
    }
    return target.ReadPointer(thrownObjectHandle);
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

IEnumerable<ExceptionStackFrameInfo> GetExceptionStackFrames(TargetPointer exceptionAddr)
{
    if (exceptionAddr == TargetPointer.Null)
        throw new ArgumentNullException();

    // The exception's _stackTrace field holds either:
    //   1) a combined object[] (PTRArray) whose slot 0 is the I1Array payload and whose
    //      remaining slots are keep-alive references for dynamic / collectible methods, or
    //   2) the I1Array payload directly.
    TargetPointer stackTraceObj = target.ReadPointer(exceptionAddr + /* Exception::StackTrace offset */);
    if (stackTraceObj == TargetPointer.Null)
        yield break;

    TargetPointer mt = Object.GetMethodTableAddress(stackTraceObj);
    TypeHandle stackTraceHandle = RuntimeTypeSystem.GetTypeHandle(mt);

    TargetPointer i1ArrayAddr;
    if (RuntimeTypeSystem.ContainsGCPointers(stackTraceHandle))
    {
        // Combined PTRArray; slot 0 holds the I1Array pointer.
        TargetPointer dataPtr = target.ReadPointer(stackTraceObj + /* Array::DataPointer offset */);
        i1ArrayAddr = target.ReadPointer(dataPtr);
    }
    else
    {
        i1ArrayAddr = stackTraceObj;
    }

    if (i1ArrayAddr == TargetPointer.Null)
        yield break;

    // The I1Array DataPointer is the start of the byte payload, which is laid out as:
    //   { StackTraceArrayHeader header; StackTraceElement elements[header.Size]; }
    TargetPointer payload = target.ReadPointer(i1ArrayAddr + /* Array::DataPointer offset */);
    uint frameCount = target.Read<uint>(payload + /* StackTraceArrayHeader::Size offset */);
    if (frameCount == 0)
        yield break;

    ulong headerSize = /* StackTraceArrayHeader type size */;
    ulong elementSize = /* StackTraceElement type size */;
    TargetPointer cursor = payload + headerSize;
    for (uint i = 0; i < frameCount; i++)
    {
        TargetPointer ip = target.ReadPointer(cursor + /* StackTraceElement::Ip offset */);
        TargetPointer md = target.ReadPointer(cursor + /* StackTraceElement::MethodDesc offset */);
        int flags = target.Read<int>(cursor + /* StackTraceElement::Flags offset */);
        yield return new ExceptionStackFrameInfo(
            ip,
            md,
            (flags & STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE) != 0);
        cursor += elementSize;
    }
}

```
