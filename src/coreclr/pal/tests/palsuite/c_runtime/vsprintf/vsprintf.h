// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

/* These functions leaks memory a lot. C'est la vie. */
inline int testvsp(char* buf, size_t buffSize, const char* format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = _vsnprintf_s(buf, buffSize, _TRUNCATE, format, arglist);
    va_end(arglist);

    return (retVal);
}

inline void DoStrTest_vsprintf(const char *formatstr, char* param, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, formatstr, checkstr, buf);
    }
}
#define DoStrTest DoStrTest_vsprintf

inline void DoWStrTest_vsprintf(const char *formatstr, WCHAR* param, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide string \"%s\" into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            convertC(param), formatstr, checkstr, buf);
    }
}
#define DoWStrTest DoWStrTest_vsprintf

inline void DoCharTest_vsprintf(const char *formatstr, char param, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            param, param, formatstr, checkstr, buf);
    }
}
#define DoCharTest DoCharTest_vsprintf

inline void DoWCharTest_vsprintf(const char *formatstr, WCHAR param, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert wide char \'%c\' (%d) into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            (char)param, param, formatstr, checkstr, buf);
    }
}
#define DoWCharTest DoWCharTest_vsprintf

inline void DoNumTest_vsprintf(const char *formatstr, int value, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %#x into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            value, formatstr, checkstr, buf);
    }
}
#define DoNumTest DoNumTest_vsprintf

inline void DoI64Test_vsprintf(const char *formatstr, INT64 value, char *valuestr, const char *checkstr)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            valuestr, formatstr, checkstr, buf);
    }
}
#define DoI64Test DoI64Test_vsprintf

inline void DoDoubleTest_vsprintf(const char *formatstr, double value, const char *checkstr1, char
*checkstr2)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\"\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n",
            value, formatstr, checkstr1, checkstr2, buf);
    }
}
#define DoDoubleTest DoDoubleTest_vsprintf

/*FROM TEST 9*/
inline void DoArgumentPrecTest_vsprintf(const char *formatstr, int precision, void *param,
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    testvsp(buf, ARRAY_SIZE(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", paramstr, formatstr,
            precision, checkstr1, checkstr2, buf);
    }

}
#define DoArgumentPrecTest DoArgumentPrecTest_vsprintf

inline void DoArgumentPrecDoubleTest_vsprintf(const char *formatstr, int precision, double param,
                              const char *checkstr1, const char *checkstr2)
{
    char buf[256];

    testvsp(buf, ARRAY_SIZE(buf), formatstr, precision, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0 &&
        memcmp(buf, checkstr2, strlen(checkstr2) + 1) != 0)
    {
        Fail("ERROR: failed to insert %f into \"%s\" with precision %d\n"
            "Expected \"%s\" or \"%s\", got \"%s\".\n", param, formatstr,
            precision, checkstr1, checkstr2, buf);
    }
}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_vsprintf

/*FROM TEST4*/
inline void DoPointerTest_vsprintf(const char *formatstr, void* param, char* paramstr,
                   const char *checkstr1)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, param);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1))
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\" got \"%s\".\n",
            paramstr, formatstr, checkstr1, buf);
    }
}
#define DoPointerTest DoPointerTest_vsprintf

inline void DoI64DoubleTest_vsprintf(const char *formatstr, INT64 value, char *valuestr,
                     const char *checkstr1)
{
    char buf[256] = { 0 };

    testvsp(buf, ARRAY_SIZE(buf), formatstr, value);
    if (memcmp(buf, checkstr1, strlen(checkstr1) + 1) != 0)
    {
        Fail("ERROR: failed to insert %s into \"%s\"\n"
            "Expected \"%s\", got \"%s\".\n",
            valuestr, formatstr, checkstr1, buf);
    }
}
#define DoI64DoubleTest DoI64DoubleTest_vsprintf

inline void DoTest_vsprintf(const char *formatstr, int param, const char *checkstr)
{
    char buf[256] = { 0 };
    int n = -1;

    testvsp(buf, ARRAY_SIZE(buf), formatstr, &n);

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
#define DoTest DoTest_vsprintf

inline void DoShortTest_vsprintf(const char *formatstr, int param, const char *checkstr)
{
    char buf[256] = { 0 };
    short int n = -1;

    testvsp(buf, ARRAY_SIZE(buf), formatstr, &n);

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
#define DoShortTest DoShortTest_vsprintf

#endif

