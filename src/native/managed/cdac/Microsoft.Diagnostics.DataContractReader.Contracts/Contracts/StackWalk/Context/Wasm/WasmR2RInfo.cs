// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;

/// <summary>
/// cDAC implementation of <see cref="IWasmR2RInfo"/>, backed by the ExecutionManager's
/// <see cref="WasmFunctionTableIndexLookup"/>.
/// </summary>
internal sealed class WasmR2RInfo : IWasmR2RInfo
{
    private readonly Target _target;
    private readonly IExecutionManager _executionManager;
    private readonly WasmFunctionTableIndexLookup _lookup;

    public WasmR2RInfo(Target target)
        : this(target, target.Contracts.ExecutionManager)
    {
    }

    internal WasmR2RInfo(Target target, IExecutionManager executionManager)
    {
        _target = target;
        _executionManager = executionManager;
        _lookup = new WasmFunctionTableIndexLookup(target);
    }

    public bool TryGetVirtualIPBase(uint frameIdentity, out ulong baseVirtualIP)
    {
        if ((frameIdentity & 1) == 0)
        {
            PortableEntryPoint portableEntryPoint =
                _target.ProcessedData.GetOrAdd<PortableEntryPoint>(new TargetPointer(frameIdentity));
            return _lookup.TryGetVirtualIPBase((uint)portableEntryPoint.ActualCode.Value, out baseVirtualIP);
        }

        if ((int)frameIdentity < 0)
        {
            uint runtimeFunctionIndex = (~frameIdentity) >> 1;
            return _lookup.TryGetCoreLibVirtualIPBase(runtimeFunctionIndex, out baseVirtualIP);
        }

        return _lookup.TryGetVirtualIPBase(frameIdentity >> 1, out baseVirtualIP);
    }

    public bool TryGetUnwindData(TargetCodePointer controlPC, out TargetPointer unwindDataAddress)
    {
        unwindDataAddress = TargetPointer.Null;
        if (!_executionManager.TryGetUnwindInfo(
            controlPC,
            out TargetPointer runtimeFunctionAddress,
            out TargetPointer unwindInfoBaseAddress))
        {
            return false;
        }

        RuntimeFunction runtimeFunction = _target.ProcessedData.GetOrAdd<RuntimeFunction>(runtimeFunctionAddress);
        unwindDataAddress = unwindInfoBaseAddress + runtimeFunction.UnwindData;
        return true;
    }
}
