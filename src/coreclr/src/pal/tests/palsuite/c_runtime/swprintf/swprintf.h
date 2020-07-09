// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  swprintf.h
**
** Purpose: Containts common testing functions for swprintf.h
**
**
**==========================================================================*/

#ifndef __SWPRINTF_H__
#define __SWPRINTF_H__

void DoWStrTest(const WCHAR *formatstr, WCHAR *param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", 
            convertC(param), convertC(formatstr), 
            convertC(checkstr), convertC(buf));
    }
}

void DoStrTest(const WCHAR *formatstr, char *param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", 
            param, convertC(formatstr), convertC(checkstr), 
            convertC(buf));
    }
}

void DoPointerTest(const WCHAR *formatstr, void* param, const WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert pointer to %#p into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n", param,
            convertC(formatstr), convertC(checkstr1), convertC(buf));
    }    
}

void DoCharTest(const WCHAR *formatstr, char param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", param, param,
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}

void DoWCharTest(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", (char) param, param,
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}

void DoNumTest(const WCHAR *formatstr, int value, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, value);
    if (memcmp(buf, checkstr, wcslen(checkstr)* 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", value, convertC(formatstr),
            convertC(checkstr), convertC(buf));
    }
}

void DoI64Test(const WCHAR *formatstr, INT64 param, char *paramdesc,
               const WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n", paramdesc,
            convertC(formatstr), convertC(checkstr1), convertC(buf));
    }
}

void DoDoubleTest(const WCHAR *formatstr, double value, const WCHAR *checkstr1,
                  const WCHAR *checkstr2)
{
    WCHAR buf[256] = { 0 };

    swprintf_s(buf, _countof(buf), formatstr, value);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, convertC(formatstr), convertC(checkstr1),
            convertC(checkstr2), convertC(buf));
    }
}

void DoArgumentPrecTest(const WCHAR *formatstr, int precision, void *param,
                        char *paramstr, const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    WCHAR buf[256];

    swprintf_s(buf, _countof(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr,
            convertC(formatstr), precision,
            convertC(checkstr1), convertC(checkstr2), convertC(buf));
    }
}

void DoArgumentPrecDoubleTest(const WCHAR *formatstr, int precision, double param,
                              const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    WCHAR buf[256];

    swprintf_s(buf, _countof(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", param,
            convertC(formatstr), precision,
            convertC(checkstr1), convertC(checkstr2), convertC(buf));
    }
}

#endif

