// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Standard header.
#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>

#include "../dlls/mscorrc/resource.h"
#ifdef HOST_UNIX
#include "resourcestring.h"
#define NATIVE_STRING_RESOURCE_NAME mscorrc
__attribute__((visibility("default"))) DECLARE_NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME);
#endif
#include "sstring.h"
#include "stringarraylist.h"
#include "corpriv.h"

#include <stdlib.h>

// External prototypes.
extern void* GetClrModuleBase();

//*****************************************************************************
// Initialize
//*****************************************************************************
LPCWSTR CCompRC::m_pDefaultResource = W("mscorrc.dll");

HRESULT CCompRC::Init(LPCWSTR pResourceFile)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

	// This function is called during Watson process.  We need to make sure
	// that this function is restartable.
	//
    // Make sure to NEVER null out the function callbacks in the Init
    // function. They get set for the "Default CCompRC" during EEStartup
    // and we want to make sure we don't wipe them out.

    if (m_pResourceFile == NULL)
    {
        if(pResourceFile)
        {
            NewArrayHolder<WCHAR> pwszResourceFile(NULL);

            DWORD lgth = (DWORD) u16_strlen(pResourceFile) + 1;
            pwszResourceFile = new(nothrow) WCHAR[lgth];
            if (pwszResourceFile)
            {
                wcscpy_s(pwszResourceFile, lgth, pResourceFile);
                LPCWSTR pFile = pwszResourceFile.Extract();
                if (InterlockedCompareExchangeT(&m_pResourceFile, pFile, NULL) != NULL)
                {
                    delete [] pFile;
                }
            }
        }
    else
        InterlockedCompareExchangeT(&m_pResourceFile, m_pDefaultResource, NULL);
    }

    if (m_pResourceFile == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if (m_csMap == NULL)
    {
    // NOTE: there are times when the debugger's helper thread is asked to do a favor for another thread in the
    // process. Typically, this favor involves putting up a dialog for the user. Putting up a dialog usually ends
    // up involving the CCompRC code since (of course) the strings in the dialog are in a resource file. Thus, the
    // debugger's helper thread will attempt to acquire this CRST. This is okay, since the helper thread only does
    // these favors for other threads when there is no debugger attached. Thus, there are no deadlock hazards with
    // this lock, and its safe for the helper thread to take, so this CRST is marked with CRST_DEBUGGER_THREAD.
        CRITSEC_COOKIE csMap = ClrCreateCriticalSection(CrstCCompRC,
                                       (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));

        if (csMap)
        {
            if (InterlockedCompareExchangeT(&m_csMap, csMap, NULL) != NULL)
            {
                ClrDeleteCriticalSection(csMap);
            }
        }
    }

    if (m_csMap == NULL)
        return E_OUTOFMEMORY;

    return S_OK;
}

void CCompRC::Destroy()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    // Free all resource libraries

    //*****************************************************************************
    // Free the loaded library if we ever loaded it. This is done
    // only in debug mode to make coverage runs accurate.
    //*****************************************************************************

#if defined(_DEBUG)
    if (m_Primary.GetLibraryHandle()) {
        ::FreeLibrary(m_Primary.GetLibraryHandle());
    }
#endif

    // destroy map structure
    if(m_pResourceFile != m_pDefaultResource)
        delete [] m_pResourceFile;
    m_pResourceFile = NULL;

    if(m_csMap) {
        ClrDeleteCriticalSection(m_csMap);
        ZeroMemory(&(m_csMap), sizeof(CRITSEC_COOKIE));
    }
}


//*****************************************************************************
// Initialization is done lazily, for backwards compatibility "mscorrc.dll"
// is consider the default location for all strings that use CCompRC.
// An instance value for CCompRC can be created to load resources from a different
// resource dll.
//*****************************************************************************
LONG    CCompRC::m_dwDefaultInitialized = 0;
CCompRC CCompRC::m_DefaultResourceDll;

CCompRC* CCompRC::GetDefaultResourceDll()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    if (m_dwDefaultInitialized)
        return &m_DefaultResourceDll;

    if(FAILED(m_DefaultResourceDll.Init(NULL)))
    {
        return NULL;
    }
    m_dwDefaultInitialized = 1;

    return &m_DefaultResourceDll;
}

//*****************************************************************************
//*****************************************************************************

// String resources packaged as PE files only exist on Windows
#ifdef HOST_WINDOWS
HRESULT CCompRC::GetLibrary(HRESOURCEDLL* phInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
        PRECONDITION(phInst != NULL);
    }
    CONTRACTL_END;

    HRESULT     hr = E_FAIL;
    HRESOURCEDLL    hInst = 0;
#ifndef DACCESS_COMPILE
    HRESOURCEDLL    hLibInst = 0; //Holds early library instance
    BOOL        fLibAlreadyOpen = FALSE; //Determine if we can close the opened library.
