//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
#ifndef FEATURE_PAL
#include <securityutil.h>
#endif

#include <ex.h>
#include <cordebug.h> // for Version nunmbers
#include <pedecoder.h>
#include <getproductversionnumber.h>
#include <dbgenginemetrics.h>

#define PSAPI_VERSION 2
#include <psapi.h>

#include "dbgshim.h"

/*

// Here's a High-level overview of the API usage

From the debugger:
A debugger calls GetStartupNotificationEvent(pid of debuggee) to get an event, which is signalled when that 
process loads a Telesto.  The debugger thus waits on that event, and when it's signalled, it can call
EnumerateCLRs / CloseCLREnumeration to get an array of Telestos in the target process (including the one
that was just loaded). 
It can then call CreateVersionStringFromModule, CreateDebuggingInterfaceFromVersion to attach to 
any or all Telestos of interest.


From the debuggee:
When a new Telesto spins up, it checks for the startup event (created via GetStartupNotificationEvent), and if it
exists, it will:
- signal it
- wait on the "Continue" event, thus giving a debugger a chance to attach to the telesto


Notes:
- There is no CreateProcess (Launch) case. All Launching is really an "Early-attach case".

*/


// Contract for public APIs. These must be NOTHROW.
#define PUBLIC_CONTRACT \
    CONTRACTL \
    { \
        NOTHROW; \
    } \
    CONTRACTL_END; \

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
const int cchEventNameBufferSize = sizeof(StartupNotifyEventNamePrefix)/sizeof(WCHAR) + 8; // + hex DWORD (8).  NULL terminator is included in sizeof(StartupNotifyEventNamePrefix)

HRESULT GetStartupNotificationEvent(DWORD debuggeePID,
                                    __out HANDLE* phStartupEvent)
{
    PUBLIC_CONTRACT;

    if (phStartupEvent == NULL)
        return E_INVALIDARG;

#ifndef FEATURE_PAL
    HRESULT hr;

    // Note this event name doesn't have a Global prefix, and so debugging across sessions will not work.
    WCHAR szEventName[cchEventNameBufferSize];
    swprintf_s(szEventName, cchEventNameBufferSize, StartupNotifyEventNamePrefix W("%08x"), debuggeePID);

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
#else
    *phStartupEvent = NULL;
#endif // FEATURE_PAL

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
HRESULT CloseCLREnumeration(HANDLE* pHandleArray, LPWSTR* pStringArray, DWORD dwArrayLength)
{
    PUBLIC_CONTRACT;

    if ((pHandleArray + dwArrayLength) != (HANDLE*)pStringArray)
        return E_INVALIDARG;

    // It's possible that EnumerateCLRs found nothing to enumerate, in which case
    // pointers and count are zeroed.  If a debugger calls this function in that
    // case, let's not try to delete [] on NULL.
    if (pHandleArray == NULL)
        return S_OK;

#ifndef FEATURE_PAL
    for (DWORD i = 0; i < dwArrayLength; i++)
    {
        HANDLE hTemp = pHandleArray[i];
        if (   (NULL != hTemp)
            && (INVALID_HANDLE_VALUE != hTemp))
        {
            CloseHandle(hTemp);
        }
    }
#endif // FEATURE_PAL

    delete[] pHandleArray;
    return S_OK;
}

#ifndef FEATURE_PAL

HRESULT GetContinueStartupEvent(DWORD debuggeePID, 
                                LPCWSTR szTelestoFullPath,
                                __out HANDLE* phContinueStartupEvent);

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
void GetTargetCLRMetrics(LPCWSTR szTelestoFullPath, 
                         CLR_ENGINE_METRICS * pEngineMetricsOut, 
                         DWORD * pdwRVAContinueStartupEvent = NULL)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    CONSISTENCY_CHECK(szTelestoFullPath != NULL);
    CONSISTENCY_CHECK(pEngineMetricsOut != NULL);

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
}

// Returns true iff the module represents CoreClr.
bool IsCoreClr(const WCHAR* pModulePath)
{
    _ASSERTE(pModulePath != NULL); 

    //strip off everything up to and including the last slash in the path to get name
    const WCHAR* pModuleName = pModulePath;
    while(wcschr(pModuleName, W('\\')) != NULL)
    {
        pModuleName = wcschr(pModuleName, W('\\'));
        pModuleName++; // pass the slash
    }

    // MAIN_CLR_MODULE_NAME_W gets changed for desktop builds, so we directly code against the CoreClr name.
    return _wcsicmp(pModuleName, MAKEDLLNAME_W(W("coreclr"))) == 0;
}

