// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// configuration.cpp
//
//
// Access and update configuration values, falling back on legacy CLRConfig methods where necessary.
//
// --------------------------------------------------------------------------------------------------

#include "stdafx.h"

#include "clrconfig.h"
#include "configuration.h"

LPCWSTR *knobNames = nullptr;
LPCWSTR *knobValues = nullptr;
int numberOfKnobs = 0;

void Configuration::InitializeConfigurationKnobs(int numberOfConfigs, LPCWSTR *names, LPCWSTR *values)
{
    numberOfKnobs = numberOfConfigs;

    // Neither should be null, or both should be null
    _ASSERT(!((names == nullptr) ^ (values == nullptr)));

    knobNames = names;
    knobValues = values;
}

static LPCWSTR GetConfigurationValue(LPCWSTR name)
{
    _ASSERT(name != nullptr);
    if (name == nullptr || knobNames == nullptr || knobValues == nullptr)
    {
        return nullptr;
    }

    for (int i = 0; i < numberOfKnobs; ++i)
    {
        _ASSERT(knobNames[i] != nullptr);
        if (u16_strcmp(name, knobNames[i]) == 0)
        {
            return knobValues[i];
        }
    }

    return nullptr;
}

DWORD Configuration::GetKnobDWORDValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo)
{
    bool returnedDefaultValue;
    DWORD legacyValue = CLRConfig::GetConfigValue(dwordInfo, &returnedDefaultValue);
    if (!returnedDefaultValue)
    {
        return legacyValue;
    }

    LPCWSTR knobValue = GetConfigurationValue(name);
    if (knobValue != nullptr)
    {
        return u16_strtoul(knobValue, nullptr, 0);
    }

    return legacyValue;
}

DWORD Configuration::GetKnobDWORDValue(LPCWSTR name, DWORD defaultValue)
{
    LPCWSTR knobValue = GetConfigurationValue(name);
    if (knobValue != nullptr)
    {
        return u16_strtoul(knobValue, nullptr, 0);
    }

    return defaultValue;
}

ULONGLONG Configuration::GetKnobULONGLONGValue(LPCWSTR name, ULONGLONG defaultValue)
{
    LPCWSTR knobValue = GetConfigurationValue(name);
    if (knobValue != nullptr)
    {
        return u16_strtoui64(knobValue, nullptr, 0);
    }

    return defaultValue;
}

LPCWSTR Configuration::GetKnobStringValue(LPCWSTR name, const CLRConfig::ConfigStringInfo& stringInfo)
{
    LPCWSTR value = CLRConfig::GetConfigValue(stringInfo);
    if (value == nullptr)
    {
        value = GetConfigurationValue(name);
    }

    return value;
}

LPCWSTR Configuration::GetKnobStringValue(LPCWSTR name)
{
    return GetConfigurationValue(name);
}

bool Configuration::GetKnobBooleanValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo)
{
    bool returnedDefaultValue;
    DWORD legacyValue = CLRConfig::GetConfigValue(dwordInfo, &returnedDefaultValue);
    if (!returnedDefaultValue)
    {
        return (legacyValue != 0);
    }

    LPCWSTR knobValue = GetConfigurationValue(name);
    if (knobValue != nullptr)
    {
        return (u16_strcmp(knobValue, W("true")) == 0);
    }

    return (legacyValue != 0);
}

bool Configuration::GetKnobBooleanValue(LPCWSTR name, const CLRConfig::ConfigDWORDInfo& dwordInfo, bool defaultValue)
{
    bool returnedDefaultValue;
    DWORD legacyValue = CLRConfig::GetConfigValue(dwordInfo, &returnedDefaultValue);
    if (!returnedDefaultValue)
    {
        return (legacyValue != 0);
    }

    return GetKnobBooleanValue(name, defaultValue);
}

bool Configuration::GetKnobBooleanValue(LPCWSTR name, bool defaultValue)
{
    LPCWSTR knobValue = GetConfigurationValue(name);
    if (knobValue != nullptr)
    {
        return (u16_strcmp(knobValue, W("true")) == 0);
    }

    return defaultValue;
}
