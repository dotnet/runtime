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

struct CompilerEmbeddedSettingsBlob
{
    uint32_t Size;
    char Data[1];
};

extern "C" CompilerEmbeddedSettingsBlob g_compilerEmbeddedSettingsBlob;
extern "C" CompilerEmbeddedSettingsBlob g_compilerEmbeddedKnobsBlob;

bool RhConfig::ReadConfigValue(_In_z_ const char *name, uint64_t* pValue, bool decimal)
{
    if (Environment::TryGetIntegerValue(name, pValue, decimal))
        return true;

    // Check the embedded configuration
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_embeddedSettings, &g_compilerEmbeddedSettingsBlob, name, true, &embeddedValue))
    {
        *pValue = strtoull(embeddedValue, NULL, decimal ? 10 : 16);
        return true;
    }

    return false;
}

bool RhConfig::ReadKnobUInt64Value(_In_z_ const char *name, uint64_t* pValue)
{
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_embeddedKnobs, &g_compilerEmbeddedKnobsBlob, name, false, &embeddedValue))
    {
        *pValue = strtoull(embeddedValue, NULL, 10);
        return true;
    }

    return false;
}

bool RhConfig::ReadKnobBooleanValue(_In_z_ const char *name, bool* pValue)
{
    const char *embeddedValue = nullptr;
    if (GetEmbeddedVariable(&g_embeddedKnobs, &g_compilerEmbeddedKnobsBlob, name, false, &embeddedValue))
    {
        *pValue = strcmp(embeddedValue, "true") == 0;
        return true;
    }

    return false;
}

bool RhConfig::GetEmbeddedVariable(void *volatile * embeddedSettings, void* compilerEmbeddedSettingsBlob, _In_z_ const char* configName, bool caseSensitive, _Out_ const char** configValue)
{
    // Read the config if we haven't yet
    if (*embeddedSettings == NULL)
    {
        ReadEmbeddedSettings(embeddedSettings, compilerEmbeddedSettingsBlob);
    }

    // Config wasn't read or reading failed
    if (*embeddedSettings == CONFIG_INI_NOT_AVAIL)
    {
        return false;
    }

    const ConfigPair* configPairs = (const ConfigPair*)*embeddedSettings;

    // Find the first name which matches
    for (uint32_t iSettings = 0; iSettings < ((CompilerEmbeddedSettingsBlob*)compilerEmbeddedSettingsBlob)->Size; iSettings++)
    {
        if ((caseSensitive && strcmp(configName, configPairs[iSettings].Key) == 0)
            || (!caseSensitive && _stricmp(configName, configPairs[iSettings].Key) == 0))
        {
            *configValue = configPairs[iSettings].Value;
            return true;
        }
    }

    // Config key was not found
    return false;
}

void RhConfig::ReadEmbeddedSettings(void *volatile * embeddedSettings, void* compilerEmbeddedSettingsBlob)
{
    if (*embeddedSettings == NULL)
    {
        uint32_t size = ((CompilerEmbeddedSettingsBlob*)compilerEmbeddedSettingsBlob)->Size;
        char* data = ((CompilerEmbeddedSettingsBlob*)compilerEmbeddedSettingsBlob)->Data;

        //if reading the file contents failed set embeddedSettings to CONFIG_INI_NOT_AVAIL
        if (size == 0)
        {
            //only set if another thread hasn't initialized the buffer yet, otherwise ignore and let the first setter win
            PalInterlockedCompareExchangePointer(embeddedSettings, CONFIG_INI_NOT_AVAIL, NULL);

            return;
        }

        ConfigPair* iniBuff = new (nothrow) ConfigPair[size];
        if (iniBuff == NULL)
        {
            //only set if another thread hasn't initialized the buffer yet, otherwise ignore and let the first setter win
            PalInterlockedCompareExchangePointer(embeddedSettings, CONFIG_INI_NOT_AVAIL, NULL);

            return;
        }

        uint32_t iBuff = 0;
        uint32_t iIniBuff = 0;
        char* currLine;

        //while we haven't reached the max number of config pairs, or the end of the file, read the next line
        while (iBuff < size)
        {
            currLine = &data[iBuff];

            //find the end of the line
            while ((data[iBuff] != '\0') && (iBuff < size))
                iBuff++;

            //parse the line
            //only increment iIniBuff if the parsing succeeded otherwise reuse the config struct
            if (ParseConfigLine(&iniBuff[iIniBuff], currLine))
            {
                iIniBuff++;
            }

            //advance to the next line;
            iBuff++;
        }

        //if another thread initialized first let the first setter win
        //delete the iniBuff to avoid leaking memory
        if (PalInterlockedCompareExchangePointer(embeddedSettings, iniBuff, NULL) != NULL)
        {
            delete[] iniBuff;
        }
    }

    return;
}

//Parses one line of config and populates values in the passed in configPair
//returns: true if the parsing was successful, false if the parsing failed.
//NOTE: if the method fails configPair is left in an uninitialized state
bool RhConfig::ParseConfigLine(_Out_ ConfigPair* configPair, _In_z_ const char * line)
{
    uint32_t iLine = 0;
    uint32_t iKey = 0;
    uint32_t iVal = 0;

    //while we haven't reached the end of the key signalled by '=', or the end of the line, or the key maxlen
    while (line[iLine] != '=' && line[iLine] != '\0' && iKey < CONFIG_KEY_MAXLEN)
    {
        configPair->Key[iKey++] = line[iLine++];
    }

    //if the current char is not '=' we reached the key maxlen, or the line ended return false
    if (line[iLine] != '=')
    {
        return FALSE;
    }

    configPair->Key[iKey] = '\0';

    //increment to start of the value
    iLine++;

    //while we haven't reached the end of the line, or val maxlen
    while (line[iLine] != '\0' && iVal < CONFIG_VAL_MAXLEN)
    {
        configPair->Value[iVal++] = line[iLine++];
    }

    //if the current char is not '\0' we didn't reach the end of the line return false
    if (line[iLine] != '\0')
    {
        return FALSE;
    }

    configPair->Value[iVal] = '\0';

    return TRUE;
}

#endif
