// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef GCHANDLETABLE_H_
#define GCHANDLETABLE_H_

#include "gcinterface.h"

class GCHandleTable : public IGCHandleTable
{
public:
    virtual bool Initialize();

    virtual void Shutdown();

    virtual void* GetGlobalHandleStore();

    virtual void* CreateHandleStore(void* context);

    virtual void* GetHandleContext(OBJECTHANDLE handle);

    virtual void DestroyHandleStore(void* store);

    virtual void UprootHandleStore(void* store);

    virtual bool ContainsHandle(void* store, OBJECTHANDLE handle);

    virtual OBJECTHANDLE CreateHandleOfType(void* store, Object* object, int type);

    virtual OBJECTHANDLE CreateHandleOfType(void* store, Object* object, int type, int heapToAffinitizeTo);

    virtual OBJECTHANDLE CreateHandleWithExtraInfo(void* store, Object* object, int type, void* pExtraInfo);

    virtual OBJECTHANDLE CreateDependentHandle(void* store, Object* primary, Object* secondary);

    virtual OBJECTHANDLE CreateGlobalHandleOfType(Object* object, int type);

    virtual OBJECTHANDLE CreateDuplicateHandle(OBJECTHANDLE handle);

    virtual void DestroyHandleOfType(OBJECTHANDLE handle, int type);

    virtual void DestroyHandleOfUnknownType(OBJECTHANDLE handle);

    virtual void* GetExtraInfoFromHandle(OBJECTHANDLE handle);
};

#endif  // GCHANDLETABLE_H_
