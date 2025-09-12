// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private enum JITTypes
    {
        TYPE_UNKNOWN = 0,
        TYPE_JIT = 1,
        TYPE_PJIT = 2,
        TYPE_INTERPRETER = 3
    };

    private abstract class JitManager
    {
        public Target Target { get; }

        protected JitManager(Target target)
        {
            Target = target;
        }

        public abstract bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info);
        public abstract void GetMethodRegionInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out uint hotSize, out TargetPointer coldStart, out uint coldSize);
        public abstract TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress);
        public abstract TargetPointer GetDebugInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out bool hasFlagByte);
        public abstract void GetGCInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out TargetPointer gcInfo, out uint gcVersion);
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

    TargetCodePointer IExecutionManager.GetFuncletStartAddress(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            throw new InvalidOperationException("Unable to get runtime function address");

        JitManager jitManager = GetJitManager(range.Data);
        TargetPointer runtimeFunctionPtr = jitManager.GetUnwindInfo(range, codeInfoHandle.Address.Value);

        if (runtimeFunctionPtr == TargetPointer.Null)
            throw new InvalidOperationException("Unable to get runtime function address");

        Data.RuntimeFunction runtimeFunction = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(runtimeFunctionPtr);

        // TODO(cdac): EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS, implement iterating over fragments until finding
        // non-fragment RuntimeFunction

        return range.Data.RangeBegin + runtimeFunction.BeginAddress;
    }

    void IExecutionManager.GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize)
    {
        hotSize = 0;
        coldStart = TargetPointer.Null;
        coldSize = 0;

        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return;

        JitManager jitManager = GetJitManager(range.Data);

        jitManager.GetMethodRegionInfo(range, codeInfoHandle.Address.Value, out hotSize, out coldStart, out coldSize);
    }

    uint IExecutionManager.GetJITType(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return 0;

        JitManager jitManager = GetJitManager(range.Data);

        if (jitManager == _eeJitManager)
        {
            return (uint)JITTypes.TYPE_JIT;
        }
        else if (jitManager == _r2rJitManager)
        {
            return (uint)JITTypes.TYPE_PJIT;
        }
        else
        {
            return (uint)JITTypes.TYPE_UNKNOWN;
        }
    }

    TargetPointer IExecutionManager.NonVirtualEntry2MethodDesc(TargetCodePointer ip)
    {
        Debug.Assert(GetCodeBlock(ip) == null);
        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, ip);
        if (range.Data == null)
            return TargetPointer.Null;
        if (range.IsRangeList)
        {
            IPrecodeStubs precodeStubs = _target.Contracts.PrecodeStubs;
            return precodeStubs.GetMethodDescFromStubAddress(ip);
        }
        return TargetPointer.Null;
    }
    TargetPointer IExecutionManager.GetUnwindInfo(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return TargetPointer.Null;

        JitManager jitManager = GetJitManager(range.Data);

        return jitManager.GetUnwindInfo(range, codeInfoHandle.Address.Value);
    }

    TargetPointer IExecutionManager.GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle)
    {
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            throw new InvalidOperationException($"{nameof(RangeSection)} not found for {codeInfoHandle.Address}");

        return range.Data.RangeBegin;
    }

    TargetPointer IExecutionManager.GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte)
    {
        hasFlagByte = false;
        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return TargetPointer.Null;

        JitManager jitManager = GetJitManager(range.Data);
        return jitManager.GetDebugInfo(range, codeInfoHandle.Address.Value, out hasFlagByte);
    }

    void IExecutionManager.GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion)
    {
        gcInfo = TargetPointer.Null;
        gcVersion = 0;

        RangeSection range = RangeSectionFromCodeBlockHandle(codeInfoHandle);
        if (range.Data == null)
            return;

        JitManager jitManager = GetJitManager(range.Data);
        jitManager.GetGCInfo(range, codeInfoHandle.Address.Value, out gcInfo, out gcVersion);
    }


    TargetNUInt IExecutionManager.GetRelativeOffset(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        return info.RelativeOffset;
    }

    private RangeSection RangeSectionFromCodeBlockHandle(CodeBlockHandle codeInfoHandle)
    {
        if (!_codeInfos.TryGetValue(codeInfoHandle.Address, out CodeBlock? info))
            throw new InvalidOperationException($"{nameof(CodeBlock)} not found for {codeInfoHandle.Address}");

        RangeSection range = RangeSection.Find(_target, _topRangeSectionMap, _rangeSectionMapLookup, codeInfoHandle.Address.Value);
        return range;
    }
}
