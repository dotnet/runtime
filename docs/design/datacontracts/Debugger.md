# Contract Debugger

This contract is for reading debugger state from the target process, including initialization status, configuration flags, metadata update state, and JIT attach state.

## APIs of contract

```csharp
record struct DebuggerData(bool IsLeftSideInitialized, uint DefinesBitField, uint MDStructuresVersion);
```

```csharp
enum HijackKind
{
   None,
   UnhandledException,
   Other
}
```

```csharp
bool TryGetDebuggerData(out DebuggerData data);
int GetAttachStateFlags();
void MarkDebuggerAttachPending();
void MarkDebuggerAttached(bool fAttached);
bool MetadataUpdatesApplied();
void RequestSyncAtEvent();
void SetSendExceptionsOutsideOfJMC(bool sendExceptionsOutsideOfJMC);
TargetPointer GetDebuggerControlBlockAddress();
void EnableGCNotificationEvents(bool fEnable);
HijackKind GetHijackKind(TargetCodePointer controlPC);
TargetPointer PrepareExceptionHijack(byte[] context, TargetPointer vmThread, byte[]? exceptionRecord, int reason, TargetPointer userData)
```

## Version 1

The contract depends on the following globals

| Global Name | Type | Description |
| --- | --- | --- |
| `Debugger` | TargetPointer | Address of the pointer to the Debugger instance (`&g_pDebugger`) |
| `CLRJitAttachState` | TargetPointer | Pointer to the CLR JIT attach state flags |
| `CORDebuggerControlFlags` | TargetPointer | Pointer to `g_CORDebuggerControlFlags` |
| `MetadataUpdatesApplied` | TargetPointer | Pointer to the g_metadataUpdatesApplied flag |
| `MaxHijackFunctions` | uint32 | Number of entries in the hijack function array. |

The contract additionally depends on these data descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Debugger` | `LeftSideInitialized` | Whether the left-side debugger infrastructure is initialized |
| `Debugger` | `Defines` | Bitfield of compile-time debugger feature defines |
| `Debugger` | `MDStructuresVersion` | Version of metadata data structures |
| `Debugger` | `RCThread` | Pointer to `DebuggerRCThread` |
| `Debugger` | `RSRequestedSync` | Sync-at-event request flag |
| `Debugger` | `SendExceptionsOutsideOfJMC` | Exception delivery policy flag |
| `Debugger` | `GCNotificationEventsEnabled` | Whether GC notification events are enabled |
| `Debugger` | `RgHijackFunction` | Pointer to the runtime's array of hijack-stub address ranges. |
| `DebuggerRCThread` | `DCB` | Pointer to `DebuggerIPCControlBlock` |
| `MemoryRange` | `StartAddress` | Inclusive start address of the range |
| `MemoryRange` | `Size` | Size of the range in bytes; the range covers `[StartAddress, StartAddress + Size)` |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `UnhandledExceptionHijackIndex` | uint | Index of unhandled exception hijack memory range. | `0` |

```csharp

private enum DebuggerControlFlag_1 : uint
{
    PendingAttach = 0x0100,
    Attached = 0x0200,
}

bool TryGetDebuggerData(out DebuggerData data)
{
    data = default;
    // The Debugger global points to g_pDebugger (a pointer-to-pointer).
    // First read gets the address of g_pDebugger, second dereferences it.
    TargetPointer debuggerPtrPtr = target.ReadGlobalPointer("Debugger");
    if (debuggerPtrPtr == TargetPointer.Null)
        return false;
    TargetPointer debuggerPtr = target.ReadPointer(debuggerPtrPtr);
    if (debuggerPtr == TargetPointer.Null)
        return false;
    bool leftSideInitialized = target.Read<int>(debuggerPtr + /* Debugger::LeftSideInitialized offset */) != 0;
    data = new DebuggerData(
        IsLeftSideInitialized: leftSideInitialized,
        DefinesBitField: target.Read<uint>(debuggerPtr + /* Debugger::Defines offset */),
        MDStructuresVersion: target.Read<uint>(debuggerPtr + /* Debugger::MDStructuresVersion offset */));
    return true;
}

int GetAttachStateFlags()
{
    TargetPointer addr = target.ReadGlobalPointer("CLRJitAttachState");
    return (int)target.Read<uint>(addr);
}

