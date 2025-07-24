// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class ExecutionManagerCore<T> : IExecutionManager
{
    private sealed class ReadyToRunJitManager : JitManager
    {
        private readonly PtrHashMapLookup _hashMap;
        private readonly HotColdLookup _hotCold;
        private readonly RuntimeFunctionLookup _runtimeFunctions;

        public ReadyToRunJitManager(Target target) : base(target)
        {
            _hashMap = PtrHashMapLookup.Create(target);
            _hotCold = HotColdLookup.Create(target);
            _runtimeFunctions = RuntimeFunctionLookup.Create(target);
        }

        public override bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
        {
            // ReadyToRunJitManager::JitCodeToMethodInfo
            info = default;

            Data.ReadyToRunInfo r2rInfo = GetReadyToRunInfo(rangeSection);
            if (!GetRuntimeFunction(rangeSection, r2rInfo, jittedCodeAddress, out TargetPointer imageBase, out uint index))
                return false;

            index = AdjustRuntimeFunctionIndexForHotCold(r2rInfo, index);
            index = AdjustRuntimeFunctionToMethodStart(r2rInfo, imageBase, index, out TargetPointer methodDesc);

            Data.RuntimeFunction function = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, index);

            TargetPointer addr = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            TargetCodePointer startAddress = imageBase + function.BeginAddress;
            TargetNUInt relativeOffset = new TargetNUInt(addr - startAddress);

            // Take hot/cold splitting into account for the relative offset
            if (_hotCold.TryGetColdFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index, out uint coldFunctionIndex))
            {
                Debug.Assert(coldFunctionIndex < r2rInfo.NumRuntimeFunctions);
                Data.RuntimeFunction coldFunction = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, coldFunctionIndex);
                TargetPointer coldStart = imageBase + coldFunction.BeginAddress;
                if (addr >= coldStart)
                {
                    // If the address is in the cold part, the relative offset is the size of the
                    // hot part plus the offset from the address to the start of the cold part
                    uint hotSize = _runtimeFunctions.GetFunctionLength(function);
                    relativeOffset = new TargetNUInt(hotSize + addr - coldStart);
                }
            }

            info = new CodeBlock(startAddress.Value, methodDesc, relativeOffset, rangeSection.Data!.JitManager);
            return true;
        }

        public override TargetPointer GetUnwindInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress)
        {
            // ReadyToRunJitManager::JitCodeToMethodInfo
            Data.ReadyToRunInfo r2rInfo = GetReadyToRunInfo(rangeSection);
            if (!GetRuntimeFunction(rangeSection, r2rInfo, jittedCodeAddress, out TargetPointer _, out uint index))
                return TargetPointer.Null;

            return _runtimeFunctions.GetRuntimeFunctionAddress(r2rInfo.RuntimeFunctions, index);
        }

        public override void GetGCInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, out TargetPointer gcInfo, out uint gcVersion)
        {
            gcInfo = TargetPointer.Null;
            gcVersion = 0;

            // ReadyToRunJitManager::GetGCInfoToken
            Data.ReadyToRunInfo r2rInfo = GetReadyToRunInfo(rangeSection);
            if (!GetRuntimeFunction(rangeSection, r2rInfo, jittedCodeAddress, out TargetPointer imageBase, out uint index))
                return;

            index = AdjustRuntimeFunctionIndexForHotCold(r2rInfo, index);
            index = AdjustRuntimeFunctionToMethodStart(r2rInfo, imageBase, index, out _);

            Data.RuntimeFunction runtimeFunction = _runtimeFunctions.GetRuntimeFunction(r2rInfo.RuntimeFunctions, index);

            TargetPointer unwindInfo = runtimeFunction.UnwindData + imageBase;
            uint unwindDataSize = GetUnwindDataSize();
            gcInfo = unwindInfo + unwindDataSize;
            gcVersion = GetR2RGCInfoVersion(r2rInfo);
        }

        // This function must be kept up to date with R2R version changes.
        // When the R2R version is bumped, it must be mapped to the correct GCInfo version.
        // Adding "MINIMUM_READYTORUN_MAJOR_VERSION" to ensure this is updated according
        // to instructions in readytorun.h
        private uint GetR2RGCInfoVersion(Data.ReadyToRunInfo r2rInfo)
        {
            Data.ReadyToRunHeader header = Target.ProcessedData.GetOrAdd<Data.ReadyToRunHeader>(r2rInfo.ReadyToRunHeader);

            // see readytorun.h for the versioning details
            return header.MajorVersion switch
            {
                < 11 => 3,
                >= 11 => 4,
            };
        }

        private uint GetUnwindDataSize()
        {
            RuntimeInfoArchitecture arch = Target.Contracts.RuntimeInfo.GetTargetArchitecture();
            return arch switch
            {
                RuntimeInfoArchitecture.X86 => sizeof(uint),
                _ => throw new NotSupportedException($"GetUnwindDataSize not supported for architecture: {arch}")
            };
        }

        #region RuntimeFunction Helpers

        private Data.ReadyToRunInfo GetReadyToRunInfo(RangeSection rangeSection)
        {
            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            Debug.Assert(rangeSection.Data.R2RModule != TargetPointer.Null);

            Data.Module r2rModule = Target.ProcessedData.GetOrAdd<Data.Module>(rangeSection.Data.R2RModule);
            Debug.Assert(r2rModule.ReadyToRunInfo != TargetPointer.Null);
            return Target.ProcessedData.GetOrAdd<Data.ReadyToRunInfo>(r2rModule.ReadyToRunInfo);
        }

        private bool GetRuntimeFunction(
            RangeSection rangeSection,
            Data.ReadyToRunInfo r2rInfo,
            TargetCodePointer jittedCodeAddress,
            out TargetPointer imageBase,
            out uint runtimeFunctionIndex)
        {
            imageBase = TargetPointer.Null;
            runtimeFunctionIndex = 0;

            if (rangeSection.Data == null)
                throw new ArgumentException(nameof(rangeSection));

            // Check if address is in a thunk
            if (IsStubCodeBlockThunk(rangeSection.Data, r2rInfo, jittedCodeAddress))
                return false;

            // Find the relative address that we are looking for
            TargetPointer addr = CodePointerUtils.AddressFromCodePointer(jittedCodeAddress, Target);
            imageBase = rangeSection.Data.RangeBegin;
            TargetPointer relativeAddr = addr - imageBase;

            return _runtimeFunctions.TryGetRuntimeFunctionIndexForAddress(r2rInfo.RuntimeFunctions, r2rInfo.NumRuntimeFunctions, relativeAddr, out runtimeFunctionIndex);
        }

        private uint AdjustRuntimeFunctionIndexForHotCold(Data.ReadyToRunInfo r2rInfo, uint index)
        {
            // Look up index in hot/cold map - if the function is in the cold part, get the index of the hot part.
            index = _hotCold.GetHotFunctionIndex(r2rInfo.NumHotColdMap, r2rInfo.HotColdMap, index);
            Debug.Assert(index < r2rInfo.NumRuntimeFunctions);
            return index;
        }

        private uint AdjustRuntimeFunctionToMethodStart(Data.ReadyToRunInfo r2rInfo, TargetPointer imageBase, uint index, out TargetPointer methodDesc)
        {
            methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            while (methodDesc == TargetPointer.Null)
            {
                // Funclets won't have a direct entry in the map of runtime function entry point to method desc.
                // The funclet's address (and index) will be greater than that of the corresponding function, so
                // we decrement the index to find the actual function / method desc for the funclet.
                index--;
                methodDesc = GetMethodDescForRuntimeFunction(r2rInfo, imageBase, index);
            }

            Debug.Assert(methodDesc != TargetPointer.Null);
            return index;
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

        #endregion
    }
}
