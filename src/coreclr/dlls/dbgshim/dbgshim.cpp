// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DbgShim.cpp
//
// This contains the APIs for creating a telesto managed-debugging session. These APIs serve to locate an
// mscordbi.dll for a given telesto dll and then instantiate the ICorDebug object.
//
//*****************************************************************************

#include <winwrap.h>
#include <utilcode.h>
#include <log.h>
#include <tlhelp32.h>
#include <cor.h>
#include <sstring.h>
#ifndef TARGET_UNIX
#include <securityutil.h>
#endif

#include <ex.h>
#include <cordebug.h> // for Version nunmbers
#include <pedecoder.h>
#include <getproductversionnumber.h>
#include <dbgenginemetrics.h>
#include <arrayholder.h>

#ifndef TARGET_UNIX
#define PSAPI_VERSION 2
#include <psapi.h>
#endif

#include "dbgshim.h"

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

//-----------------------------------------------------------------------------
// Public API.
//
// CreateProcessForLaunch - a stripped down version of the Windows CreateProcess
// that can be supported cross-platform.
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CreateProcessForLaunch(
    _In_ LPWSTR lpCommandLine,
    _In_ BOOL bSuspendProcess,
    _In_ LPVOID lpEnvironment,
    _In_ LPCWSTR lpCurrentDirectory,
    _Out_ PDWORD pProcessId,
    _Out_ HANDLE *pResumeHandle)
{
    PUBLIC_CONTRACT;

    PROCESS_INFORMATION processInfo;
    STARTUPINFOW startupInfo;
    DWORD dwCreationFlags = 0;

    ZeroMemory(&processInfo, sizeof(processInfo));
    ZeroMemory(&startupInfo, sizeof(startupInfo));

    startupInfo.cb = sizeof(startupInfo);

    if (bSuspendProcess)
    {
        dwCreationFlags = CREATE_SUSPENDED;
    }

    BOOL result = CreateProcessW(
        NULL,
        lpCommandLine,
        NULL,
        NULL,
        FALSE,
        dwCreationFlags,
        lpEnvironment,
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

//-----------------------------------------------------------------------------
// Public API.
//
// ResumeProcess - to be used with the CreateProcessForLaunch resume handle
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
ResumeProcess(
    _In_ HANDLE hResumeHandle)
{
    PUBLIC_CONTRACT;
    if (ResumeThread(hResumeHandle) == (DWORD)-1)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// Public API.
//
// CloseResumeHandle - to be used with the CreateProcessForLaunch resume handle
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CloseResumeHandle(
    _In_ HANDLE hResumeHandle)
{
    PUBLIC_CONTRACT;
    if (!CloseHandle(hResumeHandle))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }
    return S_OK;
}

#ifdef TARGET_UNIX

static
void
RuntimeStartupHandler(
    char *pszModulePath,
    HMODULE hModule,
    PVOID parameter);

#else // TARGET_UNIX

static
DWORD
StartupHelperThread(
    LPVOID p);

static
HRESULT
GetContinueStartupEvent(
    DWORD debuggeePID,
    LPCWSTR szTelestoFullPath,
    _Out_ HANDLE *phContinueStartupEvent);

#endif // TARGET_UNIX

// Functions that we'll look for in the loaded Mscordbi module.
typedef HRESULT (STDAPICALLTYPE *FPCoreCLRCreateCordbObject)(
    int iDebuggerVersion,
    DWORD pid,
    HMODULE hmodTargetCLR,
    IUnknown **ppCordb);

typedef HRESULT (STDAPICALLTYPE *FPCoreCLRCreateCordbObjectEx)(
    int iDebuggerVersion,
    DWORD pid,
    LPCWSTR lpApplicationGroupId,
    HMODULE hmodTargetCLR,
    IUnknown **ppCordb);

HRESULT CreateCoreDbgWithSandboxSupport(
    HMODULE hCLRModule, HMODULE hDBIModule, DWORD processId, LPCWSTR lpApplicationGroupId, int iDebuggerVersion, IUnknown **ppCordb)
{
    FPCoreCLRCreateCordbObjectEx fpCreate =
        (FPCoreCLRCreateCordbObjectEx)GetProcAddress(hDBIModule, "CoreCLRCreateCordbObjectEx");
    if (fpCreate == NULL)
    {
        return CORDBG_E_INCOMPATIBLE_PROTOCOL;
    }

    return fpCreate(iDebuggerVersion, processId, lpApplicationGroupId, hCLRModule, ppCordb);
}

HRESULT CreateCoreDbgWithoutSandboxSupport(
    HMODULE hCLRModule, HMODULE hDBIModule, DWORD processId, int iDebuggerVersion, IUnknown **ppCordb)
{
    FPCoreCLRCreateCordbObject fpCreate =
        (FPCoreCLRCreateCordbObject)GetProcAddress(hDBIModule, "CoreCLRCreateCordbObject");
    if (fpCreate == NULL)
    {
        return CORDBG_E_INCOMPATIBLE_PROTOCOL;
    }

    return fpCreate(iDebuggerVersion, processId, hCLRModule, ppCordb);
}

HRESULT CreateCoreDbg(
    HMODULE hCLRModule, HMODULE hDBIModule, DWORD processId, LPCWSTR lpApplicationGroupId, int iDebuggerVersion, IUnknown **ppCordb)
{
    HRESULT hr = S_OK;

    if (lpApplicationGroupId != NULL)
    {
        hr = CreateCoreDbgWithSandboxSupport(hCLRModule, hDBIModule, processId, lpApplicationGroupId, iDebuggerVersion, ppCordb);
    }
    else
    {
        hr = CreateCoreDbgWithoutSandboxSupport(hCLRModule, hDBIModule, processId, iDebuggerVersion, ppCordb);
    }

    return hr;
}

//
// Helper class for RegisterForRuntimeStartup
//
class RuntimeStartupHelper
{
    LONG m_ref;
    DWORD m_processId;
    PSTARTUP_CALLBACK m_callback;
    PVOID m_parameter;
#ifdef TARGET_UNIX
    PVOID m_unregisterToken;
    LPWSTR m_applicationGroupId;
#else
    bool m_canceled;
    HANDLE m_startupEvent;
    DWORD m_threadId;
    HANDLE m_threadHandle;
#endif // TARGET_UNIX

public:
    RuntimeStartupHelper(DWORD dwProcessId, PSTARTUP_CALLBACK pfnCallback, PVOID parameter) :
        m_ref(1),
        m_processId(dwProcessId),
        m_callback(pfnCallback),
        m_parameter(parameter),
#ifdef TARGET_UNIX
        m_unregisterToken(NULL),
        m_applicationGroupId(NULL)
#else
        m_canceled(false),
        m_startupEvent(NULL),
        m_threadId(0),
        m_threadHandle(NULL)
#endif // TARGET_UNIX
    {
    }

    ~RuntimeStartupHelper()
    {
#ifdef TARGET_UNIX
        if (m_applicationGroupId != NULL)
        {
            delete m_applicationGroupId;
        }
#else // TARGET_UNIX
        if (m_startupEvent != NULL)
        {
            CloseHandle(m_startupEvent);
        }
        if (m_threadHandle != NULL)
        {
            CloseHandle(m_threadHandle);
        }
#endif // TARGET_UNIX
    }

    LONG AddRef()
    {
        LONG ref = InterlockedIncrement(&m_ref);
        return ref;
    }

    LONG Release()
    {
        LONG ref = InterlockedDecrement(&m_ref);
        if (ref == 0)
        {
            delete this;
        }
        return ref;
    }

#ifdef TARGET_UNIX

    HRESULT Register(LPCWSTR lpApplicationGroupId)
    {
        if (lpApplicationGroupId != NULL)
        {
            int size = wcslen(lpApplicationGroupId) + 1;
            m_applicationGroupId = new (nothrow) WCHAR[size];
            if (m_applicationGroupId == NULL)
            {
                return E_OUTOFMEMORY;
            }
            wcscpy_s(m_applicationGroupId, size, lpApplicationGroupId);
        }

        DWORD pe = PAL_RegisterForRuntimeStartup(m_processId, m_applicationGroupId, RuntimeStartupHandler, this, &m_unregisterToken);
        if (pe != NO_ERROR)
        {
            return HRESULT_FROM_WIN32(pe);
        }
        return S_OK;
    }

    void Unregister()
    {
        PAL_UnregisterForRuntimeStartup(m_unregisterToken);
    }

    void InvokeStartupCallback(char *pszModulePath, HMODULE hModule)
    {
        IUnknown *pCordb = NULL;
        HMODULE hMod = NULL;
        HRESULT hr = S_OK;

        // If either of these are NULL, there was an error from the PAL
        // callback. GetLastError returns the error code from the PAL.
        if (pszModulePath == NULL || hModule == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto exit;
        }

        PAL_CPP_TRY
        {
            char dbiPath[MAX_LONGPATH];

            char *pszLast = strrchr(pszModulePath, DIRECTORY_SEPARATOR_CHAR_A);
            if (pszLast == NULL)
            {
                _ASSERT(!"InvokeStartupCallback: can find separator in coreclr path\n");
                hr = E_INVALIDARG;
                goto exit;
            }

            strncpy_s(dbiPath, ARRAY_SIZE(dbiPath), pszModulePath, pszLast - pszModulePath);
            strcat_s(dbiPath, ARRAY_SIZE(dbiPath), DIRECTORY_SEPARATOR_STR_A MAKEDLLNAME_A("mscordbi"));

            hMod = LoadLibraryA(dbiPath);
            if (hMod == NULL)
            {
                hr = CORDBG_E_DEBUG_COMPONENT_MISSING;
                goto exit;
            }

            HRESULT hr = CreateCoreDbg(hModule, hMod, m_processId, m_applicationGroupId, CorDebugVersion_2_0, &pCordb);
            _ASSERTE((pCordb == NULL) == FAILED(hr));
            if (FAILED(hr))
            {
                goto exit;
            }

            m_callback(pCordb, m_parameter, S_OK);
        }
        PAL_CPP_CATCH_ALL
        {
            hr = E_FAIL;
            goto exit;
        }
        PAL_CPP_ENDTRY

    exit:
        if (FAILED(hr))
        {
            _ASSERTE(pCordb == NULL);

            if (hMod != NULL)
            {
                FreeLibrary(hMod);
            }

            // Invoke the callback on error
            m_callback(NULL, m_parameter, hr);
        }
    }

#else // TARGET_UNIX

    HRESULT Register(LPCWSTR lpApplicationGroupId)
    {
        HRESULT hr = GetStartupNotificationEvent(m_processId, &m_startupEvent);
        if (FAILED(hr))
        {
            goto exit;
        }

        // Add a reference for the thread handler
        AddRef();

        m_threadHandle = CreateThread(
            NULL,
            0,
            ::StartupHelperThread,
            this,
            0,
            &m_threadId);

        if (m_threadHandle == NULL)
        {
            hr = E_OUTOFMEMORY;
            Release();
            goto exit;
        }

    exit:
        return hr;
    }

    bool AreAllHandlesValid(HANDLE *handleArray, DWORD arrayLength)
    {
        for (int i = 0; i < (int)arrayLength; i++)
        {
            HANDLE h = handleArray[i];
            if (h == INVALID_HANDLE_VALUE)
            {
                return false;
            }
        }
        return true;
    }

    HRESULT InternalEnumerateCLRs(HANDLE** ppHandleArray, _In_reads_(*pdwArrayLength) LPWSTR** ppStringArray, DWORD* pdwArrayLength)
    {
        int numTries = 0;
        HRESULT hr;

        while (numTries < 25)
        {
            hr = EnumerateCLRs(m_processId, ppHandleArray, ppStringArray, pdwArrayLength);

            // EnumerateCLRs uses the OS API CreateToolhelp32Snapshot which can return ERROR_BAD_LENGTH or
            // ERROR_PARTIAL_COPY. If we get either of those, we try wait 1/10th of a second try again (that
            // is the recommendation of the OS API owners).
            if ((hr != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) && (hr != HRESULT_FROM_WIN32(ERROR_BAD_LENGTH)))
            {
                // Just return any other error or if no handles were found (which means the coreclr module wasn't found yet).
                if (FAILED(hr) || *ppHandleArray == NULL || *pdwArrayLength <= 0)
                {
                    return hr;
                }
                // If EnumerateCLRs succeeded but any of the handles are INVALID_HANDLE_VALUE, then sleep and retry
                // also. This fixes a race condition where dbgshim catches the coreclr module just being loaded but
                // before g_hContinueStartupEvent has been initialized.
                if (AreAllHandlesValid(*ppHandleArray, *pdwArrayLength))
                {
                    return hr;
                }
                // Clean up memory allocated in EnumerateCLRs since this path it succeeded
                CloseCLREnumeration(*ppHandleArray, *ppStringArray, *pdwArrayLength);

                *ppHandleArray = NULL;
                *ppStringArray = NULL;
                *pdwArrayLength = 0;
            }

            // Sleep and retry enumerating the runtimes
            Sleep(100);
            numTries++;

            if (m_canceled)
            {
                break;
            }
        }

        // Indicate a timeout
        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);

        return hr;
    }

    void WakeRuntimes(HANDLE *handleArray, DWORD arrayLength)
    {
        if (handleArray != NULL)
        {
            for (int i = 0; i < (int)arrayLength; i++)
            {
                HANDLE h = handleArray[i];
                if (h != NULL && h != INVALID_HANDLE_VALUE)
                {
                    SetEvent(h);
                }
            }
        }
    }

    void Unregister()
    {
        m_canceled = true;

        HANDLE *handleArray = NULL;
        LPWSTR *stringArray = NULL;
        DWORD arrayLength = 0;

        // Wake up runtime(s)
        HRESULT hr = EnumerateCLRs(m_processId, &handleArray, &stringArray, &arrayLength);
        if (SUCCEEDED(hr))
        {
            WakeRuntimes(handleArray, arrayLength);
            CloseCLREnumeration(handleArray, stringArray, arrayLength);
        }

        // Wake up worker thread
        SetEvent(m_startupEvent);

        // Don't need to wake up and wait for the worker thread if called on it
        if (m_threadId != GetCurrentThreadId())
        {
            // Wait for work thread to exit
            WaitForSingleObject(m_threadHandle, INFINITE);
        }
    }

    HRESULT InvokeStartupCallback(bool *pCoreClrExists)
    {
        HANDLE *handleArray = NULL;
        LPWSTR *stringArray = NULL;
        DWORD arrayLength = 0;
        HRESULT hr = S_OK;

        PAL_CPP_TRY
        {
            IUnknown *pCordb = NULL;
            WCHAR verStr[MAX_LONGPATH];
            DWORD verLen;

            *pCoreClrExists = FALSE;

            hr = InternalEnumerateCLRs(&handleArray, &stringArray, &arrayLength);
            if (FAILED(hr))
            {
                goto exit;
            }

            for (int i = 0; i < (int)arrayLength; i++)
            {
                *pCoreClrExists = TRUE;

                hr = CreateVersionStringFromModule(m_processId, stringArray[i], verStr, ARRAY_SIZE(verStr), &verLen);
                if (FAILED(hr))
                {
                    goto exit;
                }

                hr = CreateDebuggingInterfaceFromVersion(verStr, &pCordb);
                if (FAILED(hr))
                {
                    goto exit;
                }

                m_callback(pCordb, m_parameter, S_OK);

                // Currently only the first coreclr module in a process is supported
                break;
            }
        }
        PAL_CPP_CATCH_ALL
        {
            hr = E_FAIL;
            goto exit;
        }
        PAL_CPP_ENDTRY

    exit:
        if (*pCoreClrExists)
        {
            // Wake up all the runtimes
            WakeRuntimes(handleArray, arrayLength);
        }
        CloseCLREnumeration(handleArray, stringArray, arrayLength);

        return hr;
    }

    void StartupHelperThread()
    {
        bool coreclrExists = false;

        HRESULT hr = InvokeStartupCallback(&coreclrExists);
        // The retry logic in InternalEnumerateCLRs failed if ERROR_TIMEOUT was returned.
        if (SUCCEEDED(hr) || (hr == HRESULT_FROM_WIN32(ERROR_TIMEOUT)))
        {
            if (!coreclrExists && !m_canceled)
            {
                // Wait until the coreclr runtime (debuggee) starts up
                if (WaitForSingleObject(m_startupEvent, INFINITE) == WAIT_OBJECT_0)
                {
                    if (!m_canceled)
                    {
                        hr = InvokeStartupCallback(&coreclrExists);
                        if (SUCCEEDED(hr))
                        {
                            // We should always find a coreclr module so fail if we don't
                            if (!coreclrExists)
                            {
                                hr = E_FAIL;
                            }
                        }
                    }
                }
                else
                {
                    hr = HRESULT_FROM_WIN32(GetLastError());
                }
            }
        }

        if (FAILED(hr) && !m_canceled)
        {
            m_callback(NULL, m_parameter, hr);
        }
    }

#endif // TARGET_UNIX
};

#ifdef TARGET_UNIX

static
void
RuntimeStartupHandler(char *pszModulePath, HMODULE hModule, PVOID parameter)
{
    RuntimeStartupHelper *helper = (RuntimeStartupHelper *)parameter;
    helper->InvokeStartupCallback(pszModulePath, hModule);
}

#else // TARGET_UNIX

static
DWORD
StartupHelperThread(LPVOID p)
{
    RuntimeStartupHelper *helper = (RuntimeStartupHelper *)p;
    helper->StartupHelperThread();
    helper->Release();
    return 0;
}

#endif // TARGET_UNIX

//-----------------------------------------------------------------------------
// Public API.
//
// RegisterForRuntimeStartup -- Refer to RegisterForRuntimeStartupEx.
//      This method calls RegisterForRuntimeStartupEx with null application group ID value
//
// dwProcessId -- process id of the target process
// pfnCallback -- invoked when coreclr runtime starts
// parameter -- data to pass to callback
// ppUnregisterToken -- pointer to put the UnregisterForRuntimeStartup token.
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
RegisterForRuntimeStartup(
    _In_ DWORD dwProcessId,
    _In_ PSTARTUP_CALLBACK pfnCallback,
    _In_ PVOID parameter,
    _Out_ PVOID *ppUnregisterToken)
{
    return RegisterForRuntimeStartupEx(dwProcessId, NULL, pfnCallback, parameter, ppUnregisterToken);
}
//-----------------------------------------------------------------------------
// Public API.
//
// RegisterForRuntimeStartupEx -- executes the callback when the coreclr runtime
//      starts in the specified process. The callback is passed the proper ICorDebug
//      instance for the version of the runtime or an error if something fails. This
//      API works for launch and attach (and even the attach scenario if the runtime
//      hasn't been loaded yet) equally on both xplat and Windows. The callback is
//      always called on a separate thread. This API returns immediately.
//
//      The callback is invoked when the coreclr runtime module is loaded during early
//      initialization. The runtime is blocked during initialization until the callback
//      returns.
//
//      If the runtime is already loaded in the process (as in the normal attach case),
//      the callback is executed and the runtime is not blocked.
//
//      The callback is always invoked on a separate thread and this API returns immediately.
//
//      Only the first coreclr module instance found in the target process is currently
//      supported.
//
// dwProcessId -- process id of the target process
// lpApplicationGroupId - A string representing the application group ID of a sandboxed
//                        process running in Mac. Pass NULL if the process is not
//                        running in a sandbox and other platforms.
// pfnCallback -- invoked when coreclr runtime starts
// parameter -- data to pass to callback
// ppUnregisterToken -- pointer to put the UnregisterForRuntimeStartup token.
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
RegisterForRuntimeStartupEx(
    _In_ DWORD dwProcessId,
    _In_ LPCWSTR lpApplicationGroupId,
    _In_ PSTARTUP_CALLBACK pfnCallback,
    _In_ PVOID parameter,
    _Out_ PVOID *ppUnregisterToken)
{
    PUBLIC_CONTRACT;

    if (pfnCallback == NULL || ppUnregisterToken == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    RuntimeStartupHelper *helper = new (nothrow) RuntimeStartupHelper(dwProcessId, pfnCallback, parameter);
    if (helper == NULL)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        hr = helper->Register(lpApplicationGroupId);
        if (FAILED(hr))
        {
            helper->Release();
            helper = NULL;
        }
    }

    *ppUnregisterToken = helper;
    return hr;
}

//-----------------------------------------------------------------------------
// Public API.
//
// UnregisterForRuntimeStartup -- stops/cancels runtime startup notification. Needs
//      to be called during the debugger's shutdown to cleanup the internal data.
//
//    This API can be called in the startup callback. Otherwise, it will block until
//    the callback thread finishes and no more callbacks will be initiated after this
//    API returns.
//
// pUnregisterToken -- unregister token from RegisterForRuntimeStartup or NULL.
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
UnregisterForRuntimeStartup(
    _In_ PVOID pUnregisterToken)
{
    PUBLIC_CONTRACT;

    if (pUnregisterToken != NULL)
    {
        RuntimeStartupHelper *helper = (RuntimeStartupHelper *)pUnregisterToken;
        helper->Unregister();
        helper->Release();
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Public API.
//
// GetStartupNotificationEvent -- creates a global, named event that is PID-
//      qualified (i.e. process global) that is used to notify the debugger of
//      any CLR instance startup in the process.
//
// debuggeePID -- process ID of the target process
// phStartupEvent -- out param for the returned event handle
//
//-----------------------------------------------------------------------------
#define StartupNotifyEventNamePrefix W("TelestoStartupEvent_")
#define SessionIdPrefix W("Session\\")

// NULL terminator is included in sizeof(StartupNotifyEventNamePrefix)
const int cchEventNameBufferSize = (sizeof(StartupNotifyEventNamePrefix) + sizeof(SessionIdPrefix)) / sizeof(WCHAR)
                                    + 8  // + hex process id DWORD
                                    + 10 // + decimal session id DWORD
                                    + 1;  // '\' after session id

DLLEXPORT
HRESULT
GetStartupNotificationEvent(
    _In_ DWORD debuggeePID,
    _Out_ HANDLE* phStartupEvent)
{
    PUBLIC_CONTRACT;

    if (phStartupEvent == NULL)
        return E_INVALIDARG;

#ifndef TARGET_UNIX
    HRESULT hr;
    DWORD currentSessionId = 0, debuggeeSessionId = 0;
    if (!ProcessIdToSessionId(GetCurrentProcessId(), &currentSessionId))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (!ProcessIdToSessionId(debuggeePID, &debuggeeSessionId))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Here we could just add "Global\" to the event name and this would solve cross-session debugging scenario, but that would require event name change
    // in CoreCLR, and break backward compatibility. Instead if we see that debugee is in a different session, we explicitly create startup event
    // in that session (by adding "Session\#\"). We could do it even for our own session, but that's vaguely documented behavior and we'd
    // like to use it as little as possible.
    WCHAR szEventName[cchEventNameBufferSize];
    if (currentSessionId == debuggeeSessionId)
    {
        swprintf_s(szEventName, cchEventNameBufferSize, StartupNotifyEventNamePrefix W("%08x"), debuggeePID);
    }
    else
    {
        swprintf_s(szEventName, cchEventNameBufferSize, SessionIdPrefix W("%u\\") StartupNotifyEventNamePrefix W("%08x"), debuggeeSessionId, debuggeePID);
    }

    // Determine an appropriate ACL and SECURITY_ATTRIBUTES to apply to this event.  We use the same logic
    // here as the debugger uses for other events (like the setup-sync-event).  Specifically, this does
    // the work to ensure a debuggee running as another user, or with a low integrity level can signal
    // this event.
    PACL pACL = NULL;
    SECURITY_ATTRIBUTES * pSA = NULL;
    IfFailRet(SecurityUtil::GetACLOfPid(debuggeePID, &pACL));
    SecurityUtil secUtil(pACL);

    HandleHolder hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, debuggeePID);
    if (hProcess == NULL)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    IfFailRet(secUtil.Init(hProcess));
    IfFailRet(secUtil.GetSA(&pSA));

    HANDLE startupEvent = WszCreateEvent(pSA,
                                FALSE,  // false -> auto-reset
                                FALSE,  // false -> initially non-signaled
                                szEventName);
    DWORD dwStatus = GetLastError();
    if (NULL == startupEvent)
    {
        // if the event already exists, try to open it, otherwise we fail.

        if (ERROR_ALREADY_EXISTS != dwStatus)
            return E_FAIL;

        startupEvent = WszOpenEvent(SYNCHRONIZE, FALSE, szEventName);

        if (NULL == startupEvent)
            return E_FAIL;
    }

    *phStartupEvent = startupEvent;
    return S_OK;
#else
    *phStartupEvent = NULL;
    return E_NOTIMPL;
#endif // TARGET_UNIX
}
// Refer to clr\src\mscoree\mscorwks_ntdef.src.
const WORD kOrdinalForMetrics = 2;

//-----------------------------------------------------------------------------
// The CLR_ENGINE_METRICS is a static struct in coreclr.dll.  It's exported by coreclr.dll at ordinal 2 in
// the export address table.  This function returns the CLR_ENGINE_METRICS and the RVA to the continue
// startup event for a coreclr.dll specified by its full path.
//
// Arguments:
//   szTelestoFullPath - (in) full path of telesto
//   pEngineMetricsOut - (out) filled in based on metrics from target telesto.
//   pdwRVAContinueStartupEvent - (out; optional) return the RVA to the continue startup event
//
// Returns:
//   Throwss on error.
//
// Notes:
//     When VS pops up the attach dialog box, it is actually enumerating all the processes on the machine
//     (if the appropiate checkbox is checked) and checking each process to see if a DLL named "coreclr.dll"
//     is loaded.  If there is one, we will go down this code path, but there is no guarantee that the
//     coreclr.dll is ours.  A malicious user can be running a process with a bogus coreclr.dll loaded.
//     That's why we need to be extra careful reading coreclr.dll in this function.
//-----------------------------------------------------------------------------
static
void
GetTargetCLRMetrics(
    LPCWSTR szTelestoFullPath,
    CLR_ENGINE_METRICS *pEngineMetricsOut,
    DWORD *pdwRVAContinueStartupEvent = NULL)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    CONSISTENCY_CHECK(szTelestoFullPath != NULL);
    CONSISTENCY_CHECK(pEngineMetricsOut != NULL);

#ifndef TARGET_UNIX
    HRESULT hr = S_OK;

    HandleHolder hCoreClrFile = WszCreateFile(szTelestoFullPath,
                                              GENERIC_READ,
                                              FILE_SHARE_READ,
                                              NULL,                 // default security descriptor
                                              OPEN_EXISTING,
                                              FILE_ATTRIBUTE_NORMAL,
                                              NULL);
    if (hCoreClrFile == INVALID_HANDLE_VALUE)
    {
        ThrowLastError();
    }

    DWORD cbFileHigh = 0;
    DWORD cbFileLow = GetFileSize(hCoreClrFile, &cbFileHigh);
    if (cbFileLow == INVALID_FILE_SIZE)
    {
        ThrowLastError();
    }

    // A maximum size of 100 MB should be more than enough for coreclr.dll.
    if ((cbFileHigh != 0) || (cbFileLow > 0x6400000) || (cbFileLow == 0))
    {
        ThrowHR(E_FAIL);
    }

    HandleHolder hCoreClrMap = WszCreateFileMapping(hCoreClrFile, NULL, PAGE_READONLY, cbFileHigh, cbFileLow, NULL);
    if (hCoreClrMap == NULL)
    {
        ThrowLastError();
    }

    MapViewHolder hCoreClrMapView = MapViewOfFile(hCoreClrMap, FILE_MAP_READ, 0, 0, 0);
    if (hCoreClrMapView == NULL)
    {
        ThrowLastError();
    }

    // At this point we have read the file into the process, but be careful because it is flat, i.e. not mapped.
    // We need to translate RVAs into file offsets, but fortunately PEDecoder can do all of that for us.
    PEDecoder pedecoder(hCoreClrMapView, (COUNT_T)cbFileLow);

    // Check the NT headers.
    if (!pedecoder.CheckNTFormat())
    {
        ThrowHR(E_FAIL);
    }

    // At this point we can safely read anything in the NT headers.

    if (!pedecoder.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXPORT) ||
        !pedecoder.CheckDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXPORT))
    {
        ThrowHR(E_FAIL);
    }
    IMAGE_DATA_DIRECTORY * pExportDirectoryEntry = pedecoder.GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXPORT);

    // At this point we can safely read the IMAGE_DATA_DIRECTORY of the export directory.

    if (!pedecoder.CheckDirectory(pExportDirectoryEntry))
    {
        ThrowHR(E_FAIL);
    }
    IMAGE_EXPORT_DIRECTORY * pExportDir =
        reinterpret_cast<IMAGE_EXPORT_DIRECTORY *>(pedecoder.GetDirectoryData(pExportDirectoryEntry));

    // At this point we have checked that everything in the export directory is readable.

    // Check to make sure the ordinal we have fits in the table in the export directory.
    // The "base" here is like the starting index of the arrays in the export directory.
    if ((pExportDir->Base > kOrdinalForMetrics) ||
        (pExportDir->NumberOfFunctions < (kOrdinalForMetrics - pExportDir->Base)))
    {
        ThrowHR(E_FAIL);
    }
    DWORD dwRealIndex = kOrdinalForMetrics - pExportDir->Base;

    // Check that we can read the RVA at the element (specified by the ordinal) in the export address table.
    // Then read the RVA to the CLR_ENGINE_METRICS.
    if (!pedecoder.CheckRva(pExportDir->AddressOfFunctions, (dwRealIndex + 1) * sizeof(DWORD)))
    {
        ThrowHR(E_FAIL);
    }
    DWORD rvaMetrics = *reinterpret_cast<DWORD *>(
        pedecoder.GetRvaData(pExportDir->AddressOfFunctions + dwRealIndex * sizeof(DWORD)));

    // Make sure we can safely read the CLR_ENGINE_METRICS at the RVA we have retrieved.
    if (!pedecoder.CheckRva(rvaMetrics, sizeof(*pEngineMetricsOut)))
    {
        ThrowHR(E_FAIL);
    }

    // Finally, copy the CLR_ENGINE_METRICS into the output buffer.
    CLR_ENGINE_METRICS * pMetricsInFile = reinterpret_cast<CLR_ENGINE_METRICS *>(pedecoder.GetRvaData(rvaMetrics));
    *pEngineMetricsOut = *pMetricsInFile;

    // At this point, we have retrieved the CLR_ENGINE_METRICS from the target process and
    // stored it in output buffer.
    if (pEngineMetricsOut->cbSize != sizeof(*pEngineMetricsOut))
    {
        ThrowHR(E_INVALIDARG);
    }

    if (pdwRVAContinueStartupEvent != NULL)
    {
        // Note that the pointer stored in the CLR_ENGINE_METRICS is assuming that the DLL is loaded at its
        // preferred base address.  We need to translate that to an RVA.
        if (((SIZE_T)pEngineMetricsOut->phContinueStartupEvent < (SIZE_T)pedecoder.GetPreferredBase()) ||
            ((SIZE_T)pEngineMetricsOut->phContinueStartupEvent >
                ((SIZE_T)pedecoder.GetPreferredBase() + pedecoder.GetVirtualSize())))
        {
            ThrowHR(E_FAIL);
        }

        DWORD rvaContinueStartupEvent =
            (DWORD)((SIZE_T)pEngineMetricsOut->phContinueStartupEvent - (SIZE_T)pedecoder.GetPreferredBase());

        // We can't use CheckRva() here because for unmapped files it actually checks the RVA against the file
        // size as well.  We have already checked the RVA above.  Now just check that the entire HANDLE
        // falls in the loaded image.
        if ((rvaContinueStartupEvent + sizeof(HANDLE)) > pedecoder.GetVirtualSize())
        {
            ThrowHR(E_FAIL);
        }

        *pdwRVAContinueStartupEvent = rvaContinueStartupEvent;
    }

    // Holder will call FreeLibrary()
