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

    //---------------------------------------------------------------------------------------------
    CLRPrivResourceStreamImpl::CLRPrivResourceStreamImpl(IStream * pStream)
        : m_pStream(pStream)
    {
        LIMITED_METHOD_CONTRACT;
        pStream->AddRef();
    }

    //---------------------------------------------------------------------------------------------
    HRESULT CLRPrivResourceStreamImpl::GetStream(
        REFIID riid,
        LPVOID * ppvStream)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pStream->QueryInterface(riid, ppvStream);
    }

    //---------------------------------------------------------------------------------------------
    HRESULT AssemblyVersion::Initialize(
        IAssemblyName * pAssemblyName)
    {
        WRAPPER_NO_CONTRACT;
        HRESULT hr = pAssemblyName->GetVersion(&dwMajorMinor, &dwBuildRevision);
        if (hr == FUSION_E_INVALID_NAME)
        {
            hr = S_FALSE;
        }
        return hr;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT AssemblyVersion::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        WRAPPER_NO_CONTRACT;
        return pAssemblyInfo->GetAssemblyVersion(&wMajor, &wMinor, &wBuild, &wRevision);
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKey::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;
        
        VALIDATE_PTR_RET(pAssemblyInfo);

        Uninitialize();

        DWORD cbKeyDef = 0;
        hr = pAssemblyInfo->GetAssemblyPublicKey(cbKeyDef, &cbKeyDef, nullptr);

        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (cbKeyDef != 0)
            {
                NewArrayHolder<BYTE> pbKeyDef = new (nothrow) BYTE[cbKeyDef];
                IfNullRet(pbKeyDef);

                if (SUCCEEDED(hr = pAssemblyInfo->GetAssemblyPublicKey(cbKeyDef, &cbKeyDef, pbKeyDef)))
                {
                    m_key = pbKeyDef.Extract();
                    m_key_owned = true;
                    m_size = cbKeyDef;
                }
            }
        }

        return hr;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKeyToken::Initialize(
        BYTE * pbKeyToken,
        DWORD cbKeyToken)
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_CONDITION((pbKeyToken == nullptr) == (cbKeyToken == 0), return E_INVALIDARG);
        VALIDATE_ARG_RET(cbKeyToken == 0 || cbKeyToken == PUBLIC_KEY_TOKEN_LEN1);

        m_cbKeyToken = cbKeyToken;

        if (pbKeyToken != nullptr)
        {
            memcpy(m_rbKeyToken, pbKeyToken, PUBLIC_KEY_TOKEN_LEN1);
        }
        else
        {
            memset(m_rbKeyToken, 0, PUBLIC_KEY_TOKEN_LEN1);
        }

        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKeyToken::Initialize(
        PublicKey const & pk)
    {
        LIMITED_METHOD_CONTRACT;

        StrongNameBufferHolder<BYTE>            pbKeyToken;
        DWORD                                   cbKeyToken;

        if (!StrongNameTokenFromPublicKey(const_cast<BYTE*>(pk.GetKey()), pk.GetSize(), &pbKeyToken, &cbKeyToken))
        {
            return static_cast<HRESULT>(StrongNameErrorInfo());
        }

        return Initialize(pbKeyToken, cbKeyToken);
    }

    //=====================================================================================================================
    HRESULT PublicKeyToken::Initialize(
        IAssemblyName * pName)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        DWORD cbKeyToken = sizeof(m_rbKeyToken);
        hr = pName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, m_rbKeyToken, &cbKeyToken);
        if (SUCCEEDED(hr))
        {
            m_cbKeyToken = cbKeyToken;
        }

        if (hr == FUSION_E_INVALID_NAME)
        {
            hr = S_FALSE;
        }

        return hr;
    }

    //=====================================================================================================================
    HRESULT PublicKeyToken::Initialize(
        ICLRPrivAssemblyInfo * pName)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        PublicKey pk;
        IfFailRet(pk.Initialize(pName));

        if (hr == S_OK) // Can return S_FALSE if no public key/token defined.
        {
            hr = Initialize(pk);
        }

        return hr;
    }

    //=====================================================================================================================
    bool operator==(
        PublicKeyToken const & lhs,
        PublicKeyToken const & rhs)
    {
        LIMITED_METHOD_CONTRACT;

        // Sizes must match
        if (lhs.GetSize() != rhs.GetSize())
        {
            return false;
        }

        // Empty PKT values are considered to be equal.
        if (lhs.GetSize() == 0)
        {
            return true;
        }

        // Compare values.
        return memcmp(lhs.GetToken(), rhs.GetToken(), lhs.GetSize()) == 0;
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        LPCWSTR wzName)
    {
        LIMITED_METHOD_CONTRACT;
        return StringCchCopy(Name, sizeof(Name) / sizeof(Name[0]), wzName);
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        DWORD cchName = sizeof(Name) / sizeof(Name[0]);
        IfFailRet(pAssemblyInfo->GetAssemblyName(cchName, &cchName, Name));
        IfFailRet(Version.Initialize(pAssemblyInfo));
        IfFailRet(KeyToken.Initialize(pAssemblyInfo));

        return hr;
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        IAssemblyName * pAssemblyName)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        DWORD cchName = sizeof(Name) / sizeof(Name[0]);
        IfFailRet(pAssemblyName->GetName(&cchName, Name));
        IfFailRet(Version.Initialize(pAssemblyName));
        IfFailRet(KeyToken.Initialize(pAssemblyName));

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
