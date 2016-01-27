// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: FusionBind.cpp
**
** Purpose: Implements fusion interface
**
**


===========================================================*/

#include "common.h"

#include <stdlib.h>
#include "fusionbind.h"
#include "shimload.h"
#include "eventtrace.h"
#include "strongnameholders.h"

HRESULT BaseAssemblySpec::ParseName()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!m_pAssemblyName)
        return S_OK;

    CQuickBytes ssName;

    hr = ssName.ConvertUtf8_UnicodeNoThrow(m_pAssemblyName);

    if (SUCCEEDED(hr))
    {
        NonVMComHolder<IAssemblyName> pName;

        IfFailRet(CreateAssemblyNameObject(&pName, (LPCWSTR) ssName.Ptr(), CANOF_PARSE_DISPLAY_NAME, NULL));

        if (m_ownedFlags & NAME_OWNED)
            delete [] m_pAssemblyName;
        m_pAssemblyName = NULL;

        hr = Init(pName);
    }

    return hr;
}

void BaseAssemblySpec::GetFileOrDisplayName(DWORD flags, SString &result) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckValue(result));
        PRECONDITION(result.IsEmpty());
    }
    CONTRACTL_END;

    if (m_pAssemblyName != NULL) {
        NonVMComHolder<IAssemblyName> pFusionName;
        IfFailThrow(CreateFusionName(&pFusionName));

        FusionBind::GetAssemblyNameDisplayName(pFusionName, result, flags);
    }
    else
        result.Set(m_wszCodeBase);
}

