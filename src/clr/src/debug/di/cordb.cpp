//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// CorDB.cpp
// 

//
// Dll* routines for entry points, and support for COM framework.  The class
// factory and other routines live in this module.
//
//*****************************************************************************
#include "stdafx.h"
#include "classfactory.h"
#include "corsym.h"
#include "contract.h"
#include "metadataexports.h"
#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"
#endif // FEATURE_DBGIPC_TRANSPORT_DI

// Helper function returns the instance handle of this module.
HINSTANCE GetModuleInst();

//********** Globals. *********************************************************
HINSTANCE       g_hInst;                // Instance handle to this piece of code.

//-----------------------------------------------------------------------------
// SxS Versioning story for Mscordbi (ICorDebug + friends)
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// In v1.0, we declared that mscordbi was a "shared" component, which means
// that we promised to provide it from now until the end of time. So every CLR implementation
// needs an Mscordbi that implements the everett guids for CorDebug + CorPublish.
//
// This works fine for CorPublish, which is truly shared.
// CorDebug however is "versioned" not "shared" - each version of the CLR has its own disjoint copy.
//
// Thus creating a CorDebug object requires a version parameter.
// CoCreateInstance doesn't have a the version param, so we use the new (v2.0+)
// shim interface CreateDebuggingInterfaceFromVersion.
//
// ** So in summary: **
// - Dlls don't do self-registration; they're registered by setup using .vrg files.
// - All CLR versions (past + future) must have the same registry footprint w.r.t mscordbi.
//     This just means that all CLRs have the same mscordbi.vrg file.
// - CorDebug is in fact versioned and each CLR version has its own copy.
// - In v1.0/1.1, CorDebug was a CoClass. In v2.0+, it is not a CoClass and is created via the
//     CreateDebuggingInterfaceFromVersion shim API, which takes a version parameter.
// - CorDebug must be SxS. V1.1 must only get the V1.1 version, and V2.0 must only get the V2.0 version.
//     V1.1: Clients will cocreate to get CorDebug. v1.1 will be the only mscordbi!DllGetClassObject
//           that provides a CorDebug, so CoCreateInstance will guarantee getting a v1.1 object.
//     V2.0: Clients use the new version-aware shim API, so it's not an issue.
//
// ** Preparing for Life in a Single-CLR world: **
// In Orcas (v3), we expect to run on single-CLR. There will only be 1 mscordbi, and it will service all versions.
// For whidbey (v2), we want to be able to flip a knob and pretend to be orcas (for testing purposes).
//
// Here's how to do that:
// - copy whidbey mscordbi & dac over the everett mscordbi.
// - When VS cocreates w/ the everett-guid, it will load the mscordbi on the everett path (
//   which will be whidbey dll), and ask for the everett guid.
// - re-add CorDebug to the g_CoClasses list.


//********** Locals. **********************************************************


//********** Code. ************************************************************


//*****************************************************************************
// Standard public helper to create a Cordb object (ICorDebug instance).
// This is used by the shim to get the Cordb object out of this module.
// This is the creation path for V2.0+ for CorDebug using the in-process debugging
// architecture (ICorDebug).  In CLR v4+ debugger may choose to use the out-of-process
// architecture to get an ICorDebugProcess directly (IClrDebugging::OpenVirtualProcess).
//
// This was used by the Mix07 release of Silverlight, but it didn't properly support versioning
// and we no longer support it's debugger protocol so we require callers to use 
// code:CoreCLRCreateCordbObject instead.
// 
// This is also still used on Mac - multi-instance debugging and debugger
// versioning isn't really implemented there yet.  This probably needs to change.
//*****************************************************************************
STDAPI CreateCordbObject(int iDebuggerVersion, IUnknown ** ppCordb)
{
#if defined(FEATURE_CORECLR) && !defined(FEATURE_DBGIPC_TRANSPORT_DI) && !defined(FEATURE_CORESYSTEM)
    // This API should not be called for Windows CoreCLR unless we are doing interop-debugging
    // (which is only supported internally).  Use code:CoreCLRCreateCordbObject instead.
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgEnableMixedModeDebugging) == 0)
    {
        _ASSERTE(!"Deprecated entry point CreateCordbObject() is called on Windows CoreCLR\n");
        return E_NOTIMPL;
    }
