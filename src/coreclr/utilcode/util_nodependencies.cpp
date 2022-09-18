// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Util_NoDependencies.cpp
//

//
// This contains a bunch of C++ utility classes needed also for UtilCode without dependencies
// (standalone version without CLR/clr.dll/mscoree.dll dependencies).
//
//*****************************************************************************

#include "stdafx.h"
#include "utilcode.h"
#include "ex.h"

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES) || defined(_DEBUG)

RunningOnStatusEnum gRunningOnStatus = RUNNING_ON_STATUS_UNINITED;

#define NON_SUPPORTED_PLATFORM_TERMINATE_ERROR_CODE     0xBAD1BAD1

//*****************************************************************************
// One time initialization of the OS version
//*****************************************************************************
void InitRunningOnVersionStatus ()
{
#ifndef TARGET_UNIX
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    BOOL fSupportedPlatform = FALSE;
    OSVERSIONINFOEX sVer;
    DWORDLONG dwlConditionMask;

    ZeroMemory(&sVer, sizeof(OSVERSIONINFOEX));
    sVer.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

    sVer.dwMajorVersion = 6;
    sVer.dwMinorVersion = 2;
    sVer.dwPlatformId = VER_PLATFORM_WIN32_NT;


    dwlConditionMask = 0;
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_PLATFORMID, VER_EQUAL);
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);

    if(VerifyVersionInfo(&sVer, VER_MAJORVERSION | VER_PLATFORMID | VER_MINORVERSION, dwlConditionMask))
    {
        gRunningOnStatus = RUNNING_ON_WIN8;
        fSupportedPlatform = TRUE;
        goto CHECK_SUPPORTED;
    }


    ZeroMemory(&sVer, sizeof(OSVERSIONINFOEX));
    sVer.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

    sVer.dwMajorVersion = 6;
    sVer.dwMinorVersion = 1;
    sVer.dwPlatformId = VER_PLATFORM_WIN32_NT;


    dwlConditionMask = 0;
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_PLATFORMID, VER_EQUAL);
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
    dwlConditionMask = VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);

    if(VerifyVersionInfo(&sVer, VER_MAJORVERSION | VER_PLATFORMID | VER_MINORVERSION, dwlConditionMask))
    {
        gRunningOnStatus = RUNNING_ON_WIN7;
        fSupportedPlatform = TRUE;
        goto CHECK_SUPPORTED;
    }

CHECK_SUPPORTED:

    if (!fSupportedPlatform)
    {
        // The current platform isn't supported. Display a message to this effect and exit.
        fprintf(stderr, "Platform not supported: The minimum supported platform is Windows 7\n");
        TerminateProcess(GetCurrentProcess(), NON_SUPPORTED_PLATFORM_TERMINATE_ERROR_CODE);
    }
#endif // TARGET_UNIX
} // InitRunningOnVersionStatus

#ifndef HOST_64BIT
//------------------------------------------------------------------------------
// Returns TRUE if we are running on a 64-bit OS in WoW, FALSE otherwise.
BOOL RunningInWow64()
{
    #ifdef TARGET_UNIX
    return FALSE;
    #else
    static int s_Wow64Process;

    if (s_Wow64Process == 0)
    {
        BOOL fWow64Process = FALSE;

        if (!IsWow64Process(GetCurrentProcess(), &fWow64Process))
            fWow64Process = FALSE;

        s_Wow64Process = fWow64Process ? 1 : -1;
    }

    return (s_Wow64Process == 1) ? TRUE : FALSE;
    #endif
}
#endif

