// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IPrecodeStubs : IContract
{
    static string IContract.Name { get; } = nameof(PrecodeStubs);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer precodeMachineDescriptorAddress = target.ReadGlobalPointer(Constants.Globals.PrecodeMachineDescriptor);
        Data.PrecodeMachineDescriptor precodeMachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(precodeMachineDescriptorAddress);
        return version switch
        {
            1 => new PrecodeStubs_1(target, precodeMachineDescriptor),
            _ => default(PrecodeStubs),
        };
    }

    TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint) => throw new NotImplementedException();

}

internal readonly struct PrecodeStubs : IPrecodeStubs
{

}
