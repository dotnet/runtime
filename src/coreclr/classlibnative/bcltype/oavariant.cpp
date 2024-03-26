// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: OAVariant.cpp
//

#include <common.h>

#ifdef FEATURE_COMINTEROP

#include <oleauto.h>
#include "oavariant.h"
#include "interopconverter.h"

extern "C" IUnknown* QCALLTYPE OAVariant_GetComIPFromObjectRef(QCall::ObjectHandleOnStack obj, ComIpType reqIPType, ComIpType* fetchedIpType)
{
    QCALL_CONTRACT;

    IUnknown* ret = nullptr;

    BEGIN_QCALL;

    GCX_COOP();
    OBJECTREF objectRef = obj.Get();
    ret = GetComIPFromObjectRef(&objectRef, reqIPType, fetchedIpType);

    END_QCALL;

    return ret;
}

extern "C" void* QCALLTYPE OAVariant_GetObjectRefFromComIP(QCall::ObjectHandleOnStack objRet, IUnknown* pUnk)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    OBJECTREF objectRef = NULL;
    GetObjectRefFromComIP(&objectRef, pUnk);
    objRet.Set(objectRef);

    END_QCALL;
}

#endif // FEATURE_COMINTEROP
