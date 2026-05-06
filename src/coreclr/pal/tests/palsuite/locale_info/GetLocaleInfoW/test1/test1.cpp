// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that GetLocaleInfoW gives the correction information for
**          LOCALE_NEUTRAL.
**
**
**==========================================================================*/

#include <palsuite.h>


int Types[] = { LOCALE_SDECIMAL, LOCALE_STHOUSAND, LOCALE_ILZERO,
    LOCALE_SCURRENCY, LOCALE_SMONDECIMALSEP, LOCALE_SMONTHOUSANDSEP };

char *TypeStrings[] = { "LOCALE_SDECIMAL", "LOCALE_STHOUSAND", "LOCALE_ILZERO",
    "LOCALE_SCURRENCY", "LOCALE_SMONDECIMALSEP", "LOCALE_SMONTHOUSANDSEP" };

typedef WCHAR InfoStrings[ARRAY_SIZE(Types)][4];

typedef struct
{
    LCID lcid;
    InfoStrings Strings;
} LocalInfoType;

LocalInfoType Locales[] =
{
    {LOCALE_NEUTRAL,
        {{'.',0}, {',',0}, {'1',0}, {'$',0}, {'.',0}, {',',0}}},
};

int NumLocales = sizeof(Locales) / sizeof(Locales[0]);


PALTEST(locale_info_GetLocaleInfoW_test1_paltest_getlocaleinfow_test1, "locale_info/GetLocaleInfoW/test1/paltest_getlocaleinfow_test1")
{
    WCHAR buffer[256] = { 0 };
    int ret;
    int i,j;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<NumLocales; i++)
    {
        for (j=0; j < ARRAY_SIZE(Types); j++)
        {
            ret = GetLocaleInfoW(Locales[i].lcid, Types[j], buffer, 256);

            if (ret == 0)
            {
                Fail("GetLocaleInfoW returned an unexpected error!\n");
            }


            if (wcscmp(buffer, Locales[i].Strings[j]) != 0)
            {

                Fail("GetLocaleInfoW gave incorrect result for %s, "
                    "locale %#x:\nExpected \"%S\", got \"%S\"!\n", TypeStrings[j],
                    Locales[i].lcid, Locales[i].Strings[j], buffer);

            }

            if (ret != wcslen(Locales[i].Strings[j]) + 1)
            {
                Fail("GetLocaleInfoW returned incorrect value for %s, "
                    "locale %#x:\nExpected %d, got %d!\n", TypeStrings[j],
                    Locales[i].lcid, wcslen(Locales[i].Strings[j])+1, ret);
            }
        }
    }


    PAL_Terminate();

    return PASS;
}

