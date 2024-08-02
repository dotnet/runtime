// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
    internal class EECodeInfo
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

    // RangeFragment and RangeSection pointers have a collectible flag on the lowest bit
    private struct ExMgrPtr
    {
        public readonly TargetPointer RawValue;

        public TargetPointer Address => RawValue & (ulong)~1u;

        public bool IsNull => Address == TargetPointer.Null;

        public static ExMgrPtr Null => new ExMgrPtr(TargetPointer.Null);

        public ExMgrPtr(TargetPointer value)
        {
            RawValue = value;
        }

        public ExMgrPtr Offset(int stride, int idx)
        {
            return new ExMgrPtr(RawValue.Value + (ulong)(stride * idx));
        }

        public T Load<T>(Target target) where T : Data.IData<T>
        {
            return target.ProcessedData.GetOrAdd<T>(Address);
        }

        public ExMgrPtr LoadPointer(Target target)
        {
            return new ExMgrPtr(target.ReadPointer(Address));
        }
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


    internal struct ExecutionManagerContract
    {
        internal readonly Target _target;
        private readonly Data.RangeSectionMap _topRangeSectionMap;
        private readonly TargetCodeManagerDescriptor _targetCodeManagerDescriptor;


        internal enum JitManagerKind
        {
            EEJitManager = 0,
            ReadyToRunJitManager = 1,
        }

        public ExecutionManagerContract(Target target, Data.RangeSectionMap topRangeSectionMap)
        {
            _target = target;
            _topRangeSectionMap = topRangeSectionMap;
            _targetCodeManagerDescriptor = TargetCodeManagerDescriptor.Create(target);
        }

        [Flags]
        private enum RangeSectionFlags : int
        {
            CodeHeap = 0x02,
            RangeList = 0x04,
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

            private bool HasFlags(RangeSectionFlags mask) => (_rangeSection!.Flags & (int)mask) != 0;
            private bool IsRangeList => HasFlags(RangeSectionFlags.RangeList);
            private bool IsCodeHeap => HasFlags(RangeSectionFlags.CodeHeap);

            public bool JitCodeToMethodInfo(Target target, TargetCodePointer jittedCodeAddress, out TargetPointer methodDescAddress)
            {
                if (_rangeSection == null)
                {
                    methodDescAddress = TargetPointer.Null;
                    return false;
                }
                // FIXME(cdac): prototype uses R2RModule to determine if the RangeSection belongs to the JIT or to R2R,
                // we don't need an extra JitManagerKind field.
                Data.IJitManager jitManager = target.ProcessedData.GetOrAdd<Data.IJitManager>(_rangeSection.JitManager);
                switch ((JitManagerKind)jitManager.JitManagerKind)
                {
                    case JitManagerKind.EEJitManager:
                        return EEJitCodeToMethodInfo(target, jitManager, jittedCodeAddress, out methodDescAddress);
                    case JitManagerKind.ReadyToRunJitManager:
                        methodDescAddress = TargetPointer.Null;
                        throw new NotImplementedException(); // TODO[cdac]:
                    default:
                        throw new InvalidOperationException($"Invalid JitManagerKind {jitManager.JitManagerKind}");
                }
            }

            private bool EEJitCodeToMethodInfo(Target target, Data.IJitManager jitManager, TargetCodePointer jittedCodeAddress, out TargetPointer methodDescAddress)
            {
                // EEJitManager::JitCodeToMethodInfo
                if (IsRangeList)
                {
                    methodDescAddress = TargetPointer.Null;
                    return false;
                }
                TargetPointer start = EEFindMethodCode(target, jitManager, jittedCodeAddress);
                if (start == TargetPointer.Null)
                {
                    methodDescAddress = TargetPointer.Null;
                    return false;
                }
                TargetPointer codeHeaderIndirect = new TargetPointer(start - (ulong)target.PointerSize);
                if (IsStubCodeBlock(target, codeHeaderIndirect))
                {
                    methodDescAddress = TargetPointer.Null;
                    return false;
                }
                TargetPointer codeHeaderAddress = target.ReadPointer(codeHeaderIndirect);
                Data.RealCodeHeader realCodeHeader = target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
                methodDescAddress = realCodeHeader.MethodDesc;
                return true;
            }

            private static bool IsStubCodeBlock(Target target, TargetPointer codeHeaderIndirect)
            {
                uint stubCodeBlockLast = target.ReadGlobal<uint>(Constants.Globals.StubCodeBlockLast);
                return codeHeaderIndirect.Value <= stubCodeBlockLast;
            }

            private TargetPointer EEFindMethodCode(Target target, Data.IJitManager jitManager, TargetCodePointer jittedCodeAddress)
            {
                // EEJitManager::FindMethodCode
                if (_rangeSection == null)
                {
                    throw new InvalidOperationException();
                }
                if (!IsCodeHeap)
                {
                    throw new InvalidOperationException("RangeSection is not a code heap");
                }
                TargetPointer heapListAddress = _rangeSection.HeapList;
                Data.HeapList heapList = _target.ProcessedData.GetOrAdd<Data.HeapList>(heapListAddress);
#if false
    HeapList *pHp = pRangeSection->_pHeapList;

    if ((currentPC < pHp->startAddress) ||
        (currentPC > pHp->endAddress))
    {
        return 0;
    }

    TADDR base = pHp->mapBase;
    PTR_DWORD pMap = pHp->pHdrMap;
    PTR_DWORD pMapStart = pMap;
#endif
                NibbleMap nibbleMap = NibbleMap.Create(target);
                return nibbleMap.FindMethodCode(mapBase, mapStart, jittedCodeAddress);

            }
        }

        // note: level is 1-indexed
        private static int EffectiveBitsForLevel(TargetCodeManagerDescriptor descriptor, TargetCodePointer address, int level)
        {
            ulong addressAsInt = address.Value;
            ulong addressBitsUsedInMap = addressAsInt >> (descriptor.MaxSetBit + 1 - (descriptor.MapLevels * descriptor.BitsPerLevel));
            ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * descriptor.BitsPerLevel);
            ulong addressBitsUsedInLevel = (ulong)(descriptor.EntriesPerMapLevel - 1) & addressBitsShifted;
            return checked((int)addressBitsUsedInLevel);
        }

        internal EECodeInfo? GetEECodeInfo(TargetCodePointer jittedCodeAddress)
        {
            RangeSection range = LookupRangeSection(jittedCodeAddress);
            if (!range.JitCodeToMethodInfo(_target, jittedCodeAddress, out TargetPointer methodDescAddress))
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
            ExMgrPtr rangeSectionFragmentPtr = GetRangeSectionForAddress(jittedCodeAddress);
            if (rangeSectionFragmentPtr.IsNull)
            {
                return new RangeSection();
            }
            while (!rangeSectionFragmentPtr.IsNull)
            {
                Data.RangeSectionFragment fragment = rangeSectionFragmentPtr.Load<Data.RangeSectionFragment>(_target);
                if (InRange(fragment, jittedCodeAddress))
                {
                    break;
                }
                rangeSectionFragmentPtr = new ExMgrPtr(fragment.Next); // TODO: load?
            }
            if (rangeSectionFragmentPtr.IsNull)
            {
                Data.RangeSectionFragment fragment = rangeSectionFragmentPtr.Load<Data.RangeSectionFragment>(_target);
                Data.RangeSection rangeSection = _target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);
                if (rangeSection.NextForDelete != TargetPointer.Null)
                {
                    return new RangeSection();
                }
                return new RangeSection(rangeSection);
            }
            return new RangeSection();
        }

        private ExMgrPtr /*PTR_RangeSectionFragment*/ GetRangeSectionForAddress(TargetCodePointer jittedCodeAddress)
        {
            /* The outer levels are all pointer arrays to the next level down.  Level 1 is an array of pointers to a RangeSectionFragment */
            int topLevelIndex = EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, _targetCodeManagerDescriptor.MapLevels);

            ExMgrPtr top = new ExMgrPtr(_topRangeSectionMap.TopLevelData);

            ExMgrPtr nextLevelAddress = top.Offset(_target.PointerSize, topLevelIndex);
            for (int level = _targetCodeManagerDescriptor.MapLevels - 1; level >= 1; level--)
            {
                ExMgrPtr rangeSectionL = nextLevelAddress.LoadPointer(_target);
                if (rangeSectionL.IsNull)
                    return ExMgrPtr.Null;
                nextLevelAddress = rangeSectionL.Offset(_target.PointerSize, EffectiveBitsForLevel(_targetCodeManagerDescriptor, jittedCodeAddress, level));
            }
            return nextLevelAddress;
        }

    }
}
