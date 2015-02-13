//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    locale.c

Abstract:

    Implementation of locale API functions requested by the PAL.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/cruntime.h"
#include "pal/dbgmsg.h"
#include "pal/locale.h"
#include "pal/utils.h"
#include <time.h>

SET_DEFAULT_DEBUG_CHANNEL(LOCALE);

/* cache the current lcid */
#if ENABLE_DOWNLEVEL_FOR_NLS
static LCID s_lcidCurrent = (LCID)0;
#endif

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

/* cache the current locale */
static CFLocaleRef s_cfLocaleCurrent = NULL;

static CFStringRef* s_CurrencyGroupingSeparatorFromBundle = NULL;
static BOOL s_bFetchedBundleForCurrencyGroupingSeparator = FALSE;

#define ORMORE 0x10

struct FormatPatternMapEntry
{
    UniChar uniChar;
    short count;
    const char *replacement;
    CFIndex length;
};

static const struct FormatPatternMapEntry dateTimeFormatPatternMap[] =
{
    /*
     * This converts Mac format strings as defined here:
     *   http://icu.sourceforge.net/userguide/formatDateTime.html#sdf
     * We translate format patterns into something .NET System.Globalization.DateTimeFormatInfo understands.
     *
     * When the BCL needs to format and parse DateTime instances on the Mac, the DTFI tokens are converted back into CF
     * tokens by FormatStringFromWindowsToMacOS in VM\COMUtilNative.cpp before PAL_FormatDateW and PAL_ParseDateW
     * are called
     */

    /* date format constituents */
    {'G', 1|ORMORE, "g"},    /* era designator */
    {'y', 4|ORMORE, "yyyy"}, /* year */
    {'y', 3,        "yyy"},
    {'y', 2,        "yy"},
    {'y', 1,        "y"},
    {'L', 4|ORMORE, "MMMM"}, /* stand alone month in year */
    {'L', 3,        "MMM"},
    {'L', 2,        "MM"},
    {'L', 1,        "M"},
    {'M', 4|ORMORE, "MMMM"}, /* month in year */
    {'M', 3,        "MMM"},
    {'M', 2,        "MM"},
    {'M', 1,        "M"},
    {'c', 2|ORMORE, "dddd"}, /* stand alone local day of week */
    {'c', 1,        "ddd"},
    {'e', 2|ORMORE, "dddd"}, /* local day of week */
    {'e', 1,        "ddd"},
    {'E', 2|ORMORE, "dddd"}, /* day in week */
    {'E', 1,        "ddd"},
    {'d', 2|ORMORE, "dd"},   /* day in month */
    {'d', 1,        "d"},

    /* time format constituents */
    {'h', 2|ORMORE, "hh"},   /* hour in am/pm (1-12) */
    {'h', 1,        "h"},
    {'K', 2|ORMORE, "hh"},   /* hour in am/pm (0-11) */
    {'K', 1,        "h"},
    {'H', 2|ORMORE, "HH"},   /* hour in day (0-23) */
    {'H', 1,        "H"},
    {'k', 2|ORMORE, "HH"},   /* hour in day (1-24) */
    {'k', 1,        "H"},
    {'m', 2|ORMORE, "mm"},   /* minute in hour */
    {'m', 1,        "m"},
    {'s', 2|ORMORE, "ss"},   /* second in minute */
    {'s', 1,        "s"},
    {'S', 1|ORMORE, "fff"},  /* millisecond */
    {'a', 1|ORMORE, "tt"},   /* am/pm marker */
    {'z', 1|ORMORE, "zzzz"},   /* time zone -0800 */
    {'Z', 1|ORMORE, "zzzz"},   /* time zone PST (treat as -0800) */
    {'v', 1|ORMORE, "zzzz"},   /* time zone PT (treat as -0800) */
    {'V', 1|ORMORE, "zzzz"},   /* time zone PT (treat as -0800) */

    /* these have no equivalent - delete them */
    {'D', 1|ORMORE, ""},     /* day of year */
    {'F', 1|ORMORE, ""},     /* day of week in month */
    {'w', 1|ORMORE, ""},     /* week of year */
    {'W', 1|ORMORE, ""},     /* week of month */
    {'Y', 1|ORMORE, ""},     /* year of "Week of Year" (w) */
    {'Q', 1|ORMORE, ""},     /* quarter of year */
    {'q', 1|ORMORE, ""},     /* stand alone quarter of year */
    {'g', 1|ORMORE, ""},     /* modified julian day "2451334" */
    {'A', 1|ORMORE, ""},     /* milliseconds in day */

    {'\0', 0, NULL}
};

struct CalIdPair
{
    uint        pcCalId;
    CFStringRef macCalId;
};

static const struct CalIdPair supportedCalendars[] =
{
    /*
     * The intersection of Mac and PC supported calendars. Mac locales can use
     * any calendar, so we add supported calendars to the list of possible 
     * calendars for a locale.
     *
     */
    {CAL_GREGORIAN, kCFGregorianCalendar},
    {CAL_JAPAN,     kCFJapaneseCalendar},
    {CAL_THAI,      kCFBuddhistCalendar},
    {CAL_TAIWAN,    kCFChineseCalendar},
    {CAL_HEBREW,    kCFHebrewCalendar},
    {CAL_HIJRI,     kCFIslamicCalendar},  
    {CAL_GREGORIAN_ARABIC, kCFIslamicCivilCalendar} 
};

static const struct CalIdPair requiredCalendars[] =
{
    /*
     * Calendars that locales must support. Note just Gregorian for now
     *
     */
    {CAL_GREGORIAN, kCFGregorianCalendar}
};

static CFComparisonResult (*s_CFStringCompareWithOptionsAndLocale)(CFStringRef, CFStringRef, CFRange, CFOptionFlags, CFLocaleRef) = NULL;

static UniChar * ToUniChar(WCHAR *buffer)
{
  return (UniChar *) buffer;
}

static UniChar * ToUniChar(LPCWSTR buffer)
{
  return (UniChar *) buffer;
}

/*
 * TODO: Callers of this function really should be using weak linking to get values that are 10.5 only
 * to do so we need to start building with XCode 3.0 so we have the 10.5 headers.  Once that happens 
 * we should fix up all the callers of this function.
 */
static BOOL FetchCFTypeRefValueFromBundle(CFStringRef cfBundleName, CFStringRef cfKeyName, CFTypeRef** cfValue)
{
    CFBundleRef b = NULL;

    b = CFBundleGetBundleWithIdentifier(cfBundleName);
    if(b == NULL) 
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return FALSE;
    }

    CFRetain(b);

    *cfValue = (CFTypeRef*) CFBundleGetDataPointerForName(b, cfKeyName);

    CFRelease(b);

    return TRUE;
}

/* 
 * TODO: On OS X 10.5 CFNumberFormatter now has a new property, kCFNumberFormatterCurrencyGroupingSeparator
 * which is what should be set to override the currency separator, unlike OSX 10.4 which uses just
 * kCFNumberFormatterGroupingSeparator.  Since we don't currently build the product using 10.5 headers we can't
 * directly use kCFNumberFormatterCurrencyGroupingSeperator, we have to pull it out of the CoreFoundation bundle
 * at runtime.  When we move to building with XCode 3.0 (with the 10.5 headers) replace this code to use weak
 * linking so we don't have to do all this stuff.
 */
static BOOL FetchCurrencyGroupingSeparatorFromBundle()
{
    CFStringRef* s = NULL;

    if(!FetchCFTypeRefValueFromBundle(CFSTR("com.apple.CoreFoundation"), CFSTR("kCFNumberFormatterCurrencyGroupingSeparator"), (CFTypeRef**) &s)) 
    {
        return FALSE;
    }

    if (s != NULL) 
    {
        CFRetain(*s);

        if (InterlockedCompareExchangePointer(&s_CurrencyGroupingSeparatorFromBundle, s, (CFStringRef *)NULL) != NULL )
        {
            /* somebody beat us to it */
            CFRelease(*s);
        }
    }

    s_bFetchedBundleForCurrencyGroupingSeparator = TRUE;

    return TRUE;
}

static const struct FormatPatternMapEntry *FindFormatPatternMapEntry(UniChar uniChar, const struct FormatPatternMapEntry *map)
{
    while (map->uniChar != '\0')
    {
        if (map->uniChar == uniChar)
        {
            return map;
        }
        map++;
    }
    return NULL;
}

static Boolean CFStringAppendSubstring(CFMutableStringRef cfMutableString, CFStringRef cfString, CFRange cfRange)
{
    CFStringRef cfStringSubstring = CFStringCreateWithSubstring(kCFAllocatorDefault, cfString, cfRange);
    if (cfString == NULL)
    {
        return FALSE;
    }
    CFStringAppend(cfMutableString, cfStringSubstring);
    CFRelease(cfStringSubstring);
    return TRUE;
}

static CFStringRef MapFormatPattern(CFStringRef cfStringFormat, const struct FormatPatternMapEntry *map)
{
    CFMutableStringRef cfMutableString;
    CFIndex length;
    CFStringInlineBuffer cfStringInlineBuffer;
    CFIndex i;

    cfMutableString = CFStringCreateMutable(kCFAllocatorDefault, 0);
    if (cfMutableString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }

    length = CFStringGetLength(cfStringFormat);
    CFStringInitInlineBuffer(cfStringFormat, &cfStringInlineBuffer, CFRangeMake(0, length));
    i = 0;
    while (i < length)
    {
        UniChar uniChar = CFStringGetCharacterFromInlineBuffer(&cfStringInlineBuffer, i++);
        if (uniChar == '\'')
        {
            CFRange cfRangeFound;
            if (CFStringFindWithOptions(cfStringFormat, CFSTR("'"), CFRangeMake(i, length - i), 0, &cfRangeFound))
            {
                CFRange cfRangeQuotedString; /* including quotes */
                cfRangeQuotedString.location = i - 1;
                cfRangeQuotedString.length = cfRangeFound.location - cfRangeQuotedString.location + cfRangeFound.length;
                if (!CFStringAppendSubstring(cfMutableString, cfStringFormat, cfRangeQuotedString))
                {
                    SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    CFRelease(cfMutableString);
                    return NULL;
                }
                i = cfRangeQuotedString.location + cfRangeQuotedString.length;
            }
            else // no ending quote found - assume up to end of string; add quote explicitly
            {
                WARN("no ending quote found\n");
                CFRange cfRangeQuotedString; /* including leading quote */
                cfRangeQuotedString.location = i - 1;
                cfRangeQuotedString.length = length - cfRangeQuotedString.location;
                if (!CFStringAppendSubstring(cfMutableString, cfStringFormat, cfRangeQuotedString))
                {
                    SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    CFRelease(cfMutableString);
                    return NULL;
                }
                CFStringAppend(cfMutableString, CFSTR("'"));
                i = length;
            }
        }
        else if (uniChar == '"')
        {
            UniChar uniChars[] = {'"', '\'', '"'};
            CFStringAppendCharacters(cfMutableString, uniChars, sizeof(uniChars)/sizeof(UniChar));
        }
        else
        {
            const struct FormatPatternMapEntry *entry = FindFormatPatternMapEntry(uniChar, map);
            if (entry == NULL)
            {
                CFStringAppendCharacters(cfMutableString, &uniChar, 1);
            }
            else
            {
                int count = 1;
                while (CFStringGetCharacterFromInlineBuffer(&cfStringInlineBuffer, i) == uniChar)
                {
                    count++;
                    i++;
                }

                /* find the map entry corresponding to the count we have found (there must be one) */
                while (!(((entry->count & ORMORE) != 0 && count >= (entry->count & ~ORMORE)) ||
                         ((entry->count & ORMORE) == 0 && count == entry->count)))
                {
                    entry++;
                    if (entry->uniChar != uniChar)
                    {
                        ASSERT("no map entry found for character %d, count %d\n", uniChar, count);
                        SetLastError(ERROR_INTERNAL_ERROR);
                        CFRelease(cfMutableString);
                        return NULL;
                    }
                }

                /* replace by what we find in the map entry */
                CFStringAppendCString(cfMutableString, entry->replacement, kCFStringEncodingASCII);
            }
        }
    }

    return cfMutableString;
}

/*
 * Convert Windows "-" locale separator to Mac "_". Corruption in CFDictionary 
 * backing store for locale data may occur when we fail to convert the Windows 
 * locale separator to "-" to "_", which mac expects. Failure to convert means
 * we could simultaneously have "en_US" locale corresponding to current user 
 * locale and "en-US", created from string passed from managed side. This 
 * doesn't repro on 10.5.
 */
static CFStringRef CFStringCreateMacFormattedLocaleName(LPCWSTR lpLocaleName)
{
    if (lpLocaleName == NULL || *lpLocaleName == L'\0')
    {
        CFStringRef emptyString = CFSTR("");
        CFRetain(emptyString);
        return emptyString;
    }

    CFMutableStringRef cfMutableLocaleName = CFStringCreateMutable(kCFAllocatorDefault, 0);
    if (cfMutableLocaleName == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }

    CFStringAppendCharacters(cfMutableLocaleName, ToUniChar(lpLocaleName), PAL_wcslen(lpLocaleName));

    CFStringFindAndReplace(cfMutableLocaleName,
                                CFSTR("-"),
                                CFSTR("_"),
                                CFRangeMake(0,CFStringGetLength(cfMutableLocaleName)),
                                0);

    return cfMutableLocaleName;
}

static CFLocaleRef CFLocaleGetCurrent()
{
    // if we're asking for the current locale, avoid the call to CFLocaleCreate
    if ( s_cfLocaleCurrent != NULL)
    {
        return s_cfLocaleCurrent;
    }

    if (s_cfLocaleCurrent == NULL)
    {
        // The very first call to CFLocaleCopyCurrent may trigger a call to dlopen.
        // Calls to dlopen (for dynamic libraries that are not yet in the process)
        // must happen outside the PAL to avoid gdb hangs.  See GetProcAddress
        // for details.
        PAL_Leave(PAL_BoundaryBottom);
        CFLocaleRef cfLocale = CFLocaleCopyCurrent();
        PAL_Enter(PAL_BoundaryBottom);
        if (InterlockedCompareExchangePointer(&s_cfLocaleCurrent, cfLocale, NULL) != NULL )
        {
            /* somebody beat us to it */
            CFRelease(cfLocale);
        }
    }
    return s_cfLocaleCurrent;
}

//
// CFLocaleCreateFromLocaleName -
// returns a CFLocaleRef corresponding to the requested lpLocaleName.
//
// Expected: lpLocaleName is in modified-Windows Culture Name form: "en" or "en_US". e.g., the
//           only difference from true windows form is the "-" has been swapped for the "_".
//
// User overrides:
//           CoreCLR always respects user overrides and there is currently no public API in
//           CoreCLR to disable user overrides.  When the supplied lpLocaleName matches the
//           localeId/RegionId portion ("en_US" portion of "en_US@foo=bar") of the current
//           OS (CFLocaleCopyCurrent) locale string ID then the Current OS locale will be returned.
//
static CFLocaleRef CFLocaleCreateFromLocaleName(LPCWSTR lpLocaleName)
{
    if (lpLocaleName == NULL || *lpLocaleName == L'\0') // invariant
    {
        CFLocaleRef cfLocale = CFLocaleGetSystem();
        CFRetain(cfLocale);
        return cfLocale;
    }
    CFLocaleRef cfRetVal = NULL;
    CFStringRef cfCurrentAbbrevLocaleName = NULL;
    CFStringCompareFlags cfStringCompareFlags = kCFCompareCaseInsensitive;

    CFStringRef cfLocaleName = CFStringCreateMacFormattedLocaleName(lpLocaleName);
    if (cfLocaleName == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    // If requested locale name matches current locale, use that so we can pick up user
    // preferences. Otherwise, create the locale

    CFLocaleRef cfCurrentLocale;
    cfCurrentLocale = CFLocaleGetCurrent();
    if (cfCurrentLocale == NULL)
    {
         SetLastError(ERROR_OUTOFMEMORY);
         goto EXIT;
    }

    // returns the string representation of an arbitrary locale identifier.
    // examples: "English", "en", "en_US", "en_US@calendar=japanese"
    CFStringRef cfCurrentLocaleName;
    cfCurrentLocaleName = CFLocaleGetIdentifier(cfCurrentLocale);
    CFComparisonResult result;

    // strip off user overrides from the identifier for purposes of the "is this CurrentCulture" string comparison
    CFRange indexOfAtSign;
    indexOfAtSign = CFStringFind(cfCurrentLocaleName, CFSTR("@"), cfStringCompareFlags);
    if (indexOfAtSign.location != kCFNotFound)
    {
        // cfCurrentAbbrevLocaleName = "en_us" <- "en_us@calendar=japanese"
        cfCurrentAbbrevLocaleName = CFStringCreateWithSubstring(kCFAllocatorDefault, cfCurrentLocaleName,
                                                                            CFRangeMake(0, indexOfAtSign.location));
        if (cfCurrentAbbrevLocaleName == NULL)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            goto EXIT;
        }

        result = CFStringCompare(cfLocaleName, cfCurrentAbbrevLocaleName, cfStringCompareFlags);        
    }
    else
    {
        result = CFStringCompare(cfLocaleName, cfCurrentLocaleName, cfStringCompareFlags);
    }


    if (result == kCFCompareEqualTo)
    {   // use the CurrentCulture with potential user overrides
        // for instance "ja_JP" may map to "ja_JP@calendar=japanese" on this computer if the user has customized settings
        cfRetVal = cfCurrentLocale;
        CFRetain(cfRetVal);
    }
    else
    {   // construct a locale with default system backstop settings
        // for instance "ja_JP" will map to the default "ja_JP" (which implies "ja_JP@calendar=Gregorian")
        cfRetVal=CFLocaleCreate(kCFAllocatorDefault,cfLocaleName);
        if (cfRetVal == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
    }

EXIT:
    if (cfCurrentAbbrevLocaleName != NULL)
    {
        CFRelease(cfCurrentAbbrevLocaleName);
    }
    if (cfLocaleName != NULL)
    {
        CFRelease(cfLocaleName);
    }
    return cfRetVal;
}

static CFStringRef CFStringCreateWithDateFormat(CFLocaleRef cfLocale, CFDateFormatterStyle dateStyle, CFDateFormatterStyle timeStyle)
{
    CFDateFormatterRef cfDateFormatter;
    CFStringRef cfString;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, dateStyle, timeStyle);
    if (cfDateFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }
    cfString = MapFormatPattern(CFDateFormatterGetFormat(cfDateFormatter), dateTimeFormatPatternMap);
    CFRelease(cfDateFormatter);
    return cfString;
}

static CFStringRef CFStringCreateFormattedDate(CFLocaleRef cfLocale, CFStringRef cfStringFormat, int month, int day)
{
    CFDateFormatterRef cfDateFormatter;
    CFGregorianDate cfGregorianDate;
    CFStringRef cfString;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, kCFDateFormatterNoStyle, kCFDateFormatterNoStyle);
    if (cfDateFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }
    CFDateFormatterSetFormat(cfDateFormatter, cfStringFormat);

    cfGregorianDate.year = 2007;
    cfGregorianDate.month = month;
    cfGregorianDate.day = day;
    cfGregorianDate.hour = 12;
    cfGregorianDate.minute = 0;
    cfGregorianDate.second = 0.0;

    cfString = CFDateFormatterCreateStringWithAbsoluteTime(kCFAllocatorDefault, cfDateFormatter, CFGregorianDateGetAbsoluteTime(cfGregorianDate, NULL));
    CFRelease(cfDateFormatter);
    if (cfString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }
    return cfString;
}



#else // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_XLOCALE
#include <xlocale.h>
#else // HAVE_XLOCALE
#include <locale.h>
#endif // HAVE_XLOCALE

LCID GetLCIDFromUnixLocaleName(LPCSTR lpUnixLocaleName);
LPSTR GetUnixLocaleNameFromLCID(const LCID localeID);

/*
 * This structure is used to build a mapping between UNIX-style locales and
 * Windows style language IDs.  In this structure, the 'locale' variable is
 * the UNIX-like locale name.  The langID variable is the Windows language ID.
 */
struct LANGID_map_entry
{
  CHAR *locale;
  LANGID langID;
};

const static struct LANGID_map_entry LANGID_map[]=
{
  {"", 0x0000}, /* Language Neutral */
  {"", 0x007f}, /* invariant locale */
  {"", 0x0400}, /* Process or User Default Language */
  {"", 0x0800}, /* System Default Language */
  {"", 0x0436}, /* Afrikaans */
  {"", 0x041c}, /* Albanian */
  {"", 0x0401}, /* Arabic (Saudi Arabia) */
  {"", 0x0801}, /* Arabic (Iraq) */
  {"", 0x0c01}, /* Arabic (Egypt) */
  {"", 0x1001}, /* Arabic (Libya) */
  {"", 0x1401}, /* Arabic (Algeria) */
  {"", 0x1801}, /* Arabic (Morocco) */
  {"", 0x1c01}, /* Arabic (Tunisia) */
  {"", 0x2001}, /* Arabic (Oman) */
  {"", 0x2401}, /* Arabic (Yemen) */
  {"", 0x2801}, /* Arabic (Syria) */
  {"", 0x2c01}, /* Arabic (Jordan) */
  {"", 0x3001}, /* Arabic (Lebanon) */
  {"", 0x3401}, /* Arabic (Kuwait) */
  {"", 0x3801}, /* Arabic (U.A.E.) */
  {"", 0x3c01}, /* Arabic (Bahrain) */
  {"", 0x4001}, /* Arabic (Qatar) */
  {"", 0x042b}, /* Windows 2000: Armenian. This is Unicode only. */
  {"", 0x044d}, /* Windows 2000: Assamese. This is Unicode only. */
  {"", 0x042c}, /* Azeri (Latin) */
  {"", 0x082c}, /* Azeri (Cyrillic) */
  {"", 0x042d}, /* Basque */
  {"", 0x0423}, /* Belarussian */
  {"", 0x0445}, /* Windows 2000: Bengali. This is Unicode only. */
  {"", 0x0402}, /* Bulgarian */
  {"", 0x0455}, /* Burmese */
  {"", 0x0403}, /* Catalan */
  {ZH_TW_LOCALE_NAME, 0x0404}, /* Chinese (Taiwan) */
  {"", 0x0804}, /* Chinese (PRC) */
  {"", 0x0c04}, /* Chinese (Hong Kong SAR, PRC) */
  {"", 0x1004}, /* Chinese (Singapore) */
  {"", 0x1404}, /* Chinese (Macau SAR) */
  {"", 0x041a}, /* Croatian */
  {"", 0x0405}, /* Czech */
  {"", 0x0406}, /* Danish */
  {"", 0x0413}, /* Dutch (Netherlands) */
  {"", 0x0813}, /* Dutch (Belgium) */
  {ISO_NAME("en_US", "8859", "1"), 0x0409}, /* English (United States) */
  {ISO_NAME("en_GB", "8859", "1"), 0x0809}, /* English (United Kingdom) */
  {"", 0x0c09}, /* English (Australian) */
  {ISO_NAME("en_CA", "8859", "1"), 0x1009}, /* English (Canadian) */
  {"", 0x1409}, /* English (New Zealand) */
  {"", 0x1809}, /* English (Ireland) */
  {"", 0x1c09}, /* English (South Africa) */
  {"", 0x2009}, /* English (Jamaica) */
  {"", 0x2409}, /* English (Caribbean) */
  {"", 0x2809}, /* English (Belize) */
  {"", 0x2c09}, /* English (Trinidad) */
  {"", 0x3009}, /* English (Zimbabwe) */
  {"", 0x3409}, /* English (Philippines) */
  {"", 0x0425}, /* Estonian */
  {"", 0x0438}, /* Faeroese */
  {"", 0x0429}, /* Farsi */
  {"", 0x040b}, /* Finnish */
  {"", 0x040c}, /* French (Standard) */
  {"", 0x080c}, /* French (Belgian) */
  {"", 0x0c0c}, /* French (Canadian) */
  {"", 0x100c}, /* French (Switzerland) */
  {"", 0x140c}, /* French (Luxembourg) */
  {"", 0x180c}, /* French (Monaco) */
  {"", 0x0437}, /* Windows 2000: Georgian. This is Unicode only. */
  {"", 0x0407}, /* German (Standard) */
  {"", 0x0807}, /* German (Switzerland) */
  {"", 0x0c07}, /* German (Austria) */
  {"", 0x1007}, /* German (Luxembourg) */
  {"", 0x1407}, /* German (Liechtenstein) */
  {"", 0x0408}, /* Greek */
  {"", 0x0447}, /* Windows 2000: Gujarati. This is Unicode only. */
  {"", 0x040d}, /* Hebrew */
  {"", 0x0439}, /* Windows 2000: Hindi. This is Unicode only. */
  {"", 0x040e}, /* Hungarian */
  {"", 0x040f}, /* Icelandic */
  {"", 0x0421}, /* Indonesian */
  {"", 0x0410}, /* Italian (Standard) */
  {"", 0x0810}, /* Italian (Switzerland) */
  {JA_JP_LOCALE_NAME, 0x0411}, /* Japanese */
  {"", 0x044b}, /* Windows 2000: Kannada. This is Unicode only. */
  {"", 0x0860}, /* Kashmiri (India) */
  {"", 0x043f}, /* Kazakh */
  {"", 0x0457}, /* Windows 2000: Konkani. This is Unicode only. */
  {KO_KR_LOCALE_NAME, 0x0412}, /* Korean */
  {"", 0x0812}, /* Korean (Johab) */
  {"", 0x0426}, /* Latvian */
  {"", 0x0427}, /* Lithuanian */
  {"", 0x0827}, /* Lithuanian (Classic) */
  {"", 0x042f}, /* Macedonian */
  {"", 0x043e}, /* Malay (Malaysian) */
  {"", 0x083e}, /* Malay (Brunel Durassalam) */
  {"", 0x044c}, /* Windows 2000: Malayalam. This is Unicode only. */
  {"", 0x0458}, /* Manipuri */
  {"", 0x044e}, /* Windows 2000: Marathi. This is Unicode only. */
  {"", 0x0861}, /* Windows 2000: Nepali (India). This is Unicode only. */
  {"", 0x0414}, /* Norwegian (Bokmal) */
  {"", 0x0814}, /* Norwegian (Nynorsk) */
  {"", 0x0448}, /* Windows 2000: Oriya. This is Unicode only. */
  {"", 0x0415}, /* Polish */
  {"", 0x0416}, /* Portuguese (Brazil) */
  {"", 0x0816}, /* Portuguese (Standard) */
  {"", 0x0446}, /* Windows 2000: Punjabi. This is Unicode only. */
  {"", 0x0418}, /* Romanian */
  {"ru_RU.KOI8-R", 0x0419}, /* Russian */
  {"", 0x044f}, /* Windows 2000: Sanskrit. This is Unicode only. */
  {"", 0x0c1a}, /* Serbian (Cyrillic) */
  {"", 0x081a}, /* Serbian (Latin) */
  {"", 0x0459}, /* Sindhi */
  {"", 0x041b}, /* Slovak */
  {"", 0x0424}, /* Slovenian */
  {"", 0x040a}, /* Spanish (Traditional Sort) */
  {"", 0x080a}, /* Spanish (Mexican) */
  {"", 0x0c0a}, /* Spanish (Modern Sort) */
  {"", 0x100a}, /* Spanish (Guatemala) */
  {"", 0x140a}, /* Spanish (Costa Rica) */
  {"", 0x180a}, /* Spanish (Panama) */
  {"", 0x1c0a}, /* Spanish (Dominican Republic) */
  {"", 0x200a}, /* Spanish (Venezuela) */
  {"", 0x240a}, /* Spanish (Colombia) */
  {"", 0x280a}, /* Spanish (Peru) */
  {"", 0x2c0a}, /* Spanish (Argentina) */
  {"", 0x300a}, /* Spanish (Ecuador) */
  {"", 0x340a}, /* Spanish (Chile) */
  {"", 0x380a}, /* Spanish (Uruguay) */
  {"", 0x3c0a}, /* Spanish (Paraguay) */
  {"", 0x400a}, /* Spanish (Bolivia) */
  {"", 0x440a}, /* Spanish (El Salvador) */
  {"", 0x480a}, /* Spanish (Honduras) */
  {"", 0x4c0a}, /* Spanish (Nicaragua) */
  {"", 0x500a}, /* Spanish (Puerto Rico) */
  {"", 0x0430}, /* Sutu */
  {"", 0x0441}, /* Swahili (Kenya) */
  {"", 0x041d}, /* Swedish */
  {"", 0x081d}, /* Swedish (Finland) */
  {"", 0x0449}, /* Windows 2000: Tamil. This is Unicode only. */
  {"", 0x0444}, /* Tatar (Tatarstan) */
  {"", 0x044a}, /* Windows 2000: Telugu. This is Unicode only. */
  {"", 0x041e}, /* Thai */
  {"", 0x041f}, /* Turkish */
  {"", 0x0422}, /* Ukrainian */
  {"", 0x0420}, /* Urdu (Pakistan) */
  {"", 0x0820}, /* Urdu (India) */
  {"", 0x0443}, /* Uzbek (Latin) */
  {"", 0x0843}, /* Uzbek (Cyrillic) */
  {"", 0x042a}, /* Vietnamese */
};

