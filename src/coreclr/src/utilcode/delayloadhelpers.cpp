// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains convenience functionality for lazily loading modules
// and getting entrypoints within them.
// 

#include "stdafx.h"

#include "crtwrap.h"
#include "winwrap.h"
#include "utilcode.h"
#include "clrhost.h"
#include "holder.h"
#include "delayloadhelpers.h"

namespace DelayLoad
{
    //=================================================================================================================
    // Used to synchronize initialization. Is not used when initialization has already taken place.

    static CRITSEC_COOKIE g_pLock = nullptr;

    //=================================================================================================================
    // Creates and initializes g_pLock when first used.

    static HRESULT InitializeLock()
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        HRESULT hr = S_OK;

        CRITSEC_COOKIE pLock = ClrCreateCriticalSection(CrstLeafLock, CRST_REENTRANCY);
        IfNullRet(pLock);
        if (InterlockedCompareExchangeT<CRITSEC_COOKIE>(&g_pLock, pLock, nullptr) != nullptr)
        {
            ClrDeleteCriticalSection(pLock);
        }

        return S_OK;
    }

    //=================================================================================================================
    HRESULT Module::GetValue(HMODULE *pHMODULE)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        HRESULT hr = S_OK;

        if (pHMODULE == nullptr)
        {
            return E_INVALIDARG;
        }

        if (!m_fInitialized)
        {
            IfFailRet(InitializeLock());

            HModuleHolder hMod = ::LoadLibraryW(m_wzDllName);
            hr = (hMod == nullptr) ? HRESULT_FROM_GetLastError() : S_OK;
            _ASSERTE(FAILED(hr) == (hMod == nullptr));

            {   // Lock scope
                CRITSEC_Holder lock(g_pLock);
                if (!m_fInitialized)
                {
                    m_hr = hr;
                    m_hMod = hMod.Extract();
                    m_fInitialized = true;
                }
            }
        }

        _ASSERTE(m_fInitialized);
        *pHMODULE = m_hMod;
        return m_hr;
    }

    //=================================================================================================================
    HRESULT Function::GetValue(LPVOID * ppvFunc)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        HRESULT hr = S_OK;

        if (ppvFunc == nullptr)
        {
            return E_INVALIDARG;
        }

        if (!m_fInitialized)
        {
            HMODULE hMod = nullptr;
            IfFailRet(m_pModule->GetValue(&hMod));

            LPVOID pvFunc = reinterpret_cast<LPVOID>(::GetProcAddress(hMod, m_szFunctionName));
            hr = (pvFunc == nullptr) ? HRESULT_FROM_GetLastError() : S_OK;
            
            {   // Lock scope
                CRITSEC_Holder lock(g_pLock);
                if (!m_fInitialized)
                {
                    m_hr = hr;
                    m_pvFunction = pvFunc;
                    m_fInitialized = true;
                }
            }
        }

        _ASSERTE(m_fInitialized);
        *ppvFunc = m_pvFunction;
        return m_hr;
    }
}
