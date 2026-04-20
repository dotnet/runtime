// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhbinder.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "CachedInterfaceDispatch.h"

//
// WASM uses a modified version of the regular cached interface dispatch mechanism. While ordinarily the dispatch
// stubs would directly call the target when it has been found (in the cache or otherwise), for WASM we cannot do
// this because there is no signature-agnostic way to transfer control. Stated otherwise, the stubs would need to
// be specialized on a per-signature basis, resulting in significant code size overhead with intrusive changes to
// the rest of dispatch code, which expects globally unique stubs. Thus we leave calling the target to codegen and
// here only resolve it. We also use only one stub, as the cost of an indirect call outweighs that of fetching the
// count of entries.
//

// Cache miss case, call the runtime to resolve the target and update the cache.
extern "C" PCODE RhpCidResolveWasm(void* pShadowStack, Object* pObject, void* pCell);

FCIMPL2(PCODE, RhpResolveInterfaceDispatch, Object* pObject, InterfaceDispatchCell* pCell)
{
    ASSERT(pObject != nullptr);
    InterfaceDispatchCache* pCache = (InterfaceDispatchCache*)pCell->GetCache();
    if (pCache != nullptr)
    {
        MethodTable* pObjectType = pObject->GetMethodTable();
        for (size_t i = 0; i < pCache->m_cEntries; i++)
        {
            InterfaceDispatchCacheEntry* pEntry = &pCache->m_rgEntries[i];
            if (pEntry->m_pInstanceType == pObjectType)
            {
                return pEntry->m_pTargetCode;
            }
        }
    }

    return RhpCidResolveWasm(pShadowStack, pObject, pCell);
}
FCIMPLEND

extern "C" void* RhpInitialInterfaceDispatch(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInitialDynamicInterfaceDispatch(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch1(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch2(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch4(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch8(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch16(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch32(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));
extern "C" void* RhpInterfaceDispatch64(void*, Object*, InterfaceDispatchCell*) __attribute__((alias ("RhpResolveInterfaceDispatch")));

// Stub dispatch routine for dispatch to a vtable slot.
FCIMPL2(void*, RhpVTableOffsetDispatch, Object* pObject, InterfaceDispatchCell* pCell)
{
    uintptr_t pVTable = reinterpret_cast<uintptr_t>(pObject->GetMethodTable());
    uintptr_t offset = pCell->m_pCache;

    return *(void**)(pVTable + offset);
}
FCIMPLEND

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
