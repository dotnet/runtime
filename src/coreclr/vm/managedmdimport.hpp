// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////




#ifndef _MANAGEDMDIMPORT_H_
#define _MANAGEDMDIMPORT_H_

#include "corhdr.h"
#include "fcall.h"
#include "runtimehandles.h"

//
// Keep the struct definitions in sync with bcl\system\reflection\mdimport.cs
//

typedef struct
{
    INT32 m_count;
    void* m_array;
} ConstArray;

class MetaDataImport
{
public:
    static FCDECL1(IMDInternalImport*, GetMetadataImport, ReflectModuleBaseObject* pModuleUNSAFE);
    static FCDECL2(HRESULT, GetScopeProps, IMDInternalImport* pScope, GUID* pmvid);
    static FCDECL3(HRESULT, GetMemberRefProps, IMDInternalImport* pScope, mdMemberRef mr, ConstArray* ppvSigBlob);

    static FCDECL4(HRESULT, GetCustomAttributeProps, IMDInternalImport* pScope, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob);

    static FCDECL6(HRESULT, GetDefaultValue, IMDInternalImport* pScope, mdToken tk, INT64* pDefaultValue, LPCWSTR* pStringValue, INT32* pLength, INT32* pCorElementType);
    static FCDECL3(HRESULT, GetName, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName);
    static FCDECL4(HRESULT, GetUserString, IMDInternalImport* pScope, mdToken tk, LPCWSTR* pszName, ULONG* pCount);
    static FCDECL3(HRESULT, GetNamespace, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName);
    static FCDECL3(HRESULT, GetParentToken, IMDInternalImport* pScope, mdToken tk, mdToken* ptk);
    static FCDECL4(HRESULT, GetParamDefProps, IMDInternalImport* pScope, mdToken tk, INT32* pSequence, INT32* pAttributes);
    static FCDECL5(HRESULT, GetPInvokeMap, IMDInternalImport* pScope, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll);

    static FCDECL4(HRESULT, GetClassLayout, IMDInternalImport* pScope, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize);
    static FCDECL5(HRESULT, GetFieldOffset, IMDInternalImport* pScope, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffsetGetFieldOffset, CLR_BOOL* found);

    static FCDECL4(HRESULT, GetEventProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags);
    static FCDECL3(HRESULT, GetGenericParamProps, IMDInternalImport* pScope, mdToken tk, DWORD* pAttributes);
    static FCDECL3(HRESULT, GetFieldDefProps, IMDInternalImport* pScope, mdToken tk, INT32 *pdwFieldFlags);
    static FCDECL5(HRESULT, GetPropertyProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppvSigBlob);

    static FCDECL3(HRESULT, GetSignatureFromToken, IMDInternalImport* pScope, mdToken tk, ConstArray* pSig);
    static FCDECL3(HRESULT, GetSigOfFieldDef, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    static FCDECL3(HRESULT, GetSigOfMethodDef, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    static FCDECL3(HRESULT, GetFieldMarshal, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    static FCDECL2(FC_BOOL_RET, IsValidToken, IMDInternalImport* pScope, mdToken tk);

    static FCDECL11(FC_BOOL_RET, GetMarshalAs,
        BYTE*   pvNativeType,
        ULONG   cbNativeType,
        INT32*  unmanagedType,
        INT32*  safeArraySubType,
        LPUTF8* safeArrayUserDefinedSubType,
        INT32*  arraySubType,
        INT32*  sizeParamIndex,
        INT32*  sizeConst,
        LPUTF8* marshalType,
        LPUTF8* marshalCookie,
        INT32*  iidParamIndex);
};

extern "C" void QCALLTYPE MetadataImport_Enum(
    IMDInternalImport* pScope,
    mdToken type,
    mdToken tkParent,
    INT32* length,
    INT32* shortResult,
    QCall::ObjectHandleOnStack longResult);

#endif