#endif // FEATURE_CORECLR && !FEATURE_DBGIPC_TRANSPORT_DI

    if (ppCordb == NULL)
    {
        return E_INVALIDARG;
    }
    if (iDebuggerVersion != CorDebugVersion_2_0 && iDebuggerVersion != CorDebugVersion_4_0)
    {
        return E_INVALIDARG;
    }

    return Cordb::CreateObject((CorDebugInterfaceVersion)iDebuggerVersion, IID_ICorDebug, (void **) ppCordb);
}

#if defined(FEATURE_CORECLR)
//
// Public API.  
// Telesto Creation path - only way to debug multi-instance.  
// This supercedes code:CreateCordbObject
// 
// Arguments:
//    iDebuggerVersion - version of ICorDebug interfaces that the debugger is requesting
//    pid - pid of debuggee that we're attaching to.
//    hmodTargetCLR - module handle to clr in target pid that we're attaching to.
//    ppCordb - (out) the resulting ICorDebug object.
//
// Notes:
//    It's inconsistent that this takes a (handle, pid) but hands back an ICorDebug instead of an ICorDebugProcess.
//    Callers will need to call *ppCordb->DebugActiveProcess(pid).
STDAPI CoreCLRCreateCordbObject(int iDebuggerVersion, DWORD pid, HMODULE hmodTargetCLR, IUnknown ** ppCordb)
{
    if (ppCordb == NULL)
    {
        return E_INVALIDARG;
    }
    if ((iDebuggerVersion < CorDebugVersion_2_0) ||
        (iDebuggerVersion > CorDebugLatestVersion))
    {
        return E_INVALIDARG;
    }

    //
    // Create the ICorDebug object
    // 
    RSExtSmartPtr<ICorDebug> pCordb;
    Cordb::CreateObject((CorDebugInterfaceVersion)iDebuggerVersion, IID_ICorDebug, (void **) &pCordb);

    // @dbgtodo - we should stash the pid and validate that it's the same pid we're attaching to in ICorDebug::DebugActiveProcess.
    
    //
    // Associate it with the target instance
    //
    HRESULT hr = static_cast<Cordb*>(pCordb.GetValue())->SetTargetCLR(hmodTargetCLR);
    if (FAILED(hr))
    {
        return hr;
    }

    //
    // Assign to out parameter.
    // 
    hr = pCordb->QueryInterface(IID_IUnknown, (void**) ppCordb);

    // Implicit release of pUnk, pCordb
    return hr;
}

#endif // FEATURE_CORECLR




//*****************************************************************************
// The main dll entry point for this module.  This routine is called by the
// OS when the dll gets loaded.  Control is simply deferred to the main code.
//*****************************************************************************
BOOL WINAPI DbgDllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
#if defined(_DEBUG)
    static int BreakOnDILoad = -1;
    if (BreakOnDILoad == -1)
        BreakOnDILoad = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnDILoad);

    if (BreakOnDILoad)
    {
        _ASSERTE(!"DI Loaded");
    }
