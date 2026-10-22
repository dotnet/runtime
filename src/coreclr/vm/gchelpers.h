// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * GCHELPERS.H
 *
 * GC Allocation and Write Barrier Helpers
 *

 *
 */

#ifndef _GCHELPERS_H_
#define _GCHELPERS_H_

//========================================================================
//
//      ALLOCATION HELPERS
//
//========================================================================

// Allocate single-dimensional array given array type
OBJECTREF AllocateSzArray(MethodTable *pArrayMT, INT32 length, GC_ALLOC_FLAGS flags = GC_ALLOC_NO_FLAGS);
OBJECTREF AllocateSzArray(TypeHandle  arrayType, INT32 length, GC_ALLOC_FLAGS flags = GC_ALLOC_NO_FLAGS);

// Allocate single-dimensional array on a frozen segment
// Returns nullptr if it's not possible.
OBJECTREF TryAllocateFrozenSzArray(MethodTable* pArrayMT, INT32 length);
// Same for non-array objects
OBJECTREF TryAllocateFrozenObject(MethodTable* pObjMT);

// The main Array allocation routine, can do multi-dimensional
OBJECTREF AllocateArrayEx(MethodTable *pArrayMT, INT32 *pArgs, DWORD dwNumArgs, GC_ALLOC_FLAGS flags = GC_ALLOC_NO_FLAGS);
OBJECTREF AllocateArrayEx(TypeHandle  arrayType, INT32 *pArgs, DWORD dwNumArgs, GC_ALLOC_FLAGS flags = GC_ALLOC_NO_FLAGS);

// Create a SD array of primitive types given an element type
OBJECTREF AllocatePrimitiveArray(CorElementType type, DWORD cElements);

// Allocate SD array of object types given an element type
OBJECTREF AllocateObjectArray(DWORD cElements, TypeHandle ElementType, BOOL bAllocateInPinnedHeap = FALSE);

// Allocate a string
STRINGREF AllocateString(DWORD cchStringLength);
STRINGREF AllocateString(DWORD cchStringLength, bool preferFrozenHeap, bool* pIsFrozen);

OBJECTREF DupArrayForCloning(BASEARRAYREF pRef);

// The JIT requests the EE to specify an allocation helper to use at each new-site.
// The EE makes this choice based on whether context boundaries may be involved,
// whether the type is a COM object, whether it is a large object,
// whether the object requires finalization.
// These functions will throw OutOfMemoryException so don't need to check
// for NULL return value from them.

OBJECTREF AllocateObject(MethodTable *pMT
                         , GC_ALLOC_FLAGS flags
#ifdef FEATURE_COMINTEROP
                         , bool fHandleCom = true
#endif
    );

inline OBJECTREF AllocateObject(MethodTable *pMT
#ifdef FEATURE_COMINTEROP
                                , bool fHandleCom = true
#endif
    )
{
    return AllocateObject(pMT, GC_ALLOC_NO_FLAGS
#ifdef FEATURE_COMINTEROP
                          , fHandleCom
#endif
        );
}

extern int StompWriteBarrierEphemeral(bool isRuntimeSuspended);
extern int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck);
extern int SwitchToWriteWatchBarrier(bool isRuntimeSuspended);
extern int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended);
extern void FlushWriteBarrierInstructionCache();
extern void UpdateCustomWriteBarrierGlobals();

extern void ThrowOutOfMemoryDimensionsExceeded();

//========================================================================
//
//      WRITE BARRIER HELPERS
//
//========================================================================

void ErectWriteBarrier(OBJECTREF* dst, OBJECTREF ref);

// Old-value store hooks for a collector that needs the old value of every overwritten reference (LXR). See the
// comment in gchelpers.cpp. They return true when the collector has been told about the old values and the caller
// must overwrite the destination without the stock card marking; false for the stock path.
bool ErectWriteBarrierObjectRangePre(Object* object, uint8_t* start, size_t byteCount);
bool ErectWriteBarrierLayoutRangePre(
    void* dst, const void* src, MethodTable* type, size_t elementSize, size_t elementCount);
bool ErectWriteBarrierLayoutFillPre(
    void* dst, const void* value, MethodTable* type, size_t elementSize, size_t elementCount);
bool ErectWriteBarrierLayoutChunkPre(
    void* dst,
    const void* src,
    MethodTable* type,
    size_t gcLayoutOffset,
    size_t elementSize,
    size_t chunkOffset,
    size_t chunkSize);

void PublishFrozenObject(Object*& orObject);

#endif // _GCHELPERS_H_
