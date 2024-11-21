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
        private readonly PtrHashMapLookup _hashMap;
        private readonly HotColdLookup _hotCold;
        private readonly RuntimeFunctionLookup _runtimeFunctions;

        public ReadyToRunJitManager(Target target) : base(target)
        {
            _runtimeFunctionSize = Target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value;
            _hashMap = PtrHashMapLookup.Create(target);
            _hotCold = HotColdLookup.Create(target);
            _runtimeFunctions = RuntimeFunctionLookup.Create(target);
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
            if (!_runtimeFunctions.TryGetRuntimeFunctionIndexForAddress(r2rInfo.RuntimeFunctions, r2rInfo.NumRuntimeFunctions, relativeAddr, out index))
                return false;

            bool featureEHFunclets = Target.ReadGlobal<byte>(Constants.Globals.FeatureEHFunclets) != 0;
            if (featureEHFunclets)
            {
                // Look up index in hot/cold map - if the function is in the cold part, get the index of the hot part.
                index = _hotCold.GetHotFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index);
            }

            TargetPointer methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            while (featureEHFunclets && methodDesc == TargetPointer.Null)
            {
                index--;
                methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            }

            Debug.Assert(methodDesc != TargetPointer.Null);
            Data.RuntimeFunction function = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, index);

            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetNUInt relativeOffset = new TargetNUInt(addr - startAddress);

            // Take any cold code into account for the relative offset
            if (_hotCold.TryGetColdFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index, out uint hotColdMapIndex, out uint coldFunctionIndex))
            {
                // ReadyToRunJitManager::JitTokenToMethodRegionInfo
                Data.RuntimeFunction coldFunction = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, coldFunctionIndex);
                TargetPointer coldStart = imageBase + coldFunction.BeginAddress;
                if (addr >= coldStart)
                {
                    uint nextColdFunctionIndex = hotColdMapIndex == r2rInfo.NumHotColdMap - 2
                        ? r2rInfo.NumRuntimeFunctions - 1
                        : Target.Read<uint>(r2rInfo.HotColdMap + (hotColdMapIndex + 2) * sizeof(uint)) - 1;
                    Data.RuntimeFunction nextColdFunction = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, nextColdFunctionIndex);
                    uint coldSize = nextColdFunction.BeginAddress + _runtimeFunctions.GetFunctionLength(nextColdFunction) - coldFunction.BeginAddress;
                    if (coldSize > 0)
                    {
                        uint hotSize = _runtimeFunctions.GetFunctionLength(function);
                        relativeOffset = new TargetNUInt(hotSize + addr - coldStart);
                    }
                }
            }

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

        private TargetPointer GetMethodDescForRuntimeFunction(Data.ReadyToRunInfo r2rInfo, TargetPointer imageBase, uint runtimeFunctionIndex)
        {
            Data.RuntimeFunction function = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, runtimeFunctionIndex);

            // ReadyToRunInfo::GetMethodDescForEntryPointInNativeImage
            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetPointer entryPoint = CodePointerUtils.AddressFromCodePointer(startAddress, Target);

            TargetPointer methodDesc = _hashMap.GetValue(r2rInfo.EntryPointToMethodDescMap, entryPoint);
            if (methodDesc == (ulong)HashMapLookup.SpecialKeys.InvalidEntry)
                return TargetPointer.Null;

            return methodDesc;
        }
    }
}
