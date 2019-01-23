// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#define NON_SUPPORTED_PLATFORM_MSGBOX_TITLE             W("Platform not supported")
#define NON_SUPPORTED_PLATFORM_MSGBOX_TEXT              W("The minimum supported platform is Windows 7")
#define NON_SUPPORTED_PLATFORM_TERMINATE_ERROR_CODE     0xBAD1BAD1

//*****************************************************************************
// One time initialization of the OS version
//*****************************************************************************
void InitRunningOnVersionStatus ()
{
#ifndef FEATURE_PAL
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
        // The current platform isn't supported. Display a message box to this effect and exit.
        // Note that this should never happen since the .NET Fx setup should not install on 
        // non supported platforms (which is why the message box text isn't localized).
        UtilMessageBoxCatastrophicNonLocalized(NON_SUPPORTED_PLATFORM_MSGBOX_TITLE, NON_SUPPORTED_PLATFORM_MSGBOX_TEXT, MB_OK | MB_ICONERROR, TRUE);
        TerminateProcess(GetCurrentProcess(), NON_SUPPORTED_PLATFORM_TERMINATE_ERROR_CODE);
    }
#endif // FEATURE_PAL
} // InitRunningOnVersionStatus

#ifndef _WIN64
//------------------------------------------------------------------------------
// Returns TRUE if we are running on a 64-bit OS in WoW, FALSE otherwise.
BOOL RunningInWow64()
{
    #ifdef PLATFORM_UNIX
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

#ifndef FEATURE_PAL
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
HRESULT GetDebuggerSettingInfoWorker(__out_ecount_part_opt(*pcchDebuggerString, *pcchDebuggerString) LPWSTR wszDebuggerString, DWORD * pcchDebuggerString, BOOL * pfAuto)
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
                    valueSize = NumItems(wzAutoKey) * sizeof(WCHAR);
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
#endif // FEATURE_PAL

#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES) || defined(_DEBUG)

//*****************************************************************************
// Convert hex value into a wide string of hex digits 
//*****************************************************************************
HRESULT GetStr(
                                 DWORD  hHexNum, 
    __out_ecount((cbHexNum * 2)) LPWSTR szHexNum, 
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
    __out_ecount(cchGuid) LPWSTR szGuid,    // String into which the GUID is stored
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
    __in_ecount((cbHexNum * 2)) LPCWSTR szHexNum, 
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
    __in_ecount(cchGuid) LPCWSTR szGuid,    // [IN] String to parse
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


#ifdef _DEBUG
// Always write regardless of registry.
void _cdecl DbgWriteEx(LPCTSTR szFmt, ...)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    
    WCHAR rcBuff[1024];
    va_list marker;

    va_start(marker, szFmt);
    _vsnwprintf_s(rcBuff, _countof(rcBuff), _TRUNCATE, szFmt, marker);
    va_end(marker);
    WszOutputDebugString(rcBuff);
}
#endif //_DEBUG

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
void TrimWhiteSpace(__deref_inout_ecount(*pcch)  LPCWSTR *pwsz, __inout LPDWORD pcch)
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

BOOL ThreadWillCreateGuardPage(SIZE_T sizeReservedStack, SIZE_T sizeCommitedStack)
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
    // For situation B, teb->StackLimit is at the beggining of the user stack (ie
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
    sizeCommitedStack = ALIGN(sizeCommitedStack, ((size_t)sysInfo.dwPageSize));  // Page Size
 
    // OS wont create guard page, we can't execute managed code safely.
    // We also have to make sure we have a 'hard' guard, thus we add another
    // page to the memory we would need comitted.
    // That is, the following code will check if sizeReservedStack is at least 2 pages 
    // more than sizeCommitedStack.
    return (sizeReservedStack > sizeCommitedStack + ((size_t)sysInfo.dwPageSize));     
} // ThreadWillCreateGuardPage

