// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface INativeCodePointers : IContract
{
    static string IContract.Name { get; } = nameof(NativeCodePointers);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer precodeMachineDescriptorAddress = target.ReadGlobalPointer(Constants.Globals.PrecodeMachineDescriptor);
        Data.PrecodeMachineDescriptor precodeMachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(precodeMachineDescriptorAddress);
        return version switch
        {
            1 => new NativeCodePointers_1(target, precodeMachineDescriptor),
            _ => default(NativeCodePointers),
        };
    }

    public virtual TargetPointer MethodDescFromStubAddress(TargetCodePointer codeAddress) => throw new NotImplementedException();
    public virtual TargetPointer ExecutionManagerGetCodeMethodDesc(TargetCodePointer jittedCodeAddress) => throw new NotImplementedException();
}

internal readonly struct NativeCodePointers : INativeCodePointers
{
    // throws NotImplementedException for all methods
}