/*++
Function:
GetLCIDFromUnixLocaleName

Return:
Non-zero value on success, else returns 0 and sets the error to
ERROR_INVALID_PARAMETER.

--*/
LCID GetLCIDFromUnixLocaleName(LPCSTR lpUnixLocaleName)
{
  struct LANGID_map_entry map_entry;
  INT nNumOfLangids = sizeof(LANGID_map)/sizeof(map_entry);
  LCID localeID = (LCID)0;
  INT i;

  /*
   * Iterate through the map entries to find the LCID for the given locale
   * name.
   */
  for(i = 0; i < nNumOfLangids; i++)
    {
      if(strcmp(LANGID_map[i].locale, lpUnixLocaleName) == 0)
    {
      localeID = MAKELCID(LANGID_map[i].langID, SORT_DEFAULT);
      break;
    }
    }

  if(i == nNumOfLangids)
    {
      SetLastError(ERROR_INVALID_PARAMETER);
    }

  /*hardcode the locale to US_ENGLISH for user default locale*/
  if(!strcmp(lpUnixLocaleName,"C"))
      localeID  = LOCALE_US_ENGLISH;
  return localeID;
}


/*++
Function:
GetUnixLocaleNameFromLCID

Return:
Non-null character pointer to the locale name.  If the argument is invalid,
then a null pointer is returned and the error is set to
ERROR_INVALID_PARAMETER.
--*/
LPSTR GetUnixLocaleNameFromLCID(const LCID localeID)
{
  LPSTR lpLocaleName = NULL;
  struct LANGID_map_entry map_entry;
  INT nNumOfLangids = sizeof(LANGID_map)/sizeof(map_entry);
  LANGID langID = LANGIDFROMLCID(localeID);
  INT i;


  /*
   * Iterate through the map entries to find the UNIX-like locale name
   * for the given locale id.
   */
  for(i = 0; i < nNumOfLangids; i++)
  {
      if(LANGID_map[i].langID == langID)
     {
         lpLocaleName = LANGID_map[i].locale;
         break;
      }
  }

  /*hardcode the locale to US_ENGLISH for user default*/
  if(localeID == LOCALE_USER_DEFAULT)
  {
      lpLocaleName = ISO_NAME("en_US", "8859", "1");
  }

  if(lpLocaleName == NULL)
  {
      SetLastError(ERROR_INVALID_PARAMETER);
  }
  return lpLocaleName;
}

#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  GetUserDefaultLocaleName

See MSDN doc.
--*/
int
PALAPI
GetUserDefaultLocaleName(
           LPWSTR lpLocaleName,
           int cchLocaleName)
{
    PERF_ENTRY(GetUserDefaultLocaleName);
    ENTRY("GetUserDefaultLocaleName(lpLocaleName=%p,cchLocaleName=%d)\n",lpLocaleName,cchLocaleName);

    int iRetVal=0;

    CFLocaleRef cfLocale = CFLocaleGetCurrent();
    if (cfLocale == NULL)
    {
         SetLastError(ERROR_OUTOFMEMORY);
         goto EXIT;
    }


    CFStringRef cfLocaleName;
    cfLocaleName = CFLocaleGetIdentifier(cfLocale);

    if (cfLocaleName == NULL)
    {
         SetLastError(ERROR_OUTOFMEMORY);
         goto EXIT;
    }
    
    int iStrLen;
    iStrLen = CFStringGetLength(cfLocaleName);

    if (cchLocaleName <= iStrLen)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto EXIT;
    }
    CFStringGetCharacters(cfLocaleName,CFRangeMake(0,iStrLen),(UniChar*)lpLocaleName);
    lpLocaleName[iStrLen]=L'\0';
    iRetVal=iStrLen+1;

EXIT:
    LOGEXIT("GetUserDefaultLocaleName returns  %d\n", iRetVal);
    PERF_EXIT(GetUserDefaultLocaleName);
    return iRetVal;
}
#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

#if ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  GetUserDefaultLCID

See MSDN doc.
--*/
LCID
PALAPI
GetUserDefaultLCID(
           void)
{
    PERF_ENTRY(GetUserDefaultLCID);
    ENTRY("GetUserDefaultLCID()\n");

    // Getting the current locale's LCID is a fairly expensive operation.
    // There are places that call this in tight loops - the prominent example
    // being the CLR's finalizer thread, which calls this once per object it
    // processes.  Therefore, once we ran this, we'll remember the result for
    // future calls.
    if (s_lcidCurrent == (LCID)0)
    {
        LPCSTR lpCurrentLocale;

        if ( !CODEPAGEAcquireReadLock() )
        {
            /* Could not get a read lock */
            SetLastError(ERROR_INTERNAL_ERROR);
            goto EXIT;
        }

        /* get current locale */
#if HAVE_XLOCALE
        lpCurrentLocale = querylocale(LC_CTYPE_MASK, LC_GLOBAL_LOCALE);
#else // HAVE_XLOCALE
        lpCurrentLocale = setlocale(LC_CTYPE,NULL);
#endif // HAVE_XLOCALE

        if (lpCurrentLocale == NULL)
        {
            ASSERT("failed to get current locale\n");
            if( !CODEPAGEReleaseLock() )
            {
                ERROR( "Unable to release the readwrite lock\n" );
            }
            SetLastError(ERROR_INTERNAL_ERROR);
            goto EXIT;
        }

        s_lcidCurrent = GetLCIDFromUnixLocaleName(lpCurrentLocale);

        if( !CODEPAGEReleaseLock() )
        {
            ERROR( "Unable to release the readwrite lock\n" );
        }

        /* harcoded value for lcid in case the app is not internationalized and 
           does not have a valid lcid*/
        if (s_lcidCurrent == (LCID)0)
        {
            /* hardcoded value for user default langid taken from the langid map */
            s_lcidCurrent = LOCALE_US_ENGLISH;
        }
    }

EXIT:
    LOGEXIT("GetUserDefaultLCID returns LCID %#04x\n", s_lcidCurrent);
    PERF_EXIT(GetUserDefaultLCID);
    return s_lcidCurrent;
}



/*++
Function:
  GetUserDefaultLangID

See MSDN doc.
--*/
LANGID
PALAPI
GetUserDefaultLangID(
             void)
{
    LANGID langID = (LANGID)0; /* return value */
    PERF_ENTRY(GetUserDefaultLangID);
    ENTRY("GetUserDefaultLangID()\n");
    langID = LANGIDFROMLCID(GetUserDefaultLCID());
    LOGEXIT("GetUserDefaultLangID returns LANGID %#04x\n", langID);
    PERF_EXIT(GetUserDefaultLangID);
    return langID;
}


/*++
Function:
  GetSystemDefaultLangID

See MSDN doc.
--*/
LANGID
PALAPI
GetSystemDefaultLangID(
               void)
{
    LANGID langID = (LANGID)0; /* return value */
    PERF_ENTRY(GetSystemDefaultLangID);
    ENTRY("GetSystemDefaultLangID()\n");
    // There is no concept of user vs system locale
    langID = GetUserDefaultLangID();
    LOGEXIT("GetSystemDefaultLangID returns LANGID %#04x\n", langID);
    PERF_EXIT(GetSystemDefaultLangID);
    return langID;
}


/*++
Function:
  SetThreadLocale

See MSDN doc.
--*/
BOOL
PALAPI
SetThreadLocale(
        IN LCID Locale)
{
  BOOL bReturnValue = FALSE;
  LPSTR lpLocaleName;

  PERF_ENTRY(SetThreadLocale);
  ENTRY("SetThreadLocale(Locale=%#04x)\n", Locale);

  /* check if the sorting bit is set or not*/

  if(LANGIDFROMLCID(Locale) != Locale)
  {
      /*The sorting bit is set to a value other than SORT_DEFAULT
       * which we dont support at this stage*/
      ASSERT("Locale(%#04x) parameter is invalid\n",Locale);
      SetLastError(ERROR_INVALID_PARAMETER);
      goto EXIT;
  }

  lpLocaleName = GetUnixLocaleNameFromLCID(Locale);

  if((lpLocaleName != NULL) && (strcmp(lpLocaleName, "") != 0))
  {
#if HAVE_XLOCALE
      locale_t loc = NULL, oldloc = NULL;
#endif // HAVE_XLOCALE

      if ( !CODEPAGEAcquireWriteLock() )
      {
          /* Could not get a write lock */
          SetLastError(ERROR_INTERNAL_ERROR);
          goto EXIT;
      }

#if HAVE_XLOCALE
      if((loc = newlocale(LC_CTYPE_MASK, lpLocaleName, NULL)) != NULL)
      {
          oldloc = uselocale(loc);
          bReturnValue = TRUE;
          if (oldloc != LC_GLOBAL_LOCALE)
          {
              freelocale(oldloc);
          }
      }
      else
      {
          ASSERT("newlocale failed for localename %s\n",lpLocaleName);
          SetLastError(ERROR_INTERNAL_ERROR);
      }
#else // HAVE_XLOCALE
      if(setlocale(LC_CTYPE,lpLocaleName))
      {
          bReturnValue = TRUE;
      }
      else
      {
          ASSERT("setlocale failed for localename %s\n",lpLocaleName);
          SetLastError(ERROR_INTERNAL_ERROR);
      }
#endif // HAVE_XLOCALE

      if( !CODEPAGEReleaseLock() )
      {
          ERROR( "Unable to release the readwrite lock\n" );
      }
  }
  else
  {
      SetLastError(ERROR_INVALID_PARAMETER);
  }
EXIT:
  LOGEXIT("SetThreadLocale returning BOOL %d)\n", bReturnValue);
  PERF_EXIT(SetThreadLocale);
  return bReturnValue;
}


/*++
Function:
  GetThreadLocale

  See comment at the top of SetThreadLocale.

See MSDN doc.
--*/
LCID
PALAPI
GetThreadLocale(
        void)
{
  LCID lcID = (LCID)0; /* return value */
  LPCSTR lpCurrentLocale;
#if HAVE_XLOCALE
  locale_t loc = NULL;
#endif // HAVE_XLOCALE

  PERF_ENTRY(GetThreadLocale);
  ENTRY("GetThreadLocale()\n");

  if ( !CODEPAGEAcquireReadLock() )
  {
      /* Could not get a read lock */
      SetLastError(ERROR_INTERNAL_ERROR);
      goto EXIT;
  }

  /* get current locale */
#if HAVE_XLOCALE
  loc = uselocale(NULL);
  if((lpCurrentLocale = querylocale(LC_CTYPE_MASK, loc)) == NULL)
  {
    ASSERT("querylocale failed to give currentlocale\n");
    SetLastError(ERROR_INTERNAL_ERROR);
  }
#else // HAVE_XLOCALE
  if((lpCurrentLocale = setlocale(LC_CTYPE,0)) == NULL)
  {
    ASSERT("setlocale failed to give currentlocale\n");
    SetLastError(ERROR_INTERNAL_ERROR);
  }
#endif // HAVE_XLOCALE
  else
  {
    lcID = GetLCIDFromUnixLocaleName(lpCurrentLocale);
  }

  if( !CODEPAGEReleaseLock() )
  {
      ERROR( "Unable to release the readwrite lock\n" );
  }

EXIT:

  LOGEXIT("GetThreadLocale returning %#04x\n", lcID);
  PERF_EXIT(GetThreadLocale);
  return lcID;
}

/*++
Function:
  IsValidLocale

See MSDN doc.
--*/
BOOL
PALAPI
IsValidLocale(
          IN LCID Locale,
          IN DWORD dwFlags)
{
  BOOL bReturnValue = FALSE;
#if !HAVE_XLOCALE
  LPSTR lpBuf = NULL;
  LPSTR lpCurrentLocale = NULL;
#endif // !HAVE_XLOCALE
  LPSTR lpLocaleName = NULL;

  PERF_ENTRY(IsValidLocale);
  ENTRY("IsValidLocale(Locale=%#04x, dwFlags=%#x)\n", Locale, dwFlags);

   /* check if the sorting bit is set or not*/

  if((LANGIDFROMLCID(Locale) != Locale) || (Locale == LOCALE_USER_DEFAULT))
  {
      /* if LANGIDFROMLCID(Locale) is different from Locale then
       * sorting bit is set to a value other than SORT_DEFAULT which
       * we don't support at this stage */
      LOGEXIT ("IsValidLocale returns BOOL %d\n",bReturnValue);
      PERF_EXIT(IsValidLocale);
      return bReturnValue;
  }

  /*
   * First, lets do the check to see if the locale is supported.
   */
  if((dwFlags & LCID_SUPPORTED) || (dwFlags & LCID_INSTALLED))
  {
      lpLocaleName = GetUnixLocaleNameFromLCID(Locale);

      if((lpLocaleName != NULL) && (strcmp(lpLocaleName, "") != 0))
      {
          bReturnValue = TRUE;
      }
  }

  /*
   * Now lets check if the locale is installed, if needed.
   */
  if((dwFlags & LCID_INSTALLED) && bReturnValue)
  {
#if HAVE_XLOCALE
      locale_t loc = NULL;
#endif // HAVE_XLOCALE

      /* Need a write lock. */
      if ( !CODEPAGEAcquireWriteLock() )
      {
          /* Could not get a write lock */  
          SetLastError(ERROR_INTERNAL_ERROR);
          goto EXIT;
      }    

#if HAVE_XLOCALE
      if((loc = newlocale(LC_CTYPE_MASK, lpLocaleName, NULL)) == NULL)
      {
          bReturnValue = FALSE;
      }
      else
      {
          freelocale(loc);
      }
#else // HAVE_XLOCALE
      /*
       * To actually test if a locale is installed, we will check the output
       * of the setlocale() function.
       */
      if((lpCurrentLocale = setlocale(LC_CTYPE, 0)) != NULL)
      {
          lpBuf = PAL__strdup(lpCurrentLocale);
          
          if (lpBuf != NULL)
          {
              if(setlocale(LC_CTYPE, lpLocaleName) == NULL)
              {
                  bReturnValue = FALSE;
              }
              else
              {
                  /* reset locale */
                  setlocale(LC_CTYPE, lpBuf);
              }

              PAL_free(lpBuf);
          }
          else
          {
              /* Could not allocate memory. */
              bReturnValue = FALSE;
              ERROR("Could not allocate memory for saving current locale.\n");
              SetLastError(ERROR_INTERNAL_ERROR);
          }
      }
      else
      {
          /* setlocale failed... */
          bReturnValue = FALSE;
          ASSERT("setlocale failed\n");
          SetLastError(ERROR_INTERNAL_ERROR);
      }
#endif // HAVE_XLOCALE

      if( !CODEPAGEReleaseLock() )
      {
          ERROR( "Unable to release the readwrite lock\n" );
      }
  }

EXIT:
  LOGEXIT ("IsValidLocale returns BOOL %d\n",bReturnValue);
  PERF_EXIT(IsValidLocale);
  return bReturnValue;
}
#endif // ENABLE_DOWNLEVEL_FOR_NLS

DWORD CalendarInfoHelper(CALID Calendar)
{
   switch(Calendar)
   {
       case CAL_GREGORIAN :
       case CAL_GREGORIAN_US:
       case CAL_GREGORIAN_ME_FRENCH:
       case CAL_GREGORIAN_ARABIC:
       case CAL_GREGORIAN_XLIT_ENGLISH:
       case CAL_GREGORIAN_XLIT_FRENCH:
                    return 2029;

       case CAL_JAPAN:
       case CAL_TAIWAN:
                    return  99;

       case CAL_KOREA:
                    return 4362;

       case CAL_HIJRI:
                    return 1451;

       case CAL_THAI:
                    return 2572;

       case CAL_HEBREW:
                    return 5790;

       default:
                ERROR("Error Calendar(%d) parameter is invalid\n",Calendar);
                SetLastError(ERROR_INVALID_PARAMETER);
                return 0;
   }
}


static CFStringRef CFStringCreateYearMonth(CFLocaleRef cfLocale)
{
    CFStringRef cfString = NULL;
    CFStringRef cfStringLanguageCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode);
    if (cfStringLanguageCode != NULL)
    {
        CFRetain(cfStringLanguageCode);
    }
    else
    {
        // should always be present
        SetLastError(ERROR_OUTOFMEMORY);
        return NULL;
    }


    if ( (CFStringCompare(cfStringLanguageCode, CFSTR("zh"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) ||
         (CFStringCompare(cfStringLanguageCode, CFSTR("ja"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) )
    {
        cfString = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                                        CFSTR("yyyy'%C'M'%C'"), 0x5e74, 0x6708);
    }
    else if ( CFStringCompare(cfStringLanguageCode, CFSTR("ko"), kCFCompareCaseInsensitive) == kCFCompareEqualTo )
    {
        cfString = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
                                                        CFSTR("yyyy'%C' M'%C'"), 0xb144, 0xc6d4);
    }
    else
    {
        cfString = CFSTR("MMMM, yyyy");
        CFRetain(cfString);
    }

    CFRelease(cfStringLanguageCode);
    return cfString;
}


#ifdef  ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  GetCalendarInfoW

See MSDN doc.
--*/
int
PALAPI
GetCalendarInfoW(
         IN LCID Locale,
         IN CALID Calendar,
         IN CALTYPE CalType,
         OUT LPWSTR lpCalData,
         IN int cchData,
         OUT LPDWORD lpValue)
{
   int nRetValue = sizeof(DWORD);
   PERF_ENTRY(GetCalendarInfoW);
   ENTRY("GetCalendarInfoW(Locale=%#04x, Calendar=%d, CalType=%d, "
         "lpCalData=%p, cchData=%d, lpValue=%p)\n",
          Locale, Calendar, CalType, lpCalData, cchData, lpValue);

   if((lpValue != NULL) && (Locale == LOCALE_USER_DEFAULT)
       && (lpCalData == NULL) &&(cchData == 0) &&
      (CalType == (CAL_ITWODIGITYEARMAX|CAL_RETURN_NUMBER)))
   {
        *lpValue=CalendarInfoHelper(Calendar);
   }
   else
   {
       ASSERT("Some parameters are invalid\n");
       SetLastError(ERROR_INVALID_PARAMETER);
       nRetValue =0;
   }

   LOGEXIT ("GetCalendarInfoW returns int %d\n",nRetValue);
   PERF_EXIT(GetCalendarInfoW);
   return nRetValue;
}
#else // ENABLE_DOWNLEVEL_FOR_NLS

/*++
Function:
  GetCalendarInfoEx

See MSDN doc.
--*/

int
PALAPI
GetCalendarInfoEx(
         IN LPCWSTR lpLocaleName,
         IN CALID Calendar,
         IN LPCWSTR pReserved,
         IN CALTYPE CalType,
         OUT LPWSTR lpCalData,
         IN int cchData,
         OUT LPDWORD lpValue)
{
    int nRetval = sizeof(DWORD);
    PERF_ENTRY(GetCalendarInfoEx);
    ENTRY("GetCalendarInfoEx(Locale=%S, Calendar=%d, pReserved =%p, CalType=%d, "
         "lpCalData=%p, cchData=%d, lpValue=%p)\n",
          lpLocaleName? lpLocaleName : W16_NULLSTRING, Calendar, pReserved, CalType, lpCalData, cchData, lpValue);

    CFLocaleRef cfLocale = NULL;

    if(lpValue != NULL) 
    {
        if ((lpCalData == NULL) && (cchData == 0) &&
            (CalType == (CAL_ITWODIGITYEARMAX|CAL_RETURN_NUMBER)))
        {
            *lpValue=CalendarInfoHelper(Calendar);
        }
    }
    else 
    {
        CFIndex length;
        CFStringRef cfString;

        cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
        if (cfLocale == NULL)
        {
            if (GetLastError() == ERROR_INVALID_PARAMETER)
            {
                ASSERT("Locale(%S) parameter is invalid\n",lpLocaleName);
            }
            return 0; 
        }
        // We don't differentiate user overrides for these types            
        switch (CalType & ~LOCALE_NOUSEROVERRIDE)
        {
            case CAL_SCALNAME:   

                cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendarIdentifier);
                if (cfString != NULL) 
                {
                    cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleCalendarIdentifier, cfString);
                }
                break;

            case CAL_SMONTHDAY:
                cfString = CFSTR("MMMM dd");
                CFRetain(cfString);
                break;

            case CAL_SSHORTDATE:
                cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterShortStyle, kCFDateFormatterNoStyle); 
                break;

            case CAL_SLONGDATE:
                cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterFullStyle, kCFDateFormatterNoStyle); 
                break;

            case CAL_SYEARMONTH:
                cfString = CFStringCreateYearMonth(cfLocale);
                break;
 
            case CAL_SSHORTESTDAYNAME1:
            case CAL_SSHORTESTDAYNAME2:
            case CAL_SSHORTESTDAYNAME3:
            case CAL_SSHORTESTDAYNAME4:
            case CAL_SSHORTESTDAYNAME5:
            case CAL_SSHORTESTDAYNAME6:
            case CAL_SSHORTESTDAYNAME7:
                cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("EE"), 1, (CalType & ~LOCALE_NOUSEROVERRIDE) - CAL_SSHORTESTDAYNAME1 + 1);
                break;

            case CAL_SDAYNAME1:
            case CAL_SDAYNAME2:
            case CAL_SDAYNAME3:
            case CAL_SDAYNAME4:
            case CAL_SDAYNAME5:
            case CAL_SDAYNAME6:
            case CAL_SDAYNAME7:
                cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("EEEE"), 1, (CalType & ~LOCALE_NOUSEROVERRIDE) - CAL_SDAYNAME1 + 1);
                break;

            case CAL_SABBREVDAYNAME1:
            case CAL_SABBREVDAYNAME2:
            case CAL_SABBREVDAYNAME3:
            case CAL_SABBREVDAYNAME4:
            case CAL_SABBREVDAYNAME5:
            case CAL_SABBREVDAYNAME6:
            case CAL_SABBREVDAYNAME7:
                cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("EEE"), 1, (CalType & ~LOCALE_NOUSEROVERRIDE) - CAL_SABBREVDAYNAME1 + 1);
                break;

            case CAL_SMONTHNAME1:
            case CAL_SMONTHNAME2:
            case CAL_SMONTHNAME3:
            case CAL_SMONTHNAME4:
            case CAL_SMONTHNAME5:
            case CAL_SMONTHNAME6:
            case CAL_SMONTHNAME7:
            case CAL_SMONTHNAME8:
            case CAL_SMONTHNAME9:
            case CAL_SMONTHNAME10:
            case CAL_SMONTHNAME11:
            case CAL_SMONTHNAME12:
                cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("MMMM"), (CalType & ~LOCALE_NOUSEROVERRIDE) - CAL_SMONTHNAME1 + 1, 1);
                break;

            case CAL_SMONTHNAME13:
                cfString = CFSTR("");
                CFRetain(cfString);
                break;

            case CAL_SABBREVMONTHNAME1:
            case CAL_SABBREVMONTHNAME2:
            case CAL_SABBREVMONTHNAME3:
            case CAL_SABBREVMONTHNAME4:
            case CAL_SABBREVMONTHNAME5:
            case CAL_SABBREVMONTHNAME6:
            case CAL_SABBREVMONTHNAME7:
            case CAL_SABBREVMONTHNAME8:
            case CAL_SABBREVMONTHNAME9:
            case CAL_SABBREVMONTHNAME10:
            case CAL_SABBREVMONTHNAME11:
            case CAL_SABBREVMONTHNAME12:
                cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("MMM"), (CalType & ~LOCALE_NOUSEROVERRIDE) - CAL_SABBREVMONTHNAME1 + 1, 1);
                break;

            case CAL_SABBREVMONTHNAME13:
                cfString = CFSTR("");
                CFRetain(cfString);
                break;

            case CAL_SMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME2 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME3 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME4 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME5 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME6 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME7 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME8 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME9 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME10 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME11 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME12 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SMONTHNAME13 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME2 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME3 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME4 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME5 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME6 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME7 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME8 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME9 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME10 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME11 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME12 | CAL_RETURN_GENITIVE_NAMES:
            case CAL_SABBREVMONTHNAME13 | CAL_RETURN_GENITIVE_NAMES:
                nRetval = 0;
                goto EXIT;
            default:
                ASSERT("Some parameters are invalid\n");
                SetLastError(ERROR_INVALID_PARAMETER);
                nRetval = 0; 
                goto EXIT;
        }
        
        // convert to return string
        length = CFStringGetLength(cfString);
        if (cchData == 0)
        {
            nRetval = length + 1;
        }
        else if (cchData >= length + 1)
        {
            CFStringGetCharacters(cfString, CFRangeMake(0, length), (UniChar*)lpCalData);
            lpCalData[length] = L'\0';
            nRetval = length + 1;
        }
        else
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            nRetval = 0;
        }
    
        if (cfString != NULL)
        {
            CFRelease(cfString);
        }
    }
 
EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    LOGEXIT ("GetCalendarInfoEx returns int %d\n",nRetval);
    PERF_EXIT(GetCalendarInfoEx);
    return nRetval;
}

#endif // ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
int
GetDateFormatHelper(
           IN CFLocaleRef cfLocale,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate)
{
    int nRetval = 0;
    CFDateFormatterRef cfDateFormatter = NULL;
    CFTimeZoneRef cfTimeZone = NULL;
    CFStringRef cfStringFormat = NULL;
    CFGregorianDate cfGregorianDate;
    CFStringRef cfStringDate = NULL;
    CFIndex length;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, kCFDateFormatterNoStyle, kCFDateFormatterNoStyle);
    if (cfDateFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfTimeZone = CFTimeZoneCreateWithTimeIntervalFromGMT(kCFAllocatorDefault, 0);
    CFDateFormatterSetProperty(cfDateFormatter, kCFDateFormatterTimeZone, cfTimeZone);

    /* TODO: In principle, we'd need to translate the format to the format
     * expected by Core Foundation.  However, currently no one calls this
     * with a format where this would actually matter.
     */
    cfStringFormat = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpFormat), PAL_wcslen(lpFormat));
    if (cfStringFormat == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    CFDateFormatterSetFormat(cfDateFormatter, cfStringFormat);

    if (lpDate)
    {
        cfGregorianDate.year = lpDate->wYear;
        cfGregorianDate.month = lpDate->wMonth;
        cfGregorianDate.day = lpDate->wDay;
        cfGregorianDate.hour = lpDate->wHour;
        cfGregorianDate.minute = lpDate->wMinute;
        cfGregorianDate.second = lpDate->wSecond + lpDate->wMilliseconds / 1000.0;
    }
    else
    {
        cfGregorianDate = CFAbsoluteTimeGetGregorianDate(CFAbsoluteTimeGetCurrent(),NULL);
    }

    cfStringDate = CFDateFormatterCreateStringWithAbsoluteTime(kCFAllocatorDefault, cfDateFormatter, CFGregorianDateGetAbsoluteTime(cfGregorianDate, NULL));

    length = CFStringGetLength(cfStringDate);
    if (cchDate == 0)
    {
        nRetval = length + 1;
    }
    else if (cchDate >= length + 1)
    {
        CFStringGetCharacters(cfStringDate, CFRangeMake(0, length), (UniChar*)lpDateStr);
        lpDateStr[length] = L'\0';
        nRetval = length + 1;
    }
    else
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        nRetval = 0;
    }

EXIT:
    if (cfStringDate != NULL)
    {
        CFRelease(cfStringDate);
    }
    if (cfStringFormat != NULL)
    {
        CFRelease(cfStringFormat);
    }
    if (cfTimeZone != NULL)
    {
        CFRelease(cfTimeZone);
    }
    if (cfDateFormatter != NULL)
    {
        CFRelease(cfDateFormatter);
    }

    return nRetval;
}




#endif


#if ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  GetDateFormatW

See MSDN doc.
--*/
int
PALAPI
GetDateFormatW(
           IN LCID Locale,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate)
{
    int nRetval = 0;
    LCID localeID = (LCID)0;
    WCHAR GG_string[] = {'g','g','\0'};

    PERF_ENTRY(GetDateFormatW);
    ENTRY("GetDateFormatW(Locale=%#04x, dwFlags=%#x, lpDate=%p, lpFormat=%p (%S), "
          "lpDateStr=%p, cchDate=%d)\n",
          Locale, dwFlags, lpDate, lpFormat ? lpFormat : W16_NULLSTRING, lpFormat ? lpFormat : W16_NULLSTRING,
          lpDateStr, cchDate);

    localeID = MAKELCID(GetSystemDefaultLangID(), SORT_DEFAULT);


    /* The implementation of this function is not reqd as of now
     *as this is called only for CAL_TAIWAN Calendar and we are supporting
     *only CAL_GREGORIAN*/
    if(((Locale == localeID) || (Locale == MAKELCID(MAKELANGID(LANG_CHINESE, SUBLANG_CHINESE_TRADITIONAL),SORT_DEFAULT)))
	   && (dwFlags & DATE_USE_ALT_CALENDAR) && (lpDate == NULL)
       && lpFormat ? (PAL_wcsncmp(lpFormat, GG_string, PAL_wcslen(lpFormat)) == 0) : FALSE)
    {
        /*Need to make a call to strftime() with appropriate params when functionality
          to use alternate calendars is implemented */
        ERROR("Not Implemented\n");
    }
    else
    {
        ASSERT("One of the input parameters is invalid\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }

    if (nRetval != 0 && cchDate != 0)
    {
        LOGEXIT("GetDateFormatW returns int %d (%S)\n", nRetval, lpDateStr);
    }
    else
    {
        LOGEXIT("GetDateFormatW returns int %d\n", nRetval);
    }
    PERF_EXIT(GetDateFormatW);
    return nRetval;
}


#else // ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  GetDateFormatEx

See MSDN doc.
--*/
int
PALAPI
GetDateFormatEx(
           IN LPCWSTR lpLocaleName,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate,
           IN LPCWSTR lpCalendar)
{
    int nRetval = 0;
    CFLocaleRef cfLocale = NULL;

    PERF_ENTRY(GetDateFormatEx);
    ENTRY("GetDateFormatEx(Locale=%S, dwFlags=%#x, lpDate=%p, lpFormat=%p (%S), "
          "lpDateStr=%p, cchDate=%d)\n",
          lpLocaleName? lpLocaleName : W16_NULLSTRING, dwFlags, lpDate, lpFormat ? lpFormat : W16_NULLSTRING, lpFormat ? lpFormat : W16_NULLSTRING,
          lpDateStr, cchDate);

    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n",lpLocaleName);
        }
        goto EXIT;
    }

    nRetval = GetDateFormatHelper(cfLocale,dwFlags,lpDate,lpFormat,lpDateStr,cchDate);

EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }

    if (nRetval != 0 && cchDate != 0)
    {
        LOGEXIT("GetDateFormatEx returns int %d (%S)\n", nRetval, lpDateStr);
    }
    else
    {
        LOGEXIT("GetDateFormatEx returns int %d\n", nRetval);
    }
    PERF_EXIT(GetDateFormatEx);
    return nRetval;
}

#endif // ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
CFMutableStringRef CFStringCreateMutableStripSymbols(CFStringRef cfString)
{
    CFIndex length;
    CFMutableStringRef cfMutableString;
    UniChar* buf;
    int j = 0;

    length = CFStringGetLength(cfString);
	// PERF: CFStringCreateMutable doesn't preallocate a buffer to hold all the data when you pass
	// in a non zero length for the string.  This leads lots of buffer resizing when the string we
	// are stripping is large.  Instead we preallocate our buffer upfront and then copy it into a
	// CFMutableString at the end.
    buf = (UniChar*) PAL_malloc(length * sizeof(UniChar));

    if(buf == NULL)
    {
       return NULL;
    }

    cfMutableString = CFStringCreateMutable(kCFAllocatorDefault, length);
    if (cfMutableString != NULL)
    {
        CFStringInlineBuffer cfStringInlineBuffer;
        CFIndex i;

        CFStringInitInlineBuffer(cfString, &cfStringInlineBuffer, CFRangeMake(0, length));
        for (i = 0; i < length; i++)
        {
            UniChar uniChar = CFStringGetCharacterFromInlineBuffer(&cfStringInlineBuffer, i);
            if (!PAL_iswpunct(uniChar))
            {
                buf[j] = uniChar;
                j++;
            }
        }
    }

    CFStringAppendCharacters(cfMutableString, buf, j);
    PAL_free(buf);
    return cfMutableString;
}

