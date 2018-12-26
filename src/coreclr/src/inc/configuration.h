// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

    // Unfortunately our traditional config system insists on interpreting numbers as 32-bit so intepret the config
    // in the traditional way separately if you need to.
    //
    // Returns value for name if found in config.
    static ULONGLONG GetKnobULONGLONGValue(LPCWSTR name);

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
    //    - The value of the ConfigDWORDInfo if it's set (1 is true, anything else is false)
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcscmp with "true").
    //    - The default set in the ConfigDWORDInfo (1 is true, anything else is false)
    static bool GetKnobBooleanValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo);

    // Returns (in priority order):
    //    - The value of the ConfigurationKnob (searched by name) if it's set (performs a wcscmp with "true").
    //    - The default value passed in
    static bool GetKnobBooleanValue(LPCWSTR name, bool defaultValue);
};

#endif // __configuration_h__
