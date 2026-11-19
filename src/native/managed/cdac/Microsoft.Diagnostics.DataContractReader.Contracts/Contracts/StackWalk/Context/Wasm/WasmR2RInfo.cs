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

    public bool TryGetVirtualIPBase(ulong frameIdentity, out ulong baseVirtualIP)
        => TryGetVirtualIPBase(frameIdentity, out baseVirtualIP, out _);

    public bool TryGetVirtualIPBase(ulong frameIdentity, out ulong baseVirtualIP, out TargetPointer unwindDataAddress)
    {
        if ((frameIdentity & 1) == 0)
        {
            PortableEntryPoint portableEntryPoint =
                _target.ProcessedData.GetOrAdd<PortableEntryPoint>(new TargetPointer(frameIdentity));
            return _lookup.TryGetVirtualIPBase((uint)portableEntryPoint.ActualCode.Value, out baseVirtualIP, out unwindDataAddress);
        }

        // Native tests the UINT_PTR identity with static_cast<INT_PTR> and complements it at pointer
        // width; the indices it encodes are DWORDs.
        bool is64Bit = _target.PointerSize == sizeof(ulong);
        if (is64Bit ? (long)frameIdentity < 0 : (int)(uint)frameIdentity < 0)
        {
            ulong complement = is64Bit ? ~frameIdentity : ~(uint)frameIdentity;
            uint runtimeFunctionIndex = (uint)(complement >> 1);
            return _lookup.TryGetCoreLibVirtualIPBase(runtimeFunctionIndex, out baseVirtualIP, out unwindDataAddress);
        }

        return _lookup.TryGetVirtualIPBase((uint)(frameIdentity >> 1), out baseVirtualIP, out unwindDataAddress);
    }

    public bool TryGetUnwindData(TargetCodePointer controlPC, out TargetPointer unwindDataAddress)
    {
        unwindDataAddress = TargetPointer.Null;
        if (_executionManager.GetCodeBlockHandle(controlPC) is not CodeBlockHandle codeBlock)
            return false;

        TargetPointer runtimeFunctionAddress = _executionManager.GetUnwindInfo(codeBlock);
        if (runtimeFunctionAddress == TargetPointer.Null)
            return false;

        TargetPointer unwindInfoBaseAddress = _executionManager.GetUnwindInfoBaseAddress(codeBlock);
        RuntimeFunction runtimeFunction = _target.ProcessedData.GetOrAdd<RuntimeFunction>(runtimeFunctionAddress);
        unwindDataAddress = unwindInfoBaseAddress + runtimeFunction.UnwindData;
        return true;
    }
}