CFMutableStringRef CFStringCreateMutableStripPunctuation(CFStringRef cfString)
{
    CFIndex length;
    CFMutableStringRef cfMutableString;
    static CFCharacterSetRef sPunctuationSet = NULL;
    UniChar* buf;
    int j = 0;

    length = CFStringGetLength(cfString);
	// PERF: CFStringCreateMutable doesn't preallocate a buffer to hold all the data when you pass
	// in a non zero length for the string.  This leads lots of buffer resizing when the string we
	// are stripping is large.  Instead we preallocate our buffer upfront and then copy it into a
	// CFMutableString at the end.
    buf = (UniChar*) PAL_malloc(length * sizeof(UniChar));

    if(buf == NULL)
    {
       return NULL;
    }

    if (sPunctuationSet == NULL)
    {
        sPunctuationSet = CFCharacterSetGetPredefined(kCFCharacterSetPunctuation);
    }

    length = CFStringGetLength(cfString);
    cfMutableString = CFStringCreateMutable(kCFAllocatorDefault, length);
    if (cfMutableString != NULL)
    {
        CFStringInlineBuffer cfStringInlineBuffer;
        CFIndex i;

        CFStringInitInlineBuffer(cfString, &cfStringInlineBuffer, CFRangeMake(0, length));
        for (i = 0; i < length; i++)
        {
            UniChar uniChar = CFStringGetCharacterFromInlineBuffer(&cfStringInlineBuffer, i);
            if (!CFCharacterSetIsCharacterMember(sPunctuationSet, uniChar))
            {
                buf[j] = uniChar;
                j++;
            }
        }
    }

    CFStringAppendCharacters(cfMutableString, buf, j);
    PAL_free(buf);
    return cfMutableString;
}
#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
int
CompareStringHelper(
    IN CFLocaleRef cfLocale,
    IN DWORD    dwCmpFlags,
    IN LPCWSTR  lpString1,
    IN int      cchCount1,
    IN LPCWSTR  lpString2,
    IN int      cchCount2,
    IN BOOL     ordinal)
{
    INT nRetVal = 0;  /* return value: 0 (failure), CSTR_* (success) */
    INT nInternalRetVal; /* used internally: < 0, == 0, > 0 */
    CFStringCompareFlags cfStringCompareFlags = (CFStringCompareFlags)0;
    CFStringRef cfString1 = NULL, cfString2 = NULL;

    // NORM_LINGUISTIC_CASING flag accepted but no effect at the moment; e.g. 
    // NORM_IGNORECASE | NORM_LINGUISTIC_CASING should behaves same as 
    // NORM_IGNORECASE. Parity with PC.
    if ((dwCmpFlags & ~(NORM_IGNORECASE|
                        NORM_IGNORENONSPACE|
                        NORM_IGNORESYMBOLS|
                        NORM_IGNOREKANATYPE|
                        NORM_IGNOREWIDTH|
                        SORT_STRINGSORT|
                        NORM_LINGUISTIC_CASING)) != 0)

    {
        ASSERT("dwCmpFlags(%#x) parameter is invalid\n",dwCmpFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT ("CompareStringHelper returns int 0\n");
        return 0;
    }

    if ( !lpString1 || !lpString2 )
    {
        ERROR("One of the two params %p and %p is Invalid\n",lpString1,lpString2);
        SetLastError( ERROR_INVALID_PARAMETER );
        LOGEXIT ("CompareStringHelper returns 0\n" );
        return 0;
    }

    if( cchCount1 == -1)
    {
        cchCount1 = PAL_wcslen( lpString1 );
    }
    if( cchCount2 == -1 )
    {
        cchCount2 = PAL_wcslen( lpString2 );
    }

    cfString1 = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpString1), cchCount1);
    if (cfString1 == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfString2 = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpString2), cchCount2);
    if (cfString2 == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if (!ordinal && cfLocale != CFLocaleGetSystem())
    {
        cfStringCompareFlags = (CFStringCompareFlags)(cfStringCompareFlags | kCFCompareNonliteral | kCFCompareLocalized);
    }
    if ((dwCmpFlags & NORM_IGNORECASE) != 0)
    {
        cfStringCompareFlags = (CFStringCompareFlags)(cfStringCompareFlags | kCFCompareCaseInsensitive);
    }

    /* see if we can avoid constructing a mutable string */
    if ((dwCmpFlags & (NORM_IGNORENONSPACE|
                       NORM_IGNORESYMBOLS|
                       NORM_IGNOREKANATYPE|
                       NORM_IGNOREWIDTH|
                       SORT_STRINGSORT)) == SORT_STRINGSORT)
    {

        // TODO: (pre 10.5) Neither the Core Foundation framework nor the Foundation framework nor the
        // Unicode Utilities implement a localized string comparison that allows the developer
        // to specify the locale to use.  CFStringCompare and friends to not even take a locale
        // parameter, and both UCCreateCollator and NSString's -compare:options:range:locale:
        // method ignore their locale argument.  All of these operations implicitly use the
        // collation order specified in the user's preferences.
        //
        // So we silently do the wrong thing here.
        if(s_CFStringCompareWithOptionsAndLocale != NULL) {
            CFRange r = CFRangeMake(0, CFStringGetLength(cfString1));
	        nInternalRetVal = s_CFStringCompareWithOptionsAndLocale(cfString1, cfString2, r, cfStringCompareFlags, cfLocale);
        } else {
            nInternalRetVal = CFStringCompare(cfString1, cfString2, cfStringCompareFlags);
        }
    }
    else
    {
        CFMutableStringRef cfMutableString1 = NULL, cfMutableString2 = NULL;
        if ((dwCmpFlags & NORM_IGNORESYMBOLS) != 0)
        {
            cfMutableString1 = CFStringCreateMutableStripSymbols(cfString1);
            if (cfMutableString1 == NULL)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfMutableString2 = CFStringCreateMutableStripSymbols(cfString2);
            if (cfMutableString2 == NULL)
            {
                CFRelease(cfMutableString1);
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
        }
        else if ((dwCmpFlags & SORT_STRINGSORT) == 0)
        {
            /* first try ignoring punctuation.  If the strings turn out equal, */
            /* we repeat the comparison with the SORT_STRINGSORT flag (see below) */
            cfMutableString1 = CFStringCreateMutableStripPunctuation(cfString1);
            if (cfMutableString1 == NULL)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfMutableString2 = CFStringCreateMutableStripPunctuation(cfString2);
            if (cfMutableString2 == NULL)
            {
                CFRelease(cfMutableString1);
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
        }
        else
        {
            cfMutableString1 = CFStringCreateMutableCopy(kCFAllocatorDefault,
                                                         cchCount1,
                                                         cfString1);
            if (cfMutableString1 == NULL)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfMutableString2 = CFStringCreateMutableCopy(kCFAllocatorDefault,
                                                         cchCount2,
                                                         cfString2);
            if (cfMutableString2 == NULL)
            {
                CFRelease(cfMutableString1);
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
        }

        if ((dwCmpFlags & NORM_IGNORENONSPACE) != 0)
        {
            if (!CFStringTransform(cfMutableString1, NULL, kCFStringTransformStripCombiningMarks, false))
            {
                ASSERT("StripCombiningMarks transform failed for lpString1\n");
            }
            if (!CFStringTransform(cfMutableString2, NULL, kCFStringTransformStripCombiningMarks, false))
            {
                ASSERT("StripCombiningMarks transform failed for lpString2\n");
            }
        }
        if ((dwCmpFlags & NORM_IGNOREKANATYPE) != 0)
        {
            if (!CFStringTransform(cfMutableString1, NULL, kCFStringTransformHiraganaKatakana, false))
            {
                ASSERT("HiraganaKatakana transform failed for lpString1\n");
            }
            if (!CFStringTransform(cfMutableString2, NULL, kCFStringTransformHiraganaKatakana, false))
            {
                ASSERT("HiraganaKatakana transform failed for lpString2\n");
            }
        }
        if ((dwCmpFlags & NORM_IGNOREWIDTH) != 0)
        {
            if (!CFStringTransform(cfMutableString1, NULL, kCFStringTransformFullwidthHalfwidth, false))
            {
                ASSERT("FullwidthHalfwidth transform failed for lpString1\n");
            }
            if (!CFStringTransform(cfMutableString2, NULL, kCFStringTransformFullwidthHalfwidth, false))
            {
                ASSERT("FullwidthHalfwidth transform failed for lpString2\n");
            }
        }

        // TODO: (pre 10.5) Neither the Core Foundation framework nor the Foundation framework nor the
        // Unicode Utilities implement a localized string comparison that allows the developer
        // to specify the locale to use.  CFStringCompare and friends to not even take a locale
        // parameter, and both UCCreateCollator and NSString's -compare:options:range:locale:
        // method ignore their locale argument.  All of these operations implicitly use the
        // collation order specified in the user's preferences.
        //
        // So we silently do the wrong thing here.
        if(s_CFStringCompareWithOptionsAndLocale != NULL) {
            CFRange r = CFRangeMake(0, CFStringGetLength(cfMutableString1));
            nInternalRetVal = s_CFStringCompareWithOptionsAndLocale(cfMutableString1, cfMutableString2, r, cfStringCompareFlags, cfLocale);
        } else {
            nInternalRetVal = CFStringCompare(cfMutableString1, cfMutableString2, cfStringCompareFlags);
        }

        if (nInternalRetVal == 0 && (dwCmpFlags & (NORM_IGNORESYMBOLS|SORT_STRINGSORT)) == 0)
        {
            /* we did a word sort and the strings were equal when we ignored punctuation. */
            /* Repeat the comparison as a string sort. */
            switch (CompareStringHelper(cfLocale, dwCmpFlags|SORT_STRINGSORT, lpString1, cchCount1, lpString2, cchCount2, FALSE))
            {
            case CSTR_EQUAL:
                nInternalRetVal = 0;
                break;
            case CSTR_GREATER_THAN:
                nInternalRetVal = 1;
                break;
            case CSTR_LESS_THAN:
                nInternalRetVal = -1;
                break;
            default:
                ERROR("internal error\n");
                break;
            }
        }

        CFRelease(cfMutableString2);
        CFRelease(cfMutableString1);
    }

    if (nInternalRetVal == 0)
    {
        nRetVal = CSTR_EQUAL;
    }
    else if (nInternalRetVal > 0)
    {
        nRetVal = CSTR_GREATER_THAN;
    }
    else /* (nInternalRetVal < 0) */
    {
        nRetVal = CSTR_LESS_THAN;
    }

EXIT:
    if (cfString2 != NULL)
    {
        CFRelease(cfString2);
    }
    if (cfString1 != NULL)
    {
        CFRelease(cfString1);
    }

    return nRetVal;
}

/*++
Function:
  CompareStringEx

See MSDN doc.
--*/
int
PALAPI
CompareStringEx(
    IN LPCWSTR     lpLocaleName,
    IN DWORD    dwCmpFlags,
    IN LPCWSTR  lpString1,
    IN int      cchCount1,
    IN LPCWSTR  lpString2,
    IN int      cchCount2,
    IN LPNLSVERSIONINFO lpVersionInformation,
    IN LPVOID      lpReserved,
    IN LPARAM lParam)
{
    INT nRetVal = 0;  /* return value: 0 (failure), CSTR_* (success) */
    CFLocaleRef cfLocale = NULL;

    PERF_ENTRY(CompareStringEx);
    ENTRY("CompareStringEx(lpLocaleName=%p (%S), dwCmpFlags=%#x, lpString1=%p (%S), "
          "cchCount1=%d, lpString2=%p (%S), cchCount2=%d)\n",
          lpLocaleName, lpLocaleName? lpLocaleName : W16_NULLSTRING, dwCmpFlags, lpString1, lpString1, cchCount1,lpString2,lpString2, cchCount2);
		  
    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("lpLocaleName(%p (%S)) parameter is invalid\n",lpLocaleName,lpLocaleName? lpLocaleName : W16_NULLSTRING);
        }
        LOGEXIT ("CompareStringEx returns int 0\n");
        PERF_EXIT(CompareStringEx);
        return 0;
    }

    nRetVal=CompareStringHelper(cfLocale,dwCmpFlags,lpString1,cchCount1,lpString2,cchCount2,FALSE);

    CFRelease(cfLocale);

    LOGEXIT("CompareStringEx returns int %d\n", nRetVal);
    PERF_EXIT(CompareStringEx);
    return nRetVal;
}

#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS


#if ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
  CompareStringW

See MSDN doc.
--*/
int
PALAPI
CompareStringW(
    IN LCID  Locale,
    IN DWORD    dwCmpFlags,
    IN LPCWSTR  lpString1,
    IN int      cchCount1,
    IN LPCWSTR  lpString2,
    IN int      cchCount2)
{
    INT nRetVal = 0;  /* return value: 0 (failure), CSTR_* (success) */
    INT nInternalRetVal; /* used internally: < 0, == 0, > 0 */
    INT nStrLen = 0;

    PERF_ENTRY(CompareStringW);
    ENTRY("CompareStringW(Locale=%#04x, dwCmpFlags=%#x, lpString1=%p (%S), "
          "cchCount1=%d, lpString2=%p (%S), cchCount2=%d)\n",
          Locale, dwCmpFlags, lpString1, lpString1, cchCount1,lpString2,lpString2, cchCount2);

    if ( Locale != LOCALE_US_ENGLISH )
    {
        ASSERT("Locale(%#04x) parameter is invalid\n",Locale);
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT ("CompareStringW returns int 0\n");
        PERF_EXIT(CompareStringW);
        return 0;
    }

    if ((dwCmpFlags & ~(NORM_IGNORECASE|NORM_IGNOREWIDTH)) != 0)
    {
        ASSERT("dwCmpFlags(%#x) parameter is invalid\n",dwCmpFlags);
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT ("CompareStringW returns int 0\n");
        PERF_EXIT(CompareStringW);
        return 0;
    }

    if ( !lpString1 || !lpString2 )
    {
        ERROR("One of the two params %p and %p is Invalid\n",lpString1,lpString2);
        SetLastError( ERROR_INVALID_PARAMETER );
        LOGEXIT ("CompareStringW returns 0\n" );
        PERF_EXIT(CompareStringW);
        return 0;
    }

    if( cchCount1 == -1)
    {
        cchCount1 = PAL_wcslen( lpString1 );
    }
    if( cchCount2 == -1 )
    {
        cchCount2 = PAL_wcslen( lpString2 );
    }

 
    /*take the length of the smaller of the 2 strings*/
    nStrLen = ( ( cchCount1 > cchCount2 ) ? cchCount2 : cchCount1 );

    if ((dwCmpFlags & NORM_IGNORECASE) != 0)
    {
        nInternalRetVal = _wcsnicmp( lpString1, lpString2, nStrLen );
    }
    else
    {
        nInternalRetVal = PAL_wcsncmp( lpString1, lpString2, nStrLen );
    }

    if (nInternalRetVal == 0)
    {
        if (cchCount1 > cchCount2)
        {
            nInternalRetVal = 1;
        }
        else if (cchCount1 < cchCount2)
        {
            nInternalRetVal = -1;
        }
    }


    if (nInternalRetVal == 0)
    {
        nRetVal = CSTR_EQUAL;
    }
    else if (nInternalRetVal > 0)
    {
        nRetVal = CSTR_GREATER_THAN;
    }
    else /* (nInternalRetVal < 0) */
    {
        nRetVal = CSTR_LESS_THAN;
    }


    LOGEXIT("CompareStringW returns int %d\n", nRetVal);
    PERF_EXIT(CompareStringW);
    return nRetVal;
}



/*++
Function:
  CompareStringA

See MSDN doc.
--*/
int
PALAPI
CompareStringA(
    IN LCID     Locale,
    IN DWORD    dwCmpFlags,
    IN LPCSTR   lpString1,
    IN int      cchCount1,
    IN LPCSTR   lpString2,
    IN int      cchCount2)
{
    LPWSTR lpwString1, lpwString2;
    int nRetVal = 0;

    PERF_ENTRY(CompareStringA);
    ENTRY("CompareStringA(Locale=%#04x, dwCmpFlags=%#x, lpString1=%p (%s), "
          "cchCount1=%d, lpString2=%p (%s), cchCount2=%d)\n",
          Locale, dwCmpFlags, lpString1, lpString1, cchCount1,lpString2,lpString2, cchCount2);

    lpwString1 = UTIL_MBToWC_Alloc(lpString1, cchCount1);
    if (lpwString1 == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    lpwString2 = UTIL_MBToWC_Alloc(lpString2, cchCount2);
    if (lpwString2 == NULL)
    {
        PAL_free(lpwString1);
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    nRetVal = CompareStringW(Locale, dwCmpFlags, lpwString1, cchCount1, lpwString2, cchCount2);

    PAL_free(lpwString1);
    PAL_free(lpwString2);

EXIT:
    LOGEXIT("CompareStringA returns int %d\n", nRetVal);
    PERF_EXIT(CompareStringA);

    return nRetVal;
}
#endif // ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
static CFStringRef CFLocaleCopyNumberFormatterProperty(CFLocaleRef cfLocale, CFNumberFormatterStyle cfNumberFormatterStyle, CFStringRef key)
{
    CFNumberFormatterRef cfNumberFormatter;
    CFStringRef cfString;

    cfNumberFormatter = CFNumberFormatterCreate(kCFAllocatorDefault, cfLocale, cfNumberFormatterStyle);
    if (cfNumberFormatter == NULL)
    {
        ASSERT("CFNumberFormatterCreate failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }
    cfString = (CFStringRef)CFNumberFormatterCopyProperty(cfNumberFormatter, key);
    if (cfString == NULL)
    {
        ASSERT("CFNumberFormatterCopyProperty failed\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    CFRelease(cfNumberFormatter);
    return cfString;
}

static CFStringRef CFLocaleCopyDateFormatterProperty(CFLocaleRef cfLocale, CFStringRef key)
{
    CFDateFormatterRef cfDateFormatter;
    CFStringRef cfString;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, kCFDateFormatterNoStyle, kCFDateFormatterNoStyle);
    if (cfDateFormatter == NULL)
    {
        ASSERT("CFDateFormatterCreate failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }
    cfString = (CFStringRef)CFDateFormatterCopyProperty(cfDateFormatter, key);
    if (cfString == NULL)
    {
        ASSERT("CFDateFormatterCopyProperty failed\n");
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    CFRelease(cfDateFormatter);
    return cfString;
}

static INT CFLocaleGetICurrency(CFLocaleRef cfLocale)
{
    INT nRetval = -1;
    CFNumberFormatterRef cfNumberFormatter;
    CFStringRef cfStringNumberFormat = NULL;
    CFRange cfRange, cfRangeCurrencySign;

    cfNumberFormatter = CFNumberFormatterCreate(kCFAllocatorDefault, cfLocale, kCFNumberFormatterCurrencyStyle);
    if (cfNumberFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfStringNumberFormat = CFNumberFormatterGetFormat(cfNumberFormatter);
    if (cfStringNumberFormat != NULL)
    {
        CFRetain(cfStringNumberFormat);
    }

    /* set cfRange to cover format string for positive numbers */
    cfRange = CFStringFind(cfStringNumberFormat, CFSTR(";"), 0);
    if (cfRange.location != -1)
    {
        /* we have two separate formats ("positive;negative") - only consider positive */
        cfRange.length = cfRange.location;
        cfRange.location = 0;
    }
    else
    {
        cfRange.location = 0;
        cfRange.length = CFStringGetLength(cfStringNumberFormat);
    }

    /* find the currency sign */
    {
        UniChar uniChar = 0x00A4; /* {CURRENCY SIGN} */
        /* cannot use CFSTR(...) here because that only supports ASCII */
        CFStringRef cfStringCurrencySign = CFStringCreateWithCharacters(kCFAllocatorDefault, &uniChar, 1);
        if (cfStringCurrencySign == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
        if (!CFStringFindWithOptions(cfStringNumberFormat, cfStringCurrencySign, cfRange, 0, &cfRangeCurrencySign))
        {
            cfRangeCurrencySign.location = -1;
        }
        CFRelease(cfStringCurrencySign);
    }

    /* identify which format it is */
    if (cfRangeCurrencySign.location == -1)
    {
        ASSERT("Invalid positive currency format string - no currency sign\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    else if (cfRangeCurrencySign.location == cfRange.location)
    {
        if (cfRange.length == 1) /* ensure index is valid in subsequent access */
        {
            ASSERT("Invalid positive currency format string - consists of currency sign only\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            goto EXIT;
        }
        else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRange.location + 1) != L' ')
        {
            nRetval = 0; /* Prefix, no separation, for example $1.1 */
        }
        else
        {
            nRetval = 2; /* Prefix, 1-character separation, for example $ 1.1 */
        }
    }
    else /* cfRangeCurrencySign.location > cfRange.location */
    {
        if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location - 1) != L' ')
        {
            nRetval = 1; /* Suffix, no separation, for example 1.1$ */
        }
        else
        {
            nRetval = 3; /* Suffix, 1-character separation, for example 1.1 $ */
        }
    }
EXIT:
    if (cfNumberFormatter != NULL)
    {
        CFRelease(cfNumberFormatter);
    }
    if (cfStringNumberFormat != NULL)
    {
        CFRelease(cfStringNumberFormat);
    }
    return nRetval;
}

static INT CFLocaleGetNegCurr(CFLocaleRef cfLocale)
{
    INT nRetval = -1;
    CFNumberFormatterRef cfNumberFormatter;
    CFStringRef cfStringNumberFormat = NULL;
    CFRange cfRange, cfRangeCurrencySign;

    cfNumberFormatter = CFNumberFormatterCreate(kCFAllocatorDefault, cfLocale, kCFNumberFormatterCurrencyStyle);
    if (cfNumberFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfStringNumberFormat = CFNumberFormatterGetFormat(cfNumberFormatter);
    if (cfStringNumberFormat != NULL)
    {
        CFRetain(cfStringNumberFormat);
    }

    /* set cfRange to cover format string for negative numbers */
    cfRange = CFStringFind(cfStringNumberFormat, CFSTR(";"), 0);
    if (cfRange.location != -1)
    {
        /* we have two separate formats ("positive;negative") - only consider negative */
        cfRange.location += cfRange.length;
        cfRange.length = CFStringGetLength(cfStringNumberFormat) - cfRange.location;
    }
    else
    {
        cfRange.location = 0;
        cfRange.length = CFStringGetLength(cfStringNumberFormat);
    }

    /* find the currency sign */
    {
        UniChar uniChar = 0x00A4; /* {CURRENCY SIGN} */
        /* cannot use CFSTR(...) here because that only supports ASCII */
        CFStringRef cfStringCurrencySign = CFStringCreateWithCharacters(kCFAllocatorDefault, &uniChar, 1);
        if (cfStringCurrencySign == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
        if (!CFStringFindWithOptions(cfStringNumberFormat, cfStringCurrencySign, cfRange, 0, &cfRangeCurrencySign))
        {
            cfRangeCurrencySign.location = -1;
        }
        CFRelease(cfStringCurrencySign);
    }

    if (cfRangeCurrencySign.location == -1)
    {
        ASSERT("Invalid negative currency format string - no currency sign\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRange.location) == L'(')
    {
        /* identify which of the parenthesized formats it is */
        if (cfRangeCurrencySign.location == cfRange.location + 1)
        {
            if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRange.location + 2) != L' ')
            {
                nRetval = 0; /* Left parenthesis, monetary symbol, number, right parenthesis. Example: ($1.1) */
            }
            else
            {
                nRetval = 14; /* Left parenthesis, monetary symbol, space, number, right parenthesis (like #0, but with a space after the monetary symbol). Example: ($ 1.1) */
            }
        }
        else
        {
            if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location - 1) != L' ')
            {
                nRetval = 4; /* Left parenthesis, number, monetary symbol, right parenthesis. Example: (1.1$) */
            }
            else
            {
                nRetval = 15; /* Left parenthesis, number, space, monetary symbol, right parenthesis (like #4, but with a space before the monetary symbol). Example: (1.1 $) */
            }
        }
    }
    else
    {
        CFRange cfRangeMinusSign;

        /* find the minus sign */
        {
            if (!CFStringFindWithOptions(cfStringNumberFormat, CFSTR("-"), cfRange, 0, &cfRangeMinusSign))
            {
                cfRangeMinusSign.location = -1;
            }
            if (cfRangeMinusSign.location == -1)
            {
                /* no minus sign: implicit with first '#' */
                if (!CFStringFindWithOptions(cfStringNumberFormat, CFSTR("#"), cfRange, 0, &cfRangeMinusSign))
                {
                    cfRangeMinusSign.location = -1;
                }
            }
        }

        /* identify which format it is */
        if (cfRangeMinusSign.location == -1)
        {
            ASSERT("Invalid negative currency format string - neither parenthesized nor minus sign nor number\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            goto EXIT;
        }
        else if (cfRangeMinusSign.location > cfRangeCurrencySign.location)
        {
            if (cfRangeMinusSign.location == cfRangeCurrencySign.location + 1) /* string contains "$-" */
            {
                if (cfRangeCurrencySign.location == cfRange.location)
                {
                    nRetval = 2; /* Monetary symbol, negative sign, number. Example: $-1.1 */
                }
                else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location - 1) != L' ')
                {
                    nRetval = 7; /* Number, monetary symbol, negative sign. Example: 1.1$- */
                }
                else
                {
                    nRetval = 10; /* Number, space, monetary symbol, negative sign (like #7, but with a space before the monetary symbol). Example: 1.1 $- */
                }
            }
            else if (cfRangeMinusSign.location == cfRangeCurrencySign.location + 2) // string contains "$ -"
            {
                nRetval = 12; /* Monetary symbol, space, negative sign, number (like #2, but with a space after the monetary symbol). Example: $ -1.1 */
            }
            else /* string contains "$...-" */
            {
                if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location + 1) != L' ')
                {
                    nRetval = 3; /* Monetary symbol, number, negative sign. Example: $1.1- */
                }
                else
                {
                    nRetval = 11; /* Monetary symbol, space, number, negative sign (like #3, but with a space after the monetary symbol). Example: $ 1.1- */
                }
            }
        }
        else
        {
            if (cfRangeMinusSign.location == cfRangeCurrencySign.location - 1) /* string contains "-$" */
            {
                if (cfRangeCurrencySign.location == cfRange.location + cfRange.length - 1)
                {
                    nRetval = 6; /* Number, negative sign, monetary symbol. Example: 1.1-$ */
                }
                else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location + 1) != L' ')
                {
                    nRetval = 1; /* Negative sign, monetary symbol, number. Example: -$1.1 */
                }
                else
                {
                    nRetval = 9; /* Negative sign, monetary symbol, space, number (like #1, but with a space after the monetary symbol). Example: -$ 1.1 */
                }
            }
            else if (cfRangeMinusSign.location == cfRangeCurrencySign.location - 2) // string contains "- $"
            {
                nRetval = 13; /* Number, negative sign, space, monetary symbol (like #6, but with a space before the monetary symbol). Example: 1.1- $ */
            }
            else /* string contains "-...$" */
            {
                if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangeCurrencySign.location - 1) != L' ')
                {
                    nRetval = 5; /* Negative sign, number, monetary symbol. Example: -1.1$ */
                }
                else
                {
                    nRetval = 8; /* Negative sign, number, space, monetary symbol (like #5, but with a space before the monetary symbol). Example: -1.1 $ */
                }
            }
        }
    }
EXIT:
    if (cfNumberFormatter != NULL)
    {
        CFRelease(cfNumberFormatter);
    }
    if (cfStringNumberFormat != NULL) 
    {
        CFRelease(cfStringNumberFormat);
    }
    return nRetval;
}


#if ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE
static INT CFLocaleGetPositivePercent(CFLocaleRef cfLocale)
{
    INT nRetval = -1;
    CFNumberFormatterRef cfNumberFormatter;
    CFStringRef cfStringNumberFormat;
    CFRange cfRange, cfRangePercentSign;

    cfNumberFormatter = CFNumberFormatterCreate(kCFAllocatorDefault, cfLocale, kCFNumberFormatterPercentStyle);
    if (cfNumberFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfStringNumberFormat = CFNumberFormatterGetFormat(cfNumberFormatter);
    if (cfStringNumberFormat != NULL)
    {
        CFRetain(cfStringNumberFormat);
    }

    /* set cfRange to cover format string for positive numbers */
    cfRange = CFStringFind(cfStringNumberFormat, CFSTR(";"), 0);
    if (cfRange.location != -1)
    {
        /* we have two separate formats ("positive;negative") - only consider positive */
        cfRange.length = cfRange.location;
        cfRange.location = 0;
    }
    else
    {
        cfRange.location = 0;
        cfRange.length = CFStringGetLength(cfStringNumberFormat);
    }

    /* find the percent sign */
    {
        CFStringRef cfStringPercentSign = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterPercentStyle, 
            kCFNumberFormatterPercentSymbol);
        if (cfStringPercentSign == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
        if (!CFStringFindWithOptions(cfStringNumberFormat, cfStringPercentSign, cfRange, 0, &cfRangePercentSign))
        {
            cfRangePercentSign.location = -1;
        }
        CFRelease(cfStringPercentSign);
    }

    /* identify which format it is */
    if (cfRangePercentSign.location == -1)
    {
        ASSERT("Invalid positive percent format string - no percent sign\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    else if (cfRangePercentSign.location == cfRange.location)
    {
        if (cfRange.length == 1) /* ensure index is valid in subsequent access */
        {
            ASSERT("Invalid positive percent format string - consists of percent sign only\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            goto EXIT;
        }
        else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRange.location + 1) != L' ')
        {
            nRetval = 2; /* Prefix, no separation, for example %1.1 */
        }
        else
        {
            nRetval = 3; /* Prefix, 1-character separation, for example % .1 */
        }
    }
    else /* cfRangePercentSign.location > cfRange.location */
    {
        if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangePercentSign.location - 1) != L' ')
        {
            nRetval = 1; /* Suffix, no separation, for example 1.1% */
        }
        else
        {
            nRetval = 0; /* Suffix, 1-character separation, for example 1.1 % */
        }
    }
EXIT:
    if (cfNumberFormatter != NULL)
    {
        CFRelease(cfNumberFormatter);
    }
    if (cfStringNumberFormat != NULL)
    {
        CFRelease(cfStringNumberFormat);
    }
    return nRetval;
}

static INT CFLocaleGetNegativePercent(CFLocaleRef cfLocale)
{
    INT nRetval = -1;
    CFNumberFormatterRef cfNumberFormatter;
    CFStringRef cfStringNumberFormat;
    CFRange cfRange, cfRangePercentSign;

    cfNumberFormatter = CFNumberFormatterCreate(kCFAllocatorDefault, cfLocale, kCFNumberFormatterPercentStyle);
    if (cfNumberFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfStringNumberFormat = CFNumberFormatterGetFormat(cfNumberFormatter);
    if (cfStringNumberFormat != NULL)
    {
        CFRetain(cfStringNumberFormat);
    }

    /* set cfRange to cover format string for negative numbers */
    cfRange = CFStringFind(cfStringNumberFormat, CFSTR(";"), 0);
    if (cfRange.location != -1)
    {
        /* we have two separate formats ("positive;negative") - only consider negative */
        cfRange.location += cfRange.length;
        cfRange.length = CFStringGetLength(cfStringNumberFormat) - cfRange.location;
    }
    else
    {
        cfRange.location = 0;
        cfRange.length = CFStringGetLength(cfStringNumberFormat);
    }

    /* find the percent sign */
    {
        CFStringRef cfStringPercentSign = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterPercentStyle, 
            kCFNumberFormatterPercentSymbol);
        if (cfStringPercentSign == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
        if (!CFStringFindWithOptions(cfStringNumberFormat, cfStringPercentSign, cfRange, 0, &cfRangePercentSign))
        {
            cfRangePercentSign.location = -1;
        }
        CFRelease(cfStringPercentSign);
    }

    if (cfRangePercentSign.location == -1)
    {
        ASSERT("Invalid negative percent format string - no percent sign\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    else
    {
        CFRange cfRangeMinusSign;

        /* find the minus sign */
        {
            if (!CFStringFindWithOptions(cfStringNumberFormat, CFSTR("-"), cfRange, 0, &cfRangeMinusSign))
            {
                cfRangeMinusSign.location = -1;
            }
            if (cfRangeMinusSign.location == -1)
            {
                /* no minus sign: implicit with first '#' */
                if (!CFStringFindWithOptions(cfStringNumberFormat, CFSTR("#"), cfRange, 0, &cfRangeMinusSign))
                {
                    cfRangeMinusSign.location = -1;
                }
            }
        }

        /* identify which format it is */
        if (cfRangeMinusSign.location == -1)
        {
            ASSERT("Invalid negative percent format string - no minus sign nor number\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            goto EXIT;
        }
        else if (cfRangeMinusSign.location > cfRangePercentSign.location)
        {
            if (cfRangeMinusSign.location == cfRangePercentSign.location + 1) /* string contains "%-" */
            {
                if (cfRangePercentSign.location == cfRange.location)
                {
                    nRetval = 3; /* Percent symbol, negative sign, number. Example: %-1.1 */
                }
                else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangePercentSign.location - 1) != L' ')
                {
                    nRetval = 6; /* Number, percent symbol, negative sign. Example: 1.1%- */
                }
                else
                {
                    nRetval = 8; /* Number, space, percent symbol, negative sign . Example: 1.1 %- */
                }
            }
            else if (cfRangeMinusSign.location == cfRangePercentSign.location + 2) // string contains "% -"
            {
                nRetval = 10; /* Percent symbol, space, negative sign, number. Example: % -1.1 */
            }
            else /* string contains "%...-" */
            {
                if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangePercentSign.location + 1) != L' ')
                {
                    nRetval = 4; /* Percent symbol, number, negative sign. Example: %1.1- */
                }
                else
                {
                    nRetval = 9; /* PErcent symbol, space, number, negative sign. Example: % 1.1- */
                }
            }
        }
        else
        {
            if (cfRangeMinusSign.location == cfRangePercentSign.location - 1) /* string contains "-%" */
            {
                if (cfRangePercentSign.location == cfRange.location + cfRange.length - 1)
                {
                    nRetval = 5; /* Number, negative sign, percent symbol. Example: 1.1-% */
                }
                else if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangePercentSign.location + 1) != L' ')
                {
                    nRetval = 2; /* Negative sign, percent symbol, number. Example: -%1.1 */
                }
                else
                {
                    nRetval = 7; /* Negative sign, percent symbol, space, number. Example: -% 1.1 */
                }
            }
            else if (cfRangeMinusSign.location == cfRangePercentSign.location - 2) // string contains "- %"
            {
                nRetval = 11; /* Number, negative sign, space, percent symbol. Example: 1.1- % */
            }
            else /* string contains "-...%" */
            {
                if (CFStringGetCharacterAtIndex(cfStringNumberFormat, cfRangePercentSign.location - 1) != L' ')
                {
                    nRetval = 1; /* Negative sign, number, percent symbol. Example: -1.1% */
                }
                else
                {
                    nRetval = 0; /* Negative sign, number, space, percent symbol. Example: -1.1 % */
                }
            }
        }
    }
EXIT:
    if (cfNumberFormatter != NULL)
    {
        CFRelease(cfNumberFormatter);
    }
    if (cfStringNumberFormat != NULL)
    {
        CFRelease(cfStringNumberFormat);
    }
    return nRetval;
}

#endif // ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE

static CALID CFLocaleGetCALID(CFLocaleRef cfLocale)
{
    // initialize to default/fallback value
    CALID nRetval = CAL_GREGORIAN_US;

    CFStringRef cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendarIdentifier);
    if (cfString != NULL) 
    {
        CFRetain(cfString);
    }

    if (CFStringCompare(cfString, kCFGregorianCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_GREGORIAN;
    }
    else if (CFStringCompare(cfString, kCFBuddhistCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_THAI;
    }
    else if (CFStringCompare(cfString, kCFChineseCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_TAIWAN;
    }
    else if (CFStringCompare(cfString, kCFHebrewCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_HEBREW;
    }
    else if (CFStringCompare(cfString, kCFIslamicCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_HIJRI;
    }
    else if (CFStringCompare(cfString, kCFIslamicCivilCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_GREGORIAN_ARABIC;
    }
    else if (CFStringCompare(cfString, kCFJapaneseCalendar, 0) == kCFCompareEqualTo)
    {
        nRetval = CAL_JAPAN;
    }
    else
    {
        ASSERT("Unsupported calendar identifier\n");
        // nRetval = CAL_GREGORIAN_US in this case
    }

    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
    return nRetval;
}

#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
int
GetLocaleInfoHelper(
    IN CFLocaleRef cfLocale,
    IN LCTYPE   LCType,
    OUT LPWSTR  lpLCData,
    IN int      cchData)
{
    INT nRetval = 0; /*return value*/
    CFStringRef cfString = NULL;
    CFIndex length;

    switch (LCType & ~LOCALE_NOUSEROVERRIDE)
    {
    case LOCALE_ILANGUAGE | LOCALE_RETURN_NUMBER:
        nRetval = 0;             // todo: this has "desktop only" note
        goto RETURN_NUMBER;

    case LOCALE_SLANGUAGE:
    {
        CFStringRef language = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode);
        CFStringRef country = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (language != NULL)
        {
            language = CFLocaleCopyDisplayNameForPropertyValue(CFLocaleGetCurrent(), kCFLocaleLanguageCode, language);
            CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, language);
            if (country != NULL && CFStringGetLength(country) > 0)
            {
                country = CFLocaleCopyDisplayNameForPropertyValue(CFLocaleGetCurrent(), kCFLocaleCountryCode, country);
                CFStringAppend(cfMutableString, CFSTR(" (")); 
                CFStringAppend(cfMutableString, country); 
                CFStringAppend(cfMutableString, CFSTR(")"));
                CFRelease(country);
            }
            cfString = cfMutableString;
        }
        break;
    }
    case LOCALE_ICOUNTRY:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (cfString != NULL) 
        {
            CFRetain(cfString);
        }
        break;
    case LOCALE_SCOUNTRY:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (cfString != NULL)
        {
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleCountryCode, cfString);
        }
        break;

    case LOCALE_SABBREVLANGNAME:
        /* fall-through (no equivalent for this property) */
    case LOCALE_SENGLANGUAGE:
        {
            CFLocaleRef cfLocaleEnglish = CFLocaleCreate(kCFAllocatorDefault, CFSTR("en"));
            if (cfLocaleEnglish == NULL)
            {
                ASSERT("CFLocaleCreate failed for \"en\"\n");
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocaleEnglish, kCFLocaleLanguageCode, (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode));
            CFRelease(cfLocaleEnglish);
            break;
        }
    case LOCALE_SNATIVELANGNAME:
        cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleLanguageCode, (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode));
        break;
    case LOCALE_SABBREVCTRYNAME:
        /* fall-through (no equivalent for this property) */
    case LOCALE_SENGCOUNTRY:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (cfString != NULL)
        {
            CFLocaleRef cfLocaleEnglish = CFLocaleCreate(kCFAllocatorDefault, CFSTR("en"));
            if (cfLocaleEnglish == NULL)
            {
                ASSERT("CFLocaleCreate failed for \"en\"\n");
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocaleEnglish, kCFLocaleCountryCode, cfString);
            CFRelease(cfLocaleEnglish);
        }
        else
        {
            cfString = CFSTR("");
            CFRetain(cfString);
        }
        break;
    case LOCALE_SNATIVECTRYNAME:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (cfString != NULL)
        {
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleCountryCode, cfString);
        }
        else
        {
            cfString = CFSTR("");
            CFRetain(cfString);
        }
        break;
    case LOCALE_INEUTRAL | LOCALE_RETURN_NUMBER:
        nRetval = CFLocaleGetValue(cfLocale, kCFLocaleCountryCode) == NULL;
        goto RETURN_NUMBER;
    case LOCALE_SLIST:
        cfString = CFSTR(","); // TODO: some locales would like to use ";"
        CFRetain(cfString);
        break;
    case LOCALE_IMEASURE | LOCALE_RETURN_NUMBER:
        nRetval = !CFBooleanGetValue((CFBooleanRef)CFLocaleGetValue(cfLocale, kCFLocaleUsesMetricSystem));
        goto RETURN_NUMBER;
    case LOCALE_SDECIMAL:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterDecimalSeparator);
        break;
    case LOCALE_STHOUSAND:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterGroupingSeparator);
        break;
    case LOCALE_SGROUPING:
        cfString = CFSTR("3;0");
        CFRetain(cfString);
        break;
    case LOCALE_IDIGITS | LOCALE_RETURN_NUMBER:
        {
            CFNumberRef cfNumber = (CFNumberRef)CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterMaxFractionDigits);
            CFNumberGetValue(cfNumber, kCFNumberSInt32Type, &nRetval);
            CFRelease(cfNumber);
        }
        goto RETURN_NUMBER;
    case LOCALE_ILZERO:
        /* harcoded to return leading zeros in decimal fields */
        cfString = CFSTR("1");
        CFRetain(cfString);
        break;
    case LOCALE_INEGNUMBER | LOCALE_RETURN_NUMBER:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterNegativeSuffix);
        if (CFStringCompare(cfString, CFSTR(")"), 0) == kCFCompareEqualTo)
        {
            nRetval = 0; /* Left parenthesis, number, right parenthesis. Example: (1.1) */
        }
        else if (CFStringGetLength(cfString) == 0)
        {
            nRetval = 1; /* Negative sign, number. Example: -1.1 */
        }
        else
        {
            nRetval = 3; /* Number, negative sign. Example: 1.1- */
        }
        goto RETURN_NUMBER;
    case LOCALE_SNATIVEDIGITS:
        {
            CFNumberFormatterRef cfFormatter = CFNumberFormatterCreate(kCFAllocatorDefault,cfLocale,kCFNumberFormatterNoStyle);
            if (cfFormatter == NULL)
            {
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;
            }
            CFNumberFormatterSetFormat(cfFormatter,CFSTR("0000000000"));
            int number=123456789;
            CFNumberRef cfNumber = CFNumberCreate(kCFAllocatorDefault,kCFNumberIntType,&number);
            if (cfNumber == NULL)
            {
                CFRelease(cfFormatter);
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;
            }
            cfString = CFNumberFormatterCreateStringWithNumber(kCFAllocatorDefault,cfFormatter,cfNumber);
            CFRelease(cfFormatter);
            CFRelease(cfNumber);
            
            if (cfString == NULL)
            {
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;
            }
        }
        
        break;
    case LOCALE_SCURRENCY:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCurrencySymbol);
        CFRetain(cfString);
        break;
    case LOCALE_SINTLSYMBOL:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCurrencyCode);
        if (cfString == NULL)
        {
            cfString = CFSTR("");
        }
        CFRetain(cfString);
        break;
    case LOCALE_SMONDECIMALSEP:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterCurrencyStyle, kCFNumberFormatterCurrencyDecimalSeparator);
        break;
    case LOCALE_SMONTHOUSANDSEP:
        if (!s_bFetchedBundleForCurrencyGroupingSeparator)
        {
            if (!FetchCurrencyGroupingSeparatorFromBundle())
                goto EXIT;   
        }
    
        if (s_CurrencyGroupingSeparatorFromBundle != NULL) 
        {
            cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterCurrencyStyle, *s_CurrencyGroupingSeparatorFromBundle);
        }
        else
        {
  	    cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterCurrencyStyle, kCFNumberFormatterGroupingSeparator);
        }
        break;
    case LOCALE_SMONGROUPING:
        cfString = CFSTR("3;0");
        CFRetain(cfString);
        break;
    case LOCALE_ICURRDIGITS | LOCALE_RETURN_NUMBER:
        /* fall-through */
    case LOCALE_IINTLCURRDIGITS | LOCALE_RETURN_NUMBER:
        {
            int32_t defaultFractionDigits;
            double roundingIncrement;

            cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCurrencyCode);
            if (cfString == NULL)
            {
                // this happens for neutral cultures
                defaultFractionDigits = 2;
            }
            else if (!CFNumberFormatterGetDecimalInfoForCurrencyCode(cfString, &defaultFractionDigits, &roundingIncrement))
            {
                ASSERT("CFNumberFormatterGetDecimalInfoForCurrencyCode failed\n");
                defaultFractionDigits = 2;
            }
            nRetval = defaultFractionDigits;
            cfString = NULL;
        }
        goto RETURN_NUMBER;
    case LOCALE_ICURRENCY | LOCALE_RETURN_NUMBER:
        nRetval = CFLocaleGetICurrency(cfLocale);
        if (nRetval == -1)
            goto EXIT;
        goto RETURN_NUMBER;
    case LOCALE_INEGCURR | LOCALE_RETURN_NUMBER:
        nRetval = CFLocaleGetNegCurr(cfLocale);
        if (nRetval == -1)
            goto EXIT;
        goto RETURN_NUMBER;
    case LOCALE_SSHORTDATE:
        cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterShortStyle, kCFDateFormatterNoStyle);
        if (cfString == NULL)
            goto EXIT;
        break;
    case LOCALE_SLONGDATE:
        cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterFullStyle, kCFDateFormatterNoStyle);
        if (cfString == NULL)
            goto EXIT;
        break;
    case LOCALE_STIMEFORMAT:
        cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterNoStyle, kCFDateFormatterFullStyle);
        if (cfString == NULL)
            goto EXIT;
        break;
    case LOCALE_S1159:
        cfString = CFLocaleCopyDateFormatterProperty(cfLocale, kCFDateFormatterAMSymbol);
        break;
    case LOCALE_S2359:
        cfString = CFLocaleCopyDateFormatterProperty(cfLocale, kCFDateFormatterPMSymbol);
        break;
    case LOCALE_ICALENDARTYPE | LOCALE_RETURN_NUMBER:
        nRetval = CFLocaleGetCALID(cfLocale);
        goto RETURN_NUMBER;
    case LOCALE_IFIRSTDAYOFWEEK | LOCALE_RETURN_NUMBER:
        {
            CFCalendarRef cfCalendar;
            CFIndex cfIndex;

            cfCalendar = (CFCalendarRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendar);
            if (cfCalendar == NULL)
            {
                ASSERT("CFLocaleGetValue(kCFLocaleCalendar) failed\n");
                SetLastError(ERROR_INVALID_PARAMETER);
                goto EXIT;
            }
            cfIndex = CFCalendarGetFirstWeekday(cfCalendar);
            nRetval = ( cfIndex + 5 ) % 7; /* CoreFoundation counts from 1 and there it means Sunday, Win32 counts from 0 and there it means Monday */
        }
        goto RETURN_NUMBER;
    case LOCALE_IFIRSTWEEKOFYEAR | LOCALE_RETURN_NUMBER:
        {
            CFCalendarRef cfCalendar;
            CFIndex cfIndex;

            cfCalendar = (CFCalendarRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendar);
            if (cfCalendar == NULL)
            {
                ASSERT("CFLocaleGetValue(kCFLocaleCalendar) failed\n");
                SetLastError(ERROR_INVALID_PARAMETER);
                goto EXIT;
            }
            cfIndex = CFCalendarGetMinimumDaysInFirstWeek(cfCalendar);
            if (cfIndex == 7)
            {
                nRetval = 1; /* First full week following 1/1 is the first week of that year. */
            }
            else if (cfIndex >= 4)
            {
                nRetval = 2; /* First week containing at least four days is the first week of that year. */
            }
            else
            {
                nRetval = 0; /* Week containing 1/1 is the first week of that year. */
            }
        }
        goto RETURN_NUMBER;

    case LOCALE_SDAYNAME1:
    case LOCALE_SDAYNAME2:
    case LOCALE_SDAYNAME3:
    case LOCALE_SDAYNAME4:
    case LOCALE_SDAYNAME5:
    case LOCALE_SDAYNAME6:
    case LOCALE_SDAYNAME7:
        cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("EEEE"), 1, (LCType & ~LOCALE_NOUSEROVERRIDE) - LOCALE_SDAYNAME1 + 1);
        if (cfString == NULL)
            goto EXIT;
        break;

    case LOCALE_SABBREVDAYNAME1:
    case LOCALE_SABBREVDAYNAME2:
    case LOCALE_SABBREVDAYNAME3:
    case LOCALE_SABBREVDAYNAME4:
    case LOCALE_SABBREVDAYNAME5:
    case LOCALE_SABBREVDAYNAME6:
    case LOCALE_SABBREVDAYNAME7:
        cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("EEE"), 1, (LCType & ~LOCALE_NOUSEROVERRIDE) - LOCALE_SABBREVDAYNAME1 + 1);
        if (cfString == NULL)
            goto EXIT;
        break;

    case LOCALE_SMONTHNAME1:
    case LOCALE_SMONTHNAME2:
    case LOCALE_SMONTHNAME3:
    case LOCALE_SMONTHNAME4:
    case LOCALE_SMONTHNAME5:
    case LOCALE_SMONTHNAME6:
    case LOCALE_SMONTHNAME7:
    case LOCALE_SMONTHNAME8:
    case LOCALE_SMONTHNAME9:
    case LOCALE_SMONTHNAME10:
    case LOCALE_SMONTHNAME11:
    case LOCALE_SMONTHNAME12:
        cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("MMMM"), (LCType & ~LOCALE_NOUSEROVERRIDE) - LOCALE_SMONTHNAME1 + 1, 1);
        if (cfString == NULL)
            goto EXIT;
        break;
    case LOCALE_SMONTHNAME13:
        cfString = CFSTR("");
        CFRetain(cfString);
        break;
    case LOCALE_SABBREVMONTHNAME1:
    case LOCALE_SABBREVMONTHNAME2:
    case LOCALE_SABBREVMONTHNAME3:
    case LOCALE_SABBREVMONTHNAME4:
    case LOCALE_SABBREVMONTHNAME5:
    case LOCALE_SABBREVMONTHNAME6:
    case LOCALE_SABBREVMONTHNAME7:
    case LOCALE_SABBREVMONTHNAME8:
    case LOCALE_SABBREVMONTHNAME9:
    case LOCALE_SABBREVMONTHNAME10:
    case LOCALE_SABBREVMONTHNAME11:
    case LOCALE_SABBREVMONTHNAME12:
        cfString = CFStringCreateFormattedDate(cfLocale, CFSTR("MMM"), (LCType & ~LOCALE_NOUSEROVERRIDE) - LOCALE_SABBREVMONTHNAME1 + 1, 1);
        if (cfString == NULL)
            goto EXIT;
        break;

    case LOCALE_SABBREVMONTHNAME13:
        cfString = CFSTR("");
        CFRetain(cfString);
        break;

    case LOCALE_SYEARMONTH:
        cfString = CFStringCreateYearMonth(cfLocale);
        break;

    case LOCALE_SPOSITIVESIGN:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterPositivePrefix);
        break;
    case LOCALE_SNEGATIVESIGN:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterNegativePrefix);
        break;
    case LOCALE_FONTSIGNATURE:
        cfString = CFSTR("");
        CFRetain(cfString);
        break;
    case LOCALE_SISO639LANGNAME:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode);
        CFRetain(cfString);
        break;
    case LOCALE_SISO3166CTRYNAME:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        if (cfString == NULL)
        {
            cfString = CFSTR("");
        }
        CFRetain(cfString);
        break;
    case LOCALE_SENGCURRNAME:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCurrencyCode);
        if (cfString != NULL)
        {
            CFLocaleRef cfLocaleEnglish = CFLocaleCreate(kCFAllocatorDefault, CFSTR("en"));
            if (cfLocaleEnglish == NULL)
            {
                ASSERT("CFLocaleCreate failed for \"en\"\n");
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;
            }
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocaleEnglish, kCFLocaleCurrencyCode, cfString);
            CFRelease(cfLocaleEnglish);
        }
        else
        {
            cfString = CFSTR("");
            CFRetain(cfString);
        }
        break;

    case LOCALE_SNATIVECURRNAME:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCurrencyCode);
        if (cfString != NULL)
        {
            cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleCurrencyCode, cfString);
        }
        else
        {
            cfString = CFSTR("");
            CFRetain(cfString);
        }
        break;


    case LOCALE_IDIGITSUBSTITUTION | LOCALE_RETURN_NUMBER:
        nRetval = 2; 
        goto RETURN_NUMBER;

    case LOCALE_SPERCENT:
        // Attempted to get from OS but limited support in common scenarios, e.g. ar-EG, so using hard-coded 
        // value like on PC