HRESULT AssemblySpec::LoadAssembly(IApplicationContext* pFusionContext,
                                 FusionSink *pSink,
                                 IAssembly** ppIAssembly,
                                 IHostAssembly** ppIHostAssembly,
                                 IBindResult** ppNativeFusionAssembly,
                                 BOOL fForIntrospectionOnly,
                                 BOOL fSuppressSecurityChecks)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;
    
    if (!IsAfContentType_Default(m_dwFlags))
    {   // Fusion can process only Default ContentType (non-WindowsRuntime)
        IfFailThrow(COR_E_BADIMAGEFORMAT);
    }
    
    NonVMComHolder<IAssembly> pIAssembly(NULL);
    NonVMComHolder<IBindResult> pNativeFusionAssembly(NULL);    
    NonVMComHolder<IHostAssembly> pIHostAssembly(NULL);
    NonVMComHolder<IAssemblyName> pSpecName;
    NonVMComHolder<IAssemblyName> pCodeBaseName;


    BOOL fFXOnly = FALSE;
    DWORD size = sizeof(fFXOnly);
    
    hr = pFusionContext->Get(ACTAG_FX_ONLY, &fFXOnly, &size, 0);
    if(FAILED(hr))
    {
        /// just in case it corrupted fFXOnly
        fFXOnly = FALSE;
    }

    // reset hr
    hr = E_FAIL;

    // Make sure we don't have malformed names
    
    if (m_pAssemblyName)
        IfFailGo(FusionBind::VerifyBindingString(m_pAssemblyName));

    if (m_context.szLocale)
        IfFailGo(FusionBind::VerifyBindingString(m_context.szLocale));

    // If we have assembly name info, first bind using that
    if (m_pAssemblyName != NULL) {
        IfFailGo(CreateFusionName(&pSpecName, FALSE));

        if(m_fParentLoadContext == LOADCTX_TYPE_UNKNOWN)
        {
            BOOL bOptionallyRetargetable;
            IfFailGo(IsOptionallyRetargetableAssembly(pSpecName, &bOptionallyRetargetable));
            if (bOptionallyRetargetable)
                return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND); // do not propagate to load, let the event handle
        }

        hr = FusionBind::RemoteLoad(pFusionContext, pSink, 
                        pSpecName, GetParentIAssembly(), NULL, 
                        &pIAssembly, &pIHostAssembly, &pNativeFusionAssembly, fForIntrospectionOnly, fSuppressSecurityChecks);
    }


    // Now, bind using the codebase.
    if (FAILED(hr) && !fFXOnly && m_wszCodeBase) {
        // No resolution by code base for SQL-hosted environment, except for introspection
        if((!fForIntrospectionOnly) && CorHost2::IsLoadFromBlocked())
        {
            hr = FUSION_E_LOADFROM_BLOCKED;
            goto ErrExit;
        }
        IfFailGo(CreateAssemblyNameObject(&pCodeBaseName, NULL, 0, NULL));

        IfFailGo(pCodeBaseName->SetProperty(ASM_NAME_CODEBASE_URL,
                                            (void*)m_wszCodeBase, 
                                            (DWORD)(wcslen(m_wszCodeBase) + 1) * sizeof(WCHAR)));

        // Note that we cannot bind a native image using a codebase, as it will
        // always be in the LoadFrom context which does not support native images.

        pSink->Reset();
        hr = FusionBind::RemoteLoad(pFusionContext, pSink, 
                        pCodeBaseName, NULL, m_wszCodeBase,
                        &pIAssembly, &pIHostAssembly, &pNativeFusionAssembly, fForIntrospectionOnly, fSuppressSecurityChecks);

        // If we had both name info and codebase, make sure they are consistent.
        if (SUCCEEDED(hr) && m_pAssemblyName != NULL) {

            NonVMComHolder<IAssemblyName> pPolicyRefName(NULL);
            if (!fForIntrospectionOnly) {
                // Get post-policy ref, because we'll be comparing
                // it against a post-policy def
                HRESULT policyHr = PreBindAssembly(pFusionContext,
                                                   pSpecName,
                                                   NULL, // pAsmParent
                                                   &pPolicyRefName,
                                                   NULL);  // pvReserved
                if (FAILED(policyHr) && (policyHr != FUSION_E_REF_DEF_MISMATCH) &&
                    (policyHr != E_INVALIDARG)) // partial ref
                    IfFailGo(policyHr);
            }

            NonVMComHolder<IAssemblyName> pBoundName;
            if (pIAssembly == NULL)
                IfFailGo(pIHostAssembly->GetAssemblyNameDef(&pBoundName));
            else
                IfFailGo(pIAssembly->GetAssemblyNameDef(&pBoundName));

            // Order matters: Ref->IsEqual(Def)
            HRESULT equalHr;
            if (pPolicyRefName)
                equalHr = pPolicyRefName->IsEqual(pBoundName, ASM_CMPF_DEFAULT);
            else
                equalHr = pSpecName->IsEqual(pBoundName, ASM_CMPF_DEFAULT);
            if (equalHr != S_OK)
            {
                // post-policy name is pBoundName and it's not correct for the  
                // original name, so we need to clear it
                ReleaseNameAfterPolicy();
                IfFailGo(FUSION_E_REF_DEF_MISMATCH);
            }
        }
    }

    // We should have found an assembly by now.
    IfFailGo(hr);

    // <NOTE> Comment about the comment below. The work is done in fusion now. 
    // But we still keep the comment here to illustrate the problem. </NOTE>
    
    // Until we can create multiple Assembly objects for a single HMODULE
    // we can only store one IAssembly* per Assembly. It is very important
    // to maintain the IAssembly* for an image that is in the load-context.
    // An Assembly in the load-from-context can bind to an assembly in the
    // load-context but not visa-versa. Therefore, if we every get an IAssembly
    // from the load-from-context we must make sure that it will never be 
    // found using a load. If it did then we could end up with Assembly dependencies
    // that are wrong. For example, if I do a LoadFrom() on an assembly in the GAC
    // and it requires another Assembly that I have preloaded in the load-from-context
    // then that dependency gets burnt into the Jitted code. Later on a Load() is
    // done on the assembly in the GAC and we single instance it back to the one
    // we have gotten from the load-from-context because the HMODULES are the same.
    // Now the dependency is wrong because it would not have the preloaded assembly
    // if the order was reversed.

