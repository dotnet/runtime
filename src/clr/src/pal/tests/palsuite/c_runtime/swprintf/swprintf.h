// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

void DoWStrTest(WCHAR *formatstr, WCHAR *param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", 
            convertC(param), convertC(formatstr), 
            convertC(checkstr), convertC(buf));
    }
}

void DoStrTest(WCHAR *formatstr, char *param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(checkstr) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", 
            param, convertC(formatstr), convertC(checkstr), 
            convertC(buf));
    }
}

void DoPointerTest(WCHAR *formatstr, void* param, WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert pointer to %#p into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n", param,
            convertC(formatstr), convertC(checkstr1), convertC(buf));
    }    
}

void DoCountTest(WCHAR *formatstr, int param, WCHAR *checkstr)
{
    WCHAR buf[512] = { 0 };
    int n = -1;

    swprintf(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
             param, n);
    }

    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n",
            convertC(checkstr), convertC(buf));
    }
}

void DoShortCountTest(WCHAR *formatstr, int param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    short int n = -1;

    swprintf(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
             param, n);
    }

    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n",
            convertC(checkstr), convertC(buf));
    }
}

void DoCharTest(WCHAR *formatstr, char param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", param, param,
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}

void DoWCharTest(WCHAR *formatstr, WCHAR param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n", (char) param, param,
             convertC(formatstr), convertC(checkstr), convertC(buf));
    }
}

void DoNumTest(WCHAR *formatstr, int value, WCHAR*checkstr)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, value);
    if (memcmp(buf, checkstr, wcslen(checkstr)* 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", value, convertC(formatstr),
            convertC(checkstr), convertC(buf));
    }
}

void DoI64Test(WCHAR *formatstr, INT64 param, char *paramdesc,
               WCHAR *checkstr1)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n", paramdesc,
            convertC(formatstr), convertC(checkstr1), convertC(buf));
    }
}

void DoDoubleTest(WCHAR *formatstr, double value, WCHAR *checkstr1,
                  WCHAR *checkstr2)
{
    WCHAR buf[256] = { 0 };

    swprintf(buf, formatstr, value);
    if (memcmp(buf, checkstr1, wcslen(checkstr1)*2 + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, convertC(formatstr), convertC(checkstr1),
            convertC(checkstr2), convertC(buf));
    }
}

void DoArgumentPrecTest(WCHAR *formatstr, int precision, void *param,
                        char *paramstr, WCHAR *checkstr1, WCHAR *checkstr2)
{
    WCHAR buf[256];

    swprintf(buf, formatstr, precision, param);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr,
            convertC(formatstr), precision,
            convertC(checkstr1), convertC(checkstr2), convertC(buf));
    }
}

void DoArgumentPrecDoubleTest(WCHAR *formatstr, int precision, double param,
                              WCHAR *checkstr1, WCHAR *checkstr2)
{
    WCHAR buf[256];

    swprintf(buf, formatstr, precision, param);
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

