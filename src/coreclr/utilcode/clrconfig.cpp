// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CLRConfig.cpp
//

#include "stdafx.h"
#include "clrconfig.h"
#include "sstring.h"
#include "ex.h"

#include "clrconfignocache.h"

using ConfigDWORDInfo = CLRConfig::ConfigDWORDInfo;
using ConfigStringInfo = CLRConfig::ConfigStringInfo;
using LookupOptions = CLRConfig::LookupOptions;

namespace
{
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
        bool MayContain(LPCWSTR name) const
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

    bool s_fUseEnvCache = false;
    ProbabilisticNameSet s_EnvNames; // set of environment value names seen

    bool EnvCacheValueNameSeenPerhaps(LPCWSTR name)
    {
        WRAPPER_NO_CONTRACT;

        return !s_fUseEnvCache
            || s_EnvNames.MayContain(name);
    }

    bool CheckLookupOption(const ConfigDWORDInfo & info, LookupOptions option)
    {
        LIMITED_METHOD_CONTRACT;
        return ((info.options & option) == option);
    }

    bool CheckLookupOption(const ConfigStringInfo & info, LookupOptions option)
    {
        LIMITED_METHOD_CONTRACT;
        return ((info.options & option) == option);
    }

    bool CheckLookupOption(LookupOptions infoOptions, LookupOptions optionToCheck)
    {
        LIMITED_METHOD_CONTRACT;
        return ((infoOptions & optionToCheck) == optionToCheck);
    }