// Returns true iff the module sent is named CoreClr.dll and has the metrics expected in it's PE header.
bool IsCoreClrWithGoodHeader(HANDLE hProcess, HMODULE hModule)
{
    HRESULT hr = S_OK;

    WCHAR modulePath[MAX_PATH];
    modulePath[0] = W('\0');
    if(0 == GetModuleFileNameEx(hProcess, hModule, modulePath, MAX_PATH))
    {
        return false;
    }
    else
    {
        modulePath[MAX_PATH-1] = 0; // on older OS'es this doesn't get null terminated automatically on truncation
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

#endif // !FEATURE_PAL

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
HRESULT EnumerateCLRs(DWORD debuggeePID, 
                      __out HANDLE** ppHandleArrayOut,
                      __out LPWSTR** ppStringArrayOut,
                      __out DWORD* pdwArrayLengthOut)
{
    PUBLIC_CONTRACT;

    // All out params must be non-NULL.
    if ((ppHandleArrayOut == NULL) || (ppStringArrayOut == NULL) || (pdwArrayLengthOut == NULL))
        return E_INVALIDARG;

#ifndef FEATURE_PAL
    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, debuggeePID);
    if (NULL == hProcess)
        ThrowHR(E_FAIL);

    // These shouldn't be freed
    HMODULE modules[1000];
    DWORD cbNeeded;
    if(!EnumProcessModules(hProcess, modules, sizeof(modules), &cbNeeded))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    //
    // count the number of coreclr.dll entries
    //
    DWORD count = 0;
    DWORD countModules = cbNeeded/sizeof(HMODULE);
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
#else
    DWORD count = 1;
#endif // FEATURE_PAL

    size_t cbEventArrayData     = sizeof(HANDLE) * count;               // event array data
    size_t cbStringArrayData    = sizeof(LPWSTR) * count;               // string array data
    size_t cbStringData         = sizeof(WCHAR)  * count * MAX_PATH;    // strings data
    size_t cbBuffer             = cbEventArrayData + cbStringArrayData + cbStringData;

    BYTE* pOutBuffer = new (nothrow) BYTE[cbBuffer];
    if (NULL == pOutBuffer)
        return E_OUTOFMEMORY;

    ZeroMemory(pOutBuffer, cbBuffer);

    HANDLE* pEventArray     = (HANDLE*) &pOutBuffer[0];
    LPWSTR* pStringArray    = (LPWSTR*) &pOutBuffer[cbEventArrayData];
    WCHAR*  pStringData     = (WCHAR*)  &pOutBuffer[cbEventArrayData + cbStringArrayData];
    DWORD idx = 0;

#ifndef FEATURE_PAL
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
            pStringArray[idx] = &pStringData[idx * MAX_PATH];
            GetModuleFileNameEx(hProcess, modules[i], pStringArray[idx], MAX_PATH);

            // fill in event handle -- if GetContinueStartupEvent fails, it will still return 
            // INVALID_HANDLE_VALUE in hContinueStartupEvent, which is what we want.  we don't
            // want to bail out of the enumeration altogether if we can't get an event from
            // one telesto.

            HANDLE hContinueStartupEvent = INVALID_HANDLE_VALUE;
            HRESULT hr = GetContinueStartupEvent(debuggeePID, pStringArray[idx], &hContinueStartupEvent);
            _ASSERTE(SUCCEEDED(hr) == (hContinueStartupEvent != INVALID_HANDLE_VALUE));

            pEventArray[idx] = hContinueStartupEvent;

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
#else 
    pStringArray[idx] = &pStringData[idx * MAX_PATH];
    wcscpy_s(pStringArray[idx], MAX_PATH, MAKEDLLNAME_W(W("coreclr")));
    idx++;
#endif // FEATURE_PAL

    *ppHandleArrayOut = pEventArray;
    *ppStringArrayOut = pStringArray;
    *pdwArrayLengthOut = idx;

    return S_OK;
}

#ifndef FEATURE_PAL

//-----------------------------------------------------------------------------
// Get the base address of a module from the remote process.
//
// Returns:
//  - On success, base address (in remote process) of mscoree, 
//  - NULL  if the module is not loaded.
//  - else Throws. *ppBaseAddress = NULL
//-----------------------------------------------------------------------------
BYTE* GetRemoteModuleBaseAddress(DWORD dwPID, LPCWSTR szFullModulePath)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
    if (NULL == hProcess)
        ThrowHR(E_FAIL);

    // These shouldn't be freed
    HMODULE modules[1000];
    DWORD cbNeeded;
    if(!EnumProcessModules(hProcess, modules, sizeof(modules), &cbNeeded))
    {
        ThrowHR(HRESULT_FROM_WIN32(GetLastError()));
    }

    DWORD countModules = cbNeeded/sizeof(HMODULE);
    for(DWORD i = 0; i < countModules; i++)
    {
        WCHAR modulePath[MAX_PATH];
        if(0 == GetModuleFileNameEx(hProcess, modules[i], modulePath, MAX_PATH))
        {
            continue;
        }
        else
        {
            modulePath[MAX_PATH-1] = 0; // on older OS'es this doesn't get null terminated automatically
            if (_wcsicmp(modulePath, szFullModulePath) == 0)
            {                
                return (BYTE*) modules[i];
            }
        }
    }


    // Successfully enumerated modules but couldn't find the requested one.
    return NULL;
}

