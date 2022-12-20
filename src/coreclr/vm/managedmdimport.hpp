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

typedef struct
{
    I4Array * largeResult;
    int length;
    int smallResult[16];
} MetadataEnumResult;

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
    //
    // GetXXXProps
    //
    MDDecl1(void, GetScopeProps, GUID* pmvid);
    MDDecl4(void, GetTypeDefProps, mdTypeDef td, STRINGREF* pszTypeDef, DWORD* pdwTypeDefFlags, mdToken* ptkExtends);
    MDDecl2(void, GetMemberRefProps, mdMemberRef mr, ConstArray* ppvSigBlob);


    ////
    //// EnumXXX
    ////
    MDDecl3(void, Enum, mdToken type, mdToken tkParent, MetadataEnumResult * pResult);
    MDDecl3(void, GetCustomAttributeProps, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob);

    ////
    //// Misc
    ////

    MDDecl4(Object *, GetDefaultValue, mdToken tk, INT64* pDefaultValue, INT32* pLength, INT32* pCorElementType);
    MDDecl2(void, GetName, mdToken tk, LPCSTR* pszName);
    MDDecl3(void, GetUserString, mdToken tk, LPCSTR* pszName, ULONG* pCount);
    MDDecl2(void, GetNamespace, mdToken tk, LPCSTR* pszName);
    MDDecl2(void, GetParentToken, mdToken tk, mdToken* ptk);
    MDDecl3(void, GetParamDefProps, mdToken tk, INT32* pSequence, INT32* pAttributes);
    MDDecl4(void, GetPinvokeMap, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll);

    MDDecl3(void, GetClassLayout, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize);
    MDDecl3(FC_BOOL_RET, GetFieldOffset, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffset);

    MDDecl3(void, GetEventProps, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags);
    MDDecl2(void, GetGenericParamProps, mdToken tk, DWORD* pAttributes);
    MDDecl2(void, GetFieldDefProps, mdToken tk, INT32 *pdwFieldFlags);
    MDDecl4(void, GetPropertyProps, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppvSigBlob);

    MDDecl2(void, GetSignatureFromToken, mdToken tk, ConstArray* pSig);
    MDDecl2(void, GetSigOfFieldDef, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl2(void, GetSigOfMethodDef, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl2(void, GetFieldMarshal, mdToken tk, ConstArray* pMarshalInfo);
    MDDecl2(mdParamDef, GetParamForMethodIndex, mdMethodDef md, ULONG ulParamSeq);
    MDDecl1(FC_BOOL_RET, IsValidToken, mdToken tk);
    MDDecl1(mdTypeDef, GetNestedClassProps, mdTypeDef tdNestedClass);
    MDDecl1(ULONG, GetNativeCallConvFromSig, ConstArray sig);

    static FCDECL11(void, GetMarshalAs,
        BYTE*           pvNativeType,
        ULONG           cbNativeType,
        INT32*          unmanagedType,
        INT32*          safeArraySubType,
        STRINGREF*      safeArrayUserDefinedSubType,
        INT32*          arraySubType,
        INT32*          sizeParamIndex,
        INT32*          sizeConst,
        STRINGREF*      marshalType,
        STRINGREF*      marshalCookie,
        INT32*          iidParamIndex);
};

#endif
