// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MSCoree.cpp
//*****************************************************************************
#include "stdafx.h"                     // Standard header.

#include <utilcode.h>                   // Utility helpers.
#include <posterror.h>                  // Error handlers
#define INIT_GUIDS  
#include <corpriv.h>
#include <winwrap.h>
#include <mscoree.h>
#include "shimload.h"
#include "metadataexports.h"
#include "ex.h"
#if !defined(FEATURE_CORECLR)
#include "corsym.h"
#endif 

#if defined(FEATURE_CORECLR)
#include "product_version.h"
#endif // FEATURE_CORECLR

#ifdef FEATURE_COMINTEROP
#include "ComCallUnmarshal.h"
#endif // FEATURE_COMINTEROP

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
#include <metahost.h>
extern ICLRRuntimeInfo *g_pCLRRuntime;
#endif // !FEATURE_CORECLR && !CROSSGEN_COMPILE

#include "clrprivhosting.h"

#ifndef FEATURE_CORECLR
#include "clr/win32.h"
#endif // FEATURE_CORECLR

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
#include "../../vm/profattach.h"
#endif // FEATURE_PROFAPI_ATTACH_DETACH


#if defined(FEATURE_CORECLR)
#include <dbgenginemetrics.h>
#endif // FEATURE_CORECLR

// Locals.
BOOL STDMETHODCALLTYPE EEDllMain( // TRUE on success, FALSE on error.
                       HINSTANCE    hInst,                  // Instance handle of the loaded module.
                       DWORD        dwReason,               // Reason for loading.
                       LPVOID       lpReserved);                // Unused.

#ifdef FEATURE_COMINTEROP_MANAGED_ACTIVATION
// try to load a com+ class and give out an IClassFactory
HRESULT STDMETHODCALLTYPE EEDllGetClassObject(
                            REFCLSID rclsid,
                            REFIID riid,
                            LPVOID FAR *ppv);
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION

// Globals.
HINSTANCE g_hThisInst;  // This library.

#ifndef CROSSGEN_COMPILE
//*****************************************************************************
// Handle lifetime of loaded library.
//*****************************************************************************

#ifdef FEATURE_CORECLR

#include <shlwapi.h>

#include <process.h> // for __security_init_cookie()

void* __stdcall GetCLRFunction(LPCSTR FunctionName);

extern "C" IExecutionEngine* __stdcall IEE();

#ifdef NO_CRT_INIT
#define _CRT_INIT(hInstance, dwReason, lpReserved) (TRUE)
#else
extern "C" BOOL WINAPI _CRT_INIT(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);
#endif

extern "C" BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);

// For the CoreClr, this is the real DLL entrypoint. We make ourselves the first entrypoint as
// we need to capture coreclr's hInstance before the C runtine initializes. This function
// will capture hInstance, let the C runtime initialize and then invoke the "classic"
// DllMain that initializes everything else.
extern "C" BOOL WINAPI CoreDllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    BOOL result;
    switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
#ifndef FEATURE_PAL        
            // Make sure the /GS security cookie is initialized before we call anything else.
            // BinScope detects the call to __security_init_cookie in its "Has Non-GS-friendly
            // Initialization" check and makes it pass.
            __security_init_cookie();
#endif // FEATURE_PAL        

            // It's critical that we invoke InitUtilCode() before the CRT initializes. 
            // We have a lot of global ctors that will break if we let the CRT initialize without
            // this step having been done.

            CoreClrCallbacks cccallbacks;
            cccallbacks.m_hmodCoreCLR               = (HINSTANCE)hInstance;
            cccallbacks.m_pfnIEE                    = IEE;
            cccallbacks.m_pfnGetCORSystemDirectory  = GetCORSystemDirectoryInternaL;
            cccallbacks.m_pfnGetCLRFunction         = GetCLRFunction;
            InitUtilcode(cccallbacks);

            if (!(result = _CRT_INIT(hInstance, dwReason, lpReserved)))
            {
                // CRT_INIT may fail to initialize the CRT heap. Make sure we don't continue 
                // down a path that would trigger an AV and tear down the host process
                break;
            }
            result = DllMain(hInstance, dwReason, lpReserved);
            break;
        
        case DLL_THREAD_ATTACH:
            _CRT_INIT(hInstance, dwReason, lpReserved);
            result = DllMain(hInstance, dwReason, lpReserved);
            break;

        case DLL_PROCESS_DETACH: // intentional fallthru
        case DLL_THREAD_DETACH:
            result = DllMain(hInstance, dwReason, lpReserved);
            _CRT_INIT(hInstance, dwReason, lpReserved);
            break;

        default:
            result = FALSE;  // it'd be an OS bug if we got here - not much we can do.
            break;   
    }
    return result;
}
#endif //FEATURE_CORECLR