#if ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterPercentSymbol);
        if (cfString == NULL)
            goto EXIT; 
#else
        cfString = CFSTR("%");
        CFRetain(cfString);
#endif // ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE
        break;

    case LOCALE_SPERMILLE:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterPerMillSymbol); 
        break;

    case LOCALE_IPOSITIVEPERCENT | LOCALE_RETURN_NUMBER:

#if ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE
        nRetval = CFLocaleGetPositivePercent(cfLocale);
        if (nRetval == -1)
            goto EXIT;
#else
        // information from OS too spotty; doing this for now
        nRetval = 0; 
#endif // ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE

        goto RETURN_NUMBER;

    case LOCALE_INEGATIVEPERCENT | LOCALE_RETURN_NUMBER:

#if ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE
        nRetval = CFLocaleGetNegativePercent(cfLocale);
        if (nRetval == -1)
            goto EXIT;
#else
        // information from OS too spotty; doing this for now
        nRetval = 0; 
#endif // ENABLE_MAC_APIS_WITH_SPOTTY_COVERAGE

        goto RETURN_NUMBER;

    case LOCALE_SNAN:
        // even though we get from OS, only seems to return NaN. We can chalk this up to an OS difference 
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterNaNSymbol);
        break;

    case LOCALE_SPOSINFINITY:
        cfString = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterInfinitySymbol);
        break;

    case LOCALE_SNEGINFINITY:
    {
        CFStringRef infinitySymbol = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterInfinitySymbol);
        CFStringRef negativeSymbol = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterNegativePrefix);
        // format string
        CFStringRef negativeFormat = CFLocaleCopyNumberFormatterProperty(cfLocale, kCFNumberFormatterDecimalStyle, kCFNumberFormatterNegativeSuffix);

        if (CFStringCompare(negativeFormat, CFSTR(")"), 0) == kCFCompareEqualTo)
        {
            // Left parenthesis, number, right parenthesis. Example: (1.1) 
            CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, CFSTR("("));
            CFStringAppend(cfMutableString, infinitySymbol);
            CFStringAppend(cfMutableString, CFSTR(")"));
            cfString = cfMutableString;
        }
        else if (CFStringGetLength(negativeFormat) == 0)
        {
            // Negative sign, number. Example: -1.1 
            CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, negativeSymbol);
            CFStringAppend(cfMutableString, infinitySymbol);
            cfString = cfMutableString;
        }
        else
        {
            // Number, negative sign. Example: 1.1- 
            CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, infinitySymbol);
            CFStringAppend(cfMutableString, negativeSymbol);
            cfString = cfMutableString;
        }
 
        if (infinitySymbol != NULL) 
        {
            CFRelease(infinitySymbol);
        }
        if (negativeSymbol != NULL) 
        {
            CFRelease(negativeSymbol);
        }
        if (negativeFormat != NULL) 
        {
            CFRelease(negativeFormat);
        }
        break;

    }
    case LOCALE_SSCRIPTS:
        cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleScriptCode);
        CFRetain(cfString);
        break;

    case LOCALE_SPARENT:
        // RFC 4646: language-Script-COUNTRY-variant
        // the variant part needs adjustment because Mac reports it as TOKEN_TOKEN whereas it should be token-token
        // the rest are reported correctly, including casing
            
        CFStringRef cfStringLanguageCode;
        cfStringLanguageCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode);

        // should be always present
        if(cfStringLanguageCode == NULL)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            goto EXIT;                
        }
        CFStringRef cfStringCountryCode, cfStringScriptCode, cfStringVariantCode;
        cfStringCountryCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
        cfStringScriptCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleScriptCode);
        cfStringVariantCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleVariantCode );

        // adjust the variant if present
        if (cfStringVariantCode)
        {
            CFMutableStringRef cfMutableStringVariantCode = CFStringCreateMutableCopy(kCFAllocatorDefault,
                                                                                      CFStringGetLength(cfStringVariantCode),
                                                                                      cfStringVariantCode);

            if (cfMutableStringVariantCode == NULL)
            {
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;
            }
            CFStringFindAndReplace(cfMutableStringVariantCode,
                                                   CFSTR("_"),
                                                   CFSTR("-"),
                                                   CFRangeMake(0,CFStringGetLength(cfStringVariantCode)),
                                                   0);
            CFStringLowercase(cfMutableStringVariantCode,CFLocaleGetSystem());
            cfStringVariantCode=cfMutableStringVariantCode;
        }

        // now build up parent based on the elements that are present
        if (cfStringScriptCode != NULL || cfStringCountryCode != NULL || cfStringVariantCode != NULL)
        {
            // append language if script, country code, or variant are present
            CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, cfStringLanguageCode);
            if (cfMutableString == NULL)
            {
                // cfStringVariantCode is a result of CFStringCreateMutableCopy (see "adjust" above) 
                if (cfStringVariantCode)
                    CFRelease(cfStringVariantCode);                    
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;
            }

            // append script if it's present and country code or variant is present. We have to special 
            // case for e.g. zh-TW, described below.
            if (cfStringScriptCode != NULL)
            {
                if (cfStringCountryCode != NULL || cfStringVariantCode != NULL)
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, cfStringScriptCode);
                }
            }
            else if (cfStringCountryCode != NULL)
            {
                // Special cases: Only have to worry about the following cases. Canonicalization API seems
                // to be broken. According to the docs, that routine should have converted zh-TW ->
                // zh-Hant-TW. That's our best approximation to how mac does fallback. Instead, special
                // case these. Note that there is first class support for Norwegian language nb so we
                // don't have to special case that.
                // - zh-TW, zh-MO, zh-HK -> zh-Hant
                // - zh-CN, zh-SG -> zh-Hans

                if ((CFStringCompare(cfStringLanguageCode, CFSTR("zh"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) &&
                        ( (CFStringCompare(cfStringCountryCode, CFSTR("TW"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) ||
                          (CFStringCompare(cfStringCountryCode, CFSTR("MO"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) ||
                          (CFStringCompare(cfStringCountryCode, CFSTR("HK"), kCFCompareCaseInsensitive) == kCFCompareEqualTo)
                        )
                    )
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, CFSTR("Hant"));
                }
                else if ((CFStringCompare(cfStringLanguageCode, CFSTR("zh"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) &&
                        ( (CFStringCompare(cfStringCountryCode, CFSTR("CN"), kCFCompareCaseInsensitive) == kCFCompareEqualTo) ||
                          (CFStringCompare(cfStringCountryCode, CFSTR("SG"), kCFCompareCaseInsensitive) == kCFCompareEqualTo)
                        )
                    )
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, CFSTR("Hans"));
                }
            }

            // append country code if it's present and variant code is present
            if (cfStringCountryCode != NULL) 
            {
                if (cfStringVariantCode != NULL)
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, cfStringCountryCode);
                }
            } 
                
            cfString = cfMutableString;
        }
        else
        {
            cfString = CFSTR("");
	    CFRetain(cfString);
        }

        if (cfStringVariantCode != NULL)
        {
            CFRelease(cfStringVariantCode);
        }

       break;
    case LOCALE_SLANGDISPLAYNAME:
        cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleLanguageCode, (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode));
        break;


    case LOCALE_SDURATION:
    case LOCALE_SKEYBOARDSTOINSTALL:
    case LOCALE_SISO639LANGNAME2:
    case LOCALE_SISO3166CTRYNAME2:
    case LOCALE_IREADINGLAYOUT | LOCALE_RETURN_NUMBER:
        // kdh: todo -- fixing  after response about subset bitfield trick. If that doesn't work, 
        // just using best list of RTL languages
        nRetval = 0;
        goto RETURN_NUMBER;

    case LOCALE_SCONSOLEFALLBACKNAME:
    case LOCALE_SSHORTTIME:
    case LOCALE_SENGLISHDISPLAYNAME:
    case LOCALE_SNATIVEDISPLAYNAME:
    case LOCALE_SMONTHDAY:
        // unimplemented Win7+ string LCType cases.  The BCL CultureInfo builds up these properties from
        // other existing properties so there is currently no need to implement these cases in the PAL
        // until Apple adds real support for these culture properties
        goto EXIT;
    case LOCALE_SNAME:
        {
            // RFC 4646: language-Script-COUNTRY-variant
            // the variant part needs adjustment because Mac reports it as TOKEN_TOKEN whereas it should be token-token
            // the rest are reported correctly, including casing
            
            CFStringRef cfStringLanguageCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleLanguageCode);
            if (cfStringLanguageCode != NULL)
            {
                CFRetain(cfStringLanguageCode);
            }
            else 
            {
                // should always be present
                SetLastError(ERROR_OUTOFMEMORY);
                goto EXIT;                
            }
            
            CFStringRef cfStringCountryCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCountryCode);
            if (cfStringCountryCode != NULL)
            {
                CFRetain(cfStringCountryCode);
            }

            CFStringRef cfStringScriptCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleScriptCode);
            if (cfStringScriptCode != NULL)
            {
                CFRetain(cfStringScriptCode);
            }

            CFStringRef cfStringCorrectedVariantCode = NULL;
            CFStringRef cfStringVariantCode = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleVariantCode );
            if (cfStringVariantCode != NULL)
            {
                CFRetain(cfStringVariantCode);

                // adjust 
                CFMutableStringRef cfMutableStringVariantCode = CFStringCreateMutableCopy(kCFAllocatorDefault,
                                                                                          CFStringGetLength(cfStringVariantCode),
                                                                                          cfStringVariantCode);

                if (cfMutableStringVariantCode == NULL)
                {
                    SetLastError(ERROR_OUTOFMEMORY);
                    goto CLEANUP;
                }
                CFStringFindAndReplace(cfMutableStringVariantCode,
                                                    CFSTR("_"),
                                                    CFSTR("-"),
                                                    CFRangeMake(0,CFStringGetLength(cfStringVariantCode)),
                                                    0);
                CFStringLowercase(cfMutableStringVariantCode,CFLocaleGetSystem());

                cfStringCorrectedVariantCode=cfMutableStringVariantCode;

                // release cfStringVariantCode and set to NULL to catch attempts to access subsequently.
                // Only introducing cfStringCorrectedVariantCode to make retain/release tracking easier
                // to spot.
                CFRelease(cfStringVariantCode);
                cfStringVariantCode = NULL;
            }

            //combine
            if (cfStringCountryCode != NULL || cfStringScriptCode !=NULL || cfStringCorrectedVariantCode != NULL)
            {
                CFMutableStringRef cfMutableString = CFStringCreateMutableCopy(kCFAllocatorDefault, 0, cfStringLanguageCode);
                if (cfMutableString == NULL)
                {
                    SetLastError(ERROR_OUTOFMEMORY);
                    goto CLEANUP;
                }
                
                if (cfStringScriptCode)
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, cfStringScriptCode);
                }
                if (cfStringCountryCode)
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, cfStringCountryCode);
                }
                if (cfStringCorrectedVariantCode)
                {
                    CFStringAppend(cfMutableString, CFSTR("-"));
                    CFStringAppend(cfMutableString, cfStringCorrectedVariantCode);
                }
                
                cfString = cfMutableString;
            }
            else
            {
                cfString = cfStringLanguageCode;
                CFRetain(cfString);
            }

CLEANUP:
            // release everything we retained above
            if (cfStringLanguageCode != NULL)
            {
                CFRelease(cfStringLanguageCode);
            }
            if (cfStringCountryCode != NULL)
            {
                CFRelease(cfStringCountryCode);
            }
            if (cfStringScriptCode != NULL)
            {
                CFRelease(cfStringScriptCode);
            }
            // cfStringVariantCode already released, but we still need to release
            // the corrected variant code
            if (cfStringCorrectedVariantCode != NULL)
            {
                CFRelease(cfStringCorrectedVariantCode);
            }

        }
        break;
    default :
        ASSERT("LCType(%#x) parameter is invalid\n",LCType);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
     
    if (LCType & LOCALE_RETURN_NUMBER)
    {
        ASSERT("Trying to return a string for LCType %#x\n", LCType);
    }
    if (cfString == NULL)
    {
        // bad locale, fail
        WARN("GetLocaleInfoHelper: returning  NULL for LCType %#x\n", LCType);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
        
    }

    length = CFStringGetLength(cfString);
    if (cchData == 0)
    {
        nRetval = length + 1;
    }
    else if (cchData >= length + 1)
    {
        CFStringGetCharacters(cfString, CFRangeMake(0, length), (UniChar*)lpLCData);
        lpLCData[length] = L'\0';
        nRetval = length + 1;
    }
    else
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        nRetval = 0;
    }
    goto EXIT;

RETURN_NUMBER:
    if (!(LCType & LOCALE_RETURN_NUMBER))
    {
        ASSERT("Trying to return anumber for LCType %#x\n", LCType);
    }

    if (cchData >= static_cast<int>(sizeof(INT32) / sizeof(WCHAR)))
    {
        *(INT32 *)lpLCData = nRetval;
        nRetval = sizeof(INT32) / sizeof(WCHAR);
    }
    else if (cchData == 0)
    {
        nRetval = sizeof(INT32) / sizeof(WCHAR);
    }
    else
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        nRetval = 0;
    }


