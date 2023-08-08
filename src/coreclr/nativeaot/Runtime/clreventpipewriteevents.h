
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

BOOL EventPipeEventEnabledDestroyGCHandle(void);
ULONG EventPipeWriteEventDestroyGCHandle(
    const void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledExceptionThrown_V1(void);
ULONG EventPipeWriteEventExceptionThrown_V1(
    const WCHAR* ExceptionType,
    const WCHAR* ExceptionMessage,
    const void*  ExceptionEIP,
    const unsigned int  ExceptionHRESULT,
    const unsigned short  ExceptionFlags,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkEdge(void);
ULONG EventPipeWriteEventGCBulkEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkMovedObjectRanges(void);
ULONG EventPipeWriteEventGCBulkMovedObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkNode(void);
ULONG EventPipeWriteEventGCBulkNode(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkRCW(void);
ULONG EventPipeWriteEventGCBulkRCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkRootCCW(void);
ULONG EventPipeWriteEventGCBulkRootCCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkRootConditionalWeakTableElementEdge(void);
ULONG EventPipeWriteEventGCBulkRootConditionalWeakTableElementEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkRootEdge(void);
ULONG EventPipeWriteEventGCBulkRootEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkSurvivingObjectRanges(void);
ULONG EventPipeWriteEventGCBulkSurvivingObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCCreateConcurrentThread_V1(void);
ULONG EventPipeWriteEventGCCreateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCCreateSegment_V1(void);
ULONG EventPipeWriteEventGCCreateSegment_V1(
    const unsigned __int64  Address,
    const unsigned __int64  Size,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCEnd_V1(void);
ULONG EventPipeWriteEventGCEnd_V1(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCFreeSegment_V1(void);
ULONG EventPipeWriteEventGCFreeSegment_V1(
    const unsigned __int64  Address,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCGenerationRange(void);
ULONG EventPipeWriteEventGCGenerationRange(
    const unsigned char  Generation,
    const void*  RangeStart,
    const unsigned __int64  RangeUsedLength,
    const unsigned __int64  RangeReservedLength,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCHeapStats_V1(void);
ULONG EventPipeWriteEventGCHeapStats_V1(
    const unsigned __int64  GenerationSize0,
    const unsigned __int64  TotalPromotedSize0,
    const unsigned __int64  GenerationSize1,
    const unsigned __int64  TotalPromotedSize1,
    const unsigned __int64  GenerationSize2,
    const unsigned __int64  TotalPromotedSize2,
    const unsigned __int64  GenerationSize3,
    const unsigned __int64  TotalPromotedSize3,
    const unsigned __int64  FinalizationPromotedSize,
    const unsigned __int64  FinalizationPromotedCount,
    const unsigned int  PinnedObjectCount,
    const unsigned int  SinkBlockCount,
    const unsigned int  GCHandleCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCJoin_V2(void);
ULONG EventPipeWriteEventGCJoin_V2(
    const unsigned int  Heap,
    const unsigned int  JoinTime,
    const unsigned int  JoinType,
    const unsigned short  ClrInstanceID,
    const unsigned int  JoinID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCMarkFinalizeQueueRoots(void);
ULONG EventPipeWriteEventGCMarkFinalizeQueueRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCMarkHandles(void);
ULONG EventPipeWriteEventGCMarkHandles(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCMarkOlderGenerationRoots(void);
ULONG EventPipeWriteEventGCMarkOlderGenerationRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCMarkStackRoots(void);
ULONG EventPipeWriteEventGCMarkStackRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCMarkWithType(void);
ULONG EventPipeWriteEventGCMarkWithType(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const unsigned int  Type,
    const unsigned __int64  Bytes,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCPerHeapHistory_V3(void);
ULONG EventPipeWriteEventGCPerHeapHistory_V3(
    const unsigned short  ClrInstanceID,
    const void*  FreeListAllocated,
    const void*  FreeListRejected,
    const void*  EndOfSegAllocated,
    const void*  CondemnedAllocated,
    const void*  PinnedAllocated,
    const void*  PinnedAllocatedAdvance,
    const unsigned int  RunningFreeListEfficiency,
    const unsigned int  CondemnReasons0,
    const unsigned int  CondemnReasons1,
    const unsigned int  CompactMechanisms,
    const unsigned int  ExpandMechanisms,
    const unsigned int  HeapIndex,
    const void*  ExtraGen0Commit,
    const unsigned int  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCTerminateConcurrentThread_V1(void);
ULONG EventPipeWriteEventGCTerminateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCTriggered(void);
ULONG EventPipeWriteEventGCTriggered(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledModuleLoad_V2(void);
ULONG EventPipeWriteEventModuleLoad_V2(
    const unsigned __int64  ModuleID,
    const unsigned __int64  AssemblyID,
    const unsigned int  ModuleFlags,
    const unsigned int  Reserved1,
    const WCHAR*  ModuleILPath,
    const WCHAR*  ModuleNativePath,
    const unsigned short  ClrInstanceID,
    const GUID* ManagedPdbSignature,
    const unsigned int  ManagedPdbAge,
    const WCHAR*  ManagedPdbBuildPath,
    const GUID* NativePdbSignature,
    const unsigned int  NativePdbAge,
    const WCHAR*  NativePdbBuildPath,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledSetGCHandle(void);
ULONG EventPipeWriteEventSetGCHandle(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCStart_V2(void);
ULONG EventPipeWriteEventGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId,// = nullptr,
    const GUID * RelatedActivityId// = nullptr
);
BOOL EventPipeEventEnabledGCRestartEEEnd_V1(void);
ULONG EventPipeWriteEventGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCRestartEEBegin_V1(void);
ULONG EventPipeWriteEventGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSuspendEEEnd_V1(void);
ULONG EventPipeWriteEventGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSuspendEEBegin_V1(void);
ULONG EventPipeWriteEventGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledDecreaseMemoryPressure(void);
ULONG EventPipeWriteEventDecreaseMemoryPressure(
    const unsigned __int64  BytesFreed,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledFinalizeObject(void);
ULONG EventPipeWriteEventFinalizeObject(
    const void*  TypeID,
    const void*  ObjectID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCFinalizersBegin_V1(void);
ULONG EventPipeWriteEventGCFinalizersBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCFinalizersEnd_V1(void);
ULONG EventPipeWriteEventGCFinalizersEnd_V1(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledContentionStart_V2(void);
ULONG EventPipeWriteEventContentionStart_V2(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned __int64  LockOwnerThreadID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledContentionStop_V1(void);
ULONG EventPipeWriteEventContentionStop_V1(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const double  DurationNs,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledContentionLockCreated(void);
ULONG EventPipeWriteEventContentionLockCreated(
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadStart(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadStart(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadStop(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadStop(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadWait(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadWait(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolMinMaxThreads(void);
ULONG EventPipeWriteEventThreadPoolMinMaxThreads(
    const unsigned short  MinWorkerThreads,
    const unsigned short  MaxWorkerThreads,
    const unsigned short  MinIOCompletionThreads,
    const unsigned short  MaxIOCompletionThreads,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentSample(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentSample(
    const double  Throughput,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentAdjustment(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentAdjustment(
    const double  AverageThroughput,
    const unsigned int  NewWorkerThreadCount,
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentStats(void);
ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentStats(
    const double  Duration,
    const double  Throughput,
    const double  ThreadWave,
    const double  ThroughputWave,
    const double  ThroughputErrorEstimate,
    const double  AverageThroughputErrorEstimate,
    const double  ThroughputRatio,
    const double  Confidence,
    const double  NewControlSetting,
    const unsigned short  NewThreadWaveMagnitude,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolIOEnqueue(void);
ULONG EventPipeWriteEventThreadPoolIOEnqueue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const BOOL  MultiDequeues,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolIODequeue(void);
ULONG EventPipeWriteEventThreadPoolIODequeue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolWorkingThreadCount(void);
ULONG EventPipeWriteEventThreadPoolWorkingThreadCount(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledThreadPoolIOPack(void);
ULONG EventPipeWriteEventThreadPoolIOPack(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCAllocationTick_V4(void);
ULONG EventPipeWriteEventGCAllocationTick_V4(
    const unsigned int  AllocationAmount,
    const unsigned int  AllocationKind,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  AllocationAmount64,
    const void*  TypeID,
    const WCHAR*  TypeName,
    const unsigned int  HeapIndex,
    const void*  Address,
    const unsigned __int64  ObjectSize,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCHeapStats_V2(void);
ULONG EventPipeWriteEventGCHeapStats_V2(
    const unsigned __int64  GenerationSize0,
    const unsigned __int64  TotalPromotedSize0,
    const unsigned __int64  GenerationSize1,
    const unsigned __int64  TotalPromotedSize1,
    const unsigned __int64  GenerationSize2,
    const unsigned __int64  TotalPromotedSize2,
    const unsigned __int64  GenerationSize3,
    const unsigned __int64  TotalPromotedSize3,
    const unsigned __int64  FinalizationPromotedSize,
    const unsigned __int64  FinalizationPromotedCount,
    const unsigned int  PinnedObjectCount,
    const unsigned int  SinkBlockCount,
    const unsigned int  GCHandleCount,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  GenerationSize4,
    const unsigned __int64  TotalPromotedSize4,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSampledObjectAllocationHigh(void);
ULONG EventPipeWriteEventGCSampledObjectAllocationHigh(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSampledObjectAllocationLow(void);
ULONG EventPipeWriteEventGCSampledObjectAllocationLow(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledPinObjectAtGCTime(void);
ULONG EventPipeWriteEventPinObjectAtGCTime(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned __int64  ObjectSize,
    const WCHAR*  TypeName,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCBulkRootStaticVar(void);
ULONG EventPipeWriteEventGCBulkRootStaticVar(
    const unsigned int  Count,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledIncreaseMemoryPressure(void);
ULONG EventPipeWriteEventIncreaseMemoryPressure(
    const unsigned __int64  BytesAllocated,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCGlobalHeapHistory_V4(void);
ULONG EventPipeWriteEventGCGlobalHeapHistory_V4(
    const unsigned __int64  FinalYoungestDesired,
    const signed int  NumHeaps,
    const unsigned int  CondemnedGeneration,
    const unsigned int  Gen0ReductionCount,
    const unsigned int  Reason,
    const unsigned int  GlobalMechanisms,
    const unsigned short  ClrInstanceID,
    const unsigned int  PauseMode,
    const unsigned int  MemoryPressure,
    const unsigned int  CondemnReasons0,
    const unsigned int  CondemnReasons1,
    const unsigned int  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGenAwareBegin(void);
ULONG EventPipeWriteEventGenAwareBegin(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGenAwareEnd(void);
ULONG EventPipeWriteEventGenAwareEnd(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCLOHCompact(void);
ULONG EventPipeWriteEventGCLOHCompact(
    const unsigned short  ClrInstanceID,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCFitBucketInfo(void);
ULONG EventPipeWriteEventGCFitBucketInfo(
    const unsigned short  ClrInstanceID,
    const unsigned short  BucketKind,
    const unsigned __int64  TotalSize,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

