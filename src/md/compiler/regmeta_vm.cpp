// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

    
//*****************************************************************************

// 
// RegMeta.cpp
//
// Implementation for meta data public interface methods for full version.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "mdperf.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include <metamodelrw.h>


#ifndef FEATURE_CORECLR

#include <metahost.h>

// Pointer to the activated CLR interface provided by the shim.
extern ICLRRuntimeInfo *g_pCLRRuntime;

#ifdef FEATURE_METADATA_EMIT_ALL

#include "iappdomainsetup.h"

// {27FFF232-A7A8-40dd-8D4A-734AD59FCD41}
EXTERN_GUID(IID_IAppDomainSetup, 0x27FFF232, 0xA7A8, 0x40dd, 0x8D, 0x4A, 0x73, 0x4A, 0xD5, 0x9F, 0xCD, 0x41);

#endif //FEATURE_METADATA_EMIT_ALL

#endif // !FEATURE_CORECLR


#define DEFINE_CUSTOM_NODUPCHECK    1
#define DEFINE_CUSTOM_DUPCHECK      2
#define SET_CUSTOM                  3

#if defined(_DEBUG) && defined(_TRACE_REMAPS)
#define LOGGING
#endif
#include <log.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

//*****************************************************************************
// Call this after initialization is complete.
//*****************************************************************************
HRESULT RegMeta::AddToCache()
{
#if defined(FEATURE_METADATA_IN_VM) || defined(FEATURE_METADATA_STANDALONE_WINRT)
    HRESULT hr = S_OK;

    // The ref count must be > 0 before the module is published, else another
    //  thread could find, use, and release the module, causing it to be deleted
    //  before this thread gets a chance to addref.
    _ASSERTE(GetRefCount() > 0);
    // add this RegMeta to the loaded module list.
    m_bCached = true;
    IfFailGo(LOADEDMODULES::AddModuleToLoadedList(this));
ErrExit:
    if (FAILED(hr))
    {
        _ASSERTE(!LOADEDMODULES::IsEntryInList(this));
        m_bCached = false;
    }
    return hr;
#else //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
    return S_OK;
#endif //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
} // RegMeta::AddToCache


//*****************************************************************************
// Search the cached RegMetas for a given scope.
//*****************************************************************************
HRESULT RegMeta::FindCachedReadOnlyEntry(
    LPCWSTR     szName,                 // Name of the desired file.
    DWORD       dwOpenFlags,            // Flags the new file is opened with.
    RegMeta     **ppMeta)               // Put found RegMeta here.
{
#if defined(FEATURE_METADATA_IN_VM) || defined(FEATURE_METADATA_STANDALONE_WINRT)
    return LOADEDMODULES::FindCachedReadOnlyEntry(szName, dwOpenFlags, ppMeta);
#else //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
    // No cache support in standalone version.
    *ppMeta = NULL;
    return S_FALSE;
#endif //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
} // RegMeta::FindCachedReadOnlyEntry


#ifdef FEATURE_METADATA_EMIT_ALL

