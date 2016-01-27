// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include "securityprincipal.h"
#include "corhost.h"
#include "security.h"

#ifndef FEATURE_CORECLR
INT32 QCALLTYPE COMPrincipal::ImpersonateLoggedOnUser(HANDLE hToken)
{
    QCALL_CONTRACT;

    HRESULT hr = S_OK;

    BEGIN_QCALL;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
        if (pSM) {
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            hr = pSM->ImpersonateLoggedOnUser(hToken);
            END_SO_TOLERANT_CODE_CALLING_HOST;
        }
        else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
        {
            if (!::ImpersonateLoggedOnUser(hToken))
                hr = HRESULT_FROM_GetLastError();
        }

    STRESS_LOG2(LF_SECURITY, LL_INFO100, "COMPrincipal::ImpersonateLoggedOnUser called with hTokenSAFE = %d. Returning 0x%x\n",hToken,hr);

    END_QCALL;

    return hr;
}

FCIMPL3(INT32, COMPrincipal::OpenThreadToken, DWORD dwDesiredAccess, DWORD dwOpenAs, SafeHandle** phThreadTokenUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(phThreadTokenUNSAFE));
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    SafeHandle** phThreadTokenSAFE = phThreadTokenUNSAFE;
    GCPROTECT_BEGININTERIOR(phThreadTokenSAFE);

    *phThreadTokenUNSAFE = NULL;
    HandleHolder hThreadToken;
    {
        GCX_PREEMP();
        BOOL bOpenAsSelf = TRUE;
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
        if (pSM) {
            if (dwOpenAs == WINSECURITYCONTEXT_THREAD)
                bOpenAsSelf = FALSE;

            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            hr = pSM->OpenThreadToken(dwDesiredAccess, bOpenAsSelf, &hThreadToken);
            END_SO_TOLERANT_CODE_CALLING_HOST;
            if (FAILED(hr) && dwOpenAs == WINSECURITYCONTEXT_BOTH) {
                bOpenAsSelf = FALSE;
                BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
                hr = pSM->OpenThreadToken(dwDesiredAccess, bOpenAsSelf, &hThreadToken);
                END_SO_TOLERANT_CODE_CALLING_HOST;
            }
        }
        else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
        {
            if (dwOpenAs == WINSECURITYCONTEXT_THREAD)
                bOpenAsSelf = FALSE;

            if (!::OpenThreadToken(::GetCurrentThread(), dwDesiredAccess, bOpenAsSelf, &hThreadToken)) {
                if (dwOpenAs == WINSECURITYCONTEXT_BOTH) {
                    bOpenAsSelf = FALSE;
                    hr = S_OK;
                    if (!::OpenThreadToken(::GetCurrentThread(), dwDesiredAccess, bOpenAsSelf, &hThreadToken))
                        hr = HRESULT_FROM_GetLastError();
                }
                else
                    hr = HRESULT_FROM_GetLastError();
            }
        }
    }

    if (SUCCEEDED(hr)) {
        struct _gc {
            SAFEHANDLE pSafeTokenHandle;
        } gc;
        gc.pSafeTokenHandle = NULL;

        GCPROTECT_BEGIN(gc);
        // Allocate a SafeHandle here
        MethodTable *pMT = MscorlibBinder::GetClass(CLASS__SAFE_TOKENHANDLE);
        gc.pSafeTokenHandle = (SAFEHANDLE) AllocateObject(pMT);
        CallDefaultConstructor(gc.pSafeTokenHandle);
        gc.pSafeTokenHandle->SetHandle((void*) hThreadToken);
        hThreadToken.SuppressRelease();

        SetObjectReference((OBJECTREF*) phThreadTokenSAFE, (OBJECTREF) gc.pSafeTokenHandle, gc.pSafeTokenHandle->GetAppDomain());
        GCPROTECT_END();
    }

    GCPROTECT_END();

    HELPER_METHOD_FRAME_END();
    return hr;
}
FCIMPLEND

INT32 QCALLTYPE COMPrincipal::RevertToSelf()
{
    QCALL_CONTRACT;

    HRESULT hr = S_OK;

    BEGIN_QCALL;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
        if (pSM) {
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            hr = pSM->RevertToSelf();
            END_SO_TOLERANT_CODE_CALLING_HOST;
        }
        else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
        {
            if (!::RevertToSelf())
                hr = HRESULT_FROM_GetLastError();
        }

    STRESS_LOG1(LF_SECURITY, LL_INFO100, "COMPrincipal::RevertToSelf returning 0x%x\n",hr);

    END_QCALL;

    return hr;
}

INT32 QCALLTYPE COMPrincipal::SetThreadToken(HANDLE hToken)
{
    QCALL_CONTRACT;

    HRESULT hr = S_OK;

    BEGIN_QCALL;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
    if (pSM)
    {
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pSM->SetThreadToken(hToken);
        END_SO_TOLERANT_CODE_CALLING_HOST;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        if (!::SetThreadToken(NULL, hToken))
            hr = HRESULT_FROM_GetLastError();
    }

    END_QCALL;

    return hr;
}
#endif // !FEATURE_CORECLR

void COMPrincipal::CLR_ImpersonateLoggedOnUser(HANDLE hToken)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS; 
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    {
        GCX_PREEMP();
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
        if (pSM) {
            hr = pSM->RevertToSelf();
            if (hr != S_OK)
            {
                // FailFast
                STRESS_LOG2(LF_EH, LL_INFO100, "CLR_ImpersonateLoggedOnUser failed for hImpersonateToken = %d with error:0x%x\n",hToken, hr);
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_SECURITY);
            }
            if (hToken != NULL)
                hr = pSM->ImpersonateLoggedOnUser(hToken);
        }
        else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
        {
            if (!::RevertToSelf())
                hr = HRESULT_FROM_GetLastError();
            if (hr != S_OK)
            {
                // FailFast
                STRESS_LOG2(LF_EH, LL_INFO100, "CLR_ImpersonateLoggedOnUser failed for hImpersonateToken = %d with error:0x%x\n",hToken, hr);
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_SECURITY);
            }            
            if (hToken != NULL && !::ImpersonateLoggedOnUser(hToken))
                hr = HRESULT_FROM_GetLastError();
        }
        
        if (hr != S_OK)
        {
            // FailFast
            STRESS_LOG2(LF_EH, LL_INFO100, "CLR_ImpersonateLoggedOnUser failed for hImpersonateToken = %d with error:0x%x\n",hToken, hr);
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_SECURITY);
        }
    }

    return;
}

