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
bool ParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData);
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
public enum GcEventType
{
    MarkEnd = 1,
}

public record struct GcEventData(GcEventType EventType, int CondemnedGeneration);

public enum NotificationType
{
    Unknown = 0,
    ModuleLoad,
    ModuleUnload,
    Jit2,
    Exception,
    Gc,
    ExceptionCatcherEnter,
}

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

NotificationType GetNotificationType(ReadOnlySpan<TargetPointer> exceptionInformation)
{
    if (exceptionInformation.IsEmpty)
        return NotificationType.Unknown;
    // switch based on exceptionInformation[0].Value
}

void ParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
{
    moduleAddress = exceptionInformation[1];
}

void ParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
{
    moduleAddress = exceptionInformation[1];
}

void ParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress)
{
    methodDescAddress = exceptionInformation[1];
    nativeCodeAddress = exceptionInformation[2];
}

void ParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress)
{
    threadAddress = exceptionInformation[1];
}

bool ParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData)
{
    GcEventType eventType = (GcEventType)(uint)exceptionInformation[1].Value;
    eventData = new GcEventData(eventType, (int)(uint)exceptionInformation[2].Value);
    return eventType == GcEventType.MarkEnd;
}

void ParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset)
{
    methodDescAddress = exceptionInformation[1];
    nativeOffset = (uint)exceptionInformation[2].Value;
}
```
