// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class ExecutionManagerBase<T> : IExecutionManager
{
    private class ReadyToRunJitManager : JitManager
    {
        private readonly uint _runtimeFunctionSize;
        private readonly PtrHashMapLookup _lookup;
        private readonly HotColdLookup _hotColdLookup;

        public ReadyToRunJitManager(Target target) : base(target)
        {
            _runtimeFunctionSize = Target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value;
            _lookup = PtrHashMapLookup.Create(target);
            _hotColdLookup = HotColdLookup.Create(target);
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
        {
            // ReadyToRunJitManager::JitCodeToMethodInfo
            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            info = default;
            Debug.Assert(rangeSection.Data.R2RModule != TargetPointer.Null);

            Data.Module r2rModule = Target.ProcessedData.GetOrAdd<Data.Module>(rangeSection.Data.R2RModule);
            Debug.Assert(r2rModule.ReadyToRunInfo != TargetPointer.Null);
            Data.ReadyToRunInfo r2rInfo = Target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(r2rModule.ReadyToRunInfo);

            // Check if address is in a thunk
            if (IsStubCodeBlockThunk(rangeSection.Data, r2rInfo, jittedCodeAddress))
                return false;

            // Find the relative address that we are looking for
            TargetPointer addr = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            TargetPointer imageBase = rangeSection.Data.RangeBegin;
            TargetPointer relativeAddr = addr - imageBase;

            uint index;
            if (!TryGetRuntimeFunctionIndexForAddress(r2rInfo, relativeAddr, out index))
                return false;

            bool featureEHFunclets = Target.ReadGlobal<byte>(Constants.Globals.FeatureEHFunclets) != 0;
            if (featureEHFunclets)
            {
                // Look up index in hot/cold map - if the function is in the cold part, get the index of the hot part.
                index = _hotColdLookup.GetHotFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index);
            }

            TargetPointer methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            while (featureEHFunclets && methodDesc == TargetPointer.Null)
            {
                index--;
                methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            }

            Debug.Assert(methodDesc != TargetPointer.Null);
            Data.RuntimeFunction function = GetRuntimeFunction(r2rInfo, index);

            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetNUInt relativeOffset = new TargetNUInt(addr - startAddress);

            // Take any cold code into account for the relative offset
            if (_hotColdLookup.TryGetColdFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index, out uint hotColdMapIndex, out uint coldFunctionIndex))
            {
                // ReadyToRunJitManager::JitTokenToMethodRegionInfo
                Data.RuntimeFunction coldFunction = GetRuntimeFunction(r2rInfo, coldFunctionIndex);
                TargetPointer coldStart = imageBase + coldFunction.BeginAddress;
                if (addr >= coldStart)
                {
                    uint nextColdFunctionIndex = hotColdMapIndex == r2rInfo.NumHotColdMap - 2
                        ? r2rInfo.NumRuntimeFunctions - 1
                        : Target.Read<uint>(r2rInfo.HotColdMap + (hotColdMapIndex + 2) * sizeof(uint)) - 1;
                    Data.RuntimeFunction nextColdFunction = GetRuntimeFunction(r2rInfo, nextColdFunctionIndex);
                    uint coldSize = nextColdFunction.BeginAddress + GetFunctionLength(nextColdFunction) - coldFunction.BeginAddress;
                    if (coldSize > 0)
                    {
                        uint hotSize = GetFunctionLength(function);
                        relativeOffset = new TargetNUInt(hotSize + addr - coldStart);
                    }
                }
            }

            info = new CodeBlock(startAddress.Value, methodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
        }

        private uint GetFunctionLength(Data.RuntimeFunction function)
        {
            if (function.EndAddress.HasValue)
                return function.EndAddress.Value - function.BeginAddress;

            Data.UnwindInfo  unwindInfo = Target.ProcessedData.GetOrAdd<Data.UnwindInfo>(function.UnwindData);
            if (unwindInfo.FunctionLength.HasValue)
                return unwindInfo.FunctionLength.Value;

            Debug.Assert(unwindInfo.Header.HasValue);

            // First 18 bits are function length / (pointer size / 2).
            // See UnwindFragmentInfo::Finalize
            uint funcLengthInHeader = unwindInfo.Header.Value & ((1 << 18) - 1);
            return (uint)(funcLengthInHeader * (Target.PointerSize / 2));
        }

        private bool IsStubCodeBlockThunk(Data.RangeSection rangeSection, Data.ReadyToRunInfo r2rInfo, TargetCodePointer jittedCodeAddress)
        {
            if (r2rInfo.DelayLoadMethodCallThunks == TargetPointer.Null)
                return false;

            // Check if the address is in the region containing thunks for READYTORUN_HELPER_DelayLoad_MethodCall
            Data.ImageDataDirectory thunksData = Target.ProcessedData.GetOrAdd<Data.ImageDataDirectory>(r2rInfo.DelayLoadMethodCallThunks);
            ulong rva = jittedCodeAddress - rangeSection.RangeBegin;
            return thunksData.VirtualAddress <= rva && rva < thunksData.VirtualAddress + thunksData.Size;
        }

        private bool TryGetRuntimeFunctionIndexForAddress(Data.ReadyToRunInfo r2rInfo, TargetPointer relativeAddress, out uint index)
        {
            // NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod
            uint start = 0;
            uint end = r2rInfo.NumRuntimeFunctions - 1;
            relativeAddress = CodePointerUtils.CodePointerFromAddress(relativeAddress, Target).AsTargetPointer;

            // Entries are sorted. Binary search until we get to 10 or fewer items.
            while (end - start > 10)
            {
                uint middle = start + (end - start) / 2;
                Data.RuntimeFunction func = GetRuntimeFunction(r2rInfo, middle);
                if (relativeAddress < func.BeginAddress)
                {
                    end = middle - 1;
                }
                else
                {
                    start = middle;
                }
            }

            // Find the runtime function that contains the address of interest
            for (uint i = start; i <= end; ++i)
            {
                // Entries are terminated by a sentinel value of -1, so we can index one past the end safely.
                // Read as a runtime function, its begin address is 0xffffffff (always > relative address).
                // See RuntimeFunctionsTableNode.GetData in RuntimeFunctionsTableNode.cs
                Data.RuntimeFunction nextFunc = GetRuntimeFunction(r2rInfo, i + 1);
                if (relativeAddress >= nextFunc.BeginAddress)
                    continue;

                Data.RuntimeFunction func = GetRuntimeFunction(r2rInfo, i);
                if (relativeAddress >= func.BeginAddress)
                {
                    index = i;
                    return true;
                }
            }

            index = ~0u;
            return false;
        }

        private Data.RuntimeFunction GetRuntimeFunction(Data.ReadyToRunInfo r2rInfo, uint index)
        {
            TargetPointer first = r2rInfo.RuntimeFunctions;
            TargetPointer addr = first + (ulong)(index * _runtimeFunctionSize);
            return Target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(addr);
        }

        private TargetPointer GetMethodDescForRuntimeFunction(Data.ReadyToRunInfo r2rInfo, TargetPointer imageBase, uint runtimeFunctionIndex)
        {
            Data.RuntimeFunction function = GetRuntimeFunction(r2rInfo, runtimeFunctionIndex);

            // ReadyToRunInfo::GetMethodDescForEntryPointInNativeImage
            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetPointer entryPoint = CodePointerUtils.AddressFromCodePointer(startAddress, Target);

            TargetPointer methodDesc = _lookup.GetValue(r2rInfo.EntryPointToMethodDescMap, entryPoint);
            if (methodDesc == (ulong)HashMapLookup.SpecialKeys.InvalidEntry)
                return TargetPointer.Null;

            return methodDesc;
        }
    }
}
