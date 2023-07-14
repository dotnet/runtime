// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Use genEventing.py to generate this file. Update script to handle
//        nativeaot runtime and allow generating separate declaration and
//        implementation files

#include <CommonTypes.h>
#include <CommonMacros.h>

#ifndef ERROR_SUCCESS
#define ERROR_SUCCESS 0L
#endif

BOOL EventEnabledDestroyGCHandle(void) { return 0; }
ULONG FireEtwDestroyGCHandle(
    void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledExceptionThrown_V1(void) { return 0; }
ULONG FireEtwExceptionThrown_V1(
    const WCHAR* ExceptionType,
    const WCHAR* ExceptionMessage,
    void*  ExceptionEIP,
    const unsigned int  ExceptionHRESULT,
    const unsigned short  ExceptionFlags,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkEdge(void) { return 0; }
ULONG FireEtwGCBulkEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkMovedObjectRanges(void) { return 0; }
ULONG FireEtwGCBulkMovedObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkNode(void) { return 0; }
ULONG FireEtwGCBulkNode(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkRCW(void) { return 0; }
ULONG FireEtwGCBulkRCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkRootCCW(void) { return 0; }
ULONG FireEtwGCBulkRootCCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkRootConditionalWeakTableElementEdge(void) { return 0; }
ULONG FireEtwGCBulkRootConditionalWeakTableElementEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkRootEdge(void) { return 0; }
ULONG FireEtwGCBulkRootEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkSurvivingObjectRanges(void) { return 0; }
ULONG FireEtwGCBulkSurvivingObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCCreateConcurrentThread_V1(void) { return 0; }
ULONG FireEtwGCCreateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCCreateSegment_V1(void) { return 0; }
ULONG FireEtwGCCreateSegment_V1(
    const unsigned __int64  Address,
    const unsigned __int64  Size,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCEnd_V1(void) { return 0; }
ULONG FireEtwGCEnd_V1(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCFreeSegment_V1(void) { return 0; }
ULONG FireEtwGCFreeSegment_V1(
    const unsigned __int64  Address,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCGenerationRange(void) { return 0; }
ULONG FireEtwGCGenerationRange(
    const unsigned char  Generation,
    void*  RangeStart,
    const unsigned __int64  RangeUsedLength,
    const unsigned __int64  RangeReservedLength,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCHeapStats_V1(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCJoin_V2(void) { return 0; }
ULONG FireEtwGCJoin_V2(
    const unsigned int  Heap,
    const unsigned int  JoinTime,
    const unsigned int  JoinType,
    const unsigned short  ClrInstanceID,
    const unsigned int  JoinID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCMarkFinalizeQueueRoots(void) { return 0; }
ULONG FireEtwGCMarkFinalizeQueueRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCMarkHandles(void) { return 0; }
ULONG FireEtwGCMarkHandles(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCMarkOlderGenerationRoots(void) { return 0; }
ULONG FireEtwGCMarkOlderGenerationRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCMarkStackRoots(void) { return 0; }
ULONG FireEtwGCMarkStackRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCMarkWithType(void) { return 0; }
ULONG FireEtwGCMarkWithType(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const unsigned int  Type,
    const unsigned __int64  Bytes,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCPerHeapHistory_V3(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCTerminateConcurrentThread_V1(void) { return 0; }
ULONG FireEtwGCTerminateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCTriggered(void) { return 0; }
ULONG FireEtwGCTriggered(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledModuleLoad_V2(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledSetGCHandle(void) { return 0; }
ULONG FireEtwSetGCHandle(
    void*  HandleID,
    void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCStart_V2(void) { return 0; }
ULONG FireEtwGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }


BOOL EventEnabledGCRestartEEEnd_V1(void) { return 0; }
ULONG FireEtwGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCRestartEEBegin_V1(void) { return 0; }
ULONG FireEtwGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCSuspendEEEnd_V1(void) { return 0; }
ULONG FireEtwGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCSuspendEEBegin_V1(void) { return 0; }
ULONG FireEtwGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledDecreaseMemoryPressure(void) { return 0; }
ULONG FireEtwDecreaseMemoryPressure(
    const unsigned __int64  BytesFreed,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledFinalizeObject(void) { return 0; }
ULONG FireEtwFinalizeObject(
    const void*  TypeID,
    const void*  ObjectID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCFinalizersBegin_V1(void) { return 0; }
ULONG FireEtwGCFinalizersBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCFinalizersEnd_V1(void) { return 0; }
ULONG FireEtwGCFinalizersEnd_V1(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledContentionStart_V2(void) { return 0; }
ULONG FireEtwContentionStart_V2(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned __int64  LockOwnerThreadID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledContentionStop_V1(void) { return 0; }
ULONG FireEtwContentionStop_V1(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const double  DurationNs,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledContentionLockCreated(void) { return 0; }
ULONG FireEtwContentionLockCreated(
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned short  ClrInstanceID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolWorkerThreadStart(void) { return 0; }
uint32_t FireEtwThreadPoolWorkerThreadStart(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

uint32_t FireEtwThreadPoolWorkerThreadStop(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

uint32_t FireEtwThreadPoolWorkerThreadWait(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolMinMaxThreads(void) { return 0; }
uint32_t FireEtwThreadPoolMinMaxThreads(
    const unsigned short  MinWorkerThreads,
    const unsigned short  MaxWorkerThreads,
    const unsigned short  MinIOCompletionThreads,
    const unsigned short  MaxIOCompletionThreads,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentSample(void) { return 0; }
uint32_t FireEtwThreadPoolWorkerThreadAdjustmentSample(
    const double  Throughput,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentAdjustment(void) { return 0; }
uint32_t FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(
    const double  AverageThroughput,
    const unsigned int  NewWorkerThreadCount,
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolWorkerThreadAdjustmentStats(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolIOEnqueue(void) { return 0; }
uint32_t FireEtwThreadPoolIOEnqueue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const BOOL  MultiDequeues,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolIODequeue(void) { return 0; }
uint32_t FireEtwThreadPoolIODequeue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolWorkingThreadCount(void) { return 0; }
uint32_t FireEtwThreadPoolWorkingThreadCount(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledThreadPoolIOPack(void) { return 0; }
uint32_t FireEtwThreadPoolIOPack(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCAllocationTick_V4(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCHeapStats_V2(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCSampledObjectAllocationHigh(void) { return 0; }
ULONG FireEtwGCSampledObjectAllocationHigh(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCSampledObjectAllocationLow(void) { return 0; }
ULONG FireEtwGCSampledObjectAllocationLow(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledPinObjectAtGCTime(void) { return 0; }
ULONG FireEtwPinObjectAtGCTime(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned __int64  ObjectSize,
    const WCHAR*  TypeName,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCBulkRootStaticVar(void) { return 0; }
ULONG FireEtwGCBulkRootStaticVar(
    const unsigned int  Count,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledIncreaseMemoryPressure(void) { return 0; }
ULONG FireEtwIncreaseMemoryPressure(
    const unsigned __int64  BytesAllocated,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCGlobalHeapHistory_V4(void) { return 0; }
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
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGenAwareBegin(void) { return 0; }
ULONG FireEtwGenAwareBegin(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGenAwareEnd(void) { return 0; }
ULONG FireEtwGenAwareEnd(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCLOHCompact(void) { return 0; }
ULONG FireEtwGCLOHCompact(
    const unsigned short  ClrInstanceID,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

BOOL EventEnabledGCFitBucketInfo(void) { return 0; }
ULONG FireEtwGCFitBucketInfo(
    const unsigned short  ClrInstanceID,
    const unsigned short  BucketKind,
    const unsigned __int64  TotalSize,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

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
)
{ return ERROR_SUCCESS; }

ULONG FireEtwPinPlugAtGCTime(
    const void*  PlugStart,
    const void*  PlugEnd,
    const void*  GapBeforeSize,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC1stNonConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC1stConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC2ndNonConBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC2ndNonConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC2ndConBegin(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGC2ndConEnd(
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCDrainMark(
    const unsigned __int64  Objects,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCRevisit(
    const unsigned __int64  Pages,
    const unsigned __int64  Objects,
    const unsigned int  IsLarge,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCOverflow(
    const unsigned __int64  Min,
    const unsigned __int64  Max,
    const unsigned __int64  Objects,
    const unsigned int  IsLarge,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCAllocWaitBegin(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwBGCAllocWaitEnd(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwGCFullNotify_V1(
    const unsigned int  GenNumber,
    const unsigned int  IsAlloc,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwPrvSetGCHandle(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

ULONG FireEtwPrvDestroyGCHandle(
    const void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID* ActivityId = nullptr,
    const GUID* RelatedActivityId = nullptr
)
{ return ERROR_SUCCESS; }

#endif // FEATURE_ETW