#ifndef TARGET_UNIX
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
BOOL GetRegistryLongValue(HKEY    hKeyParent,
                          LPCWSTR szKey,
                          LPCWSTR szName,
                          long    *pValue,
                          BOOL    fReadNonVirtualizedKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD       ret;                    // Return value from registry operation.
    HKEYHolder  hkey;                   // Registry key.
    long        iValue;                 // The value to read.
    DWORD       iType;                  // Type of value to get.
    DWORD       iSize;                  // Size of buffer.
    REGSAM      samDesired = KEY_READ;  // Desired access rights to the key

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
        ret = WszRegQueryValueEx(hkey, szName, NULL, &iType, reinterpret_cast<BYTE*>(&iValue), &iSize);

        if (ret == ERROR_SUCCESS && iType == REG_DWORD && iSize == sizeof(long))
        {   // We successfully read a DWORD value.
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
//
// Return Value:
//    S_OK on success, else detailed error code.
//
// Note:
//
//----------------------------------------------------------------------------
HRESULT GetCurrentModuleFileName(SString& pBuffer)
{
    LIMITED_METHOD_CONTRACT;


    DWORD ret = WszGetModuleFileName(NULL, pBuffer);

    if (ret == 0)
    {
        return E_UNEXPECTED;
    }


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
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HKEYHolder hKeyHolder;

    // Look for "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug\\AutoExclusionList"
    DWORD ret = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, kUnmanagedDebuggerAutoExclusionListKey, 0, KEY_READ, &hKeyHolder);

    if (ret != ERROR_SUCCESS)
    {
        // there's not even an AutoExclusionList hive
        return FALSE;
    }

    PathString wszAppName;

    // Get the appname to look up in the exclusion or inclusion list.
    if (GetCurrentModuleFileName(wszAppName) != S_OK)
    {
        // Assume it is not on the exclusion list if we cannot find the module's filename.
        return FALSE;
    }

    // Look in AutoExclusionList key for appName get the size of any value stored there.
    DWORD value, valueType, valueSize = sizeof(value);
    ret = WszRegQueryValueEx(hKeyHolder, wszAppName, 0, &valueType, reinterpret_cast<BYTE*>(&value), &valueSize);
    if ((ret == ERROR_SUCCESS) && (valueType == REG_DWORD) && (value == 1))
    {
        return TRUE;
    }

    return FALSE;
} // IsCurrentModuleFileNameInAutoExclusionList

//*****************************************************************************
// Retrieve information regarding what registered default debugger
//*****************************************************************************
void GetDebuggerSettingInfo(SString &ssDebuggerString, BOOL *pfAuto)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EX_TRY
    {
        DWORD cchDebuggerString = MAX_LONGPATH;
        INDEBUG(DWORD cchOldDebuggerString = cchDebuggerString);

        WCHAR * buf = ssDebuggerString.OpenUnicodeBuffer(cchDebuggerString);
        HRESULT hr = GetDebuggerSettingInfoWorker(buf, &cchDebuggerString, pfAuto);
        ssDebuggerString.CloseBuffer(cchDebuggerString);

        while (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            _ASSERTE(cchDebuggerString > cchOldDebuggerString);
            INDEBUG(cchOldDebuggerString = cchDebuggerString);

            buf = ssDebuggerString.OpenUnicodeBuffer(cchDebuggerString);
            hr = GetDebuggerSettingInfoWorker(buf, &cchDebuggerString, pfAuto);
            ssDebuggerString.CloseBuffer(cchDebuggerString);
        }

        if (*ssDebuggerString.GetUnicode() == W('\0'))
        {
            ssDebuggerString.Clear();
        }

        if (FAILED(hr))
        {
            ssDebuggerString.Clear();
            if (pfAuto)
            {
                *pfAuto = FALSE;
            }
        }
    }
    EX_CATCH
    {
        ssDebuggerString.Clear();
        if (pfAuto)
        {
            *pfAuto = FALSE;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
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
HRESULT GetDebuggerSettingInfoWorker(_Out_writes_to_opt_(*pcchDebuggerString, *pcchDebuggerString) LPWSTR wszDebuggerString, DWORD * pcchDebuggerString, BOOL * pfAuto)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(pcchDebuggerString != NULL);
    }
    CONTRACTL_END;

    if ((pcchDebuggerString == NULL) || ((wszDebuggerString == NULL) && (*pcchDebuggerString != 0)))
    {
        return E_INVALIDARG;
    }

    // Initialize the output values before we start.
    if ((wszDebuggerString != NULL) && (*pcchDebuggerString > 0))
    {
        *wszDebuggerString = W('\0');
    }

    if (pfAuto != NULL)
    {
        *pfAuto = FALSE;
    }

    HKEYHolder hKeyHolder;

    // Look for "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AeDebug"
    DWORD ret = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, kUnmanagedDebuggerKey, 0, KEY_READ, &hKeyHolder);

    if (ret != ERROR_SUCCESS)
    {   // Wow, there's not even an AeDebug hive, so no native debugger, no auto.
        return S_OK;
    }

    // Look in AeDebug key for "Debugger"; get the size of any value stored there.
    DWORD valueType, valueSize = 0;
    ret = WszRegQueryValueEx(hKeyHolder, kUnmanagedDebuggerValue, 0, &valueType, 0, &valueSize);

    _ASSERTE(pcchDebuggerString != NULL);
    if ((wszDebuggerString == NULL) || (*pcchDebuggerString < valueSize / sizeof(WCHAR)))
    {
        *pcchDebuggerString = valueSize / sizeof(WCHAR) + 1;
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    *pcchDebuggerString = valueSize / sizeof(WCHAR);

    // The size of an empty string with the null terminator is 2.
    BOOL fIsDebuggerStringEmptry = valueSize <= 2 ? TRUE : FALSE;

    if ((ret != ERROR_SUCCESS) || (valueType != REG_SZ) || fIsDebuggerStringEmptry)
    {
        return S_OK;
    }

    _ASSERTE(wszDebuggerString != NULL);
    ret = WszRegQueryValueEx(hKeyHolder, kUnmanagedDebuggerValue, NULL, NULL, reinterpret_cast< LPBYTE >(wszDebuggerString), &valueSize);
    if (ret != ERROR_SUCCESS)
    {
        *wszDebuggerString = W('\0');
        return S_OK;
    }

    // The callers are in nothrow scope, so we must swallow exceptions and reset the output parameters to the
    // default values if exceptions like OOM ever happen.
    EX_TRY
    {
        if (pfAuto != NULL)
        {
            BOOL fAuto = FALSE;

            // Get the appname to look up in DebugApplications key.
            PathString wzAppName;
            long iValue;

            // Check DebugApplications setting
            if ((SUCCEEDED(GetCurrentModuleFileName(wzAppName))) &&
                (
                    GetRegistryLongValue(HKEY_LOCAL_MACHINE, kDebugApplicationsPoliciesKey, wzAppName, &iValue, TRUE) ||
                    GetRegistryLongValue(HKEY_LOCAL_MACHINE, kDebugApplicationsKey, wzAppName, &iValue, TRUE) ||
                    GetRegistryLongValue(HKEY_CURRENT_USER,  kDebugApplicationsPoliciesKey, wzAppName, &iValue, TRUE) ||
                    GetRegistryLongValue(HKEY_CURRENT_USER,  kDebugApplicationsKey, wzAppName, &iValue, TRUE)
                ) &&
                (iValue == 1))
            {
                fAuto = TRUE;
            }
            else
            {
                // Look in AeDebug key for "Auto"; get the size of any value stored there.
                ret = WszRegQueryValueEx(hKeyHolder, kUnmanagedDebuggerAutoValue, 0, &valueType, 0, &valueSize);
                if ((ret == ERROR_SUCCESS) && (valueType == REG_SZ) && (valueSize / sizeof(WCHAR) < MAX_LONGPATH))
                {
                    WCHAR wzAutoKey[MAX_LONGPATH];
                    valueSize = ARRAY_SIZE(wzAutoKey) * sizeof(WCHAR);
                    WszRegQueryValueEx(hKeyHolder, kUnmanagedDebuggerAutoValue, NULL, NULL, reinterpret_cast< LPBYTE >(wzAutoKey), &valueSize);

                    // The OS's behavior is to consider Auto to be FALSE unless the first character is set
                    // to 1. They don't take into consideration the following characters. Also if the value
                    // isn't present they assume an Auto value of FALSE.
                    if ((wzAutoKey[0] == W('1')) && !IsCurrentModuleFileNameInAutoExclusionList())
                    {
                        fAuto = TRUE;
                    }
                }
            }

            *pfAuto = fAuto;
        }
    }
    EX_CATCH
    {
        if ((wszDebuggerString != NULL) && (*pcchDebuggerString > 0))
        {
            *wszDebuggerString = W('\0');
        }

        if (pfAuto != NULL)
        {
            *pfAuto = FALSE;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    return S_OK;
} // GetDebuggerSettingInfoWorker
#endif // TARGET_UNIX

#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES) || defined(_DEBUG)

//*****************************************************************************
// Convert hex value into a wide string of hex digits
//*****************************************************************************
HRESULT GetStr(
                                 DWORD  hHexNum,
    _Out_writes_((cbHexNum * 2)) LPWSTR szHexNum,
                                 DWORD  cbHexNum)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    _ASSERTE (szHexNum);
    cbHexNum *= 2; // each nibble is a char
    while (cbHexNum != 0)
    {
        DWORD thisHexDigit = hHexNum % 16;
        hHexNum /= 16;
        cbHexNum--;
        if (thisHexDigit < 10)
        {
            *(szHexNum+cbHexNum) = (BYTE)(thisHexDigit + W('0'));
        }
        else
        {
            *(szHexNum+cbHexNum) = (BYTE)(thisHexDigit - 10 + W('A'));
        }
    }
    return S_OK;
}

//*****************************************************************************
// Convert a GUID into a pointer to a Wide char string
//*****************************************************************************
int
GuidToLPWSTR(
                          GUID   Guid,      // The GUID to convert.
    _Out_writes_(cchGuid) LPWSTR szGuid,    // String into which the GUID is stored
                          DWORD  cchGuid)   // Count in wchars
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    int         i;

    // successive fields break the GUID into the form DWORD-WORD-WORD-WORD-WORD.DWORD
    // covering the 128-bit GUID. The string includes enclosing braces, which are an OLE convention.

    if (cchGuid < 39) // 38 chars + 1 null terminating.
        return 0;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    // ^
    szGuid[0]  = W('{');

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //  ^^^^^^^^
    if (FAILED (GetStr(Guid.Data1, szGuid+1 , 4))) return 0;

    szGuid[9]  = W('-');

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //           ^^^^
    if (FAILED (GetStr(Guid.Data2, szGuid+10, 2))) return 0;

    szGuid[14] = W('-');

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                ^^^^
    if (FAILED (GetStr(Guid.Data3, szGuid+15, 2))) return 0;

    szGuid[19] = W('-');

    // Get the last two fields (which are byte arrays).
    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                     ^^^^
    for (i=0; i < 2; ++i)
        if (FAILED(GetStr(Guid.Data4[i], szGuid + 20 + (i * 2), 1)))
            return (0);

    szGuid[24] = W('-');

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                          ^^^^^^^^^^^^
    for (i=0; i < 6; ++i)
        if (FAILED(GetStr(Guid.Data4[i+2], szGuid + 25 + (i * 2), 1)))
            return (0);

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                                      ^
    szGuid[37] = W('}');
    szGuid[38] = W('\0');

    return 39;
} // GuidToLPWSTR

//*****************************************************************************
// Convert wide string of (at most eight) hex digits into a hex value
//*****************************************************************************
HRESULT GetHex(
                                DWORD * phHexNum,
    _In_reads_((cbHexNum * 2)) LPCWSTR szHexNum,
                                DWORD   cbHexNum)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    _ASSERTE (szHexNum && phHexNum);
    _ASSERTE(cbHexNum == 1 || cbHexNum == 2 || cbHexNum == 4);

    cbHexNum *= 2; // each nibble is a char
    DWORD val = 0;
    for (DWORD i = 0; i < cbHexNum; ++i)
    {
        DWORD nibble = 0;
        if (szHexNum[i] >= W('0') && szHexNum[i] <= W('9'))
        {
            nibble = szHexNum[i] - '0';
        }
        else if (szHexNum[i] >= W('A') && szHexNum[i] <= W('F'))
        {
            nibble = 10 + szHexNum[i] - 'A';
        }
        else if (szHexNum[i] >= W('a') && szHexNum[i] <= W('f'))
        {
            nibble = 10 + szHexNum[i] - 'a';
        }
        else
        {
            return E_FAIL;
        }
        val = (val << 4) + nibble;
    }
    *phHexNum = val;
    return S_OK;
}

