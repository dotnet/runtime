// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  swscanf.h
**
** Purpose: Contains common testing functions for swscanf.h
**
**
**==========================================================================*/

#ifndef __SWSCANF_H__
#define __SWSCANF_H__

void DoVoidTest(WCHAR *inputstr, const WCHAR *formatstr)
{
    char buf[256] = { 0 };
    int i;
    int ret;

    ret = swscanf(inputstr, formatstr, buf);
    if (ret != 0)
    {
        Fail("ERROR: Expected sscanf to return 0, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    for (i=0; i<256; i++)
    {
        if (buf[i] != 0)
        {
            Fail("ERROR: Parameter unexpectedly modified scanning \"%s\" "
                "using \"%s\".\n", convertC(inputstr), 
                convertC(formatstr));
        }
    }

}

void DoStrTest(WCHAR *inputstr, const WCHAR *formatstr, const char *checkstr)
{
    char buf[256] = { 0 };
    int ret;

    ret = swscanf(inputstr, formatstr, buf);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (memcmp(checkstr, buf, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: scanned string incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", convertC(inputstr), 
            convertC(formatstr), checkstr, 
            buf);
    }

}

void DoWStrTest(WCHAR *inputstr, const WCHAR *formatstr, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    int ret;

    ret = swscanf(inputstr, formatstr, buf);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr),
            convertC(formatstr));
    }

    if (memcmp(checkstr, buf, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: scanned wide string incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", convertC(inputstr), 
            convertC(formatstr), convertC(checkstr), 
            convertC(buf));
    }

}

void DoNumTest(WCHAR *inputstr, const WCHAR *formatstr, int checknum)
{
    int num = 0;
    int ret;

    ret = swscanf(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (checknum != num)
    {
        Fail("ERROR: scanned number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %d, got %d.\n", convertC(inputstr), 
            convertC(formatstr), checknum, num);
    }
}

void DoShortNumTest(WCHAR *inputstr, const WCHAR *formatstr, short checknum)
{
    short num = 0;
    int ret;

    ret = swscanf(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (checknum != num)
    {
        Fail("ERROR: scanned number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %hd, got %hd.\n", convertC(inputstr), 
            convertC(formatstr), checknum, num);
    }
}

void DoI64NumTest(WCHAR *inputstr, const WCHAR *formatstr, INT64 checknum)
{
    char buf[256];
    char check[256];
    INT64 num;
    int ret;

    ret = swscanf(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (checknum != num)
    {
        sprintf_s(buf, _countof(buf), "%I64d", num);
        sprintf_s(check, _countof(check), "%I64d", checknum);
        Fail("ERROR: scanned I64 number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", convertC(inputstr), 
            convertC(formatstr), check, buf);
    }
}

void DoCharTest(WCHAR *inputstr, const WCHAR *formatstr, char* checkchars, int numchars)
{
    char buf[256];
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (char)-1;

    ret = swscanf(inputstr, formatstr, buf);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (memcmp(buf, checkchars, numchars) != 0)
    {
        buf[numchars] = 0;

        Fail("ERROR: scanned character(s) incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", convertC(inputstr), 
            convertC(formatstr), checkchars, buf);
    }

    if (buf[numchars] != (char)-1)
    {
        Fail("ERROR: overflow occurred in scanning character(s) from \"%s\" "
            "using \"%s\".\nExpected %d character(s)\n", 
            convertC(inputstr), convertC(formatstr), numchars);
    }
}

void DoWCharTest(WCHAR *inputstr, const WCHAR *formatstr, const WCHAR *checkchars, int numchars)
{
    WCHAR buf[256];
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (WCHAR)-1;

    ret = swscanf(inputstr, formatstr, buf);
    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (memcmp(buf, checkchars, numchars*2) != 0)
    {
        buf[numchars] = 0;

        Fail("ERROR: scanned wide character(s) incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", convertC(inputstr), 
            convertC(formatstr), convertC(checkchars), 
            convertC(buf));
    }

    if (buf[numchars] != (WCHAR)-1)
    {
        Fail("ERROR: overflow occurred in scanning wide character(s) from \"%s\" "
            "using \"%s\".\nExpected %d character(s)\n", 
            convertC(inputstr), convertC(formatstr), numchars);
    }
}


void DoFloatTest(WCHAR *inputstr, const WCHAR *formatstr, float checkval)
{
    char buf[256] = { 0 };
    float val;
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (char)-1;

    ret = swscanf(inputstr, formatstr, buf);
    val = *(float*)buf;

    if (ret != 1)
    {
        Fail("ERROR: Expected swscanf to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, convertC(inputstr), 
            convertC(formatstr));
    }

    if (val != checkval)
    {
        Fail("ERROR: scanned float incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%f\", got \"%f\".\n", convertC(inputstr), 
            convertC(formatstr), checkval, val);
    }

    if (buf[4] != (char)-1)
    {
        Fail("ERROR: overflow occurred in scanning float from \"%s\" "
            "using \"%s\".\n", convertC(inputstr), convertC(formatstr));

    }
}


#endif