extern "C"
BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        {
            // Save the module handle.
            g_hThisInst = (HINSTANCE)hInstance;

#ifndef FEATURE_CORECLR
            // clr.dll cannot be unloaded
            // Normally the shim prevents it from ever being unloaded, but we now support fusion loading
            // us directly, so we need to take an extra ref on our handle to ensure we don't get unloaded.
            if (FAILED(clr::win32::PreventModuleUnload(g_hThisInst)))
            {
                return FALSE;
            }
#endif // FEATURE_CORECLR

            // Prevent buffer-overruns
            // If buffer is overrun, it is possible the saved callback has been trashed.
            // The callback is unsafe.
            //SetBufferOverrunHandler();
            if (!EEDllMain((HINSTANCE)hInstance, dwReason, lpReserved))
            {
                return FALSE;
            }
        }
        break;

    case DLL_PROCESS_DETACH:
        {
            EEDllMain((HINSTANCE)hInstance, dwReason, lpReserved);
        }
        break;

    case DLL_THREAD_DETACH:
        {
            EEDllMain((HINSTANCE)hInstance, dwReason, lpReserved);
        }
        break;
    }

    return TRUE;
}

#ifndef FEATURE_CORECLR // coreclr does not export this 
// ---------------------------------------------------------------------------
// %%Function: DllGetClassObjectInternal  %%Owner: NatBro   %%Reviewed: 00/00/00
// 
// Parameters:
//  rclsid                  - reference to the CLSID of the object whose
//                            ClassObject is being requested
//  iid                     - reference to the IID of the interface on the
//                            ClassObject that the caller wants to communicate
//                            with
//  ppv                     - location to return reference to the interface
//                            specified by iid
// 
// Returns:
//  S_OK                    - if successful, valid interface returned in *ppv,
//                            otherwise *ppv is set to NULL and one of the
//                            following errors is returned:
//  E_NOINTERFACE           - ClassObject doesn't support requested interface
//  CLASS_E_CLASSNOTAVAILABLE - clsid does not correspond to a supported class
// 
// Description:
//  Returns a reference to the iid interface on the main COR ClassObject.
//  This function is one of the required by-name entry points for COM
// DLL's. Its purpose is to provide a ClassObject which by definition
// supports at least IClassFactory and can therefore create instances of
// objects of the given class.
// ---------------------------------------------------------------------------

#ifdef FEATURE_COMINTEROP
// This could be merged with Metadata's class factories!
static CComCallUnmarshalFactory g_COMCallUnmarshal;
#endif // FEATURE_COMINTEROP

STDAPI InternalDllGetClassObject(
    REFCLSID rclsid,
    REFIID riid,
    LPVOID FAR *ppv)
{
    // @todo: this is called before the runtime is really started, so the contract's don't work.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_TOLERANT;

    HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);


    if (rclsid == CLSID_CorMetaDataDispenser || rclsid == CLSID_CorMetaDataDispenserRuntime ||
        rclsid == CLSID_CorRuntimeHost || rclsid == CLSID_CLRRuntimeHost ||
        rclsid == CLSID_TypeNameFactory
        || rclsid == __uuidof(CLRPrivRuntime)
       )
    {
        hr = MetaDataDllGetClassObject(rclsid, riid, ppv);
    }
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    else if (rclsid == CLSID_CLRProfiling)
    {
        hr = ICLRProfilingGetClassObject(rclsid, riid, ppv);
    }
