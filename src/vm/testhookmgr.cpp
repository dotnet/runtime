// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

#include "common.h"
#include "testhookmgr.h"
#include "appdomain.hpp"
#include "appdomain.inl"
#include "finalizerthread.h"

#ifdef FEATURE_TESTHOOKS
CLRTestHookManager* CLRTestHookManager::g_pManager=NULL;
CLRTestHookManager::~CLRTestHookManager()
{

}

HRESULT CLRTestHookManager::AddTestHook(ICLRTestHook* hook)
{
    WRAPPER_NO_CONTRACT;
    DWORD newidx=FastInterlockIncrement(&m_nHooks);
    if (newidx>=NumItems(m_pHooks))
    {
        FastInterlockDecrement(&m_nHooks);
        return DISP_E_OVERFLOW;
    }
    m_pHooks[newidx-1].Set(hook);
    return S_OK;
}


ICLRTestHookManager* CLRTestHookManager::Start()
{
    LIMITED_METHOD_CONTRACT;
    if (g_pManager==NULL)
    {
        CLRTestHookManager* newman=new (nothrow)CLRTestHookManager();
        if (newman!=NULL && FastInterlockCompareExchangePointer(&g_pManager, newman, 0)!=0)
            delete newman;
    }
    if(g_pManager)
        g_pManager->AddRef();
    return g_pManager;
}

CLRTestHookManager::CLRTestHookManager()
{
    WRAPPER_NO_CONTRACT;
    m_nHooks=0;
    m_cRef=1;
    ZeroMemory(m_pHooks,sizeof(m_pHooks));
}

HRESULT CLRTestHookManager::AppDomainStageChanged(DWORD adid,DWORD oldstage,DWORD newstage)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD adid;
        DWORD oldstage;
        DWORD newstage;
    } param;
    param.pThis = this;
    param.adid = adid;
    param.oldstage = oldstage;
    param.newstage = newstage;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->AppDomainStageChanged(pParam->adid, pParam->oldstage, pParam->newstage);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY;
 
    return S_OK;
};


HRESULT CLRTestHookManager::NextFileLoadLevel(DWORD adid, LPVOID domainfile,DWORD newlevel)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->NextFileLoadLevel( adid,  domainfile, newlevel);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }
    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::CompletingFileLoadLevel(DWORD adid, LPVOID domainfile,DWORD newlevel)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->CompletingFileLoadLevel( adid,  domainfile, newlevel);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }

        
    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::CompletedFileLoadLevel(DWORD adid, LPVOID domainfile,DWORD newlevel)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD adid;
        LPVOID domainfile;
        DWORD newlevel;
    } param;
    param.pThis = this;
    param.adid = adid;
    param.domainfile = domainfile;
    param.newlevel = newlevel;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->CompletedFileLoadLevel(pParam->adid, pParam->domainfile, pParam->newlevel);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;
}

HRESULT CLRTestHookManager::EnteringAppDomain(DWORD adid)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->EnteringAppDomain(adid);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }
    IfFailThrow(hr);    
    return hr;
}

HRESULT CLRTestHookManager::EnteredAppDomain(DWORD adid)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->EnteredAppDomain(adid);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }        
    IfFailThrow(hr);    
    return hr;
}

HRESULT CLRTestHookManager::LeavingAppDomain(DWORD adid)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->LeavingAppDomain(adid);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }
    IfFailThrow(hr);    
    return hr;
}

HRESULT CLRTestHookManager::LeftAppDomain(DWORD adid)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->LeftAppDomain(adid);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }
    IfFailThrow(hr);   
    return hr;
}

HRESULT CLRTestHookManager::UnwindingThreads(DWORD adid)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD adid;
    } param;
    param.pThis = this;
    param.adid = adid;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->UnwindingThreads(pParam->adid);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;
}

HRESULT CLRTestHookManager::RuntimeStarted(DWORD code)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD code;
    } param;
    param.pThis = this;
    param.code = code;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->RuntimeStarted(pParam->code);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;
}

HRESULT CLRTestHookManager::UnwoundThreads(DWORD adid)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD adid;
    } param;
    param.pThis = this;
    param.adid = adid;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->UnwoundThreads(pParam->adid);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;
}