#endif

    // Save off the instance handle for later use.
    switch (dwReason)
    {

        case DLL_PROCESS_ATTACH:
        {
            g_hInst = hInstance;

#if defined(LOGGING)
            {
                WCHAR   rcFile[_MAX_PATH];
                WszGetModuleFileName(hInstance, rcFile, NumItems(rcFile));
                LOG((LF_CORDB, LL_INFO10000,
                    "DI::DbgDllMain: load right side support from file '%s'\n",
                     rcFile));
            }
#endif

#ifdef RSCONTRACTS
            // alloc a TLS slot
            DbgRSThread::s_TlsSlot = TlsAlloc();
            _ASSERTE(DbgRSThread::s_TlsSlot != TLS_OUT_OF_INDEXES);
#endif

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
            g_pDbgTransportTarget = new (nothrow) DbgTransportTarget();
            if (g_pDbgTransportTarget == NULL)
                return FALSE;

            if (FAILED(g_pDbgTransportTarget->Init()))
                return FALSE;
#endif // FEATURE_DBGIPC_TRANSPORT_DI
        }
        break;

        case DLL_THREAD_DETACH:
        {
#ifdef STRESS_LOG
            StressLog::ThreadDetach((ThreadStressLog*) ClrFlsGetValue(TlsIdx_StressLog));
#endif

#ifdef RSCONTRACTS
            // DbgRSThread are lazily created when we call GetThread(),
            // So we don't need to do anything in DLL_THREAD_ATTACH,
            // But this is our only chance to destroy the thread object.
            DbgRSThread * p = DbgRSThread::GetThread();

            p->Destroy();
#endif
        }
        break;

        case DLL_PROCESS_DETACH:
        {
#ifdef RSCONTRACTS
            TlsFree(DbgRSThread::s_TlsSlot);
            DbgRSThread::s_TlsSlot = TLS_OUT_OF_INDEXES;
#endif

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
            if (g_pDbgTransportTarget != NULL)
            {
                g_pDbgTransportTarget->Shutdown();
                delete g_pDbgTransportTarget;
                g_pDbgTransportTarget = NULL;
            }
#endif // FEATURE_DBGIPC_TRANSPORT_DI

        }
        break;
    }

    return TRUE;
}


// The obsolete v1 CLSID - see comment above for details.
static const GUID CLSID_CorDebug_V1 = {0x6fef44d0,0x39e7,0x4c77, { 0xbe,0x8e,0xc9,0xf8,0xcf,0x98,0x86,0x30}};

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)

// GUID for pipe-based debugging (Unix platforms)
const GUID CLSID_CorDebug_Telesto = {0x8bd1daae, 0x188e, 0x42f4, {0xb0, 0x09, 0x08, 0xfa, 0xfd, 0x17, 0x81, 0x3b}};

// The debug engine needs to implement an internal Visual Studio debugger interface (defined by the CPDE)
// which augments launch and attach requests so that we can obtain information from the port supplier (the
// network address of the target in our case). See RSPriv.h for the definition of the interface. (We have to
// hard code the IID and interface definition because VS does not export it, but it's not much of an issue
// since COM interfaces are completely immutable).
const GUID IID_IDebugRemoteCorDebug = {0x83C91210, 0xA34F, 0x427c, {0xB3, 0x5F, 0x79, 0xC3, 0x99, 0x5B, 0x3C, 0x14}};
#endif // FEATURE_DBGIPC_TRANSPORT_DI

