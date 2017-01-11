// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

OBJECTREF AllocateValueSzArray(TypeHandle elementType, INT32 length);
    // The main Array allocation routine, can do multi-dimensional
OBJECTREF AllocateArrayEx(TypeHandle arrayClass, INT32 *pArgs, DWORD dwNumArgs, BOOL bAllocateInLargeHeap = FALSE
                          DEBUG_ARG(BOOL bDontSetAppDomain = FALSE));
    // Optimized verion of above
OBJECTREF FastAllocatePrimitiveArray(MethodTable* arrayType, DWORD cElements, BOOL bAllocateInLargeHeap = FALSE);


#if defined(_TARGET_X86_)

    // for x86, we generate efficient allocators for some special cases
    // these are called via inline wrappers that call the generated allocators
    // via function pointers.


    // Create a SD array of primitive types
typedef HCCALL2_PTR(Object*, FastPrimitiveArrayAllocatorFuncPtr, CorElementType type, DWORD cElements);

extern FastPrimitiveArrayAllocatorFuncPtr fastPrimitiveArrayAllocator;

    // The fast version always allocates in the normal heap
OBJECTREF AllocatePrimitiveArray(CorElementType type, DWORD cElements);

    // The slow version is distinguished via overloading by an additional parameter
OBJECTREF AllocatePrimitiveArray(CorElementType type, DWORD cElements, BOOL bAllocateInLargeHeap);


// Allocate SD array of object pointers.  StubLinker-generated asm code might
// implement this, so the element TypeHandle is passed as a PVOID to avoid any
// struct calling convention weirdness.
typedef HCCALL2_PTR(Object*, FastObjectArrayAllocatorFuncPtr, /*TypeHandle*/PVOID ArrayType, DWORD cElements);

extern FastObjectArrayAllocatorFuncPtr fastObjectArrayAllocator;

    // The fast version always allocates in the normal heap
OBJECTREF AllocateObjectArray(DWORD cElements, TypeHandle ElementType);

    // The slow version is distinguished via overloading by an additional parameter
OBJECTREF AllocateObjectArray(DWORD cElements, TypeHandle ElementType, BOOL bAllocateInLargeHeap);


    // Allocate string
typedef HCCALL1_PTR(StringObject*, FastStringAllocatorFuncPtr, DWORD cchArrayLength);

extern FastStringAllocatorFuncPtr fastStringAllocator;

STRINGREF AllocateString( DWORD cchStringLength );

    // The slow version, implemented in gcscan.cpp
STRINGREF SlowAllocateString( DWORD cchStringLength );

#else

// On other platforms, go to the (somewhat less efficient) implementations in gcscan.cpp

    // Create a SD array of primitive types
OBJECTREF AllocatePrimitiveArray(CorElementType type, DWORD cElements, BOOL bAllocateInLargeHeap = FALSE);

    // Allocate SD array of object pointers
OBJECTREF AllocateObjectArray(DWORD cElements, TypeHandle ElementType, BOOL bAllocateInLargeHeap = FALSE);

STRINGREF SlowAllocateString( DWORD cchStringLength );

inline STRINGREF AllocateString( DWORD cchStringLength )
{
    WRAPPER_NO_CONTRACT;

    return SlowAllocateString( cchStringLength );
}

#endif

OBJECTREF DupArrayForCloning(BASEARRAYREF pRef, BOOL bAllocateInLargeHeap = FALSE);

// The JIT requests the EE to specify an allocation helper to use at each new-site.
// The EE makes this choice based on whether context boundaries may be involved,
// whether the type is a COM object, whether it is a large object,
// whether the object requires finalization.
// These functions will throw OutOfMemoryException so don't need to check
// for NULL return value from them.

OBJECTREF AllocateObject(MethodTable *pMT
#ifdef FEATURE_COMINTEROP
                         , bool fHandleCom = true
#endif
    );

extern void StompWriteBarrierEphemeral(bool isRuntimeSuspended);
extern void StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck);
extern void SwitchToWriteWatchBarrier(bool isRuntimeSuspended);
extern void SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended);

extern void ThrowOutOfMemoryDimensionsExceeded();

//========================================================================
//
//      WRITE BARRIER HELPERS
//
//========================================================================

void ErectWriteBarrier(OBJECTREF* dst, OBJECTREF ref);
void SetCardsAfterBulkCopy(Object **start, size_t len);
#endif // _GCHELPERS_H_
