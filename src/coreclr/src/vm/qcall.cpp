// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// QCALL.CPP
//



#include "common.h"

//
// Helpers for returning managed string from QCall
//

void QCall::StringHandleOnStack::Set(const SString& value)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    Set(StringObject::NewString(value));
}

void QCall::StringHandleOnStack::Set(LPCWSTR pwzValue)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    Set(StringObject::NewString(pwzValue));
}

void QCall::StringHandleOnStack::Set(LPCUTF8 pszValue)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    Set(StringObject::NewString(pszValue));
}

//
// Helpers for returning common managed types from QCall
//

void QCall::ObjectHandleOnStack::SetByteArray(const BYTE * p, COUNT_T length)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    BASEARRAYREF arr = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, length);
    memcpyNoGCRefs(arr->GetDataPtr(), p, length * sizeof(BYTE));
    Set(arr);
}

void QCall::ObjectHandleOnStack::SetIntPtrArray(const PVOID * p, COUNT_T length)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    BASEARRAYREF arr = (BASEARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_I, length);
    memcpyNoGCRefs(arr->GetDataPtr(), p, length * sizeof(PVOID));
    Set(arr);
}

void QCall::ObjectHandleOnStack::SetGuidArray(const GUID * p, COUNT_T length)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    TypeHandle typeHandle = MscorlibBinder::GetClass(CLASS__GUID);
    BASEARRAYREF arr = (BASEARRAYREF) AllocateValueSzArray(typeHandle, length);
    memcpyNoGCRefs(arr->GetDataPtr(), p, length * sizeof(GUID));
    Set(arr);
}

//
// Helpers for passing an AppDomain to a QCall
//

#ifdef _DEBUG

//---------------------------------------------------------------------------------------
//
// Verify that the AppDomain being passed from the BCL is valid for use in a QCall. Note: some additional
// checks are in System.AppDomain.GetNativeHandle()
//

void QCall::AppDomainHandle::VerifyDomainHandle() const
{
    LIMITED_METHOD_CONTRACT;

    // System.AppDomain.GetNativeHandle() should ensure that we're not calling through with a null AppDomain pointer.
    _ASSERTE(m_pAppDomain);

    // QCalls should only be made with pointers to the current domain
    _ASSERTE(GetAppDomain() == m_pAppDomain);

    // We should not have a QCall made on an invalid AppDomain. Once unload a domain, we won't let anyone else
    // in and any threads that are already in will be unwound.
    _ASSERTE(SystemDomain::GetAppDomainAtIndex(m_pAppDomain->GetIndex()) != NULL);
}

#endif // _DEBUG
