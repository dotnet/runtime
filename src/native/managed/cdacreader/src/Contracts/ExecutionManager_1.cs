// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ExecutionManager_1 : IExecutionManager
{
    internal class EECodeInfo
    {
        private readonly int _codeHeaderOffset;
        // maps EECodeInfoHandle.Address (which is the CodeHeaderAddress) to the EECodeInfo

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

    // RangeFragment and RangeSection pointers have a collectible flag on the lowest bit
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


    internal readonly Target _target;
    private readonly Dictionary<TargetPointer, EECodeInfo> _codeInfos = new();
    private readonly Data.ProfControlBlock _profControlBlock;
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly TargetCodeManagerDescriptor _targetCodeManagerDescriptor;


    internal enum JitManagerKind
    {
        EEJitManager = 0,
        ReadyToRunJitManager = 1,
    }

    public ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap, Data.ProfControlBlock profControlBlock)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _targetCodeManagerDescriptor = TargetCodeManagerDescriptor.Create(target);
        _profControlBlock = profControlBlock;
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

        public bool JitCodeToMethodInfo(Target target, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info)
        {
            info = null;
            if (_rangeSection == null)
            {
                return false;
            }
            TargetPointer jitManagerAddress = _rangeSection.JitManager;
            TargetPointer r2rModule = _rangeSection.R2RModule;
            JitManagerKind jitManagerKind = r2rModule == TargetPointer.Null ? JitManagerKind.EEJitManager : JitManagerKind.ReadyToRunJitManager;
            switch (jitManagerKind)
            {
                case JitManagerKind.EEJitManager:
                    return EEJitCodeToMethodInfo(target, jitManagerAddress, jittedCodeAddress, out info);
                case JitManagerKind.ReadyToRunJitManager:
                    return ReadyToRunJitCodeToMethodInfo(target, jitManagerAddress, jittedCodeAddress, out info);
                default:
                    throw new InvalidOperationException($"Invalid JitManagerKind");
            }
        }

        private bool EEJitCodeToMethodInfo(Target target, TargetPointer jitManagerAddress, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info)
        {
            info = null;
            // EEJitManager::JitCodeToMethodInfo
            if (IsRangeList)
            {
                return false;
            }
            TargetPointer start = EEFindMethodCode(target, jittedCodeAddress);
            if (start == TargetPointer.Null)
            {
                return false;
            }
            Debug.Assert(start.Value <= jittedCodeAddress.Value);
            TargetNUInt relativeOffset = new TargetNUInt(jittedCodeAddress.Value - start.Value);
            int codeHeaderOffset = target.PointerSize;
            TargetPointer codeHeaderIndirect = new TargetPointer(start - (ulong)codeHeaderOffset);
            if (IsStubCodeBlock(target, codeHeaderIndirect))
            {
                return false;
            }
            TargetPointer codeHeaderAddress = target.ReadPointer(codeHeaderIndirect);
            Data.RealCodeHeader realCodeHeader = target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
            info = new EECodeInfo(jittedCodeAddress, codeHeaderOffset, relativeOffset, realCodeHeader, jitManagerAddress);
            return true;
        }

        private static bool IsStubCodeBlock(Target target, TargetPointer codeHeaderIndirect)
        {
            uint stubCodeBlockLast = target.ReadGlobal<uint>(Constants.Globals.StubCodeBlockLast);
            return codeHeaderIndirect.Value <= stubCodeBlockLast;
        }

        private TargetPointer EEFindMethodCode(Target target, TargetCodePointer jittedCodeAddress)
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
            Data.HeapList heapList = target.ProcessedData.GetOrAdd<Data.HeapList>(heapListAddress);
            if (jittedCodeAddress < heapList.StartAddress || jittedCodeAddress > heapList.EndAddress)
            {
                return TargetPointer.Null;
            }
            TargetPointer mapBase = heapList.MapBase;
            TargetPointer mapStart = heapList.HeaderMap;
            NibbleMap nibbleMap = NibbleMap.Create(target, (uint)target.PointerSize);
            return nibbleMap.FindMethodCode(mapBase, mapStart, jittedCodeAddress);

        }
        private bool ReadyToRunJitCodeToMethodInfo(Target target, TargetPointer jitManagerAddress, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info)
        {
            throw new NotImplementedException(); // TODO(cdac): ReadyToRunJitManager::JitCodeToMethodInfo
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
        range.JitCodeToMethodInfo(_target, jittedCodeAddress, out EECodeInfo? info);
        return info;
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
        if (!rangeSectionFragmentPtr.IsNull)
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

    bool IExecutionManager.IsReJITEnabled()
    {
        bool profEnabledReJIT = (_profControlBlock.GlobalEventMask & (ulong)Legacy.COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
        // FIXME: it is very likely this is always true in the DAC
        // Most people don't set DOTNET_ProfAPI_RejitOnAttach = 0
        // See https://github.com/dotnet/runtime/issues/106148
        bool clrConfigEnabledReJIT = true;
        return profEnabledReJIT || clrConfigEnabledReJIT;
    }
}