#if 0    
    if (!fForIntrospectionOnly)
    {
        NonVMComHolder<IFusionLoadContext> pLoadContext;
        if (pIAssembly == NULL)
            IfFailGo(pIHostAssembly->GetFusionLoadContext(&pLoadContext));
        else
            IfFailGo(pIAssembly->GetFusionLoadContext(&pLoadContext));
     
        if (pLoadContext->GetContextType() == LOADCTX_TYPE_LOADFROM) {
            _ASSERTE(pIAssembly != NULL);
            HRESULT hrLocal;
    
            NonVMComHolder<IAssemblyName> pBoundName;
            pIAssembly->GetAssemblyNameDef(&pBoundName);
    
            // We need to copy the bound name to modify it
            IAssemblyName *pClone;
            IfFailGo(pBoundName->Clone(&pClone));
            pBoundName.Release();
            pBoundName = pClone;
    
            // Null out the architecture for the second bind
            IfFailGo(pBoundName->SetProperty(ASM_NAME_ARCHITECTURE, NULL, 0));
    
            NonVMComHolder<IAssembly> pAliasingAssembly;
            NonVMComHolder<IHostAssembly> pIHA;
            pSink->Reset();
            hrLocal = FusionBind::RemoteLoad(pFusionContext, pSink,
                                 pBoundName, NULL, NULL, 
                                 &pAliasingAssembly, &pIHA, fForIntrospectionOnly);
    
            if(SUCCEEDED(hrLocal)) {
                // If the paths are the same or the loadfrom assembly is in the GAC,
                // then use the non-LoadFrom assembly as the result.
    
                DWORD location;
                hrLocal = pIAssembly->GetAssemblyLocation(&location); 
                BOOL alias = (SUCCEEDED(hrLocal) && location == ASMLOC_GAC);
    
                if (!alias)  {
                    SString boundPath;
                    GetAssemblyManifestModulePath(pIAssembly, boundPath);
    
                    SString aliasingPath;
                    GetAssemblyManifestModulePath(pAliasingAssembly, aliasingPath);
    
                    alias = SString::_wcsicmp(boundPath, aliasingPath) == 0;
                }
    
                // Keep the default context's IAssembly if the paths are the same
                if (alias) 
                    pIAssembly = pAliasingAssembly.Extract();
            }
        }
    }
#endif

    if (SUCCEEDED(hr)) {
        if (pIAssembly == NULL)
            *ppIHostAssembly = pIHostAssembly.Extract();
        else
            *ppIAssembly = pIAssembly.Extract();
        if (ppNativeFusionAssembly) {
            *ppNativeFusionAssembly = pNativeFusionAssembly.Extract();
        }
    }

 ErrExit:
    return hr;
}


/* static */
HRESULT FusionBind::RemoteLoad(IApplicationContext* pFusionContext,
                               FusionSink *pSink,
                               IAssemblyName *pName,
                               IAssembly *pParentAssembly,
                               LPCWSTR pCodeBase,
                               IAssembly** ppIAssembly,
                               IHostAssembly** ppIHostAssembly,
                               IBindResult **ppNativeFusionAssembly,
                               BOOL    fForIntrospectionOnly,
                               BOOL fSuppressSecurityChecks)
{
    CONTRACTL
    {
        THROWS;
        MODE_PREEMPTIVE;
        // The resulting IP must be held so the assembly will not be scavenged.
        PRECONDITION(CheckPointer(ppIAssembly));
        PRECONDITION(CheckPointer(ppIHostAssembly));

        PRECONDITION(CheckPointer(pName));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    } CONTRACTL_END;

    ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);

    HRESULT hr;
    ASM_BIND_FLAGS dwFlags = ASM_BINDF_NONE;
    DWORD dwReserved = 0;
    LPVOID pReserved = NULL;

    // Event Tracing for Windows is used to log data for performance and functional testing purposes.
    // The events below are used to help measure the performance of the download phase of assembly binding (be it download of a remote file or accessing a local file on disk),
    // as well as of lookup scenarios such as from a host store.
    DWORD dwAppDomainId = ETWAppDomainIdNotAvailable;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        DWORD cbValue = sizeof(dwAppDomainId);
        // Gather data used by ETW events later in this function.
        if (pFusionContext == NULL || FAILED(pFusionContext->Get(ACTAG_APP_DOMAIN_ID, &dwAppDomainId, &cbValue, 0))) {
            dwAppDomainId = ETWAppDomainIdNotAvailable;
        }
    }

    NonVMComHolder< IUnknown > pSinkIUnknown(NULL);
    NonVMComHolder< IAssemblyNameBinder> pBinder(NULL);
    *ppNativeFusionAssembly=NULL;

    if(pParentAssembly != NULL) {
        // Only use a parent assembly hint when the parent assembly has a load context.
        // Assemblies in anonymous context are not dicoverable by loader's binding rules,
        // thus loader can't find their dependencies. 
        // Loader will only try to locate dependencies in default load context.
        if (pParentAssembly->GetFusionLoadContext() != LOADCTX_TYPE_UNKNOWN) {
            dwReserved = sizeof(IAssembly*);
            pReserved = (LPVOID) pParentAssembly;
            dwFlags = ASM_BINDF_PARENT_ASM_HINT;
        }
    }
    
    IfFailRet(pSink->AssemblyResetEvent());
    IfFailRet(pSink->QueryInterface(IID_IUnknown, (void**)&pSinkIUnknown));
    IUnknown *pFusionAssembly=NULL;
    IUnknown  *pNativeAssembly=NULL;
    BOOL fCached = TRUE;


    if (fForIntrospectionOnly)
    {
        dwFlags = (ASM_BIND_FLAGS)(dwFlags | ASM_BINDF_INSPECTION_ONLY);
    }

    if (fSuppressSecurityChecks)
    {
        dwFlags = (ASM_BIND_FLAGS)(dwFlags | ASM_BINDF_SUPPRESS_SECURITY_CHECKS);
    }

    IfFailRet(pName->QueryInterface(IID_IAssemblyNameBinder, (void **)&pBinder));
    {
        // In SQL, this can call back into the runtime
        CONTRACT_VIOLATION(ThrowsViolation);
        hr = pBinder->BindToObject(IID_IAssembly,
                                 pSinkIUnknown,
                                 pFusionContext,
                                 pCodeBase,
                                 dwFlags,
                                 pReserved,
                                 dwReserved,
                                 (void**) &pFusionAssembly,
                                 (void**)&pNativeAssembly);
    }
    
    if(hr == E_PENDING) {
        // If there is an assembly IP then we were successful.
        hr = pSink->Wait();
        if (SUCCEEDED(hr))
            hr = pSink->LastResult();
        if(SUCCEEDED(hr)) {
            if(pSink->m_punk) {
                if (pSink->m_pNIunk)
                    pNativeAssembly=pSink->m_pNIunk;
                pFusionAssembly = pSink->m_punk;
                fCached = FALSE;
            }
            else
                hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }
    }

    FireEtwBindingDownloadPhaseEnd(dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, pCodeBase, NULL, GetClrInstanceId());

    FireEtwBindingLookupAndProbingPhaseEnd(dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, pCodeBase, NULL, GetClrInstanceId());

    if (SUCCEEDED(hr)) {
        // Keep a handle to ensure it does not disappear from the cache
        // and allow access to modules associated with the assembly.
        hr = pFusionAssembly->QueryInterface(IID_IAssembly, 
                                             (void**) ppIAssembly);
        if (hr == E_NOINTERFACE) // IStream assembly
            hr = pFusionAssembly->QueryInterface(IID_IHostAssembly, 
                                                 (void**) ppIHostAssembly);
        if (SUCCEEDED(hr) && pNativeAssembly)
            hr=pNativeAssembly->QueryInterface(IID_IBindResult,
                                            (void**)ppNativeFusionAssembly);

        if (fCached)
        {
            pFusionAssembly->Release();
            if(pNativeAssembly)
                pNativeAssembly->Release();
        }
    }

    return hr;
}

