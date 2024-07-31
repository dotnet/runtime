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

        public ExecutionManagerContract(Target target, Data.RangeSectionMap topRangeSectionMap)
        {
            _target = target;
            _topRangeSectionMap = topRangeSectionMap;
            _targetCodeManagerDescriptor = TargetCodeManagerDescriptor.Create(target);
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
