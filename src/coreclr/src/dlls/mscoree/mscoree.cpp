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

#include "product_version.h"

#ifdef FEATURE_COMINTEROP
#include "ComCallUnmarshal.h"
#endif // FEATURE_COMINTEROP

#include "clrprivhosting.h"

#include <dbgenginemetrics.h>

// Locals.
BOOL STDMETHODCALLTYPE EEDllMain( // TRUE on success, FALSE on error.
                       HINSTANCE    hInst,                  // Instance handle of the loaded module.
                       DWORD        dwReason,               // Reason for loading.
                       LPVOID       lpReserved);                // Unused.

// Globals.
HINSTANCE g_hThisInst;  // This library.

#ifndef CROSSGEN_COMPILE
//*****************************************************************************
// Handle lifetime of loaded library.
//*****************************************************************************

#include <shlwapi.h>

#include <process.h> // for __security_init_cookie()

extern "C" IExecutionEngine* IEE();

#ifdef NO_CRT_INIT
#define _CRT_INIT(hInstance, dwReason, lpReserved) (TRUE)
#else
extern "C" BOOL WINAPI _CRT_INIT(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);
#endif

extern "C" BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);

// For the CoreClr, this is the real DLL entrypoint. We make ourselves the first entrypoint as
// we need to capture coreclr's hInstance before the C runtime initializes. This function
// will capture hInstance, let the C runtime initialize and then invoke the "classic"
// DllMain that initializes everything else.
extern "C"
#ifdef FEATURE_PAL
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI CoreDllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
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

extern "C"
#ifdef FEATURE_PAL
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        {
            // Save the module handle.
            g_hThisInst = (HINSTANCE)hInstance;

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
STDAPI DLLEXPORT MetaDataGetDispenser(            // Return HRESULT
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
STDAPI DLLEXPORT GetMetaDataInternalInterface(
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
STDAPI DLLEXPORT GetMetaDataInternalInterfaceFromPublic(
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
STDAPI DLLEXPORT GetMetaDataPublicInterfaceFromInternal(
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


// Replacement for legacy shim API GetCORRequiredVersion(...) used in linked libraries.
// Used in code:TiggerStorage::GetDefaultVersion#CallTo_CLRRuntimeHostInternal_GetImageVersionString.
HRESULT 
CLRRuntimeHostInternal_GetImageVersionString(
    __out_ecount_opt(*pcchBuffer) LPWSTR wszBuffer, 
    __inout                       DWORD *pcchBuffer)
{
    // Simply forward the call to the ICLRRuntimeHostInternal implementation.
    STATIC_CONTRACT_WRAPPER;

    HRESULT hr = GetCORVersionInternal(wszBuffer, *pcchBuffer, pcchBuffer);
    
    return hr;
} // CLRRuntimeHostInternal_GetImageVersionString

STDAPI GetCORSystemDirectoryInternaL(SString& pBuffer)
{
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

#define VERSION_NUMBER_NOSHIM W("v") QUOTE_MACRO_L(CLR_MAJOR_VERSION.CLR_MINOR_VERSION.CLR_BUILD_VERSION)

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

}

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

#if defined(CROSSGEN_COMPILE)
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