/* static */
HRESULT FusionBind::RemoteLoadModule(IApplicationContext * pFusionContext, 
                                     IAssemblyModuleImport* pModule, 
                                     FusionSink *pSink,
                                     IAssemblyModuleImport** pResult)
{
    
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(pFusionContext));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pSink));
        PRECONDITION(CheckPointer(pResult));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    } CONTRACTL_END;

    ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);

    HRESULT hr;
    IfFailGo(pSink->AssemblyResetEvent());
    hr = pModule->BindToObject(pSink,
                               pFusionContext,
                               ASM_BINDF_NONE,
                               (void**) pResult);
    if(hr == E_PENDING) {
        // If there is an assembly IP then we were successful.
        hr = pSink->Wait();
        if (SUCCEEDED(hr))
            hr = pSink->LastResult();
        if (SUCCEEDED(hr)) {
            if(pSink->m_punk)
                hr = pSink->m_punk->QueryInterface(IID_IAssemblyModuleImport, 
                                                   (void**) pResult);
            else
                hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }
    }

 ErrExit:
    return hr;
}


/* static */
HRESULT FusionBind::AddEnvironmentProperty(__in LPCWSTR variable, 
                                           __in LPCWSTR pProperty, 
                                           IApplicationContext* pFusionContext)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(pProperty));
        PRECONDITION(CheckPointer(variable));
        PRECONDITION(CheckPointer(pFusionContext));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    DWORD size = _MAX_PATH;
    WCHAR rcValue[_MAX_PATH];    // Buffer for the directory.
    WCHAR *pValue = &(rcValue[0]);
    size = WszGetEnvironmentVariable(variable, pValue, size);
    if(size > _MAX_PATH) {
        pValue = (WCHAR*) _alloca(size * sizeof(WCHAR));
        size = WszGetEnvironmentVariable(variable, pValue, size);
        size++; // Add in the null terminator
    }

    if(size)
        return pFusionContext->Set(pProperty,
                                   pValue,
                                   size * sizeof(WCHAR),
                                   0);
    else 
        return S_FALSE; // no variable found
}