//*****************************************************************************
// Parse a Wide char string into a GUID
//*****************************************************************************
BOOL
LPWSTRToGuid(
                         GUID  * Guid,      // [OUT] The GUID to fill in
    _In_reads_(cchGuid) LPCWSTR szGuid,    // [IN] String to parse
                         DWORD   cchGuid)   // [IN] Count in wchars in string
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    int         i;
    DWORD dw;

    // successive fields break the GUID into the form DWORD-WORD-WORD-WORD-WORD.DWORD
    // covering the 128-bit GUID. The string includes enclosing braces, which are an OLE convention.

    if (cchGuid < 38) // 38 chars + 1 null terminating.
        return FALSE;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    // ^
    if (szGuid[0] != W('{')) return FALSE;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //  ^^^^^^^^
    if (FAILED (GetHex(&dw, szGuid+1 , 4))) return FALSE;
    Guid->Data1 = dw;

    if (szGuid[9] != W('-')) return FALSE;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //           ^^^^
    if (FAILED (GetHex(&dw, szGuid+10, 2))) return FALSE;
    Guid->Data2 = (WORD)dw;

    if (szGuid[14] != W('-')) return FALSE;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                ^^^^
    if (FAILED (GetHex(&dw, szGuid+15, 2))) return FALSE;
    Guid->Data3 = (WORD)dw;

    if (szGuid[19] != W('-')) return FALSE;

    // Get the last two fields (which are byte arrays).
    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                     ^^^^
    for (i=0; i < 2; ++i)
    {
        if (FAILED(GetHex(&dw, szGuid + 20 + (i * 2), 1))) return FALSE;
        Guid->Data4[i] = (BYTE)dw;
    }

    if (szGuid[24] != W('-')) return FALSE;

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                          ^^^^^^^^^^^^
    for (i=0; i < 6; ++i)
    {
        if (FAILED(GetHex(&dw, szGuid + 25 + (i * 2), 1))) return FALSE;
        Guid->Data4[i+2] = (BYTE)dw;
    }

    // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
    //                                      ^
    if (szGuid[37] != W('}')) return FALSE;

    return TRUE;
} // GuidToLPWSTR

