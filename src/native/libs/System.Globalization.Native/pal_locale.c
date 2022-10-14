// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <stdbool.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>
#include <locale.h>

#include "pal_locale_internal.h"
#include "pal_locale.h"

int32_t UErrorCodeToBool(UErrorCode status)
{
    if (U_SUCCESS(status))
    {
        return true;
    }

    // assert errors that should never occur
    assert(status != U_BUFFER_OVERFLOW_ERROR);
    assert(status != U_INTERNAL_PROGRAM_ERROR);

    // add possible SetLastError support here

    return false;
}

int32_t GetLocale(const UChar* localeName,
                  char* localeNameResult,
                  int32_t localeNameResultLength,
                  UBool canonicalize,
                  UErrorCode* err)
{
    char localeNameTemp[ULOC_FULLNAME_CAPACITY] = {0};
    int32_t localeLength;

    if (U_FAILURE(*err))
    {
        return 0;
    }

    // Convert ourselves instead of doing u_UCharsToChars as that function considers '@' a variant and stops.
    for (int i = 0; i < ULOC_FULLNAME_CAPACITY - 1; i++)
    {
        UChar c = localeName[i];

        // Some versions of ICU have a bug where '/' in name can cause infinite loop, so we preemptively
        // detect this case for CultureNotFoundException (as '/' is anyway illegal in locale name and we
        // expected ICU to return this error).

        if (c > (UChar)0x7F || c == (UChar)'/')
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

void u_charsToUChars_safe(const char* str, UChar* value, int32_t valueLength, UErrorCode* err)
{
    if (U_FAILURE(*err))
    {
        return;
    }

    size_t len = strlen(str);
    if (len >= (size_t)valueLength)
    {
        *err = U_BUFFER_OVERFLOW_ERROR;
        return;
    }

    u_charsToUChars(str, value, (int32_t)(len + 1));
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

// We use whatever ICU give us as the default locale except if it is en_US_POSIX. 
//
// On Apple related platforms (OSX, iOS, tvOS, MacCatalyst), we'll take what the system locale is.  
// On all other platforms we'll map this POSIX locale to Invariant instead. 
// The reason is POSIX locale collation behavior is not desirable at all because it doesn't support case insensitive string comparisons.
const char* DetectDefaultLocaleName(void)
{
    const char* icuLocale = uloc_getDefault();

    if (strcmp(icuLocale, "en_US_POSIX") == 0)
    {
        return "";
    }

    return icuLocale;
}

// GlobalizationNative_GetLocales gets all locale names and store it in the value buffer
// in case of success, it returns the count of the characters stored in value buffer
// in case of failure, it returns negative number.
// if the input value buffer is null, it returns the length needed to store the
// locale names list.
// if the value is not null, it fills the value with locale names separated by the length
// of each name.
int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength)
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

        int32_t localeNameLength = (int32_t)strlen(pLocaleName);

        totalLength += localeNameLength + 1; // add 1 for the name length

        if (value != NULL)
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

int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;

    char localeNameBuffer[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, localeNameBuffer, ULOC_FULLNAME_CAPACITY, true, &status);
    u_charsToUChars_safe(localeNameBuffer, value, valueLength, &status);

    if (U_SUCCESS(status))
    {
        FixupLocaleName(value, valueLength);
    }

    return UErrorCodeToBool(status);
}

int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength)
{
    char localeNameBuffer[ULOC_FULLNAME_CAPACITY];
    UErrorCode status = U_ZERO_ERROR;

    const char* defaultLocale = DetectDefaultLocaleName();

#ifdef __APPLE__
    char* appleLocale = NULL;
    
    if (strcmp(defaultLocale, "") == 0)
    {
        appleLocale = DetectDefaultAppleLocaleName();
        defaultLocale = appleLocale;
    }
#endif

    uloc_getBaseName(defaultLocale, localeNameBuffer, ULOC_FULLNAME_CAPACITY, &status);
    u_charsToUChars_safe(localeNameBuffer, value, valueLength, &status);

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
            u_charsToUChars_safe("_", &value[localeNameLen], valueLength - localeNameLen, &status);
            u_charsToUChars_safe(collationValueTemp, &value[localeNameLen + 1], valueLength - localeNameLen - 1, &status);
        }
    }

#ifdef __APPLE__
    if (appleLocale)
        free(appleLocale);
#endif

    return UErrorCodeToBool(status);
}

// GlobalizationNative_IsPredefinedLocale returns TRUE if ICU has a real data for the locale.
// Otherwise it returns FALSE;

int32_t GlobalizationNative_IsPredefinedLocale(const UChar* localeName)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    if (U_FAILURE(err))
        return false;

    // ures_open returns err = U_ZERO_ERROR when ICU has data for localeName.
    // If it is fake locale, it will return err = U_USING_FALLBACK_WARNING || err = U_USING_DEFAULT_WARNING.
    // Other err values would be just a failure.
    UResourceBundle* uresb = ures_open(NULL, locale, &err);
    ures_close(uresb);

    return err == U_ZERO_ERROR;
}
