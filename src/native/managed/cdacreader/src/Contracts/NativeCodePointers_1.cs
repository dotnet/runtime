// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct NativeCodePointers_1 : INativeCodePointers
{
    private readonly Target _target;
    private readonly Data.PrecodeMachineDescriptor _precodeMachineDescriptor;
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly TargetCodeManagerDescriptor _targetCodeManagerDescriptor;

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

    public NativeCodePointers_1(Target target, Data.PrecodeMachineDescriptor precodeMachineDescriptor, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _precodeMachineDescriptor = precodeMachineDescriptor;
        _topRangeSectionMap = topRangeSectionMap; ;
        _targetCodeManagerDescriptor = TargetCodeManagerDescriptor.Create(target);
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

    private readonly struct TargetCodeManagerDescriptor
    {
        public int MapLevels { get; }
        public int BitsPerLevel { get; } = 8;
        public int MaxSetBit { get; }
        public int EntriesPerMapLevel { get; } = 256;

        private TargetCodeManagerDescriptor(int mapLevels, int maxSetBit)
        {
            MapLevels = mapLevels;
            MaxSetBit = maxSetBit;
        }
        public static TargetCodeManagerDescriptor Create(Target target)
        {
            if (target.PointerSize == 4)
            {
                return new(mapLevels: 2, maxSetBit: 31); // 0 indexed
            }
            else if (target.PointerSize == 8)
            {
                return new(mapLevels: 5, maxSetBit: 56); // 0 indexed
            }
            else
            {
                throw new InvalidOperationException("Invalid pointer size");
            }
        }
    }

    private sealed class RangeSection
    {
        private readonly Data.RangeSection? _rangeSection;

        public RangeSection()
        {
            _rangeSection = default;
        }
        public RangeSection(Data.RangeSection rangeSection)
        {
            _rangeSection = rangeSection;
        }
        public bool JitCodeToMethodInfo(TargetCodePointer jittedCodeAddress, out TargetPointer methodDescAddress)
        {
            throw new NotImplementedException();
        }

        // note: level is 1-indexed
        public static uint EffectiveBitsForLevel(TargetCodeManagerDescriptor descriptor, TargetCodePointer address, int level)
        {
            ulong addressAsInt = address.Value;
            ulong addressBitsUsedInMap = addressAsInt >> (descriptor.MaxSetBit + 1 - (descriptor.MapLevels * descriptor.BitsPerLevel));
            ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * descriptor.BitsPerLevel);
            ulong addressBitsUsedInLevel = (ulong)(descriptor.EntriesPerMapLevel - 1) & addressBitsShifted;
            return checked((uint)addressBitsUsedInLevel);
        }
    }

    private EECodeInfo? GetEECodeInfo(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = LookupRangeSection(jittedCodeAddress);
        if (!range.JitCodeToMethodInfo(jittedCodeAddress, out TargetPointer methodDescAddress))
        {
            return null;
        }
        return new EECodeInfo(jittedCodeAddress, methodDescAddress);
    }

    private static bool InRange(Data.RangeSectionFragment fragment, TargetCodePointer address)
    {
        return fragment.RangeBegin <= address && address < fragment.RangeEndOpen;
    }

    private RangeSection LookupRangeSection(TargetCodePointer jittedCodeAddress)
    {
        TargetPointer rangeSectionFragmentPtr = GetRangeSectionForAddress(jittedCodeAddress);
        if (rangeSectionFragmentPtr == TargetPointer.Null)
        {
            return new RangeSection();
        }
        while (rangeSectionFragmentPtr != TargetPointer.Null)
        {
            Data.RangeSectionFragment fragment = _target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
            if (InRange(fragment, jittedCodeAddress))
            {
                break;
            }
            rangeSectionFragmentPtr = fragment.Next; // TODO: load?
        }
        if (rangeSectionFragmentPtr != TargetPointer.Null)
        {
            Data.RangeSectionFragment fragment = _target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
            Data.RangeSection rangeSection = _target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);
            if (rangeSection.NextForDelete != TargetPointer.Null)
            {
                return new RangeSection();
            }
            return new RangeSection(rangeSection);
        }
        return new RangeSection();
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

    private TargetPointer RangeSectionPointerLoad(TargetPointer ptr)
    {
        // clear the lowest bit, which is used as a tag for collectible levels, and read the pointer
        return _target.ReadPointer(ptr.Value & (ulong)~1u);
    }

    private TargetPointer /*PTR_RangeSectionFragment*/ GetRangeSectionForAddress(TargetCodePointer jittedCodeAddress)
    {
        uint topLevelIndex = RangeSection.EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, _targetCodeManagerDescriptor.MapLevels);

        TargetPointer nextLevelAddress = _topRangeSectionMap.TopLevelData + (ulong)_target.PointerSize * topLevelIndex;
        TargetPointer rangeSectionL1;
        if (_target.PointerSize == 8)
        {
            TargetPointer rangeSectionL4 = RangeSectionPointerLoad(nextLevelAddress);
            if (rangeSectionL4 == TargetPointer.Null)
                return TargetPointer.Null;
            nextLevelAddress = rangeSectionL4 + (ulong)_target.PointerSize * RangeSection.EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, 4);
            TargetPointer rangeSectionL3 = RangeSectionPointerLoad(nextLevelAddress);
            if (rangeSectionL3 == TargetPointer.Null)
                return TargetPointer.Null;
            nextLevelAddress = rangeSectionL3 + (ulong)_target.PointerSize * RangeSection.EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, 3);
            TargetPointer rangeSectionL2 = RangeSectionPointerLoad(nextLevelAddress);
            if (rangeSectionL2 == TargetPointer.Null)
                return TargetPointer.Null;
            nextLevelAddress = rangeSectionL2 + (ulong)_target.PointerSize * RangeSection.EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, 2);
            rangeSectionL1 = RangeSectionPointerLoad(nextLevelAddress);

        }
        else if (_target.PointerSize == 4)
        {
            rangeSectionL1 = RangeSectionPointerLoad(nextLevelAddress);
        }
        else
        {
            throw new InvalidOperationException("Invalid pointer size");
        }
        if (rangeSectionL1 == TargetPointer.Null)
            return TargetPointer.Null;
        nextLevelAddress = rangeSectionL1 + (ulong)_target.PointerSize * RangeSection.EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, 1);
        return RangeSectionPointerLoad(nextLevelAddress);
    }

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
