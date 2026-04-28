# Contract CodeNotifications

This contract provides read/write access to the in-target JIT code notification
allowlist. The runtime consults this table when JIT-compiling or discarding a
method; if the (module, methodToken) pair is present with a non-zero flag set,
the runtime raises a `DEBUG_CODE_NOTIFICATION` event so the debugger/DAC can
observe JIT events for that method.

Unlike the [Notifications](Notifications.md) contract (which only decodes
events raised by the runtime), this contract writes into the target process
and may lazily allocate the notification table when needed.

## APIs of contract

``` csharp
/// Notification flag set. Mapped to/from the native `CLRDATA_METHNOTIFY_*` values at the
/// IXCLRData COM boundary (None=0, Generated=1, Discarded=2).
[Flags]
public enum CodeNotificationKind : uint { None = 0, Generated = 1, Discarded = 2 }

// Set the JIT code notification flags for a specific method.
void SetCodeNotification(TargetPointer module, uint methodToken, CodeNotificationKind flags);

// Get the JIT code notification flags for a specific method. Returns None for both an unset
// method and an unallocated in-target table — the cDAC cannot (and does not need to)
// distinguish the two, since "no notifications present" is the same observable state.
CodeNotificationKind GetCodeNotification(TargetPointer module, uint methodToken);

// Set notification flags for all methods in a module, or all methods if module is null.
void SetAllCodeNotifications(TargetPointer module, CodeNotificationKind flags);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Type | Purpose |
| --- | --- | --- | --- |
| `JITNotification` | `State` | uint16 | Notification flags (CLRDATA_METHNOTIFY_*) |
| `JITNotification` | `ClrModule` | nuint | Target pointer to the module |
| `JITNotification` | `MethodToken` | uint32 | Method metadata token |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `JITNotificationTable` | TargetPointer | Pointer to the `g_pNotificationTable` array of `JITNotification` entries |
| `JITNotificationTableSize` | uint32 | Maximum number of entries in the notification table (excluding bookkeeping) |

Contracts used: none

The JIT notification table is an array of `JITNotification` structs. Index 0 is reserved for
bookkeeping: its `MethodToken` field stores the current entry count (length). The table capacity
is a compile-time invariant exposed via the `JITNotificationTableSize` global, so slot 0's
`ClrModule` and `State` fields are unused. Actual entries start at index 1.

On Windows, the table starts as NULL (`g_pNotificationTable == 0`). On Unix, it is pre-allocated
at startup; the runtime's `new JITNotification[1001]` default-constructs every slot with
`state = 0`, `clrModule = 0`, `methodToken = 0`, so slot 0's length starts at 0 naturally.
The contract handles both cases uniformly:
- **GetCodeNotification** returns `CodeNotificationKind.None` when the table is NULL, which
  is the same value returned when the method is not registered. The cDAC does not distinguish
  "table absent" from "entry absent" — both are surfaced as "no notifications for this method".
  This is a deliberate simplification from the legacy DAC, which returned `E_OUTOFMEMORY` when
  `JITNotifications::IsActive()` was false; the information to the caller is semantically the
  same in both cases.
- **SetAllCodeNotifications** is a no-op when the table is NULL.
- **SetCodeNotification** with `CodeNotificationKind.None` is a no-op when the table is NULL.
- **SetCodeNotification** with a non-zero flag lazily allocates the table via `Target.AllocateMemory`,
  zero-fills it (so slot 0's length starts at 0), and writes the pointer back to
  `g_pNotificationTable`. If the in-target table is full, a `COMException` with
  `HResult == E_FAIL` is thrown, matching the legacy DAC's `SetNotification` failure path.
  If `AllocateMemory` is not available (e.g., when the debugger host does not support
  `ICLRDataTarget2`), a `NotImplementedException` is thrown.

This contract doesn't currently offer a capacity check, so consumers won't be able to
confirm in advance whether a batch of notification updates will all succeed. If a batch
would overflow the in-target table, the contract writes as many entries as fit and then
throws `COMException` with `HResult == E_FAIL` from the first `SetCodeNotification`
that cannot allocate a slot.

``` csharp
void SetCodeNotification(TargetPointer module, uint methodToken, CodeNotificationKind flags)
{
    // Read g_pNotificationTable pointer
    TargetPointer tablePointer = target.ReadPointer(
        target.ReadGlobalPointer("JITNotificationTable"));

    if (tablePointer == null)
    {
        if (flags == CodeNotificationKind.None) return; // nothing to clear
        // Lazily allocate via Target.AllocateMemory
        tablePointer = AllocateAndInitializeTable();
    }

    // Read bookkeeping from index 0 (length only; capacity comes from the global).
    uint length = Read<uint>(tablePointer + MethodTokenOffset);
    uint capacity = target.ReadGlobal<uint>("JITNotificationTableSize");
    ulong entriesBase = tablePointer + entrySize;

    if (flags == CodeNotificationKind.None)
    {
        // Find and clear the matching entry
    }
    else
    {
        // Find existing entry and update, or find free slot and insert.
        // If no free slot is found: throw COMException with HResult = E_FAIL.
    }
}

CodeNotificationKind GetCodeNotification(TargetPointer module, uint methodToken)
{
    // Returns CodeNotificationKind.None both when the table is NULL and when the method
    // is not registered. The cDAC does not distinguish these cases (legacy DAC did, via
    // E_OUTOFMEMORY vs. S_OK+None); the observable state is the same.
}

void SetAllCodeNotifications(TargetPointer module, CodeNotificationKind flags)
{
    // If table pointer is NULL, return (no-op)
    // Iterate all active entries; if module is non-null, filter by module
    // Set or clear each matching entry's flags.
    // When clearing (flags == None), trim trailing free entries from the stored length.
    // This deliberately diverges from JITNotifications::SetAllNotifications in
    // src/coreclr/vm/util.cpp, whose length algorithm can orphan entries from other modules
    // that sit past the trimmed length.
}
```
