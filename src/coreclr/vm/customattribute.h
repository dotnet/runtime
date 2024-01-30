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

class COMCustomAttribute
{
public:

    // custom attributes utility functions
    static FCDECL5(VOID, ParseAttributeUsageAttribute, PVOID pData, ULONG cData, ULONG* pTargets, CLR_BOOL* pInherited, CLR_BOOL* pAllowMultiple);
    static FCDECL6(LPVOID, CreateCaObject, ReflectModuleBaseObject* pAttributedModuleUNSAFE, ReflectClassBaseObject* pCaTypeUNSAFE, ReflectMethodObject *pMethodUNSAFE, BYTE** ppBlob, BYTE* pEndBlob, INT32* pcNamedArgs);
    static FCDECL7(void, GetPropertyOrFieldData, ReflectModuleBaseObject *pModuleUNSAFE, BYTE** ppBlobStart, BYTE* pBlobEnd, STRINGREF* pName, CLR_BOOL* pbIsProperty, OBJECTREF* pType, OBJECTREF* value);

private:

    static TypeHandle GetTypeHandleFromBlob(
        Assembly *pCtorAssembly,
        CorSerializationType objType,
        BYTE **pBlob,
        const BYTE *endBlob,
        Module *pModule);

    static ARG_SLOT GetDataFromBlob(
        Assembly *pCtorAssembly,
        CorSerializationType type,
        TypeHandle th,
        BYTE **pBlob,
        const BYTE *endBlob,
        Module *pModule,
        BOOL *bObjectCreated);

    static void ReadArray(
        Assembly *pCtorAssembly,
        CorSerializationType arrayType,
        int size,
        TypeHandle th,
        BYTE **pBlob,
        const BYTE *endBlob,
        Module *pModule,
        BASEARRAYREF *pArray);

    static int GetStringSize(
        BYTE **pBlob,
        const BYTE *endBlob);

    template < typename T >
    static BOOL CopyArrayVAL(
        BASEARRAYREF pArray,
        int nElements,
        BYTE **pBlob,
        const BYTE *endBlob);
};

#endif