#else
    //TODO: So far on POSIX systems we only support one version of debugging interface
    // in future we might want to detect it the same way we do it on Windows.
    pEngineMetricsOut->cbSize = sizeof(*pEngineMetricsOut);
    pEngineMetricsOut->dwDbiVersion = CorDebugLatestVersion;
    pEngineMetricsOut->phContinueStartupEvent = NULL;

    if (pdwRVAContinueStartupEvent != NULL)
    {
        *pdwRVAContinueStartupEvent = NULL;
    }
#endif // TARGET_UNIX
}

// Returns true iff the module represents CoreClr.
static
bool
IsCoreClr(
    const WCHAR* pModulePath)
{
    _ASSERTE(pModulePath != NULL);

    //strip off everything up to and including the last slash in the path to get name
    const WCHAR* pModuleName = pModulePath;
    while(wcschr(pModuleName, DIRECTORY_SEPARATOR_CHAR_W) != NULL)
    {
        pModuleName = wcschr(pModuleName, DIRECTORY_SEPARATOR_CHAR_W);
        pModuleName++; // pass the slash
    }

    // MAIN_CLR_MODULE_NAME_W gets changed for desktop builds, so we directly code against the CoreClr name.
    return _wcsicmp(pModuleName, MAKEDLLNAME_W(W("coreclr"))) == 0;
}

