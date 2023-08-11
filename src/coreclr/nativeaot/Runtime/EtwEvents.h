// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Use genEtwProvider.py to generate headers to replace this file.
//        Reconcile tracking/callbacks/contexts used for ETW vs EventPipe.
// FireEtXplat* functions handle ETW only. The naming matches the generated
// output of genEtwProvider.py.
#ifndef __RH_ETW_DEFS_INCLUDED
#define __RH_ETW_DEFS_INCLUDED

#if defined(FEATURE_ETW) && !defined(DACCESS_COMPILE)

#include <wchar.h>

#ifndef RH_ETW_INLINE
#define RH_ETW_INLINE __declspec(noinline) __inline
#endif

typedef struct _MCGEN_TRACE_CONTEXT
{
    TRACEHANDLE            RegistrationHandle;
    TRACEHANDLE            Logger;
    ULONGLONG              MatchAnyKeyword;
    ULONGLONG              MatchAllKeyword;
    ULONG                  Flags;
    ULONG                  IsEnabled;
    unsigned char          Level;
    unsigned char          Reserve;
    unsigned short         EnableBitsCount;
    ULONG *                EnableBitMask;
    const ULONGLONG*       EnableKeyWords;
    const unsigned char*   EnableLevel;
} MCGEN_TRACE_CONTEXT, *PMCGEN_TRACE_CONTEXT;

__declspec(noinline) __inline void __stdcall
EtwCallback(GUID * /*SourceId*/, uint32_t IsEnabled, uint8_t Level, uint64_t MatchAnyKeyword, uint64_t MatchAllKeyword, EVENT_FILTER_DESCRIPTOR * FilterData, void * CallbackContext);

__declspec(noinline) __inline bool __stdcall
RhEventTracingEnabled(MCGEN_TRACE_CONTEXT * EnableInfo,
                       const EVENT_DESCRIPTOR * EventDescriptor)
{
    if (!EnableInfo)
        return false;
    if ((EventDescriptor->Level <= EnableInfo->Level) || (EnableInfo->Level == 0))
    {
        if ((EventDescriptor->Keyword == (ULONGLONG)0) ||
            ((EventDescriptor->Keyword & EnableInfo->MatchAnyKeyword) &&
             ((EventDescriptor->Keyword & EnableInfo->MatchAllKeyword) == EnableInfo->MatchAllKeyword)))
            return true;
    }
    return false;
}

#define ETW_EVENT_ENABLED(Context, EventDescriptor) (Context.IsEnabled && RhEventTracingEnabled(&Context, &EventDescriptor))

extern "C" __declspec(selectany) const GUID MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER = {0x763fd754, 0x7086, 0x4dfe, {0x95, 0xeb, 0xc0, 0x1a, 0x46, 0xfa, 0xf4, 0xca}};

extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC1stConEnd = {0xd, 0x0, 0x10, 0x4, 0x1b, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC1stNonConEnd = {0xc, 0x0, 0x10, 0x4, 0x1a, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC2ndConBegin = {0x10, 0x0, 0x10, 0x4, 0x1e, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC2ndConEnd = {0x11, 0x0, 0x10, 0x4, 0x1f, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC2ndNonConBegin = {0xe, 0x0, 0x10, 0x4, 0x1c, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGC2ndNonConEnd = {0xf, 0x0, 0x10, 0x4, 0x1d, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCAllocWaitBegin = {0x17, 0x0, 0x10, 0x4, 0x25, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCAllocWaitEnd = {0x18, 0x0, 0x10, 0x4, 0x26, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCBegin = {0xb, 0x0, 0x10, 0x4, 0x19, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCDrainMark = {0x14, 0x0, 0x10, 0x4, 0x22, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCOverflow = {0x16, 0x0, 0x10, 0x4, 0x24, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCPlanEnd = {0x12, 0x0, 0x10, 0x4, 0x20, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCRevisit = {0x15, 0x0, 0x10, 0x4, 0x23, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BGCSweepEnd = {0x13, 0x0, 0x10, 0x4, 0x21, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCFullNotify_V1 = {0x19, 0x1, 0x10, 0x4, 0x13, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCGlobalHeapHistory_V1 = {0x5, 0x1, 0x10, 0x4, 0x12, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCJoin_V1 = {0x6, 0x1, 0x10, 0x5, 0x14, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCOptimized_V1 = {0x3, 0x1, 0x10, 0x5, 0x10, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCPerHeapHistory = {0x4, 0x2, 0x10, 0x4, 0x11, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCSettings = {0x2, 0x0, 0x10, 0x4, 0xe, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PinPlugAtGCTime = {0xc7, 0x0, 0x10, 0x5, 0x2c, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvDestroyGCHandle = {0xc3, 0x0, 0x10, 0x5, 0x2b, 0x1, 0x8000000000004000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvGCMarkCards_V1 = {0xa, 0x1, 0x10, 0x4, 0x18, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvGCMarkFinalizeQueueRoots_V1 = {0x8, 0x1, 0x10, 0x4, 0x16, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvGCMarkHandles_V1 = {0x9, 0x1, 0x10, 0x4, 0x17, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvGCMarkStackRoots_V1 = {0x7, 0x1, 0x10, 0x4, 0x15, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR PrvSetGCHandle = {0xc2, 0x0, 0x10, 0x5, 0x2a, 0x1, 0x8000000000004000};

extern "C" __declspec(selectany) REGHANDLE Microsoft_Windows_DotNETRuntimePrivateHandle;
extern "C" __declspec(selectany) MCGEN_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context;

#define RH_ETW_REGISTER_Microsoft_Windows_DotNETRuntimePrivate() do { PalEventRegister(&MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER, EtwCallback, &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, &Microsoft_Windows_DotNETRuntimePrivateHandle); } while (false)
#define RH_ETW_UNREGISTER_Microsoft_Windows_DotNETRuntimePrivate() do { PalEventUnregister(Microsoft_Windows_DotNETRuntimePrivateHandle); } while (false)

#define FireEtXplatBGC1stConEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC1stConEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC1stConEnd, ClrInstanceID) : 0

#define FireEtXplatBGC1stNonConEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC1stNonConEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC1stNonConEnd, ClrInstanceID) : 0

#define FireEtXplatBGC2ndConBegin(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndConBegin)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndConBegin, ClrInstanceID) : 0

#define FireEtXplatBGC2ndConEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndConEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndConEnd, ClrInstanceID) : 0

#define FireEtXplatBGC2ndNonConBegin(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndNonConBegin)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndNonConBegin, ClrInstanceID) : 0

#define FireEtXplatBGC2ndNonConEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndNonConEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGC2ndNonConEnd, ClrInstanceID) : 0

#define FireEtXplatBGCAllocWaitBegin(Reason, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCAllocWaitBegin)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCAllocWait(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCAllocWaitBegin, Reason, ClrInstanceID) : 0

#define FireEtXplatBGCAllocWaitEnd(Reason, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCAllocWaitEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCAllocWait(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCAllocWaitEnd, Reason, ClrInstanceID) : 0

#define FireEtXplatBGCBegin(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCBegin)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCBegin, ClrInstanceID) : 0

#define FireEtXplatBGCDrainMark(Objects, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCDrainMark)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCDrainMark(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCDrainMark, Objects, ClrInstanceID) : 0

#define FireEtXplatBGCOverflow(Min, Max, Objects, IsLarge, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCOverflow)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCOverflow(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCOverflow, Min, Max, Objects, IsLarge, ClrInstanceID) : 0

#define FireEtXplatBGCPlanEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCPlanEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCPlanEnd, ClrInstanceID) : 0

#define FireEtXplatBGCRevisit(Pages, Objects, IsLarge, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCRevisit)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCRevisit(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCRevisit, Pages, Objects, IsLarge, ClrInstanceID) : 0

#define FireEtXplatBGCSweepEnd(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCSweepEnd)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimePrivateHandle, &BGCSweepEnd, ClrInstanceID) : 0

#define FireEtXplatGCFullNotify_V1(GenNumber, IsAlloc, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCFullNotify_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCFullNotify_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCFullNotify_V1, GenNumber, IsAlloc, ClrInstanceID) : 0

#define FireEtXplatGCGlobalHeapHistory_V1(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCGlobalHeapHistory_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCGlobalHeap_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCGlobalHeapHistory_V1, FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID) : 0

#define FireEtXplatGCJoin_V1(Heap, JoinTime, JoinType, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCJoin_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCJoin_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCJoin_V1, Heap, JoinTime, JoinType, ClrInstanceID) : 0

#define FireEtXplatGCOptimized_V1(DesiredAllocation, NewAllocation, GenerationNumber, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCOptimized_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCOptimized_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCOptimized_V1, DesiredAllocation, NewAllocation, GenerationNumber, ClrInstanceID) : 0

#define FireEtXplatGCPerHeapHistory() (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCPerHeapHistory)) ? TemplateEventDescriptor(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCPerHeapHistory) : 0

#define FireEtXplatGCSettings(SegmentSize, LargeObjectSegmentSize, ServerGC) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCSettings)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCSettings(Microsoft_Windows_DotNETRuntimePrivateHandle, &GCSettings, SegmentSize, LargeObjectSegmentSize, ServerGC) : 0

#define FireEtXplatPinPlugAtGCTime(PlugStart, PlugEnd, GapBeforeSize, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PinPlugAtGCTime)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PinPlugAtGCTime(Microsoft_Windows_DotNETRuntimePrivateHandle, &PinPlugAtGCTime, PlugStart, PlugEnd, GapBeforeSize, ClrInstanceID) : 0

#define FireEtXplatPrvDestroyGCHandle(HandleID, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvDestroyGCHandle)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvDestroyGCHandle(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvDestroyGCHandle, HandleID, ClrInstanceID) : 0

#define FireEtXplatPrvGCMarkCards_V1(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkCards_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvGCMark_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkCards_V1, HeapNum, ClrInstanceID) : 0

#define FireEtXplatPrvGCMarkFinalizeQueueRoots_V1(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkFinalizeQueueRoots_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvGCMark_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkFinalizeQueueRoots_V1, HeapNum, ClrInstanceID) : 0

#define FireEtXplatPrvGCMarkHandles_V1(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkHandles_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvGCMark_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkHandles_V1, HeapNum, ClrInstanceID) : 0

#define FireEtXplatPrvGCMarkStackRoots_V1(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkStackRoots_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvGCMark_V1(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvGCMarkStackRoots_V1, HeapNum, ClrInstanceID) : 0

#define FireEtXplatPrvSetGCHandle(HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvSetGCHandle)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvSetGCHandle(Microsoft_Windows_DotNETRuntimePrivateHandle, &PrvSetGCHandle, HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID) : 0

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCAllocWait(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Reason, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCDrainMark(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t Objects, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &Objects, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCOverflow(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t Min, uint64_t Max, uint64_t Objects, uint32_t IsLarge, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[5];
    EventDataDescCreate(&EventData[0], &Min, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &Max, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &Objects, sizeof(uint64_t));
    EventDataDescCreate(&EventData[3], &IsLarge, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 5, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_BGCRevisit(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t Pages, uint64_t Objects, uint32_t IsLarge, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &Pages, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &Objects, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &IsLarge, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCFullNotify_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t GenNumber, uint32_t IsAlloc, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[3];
    EventDataDescCreate(&EventData[0], &GenNumber, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &IsAlloc, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCGlobalHeap_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t FinalYoungestDesired, int32_t NumHeaps, uint32_t CondemnedGeneration, uint32_t Gen0ReductionCount, uint32_t Reason, uint32_t GlobalMechanisms, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[7];
    EventDataDescCreate(&EventData[0], &FinalYoungestDesired, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &NumHeaps, sizeof(int32_t));
    EventDataDescCreate(&EventData[2], &CondemnedGeneration, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Gen0ReductionCount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[5], &GlobalMechanisms, sizeof(uint32_t));
    EventDataDescCreate(&EventData[6], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 7, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCJoin_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Heap, uint32_t JoinTime, uint32_t JoinType, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &Heap, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &JoinTime, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &JoinType, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCNoUserData(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[1];
    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 1, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCOptimized_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t DesiredAllocation, uint64_t NewAllocation, uint32_t GenerationNumber, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &DesiredAllocation, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &NewAllocation, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &GenerationNumber, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_GCSettings(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t SegmentSize, uint64_t LargeObjectSegmentSize, UInt32_BOOL ServerGC)
{
    EVENT_DATA_DESCRIPTOR EventData[3];
    EventDataDescCreate(&EventData[0], &SegmentSize, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &LargeObjectSegmentSize, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &ServerGC, sizeof(UInt32_BOOL));
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PinPlugAtGCTime(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, const void* PlugStart, const void* PlugEnd, const void* GapBeforeSize, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &PlugStart, sizeof(const void*));
    EventDataDescCreate(&EventData[1], &PlugEnd, sizeof(const void*));
    EventDataDescCreate(&EventData[2], &GapBeforeSize, sizeof(const void*));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvDestroyGCHandle(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, const void* HandleID, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &HandleID, sizeof(const void*));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvGCMark_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t HeapNum, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &HeapNum, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_PrvSetGCHandle(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, const void* HandleID, const void* ObjectID, uint32_t Kind, uint32_t Generation, uint64_t AppDomainID, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], &HandleID, sizeof(const void*));
    EventDataDescCreate(&EventData[1], &ObjectID, sizeof(const void*));
    EventDataDescCreate(&EventData[2], &Kind, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Generation, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &AppDomainID, sizeof(uint64_t));
    EventDataDescCreate(&EventData[5], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 6, EventData);
}

extern "C" __declspec(selectany) const GUID MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER = {0xe13c0d23, 0xccbc, 0x4e12, {0x93, 0x1b, 0xd9, 0xcc, 0x2e, 0xee, 0x27, 0xe4}};

extern "C" __declspec(selectany) const EVENT_DESCRIPTOR BulkType = {0xf, 0x0, 0x10, 0x4, 0xa, 0x15, 0x8000000000080000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR DestroyGCHandle = {0x1f, 0x0, 0x10, 0x4, 0x22, 0x1, 0x8000000000000002};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR ExceptionThrown_V1 = {0x50, 0x1, 0x10, 0x2, 0x1, 0x7, 0x8000000200008000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCAllocationTick_V1 = {0xa, 0x1, 0x10, 0x5, 0xb, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCAllocationTick_V2 = {0xa, 0x2, 0x10, 0x5, 0xb, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCAllocationTick_V3 = {0xa, 0x3, 0x10, 0x5, 0xb, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkEdge = {0x13, 0x0, 0x10, 0x4, 0x17, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkMovedObjectRanges = {0x16, 0x0, 0x10, 0x4, 0x1a, 0x1, 0x8000000000400000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkNode = {0x12, 0x0, 0x10, 0x4, 0x16, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkRCW = {0x25, 0x0, 0x10, 0x4, 0x27, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkRootCCW = {0x24, 0x0, 0x10, 0x4, 0x26, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkRootConditionalWeakTableElementEdge = {0x11, 0x0, 0x10, 0x4, 0x15, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkRootEdge = {0x10, 0x0, 0x10, 0x4, 0x14, 0x1, 0x8000000000100000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCBulkSurvivingObjectRanges = {0x15, 0x0, 0x10, 0x4, 0x19, 0x1, 0x8000000000400000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCCreateConcurrentThread_V1 = {0xb, 0x1, 0x10, 0x4, 0xc, 0x1, 0x8000000000010001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCCreateSegment_V1 = {0x5, 0x1, 0x10, 0x4, 0x86, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCEnd_V1 = {0x2, 0x1, 0x10, 0x4, 0x2, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCFreeSegment_V1 = {0x6, 0x1, 0x10, 0x4, 0x87, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCGenerationRange = {0x17, 0x0, 0x10, 0x4, 0x1b, 0x1, 0x8000000000400000};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCGlobalHeapHistory_V2 = {0xcd, 0x2, 0x10, 0x4, 0xcd, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCHeapStats_V1 = {0x4, 0x1, 0x10, 0x4, 0x85, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCJoin_V2 = {0xcb, 0x2, 0x10, 0x5, 0xcb, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCMarkFinalizeQueueRoots = {0x1a, 0x0, 0x10, 0x4, 0x1d, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCMarkHandles = {0x1b, 0x0, 0x10, 0x4, 0x1e, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCMarkOlderGenerationRoots = {0x1c, 0x0, 0x10, 0x4, 0x1f, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCMarkStackRoots = {0x19, 0x0, 0x10, 0x4, 0x1c, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCMarkWithType = {0xca, 0x0, 0x10, 0x4, 0xca, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCPerHeapHistory_V3 = {0xcc, 0x3, 0x10, 0x4, 0xcc, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCRestartEEBegin_V1 = {0x7, 0x1, 0x10, 0x4, 0x88, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCRestartEEEnd_V1 = {0x3, 0x1, 0x10, 0x4, 0x84, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCStart_V1 = {0x1, 0x1, 0x10, 0x4, 0x1, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCStart_V2 = {0x1, 0x2, 0x10, 0x4, 0x1, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCSuspendEEBegin_V1 = {0x9, 0x1, 0x10, 0x4, 0xa, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCSuspendEEEnd_V1 = {0x8, 0x1, 0x10, 0x4, 0x89, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCTerminateConcurrentThread_V1 = {0xc, 0x1, 0x10, 0x4, 0xd, 0x1, 0x8000000000010001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR GCTriggered = {0x23, 0x0, 0x10, 0x4, 0x23, 0x1, 0x8000000000000001};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR ModuleLoad_V2 = {0x98, 0x2, 0x10, 0x4, 0x21, 0xa, 0x8000000020000008};
extern "C" __declspec(selectany) const EVENT_DESCRIPTOR SetGCHandle = {0x1e, 0x0, 0x10, 0x4, 0x21, 0x1, 0x8000000000000002};

extern "C" __declspec(selectany) REGHANDLE Microsoft_Windows_DotNETRuntimeHandle;
extern "C" __declspec(selectany) MCGEN_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context;

#define RH_ETW_REGISTER_Microsoft_Windows_DotNETRuntime() do { PalEventRegister(&MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER, EtwCallback, &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, &Microsoft_Windows_DotNETRuntimeHandle); } while (false)
#define RH_ETW_UNREGISTER_Microsoft_Windows_DotNETRuntime() do { PalEventUnregister(Microsoft_Windows_DotNETRuntimeHandle); } while (false)

#define FireEtXplatDestroyGCHandle(HandleID, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &DestroyGCHandle)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_DestroyGCHandle(Microsoft_Windows_DotNETRuntimeHandle, &DestroyGCHandle, HandleID, ClrInstanceID) : 0

#define FireEtXplatExceptionThrown_V1(ExceptionType, ExceptionMessage, ExceptionEIP, ExceptionHRESULT, ExceptionFlags, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &ExceptionThrown_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Exception(Microsoft_Windows_DotNETRuntimeHandle, &ExceptionThrown_V1, ExceptionType, ExceptionMessage, ExceptionEIP, ExceptionHRESULT, ExceptionFlags, ClrInstanceID) : 0

#define FireEtXplatGCAllocationTick_V1(AllocationAmount, AllocationKind, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V1, AllocationAmount, AllocationKind, ClrInstanceID) : 0

#define FireEtXplatGCAllocationTick_V2(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V2)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V2(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V2, AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex) : 0

#define FireEtXplatGCAllocationTick_V3(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex, Address) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V3)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V3(Microsoft_Windows_DotNETRuntimeHandle, &GCAllocationTick_V3, AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex, Address) : 0

#define FireEtXplatGCBulkEdge(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkEdge)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkEdge(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkEdge, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkMovedObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkMovedObjectRanges)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkMovedObjectRanges(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkMovedObjectRanges, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkNode(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkNode)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkNode(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkNode, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkRCW(Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRCW)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRCW(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRCW, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkRootCCW(Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootCCW)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootCCW(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootCCW, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkRootConditionalWeakTableElementEdge(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootConditionalWeakTableElementEdge)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootConditionalWeakTableElementEdge(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootConditionalWeakTableElementEdge, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkRootEdge(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootEdge)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootEdge(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkRootEdge, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCBulkSurvivingObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkSurvivingObjectRanges)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkSurvivingObjectRanges(Microsoft_Windows_DotNETRuntimeHandle, &GCBulkSurvivingObjectRanges, Index, Count, ClrInstanceID, Values_Len_, Values) : 0

#define FireEtXplatGCCreateConcurrentThread_V1(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCCreateConcurrentThread_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCCreateConcurrentThread(Microsoft_Windows_DotNETRuntimeHandle, &GCCreateConcurrentThread_V1, ClrInstanceID) : 0

#define FireEtXplatGCCreateSegment_V1(Address, Size, Type, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCCreateSegment_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCCreateSegment_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCCreateSegment_V1, Address, Size, Type, ClrInstanceID) : 0

#define FireEtXplatGCEnd_V1(Count, Depth, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCEnd_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCEnd_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCEnd_V1, Count, Depth, ClrInstanceID) : 0

#define FireEtXplatGCFreeSegment_V1(Address, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCFreeSegment_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCFreeSegment_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCFreeSegment_V1, Address, ClrInstanceID) : 0

#define FireEtXplatGCGenerationRange(Generation, RangeStart, RangeUsedLength, RangeReservedLength, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCGenerationRange)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCGenerationRange(Microsoft_Windows_DotNETRuntimeHandle, &GCGenerationRange, Generation, RangeStart, RangeUsedLength, RangeReservedLength, ClrInstanceID) : 0

#define FireEtXplatGCGlobalHeapHistory_V2(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID, PauseMode, MemoryPressure) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCGlobalHeapHistory_V2)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCGlobalHeap_V2(Microsoft_Windows_DotNETRuntimeHandle, &GCGlobalHeapHistory_V2, FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID, PauseMode, MemoryPressure) : 0

#define FireEtXplatGCHeapStats_V1(GenerationSize0, TotalPromotedSize0, GenerationSize1, TotalPromotedSize1, GenerationSize2, TotalPromotedSize2, GenerationSize3, TotalPromotedSize3, FinalizationPromotedSize, FinalizationPromotedCount, PinnedObjectCount, SinkBlockCount, GCHandleCount, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCHeapStats_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCHeapStats_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCHeapStats_V1, GenerationSize0, TotalPromotedSize0, GenerationSize1, TotalPromotedSize1, GenerationSize2, TotalPromotedSize2, GenerationSize3, TotalPromotedSize3, FinalizationPromotedSize, FinalizationPromotedCount, PinnedObjectCount, SinkBlockCount, GCHandleCount, ClrInstanceID) : 0

#define FireEtXplatGCJoin_V2(Heap, JoinTime, JoinType, ClrInstanceID, JoinID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCJoin_V2)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCJoin_V2(Microsoft_Windows_DotNETRuntimeHandle, &GCJoin_V2, Heap, JoinTime, JoinType, ClrInstanceID, JoinID) : 0

#define FireEtXplatGCMarkFinalizeQueueRoots(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkFinalizeQueueRoots)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMark(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkFinalizeQueueRoots, HeapNum, ClrInstanceID) : 0

#define FireEtXplatGCMarkHandles(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkHandles)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMark(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkHandles, HeapNum, ClrInstanceID) : 0

#define FireEtXplatGCMarkOlderGenerationRoots(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkOlderGenerationRoots)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMark(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkOlderGenerationRoots, HeapNum, ClrInstanceID) : 0

#define FireEtXplatGCMarkStackRoots(HeapNum, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkStackRoots)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMark(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkStackRoots, HeapNum, ClrInstanceID) : 0

#define FireEtXplatGCMarkWithType(HeapNum, ClrInstanceID, Type, Bytes) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkWithType)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMarkWithType(Microsoft_Windows_DotNETRuntimeHandle, &GCMarkWithType, HeapNum, ClrInstanceID, Type, Bytes) : 0

#define FireEtXplatGCPerHeapHistory_V3(ClrInstanceID, FreeListAllocated, FreeListRejected, EndOfSegAllocated, CondemnedAllocated, PinnedAllocated, PinnedAllocatedAdvance, RunningFreeListEfficiency, CondemnReasons0, CondemnReasons1, CompactMechanisms, ExpandMechanisms, HeapIndex, ExtraGen0Commit, Count, Values_Len_, Values) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCPerHeapHistory_V3)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCPerHeapHistory_V3(Microsoft_Windows_DotNETRuntimeHandle, &GCPerHeapHistory_V3, ClrInstanceID, FreeListAllocated, FreeListRejected, EndOfSegAllocated, CondemnedAllocated, PinnedAllocated, PinnedAllocatedAdvance, RunningFreeListEfficiency, CondemnReasons0, CondemnReasons1, CompactMechanisms, ExpandMechanisms, HeapIndex, ExtraGen0Commit, Count, Values_Len_, Values) : 0

#define FireEtXplatGCRestartEEBegin_V1(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCRestartEEBegin_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimeHandle, &GCRestartEEBegin_V1, ClrInstanceID) : 0

#define FireEtXplatGCRestartEEEnd_V1(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCRestartEEEnd_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimeHandle, &GCRestartEEEnd_V1, ClrInstanceID) : 0

#define FireEtXplatGCStart_V1(Count, Depth, Reason, Type, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCStart_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCStart_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCStart_V1, Count, Depth, Reason, Type, ClrInstanceID) : 0

#define FireEtXplatGCStart_V2(Count, Depth, Reason, Type, ClrInstanceID, ClientSequenceNumber) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCStart_V2)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCStart_V2(Microsoft_Windows_DotNETRuntimeHandle, &GCStart_V2, Count, Depth, Reason, Type, ClrInstanceID, ClientSequenceNumber) : 0

#define FireEtXplatGCSuspendEEBegin_V1(Reason, Count, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCSuspendEEBegin_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCSuspendEE_V1(Microsoft_Windows_DotNETRuntimeHandle, &GCSuspendEEBegin_V1, Reason, Count, ClrInstanceID) : 0

#define FireEtXPlatGCSuspendEEEnd_V1(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCSuspendEEEnd_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCNoUserData(Microsoft_Windows_DotNETRuntimeHandle, &GCSuspendEEEnd_V1, ClrInstanceID) : 0

#define FireEtXplatGCTerminateConcurrentThread_V1(ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCTerminateConcurrentThread_V1)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCTerminateConcurrentThread(Microsoft_Windows_DotNETRuntimeHandle, &GCTerminateConcurrentThread_V1, ClrInstanceID) : 0

#define FireEtXplatGCTriggered(Reason, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &GCTriggered)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCTriggered(Microsoft_Windows_DotNETRuntimeHandle, &GCTriggered, Reason, ClrInstanceID) : 0

#define FireEtXplatModuleLoad_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &ModuleLoad_V2)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_ModuleLoadUnload_V2(Microsoft_Windows_DotNETRuntimeHandle, &ModuleLoad_V2, ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) : 0

#define FireEtXplatSetGCHandle(HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID) (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_DotNETRuntimeHandle, &SetGCHandle)) ? Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_SetGCHandle(Microsoft_Windows_DotNETRuntimeHandle, &SetGCHandle, HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID) : 0

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_BulkType(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[11];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[2], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_DestroyGCHandle(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, void* HandleID, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &HandleID, sizeof(void*));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Exception(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, LPCWSTR ExceptionType, LPCWSTR ExceptionMessage, void* ExceptionEIP, uint32_t ExceptionHRESULT, uint16_t ExceptionFlags, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], (ExceptionType != NULL) ? ExceptionType : L"", (ExceptionType != NULL) ? (ULONG)((wcslen(ExceptionType) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[1], (ExceptionMessage != NULL) ? ExceptionMessage : L"", (ExceptionMessage != NULL) ? (ULONG)((wcslen(ExceptionMessage) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[2], &ExceptionEIP, sizeof(void*));
    EventDataDescCreate(&EventData[3], &ExceptionHRESULT, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &ExceptionFlags, sizeof(uint16_t));
    EventDataDescCreate(&EventData[5], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 6, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t AllocationAmount, uint32_t AllocationKind, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[3];
    EventDataDescCreate(&EventData[0], &AllocationAmount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &AllocationKind, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V2(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t AllocationAmount, uint32_t AllocationKind, uint16_t ClrInstanceID, uint64_t AllocationAmount64, void* TypeID, LPCWSTR TypeName, uint32_t HeapIndex)
{
    EVENT_DATA_DESCRIPTOR EventData[7];
    EventDataDescCreate(&EventData[0], &AllocationAmount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &AllocationKind, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], &AllocationAmount64, sizeof(uint64_t));
    EventDataDescCreate(&EventData[4], &TypeID, sizeof(void*));
    EventDataDescCreate(&EventData[5], (TypeName != NULL) ? TypeName : L"", (TypeName != NULL) ? (ULONG)((wcslen(TypeName) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[6], &HeapIndex, sizeof(uint32_t));
    return PalEventWrite(RegHandle, Descriptor, 7, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCAllocationTick_V3(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t AllocationAmount, uint32_t AllocationKind, uint16_t ClrInstanceID, uint64_t AllocationAmount64, void* TypeID, LPCWSTR TypeName, uint32_t HeapIndex, void* Address)
{
    EVENT_DATA_DESCRIPTOR EventData[8];
    EventDataDescCreate(&EventData[0], &AllocationAmount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &AllocationKind, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], &AllocationAmount64, sizeof(uint64_t));
    EventDataDescCreate(&EventData[4], &TypeID, sizeof(void*));
    EventDataDescCreate(&EventData[5], (TypeName != NULL) ? TypeName : L"", (TypeName != NULL) ? (ULONG)((wcslen(TypeName) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[6], &HeapIndex, sizeof(uint32_t));
    EventDataDescCreate(&EventData[7], &Address, sizeof(void*));
    return PalEventWrite(RegHandle, Descriptor, 8, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkEdge(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkMovedObjectRanges(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[7];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkNode(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[8];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRCW(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[9];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[2], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootCCW(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[10];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[2], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootConditionalWeakTableElementEdge(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[7];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkRootEdge(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[8];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCBulkSurvivingObjectRanges(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Index, uint32_t Count, uint16_t ClrInstanceID, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], &Index, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[3], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCCreateConcurrentThread(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[1];
    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 1, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCCreateSegment_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t Address, uint64_t Size, uint32_t Type, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &Address, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &Size, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &Type, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCEnd_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint32_t Depth, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[3];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Depth, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCFreeSegment_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t Address, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &Address, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCGenerationRange(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint8_t Generation, void* RangeStart, uint64_t RangeUsedLength, uint64_t RangeReservedLength, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[5];
    EventDataDescCreate(&EventData[0], &Generation, sizeof(uint8_t));
    EventDataDescCreate(&EventData[1], &RangeStart, sizeof(void*));
    EventDataDescCreate(&EventData[2], &RangeUsedLength, sizeof(uint64_t));
    EventDataDescCreate(&EventData[3], &RangeReservedLength, sizeof(uint64_t));
    EventDataDescCreate(&EventData[4], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 5, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCGlobalHeap_V2(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t FinalYoungestDesired, int32_t NumHeaps, uint32_t CondemnedGeneration, uint32_t Gen0ReductionCount, uint32_t Reason, uint32_t GlobalMechanisms, uint16_t ClrInstanceID, uint32_t PauseMode, uint32_t MemoryPressure)
{
    EVENT_DATA_DESCRIPTOR EventData[9];
    EventDataDescCreate(&EventData[0], &FinalYoungestDesired, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &NumHeaps, sizeof(int32_t));
    EventDataDescCreate(&EventData[2], &CondemnedGeneration, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Gen0ReductionCount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[5], &GlobalMechanisms, sizeof(uint32_t));
    EventDataDescCreate(&EventData[6], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[7], &PauseMode, sizeof(uint32_t));
    EventDataDescCreate(&EventData[8], &MemoryPressure, sizeof(uint32_t));
    return PalEventWrite(RegHandle, Descriptor, 9, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCHeapStats_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t GenerationSize0, uint64_t TotalPromotedSize0, uint64_t GenerationSize1, uint64_t TotalPromotedSize1, uint64_t GenerationSize2, uint64_t TotalPromotedSize2, uint64_t GenerationSize3, uint64_t TotalPromotedSize3, uint64_t FinalizationPromotedSize, uint64_t FinalizationPromotedCount, uint32_t PinnedObjectCount, uint32_t SinkBlockCount, uint32_t GCHandleCount, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[14];
    EventDataDescCreate(&EventData[0], &GenerationSize0, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &TotalPromotedSize0, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &GenerationSize1, sizeof(uint64_t));
    EventDataDescCreate(&EventData[3], &TotalPromotedSize1, sizeof(uint64_t));
    EventDataDescCreate(&EventData[4], &GenerationSize2, sizeof(uint64_t));
    EventDataDescCreate(&EventData[5], &TotalPromotedSize2, sizeof(uint64_t));
    EventDataDescCreate(&EventData[6], &GenerationSize3, sizeof(uint64_t));
    EventDataDescCreate(&EventData[7], &TotalPromotedSize3, sizeof(uint64_t));
    EventDataDescCreate(&EventData[8], &FinalizationPromotedSize, sizeof(uint64_t));
    EventDataDescCreate(&EventData[9], &FinalizationPromotedCount, sizeof(uint64_t));
    EventDataDescCreate(&EventData[10], &PinnedObjectCount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[11], &SinkBlockCount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[12], &GCHandleCount, sizeof(uint32_t));
    EventDataDescCreate(&EventData[13], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 14, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCJoin_V2(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Heap, uint32_t JoinTime, uint32_t JoinType, uint16_t ClrInstanceID, uint32_t JoinID)
{
    EVENT_DATA_DESCRIPTOR EventData[5];
    EventDataDescCreate(&EventData[0], &Heap, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &JoinTime, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &JoinType, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[4], &JoinID, sizeof(uint32_t));
    return PalEventWrite(RegHandle, Descriptor, 5, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMark(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t HeapNum, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &HeapNum, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCMarkWithType(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t HeapNum, uint16_t ClrInstanceID, uint32_t Type, uint64_t Bytes)
{
    EVENT_DATA_DESCRIPTOR EventData[4];
    EventDataDescCreate(&EventData[0], &HeapNum, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[2], &Type, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Bytes, sizeof(uint64_t));
    return PalEventWrite(RegHandle, Descriptor, 4, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCNoUserData(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[1];
    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 1, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCPerHeapHistory_V3(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint16_t ClrInstanceID, void* FreeListAllocated, void* FreeListRejected, void* EndOfSegAllocated, void* CondemnedAllocated, void* PinnedAllocated, void* PinnedAllocatedAdvance, uint32_t RunningFreeListEfficiency, uint32_t CondemnReasons0, uint32_t CondemnReasons1, uint32_t CompactMechanisms, uint32_t ExpandMechanisms, uint32_t HeapIndex, void* ExtraGen0Commit, uint32_t Count, ULONG Values_Len_, const PVOID Values)
{
    EVENT_DATA_DESCRIPTOR EventData[26];
    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[1], &FreeListAllocated, sizeof(void*));
    EventDataDescCreate(&EventData[2], &FreeListRejected, sizeof(void*));
    EventDataDescCreate(&EventData[3], &EndOfSegAllocated, sizeof(void*));
    EventDataDescCreate(&EventData[4], &CondemnedAllocated, sizeof(void*));
    EventDataDescCreate(&EventData[5], &PinnedAllocated, sizeof(void*));
    EventDataDescCreate(&EventData[6], &PinnedAllocatedAdvance, sizeof(void*));
    EventDataDescCreate(&EventData[7], &RunningFreeListEfficiency, sizeof(uint32_t));
    EventDataDescCreate(&EventData[8], &CondemnReasons0, sizeof(uint32_t));
    EventDataDescCreate(&EventData[9], &CondemnReasons1, sizeof(uint32_t));
    EventDataDescCreate(&EventData[10], &CompactMechanisms, sizeof(uint32_t));
    EventDataDescCreate(&EventData[11], &ExpandMechanisms, sizeof(uint32_t));
    EventDataDescCreate(&EventData[12], &HeapIndex, sizeof(uint32_t));
    EventDataDescCreate(&EventData[13], &ExtraGen0Commit, sizeof(void*));
    EventDataDescCreate(&EventData[14], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[15], Values, Count * Values_Len_);
    return PalEventWrite(RegHandle, Descriptor, 16, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCStart_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint32_t Depth, uint32_t Reason, uint32_t Type, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[5];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Depth, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Type, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 5, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCStart_V2(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Count, uint32_t Depth, uint32_t Reason, uint32_t Type, uint16_t ClrInstanceID, uint64_t ClientSequenceNumber)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Depth, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Type, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[5], &ClientSequenceNumber, sizeof(uint64_t));
    return PalEventWrite(RegHandle, Descriptor, 6, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCSuspendEE_V1(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Reason, uint32_t Count, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[3];
    EventDataDescCreate(&EventData[0], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &Count, sizeof(uint32_t));
    EventDataDescCreate(&EventData[2], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 3, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCTerminateConcurrentThread(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[1];
    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 1, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_GCTriggered(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint32_t Reason, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[2];
    EventDataDescCreate(&EventData[0], &Reason, sizeof(uint32_t));
    EventDataDescCreate(&EventData[1], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 2, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_ModuleLoadUnload_V2(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, uint64_t ModuleID, uint64_t AssemblyID, uint32_t ModuleFlags, uint32_t Reserved1, LPCWSTR ModuleILPath, LPCWSTR ModuleNativePath, uint16_t ClrInstanceID, const GUID* ManagedPdbSignature, uint32_t ManagedPdbAge, LPCWSTR ManagedPdbBuildPath, const GUID* NativePdbSignature, uint32_t NativePdbAge, LPCWSTR NativePdbBuildPath)
{
    EVENT_DATA_DESCRIPTOR EventData[13];
    EventDataDescCreate(&EventData[0], &ModuleID, sizeof(uint64_t));
    EventDataDescCreate(&EventData[1], &AssemblyID, sizeof(uint64_t));
    EventDataDescCreate(&EventData[2], &ModuleFlags, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Reserved1, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], (ModuleILPath != NULL) ? ModuleILPath : L"", (ModuleILPath != NULL) ? (ULONG)((wcslen(ModuleILPath) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[5], (ModuleNativePath != NULL) ? ModuleNativePath : L"", (ModuleNativePath != NULL) ? (ULONG)((wcslen(ModuleNativePath) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[6], &ClrInstanceID, sizeof(uint16_t));
    EventDataDescCreate(&EventData[7], ManagedPdbSignature, sizeof(*(ManagedPdbSignature)));
    EventDataDescCreate(&EventData[8], &ManagedPdbAge, sizeof(uint32_t));
    EventDataDescCreate(&EventData[9], (ManagedPdbBuildPath != NULL) ? ManagedPdbBuildPath : L"", (ManagedPdbBuildPath != NULL) ? (ULONG)((wcslen(ManagedPdbBuildPath) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    EventDataDescCreate(&EventData[10], NativePdbSignature, sizeof(*(NativePdbSignature)));
    EventDataDescCreate(&EventData[11], &NativePdbAge, sizeof(uint32_t));
    EventDataDescCreate(&EventData[12], (NativePdbBuildPath != NULL) ? NativePdbBuildPath : L"", (NativePdbBuildPath != NULL) ? (ULONG)((wcslen(NativePdbBuildPath) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L""));
    return PalEventWrite(RegHandle, Descriptor, 13, EventData);
}

RH_ETW_INLINE uint32_t
Template_MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_SetGCHandle(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor, void* HandleID, void* ObjectID, uint32_t Kind, uint32_t Generation, uint64_t AppDomainID, uint16_t ClrInstanceID)
{
    EVENT_DATA_DESCRIPTOR EventData[6];
    EventDataDescCreate(&EventData[0], &HandleID, sizeof(void*));
    EventDataDescCreate(&EventData[1], &ObjectID, sizeof(void*));
    EventDataDescCreate(&EventData[2], &Kind, sizeof(uint32_t));
    EventDataDescCreate(&EventData[3], &Generation, sizeof(uint32_t));
    EventDataDescCreate(&EventData[4], &AppDomainID, sizeof(uint64_t));
    EventDataDescCreate(&EventData[5], &ClrInstanceID, sizeof(uint16_t));
    return PalEventWrite(RegHandle, Descriptor, 6, EventData);
}

RH_ETW_INLINE uint32_t
TemplateEventDescriptor(REGHANDLE RegHandle, const EVENT_DESCRIPTOR * Descriptor)
{
    return PalEventWrite(RegHandle, Descriptor, 0, NULL);
}

#else // FEATURE_ETW

#define ETW_EVENT_ENABLED(Context, EventDescriptor) false

#define FireEtXplatDestroyGCHandle(HandleID, ClrInstanceID)
#define FireEtXplatExceptionThrown_V1(ExceptionType, ExceptionMessage, ExceptionEIP, ExceptionHRESULT, ExceptionFlags, ClrInstanceID)
#define FireEtXplatGCAllocationTick_V1(AllocationAmount, AllocationKind, ClrInstanceID)
#define FireEtXplatGCAllocationTick_V2(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex)
#define FireEtXplatGCAllocationTick_V3(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex, Address)
#define FireEtXplatGCBulkEdge(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkMovedObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkNode(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkRCW(Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkRootCCW(Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkRootConditionalWeakTableElementEdge(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkRootEdge(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCBulkSurvivingObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values)
#define FireEtXplatGCCreateConcurrentThread_V1(ClrInstanceID)
#define FireEtXplatGCCreateSegment_V1(Address, Size, Type, ClrInstanceID)
#define FireEtXplatGCEnd_V1(Count, Depth, ClrInstanceID)
#define FireEtXplatGCFreeSegment_V1(Address, ClrInstanceID)
#define FireEtXplatGCGenerationRange(Generation, RangeStart, RangeUsedLength, RangeReservedLength, ClrInstanceID)
#define FireEtXplatGCGlobalHeapHistory_V2(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID, PauseMode, MemoryPressure)
#define FireEtXplatGCHeapStats_V1(GenerationSize0, TotalPromotedSize0, GenerationSize1, TotalPromotedSize1, GenerationSize2, TotalPromotedSize2, GenerationSize3, TotalPromotedSize3, FinalizationPromotedSize, FinalizationPromotedCount, PinnedObjectCount, SinkBlockCount, GCHandleCount, ClrInstanceID)
#define FireEtXplatGCJoin_V2(Heap, JoinTime, JoinType, ClrInstanceID, JoinID)
#define FireEtXplatGCMarkFinalizeQueueRoots(HeapNum, ClrInstanceID)
#define FireEtXplatGCMarkHandles(HeapNum, ClrInstanceID)
#define FireEtXplatGCMarkOlderGenerationRoots(HeapNum, ClrInstanceID)
#define FireEtXplatGCMarkStackRoots(HeapNum, ClrInstanceID)
#define FireEtXplatGCMarkWithType(HeapNum, ClrInstanceID, Type, Bytes)
#define FireEtXplatGCPerHeapHistory_V3(ClrInstanceID, FreeListAllocated, FreeListRejected, EndOfSegAllocated, CondemnedAllocated, PinnedAllocated, PinnedAllocatedAdvance, RunningFreeListEfficiency, CondemnReasons0, CondemnReasons1, CompactMechanisms, ExpandMechanisms, HeapIndex, ExtraGen0Commit, Count, Values_Len_, Values)
#define FireEtXplatGCRestartEEBegin_V1(ClrInstanceID)
#define FireEtXplatGCRestartEEEnd_V1(ClrInstanceID)
#define FireEtXplatGCStart_V1(Count, Depth, Reason, Type, ClrInstanceID)
#define FireEtXplatGCStart_V2(Count, Depth, Reason, Type, ClrInstanceID, ClientSequenceNumber)
#define FireEtXplatGCSuspendEEBegin_V1(Reason, Count, ClrInstanceID)
#define FireEtXPlatGCSuspendEEEnd_V1(ClrInstanceID)
#define FireEtXplatGCTerminateConcurrentThread_V1(ClrInstanceID)
#define FireEtXplatGCTriggered(Reason, ClrInstanceID)
#define FireEtXplatModuleLoad_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath)
#define FireEtXplatSetGCHandle(HandleID, ObjectID, Kind, Generation, AppDomainID, ClrInstanceID)

#endif // FEATURE_ETW

#endif // !__RH_ETW_DEFS_INCLUDED
