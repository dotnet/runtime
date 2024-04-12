// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "mlinfo.h"
#include "managedmdimport.hpp"
#include "wrappers.h"

//
// MetaDataImport
//
extern BOOL ParseNativeTypeInfo(NativeTypeParamInfo* pInfo, PCCOR_SIGNATURE pvNativeType, ULONG cbNativeType);

FCIMPL11(FC_BOOL_RET, MetaDataImport::GetMarshalAs,
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
    INT32*  iidParamIndex)
{
    FCALL_CONTRACT;

    NativeTypeParamInfo info{};

    // The zeroing out of memory is important. The Reflection API's
    // instantiation of MarshalAsAttribute doesn't reflect the default
    // values the interop subsystem uses. This means NativeTypeParamInfo's
    // constructor initialization values need to be overridden by zero
    // initialization.
    ZeroMemory(&info, sizeof(info));
    if (!ParseNativeTypeInfo(&info, pvNativeType, cbNativeType))
    {
        FC_RETURN_BOOL(FALSE);
    }

    *unmanagedType = info.m_NativeType;
    *sizeParamIndex = info.m_CountParamIdx;
    *sizeConst = info.m_Additive;
    *arraySubType = info.m_ArrayElementType;

#ifdef FEATURE_COMINTEROP
    *iidParamIndex = info.m_IidParamIndex;

    *safeArraySubType = info.m_SafeArrayElementVT;

    *safeArrayUserDefinedSubType = info.m_strSafeArrayUserDefTypeName;
#else
    *iidParamIndex = 0;

    *safeArraySubType = VT_EMPTY;

    *safeArrayUserDefinedSubType = NULL;
#endif

    *marshalType = info.m_strCMMarshalerTypeName;

    *marshalCookie = info.m_strCMCookie;

    FC_RETURN_BOOL(TRUE);
}
FCIMPLEND

MDImpl5(HRESULT, MetaDataImport::GetDefaultValue, mdToken tk, INT64* pDefaultValue, BYTE** pStringValue, INT32* pLength, INT32* pCorElementType)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    IMDInternalImport *_pScope = pScope;

    MDDefaultValue value;
    IfFailGo(_pScope->GetDefaultValue(tk, &value));

    if (value.m_bType == ELEMENT_TYPE_STRING)
    {
        *pDefaultValue = 0;
        *pStringValue = (BYTE*)value.m_wzValue;
    }
    else
    {
        *pDefaultValue = value.m_ullValue;
        *pStringValue = NULL;
    }

    *pLength = (INT32)value.m_cbSize;
    *pCorElementType = (UINT32)value.m_bType;

ErrExit:
    return hr;
}
FCIMPLEND

MDImpl3(HRESULT, MetaDataImport::GetCustomAttributeProps, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport *_pScope = pScope;

    IfFailGo(_pScope->GetCustomAttributeProps(cv, ptkType));
    IfFailGo(_pScope->GetCustomAttributeAsBlob(cv, (const void **)&ppBlob->m_array, (ULONG *)&ppBlob->m_count));
ErrExit:
    return hr;
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("y", on)   // Small critical routines, don't put in EBP frame
#endif

MDImpl1(FC_BOOL_RET, MetaDataImport::IsValidToken, mdToken tk)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    FC_RETURN_BOOL(_pScope->IsValidToken(tk));
}
FCIMPLEND

MDImpl3(HRESULT, MetaDataImport::GetClassLayout, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize)
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
    return hr;
}
FCIMPLEND

MDImpl4(FC_BOOL_RET, MetaDataImport::GetFieldOffset, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffset, HRESULT* err)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    *err = S_OK;
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
        *err = hr;
    }
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

MDImpl3(HRESULT, MetaDataImport::GetUserString, mdToken tk, BYTE** pszName, ULONG* pCount)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;
    BOOL bHasExtendedChars;

    return _pScope->GetUserString(tk, pCount, &bHasExtendedChars, (LPCWSTR *)pszName);
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetName, mdToken tk, LPCSTR* pszName)
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

    return hr;
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetNamespace, mdToken tk, LPCSTR* pszName)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;
    LPCSTR szName = NULL;

    return _pScope->GetNameOfTypeDef(tk, &szName, pszName);
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetGenericParamProps, mdToken tk, DWORD* pAttributes)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetGenericParamProps(tk, NULL, pAttributes, NULL, NULL, NULL);
}
FCIMPLEND

MDImpl3(HRESULT, MetaDataImport::GetEventProps, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetEventProps(tk, pszName, (DWORD*)pdwEventFlags, NULL);
}
FCIMPLEND

MDImpl4(HRESULT, MetaDataImport::GetPInvokeMap, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll)
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
    return hr;
}
FCIMPLEND

MDImpl3(HRESULT, MetaDataImport::GetParamDefProps, mdToken tk, INT32* pSequence, INT32* pAttributes)
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

    return hr;
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetFieldDefProps, mdToken tk, INT32 *pdwFieldFlags)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetFieldDefProps(tk, (DWORD *)pdwFieldFlags);
}
FCIMPLEND

MDImpl4(HRESULT, MetaDataImport::GetPropertyProps, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetPropertyProps(tk, pszName, (DWORD*)pdwPropertyFlags, (PCCOR_SIGNATURE*)&ppValue->m_array, (ULONG*)&ppValue->m_count);
 }
 FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetFieldMarshal, mdToken tk, ConstArray* ppValue)
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

    return hr;
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetSigOfMethodDef, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetSigOfMethodDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetSignatureFromToken, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetSigFromToken(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&(ppValue->m_array));
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetSigOfFieldDef, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;

    return _pScope->GetSigOfFieldDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
}
FCIMPLEND

