// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DbgShim.cpp
//
// This contains the APIs for creating a telesto managed-debugging session. These APIs serve to locate an
// mscordbi.dll for a given telesto dll and then instantiate the ICorDebug object.
//
//*****************************************************************************

#include <utilcode.h>

#include "dbgshim.h"

#include "palclr.h"

#include <libloaderapi.h>

/*

// Here's a High-level overview of the API usage

From the debugger:
A debugger calls GetStartupNotificationEvent(pid of debuggee) to get an event, which is signalled when
that process loads a Telesto.  The debugger thus waits on that event, and when it's signalled, it can call
EnumerateCLRs / CloseCLREnumeration to get an array of Telestos in the target process (including the one
that was just loaded). It can then call CreateVersionStringFromModule, CreateDebuggingInterfaceFromVersion
to attach to any or all Telestos of interest.

From the debuggee:
When a new Telesto spins up, it checks for the startup event (created via GetStartupNotificationEvent), and
if it exists, it will:
- signal it
- wait on the "Continue" event, thus giving a debugger a chance to attach to the telesto

Notes:
- There is no CreateProcess (Launch) case. All Launching is really an "Early-attach case".

*/

#ifndef MAX_LONGPATH
#define MAX_LONGPATH   1024
#endif

#ifdef HOST_UNIX
#define INITIALIZE_SHIM { if (PAL_InitializeDLL() != 0) return E_FAIL; }
#else
#define INITIALIZE_SHIM
#endif

// Contract for public APIs. These must be NOTHROW.
#define PUBLIC_CONTRACT \
    INITIALIZE_SHIM \
    CONTRACTL \
    { \
        NOTHROW; \
    } \
    CONTRACTL_END;

// Functions that we'll look for in the loaded Mscordbi module.
typedef HRESULT (STDAPICALLTYPE *FPCoreCLRCreateCordbObject)(
    int iDebuggerVersion,
    DWORD pid,
    HMODULE hmodTargetCLR,
    IUnknown **ppCordb);

