// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// JitDebugger.cpp
//
// Code to help with invoking the just-in-time debugger
//
//*****************************************************************************

#include "standardpch.h"
#include "runtimedetails.h"
#include "logging.h"
#include "jitdebugger.h"

// JIT debugging is broken due to utilcode changes to support LongFile. We need to re-copy
// or adjust the implementation of the below functions so they link properly.
#if 0
#ifndef TARGET_UNIX // No just-in-time debugger under PAL
#define FEATURE_JIT_DEBUGGING
#endif // !TARGET_UNIX
#endif // 0

#ifndef FEATURE_JIT_DEBUGGING

int DbgBreakCheck(const char* szFile, int iLine, const char* szExpr)
{
    LogError("SuperPMI: Assert Failure (PID %d, Thread %d/%x)\n"
             "%s\n"
             "\n"
             "%s, Line: %d\n",
             GetCurrentProcessId(), GetCurrentThreadId(), GetCurrentThreadId(), szExpr, szFile, iLine);

    return 1;
}

#else // FEATURE_JIT_DEBUGGING

// Some definitions to make this code look more like the CLR utilcode versions it was stolen from.
#define WszCreateEvent CreateEventW
#define WszGetModuleFileName GetModuleFileNameW

#ifdef WszRegOpenKeyEx
#undef WszRegOpenKeyEx
#define WszRegOpenKeyEx RegOpenKeyExW
#endif

#ifndef _WIN64
//------------------------------------------------------------------------------
// Returns TRUE if we are running on a 64-bit OS in WoW, FALSE otherwise.
BOOL RunningInWow64()
{
    static int s_Wow64Process;

    if (s_Wow64Process == 0)
    {
        BOOL fWow64Process = FALSE;

        if (!IsWow64Process(GetCurrentProcess(), &fWow64Process))
            fWow64Process = FALSE;

        s_Wow64Process = fWow64Process ? 1 : -1;
    }

    return (s_Wow64Process == 1) ? TRUE : FALSE;
}
#endif

//------------------------------------------------------------------------------
//
// GetRegistryLongValue - Reads a configuration LONG value from the registry.
//
// Parameters
//    hKeyParent             -- Parent key
//    szKey                  -- key to open
//    szName                 -- name of the value
//    pValue                 -- put value here, if found
//    fReadNonVirtualizedKey -- whether to read 64-bit hive on WOW64
//
// Returns
//   TRUE  -- If the value was found and read
//   FALSE -- The value was not found, could not be read, or was not DWORD
//
// Exceptions
//   None
//------------------------------------------------------------------------------
BOOL GetRegistryLongValue(HKEY hKeyParent, LPCWSTR szKey, LPCWSTR szName, long* pValue, BOOL fReadNonVirtualizedKey)
{
    DWORD  ret;                   // Return value from registry operation.
    HKEY   hkey;                  // Registry key.
    long   iValue;                // The value to read.
    DWORD  iType;                 // Type of value to get.
    DWORD  iSize;                 // Size of buffer.
    REGSAM samDesired = KEY_READ; // Desired access rights to the key

    if (fReadNonVirtualizedKey)
    {
        if (RunningInWow64())
        {
            samDesired |= KEY_WOW64_64KEY;
        }
    }

    ret = WszRegOpenKeyEx(hKeyParent, szKey, 0, samDesired, &hkey);

    // If we opened the key, see if there is a value.
    if (ret == ERROR_SUCCESS)
    {
        iType = REG_DWORD;
        iSize = sizeof(long);
        ret   = RegQueryValueExW(hkey, szName, NULL, &iType, reinterpret_cast<BYTE*>(&iValue), &iSize);

        if (ret == ERROR_SUCCESS && iType == REG_DWORD && iSize == sizeof(long))
        { // We successfully read a DWORD value.
            *pValue = iValue;
            return TRUE;
        }
    }

    return FALSE;
} // GetRegistryLongValue