/**************************************************************************/
void ConfigDWORD::init(const CLRConfig::ConfigDWORDInfo & info)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    // make sure that the memory was zero initialized
    _ASSERTE(m_inited == 0 || m_inited == 1);

    m_value = CLRConfig::GetConfigValue(info);
    m_inited = 1;
}

//---------------------------------------------------------------------------------------
//
// Takes a const input string, and returns the start & size of the substring that has all
// leading and trailing whitespace removed. The original string is not modified.
//
// Arguments:
//      * pwsz - [in] points to const string we want to trim; [out] points to beginning
//          of trimmed substring of input string
//      * pcch - [in] Points to length in chars of input string (not counting null
//          terminator); [out] Points to length in chars of trimmed substring (not
//          counting null terminator)
//
void TrimWhiteSpace(_Outptr_result_buffer_(*pcch)  LPCWSTR *pwsz, __inout LPDWORD pcch)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE (pwsz != NULL);
    _ASSERTE (*pwsz != NULL);
    _ASSERTE (pcch != NULL);

    DWORD cch = *pcch;
    LPCWSTR wszBeginning = *pwsz;
    LPCWSTR wszEnd = wszBeginning + (cch - 1);

    while ((cch != 0) && iswspace(*wszBeginning))
    {
        wszBeginning++;
        cch--;
    }

    while ((cch != 0) && iswspace(*wszEnd))
    {
        wszEnd--;
        cch--;
    }

    *pwsz = wszBeginning;
    *pcch = cch;
} // TrimWhiteSpace

