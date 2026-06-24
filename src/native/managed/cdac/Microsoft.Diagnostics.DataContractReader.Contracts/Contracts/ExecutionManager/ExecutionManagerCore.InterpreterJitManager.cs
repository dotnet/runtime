// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class ExecutionManagerCore<T> : IExecutionManager
{
    private sealed class InterpreterJitManager : JitManager
    {
        private readonly INibbleMap _nibbleMap;

        public InterpreterJitManager(Target target, INibbleMap nibbleMap) : base(target)
        {
            _nibbleMap = nibbleMap;
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
        {
            info = null;
            if (rangeSection.IsRangeList)
                return false;

            if (rangeSection.Data is null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return false;

            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);
            TargetNUInt relativeOffset = new TargetNUInt(jittedCodeAddress.Value - codeStart.Value);

            if (!GetInterpreterRealCodeHeader(codeStart, out Data.InterpreterRealCodeHeader? realCodeHeader))
                return false;

            info = new CodeBlock(codeStart.Value, realCodeHeader.MethodDesc, relativeOffset, rangeSection.Data.JitManager);
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
            IGCInfoHandle gcInfoHandle = gcInfo.DecodeInterpreterGCInfo(pGcInfo, gcVersion);
            hotSize = gcInfo.GetCodeLength(gcInfoHandle);
            Debug.Assert(hotSize > 0);
        }

        public override TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            // Interpreter code has no native unwind info
            return TargetPointer.Null;
        }

        public override CodeKind GetCodeKind(RangeSection rangeSection, TargetCodePointer codeAddress)
        {
            return CodeKind.Interpreter;
        }

        public override TargetPointer GetDebugInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out bool hasFlagByte)
        {
            hasFlagByte = false;
            if (rangeSection.IsRangeList || rangeSection.Data is null)
                return TargetPointer.Null;

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return TargetPointer.Null;

            if (!GetInterpreterRealCodeHeader(codeStart, out Data.InterpreterRealCodeHeader? realCodeHeader))
                return TargetPointer.Null;

            return realCodeHeader.DebugInfo;
        }

        public override void GetGCInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out TargetPointer gcInfo, out uint gcVersion)
        {
            gcInfo = TargetPointer.Null;
            gcVersion = 0;

            if (rangeSection.IsRangeList || rangeSection.Data is null)
                return;

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return;

            if (!GetInterpreterRealCodeHeader(codeStart, out Data.InterpreterRealCodeHeader? realCodeHeader))
                return;

            gcVersion = Target.ReadGlobal<uint>(Constants.Globals.GCInfoVersion);
            gcInfo = realCodeHeader.GCInfo;
        }

        public override void GetExceptionClauses(RangeSection rangeSection, CodeBlockHandle codeInfoHandle, out TargetPointer startAddr, out TargetPointer endAddr)
        {
            startAddr = TargetPointer.Null;
            endAddr = TargetPointer.Null;

            if (rangeSection.Data is null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, new TargetCodePointer(codeInfoHandle.Address));
            if (!GetInterpreterRealCodeHeader(codeStart, out Data.InterpreterRealCodeHeader? realCodeHeader))
                return;

            if (realCodeHeader.JitEHInfo is null)
                return;

            TargetNUInt numEHInfos = Target.ReadNUInt(realCodeHeader.JitEHInfo.Address - (ulong)Target.PointerSize);
            startAddr = realCodeHeader.JitEHInfo.Clauses;
            endAddr = startAddr + numEHInfos.Value * Target.GetTypeInfo(DataType.EEExceptionClause).Size!.Value;
        }

        private TargetPointer FindMethodCode(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            Debug.Assert(rangeSection.Data is not null);

            if (!rangeSection.IsCodeHeap)
                throw new InvalidOperationException("RangeSection is not a code heap");

            TargetPointer heapListAddress = rangeSection.Data.HeapList;
            Data.CodeHeapListNode heapListNode = Target.ProcessedData.GetOrAdd<Data.CodeHeapListNode>(heapListAddress);
            return _nibbleMap.FindMethodCode(heapListNode, jittedCodeAddress);
        }

        private bool GetInterpreterRealCodeHeader(TargetPointer codeStart, [NotNullWhen(true)] out Data.InterpreterRealCodeHeader? realCodeHeader)
        {
            realCodeHeader = null;
            if (codeStart == TargetPointer.Null)
                return false;

            // Same layout as EEJitManager: CodeHeader pointer lives at codeStart - pointerSize
            int codeHeaderOffset = Target.PointerSize;
            TargetPointer codeHeaderIndirect = new TargetPointer(codeStart - (ulong)codeHeaderOffset);
            if (RangeSection.IsStubCodeBlock(Target, codeHeaderIndirect))
                return false;

            TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);
            realCodeHeader = Target.ProcessedData.GetOrAdd<Data.InterpreterRealCodeHeader>(codeHeaderAddress);
            return true;
        }
    }
}
