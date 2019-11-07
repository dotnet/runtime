// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Contains helper types for assembly binding host infrastructure.

#include "common.h"

#include "utilcode.h"
#include "strsafe.h"

#include "clrprivbinderutil.h"

inline
LPWSTR CopyStringThrowing(
    LPCWSTR wszString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    NewArrayHolder<WCHAR> wszDup = NULL;
    if (wszString != NULL)
    {
        size_t wszLen = wcslen(wszString);
        wszDup = new WCHAR[wszLen + 1];
        IfFailThrow(StringCchCopy(wszDup, wszLen + 1, wszString));
    }
    wszDup.SuppressRelease();

    return wszDup;
}


namespace CLRPrivBinderUtil
{
    //---------------------------------------------------------------------------------------------
    CLRPrivResourcePathImpl::CLRPrivResourcePathImpl(LPCWSTR wzPath)
        : m_wzPath(CopyStringThrowing(wzPath))
    { STANDARD_VM_CONTRACT; }

    //---------------------------------------------------------------------------------------------
    HRESULT CLRPrivResourcePathImpl::GetPath(
        DWORD cchBuffer,
        LPDWORD pcchBuffer,
        __inout_ecount_part(cchBuffer, *pcchBuffer) LPWSTR wzBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        if (pcchBuffer == nullptr)
            IfFailRet(E_INVALIDARG);

        *pcchBuffer = (DWORD)wcslen(m_wzPath);

        if (wzBuffer != nullptr)
        {
            if (FAILED(StringCchCopy(wzBuffer, cchBuffer, m_wzPath)))
                IfFailRet(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
        }

        return hr;
    }

    //=====================================================================================================================
    // Destroys list of strings (code:WStringList).
    void
    WStringList_Delete(
        WStringList * pList)
    {
        LIMITED_METHOD_CONTRACT;

        if (pList != nullptr)
        {
            for (WStringListElem * pElem = pList->RemoveHead(); pElem != nullptr; pElem = pList->RemoveHead())
            {
                // Delete the string
                delete [] pElem->GetValue();
                delete pElem;
            }

            delete pList;
        }
    }


////////////////////////////////////////////////////////////////////////////////////////////////////
///// ----------------------------- Direct calls to VM  -------------------------------------------
////////////////////////////////////////////////////////////////////////////////////////////////////
} // namespace CLRPrivBinderUtil