EXIT:    
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
    return nRetval;
}
/*++
Function:
  GetLocaleInfoEx

See MSDN doc.
--*/
int
PALAPI
GetLocaleInfoEx(
    IN LPCWSTR  lpLocaleName,
    IN LCTYPE   LCType,
    OUT LPWSTR  lpLCData,
    IN int      cchData)
{
    INT nRetval = 0; /*return value*/
    CFLocaleRef cfLocale;

    PERF_ENTRY(GetLocaleInfoEx);
    ENTRY("GetLocaleInfoEx(lpLocaleName=%p (%S), LCType=%#x, lpLCData=%p, cchData=%d)\n",
          lpLocaleName, lpLocaleName? lpLocaleName : W16_NULLSTRING,
            LCType, lpLCData, cchData);
    
    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%#04x) parameter is invalid\n",lpLocaleName);
        }
        goto EXIT;
    }

    nRetval=GetLocaleInfoHelper(cfLocale,LCType,lpLCData,cchData);


EXIT:    
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }

    if (nRetval != 0 && cchData != 0 && !(LCType & LOCALE_RETURN_NUMBER))
    {
        LOGEXIT("GetLocaleInfoEx returns int %d (%S)\n", nRetval, lpLCData);
    }
    else
    {
        LOGEXIT("GetLocaleInfoEx returns int %d\n", nRetval);
    }
    PERF_EXIT(GetLocaleInfoEx);
    return nRetval;
}


int 
PALAPI
CompareStringOrdinal(
    IN LPCWSTR lpString1, 
	IN int cchCount1, 
	IN LPCWSTR lpString2, 
	IN int cchCount2, 
	IN BOOL bIgnoreCase)
{
    INT nRetVal = 0;  /* return value: 0 (failure), CSTR_* (success) */

    DWORD dwCmpFlags = bIgnoreCase ? NORM_IGNORECASE : 0;

    PERF_ENTRY(CompareStringOrdinal);
    ENTRY("CompareStringOrdinal(lpString1=%p (%S), "
          "cchCount1=%d, lpString2=%p (%S), cchCount2=%d)\n",
          lpString1, lpString1, cchCount1,lpString2,lpString2,cchCount2);

    nRetVal=CompareStringHelper(NULL,dwCmpFlags|SORT_STRINGSORT,lpString1,cchCount1,lpString2,cchCount2,TRUE);

    LOGEXIT("CompareStringOrdinal returns int %d\n", nRetVal);
    PERF_EXIT(CompareStringOrdinal);
    return nRetVal;
}

// disabling for now
#if 0
int 
PALAPI
FindNLSStringEx(
    IN LPCWSTR lpLocaleName, 
	IN DWORD dwFindNLSStringFlags, 
	IN LPCWSTR lpStringSource, 
	IN int cchSource, 
    IN LPCWSTR lpStringValue, 
	IN int cchValue, 
	OUT LPINT pcchFound, 
	IN LPNLSVERSIONINFOEX lpVersionInformation, 
	IN LPVOID lpReserved, 
	IN LPARAM lParam )
{
    // TODO: -- implement when more consistent NLS support is available
    return -1;
}
#endif

BOOL 
PALAPI
IsNLSDefinedString(
    IN NLS_FUNCTION Function, 
	IN DWORD dwFlags, 
	IN LPNLSVERSIONINFOEX lpVersionInfo, 
	IN LPCWSTR lpString, 
	IN int cchStr ) 
{
	// TODO: implement
    return FALSE;
}


BOOL 
PALAPI
GetThreadPreferredUILanguages(
    IN DWORD  dwFlags,
    OUT PULONG  pulNumLanguages,
    OUT PWSTR  pwszLanguagesBuffer,
    IN OUT PULONG  pcchLanguagesBuffer)
{
    PERF_ENTRY(GetThreadPreferredUILanguages);
    ENTRY("GetThreadPreferredUILanguages(dwFlags=%08x,pulNumLanguages=%p,pwszLanguagesBuffer=%p,*pcchLanguagesBuffer=%08x)\n",
        dwFlags,pulNumLanguages,pwszLanguagesBuffer,*pcchLanguagesBuffer);

    BOOL bRetVal=FALSE;

    CFArrayRef cfLanguagesArray = NULL;
    DWORD dwLen=0;

#if 0 // the implementation below works, but needs additional sanitization of the values provided by the OS
     
    DWORD dwMaxLen=*pcchLanguagesBuffer;
    bool bCheckSizeOnly=(pwszLanguagesBuffer==NULL && dwMaxLen==0);
    bool bBufferTooSmall=false;
    
    cfLanguagesArray = (CFArrayRef)CFPreferencesCopyValue(CFSTR("AppleLanguages"),
                                                            kCFPreferencesAnyApplication,
                                                            kCFPreferencesCurrentUser,
                                                            kCFPreferencesAnyHost);

    if (cfLanguagesArray == NULL)        
    {
        //none 
        dwLen=0;
        bRetVal=TRUE;
        goto EXIT;
    }

    // check whether cast to CFArrayRef was valid
    if (CFGetTypeID(cfLanguagesArray) !=CFArrayGetTypeID())
    {
        _ASSERT(FALSE);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }

    *pulNumLanguages=CFArrayGetCount(cfLanguagesArray);
    
    for (CFIndex i=0; i< CFArrayGetCount(cfLanguagesArray);i++)
    {
        CFStringRef cfArrayElement=(CFStringRef)CFArrayGetValueAtIndex(cfLanguagesArray,i);
        //check cast validity
        if (CFGetTypeID(cfArrayElement) !=CFStringGetTypeID())
        {
            _ASSERT(FALSE);
            SetLastError(ERROR_INTERNAL_ERROR);
            goto EXIT;
        }

        
        CFStringRef cfLocale=CFLocaleCreateCanonicalLanguageIdentifierFromString(
            kCFAllocatorDefault,
            cfArrayElement) ;
        if (cfLocale == NULL)        
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }

        DWORD dwLocaleLen=CFStringGetLength(cfLocale)+1;
        
        if (!bCheckSizeOnly && !bBufferTooSmall)
        {
            if (dwLen + dwLocaleLen < dwMaxLen)
            {
                //add string
                CFStringGetCharacters(cfLocale,CFRangeMake(0,dwLocaleLen),pwszLanguagesBuffer+dwLen);
                pwszLanguagesBuffer[dwLen+dwLocaleLen]=L'\0';

            }
            else
            {
                bBufferTooSmall=true;
            }
        }
        dwLen+=dwLocaleLen;
        CFRelease(cfLocale);
    }
    
    bBufferTooSmall |=(dwLen >=dwMaxLen);
    if(!bBufferTooSmall)
    {
        //add null
        pwszLanguagesBuffer[dwLen]=L'\0';
    }
    dwLen++;

    if(bBufferTooSmall && !bCheckSizeOnly)
    {
        SetLastError(ERROR_BUFFER_OVERFLOW);
    }
    else
    {
        bRetVal=TRUE;
    }

EXIT:
#else // 0
    dwLen=0;
    bRetVal=TRUE;
#endif
    *pcchLanguagesBuffer=dwLen;

    if (cfLanguagesArray != NULL)
        CFRelease(cfLanguagesArray);
    LOGEXIT("GetThreadPreferredUILanguages returns  %d\n", bRetVal);
    PERF_EXIT(GetThreadPreferredUILanguages);
    return bRetVal;
};


PALIMPORT
int
PALAPI
ResolveLocaleName(
    IN LPCWSTR lpNameToResolve,
        OUT LPWSTR lpLocaleName,
        IN int cchLocaleName )
{
    // Since neutrals are supported first-class on mac, just return the name passed in
    PERF_ENTRY(ResolveLocaleName);
    ENTRY("ResolveLocaleName(lpNameToResolve=%#04x,lpLocaleName=%p,cchLocaleName=%d)\n",
        lpNameToResolve ? lpNameToResolve : W16_NULLSTRING,lpLocaleName,cchLocaleName);

    int iRetVal = 0;

    int iStrLen = PAL_wcslen(lpNameToResolve) + 1;
    if (iStrLen > cchLocaleName) 
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto EXIT;
    }       

    PAL_wcscpy(lpLocaleName, lpNameToResolve);        
    iRetVal = iStrLen;

EXIT:
    LOGEXIT("ResolveLocaleName returns  %d\n", iRetVal);
    PERF_EXIT(ResolveLocaleName);
    return iRetVal;
}


int 
PALAPI
GetSystemDefaultLocaleName(
    OUT LPWSTR lpLocaleName, 
	IN int cchLocaleName)
{
    PERF_ENTRY(GetSystemDefaultLocaleName);
    ENTRY("GetSystemDefaultLocaleName(lpLocaleName=%p,cchLocaleName=%d)\n",lpLocaleName,cchLocaleName);

    int iRetVal=0;
    CFStringRef cfLocaleName = NULL;
    CFLocaleRef cfLocale = CFLocaleGetSystem();
    if (cfLocale == NULL)
    {
         SetLastError(ERROR_OUTOFMEMORY);
         goto EXIT;
    }
    CFRetain(cfLocale);

    cfLocaleName=CFLocaleGetIdentifier(cfLocale);
    if (cfLocaleName == NULL)
    {
         SetLastError(ERROR_OUTOFMEMORY);
         goto EXIT;
    }
    CFRetain(cfLocaleName);
    
    int iStrLen;
    iStrLen = CFStringGetLength(cfLocaleName);

    if (cchLocaleName <= iStrLen)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto EXIT;
    }
    CFStringGetCharacters(cfLocaleName,CFRangeMake(0,iStrLen),(UniChar*)lpLocaleName);
    lpLocaleName[iStrLen]=L'\0';
    iRetVal=iStrLen+1;

EXIT:
    if (cfLocaleName != NULL) 
    {
        CFRelease(cfLocaleName);
    }
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    LOGEXIT("GetSystemDefaultLocaleName returns  %d\n", iRetVal);
    PERF_EXIT(GetSystemDefaultLocaleName);
    return iRetVal;
}


#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS


#if ENABLE_DOWNLEVEL_FOR_NLS

/*++
Function:
  GetLocaleInfoW

See MSDN doc.
--*/
int
PALAPI
GetLocaleInfoW(
    IN LCID     Locale,
    IN LCTYPE   LCType,
    OUT LPWSTR  lpLCData,
    IN int      cchData)
{
    INT nRetval = 0; /*return value*/
#if HAVE_XLOCALE
    LPSTR lpLocaleName;
    locale_t loc = NULL;
#endif // HAVE_XLOCALE
    struct lconv * LCConv;
    char * InputStr;

    PERF_ENTRY(GetLocaleInfoW);
    ENTRY("GetLocaleInfoW(Locale=%#04x, LCType=%#x, lpLCData=%p, cchData=%d)\n",
          Locale, LCType, lpLCData, cchData);
    
#if HAVE_XLOCALE
    lpLocaleName = GetUnixLocaleNameFromLCID(Locale);
    if (lpLocaleName == NULL || !strcmp(lpLocaleName, ""))
#else // HAVE_XLOCALE
    if (Locale != LOCALE_NEUTRAL && Locale != LOCALE_US_ENGLISH)
#endif // HAVE_XLOCALE
    {
        ASSERT("Locale(%#04x) parameter is invalid\n",Locale);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }



#if HAVE_XLOCALE
    if ((loc = newlocale(LC_ALL_MASK, lpLocaleName, NULL)) == NULL)
    {
        ASSERT("newlocale failed for localename %s\n", lpLocaleName);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }
    LCConv = localeconv_l(loc);
#else // HAVE_XLOCALE
    LCConv = localeconv();
#endif // HAVE_XLOCALE

    switch (LCType & ~LOCALE_NOUSEROVERRIDE)
    {
    /* Harcoding most of the  values for US_ENGLISH
     * as these values are not defined for FreeBSD 4.5 */
    case LOCALE_SDECIMAL:
        InputStr = LCConv->decimal_point;
        break;
    case LOCALE_STHOUSAND:
        /*InputStr = LCConv->thousands_sep;*/
        InputStr = ",";
        break;
    case LOCALE_ILZERO:
        /*harcoded to return leading zeros
         *in decimal fields*/
        InputStr = "1";
        break;
    case LOCALE_SCURRENCY:
        /*InputStr = LCConv->currency_symbol;*/
        InputStr = "$";
        break;
    case LOCALE_SMONDECIMALSEP:
        /*InputStr = LCConv->mon_decimal_point;*/
        InputStr = ".";
        break;
    case LOCALE_SMONTHOUSANDSEP:
        /*InputStr = LCConv->mon_thousands_sep;*/
        InputStr = ",";
        break;

    default :
        ASSERT("LCType(%#x) parameter is invalid\n",LCType);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
     

    /*if output buffer size is zero return the
      *size of buffer required*/
    if(cchData == 0)
    {
        nRetval = MultiByteToWideChar(CP_ACP,0,InputStr,-1,NULL,0);
    }
    else
    {
        nRetval = MultiByteToWideChar(CP_ACP,0,InputStr, -1,lpLCData,cchData);     
    }

    if (!nRetval && (ERROR_INSUFFICIENT_BUFFER != GetLastError()))
    {
        ASSERT("MultiByteToWideChar failed.  Error is %d\n", GetLastError());
    }
EXIT:    
#if HAVE_XLOCALE
    if (loc != NULL)
    {
        freelocale(loc);
    }
#endif // HAVE_XLOCALE
    if (nRetval != 0 && cchData != 0 && !(LCType & LOCALE_RETURN_NUMBER))
    {
        LOGEXIT("GetLocaleInfoW returns int %d (%S)\n", nRetval, lpLCData);
    }
    else
    {
        LOGEXIT("GetLocaleInfoW returns int %d\n", nRetval);
    }
    PERF_EXIT(GetLocaleInfoW);
    return nRetval;
}
#endif // ENABLE_DOWNLEVEL_FOR_NLS

#if HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS

typedef enum
{
    EnumContinue,
    EnumStop,
    EnumError
} EnumResult;

static EnumResult EnumDateFormatsExExHelper(DATEFMT_ENUMPROCEXEXW lpDateFmtEnumProcEx, CFLocaleRef cfLocale, CFDateFormatterStyle cfDateFormatterStyle, LPARAM lParam)
{
    EnumResult enumResult = EnumError;
    CFDateFormatterRef cfDateFormatter = NULL;
    CFStringRef cfString = NULL;
    CFIndex length;
    WCHAR *buffer = NULL;
    CALID calid;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, cfDateFormatterStyle, kCFDateFormatterNoStyle);
    if (cfDateFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfString = MapFormatPattern(CFDateFormatterGetFormat(cfDateFormatter), dateTimeFormatPatternMap);
    // created by MapFormatPattern; no need to retain

    length = CFStringGetLength(cfString);

    buffer = (WCHAR *)PAL_malloc((length + 1) * sizeof(WCHAR));
    if (buffer == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    CFStringGetCharacters(cfString, CFRangeMake(0, length), (UniChar*)buffer);
    buffer[length] = L'\0';

    calid = CFLocaleGetCALID(cfLocale);
    TRACE("EnumDateFormatsExEx invoking callback for \"%S\", %d\n", buffer, calid);
    enumResult = lpDateFmtEnumProcEx(buffer, calid, lParam) ? EnumContinue : EnumStop;

EXIT:
    if (buffer != NULL)
    {
        PAL_free(buffer);
    }
    if (cfDateFormatter != NULL)
    {
        CFRelease(cfDateFormatter);
    }
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
    return enumResult;
}

/*++
Function:
  EnumDateFormatsExEx

See MSDN doc.
--*/
BOOL
PALAPI
EnumDateFormatsExEx(
    IN DATEFMT_ENUMPROCEXEXW lpDateFmtEnumProcEx,
    IN LPCWSTR             lpLocaleName,
    IN DWORD               dwFlags,
    IN LPARAM lParam)
{
    BOOL fRetval = FALSE;
    CFLocaleRef cfLocale = NULL;

    PERF_ENTRY(EnumDateFormatsExEx);
    ENTRY("EnumDateFormatsExEx(lpDateFmtEnumProcEx=%p, Locale=%S, dwFlags=%#x, Lparam=%p)\n",
          lpDateFmtEnumProcEx, lpLocaleName? lpLocaleName : W16_NULLSTRING , dwFlags, lParam);

    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n", lpLocaleName? lpLocaleName : W16_NULLSTRING);
        }
        goto EXIT;
    }

#define ENUM_DATE_FORMAT(style)                                             \
    switch (EnumDateFormatsExExHelper(lpDateFmtEnumProcEx, cfLocale, style, lParam)) \
    {                                                                       \
    case EnumError:                                                         \
        fRetval = FALSE;                                                    \
        goto EXIT;                                                          \
    case EnumStop:                                                          \
        fRetval = TRUE;                                                     \
        goto EXIT;                                                          \
    case EnumContinue:                                                      \
        break;                                                              \
    }

    if ((dwFlags & (DATE_LONGDATE|DATE_SHORTDATE|DATE_YEARMONTH)) == 0)
    {
        ENUM_DATE_FORMAT(kCFDateFormatterFullStyle);
        ENUM_DATE_FORMAT(kCFDateFormatterLongStyle);
        ENUM_DATE_FORMAT(kCFDateFormatterMediumStyle);
        ENUM_DATE_FORMAT(kCFDateFormatterShortStyle);
    }
    else if (dwFlags & DATE_LONGDATE)
    {
        if (dwFlags & (DATE_SHORTDATE|DATE_YEARMONTH))
        {
            ASSERT("dwFlags(%#x) invalid\n", dwFlags);
            SetLastError(ERROR_INVALID_FLAGS);
            goto EXIT;
        }
        ENUM_DATE_FORMAT(kCFDateFormatterFullStyle);
        ENUM_DATE_FORMAT(kCFDateFormatterLongStyle);
        ENUM_DATE_FORMAT(kCFDateFormatterMediumStyle);
    }
    else if (dwFlags & DATE_SHORTDATE)
    {
        if (dwFlags & (DATE_LONGDATE|DATE_YEARMONTH))
        {
            ASSERT("dwFlags(%#x) invalid\n");
            SetLastError(ERROR_INVALID_FLAGS);
            goto EXIT;
        }
        ENUM_DATE_FORMAT(kCFDateFormatterShortStyle);
    }
    else if (dwFlags & DATE_YEARMONTH)
    {
        if (dwFlags & (DATE_LONGDATE|DATE_SHORTDATE))
        {
            ASSERT("dwFlags(%#x) invalid\n");
            SetLastError(ERROR_INVALID_FLAGS);
            goto EXIT;
        }
        static WCHAR yearMonth[]={'y','y','y','y',' ','M','M','M','M',0};
        if (!lpDateFmtEnumProcEx(yearMonth, CFLocaleGetCALID(cfLocale),lParam))
        {
            fRetval=FALSE;
            goto EXIT;
        }
    }
    fRetval = TRUE;

EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    LOGEXIT("EnumDateFormatsExEx returns bool %d\n", fRetval);
    PERF_EXIT(EnumDateFormatsExEx);
    return fRetval;
}

static EnumResult EnumTimeFormatsExHelper(TIMEFMT_ENUMPROCEXW lpTimeFmtEnumProc, CFLocaleRef cfLocale, CFDateFormatterStyle cfDateFormatterStyle, LPARAM lParam)
{
    EnumResult enumResult = EnumError;
    CFDateFormatterRef cfDateFormatter = NULL;
    CFStringRef cfString = NULL;
    CFIndex length;
    WCHAR *buffer = NULL;

    cfDateFormatter = CFDateFormatterCreate(kCFAllocatorDefault, cfLocale, kCFDateFormatterNoStyle, cfDateFormatterStyle);
    if (cfDateFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfString = MapFormatPattern(CFDateFormatterGetFormat(cfDateFormatter), dateTimeFormatPatternMap);

    length = CFStringGetLength(cfString);

    buffer = (WCHAR *)PAL_malloc((length + 1) * sizeof(WCHAR));
    if (buffer == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    CFStringGetCharacters(cfString, CFRangeMake(0, length), (UniChar*)buffer);
    buffer[length] = L'\0';

    TRACE("EnumTimeFormatsEx invoking callback for \"%S\"\n", buffer);
    enumResult = lpTimeFmtEnumProc(buffer, lParam) ? EnumContinue : EnumStop;

EXIT:
    if (buffer != NULL)
    {
        PAL_free(buffer);
    }
    if (cfDateFormatter != NULL)
    {
        CFRelease(cfDateFormatter);
    }
    if (cfString != NULL) 
    {
        CFRelease(cfString);
    }

    return enumResult;
}

/*++
Function:
  EnumTimeFormatsEx

See MSDN doc.
--*/
BOOL
PALAPI
EnumTimeFormatsEx(
    IN TIMEFMT_ENUMPROCEXW lpTimeFmtEnumProc,
    IN LPCWSTR           lpLocaleName,
    IN DWORD             dwFlags,
    IN LPARAM            lParam
)
{
    BOOL fRetval = FALSE;
    CFLocaleRef cfLocale = NULL;

    PERF_ENTRY(EnumTimeFormatsEx);
    ENTRY("EnumTimeFormatsEx(lpTimeFmtEnumProc=%p, Locale=%#04x, dwFlags=%#x, lParam=%p)\n",
          lpTimeFmtEnumProc, lpLocaleName? lpLocaleName : W16_NULLSTRING , dwFlags, lParam);

    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n",lpLocaleName? lpLocaleName : W16_NULLSTRING);
        }
        goto EXIT;
    }

#define ENUM_TIME_FORMAT(style)                                                     \
    switch (EnumTimeFormatsExHelper(lpTimeFmtEnumProc, cfLocale, style, lParam))     \
    {                                                                       \
    case EnumError:                                                         \
        fRetval = FALSE;                                                    \
        goto EXIT;                                                          \
    case EnumStop:                                                          \
        fRetval = TRUE;                                                     \
        goto EXIT;                                                          \
    case EnumContinue:                                                      \
        break;                                                              \
    }

    // NOTE: System.Globalization.DateTimeFormatInfo.ShortTimePattern and
    //       System.Globalization.CultureData.ShortTimes depend on the amount
    //       and order of the data returned here.  If you update this code please
    //       ensure that you update the managed code as well!
    ENUM_TIME_FORMAT(kCFDateFormatterFullStyle);
    ENUM_TIME_FORMAT(kCFDateFormatterLongStyle);
    ENUM_TIME_FORMAT(kCFDateFormatterMediumStyle);
    ENUM_TIME_FORMAT(kCFDateFormatterShortStyle);
    fRetval = TRUE;

EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    LOGEXIT("EnumTimeFormatsEx returns bool %d\n", fRetval);
    PERF_EXIT(EnumTimeFormatsEx);
    return fRetval;
}

static BOOL EnumCalendarInfoExEx_Helper(
    CALINFO_ENUMPROCEXEXW lpCalInfoEnumProc,
    CFLocaleRef       cfLocale,
    CALID             calid,
    LPCWSTR           lpReserved,
    CALTYPE           CalType,
    LPARAM            lParam)
{
    CFStringRef cfString = NULL;

    switch (CalType & ~CAL_NOUSEROVERRIDE)
    {
        case CAL_SSHORTDATE:
            cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterShortStyle, kCFDateFormatterNoStyle); 
            break;

        case CAL_SLONGDATE:
            cfString = CFStringCreateWithDateFormat(cfLocale, kCFDateFormatterFullStyle, kCFDateFormatterNoStyle); 
            break;

        case CAL_SYEARMONTH:
            cfString = CFStringCreateYearMonth(cfLocale);
            break;

        case CAL_ICALINTVALUE:
        case CAL_ICALINTVALUE | CAL_RETURN_NUMBER:
        {
            WCHAR buffer[20];
            WCHAR wzFormatString[] = {'%', 'd', '\0'};
            _snwprintf_s(buffer, sizeof(buffer)/sizeof(*buffer), sizeof(buffer)/sizeof(*buffer), wzFormatString, calid);
            TRACE("EnumCalendarInfoExEx invoking callback for \"%S\"\n", buffer);
            lpCalInfoEnumProc(buffer,calid,const_cast<LPWSTR>(lpReserved),lParam);
            goto EXIT;
        }
        case CAL_SCALNAME:

            cfString = (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendarIdentifier);
            if (cfString != NULL)
            {
                cfString = CFLocaleCopyDisplayNameForPropertyValue(cfLocale, kCFLocaleCalendarIdentifier, cfString);
            }
            break;

        default:
            ASSERT("CalType(%#x) is invalid", CalType);
            SetLastError(ERROR_INVALID_PARAMETER);
            goto EXIT;
    }
    
    WCHAR *buffer;
    CFIndex length;
    length = CFStringGetLength(cfString);

    buffer = (WCHAR *)PAL_malloc((length + 1) * sizeof(WCHAR));
    if (buffer == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return FALSE;
    }
    CFStringGetCharacters(cfString, CFRangeMake(0, length), (UniChar*)buffer);

    buffer[length] = L'\0';
    TRACE("EnumCalendarInfoExEx invoking callback for \"%S\"\n", buffer);
    lpCalInfoEnumProc(buffer, calid, const_cast<LPWSTR>(lpReserved), lParam);
    PAL_free(buffer);

EXIT:
    if (cfString != NULL) 
    {
        CFRelease(cfString);
    }
    return TRUE;
}

