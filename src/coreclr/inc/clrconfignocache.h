// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// --------------------------------------------------------------------------------------------------
// clrconfignocache.h
//
// Logic for resolving configuration names.
//

#ifndef StrLen
#define StrLen(STR) ((sizeof(STR) / sizeof(STR[0])) - 1)
#endif // !StrLen

// Config prefixes
#define COMPLUS_PREFIX_A "COMPlus_"
#define COMPLUS_PREFIX W("COMPlus_")
#define LEN_OF_COMPLUS_PREFIX StrLen(COMPLUS_PREFIX_A)

#define DOTNET_PREFIX_A "DOTNET_"
#define DOTNET_PREFIX W("DOTNET_")
#define LEN_OF_DOTNET_PREFIX StrLen(DOTNET_PREFIX_A)

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
        result = strtoul(_value, &endPtr, radix);
        bool fSuccess = (errno != ERANGE) && (endPtr != _value);
        return fSuccess;
    }

    static CLRConfigNoCache Get(LPCSTR cfg)
    {
        if (cfg == nullptr)
            return {};

        char nameBuffer[64];
        const char* fallbackPrefix = COMPLUS_PREFIX_A;
        const size_t namelen = strlen(cfg);

        bool dotnetValid = namelen < (size_t)(_countof(nameBuffer) - 1 - LEN_OF_DOTNET_PREFIX);
        bool complusValid = namelen < (size_t)(_countof(nameBuffer) - 1 - LEN_OF_COMPLUS_PREFIX);
        if (!dotnetValid || !complusValid)
        {
            _ASSERTE(!"Environment variable name too long.");
            return {};
        }

        // Priority order is DOTNET_ and then COMPlus_.
        strcpy_s(nameBuffer, _countof(nameBuffer), DOTNET_PREFIX_A);
        strcat_s(nameBuffer, _countof(nameBuffer), cfg);

        LPCSTR val = getenv(nameBuffer);
        if (val == NULL && fallbackPrefix != NULL)
        {
            strcpy_s(nameBuffer, _countof(nameBuffer), fallbackPrefix);
            strcat_s(nameBuffer, _countof(nameBuffer), cfg);
            val = getenv(nameBuffer);
        }

        return { val };
    }
};
