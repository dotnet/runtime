// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header contains the definition of an interface between the GC/HandleTable portions of the Redhawk
// codebase and the regular Redhawk code.

#ifndef __RedhawkGCInterface_h__
#define __RedhawkGCInterface_h__

#include "forward_declarations.h"

#ifndef DACCESS_COMPILE
// Global data cells exported by the GC.
extern "C" unsigned char *g_ephemeral_low;
extern "C" unsigned char *g_ephemeral_high;
extern "C" unsigned char *g_lowest_address;
extern "C" unsigned char *g_highest_address;
#endif

class MethodInfo;
struct REGDISPLAY;
enum GCRefKind : unsigned char;
class ICodeManager;

typedef void * GcSegmentHandle;

#define RH_LARGE_OBJECT_SIZE 85000

// A 'clump' is defined as the size of memory covered by 1 byte in the card table.  These constants are
// verified against gcpriv.h in gcrhee.cpp.
#if (POINTER_SIZE == 8)
#define CLUMP_SIZE 0x800
#define LOG2_CLUMP_SIZE 11
#elif (POINTER_SIZE == 4)
#define CLUMP_SIZE 0x400
#define LOG2_CLUMP_SIZE 10
#else
#error unexpected pointer size
#endif

class RedhawkGCInterface
{
public:
    // Perform any runtime-startup initialization needed by the GC, HandleTable or environmental code in
    // gcrhenv. Returns true on success or false if a subsystem failed to initialize.
    static bool InitializeSubsystems();

    static void InitAllocContext(gc_alloc_context * pAllocContext);
    static void ReleaseAllocContext(gc_alloc_context * pAllocContext);

    static void WaitForGCCompletion();

    static void EnumGcRef(PTR_RtuObjectRef pRef, GCRefKind kind, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);
    static void EnumGcRefConservatively(PTR_RtuObjectRef pRef, ScanFunc* pfnEnumCallback, ScanContext* pvCallbackData);

    static void EnumGcRefs(ICodeManager * pCodeManager,
                           MethodInfo * pMethodInfo,
                           PTR_VOID safePointAddress,
                           REGDISPLAY * pRegisterSet,
                           ScanFunc* pfnEnumCallback,
                           ScanContext* pvCallbackData,
                           bool   isActiveStackFrame);

    static void EnumGcRefsInRegionConservatively(PTR_RtuObjectRef pLowerBound,
                                                 PTR_RtuObjectRef pUpperBound,
                                                 ScanFunc* pfnEnumCallback,
                                                 ScanContext* pvCallbackData);

    static GcSegmentHandle RegisterFrozenSegment(void * pSection, size_t allocSize, size_t commitSize, size_t reservedSize);
    static void UpdateFrozenSegment(GcSegmentHandle seg, uint8_t* allocated, uint8_t* committed);
    static void UnregisterFrozenSegment(GcSegmentHandle segment);

#ifdef FEATURE_GC_STRESS
    static void StressGc();
#endif // FEATURE_GC_STRESS

    // Returns size GCDesc. Used by type cloning.
    static uint32_t GetGCDescSize(void * pType);

    // These methods are used to get and set the type information for the last allocation on each thread.
    static MethodTable * GetLastAllocEEType();
    static void SetLastAllocEEType(MethodTable *pEEType);

    static uint64_t GetDeadThreadsNonAllocBytes();

    // Used by debugger hook
    static void* CreateTypedHandle(void* object, int type);
    static void DestroyTypedHandle(void* handle);

private:
    // The MethodTable for the last allocation.  This value is used inside of the GC allocator
    // to emit allocation ETW events with type information.  We set this value unconditionally to avoid
    // race conditions where ETW is enabled after the value is set.
    static DECLSPEC_THREAD MethodTable * tls_pLastAllocationEEType;

    // Tracks the amount of bytes that were reserved for threads in their gc_alloc_context and went unused when they died.
    // Used for GC.GetTotalAllocatedBytes
    static uint64_t s_DeadThreadsNonAllocBytes;
};

#endif // __RedhawkGCInterface_h__
