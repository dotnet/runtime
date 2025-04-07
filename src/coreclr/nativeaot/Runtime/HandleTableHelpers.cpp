// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Helper functions that are p/invoked from redhawkm in order to expose handle table functionality to managed
// code. These p/invokes are special in that the handle table code requires we remain in co-operative mode
// (since these routines mutate the handle tables which are also accessed during garbage collections). The
// binder has special knowledge of these methods and doesn't generate the normal code to transition out of the
// runtime prior to the call.
//
#include "common.h"
#include "gcenv.h"
#include "objecthandle.h"
#include "RestrictedCallouts.h"
#include "gchandleutilities.h"


FCIMPL2(OBJECTHANDLE, RhpHandleAlloc, Object *pObject, int type)
{
    return GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore()->CreateHandleOfType(pObject, (HandleType)type);
}
FCIMPLEND

FCIMPL2(OBJECTHANDLE, RhpHandleAllocDependent, Object *pPrimary, Object *pSecondary)
{
    return GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore()->CreateDependentHandle(pPrimary, pSecondary);
}
FCIMPLEND

FCIMPL1(void, RhHandleFree, OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfUnknownType(handle);
}
FCIMPLEND

FCIMPL1(Object *, RhHandleGet, OBJECTHANDLE handle)
{
    return ObjectFromHandle(handle);
}
FCIMPLEND

FCIMPL2(Object *, RhHandleGetDependent, OBJECTHANDLE handle, Object **ppSecondary)
{
    Object *pPrimary = ObjectFromHandle(handle);
    *ppSecondary = (pPrimary != NULL) ? GetDependentHandleSecondary(handle) : NULL;
    return pPrimary;
}
FCIMPLEND

FCIMPL2(void, RhHandleSetDependentSecondary, OBJECTHANDLE handle, Object *pSecondary)
{
    SetDependentHandleSecondary(handle, pSecondary);
}
FCIMPLEND

FCIMPL2(void, RhHandleSet, OBJECTHANDLE handle, Object *pObject)
{
    GCHandleUtilities::GetGCHandleManager()->StoreObjectInHandle(handle, pObject);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RhRegisterRefCountedHandleCallback, void * pCallout, MethodTable * pTypeFilter)
{
    FC_RETURN_BOOL(RestrictedCallouts::RegisterRefCountedHandleCallback(pCallout, pTypeFilter));
}
FCIMPLEND

FCIMPL2(void, RhUnregisterRefCountedHandleCallback, void * pCallout, MethodTable * pTypeFilter)
{
    RestrictedCallouts::UnregisterRefCountedHandleCallback(pCallout, pTypeFilter);
}
FCIMPLEND

// This structure mirrors the managed type System.Runtime.InteropServices.ComWrappers.ManagedObjectWrapper.
struct ManagedObjectWrapper
{
    intptr_t HolderHandle;
    uint64_t RefCount;

    int32_t UserDefinedCount;
    void* /* ComInterfaceEntry */ UserDefined;
    void* /* InternalComInterfaceDispatch* */ Dispatches;

    int32_t /* CreateComInterfaceFlagsEx */ Flags;

    uint32_t AddRef()
    {
        return GetComCount((uint64_t)PalInterlockedIncrement64((int64_t*)&RefCount));
    }

    static const uint64_t ComRefCountMask = 0x000000007fffffffUL;

    static uint32_t GetComCount(uint64_t c)
    {
        return (uint32_t)(c & ComRefCountMask);
    }
};

template<typename T>
struct Span
{
    T* _pointer;
    int _length;
};

// This structure mirrors the managed type System.Runtime.InteropServices.ComWrappers.InternalComInterfaceDispatch.
struct InternalComInterfaceDispatch
{
    ManagedObjectWrapper* _thisPtr;
    Span<void*> Vtables;
};

#ifdef TARGET_64BIT
constexpr uintptr_t DispatchAlignment = 64;
#else
constexpr uintptr_t DispatchAlignment = 16;
#endif

constexpr uintptr_t DispatchAlignmentMask = ~(DispatchAlignment - 1);

static ManagedObjectWrapper* ToManagedObjectWrapper(void* dispatchPtr)
{
    uintptr_t dispatch = reinterpret_cast<uintptr_t>(dispatchPtr) & DispatchAlignmentMask;
    return ((InternalComInterfaceDispatch*)dispatch)->_thisPtr;
}

//
// AddRef is implemented in native code so that it does not need to synchronize with the GC. This is important because Xaml
// invokes AddRef while holding a lock that it *also* holds while a GC is in progress.  If AddRef was managed, we would have
// to synchronize with the GC before entering AddRef, which would deadlock with the other thread holding Xaml's lock.
//
static uint32_t __stdcall IUnknown_AddRef(void* pComThis)
{
    ManagedObjectWrapper* wrapper = ToManagedObjectWrapper(pComThis);
    return wrapper->AddRef();
}

FCIMPL0(void*, RhGetIUnknownAddRef)
{
    return (void*)&IUnknown_AddRef;
}
FCIMPLEND
