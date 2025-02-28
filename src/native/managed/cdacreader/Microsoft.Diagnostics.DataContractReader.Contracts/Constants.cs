// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

public static class Constants
{
    public static class Globals
    {
        // See src/coreclr/debug/runtimeinfo/datadescriptor.h
        public const string AppDomain = nameof(AppDomain);
        public const string ThreadStore = nameof(ThreadStore);
        public const string FinalizerThread = nameof(FinalizerThread);
        public const string GCThread = nameof(GCThread);

        public const string FeatureCOMInterop = nameof(FeatureCOMInterop);
        public const string FeatureEHFunclets = nameof(FeatureEHFunclets);

        public const string ObjectToMethodTableUnmask = nameof(ObjectToMethodTableUnmask);
        public const string SOSBreakingChangeVersion = nameof(SOSBreakingChangeVersion);

        public const string ExceptionMethodTable = nameof(ExceptionMethodTable);
        public const string FreeObjectMethodTable = nameof(FreeObjectMethodTable);
        public const string ObjectMethodTable = nameof(ObjectMethodTable);
        public const string ObjectArrayMethodTable = nameof(ObjectArrayMethodTable);
        public const string StringMethodTable = nameof(StringMethodTable);

        public const string MiniMetaDataBuffAddress = nameof(MiniMetaDataBuffAddress);
        public const string MiniMetaDataBuffMaxSize = nameof(MiniMetaDataBuffMaxSize);

        public const string StressLogEnabled = nameof(StressLogEnabled);
        public const string StressLogHasModuleTable = nameof(StressLogHasModuleTable);
        public const string StressLog = nameof(StressLog);
        public const string StressLogModuleTable = nameof(StressLogModuleTable);
        public const string StressLogMaxModules = nameof(StressLogMaxModules);
        public const string StressLogChunkMaxSize = nameof(StressLogChunkMaxSize);
        public const string StressLogMaxMessageSize = nameof(StressLogMaxMessageSize);
        public const string StressLogChunkSize = nameof(StressLogChunkSize);
        public const string StressLogValidChunkSig = nameof(StressLogValidChunkSig);

        public const string MethodDescAlignment = nameof(MethodDescAlignment);
        public const string ObjectHeaderSize = nameof(ObjectHeaderSize);
        public const string SyncBlockValueToObjectOffset = nameof(SyncBlockValueToObjectOffset);

        public const string SyncTableEntries = nameof(SyncTableEntries);

        public const string ArrayBoundsZero = nameof(ArrayBoundsZero);

        public const string MethodDescTokenRemainderBitCount = nameof(MethodDescTokenRemainderBitCount);
        public const string DirectorySeparator = nameof(DirectorySeparator);

        public const string ExecutionManagerCodeRangeMapAddress = nameof(ExecutionManagerCodeRangeMapAddress);
        public const string StubCodeBlockLast = nameof(StubCodeBlockLast);
        public const string PlatformMetadata = nameof(PlatformMetadata);
        public const string ProfilerControlBlock = nameof(ProfilerControlBlock);

        public const string HashMapSlotsPerBucket = nameof(HashMapSlotsPerBucket);
        public const string HashMapValueMask = nameof(HashMapValueMask);
    }
    public static class FieldNames
    {
        public static class Array
        {
            public const string NumComponents = $"m_{nameof(NumComponents)}";
        }

        public static class ModuleLookupMap
        {
            public const string TableData = nameof(TableData);
        }
    }
}
