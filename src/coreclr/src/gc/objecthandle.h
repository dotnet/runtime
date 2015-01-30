//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * Wraps handle table to implement various handle types (Strong, Weak, etc.)
 *

 *
 */

#ifndef _OBJECTHANDLE_H
#define _OBJECTHANDLE_H

/*
 * include handle manager declarations
 */
#include "handletable.h"

#ifdef FEATURE_COMINTEROP
#include <weakreference.h>
#endif // FEATURE_COMINTEROP

/*
 * Convenience macros for accessing handles.  StoreFirstObjectInHandle is like
 * StoreObjectInHandle, except it only succeeds if transitioning from NULL to
 * non-NULL.  In other words, if this handle is being initialized for the first
 * time.
 */
#define ObjectFromHandle(handle)                   HndFetchHandle(handle)
#define StoreObjectInHandle(handle, object)        HndAssignHandle(handle, object)
#define InterlockedCompareExchangeObjectInHandle(handle, object, oldObj)        HndInterlockedCompareExchangeHandle(handle, object, oldObj)
#define StoreFirstObjectInHandle(handle, object)   HndFirstAssignHandle(handle, object)
#define ObjectHandleIsNull(handle)                 HndIsNull(handle)
#define IsHandleNullUnchecked(handle)              HndCheckForNullUnchecked(handle)


/*
 * HANDLES
 *
 * The default type of handle is a strong handle.
 *
 */
#define HNDTYPE_DEFAULT                         HNDTYPE_STRONG


/*
 * WEAK HANDLES
 *
 * Weak handles are handles that track an object as long as it is alive,
 * but do not keep the object alive if there are no strong references to it.
 *
 * The default type of weak handle is 'long-lived' weak handle.
 *
 */
#define HNDTYPE_WEAK_DEFAULT                    HNDTYPE_WEAK_LONG


/*
 * SHORT-LIVED WEAK HANDLES
 *
 * Short-lived weak handles are weak handles that track an object until the
 * first time it is detected to be unreachable.  At this point, the handle is
 * severed, even if the object will be visible from a pending finalization
 * graph.  This further implies that short weak handles do not track
 * across object resurrections.
 *
 */
#define HNDTYPE_WEAK_SHORT                      (0)


/*
 * LONG-LIVED WEAK HANDLES
 *
 * Long-lived weak handles are weak handles that track an object until the
 * object is actually reclaimed.  Unlike short weak handles, long weak handles
 * continue to track their referents through finalization and across any
 * resurrections that may occur.
 *
 */
#define HNDTYPE_WEAK_LONG                       (1)


/*
 * STRONG HANDLES
 *
 * Strong handles are handles which function like a normal object reference.
 * The existence of a strong handle for an object will cause the object to
 * be promoted (remain alive) through a garbage collection cycle.
 *
 */
#define HNDTYPE_STRONG                          (2)


/*
 * PINNED HANDLES
 *
 * Pinned handles are strong handles which have the added property that they
 * prevent an object from moving during a garbage collection cycle.  This is
 * useful when passing a pointer to object innards out of the runtime while GC
 * may be enabled.
 *
 * NOTE:  PINNING AN OBJECT IS EXPENSIVE AS IT PREVENTS THE GC FROM ACHIEVING
 *        OPTIMAL PACKING OF OBJECTS DURING EPHEMERAL COLLECTIONS.  THIS TYPE
 *        OF HANDLE SHOULD BE USED SPARINGLY!
 */
#define HNDTYPE_PINNED                          (3)


/*
 * VARIABLE HANDLES
 *
 * Variable handles are handles whose type can be changed dynamically.  They
 * are larger than other types of handles, and are scanned a little more often,
 * but are useful when the handle owner needs an efficient way to change the
 * strength of a handle on the fly.
 * 
 */
#define HNDTYPE_VARIABLE                        (4)

#ifdef FEATURE_COMINTEROP
/*
 * REFCOUNTED HANDLES
 *
 * Refcounted handles are handles that behave as strong handles while the
 * refcount on them is greater than 0 and behave as weak handles otherwise.
 *
 * N.B. These are currently NOT general purpose.
 *      The implementation is tied to COM Interop.
 *
 */
#define HNDTYPE_REFCOUNTED                      (5)
#endif // FEATURE_COMINTEROP


/*
 * DEPENDENT HANDLES
 *
 * Dependent handles are two handles that need to have the same lifetime.  One handle refers to a secondary object 
 * that needs to have the same lifetime as the primary object. The secondary object should not cause the primary 
 * object to be referenced, but as long as the primary object is alive, so must be the secondary
 *
 * They are currently used for EnC for adding new field members to existing instantiations under EnC modes where
 * the primary object is the original instantiation and the secondary represents the added field.
 *
 * They are also used to implement the ConditionalWeakTable class in mscorlib.dll. If you want to use
 * these from managed code, they are exposed to BCL through the managed DependentHandle class.
 *
 *
 */
