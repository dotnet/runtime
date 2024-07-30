// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct NativeCodePointers_1 : INativeCodePointers
{
    private readonly Target _target;
    private readonly Data.PrecodeMachineDescriptor _precodeMachineDescriptor;
    private readonly TargetPointer _executionManagerCodeRangeMapAddress;

    private bool IsAlignedInstrPointer(TargetPointer instrPointer) => _target.IsAlignedToPointerSize(instrPointer);

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

        internal override TargetPointer GetMethodDesc(Target target, PrecodeMachineDescriptor precodeMachineDescriptor)
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
        internal override TargetPointer GetMethodDesc(Target target, PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            TargetPointer fixupPrecodeDataAddress = InstrPointer + precodeMachineDescriptor.StubCodePageSize;
            Data.FixupPrecodeData fixupPrecodeData = target.ProcessedData.GetOrAdd<Data.FixupPrecodeData>(fixupPrecodeDataAddress);
            return fixupPrecodeData.MethodDesc;

        }
    }

    internal sealed class ThisPtrRetBufPrecode : ValidPrecode // FIXME: is this a StubPrecode?
    {
        internal ThisPtrRetBufPrecode(TargetPointer instrPointer) : base(instrPointer, KnownPrecodeType.ThisPtrRetBuf) { }

        internal override TargetPointer GetMethodDesc(Target target, PrecodeMachineDescriptor precodeMachineDescriptor)
        {
            throw new NotImplementedException(); // TODO(cdac)
        }
    }

    private byte ReadPrecodeType(TargetPointer instrPointer)
    {
        if (_precodeMachineDescriptor.ReadWidthOfPrecodeType == 1)
        {
            byte precodeType = _target.Read<byte>(instrPointer + _precodeMachineDescriptor.OffsetOfPrecodeType);
            return (byte)(precodeType >> _precodeMachineDescriptor.ShiftOfPrecodeType);
        }
        else if (_precodeMachineDescriptor.ReadWidthOfPrecodeType == 2)
        {
            ushort precodeType = _target.Read<ushort>(instrPointer + _precodeMachineDescriptor.OffsetOfPrecodeType);
            return (byte)(precodeType >> _precodeMachineDescriptor.ShiftOfPrecodeType);
        }
        else
        {
            throw new InvalidOperationException($"Invalid precode type width {_precodeMachineDescriptor.ReadWidthOfPrecodeType}");
        }
    }

    private Data.StubPrecodeData GetStubPrecodeData(TargetPointer stubInstrPointer)
    {
        TargetPointer stubPrecodeDataAddress = stubInstrPointer + _precodeMachineDescriptor.StubCodePageSize;
        return _target.ProcessedData.GetOrAdd<Data.StubPrecodeData>(stubPrecodeDataAddress);
    }

    private KnownPrecodeType? TryGetKnownPrecodeType(TargetPointer instrAddress)
    {
        // precode.h Precode::GetType()
        byte precodeType = ReadPrecodeType(instrAddress);
        if (precodeType == _precodeMachineDescriptor.StubPrecodeType)
        {
            // get the actual type from the StubPrecodeData
            Data.StubPrecodeData stubPrecodeData = GetStubPrecodeData(instrAddress);
            precodeType = stubPrecodeData.Type;
        }

        if (precodeType == _precodeMachineDescriptor.StubPrecodeType)
        {
            return KnownPrecodeType.Stub;
        }
        else if (_precodeMachineDescriptor.NDirectImportPrecodeType is byte ndType && precodeType == ndType)
        {
            return KnownPrecodeType.NDirectImport;
        }
        else if (_precodeMachineDescriptor.FixupPrecodeType is byte fixupType && precodeType == fixupType)
        {
            return KnownPrecodeType.Fixup;
        }
        // TODO: ThisPtrRetBuf
        else
        {
            return null;
        }
    }

    public NativeCodePointers_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, TargetPointer executionManagerCodeRangeMapAddress)
    {
        _target = target;
        _precodeMachineDescriptor = precodeMachineDescriptor;
        _executionManagerCodeRangeMapAddress = executionManagerCodeRangeMapAddress;
    }

    internal TargetPointer CodePointerReadableInstrPointer(TargetCodePointer codePointer)
    {
        // Mask off the thumb bit, if we're on arm32, to get the actual instruction pointer
        ulong instrPointer = (ulong)codePointer.AsTargetPointer & _precodeMachineDescriptor.CodePointerToInstrPointerMask.Value;
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


    TargetPointer INativeCodePointers.MethodDescFromStubAddress(TargetCodePointer entryPoint)
    {
        ValidPrecode precode = GetPrecodeFromEntryPoint(entryPoint);

        return precode.GetMethodDesc(_target, _precodeMachineDescriptor);
    }


    private class EECodeInfo
    {
        public TargetCodePointer CodeAddress { get; }
        public TargetPointer MethodDescAddress { get; }
        public EECodeInfo(TargetCodePointer jittedCodeAdderss, TargetPointer methodDescAddress)
        {
            CodeAddress = jittedCodeAdderss;
            MethodDescAddress = methodDescAddress;
        }

        public bool Valid => CodeAddress != default && MethodDescAddress != default;
    }

    private sealed class RangeSection
    {
        public bool JitCodeToMethodInfo(TargetCodePointer jittedCodeAddress, out TargetPointer methodDescAddress)
        {
            throw new NotImplementedException();
        }
    }

    private EECodeInfo? GetEECodeInfo(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = ExecutionManagerFindCodeRange(jittedCodeAddress);
        if (!range.JitCodeToMethodInfo(jittedCodeAddress, out TargetPointer methodDescAddress))
        {
            return null;
        }
        return new EECodeInfo(jittedCodeAddress, methodDescAddress);
    }

    private RangeSection ExecutionManagerFindCodeRange(TargetCodePointer jittedCodeAddress)
    {
        // GetCodeRangeMap()->LookupRangeSection(addr, pLockState);

        throw new NotImplementedException(); // TODO(cdac)
    }

    private RangeSection LookupRangeSection(TargetCodePointer jittedCodeAddress)
    {
#if false
        PTR_RangeSectionFragment fragment = GetRangeSectionForAddress(address, pLockState);
        if (fragment == NULL)
            return NULL;

        while ((fragment != NULL) && !fragment->InRange(address))
        {
            fragment = fragment->pRangeSectionFragmentNext.VolatileLoadWithoutBarrier(pLockState);
        }

        if (fragment != NULL)
        {
            if (fragment->pRangeSection->_pRangeSectionNextForDelete != NULL)
                return NULL;
            return fragment->pRangeSection;
        }
#endif
        throw new NotImplementedException();
    }

#if false
    PTR_RangeSectionFragment GetRangeSectionForAddress(TADDR address, RangeSectionLockState* pLockState)
    {
        uintptr_t topLevelIndex = EffectiveBitsForLevel(address, mapLevels);
        auto nextLevelAddress = &(GetTopLevel()[topLevelIndex]);
 ifdef TARGET_64BIT
        auto rangeSectionL4 = nextLevelAddress->VolatileLoad(pLockState);
        if (rangeSectionL4 == NULL)
            return NULL;
        auto rangeSectionL3 = (*rangeSectionL4)[EffectiveBitsForLevel(address, 4)].VolatileLoadWithoutBarrier(pLockState);
        if (rangeSectionL3 == NULL)
            return NULL;
        auto rangeSectionL2 = (*rangeSectionL3)[EffectiveBitsForLevel(address, 3)].VolatileLoadWithoutBarrier(pLockState);
        if (rangeSectionL2 == NULL)
            return NULL;
        auto rangeSectionL1 = (*rangeSectionL2)[EffectiveBitsForLevel(address, 2)].VolatileLoadWithoutBarrier(pLockState);
 else
        auto rangeSectionL1 = nextLevelAddress->VolatileLoad(pLockState);
 endif
        if (rangeSectionL1 == NULL)
            return NULL;

        return ((*rangeSectionL1)[EffectiveBitsForLevel(address, 1)]).VolatileLoadWithoutBarrier(pLockState);
        throw new NotImplementedException();
    }
#endif

    TargetPointer INativeCodePointers.ExecutionManagerGetCodeMethodDesc(TargetCodePointer jittedCodeAddress)
    {
        EECodeInfo? info = GetEECodeInfo(jittedCodeAddress);
        if (info == null || !info.Valid)
        {
            throw new InvalidOperationException($"Failed to get EECodeInfo for {jittedCodeAddress}");
        }
        return info.MethodDescAddress;
    }

}
