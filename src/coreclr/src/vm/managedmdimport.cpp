// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "mlinfo.h"
#include "managedmdimport.hpp"
#include "wrappers.h"

void ThrowMetaDataImportException(HRESULT hr)
{
    WRAPPER_NO_CONTRACT;

    if (hr == CLDB_E_RECORD_NOTFOUND)
        return;

    MethodDescCallSite throwError(METHOD__METADATA_IMPORT__THROW_ERROR);

    ARG_SLOT args[] = { (ARG_SLOT)hr };
    throwError.Call(args);
}

//
// MetaDataImport
//
extern BOOL ParseNativeTypeInfo(NativeTypeParamInfo* pInfo, PCCOR_SIGNATURE pvNativeType, ULONG cbNativeType);

FCIMPL11(void, MetaDataImport::GetMarshalAs,
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
    INT32*          iidParamIndex)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    {
        NativeTypeParamInfo info;

        ZeroMemory(&info, sizeof(NativeTypeParamInfo));

        if (!ParseNativeTypeInfo(&info, pvNativeType, cbNativeType))
        {
            ThrowMetaDataImportException(E_FAIL);
        }

        *unmanagedType = info.m_NativeType;
        *sizeParamIndex = info.m_CountParamIdx;
        *sizeConst = info.m_Additive;
        *arraySubType = info.m_ArrayElementType;

#ifdef FEATURE_COMINTEROP
        *iidParamIndex = info.m_IidParamIndex;

        *safeArraySubType = info.m_SafeArrayElementVT;

        *safeArrayUserDefinedSubType = info.m_strSafeArrayUserDefTypeName == NULL ? NULL :
            StringObject::NewString(info.m_strSafeArrayUserDefTypeName, info.m_cSafeArrayUserDefTypeNameBytes);
#else
        *iidParamIndex = 0;

        *safeArraySubType = VT_EMPTY;

        *safeArrayUserDefinedSubType = NULL;
#endif

        *marshalType = info.m_strCMMarshalerTypeName == NULL ? NULL :
            StringObject::NewString(info.m_strCMMarshalerTypeName, info.m_cCMMarshalerTypeNameBytes);

        *marshalCookie = info.m_strCMCookie == NULL ? NULL :
            StringObject::NewString(info.m_strCMCookie, info.m_cCMCookieStrBytes);
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

MDImpl4(Object *, MetaDataImport::GetDefaultValue, mdToken tk, INT64* pDefaultValue, INT32* pLength, INT32* pCorElementType)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    Object *pRetVal = NULL;

    IMDInternalImport *_pScope = pScope;

    MDDefaultValue value;
    IfFailGo(_pScope->GetDefaultValue(tk, &value));

    // We treat string values differently. That's because on big-endian architectures we can't return a
    // pointer to static string data in the metadata, we have to buffer the string in order to byte-swap
    // all the unicode characters. MDDefaultValue therefore has a destructor on big-endian machines which
    // reclaims this buffer, implying we can't safely return the embedded pointer to managed code.
    // The easiest thing for us to do is to construct the managed string object here, in the context of
    // the still valid MDDefaultValue. We can't return a managed object via the normal out parameter
    // because it won't be GC protected, so in this special case null the output parameter and return
    // the string via the protected return result (which is null for all other cases).
    if (value.m_bType == ELEMENT_TYPE_STRING)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_0();
        *pDefaultValue = 0;
        STRINGREF refRetval = StringObject::NewString(value.m_wzValue, value.m_cbSize / sizeof(WCHAR));
        pRetVal = STRINGREFToObject(refRetval);
        HELPER_METHOD_FRAME_END();
    }
    else
    {
        *pDefaultValue = value.m_ullValue;
    }

    *pCorElementType = (UINT32)value.m_bType;
    *pLength = (INT32)value.m_cbSize;
ErrExit:
    if (FAILED(hr))
    {
        FCThrow(kBadImageFormatException);
    }

    return pRetVal;
}
FCIMPLEND

MDImpl3(void, MetaDataImport::GetCustomAttributeProps, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport *_pScope = pScope;

    IfFailGo(_pScope->GetCustomAttributeProps(cv, ptkType));
    IfFailGo(_pScope->GetCustomAttributeAsBlob(cv, (const void **)&ppBlob->m_array, (ULONG *)&ppBlob->m_count));
ErrExit:
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

static int * EnsureResultSize(MetadataEnumResult * pResult, ULONG length)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int * p;

    if (length >= NumItems(pResult->smallResult) || DbgRandomOnExe(.01))
    {
        pResult->largeResult = (I4Array *)OBJECTREFToObject(AllocatePrimitiveArray(ELEMENT_TYPE_I4, length));
        p = pResult->largeResult->GetDirectPointerToNonObjectElements();
    }
    else
    {
        ZeroMemory(pResult->smallResult, sizeof(pResult->smallResult));
        p = pResult->smallResult;
    }

    pResult->length = length;
    return p;
}

