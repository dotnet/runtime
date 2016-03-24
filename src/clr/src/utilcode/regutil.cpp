// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// regutil.cpp
//

//
// This module contains a set of functions that can be used to access the
// registry.
//
//*****************************************************************************


#include "stdafx.h"
#include "utilcode.h"
#include "mscoree.h"
#include "sstring.h"
#include "ex.h"

#define COMPLUS_PREFIX W("COMPlus_")
#define LEN_OF_COMPLUS_PREFIX 8

#if (!defined(FEATURE_UTILCODE_NO_DEPENDENCIES) || defined(DEBUG)) && !defined(FEATURE_PAL)
#define ALLOW_REGISTRY
#endif

#undef WszRegCreateKeyEx
#undef WszRegOpenKeyEx
#undef WszRegOpenKey
#define WszRegCreateKeyEx RegCreateKeyExW
#define WszRegOpenKeyEx RegOpenKeyExW
#define WszRegOpenKey(hKey, wszSubKey, phkRes) RegOpenKeyExW(hKey, wszSubKey, 0, KEY_ALL_ACCESS, phkRes)

//*****************************************************************************
// Reads from the environment setting
//*****************************************************************************
LPWSTR REGUTIL::EnvGetString(LPCWSTR name, BOOL fPrependCOMPLUS)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
    
    WCHAR buff[64];
    
    if(wcslen(name) > (size_t)(64 - 1 - (fPrependCOMPLUS ? LEN_OF_COMPLUS_PREFIX : 0)))
    {
        return NULL;
    }

#ifdef ALLOW_REGISTRY
    if (fPrependCOMPLUS)
    {
        if (!EnvCacheValueNameSeenPerhaps(name))
            return NULL;
    }
#endif // ALLOW_REGISTRY

    if (fPrependCOMPLUS)
    {
        wcscpy_s(buff, _countof(buff), COMPLUS_PREFIX);
    }
    else
    {
        *buff = 0;
    }

    wcscat_s(buff, _countof(buff), name);

    FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

   
    NewArrayHolder<WCHAR> ret = NULL;
    HRESULT hr = S_OK;
    DWORD Len;
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(SetLastError(COR_E_STACKOVERFLOW); return NULL;)
    EX_TRY
    { 
        PathString temp;

        Len = WszGetEnvironmentVariable(buff, temp);
        if (Len != 0)
        {
            ret = temp.GetCopyOfUnicodeString();
        }    
            
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE

    if (hr != S_OK)
    {
        SetLastError(hr);
    }
       
    if(ret != NULL)
    {
        return ret.Extract();
    }
        
    return NULL;
        
   
}

#ifdef ALLOW_REGISTRY

#ifndef FEATURE_CORECLR

//*****************************************************************************
// Gives the use the ability to turn on/off REGUTIL's ability to read from
// the registry. This is useful in mscoree.dll on startup, in order to avoid
// loading advapi32 and rpcrt4 until we're ready for them
//*****************************************************************************
void REGUTIL::AllowRegistryUse(BOOL fAllowUse)
{
    s_fUseRegistry = fAllowUse;
}// AllowRegistryUse

#endif // !FEATURE_CORECLR

#endif // ALLOW_REGISTRY


BOOL REGUTIL::UseRegistry()
{
#if !defined(ALLOW_REGISTRY)
    return TRUE;
#else
    return s_fUseRegistry;
#endif
}// UseRegistry

//*****************************************************************************
// Reads a DWORD from the COR configuration according to the level specified
// Returns back defValue if the key cannot be found
//*****************************************************************************
DWORD REGUTIL::GetConfigDWORD_DontUse_(LPCWSTR name, DWORD defValue, CORConfigLevel level, BOOL fPrependCOMPLUS)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    SUPPORTS_DAC_HOST_ONLY;
    
    ULONGLONG result;
    GetConfigInteger(name, defValue, &result, TRUE, level, fPrependCOMPLUS);

    return (DWORD)result;
}

#define uniwcst(val, endptr, base) (fGetDWORD ? wcstoul(val, endptr, base) : _wcstoui64(val, endptr, base))

// 
// Look up a dword config value, and write the result to the DWORD passed in by reference.
// 
// Return value:
//     * E_FAIL if the value is not found. (result is assigned the default value)
//     * S_OK if the value is found. (result is assigned the value that was found)
//     
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details
//     * result - Pointer to the output DWORD.
// 
// static
HRESULT REGUTIL::GetConfigDWORD_DontUse_(LPCWSTR name, DWORD defValue, __out DWORD * result, CORConfigLevel level, BOOL fPrependCOMPLUS)
{
    ULONGLONG ullResult;
    HRESULT hr = GetConfigInteger(name, defValue, &ullResult, TRUE, level, fPrependCOMPLUS);
    *result = (DWORD)ullResult;
    return hr;
}

ULONGLONG REGUTIL::GetConfigULONGLONG_DontUse_(LPCWSTR name, ULONGLONG defValue, CORConfigLevel level, BOOL fPrependCOMPLUS)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    SUPPORTS_DAC_HOST_ONLY;
    
    ULONGLONG result;
    GetConfigInteger(name, defValue, &result, FALSE, level, fPrependCOMPLUS);

    return result;
}

// This function should really be refactored to return the string from the environment and let the caller decide
// what to convert it to; and return the buffer read from the reg call.
// Note for PAL: right now PAL does not have a _wcstoui64 API, so I am temporarily reading in all numbers as 
// a 32-bit number. When we have the _wcstoui64 API on MAC we will use uniwcst instead of wcstoul.
HRESULT REGUTIL::GetConfigInteger(LPCWSTR name, ULONGLONG defValue, __out ULONGLONG * result, BOOL fGetDWORD, CORConfigLevel level, BOOL fPrependCOMPLUS)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    SUPPORTS_DAC_HOST_ONLY;
    
    ULONGLONG rtn;
    ULONGLONG ret = 0;
    DWORD type = 0;
    HKEY userKey;
    HKEY machineKey;
    DWORD size = 4;

    FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

    if (level & COR_CONFIG_ENV)
    {
        WCHAR* val = EnvGetString(name, fPrependCOMPLUS);  // try getting it from the environement first
        if (val != 0) {
            errno = 0;
            LPWSTR endPtr;
            rtn = uniwcst(val, &endPtr, 16);        // treat it has hex
            BOOL fSuccess = ((errno != ERANGE) && (endPtr != val));
            delete[] val;

            if (fSuccess)                      // success
            {
                *result = rtn;
                return (S_OK);
            }
        }
    }

    // Early out if no registry access, simplifies following code.
    //
    if (!UseRegistry() || !(level & COR_CONFIG_REGISTRY))
    {
        *result = defValue;
        return (E_FAIL);
    }

