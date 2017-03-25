// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * GCHELPERS.CPP 
 *
 * GC Allocation and Write Barrier Helpers
 *

 *
 */

#include "common.h"
#include "object.h"
#include "threads.h"
#include "eetwain.h"
#include "eeconfig.h"
#include "gcheaputilities.h"
#include "corhost.h"
#include "threads.h"
#include "fieldmarshaler.h"
#include "interoputil.h"
#include "dynamicmethod.h"
#include "stubhelpers.h"
#include "eventtrace.h"

#include "excep.h"

#include "gchelpers.inl"
#include "eeprofinterfaces.inl"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "rcwwalker.h"

//========================================================================
//
//      ALLOCATION HELPERS
//
//========================================================================

#define ProfileTrackArrayAlloc(orObject) \
            OBJECTREF objref = ObjectToOBJECTREF((Object*)orObject);\
            GCPROTECT_BEGIN(objref);\
            ProfilerObjectAllocatedCallback(objref, (ClassID) orObject->GetTypeHandle().AsPtr());\
            GCPROTECT_END();\
            orObject = (ArrayBase *) OBJECTREFToObject(objref);


inline gc_alloc_context* GetThreadAllocContext()
{
    WRAPPER_NO_CONTRACT;

    assert(GCHeapUtilities::UseThreadAllocationContexts());

    return & GetThread()->m_alloc_context;
}

// When not using per-thread allocation contexts, we (the EE) need to take care that
// no two threads are concurrently modifying the global allocation context. This lock
// must be acquired before any sort of operations involving the global allocation context
// can occur.
//
// This lock is acquired by all allocations when not using per-thread allocation contexts.
// It is acquired in two kinds of places:
//   1) JIT_TrialAllocFastSP (and related assembly alloc helpers), which attempt to
//      acquire it but move into an alloc slow path if acquiring fails
//      (but does not decrement the lock variable when doing so)
//   2) Alloc and AllocAlign8 in gchelpers.cpp, which acquire the lock using
//      the Acquire and Release methods below.
class GlobalAllocLock {
    friend struct AsmOffsets;
private:
    // The lock variable. This field must always be first.
    LONG m_lock;

public:
    // Creates a new GlobalAllocLock in the unlocked state.
    GlobalAllocLock() : m_lock(-1) {}

    // Copy and copy-assignment operators should never be invoked
    // for this type
    GlobalAllocLock(const GlobalAllocLock&) = delete;
    GlobalAllocLock& operator=(const GlobalAllocLock&) = delete;

    // Acquires the lock, spinning if necessary to do so. When this method
    // returns, m_lock will be zero and the lock will be acquired.
    void Acquire()
    {
        CONTRACTL {
            NOTHROW;
            GC_TRIGGERS; // switch to preemptive mode
            MODE_COOPERATIVE;
        } CONTRACTL_END;

        DWORD spinCount = 0;
        while(FastInterlockExchange(&m_lock, 0) != -1)
        {
            GCX_PREEMP();
            __SwitchToThread(0, spinCount++);
        }

        assert(m_lock == 0);
    }

    // Releases the lock.
    void Release()
    {
        LIMITED_METHOD_CONTRACT;

        // the lock may not be exactly 0. This is because the
        // assembly alloc routines increment the lock variable and
        // jump if not zero to the slow alloc path, which eventually
        // will try to acquire the lock again. At that point, it will
        // spin in Acquire (since m_lock is some number that's not zero).
        // When the thread that /does/ hold the lock releases it, the spinning
        // thread will continue.
        MemoryBarrier();
        assert(m_lock >= 0);
        m_lock = -1;
    }

    // Static helper to acquire a lock, for use with the Holder template.
    static void AcquireLock(GlobalAllocLock *lock)
    {
        WRAPPER_NO_CONTRACT;
        lock->Acquire();
    }

    // Static helper to release a lock, for use with the Holder template
    static void ReleaseLock(GlobalAllocLock *lock)
    {
        WRAPPER_NO_CONTRACT;
        lock->Release();
    }

    typedef Holder<GlobalAllocLock *, GlobalAllocLock::AcquireLock, GlobalAllocLock::ReleaseLock> Holder;
};

typedef GlobalAllocLock::Holder GlobalAllocLockHolder;

struct AsmOffsets {
    static_assert(offsetof(GlobalAllocLock, m_lock) == 0, "ASM code relies on this property");
};

// For single-proc machines, the global allocation context is protected
// from concurrent modification by this lock.
//
// When not using per-thread allocation contexts, certain methods on IGCHeap
// require that this lock be held before calling. These methods are documented
// on the IGCHeap interface.
extern "C"
{
    GlobalAllocLock g_global_alloc_lock;
}


// Checks to see if the given allocation size exceeds the
// largest object size allowed - if it does, it throws
// an OutOfMemoryException with a message indicating that
// the OOM was not from memory pressure but from an object
// being too large.
inline void CheckObjectSize(size_t alloc_size)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    size_t max_object_size;
#ifdef BIT64
    if (g_pConfig->GetGCAllowVeryLargeObjects())
    {
        max_object_size = (INT64_MAX - 7 - min_obj_size);
    }
    else
#endif // BIT64
    {
        max_object_size = (INT32_MAX - 7 - min_obj_size);
    }

    if (alloc_size >= max_object_size)
    {
        if (g_pConfig->IsGCBreakOnOOMEnabled())
        {
            DebugBreak();
        }

        ThrowOutOfMemoryDimensionsExceeded();
    }
}


// There are only three ways to get into allocate an object.
//     * Call optimized helpers that were generated on the fly. This is how JIT compiled code does most
//         allocations, however they fall back code:Alloc, when for all but the most common code paths. These
//         helpers are NOT used if profiler has asked to track GC allocation (see code:TrackAllocations)
//     * Call code:Alloc - When the jit helpers fall back, or we do allocations within the runtime code
//         itself, we ultimately call here.
//     * Call code:AllocLHeap - Used very rarely to force allocation to be on the large object heap.
//
// While this is a choke point into allocating an object, it is primitive (it does not want to know about
// MethodTable and thus does not initialize that pointer. It also does not know if the object is finalizable
// or contains pointers. Thus we quickly wrap this function in more user-friendly ones that know about
// MethodTables etc. (see code:FastAllocatePrimitiveArray code:AllocateArrayEx code:AllocateObject)
//
// You can get an exhaustive list of code sites that allocate GC objects by finding all calls to
// code:ProfilerObjectAllocatedCallback (since the profiler has to hook them all).
inline Object* Alloc(size_t size, BOOL bFinalize, BOOL bContainsPointers )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

    _ASSERTE(!NingenEnabled() && "You cannot allocate managed objects inside the ngen compilation process.");

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP))
    {
        char *a = new char;
        delete a;
    }
