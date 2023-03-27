// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// configuration.h
//
//
// Access and update configuration values, falling back on legacy CLRConfig methods where necessary.
//
// --------------------------------------------------------------------------------------------------

#include "clrconfig.h"

#ifndef __configuration_h__
#define __configuration_h__

class Configuration
{
public:
    static void InitializeConfigurationKnobs(int numberOfConfigs, LPCWSTR *configNames, LPCWSTR *configValues);

    // Returns (in priority order):
    //    - The value of the ConfigDWORDInfo if it's set
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcstoul).
    //    - The default set in the ConfigDWORDInfo
    static DWORD GetKnobDWORDValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo);

    // Returns (in priority order):
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcstoul)
    //    - The default value passed in
    static DWORD GetKnobDWORDValue(LPCWSTR name, DWORD defaultValue);

    // Unfortunately our traditional config system insists on interpreting numbers as 32-bit so interpret the config
    // in the traditional way separately if you need to.
    //
    // Returns (in priority order):
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a _wcstoui64)
    //    - The default value passed in
    static ULONGLONG GetKnobULONGLONGValue(LPCWSTR name, ULONGLONG defaultValue);

    // Returns (in priority order):
    //    - The value of the ConfigStringInfo if it's set
    //    - The value of the ConfigurationKnob (searched by name) if it's set
    //    - nullptr
    static LPCWSTR GetKnobStringValue(LPCWSTR name, const CLRConfig::ConfigStringInfo& stringInfo);

    // Returns (in priority order):
    //    - The value of the ConfigurationKnob (searched by name) if it's set
    //    - nullptr
    static LPCWSTR GetKnobStringValue(LPCWSTR name);

    // Returns (in priority order):
    //    - The value of the ConfigDWORDInfo if it's set (0 is false, anything else is true)
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcscmp with "true").
    //    - The default set in the ConfigDWORDInfo (0 is false, anything else is true)
    static bool GetKnobBooleanValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo);

    // Returns (in priority order):
    //    - The value of the ConfigDWORDInfo if it's set (0 is false, anything else is true)
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcscmp with "true").
    //    - The default value passed in
    static bool GetKnobBooleanValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo, bool defaultValue);

    // Returns (in priority order):
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcscmp with "true").
    //    - The default value passed in
    static bool GetKnobBooleanValue(LPCWSTR name, bool defaultValue);
};

#endif // __configuration_h__
