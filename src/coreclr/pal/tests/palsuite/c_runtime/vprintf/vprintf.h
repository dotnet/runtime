// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  vprintf.h
**
** Purpose: Contains common testing functions for vprintf
**
**
**==========================================================================*/

#ifndef __vprintf_H__
#define __vprintf_H__

inline int DoVprintf(const char *format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vprintf(format, arglist);
    va_end(arglist);

    return (retVal);
}

inline void DoStrTest_vprintf(const char *formatstr, char* param, const char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoStrTest DoStrTest_vprintf

inline void DoWStrTest_vprintf(const char *formatstr, WCHAR* param, const char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoWStrTest DoWStrTest_vprintf

inline void DoPointerTest_vprintf(const char *formatstr, void* param, char* paramstr,
                   const char *checkstr1)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr1), ret);
    }
}
#define DoPointerTest DoPointerTest_vprintf

inline void DoCountTest_vprintf(const char *formatstr, int param, const char *checkstr)
{
    int ret;
    int n = -1;

    ret = DoVprintf(formatstr, &n);

    if (n != param)
    {
        Fail("Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoCountTest DoCountTest_vprintf

inline void DoShortCountTest_vprintf(const char *formatstr, int param, const char *checkstr)
{
    int ret;
    short int n = -1;

    ret = DoVprintf(formatstr, &n);

    if (n != param)
    {
        Fail("Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoShortCountTest DoShortCountTest_vprintf

inline void DoCharTest_vprintf(const char *formatstr, char param, const char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoCharTest DoCharTest_vprintf

inline void DoWCharTest_vprintf(const char *formatstr, WCHAR param, const char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoWCharTest DoWCharTest_vprintf

inline void DoNumTest_vprintf(const char *formatstr, int param, const char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoNumTest DoNumTest_vprintf

inline void DoI64Test_vprintf(const char *formatstr, INT64 param, char *valuestr, const char *checkstr1)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected vprintf to return %d, got %d.\n",
            strlen(checkstr1), ret);
    }
}
#define DoI64Test DoI64Test_vprintf

inline void DoDoubleTest_vprintf(const char *formatstr, double param, const char *checkstr1,
                  const char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoDoubleTest DoDoubleTest_vprintf

inline void DoArgumentPrecTest_vprintf(const char *formatstr, int precision, void *param,
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoArgumentPrecTest DoArgumentPrecTest_vprintf

inline void DoArgumentPrecDoubleTest_vprintf(const char *formatstr, int precision, double param,
    const char *checkstr1, const char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_vprintf

#endif