#endif

    DWORD flags = ((bContainsPointers ? GC_ALLOC_CONTAINS_REF : 0) |
                   (bFinalize ? GC_ALLOC_FINALIZE : 0));

    Object *retVal = NULL;
    CheckObjectSize(size);

    // We don't want to throw an SO during the GC, so make sure we have plenty
    // of stack before calling in.
    INTERIOR_STACK_PROBE_FOR(GetThread(), static_cast<unsigned>(DEFAULT_ENTRY_PROBE_AMOUNT * 1.5));
    if (GCHeapUtilities::UseThreadAllocationContexts())
    {
        retVal = GCHeapUtilities::GetGCHeap()->Alloc(GetThreadAllocContext(), size, flags);
    }
    else
    {
        GlobalAllocLockHolder holder(&g_global_alloc_lock);
        retVal = GCHeapUtilities::GetGCHeap()->Alloc(&g_global_alloc_context, size, flags);
    }


    if (!retVal)
    {
        ThrowOutOfMemory();
    }

    END_INTERIOR_STACK_PROBE;
    return retVal;
}

#ifdef FEATURE_64BIT_ALIGNMENT
// Helper for allocating 8-byte aligned objects (on platforms where this doesn't happen naturally, e.g. 32-bit
// platforms).
inline Object* AllocAlign8(size_t size, BOOL bFinalize, BOOL bContainsPointers, BOOL bAlignBias)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

    DWORD flags = ((bContainsPointers ? GC_ALLOC_CONTAINS_REF : 0) |
                   (bFinalize ? GC_ALLOC_FINALIZE : 0) |
                   (bAlignBias ? GC_ALLOC_ALIGN8_BIAS : 0));

    Object *retVal = NULL;
    CheckObjectSize(size);

    // We don't want to throw an SO during the GC, so make sure we have plenty
    // of stack before calling in.
    INTERIOR_STACK_PROBE_FOR(GetThread(), static_cast<unsigned>(DEFAULT_ENTRY_PROBE_AMOUNT * 1.5));
    if (GCHeapUtilities::UseThreadAllocationContexts())
    {
        retVal = GCHeapUtilities::GetGCHeap()->AllocAlign8(GetThreadAllocContext(), size, flags);
    }
    else
    {
        GlobalAllocLockHolder holder(&g_global_alloc_lock);
        retVal = GCHeapUtilities::GetGCHeap()->AllocAlign8(&g_global_alloc_context, size, flags);
    }

    if (!retVal)
    {
        ThrowOutOfMemory();
    }

    END_INTERIOR_STACK_PROBE;
    return retVal;
}
#endif // FEATURE_64BIT_ALIGNMENT

// This is one of three ways of allocating an object (see code:Alloc for more). This variation is used in the
// rare circumstance when you want to allocate an object on the large object heap but the object is not big
// enough to naturally go there.  
// 
// One (and only?) example of where this is needed is 8 byte aligning of arrays of doubles. See
// code:EEConfig.GetDoubleArrayToLargeObjectHeapThreshold and code:CORINFO_HELP_NEWARR_1_ALIGN8 for more.
inline Object* AllocLHeap(size_t size, BOOL bFinalize, BOOL bContainsPointers )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative (don't assume large heap doesn't compact!)
    } CONTRACTL_END;


    _ASSERTE(!NingenEnabled() && "You cannot allocate managed objects inside the ngen compilation process.");

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP))
    {
        char *a = new char;
        delete a;
    }
#endif

    DWORD flags = ((bContainsPointers ? GC_ALLOC_CONTAINS_REF : 0) |
                   (bFinalize ? GC_ALLOC_FINALIZE : 0));

    Object *retVal = NULL;
    CheckObjectSize(size);

    // We don't want to throw an SO during the GC, so make sure we have plenty
    // of stack before calling in.
    INTERIOR_STACK_PROBE_FOR(GetThread(), static_cast<unsigned>(DEFAULT_ENTRY_PROBE_AMOUNT * 1.5));
    retVal = GCHeapUtilities::GetGCHeap()->AllocLHeap(size, flags);

    if (!retVal)
    {
        ThrowOutOfMemory();
    }

    END_INTERIOR_STACK_PROBE;
    return retVal;
}


#ifdef  _LOGALLOC
int g_iNumAllocs = 0;

bool ToLogOrNotToLog(size_t size, const char *typeName)
{
    WRAPPER_NO_CONTRACT;

    g_iNumAllocs++;

    if (g_iNumAllocs > g_pConfig->AllocNumThreshold())
        return true;

    if (size > (size_t)g_pConfig->AllocSizeThreshold())
        return true;

    if (g_pConfig->ShouldLogAlloc(typeName))
        return true;

    return false;

}

// READ THIS!!!!!
// this function is called on managed allocation path with unprotected Object*
// as a result LogAlloc cannot call anything that would toggle the GC mode else
// you'll introduce several GC holes!
inline void LogAlloc(size_t size, MethodTable *pMT, Object* object)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef LOGGING
    if (LoggingOn(LF_GCALLOC, LL_INFO10))
    {
        LogSpewAlways("Allocated %5d bytes for %s_TYPE" FMT_ADDR FMT_CLASS "\n",
                      size,
                      pMT->IsValueType() ? "VAL" : "REF", 
                      DBG_ADDR(object),
                      DBG_CLASS_NAME_MT(pMT));

        if (LoggingOn(LF_GCALLOC, LL_INFO1000000)    || 
            (LoggingOn(LF_GCALLOC, LL_INFO100)   && 
             ToLogOrNotToLog(size, DBG_CLASS_NAME_MT(pMT))))
            {
                void LogStackTrace();
                LogStackTrace();
            }
        }
#endif
}
#else
#define LogAlloc(size, pMT, object)
#endif


inline SIZE_T MaxArrayLength(SIZE_T componentSize)
{
    // Impose limits on maximum array length in each dimension to allow efficient 
    // implementation of advanced range check elimination in future. We have to allow 
    // higher limit for array of bytes (or one byte structs) for backward compatibility.
    // Keep in sync with Array.MaxArrayLength in BCL.
    return (componentSize == 1) ? 0X7FFFFFC7 : 0X7FEFFFFF;
}

OBJECTREF AllocateValueSzArray(TypeHandle elementType, INT32 length)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative        
    } CONTRACTL_END;

    return AllocateArrayEx(elementType.MakeSZArray(), &length, 1);
}

void ThrowOutOfMemoryDimensionsExceeded()
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

#ifdef _WIN64
    EX_THROW(EEMessageException, (kOutOfMemoryException, IDS_EE_ARRAY_DIMENSIONS_EXCEEDED));
#else
    ThrowOutOfMemory();
#endif
}

