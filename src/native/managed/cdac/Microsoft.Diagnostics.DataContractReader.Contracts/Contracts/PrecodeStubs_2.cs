// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct PrecodeStubs_2_Impl : IPrecodeStubsContractCommonApi<Data.StubPrecodeData_2>
{
    public static TargetPointer StubPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        TargetPointer stubPrecodeDataAddress = instrPointer + precodeMachineDescriptor.StubCodePageSize;
        Data.StubPrecodeData_2 stubPrecodeData = target.ProcessedData.GetOrAdd<Data.StubPrecodeData_2>(stubPrecodeDataAddress);
        return stubPrecodeData.SecretParam;
    }

    public static TargetPointer FixupPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        // Version 2 of this contract behaves just like version 1
        return PrecodeStubs_1_Impl.FixupPrecode_GetMethodDesc(instrPointer, target, precodeMachineDescriptor);
    }

    public static TargetPointer ThisPtrRetBufPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
            TargetPointer stubPrecodeDataAddress = instrPointer + precodeMachineDescriptor.StubCodePageSize;
            Data.StubPrecodeData_2 stubPrecodeData = target.ProcessedData.GetOrAdd<Data.StubPrecodeData_2>(stubPrecodeDataAddress);
            Data.ThisPtrRetBufPrecodeData thisPtrRetBufPrecodeData = target.ProcessedData.GetOrAdd<Data.ThisPtrRetBufPrecodeData>(stubPrecodeData.SecretParam);
            return thisPtrRetBufPrecodeData.MethodDesc;
    }

    public static byte StubPrecodeData_GetType(Data.StubPrecodeData_2 stubPrecodeData)
    {
        return stubPrecodeData.Type;
    }
}

internal sealed class PrecodeStubs_2 : PrecodeStubsCommon<PrecodeStubs_2_Impl, Data.StubPrecodeData_2>
{
    public PrecodeStubs_2(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, CodePointerFlags codePointerFlags) : base(target, precodeMachineDescriptor, codePointerFlags) { }
}
