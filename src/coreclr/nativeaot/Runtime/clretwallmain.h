// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime
#ifndef CLR_ETW_ALL_MAIN_H
#define CLR_ETW_ALL_MAIN_H

#include "clreventpipewriteevents.h"
#include "EtwEvents.h"

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
    const WCHAR* ExceptionType,
    const WCHAR* ExceptionMessage,
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

inline BOOL EventEnabledDecreaseMemoryPressure(void) {return EventPipeEventEnabledDecreaseMemoryPressure();}

inline ULONG FireEtwDecreaseMemoryPressure(
    const unsigned __int64  BytesFreed,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventDecreaseMemoryPressure(BytesFreed,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledFinalizeObject(void) {return EventPipeEventEnabledFinalizeObject();}

inline ULONG FireEtwFinalizeObject(
    const void*  TypeID,
    const void*  ObjectID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventFinalizeObject(TypeID,ObjectID,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCFinalizersBegin_V1(void) {return EventPipeEventEnabledGCFinalizersBegin_V1();}

inline ULONG FireEtwGCFinalizersBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCFinalizersBegin_V1(ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCFinalizersEnd_V1(void) {return EventPipeEventEnabledGCFinalizersEnd_V1();}

inline ULONG FireEtwGCFinalizersEnd_V1(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCFinalizersEnd_V1(Count,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledContentionStart_V2(void) {return EventPipeEventEnabledContentionStart_V2();}

inline ULONG FireEtwContentionStart_V2(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned __int64  LockOwnerThreadID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventContentionStart_V2(ContentionFlags,ClrInstanceID,LockID,AssociatedObjectID,LockOwnerThreadID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledContentionStop_V1(void) {return EventPipeEventEnabledContentionStop_V1();}

inline ULONG FireEtwContentionStop_V1(
    const unsigned char  ContentionFlags,
    const unsigned short  ClrInstanceID,
    const double  DurationNs,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventContentionStop_V1(ContentionFlags,ClrInstanceID,DurationNs,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledContentionLockCreated(void) {return EventPipeEventEnabledContentionLockCreated();}

inline ULONG FireEtwContentionLockCreated(
    const void*  LockID,
    const void*  AssociatedObjectID,
    const unsigned short  ClrInstanceID,
    const GUID *  ActivityId = nullptr,
    const GUID *  RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventContentionLockCreated(LockID,AssociatedObjectID,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolWorkerThreadStart(void) {return EventPipeEventEnabledThreadPoolWorkerThreadStart();}

inline uint32_t FireEtwThreadPoolWorkerThreadStart(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadStart(ActiveWorkerThreadCount,RetiredWorkerThreadCount,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline uint32_t FireEtwThreadPoolWorkerThreadStop(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadStop(ActiveWorkerThreadCount,RetiredWorkerThreadCount,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline uint32_t FireEtwThreadPoolWorkerThreadWait(
    const unsigned int  ActiveWorkerThreadCount,
    const unsigned int  RetiredWorkerThreadCount,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadWait(ActiveWorkerThreadCount,RetiredWorkerThreadCount,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolMinMaxThreads(void) {return EventPipeEventEnabledThreadPoolMinMaxThreads();}

inline uint32_t FireEtwThreadPoolMinMaxThreads(
    const unsigned short  MinWorkerThreads,
    const unsigned short  MaxWorkerThreads,
    const unsigned short  MinIOCompletionThreads,
    const unsigned short  MaxIOCompletionThreads,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolMinMaxThreads(MinWorkerThreads,MaxWorkerThreads,MinIOCompletionThreads,MaxIOCompletionThreads,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolWorkerThreadAdjustmentSample(void) {return EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentSample();}

inline uint32_t FireEtwThreadPoolWorkerThreadAdjustmentSample(
    const double  Throughput,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadAdjustmentSample(Throughput,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolWorkerThreadAdjustmentAdjustment(void) {return EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentAdjustment();}

inline uint32_t FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(
    const double  AverageThroughput,
    const unsigned int  NewWorkerThreadCount,
    const unsigned int  Reason,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput,NewWorkerThreadCount,Reason,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolWorkerThreadAdjustmentStats(void) {return EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentStats();}

inline uint32_t FireEtwThreadPoolWorkerThreadAdjustmentStats(
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
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkerThreadAdjustmentStats(Duration,Throughput,ThreadWave,ThroughputWave,ThroughputErrorEstimate,AverageThroughputErrorEstimate,ThroughputRatio,Confidence,NewControlSetting,NewThreadWaveMagnitude,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolIOEnqueue(void) {return EventPipeEventEnabledThreadPoolIOEnqueue();}

inline uint32_t FireEtwThreadPoolIOEnqueue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const BOOL  MultiDequeues,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolIOEnqueue(NativeOverlapped,Overlapped,MultiDequeues,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolIODequeue(void) {return EventPipeEventEnabledThreadPoolIODequeue();}

inline uint32_t FireEtwThreadPoolIODequeue(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolIODequeue(NativeOverlapped,Overlapped,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolWorkingThreadCount(void) {return EventPipeEventEnabledThreadPoolWorkingThreadCount();}

inline uint32_t FireEtwThreadPoolWorkingThreadCount(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolWorkingThreadCount(Count,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledThreadPoolIOPack(void) {return EventPipeEventEnabledThreadPoolIOPack();}

inline uint32_t FireEtwThreadPoolIOPack(
    const void*  NativeOverlapped,
    const void*  Overlapped,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    uint32_t status = EventPipeWriteEventThreadPoolIOPack(NativeOverlapped,Overlapped,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCAllocationTick_V4(void) {return EventPipeEventEnabledGCAllocationTick_V4();}

inline ULONG FireEtwGCAllocationTick_V4(
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
{
    ULONG status = EventPipeWriteEventGCAllocationTick_V4(AllocationAmount,AllocationKind,ClrInstanceID,AllocationAmount64,TypeID,TypeName,HeapIndex,Address,ObjectSize,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCHeapStats_V2(void) {return EventPipeEventEnabledGCHeapStats_V2();}

inline ULONG FireEtwGCHeapStats_V2(
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
{
    ULONG status = EventPipeWriteEventGCHeapStats_V2(GenerationSize0,TotalPromotedSize0,GenerationSize1,TotalPromotedSize1,GenerationSize2,TotalPromotedSize2,GenerationSize3,TotalPromotedSize3,FinalizationPromotedSize,FinalizationPromotedCount,PinnedObjectCount,SinkBlockCount,GCHandleCount,ClrInstanceID,GenerationSize4,TotalPromotedSize4,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCSampledObjectAllocationHigh(void) {return EventPipeEventEnabledGCSampledObjectAllocationHigh();}

inline ULONG FireEtwGCSampledObjectAllocationHigh(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSampledObjectAllocationHigh(Address,TypeID,ObjectCountForTypeSample,TotalSizeForTypeSample,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCSampledObjectAllocationLow(void) {return EventPipeEventEnabledGCSampledObjectAllocationLow();}

inline ULONG FireEtwGCSampledObjectAllocationLow(
    const void*  Address,
    const void*  TypeID,
    const unsigned int  ObjectCountForTypeSample,
    const unsigned __int64  TotalSizeForTypeSample,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSampledObjectAllocationLow(Address,TypeID,ObjectCountForTypeSample,TotalSizeForTypeSample,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledPinObjectAtGCTime(void) {return EventPipeEventEnabledPinObjectAtGCTime();}

inline ULONG FireEtwPinObjectAtGCTime(
    const void*  HandleID,
    const void*  ObjectID,
    const unsigned __int64  ObjectSize,
    const WCHAR*  TypeName,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventPinObjectAtGCTime(HandleID,ObjectID,ObjectSize,TypeName,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCBulkRootStaticVar(void) {return EventPipeEventEnabledGCBulkRootStaticVar();}

inline ULONG FireEtwGCBulkRootStaticVar(
    const unsigned int  Count,
    const unsigned __int64  AppDomainID,
    const unsigned short  ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCBulkRootStaticVar(Count,AppDomainID,ClrInstanceID,Values_ElementSize, Values,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledIncreaseMemoryPressure(void) {return EventPipeEventEnabledIncreaseMemoryPressure();}

inline ULONG FireEtwIncreaseMemoryPressure(
    const unsigned __int64  BytesAllocated,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventIncreaseMemoryPressure(BytesAllocated,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCGlobalHeapHistory_V4(void) {return EventPipeEventEnabledGCGlobalHeapHistory_V4();}

inline ULONG FireEtwGCGlobalHeapHistory_V4(
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
{
    ULONG status = EventPipeWriteEventGCGlobalHeapHistory_V4(FinalYoungestDesired,NumHeaps,CondemnedGeneration,Gen0ReductionCount,Reason,GlobalMechanisms,ClrInstanceID,PauseMode,MemoryPressure,CondemnReasons0,CondemnReasons1,Count,Values_ElementSize, Values,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGenAwareBegin(void) {return EventPipeEventEnabledGenAwareBegin();}

inline ULONG FireEtwGenAwareBegin(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGenAwareBegin(Count,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGenAwareEnd(void) {return EventPipeEventEnabledGenAwareEnd();}

inline ULONG FireEtwGenAwareEnd(
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGenAwareEnd(Count,ClrInstanceID,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCLOHCompact(void) {return EventPipeEventEnabledGCLOHCompact();}

inline ULONG FireEtwGCLOHCompact(
    const unsigned short  ClrInstanceID,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCLOHCompact(ClrInstanceID,Count,Values_ElementSize, Values,ActivityId,RelatedActivityId);
    return status;
}

inline BOOL EventEnabledGCFitBucketInfo(void) {return EventPipeEventEnabledGCFitBucketInfo();}

inline ULONG FireEtwGCFitBucketInfo(
    const unsigned short  ClrInstanceID,
    const unsigned short  BucketKind,
    const unsigned __int64  TotalSize,
    const unsigned short  Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCFitBucketInfo(ClrInstanceID,BucketKind,TotalSize,Count,Values_ElementSize, Values,ActivityId,RelatedActivityId);
    return status;
}
#endif // __CLR_ETW_ALL_MAIN_H__