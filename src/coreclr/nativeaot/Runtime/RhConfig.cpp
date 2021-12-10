// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#ifndef DACCESS_COMPILE
#include "CommonTypes.h"
#include "daccess.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "event.h"
#include "RWLock.h"
#include "threadstore.h"
#include "RuntimeInstance.h"
#include "shash.h"
#include "RhConfig.h"

#include <string.h>

bool RhConfig::ReadConfigValue(_In_z_ const TCHAR *wszName, uint32_t* pValue, bool decimal)
{
    TCHAR wszBuffer[CONFIG_VAL_MAXLEN + 1]; // 8 hex digits plus a nul terminator.
    const uint32_t cchBuffer = sizeof(wszBuffer) / sizeof(wszBuffer[0]);

    uint32_t cchResult = 0;

#ifdef FEATURE_ENVIRONMENT_VARIABLE_CONFIG
    TCHAR wszVariableName[64] = _T("DOTNET_");
    assert(_tcslen(wszVariableName) + _tcslen(wszName) < sizeof(wszVariableName) / sizeof(wszVariableName[0]));
    _tcscat(wszVariableName, wszName);
    cchResult = PalGetEnvironmentVariable(wszVariableName, wszBuffer, cchBuffer);
#endif // FEATURE_ENVIRONMENT_VARIABLE_CONFIG

#ifdef FEATURE_EMBEDDED_CONFIG
    // if the config key wasn't found in the ini file
    if ((cchResult == 0) || (cchResult >= cchBuffer))
        cchResult = GetEmbeddedVariable(wszName, wszBuffer, cchBuffer);
#endif // FEATURE_EMBEDDED_CONFIG

    if ((cchResult == 0) || (cchResult >= cchBuffer))
        return false; // not found

    uint32_t uiResult = 0;

    for (uint32_t i = 0; i < cchResult; i++)
    {
        TCHAR ch = wszBuffer[i];

        if (decimal)
        {
            uiResult *= 10;

            if ((ch >= _T('0')) && (ch <= _T('9')))
                uiResult += ch - _T('0');
            else
                return false; // parse error
        }
        else
        {
            uiResult *= 16;

            if ((ch >= _T('0')) && (ch <= _T('9')))
                uiResult += ch - _T('0');
            else if ((ch >= _T('a')) && (ch <= _T('f')))
                uiResult += (ch - _T('a')) + 10;
            else if ((ch >= _T('A')) && (ch <= _T('F')))
                uiResult += (ch - _T('A')) + 10;
            else
                return false; // parse error
        }
    }

    *pValue = uiResult;
    return true;
}

#ifdef FEATURE_EMBEDDED_CONFIG
uint32_t RhConfig::GetEmbeddedVariable(_In_z_ const TCHAR* configName, _Out_writes_all_(cchOutputBuffer) TCHAR* outputBuffer, _In_ uint32_t cchOutputBuffer)
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

uint32_t RhConfig::GetConfigVariable(_In_z_ const TCHAR* configName, const ConfigPair* configPairs, _Out_writes_all_(cchOutputBuffer) TCHAR* outputBuffer, _In_ uint32_t cchOutputBuffer)
{
    //find the first name which matches (case insensitive to be compat with environment variable counterpart)
    for (int iSettings = 0; iSettings < RCV_Count; iSettings++)
    {
        if (_tcsicmp(configName, configPairs[iSettings].Key) == 0)
        {
            bool nullTerm = FALSE;

            uint32_t iValue;

            for (iValue = 0; (iValue < CONFIG_VAL_MAXLEN + 1) && (iValue < (int32_t)cchOutputBuffer); iValue++)
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
//NOTE: if the method fails configPair is left in an unitialized state
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
