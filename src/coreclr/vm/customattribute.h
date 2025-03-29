// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _CUSTOMATTRIBUTE_H_
#define _CUSTOMATTRIBUTE_H_

#include "fcall.h"
#include "../md/compiler/custattr.h"

using CaValueArrayFactory = Factory<SArray<CaValue>>;

namespace CustomAttribute
{
    HRESULT ParseArgumentValues(
        void* pCa,
        INT32 cCa,
        CaValueArrayFactory* pCaValueArrayFactory,
        CaArg* pCaArgs,
        COUNT_T cArgs,
        CaNamedArg* pCaNamedArgs,
        COUNT_T cNamedArgs,
        Assembly* pAssembly);
}

extern "C" BOOL QCALLTYPE CustomAttribute_ParseAttributeUsageAttribute(
    PVOID pData,
    ULONG cData,
    ULONG* pTargets,
    BOOL* pAllowMultiple,
    BOOL* pInherited);

extern "C" void QCALLTYPE CustomAttribute_CreateCustomAttributeInstance(
    QCall::ModuleHandle pModule,
    QCall::ObjectHandleOnStack pCaType,
    QCall::ObjectHandleOnStack pMethod,
    BYTE** ppBlob,
    BYTE* pEndBlob,
    INT32* pcNamedArgs,
    QCall::ObjectHandleOnStack result);

extern "C" void QCALLTYPE CustomAttribute_CreatePropertyOrFieldData(
    QCall::ModuleHandle pModule,
    BYTE** ppBlobStart,
    BYTE* pBlobEnd,
    QCall::StringHandleOnStack pName,
    BOOL* pbIsProperty,
    QCall::ObjectHandleOnStack pType,
    QCall::ObjectHandleOnStack value);

#endif // _CUSTOMATTRIBUTE_H_
