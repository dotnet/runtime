// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CustomMarshalerInfo.cpp
//

//
// Custom marshaler information used when marshaling
// a parameter with a custom marshaler.
//


#include "common.h"


#include "custommarshalerinfo.h"
#include "mlinfo.h"
#include "sigbuilder.h"

namespace
{
    MethodDesc * FindGetInstanceMethod(TypeHandle hndCustomMarshalerType)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;


        MethodTable *pMT = hndCustomMarshalerType.AsMethodTable();

        MethodDesc *pMD = MemberLoader::FindMethod(pMT, "GetInstance", &gsig_SM_Str_RetICustomMarshaler);
        if (!pMD)
        {
            DefineFullyQualifiedNameForClassW()
            COMPlusThrow(kApplicationException,
                        IDS_EE_GETINSTANCENOTIMPL,
                        GetFullyQualifiedNameForClassW(pMT));
        };

        // If the GetInstance method is generic, get an instantiating stub for it -
        // the CallDescr infrastructure doesn't know how to pass secret generic arguments.
        if (pMD->RequiresInstMethodTableArg())
        {
            pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                pMD,
                pMT,
                FALSE,           // forceBoxedEntryPoint
                Instantiation(), // methodInst
                FALSE,           // allowInstParam
                FALSE);          // forceRemotableMethod

            _ASSERTE(!pMD->RequiresInstMethodTableArg());
        }

        // Ensure that the value types in the signature are loaded.
        MetaSig::EnsureSigValueTypesLoaded(pMD);

        // Return the specified method desc.
        return pMD;
    }
}

//==========================================================================
// Implementation of the custom marshaler info class.
//==========================================================================

CustomMarshalerInfo::CustomMarshalerInfo(LoaderAllocator *pLoaderAllocator, TypeHandle hndCustomMarshalerType, TypeHandle hndManagedType, LPCUTF8 strCookie, DWORD cCookieStrBytes)
: m_pLoaderAllocator(pLoaderAllocator)
, m_hndCustomMarshaler{}
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pLoaderAllocator));
    }
    CONTRACTL_END;


    // Make sure the custom marshaller implements ICustomMarshaler.
    if (!hndCustomMarshalerType.GetMethodTable()->CanCastToInterface(CoreLibBinder::GetClass(CLASS__ICUSTOM_MARSHALER)))
    {
        DefineFullyQualifiedNameForClassW()
        COMPlusThrow(kApplicationException,
                     IDS_EE_ICUSTOMMARSHALERNOTIMPL,
                     GetFullyQualifiedNameForClassW(hndCustomMarshalerType.GetMethodTable()));
    }

    // Custom marshalling of value classes is not supported.
    if (hndManagedType.GetMethodTable()->IsValueType())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ValueClassCM"));

    // Run the <clinit> on the marshaler since it might not have run yet.
    hndCustomMarshalerType.GetMethodTable()->EnsureInstanceActive();
    hndCustomMarshalerType.GetMethodTable()->CheckRunClassInitThrowing();

    // Create a .NET string that will contain the string cookie.
    STRINGREF CookieStringObj = StringObject::NewString(strCookie, cCookieStrBytes);
    GCPROTECT_BEGIN(CookieStringObj);
    // Load the method desc for the static method to retrieve the instance.
    MethodDesc *pGetCustomMarshalerMD = FindGetInstanceMethod(hndCustomMarshalerType);

    MethodDescCallSite getCustomMarshaler(pGetCustomMarshalerMD, (OBJECTREF*)&CookieStringObj);

    pGetCustomMarshalerMD->EnsureActive();

    // Prepare the arguments that will be passed to GetCustomMarshaler.
    ARG_SLOT GetCustomMarshalerArgs[] = {
        ObjToArgSlot(CookieStringObj)
    };

    // Call the GetCustomMarshaler method to retrieve the custom marshaler to use.
    OBJECTREF CustomMarshalerObj = NULL;
    GCPROTECT_BEGIN(CustomMarshalerObj);
    CustomMarshalerObj = getCustomMarshaler.Call_RetOBJECTREF(GetCustomMarshalerArgs);
    if (!CustomMarshalerObj)
    {
        DefineFullyQualifiedNameForClassW()
        COMPlusThrow(kApplicationException,
                     IDS_EE_NOCUSTOMMARSHALER,
                     GetFullyQualifiedNameForClassW(hndCustomMarshalerType.GetMethodTable()));
    }

    m_hndCustomMarshaler = pLoaderAllocator->AllocateHandle(CustomMarshalerObj);
    GCPROTECT_END();
    GCPROTECT_END();
}


