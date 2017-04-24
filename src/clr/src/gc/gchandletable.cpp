// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

#include "common.h"
#include "gcenv.h"
#include "gchandletableimpl.h"
#include "objecthandle.h"

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

OBJECTHANDLE GCHandleStore::CreateHandleOfType(Object* object, int type)
{
    HHANDLETABLE handletable = _underlyingBucket.pTable[GetCurrentThreadHomeHeapNumber()];
    return ::HndCreateHandle(handletable, type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleStore::CreateHandleOfType(Object* object, int type, int heapToAffinitizeTo)
{
    HHANDLETABLE handletable = _underlyingBucket.pTable[heapToAffinitizeTo];
    return ::HndCreateHandle(handletable, type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleStore::CreateHandleWithExtraInfo(Object* object, int type, void* pExtraInfo)
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

IGCHandleStore* GCHandleManager::CreateHandleStore(void* context)
{
#ifndef FEATURE_REDHAWK
    GCHandleStore* store = new (nothrow) GCHandleStore();
    if (store == nullptr)
        return nullptr;

    bool success = ::Ref_InitializeHandleTableBucket(&store->_underlyingBucket, context);
    if (!success)
    {
        delete store;
        return nullptr;
    }

    return store;
#else
    assert("CreateHandleStore is not implemented when FEATURE_REDHAWK is defined!");
    return nullptr;
#endif
}

void GCHandleManager::DestroyHandleStore(IGCHandleStore* store)
{
    delete store;
}

void* GCHandleManager::GetHandleContext(OBJECTHANDLE handle)
{
    return (void*)((uintptr_t)::HndGetHandleTableADIndex(::HndGetHandleTable(handle)).m_dwIndex);
}

OBJECTHANDLE GCHandleManager::CreateGlobalHandleOfType(Object* object, int type)
{
    return ::HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], type, ObjectToOBJECTREF(object)); 
}

OBJECTHANDLE GCHandleManager::CreateDuplicateHandle(OBJECTHANDLE handle)
{
    return ::HndCreateHandle(HndGetHandleTable(handle), HNDTYPE_DEFAULT, ::HndFetchHandle(handle));
}

void GCHandleManager::DestroyHandleOfType(OBJECTHANDLE handle, int type)
{
    ::HndDestroyHandle(::HndGetHandleTable(handle), type, handle);
}

void GCHandleManager::DestroyHandleOfUnknownType(OBJECTHANDLE handle)
{
    ::HndDestroyHandleOfUnknownType(::HndGetHandleTable(handle), handle);
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

Object* GCHandleManager::InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, Object* object, Object* comparandObject)
{
    return (Object*)::HndInterlockedCompareExchangeHandle(handle, ObjectToOBJECTREF(object), ObjectToOBJECTREF(comparandObject));
}
