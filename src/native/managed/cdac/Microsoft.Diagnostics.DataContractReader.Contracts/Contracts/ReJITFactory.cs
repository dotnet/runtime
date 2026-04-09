// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ReJITFactory : IContractFactory<IReJIT>
{
    IReJIT IContractFactory<IReJIT>.CreateContract(Target target, int version)
    {
        TargetPointer profControlBlockAddress = target.ReadGlobalPointer(Constants.Globals.ProfilerControlBlock);
        Data.ProfControlBlock profControlBlock = target.ProcessedData.GetOrAdd<Data.ProfControlBlock>(profControlBlockAddress);
        return version switch
        {
            1 => new ReJIT_1(target, profControlBlock),
            _ => default(ReJIT),
        };
    }
}
