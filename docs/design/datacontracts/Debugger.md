# Contract Debugger

This contract is for reading debugger state from the target process, including initialization status, configuration flags, metadata update state, and JIT attach state.

## APIs of contract

```csharp
record struct DebuggerData(uint DefinesBitField, uint MDStructuresVersion);
```

```csharp
bool TryGetDebuggerData(out DebuggerData data);
int GetAttachStateFlags();
bool MetadataUpdatesApplied();
void RequestSyncAtEvent();
void SetSendExceptionsOutsideOfJMC(bool sendExceptionsOutsideOfJMC);
TargetPointer GetDebuggerControlBlockAddress();
void EnableGCNotificationEvents(bool fEnable);
```

## Version 1

The contract depends on the following globals

| Global Name | Type | Description |
| --- | --- | --- |
| `Debugger` | TargetPointer | Address of the pointer to the Debugger instance (`&g_pDebugger`) |
| `CLRJitAttachState` | TargetPointer | Pointer to the CLR JIT attach state flags |
| `MetadataUpdatesApplied` | TargetPointer | Pointer to the g_metadataUpdatesApplied flag |

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
| `DebuggerRCThread` | `DCB` | Pointer to `DebuggerIPCControlBlock` |

```csharp
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
    int leftSideInitialized = target.Read<int>(debuggerPtr + /* Debugger::LeftSideInitialized offset */);
    if (leftSideInitialized == 0)
        return false;
    data = new DebuggerData(
        DefinesBitField: target.Read<uint>(debuggerPtr + /* Debugger::Defines offset */),
        MDStructuresVersion: target.Read<uint>(debuggerPtr + /* Debugger::MDStructuresVersion offset */));
    return true;
}

int GetAttachStateFlags()
{
    TargetPointer addr = target.ReadGlobalPointer("CLRJitAttachState");
    return (int)target.Read<uint>(addr);
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
```
