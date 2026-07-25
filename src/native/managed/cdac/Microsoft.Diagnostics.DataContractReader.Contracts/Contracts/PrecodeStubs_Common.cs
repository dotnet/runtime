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
    public static abstract TargetPointer InterpreterPrecode_GetMethodDesc(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    public static abstract byte StubPrecodeData_GetType(TStubPrecodeData stubPrecodeData);
    public static abstract PrecodeType? TryGetKnownPrecodeType(TargetPointer instrPointer, Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
}

internal class PrecodeStubsCommon<TPrecodeStubsImplementation, TStubPrecodeData> : IPrecodeStubs where TPrecodeStubsImplementation : IPrecodeStubsContractCommonApi<TStubPrecodeData> where TStubPrecodeData : IData<TStubPrecodeData>
{
    private readonly Target _target;
    private readonly CodePointerFlags _codePointerFlags;
    internal readonly Data.PrecodeMachineDescriptor MachineDescriptor;

    protected Target Target => _target;

    internal abstract class ValidPrecode
    {
        public TargetPointer InstrPointer { get; }
        public PrecodeType PrecodeType { get; }

        protected ValidPrecode(TargetPointer instrPointer, PrecodeType precodeType)
        {
            InstrPointer = instrPointer;
            PrecodeType = precodeType;
        }

        internal abstract TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor);
    }

    internal class StubPrecode : ValidPrecode
    {
        internal StubPrecode(TargetPointer instrPointer, PrecodeType type = PrecodeType.Stub) : base(instrPointer, type) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.StubPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    internal sealed class InterpreterPrecode : ValidPrecode
    {
        internal InterpreterPrecode(TargetPointer instrPointer) : base(instrPointer, PrecodeType.Interpreter) { }

        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.InterpreterPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    public sealed class PInvokeImportPrecode : StubPrecode
    {
        internal PInvokeImportPrecode(TargetPointer instrPointer) : base(instrPointer, PrecodeType.PInvokeImport) { }
    }

    public sealed class FixupPrecode : ValidPrecode
    {
        internal FixupPrecode(TargetPointer instrPointer) : base(instrPointer, PrecodeType.Fixup) { }
        internal override TargetPointer GetMethodDesc(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            return TPrecodeStubsImplementation.FixupPrecode_GetMethodDesc(InstrPointer, target, precodeMachineDescriptor);
        }
    }

    public sealed class ThisPtrRetBufPrecode : ValidPrecode
    {
        internal ThisPtrRetBufPrecode(TargetPointer instrPointer) : base(instrPointer, PrecodeType.ThisPtrRetBuf) { }

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

    private PrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
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
        if (IsAlignedInstrPointer(instrPointer) && TryGetKnownPrecodeType(instrPointer) is PrecodeType precodeType)
        {
            switch (precodeType)
            {
                case PrecodeType.Stub:
                    return new StubPrecode(instrPointer);
                case PrecodeType.Fixup:
                    return new FixupPrecode(instrPointer);
                case PrecodeType.PInvokeImport:
                    return new PInvokeImportPrecode(instrPointer);
                case PrecodeType.ThisPtrRetBuf:
                    return new ThisPtrRetBufPrecode(instrPointer);
                case PrecodeType.Interpreter:
                    return new InterpreterPrecode(instrPointer);
                default:
                    break;
            }
        }
        throw new InvalidOperationException($"Invalid precode type 0x{instrPointer:x16}");
    }
    public PrecodeStubsCommon(Target target)
    {
        _target = target;
        IPlatformMetadata pm = target.Contracts.PlatformMetadata;
        TargetPointer descAddr = pm.GetPrecodeMachineDescriptor();
        MachineDescriptor = target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(descAddr);
        _codePointerFlags = pm.GetCodePointerFlags();
    }

    TargetPointer IPrecodeStubs.GetMethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, MachineDescriptor);
    }

    TargetPointer IPrecodeStubs.GetPrecodeEntryPointFromInteriorAddress(TargetCodePointer interiorAddress, bool isFixupPrecode)
    {
        TargetPointer instrPointer = CodePointerReadableInstrPointer(interiorAddress);

        uint stubSize;
        if (isFixupPrecode)
        {
            if (MachineDescriptor.FixupStubPrecodeSize is not byte fixupSize || fixupSize == 0)
                throw new InvalidOperationException("FixupPrecode size not available");
            stubSize = fixupSize;
        }
        else
        {
            if (MachineDescriptor.StubPrecodeSize is not byte stubPrecodeSize || stubPrecodeSize == 0)
                throw new InvalidOperationException("StubPrecode size not available");
            stubSize = stubPrecodeSize;
        }

        ulong pageMask = MachineDescriptor.StubCodePageSize - 1;
        ulong pageBase = instrPointer.Value & ~pageMask;
        ulong offset = instrPointer.Value - pageBase;
        ulong entryPointAddress = pageBase + (offset / stubSize) * stubSize;

        return new TargetPointer(entryPointAddress);
    }

    public virtual TargetCodePointer GetInterpreterCodeFromInterpreterPrecodeIfPresent(
        TargetCodePointer entryPoint) => entryPoint;
}
