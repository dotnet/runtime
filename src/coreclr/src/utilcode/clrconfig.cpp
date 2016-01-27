// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// CLRConfig.cpp
// 

//
// Unified method of accessing configuration values from environment variables,
// registry and config file. See file:../inc/CLRConfigValues.h for details on how to add config values.
// 
//*****************************************************************************

#include "stdafx.h"
#include "clrconfig.h"

#ifndef ERANGE
#define ERANGE 34
#endif

//
// Initialize the EEConfig::GetConfiguration function pointer to NULL. If EEConfig isn't init'ed, this will
// stay NULL and CLRConfig will ignore config files.
//
CLRConfig::GetConfigValueFunction CLRConfig::s_GetConfigValueCallback = NULL;

//
// Initialize the PerformanceDefaults::LookupConfigValue function pointer to NULL. If not initialized, CLRConfig
// will ignore LookupOptions::MayHavePerformanceDefault.
//
CLRConfig::GetPerformanceDefaultValueFunction CLRConfig::s_GetPerformanceDefaultValueCallback = NULL;

#ifdef FEATURE_WIN_DB_APPCOMPAT
PFN_CptQuirkIsEnabled3 CLRConfig::s_IsQuirkEnabledCallback = NULL;
PFN_CptQuirkGetData2 CLRConfig::s_GetQuirkValueCallback    = NULL;
#endif

// 
// Creating structs using the macro table in CLRConfigValues.h
// 

// These macros intialize ConfigDWORDInfo structs.
#define RETAIL_CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
    const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, CLRConfig::EEConfig_default};
#define RETAIL_CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
    const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, lookupOptions};

// These macros intialize ConfigStringInfo structs.
#define RETAIL_CONFIG_STRING_INFO(symbol, name, description) \
    const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, CLRConfig::EEConfig_default};
#define RETAIL_CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
    const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, lookupOptions};

// TEMPORARY macros that intialize strings for config value accesses that haven't been moved over to
// CLRConfig yet. Once all accesses have been moved, these macros (and corresponding instantiations in
// file:../utilcode/CLRConfig.h) should be removed.
#define RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(symbol, name, description) \
    const LPCWSTR CLRConfig::symbol = name;
#define RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(symbol, name, description) \
    const LPCWSTR CLRConfig::symbol = name;
// 
// Debug versions of the macros
// 
#ifdef _DEBUG
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
        const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, CLRConfig::EEConfig_default};
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
        const CLRConfig::ConfigDWORDInfo CLRConfig::symbol = {name, defaultValue, lookupOptions};
    #define CONFIG_STRING_INFO(symbol, name, description) \
        const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, CLRConfig::EEConfig_default};
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
        const CLRConfig::ConfigStringInfo CLRConfig::symbol = {name, lookupOptions};
    #define CONFIG_DWORD_INFO_DIRECT_ACCESS(symbol, name, description) \
        const LPCWSTR CLRConfig::symbol = name;
    #define CONFIG_STRING_INFO_DIRECT_ACCESS(symbol, name, description) \
        const LPCWSTR CLRConfig::symbol = name;
#else
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description)
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions)
    #define CONFIG_STRING_INFO(symbol, name, description)
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions)
    #define CONFIG_DWORD_INFO_DIRECT_ACCESS(symbol, name, description)
    #define CONFIG_STRING_INFO_DIRECT_ACCESS(symbol, name, description)
#endif // _DEBUG
    
    // Now that we have defined what what the macros in file:../inc/CLRConfigValues.h mean, include it to generate the code.
    #include "clrconfigvalues.h"

#undef RETAIL_CONFIG_DWORD_INFO
#undef RETAIL_CONFIG_STRING_INFO
#undef RETAIL_CONFIG_DWORD_INFO_EX
#undef RETAIL_CONFIG_STRING_INFO_EX    
#undef RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS
#undef RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS
#undef CONFIG_DWORD_INFO
#undef CONFIG_STRING_INFO
#undef CONFIG_DWORD_INFO_EX
#undef CONFIG_STRING_INFO_EX    
#undef CONFIG_DWORD_INFO_DIRECT_ACCESS
#undef CONFIG_STRING_INFO_DIRECT_ACCESS


#ifdef FEATURE_WIN_DB_APPCOMPAT

#define MAX_QUIRK_LENGTH 60
#define WIN_DB_COMPONENT_NAME W("NETFX.")
#define WIN_DB_COMPONENT_NAME_LENGTH 6

