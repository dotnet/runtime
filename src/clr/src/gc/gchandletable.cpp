// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

#include "common.h"
#include "gcenv.h"
#include "gchandletableimpl.h"
#include "objecthandle.h"

IGCHandleTable* CreateGCHandleTable()
{
    return new(nothrow) GCHandleTable();
}

bool GCHandleTable::Initialize()
{
    return Ref_Initialize();
}

void GCHandleTable::Shutdown()
{
    Ref_Shutdown();
}

void* GCHandleTable::GetHandleTableContext(void* handleTable)
{
    return (void*)((uintptr_t)::HndGetHandleTableADIndex((HHANDLETABLE)handleTable).m_dwIndex);
}

void* GCHandleTable::GetHandleTableForHandle(OBJECTHANDLE handle)
{
    return (void*)::HndGetHandleTable(handle);
}

OBJECTHANDLE GCHandleTable::CreateHandleOfType(void* table, Object* object, int type)
{
    return ::HndCreateHandle((HHANDLETABLE)table, type, ObjectToOBJECTREF(object));
}

OBJECTHANDLE GCHandleTable::CreateGlobalHandleOfType(Object* object, int type)
{
    return ::HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], type, ObjectToOBJECTREF(object)); 
}

OBJECTHANDLE GCHandleTable::CreateHandleWithExtraInfo(void* table, Object* object, int type, void* pExtraInfo)
{
    return ::HndCreateHandle((HHANDLETABLE)table, type, ObjectToOBJECTREF(object), reinterpret_cast<uintptr_t>(pExtraInfo));
}

OBJECTHANDLE GCHandleTable::CreateDependentHandle(void* table, Object* primary, Object* secondary)
{
    OBJECTHANDLE handle = ::HndCreateHandle((HHANDLETABLE)table, HNDTYPE_DEPENDENT, ObjectToOBJECTREF(primary));
    ::SetDependentHandleSecondary(handle, ObjectToOBJECTREF(secondary));

    return handle;
}