// Fusion uses a context class to drive resolution of assemblies.
// Each application has properties that can be pushed into the
// fusion context (see fusionp.h). The public api is part of
// application domains.
/* static */
HRESULT FusionBind::SetupFusionContext(LPCWSTR szAppBase,
                                       LPCWSTR szPrivateBin,
                                       IApplicationContext** ppFusionContext)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(ppFusionContext));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr;
    NonVMComHolder <IApplicationContext> pFusionContext;

    LPCWSTR pBase;
    // if the appbase is null then use the current directory
    if (szAppBase == NULL) {
        pBase = (LPCWSTR) _alloca(_MAX_PATH * sizeof(WCHAR));
        if(!WszGetCurrentDirectory(_MAX_PATH, (LPWSTR) pBase))
            IfFailGo(HRESULT_FROM_GetLastError());
    }
    else
        pBase = szAppBase;


    IfFailGo(CreateFusionContext(pBase, &pFusionContext));

    
    IfFailGo((pFusionContext)->Set(ACTAG_APP_BASE_URL,
                                   (void*) pBase,
                                   (DWORD)(wcslen(pBase) + 1) * sizeof(WCHAR),
                                   0));
        
    if (szPrivateBin)
        IfFailGo((pFusionContext)->Set(ACTAG_APP_PRIVATE_BINPATH,
                                       (void*) szPrivateBin,
                                       (DWORD)(wcslen(szPrivateBin) + 1) * sizeof(WCHAR),
                                       0));
    else
        IfFailGo(AddEnvironmentProperty(APPENV_RELATIVEPATH, ACTAG_APP_PRIVATE_BINPATH, pFusionContext));

    *ppFusionContext=pFusionContext;
    pFusionContext.SuppressRelease();
    
ErrExit:    
    return hr;
}

/* static */
HRESULT FusionBind::CreateFusionContext(LPCWSTR pzName, IApplicationContext** ppFusionContext)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(ppFusionContext));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    // This is a file name not a namespace
    LPCWSTR contextName = NULL;

    if(pzName) {
        contextName = wcsrchr( pzName, W('\\') );
        if(contextName)
            contextName++;
        else
            contextName = pzName;
    }
    // We go off and create a fusion context for this application domain.
    // Note, once it is made it can not be modified.
    NonVMComHolder<IAssemblyName> pFusionAssemblyName;
    HRESULT hr = CreateAssemblyNameObject(&pFusionAssemblyName, contextName, 0, NULL);

    if(SUCCEEDED(hr))
        hr = CreateApplicationContext(pFusionAssemblyName, ppFusionContext);
    
    return hr;
}

/* static */
HRESULT FusionBind::GetVersion(__out_ecount(*pdwVersion) LPWSTR pVersion, __inout DWORD* pdwVersion)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(pdwVersion));
        PRECONDITION(pdwVersion>0 && CheckPointer(pVersion));
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    DWORD dwCORSystem = 0;
    
    LPCWSTR pCORSystem = GetInternalSystemDirectory(&dwCORSystem);
    
    if (dwCORSystem == 0) 
        return E_FAIL;

    dwCORSystem--; // remove the null character
    if (dwCORSystem && pCORSystem[dwCORSystem-1] == W('\\'))
        dwCORSystem--; // and the trailing slash if it exists

    if (dwCORSystem==0)
        return E_FAIL;

    const WCHAR* pSeparator;
    const WCHAR* pTail = pCORSystem + dwCORSystem;

    for (pSeparator = pCORSystem+dwCORSystem-1; pSeparator > pCORSystem && *pSeparator != W('\\');pSeparator--);

    if (*pSeparator == W('\\'))
        pSeparator++;
    
    DWORD lgth = (DWORD)(pTail - pSeparator);
    
    if (lgth > *pdwVersion) {
        *pdwVersion = lgth+1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26000) // "Disable PREFast/espX warning about buffer overflow"
#endif
    while(pSeparator < pTail) 
        *pVersion++ = *pSeparator++;

    *pVersion = W('\0');
#ifdef _PREFAST_
#pragma warning(pop)
#endif

    return S_OK;
} // FusionBind::GetVersion