#endif // FEATURE_PROFAPI_ATTACH_DETACH
#ifdef FEATURE_COMINTEROP
    else if (rclsid == CLSID_ComCallUnmarshal || rclsid == CLSID_ComCallUnmarshalV4)
    {
        // We still respond to the 1.0/1.1/2.0 CLSID so we don't break anyone who is instantiating
        // this (we could be called for CLSID_ComCallUnmarshal if the process is rollForward=true)
        hr = g_COMCallUnmarshal.QueryInterface(riid, ppv);
    }
    else if (rclsid == CLSID_CorSymBinder_SxS)
    {
        EX_TRY
        {

            // PDB format - use diasymreader.dll with COM activation
            InlineSString<_MAX_PATH> ssBuf;
            if (SUCCEEDED(GetHModuleDirectory(GetModuleInst(), ssBuf)))
            {
                hr = FakeCoCallDllGetClassObject(rclsid,
                    ssBuf,
                    riid,
                    ppv,
                    NULL
                    );
            }
        }
        EX_CATCH_HRESULT(hr);
    }
    else
    {
#ifdef FEATURE_COMINTEROP_MANAGED_ACTIVATION
        // Returns a managed object imported into COM-classic.
        hr = EEDllGetClassObject(rclsid,riid,ppv);
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION
    }
#endif // FEATURE_COMINTEROP

    END_SO_INTOLERANT_CODE;
    return hr;
}  // InternalDllGetClassObject


STDAPI DllGetClassObjectInternal(
                                 REFCLSID rclsid,
                                 REFIID riid,
                                 LPVOID FAR *ppv)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_ENTRY_POINT;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;
    
    // InternalDllGetClassObject exists to resolve an issue
    // on FreeBSD, where libsscoree.so's DllGetClassObject's
    // call to DllGetClassObjectInternal() was being bound to
    // the implementation in libmscordbi.so, not the one in
    // libsscoree.so.  The fix is to disambiguate the name.
    hr = InternalDllGetClassObject(rclsid, riid, ppv);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // FEATURE_CORECLR

#ifdef FEATURE_COMINTEROP
// ---------------------------------------------------------------------------
// %%Function: DllCanUnloadNowInternal  
// 
// Returns:
//  S_FALSE                 - Indicating that COR, once loaded, may not be
//                            unloaded.
// ---------------------------------------------------------------------------
STDAPI DllCanUnloadNowInternal(void)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_ENTRY_POINT;    

    //we should never unload unless the process is dying
    return S_FALSE;
}  // DllCanUnloadNowInternal

// ---------------------------------------------------------------------------
// %%Function: DllRegisterServerInternal 
// 
// Description:
//  Registers
// ---------------------------------------------------------------------------
STDAPI DllRegisterServerInternal(HINSTANCE hMod, LPCWSTR version)
{

    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(version));
    } CONTRACTL_END;

    return S_OK;
}  // DllRegisterServerInternal

// ---------------------------------------------------------------------------
// %%Function: DllUnregisterServerInternal      
// ---------------------------------------------------------------------------
STDAPI DllUnregisterServerInternal(void)
{

    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    return S_OK;
    
}  // DllUnregisterServerInternal
#endif // FEATURE_COMINTEROP

#endif // CROSSGEN_COMPILE

HINSTANCE GetModuleInst()
{
    LIMITED_METHOD_CONTRACT;
    return (g_hThisInst);
}

// ---------------------------------------------------------------------------
// %%Function: MetaDataGetDispenser
// This function gets the Dispenser interface given the CLSID and REFIID.
// ---------------------------------------------------------------------------
STDAPI MetaDataGetDispenser(            // Return HRESULT
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;
    
    NonVMComHolder<IClassFactory> pcf(NULL);
    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;

    IfFailGo(MetaDataDllGetClassObject(rclsid, IID_IClassFactory, (void **) &pcf));
    hr = pcf->CreateInstance(NULL, riid, ppv);

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return (hr);
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataInternalInterface
// This function gets the IMDInternalImport given the metadata on memory.
// ---------------------------------------------------------------------------
STDAPI  GetMetaDataInternalInterface(
    LPVOID      pData,                  // [IN] in memory metadata section
    ULONG       cbData,                 // [IN] size of the metadata section
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [IN] desired interface
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pData));
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDInternalInterface(pData, cbData, flags, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataInternalInterfaceFromPublic
// This function gets the internal scopeless interface given the public
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI  GetMetaDataInternalInterfaceFromPublic(
    IUnknown    *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pv));
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;
    
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDInternalInterfaceFromPublic(pv, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: GetMetaDataPublicInterfaceFromInternal
// This function gets the public scopeless interface given the internal
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI  GetMetaDataPublicInterfaceFromInternal(
    void        *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface.
    void        **ppv)                  // [OUT] returned interface
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pv));
        PRECONDITION(CheckPointer(ppv));
        ENTRY_POINT;
    } CONTRACTL_END;
    
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = GetMDPublicInterfaceFromInternal(pv, riid, ppv);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}


