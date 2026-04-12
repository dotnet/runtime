// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Debugger_1 : IDebugger
{
    private enum DebuggerControlFlag_1 : uint
    {
        PendingAttach = 0x0100,
        Attached = 0x0200,
    }

    private readonly Target _target;

    internal Debugger_1(Target target)
    {
        _target = target;
    }

    bool IDebugger.TryGetDebuggerData(out DebuggerData data)
    {
        data = default;
        TargetPointer debuggerPtrPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtrPtr == TargetPointer.Null)
            return false;

        TargetPointer debuggerPtr = _target.ReadPointer(debuggerPtrPtr);
        if (debuggerPtr == TargetPointer.Null)
            return false;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        data = new DebuggerData(debugger.LeftSideInitialized != 0, debugger.Defines, debugger.MDStructuresVersion);
        return true;
    }

    int IDebugger.GetAttachStateFlags()
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CLRJitAttachState);
        return (int)_target.Read<uint>(addr.Value);
    }

    void IDebugger.MarkDebuggerAttachPending()
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CORDebuggerControlFlags);
        uint currentFlags = _target.Read<uint>(addr.Value);
        _target.Write<uint>(addr.Value, currentFlags | (uint)DebuggerControlFlag_1.PendingAttach);
    }

    void IDebugger.MarkDebuggerAttached(bool fAttached)
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CORDebuggerControlFlags);
        uint currentFlags = _target.Read<uint>(addr.Value);
        if (fAttached)
        {
            _target.Write<uint>(addr.Value, currentFlags | (uint)DebuggerControlFlag_1.Attached);
        }
        else
        {
            _target.Write<uint>(addr.Value, currentFlags & ~((uint)DebuggerControlFlag_1.Attached | (uint)DebuggerControlFlag_1.PendingAttach));
        }
    }

    bool IDebugger.MetadataUpdatesApplied()
    {
        if (_target.TryReadGlobalPointer(Constants.Globals.MetadataUpdatesApplied, out TargetPointer? addr))
        {
            return _target.Read<byte>(addr.Value.Value) != 0;
        }
        return false;
    }
}