//*****************************************************************************
// Helper function to startup the EE
// 
// Notes:
//    This is called by code:RegMeta.DefineSecurityAttributeSet.
//*****************************************************************************
HRESULT RegMeta::StartupEE()
{
#ifdef FEATURE_CORECLR
    UNREACHABLE_MSG_RET("About to CoCreateInstance!  This code should not be "
                        "reachable or needs to be reimplemented for CoreCLR!");
#else // !FEATURE_CORECLR

    struct Param
    {
        RegMeta *pThis;
        IUnknown *pSetup;
        IAppDomainSetup *pDomainSetup;
        bool fDoneStart;
        HRESULT hr;
    } param;
    param.pThis = this;
    param.pSetup = NULL;
    param.pDomainSetup = NULL;
    param.fDoneStart = false;
    param.hr = S_OK;

    PAL_TRY(Param *, pParam, &param)
    {
        HRESULT hr = S_OK;

        DWORD dwBuffer[1 + (MAX_LONGPATH+1) * sizeof(WCHAR) / sizeof(DWORD) + 1];
        BSTR  bstrDir = NULL;

        // Create a hosting environment.
        IfFailGo(g_pCLRRuntime->GetInterface(
            CLSID_CorRuntimeHost, 
            IID_ICorRuntimeHost, 
            (void **)&pParam->pThis->m_pCorHost));

        // Startup the runtime.
        IfFailGo(pParam->pThis->m_pCorHost->Start());
        pParam->fDoneStart = true;

        // Create an AppDomain Setup so we can set the AppBase.
        IfFailGo(pParam->pThis->m_pCorHost->CreateDomainSetup(&pParam->pSetup));

        // Get the current directory (place it in a BSTR).
        bstrDir = (BSTR)(dwBuffer + 1);
        if ((dwBuffer[0] = (WszGetCurrentDirectory(MAX_LONGPATH + 1, bstrDir) * sizeof(WCHAR))))
        {
            // QI for the IAppDomainSetup interface.
            IfFailGo(pParam->pSetup->QueryInterface(IID_IAppDomainSetup,
                                            (void**)&pParam->pDomainSetup));

            // Set the AppBase.
            pParam->pDomainSetup->put_ApplicationBase(bstrDir);
        }

        // Create a new AppDomain.
        IfFailGo(pParam->pThis->m_pCorHost->CreateDomainEx(W("Compilation Domain"),
                                            pParam->pSetup,
                                            NULL,
                                            &pParam->pThis->m_pAppDomain));

        // That's it, we're all set up.
        _ASSERTE(pParam->pThis->m_pAppDomain != NULL);
        pParam->pThis->m_fStartedEE = true;

    ErrExit:
        pParam->hr = hr;
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE(!"Unexpected exception setting up hosting environment for security attributes");
        param.hr = E_FAIL;
    }
    PAL_ENDTRY

    // Cleanup temporary resources.
    if (m_pAppDomain && FAILED(param.hr))
        m_pAppDomain->Release();
    if (param.pDomainSetup)
        param.pDomainSetup->Release();
    if (param.pSetup)
        param.pSetup->Release();
    if (param.fDoneStart && FAILED(param.hr))
        m_pCorHost->Stop();
    if (m_pCorHost && FAILED(param.hr))
        m_pCorHost->Release();
    return param.hr;
#endif // FEATURE_CORECLR
}

#endif //FEATURE_METADATA_EMIT_ALL

#ifdef FEATURE_METADATA_EMIT