// ---------------------------------------------------------------------------
// %%Function: ReopenMetaDataWithMemory
// This function gets the public scopeless interface given the internal
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI ReOpenMetaDataWithMemory(
    void        *pUnk,                  // [IN] Given scope. public interfaces
    LPCVOID     pData,                  // [in] Location of scope data.
    ULONG       cbData)                 // [in] Size of the data pointed to by pData.
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pData));
    } CONTRACTL_END;

    HRESULT hr = S_OK;    

    BEGIN_ENTRYPOINT_NOTHROW;
    hr = MDReOpenMetaDataWithMemory(pUnk, pData, cbData);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: ReopenMetaDataWithMemoryEx
// This function gets the public scopeless interface given the internal
// scopeless interface.
// ---------------------------------------------------------------------------
STDAPI ReOpenMetaDataWithMemoryEx(
    void        *pUnk,                  // [IN] Given scope. public interfaces
    LPCVOID     pData,                  // [in] Location of scope data.
    ULONG       cbData,                 // [in] Size of the data pointed to by pData.
    DWORD       dwReOpenFlags)          // [in] ReOpen flags              
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pData));
    } CONTRACTL_END;

    HRESULT hr = S_OK;    

    BEGIN_ENTRYPOINT_NOTHROW;
    hr = MDReOpenMetaDataWithMemoryEx(pUnk, pData, cbData, dwReOpenFlags);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

#ifdef FEATURE_FUSION
// ---------------------------------------------------------------------------
// %%Function: GetAssemblyMDImport
// This function gets the IMDAssemblyImport given the filename
// ---------------------------------------------------------------------------
STDAPI GetAssemblyMDImport(             // Return code.
    LPCWSTR     szFileName,             // [in] The scope to open.
    REFIID      riid,                   // [in] The interface desired.
    IUnknown    **ppIUnk)               // [out] Return interface on success.
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    HRESULT hr=S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr=GetAssemblyMDInternalImport(szFileName, riid, ppIUnk);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}
#endif

#ifndef CROSSGEN_COMPILE
// ---------------------------------------------------------------------------
// %%Function: CoInitializeCor
// 
// Parameters:
//  fFlags                  - Initialization flags for the engine.  See the
//                              COINITICOR enumerator for valid values.
// 
// Returns:
//  S_OK                    - On success
// 
// Description:
//  Reserved to initialize the Cor runtime engine explicitly.  This currently
//  does nothing.
// ---------------------------------------------------------------------------
STDAPI CoInitializeCor(DWORD fFlags)
{
    WRAPPER_NO_CONTRACT;

    BEGIN_ENTRYPOINT_NOTHROW;

    // Since the CLR doesn't currently support being unloaded, we don't hold a ref
    // count and don't even pretend to try to unload.
    END_ENTRYPOINT_NOTHROW;

    return (S_OK);
}

// ---------------------------------------------------------------------------
// %%Function: CoUninitializeCor
// 
// Parameters:
//  none
// 
// Returns:
//  Nothing
// 
// Description:
//  Function to indicate the client is done with the CLR. This currently does
//  nothing.
// ---------------------------------------------------------------------------
STDAPI_(void)   CoUninitializeCor(void)
{
    WRAPPER_NO_CONTRACT;

    BEGIN_ENTRYPOINT_VOIDRET;

    // Since the CLR doesn't currently support being unloaded, we don't hold a ref
    // count and don't even pretend to try to unload.
    END_ENTRYPOINT_VOIDRET;

}

// Undef LoadStringRC & LoadStringRCEx so we can export these functions.
#undef LoadStringRC
#undef LoadStringRCEx

// ---------------------------------------------------------------------------
// %%Function: LoadStringRC
// 
// Parameters:
//  none
// 
// Returns:
//  Nothing
// 
// Description:
//  Function to load a resource based on it's ID.
// ---------------------------------------------------------------------------
STDAPI LoadStringRC(
    UINT iResourceID, 
    __out_ecount(iMax) __out_z LPWSTR szBuffer, 
    int iMax, 
    int bQuiet
)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;

    if (NULL == szBuffer)
        return E_INVALIDARG;
    if (0 == iMax)
        return E_INVALIDARG;
    
    BEGIN_ENTRYPOINT_NOTHROW;
    hr = UtilLoadStringRC(iResourceID, szBuffer, iMax, bQuiet);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: LoadStringRCEx
