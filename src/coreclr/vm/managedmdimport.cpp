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

FCIMPL1(IMDInternalImport*, MetaDataImport::GetMetadataImport, ReflectModuleBaseObject * pModuleUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);
    Module *pModule = refModule->GetModule();
    return pModule->GetMDImport();
}
FCIMPLEND

FCIMPL6(HRESULT, MetaDataImport::GetDefaultValue, IMDInternalImport* pScope, mdToken tk, INT64* pDefaultValue, LPCWSTR* pStringValue, INT32* pLength, INT32* pCorElementType)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    MDDefaultValue value;
    IfFailGo(pScope->GetDefaultValue(tk, &value));

    if (value.m_bType == ELEMENT_TYPE_STRING)
    {
        *pDefaultValue = 0;
        *pStringValue = value.m_wzValue;
        *pLength = (INT32)value.m_cbSize / sizeof(WCHAR); // Length of string in character units
    }
    else
    {
        *pDefaultValue = value.m_ullValue;
        *pStringValue = NULL;
        *pLength = (INT32)value.m_cbSize;
    }
    *pCorElementType = (UINT32)value.m_bType;

ErrExit:
    return hr;
}
FCIMPLEND

FCIMPL4(HRESULT, MetaDataImport::GetCustomAttributeProps, IMDInternalImport* pScope, mdCustomAttribute cv, mdToken* ptkType, ConstArray* ppBlob)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    IfFailGo(pScope->GetCustomAttributeProps(cv, ptkType));
    IfFailGo(pScope->GetCustomAttributeAsBlob(cv, (const void **)&ppBlob->m_array, (ULONG *)&ppBlob->m_count));
ErrExit:
    return hr;
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("y", on)   // Small critical routines, don't put in EBP frame
#endif

FCIMPL2(FC_BOOL_RET, MetaDataImport::IsValidToken, IMDInternalImport* pScope, mdToken tk)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pScope->IsValidToken(tk));
}
FCIMPLEND

FCIMPL4(HRESULT, MetaDataImport::GetClassLayout, IMDInternalImport* pScope, mdTypeDef td, DWORD* pdwPackSize, ULONG* pulClassSize)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    if (pdwPackSize != NULL)
    {
        hr = pScope->GetClassPackSize(td, (ULONG *)pdwPackSize);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            *pdwPackSize = 0;
            hr = S_OK;
        }
        IfFailGo(hr);
    }

    if (pulClassSize != NULL)
    {
        hr = pScope->GetClassTotalSize(td, pulClassSize);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            *pulClassSize = 0;
            hr = S_OK;
        }
        IfFailGo(hr);
    }
ErrExit:
    return hr;
}
FCIMPLEND

FCIMPL5(HRESULT, MetaDataImport::GetFieldOffset, IMDInternalImport* pScope, mdTypeDef td, mdFieldDef target, DWORD* pdwFieldOffset, CLR_BOOL* found)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;
    MD_CLASS_LAYOUT layout;
    *found = FALSE;

    IfFailGo(pScope->GetClassLayoutInit(td, &layout));

    ULONG cFieldOffset;
    cFieldOffset = layout.m_ridFieldEnd - layout.m_ridFieldCur;

    for (COUNT_T i = 0; i < cFieldOffset; i ++)
    {
        mdFieldDef fd;
        ULONG offset;
        IfFailGo(pScope->GetClassLayoutNext(&layout, &fd, &offset));

        if (fd == target)
        {
            *pdwFieldOffset = offset;
            *found = TRUE;
            break;
        }
    }
ErrExit:
    return hr;
}
FCIMPLEND

