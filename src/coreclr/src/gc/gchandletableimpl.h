// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef GCHANDLETABLE_H_
#define GCHANDLETABLE_H_

#include "gcinterface.h"
#include "objecthandle.h"

class GCHandleStore : public IGCHandleStore
{
public:
    virtual void Uproot();

    virtual bool ContainsHandle(OBJECTHANDLE handle);

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type);

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type, int heapToAffinitizeTo);

    virtual OBJECTHANDLE CreateHandleWithExtraInfo(Object* object, HandleType type, void* pExtraInfo);

    virtual OBJECTHANDLE CreateDependentHandle(Object* primary, Object* secondary);

    virtual ~GCHandleStore();

    HandleTableBucket _underlyingBucket;
};

extern GCHandleStore* g_gcGlobalHandleStore;

class GCHandleManager : public IGCHandleManager
{
public:
    virtual bool Initialize();

    virtual void Shutdown();

    virtual IGCHandleStore* GetGlobalHandleStore();

    virtual IGCHandleStore* CreateHandleStore();

    virtual void DestroyHandleStore(IGCHandleStore* store);

    virtual OBJECTHANDLE CreateGlobalHandleOfType(Object* object, HandleType type);

    virtual OBJECTHANDLE CreateDuplicateHandle(OBJECTHANDLE handle);

    virtual void DestroyHandleOfType(OBJECTHANDLE handle, HandleType type);

    virtual void DestroyHandleOfUnknownType(OBJECTHANDLE handle);

    virtual void SetExtraInfoForHandle(OBJECTHANDLE handle, HandleType type, void* pExtraInfo);

    virtual void* GetExtraInfoFromHandle(OBJECTHANDLE handle);

    virtual void StoreObjectInHandle(OBJECTHANDLE handle, Object* object);

    virtual bool StoreObjectInHandleIfNull(OBJECTHANDLE handle, Object* object);

    virtual void SetDependentHandleSecondary(OBJECTHANDLE handle, Object* object);

    virtual Object* GetDependentHandleSecondary(OBJECTHANDLE handle);

    virtual Object* InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, Object* object, Object* comparandObject);

    virtual HandleType HandleFetchType(OBJECTHANDLE handle);

    virtual void TraceRefCountedHandles(HANDLESCANPROC callback, uintptr_t param1, uintptr_t param2);
};

#endif  // GCHANDLETABLE_H_