//----------------------------------------------------------------------------
//
// GetCurrentModuleFileName - Retrieve the current module's filename
//
// Arguments:
//    pBuffer - output string buffer
//    pcchBuffer - the number of characters of the string buffer
//
// Return Value:
//    S_OK on success, else detailed error code.
//
// Note:
//
//----------------------------------------------------------------------------
HRESULT GetCurrentModuleFileName(_Out_writes_(*pcchBuffer) LPWSTR pBuffer, __inout DWORD* pcchBuffer)
{
    LIMITED_METHOD_CONTRACT;

    if ((pBuffer == NULL) || (pcchBuffer == NULL))
    {
        return E_INVALIDARG;
    }

    // Get the appname to look up in the exclusion or inclusion list.
    WCHAR appPath[MAX_PATH + 2];

    DWORD ret = WszGetModuleFileName(NULL, appPath, ARRAY_SIZE(appPath));

    if ((ret == ARRAY_SIZE(appPath)) || (ret == 0))
    {
        // The module file name exceeded maxpath, or GetModuleFileName failed.
        return E_UNEXPECTED;
    }

    // Pick off the part after the path.
    WCHAR* appName = wcsrchr(appPath, L'\\');

    // If no backslash, use the whole name; if there is a backslash, skip it.
    appName = appName ? appName + 1 : appPath;

    if (*pcchBuffer < wcslen(appName))
    {
        *pcchBuffer = static_cast<DWORD>(wcslen(appName)) + 1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    wcscpy_s(pBuffer, *pcchBuffer, appName);
    return S_OK;
}

//----------------------------------------------------------------------------
//
// IsCurrentModuleFileNameInAutoExclusionList - decide if the current module's filename
//                                              is in the AutoExclusionList list
//
// Arguments:
//    None
//
// Return Value:
//    TRUE or FALSE
//
// Note:
//    This function cannot be used in out of process scenarios like DAC because it
//    looks at current module's filename.   In OOP we want to use target process's
//    module's filename.
//
//----------------------------------------------------------------------------
BOOL IsCurrentModuleFileNameInAutoExclusionList()
{
    HKEY hKeyHolder;

    // Look for "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug\\AutoExclusionList"
    DWORD ret = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, kUnmanagedDebuggerAutoExclusionListKey, 0, KEY_READ, &hKeyHolder);

    if (ret != ERROR_SUCCESS)
    {
        // there's not even an AutoExclusionList hive
        return FALSE;
    }

    WCHAR wszAppName[MAX_PATH];
    DWORD cchAppName = ARRAY_SIZE(wszAppName);

    // Get the appname to look up in the exclusion or inclusion list.
    if (GetCurrentModuleFileName(wszAppName, &cchAppName) != S_OK)
    {
        // Assume it is not on the exclusion list if we cannot find the module's filename.
        return FALSE;
    }

    // Look in AutoExclusionList key for appName get the size of any value stored there.
    DWORD value, valueType, valueSize = sizeof(value);
    ret = RegQueryValueExW(hKeyHolder, wszAppName, 0, &valueType, reinterpret_cast<BYTE*>(&value), &valueSize);
    if ((ret == ERROR_SUCCESS) && (valueType == REG_DWORD) && (value == 1))
    {
        return TRUE;
    }

    return FALSE;
} // IsCurrentModuleFileNameInAutoExclusionList

//*****************************************************************************
// Retrieve information regarding what registered default debugger
//*****************************************************************************
void GetDebuggerSettingInfo(LPWSTR wszDebuggerString, DWORD cchDebuggerString, BOOL* pfAuto)
{
    HRESULT hr = GetDebuggerSettingInfoWorker(wszDebuggerString, &cchDebuggerString, pfAuto);

    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        // error!
    }
} // GetDebuggerSettingInfo

