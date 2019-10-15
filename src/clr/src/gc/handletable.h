// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * Generational GC handle manager.  Entrypoint Header.
 *
 * Implements generic support for external handles into a GC heap.
 *

 *
 */

#ifndef _HANDLETABLE_H
#define _HANDLETABLE_H

#include "gcinterface.h"

/****************************************************************************
 *
 * FLAGS, CONSTANTS AND DATA TYPES
 *
 ****************************************************************************/

#ifdef _DEBUG
#define DEBUG_DestroyedHandleValue ((_UNCHECKED_OBJECTREF)0x7)
#endif

/*
 * handle flags used by HndCreateHandleTable
 */
#define HNDF_NORMAL         (0x00)
#define HNDF_EXTRAINFO      (0x01)

/*
 * handle to handle table
 */
typedef DPTR(struct HandleTable) PTR_HandleTable;
typedef DPTR(PTR_HandleTable) PTR_PTR_HandleTable;
typedef PTR_HandleTable HHANDLETABLE;
typedef PTR_PTR_HandleTable PTR_HHANDLETABLE;

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * PUBLIC ROUTINES AND MACROS
 *
 ****************************************************************************/
#ifndef DACCESS_COMPILE
/*
 * handle manager init and shutdown routines
 */
HHANDLETABLE    HndCreateHandleTable(const uint32_t *pTypeFlags, uint32_t uTypeCount);
void            HndDestroyHandleTable(HHANDLETABLE hTable);
#endif // !DACCESS_COMPILE

/*
 * retrieve index stored in table at creation 
 */
void            HndSetHandleTableIndex(HHANDLETABLE hTable, uint32_t uTableIndex);
uint32_t        HndGetHandleTableIndex(HHANDLETABLE hTable);

#ifndef DACCESS_COMPILE
/*
 * individual handle allocation and deallocation
 */
OBJECTHANDLE    HndCreateHandle(HHANDLETABLE hTable, uint32_t uType, OBJECTREF object, uintptr_t lExtraInfo = 0);
void            HndDestroyHandle(HHANDLETABLE hTable, uint32_t uType, OBJECTHANDLE handle);

void            HndDestroyHandleOfUnknownType(HHANDLETABLE hTable, OBJECTHANDLE handle);

/*
 * owner data associated with handles
 */
void            HndSetHandleExtraInfo(OBJECTHANDLE handle, uint32_t uType, uintptr_t lExtraInfo);
uintptr_t          HndCompareExchangeHandleExtraInfo(OBJECTHANDLE handle, uint32_t uType, uintptr_t lOldExtraInfo, uintptr_t lNewExtraInfo);
#endif // !DACCESS_COMPILE

GC_DAC_VISIBLE
uintptr_t          HndGetHandleExtraInfo(OBJECTHANDLE handle);

/*
 * get parent table of handle
 */
HHANDLETABLE    HndGetHandleTable(OBJECTHANDLE handle);

/*
 * write barrier
 */
void            HndWriteBarrier(OBJECTHANDLE handle, OBJECTREF value);

/* 
 * logging an ETW event (for inlined methods)
 */
void            HndLogSetEvent(OBJECTHANDLE handle, _UNCHECKED_OBJECTREF value);

/*
 * NON-GC handle enumeration
 */
GC_DAC_VISIBLE_NO_MANGLE
void HndEnumHandles(HHANDLETABLE hTable, const uint32_t *puType, uint32_t uTypeCount,
                    HANDLESCANPROC pfnEnum, uintptr_t lParam1, uintptr_t lParam2, bool fAsync);

/*
 * GC-time handle scanning
 */
#define HNDGCF_NORMAL       (0x00000000)    // normal scan
#define HNDGCF_AGE          (0x00000001)    // age handles while scanning
#define HNDGCF_ASYNC        (0x00000002)    // drop the table lock while scanning
#define HNDGCF_EXTRAINFO    (0x00000004)    // iterate per-handle data while scanning

GC_DAC_VISIBLE_NO_MANGLE
void            HndScanHandlesForGC(HHANDLETABLE hTable,
                                    HANDLESCANPROC scanProc,
                                    uintptr_t param1,
                                    uintptr_t param2,
                                    const uint32_t *types,
                                    uint32_t typeCount,
                                    uint32_t condemned,
                                    uint32_t maxgen,
                                    uint32_t flags);

void            HndResetAgeMap(HHANDLETABLE hTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, uint32_t flags);
void            HndVerifyTable(HHANDLETABLE hTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, uint32_t flags);

void            HndNotifyGcCycleComplete(HHANDLETABLE hTable, uint32_t condemned, uint32_t maxgen);

/*
 * Handle counting
 */

uint32_t        HndCountHandles(HHANDLETABLE hTable);
uint32_t        HndCountAllHandles(BOOL fUseLocks);

/*--------------------------------------------------------------------------*/


#ifdef _DEBUG_IMPL
void ValidateAssignObjrefForHandle(OBJECTREF);
void ValidateFetchObjrefForHandle(OBJECTREF);
#endif

/*
 * handle assignment
 */
void HndAssignHandle(OBJECTHANDLE handle, OBJECTREF objref);

/*
 * interlocked-exchange assignment
 */
void* HndInterlockedCompareExchangeHandle(OBJECTHANDLE handle, OBJECTREF objref, OBJECTREF oldObjref);

/*
 * Note that HndFirstAssignHandle is similar to HndAssignHandle, except that it only
 * succeeds if transitioning from NULL to non-NULL.  In other words, if this handle
 * is being initialized for the first time.
 */
BOOL HndFirstAssignHandle(OBJECTHANDLE handle, OBJECTREF objref);

/*
 * inline handle dereferencing
 *
 * NOTE: Changes to this implementation should be kept in sync with ObjectFromHandle
 *       on the VM side.
 *
 */
GC_DAC_VISIBLE
FORCEINLINE
OBJECTREF HndFetchHandle(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    // sanity
    _ASSERTE(handle);

#ifdef _DEBUG_IMPL
    _ASSERTE("Attempt to access destroyed handle." && *(_UNCHECKED_OBJECTREF *)handle != DEBUG_DestroyedHandleValue);

    // Make sure the objref for handle is valid
    ValidateFetchObjrefForHandle(ObjectToOBJECTREF(*(Object **)handle));
#endif // _DEBUG_IMPL

    // wrap the raw objectref and return it
    return UNCHECKED_OBJECTREF_TO_OBJECTREF(*PTR_UNCHECKED_OBJECTREF(handle));
}


/*
 * inline null testing (needed in certain cases where we're in the wrong GC mod)
 */
FORCEINLINE BOOL HndIsNull(OBJECTHANDLE handle)
{
    LIMITED_METHOD_CONTRACT;

    // sanity
    _ASSERTE(handle);

    return NULL == *(Object **)handle;
}


/*
 *
 * Checks handle value for null or special value used for free handles in cache.
 *
 */
FORCEINLINE BOOL HndIsNullOrDestroyedHandle(_UNCHECKED_OBJECTREF value)
{
    LIMITED_METHOD_CONTRACT;

#ifdef DEBUG_DestroyedHandleValue
    if (value == DEBUG_DestroyedHandleValue)
         return TRUE;
#endif

    return (value == NULL);
}

/*--------------------------------------------------------------------------*/

#include "handletable.inl"

#endif //_HANDLETABLE_H