MDImpl3(void, MetaDataImport::Enum, mdToken type, mdToken tkParent, MetadataEnumResult * pResult)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(pResult != NULL);
    }
    CONTRACTL_END;

    HELPER_METHOD_FRAME_BEGIN_0();
    {
        IMDInternalImport *_pScope = pScope;

        if (type == mdtTypeDef)
        {
            ULONG nestedClassesCount;
            IfFailThrow(_pScope->GetCountNestedClasses(tkParent, &nestedClassesCount));

            mdTypeDef* arToken = (mdTypeDef*)EnsureResultSize(pResult, nestedClassesCount);
            IfFailThrow(_pScope->GetNestedClasses(tkParent, arToken, nestedClassesCount, &nestedClassesCount));
        }
        else if (type == mdtMethodDef && (TypeFromToken(tkParent) == mdtProperty || TypeFromToken(tkParent) == mdtEvent))
        {
            HENUMInternalHolder hEnum(pScope);
            hEnum.EnumAssociateInit(tkParent);

            ULONG associatesCount = hEnum.EnumGetCount();

            static_assert_no_msg(sizeof(ASSOCIATE_RECORD) == 2 * sizeof(int));

            ASSOCIATE_RECORD* arAssocRecord = (ASSOCIATE_RECORD*)EnsureResultSize(pResult, 2 * associatesCount);
            IfFailThrow(_pScope->GetAllAssociates(&hEnum, arAssocRecord, associatesCount));
        }
        else
        {
            HENUMInternalHolder hEnum(pScope);
            hEnum.EnumInit(type, tkParent);

            ULONG count = hEnum.EnumGetCount();

            mdToken* arToken = (mdToken*)EnsureResultSize(pResult, count);
            for(COUNT_T i = 0; i < count && _pScope->EnumNext(&hEnum, &arToken[i]); i++);
        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("y", on)		// Small critical routines, don't put in EBP frame
#endif

MDImpl1(FC_BOOL_RET, MetaDataImport::IsValidToken, mdToken tk)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    FC_RETURN_BOOL(_pScope->IsValidToken(tk));
}
FCIMPLEND


MDImpl3(void, MetaDataImport::GetClassLayout, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    {
        IMDInternalImport *_pScope = pScope;

        if (pdwPackSize != NULL)
        {
            hr = _pScope->GetClassPackSize(td, (ULONG *)pdwPackSize);
            if (hr == CLDB_E_RECORD_NOTFOUND)
            {
                *pdwPackSize = 0;
                hr = S_OK;
            }
            IfFailGo(hr);
        }

        if (pulClassSize != NULL)
        {
            hr = _pScope->GetClassTotalSize(td, pulClassSize);
            if (hr == CLDB_E_RECORD_NOTFOUND)
            {
                *pulClassSize = 0;
                hr = S_OK;
            }
            IfFailGo(hr);
        }
    }
ErrExit:
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl3(FC_BOOL_RET, MetaDataImport::GetFieldOffset, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffset)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport *_pScope = pScope;
    MD_CLASS_LAYOUT layout;
    BOOL retVal = FALSE;

    IfFailGo(_pScope->GetClassLayoutInit(td, &layout));

    ULONG cFieldOffset;
    cFieldOffset = layout.m_ridFieldEnd - layout.m_ridFieldCur;

    for (COUNT_T i = 0; i < cFieldOffset; i ++)
    {
        mdFieldDef fd;
        ULONG offset;
        IfFailGo(_pScope->GetClassLayoutNext(&layout, &fd, &offset));

        if (fd == target)
        {
            *pdwFieldOffset = offset;
            retVal = TRUE;
            break;
        }
    }
ErrExit:
    if (FAILED(hr))
    {
        FCThrow(kBadImageFormatException);
    }
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

MDImpl3(void, MetaDataImport::GetUserString, mdToken tk, LPCSTR* pszName, ULONG* pCount)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;
    BOOL bHasExtendedChars;

    hr = _pScope->GetUserString(tk, pCount, &bHasExtendedChars, (LPCWSTR *)pszName);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetName, mdToken tk, LPCSTR* pszName)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport *_pScope = pScope;

    if (TypeFromToken(tk) == mdtMethodDef)
    {
        hr = _pScope->GetNameOfMethodDef(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtParamDef)
    {
        USHORT seq;
        DWORD attr;
        hr = _pScope->GetParamDefProps(tk, &seq, &attr, pszName);
    }
    else if (TypeFromToken(tk) == mdtFieldDef)
    {
        hr = _pScope->GetNameOfFieldDef(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtProperty)
    {
        hr = _pScope->GetPropertyProps(tk, pszName, NULL, NULL, NULL);
    }
    else if (TypeFromToken(tk) == mdtEvent)
    {
        hr = _pScope->GetEventProps(tk, pszName, NULL, NULL);
    }
    else if (TypeFromToken(tk) == mdtModule)
    {
        hr = _pScope->GetModuleRefProps(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtTypeDef)
    {
        LPCSTR szNamespace = NULL;
        hr = _pScope->GetNameOfTypeDef(tk, pszName, &szNamespace);
    }
    else
    {
        hr = E_FAIL;
    }

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetNamespace, mdToken tk, LPCSTR* pszName)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;
    LPCSTR szName = NULL;

    hr = _pScope->GetNameOfTypeDef(tk, &szName, pszName);

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND


MDImpl2(void, MetaDataImport::GetGenericParamProps, mdToken tk, DWORD* pAttributes)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetGenericParamProps(tk, NULL, pAttributes, NULL, NULL, NULL);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl3(void, MetaDataImport::GetEventProps, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetEventProps(tk, pszName, (DWORD*)pdwEventFlags, NULL);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl4(void, MetaDataImport::GetPinvokeMap, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;
    mdModule tkModule;

    hr = _pScope->GetPinvokeMap(tk, pMappingFlags, pszImportName, &tkModule);
    if (FAILED(hr))
    {
        *pMappingFlags = 0;
        *pszImportName = NULL;
        *pszImportDll = NULL;
        hr = S_OK;
    }
    else
    {
        hr = _pScope->GetModuleRefProps(tkModule, pszImportDll);
    }

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl3(void, MetaDataImport::GetParamDefProps, mdToken tk, INT32* pSequence, INT32* pAttributes)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;
    USHORT usSequence = 0;

    // Is this a valid token?
    if (_pScope->IsValidToken((mdParamDef)tk))
    {
        LPCSTR szParamName;
        hr = _pScope->GetParamDefProps(tk, &usSequence, (DWORD *)pAttributes, &szParamName);
    }
    else
    {
        // Invalid token - throw an exception
        hr = COR_E_BADIMAGEFORMAT;
    }
    *pSequence = (INT32) usSequence;

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetFieldDefProps, mdToken tk, INT32 *pdwFieldFlags)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetFieldDefProps(tk, (DWORD *)pdwFieldFlags);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl4(void, MetaDataImport::GetPropertyProps, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetPropertyProps(tk, pszName, (DWORD*)pdwPropertyFlags, (PCCOR_SIGNATURE*)&ppValue->m_array, (ULONG*)&ppValue->m_count);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetFieldMarshal, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetFieldMarshal(tk, (PCCOR_SIGNATURE *)&ppValue->m_array, (ULONG *)&ppValue->m_count);
    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        ppValue->m_array = NULL;
        ppValue->m_count = 0;
        hr = S_OK;
    }

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetSigOfMethodDef, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetSigOfMethodDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetSignatureFromToken, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetSigFromToken(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&(ppValue->m_array));
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetSigOfFieldDef, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    hr = _pScope->GetSigOfFieldDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl2(void, MetaDataImport::GetParentToken, mdToken tk, mdToken* ptk)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;

    switch (TypeFromToken(tk))
    {
    case mdtTypeDef:
        hr = _pScope->GetNestedClassProps(tk, ptk);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            *ptk = mdTypeDefNil;
            hr = S_OK;
        }
        break;

    case mdtGenericParam:
        hr = _pScope->GetGenericParamProps(tk, NULL, NULL, ptk, NULL, NULL);
        break;

    case mdtMethodDef:
    case mdtMethodSpec:
    case mdtFieldDef:
    case mdtParamDef:
    case mdtMemberRef:
    case mdtCustomAttribute:
    case mdtEvent:
    case mdtProperty:
        hr = _pScope->GetParentToken(tk, ptk);
        break;

    default:
        hr = COR_E_BADIMAGEFORMAT;
        break;
    }

    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

MDImpl1(void, MetaDataImport::GetScopeProps, GUID* pmvid)
{
    FCALL_CONTRACT;

    HRESULT hr;
    LPCSTR szName;

    IMDInternalImport *_pScope = pScope;
    hr = _pScope->GetScopeProps(&szName, pmvid);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND


MDImpl2(void, MetaDataImport::GetMemberRefProps,
    mdMemberRef mr,
    ConstArray* ppvSigBlob)
{
    FCALL_CONTRACT;

    HRESULT hr;
    IMDInternalImport *_pScope = pScope;
    LPCSTR szName_Ignore;

    hr = _pScope->GetNameAndSigOfMemberRef(mr, (PCCOR_SIGNATURE*)&ppvSigBlob->m_array, (ULONG*)&ppvSigBlob->m_count, &szName_Ignore);
    if (FAILED(hr))
    {
        FCThrowVoid(kBadImageFormatException);
    }
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("", on)			// restore command line optimization defaults
#endif