//
// Handles arrays of arbitrary dimensions
//
// If dwNumArgs is set to greater than 1 for a SZARRAY this function will recursively 
// allocate sub-arrays and fill them in.  
//
// For arrays with lower bounds, pBounds is <lower bound 1>, <count 1>, <lower bound 2>, ...
OBJECTREF AllocateArrayEx(TypeHandle arrayType, INT32 *pArgs, DWORD dwNumArgs, BOOL bAllocateInLargeHeap 
                          DEBUG_ARG(BOOL bDontSetAppDomain))
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
        PRECONDITION(CheckPointer(pArgs));
        PRECONDITION(dwNumArgs > 0);
    } CONTRACTL_END;

    ArrayBase * orArray = NULL;

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP))
    {
        char *a = new char;
        delete a;
    }
#endif

    ArrayTypeDesc* arrayDesc = arrayType.AsArray();
    MethodTable* pArrayMT = arrayDesc->GetMethodTable();
    _ASSERTE(pArrayMT->CheckInstanceActivated());
    PREFIX_ASSUME(pArrayMT != NULL);
    CorElementType kind = arrayType.GetInternalCorElementType();
    _ASSERTE(kind == ELEMENT_TYPE_ARRAY || kind == ELEMENT_TYPE_SZARRAY);
    
    CorElementType elemType = arrayDesc->GetTypeParam().GetInternalCorElementType();
    // Disallow the creation of void[,] (a multi-dim  array of System.Void)
    if (elemType == ELEMENT_TYPE_VOID)
        COMPlusThrow(kArgumentException);

    // Calculate the total number of elements in the array
    UINT32 cElements;

    // IBC Log MethodTable access
    g_IBCLogger.LogMethodTableAccess(pArrayMT);
    SetTypeHandleOnThreadForAlloc(arrayType);

    SIZE_T componentSize = pArrayMT->GetComponentSize();
    bool maxArrayDimensionLengthOverflow = false;
    bool providedLowerBounds = false;

    if (kind == ELEMENT_TYPE_ARRAY)
    {
        unsigned rank = arrayDesc->GetRank();
        _ASSERTE(dwNumArgs == rank || dwNumArgs == 2*rank);

        // Morph a ARRAY rank 1 with 0 lower bound into an SZARRAY
        if (rank == 1 && (dwNumArgs == 1 || pArgs[0] == 0)) 
        {   // lower bound is zero

            // This recursive call doesn't go any farther, because the dwNumArgs will be 1,
            //  so don't bother with stack probe.
            TypeHandle szArrayType = ClassLoader::LoadArrayTypeThrowing(arrayDesc->GetArrayElementTypeHandle(), ELEMENT_TYPE_SZARRAY, 1);
            return AllocateArrayEx(szArrayType, &pArgs[dwNumArgs - 1], 1, bAllocateInLargeHeap DEBUG_ARG(bDontSetAppDomain));
        }

        providedLowerBounds = (dwNumArgs == 2*rank);

        S_UINT32 safeTotalElements = S_UINT32(1);

        for (unsigned i = 0; i < dwNumArgs; i++)
        {
            int lowerBound = 0;
            if (providedLowerBounds)
            {
                lowerBound = pArgs[i];
                i++;
            }
            int length = pArgs[i];
            if (length < 0)
                COMPlusThrow(kOverflowException);
            if ((SIZE_T)length > MaxArrayLength(componentSize))
                maxArrayDimensionLengthOverflow = true;
            if ((length > 0) && (lowerBound + (length - 1) < lowerBound))
                COMPlusThrow(kArgumentOutOfRangeException, W("ArgumentOutOfRange_ArrayLBAndLength"));
            safeTotalElements = safeTotalElements * S_UINT32(length);
            if (safeTotalElements.IsOverflow())
                ThrowOutOfMemoryDimensionsExceeded();
        }

        cElements = safeTotalElements.Value();
    } 
    else
    {
        int length = pArgs[0];
        if (length < 0)
            COMPlusThrow(kOverflowException);
        if ((SIZE_T)length > MaxArrayLength(componentSize))
            maxArrayDimensionLengthOverflow = true;
        cElements = length;         
    }

    // Throw this exception only after everything else was validated for backward compatibility.
    if (maxArrayDimensionLengthOverflow)
        ThrowOutOfMemoryDimensionsExceeded();

    // Allocate the space from the GC heap
    S_SIZE_T safeTotalSize = S_SIZE_T(cElements) * S_SIZE_T(componentSize) + S_SIZE_T(pArrayMT->GetBaseSize());
    if (safeTotalSize.IsOverflow())
        ThrowOutOfMemoryDimensionsExceeded();

    size_t totalSize = safeTotalSize.Value();

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    if ((elemType == ELEMENT_TYPE_R8) && 
        (cElements >= g_pConfig->GetDoubleArrayToLargeObjectHeapThreshold()))
    {
        STRESS_LOG2(LF_GC, LL_INFO10, "Allocating double MD array of size %d and length %d to large object heap\n", totalSize, cElements);
        bAllocateInLargeHeap = TRUE;
    }
#endif

    if (bAllocateInLargeHeap)
    {
        orArray = (ArrayBase *) AllocLHeap(totalSize, FALSE, pArrayMT->ContainsPointers());
        orArray->SetMethodTableForLargeObject(pArrayMT);
    }
    else
    {
#ifdef FEATURE_64BIT_ALIGNMENT
        MethodTable *pElementMT = arrayDesc->GetTypeParam().GetMethodTable();
        if (pElementMT->RequiresAlign8() && pElementMT->IsValueType())
        {
            // This platform requires that certain fields are 8-byte aligned (and the runtime doesn't provide
            // this guarantee implicitly, e.g. on 32-bit platforms). Since it's the array payload, not the
            // header that requires alignment we need to be careful. However it just so happens that all the
            // cases we care about (single and multi-dim arrays of value types) have an even number of DWORDs
            // in their headers so the alignment requirements for the header and the payload are the same.
            _ASSERTE(((pArrayMT->GetBaseSize() - SIZEOF_OBJHEADER) & 7) == 0);
            orArray = (ArrayBase *) AllocAlign8(totalSize, FALSE, pArrayMT->ContainsPointers(), FALSE);
        }
        else
#endif
        {
            orArray = (ArrayBase *) Alloc(totalSize, FALSE, pArrayMT->ContainsPointers());
        }
        orArray->SetMethodTable(pArrayMT);
    }

    // Initialize Object
    orArray->m_NumComponents = cElements;

    if (bAllocateInLargeHeap || 
        (totalSize >= LARGE_OBJECT_SIZE))
    {
        GCHeapUtilities::GetGCHeap()->PublishObject((BYTE*)orArray);
    }

#ifdef  _LOGALLOC
    LogAlloc(totalSize, pArrayMT, orArray);
#endif // _LOGALLOC