// 
// Parameters:
//  none
// 
// Returns:
//  Nothing
// 
// Description:
//  Ex version of the function to load a resource based on it's ID.
// ---------------------------------------------------------------------------
#ifdef FEATURE_USE_LCID
STDAPI LoadStringRCEx(
    LCID lcid,
    UINT iResourceID, 
    __out_ecount(iMax) __out_z LPWSTR szBuffer, 
    int iMax, 
    int bQuiet,
    int *pcwchUsed
)
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr = S_OK;

    if (NULL == szBuffer)
        return E_INVALIDARG;
    if (0 == iMax)
        return E_INVALIDARG;
    
    BEGIN_ENTRYPOINT_NOTHROW;   
    hr = UtilLoadStringRCEx(lcid, iResourceID, szBuffer, iMax, bQuiet, pcwchUsed);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}
#endif
// Redefine them as errors to prevent people from using these from inside the rest of the compilation unit.
#define LoadStringRC __error("From inside the CLR, use UtilLoadStringRC; LoadStringRC is only meant to be exported.")
#define LoadStringRCEx __error("From inside the CLR, use UtilLoadStringRCEx; LoadStringRC is only meant to be exported.")

#endif // CROSSGEN_COMPILE




// Note that there are currently two callers of this function: code:CCompRC.LoadLibrary
// and code:CorLaunchApplication.
STDAPI GetRequestedRuntimeInfoInternal(LPCWSTR pExe, 
                               LPCWSTR pwszVersion,
                               LPCWSTR pConfigurationFile, 
                               DWORD startupFlags,
                               DWORD runtimeInfoFlags, 
                                __out_ecount_opt(dwDirectory) LPWSTR pDirectory,
                               DWORD dwDirectory, 
                               __out_opt DWORD *pdwDirectoryLength, 
                               __out_ecount_opt(cchBuffer) LPWSTR pVersion, 
                               DWORD cchBuffer, 
                               __out_opt DWORD* pdwLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION( pVersion != NULL && cchBuffer > 0);
    } CONTRACTL_END;

    // for simplicity we will cheat and return the entire system directory in pDirectory
    pVersion[0] = 0;
    if (pdwLength != NULL)
        *pdwLength = 0;
    HRESULT hr;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(SetLastError(COR_E_STACKOVERFLOW); return COR_E_STACKOVERFLOW;)
    EX_TRY
    {

        PathString pDirectoryPath;

        hr = GetCORSystemDirectoryInternaL(pDirectoryPath);
        *pdwLength = pDirectoryPath.GetCount() + 1;
        if (dwDirectory >= *pdwLength)
        {
            wcscpy_s(pDirectory, pDirectoryPath.GetCount() + 1, pDirectoryPath);
        }
        else
        {
            hr = E_FAIL;
        }
        
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE

    return hr;
}

// Replacement for legacy shim API GetCORRequiredVersion(...) used in linked libraries.
// Used in code:TiggerStorage::GetDefaultVersion#CallTo_CLRRuntimeHostInternal_GetImageVersionString.
HRESULT 
CLRRuntimeHostInternal_GetImageVersionString(
    __out_ecount_opt(*pcchBuffer) LPWSTR wszBuffer, 
    __inout                       DWORD *pcchBuffer)
{
    // Simply forward the call to the ICLRRuntimeHostInternal implementation.
    STATIC_CONTRACT_WRAPPER;

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
    HRESULT hr = GetCORVersionInternal(wszBuffer, *pcchBuffer, pcchBuffer);
#else
    ReleaseHolder<ICLRRuntimeHostInternal> pRuntimeHostInternal;
    HRESULT hr = g_pCLRRuntime->GetInterface(CLSID_CLRRuntimeHostInternal,
                                             IID_ICLRRuntimeHostInternal,
                                             &pRuntimeHostInternal);
    if (SUCCEEDED(hr))
    {
        hr = pRuntimeHostInternal->GetImageVersionString(wszBuffer, pcchBuffer);
    }
#endif
    
    return hr;
} // CLRRuntimeHostInternal_GetImageVersionString

  //LONGPATH:TODO: Remove this once Desktop usage has been removed 
