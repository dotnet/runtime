// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _CUSTOMATTRIBUTE_H_
#define _CUSTOMATTRIBUTE_H_

#include "fcall.h"
#include "../md/compiler/custattr.h"

typedef Factory< SArray<CaValue> > CaValueArrayFactory;

class Attribute
{
public:
    static HRESULT ParseAttributeArgumentValues(
        void* pCa,
        INT32 cCa,
        CaValueArrayFactory* pCaValueArrayFactory,
        CaArg* pCaArgs,
        COUNT_T cArgs,
        CaNamedArg* pCaNamedArgs,
        COUNT_T cNamedArgs,
        DomainAssembly* pDomainAssembly);
};

extern "C" void QCALLTYPE CustomAttribute_CreateCustomAttributeInstance(
    QCall::ObjectHandleOnStack pAttributedModule,
    QCall::ObjectHandleOnStack pCaType,
    QCall::ObjectHandleOnStack pMethod,
    BYTE** ppBlob,
    BYTE* pEndBlob,
    INT32* pcNamedArgs,
    QCall::ObjectHandleOnStack result);

extern "C" void QCALLTYPE CustomAttribute_CreatePropertyOrFieldData(
    QCall::ObjectHandleOnStack pModule,
    BYTE** ppBlobStart,
    BYTE* pBlobEnd,
    QCall::ObjectHandleOnStack pName,
    bool* pbIsProperty,
    QCall::ObjectHandleOnStack pType,
    QCall::ObjectHandleOnStack value);
#endif

