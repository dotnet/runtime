
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

#include "clreventpipewriteevents.h"
#include "etwevents.h"

inline BOOL EventEnabledDestroyGCHandle(void) {return EventPipeEventEnabledDestroyGCHandle();}

inline ULONG FireEtwDestroyGCHandle(
    void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventDestroyGCHandle(HandleID,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatDestroyGCHandle(HandleID,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledExceptionThrown_V1(void) {return EventPipeEventEnabledExceptionThrown_V1();}

inline ULONG FireEtwExceptionThrown_V1(
    wchar_t* ExceptionType,
    wchar_t* ExceptionMessage,
    void*  ExceptionEIP,
    const unsigned int  ExceptionHRESULT,
    const unsigned short  ExceptionFlags,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventExceptionThrown_V1(ExceptionType,ExceptionMessage,ExceptionEIP,ExceptionHRESULT,ExceptionFlags,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatExceptionThrown_V1(ExceptionType,ExceptionMessage,ExceptionEIP,ExceptionHRESULT,ExceptionFlags,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCAllocationTick_V1(void) {return EventPipeEventEnabledGCAllocationTick_V1();}

inline ULONG FireEtwGCAllocationTick_V1(
    const unsigned int  AllocationAmount,
    const unsigned int  AllocationKind,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCAllocationTick_V1(AllocationAmount,AllocationKind,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCAllocationTick_V1(AllocationAmount,AllocationKind,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCAllocationTick_V2(void) {return EventPipeEventEnabledGCAllocationTick_V2();}

inline ULONG FireEtwGCAllocationTick_V2(
    const unsigned int  AllocationAmount,
    const unsigned int  AllocationKind,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  AllocationAmount64,
    void*  TypeID,
    wchar_t*  TypeName,
    const unsigned int  HeapIndex,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCAllocationTick_V2(AllocationAmount,AllocationKind,ClrInstanceID,AllocationAmount64,TypeID,TypeName,HeapIndex,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCAllocationTick_V2(AllocationAmount,AllocationKind,ClrInstanceID,AllocationAmount64,TypeID,TypeName,HeapIndex);
#endif
    return status;
}

inline BOOL EventEnabledGCAllocationTick_V3(void) {return EventPipeEventEnabledGCAllocationTick_V3();}

inline ULONG FireEtwGCAllocationTick_V3(
    const unsigned int  AllocationAmount,
    const unsigned int  AllocationKind,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  AllocationAmount64,
    void*  TypeID,
    wchar_t*  TypeName,
    const unsigned int  HeapIndex,
    void*  Address,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCAllocationTick_V3(AllocationAmount,AllocationKind,ClrInstanceID,AllocationAmount64,TypeID,TypeName,HeapIndex,Address,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCAllocationTick_V3(AllocationAmount,AllocationKind,ClrInstanceID,AllocationAmount64,TypeID,TypeName,HeapIndex,Address);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkEdge(void) {return EventPipeEventEnabledGCBulkEdge();}

inline ULONG FireEtwGCBulkEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkMovedObjectRanges(void) {return EventPipeEventEnabledGCBulkMovedObjectRanges();}

inline ULONG FireEtwGCBulkMovedObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkMovedObjectRanges(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkMovedObjectRanges(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkNode(void) {return EventPipeEventEnabledGCBulkNode();}

inline ULONG FireEtwGCBulkNode(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkNode(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkNode(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkRCW(void) {return EventPipeEventEnabledGCBulkRCW();}

inline ULONG FireEtwGCBulkRCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkRCW(Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkRCW(Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkRootCCW(void) {return EventPipeEventEnabledGCBulkRootCCW();}

inline ULONG FireEtwGCBulkRootCCW(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkRootCCW(Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkRootCCW(Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkRootConditionalWeakTableElementEdge(void) {return EventPipeEventEnabledGCBulkRootConditionalWeakTableElementEdge();}

inline ULONG FireEtwGCBulkRootConditionalWeakTableElementEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkRootConditionalWeakTableElementEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkRootConditionalWeakTableElementEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkRootEdge(void) {return EventPipeEventEnabledGCBulkRootEdge();}

inline ULONG FireEtwGCBulkRootEdge(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkRootEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkRootEdge(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCBulkSurvivingObjectRanges(void) {return EventPipeEventEnabledGCBulkSurvivingObjectRanges();}

inline ULONG FireEtwGCBulkSurvivingObjectRanges(
    const unsigned int  Index,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkSurvivingObjectRanges(Index,Count,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCBulkSurvivingObjectRanges(Index,Count,ClrInstanceID,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCCreateConcurrentThread_V1(void) {return EventPipeEventEnabledGCCreateConcurrentThread_V1();}

inline ULONG FireEtwGCCreateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCCreateConcurrentThread_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCCreateConcurrentThread_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCCreateSegment_V1(void) {return EventPipeEventEnabledGCCreateSegment_V1();}

inline ULONG FireEtwGCCreateSegment_V1(
    const unsigned __int64  Address,
    const unsigned __int64  Size,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCCreateSegment_V1(Address,Size,Type,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCCreateSegment_V1(Address,Size,Type,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCEnd_V1(void) {return EventPipeEventEnabledGCEnd_V1();}

inline ULONG FireEtwGCEnd_V1(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCEnd_V1(Count,Depth,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCEnd_V1(Count,Depth,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCFreeSegment_V1(void) {return EventPipeEventEnabledGCFreeSegment_V1();}

inline ULONG FireEtwGCFreeSegment_V1(
    const unsigned __int64  Address,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCFreeSegment_V1(Address,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCFreeSegment_V1(Address,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCGenerationRange(void) {return EventPipeEventEnabledGCGenerationRange();}

inline ULONG FireEtwGCGenerationRange(
    const unsigned char  Generation,
    void*  RangeStart,
    const unsigned __int64  RangeUsedLength,
    const unsigned __int64  RangeReservedLength,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCGenerationRange(Generation,RangeStart,RangeUsedLength,RangeReservedLength,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCGenerationRange(Generation,RangeStart,RangeUsedLength,RangeReservedLength,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCGlobalHeapHistory_V2(void) {return EventPipeEventEnabledGCGlobalHeapHistory_V2();}

inline ULONG FireEtwGCGlobalHeapHistory_V2(
    const unsigned __int64  FinalYoungestDesired,
    const signed int  NumHeaps,
    const unsigned int  CondemnedGeneration,
    const unsigned int  Gen0ReductionCount,
    const unsigned int  Reason,
    const unsigned int  GlobalMechanisms,
    const unsigned short  ClrInstanceID,
    const unsigned int  PauseMode,
    const unsigned int  MemoryPressure,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCGlobalHeapHistory_V2(FinalYoungestDesired,NumHeaps,CondemnedGeneration,Gen0ReductionCount,Reason,GlobalMechanisms,ClrInstanceID,PauseMode,MemoryPressure,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCGlobalHeapHistory_V2(FinalYoungestDesired,NumHeaps,CondemnedGeneration,Gen0ReductionCount,Reason,GlobalMechanisms,ClrInstanceID,PauseMode,MemoryPressure);
#endif
    return status;
}

inline BOOL EventEnabledGCHeapStats_V1(void) {return EventPipeEventEnabledGCHeapStats_V1();}

inline ULONG FireEtwGCHeapStats_V1(
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
{
    ULONG status = EventPipeWriteEventGCHeapStats_V1(GenerationSize0,TotalPromotedSize0,GenerationSize1,TotalPromotedSize1,GenerationSize2,TotalPromotedSize2,GenerationSize3,TotalPromotedSize3,FinalizationPromotedSize,FinalizationPromotedCount,PinnedObjectCount,SinkBlockCount,GCHandleCount,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCHeapStats_V1(GenerationSize0,TotalPromotedSize0,GenerationSize1,TotalPromotedSize1,GenerationSize2,TotalPromotedSize2,GenerationSize3,TotalPromotedSize3,FinalizationPromotedSize,FinalizationPromotedCount,PinnedObjectCount,SinkBlockCount,GCHandleCount,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCJoin_V2(void) {return EventPipeEventEnabledGCJoin_V2();}

inline ULONG FireEtwGCJoin_V2(
    const unsigned int  Heap,
    const unsigned int  JoinTime,
    const unsigned int  JoinType,
    const unsigned short  ClrInstanceID,
    const unsigned int  JoinID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCJoin_V2(Heap,JoinTime,JoinType,ClrInstanceID,JoinID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCJoin_V2(Heap,JoinTime,JoinType,ClrInstanceID,JoinID);
#endif
    return status;
}

inline BOOL EventEnabledGCMarkFinalizeQueueRoots(void) {return EventPipeEventEnabledGCMarkFinalizeQueueRoots();}

inline ULONG FireEtwGCMarkFinalizeQueueRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCMarkFinalizeQueueRoots(HeapNum,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCMarkFinalizeQueueRoots(HeapNum,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCMarkHandles(void) {return EventPipeEventEnabledGCMarkHandles();}

inline ULONG FireEtwGCMarkHandles(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCMarkHandles(HeapNum,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCMarkHandles(HeapNum,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCMarkOlderGenerationRoots(void) {return EventPipeEventEnabledGCMarkOlderGenerationRoots();}

inline ULONG FireEtwGCMarkOlderGenerationRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCMarkOlderGenerationRoots(HeapNum,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCMarkOlderGenerationRoots(HeapNum,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCMarkStackRoots(void) {return EventPipeEventEnabledGCMarkStackRoots();}

inline ULONG FireEtwGCMarkStackRoots(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCMarkStackRoots(HeapNum,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCMarkStackRoots(HeapNum,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCMarkWithType(void) {return EventPipeEventEnabledGCMarkWithType();}

inline ULONG FireEtwGCMarkWithType(
    const unsigned int  HeapNum,
    const unsigned short  ClrInstanceID,
    const unsigned int  Type,
    const unsigned __int64  Bytes,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCMarkWithType(HeapNum,ClrInstanceID,Type,Bytes,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCMarkWithType(HeapNum,ClrInstanceID,Type,Bytes);
#endif
    return status;
}

inline BOOL EventEnabledGCPerHeapHistory_V3(void) {return EventPipeEventEnabledGCPerHeapHistory_V3();}

inline ULONG FireEtwGCPerHeapHistory_V3(
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
{
    ULONG status = EventPipeWriteEventGCPerHeapHistory_V3(ClrInstanceID,FreeListAllocated,FreeListRejected,EndOfSegAllocated,CondemnedAllocated,PinnedAllocated,PinnedAllocatedAdvance,RunningFreeListEfficiency,CondemnReasons0,CondemnReasons1,CompactMechanisms,ExpandMechanisms,HeapIndex,ExtraGen0Commit,Count,Values_ElementSize, Values,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCPerHeapHistory_V3(ClrInstanceID,FreeListAllocated,FreeListRejected,EndOfSegAllocated,CondemnedAllocated,PinnedAllocated,PinnedAllocatedAdvance,RunningFreeListEfficiency,CondemnReasons0,CondemnReasons1,CompactMechanisms,ExpandMechanisms,HeapIndex,ExtraGen0Commit,Count,Values_ElementSize, Values);
#endif
    return status;
}

inline BOOL EventEnabledGCTerminateConcurrentThread_V1(void) {return EventPipeEventEnabledGCTerminateConcurrentThread_V1();}

inline ULONG FireEtwGCTerminateConcurrentThread_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCTerminateConcurrentThread_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCTerminateConcurrentThread_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCTriggered(void) {return EventPipeEventEnabledGCTriggered();}

inline ULONG FireEtwGCTriggered(
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCTriggered(Reason,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCTriggered(Reason,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledModuleLoad_V2(void) {return EventPipeEventEnabledModuleLoad_V2();}

inline ULONG FireEtwModuleLoad_V2(
    const unsigned __int64  ModuleID,
    const unsigned __int64  AssemblyID,
    const unsigned int  ModuleFlags,
    const unsigned int  Reserved1,
    wchar_t*  ModuleILPath,
    wchar_t*  ModuleNativePath,
    const unsigned short  ClrInstanceID,
    const GUID* ManagedPdbSignature,
    const unsigned int  ManagedPdbAge,
    wchar_t*  ManagedPdbBuildPath,
    const GUID* NativePdbSignature,
    const unsigned int  NativePdbAge,
    wchar_t*  NativePdbBuildPath,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventModuleLoad_V2(ModuleID,AssemblyID,ModuleFlags,Reserved1,ModuleILPath,ModuleNativePath,ClrInstanceID,ManagedPdbSignature,ManagedPdbAge,ManagedPdbBuildPath,NativePdbSignature,NativePdbAge,NativePdbBuildPath,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatModuleLoad_V2(ModuleID,AssemblyID,ModuleFlags,Reserved1,ModuleILPath,ModuleNativePath,ClrInstanceID,ManagedPdbSignature,ManagedPdbAge,ManagedPdbBuildPath,NativePdbSignature,NativePdbAge,NativePdbBuildPath);
#endif
    return status;
}

inline BOOL EventEnabledSetGCHandle(void) {return EventPipeEventEnabledSetGCHandle();}

inline ULONG FireEtwSetGCHandle(
    void*  HandleID,
    void*  ObjectID,
    const unsigned int  Kind,
    const unsigned int  Generation,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventSetGCHandle(HandleID,ObjectID,Kind,Generation,AppDomainID,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatSetGCHandle(HandleID,ObjectID,Kind,Generation,AppDomainID,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCStart_V1(void) {return EventPipeEventEnabledGCStart_V1();}

inline ULONG FireEtwGCStart_V1(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCStart_V1(Count,Depth,Reason,Type,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCStart_V1(Count,Depth,Reason,Type,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCStart_V2(void) {return EventPipeEventEnabledGCStart_V2();}

inline ULONG FireEtwGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCStart_V2(Count,Depth,Reason,Type,ClrInstanceID,ClientSequenceNumber,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCStart_V2(Count,Depth,Reason,Type,ClrInstanceID,ClientSequenceNumber);
#endif
    return status;
}


inline BOOL EventEnabledGCRestartEEEnd_V1(void) {return EventPipeEventEnabledGCRestartEEEnd_V1();}

inline ULONG FireEtwGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCRestartEEEnd_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCRestartEEEnd_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCRestartEEBegin_V1(void) {return EventPipeEventEnabledGCRestartEEBegin_V1();}

inline ULONG FireEtwGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCRestartEEBegin_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCRestartEEBegin_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCSuspendEEEnd_V1(void) {return EventPipeEventEnabledGCSuspendEEEnd_V1();}

inline ULONG FireEtwGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSuspendEEEnd_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXPlatGCSuspendEEEnd_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCSuspendEEBegin_V1(void) {return EventPipeEventEnabledGCSuspendEEBegin_V1();}

inline ULONG FireEtwGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSuspendEEBegin_V1(Reason,Count,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCSuspendEEBegin_V1(Reason,Count,ClrInstanceID);
#endif
    return status;
}
