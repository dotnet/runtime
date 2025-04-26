// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// Interface used to abstract behavior which may be different between multiple versions of the precode stub implementations
internal interface IPrecodeStubsContractCommonApi<TStubPrecodeData>
{
    public static abstract TargetPointer StubPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract TargetPointer ThisPtrRetBufPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract TargetPointer FixupPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract byte StubPrecodeData_GetType(TStubPrecodeData stubPrecodeData);
}

internal class PrecodeStubsCommon<TPrecodeStubsImplementation, TStubPrecodeData> : IPrecodeStubs where TPrecodeStubsImplementation : IPrecodeStubsContractCommonApi<TStubPrecodeData> where TStubPrecodeData : IData<TStubPrecodeData>
{
    private readonly Target _target;
    private readonly CodePointerFlags _codePointerFlags;
    internal readonly Data.PrecodeMachineDescriptor MachineDescriptor;

    internal enum KnownPrecodeType
    {
        Stub = 1,
        PInvokeImport, // also known as NDirectImport in the runtime
        Fixup,
        ThisPtrRetBuf,
    }

    internal abstract class ValidPrecode
    {
        public TargetPointer InstrPointer { get; }
        public KnownPrecodeType PrecodeType { get; }

        protected ValidPrecode(TargetPointer instrPointer, KnownPrecodeType precodeType)
        {
            InstrPointer = instrPointer;
            PrecodeType = precodeType;
        }

        internal abstract TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);

    }

    internal class StubPrecode : ValidPrecode
    {
        internal StubPrecode(TargetPointer instrPointer, KnownPrecodeType type = KnownPrecodeType.Stub) : base(instrPointer, type) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.StubPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    public sealed class PInvokeImportPrecode : StubPrecode
    {
        internal PInvokeImportPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.PInvokeImport) { }
    }

    public sealed class FixupPrecode : ValidPrecode
    {
        internal FixupPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.Fixup) { }
        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.FixupPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    public sealed class ThisPtrRetBufPrecode : ValidPrecode
    {
        internal ThisPtrRetBufPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.ThisPtrRetBuf) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.ThisPtrRetBufPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    private bool IsAlignedInstrPointer(TargetPointer instrPointer) => _target.IsAlignedToPointerSize(instrPointer);

    private byte ReadPrecodeType(TargetPointer instrPointer)
    {
        if (MachineDescriptor.ReadWidthOfPrecodeType == 1)
        {
            byte precodeType = _target.Read<byte>(instrPointer + MachineDescriptor.OffsetOfPrecodeType);
            return (byte)(precodeType >> MachineDescriptor.ShiftOfPrecodeType);
        }
        else if (MachineDescriptor.ReadWidthOfPrecodeType == 2)
        {
            ushort precodeType = _target.Read<ushort>(instrPointer + MachineDescriptor.OffsetOfPrecodeType);
            return (byte)(precodeType >> MachineDescriptor.ShiftOfPrecodeType);
        }
        else
        {
            throw new InvalidOperationException($"Invalid precode type width {MachineDescriptor.ReadWidthOfPrecodeType}");
        }
    }

    private TStubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer)
    {
        TargetPointer stubPrecodeDataAddress = stubInstrPointer + MachineDescriptor.StubCodePageSize;
        return _target.ProcessedData.GetOrAdd<TStubPrecodeData>(stubPrecodeDataAddress);
    }

    private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
    {
        // We get the precode type in two phases:
        // 1. Read the precode type from the intruction address.
        // 2. If it's "stub", look at the stub data and get the actual precode type - it could be stub,
        //    but it could also be a pinvoke precode or a ThisPtrRetBufPrecode
        // precode.h Precode::GetType()
        byte approxPrecodeType = ReadPrecodeType(instrAddress);
        byte exactPrecodeType;
        if (approxPrecodeType == MachineDescriptor.StubPrecodeType)
        {
            // get the actual type from the StubPrecodeData
            TStubPrecodeData stubPrecodeData = GetStubPrecodeData(instrAddress);
            exactPrecodeType = TPrecodeStubsImplementation.StubPrecodeData_GetType(stubPrecodeData);
        }
        else
        {
            exactPrecodeType = approxPrecodeType;
        }

        if (exactPrecodeType == MachineDescriptor.StubPrecodeType)
        {
            return KnownPrecodeType.Stub;
        }
        else if (MachineDescriptor.PInvokeImportPrecodeType is byte ndType && exactPrecodeType == ndType)
        {
            return KnownPrecodeType.PInvokeImport;
        }
        else if (MachineDescriptor.FixupPrecodeType is byte fixupType && exactPrecodeType == fixupType)
        {
            return KnownPrecodeType.Fixup;
        }
        else if (MachineDescriptor.ThisPointerRetBufPrecodeType is byte thisPtrRetBufType && exactPrecodeType == thisPtrRetBufType)
        {
            return KnownPrecodeType.ThisPtrRetBuf;
        }
        else
        {
            return null;
        }
    }

    internal TargetPointer CodePointerReadableInstrPointer(TargetCodePointer codePointer)
    {
        if (_codePointerFlags.HasFlag(CodePointerFlags.HasArm32ThumbBit))
        {
            return codePointer.AsTargetPointer & ~1ul;
        }
        if (_codePointerFlags.HasFlag(CodePointerFlags.HasArm64PtrAuth))
        {
            throw new NotImplementedException("CodePointerReadableInstrPointer for ARM64 with pointer authentication");
        }
        Debug.Assert(_codePointerFlags == 0);
        return codePointer.AsTargetPointer;
    }


    internal ValidPrecode GetPrecodeFromEntryPoint(TargetCodePointer entryPoint)
    {
        TargetPointer instrPointer = CodePointerReadableInstrPointer(entryPoint);
        if (IsAlignedInstrPointer(instrPointer) && TryGetKnownPrecodeType(instrPointer) is KnownPrecodeType precodeType)
        {
            switch (precodeType)
            {
                case KnownPrecodeType.Stub:
                    return new StubPrecode(instrPointer);
                case KnownPrecodeType.Fixup:
                    return new FixupPrecode(instrPointer);
                case KnownPrecodeType.PInvokeImport:
                    return new PInvokeImportPrecode(instrPointer);
                case KnownPrecodeType.ThisPtrRetBuf:
                    return new ThisPtrRetBufPrecode(instrPointer);
                default:
                    break;
            }
        }
        throw new InvalidOperationException($"Invalid precode type 0x{instrPointer:x16}");
    }
    public PrecodeStubsCommon(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, CodePointerFlags codePointerFlags)
    {
        _target = target;
        MachineDescriptor = precodeMachineDescriptor;
        _codePointerFlags = codePointerFlags;
    }

    TargetPointer IPrecodeStubs.GetMethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, MachineDescriptor);
    }
}
