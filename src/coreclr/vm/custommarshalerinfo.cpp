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

//==========================================================================
// Implementation of the custom marshaler info class.
//==========================================================================

CustomMarshalerInfo::CustomMarshalerInfo(LoaderAllocator *pLoaderAllocator, TypeHandle hndCustomMarshalerType, TypeHandle hndManagedType, LPCUTF8 strCookie, DWORD cCookieStrBytes)
: m_NativeSize(0)
, m_hndManagedType(hndManagedType)
, m_pLoaderAllocator(pLoaderAllocator)
, m_hndCustomMarshaler(NULL)
, m_pMarshalNativeToManagedMD(NULL)
, m_pMarshalManagedToNativeMD(NULL)
, m_pCleanUpNativeDataMD(NULL)
, m_pCleanUpManagedDataMD(NULL)
, m_bDataIsByValue(FALSE)
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

    // Determine if this type is a value class.
    m_bDataIsByValue = m_hndManagedType.GetMethodTable()->IsValueType();

    // Custom marshalling of value classes is not currently supported.
    if (m_bDataIsByValue)
        COMPlusThrow(kNotSupportedException, W("NotSupported_ValueClassCM"));

    // Run the <clinit> on the marshaler since it might not have run yet.
    hndCustomMarshalerType.GetMethodTable()->EnsureInstanceActive();
    hndCustomMarshalerType.GetMethodTable()->CheckRunClassInitThrowing();

    // Create a COM+ string that will contain the string cookie.
    STRINGREF CookieStringObj = StringObject::NewString(strCookie, cCookieStrBytes);
    GCPROTECT_BEGIN(CookieStringObj);
    // Load the method desc for the static method to retrieve the instance.
    MethodDesc *pGetCustomMarshalerMD = GetCustomMarshalerMD(CustomMarshalerMethods_GetInstance, hndCustomMarshalerType);

    // If the GetInstance method is generic, get an instantiating stub for it -
    // the CallDescr infrastructure doesn't know how to pass secret generic arguments.
    if (pGetCustomMarshalerMD->RequiresInstMethodTableArg())
    {
        pGetCustomMarshalerMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pGetCustomMarshalerMD,
            hndCustomMarshalerType.GetMethodTable(),
            FALSE,           // forceBoxedEntryPoint
            Instantiation(), // methodInst
            FALSE,           // allowInstParam
            FALSE);          // forceRemotableMethod

        _ASSERTE(!pGetCustomMarshalerMD->RequiresInstMethodTableArg());
    }

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
    // Load the method desc's for all the methods in the ICustomMarshaler interface based on the type of the marshaler object.
    TypeHandle customMarshalerObjType = CustomMarshalerObj->GetMethodTable();

    m_pMarshalNativeToManagedMD = GetCustomMarshalerMD(CustomMarshalerMethods_MarshalNativeToManaged, customMarshalerObjType);
    m_pMarshalManagedToNativeMD = GetCustomMarshalerMD(CustomMarshalerMethods_MarshalManagedToNative, customMarshalerObjType);
    m_pCleanUpNativeDataMD = GetCustomMarshalerMD(CustomMarshalerMethods_CleanUpNativeData, customMarshalerObjType);
    m_pCleanUpManagedDataMD = GetCustomMarshalerMD(CustomMarshalerMethods_CleanUpManagedData, customMarshalerObjType);

    m_hndCustomMarshaler = pLoaderAllocator->AllocateHandle(CustomMarshalerObj);
    GCPROTECT_END();

    // Retrieve the size of the native data.
    if (m_bDataIsByValue)
    {
        // <TODO>@TODO(DM): Call GetNativeDataSize() to retrieve the size of the native data.</TODO>
        _ASSERTE(!"Value classes are not yet supported by the custom marshaler!");
    }
    else
    {
        m_NativeSize = sizeof(void *);
    }

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
    m_hndCustomMarshaler = NULL;
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

OBJECTREF CustomMarshalerInfo::InvokeMarshalNativeToManagedMeth(void *pNative)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative, NULL_OK));
    }
    CONTRACTL_END;

    if (!pNative)
        return NULL;

    OBJECTREF managedObject;

    OBJECTREF customMarshaler = m_pLoaderAllocator->GetHandleValue(m_hndCustomMarshaler);
    GCPROTECT_BEGIN (customMarshaler);
    MethodDescCallSite marshalNativeToManaged(m_pMarshalNativeToManagedMD, &customMarshaler);

    ARG_SLOT Args[] = {
        ObjToArgSlot(customMarshaler),
        PtrToArgSlot(pNative)
    };

    managedObject = marshalNativeToManaged.Call_RetOBJECTREF(Args);
    GCPROTECT_END ();

    return managedObject;
}