CustomMarshalerInfo::~CustomMarshalerInfo()
{
    WRAPPER_NO_CONTRACT;
    if (m_pLoaderAllocator->IsAlive() && m_hndCustomMarshaler)
    {
        // Only free the LOADERHANDLE if the LoaderAllocator is still alive.
        // If the loader allocator isn't alive, the handle has automatically
        // been collected already.
        m_pLoaderAllocator->FreeHandle(m_hndCustomMarshaler);
    }
    m_hndCustomMarshaler = 0;
}


void *CustomMarshalerInfo::operator new(size_t size, LoaderHeap *pHeap)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pHeap));
    }
    CONTRACTL_END;

    return pHeap->AllocMem(S_SIZE_T(sizeof(CustomMarshalerInfo)));
}


void CustomMarshalerInfo::operator delete(void *pMem)
{
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
    LIMITED_METHOD_CONTRACT;
}

#ifdef FEATURE_COMINTEROP
CustomMarshalerInfo* CustomMarshalerInfo::CreateIEnumeratorMarshalerInfo(LoaderHeap* pHeap, LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pHeap));
        PRECONDITION(CheckPointer(pLoaderAllocator));
    }
    CONTRACTL_END;

    CustomMarshalerInfo* pInfo = nullptr;
    OBJECTREF IEnumeratorMarshalerObj = nullptr;

    GCX_COOP();
    GCPROTECT_BEGIN(IEnumeratorMarshalerObj);

    MethodDescCallSite getMarshaler(METHOD__STUBHELPERS__GET_IENUMERATOR_TO_ENUM_VARIANT_MARSHALER);
    IEnumeratorMarshalerObj = getMarshaler.Call_RetOBJECTREF(NULL);

    pInfo = new (pHeap) CustomMarshalerInfo(pLoaderAllocator, pLoaderAllocator->AllocateHandle(IEnumeratorMarshalerObj));

    GCPROTECT_END();

    return pInfo;
}
#endif

//==========================================================================
// Implementation of the custom marshaler hashtable helper.
//==========================================================================

EEHashEntry_t * EECMInfoHashtableHelper::AllocateEntry(EECMInfoHashtableKey *pKey, BOOL bDeepCopy, void* pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END;

    EEHashEntry_t *pEntry;

    if (bDeepCopy)
    {
        S_SIZE_T cbEntry = S_SIZE_T(sizeof(EEHashEntry) - 1 + sizeof(EECMInfoHashtableKey));
        cbEntry += S_SIZE_T(pKey->GetMarshalerTypeNameByteCount());
        cbEntry += S_SIZE_T(pKey->GetCookieStringByteCount());
        cbEntry += S_SIZE_T(pKey->GetMarshalerInstantiation().GetNumArgs()) * S_SIZE_T(sizeof(LPVOID));
        cbEntry += S_SIZE_T(sizeof(LPVOID)); // For EECMInfoHashtableKey::m_invokingAssembly

        if (cbEntry.IsOverflow())
            return NULL;

        pEntry = (EEHashEntry_t *) new (nothrow) BYTE[cbEntry.Value()];
        if (!pEntry)
            return NULL;

        EECMInfoHashtableKey *pEntryKey = (EECMInfoHashtableKey *) pEntry->Key;
        pEntryKey->m_cMarshalerTypeNameBytes = pKey->GetMarshalerTypeNameByteCount();
        pEntryKey->m_strMarshalerTypeName = (LPSTR) pEntry->Key + sizeof(EECMInfoHashtableKey);
        pEntryKey->m_cCookieStrBytes = pKey->GetCookieStringByteCount();
        pEntryKey->m_strCookie = (LPSTR) pEntry->Key + sizeof(EECMInfoHashtableKey) + pEntryKey->m_cMarshalerTypeNameBytes;
        pEntryKey->m_Instantiation = Instantiation(
            (TypeHandle *) (pEntryKey->m_strCookie + pEntryKey->m_cCookieStrBytes),
            pKey->GetMarshalerInstantiation().GetNumArgs());
        memcpy((void*)pEntryKey->m_strMarshalerTypeName, pKey->GetMarshalerTypeName(), pKey->GetMarshalerTypeNameByteCount());
        memcpy((void*)pEntryKey->m_strCookie, pKey->GetCookieString(), pKey->GetCookieStringByteCount());
        memcpy((void*)pEntryKey->m_Instantiation.GetRawArgs(), pKey->GetMarshalerInstantiation().GetRawArgs(),
            pEntryKey->m_Instantiation.GetNumArgs() * sizeof(LPVOID));
        pEntryKey->m_invokingAssembly = pKey->GetInvokingAssembly();
    }
    else
    {
        pEntry = (EEHashEntry_t *)
            new (nothrow) BYTE[sizeof(EEHashEntry) - 1 + sizeof(EECMInfoHashtableKey)];
        if (!pEntry)
            return NULL;

        EECMInfoHashtableKey *pEntryKey = (EECMInfoHashtableKey *) pEntry->Key;
        pEntryKey->m_cMarshalerTypeNameBytes = pKey->GetMarshalerTypeNameByteCount();
        pEntryKey->m_strMarshalerTypeName = pKey->GetMarshalerTypeName();
        pEntryKey->m_cCookieStrBytes = pKey->GetCookieStringByteCount();
        pEntryKey->m_strCookie = pKey->GetCookieString();
        pEntryKey->m_Instantiation = Instantiation(pKey->GetMarshalerInstantiation());
        pEntryKey->m_invokingAssembly = pKey->GetInvokingAssembly();
    }

    return pEntry;
}


