// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Use genEventing.py to generate this file. Update script to handle
//        nativeaot runtime and allow generating separate declaration and
//        implementation files
// FireEtw* functions handle both EventPipe and ETW. The naming matches the
// generated output of genEventing.py used in shared code and other runtimes.
#ifndef CLR_ETW_ALL_MAIN_H
#define CLR_ETW_ALL_MAIN_H

BOOL EventEnabledDestroyGCHandle(void);
ULONG FireEtwDestroyGCHandle(
    void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledExceptionThrown_V1(void);
ULONG FireEtwExceptionThrown_V1(
    const WCHAR* ExceptionType,
    const WCHAR* ExceptionMessage,
    void*  ExceptionEIP,
    const unsigned int  ExceptionHRESULT,
    const unsigned short  ExceptionFlags,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkEdge(void);
ULONG FireEtwGCBulkEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkMovedObjectRanges(void);
ULONG FireEtwGCBulkMovedObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkNode(void);
ULONG FireEtwGCBulkNode(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkRCW(void);
ULONG FireEtwGCBulkRCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkRootCCW(void);
ULONG FireEtwGCBulkRootCCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkRootConditionalWeakTableElementEdge(void);
ULONG FireEtwGCBulkRootConditionalWeakTableElementEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkRootEdge(void);
ULONG FireEtwGCBulkRootEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkSurvivingObjectRanges(void);
ULONG FireEtwGCBulkSurvivingObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCCreateConcurrentThread_V1(void);
ULONG FireEtwGCCreateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCCreateSegment_V1(void);
ULONG FireEtwGCCreateSegment_V1(
    const unsigned __int64  Address,
    const unsigned __int64  Size,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCEnd_V1(void);
ULONG FireEtwGCEnd_V1(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCFreeSegment_V1(void);
ULONG FireEtwGCFreeSegment_V1(
    const unsigned __int64  Address,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCGenerationRange(void);
ULONG FireEtwGCGenerationRange(
    const unsigned char  Generation,
    void*  RangeStart,
    const unsigned __int64  RangeUsedLength,
    const unsigned __int64  RangeReservedLength,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCHeapStats_V1(void);
ULONG FireEtwGCHeapStats_V1(
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

BOOL EventEnabledGCJoin_V2(void);
ULONG FireEtwGCJoin_V2(
    const unsigned int  Heap,
    const unsigned int  JoinTime,
    const unsigned int  JoinType,
    const unsigned short  ClrInstanceID,
    const unsigned int  JoinID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCMarkFinalizeQueueRoots(void);
ULONG FireEtwGCMarkFinalizeQueueRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCMarkHandles(void);
ULONG FireEtwGCMarkHandles(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCMarkOlderGenerationRoots(void);
ULONG FireEtwGCMarkOlderGenerationRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCMarkStackRoots(void);
ULONG FireEtwGCMarkStackRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCMarkWithType(void);
ULONG FireEtwGCMarkWithType(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const unsigned int  Type,
    const unsigned __int64  Bytes,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCPerHeapHistory_V3(void);
ULONG FireEtwGCPerHeapHistory_V3(
    const unsigned short  ClrInstanceID,
    void*  FreeListAllocated,
    void*  FreeListRejected,
    void*  EndOfSegAllocated,
    void*  CondemnedAllocated,
    void*  PinnedAllocated,
    void*  PinnedAllocatedAdvance,
    const unsigned int  RunningFreeListEfficiency,
    const unsigned int  CondemnReasons0,
    const unsigned int  CondemnReasons1,
    const unsigned int  CompactMechanisms,
    const unsigned int  ExpandMechanisms,
    const unsigned int  HeapIndex,
    void*  ExtraGen0Commit,
    const unsigned int  Count,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCTerminateConcurrentThread_V1(void);
ULONG FireEtwGCTerminateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCTriggered(void);
ULONG FireEtwGCTriggered(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledModuleLoad_V2(void);
ULONG FireEtwModuleLoad_V2(
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

BOOL EventEnabledSetGCHandle(void);
ULONG FireEtwSetGCHandle(
    void*  HandleID,
    void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCStart_V2(void);
ULONG FireEtwGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);


BOOL EventEnabledGCRestartEEEnd_V1(void);
ULONG FireEtwGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCRestartEEBegin_V1(void);
ULONG FireEtwGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCSuspendEEEnd_V1(void);
ULONG FireEtwGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCSuspendEEBegin_V1(void);
ULONG FireEtwGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledDecreaseMemoryPressure(void);
ULONG FireEtwDecreaseMemoryPressure(
    const unsigned __int64  BytesFreed,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledFinalizeObject(void);
ULONG FireEtwFinalizeObject(
    const void*  TypeID,
    const void*  ObjectID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCFinalizersBegin_V1(void);
ULONG FireEtwGCFinalizersBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCFinalizersEnd_V1(void);
ULONG FireEtwGCFinalizersEnd_V1(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledContentionStart_V2(void);
ULONG FireEtwContentionStart_V2(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned __int64  LockOwnerThreadID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
);

BOOL EventEnabledContentionStop_V1(void);
ULONG FireEtwContentionStop_V1(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const double  DurationNs,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
);

BOOL EventEnabledContentionLockCreated(void);
ULONG FireEtwContentionLockCreated(
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned short  ClrInstanceID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolWorkerThreadStart(void);
uint32_t FireEtwThreadPoolWorkerThreadStart(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

uint32_t FireEtwThreadPoolWorkerThreadStop(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

uint32_t FireEtwThreadPoolWorkerThreadWait(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolMinMaxThreads(void);
uint32_t FireEtwThreadPoolMinMaxThreads(
    const unsigned short  MinWorkerThreads,
    const unsigned short  MaxWorkerThreads,
    const unsigned short  MinIOCompletionThreads,
    const unsigned short  MaxIOCompletionThreads,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentSample(void);
uint32_t FireEtwThreadPoolWorkerThreadAdjustmentSample(
    const double  Throughput,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentAdjustment(void);
uint32_t FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(
    const double  AverageThroughput,
    const unsigned int  NewWorkerThreadCount,
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentStats(void);
uint32_t FireEtwThreadPoolWorkerThreadAdjustmentStats(
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

BOOL EventEnabledThreadPoolIOEnqueue(void);
uint32_t FireEtwThreadPoolIOEnqueue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const BOOL  MultiDequeues,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolIODequeue(void);
uint32_t FireEtwThreadPoolIODequeue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolWorkingThreadCount(void);
uint32_t FireEtwThreadPoolWorkingThreadCount(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledThreadPoolIOPack(void);
uint32_t FireEtwThreadPoolIOPack(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCAllocationTick_V4(void);
ULONG FireEtwGCAllocationTick_V4(
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

BOOL EventEnabledGCHeapStats_V2(void);
ULONG FireEtwGCHeapStats_V2(
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

BOOL EventEnabledGCSampledObjectAllocationHigh(void);
ULONG FireEtwGCSampledObjectAllocationHigh(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCSampledObjectAllocationLow(void);
ULONG FireEtwGCSampledObjectAllocationLow(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledPinObjectAtGCTime(void);
ULONG FireEtwPinObjectAtGCTime(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned __int64  ObjectSize,
    const WCHAR*  TypeName,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCBulkRootStaticVar(void);
ULONG FireEtwGCBulkRootStaticVar(
    const unsigned int  Count,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledIncreaseMemoryPressure(void);
ULONG FireEtwIncreaseMemoryPressure(
    const unsigned __int64  BytesAllocated,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCGlobalHeapHistory_V4(void);
ULONG FireEtwGCGlobalHeapHistory_V4(
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

BOOL EventEnabledGenAwareBegin(void);
ULONG FireEtwGenAwareBegin(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGenAwareEnd(void);
ULONG FireEtwGenAwareEnd(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCLOHCompact(void);
ULONG FireEtwGCLOHCompact(
    const unsigned short  ClrInstanceID,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

BOOL EventEnabledGCFitBucketInfo(void);
ULONG FireEtwGCFitBucketInfo(
    const unsigned short  ClrInstanceID,
    const unsigned short  BucketKind,
    const unsigned __int64  TotalSize,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);

#ifdef FEATURE_ETW

// ==================================================================
// Events currently only fired via ETW (private runtime provider)
// ==================================================================

ULONG FireEtwGCSettings(
    const unsigned __int64  SegmentSize,
    const unsigned __int64  LargeObjectSegmentSize,
    const BOOL  ServerGC,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwPinPlugAtGCTime(
    const void*  PlugStart,
    const void*  PlugEnd,
    const void*  GapBeforeSize,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC1stNonConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC1stConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC2ndNonConBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC2ndNonConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC2ndConBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGC2ndConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCDrainMark(
    const unsigned __int64  Objects,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCRevisit(
    const unsigned __int64  Pages,
    const unsigned __int64  Objects,
    const unsigned int  IsLarge,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCOverflow(
    const unsigned __int64  Min,
    const unsigned __int64  Max,
    const unsigned __int64  Objects,
    const unsigned int  IsLarge,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCAllocWaitBegin(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwBGCAllocWaitEnd(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwGCFullNotify_V1(
    const unsigned int  GenNumber,
    const unsigned int  IsAlloc,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwPrvSetGCHandle(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

ULONG FireEtwPrvDestroyGCHandle(
    const void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
);

#else

#define FireEtwBGC1stConEnd(ClrInstanceID)
#define FireEtwBGC1stNonConEnd(ClrInstanceID)
#define FireEtwBGC2ndConBegin(ClrInstanceID)
#define FireEtwBGC2ndConEnd(ClrInstanceID)
#define FireEtwBGC2ndNonConBegin(ClrInstanceID)
#define FireEtwBGC2ndNonConEnd(ClrInstanceID)
#define FireEtwBGCAllocWaitBegin(Reason, ClrInstanceID)
#define FireEtwBGCAllocWaitEnd(Reason, ClrInstanceID)
#define FireEtwBGCBegin(ClrInstanceID)
#define FireEtwBGCDrainMark(Objects, ClrInstanceID)
#define FireEtwBGCOverflow(Min, Max, Objects, IsLarge, ClrInstanceID)
#define FireEtwBGCRevisit(Pages, Objects, IsLarge, ClrInstanceID)
#define FireEtwGCFullNotify_V1(GenNumber, IsAlloc, ClrInstanceID)
#define FireEtwGCSettings(SegmentSize, LargeObjectSegmentSize, ServerGC)
#define FireEtwPinPlugAtGCTime(PlugStart, PlugEnd, GapBeforeSize, ClrInstanceID)
#define FireEtwPrvDestroyGCHandle(HandleID, ClrInstanceID)
#define FireEtwPrvSetGCHandle(HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID)

#endif // FEATURE_ETW

#endif // __CLR_ETW_ALL_MAIN_H__