#define HNDTYPE_DEPENDENT		                     (6)

/*
 * PINNED HANDLES for asynchronous operation
 *
 * Pinned handles are strong handles which have the added property that they
 * prevent an object from moving during a garbage collection cycle.  This is
 * useful when passing a pointer to object innards out of the runtime while GC
 * may be enabled.
 *
 * NOTE:  PINNING AN OBJECT IS EXPENSIVE AS IT PREVENTS THE GC FROM ACHIEVING
 *        OPTIMAL PACKING OF OBJECTS DURING EPHEMERAL COLLECTIONS.  THIS TYPE
 *        OF HANDLE SHOULD BE USED SPARINGLY!
 */
#define HNDTYPE_ASYNCPINNED                          (7)


/*
 * SIZEDREF HANDLES
 *
 * SizedRef handles are strong handles. Each handle has a piece of user data associated
 * with it that stores the size of the object this handle refers to. These handles
 * are scanned as strong roots during each GC but only during full GCs would the size
 * be calculated.
 *
 */
#define HNDTYPE_SIZEDREF                             (8)

#ifdef FEATURE_COMINTEROP

/*
 * WINRT WEAK HANDLES
 *
 * WinRT weak reference handles hold two different types of weak handles to any
 * RCW with an underlying COM object that implements IWeakReferenceSource.  The
 * object reference itself is a short weak handle to the RCW.  In addition an
 * IWeakReference* to the underlying COM object is stored, allowing the handle
 * to create a new RCW if the existing RCW is collected.  This ensures that any
 * code holding onto a WinRT weak reference can always access an RCW to the
 * underlying COM object as long as it has not been released by all of its strong
 * references.
 */
#define HNDTYPE_WEAK_WINRT                           (9)

#endif // FEATURE_COMINTEROP

typedef DPTR(struct HandleTableMap) PTR_HandleTableMap;
typedef DPTR(struct HandleTableBucket) PTR_HandleTableBucket;
typedef DPTR(PTR_HandleTableBucket) PTR_PTR_HandleTableBucket;

struct HandleTableMap
{
    PTR_PTR_HandleTableBucket   pBuckets;
    PTR_HandleTableMap          pNext;
    DWORD                       dwMaxIndex;
};

GVAL_DECL(HandleTableMap, g_HandleTableMap);

#define INITIAL_HANDLE_TABLE_ARRAY_SIZE 10

// struct containing g_SystemInfo.dwNumberOfProcessors HHANDLETABLEs and current table index
// instead of just single HHANDLETABLE for on-fly balancing while adding handles on multiproc machines

struct HandleTableBucket
{
    PTR_HHANDLETABLE pTable;
    UINT             HandleTableIndex;

    bool Contains(OBJECTHANDLE handle);
};


/*
 * Type mask definitions for HNDTYPE_VARIABLE handles.
 */
#define VHT_WEAK_SHORT              (0x00000100)  // avoid using low byte so we don't overlap normal types
#define VHT_WEAK_LONG               (0x00000200)  // avoid using low byte so we don't overlap normal types
#define VHT_STRONG                  (0x00000400)  // avoid using low byte so we don't overlap normal types
#define VHT_PINNED                  (0x00000800)  // avoid using low byte so we don't overlap normal types

#define IS_VALID_VHT_VALUE(flag)   ((flag == VHT_WEAK_SHORT) || \
                                    (flag == VHT_WEAK_LONG)  || \
                                    (flag == VHT_STRONG)     || \
                                    (flag == VHT_PINNED))

#ifndef DACCESS_COMPILE
/*
 * Convenience macros and prototypes for the various handle types we define
 */

inline OBJECTHANDLE CreateTypedHandle(HHANDLETABLE table, OBJECTREF object, int type)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, type, object); 
}

inline void DestroyTypedHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandleOfUnknownType(HndGetHandleTable(handle), handle);
}

inline OBJECTHANDLE CreateHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_DEFAULT, object); 
}

inline void DestroyHandle(OBJECTHANDLE handle)
{ 
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_DEFAULT, handle);
}

inline OBJECTHANDLE CreateDuplicateHandle(OBJECTHANDLE handle) {
    WRAPPER_NO_CONTRACT;

    // Create a new STRONG handle in the same table as an existing handle.  
    return HndCreateHandle(HndGetHandleTable(handle), HNDTYPE_DEFAULT, ObjectFromHandle(handle));
}


inline OBJECTHANDLE CreateWeakHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_WEAK_DEFAULT, object); 
}

inline void DestroyWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_DEFAULT, handle);
}