// Returns true iff the module sent is named CoreClr.dll and has the metrics expected in it's PE header.
static
bool
IsCoreClrWithGoodHeader(
    HANDLE hProcess,
    HMODULE hModule)
{
    HRESULT hr = S_OK;

    WCHAR modulePath[MAX_LONGPATH];
    modulePath[0] = W('\0');
    if(0 == GetModuleFileNameEx(hProcess, hModule, modulePath, MAX_LONGPATH))
    {
        return false;
    }
    else
    {
        modulePath[MAX_LONGPATH-1] = 0; // on older OS'es this doesn't get null terminated automatically on truncation
    }

    if (IsCoreClr(modulePath))
    {
        // We don't care about the particular error returned, only that
        // what we tried wasn't a 'real' coreclr.dll.
        EX_TRY
        {
            CLR_ENGINE_METRICS metricsStruct;
            GetTargetCLRMetrics(modulePath, &metricsStruct); // throws

            // If we got this far, then we think it's a good one.
        }
        EX_CATCH_HRESULT(hr);
        return (hr == S_OK);
    }

    return false;
}

static
HRESULT
EnumProcessModulesInternal(
    HANDLE hProcess,
    DWORD *pCountModules,
    HMODULE** ppModules)
{
    *pCountModules = 0;
    *ppModules = nullptr;

    // Start with 1024 modules
    DWORD cbNeeded = sizeof(HMODULE) * 1024;

    ArrayHolder<HMODULE> modules = new (nothrow) HMODULE[cbNeeded / sizeof(HMODULE)];
    if (modules == nullptr)
    {
        return HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    if(!EnumProcessModules(hProcess, modules, cbNeeded, &cbNeeded))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // If 1024 isn't enough, try the modules array size returned (cbNeeded)
    if (cbNeeded > (sizeof(HMODULE) * 1024))
    {
        modules = new (nothrow) HMODULE[cbNeeded / sizeof(HMODULE)];
        if (modules == nullptr)
        {
            return HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
        }

        DWORD cbNeeded2;
        if(!EnumProcessModules(hProcess, modules, cbNeeded, &cbNeeded2))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        // The only way cbNeeded2 could change on the second call is if number of
        // modules loaded by the process changed in the small window between the
        // above EnumProcessModules calls. If this actually happens, then give
        // up on trying to get the whole module list and risk missing the coreclr
        // module.
        cbNeeded = min(cbNeeded, cbNeeded2);
    }

    *pCountModules = cbNeeded / sizeof(HMODULE);
    *ppModules = modules.Detach();
    return S_OK;
}

//-----------------------------------------------------------------------------
// Public API.
//
// EnumerateCLRs -- returns an array of full paths to each coreclr.dll in the
//      target process.  Also returns a corresponding array of continue events
//      that *MUST* be signaled by the caller in order to allow the CLRs in the
//      target process to proceed.
//
// debuggeePID -- process ID of the target process
// ppHandleArrayOut -- out parameter in which an array of handles is returned.
//      the length of this array is returned by the pdwArrayLengthOut out param
// ppStringArrayOut -- out parameter in which an array of full paths to each
//      coreclr.dll in the process is returned.  The length of this array is the
//      same as the handle array and is returned by the pdwArrayLengthOut param
// pdwArrayLengthOut -- out param in which the length of the two returned arrays
//      are returned.
//
// Notes:
//   Callers use  code:CloseCLREnumeration to free the returned arrays.
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
EnumerateCLRs(
    DWORD debuggeePID,
    _Out_ HANDLE** ppHandleArrayOut,
    _Out_ LPWSTR** ppStringArrayOut,
    _Out_ DWORD* pdwArrayLengthOut)
{
    PUBLIC_CONTRACT;

    // All out params must be non-NULL.
    if ((ppHandleArrayOut == NULL) || (ppStringArrayOut == NULL) || (pdwArrayLengthOut == NULL))
        return E_INVALIDARG;

    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, debuggeePID);
    if (NULL == hProcess)
        return E_FAIL;

    // The modules in the array returned don't need to be closed
    DWORD countModules;
    ArrayHolder<HMODULE> modules = nullptr;
    HRESULT hr = EnumProcessModulesInternal(hProcess, &countModules, &modules);
    if (FAILED(hr))
    {
        return hr;
    }

    //
    // count the number of coreclr.dll entries
    //
    DWORD count = 0;
    for(DWORD i = 0; i < countModules; i++)
    {
        if (IsCoreClrWithGoodHeader(hProcess, modules[i]))
        {
            count++;
        }
    }

    // If we didn't find anything, no point in continuing.
    if (count == 0)
    {
        *ppHandleArrayOut = NULL;
        *ppStringArrayOut = NULL;
        *pdwArrayLengthOut = 0;

        return S_OK;
    }

    size_t cbEventArrayData     = sizeof(HANDLE) * count;               // event array data
    size_t cbStringArrayData    = sizeof(LPWSTR) * count;               // string array data
    size_t cbStringData         = sizeof(WCHAR)  * count * MAX_LONGPATH;    // strings data
    size_t cbBuffer             = cbEventArrayData + cbStringArrayData + cbStringData;

    BYTE* pOutBuffer = new (nothrow) BYTE[cbBuffer];
    if (NULL == pOutBuffer)
        return E_OUTOFMEMORY;

    ZeroMemory(pOutBuffer, cbBuffer);

    HANDLE* pEventArray     = (HANDLE*) &pOutBuffer[0];
    LPWSTR* pStringArray    = (LPWSTR*) &pOutBuffer[cbEventArrayData];
    WCHAR*  pStringData     = (WCHAR*)  &pOutBuffer[cbEventArrayData + cbStringArrayData];
    DWORD idx = 0;

    // There's no guarantee that another coreclr hasn't loaded already anyhow,
    // so if we get the corner case that the second time through we enumerate
    // more coreclrs, just ignore the extras.
    // This mismatch could happen when
    // a) take module shapshot
    // b) underlying file is opened for exclusive access/deleted/moved/ACL'd etc so we can't open it
    // c) count is determined
    // d) file is closed/copied/moved/ACL'd etc so we can find/open it again
    // e) this loop runs
    // Thus the loop checks idx < count

    for(DWORD i = 0; i < countModules && idx < count; i++)
    {
        if (IsCoreClrWithGoodHeader(hProcess, modules[i]))
        {
            // fill in path
            pStringArray[idx] = &pStringData[idx * MAX_LONGPATH];
            GetModuleFileNameEx(hProcess, modules[i], pStringArray[idx], MAX_LONGPATH);

#ifndef TARGET_UNIX
            // fill in event handle -- if GetContinueStartupEvent fails, it will still return
            // INVALID_HANDLE_VALUE in hContinueStartupEvent, which is what we want.  we don't
            // want to bail out of the enumeration altogether if we can't get an event from
            // one telesto.

            HANDLE hContinueStartupEvent = INVALID_HANDLE_VALUE;
            HRESULT hr = GetContinueStartupEvent(debuggeePID, pStringArray[idx], &hContinueStartupEvent);
            pEventArray[idx] = hContinueStartupEvent;
#else
            pEventArray[idx] = NULL;
#endif // TARGET_UNIX

            idx++;
        }
    }

    // Patch things up so CloseCLREnumeration() can still have it's
    // pointer arithmatic checks succeed, and the user doesn't see a 'dead' entry.
    // Specifically, it's expected that pEventArray and pStringArray point to the
    // same contiguous chunk of memory so that pStringArray == pEventArray[*pdwArrayLengthOut].
    // This is expected to be a very rare case.
    if (idx < count)
    {
        // Move the string pointers back.
        LPWSTR* pSATemp = (LPWSTR*)&pOutBuffer[sizeof(HANDLE)*idx];
        for (DWORD i = 0; i < idx; i++)
        {
            pSATemp[i] = pStringArray[i];
        }

        // Fix up string array pointer.
        pStringArray = (LPWSTR*)&pOutBuffer[sizeof(HANDLE)*idx];

        // Strings themselves don't need moved.
    }

    *ppHandleArrayOut = pEventArray;
    *ppStringArrayOut = pStringArray;
    *pdwArrayLengthOut = idx;

    return S_OK;
}

