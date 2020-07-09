// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  printf.h
**
** Purpose: Containts common testing functions for printf
**
**
**==========================================================================*/

#ifndef __printf_H__
#define __printf_H__

void DoStrTest(const char *formatstr, char* param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoWStrTest(const char *formatstr, WCHAR* param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoPointerTest(const char *formatstr, void* param, char* paramstr, 
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

void DoCountTest(const char *formatstr, int param, const char *checkstr)
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

void DoShortCountTest(const char *formatstr, int param, const char *checkstr)
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


void DoCharTest(const char *formatstr, char param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoWCharTest(const char *formatstr, WCHAR param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }    
}

void DoNumTest(const char *formatstr, int param, const char *checkstr)
{
    int ret;

    ret = printf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }    
}

void DoI64Test(const char *formatstr, INT64 param, char *valuestr, 
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

void DoDoubleTest(const char *formatstr, double param, 
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

void DoArgumentPrecTest(const char *formatstr, int precision, void *param, 
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

void DoArgumentPrecDoubleTest(const char *formatstr, int precision, double param, 
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

#endif

