// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  printf.h
**
** Purpose: Contains common testing functions for printf
**
**
**==========================================================================*/

#ifndef __printf_H__
#define __printf_H__

inline void DoStrTest_printf(const char *formatstr, char* param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoStrTest DoStrTest_printf

inline void DoWStrTest_printf(const char *formatstr, WCHAR* param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoWStrTest DoWStrTest_printf

inline void DoPointerTest_printf(const char *formatstr, void* param, char* paramstr,
                   const char *checkstr1)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr1), ret);
    }
}
#define DoPointerTest DoPointerTest_printf

inline void DoCountTest_printf(const char *formatstr, int param, const char *checkstr)
{
    int ret;
    int n = -1;

    ret = printf(formatstr, &n);

    if (n != param)
    {
        Fail("Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoCountTest DoCountTest_printf

inline void DoShortCountTest_printf(const char *formatstr, int param, const char *checkstr)
{
    int ret;
    short int n = -1;

    ret = printf(formatstr, &n);

    if (n != param)
    {
        Fail("Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoShortCountTest DoShortCountTest_printf

inline void DoCharTest_printf(const char *formatstr, char param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoCharTest DoCharTest_printf

inline void DoWCharTest_printf(const char *formatstr, WCHAR param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoWCharTest DoWCharTest_printf

inline void DoNumTest_printf(const char *formatstr, int param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr), ret);
    }
}
#define DoNumTest DoNumTest_printf

inline void DoI64Test_printf(const char *formatstr, INT64 param, char *valuestr,
               const char *checkstr1)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected printf to return %d, got %d.\n",
            strlen(checkstr1), ret);
    }
}
#define DoI64Test DoI64Test_printf

inline void DoDoubleTest_printf(const char *formatstr, double param,
                  const char *checkstr1, const char *checkstr2)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected printf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoDoubleTest DoDoubleTest_printf

inline void DoArgumentPrecTest_printf(const char *formatstr, int precision, void *param,
                        char *paramstr, const char *checkstr1, const char *checkstr2)
{
    int ret;

    ret = printf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected printf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoArgumentPrecTest DoArgumentPrecTest_printf

inline void DoArgumentPrecDoubleTest_printf(const char *formatstr, int precision, double param,
    const char *checkstr1, const char *checkstr2)
{
    int ret;

    ret = printf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected printf to return %d or %d, got %d.\n",
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_printf

#endif