// queries the DB if the quirk is enabled. If the quirk is enabled then it also gets the value associated with the quirk.
// pass in quirkData as NULL if the value is not required and only enabled/disabled is needed.
// Length of quirk cannot be greater than 60. If it is greater than 60 then this api returns E_FAIL.
HRESULT CLRConfig::getQuirkEnabledAndValueFromWinDB(LPCWSTR wszQuirkName, BOOL* isEnabled, CPT_QUIRK_DATA* quirkData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(isEnabled, NULL_NOT_OK));
    }
    CONTRACTL_END;

    if(wszQuirkName == NULL)
        return E_FAIL;

    WCHAR wszCompleteQuirkName[MAX_QUIRK_LENGTH + WIN_DB_COMPONENT_NAME_LENGTH + 1];
    WCHAR wszComponentName[] = WIN_DB_COMPONENT_NAME;
    size_t cchCompleteQuirkName = MAX_QUIRK_LENGTH + WIN_DB_COMPONENT_NAME_LENGTH + 1;

    _ASSERT(wcslen(wszComponentName) == WIN_DB_COMPONENT_NAME_LENGTH);
    
    size_t cchOrig = wcslen(wszQuirkName);
    if(cchOrig > MAX_QUIRK_LENGTH)
    {
        return E_FAIL;
    }
    
    // Create comlete name of the quirk. Windows expects complete quirkName i.e. componentName.quirkName
    // eg. ETWEnabled will become NETFX.ETWEnabled
    errno_t err = wcsncpy_s(wszCompleteQuirkName, cchCompleteQuirkName, wszComponentName, WIN_DB_COMPONENT_NAME_LENGTH);
    if (err != 0)
    {
        return E_FAIL;
    }
    
    err = wcscat_s(wszCompleteQuirkName, cchCompleteQuirkName, wszQuirkName);
    if (err != 0)
    {
        return E_FAIL;
    }


    UINT32 version = 0xFFFFFFFF;
    BOOL fIsEnabled;
    //call windows api
    // Version passed must be 0xFFFFFFFF for NETFX. Passing any other version requires more 
    // understanding of the windows API. 
    fIsEnabled = s_IsQuirkEnabledCallback(wszCompleteQuirkName,version);

    if(fIsEnabled && quirkData != NULL)
    {
        quirkData->Size = sizeof(CPT_QUIRK_DATA);
        // Query for quirkData 
        if(!SUCCEEDED(s_GetQuirkValueCallback(wszCompleteQuirkName, quirkData)))
        {
            return E_FAIL;
        }
    }

    *isEnabled = fIsEnabled;

    return S_OK;
}

#endif


// Return if a quirk is a enabled.
// This will also return enabled as true when the quirk has a value set.
BOOL CLRConfig::IsConfigEnabled(const ConfigDWORDInfo & info)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    DWORD result = info.defaultValue;

#ifdef FEATURE_WIN_DB_APPCOMPAT
    // Windows Shim DB should be the first place to look as it applies microsoft enforced policy
    // and overrides setting at any other place like config or registry
    if(CheckLookupOption(info, IgnoreWindowsQuirkDB) == FALSE && 
       s_IsQuirkEnabledCallback != NULL )// Check that IsQuirkEnabledCallback function has been registered.
    {
        BOOL enabledInDB = FALSE;
        if(SUCCEEDED(getQuirkEnabledAndValueFromWinDB(info.name, &enabledInDB, NULL)))
        {
            if(enabledInDB)
            {
                return TRUE;
            }
        }
    }
