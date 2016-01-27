// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    vsprintf.h
**
** Purpose:   Helper functions for the vsprintf tests.
**
**
**===================================================================*/
#ifndef __VSPRINTF_H__
#define __VSPRINTF_H__

/* These functions leaks memory like crazy. C'est la vie. */
int testvsp(char* buf, const char* format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vsprintf(buf, format, arglist);
    va_end(arglist);

    return (retVal);
}

void DoStrTest(char *formatstr, char* param, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, formatstr, checkstr, buf);
    }
}

void DoWStrTest(char *formatstr, WCHAR* param, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            convertC(param), formatstr, checkstr, buf);
    }
}


void DoCharTest(char *formatstr, char param, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, param, formatstr, checkstr, buf);
    }
}

void DoWCharTest(char *formatstr, WCHAR param, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char)param, param, formatstr, checkstr, buf);
    }
}

void DoNumTest(char *formatstr, int value, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            value, formatstr, checkstr, buf);
    }
}

void DoI64Test(char *formatstr, INT64 value, char *valuestr, char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            valuestr, formatstr, checkstr, buf);
    }
}
void DoDoubleTest(char *formatstr, double value, char *checkstr1, char
*checkstr2)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
}
/*FROM TEST 9*/
void DoArgumentPrecTest(char *formatstr, int precision, void *param,
                        char *paramstr, char *checkstr1, char *checkstr2)
{
    char buf[256];

    testvsp(buf, formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr, formatstr,
            precision, checkstr1, checkstr2, buf);
    }

}

void DoArgumentPrecDoubleTest(char *formatstr, int precision, double param,
                              char *checkstr1, char *checkstr2)
{
    char buf[256];

    testvsp(buf, formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", param, formatstr,
            precision, checkstr1, checkstr2, buf);
    }
}
/*FROM TEST4*/
void DoPointerTest(char *formatstr, void* param, char* paramstr,
                   char *checkstr1)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1))
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            paramstr, formatstr, checkstr1, buf);
    }
}

void DoI64DoubleTest(char *formatstr, INT64 value, char *valuestr,
                     char *checkstr1)
{
    char buf[256] = { 0 };

    testvsp(buf, formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
            valuestr, formatstr, checkstr1, buf);
    }
}

void DoTest(char *formatstr, int param, char *checkstr)
{
    char buf[256] = { 0 };
    int n = -1;

    testvsp(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
             param, n);
    }
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }
}

void DoShortTest(char *formatstr, int param, char *checkstr)
{
    char buf[256] = { 0 };
    short int n = -1;

    testvsp(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
             param, n);
    }
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }
}

#endif

