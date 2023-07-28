// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _CUSTOMATTRIBUTE_H_
#define _CUSTOMATTRIBUTE_H_

#include "fcall.h"
#include "../md/compiler/custattr.h"

struct CustomAttributeType;
struct CustomAttributeValue;
struct CustomAttributeArgument;
struct CustomAttributeNamedArgument;

typedef Array<CustomAttributeArgument> CaArgArray;
typedef Array<CustomAttributeNamedArgument> CaNamedArgArray;
typedef Array<CustomAttributeValue> CaValueArray;

typedef DPTR(CaArgArray) PTR_CaArgArray;
typedef DPTR(CaNamedArgArray) PTR_CaNamedArgArray;
typedef DPTR(CaValueArray) PTR_CaValueArray;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<CaArgArray> CaArgArrayREF;
typedef REF<CaNamedArgArray> CaNamedArgArrayREF;
typedef REF<CaValueArray> CaValueArrayREF;
#else
typedef PTR_CaArgArray CaArgArrayREF;
typedef PTR_CaNamedArgArray CaNamedArgArrayREF;
typedef PTR_CaValueArray CaValueArrayREF;
#endif


#include <pshpack1.h>
struct CustomAttributeType
{
    STRINGREF m_enumName;
    CorSerializationType m_tag;
    CorSerializationType m_enumType;
    CorSerializationType m_arrayType;
    CorSerializationType m_padding;
};

struct CustomAttributeValue
{
#ifdef HOST_64BIT
    // refs come before longs on win64
    CaValueArrayREF     m_value;
    STRINGREF           m_enumOrTypeName;
    INT64               m_rawValue;
#else
    // longs come before refs on x86
    INT64               m_rawValue;
    CaValueArrayREF     m_value;
    STRINGREF           m_enumOrTypeName;
#endif
    CustomAttributeType m_type;
#if defined(FEATURE_64BIT_ALIGNMENT)
    DWORD m_padding;
#endif
};

struct CustomAttributeArgument
{
    CustomAttributeType m_type;
#if (!defined(HOST_64BIT) && (DATA_ALIGNMENT > 4)) || defined(FEATURE_64BIT_ALIGNMENT)
    DWORD m_padding;
#endif
    CustomAttributeValue m_value;
};

struct CustomAttributeNamedArgument
{
    STRINGREF m_argumentName;
    CorSerializationType m_propertyOrField;
    CorSerializationType m_padding;
#if !defined(HOST_64BIT) && (DATA_ALIGNMENT > 4)
    DWORD m_padding2;
#endif
    CustomAttributeType m_type;
#if !defined(HOST_64BIT) && (DATA_ALIGNMENT > 4)
    DWORD m_padding3;
#endif
    CustomAttributeValue m_value;
};
#include <poppack.h>


typedef struct {
    STRINGREF string;
    CaValueArrayREF array;
} CustomAttributeManagedValues;

typedef Factory< SArray<CaValue> > CaValueArrayFactory;

class Attribute
{
public:
    static FCDECL5(VOID, ParseAttributeArguments,
        void* pCa,
        INT32 cCa,
        CaArgArrayREF* ppCustomAttributeArguments,
        CaNamedArgArrayREF* ppCustomAttributeNamedArguments,
        AssemblyBaseObject* pAssemblyUNSAFE);

    static HRESULT ParseAttributeArgumentValues(
        void* pCa,
        INT32 cCa,
        CaValueArrayFactory* pCaValueArrayFactory,
        CaArg* pCaArgs,
        COUNT_T cArgs,
        CaNamedArg* pCaNamedArgs,
        COUNT_T cNamedArgs,
        DomainAssembly* pDomainAssembly);

private:
    static HRESULT ParseCaValue(
        CustomAttributeParser &ca,
        CaValue* pCaArg,
        CaType* pCaParam,
        CaValueArrayFactory* pCaValueArrayFactory,
        DomainAssembly* pDomainAssembly);

    static HRESULT ParseCaCtorArgs(
        CustomAttributeParser &ca,
        CaArg* pArgs,
        ULONG cArgs,
        CaValueArrayFactory* pCaValueArrayFactory,
        DomainAssembly* pDomainAssembly);

    static HRESULT ParseCaNamedArgs(
        CustomAttributeParser &ca, // The Custom Attribute blob.
        CaNamedArg *pNamedParams,  // Array of argument descriptors.
        ULONG cNamedParams,
        CaValueArrayFactory* pCaValueArrayFactory,
        DomainAssembly* pDomainAssembly);

    static HRESULT InitCaType(
        CustomAttributeType* pType,
        Factory<SString>* pSstringFactory,
        CaType* pCaType);

    static HRESULT ParseCaType(
        CustomAttributeParser &ca,
        CaType* pCaType,
        DomainAssembly* pDomainAssembly,
        StackSString* ss = NULL);

    static TypeHandle GetTypeForEnum(
        LPCUTF8 szEnumName,
        COUNT_T cbEnumName,
        DomainAssembly* pDomainAssembly);

    static void SetBlittableCaValue(
        CustomAttributeValue* pVal,
        CaValue* pCaVal,
        BOOL* pbAllBlittableCa);

    static void SetManagedValue(
        CustomAttributeManagedValues gc,
        CustomAttributeValue* pValue);

    static CustomAttributeManagedValues GetManagedCaValue(CaValue* pCaVal);
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

