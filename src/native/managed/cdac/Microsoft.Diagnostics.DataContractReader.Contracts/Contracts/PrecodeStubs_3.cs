// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct PrecodeStubs_3_Impl : IPrecodeStubsContractCommonApi<Data.StubPrecodeData_2>
{
    public static TargetPointer StubPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        // Version 3 of this contract behaves just like version 2
        return PrecodeStubs_2_Impl.StubPrecode_GetMethodDesc(instrPointer, target, precodeMachineDescriptor);
    }

    public static TargetPointer FixupPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        // Version 3 of this contract behaves just like version 1
        return PrecodeStubs_1_Impl.FixupPrecode_GetMethodDesc(instrPointer, target, precodeMachineDescriptor);
    }

    public static TargetPointer ThisPtrRetBufPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        // Version 3 of this contract behaves just like version 2
        return PrecodeStubs_2_Impl.ThisPtrRetBufPrecode_GetMethodDesc(instrPointer, target, precodeMachineDescriptor);
    }

    public static byte StubPrecodeData_GetType(Data.StubPrecodeData_2 stubPrecodeData)
    {
        // Version 3 of this contract behaves just like version 2
        return PrecodeStubs_2_Impl.StubPrecodeData_GetType(stubPrecodeData);
    }

    private static Data.StubPrecodeData_2 GetStubPrecodeData(TargetPointer stubInstrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        TargetPointer stubPrecodeDataAddress = stubInstrPointer + precodeMachineDescriptor.StubCodePageSize;
        return target.ProcessedData.GetOrAdd<Data.StubPrecodeData_2>(stubPrecodeDataAddress);
    }

    public static KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        if (ReadBytesAndCompare(instrPointer, precodeMachineDescriptor.StubBytes!, precodeMachineDescriptor.StubIgnoredBytes!, target))
        {
            // get the actual type from the StubPrecodeData
            Data.StubPrecodeData_2 stubPrecodeData = GetStubPrecodeData(instrPointer, target, precodeMachineDescriptor);
            byte exactPrecodeType = stubPrecodeData.Type;
            if (exactPrecodeType == 0)
                return null;

            if (exactPrecodeType == precodeMachineDescriptor.StubPrecodeType)
            {
                return KnownPrecodeType.Stub;
            }
            else if (precodeMachineDescriptor.PInvokeImportPrecodeType is byte compareByte1 && compareByte1 == exactPrecodeType)
            {
                return KnownPrecodeType.PInvokeImport;
            }
            else if (precodeMachineDescriptor.ThisPointerRetBufPrecodeType is byte compareByte2 && compareByte2 == exactPrecodeType)
            {
                return KnownPrecodeType.ThisPtrRetBuf;
            }
            else if (precodeMachineDescriptor.UMEntryPrecodeType is byte compareByte3 && compareByte3 == exactPrecodeType)
            {
                return KnownPrecodeType.UMEntry;
            }
            else if (precodeMachineDescriptor.InterpreterPrecodeType is byte compareByte4 && compareByte4 == exactPrecodeType)
            {
                return KnownPrecodeType.Interpreter;
            }
            else if (precodeMachineDescriptor.DynamicHelperPrecodeType is byte compareByte5 && compareByte5 == exactPrecodeType)
            {
                return KnownPrecodeType.DynamicHelper;
            }
        }
        else if (ReadBytesAndCompare(instrPointer, precodeMachineDescriptor.FixupBytes!, precodeMachineDescriptor.FixupIgnoredBytes!, target))
        {
            return KnownPrecodeType.Fixup;
        }
        return null;

        static bool ReadBytesAndCompare(TargetPointer instrAddress, byte[] expectedBytePattern, byte[] bytesToIgnore, Target target)
        {
            for (ulong i = 0; i < (ulong)expectedBytePattern.Length; i++)
            {
                if (bytesToIgnore[i] == 0)
                {
                    byte targetBytePattern = target.Read<byte>(new TargetPointer((instrAddress.Value + (ulong)i)));
                    if (expectedBytePattern[i] != targetBytePattern)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

internal sealed class PrecodeStubs_3 : PrecodeStubsCommon<PrecodeStubs_3_Impl, Data.StubPrecodeData_2>
{
    public PrecodeStubs_3(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, CodePointerFlags codePointerFlags) : base(target, precodeMachineDescriptor, codePointerFlags) { }
}
