// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "common.h"
#include "gcenv.h"
#include "gchandletableimpl.h"
#include "objecthandle.h"
#include "handletablepriv.h"

GCHandleStore* g_gcGlobalHandleStore;

IGCHandleManager* CreateGCHandleManager()
{
    return new (nothrow) GCHandleManager();
}

void GCHandleStore::Uproot()
{
    Ref_RemoveHandleTableBucket(&_underlyingBucket);
}

bool GCHandleStore::ContainsHandle(OBJECTHANDLE handle)
{
    return _underlyingBucket.Contains(handle);
}

// this is the number of handles we allocate in a handle table before we switch to the next table.
#define HANDLE_THRESHOLD (15)
static int s_numTableSlots = 0;

HHANDLETABLE GCHandleStore::GetTable()
{
    if (!s_numTableSlots)
    {
        s_numTableSlots = getNumberOfSlots();
    }

    if (s_numTableSlots == 1)
        return _underlyingBucket.pTable[0];

    gc_alloc_context* ctx = GetCurrentThreadAllocContext();
    if (!ctx)
        return _underlyingBucket.pTable[0];

    // high 16-bits are for the handle info.
    int handleInfo = (uint32_t)(ctx->alloc_count) >> 16;

    // high 10 bits store the cpu index. 
    // low 6 bits store the # of handles allocated so far (before the next reset).
    int numHandles = handleInfo & 0x3f;
    int cpuIndex = handleInfo >> 6;
    int savedCpuIndex = cpuIndex;
    if (numHandles == HANDLE_THRESHOLD)
    {
        numHandles = 0;
        cpuIndex++;
        cpuIndex = (cpuIndex > (s_numTableSlots - 1)) ? 0 : cpuIndex;
    }
    else
    {
        numHandles++;
    }

    int newHandleInfo = numHandles | (cpuIndex << 6);

    int low_16_bits = ctx->alloc_count & 0xffff;
    ctx->alloc_count = low_16_bits | (newHandleInfo << 16);

    HHANDLETABLE handletable = _underlyingBucket.pTable[cpuIndex];
    return handletable;
}

OBJECTHANDLE GCHandleStore::CreateHandleOfType(Object* object, HandleType type)
{
    HHANDLETABLE handletable = GetTable();
    return ::HndCreateHandle(handletable, type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleStore::CreateHandleOfType(Object* object, HandleType type, int heapToAffinitizeTo)
{
    HHANDLETABLE handletable = _underlyingBucket.pTable[heapToAffinitizeTo];
    return ::HndCreateHandle(handletable, type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleStore::CreateHandleWithExtraInfo(Object* object, HandleType type, void* pExtraInfo)
{
    HHANDLETABLE handletable = _underlyingBucket.pTable[GetCurrentThreadHomeHeapNumber()];
    return ::HndCreateHandle(handletable, type, ObjectToOBJECTREF(object), reinterpret_cast<uintptr_t>(pExtraInfo));
}

OBJECTHANDLE GCHandleStore::CreateDependentHandle(Object* primary, Object* secondary)
{
    HHANDLETABLE handletable = _underlyingBucket.pTable[GetCurrentThreadHomeHeapNumber()];
    OBJECTHANDLE handle = ::HndCreateHandle(handletable, HNDTYPE_DEPENDENT, ObjectToOBJECTREF(primary));
    if (!handle)
    {
        return nullptr;
    }

    ::SetDependentHandleSecondary(handle, ObjectToOBJECTREF(secondary));
    return handle;
}

GCHandleStore::~GCHandleStore()
{
    ::Ref_DestroyHandleTableBucket(&_underlyingBucket);
}

bool GCHandleManager::Initialize()
{
    return Ref_Initialize();
}

void GCHandleManager::Shutdown()
{
    if (g_gcGlobalHandleStore != nullptr)
    {
        DestroyHandleStore(g_gcGlobalHandleStore);
    }

    ::Ref_Shutdown();
}

IGCHandleStore* GCHandleManager::GetGlobalHandleStore()
{
    return g_gcGlobalHandleStore;
}

IGCHandleStore* GCHandleManager::CreateHandleStore()
{
#ifndef FEATURE_NATIVEAOT
    GCHandleStore* store = new (nothrow) GCHandleStore();
    if (store == nullptr)
    {
        return nullptr;
    }

    bool success = ::Ref_InitializeHandleTableBucket(&store->_underlyingBucket);
    if (!success)
    {
        delete store;
        return nullptr;
    }

    return store;
#else
    assert(!"CreateHandleStore is not implemented when FEATURE_NATIVEAOT is defined!");
    return nullptr;
#endif
}

void GCHandleManager::DestroyHandleStore(IGCHandleStore* store)
{
    delete store;
}

OBJECTHANDLE GCHandleManager::CreateGlobalHandleOfType(Object* object, HandleType type)
{
    return ::HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleManager::CreateDuplicateHandle(OBJECTHANDLE handle)
{
    return ::HndCreateHandle(HndGetHandleTable(handle), HNDTYPE_DEFAULT, ::HndFetchHandle(handle));
}

void GCHandleManager::DestroyHandleOfType(OBJECTHANDLE handle, HandleType type)
{
    ::HndDestroyHandle(::HndGetHandleTable(handle), type, handle);
}

void GCHandleManager::DestroyHandleOfUnknownType(OBJECTHANDLE handle)
{
    ::HndDestroyHandleOfUnknownType(::HndGetHandleTable(handle), handle);
}

void GCHandleManager::SetExtraInfoForHandle(OBJECTHANDLE  handle, HandleType type, void* pExtraInfo)
{
    ::HndSetHandleExtraInfo(handle, type, (uintptr_t)pExtraInfo);
}

void* GCHandleManager::GetExtraInfoFromHandle(OBJECTHANDLE handle)
{
    return (void*)::HndGetHandleExtraInfo(handle);
}

void GCHandleManager::StoreObjectInHandle(OBJECTHANDLE handle, Object* object)
{
    ::HndAssignHandle(handle, ObjectToOBJECTREF(object));
}

bool GCHandleManager::StoreObjectInHandleIfNull(OBJECTHANDLE handle, Object* object)
{
    return !!::HndFirstAssignHandle(handle, ObjectToOBJECTREF(object));
}

void GCHandleManager::SetDependentHandleSecondary(OBJECTHANDLE handle, Object* object)
{
    ::SetDependentHandleSecondary(handle, ObjectToOBJECTREF(object));
}

Object* GCHandleManager::GetDependentHandleSecondary(OBJECTHANDLE handle)
{
    return OBJECTREFToObject(::GetDependentHandleSecondary(handle));
}

Object* GCHandleManager::InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, Object* object, Object* comparandObject)
{
    return (Object*)::HndInterlockedCompareExchangeHandle(handle, ObjectToOBJECTREF(object), ObjectToOBJECTREF(comparandObject));
}

HandleType GCHandleManager::HandleFetchType(OBJECTHANDLE handle)
{
    uint32_t type = ::HandleFetchType(handle);
    assert(type >= HNDTYPE_WEAK_SHORT && type <= HNDTYPE_WEAK_NATIVE_COM);
    return static_cast<HandleType>(type);
}

void GCHandleManager::TraceRefCountedHandles(HANDLESCANPROC callback, uintptr_t param1, uintptr_t param2)
{
    ::Ref_TraceRefCountHandles(callback, param1, param2);
}