#endif
    //
    // Set up REGUTIL options.
    // 
    REGUTIL::CORConfigLevel level = GetConfigLevel(info.options);
    BOOL prependCOMPLUS = !CheckLookupOption(info, DontPrependCOMPLUS_);
    
    // 
    // If we aren't favoring config files, we check REGUTIL here.
    // 
    if(CheckLookupOption(info, FavorConfigFile) == FALSE)
    {
        REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS);
        if(result>0)
            return TRUE;
        LPWSTR result = REGUTIL::GetConfigString_DontUse_(info.name, prependCOMPLUS, level);
        if(result != NULL && result[0] != 0)
        {
            return TRUE;
        }
    }

    // 
    // Check config files through EEConfig.
    // 
    if(CheckLookupOption(info, IgnoreConfigFiles) == FALSE && // Check that we aren't ignoring config files.
        s_GetConfigValueCallback != NULL)// Check that GetConfigValueCallback function has been registered.
    {        
        LPCWSTR pvalue;

        // EEConfig lookup options.
        BOOL systemOnly = CheckLookupOption(info, ConfigFile_SystemOnly) ? TRUE : FALSE;
        BOOL applicationFirst = CheckLookupOption(info, ConfigFile_ApplicationFirst) ? TRUE : FALSE;
        
        if(SUCCEEDED(s_GetConfigValueCallback(info.name, &pvalue, systemOnly, applicationFirst)) && pvalue != NULL)
        {
            WCHAR * end;
            errno = 0;
            result = wcstoul(pvalue, &end, 0);
            
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno == ERANGE || end == pvalue)
            {
                if(pvalue[0]!=0)
                    return TRUE;

                result = info.defaultValue; 
            }
            
            if(result>0)
                return TRUE;
        }
    }

    // 
    // If we are favoring config files and we don't have a result from EEConfig, we check REGUTIL here.
    // 
    if(CheckLookupOption(info, FavorConfigFile) == TRUE)
    {
        REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS);
        if(result>0)
            return TRUE;
        LPWSTR result = REGUTIL::GetConfigString_DontUse_(info.name, prependCOMPLUS, level);
        if(result != NULL && result[0] != 0)
        {
            return TRUE;
        }
    }

    //
    // If we get here, the option was not listed in REGUTIL or EEConfig; check whether the option
    // has a PerformanceDefault-specified value before falling back to the built-in default
    //
    DWORD performanceDefaultValue;
    if (CheckLookupOption(info, MayHavePerformanceDefault) &&
        s_GetPerformanceDefaultValueCallback != NULL &&
        s_GetPerformanceDefaultValueCallback(info.name, &performanceDefaultValue))
    {
        if (!SUCCEEDED(REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS)))
        {
            if(performanceDefaultValue>0)
                return TRUE;
        }
    }

    if(info.defaultValue>0)
        return TRUE;
    else
        return FALSE;
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
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT; // Need this to be tolerant to stack overflows since REGUTIL::GetConfigDWORD was too. (This replaces calls to REGUTIL::GetConfigDWORD) 
    }
    CONTRACTL_END;

    DWORD result = info.defaultValue;

#ifdef FEATURE_WIN_DB_APPCOMPAT
    // Windows Shim DB should be the first place to look as it applies microsoft enforced policy
    // and overrides setting at any other place like config or registry
    if(CheckLookupOption(info, IgnoreWindowsQuirkDB) == FALSE && 
       s_IsQuirkEnabledCallback != NULL )// Check that IsQuirkEnabledCallback function has been registered.
    {

        BOOL isEnabledInDB = FALSE;
        CPT_QUIRK_DATA quirkData;
        if(SUCCEEDED(getQuirkEnabledAndValueFromWinDB(info.name, &isEnabledInDB, &quirkData)))
        {
            if(isEnabledInDB)
            {
                WCHAR *end;
                errno = 0;
                result = wcstoul(quirkData.CommandLine, &end, 0);
                
                // errno is ERANGE if the number is out of range, and end is set to pvalue if
                // no valid conversion exists.
                if (errno != ERANGE && end != quirkData.CommandLine)
                {
                    return result;
                }
                else
                {
                    // If an invalid value is defined we treat it as the default value.
                    // i.e. we don't look further.
                    return info.defaultValue; 
                }
            }
        }
    }