//-----------------------------------------------------------------------------
// Public API.
//
// CloseCLREnumeration -- used to free resources allocated by EnumerateCLRs
//
// pHandleArray -- handle array originally returned by EnumerateCLRs
// pStringArray -- string array originally returned by EnumerateCLRs
// dwArrayLength -- array length originally returned by EnumerateCLRs
//
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CloseCLREnumeration(
    _In_ HANDLE* pHandleArray,
    _In_ LPWSTR* pStringArray,
    _In_ DWORD dwArrayLength)
{
    PUBLIC_CONTRACT;

    // It's possible that EnumerateCLRs found nothing to enumerate, in which case
    // pointers and count are zeroed.  If a debugger calls this function in that
    // case, let's not try to delete [] on NULL.
    if (pHandleArray == NULL)
        return S_OK;

    if ((pHandleArray + dwArrayLength) != (HANDLE*)pStringArray)
        return E_INVALIDARG;

#ifndef TARGET_UNIX
    for (DWORD i = 0; i < dwArrayLength; i++)
    {
        HANDLE hTemp = pHandleArray[i];
        if (   (NULL != hTemp)
            && (INVALID_HANDLE_VALUE != hTemp))
        {
            CloseHandle(hTemp);
        }
    }
#endif // TARGET_UNIX

    delete[] pHandleArray;
    return S_OK;
}