//*****************************************************************************
// Persist a set of security custom attributes into a set of permission set
// blobs on the same class or method.
// 
// Notes:
//    Only in the full version because this is an emit operation.
//*****************************************************************************
HRESULT RegMeta::DefineSecurityAttributeSet(// Return code.
    mdToken     tkObj,                  // [IN] Class or method requiring security attributes.
    COR_SECATTR rSecAttrs[],            // [IN] Array of security attribute descriptions.
    ULONG       cSecAttrs,              // [IN] Count of elements in above array.
    ULONG       *pulErrorAttr)          // [OUT] On error, index of attribute causing problem.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    NewArrayHolder <CORSEC_ATTRSET> rAttrSets;
    DWORD           i;
    mdPermission    ps;
    DWORD           dwAction;
    bool fProcessDeclarativeSecurityAtRuntime;

    LOG((LOGMD, "RegMeta::DefineSecurityAttributeSet(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
         tkObj, rSecAttrs, cSecAttrs, pulErrorAttr));
    START_MD_PERF();
    LOCKWRITE();
    
    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
    
    rAttrSets = new (nothrow) CORSEC_ATTRSET[dclMaximumValue + 1];
    if (rAttrSets == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto ErrExit;
    }

    memset(rAttrSets, 0, sizeof(CORSEC_ATTRSET) * (dclMaximumValue + 1));

    // Initialize error index to indicate a general error.
    if (pulErrorAttr)
        *pulErrorAttr = cSecAttrs;

    fProcessDeclarativeSecurityAtRuntime = true;

    // See if we should default to old v1.0/v1.1 serialization behavior
    if (m_OptionValue.m_MetadataVersion < MDVersion2)
        fProcessDeclarativeSecurityAtRuntime = false;

    // Startup the EE just once, no matter how many times we're called (this is
    // better on performance and the EE falls over if we try a start-stop-start
    // cycle anyway).
    if (!m_fStartedEE && !fProcessDeclarativeSecurityAtRuntime)
    {
        IfFailGo(StartupEE());
    }

    // Group the security attributes by SecurityAction (thus creating an array of CORSEC_PERM's)
    IfFailGo(GroupSecurityAttributesByAction(/*OUT*/rAttrSets, rSecAttrs, cSecAttrs, tkObj, pulErrorAttr, &m_pStgdb->m_MiniMd, NULL));
    
    // Put appropriate data in the metadata
    for (i = 0; i <= dclMaximumValue; i++) 
    {
        NewArrayHolder <BYTE>    pbBlob(NULL);
        NewArrayHolder <BYTE>    pbNonCasBlob(NULL);
        DWORD              cbBlob = 0;
        DWORD              cbNonCasBlob = 0;

        rAttrSets[i].pImport = this;
        rAttrSets[i].pAppDomain = m_pAppDomain;
        if (rAttrSets[i].dwAttrCount == 0)
            continue;
        if (pulErrorAttr)
            *pulErrorAttr = i;

        if(fProcessDeclarativeSecurityAtRuntime)
        {
            // Put a serialized CORSEC_ATTRSET in the metadata
            SIZE_T cbAttrSet = 0;
            IfFailGo(AttributeSetToBlob(&rAttrSets[i], NULL, &cbAttrSet, this, i)); // count size required for buffer
            if (!FitsIn<DWORD>(cbAttrSet))
            {
                hr = COR_E_OVERFLOW;
                goto ErrExit;
            }
            cbBlob = static_cast<DWORD>(cbAttrSet);

            pbBlob = new (nothrow) BYTE[cbBlob]; // allocate buffer
            if (pbBlob == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto ErrExit;
            }

            IfFailGo(AttributeSetToBlob(&rAttrSets[i], pbBlob, NULL, this, i)); // serialize into the buffer
            IfFailGo(_DefinePermissionSet(rAttrSets[i].tkObj, rAttrSets[i].dwAction, pbBlob, cbBlob, &ps)); // put it in metadata
        }
        else
        {
            // Now translate the sets of security attributes into a real permission
            // set and convert this to a serialized Xml blob. We may possibly end up
            // with two sets as the result of splitting CAS and non-CAS permissions
            // into separate sets.
            hr = TranslateSecurityAttributes(&rAttrSets[i], &pbBlob, &cbBlob, &pbNonCasBlob, &cbNonCasBlob, pulErrorAttr);
            IfFailGo(hr);

            // Persist the permission set blob into the metadata. For empty CAS
            // blobs this is only done if the corresponding non-CAS blob is empty
            if (cbBlob || !cbNonCasBlob)
                IfFailGo(_DefinePermissionSet(rAttrSets[i].tkObj, rAttrSets[i].dwAction, pbBlob, cbBlob, &ps));
            
            if (pbNonCasBlob)
            {
                // Map the SecurityAction to a special non-CAS action so this
                // blob will have its own entry in the metadata
                switch (rAttrSets[i].dwAction)
                {
                case dclDemand:
                    dwAction = dclNonCasDemand;
                    break;
                case dclLinktimeCheck:
                    dwAction = dclNonCasLinkDemand;
                    break;
                case dclInheritanceCheck:
                    dwAction = dclNonCasInheritance;
                    break;
                default:
                    PostError(CORSECATTR_E_BAD_NONCAS);
                    IfFailGo(CORSECATTR_E_BAD_NONCAS);
                }

                // Persist to metadata
                IfFailGo(_DefinePermissionSet(rAttrSets[i].tkObj,
                                              dwAction,
                                              pbNonCasBlob,
                                              cbNonCasBlob,
                                              &ps));
            }
        }
    }

