// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

#include <dbgenginemetrics.h>

// Globals
extern HINSTANCE g_hThisInst;

// Locals.
BOOL STDMETHODCALLTYPE EEDllMain( // TRUE on success, FALSE on error.
                       HINSTANCE    hInst,                  // Instance handle of the loaded module.
                       DWORD        dwReason,               // Reason for loading.
                       LPVOID       lpReserved);                // Unused.

#ifndef CROSSGEN_COMPILE
//*****************************************************************************
// Handle lifetime of loaded library.
//*****************************************************************************

#include <shlwapi.h>

#ifdef TARGET_WINDOWS

#include <process.h> // for __security_init_cookie()

extern "C" BOOL WINAPI _CRT_INIT(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);
extern "C" BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);

// For the CoreClr, this is the real DLL entrypoint. We make ourselves the first entrypoint as
// we need to capture coreclr's hInstance before the C runtime initializes. This function
// will capture hInstance, let the C runtime initialize and then invoke the "classic"
// DllMain that initializes everything else.
extern "C" BOOL WINAPI CoreDllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    BOOL result;
    switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
            // Make sure the /GS security cookie is initialized before we call anything else.
            // BinScope detects the call to __security_init_cookie in its "Has Non-GS-friendly
            // Initialization" check and makes it pass.
            __security_init_cookie();

            // It's critical that we initialize g_hmodCoreCLR before the CRT initializes.
            // We have a lot of global ctors that will break if we let the CRT initialize without
            // this step having been done.

            g_hmodCoreCLR = (HINSTANCE)hInstance;

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

#endif // TARGET_WINDOWS


extern "C"
#ifdef TARGET_UNIX
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        {
#ifndef TARGET_WINDOWS
            g_hmodCoreCLR = (HINSTANCE)hInstance;
#endif

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
void SetCoreLibPath(LPCWSTR wzSystemDirectory)
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
