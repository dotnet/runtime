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

        public ReadyToRunJitManager(Target target) : base(target)
        {
            _runtimeFunctionSize = Target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value;
            _lookup = PtrHashMapLookup.Create(target);
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

            int index = GetRuntimeFunctionIndexForAddress(r2rInfo, relativeAddr);
            if (index < 0)
                return false;

            bool featureEHFunclets = Target.ReadGlobal<byte>(Constants.Globals.FeatureEHFunclets) != 0;
            if (featureEHFunclets)
            {
                // TODO: [cdac] Look up in hot/cold mapping lookup table and if the method is in the cold block,
                // get the index of the associated hot block.
                //   HotColdMappingLookupTable::LookupMappingForMethod
                //
                // while GetMethodDescForEntryPoint for the begin address of function at index is null
                //   index--
            }

            TargetPointer functionEntry = r2rInfo.RuntimeFunctions + (ulong)(index * _runtimeFunctionSize);
            Data.RuntimeFunction function = Target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(functionEntry);

            // ReadyToRunInfo::GetMethodDescForEntryPointInNativeImage
            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetPointer entryPoint = CodePointerUtils.AddressFromCodePointer(startAddress, Target);

            TargetPointer methodDesc = _lookup.GetValue(r2rInfo.EntryPointToMethodDescMap, entryPoint);
            Debug.Assert(methodDesc != TargetPointer.Null);

            // TODO: [cdac] Handle method with cold code when computing relative offset
            // ReadyToRunJitManager::JitTokenToMethodRegionInfo
            TargetNUInt relativeOffset = new TargetNUInt(addr - startAddress);

            info = new CodeBlock(startAddress.Value, methodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
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

        private int GetRuntimeFunctionIndexForAddress(Data.ReadyToRunInfo r2rInfo, TargetPointer relativeAddress)
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
                    return (int)i;
            }

            return -1;
        }

        private Data.RuntimeFunction GetRuntimeFunction(Data.ReadyToRunInfo r2rInfo, uint index)
        {
            TargetPointer first = r2rInfo.RuntimeFunctions;
            TargetPointer addr = first + (ulong)(index * _runtimeFunctionSize);
            return Target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(addr);
        }
    }
}