#ifdef _DEBUG
    // Ensure the typehandle has been interned prior to allocation.
    // This is important for OOM reliability.
    OBJECTREF objref = ObjectToOBJECTREF((Object *) orArray);
    GCPROTECT_BEGIN(objref);

    orArray->GetTypeHandle(); 

    GCPROTECT_END();    
    orArray = (ArrayBase *) OBJECTREFToObject(objref);
#endif

#if CHECK_APP_DOMAIN_LEAKS
    if (!bDontSetAppDomain && g_pConfig->AppDomainLeaks())
        orArray->SetAppDomain();
#endif

    if (kind == ELEMENT_TYPE_ARRAY)
    {
        INT32 *pCountsPtr      = (INT32 *) orArray->GetBoundsPtr();
        INT32 *pLowerBoundsPtr = (INT32 *) orArray->GetLowerBoundsPtr();
        for (unsigned i = 0; i < dwNumArgs; i++)
        {
            if (providedLowerBounds)
                *pLowerBoundsPtr++ = pArgs[i++];        // if not stated, lower bound becomes 0
            *pCountsPtr++ = pArgs[i];
        }
    }

    // Notify the profiler of the allocation
    // do this after initializing bounds so callback has size information
    if (TrackAllocations())
    {
        ProfileTrackArrayAlloc(orArray);
    }

#ifdef FEATURE_EVENT_TRACE
    // Send ETW event for allocation
    if(ETW::TypeSystemLog::IsHeapAllocEventEnabled())
    {
        ETW::TypeSystemLog::SendObjectAllocatedEvent(orArray);
    }
#endif // FEATURE_EVENT_TRACE

    if (kind != ELEMENT_TYPE_ARRAY)
    {
        // Handle allocating multiple jagged array dimensions at once
        if (dwNumArgs > 1)
        {
            PTRARRAYREF outerArray = (PTRARRAYREF) ObjectToOBJECTREF((Object *) orArray);
            GCPROTECT_BEGIN(outerArray);

            // Turn off GC stress, it is of little value here
            {
                GCStressPolicy::InhibitHolder iholder;
                
                // Allocate dwProvidedBounds arrays
                if (!arrayDesc->GetArrayElementTypeHandle().IsArray())
                {
                    orArray = NULL;
                }
                else
                {
                    // Since we're about to *really* recurse, probe for stack.
                    // @todo: is the default amount really correct? 
                    _ASSERTE(GetThread());
                    INTERIOR_STACK_PROBE(GetThread());

                    TypeHandle subArrayType = arrayDesc->GetArrayElementTypeHandle();
                    for (UINT32 i = 0; i < cElements; i++)
                    {
                        OBJECTREF obj = AllocateArrayEx(subArrayType, &pArgs[1], dwNumArgs-1, bAllocateInLargeHeap DEBUG_ARG(bDontSetAppDomain));
                        outerArray->SetAt(i, obj);
                    }

                    iholder.Release();

                    END_INTERIOR_STACK_PROBE

                    orArray = (ArrayBase *) OBJECTREFToObject(outerArray);
                }
            } // GcStressPolicy::~InhibitHolder()
            
            GCPROTECT_END();
        }
    }

    return ObjectToOBJECTREF((Object *) orArray);
}

/*
 * Allocates a single dimensional array of primitive types.
 */
OBJECTREF   AllocatePrimitiveArray(CorElementType type, DWORD cElements, BOOL bAllocateInLargeHeap)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_COOPERATIVE;  // returns an objref without pinning it => cooperative
    }
    CONTRACTL_END


    // Allocating simple primite arrays is done in various places as internal storage.
    // Because this is unlikely to result in any bad recursions, we will override the type limit
    // here rather forever chase down all the callers.
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    _ASSERTE(CorTypeInfo::IsPrimitiveType(type));

    // Fetch the proper array type
    if (g_pPredefinedArrayTypes[type] == NULL)
    {
        TypeHandle elemType = TypeHandle(MscorlibBinder::GetElementType(type));
        TypeHandle typHnd = ClassLoader::LoadArrayTypeThrowing(elemType, ELEMENT_TYPE_SZARRAY, 0);
        g_pPredefinedArrayTypes[type] = typHnd.AsArray();
    }
    return FastAllocatePrimitiveArray(g_pPredefinedArrayTypes[type]->GetMethodTable(), cElements, bAllocateInLargeHeap);
}

/*
 * Allocates a single dimensional array of primitive types.
 */

OBJECTREF   FastAllocatePrimitiveArray(MethodTable* pMT, DWORD cElements, BOOL bAllocateInLargeHeap)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
        PRECONDITION(pMT->CheckInstanceActivated());
    } CONTRACTL_END;

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP))
    {
        char *a = new char;
        delete a;
    }
#endif

    _ASSERTE(pMT && pMT->IsArray());
    _ASSERTE(pMT->IsRestored_NoLogging());
    _ASSERTE(CorTypeInfo::IsPrimitiveType(pMT->GetArrayElementType()) &&
             g_pPredefinedArrayTypes[pMT->GetArrayElementType()] != NULL);
    
    g_IBCLogger.LogMethodTableAccess(pMT);
    SetTypeHandleOnThreadForAlloc(TypeHandle(pMT));

    SIZE_T componentSize = pMT->GetComponentSize();
    if (cElements > MaxArrayLength(componentSize))
        ThrowOutOfMemory();

    S_SIZE_T safeTotalSize = S_SIZE_T(cElements) * S_SIZE_T(componentSize) + S_SIZE_T(pMT->GetBaseSize());
    if (safeTotalSize.IsOverflow())
        ThrowOutOfMemory();

    size_t totalSize = safeTotalSize.Value();

    BOOL bPublish = bAllocateInLargeHeap;

    ArrayBase* orObject;
    if (bAllocateInLargeHeap)
    {
        orObject = (ArrayBase*) AllocLHeap(totalSize, FALSE, FALSE);
    }
    else 
    {
        ArrayTypeDesc *pArrayR8TypeDesc = g_pPredefinedArrayTypes[ELEMENT_TYPE_R8];
        if (DATA_ALIGNMENT < sizeof(double) && pArrayR8TypeDesc != NULL && pMT == pArrayR8TypeDesc->GetMethodTable() && totalSize < LARGE_OBJECT_SIZE - MIN_OBJECT_SIZE) 
        {
            // Creation of an array of doubles, not in the large object heap.
            // We want to align the doubles to 8 byte boundaries, but the GC gives us pointers aligned
            // to 4 bytes only (on 32 bit platforms). To align, we ask for 12 bytes more to fill with a
            // dummy object.
            // If the GC gives us a 8 byte aligned address, we use it for the array and place the dummy
            // object after the array, otherwise we put the dummy object first, shifting the base of
            // the array to an 8 byte aligned address.
            // Note: on 64 bit platforms, the GC always returns 8 byte aligned addresses, and we don't
            // execute this code because DATA_ALIGNMENT < sizeof(double) is false.

            _ASSERTE(DATA_ALIGNMENT == sizeof(double)/2);
            _ASSERTE((MIN_OBJECT_SIZE % sizeof(double)) == DATA_ALIGNMENT);   // used to change alignment
            _ASSERTE(pMT->GetComponentSize() == sizeof(double));
            _ASSERTE(g_pObjectClass->GetBaseSize() == MIN_OBJECT_SIZE);
            _ASSERTE(totalSize < totalSize + MIN_OBJECT_SIZE);
            orObject = (ArrayBase*) Alloc(totalSize + MIN_OBJECT_SIZE, FALSE, FALSE);

            Object *orDummyObject;
            if((size_t)orObject % sizeof(double))
            {
                orDummyObject = orObject;
                orObject = (ArrayBase*) ((size_t)orObject + MIN_OBJECT_SIZE);
            }
            else
            {
                orDummyObject = (Object*) ((size_t)orObject + totalSize);
            }
            _ASSERTE(((size_t)orObject % sizeof(double)) == 0);
            orDummyObject->SetMethodTable(g_pObjectClass);
        }
        else
        {
            orObject = (ArrayBase*) Alloc(totalSize, FALSE, FALSE);
            bPublish = (totalSize >= LARGE_OBJECT_SIZE);
        }
    }

    // Initialize Object
    orObject->SetMethodTable( pMT );
    _ASSERTE(orObject->GetMethodTable() != NULL);
    orObject->m_NumComponents = cElements;

    if (bPublish)
    {
        GCHeapUtilities::GetGCHeap()->PublishObject((BYTE*)orObject);
    }

    // Notify the profiler of the allocation
    if (TrackAllocations())
    {
        OBJECTREF objref = ObjectToOBJECTREF((Object*)orObject);
        GCPROTECT_BEGIN(objref);
        ProfilerObjectAllocatedCallback(objref, (ClassID) orObject->GetTypeHandle().AsPtr());
        GCPROTECT_END();
        
        orObject = (ArrayBase *) OBJECTREFToObject(objref); 
    }