void *CustomMarshalerInfo::InvokeMarshalManagedToNativeMeth(OBJECTREF MngObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    void *RetVal = NULL;

    if (!MngObj)
        return NULL;

    GCPROTECT_BEGIN (MngObj);
    OBJECTREF customMarshaler = m_pLoaderAllocator->GetHandleValue(m_hndCustomMarshaler);
    GCPROTECT_BEGIN (customMarshaler);
    MethodDescCallSite marshalManagedToNative(m_pMarshalManagedToNativeMD, &customMarshaler);

    ARG_SLOT Args[] = {
        ObjToArgSlot(customMarshaler),
        ObjToArgSlot(MngObj)
    };

    RetVal = marshalManagedToNative.Call_RetLPVOID(Args);
    GCPROTECT_END ();
    GCPROTECT_END ();

    return RetVal;
}


void CustomMarshalerInfo::InvokeCleanUpNativeMeth(void *pNative)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pNative, NULL_OK));
    }
    CONTRACTL_END;

    if (!pNative)
        return;

    OBJECTREF customMarshaler = m_pLoaderAllocator->GetHandleValue(m_hndCustomMarshaler);
    GCPROTECT_BEGIN (customMarshaler);
    MethodDescCallSite cleanUpNativeData(m_pCleanUpNativeDataMD, &customMarshaler);

    ARG_SLOT Args[] = {
        ObjToArgSlot(customMarshaler),
        PtrToArgSlot(pNative)
    };

    cleanUpNativeData.Call(Args);
    GCPROTECT_END();
}


void CustomMarshalerInfo::InvokeCleanUpManagedMeth(OBJECTREF MngObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!MngObj)
        return;

    GCPROTECT_BEGIN (MngObj);
    OBJECTREF customMarshaler = m_pLoaderAllocator->GetHandleValue(m_hndCustomMarshaler);
    GCPROTECT_BEGIN (customMarshaler);
    MethodDescCallSite cleanUpManagedData(m_pCleanUpManagedDataMD, &customMarshaler);

    ARG_SLOT Args[] = {
        ObjToArgSlot(customMarshaler),
        ObjToArgSlot(MngObj)
    };

    cleanUpManagedData.Call(Args);
    GCPROTECT_END ();
    GCPROTECT_END ();
}

MethodDesc *CustomMarshalerInfo::GetCustomMarshalerMD(EnumCustomMarshalerMethods Method, TypeHandle hndCustomMarshalertype)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;


    MethodTable *pMT = hndCustomMarshalertype.AsMethodTable();

    _ASSERTE(pMT->CanCastToInterface(CoreLibBinder::GetClass(CLASS__ICUSTOM_MARSHALER)));

    MethodDesc *pMD = NULL;

    switch (Method)
    {
        case CustomMarshalerMethods_MarshalNativeToManaged:
            pMD = pMT->GetMethodDescForInterfaceMethod(
                       CoreLibBinder::GetMethod(METHOD__ICUSTOM_MARSHALER__MARSHAL_NATIVE_TO_MANAGED),
                       TRUE /* throwOnConflict */);
            break;
        case CustomMarshalerMethods_MarshalManagedToNative:
            pMD = pMT->GetMethodDescForInterfaceMethod(
                       CoreLibBinder::GetMethod(METHOD__ICUSTOM_MARSHALER__MARSHAL_MANAGED_TO_NATIVE),
                       TRUE /* throwOnConflict */);
            break;
        case CustomMarshalerMethods_CleanUpNativeData:
            pMD = pMT->GetMethodDescForInterfaceMethod(
                        CoreLibBinder::GetMethod(METHOD__ICUSTOM_MARSHALER__CLEANUP_NATIVE_DATA),
                        TRUE /* throwOnConflict */);
            break;

        case CustomMarshalerMethods_CleanUpManagedData:
            pMD = pMT->GetMethodDescForInterfaceMethod(
                        CoreLibBinder::GetMethod(METHOD__ICUSTOM_MARSHALER__CLEANUP_MANAGED_DATA),
                        TRUE /* throwOnConflict */);
            break;
        case CustomMarshalerMethods_GetNativeDataSize:
            pMD = pMT->GetMethodDescForInterfaceMethod(
                        CoreLibBinder::GetMethod(METHOD__ICUSTOM_MARSHALER__GET_NATIVE_DATA_SIZE),
                        TRUE /* throwOnConflict */);
            break;
        case CustomMarshalerMethods_GetInstance:
            // Must look this up by name since it's static
            pMD = MemberLoader::FindMethod(pMT, "GetInstance", &gsig_SM_Str_RetICustomMarshaler);
            if (!pMD)
            {
                DefineFullyQualifiedNameForClassW()
                COMPlusThrow(kApplicationException,
                             IDS_EE_GETINSTANCENOTIMPL,
                             GetFullyQualifiedNameForClassW(pMT));
            }
            break;
        default:
            _ASSERTE(!"Unknown custom marshaler method");
    }

    _ASSERTE(pMD && "Unable to find specified CustomMarshaler method");

    // Ensure that the value types in the signature are loaded.
    MetaSig::EnsureSigValueTypesLoaded(pMD);

    // Return the specified method desc.
    return pMD;
}


