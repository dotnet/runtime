// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

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

    internal static byte ReadPrecodeType(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        if (precodeMachineDescriptor.ReadWidthOfPrecodeType!.Value == 1)
        {
            byte precodeType = target.Read<byte>(instrPointer + precodeMachineDescriptor.OffsetOfPrecodeType!.Value);
            return (byte)(precodeType >> precodeMachineDescriptor.ShiftOfPrecodeType!.Value);
        }
        else if (precodeMachineDescriptor.ReadWidthOfPrecodeType!.Value == 2)
        {
            ushort precodeType = target.Read<ushort>(instrPointer + precodeMachineDescriptor.OffsetOfPrecodeType!.Value);
            return (byte)(precodeType >> precodeMachineDescriptor.ShiftOfPrecodeType!.Value);
        }
        else
        {
            throw new InvalidOperationException($"Invalid precode type width {precodeMachineDescriptor.ReadWidthOfPrecodeType}");
        }
    }

    public static KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        return TryGetKnownPrecodeType_Impl<PrecodeStubs_1_Impl, Data.StubPrecodeData_1>(instrPointer, target, precodeMachineDescriptor);
    }

    public static KnownPrecodeType? TryGetKnownPrecodeType_Impl<TPrecodeStubsImplementation, TStubPrecodeData>(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor) where TPrecodeStubsImplementation : IPrecodeStubsContractCommonApi<TStubPrecodeData> where TStubPrecodeData : IData<TStubPrecodeData>
    {
        // We get the precode type in two phases:
        // 1. Read the precode type from the intruction address.
        // 2. If it's "stub", look at the stub data and get the actual precode type - it could be stub,
        //    but it could also be a pinvoke precode or a ThisPtrRetBufPrecode
        // precode.h Precode::GetType()
        byte approxPrecodeType = ReadPrecodeType(instrPointer, target, precodeMachineDescriptor);
        byte exactPrecodeType;
        if (approxPrecodeType == precodeMachineDescriptor.StubPrecodeType)
        {
            // get the actual type from the StubPrecodeData
            TStubPrecodeData stubPrecodeData = GetStubPrecodeData(instrPointer, target, precodeMachineDescriptor);
            exactPrecodeType = TPrecodeStubsImplementation.StubPrecodeData_GetType(stubPrecodeData);
        }
        else
        {
            exactPrecodeType = approxPrecodeType;
        }

        if (exactPrecodeType == precodeMachineDescriptor.StubPrecodeType)
        {
            return KnownPrecodeType.Stub;
        }
        else if (precodeMachineDescriptor.PInvokeImportPrecodeType is byte ndType && exactPrecodeType == ndType)
        {
            return KnownPrecodeType.PInvokeImport;
        }
        else if (precodeMachineDescriptor.FixupPrecodeType is byte fixupType && exactPrecodeType == fixupType)
        {
            return KnownPrecodeType.Fixup;
        }
        else if (precodeMachineDescriptor.ThisPointerRetBufPrecodeType is byte thisPtrRetBufType && exactPrecodeType == thisPtrRetBufType)
        {
            return KnownPrecodeType.ThisPtrRetBuf;
        }
        else
        {
            return null;
        }

        static TStubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer stubPrecodeDataAddress = stubInstrPointer + precodeMachineDescriptor.StubCodePageSize;
            return target.ProcessedData.GetOrAdd<TStubPrecodeData>(stubPrecodeDataAddress);
        }
    }
}

internal sealed class PrecodeStubs_1 : PrecodeStubsCommon<PrecodeStubs_1_Impl, Data.StubPrecodeData_1>
{
    public PrecodeStubs_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, CodePointerFlags codePointerFlags) : base(target, precodeMachineDescriptor, codePointerFlags) { }
}