#ifdef ALLOW_REGISTRY
    // Probe the config cache to see if there is any point
    // probing the registry; if not, don't bother.
    //
    if (!RegCacheValueNameSeenPerhaps(name))
    {
        *result = defValue;
        return (E_FAIL);
    }
#endif // ALLOW_REGISTRY

    if (level & COR_CONFIG_USER)
    {
#ifdef ALLOW_REGISTRY
        {
            LONG retVal = ERROR_SUCCESS;
            BOOL bCloseHandle = FALSE;
            userKey = s_hUserFrameworkKey;
            
            if (userKey == INVALID_HANDLE_VALUE)
            {
                retVal = WszRegOpenKeyEx(HKEY_CURRENT_USER, FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &userKey);
                bCloseHandle = TRUE;
            }

            if (retVal == ERROR_SUCCESS)
            {
                rtn = WszRegQueryValueEx(userKey, name, 0, &type, (LPBYTE)&ret, &size);

                if (bCloseHandle)
                    VERIFY(!RegCloseKey(userKey));
                
                if (rtn == ERROR_SUCCESS && (type == REG_DWORD || (!fGetDWORD && type == REG_QWORD)))
                {
                    *result = ret;
                    return (S_OK);
                }
            }
        }
#endif // ALLOW_REGISTRY
    }

    if (level & COR_CONFIG_MACHINE)
    {
#ifdef ALLOW_REGISTRY
        {
            LONG retVal = ERROR_SUCCESS;
            BOOL bCloseHandle = FALSE;
            machineKey = s_hMachineFrameworkKey;
            
            if (machineKey == INVALID_HANDLE_VALUE)
            {
                retVal = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &machineKey);
                bCloseHandle = TRUE;
            }

            if (retVal == ERROR_SUCCESS)
            {
                rtn = WszRegQueryValueEx(machineKey, name, 0, &type, (LPBYTE)&ret, &size);

                if (bCloseHandle)
                    VERIFY(!RegCloseKey(machineKey));
                
                if (rtn == ERROR_SUCCESS && (type == REG_DWORD || (!fGetDWORD && type == REG_QWORD)))
                {
                    *result = ret;
                    return (S_OK);
                }
            }
        }
#endif // ALLOW_REGISTRY
    }

    *result = defValue;
    return (E_FAIL);
}

#define FUSION_REGISTRY_KEY_W W("Software\\Microsoft\\Fusion")

//*****************************************************************************
// Reads a string from the COR configuration according to the level specified
// The caller is responsible for deallocating the returned string by 
// calling code:REGUTIL::FreeConfigString or using a code:ConfigStringHolder 
//*****************************************************************************

LPWSTR REGUTIL::GetConfigString_DontUse_(LPCWSTR name, BOOL fPrependCOMPLUS, CORConfigLevel level, BOOL fUsePerfCache)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;
    
#ifdef ALLOW_REGISTRY
    HRESULT lResult;
    RegKeyHolder userKey = NULL;
    RegKeyHolder machineKey = NULL;
    RegKeyHolder fusionKey = NULL;
    DWORD type;
    DWORD size;
#endif // ALLOW_REGISTRY
    NewArrayHolder<WCHAR> ret(NULL);

    FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

    if (level & COR_CONFIG_ENV)
    {
        ret = EnvGetString(name, fPrependCOMPLUS);  // try getting it from the environement first
        if (ret != 0) {
            if (*ret != 0) 
            {
                ret.SuppressRelease();
                return(ret);
            }
            ret.Clear();
        }
    }

    // Early out if no registry access, simplifies following code.
    //
    if (!UseRegistry() || !(level & COR_CONFIG_REGISTRY))
    {
        return(ret);
    }

#ifdef ALLOW_REGISTRY
    // Probe the config cache to see if there is any point
    // probing the registry; if not, don't bother.
    //
    if (fUsePerfCache && !RegCacheValueNameSeenPerhaps(name))
        return ret;
#endif // ALLOW_REGISTRY

    if (level & COR_CONFIG_USER)
    {
#ifdef ALLOW_REGISTRY
        BOOL    bUsingCachedKey = FALSE;
        
        if (s_hUserFrameworkKey != INVALID_HANDLE_VALUE)
        {
            bUsingCachedKey = TRUE;
            userKey = s_hUserFrameworkKey;
        }
            
        if (bUsingCachedKey || WszRegOpenKeyEx(HKEY_CURRENT_USER, FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &userKey) == ERROR_SUCCESS)
        {
            BOOL bReturn = FALSE;
            if (WszRegQueryValueEx(userKey, name, 0, &type, 0, &size) == ERROR_SUCCESS &&
                type == REG_SZ)
            {
                ret = (LPWSTR) new (nothrow) BYTE [size];
                if (ret)
                {
                    ret[0] = W('\0');
                    lResult = WszRegQueryValueEx(userKey, name, 0, 0, (LPBYTE) ret.GetValue(), &size);
                    _ASSERTE(lResult == ERROR_SUCCESS);
                    {
                        ret.SuppressRelease();
                    }
                }
                bReturn = TRUE;
            }

            if (bUsingCachedKey)
                userKey.SuppressRelease();

            if (bReturn)
                return  ret;
        }

#endif // ALLOW_REGISTRY
    }

    if (level & COR_CONFIG_MACHINE)
    {
#ifdef ALLOW_REGISTRY
        BOOL    bUsingCachedKey = FALSE;
        
        if (s_hMachineFrameworkKey != INVALID_HANDLE_VALUE)
        {
            bUsingCachedKey = TRUE;
            machineKey = s_hMachineFrameworkKey;
        }
            
        if (bUsingCachedKey || WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &machineKey) == ERROR_SUCCESS)
        {
            BOOL bReturn = FALSE;
            if (WszRegQueryValueEx(machineKey, name, 0, &type, 0, &size) == ERROR_SUCCESS &&
                type == REG_SZ)
            {
                ret = (LPWSTR) new (nothrow) BYTE [size];
                if (ret)
                {
                    ret[0] = W('\0');
                    lResult = WszRegQueryValueEx(machineKey, name, 0, 0, (LPBYTE) ret.GetValue(), &size);
                    _ASSERTE(lResult == ERROR_SUCCESS);
                    {
                        ret.SuppressRelease();
                    }
                }
                bReturn = TRUE;
            }

            if (bUsingCachedKey)
                machineKey.SuppressRelease();

            if (bReturn)
                return  ret;
        }

