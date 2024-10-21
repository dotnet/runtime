// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class PrecodeStubsFactory : IContractFactory<IPrecodeStubs>
{
    IPrecodeStubs IContractFactory<IPrecodeStubs>.CreateContract(Target target, int version)
    {
        ICDacMetadata cDacMetadata = target.Contracts.CDacMetadata;
        TargetPointer precodeMachineDescriptorAddress = cDacMetadata.GetPrecodeMachineDescriptor();
        Data.PrecodeMachineDescriptor precodeMachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(precodeMachineDescriptorAddress);
        return version switch
        {
            1 => new PrecodeStubs_1(target, precodeMachineDescriptor),
            _ => default(PrecodeStubs),
        };
    }
}