//The following characters have special sorting weights when combined with other
//characters, which means we can't use our fast sorting algorithm on them.
//Most of these are pretty rare control characters, but apostrophe and hyphen
//are fairly common and force us down the slower path.  This is because we want
//"word sorting", which means that "coop" and "co-op" sort together, instead of
//separately as they would if we were doing a string sort.
//      0x0001   6    3    2   2   0  ;Start Of Heading
//      0x0002   6    4    2   2   0  ;Start Of Text
//      0x0003   6    5    2   2   0  ;End Of Text
//      0x0004   6    6    2   2   0  ;End Of Transmission
//      0x0005   6    7    2   2   0  ;Enquiry
//      0x0006   6    8    2   2   0  ;Acknowledge
//      0x0007   6    9    2   2   0  ;Bell
//      0x0008   6   10    2   2   0  ;Backspace

//      0x000e   6   11    2   2   0  ;Shift Out
//      0x000f   6   12    2   2   0  ;Shift In
//      0x0010   6   13    2   2   0  ;Data Link Escape
//      0x0011   6   14    2   2   0  ;Device Control One
//      0x0012   6   15    2   2   0  ;Device Control Two
//      0x0013   6   16    2   2   0  ;Device Control Three
//      0x0014   6   17    2   2   0  ;Device Control Four
//      0x0015   6   18    2   2   0  ;Negative Acknowledge
//      0x0016   6   19    2   2   0  ;Synchronous Idle
//      0x0017   6   20    2   2   0  ;End Of Transmission Block
//      0x0018   6   21    2   2   0  ;Cancel
//      0x0019   6   22    2   2   0  ;End Of Medium
//      0x001a   6   23    2   2   0  ;Substitute
//      0x001b   6   24    2   2   0  ;Escape
//      0x001c   6   25    2   2   0  ;File Separator
//      0x001d   6   26    2   2   0  ;Group Separator
//      0x001e   6   27    2   2   0  ;Record Separator
//      0x001f   6   28    2   2   0  ;Unit Separator

//      0x0027   6  128    2   2   0  ;Apostrophe-Quote
//      0x002d   6  130    2   2   0  ;Hyphen-Minus

//      0x007f   6   29    2   2   0  ;Delete

