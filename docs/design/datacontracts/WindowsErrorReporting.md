# Contract WindowsErrorReporting

This contract reads Windows Error Reporting data for a runtime thread.

## APIs of contract

``` csharp
// Returns the GenericModeBlock Watson bucket data for the specified thread.
// Returns an empty array when the thread has no Watson buckets.
byte[] GetWatsonBuckets(TargetPointer threadPointer);
```

## Version 1

Data descriptors used:

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Exception` | `WatsonBuckets` | Pointer to exception Watson buckets |
| `ExceptionInfo` | `ThrownObject` | Pointer to the active exception object |
| `ExceptionInfo` | `ExceptionWatsonBucketTrackerBuckets` | Pointer to Watson unhandled buckets on non-Unix |
| `Thread` | `ExceptionTracker` | Pointer to exception tracking information |
| `Thread` | `UEWatsonBucketTrackerBuckets` | Pointer to thread Watson buckets data (optional, Windows only) |

Global variables used:

| Global Name | Type | Purpose |
| --- | --- | --- |
| `SizeOfGenericModeBlock` | uint32 | Size of the GenericModeBlock struct |

Contracts used:

| Contract |
| --- |
| Object |

``` csharp
byte[] GetWatsonBuckets(TargetPointer threadPointer)
{
    TargetPointer readFrom;
    Data.Thread thread = target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);
    TargetPointer exceptionTrackerPtr = target.ReadPointer(thread.ExceptionTracker);
    Data.ExceptionInfo? exceptionInfo = exceptionTrackerPtr == TargetPointer.Null
        ? null
        : target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionTrackerPtr);
    if (exceptionInfo == null)
        return Array.Empty<byte>();

    TargetPointer thrownObject = exceptionInfo.ThrownObject;
    if (thrownObject != TargetPointer.Null)
    {
        Data.Exception exception = target.ProcessedData.GetOrAdd<Data.Exception>(thrownObject);
        if (exception.WatsonBuckets != TargetPointer.Null)
        {
            readFrom = target.Contracts.Object.GetArrayData(exception.WatsonBuckets, out _, out _, out _);
        }
        else
        {
            readFrom = thread.UEWatsonBucketTrackerBuckets ?? TargetPointer.Null;
            if (readFrom == TargetPointer.Null)
            {
                readFrom = exceptionInfo.ExceptionWatsonBucketTrackerBuckets ?? TargetPointer.Null;
            }
            else
            {
                return Array.Empty<byte>();
            }
        }
    }
    else
    {
        readFrom = thread.UEWatsonBucketTrackerBuckets ?? TargetPointer.Null;
    }

    if (readFrom == TargetPointer.Null)
        return Array.Empty<byte>();

    byte[] buckets = new byte[target.ReadGlobal<uint>("SizeOfGenericModeBlock")];
    target.ReadBuffer(readFrom, buckets);
    return buckets;
}
```
