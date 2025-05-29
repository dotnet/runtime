// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed partial class ExecutionManagerCore<T> : IExecutionManager
    where T : INibbleMap
{
    internal readonly Target _target;

    // maps CodeBlockHandle.Address (which is the CodeHeaderAddress) to the CodeBlock
    private readonly Dictionary<TargetPointer, CodeBlock> _codeInfos = new();
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly ExecutionManagerHelpers.RangeSectionMap _rangeSectionMapLookup;
    private readonly EEJitManager _eeJitManager;
    private readonly ReadyToRunJitManager _r2rJitManager;

    public ExecutionManagerCore(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionMapLookup = ExecutionManagerHelpers.RangeSectionMap.Create(_target);
        INibbleMap nibbleMap = T.Create(_target);
        _eeJitManager = new EEJitManager(_target, nibbleMap);
        _r2rJitManager = new ReadyToRunJitManager(_target);
    }

    // Note, because of RelativeOffset, this code info is per code pointer, not per method
    private sealed class CodeBlock
    {
        public TargetCodePointer StartAddress { get; }
        public TargetPointer MethodDescAddress { get; }
        public TargetPointer JitManagerAddress { get; }
        public TargetNUInt RelativeOffset { get; }
        public CodeBlock(TargetCodePointer startAddress, TargetPointer methodDesc, TargetNUInt relativeOffset, TargetPointer jitManagerAddress)
        {
            StartAddress = startAddress;
            MethodDescAddress = methodDesc;
            RelativeOffset = relativeOffset;
            JitManagerAddress = jitManagerAddress;
        }

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

        public abstract bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info);
        public abstract TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress);
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
            byte stubCodeBlockLast = target.ReadGlobal<byte>(Constants.Globals.StubCodeBlockLast);
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

    private CodeBlock? GetCodeBlock(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, jittedCodeAddress);
        if (range.Data == null)
        {
            return null;
        }
        JitManager jitManager = GetJitManager(range.Data);
        if (jitManager.GetMethodInfo(range, jittedCodeAddress, out CodeBlock? info))
        {
            return info;
        }
        else
        {
            return null;
        }
    }
    CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
    {
        TargetPointer key = ip.AsTargetPointer; // FIXME: thumb bit. It's harmless (we potentialy have 2 cache entries per IP), but we should fix it
        if (_codeInfos.ContainsKey(key))
        {
            return new CodeBlockHandle(key);
        }
        CodeBlock? info = GetCodeBlock(ip);
        if (info == null || !info.Valid)
        {
            return null;
        }
        _codeInfos.TryAdd(key, info);
        return new CodeBlockHandle(key);
    }

    TargetPointer IExecutionManager.GetMethodDesc(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.MethodDescAddress;
    }

    TargetCodePointer IExecutionManager.GetStartAddress(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.StartAddress;
    }

    TargetPointer IExecutionManager.GetUnwindInfo(CodeBlockHandle codeInfoHandle, TargetCodePointer ip)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, ip);
        if (range.Data == null)
            return TargetPointer.Null;

        JitManager jitManager = GetJitManager(range.Data);

        return jitManager.GetUnwindInfo(range, ip);
    }

    TargetPointer IExecutionManager.GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, new TargetCodePointer(codeInfoHandle.Address));
        if (range.Data == null)
            throw new InvalidOperationException($"{nameof(RangeSection)} not found for {codeInfoHandle.Address}");

        return range.Data.RangeBegin;
    }
}