#endif // ALLOW_REGISTRY
    }

    if (level & COR_CONFIG_FUSION)
    {
#ifdef ALLOW_REGISTRY
        if ((WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, FUSION_REGISTRY_KEY_W, 0, KEY_READ, &fusionKey) == ERROR_SUCCESS) &&
            (WszRegQueryValueEx(fusionKey, name, 0, &type, 0, &size) == ERROR_SUCCESS) &&
            type == REG_SZ) 
        {
            ret = (LPWSTR) new (nothrow) BYTE [size];
            if (!ret)
            {
                return NULL;
            }
            ret[0] = W('\0');            
            lResult = WszRegQueryValueEx(fusionKey, name, 0, 0, (LPBYTE) ret.GetValue(), &size);
            _ASSERTE(lResult == ERROR_SUCCESS);
            ret.SuppressRelease();
            return(ret);
        }
#endif // ALLOW_REGISTRY
    }
    
    return NULL;
}

//*****************************************************************************
// Deallocation function for code:REGUTIL::GetConfigString_DontUse_ 
//
// Notes: 
//     Use a code:ConfigStringHolder to automatically call this.
//*****************************************************************************
void REGUTIL::FreeConfigString(__in_z LPWSTR str)
{
    LIMITED_METHOD_CONTRACT;
    
    delete [] str;
}

//*****************************************************************************
// Reads a BIT flag from the COR configuration according to the level specified
// Returns back defValue if the key cannot be found
//*****************************************************************************
DWORD REGUTIL::GetConfigFlag_DontUse_(LPCWSTR name, DWORD bitToSet, BOOL defValue)
{
    WRAPPER_NO_CONTRACT;
    
    return(GetConfigDWORD_DontUse_(name, defValue) != 0 ? bitToSet : 0);
}


#ifdef ALLOW_REGISTRY

#ifndef FEATURE_CORECLR

//*****************************************************************************
// Open's the given key and returns the value desired.  If the key or value is
// not found, then the default is returned.
//*****************************************************************************
long REGUTIL::GetLong(                  // Return value from registry or default.
    LPCTSTR     szName,                 // Name of value to get.
    long        iDefault,               // Default value to return if not found.
    LPCTSTR     szKey,                  // Name of key, NULL==default.
    HKEY        hKeyVal)                // What key to work on.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    long        iValue;                 // The value to read.
    DWORD       iType;                  // Type of value to get.
    DWORD       iSize;                  // Size of buffer.
    HKEY        hKey;                   // Key for the registry entry.

    _ASSERTE(UseRegistry());
    
    FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.
    
    // Open the key if it is there.
    if (ERROR_SUCCESS != WszRegOpenKeyEx(hKeyVal, (szKey) ? szKey : FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &hKey))
        return (iDefault);

    // Read the key value if found.
    iType = REG_DWORD;
    iSize = sizeof(long);
    if (ERROR_SUCCESS != WszRegQueryValueEx(hKey, szName, NULL, 
            &iType, (LPBYTE)&iValue, &iSize) || iType != REG_DWORD)
        iValue = iDefault;

    // We're done with the key now.
    VERIFY(!RegCloseKey(hKey));
    return (iValue);
}


// Opens or creates desired reg key, then writes iValue
//*****************************************************************************
long REGUTIL::SetOrCreateLong(          // Return value from registry or default.
    LPCTSTR     szName,                 // Name of value to get.
    long        iValue,                 // Value to set.
    LPCTSTR     szKey,                  // Name of key, NULL==default.
    HKEY        hKeyVal)                // What key to work on.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(UseRegistry());

    long        lRtn;                   // Return code.
    HKEY        hKey;                   // Key for the registry entry.


    // Open the key if it is there, else create it
    if (WszRegCreateKeyEx(hKeyVal,
                      (szKey) ? szKey : FRAMEWORK_REGISTRY_KEY_W,
                      0,
                      NULL,
                      REG_OPTION_NON_VOLATILE,
                      KEY_WRITE,
                      NULL,
                      &hKey,
                      NULL) != ERROR_SUCCESS)
    {
        return (-1);        
    }
                                                    
    // Read the key value if found.
    lRtn = WszRegSetValueEx(hKey, szName, NULL, REG_DWORD, (const BYTE *) &iValue, sizeof(DWORD));

    // We're done with the key now.
    VERIFY(!RegCloseKey(hKey));
    return (lRtn);
}


//*****************************************************************************
// Open's the given key and returns the value desired.  If the key or value is
// not found, then the default is returned.
//*****************************************************************************
long REGUTIL::SetLong(                  // Return value from registry or default.
    LPCTSTR     szName,                 // Name of value to get.
    long        iValue,                 // Value to set.
    LPCTSTR     szKey,                  // Name of key, NULL==default.
    HKEY        hKeyVal)                // What key to work on.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(UseRegistry());
    
    long        lRtn;                   // Return code.
    HKEY        hKey;                   // Key for the registry entry.

    // Open the key if it is there.
    if (ERROR_SUCCESS != WszRegOpenKey(hKeyVal, (szKey) ? szKey : FRAMEWORK_REGISTRY_KEY_W, &hKey))
        return (-1);

    // Read the key value if found.
    lRtn = WszRegSetValueEx(hKey, szName, NULL, REG_DWORD, (const BYTE *) &iValue, sizeof(DWORD));

    // We're done with the key now.
    VERIFY(!RegCloseKey(hKey));
    return (lRtn);
}

