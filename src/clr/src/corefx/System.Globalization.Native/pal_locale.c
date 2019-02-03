// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <assert.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>
#include <locale.h>

#include "icushim.h"
#include "locale.hpp"

int32_t UErrorCodeToBool(UErrorCode status)
{
    if (U_SUCCESS(status))
    {
        return 1;
    }

    // assert errors that should never occur
    assert(status != U_BUFFER_OVERFLOW_ERROR);
    assert(status != U_INTERNAL_PROGRAM_ERROR);

    // add possible SetLastError support here

    return 0;
}

int32_t GetLocale(
    const UChar* localeName, char* localeNameResult, int32_t localeNameResultLength, bool canonicalize, UErrorCode* err)
{
    char localeNameTemp[ULOC_FULLNAME_CAPACITY] = {0};
    int32_t localeLength;

    // Convert ourselves instead of doing u_UCharsToChars as that function considers '@' a variant and stops.
    for (int i = 0; i < ULOC_FULLNAME_CAPACITY - 1; i++)
    {
        UChar c = localeName[i];

        if (c > (UChar)0x7F)
        {
            *err = U_ILLEGAL_ARGUMENT_ERROR;
            return ULOC_FULLNAME_CAPACITY;
        }

        localeNameTemp[i] = (char)c;

        if (c == (UChar)0x0)
        {
            break;
        }
    }

    if (canonicalize)
    {
        localeLength = uloc_canonicalize(localeNameTemp, localeNameResult, localeNameResultLength, err);
    }
    else
    {
        localeLength = uloc_getName(localeNameTemp, localeNameResult, localeNameResultLength, err);
    }

    if (U_SUCCESS(*err))
    {
        // Make sure the "language" part of the locale is reasonable (i.e. we can fetch it and it is within range).
        // This mimics how the C++ ICU API determines if a locale is "bogus" or not.

        char language[ULOC_LANG_CAPACITY];
        uloc_getLanguage(localeNameTemp, language, ULOC_LANG_CAPACITY, err);

        if (*err == U_BUFFER_OVERFLOW_ERROR || *err == U_STRING_NOT_TERMINATED_WARNING)
        {
            // ULOC_LANG_CAPACITY includes the null terminator, so if we couldn't extract the language with the null
            // terminator, the language must be invalid.

            *err = U_ILLEGAL_ARGUMENT_ERROR;
        }
    }

    return localeLength;
}

UErrorCode u_charsToUChars_safe(const char* str, UChar* value, int32_t valueLength)
{
    int len = strlen(str);

    if (len >= valueLength)
    {
        return U_BUFFER_OVERFLOW_ERROR;
    }

    u_charsToUChars(str, value, len + 1);
    return U_ZERO_ERROR;
}

int32_t FixupLocaleName(UChar* value, int32_t valueLength)
{
    int32_t i = 0;
    for (; i < valueLength; i++)
    {
        if (value[i] == (UChar)'\0')
        {
            break;
        }
        else if (value[i] == (UChar)'_')
        {
            value[i] = (UChar)'-';
        }
    }

    return i;
}

bool IsEnvVarSet(const char* name)
{
    const char* value = getenv(name);

    return (value != nullptr) && (strcmp("", value) != 0);
}

// The behavior of uloc_getDefault() on POSIX systems is to query
// setlocale(LC_MESSAGES) and use that value, unless it is C or
// POSIX. In that case it tries to read LC_ALL, LC_MESSAGES and LANG
// and then falls back to en_US_POSIX if none of them are set.
//
// en_US_POSIX is a weird locale since the collation rules treat 'a'
// and 'A' as different letters even when ignoring case. Furthermore
// it's common for LC_ALL, LC_MESSAGES and LANG to be unset when
// running under Docker.
//
// We'd rather default to invariant in this case. If any of these
// are set, we'll just call into ICU and let it do whatever
// normalization it would do.
const char* DetectDefaultLocaleName()
{
    char* loc = setlocale(LC_MESSAGES, nullptr);

    if (loc != nullptr && (strcmp("C", loc) == 0 || strcmp("POSIX", loc) == 0))
    {
        if (!IsEnvVarSet("LC_ALL") && !IsEnvVarSet("LC_MESSAGES") && !IsEnvVarSet("LANG"))
        {
            return "";
        }
    }

    return uloc_getDefault();
}

// GlobalizationNative_GetLocales gets all locale names and store it in the value buffer
// in case of success, it returns the count of the characters stored in value buffer  
// in case of failure, it returns negative number.
// if the input value buffer is null, it returns the length needed to store the 
// locale names list.
// if the value is not null, it fills the value with locale names separated by the length 
// of each name. 
extern "C" int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength)
{
    int32_t totalLength = 0;
    int32_t index = 0;
    int32_t localeCount = uloc_countAvailable();
    
    if (localeCount <=  0)
        return -1; // failed
    
    for (int32_t i = 0; i < localeCount; i++)
    {
        const char *pLocaleName = uloc_getAvailable(i);
        if (pLocaleName[0] == 0) // unexpected empty name
            return -2;
        
        int32_t localeNameLength = strlen(pLocaleName);
        
        totalLength += localeNameLength + 1; // add 1 for the name length
        
        if (value != nullptr)
        {
            if (totalLength > valueLength)
                return -3;
            
            value[index++] = (UChar) localeNameLength;
            
            for (int j=0; j<localeNameLength; j++)
            {
                if (pLocaleName[j] == '_') // fix the locale name  
                {
                    value[index++] = (UChar) '-';
                }
                else
                {
                    value[index++] = (UChar) pLocaleName[j];
                }
            }
        }
    }
    
    return totalLength;
}

extern "C" int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;

    char localeNameBuffer[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, localeNameBuffer, ULOC_FULLNAME_CAPACITY, true, &status);

    if (U_SUCCESS(status))
    {
        status = u_charsToUChars_safe(localeNameBuffer, value, valueLength);

        if (U_SUCCESS(status))
        {
            FixupLocaleName(value, valueLength);
        }
    }

    return UErrorCodeToBool(status);
}

extern "C" int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength)
{
    char localeNameBuffer[ULOC_FULLNAME_CAPACITY];
    UErrorCode status = U_ZERO_ERROR;

    const char* defaultLocale = DetectDefaultLocaleName();

    uloc_getBaseName(defaultLocale, localeNameBuffer, ULOC_FULLNAME_CAPACITY, &status);

    if (U_SUCCESS(status))
    {
        status = u_charsToUChars_safe(localeNameBuffer, value, valueLength);

        if (U_SUCCESS(status))
        {
            int localeNameLen = FixupLocaleName(value, valueLength);

            char collationValueTemp[ULOC_KEYWORDS_CAPACITY];
            int32_t collationLen =
                uloc_getKeywordValue(defaultLocale, "collation", collationValueTemp, ULOC_KEYWORDS_CAPACITY, &status);

            if (U_SUCCESS(status) && collationLen > 0)
            {
                // copy the collation; managed uses a "_" to represent collation (not
                // "@collation=")
                status = u_charsToUChars_safe("_", &value[localeNameLen], valueLength - localeNameLen);
                if (U_SUCCESS(status))
                {
                    status = u_charsToUChars_safe(
                        collationValueTemp, &value[localeNameLen + 1], valueLength - localeNameLen - 1);
                }
            }
        }
    }

    return UErrorCodeToBool(status);
}
