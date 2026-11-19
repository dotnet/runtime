// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "CommonMacros.inl"
#include "volatile.h"
#include "Pal.h"
#include "rhassert.h"

#include "slist.h"
#include "shash.h"
#include "holder.h"
#include "rhbinder.h"
#include "Crst.h"
#include "RuntimeInstance.h"
#include "event.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "MethodTable.h"
#include "TypeManager.h"
#include "MethodTable.inl"
#include "ObjectLayout.h"

#include "GCMemoryHelpers.inl"

#if defined(FEATURE_PORTABLE_HELPERS)
// @TODO Implement UniversalTransitionTailCall
EXTERN_C void * ReturnFromUniversalTransitionTailCall;
void * ReturnFromUniversalTransitionTailCall;

#if !defined(HOST_WASM)
FCIMPL2_RAW(void, RhpAssignRef, Object ** dst, Object * ref)
{
    // @TODO: FEATURE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineWriteBarrier(dst, ref);
}
FCIMPLEND

FCIMPL2_RAW(void, RhpCheckedAssignRef, Object ** dst, Object * ref)
{
    // @TODO: FEATURE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineCheckedWriteBarrier(dst, ref);
}
FCIMPLEND
#endif

FCIMPL3(Object *, RhpCheckedLockCmpXchg, Object ** location, Object * value, Object * comparand)
{
    Object * ret = (Object *)PalInterlockedCompareExchangePointer((void * volatile *)location, value, comparand);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}
FCIMPLEND

FCIMPL2(Object *, RhpCheckedXchg, Object ** location, Object * value)
{
    // @TODO: FEATURE_PORTABLE_HELPERS - Null check
    Object * ret = (Object *)PalInterlockedExchangePointer((void * volatile *)location, value);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}
FCIMPLEND

EXTERN_C HRESULT QCALLTYPE RhAllocateThunksMapping(void** ppThunksSection)
{
    return E_FAIL;
}

FCIMPL0(void *, RhpGetThunksBase)
{
    return NULL;
}
FCIMPLEND

FCIMPL0(int, RhpGetNumThunkBlocksPerMapping)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(int, RhpGetNumThunksPerBlock)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(int, RhpGetThunkSize)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL1(void*, RhpGetThunkDataBlockAddress, void* pThunkStubAddress)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL1(void*, RhpGetThunkStubsBlockAddress, void* pThunkDataAddress)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL0(int, RhpGetThunkBlockSize)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(void, RhpGcPoll)
{
    // TODO: implement
}
FCIMPLEND

#endif