HRESULT CLRTestHookManager::AppDomainDestroyed(DWORD adid)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        DWORD adid;
    } param;
    param.pThis = this;
    param.adid = adid;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook* hook = pParam->pThis->m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr=hook->AppDomainDestroyed(pParam->adid);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;
}

STDMETHODIMP CLRTestHookManager::ImageMapped(LPCWSTR wszPath, LPCVOID pBaseAddress,DWORD flags)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLRTestHookManager *pThis;
        LPCWSTR wszPath;
        LPCVOID pBaseAddress;
        DWORD flags;
    } param;
    param.pThis = this;
    param.wszPath = wszPath;
    param.pBaseAddress = pBaseAddress;
    param.flags = flags;

    PAL_TRY(Param *, pParam, &param)
    {
        //ignores the returned codes
        for (LONG i = 0; i < pParam->pThis->m_nHooks; i++)
        {
            ICLRTestHook2* hook = pParam->pThis->m_pHooks[i].v2();
            if(hook)
            {
                HRESULT hr=hook->ImageMapped(pParam->wszPath,pParam->pBaseAddress,pParam->flags);
                _ASSERTE(SUCCEEDED(hr));
            }
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Test Hook threw an exception.");
    }
    PAL_ENDTRY
        
    return S_OK;

}

HRESULT CLRTestHookManager::AppDomainCanBeUnloaded(DWORD adid, BOOL bUnsafePoint)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        if (!ThreadCanBeAborted())
            return S_OK;
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook* hook=m_pHooks[i].v1();
            if(hook)
            {
                HRESULT hr2=hook->AppDomainCanBeUnloaded(adid,bUnsafePoint);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }
    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::StartingNativeImageBind(LPCWSTR wszAsmName, BOOL bIsCompilationProcess)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook3* hook=m_pHooks[i].v3();
            if(hook)
            {
                HRESULT hr2=hook->StartingNativeImageBind(wszAsmName, bIsCompilationProcess);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }

    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::CompletedNativeImageBind(LPVOID pFile,LPCUTF8 simpleName, BOOL hasNativeImage)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook3* hook=m_pHooks[i].v3();
            if(hook)
            {
                HRESULT hr2=hook->CompletedNativeImageBind(pFile, simpleName, hasNativeImage);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }

    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::AboutToLockImage(LPCWSTR wszPath, BOOL bIsCompilationProcess)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    {
        for (LONG i=0;i<m_nHooks;i++)
        {
            ICLRTestHook3* hook=m_pHooks[i].v3();
            if(hook)
            {
                HRESULT hr2=hook->AboutToLockImage(wszPath, bIsCompilationProcess);
                _ASSERTE(SUCCEEDED(hr)||SUCCEEDED(hr2));
                if (SUCCEEDED(hr))
                    hr=hr2;
            }
        }
    }

    IfFailThrow(hr);
    return hr;
}

HRESULT CLRTestHookManager::EnableSlowPath (BOOL bEnable)
{
    WRAPPER_NO_CONTRACT;
    ThreadStore::TrapReturningThreads(bEnable);
    return S_OK;
}

ULONG CLRTestHookManager::AddRef()
{
    return FastInterlockIncrement(&m_cRef);
}

ULONG CLRTestHookManager::Release()
{
    ULONG nRet= FastInterlockDecrement(&m_cRef);
    // never goes away
    return nRet;
}

HRESULT CLRTestHookManager::QueryInterface(REFIID riid, void **ppv)
{
    if (riid!=IID_IUnknown && riid!=IID_ICLRTestHookManager)
        return E_NOINTERFACE;
    AddRef();
    *ppv=(ICLRTestHookManager*)this;
    return S_OK;
}


HRESULT CLRTestHookManager::CheckConfig()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;
    if (g_pConfig)
    {
        LPWSTR szTestHooks=NULL;
        hr=CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TestHooks,&szTestHooks);
        if (SUCCEEDED(hr) && szTestHooks!=NULL && *szTestHooks!=W('\0'))
        {
            LPWSTR curr=szTestHooks;
            do
            {
                LPWSTR next=wcschr(curr,W(';'));
                if (next)
                    *(next++)=0;
                LPWSTR delim=wcschr(curr,W(','));
                if (delim)
                {
                    *(delim++)=W('\0');
                    HMODULE hMod=WszLoadLibrary(curr);
                    _ASSERTE(hMod);
                    if (hMod!=NULL)
                    {
                        MAKE_MULTIBYTE_FROMWIDE(szFName,delim,CP_ACP);
                        CLRTESTHOOKPROC* fn=(CLRTESTHOOKPROC*)GetProcAddress(hMod,szFName);
                        _ASSERTE(fn);
                        if(fn)
                            fn(Start());
                    }
                }
                curr=next;
            }
            while(curr!=NULL && *curr!=W('\0'));
            
            delete szTestHooks;
        }
    }
    return hr;
}


