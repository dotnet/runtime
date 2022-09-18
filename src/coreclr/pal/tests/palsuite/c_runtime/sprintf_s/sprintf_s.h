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

#ifndef __SPRINTF_S_H__
#define __SPRINTF_S_H__

inline void DoStrTest_sprintf_s(const char *formatstr, char* param, const char *checkstr)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             param, formatstr, checkstr, buf);
    }
}
#define DoStrTest DoStrTest_sprintf_s

inline void DoWStrTest_sprintf_s(const char *formatstr, WCHAR* param, const char *checkstr)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             convertC(param), formatstr, checkstr, buf);
    }
}
#define DoWStrTest DoWStrTest_sprintf_s

inline void DoPointerTest_sprintf_s(const char *formatstr, void* param, char* paramstr,
                   const char *checkstr1)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             paramstr, formatstr, checkstr1, buf);
    }
}
#define DoPointerTest DoPointerTest_sprintf_s

inline void DoCountTest_sprintf_s(const char *formatstr, int param, const char *checkstr)
{
    char buf[512] = { 0 };
    int n = -1;

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, &n);

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
#define DoCountTest DoCountTest_sprintf_s

inline void DoShortCountTest_sprintf_s(const char *formatstr, int param, const char *checkstr)
{
    char buf[256] = { 0 };
    short int n = -1;

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, &n);

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
#define DoShortCountTest DoShortCountTest_sprintf_s

inline void DoCharTest_sprintf_s(const char *formatstr, char param, const char *checkstr)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             param, param, formatstr, checkstr, buf);
    }
}
#define DoCharTest DoCharTest_sprintf_s

inline void DoWCharTest_sprintf_s(const char *formatstr, WCHAR param, const char *checkstr)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             (char)param, param, formatstr, checkstr, buf);
    }
}
#define DoWCharTest DoWCharTest_sprintf_s

inline void DoNumTest_sprintf_s(const char *formatstr, int value, const char *checkstr)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
             "Expected \"%s\" got \"%s\".\n",
             value, formatstr, checkstr, buf);
    }
}
#define DoNumTest DoNumTest_sprintf_s

inline void DoI64Test_sprintf_s(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
             "Expected \"%s\", got \"%s\".\n",
             valuestr, formatstr, checkstr1, buf);
    }
}
#define DoI64Test DoI64Test_sprintf_s

inline void DoDoubleTest_sprintf_s(const char *formatstr, double value, const char *checkstr1,
                  const char *checkstr2)
{
    char buf[256] = { 0 };

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
}
#define DoDoubleTest DoDoubleTest_sprintf_s

inline void DoArgumentPrecTest_sprintf_s(const char *formatstr, int precision, void *param,
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
             "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr, formatstr,
             precision, checkstr1, checkstr2, buf);
    }

}
#define DoArgumentPrecTest DoArgumentPrecTest_sprintf_s

inline void DoArgumentPrecDoubleTest_sprintf_s(const char *formatstr, int precision, double param,
                              const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    sprintf_s(buf, ARRAY_SIZE(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
             "Expected \"%s\" or \"%s\", got \"%s\".\n", param, formatstr,
             precision, checkstr1, checkstr2, buf);
    }

}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_sprintf_s

#endif


