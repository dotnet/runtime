// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// clrconfignocache.h
//
// Logic for resolving configuration names.
//

#include<minipal/utils.h>

// Config prefixes
#define COMPLUS_PREFIX_A "COMPlus_"
#define COMPLUS_PREFIX W("COMPlus_")
#define LEN_OF_COMPLUS_PREFIX STRING_LENGTH(COMPLUS_PREFIX_A)

#define DOTNET_PREFIX_A "DOTNET_"
#define DOTNET_PREFIX W("DOTNET_")
#define LEN_OF_DOTNET_PREFIX STRING_LENGTH(DOTNET_PREFIX_A)

class CLRConfigNoCache
{
    const char* _value;

    CLRConfigNoCache() = default;
    CLRConfigNoCache(LPCSTR cfg) : _value { cfg }
    { }

public:
    bool IsSet() const { return _value != NULL; }

    LPCSTR AsString() const
    {
        _ASSERTE(IsSet());
        return _value;
    }

    bool TryAsInteger(int radix, DWORD& result) const
    {
        _ASSERTE(IsSet());

        errno = 0;
        LPSTR endPtr;
        unsigned long rawResult = strtoul(_value, &endPtr, radix);
        if ((DWORD)rawResult != rawResult || errno == ERANGE)
        {
            return false;
        }
        bool fSuccess = endPtr != _value;
        return fSuccess;
    }

    static CLRConfigNoCache Get(LPCSTR cfg, bool noPrefix = false, char*(*getEnvFptr)(const char*) = nullptr)
    {
        char nameBuffer[64];
        const char* fallbackPrefix = NULL;
        const size_t namelen = strlen(cfg);

        if (noPrefix)
        {
            if (namelen >= ARRAY_SIZE(nameBuffer))
            {
                _ASSERTE(!"Environment variable name too long.");
                return {};
            }

            *nameBuffer = '\0';
        }
        else
        {
            bool dotnetValid = namelen < (size_t)(STRING_LENGTH(nameBuffer) - LEN_OF_DOTNET_PREFIX);
            bool complusValid = namelen < (size_t)(STRING_LENGTH(nameBuffer) - LEN_OF_COMPLUS_PREFIX);
            if (!dotnetValid || !complusValid)
            {
                _ASSERTE(!"Environment variable name too long.");
                return {};
            }

            // Priority order is DOTNET_ and then COMPlus_.
            strcpy_s(nameBuffer, ARRAY_SIZE(nameBuffer), DOTNET_PREFIX_A);
            fallbackPrefix = COMPLUS_PREFIX_A;
        }

        strcat_s(nameBuffer, ARRAY_SIZE(nameBuffer), cfg);

        LPCSTR val = getEnvFptr != NULL ? getEnvFptr(nameBuffer) : getenv(nameBuffer);
        if (val == NULL && fallbackPrefix != NULL)
        {
            strcpy_s(nameBuffer, ARRAY_SIZE(nameBuffer), fallbackPrefix);
            strcat_s(nameBuffer, ARRAY_SIZE(nameBuffer), cfg);
            val = getEnvFptr != NULL ? getEnvFptr(nameBuffer) : getenv(nameBuffer);
        }

        return { val };
    }
};
