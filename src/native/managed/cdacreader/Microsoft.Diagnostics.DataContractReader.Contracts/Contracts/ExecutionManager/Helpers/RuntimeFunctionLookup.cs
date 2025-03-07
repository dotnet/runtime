// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class RuntimeFunctionLookup
{
    public static RuntimeFunctionLookup Create(Target target)
        => new RuntimeFunctionLookup(target);

    private readonly uint _runtimeFunctionSize;
    private readonly Target _target;

    private RuntimeFunctionLookup(Target target)
    {
        _target = target;
        _runtimeFunctionSize = target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value;
    }

    public uint GetFunctionLength(Data.RuntimeFunction function)
    {
        if (function.EndAddress.HasValue)
            return function.EndAddress.Value - function.BeginAddress;

        Data.UnwindInfo unwindInfo = _target.ProcessedData.GetOrAdd<Data.UnwindInfo>(function.UnwindData);
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
        // NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod
        uint start = 0;
        uint end = numRuntimeFunctions - 1;
        relativeAddress = CodePointerUtils.CodePointerFromAddress(relativeAddress, _target).AsTargetPointer;

        // Entries are sorted.
        return BinaryThenLinearSearch.Search(start, end, Compare, Match, out index);

        bool Compare(uint index)
        {
            Data.RuntimeFunction func = GetRuntimeFunction(runtimeFunctions, index);
            return relativeAddress < func.BeginAddress;
        };

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

    public TargetPointer GetRuntimeFunctionAddress(TargetPointer runtimeFunctions, uint index)
    {
        return runtimeFunctions + (index * _runtimeFunctionSize);
    }

    public Data.RuntimeFunction GetRuntimeFunction(TargetPointer runtimeFunctions, uint index)
    {
        TargetPointer addr = GetRuntimeFunctionAddress(runtimeFunctions, index);
        return _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(addr);
    }
}