//*****************************************************************************
// Set an entry in the registry of the form:
// HKEY_CLASSES_ROOT\szKey\szSubkey = szValue.  If szSubkey or szValue are
// NULL, omit them from the above expression.
//*****************************************************************************
BOOL REGUTIL::SetKeyAndValue(           // TRUE or FALSE.
    LPCTSTR     szKey,                  // Name of the reg key to set.
    LPCTSTR     szSubkey,               // Optional subkey of szKey.
    LPCTSTR     szValue)                // Optional value for szKey\szSubkey.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(UseRegistry());

    size_t nLen = _tcslen(szKey) + 1;
    if (szSubkey)
        nLen += (_tcslen(szSubkey) + 1);

    NewArrayHolder<TCHAR> rcKey = new (nothrow) TCHAR[nLen]; // Buffer for the full key name.
    if (rcKey == NULL)
        return FALSE;

    HKEY hKey = NULL; // Handle to the new reg key.

    // Init the key with the base key name.
    _tcscpy_s(rcKey, nLen, szKey);

    // Append the subkey name (if there is one).
    if (szSubkey != NULL)
    {
        _tcscat_s(rcKey, nLen, _T("\\"));
        _tcscat_s(rcKey, nLen, szSubkey);
    }

    // Create the registration key.
    if (WszRegCreateKeyEx(HKEY_CLASSES_ROOT, rcKey, 0, NULL,
                        REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL,
                        &hKey, NULL) != ERROR_SUCCESS)
        return(FALSE);

    // Set the value (if there is one).
    if (szValue != NULL)
         if( WszRegSetValueEx(hKey, NULL, 0, REG_SZ, (BYTE *) szValue,
                         (Wszlstrlen(szValue)+1) * sizeof(TCHAR)) != ERROR_SUCCESS ) {
              VERIFY(!RegCloseKey(hKey));
              return(FALSE);
         }            
    	
    VERIFY(!RegCloseKey(hKey));
    return(TRUE);
}


//*****************************************************************************
// Delete an entry in the registry of the form:
// HKEY_CLASSES_ROOT\szKey\szSubkey.
//*****************************************************************************
LONG REGUTIL::DeleteKey(                // TRUE or FALSE.
    LPCTSTR     szKey,                  // Name of the reg key to set.
    LPCTSTR     szSubkey)               // Subkey of szKey.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(UseRegistry());

    size_t nLen = _tcslen(szKey) + 1;
    if (szSubkey)
        nLen += (_tcslen(szSubkey) + 1);

    NewArrayHolder<TCHAR> rcKey = new (nothrow) TCHAR[nLen]; // Buffer for the full key name.
    if (rcKey == NULL)
        return ERROR_NOT_ENOUGH_MEMORY;

    // Init the key with the base key name.
    _tcscpy_s(rcKey, nLen, szKey);

    // Append the subkey name (if there is one).
    if (szSubkey != NULL)
    {
        _tcscat_s(rcKey, nLen, _T("\\"));
        _tcscat_s(rcKey, nLen, szSubkey);
    }

    // Delete the registration key.
    return WszRegDeleteKey(HKEY_CLASSES_ROOT, rcKey);
}


//*****************************************************************************
// Open the key, create a new keyword and value pair under it.
//*****************************************************************************
BOOL REGUTIL::SetRegValue(              // Return status.
    LPCTSTR     szKeyName,              // Name of full key.
    LPCTSTR     szKeyword,              // Name of keyword.
    LPCTSTR     szValue)                // Value of keyword.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(UseRegistry());

    HKEY        hKey;                   // Handle to the new reg key.

    // Create the registration key.
    if (WszRegCreateKeyEx(HKEY_CLASSES_ROOT, szKeyName, 0, NULL,
                        REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL,
                        &hKey, NULL) != ERROR_SUCCESS)
        return (FALSE);

    // Set the value (if there is one).
    if (szValue != NULL)
        if( WszRegSetValueEx(hKey, szKeyword, 0, REG_SZ, (BYTE *)szValue, 
        	(Wszlstrlen(szValue)+1) * sizeof(TCHAR)) != ERROR_SUCCESS) {
              VERIFY(!RegCloseKey(hKey));
              return(FALSE);
        }                	

    VERIFY(!RegCloseKey(hKey));
    return (TRUE);
}


//*****************************************************************************
// Does standard registration of a CoClass with a progid.
//*****************************************************************************
HRESULT REGUTIL::RegisterCOMClass(      // Return code.
    REFCLSID    rclsid,                 // Class ID.
    LPCTSTR     szDesc,                 // Description of the class.
    LPCTSTR     szProgIDPrefix,         // Prefix for progid.
    int         iVersion,               // Version # for progid.
    LPCTSTR     szClassProgID,          // Class progid.
    LPCTSTR     szThreadingModel,       // What threading model to use.
    LPCTSTR     szModule,               // Path to class.
    HINSTANCE   hInst,                  // Handle to module being registered
    LPCTSTR     szAssemblyName,         // Optional Assembly,
    LPCTSTR     szVersion,              // Optional Runtime version (directory containing runtime)
    BOOL        fExternal,              // flag - External to mscoree.
    BOOL        fRelativePath)          // flag - Relative path in szModule 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       rcCLSID[256];           // CLSID\\szID.
    WCHAR       rcInproc[_MAX_PATH+64]; // CLSID\\InprocServer32
    WCHAR       rcProgID[256];          // szProgIDPrefix.szClassProgID
    WCHAR       rcIndProgID[256];       // rcProgID.iVersion
    WCHAR       rcShim[_MAX_PATH];
    HRESULT     hr;

    // Format the prog ID values.
    VERIFY(_snwprintf_s(rcIndProgID, _countof(rcIndProgID), _TRUNCATE, W("%s.%s"), szProgIDPrefix, szClassProgID));

    VERIFY(_snwprintf_s(rcProgID, _countof(rcProgID), _TRUNCATE, W("%s.%d"), rcIndProgID, iVersion));

    // Do the initial portion.
    if (FAILED(hr = RegisterClassBase(rclsid, szDesc, rcProgID, rcIndProgID, rcCLSID, NumItems(rcCLSID))))
        return (hr);
    
    VERIFY(_snwprintf_s(rcInproc, _countof(rcInproc), _TRUNCATE, W("%s\\%s"), rcCLSID, W("InprocServer32")));

    if (!fExternal){
        SetKeyAndValue(rcCLSID, W("InprocServer32"), szModule);
    }
    else{
        LPCTSTR pSep = szModule;
        if (!fRelativePath && szModule) {
            pSep = wcsrchr(szModule, W('\\'));
            if(pSep == NULL)
                pSep = szModule;
            else 
                pSep++;
        }        
        HMODULE hMod = WszLoadLibrary(W("mscoree.dll"));
        if (!hMod)
            return E_FAIL;
        
        DWORD ret;
        VERIFY(ret = WszGetModuleFileName(hMod, rcShim, NumItems(rcShim)));
        FreeLibrary(hMod);        
        if( !ret ) 
        	return E_FAIL;	       
        
        // Set the server path.
        SetKeyAndValue(rcCLSID, W("InprocServer32"), rcShim);
        if(pSep)
            SetKeyAndValue(rcCLSID, W("Server"), pSep);

        if(szAssemblyName) {
            SetRegValue(rcInproc, W("Assembly"), szAssemblyName);
            SetRegValue(rcInproc, W("Class"), rcIndProgID);
        }
    }

    // Set the runtime version, it needs to be passed in from the outside
    if(szVersion != NULL) {
        LPCTSTR pSep2 = NULL;
        LPTSTR pSep1 = const_cast<LPTSTR>(wcsrchr(szVersion, W('\\')));
        if(pSep1 != NULL) {
            *pSep1 = '\0';
            pSep2 = wcsrchr(szVersion, W('\\'));
            if (!pSep2)
                pSep2 = szVersion;
            else
                pSep2 = pSep2++;    // exclude '\\'
        }
        else 
            pSep2 = szVersion;

        size_t bufLen = wcslen(rcInproc)+wcslen(pSep2)+2;
        WCHAR* rcVersion = new (nothrow) WCHAR[bufLen];
        if(rcVersion==NULL)
            return (E_OUTOFMEMORY);
        wcscpy_s(rcVersion, bufLen, rcInproc);
        wcscat_s(rcVersion, bufLen, W("\\"));
        wcscat_s(rcVersion, bufLen, pSep2);
        SetRegValue(rcVersion, W("ImplementedInThisVersion"), W(""));
        delete[] rcVersion;

        if(pSep1 != NULL)
            *pSep1 = W('\\');
    }

    // Add the threading model information.
    SetRegValue(rcInproc, W("ThreadingModel"), szThreadingModel);
    return (S_OK);
}



