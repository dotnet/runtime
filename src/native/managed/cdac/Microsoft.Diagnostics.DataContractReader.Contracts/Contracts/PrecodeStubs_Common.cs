// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal enum KnownPrecodeType
{
    Stub = 1,
    PInvokeImport,
    Fixup,
    ThisPtrRetBuf,
    UMEntry,
    Interpreter,
    DynamicHelper
}

// Interface used to abstract behavior which may be different between multiple versions of the precode stub implementations
internal interface IPrecodeStubsContractCommonApi<TStubPrecodeData>
{
    public static abstract TargetPointer StubPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract TargetPointer ThisPtrRetBufPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract TargetPointer FixupPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract byte StubPrecodeData_GetType(TStubPrecodeData stubPrecodeData);
    public static abstract KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
}

internal class PrecodeStubsCommon<TPrecodeStubsImplementation, TStubPrecodeData> : IPrecodeStubs where TPrecodeStubsImplementation : IPrecodeStubsContractCommonApi<TStubPrecodeData> where TStubPrecodeData : IData<TStubPrecodeData>
{
    private readonly Target _target;
    private readonly CodePointerFlags _codePointerFlags;
    internal readonly Data.PrecodeMachineDescriptor MachineDescriptor;

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

    private TStubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer)
    {
        TargetPointer stubPrecodeDataAddress = stubInstrPointer + MachineDescriptor.StubCodePageSize;
        return _target.ProcessedData.GetOrAdd<TStubPrecodeData>(stubPrecodeDataAddress);
    }

    private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
    {
        return TPrecodeStubsImplementation.TryGetKnownPrecodeType(instrAddress, _target, MachineDescriptor);
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
