// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// CLRConfig.h
//

//
// Unified method of accessing configuration values.
// To define a flag, add an entry in the table in file:CLRConfigValues.h.
// --------------------------------------------------------------------------------------------------


#include "utilcode.h"
#include "holder.h"

#ifndef __CLRConfig_h__
#define __CLRConfig_h__

class CLRConfig
{
public:
    // Setting each option results in some change to the config value.
    enum class LookupOptions
    {
        // Default options.
        Default = 0,

        // If set, do not prepend prefix when doing environment variable lookup.
        DontPrependPrefix = 0x1,

        // Remove any whitespace at beginning and end of value.  (Only applicable for
        // *string* configuration values.)
        TrimWhiteSpaceFromStringValue = 0x2,

        // The configuration should be parsed using a 10 radix as opposed to the
        // default of 16.
        ParseIntegerAsBase10 = 0x4,
    };

    // Struct used to store information about where/how to find a Config DWORD.
    // NOTE: Please do NOT create instances of this struct. Use the macros in file:CLRConfigValues.h instead.
    typedef struct ConfigDWORDInfo
    {
        LPCWSTR name;
        DWORD defaultValue;
        LookupOptions options;
    } ConfigDWORDInfo;

    // Struct used to store information about where/how to find a Config String.
    // NOTE: Please do NOT create instances of this struct. Use the macros in file:CLRConfigValues.h instead.
    typedef struct ConfigStringInfo
    {
        LPCWSTR name;
        LookupOptions options;
    } ConfigStringInfo;

    //
    // Declaring structs using the macro table in CLRConfigValues.h
    //

    // These macros declare ConfigDWORDInfo structs.
    #define RETAIL_CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
        static const ConfigDWORDInfo symbol;
    #define RETAIL_CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
        static const ConfigDWORDInfo symbol;

    // These macros declare ConfigStringInfo structs.
    #define RETAIL_CONFIG_STRING_INFO(symbol, name, description) \
        static const ConfigStringInfo symbol;
    #define RETAIL_CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
        static const ConfigStringInfo symbol;

    //
    // Debug versions of the macros
    //
#ifdef _DEBUG
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description) \
        static const ConfigDWORDInfo symbol;
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions) \
        static const ConfigDWORDInfo symbol;
    #define CONFIG_STRING_INFO(symbol, name, description) \
        static const ConfigStringInfo symbol;
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions) \
        static const ConfigStringInfo symbol;
#else
    #define CONFIG_DWORD_INFO(symbol, name, defaultValue, description)
    #define CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions)
    #define CONFIG_STRING_INFO(symbol, name, description)
    #define CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions)
#endif // _DEBUG

        // Now that we have defined what what the macros in file:CLRConfigValues.h mean, include it to generate the code.
        #include "clrconfigvalues.h"

    #undef RETAIL_CONFIG_DWORD_INFO
    #undef RETAIL_CONFIG_STRING_INFO
    #undef RETAIL_CONFIG_DWORD_INFO_EX
    #undef RETAIL_CONFIG_STRING_INFO_EX
    #undef CONFIG_DWORD_INFO
    #undef CONFIG_STRING_INFO
    #undef CONFIG_DWORD_INFO_EX
    #undef CONFIG_STRING_INFO_EX

    //
    // Methods to do config value (DWORD and String) lookups.
    //
    static BOOL IsConfigEnabled(const ConfigDWORDInfo & info);

    // Look up a DWORD config value.
    static DWORD GetConfigValue(const ConfigDWORDInfo & info);

    // Look up a DWORD config value.
    static DWORD GetConfigValue(const ConfigDWORDInfo & info, /* [Out] */ bool *isDefault);

    // Look up a DWORD config value.
    static DWORD GetConfigValue(const ConfigDWORDInfo & info, DWORD defaultValue);

    // Look up a string config value.
    // You own the string that's returned.
    static LPWSTR GetConfigValue(const ConfigStringInfo & info);

    // Look up a string config value, passing it out through a pointer reference. Reports out of memory
    // errors (HRESULT E_OUTOFMEMORY).
    // You own the string that's returned.
    static HRESULT GetConfigValue(const ConfigStringInfo & info, __deref_out_z LPWSTR * outVal);

    //
    // Check whether an option is specified (e.g. explicitly listed) in any of the CLRConfig
    // locations: environment or registry (with or without COMPlus_) or any config file.
    // The result is therefore a conservative approximation (some settings do not actually
    // take effect everywhere and no setting is valid both with and without COMPlus_)
    //
    static BOOL IsConfigOptionSpecified(LPCWSTR name);

    // Free a string returned by GetConfigValue
    static void FreeConfigString(__in __in_z LPWSTR name);

    // Initialize the configuration.
    static void Initialize();
};

inline CLRConfig::LookupOptions operator|(CLRConfig::LookupOptions lhs, CLRConfig::LookupOptions rhs)
{
    return static_cast<CLRConfig::LookupOptions>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
}

inline CLRConfig::LookupOptions operator&(CLRConfig::LookupOptions lhs, CLRConfig::LookupOptions rhs)
{
    return static_cast<CLRConfig::LookupOptions>(static_cast<DWORD>(lhs) & static_cast<DWORD>(rhs));
}

typedef Wrapper<LPWSTR, DoNothing, CLRConfig::FreeConfigString, NULL> CLRConfigStringHolder;

#endif //__CLRConfig_h__