#endif

    // Try to match the primary entry, or else use the primary if we don't care.
    if (m_Primary.IsSet())
    {
        hInst = m_Primary.GetLibraryHandle();
        hr = S_OK;
    }
    else if(m_Primary.IsMissing())
    {
        // If primary is missing then the hash will not have anything either
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }
#ifndef DACCESS_COMPILE
    // If this is the first visit, we must set the primary entry
    else
    {
        // Don't immediately return if LoadLibrary fails so we can indicate the file was missing
        hr = LoadLibrary(&hLibInst);
        // If it's a transient failure, don't cache the failure
        if (FAILED(hr) && Exception::IsTransient(hr))
        {
            return hr;
        }

        CRITSEC_Holder csh (m_csMap);
        // As we expected
        if (!m_Primary.IsSet() && !m_Primary.IsMissing())
        {
            hInst  = hLibInst;
            if (SUCCEEDED(hr))
            {
                m_Primary.Set(hLibInst);
            }
            else
            {
                m_Primary.SetMissing();
            }
        }

        // Someone got into this critical section before us and set the primary already
        else
        {
            hInst = m_Primary.GetLibraryHandle();
            fLibAlreadyOpen = TRUE;
        }

        IfFailRet(hr);

        if (fLibAlreadyOpen)
        {
            FreeLibrary(hLibInst);
            fLibAlreadyOpen = FALSE;
        }
    }
#endif

    _ASSERTE(SUCCEEDED(hr) || hInst == NULL);
    *phInst = hInst;
    return hr;
}
#endif // HOST_WINDOWS

//*****************************************************************************
// Load the string
// We load the localized libraries and cache the handle for future use.
// Mutliple threads may call this, so the cache structure is thread safe.
//*****************************************************************************
HRESULT CCompRC::LoadString(ResourceCategory eCategory, UINT iResourceID, _Out_writes_(iMax) LPWSTR szBuffer, int iMax,  int *pcwchUsed)
{
#ifdef DBI_COMPONENT_MONO
    return E_NOTIMPL;
#else
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

#ifdef HOST_WINDOWS
    HRESULT         hr;
    HRESOURCEDLL    hInst = 0; //instance of cultured resource dll
    int length;

    hr = GetLibrary(&hInst);

    if (SUCCEEDED(hr))
    {
        // Now that we have the proper dll handle, load the string
        _ASSERTE(hInst != NULL);

        length = ::LoadString(hInst, iResourceID, szBuffer, iMax);
        if(length > 0)
        {
            if(pcwchUsed)
            {
                *pcwchUsed = length;
            }
            return (S_OK);
        }
        if(GetLastError()==ERROR_SUCCESS)
            hr=HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            hr=HRESULT_FROM_GetLastError();
    }

    // Return an empty string to save the people with a bad error handling
    if (szBuffer && iMax)
        *szBuffer = W('\0');

    return hr;
#else // HOST_WINDOWS
    return LoadNativeStringResource(NATIVE_STRING_RESOURCE_TABLE(NATIVE_STRING_RESOURCE_NAME), iResourceID,
      szBuffer, iMax, pcwchUsed);
#endif // HOST_WINDOWS
#endif
}

#ifndef DACCESS_COMPILE

// String resources packaged as PE files only exist on Windows
#ifdef HOST_WINDOWS
HRESULT CCompRC::LoadResourceFile(HRESOURCEDLL * pHInst, LPCWSTR lpFileName)
{
    DWORD dwLoadLibraryFlags;
    if(m_pResourceFile == m_pDefaultResource)
    {
        dwLoadLibraryFlags = LOAD_LIBRARY_AS_DATAFILE;
    }
    else
    {
        dwLoadLibraryFlags = 0;
    }

    if((*pHInst = WszLoadLibrary(lpFileName, NULL, dwLoadLibraryFlags)) == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }
    return S_OK;
}

//*****************************************************************************
// Load the library from root path (<dir passed>\mscorrc.dll). No locale support.
//*****************************************************************************
HRESULT CCompRC::LoadLibraryHelper(HRESOURCEDLL *pHInst,
                                   SString& rcPath)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    HRESULT     hr = E_FAIL;


    _ASSERTE(m_pResourceFile != NULL);

    // must initialize before calling SString::Empty()
    SString::Startup();

    EX_TRY
    {
        PathString rcPathName(rcPath);

        if (!rcPathName.EndsWith(SL(W("\\"))))
        {
            rcPathName.Append(W("\\"));
        }

        {
            rcPathName.Append(m_pResourceFile);
        }

        // Load the resource library as a data file, so that the OS doesn't have
        // to allocate it as code.  This only works so long as the file contains
        // only strings.
        hr = LoadResourceFile(pHInst, rcPathName);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// Two-stage approach:
// First try module directory, then try CORSystemDirectory for default resource
HRESULT CCompRC::LoadLibraryThrows(HRESOURCEDLL * pHInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
#ifdef      MODE_PREEMPTIVE
        MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    _ASSERTE(pHInst != NULL);


#ifdef SELF_NO_HOST
    _ASSERTE(!"CCompRC::LoadLibraryThrows not implemented for SELF_NO_HOST");
    hr = E_NOTIMPL;
#else // SELF_NO_HOST
    PathString       rcPath;      // Path to resource DLL.

    // Try first in the same directory as this dll.

    hr = GetClrModuleDirectory(rcPath);
    if (FAILED(hr))
        return hr;

    hr = LoadLibraryHelper(pHInst, rcPath);
#endif


    return hr;
}

HRESULT CCompRC::LoadLibrary(HRESOURCEDLL * pHInst)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
    NOTHROW;
#ifdef      MODE_PREEMPTIVE
    MODE_PREEMPTIVE;
#endif
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = LoadLibraryThrows(pHInst);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
#endif // DACCESS_COMPILE
#endif //HOST_WINDOWS
