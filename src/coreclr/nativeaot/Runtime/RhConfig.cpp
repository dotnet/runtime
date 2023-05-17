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

bool RhConfig::ReadConfigValue(_In_z_ const char *name, uint64_t* pValue, bool decimal)
{
    char buffer[CONFIG_VAL_MAXLEN + 1]; // hex digits plus a nul terminator.
    const uint32_t cchBuffer = ARRAY_SIZE(buffer);

    uint32_t cchResult = 0;

    char variableName[64] = "DOTNET_";
    assert(strlen(variableName) + strlen(name) < ARRAY_SIZE(variableName));
    strcat(variableName, name);
    cchResult = PalGetEnvironmentVariableA(variableName, buffer, cchBuffer);

#ifdef FEATURE_EMBEDDED_CONFIG
    // if the config key wasn't found in the ini file
    if ((cchResult == 0) || (cchResult >= cchBuffer))
        cchResult = GetEmbeddedVariable(name, buffer, cchBuffer);
#endif // FEATURE_EMBEDDED_CONFIG

    if ((cchResult == 0) || (cchResult >= cchBuffer))
        return false; // not found

    uint64_t uiResult = 0;

    for (uint32_t i = 0; i < cchResult; i++)
    {
        char ch = buffer[i];

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

    *pValue = uiResult;
    return true;
}

#ifdef FEATURE_EMBEDDED_CONFIG
uint32_t RhConfig::GetEmbeddedVariable(_In_z_ const char* configName, _Out_writes_all_(cchOutputBuffer) char* outputBuffer, _In_ uint32_t cchOutputBuffer)
{
    //the buffer needs to be big enough to read the value buffer + null terminator
    if (cchOutputBuffer < CONFIG_VAL_MAXLEN + 1)
    {
        return 0;
    }

    //if we haven't read the config yet try to read
    if (g_embeddedSettings == NULL)
    {
        ReadEmbeddedSettings();
    }

    //if the config wasn't read or reading failed return 0 immediately
    if (g_embeddedSettings == CONFIG_INI_NOT_AVAIL)
    {
        return 0;
    }

    return GetConfigVariable(configName, (ConfigPair*)g_embeddedSettings, outputBuffer, cchOutputBuffer);
}
#endif // FEATURE_EMBEDDED_CONFIG

uint32_t RhConfig::GetConfigVariable(_In_z_ const char* configName, const ConfigPair* configPairs, _Out_writes_all_(cchOutputBuffer) char* outputBuffer, _In_ uint32_t cchOutputBuffer)
{
    //find the first name which matches (case insensitive to be compat with environment variable counterpart)
    for (int iSettings = 0; iSettings < RCV_Count; iSettings++)
    {
        if (_stricmp(configName, configPairs[iSettings].Key) == 0)
        {
            bool nullTerm = FALSE;

            uint32_t iValue;

            for (iValue = 0; (iValue < CONFIG_VAL_MAXLEN + 1) && (iValue < cchOutputBuffer); iValue++)
            {
                outputBuffer[iValue] = configPairs[iSettings].Value[iValue];

                if (outputBuffer[iValue] == '\0')
                {
                    nullTerm = true;
                    break;
                }
            }

            //return the length of the config value if null terminated else return zero
            return nullTerm ? iValue : 0;
        }
    }

    //if the config key was not found return 0
    return 0;
}

#ifdef FEATURE_EMBEDDED_CONFIG
struct CompilerEmbeddedSettingsBlob
{
    uint32_t Size;
    char Data[1];
};

extern "C" CompilerEmbeddedSettingsBlob g_compilerEmbeddedSettingsBlob;

void RhConfig::ReadEmbeddedSettings()
{
    if (g_embeddedSettings == NULL)
    {
        //if reading the file contents failed set g_embeddedSettings to CONFIG_INI_NOT_AVAIL
        if (g_compilerEmbeddedSettingsBlob.Size == 0)
        {
            //only set if another thread hasn't initialized the buffer yet, otherwise ignore and let the first setter win
            PalInterlockedCompareExchangePointer(&g_embeddedSettings, CONFIG_INI_NOT_AVAIL, NULL);

            return;
        }

        ConfigPair* iniBuff = new (nothrow) ConfigPair[RCV_Count];
        if (iniBuff == NULL)
        {
            //only set if another thread hasn't initialized the buffer yet, otherwise ignore and let the first setter win
            PalInterlockedCompareExchangePointer(&g_embeddedSettings, CONFIG_INI_NOT_AVAIL, NULL);

            return;
        }

        uint32_t iBuff = 0;
        uint32_t iIniBuff = 0;
        char* currLine;

        //while we haven't reached the max number of config pairs, or the end of the file, read the next line
        while (iIniBuff < RCV_Count && iBuff < g_compilerEmbeddedSettingsBlob.Size)
        {
            currLine = &g_compilerEmbeddedSettingsBlob.Data[iBuff];

            //find the end of the line
            while ((g_compilerEmbeddedSettingsBlob.Data[iBuff] != '\0') && (iBuff < g_compilerEmbeddedSettingsBlob.Size))
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

        //initialize the remaining config pairs to "\0"
        while (iIniBuff < RCV_Count)
        {
            iniBuff[iIniBuff].Key[0] = '\0';
            iniBuff[iIniBuff].Value[0] = '\0';
            iIniBuff++;
        }

        //if another thread initialized first let the first setter win
        //delete the iniBuff to avoid leaking memory
        if (PalInterlockedCompareExchangePointer(&g_embeddedSettings, iniBuff, NULL) != NULL)
        {
            delete[] iniBuff;
        }
    }

    return;
}
#endif // FEATURE_EMBEDDED_CONFIG

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