//*****************************************************************************
// Called by COM to get a class factory for a given CLSID.  If it is one we
// support, instantiate a class factory object and prepare for create instance.
//*****************************************************************************
STDAPI DllGetClassObjectInternal(               // Return code.
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{
    HRESULT         hr;
    CClassFactory   *pClassFactory;         // To create class factory object.
    PFN_CREATE_OBJ  pfnCreateObject = NULL;


#if defined(FEATURE_DBG_PUBLISH)
    if (rclsid == CLSID_CorpubPublish)
    {
        pfnCreateObject = CorpubPublish::CreateObject;
    }
    else
#endif
#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
    if (rclsid == CLSID_CorDebug_Telesto)
    {
        pfnCreateObject = Cordb::CreateObjectTelesto;
    }
#else  // !FEATURE_DBGIPC_TRANSPORT_DI
    if(rclsid == CLSID_CorDebug_V1)
    {
        if (0) // if (IsSingleCLR())
        {
            // Don't allow creating backwards objects until we ensure that the v2.0 Right-side
            // is backwards compat. This may involve using CordbProcess::SupportsVersion to conditionally
            // emulate old behavior.
            // If emulating V1.0, QIs for V2.0 interfaces should fail.
            _ASSERTE(!"Ensure that V2.0 RS is backwards compat");
            pfnCreateObject = Cordb::CreateObjectV1;
        }
    }
#endif // FEATURE_DBGIPC_TRANSPORT_DI

    if (pfnCreateObject == NULL)
        return (CLASS_E_CLASSNOTAVAILABLE);

    // Allocate the new factory object.  The ref count is set to 1 in the constructor.
    pClassFactory = new (nothrow) CClassFactory(pfnCreateObject);
    if (!pClassFactory)
        return (E_OUTOFMEMORY);

    // Pick the v-table based on the caller's request.
    hr = pClassFactory->QueryInterface(riid, ppv);

    // Always release the local reference, if QI failed it will be
    // the only one and the object gets freed.
    pClassFactory->Release();

    return hr;
}

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
// In V2 we started hiding DllGetClassObject because activation was no longer performed through COM directly
// (we went through the shim). CoreCLR doesn't have a shim and we go back to the COM model so we re-expose
// DllGetClassObject to make that work.

STDAPI DllGetClassObject(               // Return code.
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{
    return DllGetClassObjectInternal(rclsid, riid, ppv);
}
#endif // FEATURE_DBGIPC_TRANSPORT_DI


//*****************************************************************************
//
//********** Class factory code.
//
//*****************************************************************************


//*****************************************************************************
// QueryInterface is called to pick a v-table on the co-class.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE CClassFactory::QueryInterface(
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

    // Avoid confusion.
    *ppvObject = NULL;

    // Pick the right v-table based on the IID passed in.
    if (riid == IID_IUnknown)
        *ppvObject = (IUnknown *) this;
    else if (riid == IID_IClassFactory)
        *ppvObject = (IClassFactory *) this;

    // If successful, add a reference for out pointer and return.
    if (*ppvObject)
    {
        hr = S_OK;
        AddRef();
    }
    else
        hr = E_NOINTERFACE;
    return (hr);
}


//*****************************************************************************
// CreateInstance is called to create a new instance of the coclass for which
// this class was created in the first place.  The returned pointer is the
// v-table matching the IID if there.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE CClassFactory::CreateInstance(
    IUnknown    *pUnkOuter,
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

    // Avoid confusion.
    *ppvObject = NULL;
    _ASSERTE(m_pfnCreateObject);

    // Aggregation is not supported by these objects.
    if (pUnkOuter)
        return (CLASS_E_NOAGGREGATION);

    // Ask the object to create an instance of itself, and check the iid.
    hr = (*m_pfnCreateObject)(riid, ppvObject);
    return (hr);
}


HRESULT STDMETHODCALLTYPE CClassFactory::LockServer(
    BOOL        fLock)
{
//<TODO>@todo: hook up lock server logic.</TODO>
    return (S_OK);
}





//*****************************************************************************
// This helper provides access to the instance handle of the loaded image.
//*****************************************************************************
HINSTANCE GetModuleInst()
{
    return g_hInst;
}


//-----------------------------------------------------------------------------
// Substitute for mscoree
// 
// Notes:
//    Mscordbi does not link with mscoree, provide a stub implementation. 
//    Callers are in dead-code paths, but we still need to provide a stub. Ideally, we'd factor
//    out the callers too and then we wouldn't need an E_NOTIMPL stub.
STDAPI GetRequestedRuntimeInfo(LPCWSTR pExe, 
                               LPCWSTR pwszVersion, 
                               LPCWSTR pConfigurationFile, 
                               DWORD startupFlags, 
                               DWORD runtimeInfoFlags, 
                               __out_ecount_opt(dwDirectory) LPWSTR pDirectory, 
                               DWORD dwDirectory, 
                               DWORD *dwDirectoryLength, 
                               __out_ecount_opt(cchBuffer)   LPWSTR pVersion, 
                               DWORD cchBuffer, 
                               DWORD* dwlength)
{
    _ASSERTE(!"GetRequestedRuntimeInfo not impl");
    return E_NOTIMPL;
}