MDImpl2(HRESULT, MetaDataImport::GetParentToken, mdToken tk, mdToken* ptk)
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

    return hr;
}
FCIMPLEND

MDImpl1(HRESULT, MetaDataImport::GetScopeProps, GUID* pmvid)
{
    FCALL_CONTRACT;

    LPCSTR szName;
    IMDInternalImport *_pScope = pScope;
    return _pScope->GetScopeProps(&szName, pmvid);
}
FCIMPLEND


MDImpl2(HRESULT, MetaDataImport::GetMemberRefProps,
    mdMemberRef mr,
    ConstArray* ppvSigBlob)
{
    FCALL_CONTRACT;

    IMDInternalImport *_pScope = pScope;
    LPCSTR szName_Ignore;

    return _pScope->GetNameAndSigOfMemberRef(mr, (PCCOR_SIGNATURE*)&ppvSigBlob->m_array, (ULONG*)&ppvSigBlob->m_count, &szName_Ignore);
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("", on)    // restore command line optimization defaults
#endif

struct ResultMemory final
{
    INT32 length;
    INT32* alloc;

    ~ResultMemory()
    {
        if (alloc != NULL)
            ::free(alloc);
    }

    void AllocateManagedArray(QCall::ObjectHandleOnStack& longResult)
    {
        CONTRACTL
        {
            THROWS;
            MODE_PREEMPTIVE;
            PRECONDITION(alloc != NULL);
        }
        CONTRACTL_END;

        {
            GCX_COOP();
            longResult.Set(AllocatePrimitiveArray(ELEMENT_TYPE_I4, length));
            void* p = ((I4Array*)OBJECTREFToObject(longResult.Get()))->GetDirectPointerToNonObjectElements();
            memcpyNoGCRefs(p, alloc, length * sizeof(INT32));
        }
    }
};

static void* EnsureResultSize(
    INT32 resultLength,
    INT32 shortResultLen,
    INT32* shortResult,
    ResultMemory& resultMemory)
{
    CONTRACT(void*)
    {
        THROWS;
        MODE_PREEMPTIVE;
        PRECONDITION(shortResultLen > 0);
        PRECONDITION(shortResult != NULL);
        POSTCONDITION((RETVAL != NULL));
    }
    CONTRACT_END;

    void* p;
    INT32 resultInBytes;
    if (resultLength <= shortResultLen)
    {
        resultInBytes = shortResultLen * sizeof(INT32);
        p = shortResult;
    }
    else
    {
        _ASSERTE(resultLength > 0);
        resultInBytes = resultLength * sizeof(INT32);

        resultMemory.length = resultLength;
        resultMemory.alloc = (INT32*)::malloc(resultInBytes);
        if (resultMemory.alloc == NULL)
            ThrowOutOfMemory();
        p = resultMemory.alloc;
    }
    ZeroMemory(p, resultInBytes);
    RETURN p;
}

extern "C" void QCALLTYPE MetadataImport_Enum(
    IMDInternalImport* pScope,
    mdToken type,
    mdToken tkParent,
    /* in/out */ INT32* length,
    INT32* shortResult,
    QCall::ObjectHandleOnStack longResult)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pScope != NULL);
        PRECONDITION(length != NULL);
        PRECONDITION(shortResult != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    IMDInternalImport *_pScope = pScope;

    ResultMemory memory{};
    ULONG resultLength;
    INT32 shortResultLen = *length;
    if (type == mdtTypeDef)
    {
        IfFailThrow(_pScope->GetCountNestedClasses(tkParent, &resultLength));

        mdTypeDef* arToken = (mdTypeDef*)EnsureResultSize(resultLength, shortResultLen, shortResult, memory);
        IfFailThrow(_pScope->GetNestedClasses(tkParent, arToken, resultLength, &resultLength));
    }
    else if (type == mdtMethodDef && (TypeFromToken(tkParent) == mdtProperty || TypeFromToken(tkParent) == mdtEvent))
    {
        HENUMInternalHolder hEnum(pScope);
        hEnum.EnumAssociateInit(tkParent);

        ULONG associatesCount = hEnum.EnumGetCount();

        // The ASSOCIATE_RECORD is a pair of integers.
        // This means we require a size of 2x the returned length.
        resultLength = associatesCount * 2;
        static_assert_no_msg(sizeof(ASSOCIATE_RECORD) == 2 * sizeof(INT32));

        ASSOCIATE_RECORD* arAssocRecord = (ASSOCIATE_RECORD*)EnsureResultSize(resultLength, shortResultLen, shortResult, memory);
        IfFailThrow(_pScope->GetAllAssociates(&hEnum, arAssocRecord, associatesCount));
    }
    else
    {
        HENUMInternalHolder hEnum(pScope);
        hEnum.EnumInit(type, tkParent);

        resultLength = hEnum.EnumGetCount();

        mdToken* arToken = (mdToken*)EnsureResultSize(resultLength, shortResultLen, shortResult, memory);
        for(COUNT_T i = 0; i < resultLength && _pScope->EnumNext(&hEnum, &arToken[i]); i++);
    }

    // If the result was longer than the short, we need to allocate an array.
    if (resultLength > (ULONG)shortResultLen)
        memory.AllocateManagedArray(longResult);

    *length = resultLength;

    END_QCALL;
}