inline OBJECTHANDLE CreateShortWeakHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_WEAK_SHORT, object); 
}

inline void DestroyShortWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_SHORT, handle);
}


inline OBJECTHANDLE CreateLongWeakHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_WEAK_LONG, object); 
}

inline void DestroyLongWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_LONG, handle);
}

#ifndef FEATURE_REDHAWK
typedef Holder<OBJECTHANDLE,DoNothing<OBJECTHANDLE>,DestroyLongWeakHandle> LongWeakHandleHolder;
#endif

inline OBJECTHANDLE CreateStrongHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_STRONG, object); 
}

inline void DestroyStrongHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_STRONG, handle);
}

inline OBJECTHANDLE CreatePinningHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_PINNED, object); 
}

inline void DestroyPinningHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_PINNED, handle);
}

#ifndef FEATURE_REDHAWK
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyPinningHandle, NULL> PinningHandleHolder;
#endif

inline OBJECTHANDLE CreateAsyncPinningHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_ASYNCPINNED, object); 
}

inline void DestroyAsyncPinningHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_ASYNCPINNED, handle);
}

#ifndef FEATURE_REDHAWK
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyAsyncPinningHandle, NULL> AsyncPinningHandleHolder;
#endif

inline OBJECTHANDLE CreateSizedRefHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_SIZEDREF, object, (LPARAM)0);
}

void DestroySizedRefHandle(OBJECTHANDLE handle);

#ifndef FEATURE_REDHAWK
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroySizedRefHandle, NULL> SizeRefHandleHolder;
#endif

#ifdef FEATURE_COMINTEROP
inline OBJECTHANDLE CreateRefcountedHandle(HHANDLETABLE table, OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(table, HNDTYPE_REFCOUNTED, object); 
}

inline void DestroyRefcountedHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_REFCOUNTED, handle);
}

inline OBJECTHANDLE CreateWinRTWeakHandle(HHANDLETABLE table, OBJECTREF object, IWeakReference* pWinRTWeakReference)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pWinRTWeakReference != nullptr);
    return HndCreateHandle(table, HNDTYPE_WEAK_WINRT, object, reinterpret_cast<LPARAM>(pWinRTWeakReference));
}

void DestroyWinRTWeakHandle(OBJECTHANDLE handle);

#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE

OBJECTREF GetDependentHandleSecondary(OBJECTHANDLE handle);

#ifndef DACCESS_COMPILE
OBJECTHANDLE CreateDependentHandle(HHANDLETABLE table, OBJECTREF primary, OBJECTREF secondary);
void SetDependentHandleSecondary(OBJECTHANDLE handle, OBJECTREF secondary);

inline void DestroyDependentHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

	HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_DEPENDENT, handle);
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE

OBJECTHANDLE CreateVariableHandle(HHANDLETABLE hTable, OBJECTREF object, UINT type);
void         UpdateVariableHandleType(OBJECTHANDLE handle, UINT type);

inline void  DestroyVariableHandle(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_VARIABLE, handle);
}

void GCHandleValidatePinnedObject(OBJECTREF obj);

/*
 * Holder for OBJECTHANDLE
 */

#ifndef FEATURE_REDHAWK
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyHandle > OHWrapper;

class OBJECTHANDLEHolder : public OHWrapper
{
public:
    FORCEINLINE OBJECTHANDLEHolder(OBJECTHANDLE p = NULL) : OHWrapper(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    FORCEINLINE void operator=(OBJECTHANDLE p)
    {
        WRAPPER_NO_CONTRACT;

        OHWrapper::operator=(p);
    }
};
#endif

#ifdef FEATURE_COMINTEROP

typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyRefcountedHandle> RefCountedOHWrapper;

class RCOBJECTHANDLEHolder : public RefCountedOHWrapper
{
public:
    FORCEINLINE RCOBJECTHANDLEHolder(OBJECTHANDLE p = NULL) : RefCountedOHWrapper(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    FORCEINLINE void operator=(OBJECTHANDLE p)
    {
        WRAPPER_NO_CONTRACT;

        RefCountedOHWrapper::operator=(p);
    }
};

#endif // FEATURE_COMINTEROP
/*
 * Convenience prototypes for using the global handles
 */

int GetCurrentThreadHomeHeapNumber();

inline OBJECTHANDLE CreateGlobalTypedHandle(OBJECTREF object, int type)
{ 
    WRAPPER_NO_CONTRACT;
    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], type, object); 
}

inline void DestroyGlobalTypedHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandleOfUnknownType(HndGetHandleTable(handle), handle);
}

inline OBJECTHANDLE CreateGlobalHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_DEFAULT, object); 
}

inline void DestroyGlobalHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_DEFAULT, handle);
}

