# Contract Notifications

This contract is for decoding debugger notifications raised by the runtime.

## APIs of contract

``` csharp
// Set the GC notification for condemned generations
// The argument is a bitmask where the i-th bit set represents the i-th generation.
void SetGcNotification(int condemnedGeneration);

// Parses the exception information array into a typed notification object.
// Returns false if the notification type is unknown. Pattern match on the result to access notification-specific fields.
bool TryParseNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out NotificationData? notification);
```

Management of the JIT code-notification allowlist is a separate contract, see
[CodeNotifications](CodeNotifications.md).

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

public abstract record NotificationData(NotificationType Type);
public record ModuleLoadNotificationData(TargetPointer ModuleAddress) : NotificationData(NotificationType.ModuleLoad);
public record ModuleUnloadNotificationData(TargetPointer ModuleAddress) : NotificationData(NotificationType.ModuleUnload);
public record JitNotificationData(TargetPointer MethodDescAddress, TargetPointer NativeCodeAddress) : NotificationData(NotificationType.Jit2);
public record ExceptionNotificationData(TargetPointer ThreadAddress) : NotificationData(NotificationType.Exception);
public record GcNotificationData(GcEventData EventData, bool IsSupportedEvent) : NotificationData(NotificationType.Gc);
public record ExceptionCatcherEnterNotificationData(TargetPointer MethodDescAddress, uint NativeOffset) : NotificationData(NotificationType.ExceptionCatcherEnter);

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

private enum NativeNotificationType : uint
{
    ModuleLoad = 1,
    ModuleUnload = 2,
    Exception = 5,
    Gc = 6,
    ExceptionCatcherEnter = 7,
    Jit2 = 8,
}

bool TryParseNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out NotificationData? notification)
{
    if (exceptionInformation.IsEmpty)
    {
        notification = null;
        return false;
    }

    notification = (NativeNotificationType)(uint)exceptionInformation[0].Value switch
    {
        NativeNotificationType.ModuleLoad => new ModuleLoadNotificationData(exceptionInformation[1]),
        NativeNotificationType.ModuleUnload => new ModuleUnloadNotificationData(exceptionInformation[1]),
        NativeNotificationType.Jit2 => new JitNotificationData(exceptionInformation[1], exceptionInformation[2]),
        NativeNotificationType.Exception => new ExceptionNotificationData(exceptionInformation[1]),
        NativeNotificationType.Gc => ParseGcNotification(exceptionInformation),
        NativeNotificationType.ExceptionCatcherEnter => new ExceptionCatcherEnterNotificationData(exceptionInformation[1], (uint)exceptionInformation[2].Value),
        _ => null,
    };

    return notification is not null;
}
```
