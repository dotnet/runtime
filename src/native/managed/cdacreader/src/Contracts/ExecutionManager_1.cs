// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ExecutionManager_1 : IExecutionManager
{
    internal readonly Target _target;

    // maps EECodeInfoHandle.Address (which is the CodeHeaderAddress) to the EECodeInfo
    private readonly Dictionary<TargetPointer, EECodeInfo> _codeInfos = new();
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly RangeSectionLookupAlgorithm _rangeSectionLookupAlgorithm;
    private readonly EEJitManager _eeJitManager;
    private readonly ReadyToRunJitManager _r2rJitManager;

    public ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionLookupAlgorithm = RangeSectionLookupAlgorithm.Create(target);
        NibbleMap nibbleMap = NibbleMap.Create(target);
        _eeJitManager = new EEJitManager(target, nibbleMap);
        _r2rJitManager = new ReadyToRunJitManager(target);
    }

    private class EECodeInfo
    {
        private readonly int _codeHeaderOffset;

        public TargetCodePointer StartAddress { get; }
        // note: this is the address of the pointer to the "real code header", you need to
        // dereference it to get the address of _codeHeaderData
        public TargetPointer CodeHeaderAddress => StartAddress.Value - (ulong)_codeHeaderOffset;
        private Data.RealCodeHeader _codeHeaderData;
        public TargetPointer JitManagerAddress { get; }
        public TargetNUInt RelativeOffset { get; }
        public EECodeInfo(TargetCodePointer startAddress, int codeHeaderOffset, TargetNUInt relativeOffset, Data.RealCodeHeader codeHeaderData, TargetPointer jitManagerAddress)
        {
            _codeHeaderOffset = codeHeaderOffset;
            StartAddress = startAddress;
            _codeHeaderData = codeHeaderData;
            RelativeOffset = relativeOffset;
            JitManagerAddress = jitManagerAddress;
        }

        public TargetPointer MethodDescAddress => _codeHeaderData.MethodDesc;
        public bool Valid => JitManagerAddress != TargetPointer.Null;
    }

    // "ExecutionManagerPointer": a pointer to a RangeFragment and RangeSection.
    // The pointers have a collectible flag on the lowest bit
    private struct ExMgrPtr
    {
        public readonly TargetPointer RawValue;

        public TargetPointer Address => RawValue & ~1ul;

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

    private readonly struct RangeSectionLookupAlgorithm
    {
        private int MapLevels { get; }
        private int BitsPerLevel { get; } = 8;
        private int MaxSetBit { get; }
        private int EntriesPerMapLevel { get; } = 256;

        private RangeSectionLookupAlgorithm(int mapLevels, int maxSetBit)
        {
            MapLevels = mapLevels;
            MaxSetBit = maxSetBit;
        }
        public static RangeSectionLookupAlgorithm Create(Target target)
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

        // note: level is 1-indexed
        private int EffectiveBitsForLevel(TargetCodePointer address, int level)
        {
            ulong addressAsInt = address.Value;
            ulong addressBitsUsedInMap = addressAsInt >> (MaxSetBit + 1 - (MapLevels * BitsPerLevel));
            ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * BitsPerLevel);
            ulong addressBitsUsedInLevel = (ulong)(EntriesPerMapLevel - 1) & addressBitsShifted;
            return checked((int)addressBitsUsedInLevel);
        }

        internal ExMgrPtr /*PTR_RangeSectionFragment*/ FindFragment(Target target, Data.RangeSectionMap topRangeSectionMap, TargetCodePointer jittedCodeAddress)
        {
            /* The outer levels are all pointer arrays to the next level down.  Level 1 is an array of pointers to a RangeSectionFragment */
            int topLevelIndex = EffectiveBitsForLevel(jittedCodeAddress, MapLevels);

            ExMgrPtr top = new ExMgrPtr(topRangeSectionMap.TopLevelData);

            ExMgrPtr nextLevelAddress = top.Offset(target.PointerSize, topLevelIndex);
            for (int level = MapLevels - 1; level >= 1; level--)
            {
                ExMgrPtr rangeSectionL = nextLevelAddress.LoadPointer(target);
                if (rangeSectionL.IsNull)
                    return ExMgrPtr.Null;
                nextLevelAddress = rangeSectionL.Offset(target.PointerSize, EffectiveBitsForLevel(jittedCodeAddress, level));
            }
            return nextLevelAddress;
        }
    }

    [Flags]
    private enum RangeSectionFlags : int
    {
        CodeHeap = 0x02,
        RangeList = 0x04,
    }
    private abstract class JitManager
    {
        public Target Target { get; }

        protected JitManager(Target target)
        {
            Target = target;
        }

        public abstract bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info);

    }

    private class ReadyToRunJitManager : JitManager
    {
        public ReadyToRunJitManager(Target target) : base(target)
        {
        }
        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info)
        {
            throw new NotImplementedException(); // TODO(cdac): ReadyToRunJitManager::JitCodeToMethodInfo
        }
    }

    private sealed class RangeSection
    {
        public readonly Data.RangeSection? Data;

        public RangeSection()
        {
            Data = default;
        }
        public RangeSection(Data.RangeSection rangeSection)
        {
            Data = rangeSection;
        }

        private bool HasFlags(RangeSectionFlags mask) => (Data!.Flags & (int)mask) != 0;
        internal bool IsRangeList => HasFlags(RangeSectionFlags.RangeList);
        internal bool IsCodeHeap => HasFlags(RangeSectionFlags.CodeHeap);

        internal static bool IsStubCodeBlock(Target target, TargetPointer codeHeaderIndirect)
        {
            uint stubCodeBlockLast = target.ReadGlobal<uint>(Constants.Globals.StubCodeBlockLast);
            return codeHeaderIndirect.Value <= stubCodeBlockLast;
        }

        internal static RangeSection Find(Target target, Data.RangeSectionMap topRangeSectionMap, RangeSectionLookupAlgorithm rangeSectionLookup, TargetCodePointer jittedCodeAddress)
        {
            ExMgrPtr rangeSectionFragmentPtr = rangeSectionLookup.FindFragment(target, topRangeSectionMap, jittedCodeAddress);
            if (rangeSectionFragmentPtr.IsNull)
            {
                return new RangeSection();
            }
            while (!rangeSectionFragmentPtr.IsNull)
            {
                Data.RangeSectionFragment fragment = rangeSectionFragmentPtr.Load<Data.RangeSectionFragment>(target);
                if (fragment.Contains(jittedCodeAddress))
                {
                    break;
                }
                rangeSectionFragmentPtr = new ExMgrPtr(fragment.Next);
            }
            if (!rangeSectionFragmentPtr.IsNull)
            {
                Data.RangeSectionFragment fragment = rangeSectionFragmentPtr.Load<Data.RangeSectionFragment>(target);
                Data.RangeSection rangeSection = target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);
                if (rangeSection.NextForDelete != TargetPointer.Null)
                {
                    return new RangeSection();
                }
                return new RangeSection(rangeSection);
            }
            return new RangeSection();
        }
    }

    private JitManager GetJitManager(Data.RangeSection rangeSectionData)
    {
        if (rangeSectionData.R2RModule == TargetPointer.Null)
        {
            return _eeJitManager;
        }
        else
        {
            return _r2rJitManager;
        }
    }

    private EECodeInfo? GetEECodeInfo(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionLookupAlgorithm, jittedCodeAddress);
        if (range.Data == null)
        {
            return null;
        }
        JitManager jitManager = GetJitManager(range.Data);
        if (jitManager.GetMethodInfo(range, jittedCodeAddress, out EECodeInfo? info))
        {
            return info;
        }
        else
        {
            return null;
        }
    }
    EECodeInfoHandle? IExecutionManager.GetEECodeInfoHandle(TargetCodePointer ip)
    {
        // TODO: some kind of cache based on ip, too?
        // we don't know if the ip is the start of the code.
        EECodeInfo? info = GetEECodeInfo(ip);
        if (info == null || !info.Valid)
        {
            return null;
        }
        TargetPointer key = info.CodeHeaderAddress;
        _codeInfos.TryAdd(key, info);
        return new EECodeInfoHandle(key);
    }

    TargetPointer IExecutionManager.GetMethodDesc(EECodeInfoHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out EECodeInfo? info))
        {
            throw new InvalidOperationException("EECodeInfo not found");
        }
        return info.MethodDescAddress;
    }

    TargetCodePointer IExecutionManager.GetStartAddress(EECodeInfoHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out EECodeInfo? info))
        {
            throw new InvalidOperationException("EECodeInfo not found");
        }
        return info.StartAddress;
    }
}