inline OBJECTHANDLE CreateGlobalWeakHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_WEAK_DEFAULT, object); 
}

inline void DestroyGlobalWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_DEFAULT, handle);
}

inline OBJECTHANDLE CreateGlobalShortWeakHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_WEAK_SHORT, object);     
}

inline void DestroyGlobalShortWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_SHORT, handle);
}

#ifndef FEATURE_REDHAWK
typedef Holder<OBJECTHANDLE,DoNothing<OBJECTHANDLE>,DestroyGlobalShortWeakHandle> GlobalShortWeakHandleHolder;
#endif

inline OBJECTHANDLE CreateGlobalLongWeakHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_WEAK_LONG, object); 
}

inline void DestroyGlobalLongWeakHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_WEAK_LONG, handle);
}

inline OBJECTHANDLE CreateGlobalStrongHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_STRONG, object); 
}

inline void DestroyGlobalStrongHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_STRONG, handle);
}

#ifndef FEATURE_REDHAWK
typedef Holder<OBJECTHANDLE,DoNothing<OBJECTHANDLE>,DestroyGlobalStrongHandle> GlobalStrongHandleHolder;
#endif

inline OBJECTHANDLE CreateGlobalPinningHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_PINNED, object); 
}

inline void DestroyGlobalPinningHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_PINNED, handle);
}

#ifdef FEATURE_COMINTEROP
inline OBJECTHANDLE CreateGlobalRefcountedHandle(OBJECTREF object)
{ 
    WRAPPER_NO_CONTRACT;

    return HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_REFCOUNTED, object); 
}

inline void DestroyGlobalRefcountedHandle(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    HndDestroyHandle(HndGetHandleTable(handle), HNDTYPE_REFCOUNTED, handle);
}
#endif // FEATURE_COMINTEROP

inline void ResetOBJECTHANDLE(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    StoreObjectInHandle(handle, NULL);
}

#ifndef FEATURE_REDHAWK
typedef Holder<OBJECTHANDLE,DoNothing<OBJECTHANDLE>,ResetOBJECTHANDLE> ObjectInHandleHolder;
#endif

/*
 * Table maintenance routines
 */
bool Ref_Initialize();
void Ref_Shutdown();
HandleTableBucket *Ref_CreateHandleTableBucket(ADIndex uADIndex);
BOOL Ref_HandleAsyncPinHandles();
void Ref_RelocateAsyncPinHandles(HandleTableBucket *pSource, HandleTableBucket *pTarget);
void Ref_RemoveHandleTableBucket(HandleTableBucket *pBucket);
void Ref_DestroyHandleTableBucket(HandleTableBucket *pBucket);
BOOL Ref_ContainHandle(HandleTableBucket *pBucket, OBJECTHANDLE handle);

/*
 * GC-time scanning entrypoints
 */
struct ScanContext;
struct DhContext;
struct ProfilingScanContext;
void Ref_BeginSynchronousGC   (UINT uCondemnedGeneration, UINT uMaxGeneration);
void Ref_EndSynchronousGC     (UINT uCondemnedGeneration, UINT uMaxGeneration);

typedef void Ref_promote_func(class Object**, ScanContext*, DWORD);

void Ref_TraceRefCountHandles(HANDLESCANPROC callback, LPARAM lParam1, LPARAM lParam2);
void Ref_TracePinningRoots(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_TraceNormalRoots(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_UpdatePointers(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_UpdatePinnedPointers(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
DhContext *Ref_GetDependentHandleContext(ScanContext* sc);
bool Ref_ScanDependentHandlesForPromotion(DhContext *pDhContext);
void Ref_ScanDependentHandlesForClearing(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_ScanDependentHandlesForRelocation(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_ScanSizedRefHandles(UINT condemned, UINT maxgen, ScanContext* sc, Ref_promote_func* fn);

void Ref_CheckReachable       (UINT uCondemnedGeneration, UINT uMaxGeneration, LPARAM lp1);
void Ref_CheckAlive           (UINT uCondemnedGeneration, UINT uMaxGeneration, LPARAM lp1);
void Ref_ScanPointersForProfilerAndETW(UINT uMaxGeneration, LPARAM lp1);
void Ref_ScanDependentHandlesForProfilerAndETW(UINT uMaxGeneration, ProfilingScanContext * SC);
void Ref_AgeHandles           (UINT uCondemnedGeneration, UINT uMaxGeneration, LPARAM lp1);
void Ref_RejuvenateHandles(UINT uCondemnedGeneration, UINT uMaxGeneration, LPARAM lp1);

void Ref_VerifyHandleTable(UINT condemned, UINT maxgen, ScanContext* sc);

#endif // DACCESS_COMPILE

#endif //_OBJECTHANDLE_H
