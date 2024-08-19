// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct ExecutionManager_1 : IExecutionManager
{
    private class EEJitManager : JitManager
    {
        private readonly NibbleMap _nibbleMap;
        public EEJitManager(Target target, NibbleMap nibbleMap) : base(target)
        {
            _nibbleMap = nibbleMap;
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out EECodeInfo? info)
        {
            info = null;
            // EEJitManager::JitCodeToMethodInfo
            if (rangeSection.IsRangeList)
            {
                return false;
            }
            TargetPointer start = FindMethodCode(rangeSection, jittedCodeAddress);
            if (start == TargetPointer.Null)
            {
                return false;
            }
            Debug.Assert(start.Value <= jittedCodeAddress.Value);
            TargetNUInt relativeOffset = new TargetNUInt(jittedCodeAddress.Value - start.Value);
            int codeHeaderOffset = Target.PointerSize;
            TargetPointer codeHeaderIndirect = new TargetPointer(start - (ulong)codeHeaderOffset);
            if (RangeSection.IsStubCodeBlock(Target, codeHeaderIndirect))
            {
                return false;
            }
            TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);
            Data.RealCodeHeader realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
            info = new EECodeInfo(jittedCodeAddress, codeHeaderOffset, relativeOffset, realCodeHeader, rangeSection.Data!.JitManager);
            return true;
        }

        private TargetPointer FindMethodCode(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            // EEJitManager::FindMethodCode
            if (rangeSection.Data == null)
            {
                throw new InvalidOperationException();
            }
            if (!rangeSection.IsCodeHeap)
            {
                throw new InvalidOperationException("RangeSection is not a code heap");
            }
            TargetPointer heapListAddress = rangeSection.Data.HeapList;
            Data.HeapList heapList = Target.ProcessedData.GetOrAdd<Data.HeapList>(heapListAddress);
            if (jittedCodeAddress < heapList.StartAddress || jittedCodeAddress > heapList.EndAddress)
            {
                return TargetPointer.Null;
            }
            TargetPointer mapBase = heapList.MapBase;
            TargetPointer mapStart = heapList.HeaderMap;
            return _nibbleMap.FindMethodCode(mapBase, mapStart, jittedCodeAddress);
        }

    }
}