    //*****************************************************************************
    // Reads from the environment setting
    //*****************************************************************************
    LPWSTR EnvGetString(
        LPCWSTR name,
        LookupOptions options)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
            CANNOT_TAKE_LOCK;
        }
        CONTRACTL_END;

        WCHAR buff[64];
        const WCHAR* fallbackPrefix = NULL;
        const size_t namelen = u16_strlen(name);

        bool noPrefix = CheckLookupOption(options, LookupOptions::DontPrependPrefix);
        if (noPrefix)
        {
            if (namelen >= ARRAY_SIZE(buff))
            {
                _ASSERTE(!"Environment variable name too long.");
                return NULL;
            }

            *buff = W('\0');
        }
        else
        {
            bool dotnetValid = namelen < (size_t)(STRING_LENGTH(buff) - LEN_OF_DOTNET_PREFIX);
            bool complusValid = namelen < (size_t)(STRING_LENGTH(buff) - LEN_OF_COMPLUS_PREFIX);
            if(!dotnetValid || !complusValid)
            {
                _ASSERTE(!"Environment variable name too long.");
                return NULL;
            }

            // Check if the name has been cached.
            if (!EnvCacheValueNameSeenPerhaps(name))
                return NULL;

            // Priority order is DOTNET_ and then COMPlus_.
            wcscpy_s(buff, ARRAY_SIZE(buff), DOTNET_PREFIX);
            fallbackPrefix = COMPLUS_PREFIX;
        }

        wcscat_s(buff, ARRAY_SIZE(buff), name);

        FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

        NewArrayHolder<WCHAR> ret = NULL;
        HRESULT hr = S_OK;
        EX_TRY
        {
            PathString temp;

            DWORD len = WszGetEnvironmentVariable(buff, temp);
            if (len == 0 && fallbackPrefix != NULL)
            {
                wcscpy_s(buff, ARRAY_SIZE(buff), fallbackPrefix);
                wcscat_s(buff, ARRAY_SIZE(buff), name);
                len = WszGetEnvironmentVariable(buff, temp);
            }

            if (len != 0)
            {
                ret = temp.GetCopyOfUnicodeString();

#if defined(DEBUG) && !defined(SELF_NO_HOST)
                // Validate the cache and no-cache logic result in the same answer
                SString nameToConvert(name);

                CLRConfigNoCache nonCache = CLRConfigNoCache::Get(nameToConvert.GetUTF8(), noPrefix);
                LPCSTR valueNoCache = nonCache.AsString();

                _ASSERTE(SString::_stricmp(valueNoCache, temp.GetUTF8()) == 0);
#endif // defined(DEBUG) && !defined(SELF_NO_HOST)
            }
        }
        EX_CATCH_HRESULT(hr);

        if (hr != S_OK)
        {
            SetLastError(hr);
        }

        if(ret != NULL)
            return ret.Extract();

        return NULL;
    }

    HRESULT GetConfigDWORD(
        LPCWSTR name,
        DWORD defValue,
        _Out_ DWORD *result,
        LookupOptions options)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
            CANNOT_TAKE_LOCK;
        }
        CONTRACTL_END;

        SUPPORTS_DAC_HOST_ONLY;

        FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

        int radix = CheckLookupOption(options, LookupOptions::ParseIntegerAsBase10)
            ? 10
            : 16; // Parse as hex by default.

        NewArrayHolder<WCHAR> val = EnvGetString(name, options);
        if (val != NULL)
        {
            errno = 0;
            LPWSTR endPtr;
            DWORD configMaybe = u16_strtoul(val, &endPtr, radix);
            BOOL fSuccess = ((errno != ERANGE) && (endPtr != val));
            if (fSuccess)
            {
                *result = configMaybe;
                return (S_OK);
            }
        }

        *result = defValue;
        return (E_FAIL);
    }

    LPWSTR GetConfigString(
        LPCWSTR name,
        LookupOptions options)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
        }
        CONTRACTL_END;

        NewArrayHolder<WCHAR> ret(NULL);

        FAULT_NOT_FATAL(); // We don't report OOM errors here, we return a default value.

        ret = EnvGetString(name, options);
        if (ret != NULL)
        {
            if (*ret != W('\0'))
            {
                ret.SuppressRelease();
                return(ret);
            }
            ret.Clear();
        }

        return NULL;
    }

    //---------------------------------------------------------------------------------------
    //
    // Given an input string, returns a newly-allocated string equal to the input but with
    // leading and trailing whitespace trimmed off. If input is already trimmed, or if
    // trimming would result in an empty string, this function sets the output string to NULL
    //
    // Caller must free *pwszTrimmed if non-NULL
    //
    // Arguments:
    //      * wszOrig - String to trim
    //      * pwszTrimmed - [out]: On return, points to newly allocated, trimmed string (or
    //          NULL)
    //
    // Return Value:
    //     HRESULT indicating success or failure.
    //
    HRESULT TrimWhiteSpace(LPCWSTR wszOrig, _Outptr_result_z_ LPWSTR * pwszTrimmed)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(wszOrig != NULL);
        _ASSERTE(pwszTrimmed != NULL);

        // In case we return early, set [out] to NULL by default
        *pwszTrimmed = NULL;

        // Get pointers into internal string that show where to do the trimming.
        size_t cchOrig = u16_strlen(wszOrig);
        if (!FitsIn<DWORD>(cchOrig))
            return COR_E_OVERFLOW;
        DWORD cchAfterTrim = (DWORD) cchOrig;
        LPCWSTR wszAfterTrim = wszOrig;
        ::TrimWhiteSpace(&wszAfterTrim, &cchAfterTrim);

        // Is input string already trimmed?  If so, save an allocation and just return.
        if ((wszOrig == wszAfterTrim) && (cchOrig == cchAfterTrim))
        {
            // Yup, just return success
            return S_OK;
        }

        if (cchAfterTrim == 0)
        {
            // After trimming, there's nothing left, so just return NULL
            return S_OK;
        }

        // Create a new buffer to hold a copy of the trimmed string.  Caller will be
        // responsible for this buffer if we return it.
        NewArrayHolder<WCHAR> wszTrimmedCopy(new (nothrow) WCHAR[cchAfterTrim + 1]);
        if (wszTrimmedCopy == NULL)
        {
            return E_OUTOFMEMORY;
        }

        errno_t err = wcsncpy_s(wszTrimmedCopy, cchAfterTrim + 1, wszAfterTrim, cchAfterTrim);
        if (err != 0)
        {
            return E_FAIL;
        }

        // Successfully made a copy of the trimmed string.  Return it. Caller will be responsible for
        // deleting it.
        wszTrimmedCopy.SuppressRelease();
        *pwszTrimmed = wszTrimmedCopy;
        return S_OK;
    }
}