FCIMPL4(HRESULT, MetaDataImport::GetUserString, IMDInternalImport* pScope, mdToken tk, LPCWSTR* pszName, ULONG* pCount)
{
    FCALL_CONTRACT;

    BOOL bHasExtendedChars;
    return pScope->GetUserString(tk, pCount, &bHasExtendedChars, pszName);
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetName, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName)
{
    FCALL_CONTRACT;

    HRESULT hr = S_OK;

    if (TypeFromToken(tk) == mdtMethodDef)
    {
        hr = pScope->GetNameOfMethodDef(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtParamDef)
    {
        USHORT seq;
        DWORD attr;
        hr = pScope->GetParamDefProps(tk, &seq, &attr, pszName);
    }
    else if (TypeFromToken(tk) == mdtFieldDef)
    {
        hr = pScope->GetNameOfFieldDef(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtProperty)
    {
        hr = pScope->GetPropertyProps(tk, pszName, NULL, NULL, NULL);
    }
    else if (TypeFromToken(tk) == mdtEvent)
    {
        hr = pScope->GetEventProps(tk, pszName, NULL, NULL);
    }
    else if (TypeFromToken(tk) == mdtModule)
    {
        hr = pScope->GetModuleRefProps(tk, pszName);
    }
    else if (TypeFromToken(tk) == mdtTypeDef)
    {
        LPCSTR szNamespace = NULL;
        hr = pScope->GetNameOfTypeDef(tk, pszName, &szNamespace);
    }
    else
    {
        hr = E_FAIL;
    }

    return hr;
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetNamespace, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName)
{
    FCALL_CONTRACT;

    LPCSTR szName = NULL;
    return pScope->GetNameOfTypeDef(tk, &szName, pszName);
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetGenericParamProps, IMDInternalImport* pScope, mdToken tk, DWORD* pAttributes)
{
    FCALL_CONTRACT;

    return pScope->GetGenericParamProps(tk, NULL, pAttributes, NULL, NULL, NULL);
}
FCIMPLEND

FCIMPL4(HRESULT, MetaDataImport::GetEventProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwEventFlags)
{
    FCALL_CONTRACT;

    return pScope->GetEventProps(tk, pszName, (DWORD*)pdwEventFlags, NULL);
}
FCIMPLEND

FCIMPL5(HRESULT, MetaDataImport::GetPInvokeMap, IMDInternalImport* pScope, mdToken tk, DWORD* pMappingFlags, LPCSTR* pszImportName, LPCSTR* pszImportDll)
{
    FCALL_CONTRACT;

    HRESULT hr;
    mdModule tkModule;

    hr = pScope->GetPinvokeMap(tk, pMappingFlags, pszImportName, &tkModule);
    if (FAILED(hr))
    {
        *pMappingFlags = 0;
        *pszImportName = NULL;
        *pszImportDll = NULL;
        hr = S_OK;
    }
    else
    {
        hr = pScope->GetModuleRefProps(tkModule, pszImportDll);
    }
    return hr;
}
FCIMPLEND

FCIMPL4(HRESULT, MetaDataImport::GetParamDefProps, IMDInternalImport* pScope, mdToken tk, INT32* pSequence, INT32* pAttributes)
{
    FCALL_CONTRACT;

    HRESULT hr;
    USHORT usSequence = 0;

    // Is this a valid token?
    if (pScope->IsValidToken((mdParamDef)tk))
    {
        LPCSTR szParamName;
        hr = pScope->GetParamDefProps(tk, &usSequence, (DWORD *)pAttributes, &szParamName);
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

FCIMPL3(HRESULT, MetaDataImport::GetFieldDefProps, IMDInternalImport* pScope, mdToken tk, INT32 *pdwFieldFlags)
{
    FCALL_CONTRACT;

    return pScope->GetFieldDefProps(tk, (DWORD *)pdwFieldFlags);
}
FCIMPLEND

FCIMPL5(HRESULT, MetaDataImport::GetPropertyProps, IMDInternalImport* pScope, mdToken tk, LPCSTR* pszName, INT32 *pdwPropertyFlags, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    return pScope->GetPropertyProps(tk, pszName, (DWORD*)pdwPropertyFlags, (PCCOR_SIGNATURE*)&ppValue->m_array, (ULONG*)&ppValue->m_count);
 }
 FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetFieldMarshal, IMDInternalImport* pScope, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    HRESULT hr;

    hr = pScope->GetFieldMarshal(tk, (PCCOR_SIGNATURE *)&ppValue->m_array, (ULONG *)&ppValue->m_count);
    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        ppValue->m_array = NULL;
        ppValue->m_count = 0;
        hr = S_OK;
    }

    return hr;
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetSigOfMethodDef, IMDInternalImport* pScope, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    return pScope->GetSigOfMethodDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetSignatureFromToken, IMDInternalImport* pScope, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    return pScope->GetSigFromToken(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&(ppValue->m_array));
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetSigOfFieldDef, IMDInternalImport* pScope, mdToken tk, ConstArray* ppValue)
{
    FCALL_CONTRACT;

    return pScope->GetSigOfFieldDef(tk, (ULONG*)&ppValue->m_count, (PCCOR_SIGNATURE *)&ppValue->m_array);
}
FCIMPLEND

FCIMPL3(HRESULT, MetaDataImport::GetParentToken, IMDInternalImport* pScope, mdToken tk, mdToken* ptk)
{
    FCALL_CONTRACT;

    HRESULT hr;

    switch (TypeFromToken(tk))
    {
    case mdtTypeDef:
        hr = pScope->GetNestedClassProps(tk, ptk);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            *ptk = mdTypeDefNil;
            hr = S_OK;
        }
        break;

    case mdtGenericParam:
        hr = pScope->GetGenericParamProps(tk, NULL, NULL, ptk, NULL, NULL);
        break;

    case mdtMethodDef:
    case mdtMethodSpec:
    case mdtFieldDef:
    case mdtParamDef:
    case mdtMemberRef:
    case mdtCustomAttribute:
    case mdtEvent:
    case mdtProperty:
        hr = pScope->GetParentToken(tk, ptk);
        break;

    default:
        hr = COR_E_BADIMAGEFORMAT;
        break;
    }

    return hr;
}
FCIMPLEND

FCIMPL2(HRESULT, MetaDataImport::GetScopeProps, IMDInternalImport* pScope, GUID* pmvid)
{
    FCALL_CONTRACT;

    LPCSTR szName;
    return pScope->GetScopeProps(&szName, pmvid);
}
FCIMPLEND


FCIMPL3(HRESULT, MetaDataImport::GetMemberRefProps,
    IMDInternalImport* pScope,
    mdMemberRef mr,
    ConstArray* ppvSigBlob)
{
    FCALL_CONTRACT;

    LPCSTR szName_Ignore;
    return pScope->GetNameAndSigOfMemberRef(mr, (PCCOR_SIGNATURE*)&ppvSigBlob->m_array, (ULONG*)&ppvSigBlob->m_count, &szName_Ignore);
}
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("", on)    // restore command line optimization defaults
#endif

class ResultMemory final
{
    INT32 _length;
    INT32* _alloc;

public:
    ResultMemory() = default;

    ~ResultMemory()
    {
        STANDARD_VM_CONTRACT;
        delete[] _alloc;
    }

    INT32* AllocateUnmanagedArray(INT32 length)
    {
        CONTRACT(INT32*)
        {
            THROWS;
            MODE_PREEMPTIVE;
            PRECONDITION(_alloc == NULL);
            POSTCONDITION((length == _length));
            POSTCONDITION((RETVAL != NULL));
        }
        CONTRACT_END;

        _alloc = new INT32[length];
        _length = length;
        RETURN _alloc;
    }

    void AllocateManagedArray(QCall::ObjectHandleOnStack& longResult)
    {
        CONTRACTL
        {
            THROWS;
            MODE_PREEMPTIVE;
            PRECONDITION(_alloc != NULL);
        }
        CONTRACTL_END;

        {
            GCX_COOP();
            longResult.Set(AllocatePrimitiveArray(ELEMENT_TYPE_I4, _length));
            void* p = ((I4Array*)OBJECTREFToObject(longResult.Get()))->GetDirectPointerToNonObjectElements();
            memcpyNoGCRefs(p, _alloc, (size_t)_length * sizeof(INT32));
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
    if (resultLength <= shortResultLen)
    {
        p = shortResult;
    }
    else
    {
        _ASSERTE(resultLength > 0);
        p = resultMemory.AllocateUnmanagedArray(resultLength);
    }
    ZeroMemory(p, (size_t)resultLength * sizeof(INT32));
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

    ResultMemory memory{};
    ULONG resultLength;
    INT32 shortResultLen = *length;
    if (type == mdtTypeDef)
    {
        IfFailThrow(pScope->GetCountNestedClasses(tkParent, &resultLength));

        mdTypeDef* arToken = (mdTypeDef*)EnsureResultSize(resultLength, shortResultLen, shortResult, memory);
        IfFailThrow(pScope->GetNestedClasses(tkParent, arToken, resultLength, &resultLength));
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
        IfFailThrow(pScope->GetAllAssociates(&hEnum, arAssocRecord, associatesCount));
    }
    else
    {
        HENUMInternalHolder hEnum(pScope);
        hEnum.EnumInit(type, tkParent);

        resultLength = hEnum.EnumGetCount();

        mdToken* arToken = (mdToken*)EnsureResultSize(resultLength, shortResultLen, shortResult, memory);
        for(COUNT_T i = 0; i < resultLength && pScope->EnumNext(&hEnum, &arToken[i]); i++);
    }

    // If the result was longer than the short, we need to allocate an array.
    if (resultLength > (ULONG)shortResultLen)
        memory.AllocateManagedArray(longResult);

    *length = resultLength;

    END_QCALL;
}
