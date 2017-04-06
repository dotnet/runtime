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

    virtual void* GetHandleTableContext(void* handleTable);

    virtual void* GetHandleTableForHandle(OBJECTHANDLE handle);

    virtual OBJECTHANDLE CreateHandleOfType(void* table, Object* object, int type);

    virtual OBJECTHANDLE CreateHandleWithExtraInfo(void* table, Object* object, int type, void* pExtraInfo);

    virtual OBJECTHANDLE CreateDependentHandle(void* table, Object* primary, Object* secondary);

    virtual OBJECTHANDLE CreateGlobalHandleOfType(Object* object, int type);
};

#endif  // GCHANDLETABLE_H_