//
// Creating structs using the macro table in CLRConfigValues.h
//

// These macros initialize ConfigDWORDInfo structs.
#define RETAIL_CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
    const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, CLRConfig::LookupOptions::Default};
#define RETAIL_CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
    const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, lookupOptions};

// These macros initialize ConfigStringInfo structs.
#define RETAIL_CONFIG_STRING_INFO(symbol, name, description) \
    const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, CLRConfig::LookupOptions::Default};
#define RETAIL_CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
    const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, lookupOptions};
//
// Debug versions of the macros
//
#ifdef _DEBUG
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
        const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, CLRConfig::LookupOptions::Default};
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
        const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, lookupOptions};
    #define CONFIG_STRING_INFO(symbol, name, description) \
        const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, CLRConfig::LookupOptions::Default};
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
        const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, lookupOptions};
#else
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description)
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions)
    #define CONFIG_STRING_INFO(symbol, name, description)
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions)
#endif // _DEBUG

    // Now that we have defined what what the macros in file:../inc/CLRConfigValues.h mean, include it to generate the code.
    #include "clrconfigvalues.h"

#undef RETAIL_CONFIG_DWORD_INFO
#undef RETAIL_CONFIG_STRING_INFO
#undef RETAIL_CONFIG_DWORD_INFO_EX
#undef RETAIL_CONFIG_STRING_INFO_EX
#undef CONFIG_DWORD_INFO
#undef CONFIG_STRING_INFO
#undef CONFIG_DWORD_INFO_EX
#undef CONFIG_STRING_INFO_EX

BOOL CLRConfig::IsConfigEnabled(const ConfigDWORDInfo & info)
{
    WRAPPER_NO_CONTRACT;

    return IsConfigOptionSpecified(info.name);
}

//
// Look up a DWORD config value.
//
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details.
//     * isDefault - the value was not set or had an invalid format so the default was returned.
//     * result - the result.
//
// Return value:
//     * true for success, false otherwise.
//
// static
DWORD CLRConfig::GetConfigValue(const ConfigDWORDInfo & info, /* [Out] */ bool *isDefault)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    _ASSERTE (isDefault != nullptr);

    DWORD resultMaybe;
    HRESULT hr = GetConfigDWORD(info.name, info.defaultValue, &resultMaybe, info.options);
    if (SUCCEEDED(hr))
    {
        *isDefault = false;
        return resultMaybe;
    }

    *isDefault = true;
    return info.defaultValue;
}

//
// Look up a DWORD config value.
//
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details
//
// static
DWORD CLRConfig::GetConfigValue(const ConfigDWORDInfo & info, DWORD defaultValue)
{
    bool isDefault = false;
    DWORD valueMaybe = GetConfigValue(info, &isDefault);

    // If the default value was returned, defer to the user supplied version.
    if (isDefault)
        return defaultValue;

    return valueMaybe;
}

//
// Look up a DWORD config value.
//
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details
//
// static
DWORD CLRConfig::GetConfigValue(const ConfigDWORDInfo & info)
{
    WRAPPER_NO_CONTRACT;

    bool unused;
    return GetConfigValue(info, &unused);
}

//
// Look up a String config value.
//
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details
//
// Return value:
//     * Pointer to the string value, if found. You own the string that's returned. Returns NULL if the value
//         is not found.
//
// static
LPWSTR CLRConfig::GetConfigValue(const ConfigStringInfo & info)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    LPWSTR result = NULL;

    // TODO: We swallow OOM exception here. Is this OK?
    FAULT_NOT_FATAL();

    // If this fails, result will stay NULL.
    GetConfigValue(info, &result);

    return result;
}