#ifdef FEATURE_EVENT_TRACE
    // Send ETW event for allocation
    if(ETW::TypeSystemLog::IsHeapAllocEventEnabled())
    {
        ETW::TypeSystemLog::SendObjectAllocatedEvent(orObject);
    }
#endif // FEATURE_EVENT_TRACE

    // IBC Log MethodTable access
    g_IBCLogger.LogMethodTableAccess(pMT);

    LogAlloc(totalSize, pMT, orObject);

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        orObject->SetAppDomain();
#endif
    
    return( ObjectToOBJECTREF((Object*)orObject) );
}

//
// Allocate an array which is the same size as pRef.  However, do not zero out the array.
//
OBJECTREF   DupArrayForCloning(BASEARRAYREF pRef, BOOL bAllocateInLargeHeap)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

    ArrayTypeDesc arrayType(pRef->GetMethodTable(), pRef->GetArrayElementTypeHandle());
    unsigned rank = arrayType.GetRank();

    DWORD numArgs =  rank*2;
    INT32* args = (INT32*) _alloca(sizeof(INT32)*numArgs);

    if (arrayType.GetInternalCorElementType() == ELEMENT_TYPE_ARRAY)
    {
        const INT32* bounds = pRef->GetBoundsPtr();
        const INT32* lowerBounds = pRef->GetLowerBoundsPtr();
        for(unsigned int i=0; i < rank; i++) 
        {
            args[2*i]   = lowerBounds[i];
            args[2*i+1] = bounds[i];
        }
    }
    else
    {
        numArgs = 1;
        args[0] = pRef->GetNumComponents();
    }
    return AllocateArrayEx(TypeHandle(&arrayType), args, numArgs, bAllocateInLargeHeap DEBUG_ARG(FALSE));
}

#if defined(_TARGET_X86_)

// The fast version always allocates in the normal heap
OBJECTREF AllocatePrimitiveArray(CorElementType type, DWORD cElements)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

#ifdef _DEBUG
    // fastPrimitiveArrayAllocator is called by VM and managed code.  If called from managed code, we
    // make sure that the thread is in SOTolerantState.
#ifdef FEATURE_STACK_PROBE
    Thread::DisableSOCheckInHCALL disableSOCheckInHCALL;
#endif  // FEATURE_STACK_PROBE
#endif  // _DEBUG
    return OBJECTREF( HCCALL2(fastPrimitiveArrayAllocator, type, cElements) );
}

// The fast version always allocates in the normal heap
OBJECTREF AllocateObjectArray(DWORD cElements, TypeHandle ElementType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;


    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    // We must call this here to ensure the typehandle for this object is
    // interned before the object is allocated. As soon as the object is allocated,
    // the profiler could do a heapwalk and it expects to find an interned
    // typehandle for every object in the heap.
    TypeHandle ArrayType = ClassLoader::LoadArrayTypeThrowing(ElementType);

#ifdef _DEBUG
    // fastObjectArrayAllocator is called by VM and managed code.  If called from managed code, we
    // make sure that the thread is in SOTolerantState.
#ifdef FEATURE_STACK_PROBE
    Thread::DisableSOCheckInHCALL disableSOCheckInHCALL;
#endif  // FEATURE_STACK_PROBE
#endif  // _DEBUG
    return OBJECTREF( HCCALL2(fastObjectArrayAllocator, ArrayType.AsPtr(), cElements));
}

STRINGREF AllocateString( DWORD cchStringLength )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

#ifdef _DEBUG
    // fastStringAllocator is called by VM and managed code.  If called from managed code, we
    // make sure that the thread is in SOTolerantState.
#ifdef FEATURE_STACK_PROBE
    Thread::DisableSOCheckInHCALL disableSOCheckInHCALL;
#endif  // FEATURE_STACK_PROBE
#endif  // _DEBUG
    return STRINGREF(HCCALL1(fastStringAllocator, cchStringLength));
}

#endif

//
// Helper for parts of the EE which are allocating arrays
//
OBJECTREF   AllocateObjectArray(DWORD cElements, TypeHandle elementType, BOOL bAllocateInLargeHeap)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    // The object array class is loaded at startup.
    _ASSERTE(g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT] != NULL);

#ifdef _DEBUG
    ArrayTypeDesc arrayType(g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT]->GetMethodTable(), elementType);
    _ASSERTE(arrayType.GetRank() == 1);
    _ASSERTE(arrayType.GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);
#endif //_DEBUG

    return AllocateArrayEx(ClassLoader::LoadArrayTypeThrowing(elementType),
                           (INT32 *)(&cElements),
                           1,
                           bAllocateInLargeHeap
                           DEBUG_ARG(FALSE));
}


