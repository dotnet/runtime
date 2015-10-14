//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <stdint.h>
#include <string.h>

#include "locale.hpp"

#include "unicode/dcfmtsym.h" //decimal
#include "unicode/dtfmtsym.h" //date
#include "unicode/localpointer.h"
#include "unicode/ulocdata.h"

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

Locale GetLocale(const UChar* localeName, bool canonize)
{
    char localeNameTemp[ULOC_FULLNAME_CAPACITY];

    if (localeName != NULL)
    {
        // use UnicodeString.extract instead of u_UCharsToChars; u_UCharsToChars
        // considers '@' a variant and stops
        UnicodeString str(localeName, -1, ULOC_FULLNAME_CAPACITY);
        str.extract(0, str.length(), localeNameTemp);
    }

    Locale loc;
    if (canonize)
    {
        loc = Locale::createCanonical(localeName == NULL ? NULL : localeNameTemp);
    }
    else
    {
        loc = Locale::createFromName(localeName == NULL ? NULL : localeNameTemp);
    }

    return loc;
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

int FixupLocaleName(UChar* value, int32_t valueLength)
{
    int i = 0;
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

extern "C" int32_t GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
    Locale locale = GetLocale(localeName, true);

    if (locale.isBogus())
    {
        // localeName not properly formatted
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    // other validation done on managed side

    UErrorCode status = u_charsToUChars_safe(locale.getName(), value, valueLength);
    if (U_SUCCESS(status))
    {
        FixupLocaleName(value, valueLength);
    }

    return UErrorCodeToBool(status);
}

extern "C" int32_t GetDefaultLocaleName(UChar* value, int32_t valueLength)
{
    Locale locale = GetLocale(NULL);
    if (locale.isBogus())
    {
        // ICU should be able to get default locale
        return UErrorCodeToBool(U_INTERNAL_PROGRAM_ERROR);
    }

    UErrorCode status = u_charsToUChars_safe(locale.getBaseName(), value, valueLength);
    if (U_SUCCESS(status))
    {
        int localeNameLen = FixupLocaleName(value, valueLength);

        // if collation is present, return that to managed side
        char collationValueTemp[ULOC_KEYWORDS_CAPACITY];
        if (locale.getKeywordValue("collation", collationValueTemp, ULOC_KEYWORDS_CAPACITY, status) > 0)
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

    return UErrorCodeToBool(status);
}
