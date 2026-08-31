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
    FCDECL1(static IMDInternalImport*, GetMetadataImport, ReflectModuleBaseObject* pModuleUNSAFE);
    FCDECL2(static HRESULT, GetScopeProps, IMDInternalImport* pScope, GUID* pmvid);
    FCDECL3(static HRESULT, GetMemberRefProps, IMDInternalImport* pScope, mdMemberRef mr, ConstArray* ppvSigBlob);

    FCDECL4(static HRESULT, GetCustomAttributeProps, IMDInternalImport* pScope, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob);

    FCDECL6(static HRESULT, GetDefaultValue, IMDInternalImport* pScope, mdToken tk, INT64* pDefaultValue, LPCWSTR* pStringValue, INT32* pLength, INT32* pCorElementType);
    FCDECL3(static HRESULT, GetName, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName);
    FCDECL4(static HRESULT, GetUserString, IMDInternalImport* pScope, mdToken tk, LPCWSTR* pszName, ULONG* pCount);
    FCDECL3(static HRESULT, GetNamespace, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName);
    FCDECL3(static HRESULT, GetParentToken, IMDInternalImport* pScope, mdToken tk, mdToken* ptk);
    FCDECL4(static HRESULT, GetParamDefProps, IMDInternalImport* pScope, mdToken tk, INT32* pSequence, INT32* pAttributes);
    FCDECL5(static HRESULT, GetPInvokeMap, IMDInternalImport* pScope, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll);

    FCDECL4(static HRESULT, GetClassLayout, IMDInternalImport* pScope, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize);
    FCDECL5(static HRESULT, GetFieldOffset, IMDInternalImport* pScope, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffsetGetFieldOffset, CLR_BOOL* found);

    FCDECL4(static HRESULT, GetEventProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags);
    FCDECL3(static HRESULT, GetGenericParamProps, IMDInternalImport* pScope, mdToken tk, DWORD* pAttributes);
    FCDECL3(static HRESULT, GetFieldDefProps, IMDInternalImport* pScope, mdToken tk, INT32 *pdwFieldFlags);
    FCDECL5(static HRESULT, GetPropertyProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppvSigBlob);

    FCDECL3(static HRESULT, GetSignatureFromToken, IMDInternalImport* pScope, mdToken tk, ConstArray* pSig);
    FCDECL3(static HRESULT, GetSigOfFieldDef, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    FCDECL3(static HRESULT, GetSigOfMethodDef, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    FCDECL3(static HRESULT, GetFieldMarshal, IMDInternalImport* pScope, mdToken tk, ConstArray* pMarshalInfo);
    FCDECL2(static FC_BOOL_RET, IsValidToken, IMDInternalImport* pScope, mdToken tk);
};

extern "C" BOOL QCALLTYPE MetadataImport_GetMarshalAs(
    BYTE*   pvNativeType,
    ULONG   cbNativeType,
    INT32*  unmanagedType,
    INT32*  safeArraySubType,
    LPUTF8* safeArrayUserDefinedSubType,
    INT32*  safeArrayUserDefinedSubTypeLength,
    INT32*  arraySubType,
    INT32*  sizeParamIndex,
    INT32*  sizeConst,
    LPUTF8* marshalType,
    INT32*  marshalTypeLength,
    LPUTF8* marshalCookie,
    INT32*  marshalCookieLength,
    INT32*  iidParamIndex);

extern "C" void QCALLTYPE MetadataImport_Enum(
    IMDInternalImport* pScope,
    mdToken type,
    mdToken tkParent,
    INT32* length,
    INT32* shortResult,
    QCall::ObjectHandleOnStack longResult);

#endif