//*****************************************************************************
// Does standard registration of a CoClass with a progid.
// NOTE: This is the non-side-by-side execution version.
//*****************************************************************************
HRESULT REGUTIL::RegisterCOMClass(      // Return code.
    REFCLSID    rclsid,                 // Class ID.
    LPCTSTR     szDesc,                 // Description of the class.
    LPCTSTR     szProgIDPrefix,         // Prefix for progid.
    int         iVersion,               // Version # for progid.
    LPCTSTR     szClassProgID,          // Class progid.
    LPCTSTR     szThreadingModel,       // What threading model to use.
    LPCTSTR     szModule,               // Path to class.
    BOOL        bInprocServer)          // Whether we register the server as inproc or local
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       rcCLSID[256];           // CLSID\\szID.
    WCHAR       rcInproc[_MAX_PATH+64]; // CLSID\\InprocServer32
    WCHAR       rcProgID[256];          // szProgIDPrefix.szClassProgID
    WCHAR       rcIndProgID[256];       // rcProgID.iVersion
    HRESULT     hr;

    // Format the prog ID values.
    VERIFY(_snwprintf_s(rcIndProgID, _countof(rcIndProgID), _TRUNCATE, W("%s.%s"), szProgIDPrefix, szClassProgID));

    VERIFY(_snwprintf_s(rcProgID, _countof(rcProgID), _TRUNCATE, W("%s.%d"), rcIndProgID, iVersion));

    // Do the initial portion.
    if (FAILED(hr = RegisterClassBase(rclsid, szDesc, rcProgID, rcIndProgID, rcCLSID, NumItems(rcCLSID))))
        return (hr);

    WCHAR *szServerType = bInprocServer ? W("InprocServer32") : W("LocalServer32");

    // Set the server path.
    SetKeyAndValue(rcCLSID, szServerType , szModule);

    // Add the threading model information.
    VERIFY(_snwprintf_s(rcInproc, _countof(rcInproc), _TRUNCATE, W("%s\\%s"), rcCLSID, szServerType));
   
    SetRegValue(rcInproc, W("ThreadingModel"), szThreadingModel);
    return (S_OK);
}



//*****************************************************************************
// Register the basics for a in proc server.
//*****************************************************************************
HRESULT REGUTIL::RegisterClassBase(     // Return code.
    REFCLSID    rclsid,                 // Class ID we are registering.
    LPCTSTR     szDesc,                 // Class description.
    LPCTSTR     szProgID,               // Class prog ID.
    LPCTSTR     szIndepProgID,          // Class version independant prog ID.
    __out_ecount(cchOutCLSID) LPTSTR      szOutCLSID,             // CLSID formatted in character form.
    DWORD      cchOutCLSID)           // Out CLS ID buffer size
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    TCHAR       szID[64];               // The class ID to register.

    // Create some base key strings.
    GuidToLPWSTR(rclsid, szID, NumItems(szID));

    size_t nLen = _tcslen(_T("CLSID\\")) + _tcslen( szID) + 1;
    if( cchOutCLSID < nLen ) 	
	return E_INVALIDARG;

    _tcscpy_s(szOutCLSID, cchOutCLSID, W("CLSID\\"));
    _tcscat_s(szOutCLSID, cchOutCLSID, szID);

    // Create ProgID keys.
    SetKeyAndValue(szProgID, NULL, szDesc);
    SetKeyAndValue(szProgID, W("CLSID"), szID);

    // Create VersionIndependentProgID keys.
    SetKeyAndValue(szIndepProgID, NULL, szDesc);
    SetKeyAndValue(szIndepProgID, W("CurVer"), szProgID);
    SetKeyAndValue(szIndepProgID, W("CLSID"), szID);

    // Create entries under CLSID.
    SetKeyAndValue(szOutCLSID, NULL, szDesc);
    SetKeyAndValue(szOutCLSID, W("ProgID"), szProgID);
    SetKeyAndValue(szOutCLSID, W("VersionIndependentProgID"), szIndepProgID);
    SetKeyAndValue(szOutCLSID, W("NotInsertable"), NULL);
    return (S_OK);
}