STRINGREF SlowAllocateString( DWORD cchStringLength )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
    } CONTRACTL_END;

    StringObject    *orObject  = NULL;

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP))
    {
        char *a = new char;
        delete a;
    }
#endif

    // Limit the maximum string size to <2GB to mitigate risk of security issues caused by 32-bit integer
    // overflows in buffer size calculations.
    if (cchStringLength > 0x3FFFFFDF)
        ThrowOutOfMemory();

    SIZE_T ObjectSize = PtrAlign(StringObject::GetSize(cchStringLength));
    _ASSERTE(ObjectSize > cchStringLength);

    SetTypeHandleOnThreadForAlloc(TypeHandle(g_pStringClass));

    orObject = (StringObject *)Alloc( ObjectSize, FALSE, FALSE );

    // Object is zero-init already
    _ASSERTE( orObject->HasEmptySyncBlockInfo() );

    // Initialize Object
    //<TODO>@TODO need to build a LARGE g_pStringMethodTable before</TODO>
    orObject->SetMethodTable( g_pStringClass );
    orObject->SetStringLength( cchStringLength );

    if (ObjectSize >= LARGE_OBJECT_SIZE)
    {
        GCHeapUtilities::GetGCHeap()->PublishObject((BYTE*)orObject);
    }

    // Notify the profiler of the allocation
    if (TrackAllocations())
    {
        OBJECTREF objref = ObjectToOBJECTREF((Object*)orObject);
        GCPROTECT_BEGIN(objref);
        ProfilerObjectAllocatedCallback(objref, (ClassID) orObject->GetTypeHandle().AsPtr());
        GCPROTECT_END();
        
        orObject = (StringObject *) OBJECTREFToObject(objref); 
    }

#ifdef FEATURE_EVENT_TRACE
    // Send ETW event for allocation
    if(ETW::TypeSystemLog::IsHeapAllocEventEnabled())
    {
        ETW::TypeSystemLog::SendObjectAllocatedEvent(orObject);
    }
#endif // FEATURE_EVENT_TRACE

    LogAlloc(ObjectSize, g_pStringClass, orObject);

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        orObject->SetAppDomain(); 
#endif

    return( ObjectToSTRINGREF(orObject) );
}

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
// OBJECTREF AllocateComClassObject(ComClassFactory* pComClsFac)
void AllocateComClassObject(ComClassFactory* pComClsFac, OBJECTREF* ppRefClass)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref (out param) without pinning it => cooperative
        PRECONDITION(CheckPointer(pComClsFac));
        PRECONDITION(CheckPointer(ppRefClass));
    } CONTRACTL_END;

    // Create a COM+ Class object.
    MethodTable *pMT = g_pRuntimeTypeClass;
    _ASSERTE(pMT != NULL);
    *ppRefClass= AllocateObject(pMT);
    
    if (*ppRefClass != NULL)
    {
        SyncBlock* pSyncBlock = (*((REFLECTCLASSBASEREF*) ppRefClass))->GetSyncBlock();

        // <TODO> This needs to support a COM version of ReflectClass.  Right now we 
        //  still work as we used to <darylo> </TODO>
        MethodTable* pComMT = g_pBaseCOMObject;
        _ASSERTE(pComMT != NULL);

        // class for ComObject
        (*((REFLECTCLASSBASEREF*) ppRefClass))->SetType(TypeHandle(pComMT));

        pSyncBlock->GetInteropInfo()->SetComClassFactory(pComClsFac);
    }   
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

// AllocateObject will throw OutOfMemoryException so don't need to check
// for NULL return value from it.
OBJECTREF AllocateObject(MethodTable *pMT
#ifdef FEATURE_COMINTEROP
                         , bool fHandleCom
#endif
    )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // returns an objref without pinning it => cooperative
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->CheckInstanceActivated());
    } CONTRACTL_END;

    Object     *orObject = NULL;
    // use unchecked oref here to avoid triggering assert in Validate that the AD is
    // not set becuase it isn't until near the end of the fcn at which point we can allow
    // the check.
    _UNCHECKED_OBJECTREF oref;

    g_IBCLogger.LogMethodTableAccess(pMT);
    SetTypeHandleOnThreadForAlloc(TypeHandle(pMT));


#ifdef FEATURE_COMINTEROP
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    if (fHandleCom && pMT->IsComObjectType() && !pMT->IsWinRTObjectType())
    {
        // Create a instance of __ComObject here is not allowed as we don't know what COM object to create
        if (pMT == g_pBaseCOMObject)
            COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);

        oref = OBJECTREF_TO_UNCHECKED_OBJECTREF(AllocateComObject_ForManaged(pMT));
    }
    else
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP
    {   
        DWORD baseSize = pMT->GetBaseSize();

#ifdef FEATURE_64BIT_ALIGNMENT
        if (pMT->RequiresAlign8())
        {
            // The last argument to the allocation, indicates whether the alignment should be "biased". This
            // means that the object is allocated so that its header lies exactly between two 8-byte
            // boundaries. This is required in cases where we need to mis-align the header in order to align
            // the actual payload. Currently this is false for classes (where we apply padding to ensure the
            // first field is aligned relative to the header) and true for boxed value types (where we can't
            // do the same padding without introducing more complexity in type layout and unboxing stubs).
            _ASSERTE(sizeof(Object) == 4);
            orObject = (Object *) AllocAlign8(baseSize,
                                              pMT->HasFinalizer(),
                                              pMT->ContainsPointers(),
                                              pMT->IsValueType());
        }
        else
#endif // FEATURE_64BIT_ALIGNMENT
        {
            orObject = (Object *) Alloc(baseSize,
                                        pMT->HasFinalizer(),
                                        pMT->ContainsPointers());
        }

        // verify zero'd memory (at least for sync block)
        _ASSERTE( orObject->HasEmptySyncBlockInfo() );


        if ((baseSize >= LARGE_OBJECT_SIZE))
        {
            orObject->SetMethodTableForLargeObject(pMT);
            GCHeapUtilities::GetGCHeap()->PublishObject((BYTE*)orObject);
        }
        else
        {
            orObject->SetMethodTable(pMT);
        }

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->AppDomainLeaks())
            orObject->SetAppDomain(); 
        else
#endif
        if (pMT->HasFinalizer())
            orObject->SetAppDomain(); 

        // Notify the profiler of the allocation
        if (TrackAllocations())
        {
            OBJECTREF objref = ObjectToOBJECTREF((Object*)orObject);
            GCPROTECT_BEGIN(objref);
            ProfilerObjectAllocatedCallback(objref, (ClassID) orObject->GetTypeHandle().AsPtr());
            GCPROTECT_END();

            orObject = (Object *) OBJECTREFToObject(objref); 
        }