//==========================================================================
// Implementation of the custom marshaler hashtable helper.
//==========================================================================

EEHashEntry_t * EECMHelperHashtableHelper::AllocateEntry(EECMHelperHashtableKey *pKey, BOOL bDeepCopy, void* pHeap)
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
        S_SIZE_T cbEntry = S_SIZE_T(sizeof(EEHashEntry) - 1 + sizeof(EECMHelperHashtableKey));
        cbEntry += S_SIZE_T(pKey->GetMarshalerTypeNameByteCount());
        cbEntry += S_SIZE_T(pKey->GetCookieStringByteCount());
        cbEntry += S_SIZE_T(pKey->GetMarshalerInstantiation().GetNumArgs()) * S_SIZE_T(sizeof(LPVOID));
        cbEntry += S_SIZE_T(sizeof(LPVOID)); // For EECMHelperHashtableKey::m_invokingAssembly

        if (cbEntry.IsOverflow())
            return NULL;

        pEntry = (EEHashEntry_t *) new (nothrow) BYTE[cbEntry.Value()];
        if (!pEntry)
            return NULL;

        EECMHelperHashtableKey *pEntryKey = (EECMHelperHashtableKey *) pEntry->Key;
        pEntryKey->m_cMarshalerTypeNameBytes = pKey->GetMarshalerTypeNameByteCount();
        pEntryKey->m_strMarshalerTypeName = (LPSTR) pEntry->Key + sizeof(EECMHelperHashtableKey);
        pEntryKey->m_cCookieStrBytes = pKey->GetCookieStringByteCount();
        pEntryKey->m_strCookie = (LPSTR) pEntry->Key + sizeof(EECMHelperHashtableKey) + pEntryKey->m_cMarshalerTypeNameBytes;
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
            new (nothrow) BYTE[sizeof(EEHashEntry) - 1 + sizeof(EECMHelperHashtableKey)];
        if (!pEntry)
            return NULL;

        EECMHelperHashtableKey *pEntryKey = (EECMHelperHashtableKey *) pEntry->Key;
        pEntryKey->m_cMarshalerTypeNameBytes = pKey->GetMarshalerTypeNameByteCount();
        pEntryKey->m_strMarshalerTypeName = pKey->GetMarshalerTypeName();
        pEntryKey->m_cCookieStrBytes = pKey->GetCookieStringByteCount();
        pEntryKey->m_strCookie = pKey->GetCookieString();
        pEntryKey->m_Instantiation = Instantiation(pKey->GetMarshalerInstantiation());
        pEntryKey->m_invokingAssembly = pKey->GetInvokingAssembly();
    }

    return pEntry;
}


void EECMHelperHashtableHelper::DeleteEntry(EEHashEntry_t *pEntry, void* pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    delete[] (BYTE*)pEntry;
}


BOOL EECMHelperHashtableHelper::CompareKeys(EEHashEntry_t *pEntry, EECMHelperHashtableKey *pKey)
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

    EECMHelperHashtableKey *pEntryKey = (EECMHelperHashtableKey *) pEntry->Key;

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