//-----------------------------------------------------------------------------
// Replacement for legacy shim API GetCORRequiredVersion(...) used in linked libraries.
// Used in code:TiggerStorage::GetDefaultVersion#CallTo_CLRRuntimeHostInternal_GetImageVersionString.
// 
// Notes:
//   Mscordbi does not statically link to mscoree.dll.
//   This is used in EnC for IMetadataEmit2::GetSaveSize to computer size of header.
//   see code:TiggerStorage::GetDefaultVersion.
//   
//   Implemented by returning the version we're built for.  Mscordbi.dll has a tight coupling with
//   the CLR version, so this will match exactly the build version we're debugging.  
//   One potential caveat is that the build version doesn't necessarily match the install string
//   (eg. we may install as "v4.0.x86chk" but that's not captured in the build version).  But this should
//   be internal scenarios only, and shouldn't actually matter here.  If it did, we could instead get
//   the last components of the directory name the current mscordbi.dll is located in.
//     
HRESULT 
CLRRuntimeHostInternal_GetImageVersionString(
    __out_ecount_part(*pcchBuffer, *pcchBuffer) LPWSTR wszBuffer, 
    DWORD *pcchBuffer)
{
    // Construct the cannoncial version string we're built as - eg. "v4.0.1234"
    const WCHAR k_wszBuiltFor[] = W("v")VER_PRODUCTVERSION_NO_QFE_STR_L;

    // Copy our buffer in
    HRESULT hr = HRESULT_FROM_WIN32(wcscpy_s(wszBuffer, *pcchBuffer, k_wszBuiltFor));

    // Hand out length regardless of success - like GetCORRequiredVersion
    *pcchBuffer = _countof(k_wszBuiltFor);

    return hr;
} // CLRRuntimeHostInternal_GetImageVersionString


#ifdef _TARGET_ARM_
BOOL
DbiGetThreadContext(HANDLE hThread,
    DT_CONTEXT *lpContext)
{
    // if we aren't local debugging this isn't going to work
#if !defined(_ARM_) || defined(FEATURE_DBGIPC_TRANSPORT_DI)
    _ASSERTE(!"Can't use local GetThreadContext remotely, this needed to go to datatarget");
    return FALSE;
#else
    BOOL res = FALSE;
    if (((ULONG)lpContext) & ~0x10)
    {
        CONTEXT *ctx = (CONTEXT*)_aligned_malloc(sizeof(CONTEXT), 16);
        if (ctx)
        {
            ctx->ContextFlags = lpContext->ContextFlags;
            if (::GetThreadContext(hThread, ctx))
            {
                *lpContext = *(DT_CONTEXT*)ctx;
                res = TRUE;
            }

            _aligned_free(ctx);
        }
        else
        {
            // malloc does not set the last error, but the caller of GetThreadContext
            // will expect it to be set on failure.
            SetLastError(ERROR_OUTOFMEMORY);
        }
    }
    else
    {
        res = ::GetThreadContext(hThread, (CONTEXT*)lpContext);
    }
    
    return res;
#endif
}

BOOL
DbiSetThreadContext(HANDLE hThread,
    const DT_CONTEXT *lpContext)
{
#if !defined(_ARM_) || defined(FEATURE_DBGIPC_TRANSPORT_DI)
    _ASSERTE(!"Can't use local GetThreadContext remotely, this needed to go to datatarget");
    return FALSE;
#else
    BOOL res = FALSE;
    if (((ULONG)lpContext) & ~0x10)
    {
        CONTEXT *ctx = (CONTEXT*)_aligned_malloc(sizeof(CONTEXT), 16);
        if (ctx)
        {
            *ctx = *(CONTEXT*)lpContext;
            res = ::SetThreadContext(hThread, ctx);
            _aligned_free(ctx);
        }   
        else
        {
            // malloc does not set the last error, but the caller of SetThreadContext
            // will expect it to be set on failure.
            SetLastError(ERROR_OUTOFMEMORY);
        }
    }
    else
    {
        res = ::SetThreadContext(hThread, (CONTEXT*)lpContext);
    }
    
    return res;
#endif
}
#endif
