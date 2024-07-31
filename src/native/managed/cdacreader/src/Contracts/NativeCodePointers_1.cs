// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
    private readonly Target _target;
    private readonly PrecodeContract _precodeContract;
    private readonly ExecutionManagerContract _executionManagerContract;


    public NativeCodePointers_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _precodeContract = new PrecodeContract(target, precodeMachineDescriptor);
        _executionManagerContract = new ExecutionManagerContract(target, topRangeSectionMap);
    }

    TargetPointer INativeCodePointers.MethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = _precodeContract.GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, _precodeContract.MachineDescriptor);
    }

    TargetPointer INativeCodePointers.ExecutionManagerGetCodeMethodDesc(TargetCodePointer jittedCodeAddress)
    {
        EECodeInfo? info = _executionManagerContract.GetEECodeInfo(jittedCodeAddress);
        if (info == null || !info.Valid)
        {
            throw new InvalidOperationException($"Failed to get EECodeInfo for {jittedCodeAddress}");
        }
        return info.MethodDescAddress;
    }

}