#endif // FEATURE_WIN_DB_APPCOMPAT

    //
    // Set up REGUTIL options.
    // 
    REGUTIL::CORConfigLevel level = GetConfigLevel(info.options);
    BOOL prependCOMPLUS = !CheckLookupOption(info, DontPrependCOMPLUS_);
    
    // 
    // If we aren't favoring config files, we check REGUTIL here.
    // 
    if(CheckLookupOption(info, FavorConfigFile) == FALSE)
    {
        REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS);
        // TODO: We are ignoring explicitly defined default values to avoid change in behavior. 
        // TODO: Ideally, the following should check the hresult for success.
        if(result != info.defaultValue)
        {
            return result;
        }
    }

    // 
    // Check config files through EEConfig.
    // 
    if(CheckLookupOption(info, IgnoreConfigFiles) == FALSE && // Check that we aren't ignoring config files.
        s_GetConfigValueCallback != NULL)// Check that GetConfigValueCallback function has been registered.
    {        
        LPCWSTR pvalue;

        // EEConfig lookup options.
        BOOL systemOnly = CheckLookupOption(info, ConfigFile_SystemOnly) ? TRUE : FALSE;
        BOOL applicationFirst = CheckLookupOption(info, ConfigFile_ApplicationFirst) ? TRUE : FALSE;
        
        if(SUCCEEDED(s_GetConfigValueCallback(info.name, &pvalue, systemOnly, applicationFirst)) && pvalue != NULL)
        {
            WCHAR * end;
            errno = 0;
            result = wcstoul(pvalue, &end, 0);
			
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno != ERANGE && end != pvalue)
            {
                return result;
            }
            else
            {
                // If an invalid value is defined we treat it as the default value.
                // i.e. we don't look further.
                return info.defaultValue; 
            }
        }
    }

    // 
    // If we are favoring config files and we don't have a result from EEConfig, we check REGUTIL here.
    // 
    if(CheckLookupOption(info, FavorConfigFile) == TRUE)
    {
        REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS);
        // TODO: We are ignoring explicitly defined default values to avoid change in behavior. 
        // TODO: Ideally, the following should check the hresult for success.
        if(result != info.defaultValue)
        {
            return result;
        }
    }

    //
    // If we get here, the option was not listed in REGUTIL or EEConfig; check whether the option
    // has a PerformanceDefault-specified value before falling back to the built-in default
    //
    DWORD performanceDefaultValue;
    if (CheckLookupOption(info, MayHavePerformanceDefault) &&
        s_GetPerformanceDefaultValueCallback != NULL &&
        s_GetPerformanceDefaultValueCallback(info.name, &performanceDefaultValue))
    {
        // TODO: We ignore explicitly defined default values above, but we do not want to let performance defaults override these.
        // TODO: Ideally, the above would use hresult for success and this check would be removed.
        if (!SUCCEEDED(REGUTIL::GetConfigDWORD_DontUse_(info.name, info.defaultValue, &result, level, prependCOMPLUS)))
            return performanceDefaultValue;
    }

    return info.defaultValue;
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
HRESULT CLRConfig::GetConfigValue(const ConfigStringInfo & info, __deref_out_z LPWSTR * outVal)
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT (CONTRACT_RETURN E_OUTOFMEMORY);
        POSTCONDITION(CheckPointer(outVal, NULL_OK)); // TODO: Should this check be *outVal instead of outVal?
    } CONTRACT_END;

    LPWSTR result = NULL;

#ifdef FEATURE_WIN_DB_APPCOMPAT
    // Windows Shim DB should be the first place to look as it applies microsoft enforced policy
    // and overrides setting at any other place like config or registry
    if(CheckLookupOption(info, IgnoreWindowsQuirkDB) == FALSE && 
       s_IsQuirkEnabledCallback != NULL )// Check that IsQuirkEnabledCallback function has been registered.
    {

        BOOL isEnabledInDB = FALSE;
        CPT_QUIRK_DATA quirkData;
        if(SUCCEEDED(getQuirkEnabledAndValueFromWinDB(info.name, &isEnabledInDB, &quirkData)))
        {
            if(isEnabledInDB)
            {
                size_t len = wcslen(quirkData.CommandLine) + 1;
                result = new (nothrow) WCHAR[len];
                if (result == NULL)
                {            
                    RETURN E_OUTOFMEMORY;
                }
                wcscpy_s(result, len, quirkData.CommandLine);
            }
        }
    }