/*++
Function:
  AddCalendarToLocale

Add specified calendar to a locale. 

--*/
BOOL
PALAPI
AddCalendarsToLocale(
    IN LPCWSTR           lpLocale,
    const struct CalIdPair *calIdsToAdd,
    IN int               numCalsToAdd,
    IN uint              defaultCalid,
    IN CALINFO_ENUMPROCEXEXW lpCalInfoEnumProc,
    IN LPCWSTR           lpReserved,
    IN CALTYPE           CalType,
    IN LPARAM            lParam)
{

    PERF_ENTRY(AddCalendarsToLocale);

    BOOL fRetval = FALSE;
    CFDictionaryRef cfDictionary = NULL;
    CFMutableDictionaryRef cfMutableDictionary = NULL;
    CFLocaleRef cfLocale = NULL;
    CALID calid;
    CFStringRef tempLocaleName = NULL;

    CFStringRef cfStringLocaleName = CFStringCreateMacFormattedLocaleName(lpLocale);
    if (cfStringLocaleName == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfDictionary = CFLocaleCreateComponentsFromLocaleIdentifier(kCFAllocatorDefault, cfStringLocaleName);
    if (cfDictionary == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfMutableDictionary = CFDictionaryCreateMutableCopy(kCFAllocatorDefault, CFDictionaryGetCount(cfDictionary) + 1, cfDictionary);
    CFRelease(cfDictionary);
    if (cfMutableDictionary == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    for(int i = 0; i < numCalsToAdd; i++)
    {
        // don't re-add default calendar
        if (defaultCalid == calIdsToAdd[i].pcCalId)
        {
            continue;
        }

        CFDictionarySetValue(cfMutableDictionary, kCFLocaleCalendarIdentifier, calIdsToAdd[i].macCalId);
        tempLocaleName = CFLocaleCreateLocaleIdentifierFromComponents(kCFAllocatorDefault, cfMutableDictionary);
        if (tempLocaleName == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }

        cfLocale = CFLocaleCreate(kCFAllocatorDefault, tempLocaleName);
        CFRelease(tempLocaleName);
        if (cfLocale == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }

        calid = CFLocaleGetCALID(cfLocale);

        _ASSERTE(calid == calIdsToAdd[i].pcCalId);
        fRetval = EnumCalendarInfoExEx_Helper(
            lpCalInfoEnumProc, cfLocale, calIdsToAdd[i].pcCalId, lpReserved, CalType, lParam);

        CFRelease(cfLocale);
    }

EXIT:
    if (cfStringLocaleName != NULL)
    {
        CFRelease(cfStringLocaleName);
    }
    if (cfMutableDictionary != NULL)
    {
        CFRelease(cfMutableDictionary);
    }
    PERF_EXIT(AddCalendarsToLocale);
    return fRetval;
}


/*++
Function:
  EnumCalendarInfoExEx

See MSDN doc.
--*/
BOOL
PALAPI
EnumCalendarInfoExEx(
    IN CALINFO_ENUMPROCEXEXW lpCalInfoEnumProc,
    IN LPCWSTR           lpLocale,
    IN CALID             Calendar,
    IN LPCWSTR           lpReserved,
    IN CALTYPE           CalType,
    IN LPARAM            lParam)
{
    BOOL fRetval = FALSE;
    CFLocaleRef cfLocale;
    CALID calid;
    int numCals;

    PERF_ENTRY(EnumCalendarInfoExEx);
    ENTRY("EnumCalendarInfoExEx(lpCalInfoEnumProc=%p, Locale=%S, Calendar=%#x, CalType=%#x, lParam=%p)\n",
          lpCalInfoEnumProc, lpLocale, Calendar, CalType,lParam);

    if ( (Calendar != ENUM_ALL_CALENDARS) && (Calendar < CAL_GREGORIAN || Calendar > CAL_JULIAN) )
    {
        ASSERT("Calendar(%#x) is invalid", Calendar);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    cfLocale = CFLocaleCreateFromLocaleName(lpLocale);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n",lpLocale);
        }
        goto EXIT;
    }


    // If CALID is something other than ENUM_ALL_CALENDARS, use that; otherwise get from the locale
    calid = (Calendar != ENUM_ALL_CALENDARS) ? Calendar : CFLocaleGetCALID(cfLocale);
    fRetval = EnumCalendarInfoExEx_Helper(
        lpCalInfoEnumProc, cfLocale, calid, lpReserved, CalType, lParam);

    CFRelease(cfLocale);
    cfLocale = NULL;
    
    if (Calendar == ENUM_ALL_CALENDARS)
    {
        numCals = sizeof(supportedCalendars)/sizeof(CalIdPair);
        fRetval = AddCalendarsToLocale(lpLocale, supportedCalendars, numCals, calid,
                                  lpCalInfoEnumProc, lpReserved, CalType, lParam);
    }
    else 
    {
        // Every locale must support the Gregorian calendar.  This locale's
        // calendar is not set to Gregorian, so enumerate it explicitly.
        if (fRetval && calid != CAL_GREGORIAN)
        {

             numCals = sizeof(requiredCalendars)/sizeof(CalIdPair);
             fRetval = AddCalendarsToLocale(lpLocale, requiredCalendars, numCals, calid, 
                 lpCalInfoEnumProc, lpReserved, CalType, lParam);
        }
    }

EXIT:
    LOGEXIT("EnumCalendarInfoExEx returns bool %d\n", fRetval);
    PERF_EXIT(EnumCalendarInfoExEx);
    return fRetval;
}

/*++
Function:
  LCMapStringHelper
--*/
int
PALAPI
LCMapStringHelper(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest,
    LPNLSVERSIONINFO lpVersionInformation, 
    LPVOID lpReserved, 
    LPARAM lParam,
    IN BOOL returnSrcIfGrows )
{
    CFLocaleRef cfLocale = NULL;
    INT nRetval = 0;
    CFMutableStringRef cfMutableString = NULL;

    PERF_ENTRY(LCMapStringHelper);
    ENTRY("LCMapStringHelper(Locale=%S, dwMapFlags=%#x, lpSrcStr=%p(%S), cchSrc=%d, lpDestStr=%p, cchDest=%d, returnSrcIfGrows=%d)\n",
          lpLocaleName? lpLocaleName : W16_NULLSTRING , dwMapFlags, lpSrcStr, lpSrcStr, cchSrc, lpDestStr, cchDest, returnSrcIfGrows);

    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n",lpLocaleName);
        }
        goto EXIT;
    }

    if (dwMapFlags != 0 && 
        dwMapFlags != LCMAP_LOWERCASE && 
        dwMapFlags != LCMAP_UPPERCASE &&
        dwMapFlags != (LCMAP_LOWERCASE | LCMAP_LINGUISTIC_CASING) && 
        dwMapFlags != (LCMAP_UPPERCASE | LCMAP_LINGUISTIC_CASING)
       )
    {
        ASSERT("dwMapFlags(%#x) invalid\n", dwMapFlags);
        SetLastError(ERROR_INVALID_FLAGS);
        goto EXIT;
    }

    if (lpSrcStr == NULL || (cchDest != 0 && lpDestStr == NULL))
    {
        ASSERT("Source and destination strings must be non-null\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    if (cchSrc < 0)
    {
        cchSrc = PAL_wcslen(lpSrcStr) + 1; // in this case, include NUL
    }

    cfMutableString = CFStringCreateMutable(kCFAllocatorDefault, 0);
    if (cfMutableString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    CFStringAppendCharacters(cfMutableString, ToUniChar(lpSrcStr), cchSrc);

    switch (dwMapFlags)
    {
        case LCMAP_LOWERCASE:
            CFStringLowercase(cfMutableString, NULL);
            break;
        case LCMAP_LOWERCASE | LCMAP_LINGUISTIC_CASING:
            CFStringLowercase(cfMutableString, cfLocale);
            break;
        case LCMAP_UPPERCASE:
            // Note: this function does not always map single characters to single characters
            CFStringUppercase(cfMutableString, NULL);
            break;
        case LCMAP_UPPERCASE | LCMAP_LINGUISTIC_CASING:
            // Note: this function does not always map single characters to single characters
            CFStringUppercase(cfMutableString, cfLocale);
            break;
        default:
            // We should never get here because we validated above.
            ASSERT("dwMapFlags(%#x) invalid.\n", dwMapFlags);
            SetLastError(ERROR_INVALID_FLAGS);
            goto EXIT;
    }

    nRetval = CFStringGetLength(cfMutableString);
    if (cchDest != 0)
    {
        if (nRetval > cchDest)
        {
            if (returnSrcIfGrows)
            {
                nRetval = cchSrc;
                memcpy(lpDestStr, lpSrcStr, nRetval * sizeof (WCHAR));                
            }
            else 
            {
                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                nRetval = 0;
            }
        }
        else 
        {
            CFStringGetCharacters(cfMutableString, CFRangeMake(0, nRetval), (UniChar*)lpDestStr);
        }
    }


EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    if (cfMutableString != NULL)
    {
        CFRelease(cfMutableString);
    }
    if (nRetval != 0 && cchDest != 0)
    {
        LOGEXIT("LCMapStringHelper returns int %d (%S)\n", nRetval, lpDestStr);
    }
    else
    {
        LOGEXIT("LCMapStringHelper returns int %d\n", nRetval);
    }
    PERF_EXIT(LCMapStringHelper);
    return nRetval;

}

/*++
Function:
  LCMapStringEx

See MSDN doc.
--*/
int
PALAPI
LCMapStringEx(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest,
    LPNLSVERSIONINFO lpVersionInformation,
    LPVOID lpReserved,
    LPARAM lParam )
{
    return LCMapStringHelper(lpLocaleName, dwMapFlags, lpSrcStr, cchSrc, lpDestStr, cchDest, lpVersionInformation, lpReserved, lParam, FALSE);
}

/*++
Function:
  PAL_LCMapCharW

Special case for chars on mac: if char expands through decomposition, then return original char
--*/
int
PALAPI
PAL_LCMapCharW(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN WCHAR   srcChar,
    OUT WCHAR  *destChar,
    LPNLSVERSIONINFO lpVersionInformation,
    LPVOID lpReserved,
    LPARAM lParam )
{
    return LCMapStringHelper(lpLocaleName, dwMapFlags, &srcChar, 1, destChar, 1, lpVersionInformation, lpReserved, lParam, TRUE);
}

/*++
Function:
  PAL_NormalizeStringExW
--*/
int
PALAPI
PAL_NormalizeStringExW(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest)
{
    CFLocaleRef cfLocale = NULL;
    INT nRetval = 0, length;
    CFMutableStringRef cfMutableString = NULL;

    PERF_ENTRY(PAL_NormalizeStringExW);
    ENTRY("PAL_NormalizeStringExW(Locale=%S, dwMapFlags=%#x, lpSrcStr=%p(%S), cchSrc=%d, lpDestStr=%p, cchDest=%d)\n",
          lpLocaleName? lpLocaleName : W16_NULLSTRING, dwMapFlags, lpSrcStr, lpSrcStr, cchSrc, lpDestStr, cchDest);

    cfLocale = CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        if (GetLastError() == ERROR_INVALID_PARAMETER)
        {
            ASSERT("Locale(%S) parameter is invalid\n",lpLocaleName);
        }
        goto EXIT;
    }

    if (dwMapFlags != 0 && dwMapFlags != NORM_IGNORECASE)
    {
        ASSERT("dwMapFlags(%#x) invalid\n", dwMapFlags);
        SetLastError(ERROR_INVALID_FLAGS);
        goto EXIT;
    }

    if (lpSrcStr == NULL || (cchDest != 0 && lpDestStr == NULL))
    {
        ASSERT("Source and destination strings must be non-null\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    if (cchSrc < 0)
    {
        cchSrc = PAL_wcslen(lpSrcStr);
    }

    cfMutableString = CFStringCreateMutable(kCFAllocatorDefault, 0);
    if (cfMutableString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    CFStringAppendCharacters(cfMutableString, ToUniChar(lpSrcStr), cchSrc);

    // TODO: Additionally, we should normalize according to the locale.
    // Unfortunately, Core Foundation has no such function in Mac OS X 10.4.
    CFStringNormalize(cfMutableString, kCFStringNormalizationFormC);

    if (dwMapFlags == NORM_IGNORECASE)
    {
        CFStringLowercase(cfMutableString, cfLocale);
    }

    length = CFStringGetLength(cfMutableString);
    if (length > 0 && CFStringGetCharacterAtIndex(cfMutableString, length - 1) == L'\0')
    {
        nRetval = length;
    }
    else
    {
        nRetval = length + 1; // always NUL-terminate result
    }

    if (cchDest != 0)
    {
        if (nRetval > cchDest)
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            nRetval = 0;
            goto EXIT;
        }
        CFStringGetCharacters(cfMutableString, CFRangeMake(0, nRetval), (UniChar*)lpDestStr);
        if (nRetval != length)
        {
            lpDestStr[length] = L'\0';
        }
    }

EXIT:
    if (cfMutableString != NULL)
    {
        CFRelease(cfMutableString);
        cfMutableString = NULL;
    }
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
        cfLocale = NULL;
    }

    if (nRetval != 0 && cchDest != 0)
    {
        LOGEXIT("PAL_NormalizeStringExW returns int %d (%S)\n", nRetval, lpDestStr);
    }
    else
    {
        LOGEXIT("PAL_NormalizeStringExW returns int %d\n", nRetval);
    }
    PERF_EXIT(PAL_NormalizeStringExW);
    return nRetval;
}


PALIMPORT
int
PALAPI
PAL_ParseDateW(
    IN LPCWSTR   lpLocaleName,
    IN LPCWSTR   lpFormat,
    IN LPCWSTR   lpString,
    OUT LPSYSTEMTIME lpTime)
{

    PERF_ENTRY(PAL_ParseDateW);
    ENTRY("PAL_ParseDateW (lpLocaleName=%S, lpFormat=%S, lpString=%S, lpTime=%p)\n",
          lpLocaleName ? lpLocaleName : W16_NULLSTRING, 
          lpFormat ? lpFormat : W16_NULLSTRING, 
          lpString ? lpString : W16_NULLSTRING, 
          lpTime);

    int RetVal=0;

    CFLocaleRef cfLocale=NULL;
    CFDateFormatterRef cfFormatter=NULL;
    CFStringRef cfFormat=NULL;
    CFStringRef cfString=NULL;    
    CFStringRef cfFormatString=NULL;
    CFRange cfRange;
    CFTimeZoneRef cfTimeZone=NULL;

    cfLocale=CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfFormat = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpFormat), PAL_wcslen(lpFormat));
    if (cfFormat == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfString = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpString), PAL_wcslen(lpString));
    if (cfString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }


    
    cfFormatter=CFDateFormatterCreate(NULL,cfLocale,kCFDateFormatterNoStyle,kCFDateFormatterNoStyle);
    if (cfFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }



    CFDateFormatterSetFormat(cfFormatter,cfFormat);

    // Does format contain 'Z'
    cfFormatString = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpFormat), PAL_wcslen(lpFormat));
    if (cfFormatString == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }
    cfRange = CFStringFind(cfFormatString, CFSTR("'Z'"), 0);
    if (cfRange.location != kCFNotFound)
    {
        cfTimeZone = CFTimeZoneCreateWithName(NULL, CFSTR("UTC"), true);
        if (cfTimeZone == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;            
        }
        CFDateFormatterSetProperty(cfFormatter, kCFDateFormatterTimeZone, cfTimeZone);
    }

    CFAbsoluteTime cfTime;

    if (CFDateFormatterGetAbsoluteTimeFromString(cfFormatter,cfString,NULL,&cfTime))
    {
        CFGregorianDate cfDate = CFAbsoluteTimeGetGregorianDate (cfTime, NULL);
        lpTime->wYear = cfDate.year;
        lpTime->wMonth = cfDate.month;
        lpTime->wDay = cfDate.day;
        lpTime->wHour = cfDate.hour;
        lpTime->wMinute = cfDate.minute;
        lpTime->wSecond = (int)cfDate.second;
        lpTime->wMilliseconds = (int)((cfDate.second - lpTime->wSecond)*1000);
        RetVal=1;
    }
    else
    {
         SetLastError(ERROR_INVALID_PARAMETER);
    }
        

    
EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    if (cfFormat != NULL)
    {
        CFRelease(cfFormat);
    }
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
    if (cfFormatter != NULL)
    {
        CFRelease(cfFormatter);
    }
    if (cfTimeZone != NULL)
    {
        CFRelease(cfTimeZone);        
    }
    if (cfFormatString != NULL)
    {
        CFRelease(cfFormatString);        
    }
    
    LOGEXIT("PAL_ParseDateW returns %d \n", RetVal);
    PERF_EXIT(PAL_ParseDateW);
    return RetVal;

}

static CFStringRef* s_GregorianStartDateFromBundle = NULL;
static BOOL s_bFetchedFundleForGregorianStartDate = FALSE;
static CFDateRef s_cfGregorianStartDate = NULL;

PALIMPORT
int
PALAPI
PAL_FormatDateW(
    IN LPCWSTR   lpLocaleName,
    IN LPCWSTR   lpFormat,
    IN BOOL fUseUTC,
    IN BOOL fUseCustomTz,
    IN int tzOffsetSeconds,
    IN LPSYSTEMTIME lpTime,
    OUT LPWSTR lpDestStr,
    IN int     cchDest)
{
    CFLocaleRef cfLocale=NULL;
    CFDateFormatterRef cfFormatter=NULL;
    CFStringRef cfFormat=NULL;
    CFStringRef cfString=NULL;    
    CFTimeZoneRef cfTimeZone=NULL;    

    PERF_ENTRY(PAL_FormatDateW);
    ENTRY("PAL_FormatDateW (lpLocaleName=%S, lpFormat=%S, fUseUTC, fUseCustomTz, tzOffsetSeconds, year=%d, month=%d, day=%d, hour=%d, minute=%d, second=%d, millisecond=%d, lpDestStr=%p, cchDest=%d)\n",
          lpLocaleName ? lpLocaleName : W16_NULLSTRING, 
          lpFormat ? lpFormat : W16_NULLSTRING, fUseUTC,
          lpTime->wYear, lpTime->wMonth, lpTime->wDay, lpTime->wHour, lpTime->wMinute, lpTime->wSecond, lpTime->wMilliseconds,
          lpDestStr,
          cchDest);

    int RetVal=0;

    cfLocale=CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    cfFormat = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(lpFormat), PAL_wcslen(lpFormat));
    if (cfFormat == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

   
    cfFormatter=CFDateFormatterCreate(NULL,cfLocale,kCFDateFormatterNoStyle,kCFDateFormatterNoStyle);
    if (cfFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    /* The BCL Gregorian calendar is extended back in time to 1/1/0001, where as Apple provides
     * historical information on when the Gregorian calendar was adopted for a locale and uses
     * that information for formating a date.  This can lead to dates being printed wrong.  On 10.5
     * we can set the Gregorian start date to 1/1/0001 which will cause us to format the date
     * correctly.
     */

    if (!s_bFetchedFundleForGregorianStartDate)
    {
	CFStringRef* s = NULL;

        if(!FetchCFTypeRefValueFromBundle(CFSTR("com.apple.CoreFoundation"), CFSTR("kCFDateFormatterGregorianStartDate"), (CFTypeRef**) &s)) 
        {
	   goto EXIT;
	} 

	if (s != NULL) 
	{

	    CFRetain(*s);

	    if (InterlockedCompareExchangePointer(&s_GregorianStartDateFromBundle, s, NULL) != NULL )
	    {
		/* somebody beat us to it */
		CFRelease(*s);
	    }
	}

    s_bFetchedFundleForGregorianStartDate = TRUE;
    } 

    if (s_GregorianStartDateFromBundle != NULL) 
    {
        if(s_cfGregorianStartDate == NULL)
	{

            CFDateRef cfGregorianStartDate=NULL;

            cfGregorianStartDate = CFDateCreate(kCFAllocatorDefault, (CFAbsoluteTime) -63113904000LL /* Jan 1, 0001 */ );

            if (cfGregorianStartDate == NULL) 
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto EXIT;            
            }

	    if (InterlockedCompareExchangePointer(&s_cfGregorianStartDate, cfGregorianStartDate, NULL) != NULL )
	    {
		/* somebody beat us to it */
		CFRelease(cfGregorianStartDate);
	    }           
	} 
        
        CFDateFormatterSetProperty(cfFormatter, *s_GregorianStartDateFromBundle, s_cfGregorianStartDate); 
    }

    if (fUseUTC)
    {
        cfTimeZone = CFTimeZoneCreateWithName(NULL, CFSTR("UTC"), true);
        if (cfTimeZone == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;            
        }
        CFDateFormatterSetProperty(cfFormatter, kCFDateFormatterTimeZone, cfTimeZone);
    }
    else if (fUseCustomTz /* System.DateTimeOffset case */)
    {
        //
        // It is worth noting that on Mac OS 10.4, the "Z" formatter produces strange
        // results for the minutes-field in the time zone offset.  This bad "Z" behavior
        // does not exist on Mac OS 10.5:
        //
        // Offset     "z"           "Z"
        // -13:00     GMT-13:00     -1300
        // -12:55     GMT-12:55     -1345
        // -12:50     GMT-12:50     -1330
        // -12:45     GMT-12:45     -1320
        // -12:40     GMT-12:40     -1305
        // -12:35     GMT-12:35     -1290
        // -12:30     GMT-12:30     -1280
        // -12:25     GMT-12:25     -1265
        // -12:20     GMT-12:20     -1250
        // -12:15     GMT-12:15     -1240
        // -12:10     GMT-12:10     -1225
        // -12:05     GMT-12:05     -1210
        // -12:00     GMT-12:00     -1200
        //
        cfTimeZone = CFTimeZoneCreateWithTimeIntervalFromGMT(NULL, (CFTimeInterval)(tzOffsetSeconds));
        if (cfTimeZone == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
        CFDateFormatterSetProperty(cfFormatter, kCFDateFormatterTimeZone, cfTimeZone);
    }


    CFDateFormatterSetFormat(cfFormatter,cfFormat);

    CFGregorianDate gdate;
    gdate.year = lpTime->wYear;
    gdate.month = lpTime->wMonth;
    gdate.day = lpTime->wDay;
    gdate.hour = lpTime->wHour;
    gdate.minute = lpTime->wMinute;
    gdate.second = lpTime->wSecond + ((double)lpTime->wMilliseconds) / 1000.;
    CFAbsoluteTime cfTime;
    cfTime = CFGregorianDateGetAbsoluteTime (gdate, NULL);

    // Currently there is an issue in CFDateFormatterCreateStringWithAbsoluteTime, which is an API of CoreFoundation
    // framework. It will result in a segment fault when the parameters meet following conditions:
    // 1. Calendar is set to Islamic
    // 2. The date to be formatted is before July  18th, 622 AD
    // 3. The format specifer contain "MMM" or "MMMM" which suggests the full name of month.
    if (cfTime <= -43499894400LL /* CFAbsoluteTime of July 19th, 622 AD */&&
         0 == CFStringCompare(
                              (CFStringRef)CFLocaleGetValue(cfLocale, kCFLocaleCalendarIdentifier),
                              CFSTR("islamic"),
                              0) &&
         (CFStringFind(cfFormat, CFSTR("MMM"), 0)).location != kCFNotFound)
    {
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }
   

    cfString=CFDateFormatterCreateStringWithAbsoluteTime(NULL,cfFormatter,cfTime);
    if (cfFormatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    CFIndex strLen;
    strLen = CFStringGetLength(cfString);
    if (strLen == 0)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    
    if (strLen >= cchDest)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto EXIT;
    }

    CFStringGetCharacters(cfString,CFRangeMake(0, strLen),(UniChar*)lpDestStr);
    lpDestStr[strLen] = L'\0';
    RetVal = strLen;
    
EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }
    if (cfFormat != NULL)
    {
        CFRelease(cfFormat);
    }
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
    if (cfFormatter != NULL)
    {
        CFRelease(cfFormatter);
    }
    if (cfTimeZone != NULL)
    {
        CFRelease(cfTimeZone);
    }

    LOGEXIT("PAL_FormatDateW returns %d (%S)\n", RetVal,RetVal?lpDestStr:W16_NULLSTRING);
    PERF_EXIT(PAL_FormatDateW);
    return RetVal;

};