DWORD EECMHelperHashtableHelper::Hash(EECMHelperHashtableKey *pKey)
{
    WRAPPER_NO_CONTRACT;

    return (DWORD)
        (HashBytes((const BYTE *) pKey->GetMarshalerTypeName(), pKey->GetMarshalerTypeNameByteCount()) +
        HashBytes((const BYTE *) pKey->GetCookieString(), pKey->GetCookieStringByteCount()) +
        HashBytes((const BYTE *) pKey->GetMarshalerInstantiation().GetRawArgs(), pKey->GetMarshalerInstantiation().GetNumArgs() * sizeof(LPVOID)));
}


OBJECTREF CustomMarshalerHelper::InvokeMarshalNativeToManagedMeth(void *pNative)
{
    WRAPPER_NO_CONTRACT;
    return GetCustomMarshalerInfo()->InvokeMarshalNativeToManagedMeth(pNative);
}


void *CustomMarshalerHelper::InvokeMarshalManagedToNativeMeth(OBJECTREF MngObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    void *RetVal = NULL;

    GCPROTECT_BEGIN(MngObj)
    {
        CustomMarshalerInfo *pCMInfo = GetCustomMarshalerInfo();
        RetVal = pCMInfo->InvokeMarshalManagedToNativeMeth(MngObj);
    }
    GCPROTECT_END();

    return RetVal;
}


void CustomMarshalerHelper::InvokeCleanUpNativeMeth(void *pNative)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF ExceptionObj = NULL;
    GCPROTECT_BEGIN(ExceptionObj)
    {
        EX_TRY
        {
            GetCustomMarshalerInfo()->InvokeCleanUpNativeMeth(pNative);
        }
        EX_CATCH
        {
            ExceptionObj = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    GCPROTECT_END();
}


void CustomMarshalerHelper::InvokeCleanUpManagedMeth(OBJECTREF MngObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(MngObj)
    {
        CustomMarshalerInfo *pCMInfo = GetCustomMarshalerInfo();
        pCMInfo->InvokeCleanUpManagedMeth(MngObj);
    }
    GCPROTECT_END();
}


void *NonSharedCustomMarshalerHelper::operator new(size_t size, LoaderHeap *pHeap)
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

    return pHeap->AllocMem(S_SIZE_T(sizeof(NonSharedCustomMarshalerHelper)));
}


void NonSharedCustomMarshalerHelper::operator delete(void *pMem)
{
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
    LIMITED_METHOD_CONTRACT;
}


SharedCustomMarshalerHelper::SharedCustomMarshalerHelper(Assembly *pAssembly, TypeHandle hndManagedType, LPCUTF8 strMarshalerTypeName, DWORD cMarshalerTypeNameBytes, LPCUTF8 strCookie, DWORD cCookieStrBytes)
: m_pAssembly(pAssembly)
, m_hndManagedType(hndManagedType)
, m_cMarshalerTypeNameBytes(cMarshalerTypeNameBytes)
, m_strMarshalerTypeName(strMarshalerTypeName)
, m_cCookieStrBytes(cCookieStrBytes)
, m_strCookie(strCookie)
{
    WRAPPER_NO_CONTRACT;
}


void *SharedCustomMarshalerHelper::operator new(size_t size, LoaderHeap *pHeap)
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

    return pHeap->AllocMem(S_SIZE_T(sizeof(SharedCustomMarshalerHelper)));
}


void SharedCustomMarshalerHelper::operator delete(void *pMem)
{
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
    LIMITED_METHOD_CONTRACT;
}


CustomMarshalerInfo *SharedCustomMarshalerHelper::GetCustomMarshalerInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Retrieve the marshalling data for the current app domain.
    EEMarshalingData *pMarshalingData = GetThread()->GetDomain()->GetLoaderAllocator()->GetMarshalingData();

    // Retrieve the custom marshaling information for the current shared custom
    // marshaling helper.
    return pMarshalingData->GetCustomMarshalerInfo(this);
}

extern "C" void QCALLTYPE CustomMarshaler_GetMarshalerObject(CustomMarshalerHelper* pCMHelper, QCall::ObjectHandleOnStack retObject)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;
    GCX_COOP();

    retObject.Set(pCMHelper->GetCustomMarshalerInfo()->GetCustomMarshaler());

    END_QCALL;
}
