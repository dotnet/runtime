// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  _vsnprintf_s.h
**
** Purpose: Contains common testing functions for _vsnprintf
**
**
**==========================================================================*/

#ifndef __STRINGTEST_H__
#define __STRINGTEST_H__

/* These functions leaks memory a lot. C'est la vie. */
int Testvsnprintf(char* buf, size_t count, const char* format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = _vsnprintf_s(buf, count, _TRUNCATE, format, arglist);
    va_end(arglist);

    return (retVal);
}


void DoStrTest(const char *formatstr, char* param, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, formatstr, checkstr, buf);
    }
}

void DoWStrTest(const char *formatstr, WCHAR* param, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            convertC(param), formatstr, checkstr, buf);
    }
}


void DoCharTest(const char *formatstr, char param, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, param, formatstr, checkstr, buf);
    }
}

void DoWCharTest(const char *formatstr, WCHAR param, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char)param, param, formatstr, checkstr, buf);
    }
}

void DoNumTest(const char *formatstr, int value, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            value, formatstr, checkstr, buf);
    }
}

void DoI64Test(const char *formatstr, INT64 value, char *valuestr, const char *checkstr)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf, 256, formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            valuestr, formatstr, checkstr, buf);
    }
}
void DoDoubleTest(const char *formatstr, double value, const char *checkstr1, char
 *checkstr2)
{
    char buf[256] = { 0 };

    Testvsnprintf(buf,256, formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
}

#endif
