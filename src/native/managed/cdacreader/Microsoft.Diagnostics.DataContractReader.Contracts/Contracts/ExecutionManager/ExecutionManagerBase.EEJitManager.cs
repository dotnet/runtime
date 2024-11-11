// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class ExecutionManagerBase<T> : IExecutionManager
{
    private class EEJitManager : JitManager
    {
        private readonly INibbleMap _nibbleMap;
        public EEJitManager(Target target, INibbleMap nibbleMap) : base(target)
        {
            _nibbleMap = nibbleMap;
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
        {
            info = null;
            // EEJitManager::JitCodeToMethodInfo
            if (rangeSection.IsRangeList)
                return false;

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer start = FindMethodCode(rangeSection, jittedCodeAddress);
            if (start == TargetPointer.Null)
                return false;

            Debug.Assert(start.Value <= jittedCodeAddress.Value);
            TargetNUInt relativeOffset = new TargetNUInt(jittedCodeAddress.Value - start.Value);
            // See EEJitManager::GetCodeHeaderFromStartAddress in vm/codeman.h
            int codeHeaderOffset = Target.PointerSize;
            TargetPointer codeHeaderIndirect = new TargetPointer(start - (ulong)codeHeaderOffset);
            if (RangeSection.IsStubCodeBlock(Target, codeHeaderIndirect))
            {
                return false;
            }
            TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);
            Data.RealCodeHeader realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
            info = new CodeBlock(start.Value, realCodeHeader.MethodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
        }

        private TargetPointer FindMethodCode(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            // EEJitManager::FindMethodCode
            Debug.Assert(rangeSection.Data != null);

            if (!rangeSection.IsCodeHeap)
                throw new InvalidOperationException("RangeSection is not a code heap");

            TargetPointer heapListAddress = rangeSection.Data.HeapList;
            Data.CodeHeapListNode heapListNode = Target.ProcessedData.GetOrAdd<Data.CodeHeapListNode>(heapListAddress);
            return _nibbleMap.FindMethodCode(heapListNode, jittedCodeAddress);
        }
    }
}
