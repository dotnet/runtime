// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

public static class Constants
{
    public static class Globals
    {
        // See src/coreclr/debug/runtimeinfo/datadescriptor.inc
        public const string AppDomain = nameof(AppDomain);
        public const string SystemDomain = nameof(SystemDomain);
        public const string ThreadStore = nameof(ThreadStore);
        public const string FinalizerThread = nameof(FinalizerThread);
        public const string GCThread = nameof(GCThread);

        public const string FeatureCOMInterop = nameof(FeatureCOMInterop);
        public const string FeatureOnStackReplacement = nameof(FeatureOnStackReplacement);

        public const string ObjectToMethodTableUnmask = nameof(ObjectToMethodTableUnmask);
        public const string SOSBreakingChangeVersion = nameof(SOSBreakingChangeVersion);

        public const string ExceptionMethodTable = nameof(ExceptionMethodTable);
        public const string FreeObjectMethodTable = nameof(FreeObjectMethodTable);
        public const string ObjectMethodTable = nameof(ObjectMethodTable);
        public const string ObjectArrayMethodTable = nameof(ObjectArrayMethodTable);
        public const string StringMethodTable = nameof(StringMethodTable);

        public const string MiniMetaDataBuffAddress = nameof(MiniMetaDataBuffAddress);
        public const string MiniMetaDataBuffMaxSize = nameof(MiniMetaDataBuffMaxSize);
        public const string DacNotificationFlags = nameof(DacNotificationFlags);
        public const string OffsetOfCurrentThreadInfo = nameof(OffsetOfCurrentThreadInfo);
        public const string TlsIndexBase = nameof(TlsIndexBase);
        public const string ThinlockThreadIdDispenser = nameof(ThinlockThreadIdDispenser);

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
        public const string SizeOfGenericModeBlock = nameof(SizeOfGenericModeBlock);

        public const string MethodDescTokenRemainderBitCount = nameof(MethodDescTokenRemainderBitCount);
        public const string DirectorySeparator = nameof(DirectorySeparator);

        public const string ExecutionManagerCodeRangeMapAddress = nameof(ExecutionManagerCodeRangeMapAddress);
        public const string StubCodeBlockLast = nameof(StubCodeBlockLast);
        public const string DefaultADID = nameof(DefaultADID);
        public const string StaticsPointerMask = nameof(StaticsPointerMask);
        public const string PtrArrayOffsetToDataArray = nameof(PtrArrayOffsetToDataArray);
        public const string NumberOfTlsOffsetsNotUsedInNoncollectibleArray = nameof(NumberOfTlsOffsetsNotUsedInNoncollectibleArray);
        public const string MaxClrNotificationArgs = nameof(MaxClrNotificationArgs);
        public const string EEConfig = nameof(EEConfig);
        public const string CORDebuggerControlFlags = nameof(CORDebuggerControlFlags);
        public const string ClrNotificationArguments = nameof(ClrNotificationArguments);
        public const string PlatformMetadata = nameof(PlatformMetadata);
        public const string ProfilerControlBlock = nameof(ProfilerControlBlock);

        public const string MethodDescSizeTable = nameof(MethodDescSizeTable);

        public const string HashMapSlotsPerBucket = nameof(HashMapSlotsPerBucket);
        public const string HashMapValueMask = nameof(HashMapValueMask);

        public const string Architecture = nameof(Architecture);
        public const string OperatingSystem = nameof(OperatingSystem);

        public const string GCInfoVersion = nameof(GCInfoVersion);
        public const string GCLowestAddress = nameof(GCLowestAddress);
        public const string GCHighestAddress = nameof(GCHighestAddress);

        // Globals found on GCDescriptor
        // see src/coreclr/gc/datadescriptors/datadescriptor.inc
        public const string GCIdentifiers = nameof(GCIdentifiers);
        public const string MaxGeneration = nameof(MaxGeneration);
        public const string StructureInvalidCount = nameof(StructureInvalidCount);
        public const string NumHeaps = nameof(NumHeaps);
        public const string Heaps = nameof(Heaps);
        public const string CurrentGCState = nameof(CurrentGCState);
        public const string CFinalizeFillPointersLength = nameof(CFinalizeFillPointersLength);
        public const string TotalGenerationCount = nameof(TotalGenerationCount);

        public const string GCHeapMarkArray = nameof(GCHeapMarkArray);
        public const string GCHeapNextSweepObj = nameof(GCHeapNextSweepObj);
        public const string GCHeapBackgroundMinSavedAddr = nameof(GCHeapBackgroundMinSavedAddr);
        public const string GCHeapBackgroundMaxSavedAddr = nameof(GCHeapBackgroundMaxSavedAddr);
        public const string GCHeapAllocAllocated = nameof(GCHeapAllocAllocated);
        public const string GCHeapEphemeralHeapSegment = nameof(GCHeapEphemeralHeapSegment);
        public const string GCHeapCardTable = nameof(GCHeapCardTable);
        public const string GCHeapFinalizeQueue = nameof(GCHeapFinalizeQueue);
        public const string GCHeapGenerationTable = nameof(GCHeapGenerationTable);
        public const string GCHeapSavedSweepEphemeralSeg = nameof(GCHeapSavedSweepEphemeralSeg);
        public const string GCHeapSavedSweepEphemeralStart = nameof(GCHeapSavedSweepEphemeralStart);
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