HRESULT CLRTestHookManager::UnloadAppDomain(DWORD adid,DWORD flags)
{
    return COR_E_CANNOTUNLOADAPPDOMAIN;
}

VOID CLRTestHookManager::DoAppropriateWait( int cObjs, HANDLE *pObjs, INT32 iTimeout, BOOL bWaitAll, int* res)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    Thread* thread=GetThread();
    DWORD result = WAIT_FAILED;
    if(thread)
        result=thread->DoAppropriateWait(cObjs,pObjs,bWaitAll,iTimeout,WaitMode_Alertable,NULL);
    else
    {
        result = WaitForMultipleObjectsEx(cObjs,pObjs,bWaitAll,iTimeout,TRUE);
    }
}


HRESULT CLRTestHookManager::GC(int generation)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(GetThread()==NULL || !GetThread()->PreemptiveGCDisabled());
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation);
    FinalizerThread::FinalizerThreadWait();
    return S_OK;
}


HRESULT CLRTestHookManager::GetSimpleName(LPVOID domainfile,LPCUTF8* name)
{
    HRESULT hr=S_OK;
    EX_TRY
    {
        *name=((DomainFile*)domainfile)->GetSimpleName();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}



INT_PTR CLRTestHookManager::GetCurrentThreadType()
{
    WRAPPER_NO_CONTRACT;
    return (INT_PTR) ClrFlsGetValue (TlsIdx_ThreadType);
}

INT_PTR CLRTestHookManager::GetCurrentThreadLockCount (VOID)
{
    LIMITED_METHOD_CONTRACT;
    Thread* thread=GetThread();
    if(!thread)
        return 0;
    return thread->m_dwLockCount;

};


BOOL CLRTestHookManager::IsPreemptiveGC (VOID)
{
    LIMITED_METHOD_CONTRACT;
    Thread *thread = GetThread();
    // Preemptive GC is default
    if (thread == NULL)
        return TRUE;
    else
        return !thread->PreemptiveGCDisabled();
};


BOOL CLRTestHookManager::ThreadCanBeAborted (VOID) 
{
    LIMITED_METHOD_CONTRACT;
    return (GetThread()==NULL || GetThread()->IsAbortPrevented() || GetThread()->IsAsyncPrevented())?FALSE:TRUE;
}

HRESULT CLRTestHookManager::HasNativeImage(LPVOID domainfile,BOOL* pHasNativeImage)
{
    STATIC_CONTRACT_THROWS;
    HRESULT hr=S_OK;
    EX_TRY
    {
        if (domainfile && ((DomainFile*)domainfile)->GetFile())
        {
            *pHasNativeImage=((DomainFile*)domainfile)->GetFile()->HasNativeImage();
        }
        else
            *pHasNativeImage = 0;     
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


void  CLRTestHookInfo::Set(ICLRTestHook* hook)
{
    LIMITED_METHOD_CONTRACT;
    if (SUCCEEDED(hook->QueryInterface(IID_ICLRTestHook3,(void**)&m_Hook.v3)))
    {
        m_Version=3;
        return;
    }
    else if (SUCCEEDED(hook->QueryInterface(IID_ICLRTestHook2,(void**)&m_Hook.v2)))
    {
        m_Version=2;
        return;
    }
    else
    {
        m_Version=1;
    }
    hook->AddRef();
    m_Hook.v1=hook;
}

ICLRTestHook*  CLRTestHookInfo::v1()
{
    return m_Hook.v1;
}

ICLRTestHook2*  CLRTestHookInfo::v2()
{
    LIMITED_METHOD_CONTRACT;
    if(m_Version==2)
        return m_Hook.v2;
    return NULL;
}

ICLRTestHook3*  CLRTestHookInfo::v3()
{
    if(m_Version>=3)
        return m_Hook.v3;
    return NULL;
}



//to make sure CLRTestHook is ok
static CLRTestHook _hook; 

#endif


