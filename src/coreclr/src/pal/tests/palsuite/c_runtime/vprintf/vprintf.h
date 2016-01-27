// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  vprintf.h
**
** Purpose: Containts common testing functions for vprintf
**
**
**==========================================================================*/

#ifndef __vprintf_H__
#define __vprintf_H__

int DoVprintf(char *format, ...)
{
    int retVal;
    va_list arglist;

    va_start(arglist, format);
    retVal = vprintf(format, arglist);
    va_end(arglist);

    return (retVal);
}

void DoStrTest(char *formatstr, char* param, char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoWStrTest(char *formatstr, WCHAR* param, char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoPointerTest(char *formatstr, void* param, char* paramstr, 
                   char *checkstr1)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr1), ret);
    }
}

void DoCountTest(char *formatstr, int param, char *checkstr)
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

void DoShortCountTest(char *formatstr, int param, char *checkstr)
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


void DoCharTest(char *formatstr, char param, char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }
}

void DoWCharTest(char *formatstr, WCHAR param, char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }    
}

void DoNumTest(char *formatstr, int param, char *checkstr)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);
    }    
}

void DoI64Test(char *formatstr, INT64 param, char *valuestr, char *checkstr1)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr1), ret);
    }
}

void DoDoubleTest(char *formatstr, double param, char *checkstr1, 
                  char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n", 
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}

void DoArgumentPrecTest(char *formatstr, int precision, void *param, 
                        char *paramstr, char *checkstr1, char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n", 
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}

void DoArgumentPrecDoubleTest(char *formatstr, int precision, double param, 
    char *checkstr1, char *checkstr2)
{
    int ret;

    ret = DoVprintf(formatstr, precision, param);
    if (ret != strlen(checkstr1) && ret != strlen(checkstr2))
    {
        Fail("Expected vprintf to return %d or %d, got %d.\n", 
            strlen(checkstr1), strlen(checkstr2), ret);
    }
}

#endif