//*****************************************************************************
// Unregister the basic information in the system registry for a given object
// class.
//*****************************************************************************
HRESULT REGUTIL::UnregisterCOMClass(    // Return code.
    REFCLSID    rclsid,                 // Class ID we are registering.
    LPCTSTR     szProgIDPrefix,         // Prefix for progid.
    int         iVersion,               // Version # for progid.
    LPCTSTR     szClassProgID,          // Class progid.
    BOOL        fExternal)              // flag - External to mscoree.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       rcCLSID[64];            // CLSID\\szID.
    WCHAR       rcProgID[128];          // szProgIDPrefix.szClassProgID
    WCHAR       rcIndProgID[128];       // rcProgID.iVersion

    // Format the prog ID values.
    VERIFY(_snwprintf_s(rcProgID, _countof(rcProgID), _TRUNCATE, W("%s.%s"), szProgIDPrefix, szClassProgID));

    VERIFY(_snwprintf_s(rcIndProgID, _countof(rcIndProgID), _TRUNCATE, W("%s.%d"), rcProgID, iVersion));

    UnregisterClassBase(rclsid, rcProgID, rcIndProgID, rcCLSID, NumItems(rcCLSID));
    DeleteKey(rcCLSID, W("InprocServer32"));
    if (fExternal){
        DeleteKey(rcCLSID, W("Server"));
        DeleteKey(rcCLSID, W("Version"));
    }
    GuidToLPWSTR(rclsid, rcCLSID, NumItems(rcCLSID));
    DeleteKey(W("CLSID"), rcCLSID);
    return (S_OK);
}


//*****************************************************************************
// Unregister the basic information in the system registry for a given object
// class.
// NOTE: This is the non-side-by-side execution version.
//*****************************************************************************
HRESULT REGUTIL::UnregisterCOMClass(    // Return code.
    REFCLSID    rclsid,                 // Class ID we are registering.
    LPCTSTR     szProgIDPrefix,         // Prefix for progid.
    int         iVersion,               // Version # for progid.
    LPCTSTR     szClassProgID)          // Class progid.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       rcCLSID[64];            // CLSID\\szID.
    WCHAR       rcProgID[128];          // szProgIDPrefix.szClassProgID
    WCHAR       rcIndProgID[128];       // rcProgID.iVersion

    // Format the prog ID values.
    VERIFY(_snwprintf_s(rcProgID, _countof(rcProgID), _TRUNCATE, W("%s.%s"), szProgIDPrefix, szClassProgID));

    VERIFY(_snwprintf_s(rcIndProgID, _countof(rcIndProgID), _TRUNCATE, W("%s.%d"), rcProgID, iVersion));

    UnregisterClassBase(rclsid, rcProgID, rcIndProgID, rcCLSID, NumItems(rcCLSID));
    DeleteKey(rcCLSID, W("InprocServer32"));
    DeleteKey(rcCLSID, W("LocalServer32"));
    
    GuidToLPWSTR(rclsid, rcCLSID, NumItems(rcCLSID));
    DeleteKey(W("CLSID"), rcCLSID);
    return (S_OK);
}


//*****************************************************************************
// Delete the basic settings for an inproc server.
//*****************************************************************************
HRESULT REGUTIL::UnregisterClassBase(   // Return code.
    REFCLSID    rclsid,                 // Class ID we are registering.
    LPCTSTR     szProgID,               // Class prog ID.
    LPCTSTR     szIndepProgID,          // Class version independant prog ID.
    __out_ecount(cchOutCLSID) LPTSTR      szOutCLSID,             // Return formatted class ID here.
    DWORD      cchOutCLSID)           // Out CLS ID buffer size    
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    TCHAR       szID[64];               // The class ID to register.

    // Create some base key strings.
    GuidToLPWSTR(rclsid, szID, NumItems(szID));
    size_t nLen = _tcslen(_T("CLSID\\")) + _tcslen( szID) + 1;
    if( cchOutCLSID < nLen ) 	
	return E_INVALIDARG;

    _tcscpy_s(szOutCLSID, cchOutCLSID, W("CLSID\\"));
    _tcscat_s(szOutCLSID, cchOutCLSID, szID);

    // Delete the version independant prog ID settings.
    DeleteKey(szIndepProgID, W("CurVer"));
    DeleteKey(szIndepProgID, W("CLSID"));
    WszRegDeleteKey(HKEY_CLASSES_ROOT, szIndepProgID);

    // Delete the prog ID settings.
    DeleteKey(szProgID, W("CLSID"));
    WszRegDeleteKey(HKEY_CLASSES_ROOT, szProgID);

    // Delete the class ID settings.
    DeleteKey(szOutCLSID, W("ProgID"));
    DeleteKey(szOutCLSID, W("VersionIndependentProgID"));
    DeleteKey(szOutCLSID, W("NotInsertable"));
    WszRegDeleteKey(HKEY_CLASSES_ROOT, szOutCLSID);
    return (S_OK);
}


//*****************************************************************************
// Register a type library.
//*****************************************************************************
HRESULT REGUTIL::RegisterTypeLib(       // Return code.
    REFGUID     rtlbid,                 // TypeLib ID we are registering.
    int         iVersion,               // Typelib version.
    LPCTSTR     szDesc,                 // TypeLib description.
    LPCTSTR     szModule)               // Path to the typelib.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       szID[64];               // The typelib ID to register.
    WCHAR       szTLBID[256];           // TypeLib\\szID.
    WCHAR       szHelpDir[_MAX_PATH];
    WCHAR       szDrive[_MAX_DRIVE] = {0};
    WCHAR       szDir[_MAX_DIR] = {0};
    WCHAR       szVersion[64];
    LPWSTR      szTmp;

    // Create some base key strings.
    GuidToLPWSTR(rtlbid, szID, NumItems(szID));

    _tcscpy_s(szTLBID, _countof(szTLBID), W("TypeLib\\"));
    _tcscat_s(szTLBID, _countof(szTLBID), szID);

    VERIFY(_snwprintf_s(szVersion, _countof(szVersion), _TRUNCATE, W("%d.0"), iVersion));

    // Create Typelib keys.
    SetKeyAndValue(szTLBID, NULL, NULL);
    SetKeyAndValue(szTLBID, szVersion, szDesc);
    _tcscat_s(szTLBID, _countof(szTLBID), W("\\"));
    _tcscat_s(szTLBID, _countof(szTLBID), szVersion);
    SetKeyAndValue(szTLBID, W("0"), NULL);
    SetKeyAndValue(szTLBID, W("0\\win32"), szModule);
    SetKeyAndValue(szTLBID, W("FLAGS"), W("0"));
    SplitPath(szModule, szDrive, _MAX_DRIVE, szDir, _MAX_DIR, NULL, 0, NULL, 0);
    _tcscpy_s(szHelpDir, _countof(szHelpDir), szDrive);
    if ((szTmp = CharPrev(szDir, szDir + Wszlstrlen(szDir))) != NULL)
        *szTmp = '\0';
    _tcscat_s(szHelpDir, _countof(szHelpDir), szDir);
    SetKeyAndValue(szTLBID, W("HELPDIR"), szHelpDir);
    return (S_OK);
}


