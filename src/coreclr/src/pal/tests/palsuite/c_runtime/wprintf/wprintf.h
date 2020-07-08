// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: wprintf.h
**
** Purpose: Containts common testing functions for wprintf
**
**
**==========================================================================*/

#ifndef __wprintf_H__
#define __wprintf_H__

void DoStrTest(const WCHAR *formatstr, const WCHAR *param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoStrTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }
}


void DoPointerTest(const WCHAR *formatstr, void* param, WCHAR* paramstr, 
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

void DoCountTest(const WCHAR *formatstr, int param, const WCHAR *checkstr)
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

void DoShortCountTest(const WCHAR *formatstr, int param, const WCHAR *checkstr)
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


void DoCharTest(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoCharTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }
}

void DoWCharTest(const WCHAR *formatstr, WCHAR param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoWCharTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }    
}

void DoNumTest(const WCHAR *formatstr, int param, const WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoNumTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }    
}

void DoI64Test(const WCHAR *formatstr, INT64 param, const WCHAR *valuestr, 
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

void DoDoubleTest(const WCHAR *formatstr, double param, 
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

void DoArgumentPrecTest(const WCHAR *formatstr, int precision, void *param, 
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

void DoArgumentPrecDoubleTest(const WCHAR *formatstr, int precision, double param, 
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

#endif

