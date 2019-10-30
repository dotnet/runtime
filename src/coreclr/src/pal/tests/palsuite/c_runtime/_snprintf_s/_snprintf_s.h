// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  sprintf_s.h
**
** Purpose: Containts common testing functions for sprintf_s
**
**
**==========================================================================*/

#ifndef __STRINGTEST_H__
#define __STRINGTEST_H__

void DoStrTest(const char *formatstr, char* param, const char *checkstr)
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

void DoWStrTest(const char *formatstr, WCHAR* param, const char *checkstr)
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


void DoPointerTest(const char *formatstr, void* param, char* paramstr, char
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

void DoCountTest(const char *formatstr, int param, const char *checkstr)
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

void DoShortCountTest(const char *formatstr, int param, const char *checkstr)
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

void DoCharTest(const char *formatstr, char param, const char *checkstr)
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

void DoWCharTest(const char *formatstr, WCHAR param, const char *checkstr)
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

void DoNumTest(const char *formatstr, int value, const char *checkstr)
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

void DoI64Test(const char *formatstr, INT64 value, char *valuestr, const char *checkstr1)
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

void DoDoubleTest(const char *formatstr, double value, const char *checkstr1, char 
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

void DoArgumentPrecTest(const char *formatstr, int precision, void *param, char 
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

void DoArgumentPrecDoubleTest(const char *formatstr, int precision, double param, 
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

#endif