//---------------------------------------------------------------------------------------
//
// GetDebuggerSettingInfoWorker - retrieve information regarding what registered default debugger
//
// Arguments:
//      * wszDebuggerString - [out] the string buffer to store the registered debugger launch
//                            string
//      * pcchDebuggerString - [in, out] the size of string buffer in characters
//      * pfAuto - [in] the flag to indicate whether the debugger neeeds to be launched
//                 automatically
//
// Return Value:
//    HRESULT indicating success or failure.
//
// Notes:
//     * wszDebuggerString can be NULL.   When wszDebuggerString is NULL, pcchDebuggerString should
//     * point to a DWORD of zero.   pcchDebuggerString cannot be NULL, and the DWORD pointed by
//     * pcchDebuggerString will store the used or required string buffer size in characters.
HRESULT GetDebuggerSettingInfoWorker(_Out_writes_to_opt_(*pcchDebuggerString, *pcchDebuggerString)
                                         LPWSTR wszDebuggerString,
                                     DWORD*     pcchDebuggerString,
                                     BOOL*      pfAuto)
{
    if ((pcchDebuggerString == NULL) || ((wszDebuggerString == NULL) && (*pcchDebuggerString != 0)))
    {
        return E_INVALIDARG;
    }

    // Initialize the output values before we start.
    if ((wszDebuggerString != NULL) && (*pcchDebuggerString > 0))
    {
        *wszDebuggerString = L'\0';
    }

    if (pfAuto != NULL)
    {
        *pfAuto = FALSE;
    }

    HKEY hKey;

    // Look for "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug"
    DWORD ret = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, kUnmanagedDebuggerKey, 0, KEY_READ, &hKey);

    if (ret != ERROR_SUCCESS)
    { // Wow, there's not even an AeDebug hive, so no native debugger, no auto.
        return S_OK;
    }

    // Look in AeDebug key for "Debugger"; get the size of any value stored there.
    DWORD valueType, valueSize;
    ret = RegQueryValueExW(hKey, kUnmanagedDebuggerValue, 0, &valueType, 0, &valueSize);

    if ((wszDebuggerString == NULL) || (*pcchDebuggerString < valueSize / sizeof(WCHAR)))
    {
        *pcchDebuggerString = valueSize / sizeof(WCHAR) + 1;
        RegCloseKey(hKey);
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    *pcchDebuggerString = valueSize / sizeof(WCHAR);

    // The size of an empty string with the null terminator is 2.
    BOOL fIsDebuggerStringEmpty = valueSize <= 2 ? TRUE : FALSE;

    if ((ret != ERROR_SUCCESS) || (valueType != REG_SZ) || fIsDebuggerStringEmpty)
    {
        RegCloseKey(hKey);
        return S_OK;
    }

    ret = RegQueryValueExW(hKey, kUnmanagedDebuggerValue, NULL, NULL, reinterpret_cast<LPBYTE>(wszDebuggerString),
                           &valueSize);
    if (ret != ERROR_SUCCESS)
    {
        *wszDebuggerString = L'\0';
        RegCloseKey(hKey);
        return S_OK;
    }

    if (pfAuto != NULL)
    {
        BOOL fAuto = FALSE;

        // Get the appname to look up in DebugApplications key.
        WCHAR wzAppName[MAX_PATH];
        DWORD cchAppName = ARRAY_SIZE(wzAppName);
        long  iValue;

        // Check DebugApplications setting
        if ((SUCCEEDED(GetCurrentModuleFileName(wzAppName, &cchAppName))) &&
            (GetRegistryLongValue(HKEY_LOCAL_MACHINE, kDebugApplicationsPoliciesKey, wzAppName, &iValue, TRUE) ||
             GetRegistryLongValue(HKEY_LOCAL_MACHINE, kDebugApplicationsKey, wzAppName, &iValue, TRUE) ||
             GetRegistryLongValue(HKEY_CURRENT_USER, kDebugApplicationsPoliciesKey, wzAppName, &iValue, TRUE) ||
             GetRegistryLongValue(HKEY_CURRENT_USER, kDebugApplicationsKey, wzAppName, &iValue, TRUE)) &&
            (iValue == 1))
        {
            fAuto = TRUE;
        }
        else
        {
            // Look in AeDebug key for "Auto"; get the size of any value stored there.
            ret = RegQueryValueExW(hKey, kUnmanagedDebuggerAutoValue, 0, &valueType, 0, &valueSize);
            if ((ret == ERROR_SUCCESS) && (valueType == REG_SZ) && (valueSize / sizeof(WCHAR) < MAX_PATH))
            {
                WCHAR wzAutoKey[MAX_PATH];
                valueSize = ARRAY_SIZE(wzAutoKey) * sizeof(WCHAR);
                RegQueryValueExW(hKey, kUnmanagedDebuggerAutoValue, NULL, NULL, reinterpret_cast<LPBYTE>(wzAutoKey),
                                 &valueSize);

                // The OS's behavior is to consider Auto to be FALSE unless the first character is set
                // to 1. They don't take into consideration the following characters. Also if the value
                // isn't present they assume an Auto value of FALSE.
                if ((wzAutoKey[0] == L'1') && !IsCurrentModuleFileNameInAutoExclusionList())
                {
                    fAuto = TRUE;
                }
            }
        }

        *pfAuto = fAuto;
    }

    RegCloseKey(hKey);
    return S_OK;
} // GetDebuggerSettingInfoWorker