#endif // FEATURE_PAL

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
//  
// Notes:
//   The null-terminated version string including null, is 
//   copied to pVersion on output. Thus *pdwLength == wcslen(pBuffer)+1.
//   The version string is an opaque string that can only be passed back to other 
//   DbgShim APIs.
//-----------------------------------------------------------------------------
HRESULT CreateVersionStringFromModule(DWORD pidDebuggee,
                                      LPCWSTR szModuleName,
                                      __out_ecount_part(cchBuffer, *pdwLength) LPWSTR pBuffer,
                                      DWORD cchBuffer,
                                      __out DWORD* pdwLength)
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
#ifndef FEATURE_PAL
        HRESULT hr = S_OK;
        EX_TRY
        {        
            CorDebugInterfaceVersion dbiVersion = CorDebugInvalidVersion;
            DWORD pid = pidDebuggee;

            CLR_ENGINE_METRICS metricsStruct;

            GetTargetCLRMetrics(szModuleName, &metricsStruct); // throws
            dbiVersion = (CorDebugInterfaceVersion) metricsStruct.dwDbiVersion;
            
            BYTE* hmodTargetCLR = GetRemoteModuleBaseAddress(pidDebuggee, szModuleName); // throws

            swprintf_s(pBuffer, cchBuffer, W("%08x;%08x;%p"), dbiVersion, pid, hmodTargetCLR);
        }
        EX_CATCH_HRESULT(hr);
        return hr;
#else
    swprintf_s(pBuffer, cchBuffer, W("%08x"), pidDebuggee);
#endif // FEATURE_PAL
    }

    return S_OK;
}

#ifndef FEATURE_PAL