#if !defined(FEATURE_CORECLR) 
STDAPI GetCORSystemDirectoryInternal(__out_ecount_part_opt(cchBuffer, *pdwLength) LPWSTR pBuffer,
    DWORD  cchBuffer,
    __out_opt DWORD* pdwLength)
{
#if defined(CROSSGEN_COMPILE)

    CONTRACTL{
        NOTHROW;
    GC_NOTRIGGER;
    ENTRY_POINT;
    PRECONDITION(CheckPointer(pBuffer, NULL_OK));
    PRECONDITION(CheckPointer(pdwLength, NULL_OK));
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    if (pdwLength == NULL)
        IfFailGo(E_POINTER);

    if (pBuffer == NULL)
        IfFailGo(E_POINTER);

    if (WszGetModuleFileName(NULL, pBuffer, cchBuffer) == 0)
    {
        IfFailGo(HRESULT_FROM_GetLastError());
    }
    WCHAR *pSeparator;
    pSeparator = wcsrchr(pBuffer, DIRECTORY_SEPARATOR_CHAR_W);
    if (pSeparator == NULL)
    {
        IfFailGo(HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND));
    }
    *pSeparator = W('\0');

    // Include the null terminator in the length
    *pdwLength = (DWORD)wcslen(pBuffer) + 1;

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;

#else // CROSSGEN_COMPILE

    // Simply forward the call to the ICLRRuntimeInfo implementation.
    STATIC_CONTRACT_WRAPPER;
    HRESULT hr = S_OK;
    if (g_pCLRRuntime)
    {
        hr = g_pCLRRuntime->GetRuntimeDirectory(pBuffer, &cchBuffer);
        *pdwLength = cchBuffer;
    }
    else
    {
        // not invoked via shim (most probably loaded by Fusion)
        WCHAR wszPath[_MAX_PATH];
        DWORD dwLength = WszGetModuleFileName(g_hThisInst, wszPath, NumItems(wszPath));


        if (dwLength == 0 || (dwLength == NumItems(wszPath) && GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            return E_UNEXPECTED;
        }

        LPWSTR pwzSeparator = wcsrchr(wszPath, W('\\'));
        if (pwzSeparator == NULL)
        {
            return E_UNEXPECTED;
        }
        pwzSeparator[1] = W('\0'); // after '\'

        LPWSTR pwzDirectoryName = wszPath;

        size_t cchLength = wcslen(pwzDirectoryName) + 1;

        if (cchBuffer < cchLength)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        else
        {
            if (pBuffer != NULL)
            {
                // all look good, copy the string over
                wcscpy_s(pBuffer,
                    cchLength,
                    pwzDirectoryName
                    );
            }
        }

        // hand out the length regardless of success/failure
        *pdwLength = (DWORD)cchLength;
    }
    return hr;

#endif // CROSSGEN_COMPILE
}
#endif // !FEATURE_CORECLR 

STDAPI GetCORSystemDirectoryInternaL(SString& pBuffer)
{
#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;        
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;
    
        
#ifdef CROSSGEN_COMPILE

    if (WszGetModuleFileName(NULL, pBuffer) > 0)
    {     
        hr = CopySystemDirectory(pBuffer, pBuffer);    
    }
    else {
        hr = HRESULT_FROM_GetLastError();
    }
        
#else
       
    if (!PAL_GetPALDirectoryWrapper(pBuffer)) {
        hr = HRESULT_FROM_GetLastError();
    }
#endif

    END_ENTRYPOINT_NOTHROW;
    return hr;

#else // FEATURE_CORECLR || CROSSGEN_COMPILE
    DWORD cchBuffer = MAX_PATH - 1;
    // Simply forward the call to the ICLRRuntimeInfo implementation.
    STATIC_CONTRACT_WRAPPER;
    HRESULT hr = S_OK;
    if (g_pCLRRuntime)
    {
        WCHAR* temp = pBuffer.OpenUnicodeBuffer(cchBuffer);
        hr = g_pCLRRuntime->GetRuntimeDirectory(temp, &cchBuffer);
        pBuffer.CloseBuffer(cchBuffer - 1);
    }
    else
    {
        // not invoked via shim (most probably loaded by Fusion)
        DWORD dwLength = WszGetModuleFileName(g_hThisInst, pBuffer);
            

        if (dwLength == 0 || ((dwLength == pBuffer.GetCount() + 1) && GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            return E_UNEXPECTED;
        }

        CopySystemDirectory(pBuffer, pBuffer);
    }
    return hr;

#endif // FEATURE_CORECLR || CROSSGEN_COMPILE
}

//
// Returns version of the runtime (null-terminated).
// 
// Arguments:
//    pBuffer - [out] Output buffer allocated by caller of size cchBuffer.
//    cchBuffer - Size of pBuffer in characters.
//    pdwLength - [out] Size of the version string in characters (incl. null-terminator). Will be filled 
//                even if ERROR_INSUFFICIENT_BUFFER is returned.
// 
// Return Value:
//    S_OK - Output buffer contains the version string.
//    HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) - *pdwLength contains required size of the buffer in 
//                                                    characters.

STDAPI GetCORVersionInternal(
__out_ecount_z_opt(cchBuffer) LPWSTR pBuffer, 
                              DWORD cchBuffer,
                        __out DWORD *pdwLength)
{
#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pBuffer, NULL_OK));
        PRECONDITION(CheckPointer(pdwLength));
    } CONTRACTL_END;
        
    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;
    
    if ((pBuffer != NULL) && (cchBuffer > 0))
    {   // Initialize the output for case the function fails
        *pBuffer = W('\0');
    }

#define VERSION_NUMBER_NOSHIM W("v") QUOTE_MACRO_L(VER_MAJORVERSION.VER_MINORVERSION.VER_PRODUCTBUILD)

    DWORD length = (DWORD)(wcslen(VERSION_NUMBER_NOSHIM) + 1);
    if (length > cchBuffer)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }
    else
    {
        if (pBuffer == NULL)
        {
            hr = E_POINTER;
        }
        else
        {
            CopyMemory(pBuffer, VERSION_NUMBER_NOSHIM, length * sizeof(WCHAR));
            hr = S_OK;
        }
    }
    *pdwLength = length;

    END_ENTRYPOINT_NOTHROW;
    return hr;

#else // FEATURE_CORECLR || CROSSGEN_COMPILE

    // Simply forward the call to the ICLRRuntimeInfo implementation.
    STATIC_CONTRACT_WRAPPER;
    HRESULT hr = S_OK;
    if (g_pCLRRuntime)
    {
        hr = g_pCLRRuntime->GetVersionString(pBuffer, &cchBuffer);
       *pdwLength = cchBuffer;
    }
    else
    {
        // not invoked via shim (most probably loaded by Fusion)
        WCHAR wszPath[_MAX_PATH];
        DWORD dwLength = WszGetModuleFileName(g_hThisInst, wszPath,NumItems(wszPath));
            

        if (dwLength == 0 || (dwLength == NumItems(wszPath) && GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            return E_UNEXPECTED;
        }

        LPWSTR pwzSeparator = wcsrchr(wszPath, W('\\'));
        if (pwzSeparator == NULL)
        {
            return E_UNEXPECTED;
        }
        *pwzSeparator = W('\0');

        LPWSTR pwzDirectoryName = wcsrchr(wszPath, W('\\')); 
        if (pwzDirectoryName == NULL)
        {
            return E_UNEXPECTED;
        }
        pwzDirectoryName++; // skip '\'

        size_t cchLength = wcslen(pwzDirectoryName) + 1;

        if (cchBuffer < cchLength)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        else
        {
            if (pBuffer != NULL)
            {
                // all look good, copy the string over
                wcscpy_s(pBuffer,
                         cchLength,
                         pwzDirectoryName
                        );
            }
        }

        // hand out the length regardless of success/failure
        *pdwLength = (DWORD)cchLength;
       
    }
    return hr;

#endif // FEATURE_CORECLR || CROSSGEN_COMPILE

}

#ifndef CROSSGEN_COMPILE
#ifndef FEATURE_CORECLR
STDAPI LoadLibraryShimInternal(LPCWSTR szDllName, LPCWSTR szVersion, LPVOID pvReserved, HMODULE *phModDll)
{
    // Simply forward the call to the ICLRRuntimeInfo implementation.
    STATIC_CONTRACT_WRAPPER;
    if (g_pCLRRuntime)
    {
        return g_pCLRRuntime->LoadLibrary(szDllName, phModDll);
    }
    else
    {
        // no runtime info, probably loaded directly (e.g. from Fusion)
        // just look next to ourselves.
        WCHAR wszPath[MAX_PATH];
        DWORD dwLength = WszGetModuleFileName(g_hThisInst, wszPath,NumItems(wszPath));
            

        if (dwLength == 0 || (dwLength == NumItems(wszPath) && GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            return E_UNEXPECTED;
        }

        LPWSTR pwzSeparator = wcsrchr(wszPath, W('\\'));
        if (pwzSeparator == NULL)
        {
            return E_UNEXPECTED;
        }
        pwzSeparator[1]=W('\0');

        wcscat_s(wszPath,NumItems(wszPath),szDllName);
        *phModDll= WszLoadLibraryEx(wszPath,NULL,GetLoadWithAlteredSearchPathFlag());

        if (*phModDll == NULL)
        {
            return HRESULT_FROM_GetLastError();
        }
        return S_OK;
    }
}
#endif
#endif 

static DWORD g_dwSystemDirectory = 0;
static WCHAR * g_pSystemDirectory = NULL;

HRESULT GetInternalSystemDirectory(__out_ecount_part_opt(*pdwLength,*pdwLength) LPWSTR buffer, __inout DWORD* pdwLength)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(buffer, NULL_OK));
        PRECONDITION(CheckPointer(pdwLength));
    } CONTRACTL_END;
    
    if (g_dwSystemDirectory == 0)
        SetInternalSystemDirectory();

    //
    // g_dwSystemDirectory includes the NULL in its count!
    //
    if(*pdwLength < g_dwSystemDirectory) 
    {
        *pdwLength = g_dwSystemDirectory;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    if (buffer != NULL)
    {
        //
        // wcsncpy_s will automatically append a null and g_dwSystemDirectory 
        // includes the null in its count, so we have to subtract 1.
        //
        wcsncpy_s(buffer, *pdwLength, g_pSystemDirectory, g_dwSystemDirectory-1);
    }
    *pdwLength = g_dwSystemDirectory;
    return S_OK;
}