BOOL LaunchJITDebugger()
{
    BOOL fSuccess = FALSE;

    WCHAR debugger[1000];
    GetDebuggerSettingInfo(debugger, ARRAY_SIZE(debugger), NULL);

    SECURITY_ATTRIBUTES sa;
    sa.nLength              = sizeof(sa);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle       = TRUE;

    // We can leave this event as it is since it is inherited by a child process.
    // We will block one scheduler, but the process is asking a user if they want to attach debugger.
    HANDLE eventHandle = WszCreateEvent(&sa, TRUE, FALSE, NULL);
    if (eventHandle == NULL)
    {
        return FALSE;
    }

    WCHAR cmdLine[1000];
    swprintf_s(cmdLine, debugger, GetCurrentProcessId(), eventHandle);

    STARTUPINFOW StartupInfo;
    memset(&StartupInfo, 0, sizeof(StartupInfo));
    StartupInfo.cb        = sizeof(StartupInfo);
    StartupInfo.lpDesktop = L"Winsta0\\Default";

    PROCESS_INFORMATION ProcessInformation;
    if (CreateProcessW(NULL, cmdLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &ProcessInformation))
    {
        WaitForSingleObject(eventHandle, INFINITE);
        fSuccess = TRUE;
    }

    CloseHandle(eventHandle);

    return fSuccess;
}

// See if we should invoke the just-in-time debugger on an assert.
int DbgBreakCheck(const char* szFile, int iLine, const char* szExpr)
{
    char dialogText[1000];
    char dialogTitle[1000];

    sprintf_s(dialogText, sizeof(dialogText),
              "%s\n\n%s, Line: %d\n\nAbort - Kill program\nRetry - Debug\nIgnore - Keep running\n", szExpr, szFile,
              iLine);
    sprintf_s(dialogTitle, sizeof(dialogTitle), "SuperPMI: Assert Failure (PID %d, Thread %d/%x)        ",
              GetCurrentProcessId(), GetCurrentThreadId(), GetCurrentThreadId());

    // Tell user there was an error.
    int ret = MessageBoxA(NULL, dialogText, dialogTitle, MB_ABORTRETRYIGNORE | MB_ICONEXCLAMATION | MB_TOPMOST);

    switch (ret)
    {
        case IDABORT:
            TerminateProcess(GetCurrentProcess(), 1);
            break;

        // Tell caller to break at the correct location.
        case IDRETRY:

            if (IsDebuggerPresent())
            {
                SetErrorMode(0);
            }
            else
            {
                LaunchJITDebugger();
            }

            return 1;

        case IDIGNORE:
            // nothing to do
            break;
    }

    return 0;
}

#endif // FEATURE_JIT_DEBUGGING
