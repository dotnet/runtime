// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  sprintf_s.h
**
** Purpose: Contains common testing functions for sprintf_s
**
**
**==========================================================================*/

#ifndef __STRINGTEST_H__
#define __STRINGTEST_H__

inline void DoStrTest_snprintf_s(const char *formatstr, char* param, const char *checkstr)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, formatstr, checkstr, buf);
    }
}
#define DoStrTest DoStrTest_snprintf_s

inline void DoWStrTest_snprintf_s(const char *formatstr, WCHAR* param, const char *checkstr)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            convertC(param), formatstr, checkstr, buf);
    }
}
#define DoWStrTest DoWStrTest_snprintf_s

inline void DoPointerTest_snprintf_s(const char *formatstr, void* param, char* paramstr, char
                   *checkstr1)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
            paramstr, formatstr, checkstr1, buf);
    }
}
#define DoPointerTest DoPointerTest_snprintf_s

inline void DoCountTest_snprintf_s(const char *formatstr, int param, const char *checkstr)
{
    char buf[512] = { 0 };
    int n = -1;

    sprintf_s(buf, 512, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
             param, n);
    }
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }
}
#define DoCountTest DoCountTest_snprintf_s

inline void DoShortCountTest_snprintf_s(const char *formatstr, int param, const char *checkstr)
{
    char buf[256] = { 0 };
    short int n = -1;

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
              param, n);
    }
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }
}
#define DoShortCountTest DoShortCountTest_snprintf_s

inline void DoCharTest_snprintf_s(const char *formatstr, char param, const char *checkstr)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, param, formatstr, checkstr, buf);
    }
}
#define DoCharTest DoCharTest_snprintf_s

inline void DoWCharTest_snprintf_s(const char *formatstr, WCHAR param, const char *checkstr)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char)param, param, formatstr, checkstr, buf);
    }
}
#define DoWCharTest DoWCharTest_snprintf_s

inline void DoNumTest_snprintf_s(const char *formatstr, int value, const char *checkstr)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, value);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            value, formatstr, checkstr, buf);
    }
}
#define DoNumTest DoNumTest_snprintf_s

inline void DoI64Test_snprintf_s(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
            valuestr, formatstr, checkstr1, buf);
    }
}
#define DoI64Test DoI64Test_snprintf_s

inline void DoDoubleTest_snprintf_s(const char *formatstr, double value, const char *checkstr1, char
*checkstr2)
{
    char buf[256] = { 0 };

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0
       && memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
}
#define DoDoubleTest DoDoubleTest_snprintf_s

inline void DoArgumentPrecTest_snprintf_s(const char *formatstr, int precision, void *param, char
*paramstr, const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
       memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
             paramstr, formatstr, precision, checkstr1, checkstr2, buf);
    }

}
#define DoArgumentPrecTest DoArgumentPrecTest_snprintf_s

inline void DoArgumentPrecDoubleTest_snprintf_s(const char *formatstr, int precision, double param,
const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    _snprintf_s(buf, 256, _TRUNCATE, formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
       memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
             "Expected \"%s\" or \"%s\", got \"%s\".\n",
             param, formatstr, precision, checkstr1, checkstr2, buf);
    }

}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_snprintf_s

#endif

