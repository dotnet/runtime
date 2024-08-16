// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct PrecodeStubs_1 : IPrecodeStubs
{
    private readonly Target _target;
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
            TargetPointer stubPrecodeDataAddress = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            Data.StubPrecodeData stubPrecodeData = target.ProcessedData.GetOrAdd<Data.StubPrecodeData>(stubPrecodeDataAddress);
            return stubPrecodeData.MethodDesc;
        }
    }

    internal sealed class PInvokeImportPrecode : StubPrecode
    {
        internal PInvokeImportPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.PInvokeImport) { }
    }

    internal sealed class FixupPrecode : ValidPrecode
    {
        internal FixupPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.Fixup) { }
        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer fixupPrecodeDataAddress = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            Data.FixupPrecodeData fixupPrecodeData = target.ProcessedData.GetOrAdd<Data.FixupPrecodeData>(fixupPrecodeDataAddress);
            return fixupPrecodeData.MethodDesc;

        }
    }

    internal sealed class ThisPtrRetBufPrecode : ValidPrecode // FIXME: is this a StubPrecode?
    {
        internal ThisPtrRetBufPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.ThisPtrRetBuf) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            throw new NotImplementedException(); // TODO(cdac)
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

    private Data.StubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer)
    {
        TargetPointer stubPrecodeDataAddress = stubInstrPointer + MachineDescriptor.StubCodePageSize;
        return _target.ProcessedData.GetOrAdd<Data.StubPrecodeData>(stubPrecodeDataAddress);
    }

    private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
    {
        // precode.h Precode::GetType()
        byte precodeType = ReadPrecodeType(instrAddress);
        if (precodeType == MachineDescriptor.StubPrecodeType)
        {
            // get the actual type from the StubPrecodeData
            Data.StubPrecodeData stubPrecodeData = GetStubPrecodeData(instrAddress);
            precodeType = stubPrecodeData.Type;
        }

        if (precodeType == MachineDescriptor.StubPrecodeType)
        {
            return KnownPrecodeType.Stub;
        }
        else if (MachineDescriptor.PInvokeImportPrecodeType is byte ndType && precodeType == ndType)
        {
            return KnownPrecodeType.PInvokeImport;
        }
        else if (MachineDescriptor.FixupPrecodeType is byte fixupType && precodeType == fixupType)
        {
            return KnownPrecodeType.Fixup;
        }
        // TODO: ThisPtrRetBuf
        else
        {
            return null;
        }
    }

    internal TargetPointer CodePointerReadableInstrPointer(TargetCodePointer codePointer)
    {
        // Mask off the thumb bit, if we're on arm32, to get the actual instruction pointer
        ulong instrPointer = (ulong)codePointer.AsTargetPointer & MachineDescriptor.CodePointerToInstrPointerMask.Value;
        return new TargetPointer(instrPointer);
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
    public PrecodeStubs_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
    {
        _target = target;
        MachineDescriptor = precodeMachineDescriptor;
    }

    TargetPointer IPrecodeStubs.MethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, MachineDescriptor);
    }

}