#endif // FEATURE_WIN_DB_APPCOMPAT

    //
    // Set up REGUTIL options.
    // 
    REGUTIL::CORConfigLevel level = GetConfigLevel(info.options);
    BOOL prependCOMPLUS = !CheckLookupOption(info, DontPrependCOMPLUS_);

    // 
    // If we aren't favoring config files, we check REGUTIL here.
    // 
    if(result == NULL && CheckLookupOption(info, FavorConfigFile) == FALSE)
    {        
        result = REGUTIL::GetConfigString_DontUse_(info.name, prependCOMPLUS, level);
    }

    // 
    // Check config files through EEConfig.
    // 
    if(result == NULL && // Check that we don't have a value from REGUTIL
        CheckLookupOption(info, IgnoreConfigFiles) == FALSE && // Check that we aren't ignoring config files.
        s_GetConfigValueCallback != NULL) // Check that GetConfigValueCallback function has been registered.
    {
        LPCWSTR pResult;

        // EEConfig lookup options.
        BOOL systemOnly = CheckLookupOption(info, ConfigFile_SystemOnly) ? TRUE : FALSE;
        BOOL applicationFirst = CheckLookupOption(info, ConfigFile_ApplicationFirst) ? TRUE : FALSE;

        if(SUCCEEDED(s_GetConfigValueCallback(info.name, &pResult, systemOnly, applicationFirst)) && pResult != NULL)
        {
            size_t len = wcslen(pResult) + 1;
            result = new (nothrow) WCHAR[len];
            if (result == NULL)
            {            
                RETURN E_OUTOFMEMORY;
            }
            wcscpy_s(result, len, pResult);
        }
    }

    // 
    // If we are favoring config files and we don't have a result from EEConfig, we check REGUTIL here.
    // 
    if(result==NULL && 
        CheckLookupOption(info, FavorConfigFile) == TRUE)
    {
        result = REGUTIL::GetConfigString_DontUse_(info.name, prependCOMPLUS, level);
    }

    if ((result != NULL) && CheckLookupOption(info, TrimWhiteSpaceFromStringValue))
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

    // If we ever want a PerformanceDefault for a string value, you can replace this assert
    // with code that follows the pattern for DWORD values above.
    _ASSERTE(!CheckLookupOption(info, MayHavePerformanceDefault));

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

    // Check config files
    {
        LPCWSTR result = NULL;

        if (s_GetConfigValueCallback != NULL && 
            SUCCEEDED(s_GetConfigValueCallback(name, &result, FALSE, FALSE)) && 
            result != NULL)
        {
            return TRUE;
        }
    }

    // Check REGUTIL, both with and without the COMPLUS_ prefix
    {
        LPWSTR result = NULL;
    
        result = REGUTIL::GetConfigString_DontUse_(name, TRUE);
        if (result != NULL)
        {
            FreeConfigString(result);
            return TRUE;
        }

        result = REGUTIL::GetConfigString_DontUse_(name, FALSE);
        if (result != NULL)
        {
            FreeConfigString(result);
            return TRUE;
        }

    }

    return FALSE;
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
HRESULT CLRConfig::TrimWhiteSpace(LPCWSTR wszOrig, __deref_out_z LPWSTR * pwszTrimmed)
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
    size_t cchOrig = wcslen(wszOrig);
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


// 
// Deallocation function for code:CLRConfig::FreeConfigString
//
void CLRConfig::FreeConfigString(__in_z LPWSTR str)
{
    LIMITED_METHOD_CONTRACT;
    
    delete [] str;
}

// 
// Register EEConfig's GetConfigValueCallback function so CLRConfig can look in config files.
// 
//static
void CLRConfig::RegisterGetConfigValueCallback(GetConfigValueFunction func)
{
    LIMITED_METHOD_CONTRACT;
    s_GetConfigValueCallback = func;
}

// 
// Register PerformanceDefaults' LookupConfigValue so CLRConfig can support 'MayHavePerformanceDefault' values
// 
//static 
void CLRConfig::RegisterGetPerformanceDefaultValueCallback(GetPerformanceDefaultValueFunction func)
{
    LIMITED_METHOD_CONTRACT;
    s_GetPerformanceDefaultValueCallback = func;
}

#ifdef FEATURE_WIN_DB_APPCOMPAT
void CLRConfig::RegisterWinDbQuirkApis(PFN_CptQuirkIsEnabled3 func1, PFN_CptQuirkGetData2 func2)
{
    LIMITED_METHOD_CONTRACT;
    s_IsQuirkEnabledCallback = func1;
    s_GetQuirkValueCallback = func2;
}
#endif // FEATURE_WIN_DB_APPCOMPAT

// 
// Helper method to translate LookupOptions to REGUTIL::CORConfigLevel.
// 
//static 
REGUTIL::CORConfigLevel CLRConfig::GetConfigLevel(LookupOptions options)
{
    LIMITED_METHOD_CONTRACT;
    
    REGUTIL::CORConfigLevel level = (REGUTIL::CORConfigLevel) 0;

    if(CheckLookupOption(options, IgnoreEnv) == FALSE)
        level = static_cast<REGUTIL::CORConfigLevel>(level | REGUTIL::COR_CONFIG_ENV);
    
    if(CheckLookupOption(options, IgnoreHKCU) == FALSE)
        level = static_cast<REGUTIL::CORConfigLevel>(level | REGUTIL::COR_CONFIG_USER);
    
    if(CheckLookupOption(options, IgnoreHKLM) == FALSE)
        level = static_cast<REGUTIL::CORConfigLevel>(level | REGUTIL::COR_CONFIG_MACHINE);
    
    return level;
}
