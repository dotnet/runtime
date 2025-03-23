// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct PrecodeStubs_1_Impl : IPrecodeStubsContractCommonApi<Data.StubPrecodeData_1>
{
    public static TargetPointer StubPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        TargetPointer stubPrecodeDataAddress = instrPointer + precodeMachineDescriptor.StubCodePageSize;
        Data.StubPrecodeData_1 stubPrecodeData = target.ProcessedData.GetOrAdd<Data.StubPrecodeData_1>(stubPrecodeDataAddress);
        return stubPrecodeData.MethodDesc;
    }

    public static TargetPointer FixupPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        TargetPointer fixupPrecodeDataAddress = instrPointer + precodeMachineDescriptor.StubCodePageSize;
        Data.FixupPrecodeData fixupPrecodeData = target.ProcessedData.GetOrAdd<Data.FixupPrecodeData>(fixupPrecodeDataAddress);
        return fixupPrecodeData.MethodDesc;
    }

    public static TargetPointer ThisPtrRetBufPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        throw new NotImplementedException(); // TODO(cdac)
    }

    public static byte StubPrecodeData_GetType(Data.StubPrecodeData_1 stubPrecodeData)
    {
        return stubPrecodeData.Type;
    }
}

internal sealed class PrecodeStubs_1 : PrecodeStubsCommon<PrecodeStubs_1_Impl, Data.StubPrecodeData_1>
{
    public PrecodeStubs_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, CodePointerFlags codePointerFlags) : base(target, precodeMachineDescriptor, codePointerFlags) { }
}
