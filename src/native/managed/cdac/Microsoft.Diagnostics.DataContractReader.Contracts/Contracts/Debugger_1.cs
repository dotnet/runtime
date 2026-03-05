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

    bool IDebugger.IsLeftSideInitialized()
    {
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtr == TargetPointer.Null)
            return false;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        return debugger.LeftSideInitialized != 0;
    }

    uint IDebugger.GetDefinesBitField()
    {
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        return debugger.Defines;
    }

    uint IDebugger.GetMDStructuresVersion()
    {
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        return debugger.MDStructuresVersion;
    }

    int IDebugger.GetAttachStateFlags()
    {
        if (_target.TryReadGlobalPointer("CLRJitAttachState", out TargetPointer? addr))
        {
            return (int)_target.Read<uint>(addr.Value.Value);
        }
        return 0;
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
