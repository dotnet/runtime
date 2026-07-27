// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;

/// <summary>
/// cDAC implementation of <see cref="IWasmR2RInfo"/>, mirroring the native
/// <c>ExecutionManager::{FindFunctionTableIndexRangeSection, IsFuncletFunctionIndex,
/// GetWasmVirtualIPFromFunctionTableIndex}</c> in <c>src/coreclr/vm/codeman.cpp</c>. It resolves an
/// R2R function table entry index against the <c>FunctionTableIndexRangeList</c> to its owning
/// module's <see cref="Data.ReadyToRunInfo"/>, then reads the corresponding
/// <c>RUNTIME_FUNCTION</c> for the funclet flag, base virtual IP, and unwind data.
/// </summary>
internal sealed class WasmR2RInfo : IWasmR2RInfo
{
    // RUNTIME_FUNCTION__IsFunclet: the funclet flag is the high bit of BeginAddress (clrnt.h).
    private const uint FuncletFlag = 0x80000000;

    private readonly Target _target;
    private readonly RuntimeFunctionLookup _runtimeFunctions;

    public WasmR2RInfo(Target target)
    {
        _target = target;
        _runtimeFunctions = RuntimeFunctionLookup.Create(target);
    }

    // Mirrors ExecutionManager::FindFunctionTableIndexRangeSection.
    private Data.FunctionTableIndexRangeSection? FindSection(uint functionTableIndex)
    {
        if (!_target.TryReadGlobalPointer(Constants.Globals.FunctionTableIndexRangeList, out TargetPointer? listHeadSlot))
            return null;

        // The global holds the address of the s_pFunctionTableIndexRangeList slot (a pointer-to-
        // pointer); dereference it once to obtain the actual list head.
        TargetPointer current = _target.ReadPointer(listHeadSlot.Value);
        while (current != TargetPointer.Null)
        {
            Data.FunctionTableIndexRangeSection section = _target.ProcessedData.GetOrAdd<Data.FunctionTableIndexRangeSection>(current);
            if (functionTableIndex >= section.MinFunctionTableIndex &&
                functionTableIndex < section.MinFunctionTableIndex + section.NumRuntimeFunctions)
            {
                return section;
            }
            current = section.Next;
        }

        return null;
    }

    private Data.ReadyToRunInfo GetReadyToRunInfo(Data.FunctionTableIndexRangeSection section)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(section.R2RModule);
        return _target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(module.ReadyToRunInfo);
    }

    private Data.RuntimeFunction GetRuntimeFunction(Data.ReadyToRunInfo r2rInfo, uint localIndex)
        => _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, localIndex);

    public bool TryGetVirtualIPBase(uint functionTableIndex, out ulong baseVirtualIP)
    {
        baseVirtualIP = 0;
        Data.FunctionTableIndexRangeSection? section = FindSection(functionTableIndex);
        if (section is null)
            return false;

        Data.ReadyToRunInfo r2rInfo = GetReadyToRunInfo(section);
        if (r2rInfo.MinVirtualIP is not TargetPointer minVirtualIP)
            return false;

        // Funclets' function-local virtual IPs are relative to their controlling function, so index
        // backwards past funclet entries to the controlling (non-funclet) function.
        uint localIndex = functionTableIndex - section.MinFunctionTableIndex;
        while (true)
        {
            Data.RuntimeFunction runtimeFunction = GetRuntimeFunction(r2rInfo, localIndex);
            if ((runtimeFunction.BeginAddress & FuncletFlag) != 0)
            {
                if (localIndex == 0)
                    return false;
                localIndex--;
                continue;
            }

            baseVirtualIP = minVirtualIP.Value + runtimeFunction.BeginAddress;
            return true;
        }
    }

    public bool TryGetUnwindData(uint functionTableIndex, out TargetPointer unwindDataAddress)
    {
        unwindDataAddress = TargetPointer.Null;
        Data.FunctionTableIndexRangeSection? section = FindSection(functionTableIndex);
        if (section is null)
            return false;

        Data.ReadyToRunInfo r2rInfo = GetReadyToRunInfo(section);
        uint localIndex = functionTableIndex - section.MinFunctionTableIndex;
        Data.RuntimeFunction runtimeFunction = GetRuntimeFunction(r2rInfo, localIndex);
        unwindDataAddress = new TargetPointer(r2rInfo.LoadedImageBase.Value + runtimeFunction.UnwindData);
        return true;
    }
}
