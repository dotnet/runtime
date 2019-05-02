// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  _vsnwprintf_s.h
**
** Purpose: Containts common testing functions for _vsnwprintf_s
**
**
**==========================================================================*/

#ifndef ___VSNWPRINTF_H__
#define ___VSNWPRINTF_H__

/* These functions leaks memory a lot. C'est la vie. */
int TestVsnwprintf_s(wchar_t* buf, size_t count, const wchar_t* format, ...)
{
    int retVal = 0;
    va_list arglist;

    va_start(arglist, format);
    retVal = _vsnwprintf_s(buf, count, _TRUNCATE, format, arglist);
    va_end(arglist);

    return( retVal);
}


void DoWStrTest(const WCHAR *formatstr, WCHAR *param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(buf) * 2 + 2) != 0)
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

    TestVsnwprintf_s(buf, 256, formatstr, param);

    if (memcmp(buf, checkstr, wcslen(buf) * 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", 
            param, convertC(formatstr), convertC(checkstr),
            convertC(buf));
    }
}

void DoCharTest(const WCHAR *formatstr, char param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(buf)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", 
            param, param, convertC(formatstr), convertC(checkstr),
            convertC(buf));
    }
}

void DoWCharTest(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, wcslen(buf)*2 + 2) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char) param, param, convertC(formatstr),
            convertC(checkstr), convertC(buf));
    }    
}

void DoNumTest(const WCHAR *formatstr, int value, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, value);
    if (memcmp(buf, checkstr, wcslen(buf)* 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", value, convertC(formatstr),
            convertC(checkstr), convertC(buf));
    }    
}

void DoI64NumTest(const WCHAR *formatstr, INT64 value, char *valuestr, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, value);
    if (memcmp(buf, checkstr, wcslen(buf)* 2 + 2) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n", valuestr, convertC(formatstr),
            convertC(checkstr), convertC(buf));
    }    
}
void DoDoubleTest(const WCHAR *formatstr, double value,
                  const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    WCHAR buf[256] = { 0 };

    TestVsnwprintf_s(buf, 256, formatstr, value);
    if (memcmp(buf, checkstr1, wcslen(checkstr1) + 2) != 0 &&
        memcmp(buf, checkstr2, wcslen(checkstr2) + 2) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
                value,
                convertC(formatstr),
                convertC(checkstr1),
                convertC(checkstr2),
                convertC(buf));
    }
}

#endif