//
// Look up a string config value, passing it out through a pointer reference.
//
// Return value:
//     * Reports out of memory errors (HRESULT E_OUTOFMEMORY).
//
// Arguments:
//     * info - see file:../inc/CLRConfig.h for details
//     * outVal - Set to the result string. You own the string that's returned. Set to NULL if the value is
//         not found.
//
// static
HRESULT CLRConfig::GetConfigValue(const ConfigStringInfo & info, _Outptr_result_z_ LPWSTR * outVal)
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT (CONTRACT_RETURN E_OUTOFMEMORY);
        POSTCONDITION(CheckPointer(outVal, NULL_OK)); // TODO: Should this check be *outVal instead of outVal?
    } CONTRACT_END;

    LPWSTR result = NULL;

    result = GetConfigString(info.name, info.options);
    if ((result != NULL) && CheckLookupOption(info, LookupOptions::TrimWhiteSpaceFromStringValue))
    {
        // If this fails, result remains untouched, so we'll just return the untrimmed
        // value.
        LPWSTR wszTrimmedResult = NULL;
        if (SUCCEEDED(TrimWhiteSpace(result, &wszTrimmedResult)) &&
            (wszTrimmedResult != NULL))
        {
            // wszTrimmedResult should be the result we return.  Delete the untrimmed
            // result.
            delete [] result;
            result = wszTrimmedResult;
        }
    }

    *outVal = result;
    RETURN S_OK;
}

//
// Check whether an option is specified (e.g. explicitly listed) in any location
//
// Arguments:
//     * name - the name field of the desired ConfigDWORDInfo/ConfigStringInfo
//
// static
BOOL CLRConfig::IsConfigOptionSpecified(LPCWSTR name)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    {
        LPWSTR result = NULL;

        result = GetConfigString(name, LookupOptions::Default);
        if (result != NULL)
        {
            FreeConfigString(result);
            return TRUE;
        }

        result = GetConfigString(name, LookupOptions::DontPrependPrefix);
        if (result != NULL)
        {
            FreeConfigString(result);
            return TRUE;
        }
    }

    return FALSE;
}

//
// Deallocation function for code:CLRConfig::FreeConfigString
//
// static
void CLRConfig::FreeConfigString(_In_z_ LPWSTR str)
{
    LIMITED_METHOD_CONTRACT;

    // See EnvGetString().
    delete [] str;
}

//
// Initialize the internal cache for faster lookup.
//
// static
void CLRConfig::Initialize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Check if caching is disabled.
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DisableConfigCache) != 0)
        return;

    const WCHAR prefixC = (WCHAR)towlower(COMPLUS_PREFIX[0]);
    const WCHAR prefixD = (WCHAR)towlower(DOTNET_PREFIX[0]);

    // Create a cache of environment variables
    WCHAR* wszStrings = GetEnvironmentStringsW();
    if (wszStrings != NULL)
    {
        // GetEnvironmentStrings returns pointer to a null terminated block containing
        // null terminated strings
        for(WCHAR *wszCurr = wszStrings; *wszCurr; wszCurr++)
        {
            WCHAR wch = (WCHAR)towlower(*wszCurr);

            // Lets only cache env variables with targeted prefixes
            bool matchC = wch == prefixC;
            bool matchD = wch == prefixD;
            if (matchC || matchD)
            {
                WCHAR *wszName = wszCurr;

                // Look for the separator between name and value
                while (*wszCurr && *wszCurr != W('='))
                    wszCurr++;

                if (*wszCurr == W('='))
                {
                    // Check the prefix
                    if(matchC
                        && SString::_wcsnicmp(wszName, COMPLUS_PREFIX, LEN_OF_COMPLUS_PREFIX) == 0)
                    {
                        wszName += LEN_OF_COMPLUS_PREFIX;
                        s_EnvNames.Add(wszName, (DWORD) (wszCurr - wszName));
                    }
                    else if (matchD
                        && SString::_wcsnicmp(wszName, DOTNET_PREFIX, LEN_OF_DOTNET_PREFIX) == 0)
                    {
                        wszName += LEN_OF_DOTNET_PREFIX;
                        s_EnvNames.Add(wszName, (DWORD) (wszCurr - wszName));
                    }
                }
            }

            // Look for current string termination
            while (*wszCurr)
                wszCurr++;
        }

        FreeEnvironmentStringsW(wszStrings);
        s_fUseEnvCache = true;
    }
}