//-----------------------------------------------------------------------------
// Public API.
//
// CreateProcessForLaunch - a stripped down version of the Windows CreateProcess
// that can be supported cross-platform.
//
//-----------------------------------------------------------------------------
MONO_API HRESULT
CreateProcessForLaunch(
    __in LPWSTR lpCommandLine,
    __in BOOL bSuspendProcess,
    __in LPVOID lpEnvironment,
    __in LPCWSTR lpCurrentDirectory,
    __out PDWORD pProcessId,
    __out HANDLE *pResumeHandle)
{
    PUBLIC_CONTRACT;
    PROCESS_INFORMATION processInfo;
    STARTUPINFOW startupInfo;
    DWORD dwCreationFlags = 0;

    ZeroMemory(&processInfo, sizeof(processInfo));
    ZeroMemory(&startupInfo, sizeof(startupInfo));

    startupInfo.cb = sizeof(startupInfo);

    putenv("MONO_ENV_OPTIONS='--debugger-agent=transport=dt_socket,address=127.0.0.1:pid_based,server=n,suspend=y,loglevel=10,timeout=100000'");

    BOOL result = CreateProcessW(
        NULL,
        lpCommandLine,
        NULL,
        NULL,
        FALSE,
        dwCreationFlags,
        NULL,
        lpCurrentDirectory,
        &startupInfo,
        &processInfo);

    if (!result) {
        *pProcessId = 0;
        *pResumeHandle = NULL;
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (processInfo.hProcess != NULL)
    {
        CloseHandle(processInfo.hProcess);
    }

    *pProcessId = processInfo.dwProcessId;
    *pResumeHandle = processInfo.hThread;

    return S_OK;
}

MONO_API HRESULT
ResumeProcess(
    __in HANDLE hResumeHandle)
{
    return S_OK;
}

MONO_API HRESULT
CloseResumeHandle(
    __in HANDLE hResumeHandle)
{
    return S_OK;
}


HRESULT CreateCoreDbg(HMODULE hDBIModule, DWORD processId, int iDebuggerVersion, IUnknown **ppCordb)
{
    HRESULT hr = S_OK;

    FPCoreCLRCreateCordbObject fpCreate =
        (FPCoreCLRCreateCordbObject)GetProcAddress(hDBIModule, "CoreCLRCreateCordbObject");
    if (fpCreate == NULL)
    {
        return CORDBG_E_INCOMPATIBLE_PROTOCOL;
    }

    return fpCreate(iDebuggerVersion, processId, NULL, ppCordb);

    return hr;
}

char* convertC(const WCHAR * wString) 
{
    int size;
    char * MultiBuffer = NULL;

    size = WideCharToMultiByte(CP_ACP,0,wString,-1,MultiBuffer,0,NULL,NULL);
    MultiBuffer = (char*) malloc(size);
    if (MultiBuffer == NULL)
    {
        return NULL;
    }
    WideCharToMultiByte(CP_ACP,0,wString,-1,MultiBuffer,size,NULL,NULL);
    return MultiBuffer;
}

static IUnknown* pCordb = NULL;

MONO_API HRESULT
RegisterForRuntimeStartup(
    __in DWORD dwProcessId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken)
{
    if (pCordb != NULL)
        return S_OK;

    HRESULT hr = S_OK;
    HMODULE hMod = NULL;
    
    char* msCorDbiPath = getenv("MSCORDBI_PATH");

    hMod = LoadLibraryA(msCorDbiPath);
    if (hMod == NULL)
    {
        hr = CORDBG_E_DEBUG_COMPONENT_MISSING;
        goto exit;
    }

    hr = CreateCoreDbg(hMod, dwProcessId, 0, &pCordb);

    exit:
    if (FAILED(hr))
    {
        _ASSERTE(pCordb == NULL);

        if (hMod != NULL)
        {
            FreeLibrary(hMod);
        }

        // Invoke the callback on error
        pfnCallback(NULL, parameter, hr);
    }
    pfnCallback(pCordb, parameter, S_OK);
    return S_OK;
}

MONO_API HRESULT
RegisterForRuntimeStartupEx(
    __in DWORD dwProcessId,
    __in LPCWSTR szApplicationGroupId,
    __in PSTARTUP_CALLBACK pfnCallback,
    __in PVOID parameter,
    __out PVOID *ppUnregisterToken)
{
    return S_OK;
}

MONO_API HRESULT
UnregisterForRuntimeStartup(
    __in PVOID pUnregisterToken)
{
    return S_OK;
}

MONO_API HRESULT
GetStartupNotificationEvent(
    __in DWORD debuggeePID,
    __out HANDLE* phStartupEvent)
{
    return S_OK;
}

MONO_API HRESULT
EnumerateCLRs(DWORD debuggeePID,
    __out HANDLE** ppHandleArrayOut,
    __out LPWSTR** ppStringArrayOut,
    __out DWORD* pdwArrayLengthOut)
{
    return S_OK;
}

MONO_API HRESULT
CloseCLREnumeration(
    __in HANDLE* pHandleArray,
    __in LPWSTR* pStringArray,
    __in DWORD dwArrayLength)
{
    return S_OK;
}

MONO_API HRESULT
CreateVersionStringFromModule(
    __in DWORD pidDebuggee,
    __in LPCWSTR szModuleName,
    __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
    __in DWORD cchBuffer,
    __out DWORD* pdwLength)
{
    return S_OK;
}

MONO_API HRESULT
CreateDebuggingInterfaceFromVersionEx(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb)
{
    return S_OK;
}

MONO_API
HRESULT
CreateDebuggingInterfaceFromVersion2(
    __in int iDebuggerVersion,
    __in LPCWSTR szDebuggeeVersion,
    __in LPCWSTR szApplicationGroupId,
    __out IUnknown ** ppCordb)
{
    return S_OK;
}

MONO_API HRESULT
CreateDebuggingInterfaceFromVersion(
    __in LPCWSTR szDebuggeeVersion,
    __out IUnknown ** ppCordb)
{
    return S_OK;
}
