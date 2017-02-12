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
    if (WCHAR * wszStrings = WszGetEnvironmentStrings())
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
