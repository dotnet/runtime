// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
    internal enum KnownPrecodeType
    {
        Stub = 1,
        NDirectImport,
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

    internal sealed class NDirectImportPrecode : StubPrecode
    {
        internal NDirectImportPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.NDirectImport) { }
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

    internal struct PrecodeContract
    {
        public readonly Target _target;
        public readonly PrecodeMachineDescriptor _machineDescriptor;

        public PrecodeContract(Target target, PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            _target = target;
            _machineDescriptor = precodeMachineDescriptor;
        }

        internal PrecodeMachineDescriptor MachineDescriptor => _machineDescriptor;
        private bool IsAlignedInstrPointer(TargetPointer instrPointer) => _target.IsAlignedToPointerSize(instrPointer);

        private byte ReadPrecodeType(TargetPointer instrPointer)
        {
            if (_machineDescriptor.ReadWidthOfPrecodeType == 1)
            {
                byte precodeType = _target.Read<byte>(instrPointer + _machineDescriptor.OffsetOfPrecodeType);
                return (byte)(precodeType >> _machineDescriptor.ShiftOfPrecodeType);
            }
            else if (_machineDescriptor.ReadWidthOfPrecodeType == 2)
            {
                ushort precodeType = _target.Read<ushort>(instrPointer + _machineDescriptor.OffsetOfPrecodeType);
                return (byte)(precodeType >> _machineDescriptor.ShiftOfPrecodeType);
            }
            else
            {
                throw new InvalidOperationException($"Invalid precode type width {_machineDescriptor.ReadWidthOfPrecodeType}");
            }
        }

        private Data.StubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer)
        {
            TargetPointer stubPrecodeDataAddress = stubInstrPointer + _machineDescriptor.StubCodePageSize;
            return _target.ProcessedData.GetOrAdd<Data.StubPrecodeData>(stubPrecodeDataAddress);
        }

        private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
        {
            // precode.h Precode::GetType()
            byte precodeType = ReadPrecodeType(instrAddress);
            if (precodeType == _machineDescriptor.StubPrecodeType)
            {
                // get the actual type from the StubPrecodeData
                Data.StubPrecodeData stubPrecodeData = GetStubPrecodeData(instrAddress);
                precodeType = stubPrecodeData.Type;
            }

            if (precodeType == _machineDescriptor.StubPrecodeType)
            {
                return KnownPrecodeType.Stub;
            }
            else if (_machineDescriptor.NDirectImportPrecodeType is byte ndType && precodeType == ndType)
            {
                return KnownPrecodeType.NDirectImport;
            }
            else if (_machineDescriptor.FixupPrecodeType is byte fixupType && precodeType == fixupType)
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
            ulong instrPointer = (ulong)codePointer.AsTargetPointer & _machineDescriptor.CodePointerToInstrPointerMask.Value;
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
                    case KnownPrecodeType.NDirectImport:
                        return new NDirectImportPrecode(instrPointer);
                    case KnownPrecodeType.ThisPtrRetBuf:
                        return new ThisPtrRetBufPrecode(instrPointer);
                    default:
                        break;
                }
            }
            throw new InvalidOperationException($"Invalid precode type 0x{instrPointer:x16}");
        }

    }
}
