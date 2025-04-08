// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class PrecodeStubsFactory : IContractFactory<IPrecodeStubs>
{
    IPrecodeStubs IContractFactory<IPrecodeStubs>.CreateContract(Target target, int version)
    {
        IPlatformMetadata cDacMetadata = target.Contracts.PlatformMetadata;
        TargetPointer precodeMachineDescriptorAddress = cDacMetadata.GetPrecodeMachineDescriptor();
        Data.PrecodeMachineDescriptor precodeMachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(precodeMachineDescriptorAddress);
        CodePointerFlags codePointerFlags= cDacMetadata.GetCodePointerFlags();
        return version switch
        {
            1 => new PrecodeStubs_1(target, precodeMachineDescriptor, codePointerFlags),
            _ => default(PrecodeStubs),
        };
    }
}
