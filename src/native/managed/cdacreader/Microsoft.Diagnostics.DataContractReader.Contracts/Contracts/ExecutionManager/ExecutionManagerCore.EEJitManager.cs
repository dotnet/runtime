// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            if (rangeSection.IsRangeList)
                return false;

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return false;

            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);
            TargetNUInt relativeOffset = new TargetNUInt(jittedCodeAddress.Value - codeStart.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader))
                return false;

            info = new CodeBlock(codeStart.Value, realCodeHeader.MethodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
        }

        public override TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            // TODO: This only works with funclets enabled. See runtime definition of RealCodeHeader for more info.
            if (rangeSection.IsRangeList)
                return TargetPointer.Null;
            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            TargetPointer codeStart = FindMethodCode(rangeSection, jittedCodeAddress);
            if (codeStart == TargetPointer.Null)
                return TargetPointer.Null;
            Debug.Assert(codeStart.Value <= jittedCodeAddress.Value);

            if (!GetRealCodeHeader(rangeSection, codeStart, out Data.RealCodeHeader? realCodeHeader))
                return TargetPointer.Null;

            if (realCodeHeader.NumUnwindInfos is not uint numUnwindInfos)
            {
                throw new InvalidOperationException("Unable to get NumUnwindInfos");
            }
            if (realCodeHeader.UnwindInfos is not TargetPointer unwindInfos)
            {
                throw new InvalidOperationException("Unable to get NumUnwindInfos");
            }

            if (numUnwindInfos == 0)
            {
                return TargetPointer.Null;
            }

            // Find the relative address that we are looking for
            TargetPointer addr = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            TargetPointer imageBase = rangeSection.Data.RangeBegin;
            TargetPointer relativeAddr = addr - imageBase;

            if (!_runtimeFunctions.TryGetRuntimeFunctionIndexForAddress(unwindInfos, numUnwindInfos, relativeAddr, out uint index))
                return TargetPointer.Null;

            return _runtimeFunctions.GetRuntimeFunctionAddress(unwindInfos, index);
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

        private bool GetRealCodeHeader(RangeSection rangeSection, TargetPointer codeStart, [NotNullWhen(true)] out Data.RealCodeHeader? realCodeHeader)
        {
            realCodeHeader = null;
            // EEJitManager::JitCodeToMethodInfo
            if (rangeSection.IsRangeList)
                return false;

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            if (codeStart == TargetPointer.Null)
                return false;

            // See EEJitManager::GetCodeHeaderFromStartAddress in vm/codeman.h
            int codeHeaderOffset = Target.PointerSize;
            TargetPointer codeHeaderIndirect = new TargetPointer(codeStart - (ulong)codeHeaderOffset);
            if (RangeSection.IsStubCodeBlock(Target, codeHeaderIndirect))
            {
                return false;
            }
            TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);
            realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
            return true;
        }
    }
}
