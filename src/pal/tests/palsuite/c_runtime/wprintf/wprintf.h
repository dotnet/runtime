// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

void DoStrTest(WCHAR *formatstr, WCHAR* param, WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoStrTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }
}


void DoPointerTest(WCHAR *formatstr, void* param, WCHAR* paramstr, 
                   WCHAR *checkstr1)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1))
    {
        Fail("DoPointerTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr1), ret);
    }
}

void DoCountTest(WCHAR *formatstr, int param, WCHAR *checkstr)
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

void DoShortCountTest(WCHAR *formatstr, int param, WCHAR *checkstr)
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


void DoCharTest(WCHAR *formatstr, WCHAR param, WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoCharTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }
}

void DoWCharTest(WCHAR *formatstr, WCHAR param, WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoWCharTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }    
}

void DoNumTest(WCHAR *formatstr, int param, WCHAR *checkstr)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr))
    {
        Fail("DoNumTest:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr), ret);
    }    
}

void DoI64Test(WCHAR *formatstr, INT64 param, WCHAR *valuestr, 
               WCHAR *checkstr1)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1))
    {
        Fail("DoI64Test:Expected wprintf to return %d, got %d.\n", 
            wcslen(checkstr1), ret);
    }
}

void DoDoubleTest(WCHAR *formatstr, double param, 
                  WCHAR *checkstr1, WCHAR *checkstr2)
{
    int ret;

    ret = wprintf(formatstr, param);
    if (ret != wcslen(checkstr1) && ret != wcslen(checkstr2))
    {
        Fail("DoDoubleTest:Expected wprintf to return %d or %d, got %d.\n", 
            wcslen(checkstr1), wcslen(checkstr2), ret);
    }
}

void DoArgumentPrecTest(WCHAR *formatstr, int precision, void *param, 
                        WCHAR *paramstr, WCHAR *checkstr1, WCHAR *checkstr2)
{
    int ret;

    ret = wprintf(formatstr, precision, param);
    if (ret != wcslen(checkstr1) && ret != wcslen(checkstr2))
    {
        Fail("DoArgumentPrecTest:Expected wprintf to return %d or %d, got %d.\n", 
            wcslen(checkstr1), wcslen(checkstr2), ret);
    }
}

void DoArgumentPrecDoubleTest(WCHAR *formatstr, int precision, double param, 
    WCHAR *checkstr1, WCHAR *checkstr2)
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