#ifdef FEATURE_EVENT_TRACE
        // Send ETW event for allocation
        if(ETW::TypeSystemLog::IsHeapAllocEventEnabled())
        {
            ETW::TypeSystemLog::SendObjectAllocatedEvent(orObject);
        }
#endif // FEATURE_EVENT_TRACE

        LogAlloc(pMT->GetBaseSize(), pMT, orObject);

        oref = OBJECTREF_TO_UNCHECKED_OBJECTREF(orObject);
    }

    return UNCHECKED_OBJECTREF_TO_OBJECTREF(oref);
}

//========================================================================
//
//      WRITE BARRIER HELPERS
//
//========================================================================


#define card_byte(addr) (((size_t)(addr)) >> card_byte_shift)
#define card_bit(addr)  (1 << ((((size_t)(addr)) >> (card_byte_shift - 3)) & 7))

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#define card_bundle_byte(addr) (((size_t)(addr)) >> card_bundle_byte_shift)

static void SetCardBundleByte(BYTE* addr)
{
    BYTE* cbByte = (BYTE *)VolatileLoadWithoutBarrier(&g_card_bundle_table) + card_bundle_byte(addr);
    if (*cbByte != 0xFF)
    {
        *cbByte = 0xFF;
    }
}
#endif

#ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS

// implemented in assembly
// extern "C" HCIMPL2_RAW(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *refUNSAFE)
// extern "C" HCIMPL2_RAW(VOID, JIT_WriteBarrier, Object **dst, Object *refUNSAFE)

#else // FEATURE_USE_ASM_GC_WRITE_BARRIERS

// NOTE: non-ASM write barriers only work with Workstation GC.

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
static UINT64 CheckedBarrierCount = 0;
static UINT64 CheckedBarrierRetBufCount = 0;
static UINT64 CheckedBarrierByrefArgCount = 0;
static UINT64 CheckedBarrierByrefOtherLocalCount = 0;
static UINT64 CheckedBarrierAddrOfLocalCount = 0;
static UINT64 UncheckedBarrierCount = 0;
static UINT64 CheckedAfterHeapFilter = 0;
static UINT64 CheckedAfterRefInEphemFilter = 0;
static UINT64 CheckedAfterAlreadyDirtyFilter = 0;
static UINT64 CheckedDestInEphem = 0;
static UINT64 UncheckedAfterRefInEphemFilter = 0;
static UINT64 UncheckedAfterAlreadyDirtyFilter = 0;
static UINT64 UncheckedDestInEphem = 0;

const unsigned BarrierCountPrintInterval = 1000000;
static unsigned CheckedBarrierInterval = BarrierCountPrintInterval;
static unsigned UncheckedBarrierInterval = BarrierCountPrintInterval;


void IncCheckedBarrierCount()
{
	++CheckedBarrierCount;
	if (--CheckedBarrierInterval == 0)
	{
		CheckedBarrierInterval = BarrierCountPrintInterval;
		printf("GC write barrier counts: checked = %lld, unchecked = %lld, total = %lld.\n",
			CheckedBarrierCount, UncheckedBarrierCount, (CheckedBarrierCount + UncheckedBarrierCount));
		printf("    [Checked: %lld after heap check, %lld after ephem check, %lld after already dirty check.]\n",
			CheckedAfterHeapFilter, CheckedAfterRefInEphemFilter, CheckedAfterAlreadyDirtyFilter);
		printf("    [Unchecked: %lld after ephem check, %lld after already dirty check.]\n",
			UncheckedAfterRefInEphemFilter, UncheckedAfterAlreadyDirtyFilter);
		printf("    [Dest in ephem: checked = %lld, unchecked = %lld.]\n", 
			CheckedDestInEphem, UncheckedDestInEphem);
        printf("    [Checked: %lld are stores to fields of ret buff, %lld via byref args,\n",
            CheckedBarrierRetBufCount, CheckedBarrierByrefArgCount);
        printf("     %lld via other locals, %lld via addr of local.]\n",
            CheckedBarrierByrefOtherLocalCount, CheckedBarrierAddrOfLocalCount);
	}
}

void IncUncheckedBarrierCount()
{
	++UncheckedBarrierCount;
	if (--UncheckedBarrierInterval == 0)
	{
		printf("GC write barrier counts: checked = %lld, unchecked = %lld, total = %lld.\n",
			CheckedBarrierCount, UncheckedBarrierCount, (CheckedBarrierCount + UncheckedBarrierCount));
		UncheckedBarrierInterval = BarrierCountPrintInterval;
	}
}
#endif // FEATURE_COUNT_GC_WRITE_BARRIERS

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
// (We ignore the advice below on using a _RAW macro for this performance diagnostic mode, which need not function properly in
// all situations...)
extern "C" HCIMPL3(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref, CheckedWriteBarrierKinds kind)
#else