//-----------------------------------------------------------------------------
// Get the base address of a module from the remote process.
//
// Returns:
//  - On success, base address (in remote process) of mscoree,
//  - NULL  if the module is not loaded.
//  - else Throws. *ppBaseAddress = NULL
//-----------------------------------------------------------------------------
static
BYTE*
GetRemoteModuleBaseAddress(
    DWORD dwPID,
    LPCWSTR szFullModulePath)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
    if (NULL == hProcess)
    {
        ThrowHR(E_FAIL);
    }

    // The modules in the array returned don't need to be closed
    DWORD countModules;
    ArrayHolder<HMODULE> modules = nullptr;
    HRESULT hr = EnumProcessModulesInternal(hProcess, &countModules, &modules);
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }

    for(DWORD i = 0; i < countModules; i++)
    {
        WCHAR modulePath[MAX_LONGPATH];
        if(0 == GetModuleFileNameEx(hProcess, modules[i], modulePath, MAX_LONGPATH))
        {
            continue;
        }
        else
        {
            modulePath[MAX_LONGPATH-1] = 0; // on older OS'es this doesn't get null terminated automatically
            if (_wcsicmp(modulePath, szFullModulePath) == 0)
            {
                return (BYTE*) modules[i];
            }
        }
    }

    // Successfully enumerated modules but couldn't find the requested one.
    return NULL;
}