void MarkDebuggerAttachPending()
{
    /* OR global "CORDebuggerControlFlags" with PendingAttach flag */;
}

void MarkDebuggerAttached(bool fAttached)
{
    // if fAttached is true, OR global "CORDebuggerControlFlags" with Attached flag
    // otherwise clear both Attached and PendingAttach flags
}

bool MetadataUpdatesApplied()
{
    if (target.TryReadGlobalPointer("MetadataUpdatesApplied", out TargetPointer addr))
        return target.Read<byte>(addr) != 0;
    return false;
}

void RequestSyncAtEvent()
{
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return;

    target.Write<int>(debuggerAddress + /* Debugger::RSRequestedSync offset */, 1);
}

void SetSendExceptionsOutsideOfJMC(bool sendExceptionsOutsideOfJMC)
{
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return;

    target.Write<int>(
        debuggerAddress + /* Debugger::SendExceptionsOutsideOfJMC offset */,
        sendExceptionsOutsideOfJMC ? 1 : 0);
}

TargetPointer GetDebuggerControlBlockAddress()
{
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return TargetPointer.Null;

    TargetPointer rcThread = target.ReadPointer(debuggerAddress + /* Debugger::RCThread offset */);
    if (rcThread == TargetPointer.Null)
        return TargetPointer.Null;

    return target.ReadPointer(rcThread + /* DebuggerRCThread::DCB offset */);
}

void EnableGCNotificationEvents(bool fEnable)
{
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return;

    target.Write<int>(
        debuggerAddress + /* Debugger::GCNotificationEventsEnabled offset */,
        fEnable ? 1 : 0);
}

HijackKind GetHijackKind(TargetCodePointer controlPC)
{
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return HijackKind.None;

    TargetPointer rgHijack = target.ReadPointer(
        debuggerAddress + /* Debugger::RgHijackFunction offset */);
    if (rgHijack == TargetPointer.Null)
        return HijackKind.None;

    uint maxHijackFunctions = target.ReadGlobal<uint>("MaxHijackFunctions");
    if (maxHijackFunctions == 0)
        return HijackKind.None;

    uint stride = // Size of one MemoryRange entry

    for (uint i = 0; i < maxHijackFunctions; i++)
    {
        TargetPointer entryAddress = rgHijack + (ulong)(i * stride);
        TargetPointer start = target.ReadPointer(
            entryAddress + /* MemoryRange::StartAddress offset */);
        TargetNUInt size = target.Read<TargetNUInt>(
            entryAddress + /* MemoryRange::Size offset */);

        ulong end = start.Value + size.Value;
        if (controlPC.Value >= start.Value && controlPC.Value < end)
        {
            return (i == UnhandledExceptionHijackIndex)
                ? HijackKind.UnhandledException
                : HijackKind.Other;
        }
    }
    return HijackKind.None;
}

private TargetPointer GetHijackAddress()
{
    // Returns the start address of the unhandled-exception hijack function
    // (index UnhandledExceptionHijackIndex == 0 in the RgHijackFunction array).
    if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
        return TargetPointer.Null;

    TargetPointer rgHijack = target.ReadPointer(
        debuggerAddress + /* Debugger::RgHijackFunction offset */);
    if (rgHijack == TargetPointer.Null)
        return TargetPointer.Null;

    uint maxHijackFunctions = target.ReadGlobal<uint>("MaxHijackFunctions");
    if (UnhandledExceptionHijackIndex >= maxHijackFunctions)
        return TargetPointer.Null;

    uint stride = // Size of one MemoryRange entry
    TargetPointer entryAddress = rgHijack + (ulong)(UnhandledExceptionHijackIndex * stride);
    return target.ReadPointer(entryAddress + /* MemoryRange::StartAddress offset */);
}

TargetPointer PrepareExceptionHijack(byte[] context, TargetPointer vmThread, byte[]? exceptionRecord, int reason, TargetPointer userData)
{
    // Finds hijack address via GetHijackAddress.
    // Writes the exception record and context into the target stack as necessary.
    // Places the arguments to ExceptionHijackWorker as dictated by the native ABI.
    // Mutates stack pointer and context as necessary.
}
```
