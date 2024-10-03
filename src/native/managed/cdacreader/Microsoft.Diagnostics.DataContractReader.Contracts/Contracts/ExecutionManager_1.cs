// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ExecutionManager_1 : IExecutionManager
{
    internal readonly Target _target;

    // maps EECodeInfoHandle.Address (which is the CodeHeaderAddress) to the EECodeInfo
    private readonly Dictionary<TargetPointer, EECodeInfo> _codeInfos = new();
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly ExecutionManagerHelpers.RangeSectionMap _rangeSectionMapLookup;
    private readonly EEJitManager _eeJitManager;
    private readonly ReadyToRunJitManager _r2rJitManager;

    public ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionMapLookup = ExecutionManagerHelpers.RangeSectionMap.Create(_target);
        ExecutionManagerHelpers.NibbleMap nibbleMap = ExecutionManagerHelpers.NibbleMap.Create(_target);
        _eeJitManager = new EEJitManager(_target, nibbleMap);
        _r2rJitManager = new ReadyToRunJitManager(_target);
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

        internal static RangeSection Find(Target target, Data.RangeSectionMap topRangeSectionMap, ExecutionManagerHelpers.RangeSectionMap rangeSectionLookup, TargetCodePointer jittedCodeAddress)
        {
            TargetPointer rangeSectionFragmentPtr = rangeSectionLookup.FindFragment(target, topRangeSectionMap, jittedCodeAddress);
            // The lowest level of the range section map covers a large address space which may contain multiple small fragments.
            // Iterate over them to find the one that contains the jitted code address.
            while (rangeSectionFragmentPtr != TargetPointer.Null)
            {
                Data.RangeSectionFragment curFragment = target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
                if (curFragment.Contains(jittedCodeAddress))
                {
                    break;
                }
                rangeSectionFragmentPtr = curFragment.Next;
            }
            if (rangeSectionFragmentPtr == TargetPointer.Null)
            {
                return new RangeSection();
            }
            Data.RangeSectionFragment fragment = target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(rangeSectionFragmentPtr);
            Data.RangeSection rangeSection = target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);
            if (rangeSection.NextForDelete != TargetPointer.Null)
            {
                return new RangeSection();
            }
            return new RangeSection(rangeSection);
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
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, jittedCodeAddress);
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