ErrExit:
    STOP_MD_PERF(DefineSecurityAttributeSet);

    END_ENTRYPOINT_NOTHROW;
    
    return (hr);
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DefineSecurityAttributeSet

#endif //FEATURE_METADATA_EMIT


//*****************************************************************************
// Implementation of IMetaDataImport::ResolveTypeRef to resolve a typeref across scopes. 
// 
// Arguments:
//    tr - typeref within this scope to resolve
//    riid - interface on ppIScope to support
//    ppIScope - out-parameter to get metadata scope for typedef (*ptd)
//    ptd - out-parameter to get typedef that the ref resolves to.
// 
// Notes:
// TypeDefs define a type within a scope. TypeRefs refer to type-defs in other scopes
// and allow you to import a type from another scope. This function attempts to determine
// which type-def a type-ref points to.
// 
// This resolve (type-ref, this cope) --> (type-def=*ptd, other scope=*ppIScope)
// 
// However, this resolution requires knowing what modules have been loaded, which is not decided
// until runtime via loader / fusion policy. Thus this interface can't possibly be correct since
// it doesn't have that knowledge. Furthermore, when inspecting metadata from another process
// (such as a debugger inspecting the debuggee's metadata), this API can be truly misleading.
// 
// This API usage should be avoided.
// 
//*****************************************************************************
STDMETHODIMP 
RegMeta::ResolveTypeRef(
    mdTypeRef   tr, 
    REFIID      riid, 
    IUnknown ** ppIScope, 
    mdTypeDef * ptd)
{
#ifdef FEATURE_METADATA_IN_VM
    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    TypeRefRec * pTypeRefRec;
    WCHAR        wzNameSpace[_MAX_PATH];
    CMiniMdRW *  pMiniMd = NULL;
    WCHAR rcModule[_MAX_PATH];

    LOG((LOGMD, "{%08x} RegMeta::ResolveTypeRef(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n", 
        this, tr, riid, ppIScope, ptd));

    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);

    _ASSERTE((ppIScope != NULL) && (ptd != NULL));

    // Init the output values.
    *ppIScope = NULL;
    *ptd = 0;

    if (IsNilToken(tr))
    {
        if (ptd != NULL) 
        {
            *ptd = mdTypeDefNil;
        }

        if (ppIScope != NULL)
        {
            *ppIScope = NULL;
        }
        
        STOP_MD_PERF(ResolveTypeRef);
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    if (TypeFromToken(tr) == mdtTypeDef)
    {
        // Shortcut when we receive a TypeDef token
        *ptd = tr;
        STOP_MD_PERF(ResolveTypeRef);
        hr = this->QueryInterface(riid, (void **)ppIScope);
        goto ErrExit;
    }

    // Get the class ref row.
    _ASSERTE(TypeFromToken(tr) == mdtTypeRef);

    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tr), &pTypeRefRec));
    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, wzNameSpace, lengthof(wzNameSpace), NULL));
    if (hr != NOERROR)
    {
        _ASSERTE(hr == CLDB_S_TRUNCATION);
        // Truncate the namespace string
        wzNameSpace[lengthof(wzNameSpace) - 1] = 0;
    }
    
    //***********************
    // before we go off to CORPATH, check the loaded modules!
    //***********************
    if (LOADEDMODULES::ResolveTypeRefWithLoadedModules(
        tr, 
        this, 
        pMiniMd, 
        riid, 
        ppIScope, 
        ptd) == NOERROR)
    {
        // Done!! We found one match among the loaded modules.
        goto ErrExit;
    }

