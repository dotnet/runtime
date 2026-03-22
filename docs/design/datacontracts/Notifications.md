# Contract Notifications

This contract is for debugger notifications.

## APIs of contract

``` csharp
// Set the GC notification for condemned generations
// The argument is a bitmask where the i-th bit set represents the i-th generation.
void SetGcNotification(int condemnedGeneration);

// Returns the notification type encoded in the first element of the exception information array.
NotificationType GetNotificationType(ReadOnlySpan<TargetPointer> exceptionInformation);

// Parse methods extract fields from the exception information array.
// The caller must verify the notification type with GetNotificationType before calling these.
void ParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress);
void ParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress);
void ParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress);
void ParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress);
void ParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData);
void ParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset);
```

## Version 1

Data descriptors used: none

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `GcNotificationFlags` | TargetPointer | Global flag for storing GC notification data |

Contracts used: none

``` csharp
void SetGcNotification(int condemnedGeneration)
{
    TargetPointer pGcNotificationFlags = _target.ReadGlobalPointer("GcNotificationFlags");
    uint currentFlags = _target.Read<uint>(pGcNotificationFlags);
    if (condemnedGeneration == 0)
        _target.Write<uint>(pGcNotificationFlags, 0);
    else
    {
        _target.Write<uint>(pGcNotificationFlags, (uint)(currentFlags | condemnedGeneration));
    }
}
```
