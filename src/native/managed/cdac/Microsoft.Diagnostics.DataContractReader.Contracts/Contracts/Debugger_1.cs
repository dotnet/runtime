// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Debugger_1 : IDebugger
{
    private readonly Target _target;

    internal Debugger_1(Target target)
    {
        _target = target;
    }

    private bool TryGetDebuggerAddress(out TargetPointer debuggerAddress)
    {
        debuggerAddress = TargetPointer.Null;

        TargetPointer debuggerPtrPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtrPtr == TargetPointer.Null)
            return false;

        debuggerAddress = _target.ReadPointer(debuggerPtrPtr);
        return debuggerAddress != TargetPointer.Null;
    }

    bool IDebugger.TryGetDebuggerData(out DebuggerData data)
    {
        data = default;
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return false;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        if (debugger.LeftSideInitialized == 0)
            return false;

        data = new DebuggerData(debugger.Defines, debugger.MDStructuresVersion);
        return true;
    }

    int IDebugger.GetAttachStateFlags()
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CLRJitAttachState);
        return (int)_target.Read<uint>(addr.Value);
    }

    bool IDebugger.MetadataUpdatesApplied()
    {
        if (_target.TryReadGlobalPointer(Constants.Globals.MetadataUpdatesApplied, out TargetPointer? addr))
        {
            return _target.Read<byte>(addr.Value.Value) != 0;
        }
        return false;
    }

    void IDebugger.RequestSyncAtEvent()
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.SetField(_target, nameof(Data.Debugger.RSRequestedSync), 1);
    }

    void IDebugger.SetSendExceptionsOutsideOfJMC(bool sendExceptionsOutsideOfJMC)
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.SetField(_target, nameof(Data.Debugger.SendExceptionsOutsideOfJMC), sendExceptionsOutsideOfJMC ? 1 : 0);
    }

    TargetPointer IDebugger.GetDebuggerControlBlockAddress()
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return TargetPointer.Null;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        TargetPointer rcThread = debugger.RCThread;
        if (rcThread == TargetPointer.Null)
            return TargetPointer.Null;

        Data.DebuggerRCThread debuggerRcThread = _target.ProcessedData.GetOrAdd<Data.DebuggerRCThread>(rcThread);
        return debuggerRcThread.DCB;
    }

    void IDebugger.EnableGCNotificationEvents(bool fEnable)
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.SetField(_target, nameof(Data.Debugger.GCNotificationEventsEnabled), fEnable ? 1 : 0);
    }
}
