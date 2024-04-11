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

#define MDDecl0(RET, NAME) static FCDECL1(RET, NAME, IMDInternalImport* pScope)
#define MDDecl1(RET, NAME, arg0) static FCDECL2(RET, NAME, IMDInternalImport* pScope, arg0)
#define MDDecl2(RET, NAME, arg0, arg1) static FCDECL3(RET, NAME, IMDInternalImport* pScope, arg0, arg1)
#define MDDecl3(RET, NAME, arg0, arg1, arg2) static FCDECL4(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2)
#define MDDecl4(RET, NAME, arg0, arg1, arg2, arg3) static FCDECL5(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3)
#define MDDecl5(RET, NAME, arg0, arg1, arg2, arg3, arg4) static FCDECL6(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4)
#define MDDecl6(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5) static FCDECL7(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5)
#define MDDecl7(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6) static FCDECL8(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6)
#define MDDecl8(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) static FCDECL9(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)
#define MDDecl9(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) static FCDECL10(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)
#define MDDecl10(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) static FCDECL11(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)

#define MDImpl0(RET, NAME) FCIMPL1(RET, NAME, IMDInternalImport* pScope)
#define MDImpl1(RET, NAME, arg0) FCIMPL2(RET, NAME, IMDInternalImport* pScope, arg0)
#define MDImpl2(RET, NAME, arg0, arg1) FCIMPL3(RET, NAME, IMDInternalImport* pScope, arg0, arg1)
#define MDImpl3(RET, NAME, arg0, arg1, arg2) FCIMPL4(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2)
#define MDImpl4(RET, NAME, arg0, arg1, arg2, arg3) FCIMPL5(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3)
#define MDImpl5(RET, NAME, arg0, arg1, arg2, arg3, arg4) FCIMPL6(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4)
#define MDImpl6(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5) FCIMPL7(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5)
#define MDImpl7(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6) FCIMPL8(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6)
#define MDImpl8(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7) FCIMPL9(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)
#define MDImpl9(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) FCIMPL10(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)
#define MDImpl10(RET, NAME, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) FCIMPL11(RET, NAME, IMDInternalImport* pScope, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)

class MetaDataImport
{
public:
    MDDecl1(HRESULT, GetScopeProps, GUID* pmvid);
    MDDecl2(HRESULT, GetMemberRefProps, mdMemberRef mr, ConstArray* ppvSigBlob);

    MDDecl3(HRESULT, GetCustomAttributeProps, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob);

    MDDecl5(HRESULT, GetDefaultValue, mdToken tk, INT64* pDefaultValue, BYTE** pStringValue, INT32* pLength, INT32* pCorElementType);
    MDDecl2(HRESULT, GetName, mdToken tk, LPCSTR* pszName);
    MDDecl3(HRESULT, GetUserString, mdToken tk, BYTE** pszName, ULONG* pCount);
    MDDecl2(HRESULT, GetNamespace, mdToken tk, LPCSTR* pszName);
    MDDecl2(HRESULT, GetParentToken, mdToken tk, mdToken* ptk);
    MDDecl3(HRESULT, GetParamDefProps, mdToken tk, INT32* pSequence, INT32* pAttributes);
    MDDecl4(HRESULT, GetPInvokeMap, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll);

    MDDecl3(HRESULT, GetClassLayout, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize);
    MDDecl4(FC_BOOL_RET, GetFieldOffset, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffsetGetFieldOffset, HRESULT* err);

    MDDecl3(HRESULT, GetEventProps, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags);
    MDDecl2(HRESULT, GetGenericParamProps, mdToken tk, DWORD* pAttributes);
    MDDecl2(HRESULT, GetFieldDefProps, mdToken tk, INT32 *pdwFieldFlags);
    MDDecl4(HRESULT, GetPropertyProps, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppvSigBlob);

    MDDecl2(HRESULT, GetSignatureFromToken, mdToken tk, ConstArray* pSig);
    MDDecl2(HRESULT, GetSigOfFieldDef, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl2(HRESULT, GetSigOfMethodDef, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl2(HRESULT, GetFieldMarshal, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl1(FC_BOOL_RET, IsValidToken, mdToken tk);

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