void OutputDebugStringUtf8(LPCUTF8 utf8DebugMsg)
{
#ifdef TARGET_UNIX
    OutputDebugStringA(utf8DebugMsg);
#else
    if (utf8DebugMsg == NULL)
        utf8DebugMsg = "";

    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wideDebugMsg, utf8DebugMsg);
    OutputDebugStringW(wideDebugMsg);
#endif // !TARGET_UNIX
}

BOOL ThreadWillCreateGuardPage(SIZE_T sizeReservedStack, SIZE_T sizeCommittedStack)
{
    // We need to make sure there will be a reserved but never committed page at the end
    // of the stack. We do here the check NT does when it creates the user stack to decide
    // if there is going to be a guard page. However, that is not enough, as if we only
    // have a guard page, we have nothing to protect us from going pass it. Well, in
    // fact, there is something that we will protect you, there are certain places
    // (RTLUnwind) in NT that will check that the current frame is within stack limits.
    // If we are not it will bomb out. We will also bomb out if we touch the hard guard
    // page.
    //
    // For situation B, teb->StackLimit is at the beginning of the user stack (ie
    // before updating StackLimit it checks if it was able to create a new guard page,
    // in this case, it can't), which makes the check fail in RtlUnwind.
    //
    //    Situation A  [ Hard guard page | Guard page | user stack]
    //
    //    Situation B  [ Guard page | User stack ]
    //
    //    Situation C  [ User stack ( no room for guard page) ]
    //
    //    Situation D (W9x) : Guard page or not, w9x has a 64k reserved region below
    //                        the stack, we don't need any checks at all
    //
    // We really want to be in situation A all the time, so we add one more page
    // to our requirements (we require guard page + hard guard)

    SYSTEM_INFO sysInfo;
    ::GetSystemInfo(&sysInfo);

    // OS rounds up sizes the following way to decide if it marks a guard page
    sizeReservedStack = ALIGN(sizeReservedStack, ((size_t)sysInfo.dwAllocationGranularity));   // Allocation granularity
    sizeCommittedStack = ALIGN(sizeCommittedStack, ((size_t)sysInfo.dwPageSize));  // Page Size

    // OS wont create guard page, we can't execute managed code safely.
    // We also have to make sure we have a 'hard' guard, thus we add another
    // page to the memory we would need comitted.
    // That is, the following code will check if sizeReservedStack is at least 2 pages
    // more than sizeCommittedStack.
    return (sizeReservedStack > sizeCommittedStack + ((size_t)sysInfo.dwPageSize));
} // ThreadWillCreateGuardPage
