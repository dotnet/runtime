// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


internal interface IReJIT : IContract
{
    static string IContract.Name { get; } = nameof(ReJIT);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer profControlBlockAddress = target.ReadGlobalPointer(Constants.Globals.ProfilerControlBlock);
        Data.ProfControlBlock profControlBlock = target.ProcessedData.GetOrAdd<Data.ProfControlBlock>(profControlBlockAddress);
        return version switch
        {
            1 => new ReJIT_1(target, profControlBlock),
            _ => default(ExecutionManager),
        };
    }

    bool IsEnabled() => throw new NotImplementedException();
}

internal readonly struct ReJIT : IExecutionManager
{

}
