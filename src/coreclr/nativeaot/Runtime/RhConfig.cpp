// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#ifndef DACCESS_COMPILE
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "holder.h"
#include "RhConfig.h"

#include <string.h>

#define DOTNET_PREFIX _T("DOTNET_")
#define DOTNET_PREFIX_LEN STRING_LENGTH(DOTNET_PREFIX)

namespace
{
    void GetEnvironmentConfigName(const char* name, TCHAR* buffer, uint32_t bufferSize)
    {
        assert(DOTNET_PREFIX_LEN + strlen(name) < bufferSize);
        memcpy(buffer, DOTNET_PREFIX, (DOTNET_PREFIX_LEN) * sizeof(TCHAR));
    #ifdef TARGET_WINDOWS
        size_t nameLen = strlen(name);
        for (size_t i = 0; i < nameLen; i++)
        {
            buffer[DOTNET_PREFIX_LEN + i] = name[i];
        }
        buffer[DOTNET_PREFIX_LEN + nameLen] = '\0';
    #else
        strcpy(buffer + DOTNET_PREFIX_LEN, name);
    #endif
    }
}

bool RhConfig::Environment::TryGetBooleanValue(const char* name, bool* value)
{
    uint64_t intValue;
    if (!TryGetIntegerValue(name, &intValue))
        return false;

    *value = intValue != 0;
    return true;
}

bool RhConfig::Environment::TryGetIntegerValue(const char* name, uint64_t* value, bool decimal)
{
    TCHAR variableName[64];
    GetEnvironmentConfigName(name, variableName, ARRAY_SIZE(variableName));

    TCHAR buffer[CONFIG_VAL_MAXLEN + 1]; // hex digits plus a nul terminator.
    const uint32_t cchBuffer = ARRAY_SIZE(buffer);
    uint32_t cchResult = PalGetEnvironmentVariable(variableName, buffer, cchBuffer);
    if (cchResult == 0 || cchResult >= cchBuffer)
        return false;

    // Environment variable was set. Convert it to an integer.
    uint64_t uiResult = 0;
    for (uint32_t i = 0; i < cchResult; i++)
    {
        TCHAR ch = buffer[i];

        if (decimal)
        {
            uiResult *= 10;

            if ((ch >= '0') && (ch <= '9'))
                uiResult += ch - '0';
            else
                return false; // parse error
        }
        else
        {
            uiResult *= 16;

            if ((ch >= '0') && (ch <= '9'))
                uiResult += ch - '0';
            else if ((ch >= 'a') && (ch <= 'f'))
                uiResult += (ch - 'a') + 10;
            else if ((ch >= 'A') && (ch <= 'F'))
                uiResult += (ch - 'A') + 10;
            else
                return false; // parse error
        }
    }

    *value = uiResult;
    return true;
}

bool RhConfig::Environment::TryGetStringValue(const char* name, char** value)
{
    TCHAR variableName[64];
    GetEnvironmentConfigName(name, variableName, ARRAY_SIZE(variableName));

    TCHAR buffer[260];
    uint32_t bufferLen = ARRAY_SIZE(buffer);
    uint32_t actualLen = PalGetEnvironmentVariable(variableName, buffer, bufferLen);
    if (actualLen == 0)
        return false;

    if (actualLen < bufferLen)
    {
        *value = PalCopyTCharAsChar(buffer);
        return true;
    }

    // Expand the buffer to get the value
    bufferLen = actualLen + 1;
    NewArrayHolder<TCHAR> newBuffer {new (nothrow) TCHAR[bufferLen]};
    actualLen = PalGetEnvironmentVariable(variableName, newBuffer, bufferLen);
    if (actualLen >= bufferLen)
        return false;

#ifdef TARGET_WINDOWS
    *value = PalCopyTCharAsChar(newBuffer);
#else
    *value = newBuffer.Extract();
#endif
    return true;
}

extern "C" RhConfig::Config g_compilerEmbeddedSettingsBlob;
extern "C" RhConfig::Config g_compilerEmbeddedKnobsBlob;

bool RhConfig::ReadConfigValue(_In_z_ const char *name, uint64_t* pValue, bool decimal)
{
    if (Environment::TryGetIntegerValue(name, pValue, decimal))
        return true;

    // Check the embedded configuration
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_compilerEmbeddedSettingsBlob, name, true, &embeddedValue))
    {
        *pValue = strtoull(embeddedValue, NULL, decimal ? 10 : 16);
        return true;
    }

    return false;
}

bool RhConfig::ReadKnobUInt64Value(_In_z_ const char *name, uint64_t* pValue)
{
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_compilerEmbeddedKnobsBlob, name, false, &embeddedValue))
    {
        *pValue = strtoull(embeddedValue, NULL, 10);
        return true;
    }

    return false;
}

bool RhConfig::ReadKnobBooleanValue(_In_z_ const char *name, bool* pValue)
{
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_compilerEmbeddedKnobsBlob, name, false, &embeddedValue))
    {
        *pValue = strcmp(embeddedValue, "true") == 0;
        return true;
    }

    return false;
}

char** RhConfig::GetKnobNames()
{
    return g_compilerEmbeddedKnobsBlob.GetKeys();
}

char** RhConfig::GetKnobValues()
{
    return g_compilerEmbeddedKnobsBlob.GetValues();
}

uint32_t RhConfig::GetKnobCount()
{
    return g_compilerEmbeddedKnobsBlob.GetCount();
}

bool RhConfig::GetEmbeddedVariable(Config* config, _In_z_ const char* configName, bool caseSensitive, _Out_ const char** configValue)
{
    // Find the first name which matches
    for (uint32_t iSettings = 0; iSettings < config->GetCount(); iSettings++)
    {
        if ((caseSensitive && strcmp(configName, config->GetKeyAt(iSettings)) == 0)
            || (!caseSensitive && _stricmp(configName, config->GetKeyAt(iSettings)) == 0))
        {
            *configValue = config->GetValueAt(iSettings);
            return true;
        }
    }

    // Config key was not found
    return false;
}

#endif