PALIMPORT
int
PALAPI
PAL_GetCalendar(
    IN LPCWSTR   lpLocaleName,
    OUT CALID*   pCalendar)
{
    CFLocaleRef cfLocale=NULL;
    *pCalendar = 0;
    
    PERF_ENTRY(PAL_GetCalendar);
    ENTRY("PAL_GetCalendar (lpLocaleName=%S, pCalendar=%p)\n",
          lpLocaleName ? lpLocaleName : W16_NULLSTRING,
          pCalendar);

    int RetVal=0;
    cfLocale=CFLocaleCreateFromLocaleName(lpLocaleName);
    if (cfLocale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    *pCalendar = CFLocaleGetCALID(cfLocale);
    RetVal = 1;
    
EXIT:
    if (cfLocale != NULL)
    {
        CFRelease(cfLocale);
    }

    LOGEXIT("PAL_GetCalendar returns %d (%d)\n", RetVal,*pCalendar);
    PERF_EXIT(PAL_GetCalendar);
    return RetVal;

};

PALIMPORT
void
PALAPI
PAL_ReleaseNumber(PALNUMBER number)
{
    PERF_ENTRY(PAL_ReleaseNumber);
    ENTRY("PAL_ReleaseNumber (number=%p)\n",number);

    if (number != NULL)
        CFRelease((CFNumberRef)number);

    LOGEXIT("PAL_ReleaseNumber returns \n" );
    PERF_EXIT(PAL_ReleaseNumber);
}

PALIMPORT PALNUMBER PALAPI PAL_IntToNumber(int number)
{
    // returns NULL on OOM
    PERF_ENTRY(PAL_IntToNumber);
    ENTRY("PAL_IntToNumber (number=%i)\n",number);
    
    PALNUMBER retVal = (PALNUMBER)CFNumberCreate(NULL,kCFNumberIntType,&number);

    LOGEXIT("PAL_IntToNumber returns %p\n" ,number);
    PERF_EXIT(PAL_IntToNumber);

    return retVal;    
}

PALIMPORT PALNUMBER PALAPI PAL_Int64ToNumber(INT64 number)
{
    // returns NULL on OOM
    PERF_ENTRY(PAL_Int64ToNumber);
    ENTRY("PAL_Int64ToNumber (number=%i)\n",number);
    
    PALNUMBER retVal = (PALNUMBER)CFNumberCreate(NULL,kCFNumberSInt64Type,&number);
    
    LOGEXIT("PAL_Int64ToNumber returns %p\n" ,number);
    PERF_EXIT(PAL_Int64ToNumber);
    return retVal;
}

PALIMPORT PALNUMBER PALAPI PAL_UIntToNumber(unsigned int number)
{
    // returns NULL on OOM
    PERF_ENTRY(PAL_UIntToNumber);
    ENTRY("PAL_UIntToNumber (number=%u)\n",number);
    
    PALNUMBER retVal = PAL_Int64ToNumber((INT64)number) ;

    LOGEXIT("PAL_UIntToNumber returns %p\n" ,number);
    PERF_EXIT(PAL_UIntToNumber);
    return retVal;
    
}

PALIMPORT PALNUMBER PALAPI PAL_UInt64ToNumber(UINT64 number)
{
    // returns NULL on OOM
    PERF_ENTRY(PAL_UInt64ToNumber);
    ENTRY("PAL_UInt64ToNumber (number=%u)\n",number);
    
    PALNUMBER retVal; 
    
    if (number >> 63)
        retVal = PAL_DoubleToNumber((double)number);
    else
        retVal = PAL_Int64ToNumber((INT64)number);

    LOGEXIT("PAL_UInt64ToNumber returns %p\n" ,number);
    PERF_EXIT(PAL_UInt64ToNumber);
    return retVal;    
}

PALIMPORT PALNUMBER PALAPI PAL_DoubleToNumber(double number)
{
    // returns NULL on OOM
    PERF_ENTRY(PAL_DoubleToNumber);
    ENTRY("PAL_DoubleToNumber (number=%f)\n",number);
    
    PALNUMBER retVal =(PALNUMBER)CFNumberCreate(NULL,kCFNumberDoubleType,&number);

    LOGEXIT("PAL_DoubleToNumber returns %p\n" ,number);
    PERF_EXIT(PAL_DoubleToNumber);
    return retVal;    
    
}

typedef 
struct  
{
    LPCSTR sPrefix;
    LPCSTR sSuffix;        
} FORMATINFO;

#define CHECKFORMATINDEX(array, index)                             \
        if (index >= sizeof(array)/sizeof(array[0]))                      \
        {                                                                                       \
            SetLastError(ERROR_INVALID_PARAMETER);                  \
            goto EXIT;                                                                     \
        };




FORMATINFO s_CurrencyPositive[]=
{
    {"$",NULL},
    {NULL,"$"},
    {"$ ",NULL},
    {NULL," $"}
};

FORMATINFO s_CurrencyNegative[]=
{
    {"($",")"},
    {"-$",NULL},
    {"$-",NULL},
    {"$","-"},
    {"(","$)"},
    {"-","$"},
    {NULL,"-$"},
    {NULL,"$-"},
    {"-"," $"},
    {"-$ ",NULL},
    {NULL," $-"},
    {"$ ","-"},
    {"$ -",NULL},
    {NULL,"- $"},
    {"($ ",")"},
    {"("," $)"}
};

FORMATINFO  s_PercentPositive[]=
{
    {NULL," $"},
    {NULL,"$"},
    {"$",NULL},
    {"$ ",NULL}
};

FORMATINFO  s_PercentNegative[]=
{
    {"-"," $"},
    {"-","$"},
    {"-$",NULL},
    {"$-",NULL},
    {"$","-"},
    {NULL,"-$"},
    {NULL,"$-"},
    {"-$ ",NULL},
    {NULL," $-"},
    {"$ ","-"},
    {"$ -",NULL},
    {NULL,"- $"}
};

FORMATINFO s_NumberNegative[]=
{
    {"(",")"},
    {"-",NULL},
    {"- ",NULL},
    {NULL,"-"},
    {NULL," -"}
};

static BOOL CFNumberFormatterSetPropertyString(CFNumberFormatterRef formatter, CFStringRef cfProperty, LPCWSTR sPropertyValue)
{
    // NULL means default
    if (sPropertyValue == NULL)
        return TRUE;
    CFStringRef cfPropertyValue = CFStringCreateWithCharacters(kCFAllocatorDefault, ToUniChar(sPropertyValue), PAL_wcslen(sPropertyValue));
    if (cfPropertyValue == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return FALSE;
    }
    CFNumberFormatterSetProperty(formatter,cfProperty,cfPropertyValue);
    CFRelease(cfPropertyValue);
    return TRUE;
}

static BOOL CFNumberFormatterAdjust(CFNumberFormatterRef formatter, LPCSTR format,CFStringRef cfProperty, LPCWSTR sMinus, LPCWSTR sDollar)
{
    if (format == NULL)
    {
        CFNumberFormatterSetProperty(formatter,cfProperty,CFSTR(""));
        return TRUE;
    }

    // apply overrides to format    
    CFMutableStringRef sFormat=CFStringCreateMutable(kCFAllocatorDefault,0);
    if (sFormat == NULL)
    {
        SetLastError(ERROR_OUTOFMEMORY);
        return FALSE;
    }
    SIZE_T i;
    SIZE_T iStrLen=strlen(format);

    for ( i=0;i<iStrLen;i++)
    {
        switch(format[i])
        {
            case '(': CFStringAppend(sFormat,CFSTR("(")); break;
            case ')': CFStringAppend(sFormat,CFSTR(")")); break;
            case ' ': CFStringAppend(sFormat,CFSTR(" ")); break;            
            case '-': 
                if(sMinus)
                    CFStringAppendCharacters(sFormat,ToUniChar(sMinus),PAL_wcslen(sMinus)); 
                break;
            case '$': 
                if(sDollar)
                    CFStringAppendCharacters(sFormat,ToUniChar(sDollar),PAL_wcslen(sDollar)); 
                break;
                
            default:
                ASSERT("Unknown character in prefix/suffix format\n");
                CFRelease(sFormat);
                return FALSE;
        }
                
    }

    CFNumberFormatterSetProperty(formatter,cfProperty,sFormat);
    CFRelease(sFormat);
    return TRUE;
}

inline static BOOL CFNumberAdjustFormat(CFNumberFormatterRef formatter, FORMATINFO* pFormat, BOOL bPositive, LPCWSTR sMinus, LPCWSTR sDollar)
{
    CFStringRef cfPrefix=bPositive?kCFNumberFormatterPositivePrefix:kCFNumberFormatterNegativePrefix; 
    CFStringRef cfSuffix=bPositive?kCFNumberFormatterPositiveSuffix:kCFNumberFormatterNegativeSuffix;
    return CFNumberFormatterAdjust(formatter,pFormat->sPrefix,cfPrefix,sMinus,sDollar) &&
               CFNumberFormatterAdjust(formatter,pFormat->sSuffix,cfSuffix,sMinus,sDollar);
}


static BOOL CFNumberFormatterSetPropertyInt(CFNumberFormatterRef formatter, CFStringRef cfProperty, int iPropertyValue)
{
    CFNumberRef cfPropertyValue = CFNumberCreate(kCFAllocatorDefault, kCFNumberIntType, &iPropertyValue);
    if (cfPropertyValue == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return FALSE;
    }
    CFNumberFormatterSetProperty(formatter,cfProperty,cfPropertyValue);
    CFRelease(cfPropertyValue);
    return TRUE;
}

static int CFNumberFormatHelper(CFNumberFormatterRef formatter, PALNUMBER number, LPWSTR pBuffer, SIZE_T cchBuffer)
{
    CFStringRef cfFormattedNumber=CFNumberFormatterCreateStringWithNumber(kCFAllocatorDefault,formatter,(CFNumberRef)number);
    if (cfFormattedNumber == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return -1;
    }
    
    int iStrLen=CFStringGetLength(cfFormattedNumber);

    if (pBuffer)
    {
        if (static_cast<int>(cchBuffer) <= (iStrLen))
        {
            CFRelease(cfFormattedNumber);
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return -1;
        }
        CFStringGetCharacters(cfFormattedNumber,CFRangeMake(0,iStrLen),(UniChar*)pBuffer);
        pBuffer[iStrLen]=L'\0';
    }

    CFRelease(cfFormattedNumber);

    return iStrLen;    
}

static CFMutableDictionaryRef kCFNumberFormatterScientificStyleCache;
static pthread_mutex_t kCFNumberFormatterScientificStyleLock = PTHREAD_MUTEX_INITIALIZER;

PALIMPORT int  PALAPI PAL_FormatScientific(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits,
                                                                      LPCWSTR sExponent, LPCWSTR sNumberDecimal, LPCWSTR sPositive, LPCWSTR sNegative, LPCWSTR sZero)
{
    PERF_ENTRY(PAL_FormatScientific);
    ENTRY("PAL_FormatScientific (sLocale==\"%S\", pBuffer=%p, cchBuffer =%i, number=%p, nMinDigits=%i, nMaxDigits=%i"
               "sExponent=\"%S\", sNumberDecimal=\"%S\", sPositive=\"%S\", sNegative=\"%S\", sZero=\"%S\")\n",
               sLocale?sLocale:W16_NULLSTRING, pBuffer, cchBuffer, number, nMinDigits, nMaxDigits,
               sExponent?sExponent:W16_NULLSTRING,sNumberDecimal?sNumberDecimal:W16_NULLSTRING,
               sPositive?sPositive:W16_NULLSTRING,sNegative?sNegative:W16_NULLSTRING,sZero?sZero:W16_NULLSTRING);
    
    CFLocaleRef locale=NULL;
    CFNumberFormatterRef formatter = NULL;
    int iRet=-1;

    pthread_mutex_lock(&kCFNumberFormatterScientificStyleLock);

    if(kCFNumberFormatterScientificStyleCache == NULL) 
    {
        kCFNumberFormatterScientificStyleCache = CFDictionaryCreateMutable(kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
        if(kCFNumberFormatterScientificStyleCache == NULL) 
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
    }

    locale=CFLocaleCreateFromLocaleName(sLocale);
    if(locale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if(CFDictionaryContainsKey(kCFNumberFormatterScientificStyleCache, locale)) 
    {
        formatter = (CFNumberFormatterRef) CFDictionaryGetValue(kCFNumberFormatterScientificStyleCache, locale);
        CFRetain(formatter);
    } 
    else 
    {
        formatter = CFNumberFormatterCreate(kCFAllocatorDefault,locale,kCFNumberFormatterScientificStyle);
	    
        if(formatter == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }

        CFDictionaryAddValue(kCFNumberFormatterScientificStyleCache, locale, formatter);
    }

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterExponentSymbol,sExponent))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterDecimalSeparator,sNumberDecimal))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterPlusSign,sPositive))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterMinusSign,sNegative))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterZeroSymbol,sZero))
        goto EXIT;


    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMinFractionDigits,nMinDigits))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxFractionDigits,nMaxDigits))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMinIntegerDigits, 1))
        goto EXIT;


    iRet=CFNumberFormatHelper(formatter, number, pBuffer,cchBuffer);
    
EXIT:
    pthread_mutex_unlock(&kCFNumberFormatterScientificStyleLock);

    if (locale)
        CFRelease(locale);

    if (formatter)
        CFRelease(formatter);

    LOGEXIT("PAL_FormatScientific returns %i\n" ,iRet);
    PERF_EXIT(PAL_FormatScientific);

    return iRet;
    
}

PALIMPORT int  PALAPI  PAL_FormatCurrency(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits, int iNegativeFormat, int iPositiveFormat,
                      int iPrimaryGroup, int iSecondaryGroup, LPCWSTR sCurrencyDecimal, LPCWSTR sCurrencyGroup, LPCWSTR sNegative, LPCWSTR sCurrency, LPCWSTR sZero)
{

    PERF_ENTRY(PAL_FormatCurrency);
    ENTRY("PAL_FormatCurrency (sLocale==\"%S\", pBuffer=%p, cchBuffer =%i, number=%p, nMinDigits=%i, nMaxDigits=%i"
               "iNegativeFormat=%i, iPositiveFormat=%i, iPrimaryGroup=%i, iSecondaryGroup=%i, "
               "sCurrencyDecimal=\"%S\", sCurrencyGroup=\"%S\", sNegative=\"%S\", sCurrency=\"%S\", sZero=\"%S\")\n",
               sLocale?sLocale:W16_NULLSTRING, pBuffer, cchBuffer, number, nMinDigits,nMaxDigits,
               iNegativeFormat, iPositiveFormat, iPrimaryGroup, iSecondaryGroup,
               sCurrencyDecimal?sCurrencyDecimal:W16_NULLSTRING,sCurrencyGroup?sCurrencyGroup:W16_NULLSTRING,
               sNegative?sNegative:W16_NULLSTRING,sCurrency?sCurrency:W16_NULLSTRING,sZero?sZero:W16_NULLSTRING);
    
    CFLocaleRef locale=NULL;
    CFNumberFormatterRef formatter = NULL;
    int iRet=-1;

    locale=CFLocaleCreateFromLocaleName(sLocale);
    if(locale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    formatter=CFNumberFormatterCreate(kCFAllocatorDefault,locale,kCFNumberFormatterCurrencyStyle);
    if(formatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxIntegerDigits,600)) //LARGE_BUFFER_SIZE in NLS
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterCurrencyDecimalSeparator,sCurrencyDecimal))
        goto EXIT;

    if (!s_bFetchedBundleForCurrencyGroupingSeparator)
    {
        if (!FetchCurrencyGroupingSeparatorFromBundle())
            goto EXIT;   
    }

    if (s_CurrencyGroupingSeparatorFromBundle != NULL) 
    {
        if (!CFNumberFormatterSetPropertyString(formatter, *s_CurrencyGroupingSeparatorFromBundle,sCurrencyGroup))
            goto EXIT;
    }
    else
    {

        if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterGroupingSeparator,sCurrencyGroup))
            goto EXIT;
    }

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterMinusSign,sNegative))
        goto EXIT;
  
    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterCurrencySymbol,sCurrency))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterZeroSymbol,sZero))
        goto EXIT;
    

    if (iPrimaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterGroupingSize,iPrimaryGroup))
        goto EXIT;

    if (iSecondaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterSecondaryGroupingSize,iSecondaryGroup))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMinFractionDigits,nMinDigits))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxFractionDigits,nMaxDigits))
        goto EXIT;

    if (iNegativeFormat >= 0 && iNegativeFormat < static_cast<int>((sizeof(s_CurrencyNegative) / sizeof(FORMATINFO))))
    {
        CHECKFORMATINDEX(s_CurrencyNegative,static_cast<size_t>(iNegativeFormat));
        
        if(!CFNumberAdjustFormat(formatter,s_CurrencyNegative+iNegativeFormat,FALSE,sNegative,sCurrency))
            goto EXIT;;
    }


    if (iPositiveFormat >= 0 && iPositiveFormat < static_cast<int>((sizeof(s_CurrencyPositive) / sizeof(FORMATINFO))))
    {
        CHECKFORMATINDEX(s_CurrencyPositive,static_cast<size_t>(iPositiveFormat) );
        
        if(!CFNumberAdjustFormat(formatter,s_CurrencyPositive+iPositiveFormat,TRUE,NULL,sCurrency))
            goto EXIT;;
    }
    
    iRet=CFNumberFormatHelper(formatter, number, pBuffer,cchBuffer);

EXIT:
    if (locale)
        CFRelease(locale);

    if (formatter)
        CFRelease(formatter);

    LOGEXIT("PAL_FormatCurrency returns %i\n" ,iRet);
    PERF_EXIT(PAL_FormatCurrency);

    return iRet;
    
}

PALIMPORT int PALAPI  PAL_FormatPercent(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits, int iNegativeFormat, int iPositiveFormat, 
                                        int iPrimaryGroup, int iSecondaryGroup, LPCWSTR sPercentDecimal, LPCWSTR sPercentGroup, LPCWSTR sNegative, LPCWSTR sPercent, LPCWSTR sZero)

{

    PERF_ENTRY(PAL_FormatPercent);
    ENTRY("PAL_FormatPercent (sLocale==\"%S\", pBuffer=%p, cchBuffer =%i, number=%p, nMinDigits=%i, nMaxDigits=%i"
               "iNegativeFormat=%i, iPositiveFormat=%i, iPrimaryGroup=%i, iSecondaryGroup=%i, "
               "sPercentDecimal=\"%S\", sPercentGroup=\"%S\", sNegative=\"%S\", sPercent=\"%S\", sZero=\"%S\")\n",
               sLocale?sLocale:W16_NULLSTRING, pBuffer, cchBuffer, number, nMinDigits,nMaxDigits,
               iNegativeFormat, iPositiveFormat, iPrimaryGroup, iSecondaryGroup,
               sPercentDecimal?sPercentDecimal:W16_NULLSTRING,sPercentGroup?sPercentGroup:W16_NULLSTRING,
               sNegative?sNegative:W16_NULLSTRING,sPercent?sPercent:W16_NULLSTRING,sZero?sZero:W16_NULLSTRING);
    
    CFLocaleRef locale=NULL;
    CFNumberFormatterRef formatter = NULL;
    int iRet=-1;

    locale=CFLocaleCreateFromLocaleName(sLocale);
    if(locale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    formatter=CFNumberFormatterCreate(kCFAllocatorDefault,locale,kCFNumberFormatterPercentStyle);
    if(formatter == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxIntegerDigits,600)) //LARGE_BUFFER_SIZE in NLS
        goto EXIT;


    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterDecimalSeparator,sPercentDecimal))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterGroupingSeparator,sPercentGroup))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterMinusSign,sNegative))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterPercentSymbol,sPercent))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterZeroSymbol,sZero))
        goto EXIT;


    if (iPrimaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterGroupingSize,iPrimaryGroup))
        goto EXIT;

    if (iSecondaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterSecondaryGroupingSize,iSecondaryGroup))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMinFractionDigits,nMinDigits))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxFractionDigits,nMaxDigits))
        goto EXIT;

    if (iNegativeFormat >= 0 && iNegativeFormat < static_cast<int>((sizeof(s_CurrencyNegative) / sizeof(FORMATINFO))))
    {
        CHECKFORMATINDEX(s_PercentNegative,static_cast<size_t>(iNegativeFormat));

        
        if(!CFNumberAdjustFormat(formatter, s_PercentNegative+iNegativeFormat,FALSE,sNegative,sPercent))
            goto EXIT;;
    }


    if (iPositiveFormat >= 0 && iPositiveFormat < static_cast<int>((sizeof(s_CurrencyPositive) / sizeof(FORMATINFO))))
    {
        CHECKFORMATINDEX(s_PercentPositive,static_cast<size_t>(iPositiveFormat) );
        
        if(!CFNumberAdjustFormat(formatter, s_PercentPositive+iPositiveFormat,TRUE,NULL,sPercent))
            goto EXIT;;
    }
    
    iRet=CFNumberFormatHelper(formatter, number, pBuffer,cchBuffer);


EXIT:
    if (locale)
        CFRelease(locale);

    if (formatter)
        CFRelease(formatter);

    LOGEXIT("PAL_FormatPercent returns %i\n" ,iRet);
    PERF_EXIT(PAL_FormatPercent);


    return iRet;
    
}

static CFMutableDictionaryRef kCFNumberFormatterDecimalStyleCache;
static pthread_mutex_t kCFNumberFormatterDecimalStyleLock = PTHREAD_MUTEX_INITIALIZER;

PALIMPORT int PALAPI  PAL_FormatDecimal(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits, int iNegativeFormat, 
                                    int iPrimaryGroup, int iSecondaryGroup, LPCWSTR sDecimal, LPCWSTR sGroup, LPCWSTR sNegative, LPCWSTR sZero)
{

    PERF_ENTRY(PAL_FormatDecimal);
    ENTRY("PAL_FormatDecimal (sLocale==\"%S\", pBuffer=%p, cchBuffer =%i, number=%p, nMinDigits=%i, nMaxDigits=%i"
               "iNegativeFormat=%i, iPrimaryGroup=%i, iSecondaryGroup=%i, "
               "sDecimal=\"%S\", sGroup=\"%S\", sNegative=\"%S\", sDigits=\"%S\")\n",
               sLocale?sLocale:W16_NULLSTRING, pBuffer, cchBuffer, number, nMinDigits,nMaxDigits,
               iNegativeFormat, iPrimaryGroup, iSecondaryGroup,
               sDecimal?sDecimal:W16_NULLSTRING,sGroup?sGroup:W16_NULLSTRING,
               sNegative?sNegative:W16_NULLSTRING,sZero?sZero:W16_NULLSTRING);

    
    CFLocaleRef locale=NULL;
    CFNumberFormatterRef formatter = NULL;
    int iRet=-1;

    pthread_mutex_lock(&kCFNumberFormatterDecimalStyleLock);

    if(kCFNumberFormatterDecimalStyleCache == NULL) 
    {
        kCFNumberFormatterDecimalStyleCache = CFDictionaryCreateMutable(kCFAllocatorDefault, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
        if(kCFNumberFormatterDecimalStyleCache == NULL) 
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }
    }

    locale=CFLocaleCreateFromLocaleName(sLocale);
    if(locale == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if(CFDictionaryContainsKey(kCFNumberFormatterDecimalStyleCache, locale)) 
    {
        formatter = (CFNumberFormatterRef) CFDictionaryGetValue(kCFNumberFormatterDecimalStyleCache, locale);
        CFRetain(formatter);
    } 
    else 
    {
        formatter = CFNumberFormatterCreate(kCFAllocatorDefault,locale,kCFNumberFormatterDecimalStyle);
	    
        if(formatter == NULL)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto EXIT;
        }

        CFDictionaryAddValue(kCFNumberFormatterDecimalStyleCache, locale, formatter);
    }

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxIntegerDigits,600)) //LARGE_BUFFER_SIZE in NLS
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterDecimalSeparator,sDecimal))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterGroupingSeparator,sGroup))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterMinusSign,sNegative))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyString(formatter, kCFNumberFormatterZeroSymbol,sZero))
        goto EXIT;


    if (iPrimaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterGroupingSize,iPrimaryGroup))
        goto EXIT;

    if (iSecondaryGroup && !CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterSecondaryGroupingSize,iSecondaryGroup))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMinFractionDigits,nMinDigits))
        goto EXIT;

    if (!CFNumberFormatterSetPropertyInt(formatter, kCFNumberFormatterMaxFractionDigits,nMaxDigits))
        goto EXIT;


    if (iNegativeFormat >= 0 && iNegativeFormat < static_cast<int>((sizeof(s_CurrencyNegative) / sizeof(FORMATINFO))))
    {
        CHECKFORMATINDEX(s_NumberNegative,static_cast<size_t>(iNegativeFormat));
        
        if(!CFNumberAdjustFormat(formatter, s_NumberNegative+iNegativeFormat,FALSE,sNegative,NULL))
            goto EXIT;;
    }
    
    iRet=CFNumberFormatHelper(formatter, number, pBuffer,cchBuffer);


EXIT:
    pthread_mutex_unlock(&kCFNumberFormatterDecimalStyleLock);

    if (locale)
        CFRelease(locale);

    if (formatter)
        CFRelease(formatter);

    LOGEXIT("PAL_FormatDecimal returns %i\n" ,iRet);
    PERF_EXIT(PAL_FormatDecimal);

    return iRet;
    
}

BOOL LocaleInitialize( void )
{
  CFBundleRef b = CFBundleGetBundleWithIdentifier(CFSTR("com.apple.CoreFoundation"));

    if(b == NULL) 
    {
	return FALSE;
    }

    CFRetain(b);

    s_CFStringCompareWithOptionsAndLocale = (CFComparisonResult (*)(CFStringRef, CFStringRef, CFRange, CFOptionFlags, CFLocaleRef)) CFBundleGetFunctionPointerForName(b, CFSTR("CFStringCompareWithOptionsAndLocale"));

    CFRelease(b);

    return TRUE;

}

void LocaleCleanup( void )
{

    pthread_mutex_destroy(&kCFNumberFormatterScientificStyleLock);
    pthread_mutex_destroy(&kCFNumberFormatterDecimalStyleLock);

    if(kCFNumberFormatterScientificStyleCache != NULL)
    {
        CFRelease(kCFNumberFormatterScientificStyleCache);
    }

    if(kCFNumberFormatterDecimalStyleCache != NULL)
    {
        CFRelease(kCFNumberFormatterDecimalStyleCache);
    }
    if(s_cfGregorianStartDate != NULL)
    {
        CFRelease(s_cfGregorianStartDate);
    }
}

#endif // HAVE_COREFOUNDATION && !ENABLE_DOWNLEVEL_FOR_NLS
