// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct EECodeInfoHandle
{
    public readonly TargetPointer Address;
    internal EECodeInfoHandle(TargetPointer address) => Address = address;
}

internal interface IExecutionManager : IContract
{
    static string IContract.Name { get; } = nameof(ExecutionManager);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);
        TargetPointer profControlBlockAddress = target.ReadGlobalPointer(Constants.Globals.ProfilerControlBlock);
        Data.ProfControlBlock profControlBlock = target.ProcessedData.GetOrAdd<Data.ProfControlBlock>(profControlBlockAddress);
        return version switch
        {
            1 => new ExecutionManager_1(target, rangeSectionMap, profControlBlock),
            _ => default(ExecutionManager),
        };
    }

    EECodeInfoHandle? GetEECodeInfoHandle(TargetCodePointer ip) => throw new NotImplementedException();
    TargetPointer GetMethodDesc(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetStartAddress(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();

}

internal readonly struct ExecutionManager : IExecutionManager
{

}
