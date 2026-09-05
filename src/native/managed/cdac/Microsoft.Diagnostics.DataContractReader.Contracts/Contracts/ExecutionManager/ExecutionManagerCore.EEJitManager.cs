// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class ExecutionManagerCore<T> : IExecutionManager
{
    private sealed class EEJitManager : JitManager
    {
        private readonly INibbleMap _nibbleMap;
        private readonly RuntimeFunctionLookup _runtimeFunctions;
        public EEJitManager(Target target, INibbleMap nibbleMap) : base(target)
        {
            _nibbleMap = nibbleMap;
            _runtimeFunctions = RuntimeFunctionLookup.Create(target);
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
        {
            info = null;
            // EEJitManager::JitCodeToMethodInfo
            Debug.Assert(!rangeSection.IsRangeList);

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return false;

            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);
            TargetPointer hotCodeStart = GetCodeHeaderAddress(rangeSection, codeStart) + (ulong)Target.PointerSize;
            TargetPointer instrPointer = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            TargetNUInt relativeOffset = new TargetNUInt(instrPointer.Value - codeStart.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader))
                return false;

            if (codeStart != hotCodeStart)
            {
                GetMethodRegionInfo(
                    rangeSection, jittedCodeAddress, out uint hotSize, out TargetPointer coldStart, out _);
                if (coldStart == TargetPointer.Null)
                    return false;

                relativeOffset = new TargetNUInt(hotSize + relativeOffset.Value);
            }

            info = new CodeBlock(hotCodeStart, realCodeHeader.MethodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
        }

        public override void GetMethodRegionInfo(
            RangeSection rangeSection,
            TargetCodePointer jittedCodeAddress,
            out uint hotSize,
            out TargetPointer coldStart,
            out uint coldSize)
        {
            coldStart = TargetPointer.Null;
            coldSize = 0;

            IGCInfo gcInfo = Target.Contracts.GCInfo;
            GetGCInfo(rangeSection, jittedCodeAddress, out TargetPointer pGcInfo, out uint gcVersion);
            IGCInfoHandle gcInfoHandle = gcInfo.DecodePlatformSpecificGCInfo(pGcInfo, gcVersion);
            hotSize = gcInfo.GetCodeLength(gcInfoHandle);
            Debug.Assert(hotSize > 0);

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null ||
                !GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader) ||
                realCodeHeader.ColdCodeHeader is not TargetPointer coldCodeHeader ||
                coldCodeHeader == TargetPointer.Null)
            {
                return;
            }

            if (realCodeHeader.NumUnwindInfos <= 1)
                return;

            coldStart = coldCodeHeader + (ulong)Target.PointerSize;
            TargetPointer imageBase = rangeSection.Data!.RangeBegin;
            Data.RuntimeFunction finalFunction =
                _runtimeFunctions.GetRuntimeFunction(realCodeHeader.UnwindInfos, realCodeHeader.NumUnwindInfos - 1);
            TargetPointer finalFunctionStart = CodePointerUtils.AddressFromCodePointer(
                new TargetCodePointer(imageBase + finalFunction.BeginAddress), Target);
            uint finalFunctionStartOffset = checked((uint)(finalFunctionStart - imageBase));
            uint coldEndOffset = checked(
                finalFunctionStartOffset + _runtimeFunctions.GetFunctionLength(imageBase, finalFunction));
            uint coldStartOffset = checked((uint)(coldStart.Value - imageBase.Value));
            coldSize = checked(coldEndOffset - coldStartOffset);
            hotSize = checked(hotSize - coldSize);
        }

        public override TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            if (!TryGetRuntimeFunction(
                    rangeSection, jittedCodeAddress, out Data.RealCodeHeader? realCodeHeader, out _, out uint index))
                return TargetPointer.Null;

            return _runtimeFunctions.GetRuntimeFunctionAddress(realCodeHeader.UnwindInfos, index);
        }

        public override TargetPointer GetFuncletStartAddress(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            if (Target.Contracts.RuntimeInfo.GetTargetArchitecture() is not RuntimeInfoArchitecture.Arm64)
                return base.GetFuncletStartAddress(rangeSection, jittedCodeAddress);

            if (!TryGetRuntimeFunction(
                    rangeSection, jittedCodeAddress, out Data.RealCodeHeader? realCodeHeader, out TargetPointer imageBase, out uint index))
            {
                return TargetPointer.Null;
            }

            Data.RuntimeFunction function = _runtimeFunctions.GetRuntimeFunction(realCodeHeader.UnwindInfos, index);
            while (index > 0 && IsArm64FunctionFragment(imageBase, function))
            {
                function = _runtimeFunctions.GetRuntimeFunction(realCodeHeader.UnwindInfos, --index);
            }

            return CodePointerUtils.AddressFromCodePointer(
                new TargetCodePointer(imageBase + function.BeginAddress), Target);
        }

        public override bool IsFunclet(
            RangeSection rangeSection,
            TargetCodePointer jittedCodeAddress,
            TargetPointer methodStartAddress)
        {
            TargetPointer funcletStartAddress = GetFuncletStartAddress(rangeSection, jittedCodeAddress);
            if (funcletStartAddress == TargetPointer.Null)
                throw new InvalidOperationException("Unable to get runtime function address");

            if (funcletStartAddress == methodStartAddress)
                return false;

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return false;

            TargetPointer hotCodeStart = GetCodeHeaderAddress(rangeSection, codeStart) + (ulong)Target.PointerSize;
            if (codeStart == hotCodeStart ||
                Target.Contracts.RuntimeInfo.GetTargetArchitecture() is RuntimeInfoArchitecture.Arm64)
            {
                return true;
            }

            GetExceptionClauses(
                rangeSection,
                new CodeBlockHandle(jittedCodeAddress.AsTargetPointer),
                out TargetPointer clausesStart,
                out TargetPointer clausesEnd);
            if (clausesStart >= clausesEnd)
                return false;

            GetMethodRegionInfo(rangeSection, jittedCodeAddress, out uint hotSize, out TargetPointer coldStart, out _);
            uint funcletStartOffset = checked(hotSize + (uint)(funcletStartAddress - coldStart));
            Data.EEExceptionClause firstClause = Target.ProcessedData.GetOrAdd<Data.EEExceptionClause>(clausesStart);
            return firstClause.HandlerStartPC <= funcletStartOffset;
        }

        public override TargetPointer GetDebugInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out bool hasFlagByte)
        {
            hasFlagByte = false;
            Debug.Assert(!rangeSection.IsRangeList);
            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return TargetPointer.Null;
            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader))
                return TargetPointer.Null;

            bool featureOnStackReplacement = Target.Contracts.FeatureFlags.IsEnabled(RuntimeFeature.OnStackReplacement);
            Data.EEJitManager eeJitManager = Target.ProcessedData.GetOrAdd<Data.EEJitManager>(rangeSection.Data.JitManager);
            if (featureOnStackReplacement || eeJitManager.StoreRichDebugInfo)
                hasFlagByte = true;

            return realCodeHeader.DebugInfo;
        }

        public override CodeKind GetCodeKind(RangeSection rangeSection, TargetCodePointer codeAddress)
        {
            TargetPointer startAddr = FindMethodCode(rangeSection, codeAddress); // validate that the code address is within the method's code range
            if (startAddr == TargetPointer.Null)
                return CodeKind.Unknown;
            return GetCodeHeaderStubKind(rangeSection, startAddr);
        }

        public override void GetGCInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out TargetPointer gcInfo, out uint gcVersion)
        {
            gcInfo = TargetPointer.Null;
            gcVersion = 0;

            // EEJitManager::GetGCInfoToken
            Debug.Assert(!rangeSection.IsRangeList);

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return;
            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader))
                return;

            gcVersion = Target.ReadGlobal<uint>(Constants.Globals.GCInfoVersion);
            gcInfo = realCodeHeader.GCInfo;
        }

        private TargetPointer FindMethodCode(RangeSection rangeSection, TargetCodePointer codeAddress)
        {
            // EEJitManager::FindMethodCode
            Debug.Assert(rangeSection.Data != null);

            if (!rangeSection.IsCodeHeap)
                throw new InvalidOperationException("RangeSection is not a code heap");

            TargetPointer heapListAddress = rangeSection.Data.HeapList;
            Data.CodeHeapListNode heapListNode = Target.ProcessedData.GetOrAdd<Data.CodeHeapListNode>(heapListAddress);
            return _nibbleMap.FindMethodCode(heapListNode, codeAddress);
        }

        public List<TargetPointer> EnumerateFunctionTableEntries(Data.CodeHeapListNode heapListNode)
        {
            // Port of the reverse code-header walk in OutOfProcessFunctionTableCallbackEx. Starting from
            // the end of the used portion of the code heap, walk backwards through the nibble map to visit
            // each method, skip stub code blocks, and collect the RUNTIME_FUNCTION entries of the real code
            // headers. Entries are ordered by descending method start address, ascending within a method.
            uint runtimeFunctionSize = Target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value;
            List<TargetPointer> entries = [];

            TargetCodePointer current = new(heapListNode.BottomEndAddress.Value);
            while (true)
            {
                TargetPointer codeStart = _nibbleMap.FindMethodCode(heapListNode, current);
                if (codeStart == TargetPointer.Null)
                    break;

                // The real code header pointer is stored immediately before the code start.
                TargetPointer codeHeaderIndirect = codeStart - (ulong)Target.PointerSize;
                TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);

                // Only real code headers (not stub code blocks) contribute unwind info entries.
                if (!RangeSection.IsStubCodeBlock(Target, codeHeaderAddress))
                {
                    Data.RealCodeHeader realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
                    for (uint i = 0; i < realCodeHeader.NumUnwindInfos; i++)
                        entries.Add(realCodeHeader.UnwindInfos + (ulong)(i * runtimeFunctionSize));
                }

                if (codeStart.Value <= heapListNode.StartAddress.Value)
                    break;

                current = new TargetCodePointer(codeStart.Value - 1);
            }

            return entries;
        }

        private TargetPointer GetCodeHeaderAddress(RangeSection rangeSection, TargetPointer codeStart)
        {
            // EEJitManager::JitCodeToMethodInfo
            Debug.Assert(!rangeSection.IsRangeList);

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeHeaderAddress = codeStart - (ulong)Target.PointerSize;
            Data.CodeHeapListNode heapListNode =
                Target.ProcessedData.GetOrAdd<Data.CodeHeapListNode>(rangeSection.Data.HeapList);
            if (codeHeaderAddress >= heapListNode.BottomEndAddress)
            {
                // A cold-code header points back to the hot CodeHeader.
                codeHeaderAddress = Target.ReadPointer(codeHeaderAddress);
            }

            return codeHeaderAddress;
        }

        private bool GetRealCodeHeader(RangeSection rangeSection, TargetPointer codeStart, [NotNullWhen(true)] out Data.RealCodeHeader? realCodeHeader)
        {
            realCodeHeader = null;
            TargetPointer codeHeaderAddress = GetCodeHeaderAddress(rangeSection, codeStart);
            TargetPointer realCodeHeaderAddress = Target.ReadPointer(codeHeaderAddress);
            if (RangeSection.IsStubCodeBlock(Target, realCodeHeaderAddress))
            {
                return false;
            }
            realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(realCodeHeaderAddress);
            return true;
        }

        private CodeKind GetCodeHeaderStubKind(RangeSection rangeSection, TargetPointer codeStart)
        {
            TargetPointer codeHeaderAddress = GetCodeHeaderAddress(rangeSection, codeStart);
            TargetPointer realCodeHeaderAddress = Target.ReadPointer(codeHeaderAddress);
            if (RangeSection.IsStubCodeBlock(Target, realCodeHeaderAddress))
            {
                return GetStubKind((StubKind)realCodeHeaderAddress.Value);
            }
            return CodeKind.Jitted;
        }

        private bool TryGetRuntimeFunction(
            RangeSection rangeSection,
            TargetCodePointer jittedCodeAddress,
            [NotNullWhen(true)] out Data.RealCodeHeader? realCodeHeader,
            out TargetPointer imageBase,
            out uint index)
        {
            realCodeHeader = null;
            imageBase = TargetPointer.Null;
            index = 0;

            Debug.Assert(!rangeSection.IsRangeList);
            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return false;
            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out realCodeHeader) ||
                realCodeHeader.NumUnwindInfos == 0)
            {
                return false;
            }

            TargetPointer addr = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            imageBase = rangeSection.Data.RangeBegin;
            TargetPointer relativeAddr = addr - imageBase;
            return _runtimeFunctions.TryGetRuntimeFunctionIndexForAddress(
                realCodeHeader.UnwindInfos, realCodeHeader.NumUnwindInfos, relativeAddr, out index);
        }

        private bool IsArm64FunctionFragment(TargetPointer imageBase, Data.RuntimeFunction function)
        {
            if ((function.UnwindData & 3) != 0)
                return false;

            TargetPointer unwindData = imageBase + function.UnwindData;
            uint unwindHeader = Target.Read<uint>(unwindData);
            if (((unwindHeader >> 18) & 3) != 0)
                return false;

            uint epilogCount = (unwindHeader >> 22) & 0x1f;
            uint codeWords = unwindHeader >> 27;
            TargetPointer unwindCodes = unwindData + sizeof(uint);
            if (codeWords == 0 && epilogCount == 0)
            {
                uint extendedHeader = Target.Read<uint>(unwindCodes);
                epilogCount = extendedHeader & 0xffff;
                unwindCodes += sizeof(uint);
            }

            bool hasSingleEpilog = (unwindHeader & (1 << 21)) != 0;
            if (!hasSingleEpilog)
                unwindCodes += epilogCount * sizeof(uint);

            return Target.Read<byte>(unwindCodes) == 0xe5;
        }

        public override void GetExceptionClauses(RangeSection rangeSection, CodeBlockHandle codeInfoHandle, out TargetPointer startAddr, out TargetPointer endAddr)
        {
            startAddr = TargetPointer.Null;
            endAddr = TargetPointer.Null;

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            Data.RealCodeHeader? realCodeHeader;
            TargetPointer codeStart = FindMethodCode(rangeSection, new TargetCodePointer(codeInfoHandle.Address));
            if (codeStart == TargetPointer.Null)
                return;
            if (!GetRealCodeHeader(rangeSection, codeStart, out realCodeHeader) || realCodeHeader == null)
                return;

            if (realCodeHeader.EHInfo == TargetPointer.Null)
                return;

            Data.EEILException ehInfo = Target.ProcessedData.GetOrAdd<Data.EEILException>(realCodeHeader.EHInfo);
            TargetNUInt numEHInfos = Target.ReadNUInt(ehInfo.Address - (ulong)Target.PointerSize);
            startAddr = ehInfo.Clauses;
            endAddr = startAddr + numEHInfos.Value * Data.EEExceptionClause.GetSize(Target);
        }
    }
}