const BYTE 
HighCharHelper::HighCharTable[]= {
    TRUE,     /* 0x0, 0x0 */
    TRUE, /* 0x1, .*/
    TRUE, /* 0x2, .*/
    TRUE, /* 0x3, .*/
    TRUE, /* 0x4, .*/
    TRUE, /* 0x5, .*/
    TRUE, /* 0x6, .*/
    TRUE, /* 0x7, .*/
    TRUE, /* 0x8, .*/
    FALSE, /* 0x9,   */
#ifdef PLATFORM_UNIX
    TRUE, /* 0xA,  */
#else    
    FALSE, /* 0xA,  */
#endif // PLATFORM_UNIX
    FALSE, /* 0xB, .*/
    FALSE, /* 0xC, .*/
#ifdef PLATFORM_UNIX
    TRUE, /* 0xD,  */
#else    
    FALSE, /* 0xD,  */
#endif // PLATFORM_UNIX
    TRUE, /* 0xE, .*/
    TRUE, /* 0xF, .*/
    TRUE, /* 0x10, .*/
    TRUE, /* 0x11, .*/
    TRUE, /* 0x12, .*/
    TRUE, /* 0x13, .*/
    TRUE, /* 0x14, .*/
    TRUE, /* 0x15, .*/
    TRUE, /* 0x16, .*/
    TRUE, /* 0x17, .*/
    TRUE, /* 0x18, .*/
    TRUE, /* 0x19, .*/
    TRUE, /* 0x1A, */
    TRUE, /* 0x1B, .*/
    TRUE, /* 0x1C, .*/
    TRUE, /* 0x1D, .*/
    TRUE, /* 0x1E, .*/
    TRUE, /* 0x1F, .*/
    FALSE, /*0x20,  */
    FALSE, /*0x21, !*/
    FALSE, /*0x22, "*/
    FALSE, /*0x23,  #*/
    FALSE, /*0x24,  $*/
    FALSE, /*0x25,  %*/
    FALSE, /*0x26,  &*/
    TRUE,  /*0x27, '*/
    FALSE, /*0x28, (*/
    FALSE, /*0x29, )*/
    FALSE, /*0x2A **/
    FALSE, /*0x2B, +*/
    FALSE, /*0x2C, ,*/
    TRUE,  /*0x2D, -*/
    FALSE, /*0x2E, .*/
    FALSE, /*0x2F, /*/
    FALSE, /*0x30, 0*/
    FALSE, /*0x31, 1*/
    FALSE, /*0x32, 2*/
    FALSE, /*0x33, 3*/
    FALSE, /*0x34, 4*/
    FALSE, /*0x35, 5*/
    FALSE, /*0x36, 6*/
    FALSE, /*0x37, 7*/
    FALSE, /*0x38, 8*/
    FALSE, /*0x39, 9*/
    FALSE, /*0x3A, :*/
    FALSE, /*0x3B, ;*/
    FALSE, /*0x3C, <*/
    FALSE, /*0x3D, =*/
    FALSE, /*0x3E, >*/
    FALSE, /*0x3F, ?*/
    FALSE, /*0x40, @*/
    FALSE, /*0x41, A*/
    FALSE, /*0x42, B*/
    FALSE, /*0x43, C*/
    FALSE, /*0x44, D*/
    FALSE, /*0x45, E*/
    FALSE, /*0x46, F*/
    FALSE, /*0x47, G*/
    FALSE, /*0x48, H*/
    FALSE, /*0x49, I*/
    FALSE, /*0x4A, J*/
    FALSE, /*0x4B, K*/
    FALSE, /*0x4C, L*/
    FALSE, /*0x4D, M*/
    FALSE, /*0x4E, N*/
    FALSE, /*0x4F, O*/
    FALSE, /*0x50, P*/
    FALSE, /*0x51, Q*/
    FALSE, /*0x52, R*/
    FALSE, /*0x53, S*/
    FALSE, /*0x54, T*/
    FALSE, /*0x55, U*/
    FALSE, /*0x56, V*/
    FALSE, /*0x57, W*/
    FALSE, /*0x58, X*/
    FALSE, /*0x59, Y*/
    FALSE, /*0x5A, Z*/
    FALSE, /*0x5B, [*/
    FALSE, /*0x5C, \*/
    FALSE, /*0x5D, ]*/
    FALSE, /*0x5E, ^*/
    FALSE, /*0x5F, _*/
    FALSE, /*0x60, `*/
    FALSE, /*0x61, a*/
    FALSE, /*0x62, b*/
    FALSE, /*0x63, c*/
    FALSE, /*0x64, d*/
    FALSE, /*0x65, e*/
    FALSE, /*0x66, f*/
    FALSE, /*0x67, g*/
    FALSE, /*0x68, h*/
    FALSE, /*0x69, i*/
    FALSE, /*0x6A, j*/
    FALSE, /*0x6B, k*/
    FALSE, /*0x6C, l*/
    FALSE, /*0x6D, m*/
    FALSE, /*0x6E, n*/
    FALSE, /*0x6F, o*/
    FALSE, /*0x70, p*/
    FALSE, /*0x71, q*/
    FALSE, /*0x72, r*/
    FALSE, /*0x73, s*/
    FALSE, /*0x74, t*/
    FALSE, /*0x75, u*/
    FALSE, /*0x76, v*/
    FALSE, /*0x77, w*/
    FALSE, /*0x78, x*/
    FALSE, /*0x79, y*/
    FALSE, /*0x7A, z*/
    FALSE, /*0x7B, {*/
    FALSE, /*0x7C, |*/
    FALSE, /*0x7D, }*/
    FALSE, /*0x7E, ~*/
    TRUE, /*0x7F, */
};  // HighCharHelper::HighCharTable
