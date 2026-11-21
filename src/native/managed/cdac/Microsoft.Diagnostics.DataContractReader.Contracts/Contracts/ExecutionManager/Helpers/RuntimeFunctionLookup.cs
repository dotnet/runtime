// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class RuntimeFunctionLookup
{
    public static RuntimeFunctionLookup Create(Target target, bool isWasm = false)
        => new RuntimeFunctionLookup(target, isWasm);

    private readonly bool _isWasm;
    private readonly uint _runtimeFunctionSize;
    private readonly Target _target;

    private RuntimeFunctionLookup(Target target, bool isWasm)
    {
        _target = target;
        _isWasm = isWasm;
        _runtimeFunctionSize = Data.RuntimeFunction.GetSize(target);
    }

    public uint GetBeginAddress(TargetPointer function)
        => _isWasm
            ? _target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(function).BeginAddress
            : _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(function).BeginAddress;

    public uint GetUnwindData(TargetPointer function)
        => _isWasm
            ? _target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(function).UnwindData
            : _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(function).UnwindData;

    public bool IsSkip(TargetPointer function)
        => _isWasm && _target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(function).IsSkip;

    public bool IsFunclet(TargetPointer function)
        => _isWasm && _target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(function).IsFunclet;

    public uint GetFunctionLength(TargetPointer imageBase, TargetPointer functionAddress)
    {
        Debug.Assert(!_isWasm);
        Data.RuntimeFunction function = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(functionAddress);
        if (function.EndAddress.HasValue)
            return function.EndAddress.Value - function.BeginAddress;

        Data.UnwindInfo unwindInfo = _target.ProcessedData.GetOrAdd<Data.UnwindInfo>(imageBase + function.UnwindData);
        if (unwindInfo.FunctionLength.HasValue)
            return unwindInfo.FunctionLength.Value;

        Debug.Assert(unwindInfo.Header.HasValue);

        // First 18 bits are function length / (pointer size / 2).
        // See UnwindFragmentInfo::Finalize
        uint funcLengthInHeader = unwindInfo.Header.Value & ((1 << 18) - 1);
        return (uint)(funcLengthInHeader * (_target.PointerSize / 2));
    }

    public bool TryGetRuntimeFunctionIndexForAddress(TargetPointer runtimeFunctions, uint numRuntimeFunctions, TargetPointer relativeAddress, out uint index)
    {
        index = 0;
        if (numRuntimeFunctions == 0)
            return false;

        relativeAddress = CodePointerUtils.CodePointerFromAddress(relativeAddress, _target).AsTargetPointer;

        if (_isWasm)
            return TryGetWasmRuntimeFunctionIndexForAddress(runtimeFunctions, numRuntimeFunctions, relativeAddress, out index);

        // NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod. Entries are sorted.
        return BinaryThenLinearSearch.Search(0, numRuntimeFunctions - 1, Compare, Match, out index);

        bool Compare(uint index)
        {
            Data.RuntimeFunction func = GetRuntimeFunction(runtimeFunctions, index);
            return relativeAddress < func.BeginAddress;
        }

        bool Match(uint index)
        {
            // If there is a next Unwind Info, check if the address is in the next Unwind Info.
            if (index < numRuntimeFunctions - 1)
            {
                Data.RuntimeFunction nextFunc = GetRuntimeFunction(runtimeFunctions, index + 1);
                if (relativeAddress >= nextFunc.BeginAddress)
                    return false;
            }

            Data.RuntimeFunction func = GetRuntimeFunction(runtimeFunctions, index);
            return relativeAddress >= func.BeginAddress;
        }
    }

    private bool TryGetWasmRuntimeFunctionIndexForAddress(TargetPointer runtimeFunctions, uint numRuntimeFunctions, TargetPointer relativeAddress, out uint index)
    {
        // NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod
        uint low = 0;
        uint high = numRuntimeFunctions;
        while (low < high)
        {
            uint middle = low + (high - low) / 2;
            uint candidate = middle;
            while (candidate > 0 && IsSkip(GetRuntimeFunctionAddress(runtimeFunctions, candidate)))
                candidate--;

            TargetPointer function = GetRuntimeFunctionAddress(runtimeFunctions, candidate);
            if (!IsSkip(function) && relativeAddress >= GetBeginAddress(function))
                low = middle + 1;
            else
                high = middle;
        }

        index = 0;
        if (low == 0)
            return false;

        index = low - 1;
        while (index > 0 && IsSkip(GetRuntimeFunctionAddress(runtimeFunctions, index)))
            index--;

        return !IsSkip(GetRuntimeFunctionAddress(runtimeFunctions, index));
    }

    public TargetPointer GetRuntimeFunctionAddress(TargetPointer runtimeFunctions, uint index)
    {
        return runtimeFunctions + (index * _runtimeFunctionSize);
    }

    private Data.RuntimeFunction GetRuntimeFunction(TargetPointer runtimeFunctions, uint index)
    {
        TargetPointer address = GetRuntimeFunctionAddress(runtimeFunctions, index);
        return _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(address);
    }
}
