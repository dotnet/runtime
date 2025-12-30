// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ObjectNative.cpp
//

//
// Purpose: Native methods on System.Object
//
//

#include "common.h"

#include "objectnative.h"
#include "excep.h"
#include "vars.hpp"
#include "field.h"
#include "object.h"
#include "comsynchronizable.h"
#include "eeconfig.h"

extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    INT32 idx = 0;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(objHandle.Get() != NULL);
    idx = objHandle.Get()->GetHashCodeEx();

    END_QCALL;

    return idx;
}

FCIMPL1(INT32, ObjectNative::TryGetHashCode, Object* obj)
{
    FCALL_CONTRACT;

    if (obj == NULL)
        return 0;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    return objRef->TryGetHashCode();
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ObjectNative::ContentEquals, Object *pThisRef, Object *pCompareRef)
{
    FCALL_CONTRACT;

    // Should be ensured by caller
    _ASSERTE(pThisRef != NULL);
    _ASSERTE(pCompareRef != NULL);
    _ASSERTE(pThisRef->GetMethodTable() == pCompareRef->GetMethodTable());

    MethodTable *pThisMT = pThisRef->GetMethodTable();

    // Compare the contents
    BOOL ret = memcmp(
        pThisRef->GetData(),
        pCompareRef->GetData(),
        pThisMT->GetNumInstanceFieldBytes()) == 0;

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF refClone = objHandle.Get();
    _ASSERTE(refClone != NULL); // Should be handled at managed side
    MethodTable* pMT = refClone->GetMethodTable();

    // assert that String has overloaded the Clone() method
    _ASSERTE(pMT != g_pStringClass);

    if (pMT->IsArray())
    {
        objHandle.Set(DupArrayForCloning((BASEARRAYREF)refClone));
    }
    else
    {
        // We don't need to call the <cinit> because we know
        //  that it has been called....(It was called before this was created)
        objHandle.Set(AllocateObject(pMT));
    }

    END_QCALL;
}
