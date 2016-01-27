// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h"
#ifdef FEATURE_CAS_POLICY

#include "hostexecutioncontext.h"
#include "corhost.h"
#include "security.h"

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
IHostSecurityContext *HostExecutionContextManager::m_pRestrictedHostContext = NULL;
#endif // FEATURE_INCLUDE_ALL_INTERFACES

// initialize HostRestrictedContext
void HostExecutionContextManager::InitializeRestrictedContext()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
	
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	_ASSERTE(m_pRestrictedHostContext == NULL);
	
	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
	if (pSM)
	{
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
		pSM->GetSecurityContext(eRestrictedContext, &m_pRestrictedHostContext);
        END_SO_TOLERANT_CODE_CALLING_HOST;
	}	
#endif // FEATURE_INCLUDE_ALL_INTERFACES
}
// notify the Host to SetRestrictedContext
void HostExecutionContextManager::SetHostRestrictedContext()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
	
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	if(m_pRestrictedHostContext != NULL)
	{		
        	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
		if (pSM)
		{
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
			pSM->SetSecurityContext(eRestrictedContext, m_pRestrictedHostContext);
            END_SO_TOLERANT_CODE_CALLING_HOST;
		}
	}
#endif // FEATURE_INCLUDE_ALL_INTERFACES
}

FCIMPL0(FC_BOOL_RET, HostExecutionContextManager::HostPresent)
{
    FCALL_CONTRACT;
    
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    FC_RETURN_BOOL(CorHost2::GetHostSecurityManager() != NULL);
#else // !FEATURE_INCLUDE_ALL_INTERFACES
    FC_RETURN_BOOL(FALSE);
#endif // FEATURE_INCLUDE_ALL_INTERFACES
}
FCIMPLEND

FCIMPL1(HRESULT, HostExecutionContextManager::ReleaseSecurityContext, LPVOID handle)
{
	CONTRACTL {
        FCALL_CHECK;
	 PRECONDITION(CheckPointer(handle));
    	} CONTRACTL_END;

	HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();
	
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
	if (pSM)
	{		
		// get the IUnknown pointer from handle
		IHostSecurityContext* pSecurityContext = (IHostSecurityContext*)handle;
		// null out the IUnknown pointer in the handle
		//hTokenSAFE->SetHandle((void*)NULL);
		// release the IUnknown pointer if it is non null
		if (pSecurityContext != NULL)
		{
			pSecurityContext->Release();			
		}
	}
#endif // FEATURE_INCLUDE_ALL_INTERFACES
	
	HELPER_METHOD_FRAME_END();
	return S_OK;

}
FCIMPLEND

FCIMPL1(HRESULT, HostExecutionContextManager::CaptureSecurityContext, SafeHandle* hTokenUNSAFE)
{
	CONTRACTL {
        FCALL_CHECK;
	 PRECONDITION(CheckPointer(hTokenUNSAFE));
    	} CONTRACTL_END;
	
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	IHostSecurityContext* pCurrentHostSecurityContext = NULL;
	IHostSecurityContext* pCapturedSecurityContext = NULL;
#endif // FEATURE_INCLUDE_ALL_INTERFACES

	HRESULT hr = S_OK;
	SAFEHANDLE hTokenSAFE = (SAFEHANDLE) hTokenUNSAFE;
	HELPER_METHOD_FRAME_BEGIN_RET_1(hTokenSAFE);
	
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
	if (pSM)
	{
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
		hr = pSM->GetSecurityContext(eCurrentContext, &pCurrentHostSecurityContext);
        END_SO_TOLERANT_CODE_CALLING_HOST;
		if (hr == S_OK)
		{
			if(pCurrentHostSecurityContext != NULL)
			{				
                BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
				hr = pCurrentHostSecurityContext->Capture(&pCapturedSecurityContext);				
                END_SO_TOLERANT_CODE_CALLING_HOST;
				hTokenSAFE->SetHandle((void*)pCapturedSecurityContext);
				SafeRelease(pCurrentHostSecurityContext);
			}			
		}
	}
#endif // FEATURE_INCLUDE_ALL_INTERFACES
	
	if (FAILED(hr))
		COMPlusThrowHR(hr);	
	
	HELPER_METHOD_FRAME_END();
	return hr;

}
FCIMPLEND

FCIMPL2(HRESULT, HostExecutionContextManager::CloneSecurityContext, SafeHandle* hTokenUNSAFE, SafeHandle* hTokenClonedUNSAFE)
{
	CONTRACTL {
        FCALL_CHECK;
	PRECONDITION(CheckPointer(hTokenUNSAFE));
	PRECONDITION(CheckPointer(hTokenClonedUNSAFE));
    	} CONTRACTL_END;

	SAFEHANDLE hTokenClonedSAFE = (SAFEHANDLE) hTokenClonedUNSAFE;
	SAFEHANDLE hTokenSAFE = (SAFEHANDLE)hTokenUNSAFE;
	
	HELPER_METHOD_FRAME_BEGIN_RET_2(hTokenSAFE, hTokenClonedSAFE);

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
	if (pSM)
	{		
		IHostSecurityContext* pSecurityContext = (IHostSecurityContext*)hTokenSAFE->GetHandle();
		if (pSecurityContext != NULL)
		{
			pSecurityContext->AddRef();
			hTokenClonedSAFE->SetHandle((void*)pSecurityContext);
		}
	}
#endif // FEATURE_INCLUDE_ALL_INTERFACES
	
	HELPER_METHOD_FRAME_END();
	return S_OK;
}
FCIMPLEND

FCIMPL3(HRESULT, HostExecutionContextManager::SetSecurityContext, SafeHandle* hTokenUNSAFE, CLR_BOOL fReturnPrevious, SafeHandle* hTokenPreviousUNSAFE)
{
	CONTRACTL {
        FCALL_CHECK;
	 PRECONDITION(CheckPointer(hTokenUNSAFE));
    	} CONTRACTL_END;

	HRESULT hr = S_OK;
	
	SAFEHANDLE hTokenPreviousSAFE = (SAFEHANDLE) hTokenPreviousUNSAFE;
	SAFEHANDLE hTokenSAFE = (SAFEHANDLE) hTokenUNSAFE;
	
	HELPER_METHOD_FRAME_BEGIN_RET_2(hTokenSAFE, hTokenPreviousSAFE);

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
	IHostSecurityManager *pSM = CorHost2::GetHostSecurityManager();
	if (pSM)
	{
		if (fReturnPrevious)
		{
			IHostSecurityContext* pPreviousHostSecurityContext = NULL;
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
			hr = pSM->GetSecurityContext(eCurrentContext, &pPreviousHostSecurityContext);
            END_SO_TOLERANT_CODE_CALLING_HOST;
			if (FAILED(hr))
				COMPlusThrowHR(hr);
			// store the previous host context in the safe handle
			hTokenPreviousSAFE->SetHandle((void*)pPreviousHostSecurityContext);
		}
		
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
		hr = pSM->SetSecurityContext(eCurrentContext, (IHostSecurityContext*)hTokenSAFE->GetHandle());
        END_SO_TOLERANT_CODE_CALLING_HOST;
		if (FAILED(hr))
			COMPlusThrowHR(hr);
	}
#endif // FEATURE_INCLUDE_ALL_INTERFACES

	HELPER_METHOD_FRAME_END();
	return hr;
}
FCIMPLEND
#endif // #ifdef FEATURE_CAS_POLICY