#ifndef FEATURE_CORECLR
    wcscpy_s(rcModule, _MAX_PATH, wzNameSpace);

    //******************
    // Try to find the module on CORPATH
    //******************

    if ((wcsncmp(rcModule, W("System."), 16) != 0) && 
        (wcsncmp(rcModule, W("System/"), 16) != 0))
    {
        // only go through regular CORPATH lookup by fully qualified class name when
        // it is not System.*
        hr = CORPATHService::GetClassFromCORPath(
            rcModule,
            tr,
            pMiniMd,
            riid,
            ppIScope,
            ptd);
    }
    else 
    {
        // force it to look for System.* in mscorlib.dll
        hr = S_FALSE;
    }

    if (hr == S_FALSE)
    {
        LPWSTR szTmp;
        WszSearchPath(
            NULL, 
            W("mscorlib.dll"), 
            NULL, 
            sizeof(rcModule) / sizeof(rcModule[0]), 
            rcModule, 
            &szTmp);

        //*******************
        // Last desperate try!!
        //*******************

        // Use the file name "mscorlib:
        IfFailGo(CORPATHService::FindTypeDef(
            rcModule, 
            tr, 
            pMiniMd, 
            riid, 
            ppIScope, 
            ptd));
        if (hr == S_FALSE)
        {
            IfFailGo(META_E_CANNOTRESOLVETYPEREF);
        }
    }
#else //FEATURE_CORECLR
    IfFailGo(META_E_CANNOTRESOLVETYPEREF);
#endif //FEATURE_CORECLR

ErrExit:
    STOP_MD_PERF(ResolveTypeRef);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
#else //!FEATURE_METADATA_IN_VM
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_IN_VM
} // RegMeta::ResolveTypeRef



// Full version handles metadata caching, which Release() needs to coordinate with. 
// Thus Release() is in a satellite lib.
ULONG RegMeta::Release()
{
    // This is called during cleanup.  We can not fail this call by probing.
    // As long as we make sure the cleanup does not use too much space through 
    // BEGIN_CLEANUP_ENTRYPOINT, we are OK.
    CONTRACT_VIOLATION (SOToleranceViolation);
    BEGIN_CLEANUP_ENTRYPOINT;

#if defined(FEATURE_METADATA_IN_VM) || defined(FEATURE_METADATA_STANDALONE_WINRT)
    _ASSERTE(!m_bCached || LOADEDMODULES::IsEntryInList(this));
#else
    _ASSERTE(!m_bCached);
#endif //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
    BOOL  bCached = m_bCached;
    ULONG cRef = InterlockedDecrement(&m_cRef);
    // NOTE: 'this' may be unsafe after this point, if the module is cached, and
    //  another thread finds the module in the cache, releases it, and deletes it
    //  before we get around to deleting it. (That's why we must make a local copy
    //  of m_bCached.)
    // If no references left...
    if (cRef == 0)
    {
        if (!bCached)
        {   // If the module is not (was not) cached, no other thread can have
            //  discovered the module, so this thread can now safely delete it.
            delete this;
        }
#if defined(FEATURE_METADATA_IN_VM) || defined(FEATURE_METADATA_STANDALONE_WINRT)
        else if (LOADEDMODULES::RemoveModuleFromLoadedList(this))
        {   // If the module was cached, RemoveModuleFromLoadedList() will try to
            //  safely un-publish the module, and if it succeeds, no other thread
            //  has (or will) discover the module, so this thread can delete it.
            m_bCached = false;
            delete this;
        }
#endif //!FEATURE_METADATA_IN_VM && !FEATURE_METADATA_STANDALONE_WINRT
    }
    END_CLEANUP_ENTRYPOINT
    
    return cRef;
} // RegMeta::Release