void EECMInfoHashtableHelper::DeleteEntry(EEHashEntry_t *pEntry, void* pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    CustomMarshalerInfo* pInfo = reinterpret_cast<CustomMarshalerInfo*>(pEntry->Data);

    delete pInfo;

    delete[] (BYTE*)pEntry;
}


BOOL EECMInfoHashtableHelper::CompareKeys(EEHashEntry_t *pEntry, EECMInfoHashtableKey *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
        PRECONDITION(CheckPointer(pKey));
    }
    CONTRACTL_END;

    EECMInfoHashtableKey *pEntryKey = (EECMInfoHashtableKey *) pEntry->Key;

    if (pEntryKey->GetMarshalerTypeNameByteCount() != pKey->GetMarshalerTypeNameByteCount())
        return FALSE;

    if (memcmp(pEntryKey->GetMarshalerTypeName(), pKey->GetMarshalerTypeName(), pEntryKey->GetMarshalerTypeNameByteCount()) != 0)
        return FALSE;

    if (pEntryKey->GetCookieStringByteCount() != pKey->GetCookieStringByteCount())
        return FALSE;

    if (memcmp(pEntryKey->GetCookieString(), pKey->GetCookieString(), pEntryKey->GetCookieStringByteCount()) != 0)
        return FALSE;

    DWORD dwNumTypeArgs = pEntryKey->GetMarshalerInstantiation().GetNumArgs();
    if (dwNumTypeArgs != pKey->GetMarshalerInstantiation().GetNumArgs())
        return FALSE;

    for (DWORD i = 0; i < dwNumTypeArgs; i++)
    {
        if (pEntryKey->GetMarshalerInstantiation()[i] != pKey->GetMarshalerInstantiation()[i])
            return FALSE;
    }

    if (pEntryKey->GetInvokingAssembly() != pKey->GetInvokingAssembly())
        return FALSE;

    return TRUE;
}


DWORD EECMInfoHashtableHelper::Hash(EECMInfoHashtableKey *pKey)
{
    WRAPPER_NO_CONTRACT;

    return (DWORD)
        (HashBytes((const BYTE *) pKey->GetMarshalerTypeName(), pKey->GetMarshalerTypeNameByteCount()) +
        HashBytes((const BYTE *) pKey->GetCookieString(), pKey->GetCookieStringByteCount()) +
        HashBytes((const BYTE *) pKey->GetMarshalerInstantiation().GetRawArgs(), pKey->GetMarshalerInstantiation().GetNumArgs() * sizeof(LPVOID)));
}