//*****************************************************************************
// Remove the registry keys for a type library.
//*****************************************************************************
HRESULT REGUTIL::UnregisterTypeLib(     // Return code.
    REFGUID     rtlbid,                 // TypeLib ID we are registering.
    int         iVersion)               // Typelib version.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR       szID[64];               // The typelib ID to register.
    WCHAR       szTLBID[256];           // TypeLib\\szID.
    WCHAR       szTLBVersion[256];      // TypeLib\\szID\\szVersion
    WCHAR       szVersion[64];

    // Create some base key strings.
    GuidToLPWSTR(rtlbid, szID, NumItems(szID));

    VERIFY(_snwprintf_s(szVersion, _countof(szVersion), _TRUNCATE, W("%d.0"), iVersion));

    _tcscpy_s(szTLBID, _countof(szTLBID), W("TypeLib\\"));
    _tcscat_s(szTLBID, _countof(szTLBID), szID);
    _tcscpy_s(szTLBVersion, _countof(szTLBVersion), szTLBID);
    _tcscat_s(szTLBVersion, _countof(szTLBVersion), W("\\"));
    _tcscat_s(szTLBVersion, _countof(szTLBVersion), szVersion);

    // Delete Typelib keys.
    DeleteKey(szTLBVersion, W("HELPDIR"));
    DeleteKey(szTLBVersion, W("FLAGS"));
    DeleteKey(szTLBVersion, W("0\\win32"));
    DeleteKey(szTLBVersion, W("0"));
    DeleteKey(szTLBID, szVersion);
    WszRegDeleteKey(HKEY_CLASSES_ROOT, szTLBID);
    return (0);
}

#endif // !FEATURE_CORECLR


//
// ProbabilisticNameSet:
//
//  (Used by ConfigCache, below.  If used elsewhere, might justify
//  promotion to a standalone header file.)
//
//  Represent a set of names in a small, fixed amount of storage.
//  We turn a name into a small integer, then add the integer to a bitvector.
//  An old trick we used in VC++4 minimal rebuild.
//
//  For best results, the number of elements should be a fraction of
//  the total number of bits in 'bits'.
//
// Note, only the const methods are thread-safe.
// Callers are responsible for providing their own synchronization when
// constructing and Add'ing names to the set.
//
class ProbabilisticNameSet {
public:
    ProbabilisticNameSet()
    {
        WRAPPER_NO_CONTRACT;

        memset(bits, 0, sizeof(bits));
    }

    // Add a name to the set.
    //
    void Add(LPCWSTR name)
    {
        WRAPPER_NO_CONTRACT;

        unsigned i, mask;
        GetBitIndex(name, 0, &i, &mask);
        bits[i] |= mask;
    }

    void Add(LPCWSTR name, DWORD count)
    {
        WRAPPER_NO_CONTRACT;

        unsigned i, mask;
        GetBitIndex(name, count, &i, &mask);
        bits[i] |= mask;
    }

    // Return TRUE if a name *may have* been added to the set;
    // return FALSE if the name *definitely* was NOT ever added to the set.
    //
    BOOL MayContain(LPCWSTR name) const
    {
        WRAPPER_NO_CONTRACT;

        unsigned i, mask;
        GetBitIndex(name, 0, &i, &mask);
        return !!(bits[i] & mask);
    }

private:
    static const unsigned cbitSet = 256U;
    static const unsigned cbitWord = 8U*sizeof(unsigned);
    unsigned bits[cbitSet/cbitWord];

    // Return the word index and bit mask corresponding to the bitvector member
    // addressed by the *case-insensitive* hash of the given name.
    //
    void GetBitIndex(LPCWSTR name, DWORD count, unsigned* pi, unsigned* pmask) const
    {
        LIMITED_METHOD_CONTRACT;
        unsigned hash;
        if (count > 0)
            hash = HashiStringNKnownLower80(name, count) % cbitSet;
        else
            hash = HashiStringKnownLower80(name) % cbitSet;
        *pi = hash / cbitWord;
        *pmask = (1U << (hash % cbitWord));
    }

};


// From the Win32 SDK docs:
//  Registry Element Size Limits
//  ...
//  The maximum size of a value name is as follows: 
//  Windows Server 2003 and Windows XP:  16,383 characters
//  Windows 2000:  260 ANSI characters or 16,383 Unicode characters.
//  Windows Me/98/95:  255 characters
// Despite that, we only cache value names of 80 characters or less --
// longer names don't make sense as configuration settings names.
//
static const unsigned cchRegValueNameMax = 80;

BOOL REGUTIL::s_fUseRegCache = FALSE;
BOOL REGUTIL::s_fUseEnvCache = FALSE;
HKEY REGUTIL::s_hMachineFrameworkKey = (HKEY) INVALID_HANDLE_VALUE;
HKEY REGUTIL::s_hUserFrameworkKey = (HKEY) INVALID_HANDLE_VALUE;
BOOL REGUTIL::s_fUseRegistry = TRUE;
static ProbabilisticNameSet regNames; // set of registry value names seen; should be
                                   // a static field of REGUTIL but I don't
                                   // want to expose ProbabilisticNameSet.
static ProbabilisticNameSet envNames; // set of environment value names seen; 

