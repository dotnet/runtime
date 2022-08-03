// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: wprintf.h
**
** Purpose: Contains common testing functions for wprintf
**
**
**==========================================================================*/

#ifndef __wprintf_H__
#define __wprintf_H__

inline void DoStrTest_wprintf(const WCHAR *formatstr, const WCHAR *param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoStrTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoStrTest DoStrTest_wprintf

inline void DoPointerTest_wprintf(const WCHAR *formatstr, void* param, WCHAR* paramstr,
                   const WCHAR *checkstr1)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1))
    {
        Fail("DoPointerTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr1), ret);
    }
}
#define DoPointerTest DoPointerTest_wprintf

inline void DoCountTest_wprintf(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    int ret;
    int n = -1;

    ret = wprintf(formatstr, &n);

    if (n != param)
    {
        Fail("DoCountTest:Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != wcslen(checkstr))
    {
        Fail("DoCountTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoCountTest DoCountTest_wprintf

inline void DoShortCountTest_wprintf(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    int ret;
    short int n = -1;

    ret = wprintf(formatstr, &n);

    if (n != param)
    {
        Fail("DoShortCountTest:Expected count parameter to resolve to %d, got %d\n", param, n);
    }

    if (ret != wcslen(checkstr))
    {
        Fail("DoShortCountTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoShortCountTest DoShortCountTest_wprintf

inline void DoCharTest_wprintf(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoCharTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoCharTest DoCharTest_wprintf

inline void DoWCharTest_wprintf(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoWCharTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoWCharTest DoWCharTest_wprintf

inline void DoNumTest_wprintf(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoNumTest:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr), ret);
    }
}
#define DoNumTest DoNumTest_wprintf

inline void DoI64Test_wprintf(const WCHAR *formatstr, INT64 param, const WCHAR *valuestr,
               const WCHAR *checkstr1)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1))
    {
        Fail("DoI64Test:Expected wprintf to return %d, got %d.\n",
            wcslen(checkstr1), ret);
    }
}
#define DoI64Test DoI64Test_wprintf

inline void DoDoubleTest_wprintf(const WCHAR *formatstr, double param,
                  const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1) && ret != wcslen(checkstr2))
    {
        Fail("DoDoubleTest:Expected wprintf to return %d or %d, got %d.\n",
            wcslen(checkstr1), wcslen(checkstr2), ret);
    }
}
#define DoDoubleTest DoDoubleTest_wprintf

inline void DoArgumentPrecTest_wprintf(const WCHAR *formatstr, int precision, void *param,
                        WCHAR *paramstr, const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    int ret;

    ret = wprintf(formatstr, precision, param);
    if (ret != wcslen(checkstr1) && ret != wcslen(checkstr2))
    {
        Fail("DoArgumentPrecTest:Expected wprintf to return %d or %d, got %d.\n",
            wcslen(checkstr1), wcslen(checkstr2), ret);
    }
}
#define DoArgumentPrecTest DoArgumentPrecTest_wprintf

inline void DoArgumentPrecDoubleTest_wprintf(const WCHAR *formatstr, int precision, double param,
    const WCHAR *checkstr1, const WCHAR *checkstr2)
{
    int ret;

    ret = wprintf(formatstr, precision, param);
    if (ret != wcslen(checkstr1) && ret != wcslen(checkstr2))
    {
        Fail("DoArgumentPrecDoubleTest:Expected wprintf to return %d or %d, got %d.\n",
            wcslen(checkstr1), wcslen(checkstr2), ret);
    }
}
#define DoArgumentPrecDoubleTest DoArgumentPrecDoubleTest_wprintf

#endif