// This function is a JIT helper, but it must NOT use HCIMPL2 because it
// modifies Thread state that will not be restored if an exception occurs
// inside of memset.  A normal EH unwind will not occur.
extern "C" HCIMPL2_RAW(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref)
#endif
{
    // Must use static contract here, because if an AV occurs, a normal EH
    // unwind will not occur, and destructors will not run.
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    IncCheckedBarrierCount();
    switch (kind)
    {
    case CWBKind_RetBuf:
        CheckedBarrierRetBufCount++;
        break;
    case CWBKind_ByRefArg:
        CheckedBarrierByrefArgCount++;
        break;
    case CWBKind_OtherByRefLocal:
        CheckedBarrierByrefOtherLocalCount++;
        break;
    case CWBKind_AddrOfLocal:
        CheckedBarrierAddrOfLocalCount++;
        break;
    case CWBKind_Unclassified:
        break;
    default:
        // It should be some member of the enumeration.
        _ASSERTE_ALL_BUILDS(__FILE__, false);
        break;
    }
#endif // FEATURE_COUNT_GC_WRITE_BARRIERS
    
    // no HELPER_METHOD_FRAME because we are MODE_COOPERATIVE, GC_NOTRIGGER
    
    *dst = ref;

    // if the dst is outside of the heap (unboxed value classes) then we
    //      simply exit
    if (((BYTE*)dst < g_lowest_address) || ((BYTE*)dst >= g_highest_address))
        return;
    
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    CheckedAfterHeapFilter++;
#endif

#ifdef WRITE_BARRIER_CHECK
    updateGCShadow(dst, ref);     // support debugging write barrier
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (GCHeapUtilities::SoftwareWriteWatchIsEnabled())
    {
        GCHeapUtilities::SoftwareWriteWatchSetDirty(dst, sizeof(*dst));
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    if((BYTE*) dst >= g_ephemeral_low && (BYTE*) dst < g_ephemeral_high)
    {
        CheckedDestInEphem++;
    }
#endif
    if((BYTE*) ref >= g_ephemeral_low && (BYTE*) ref < g_ephemeral_high)
    {
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
        CheckedAfterRefInEphemFilter++;
#endif
        // VolatileLoadWithoutBarrier() is used here to prevent fetch of g_card_table from being reordered 
        // with g_lowest/highest_address check above. See comment in code:gc_heap::grow_brick_card_tables.
        BYTE* pCardByte = (BYTE *)VolatileLoadWithoutBarrier(&g_card_table) + card_byte((BYTE *)dst);
        if(*pCardByte != 0xFF)
        {
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
            CheckedAfterAlreadyDirtyFilter++;
#endif
            *pCardByte = 0xFF;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            SetCardBundleByte((BYTE*)dst);
#endif
        }
    }
}
HCIMPLEND_RAW

// This function is a JIT helper, but it must NOT use HCIMPL2 because it
// modifies Thread state that will not be restored if an exception occurs
// inside of memset.  A normal EH unwind will not occur.
extern "C" HCIMPL2_RAW(VOID, JIT_WriteBarrier, Object **dst, Object *ref)
{
    // Must use static contract here, because if an AV occurs, a normal EH
    // unwind will not occur, and destructors will not run.
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    IncUncheckedBarrierCount();
#endif
    // no HELPER_METHOD_FRAME because we are MODE_COOPERATIVE, GC_NOTRIGGER
    
    *dst = ref;

    // If the store above succeeded, "dst" should be in the heap.
   assert(GCHeapUtilities::GetGCHeap()->IsHeapPointer((void*)dst));

#ifdef WRITE_BARRIER_CHECK
    updateGCShadow(dst, ref);     // support debugging write barrier
#endif
    
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (GCHeapUtilities::SoftwareWriteWatchIsEnabled())
    {
        GCHeapUtilities::SoftwareWriteWatchSetDirty(dst, sizeof(*dst));
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    if((BYTE*) dst >= g_ephemeral_low && (BYTE*) dst < g_ephemeral_high)
    {
        UncheckedDestInEphem++;
    }
#endif
    if((BYTE*) ref >= g_ephemeral_low && (BYTE*) ref < g_ephemeral_high)
    {
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
        UncheckedAfterRefInEphemFilter++;
#endif
        BYTE* pCardByte = (BYTE *)VolatileLoadWithoutBarrier(&g_card_table) + card_byte((BYTE *)dst);
        if(*pCardByte != 0xFF)
        {
#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
            UncheckedAfterAlreadyDirtyFilter++;
#endif
            *pCardByte = 0xFF;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            SetCardBundleByte((BYTE*)dst);
#endif

        }
    }
}
HCIMPLEND_RAW

#endif // FEATURE_USE_ASM_GC_WRITE_BARRIERS

extern "C" HCIMPL2_RAW(VOID, JIT_WriteBarrierEnsureNonHeapTarget, Object **dst, Object *ref)
{
    // Must use static contract here, because if an AV occurs, a normal EH
    // unwind will not occur, and destructors will not run.
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    assert(!GCHeapUtilities::GetGCHeap()->IsHeapPointer((void*)dst));

    // no HELPER_METHOD_FRAME because we are MODE_COOPERATIVE, GC_NOTRIGGER
    
    *dst = ref;
}
HCIMPLEND_RAW

// This function sets the card table with the granularity of 1 byte, to avoid ghost updates
//    that could occur if multiple threads were trying to set different bits in the same card.

#include <optsmallperfcritical.h>
void ErectWriteBarrier(OBJECTREF *dst, OBJECTREF ref)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

    // if the dst is outside of the heap (unboxed value classes) then we
    //      simply exit
    if (((BYTE*)dst < g_lowest_address) || ((BYTE*)dst >= g_highest_address))
        return;
    
#ifdef WRITE_BARRIER_CHECK
    updateGCShadow((Object**) dst, OBJECTREFToObject(ref));     // support debugging write barrier
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (GCHeapUtilities::SoftwareWriteWatchIsEnabled())
    {
        GCHeapUtilities::SoftwareWriteWatchSetDirty(dst, sizeof(*dst));
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    if ((BYTE*) OBJECTREFToObject(ref) >= g_ephemeral_low && (BYTE*) OBJECTREFToObject(ref) < g_ephemeral_high)
    {
        // VolatileLoadWithoutBarrier() is used here to prevent fetch of g_card_table from being reordered 
        // with g_lowest/highest_address check above. See comment in code:gc_heap::grow_brick_card_tables.
        BYTE* pCardByte = (BYTE *)VolatileLoadWithoutBarrier(&g_card_table) + card_byte((BYTE *)dst);
        if (*pCardByte != 0xFF)
        {
            *pCardByte = 0xFF;
            
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            SetCardBundleByte((BYTE*)dst);
#endif

        }
    }
}
#include <optdefault.h>

void ErectWriteBarrierForMT(MethodTable **dst, MethodTable *ref)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

    *dst = ref;

#ifdef WRITE_BARRIER_CHECK
    updateGCShadow((Object **)dst, (Object *)ref);     // support debugging write barrier, updateGCShadow only cares that these are pointers
#endif
    
    if (ref->Collectible())
    {
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        if (GCHeapUtilities::SoftwareWriteWatchIsEnabled())
        {
            GCHeapUtilities::SoftwareWriteWatchSetDirty(dst, sizeof(*dst));
        }

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        BYTE *refObject = *(BYTE **)((MethodTable*)ref)->GetLoaderAllocatorObjectHandle();
        if((BYTE*) refObject >= g_ephemeral_low && (BYTE*) refObject < g_ephemeral_high)
        {
            // See comment above
            BYTE* pCardByte = (BYTE *)VolatileLoadWithoutBarrier(&g_card_table) + card_byte((BYTE *)dst);
            if( !((*pCardByte) & card_bit((BYTE *)dst)) )
            {
                *pCardByte = 0xFF;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                SetCardBundleByte((BYTE*)dst);
#endif

            }
        }
    }
}

//----------------------------------------------------------------------------
//
// Write Barrier Support for bulk copy ("Clone") operations
//
// StartPoint is the target bulk copy start point
// len is the length of the bulk copy (in bytes)
//
//
// Performance Note:
//
// This is implemented somewhat "conservatively", that is we
// assume that all the contents of the bulk copy are object
// references.  If they are not, and the value lies in the
// ephemeral range, we will set false positives in the card table.
//
// We could use the pointer maps and do this more accurately if necessary

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("y", on)        // Small critical routines, don't put in EBP frame
#endif //_MSC_VER && _TARGET_X86_

void
SetCardsAfterBulkCopy(Object **start, size_t len)
{
    // If the size is smaller than a pointer, no write barrier is required.
    if (len >= sizeof(uintptr_t))
    {
        InlinedSetCardsAfterBulkCopyHelper(start, len);
    }
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("", on)        // Go back to command line default optimizations
#endif //_MSC_VER && _TARGET_X86_