// "Registry Configuration Cache"
//
// Initialize the (optional) registry config cache.
//
// The purpose of the cache is to avoid hundreds of registry probes
// otherwise incurred by calls to GetConfigDWORD_DontUse_ and GetConfigString_DontUse_.
//
// We accomplish this by enumerating the relevant registry keys and
// remembering the extant value names; and then by avoiding probing
// for a name that was not seen in the enumeration (initialization) phase.
//
// It is optional in the sense that REGUTIL facilities like
// GetConfigDWORD_DontUse_ and GetConfigString_DontUse_ will work fine if the cache
// is never initialized; however, each config access then will hit
// the registry (typically multiple times to search HKCU and HKLM).
//
//
// Initialization: Enumerate these registry keys
//  HKCU Software\Microsoft\.NetFramework
//  HKLM Software\Microsoft\.NetFramework
// for value names, and "remember" them in the ProbalisticNameSet 'names'.
//
// If we ever find a reg value named DisableConfigCache under any of these
// three keys, the feature is disabled.
//
// This method is not thread-safe.  It should only be called once.
//
// Perf Optimization for VSWhidbey:113373.
//
void REGUTIL::InitOptionalConfigCache()
{ 
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    static const HKEY roots[] = { HKEY_CURRENT_USER,
                                  HKEY_LOCAL_MACHINE};

    LONG l = ERROR_SUCCESS; // general Win32 API error return code
    HKEY hkey = NULL;

    // No caching if the environment variable COMPlus_DisableConfigCache is set
    //
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DisableConfigCache) != 0)
        goto failure;

    // Enumerate each root
    //
    for (int i = 0; i < NumItems(roots); i++) {
        hkey = NULL; // defensive
        l = WszRegOpenKeyEx(roots[i], FRAMEWORK_REGISTRY_KEY_W, 0, KEY_READ, &hkey);
        if (l == ERROR_FILE_NOT_FOUND) {
            // That registry key is not present.
            // For example, installation with no HKCU\...\.NETFramework.
            // Should be OK to proceed.
            continue;
        }
#if defined(FEATURE_APPX) && !defined(DACCESS_COMPILE)
        else if (l == ERROR_ACCESS_DENIED && AppX::IsAppXProcess()) {
            // If we encounter access denied for the current key in AppX, ignore
            // the failure and continue to cache the rest.  Effectively this means
            // we are caching that key as containing no values, which is correct
            // because in the unlikely event there are values hiding underneath
            // later attempts to access them (open the key) would also hit access
            // denied and continue on probing other locations.
            continue;
        }
#endif // FEATURE_APPX && !DACCESS_COMPILE
        else if (l != ERROR_SUCCESS) {
            // Something else went wrong. To be safe, don't enable the cache.
            goto failure;
        }

        // Enumerate every value name under this key.
        //
        for (int j = 0; ; j++) {
            WCHAR wszValue[cchRegValueNameMax + 1];
            DWORD dwValueSize = NumItems(wszValue);
            l = WszRegEnumValue(hkey, j, wszValue, &dwValueSize,
                                NULL, NULL, NULL, NULL);

            if (l == ERROR_SUCCESS) {
                // Add value name to the names cache.
                regNames.Add(wszValue);
            }
            else if (l == ERROR_NO_MORE_ITEMS) {
                // Expected case: we've considered every value under this key.
                break;
            }
            else if ((l == ERROR_INSUFFICIENT_BUFFER || l == ERROR_MORE_DATA) &&
                     (dwValueSize > cchRegValueNameMax)) {
                // Name is too long.  That's OK, we don't cache such names.
                continue;
            }
#if defined(FEATURE_APPX) && !defined DACCESS_COMPILE
            else if (l == ERROR_ACCESS_DENIED && AppX::IsAppXProcess()) {
                // As above, ignore access denied in AppX and continue on trying to cache
                continue;
            }
#endif // FEATURE_APPX && !DACCESS_COMPILE
            else {
                // WszRegEnumValue failed OOM, or something else went wrong.
                // To be safe, don't enable the cache.
                goto failure;
            }
        }

        // Save the handles to framework regkeys so that future reads dont have to 
        // open it again
        if (roots[i] == HKEY_CURRENT_USER)
            s_hUserFrameworkKey = hkey;
        else if (roots[i] == HKEY_LOCAL_MACHINE)
            s_hMachineFrameworkKey = hkey;
        else
            RegCloseKey(hkey);
        
        hkey = NULL;
    }

    // Success. We've enumerated all value names under the roots;
    // enable the REGUTIL value name config cache.
    //
    s_fUseRegCache = TRUE;

    // Now create a cache of environment variables
    WCHAR * wszStrings = WszGetEnvironmentStrings();
    if (wszStrings)
    {
        // GetEnvironmentStrings returns pointer to a null terminated block containing
        // null terminated strings
        for(WCHAR *wszCurr = wszStrings; *wszCurr; wszCurr++)
        {
            WCHAR wch = towlower(*wszCurr);
            
            // Lets only cache env variables with the COMPlus prefix only
            if (wch == W('c'))
            {
                WCHAR *wszName = wszCurr;
                
                // Look for the separator between name and value
                while (*wszCurr && *wszCurr != W('='))
                    wszCurr++;

                if (*wszCurr == W('='))
                {
                    // Check the prefix
                    if(!SString::_wcsnicmp(wszName, COMPLUS_PREFIX, LEN_OF_COMPLUS_PREFIX))
                    {
                        wszName += LEN_OF_COMPLUS_PREFIX;
                        envNames.Add(wszName, (DWORD) (wszCurr - wszName));
                    }
                }
                
            }
            // Look for current string termination
            while (*wszCurr)
                wszCurr++;
        
        }

        WszFreeEnvironmentStrings(wszStrings);
        s_fUseEnvCache = TRUE;
        
    }
    return;

failure:
    if (hkey != NULL)
        RegCloseKey(hkey);
}

// Return TRUE if the registry value name was seen (or might have been seen)
// in the registry at cache initialization time;
// return FALSE if it definitely was not seen at startup.
//
// If not using the config cache, return TRUE always.
//
// Perf Optimization for VSWhidbey:113373.
//
BOOL REGUTIL::RegCacheValueNameSeenPerhaps(LPCWSTR name)
{
    WRAPPER_NO_CONTRACT;

    return !s_fUseRegCache
           || (wcslen(name) > cchRegValueNameMax)
           || regNames.MayContain(name);
}

BOOL REGUTIL::EnvCacheValueNameSeenPerhaps(LPCWSTR name)
{
    WRAPPER_NO_CONTRACT;

    return !s_fUseEnvCache
           || envNames.MayContain(name);
}

#endif // ALLOW_REGISTRY
