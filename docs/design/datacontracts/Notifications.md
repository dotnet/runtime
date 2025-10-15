# Contract Notifications

This contract is for debugger notifications.

## APIs of contract

``` csharp
// Set the GC notification for condemned generations
// The argument is a bitmask where the i-th bit set represents the i-th generation.
void SetGcNotification(int condemnedGeneration);
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
