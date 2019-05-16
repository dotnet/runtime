// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

typedef DPTR(struct HandleTableMap) PTR_HandleTableMap;
typedef DPTR(struct HandleTableBucket) PTR_HandleTableBucket;
typedef DPTR(PTR_HandleTableBucket) PTR_PTR_HandleTableBucket;

struct HandleTableMap
{
    PTR_PTR_HandleTableBucket   pBuckets;
    PTR_HandleTableMap          pNext;
    uint32_t                    dwMaxIndex;
};

extern HandleTableMap g_HandleTableMap;

// struct containing g_SystemInfo.dwNumberOfProcessors HHANDLETABLEs and current table index
// instead of just single HHANDLETABLE for on-fly balancing while adding handles on multiproc machines

struct HandleTableBucket
{
    PTR_HHANDLETABLE pTable;
    uint32_t         HandleTableIndex;

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

GC_DAC_VISIBLE
OBJECTREF GetDependentHandleSecondary(OBJECTHANDLE handle);

#ifndef DACCESS_COMPILE
void SetDependentHandleSecondary(OBJECTHANDLE handle, OBJECTREF secondary);
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
uint32_t     GetVariableHandleType(OBJECTHANDLE handle);
void         UpdateVariableHandleType(OBJECTHANDLE handle, uint32_t type);
uint32_t     CompareExchangeVariableHandleType(OBJECTHANDLE handle, uint32_t oldType, uint32_t newType);

/*
 * Convenience prototypes for using the global handles
 */

int GetCurrentThreadHomeHeapNumber();

/*
 * Table maintenance routines
 */
bool Ref_Initialize();
void Ref_Shutdown();
bool Ref_InitializeHandleTableBucket(HandleTableBucket* bucket);
void Ref_RemoveHandleTableBucket(HandleTableBucket *pBucket);
void Ref_DestroyHandleTableBucket(HandleTableBucket *pBucket);

/*
 * GC-time scanning entrypoints
 */
struct ScanContext;
struct DhContext;
void Ref_BeginSynchronousGC   (uint32_t uCondemnedGeneration, uint32_t uMaxGeneration);
void Ref_EndSynchronousGC     (uint32_t uCondemnedGeneration, uint32_t uMaxGeneration);

typedef void Ref_promote_func(class Object**, ScanContext*, uint32_t);

void Ref_TraceRefCountHandles(HANDLESCANPROC callback, uintptr_t lParam1, uintptr_t lParam2);
void Ref_TracePinningRoots(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_TraceNormalRoots(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_UpdatePointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_UpdatePinnedPointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
DhContext *Ref_GetDependentHandleContext(ScanContext* sc);
bool Ref_ScanDependentHandlesForPromotion(DhContext *pDhContext);
void Ref_ScanDependentHandlesForClearing(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_ScanDependentHandlesForRelocation(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
void Ref_ScanSizedRefHandles(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
#ifdef FEATURE_REDHAWK
void Ref_ScanPointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn);
#endif

void Ref_CheckReachable       (uint32_t uCondemnedGeneration, uint32_t uMaxGeneration, uintptr_t lp1);
void Ref_CheckAlive           (uint32_t uCondemnedGeneration, uint32_t uMaxGeneration, uintptr_t lp1);
void Ref_ScanHandlesForProfilerAndETW(uint32_t uMaxGeneration, uintptr_t lp1, handle_scan_fn fn);
void Ref_ScanDependentHandlesForProfilerAndETW(uint32_t uMaxGeneration, ScanContext * SC, handle_scan_fn fn);
void Ref_AgeHandles           (uint32_t uCondemnedGeneration, uint32_t uMaxGeneration, uintptr_t lp1);
void Ref_RejuvenateHandles(uint32_t uCondemnedGeneration, uint32_t uMaxGeneration, uintptr_t lp1);

void Ref_VerifyHandleTable(uint32_t condemned, uint32_t maxgen, ScanContext* sc);

#endif // DACCESS_COMPILE

#endif //_OBJECTHANDLE_H