// DBI version: max 8 hex chars
// SEMICOLON: 1
// PID: max 8 hex chars
// SEMICOLON: 1
// HMODULE: max 16 hex chars (64-bit)
// SEMICOLON: 1
// PROTOCOL STRING: (variable length)
const int c_iMaxVersionStringLen = 8 + 1 + 8 + 1 + 16; // 64-bit hmodule
const int c_iMinVersionStringLen = 8 + 1 + 8 + 1 + 8; // 32-bit hmodule
const int c_idxFirstSemi = 8;
const int c_idxSecondSemi = 17;
const WCHAR *c_versionStrFormat = W("%08x;%08x;%p");

//-----------------------------------------------------------------------------
// Public API.
// Given a path to a coreclr.dll, get the Version string.
//
// Arguments:
//   pidDebuggee - OS process ID of debuggee.
//   szModuleName - a full or relative path to a valid coreclr.dll in the debuggee.
//   pBuffer - the buffer to fill the version string into
//     if pdwLength != NULL, we set *pdwLength to the length of the version string on
//     output (including the null terminator).
//   cchBuffer - length of pBuffer on input in characters
//
// Returns:
//  S_OK - on success.
//  E_INVALIDARG -
//  HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) if the buffer is too small.
//  COR_E_FILENOTFOUND - module is not found in a given debugee process
//
// Notes:
//   The null-terminated version string including null, is
//   copied to pVersion on output. Thus *pdwLength == wcslen(pBuffer)+1.
//   The version string is an opaque string that can only be passed back to other
//   DbgShim APIs.
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CreateVersionStringFromModule(
    _In_ DWORD pidDebuggee,
    _In_ LPCWSTR szModuleName,
    _Out_writes_to_opt_(cchBuffer, *pdwLength) LPWSTR pBuffer,
    _In_ DWORD cchBuffer,
    _Out_ DWORD* pdwLength)
{
    PUBLIC_CONTRACT;

    if (szModuleName == NULL)
    {
        return E_INVALIDARG;
    }

    // it is ok for both to be null (to query the required buffer size) or both to be non-null.
    if ((pBuffer == NULL) != (cchBuffer == 0))
    {
        return E_INVALIDARG;
    }

    SIZE_T nLengthWithNull = c_iMaxVersionStringLen + 1;
    _ASSERTE(nLengthWithNull > 0);

    if (pdwLength != NULL)
    {
        *pdwLength = (DWORD) nLengthWithNull;
    }

    if (nLengthWithNull > cchBuffer)
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }
    else if (pBuffer != NULL)
    {

        HRESULT hr = S_OK;
        EX_TRY
        {
            CorDebugInterfaceVersion dbiVersion = CorDebugInvalidVersion;
            BYTE* hmodTargetCLR = NULL;
            CLR_ENGINE_METRICS metricsStruct;

            GetTargetCLRMetrics(szModuleName, &metricsStruct); // throws
            dbiVersion = (CorDebugInterfaceVersion) metricsStruct.dwDbiVersion;

            hmodTargetCLR = GetRemoteModuleBaseAddress(pidDebuggee, szModuleName); // throws
            if (hmodTargetCLR == NULL)
            {
                hr = COR_E_FILENOTFOUND;
            }
            else
            {
                swprintf_s(pBuffer, cchBuffer, c_versionStrFormat, dbiVersion, pidDebuggee, hmodTargetCLR);
            }
        }
        EX_CATCH_HRESULT(hr);
        return hr;
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Parse a version string into useful data.
//
// Arguments:
//    szDebuggeeVersion - (in) null terminated version string
//    piDebuggerVersion - (out) interface number that the debugger expects to use.
//    pdwPidDebuggee    - (out) OS process ID of debuggee
//    phmodTargetCLR    - (out) module handle of CoreClr within the debuggee.
//
// Returns:
//    S_OK on success. Else failures.
//
// Notes:
//    The version string is coming from the target CoreClr and in the case of a corrupted target, could be
//    an arbitrary string. It should be treated as untrusted public input.
//-----------------------------------------------------------------------------
static
HRESULT
ParseVersionString(
    LPCWSTR szDebuggeeVersion,
    CorDebugInterfaceVersion *piDebuggerVersion,
    DWORD *pdwPidDebuggee,
    HMODULE *phmodTargetCLR)
{
    if ((piDebuggerVersion == NULL) ||
        (pdwPidDebuggee == NULL) ||
        (phmodTargetCLR == NULL) ||
        (wcslen(szDebuggeeVersion) < c_iMinVersionStringLen) ||
        (W(';') != szDebuggeeVersion[c_idxFirstSemi]) ||
        (W(';') != szDebuggeeVersion[c_idxSecondSemi]))
    {
        return E_INVALIDARG;
    }

    int numFieldsAssigned = swscanf_s(szDebuggeeVersion, c_versionStrFormat, piDebuggerVersion, pdwPidDebuggee, phmodTargetCLR);
    if (numFieldsAssigned != 3)
    {
        return E_FAIL;
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Appends "\mscordbi.dll" to the path. This converts a directory name into the full path to mscordbi.dll.
//
// Arguments:
//    szFullDbiPath - (in/out): on input, the directory containing dbi. On output, the full path to dbi.dll.
//-----------------------------------------------------------------------------
static
void
AppendDbiDllName(SString & szFullDbiPath)
{
    const WCHAR * pDbiDllName = DIRECTORY_SEPARATOR_STR_W MAKEDLLNAME_W(W("mscordbi"));
    szFullDbiPath.Append(pDbiDllName);
}

//-----------------------------------------------------------------------------
// Return a path to the dbi next to the runtime, if present.
//
// Arguments:
//    pidDebuggee - OS process ID of debuggee
//    hmodTargetCLR - handle to CoreClr within debuggee process
//    szFullDbiPath - (out) the full path of Mscordbi.dll next to the debuggee's CoreClr.dll.
//
// Notes:
//    This just calculates a filename and does not determine if the file actually exists.
//-----------------------------------------------------------------------------
static
void
GetDbiFilenameNextToRuntime(
    DWORD pidDebuggee,
    HMODULE hmodTargetCLR,
    SString & szFullDbiPath,
    SString & szFullCoreClrPath)
{
    szFullDbiPath.Clear();

    //
    // Step 1: (pid, hmodule) --> full path
    //
    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pidDebuggee);
    WCHAR modulePath[MAX_LONGPATH];
    if(0 == GetModuleFileNameEx(hProcess, hmodTargetCLR, modulePath, MAX_LONGPATH))
    {
        ThrowHR(E_FAIL);
    }

    //
    // Step 2: 'Coreclr.dll' --> 'mscordbi.dll'
    //
    WCHAR * pCoreClrPath = modulePath;
    WCHAR * pLast = wcsrchr(pCoreClrPath, DIRECTORY_SEPARATOR_CHAR_W);
    if (pLast == NULL)
    {
        ThrowHR(E_FAIL);
    }

    //   c:\abc\coreclr.dll
    //   01234567890
    //   c:\abc\mscordbi.dll

    // Copy everything up to but not including the last '\', thus excluding '\coreclr.dll'
    // Then append '\mscordbi.dll' to get a full path to dbi.
    COUNT_T len = (COUNT_T) (pLast - pCoreClrPath); // length not including final '\'
    szFullDbiPath.Set(pCoreClrPath, len);

    AppendDbiDllName(szFullDbiPath);

    szFullCoreClrPath.Set(pCoreClrPath, (COUNT_T)wcslen(pCoreClrPath));
}


//---------------------------------------------------------------------------------------
//
// The current policy is that the DBI DLL must live right next to the coreclr DLL.  We check the product
// version number of both of them to make sure they match.
//
// Arguments:
//    szFullDbiPath     - full path to mscordbi.dll
//    szFullCoreClrPath - full path to coreclr.dll
//
// Return Value:
//    true if the versions match
//
static
bool
CheckDbiAndRuntimeVersion(
    SString & szFullDbiPath,
    SString & szFullCoreClrPath)
{
#ifndef TARGET_UNIX
    DWORD dwDbiVersionMS = 0;
    DWORD dwDbiVersionLS = 0;
    DWORD dwCoreClrVersionMS = 0;
    DWORD dwCoreClrVersionLS = 0;

    // The version numbers follow the convention used by VS_FIXEDFILEINFO.
    GetProductVersionNumber(szFullDbiPath, &dwDbiVersionMS, &dwDbiVersionLS);
    GetProductVersionNumber(szFullCoreClrPath, &dwCoreClrVersionMS, &dwCoreClrVersionLS);

    if ((dwDbiVersionMS == dwCoreClrVersionMS) &&
        (dwDbiVersionLS == dwCoreClrVersionLS))
    {
        return true;
    }
    else
    {
        return false;
    }
#else
    return true;
#endif // TARGET_UNIX
}

//-----------------------------------------------------------------------------
// Public API.
// Given a version string, create the matching mscordbi.dll for it.
// Create a managed debugging interface for the specified version.
//
// Parameters:
//    iDebuggerVersion - the version of interface the debugger (eg, Cordbg) expects.
//    szDebuggeeVersion - the version of the debuggee. This will map to a version of mscordbi.dll
//    ppCordb - the outparameter used to return the debugging interface object.
//
// Return:
//  S_OK on success. *ppCordb will be non-null.
//  CORDBG_E_INCOMPATIBLE_PROTOCOL - if the proper DBI is not available. This can be a very common error if
//    the right debug pack is not installed.
//  else Error. (*ppCordb will be null)
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CreateDebuggingInterfaceFromVersionEx(
    _In_ int iDebuggerVersion,
    _In_ LPCWSTR szDebuggeeVersion,
    _Out_ IUnknown ** ppCordb)
{
    return CreateDebuggingInterfaceFromVersion2(iDebuggerVersion, szDebuggeeVersion, NULL, ppCordb);
}

//-----------------------------------------------------------------------------
// Public API.
// Given a version string, create the matching mscordbi.dll for it.
// Create a managed debugging interface for the specified version.
//
// Parameters:
//    iDebuggerVersion - the version of interface the debugger (eg, Cordbg) expects.
//    szDebuggeeVersion - the version of the debuggee. This will map to a version of mscordbi.dll
//    lpApplicationGroupId - A string representing the application group ID of a sandboxed
//                           process running in Mac. Pass NULL if the process is not
//                           running in a sandbox and other platforms.
//    ppCordb - the outparameter used to return the debugging interface object.
//
// Return:
//  S_OK on success. *ppCordb will be non-null.
//  CORDBG_E_INCOMPATIBLE_PROTOCOL - if the proper DBI is not available. This can be a very common error if
//    the right debug pack is not installed.
//  else Error. (*ppCordb will be null)
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CreateDebuggingInterfaceFromVersion2(
    _In_ int iDebuggerVersion,
    _In_ LPCWSTR szDebuggeeVersion,
    _In_ LPCWSTR szApplicationGroupId,
    _Out_ IUnknown ** ppCordb)
{
    PUBLIC_CONTRACT;

    HRESULT hrIgnore = S_OK; // ignored HResult
    HRESULT hr = S_OK;
    HMODULE hMod = NULL;
    IUnknown * pCordb = NULL;

    LOG((LF_CORDB, LL_EVERYTHING, "Calling CreateDebuggerInterfaceFromVersion, ver=%S\n", szDebuggeeVersion));

    if ((szDebuggeeVersion == NULL) || (ppCordb == NULL))
    {
        hr = E_INVALIDARG;
        goto Exit;
    }

    //
    // Step 1: Parse version information into internal data structures
    //

    CorDebugInterfaceVersion iTargetVersion;  // the CorDebugInterfaceVersion (CorDebugVersion_2_0)
    DWORD pidDebuggee;     // OS process ID of the debuggee
    HMODULE hmodTargetCLR; // module of Telesto in target (the clrInstanceId)

    hr = ParseVersionString(szDebuggeeVersion, &iTargetVersion, &pidDebuggee, &hmodTargetCLR);
    if (FAILED(hr))
        goto Exit;

    //
    // Step 2:  Find the proper dbi module (mscordbi) and load it.
    //

    // Check for dbi next to target CLR.
    // This will be very common for internal developer setups, but not common in end-user setups.
    EX_TRY
    {
        SString szFullDbiPath;
        SString szFullCoreClrPath;

        GetDbiFilenameNextToRuntime(pidDebuggee, hmodTargetCLR, szFullDbiPath, szFullCoreClrPath);

        if (!CheckDbiAndRuntimeVersion(szFullDbiPath, szFullCoreClrPath))
        {
            hr = CORDBG_E_INCOMPATIBLE_PROTOCOL;
            goto Exit;
        }

        // We calculated where dbi would be, but haven't yet verified if it's there.
        // Try to load it. We're using this to check for file existence.

        // Issue:951525: coreclr mscordbi load fails on downlevel OS since LoadLibraryEx can't find
        // dependent forwarder DLLs. Force LoadLibrary to look for dependencies in szFullDbiPath plus the default
        // search paths.
#ifndef TARGET_UNIX
        hMod = WszLoadLibraryEx(szFullDbiPath, NULL, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
#else
        hMod = LoadLibraryExW(szFullDbiPath, NULL, 0);
#endif
    }
    EX_CATCH_HRESULT(hrIgnore); // failure leaves hMod null

    // Couldn't find Dbi, likely because the right debug pack is not installed. Failure.
    if (NULL == hMod)
    {
        // Check for the following two HRESULTs and return them specifically.  These are returned by
        // CreateToolhelp32Snapshot() and could be transient errors.  The debugger may choose to retry.
        if ((hrIgnore == HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) || (hrIgnore == HRESULT_FROM_WIN32(ERROR_BAD_LENGTH)))
        {
            hr = hrIgnore;
        }
        else
        {
            hr = CORDBG_E_DEBUG_COMPONENT_MISSING;
        }
        goto Exit;
    }

    //
    // Step 3: Now that module is loaded, instantiate an ICorDebug.
    //
    hr = CreateCoreDbg(hmodTargetCLR, hMod, pidDebuggee, szApplicationGroupId, iDebuggerVersion, &pCordb);
    _ASSERTE((pCordb == NULL) == FAILED(hr));

Exit:
    if (FAILED(hr))
    {
        if (pCordb != NULL)
        {
            pCordb->Release();
            pCordb = NULL;
        }

        if (hMod != NULL)
        {
            _ASSERTE(pCordb == NULL);
            FreeLibrary(hMod);
        }
    }

    // Set our outparam.
    *ppCordb = pCordb;

    // On success case, mscordbi.dll is leaked.
    // - We never give the caller back the module handle, so our caller can't do FreeLibrary().
    // - ICorDebug can't unload itself.

    return hr;
}

//-----------------------------------------------------------------------------
// Public API.
// Superceded by CreateDebuggingInterfaceFromVersionEx in SLv4.
// Given a version string, create the matching mscordbi.dll for it.
// Create a managed debugging interface for the specified version.
//
// Parameters:
//    szDebuggeeVersion - the version of the debuggee. This will map to a version of mscordbi.dll
//    ppCordb - the outparameter used to return the debugging interface object.
//
// Return:
//  S_OK on success. *ppCordb will be non-null.
//  CORDBG_E_INCOMPATIBLE_PROTOCOL - if the proper DBI is not available. This can be a very common error if
//    the right debug pack is not installed.
//  else Error. (*ppCordb will be null)
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CreateDebuggingInterfaceFromVersion(
    _In_ LPCWSTR szDebuggeeVersion,
    _Out_ IUnknown ** ppCordb
)
{
    PUBLIC_CONTRACT;

    return CreateDebuggingInterfaceFromVersionEx(CorDebugVersion_2_0, szDebuggeeVersion, ppCordb);
}

#ifndef TARGET_UNIX

//------------------------------------------------------------------------------
// Manually retrieves the "continue startup" event from the correct CLR instance
// in the target process.
//
// Arguments:
//    debuggeePID - (in) OS Process ID of debuggee
//    szTelestoFullPath - (in) full path to telesto within the process.
//    phContinueStartupEvent - (out)
//
// Returns:
//   S_OK on success.
//------------------------------------------------------------------------------
HRESULT
GetContinueStartupEvent(
    DWORD debuggeePID,
    LPCWSTR szTelestoFullPath,
    _Out_ HANDLE* phContinueStartupEvent)
{
    if ((phContinueStartupEvent == NULL) || (szTelestoFullPath == NULL))
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    EX_TRY
    {
        *phContinueStartupEvent = INVALID_HANDLE_VALUE;

        DWORD  dwCoreClrContinueEventOffset = 0;
        CLR_ENGINE_METRICS metricsStruct;

        GetTargetCLRMetrics(szTelestoFullPath, &metricsStruct, &dwCoreClrContinueEventOffset); // throws


        BYTE* pbBaseAddress = GetRemoteModuleBaseAddress(debuggeePID, szTelestoFullPath); // throws


        HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, debuggeePID);
        if (NULL == hProcess)
            ThrowHR(E_FAIL);

        HANDLE continueEvent = NULL;

        SIZE_T nBytesRead;
        if (!ReadProcessMemory(hProcess, pbBaseAddress + dwCoreClrContinueEventOffset, &continueEvent,
            sizeof(continueEvent), &nBytesRead))
        {
            ThrowHR(E_FAIL);
        }

        if (NULL != continueEvent && INVALID_HANDLE_VALUE != continueEvent)
        {
            if (!DuplicateHandle(hProcess, continueEvent, GetCurrentProcess(), &continueEvent,
                EVENT_MODIFY_STATE, FALSE, 0))
            {
                ThrowHR(E_FAIL);
            }
        }

        *phContinueStartupEvent = continueEvent;
    }
    EX_CATCH_HRESULT(hr)
    return hr;
}

#endif // !TARGET_UNIX

#if defined(FEATURE_CORESYSTEM)
#include "debugshim.h"
#endif

//-----------------------------------------------------------------------------
// Public API.
//
// Parameters:
//  clsid
//  riid
//  ppInterface
//
// Return:
//  S_OK on success.
//-----------------------------------------------------------------------------
DLLEXPORT
HRESULT
CLRCreateInstance(
    REFCLSID clsid,
    REFIID riid,
    LPVOID *ppInterface)
{
    PUBLIC_CONTRACT;

#if defined(FEATURE_CORESYSTEM)

    if (ppInterface == NULL)
        return E_POINTER;

    if (clsid != CLSID_CLRDebugging || riid != IID_ICLRDebugging)
        return E_NOINTERFACE;

#if defined(FEATURE_CORESYSTEM)
    GUID skuId = CLR_ID_ONECORE_CLR;
#else
    GUID skuId = CLR_ID_CORECLR;
#endif

    CLRDebuggingImpl *pDebuggingImpl = new (nothrow) CLRDebuggingImpl(skuId);
    if (NULL == pDebuggingImpl)
        return E_OUTOFMEMORY;

    return pDebuggingImpl->QueryInterface(riid, ppInterface);
#else
    return E_NOTIMPL;
#endif
}