// Functions that we'll look for in the loaded Mscordbi module.
typedef HRESULT (STDAPICALLTYPE *FPCoreCLRCreateCordbObject)(
    int iDebuggerVersion, 
    DWORD pid, 
    HMODULE hmodTargetCLR, 
    IUnknown ** ppCordb);

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
HRESULT ParseVersionString(LPCWSTR szDebuggeeVersion, CorDebugInterfaceVersion * piDebuggerVersion, DWORD * pdwPidDebuggee, 
                           HMODULE * phmodTargetCLR)
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

    
    int numFieldsAssigned = swscanf_s(szDebuggeeVersion, W("%08x;%08x;%p;"), piDebuggerVersion, pdwPidDebuggee, 
                                        phmodTargetCLR);
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
void AppendDbiDllName(SString & szFullDbiPath)
{
    const WCHAR * pDbiDllName = W("\\") MAKEDLLNAME_W(W("mscordbi"));
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
void GetDbiFilenameNextToRuntime(DWORD pidDebuggee, HMODULE hmodTargetCLR, SString & szFullDbiPath, 
                                 SString & szFullCoreClrPath)
{
    szFullDbiPath.Clear();

    // 
    // Step 1: (pid, hmodule) --> full path
    // 
    HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pidDebuggee);
    WCHAR modulePath[MAX_PATH];
    if(0 == GetModuleFileNameEx(hProcess, hmodTargetCLR, modulePath, MAX_PATH))
    {
        ThrowHR(E_FAIL);
    }
    
    // 
    // Step 2: 'Coreclr.dll' --> 'mscordbi.dll' 
    // 
    WCHAR * pCoreClrPath = modulePath;
    WCHAR * pLast = wcsrchr(pCoreClrPath, '\\');
    if (pLast == NULL)
    {
        ThrowHR(E_FAIL);
    }


    // Change:
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

bool CheckDbiAndRuntimeVersion(SString & szFullDbiPath, SString & szFullCoreClrPath)
{
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
}

#else

// Functions that we'll look for in the loaded Mscordbi module.
typedef HRESULT (STDAPICALLTYPE *FPCreateCordbObject)(
    int iDebuggerVersion, 
    IUnknown ** ppCordb);

#endif // FEATURE_PAL

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
HRESULT CreateDebuggingInterfaceFromVersionEx(
    int iDebuggerVersion,
    LPCWSTR szDebuggeeVersion,
    IUnknown ** ppCordb
    )
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

    *ppCordb = NULL;

#ifndef FEATURE_PAL
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
    // Step 2:  Find the proper Dbi module (mscordbi.dll) and load it.
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
        hMod = WszLoadLibraryEx(szFullDbiPath, NULL, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
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
    FPCoreCLRCreateCordbObject fpCreate2 = (FPCoreCLRCreateCordbObject)GetProcAddress(hMod, "CoreCLRCreateCordbObject");
    if (fpCreate2 == NULL)
    {
        // New-style creation API didn't exist - this DBI must be the wrong version, for the Mix07 protocol
        hr = CORDBG_E_INCOMPATIBLE_PROTOCOL;
        goto Exit;
    }

    // Invoke to instantiate an ICorDebug. This export was introduced after the Mix'07 release.
    hr = fpCreate2(iDebuggerVersion, pidDebuggee, hmodTargetCLR, &pCordb);
#else
    {
        hMod = LoadLibraryExW(MAKEDLLNAME_W(W("mscordbi")), NULL, 0);
        if (NULL == hMod)
        {
            hr = CORDBG_E_DEBUG_COMPONENT_MISSING;
            goto Exit;
        }
        FPCreateCordbObject fpCreate = (FPCreateCordbObject)GetProcAddress(hMod, "CreateCordbObject");
        if (fpCreate == NULL)
        {
            hr = CORDBG_E_INCOMPATIBLE_PROTOCOL;
            goto Exit;
        }
        hr = fpCreate(iDebuggerVersion, &pCordb);
    }
#endif // FEATURE_PAL
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
    if (ppCordb != NULL)
    {
        *ppCordb = pCordb;
    }

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
HRESULT CreateDebuggingInterfaceFromVersion(
    LPCWSTR szDebuggeeVersion, 
    IUnknown ** ppCordb
)
{
    PUBLIC_CONTRACT;

    return CreateDebuggingInterfaceFromVersionEx(CorDebugVersion_2_0,
                                                 szDebuggeeVersion,
                                                 ppCordb);
}

#ifndef FEATURE_PAL

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
HRESULT GetContinueStartupEvent(DWORD debuggeePID, 
                                LPCWSTR szTelestoFullPath,
                                __out HANDLE* phContinueStartupEvent)
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

        if (NULL != continueEvent)
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

#endif // !FEATURE_PAL

#if defined(FEATURE_CORESYSTEM) && defined(_TARGET_X86_)
#include "debugshim.h"
#endif

HRESULT CLRCreateInstance(REFCLSID clsid, REFIID riid, LPVOID *ppInterface)
{
#if defined(FEATURE_CORESYSTEM) && defined(_TARGET_X86_)

    if (ppInterface == NULL)
        return E_POINTER;

    if (clsid != CLSID_CLRDebugging || riid != IID_ICLRDebugging)
        return E_NOINTERFACE;
    
#if defined(FEATURE_CORESYSTEM)
    GUID skuId = CLR_ID_ONECORE_CLR;
#elif defined(FEATURE_CORECLR)
    GUID skuId = CLR_ID_CORECLR;
#else
    GUID skuId = CLR_ID_V4_DESKTOP;
#endif
    
    CLRDebuggingImpl *pDebuggingImpl = new CLRDebuggingImpl(skuId);
    return pDebuggingImpl->QueryInterface(riid, ppInterface);
#else
    return E_NOTIMPL;
#endif
}

#ifdef FEATURE_PAL

EXTERN_C BOOL WINAPI
DllMain(HANDLE instance, DWORD reason, LPVOID reserved)
{
    int err = 0;

    switch (reason)
    {
        case DLL_PROCESS_ATTACH:
        {
            err = PAL_InitializeDLL();
            break;
        }

        case DLL_THREAD_ATTACH:
            err = PAL_EnterTop();
            break;
    }

    if (err != 0)
    {
        return FALSE;
    }

    return TRUE;
}

#endif // FEATURE_PAL