LPCWSTR GetInternalSystemDirectory(__out DWORD* pdwLength)
{
    LIMITED_METHOD_CONTRACT;

    if (g_dwSystemDirectory == 0)
    {
        SetInternalSystemDirectory();
    }
    
    if (pdwLength != NULL)
    {
        * pdwLength = g_dwSystemDirectory;
    }

    return g_pSystemDirectory;
}


HRESULT SetInternalSystemDirectory()
 {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    if(g_dwSystemDirectory == 0) {

        DWORD len = 0;
        NewArrayHolder<WCHAR> pSystemDirectory;
        EX_TRY{

            // use local buffer for thread safety
            PathString wzSystemDirectory;
            
            hr = GetCORSystemDirectoryInternaL(wzSystemDirectory);

            if (FAILED(hr)) {
                wzSystemDirectory.Set(W('\0'));
            }

            pSystemDirectory = wzSystemDirectory.GetCopyOfUnicodeString();
            if (pSystemDirectory == NULL)
            {
               hr =  HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
            }
            len = wzSystemDirectory.GetCount() + 1;

        }
        EX_CATCH_HRESULT(hr);

        // publish results idempotently with correct memory ordering
        g_pSystemDirectory = pSystemDirectory.Extract();
        
        (void)InterlockedExchange((LONG *)&g_dwSystemDirectory, len);
    }

    return hr;
}

#if defined(CROSSGEN_COMPILE) && defined(FEATURE_CORECLR)
void SetMscorlibPath(LPCWSTR wzSystemDirectory)
{
    DWORD len = (DWORD)wcslen(wzSystemDirectory);
    bool appendSeparator = wzSystemDirectory[len-1] != DIRECTORY_SEPARATOR_CHAR_W;
    DWORD lenAlloc = appendSeparator ? len+2 : len+1;
    if (g_dwSystemDirectory < lenAlloc)
    {
        delete [] g_pSystemDirectory;
        g_pSystemDirectory = new (nothrow) WCHAR[lenAlloc];
        
        if (g_pSystemDirectory == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            return;
        }
    }
    
    wcscpy_s(g_pSystemDirectory, len+1, wzSystemDirectory);
    
    if(appendSeparator)
    {
        g_pSystemDirectory[len] = DIRECTORY_SEPARATOR_CHAR_W;
        g_pSystemDirectory[len+1] = W('\0');
        g_dwSystemDirectory = len + 1;
    }
    else
    {
        g_dwSystemDirectory = len;
    }
}
#endif
