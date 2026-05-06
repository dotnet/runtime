// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  sscanf_s.h
**
** Purpose: Contains common testing functions for sscanf_s
**
**
**==========================================================================*/

#ifndef __SSCANF_S_H__
#define __SSCANF_S_H__

inline void DoVoidTest_scanf_s(char *inputstr, const char *formatstr)
{
    char buf[256] = { 0 };
    int i;
    int ret;

    ret = sscanf_s(inputstr, formatstr, buf);
    if (ret != 0)
    {
        Fail("ERROR: Expected sscanf_s to return 0, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    for (i=0; i<256; i++)
    {
        if (buf[i] != 0)
        {
            Fail("ERROR: Parameter unexpectedly modified scanning \"%s\" "
                "using \"%s\".\n", inputstr, formatstr);
        }
    }

}
#define DoVoidTest DoVoidTest_scanf_s

inline void DoStrTest_scanf_s(char *inputstr, const char *formatstr, const char *checkstr)
{
    char buf[256] = { 0 };
    int ret;

    ret = sscanf_s(inputstr, formatstr, buf, ARRAY_SIZE(buf));
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (memcmp(checkstr, buf, strlen(checkstr) + 1) != 0)
    {
        Fail("ERROR: scanned string incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", inputstr, formatstr, checkstr,
            buf);
    }

}
#define DoStrTest DoStrTest_scanf_s

inline void DoWStrTest_scanf_s(char *inputstr, const char *formatstr, const WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    int ret;

    ret = sscanf_s(inputstr, formatstr, buf, ARRAY_SIZE(buf));
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (memcmp(checkstr, buf, wcslen(checkstr)*2 + 2) != 0)
    {
        Fail("ERROR: scanned wide string incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%s\", got \"%s\".\n", inputstr, formatstr,
            convertC(checkstr), convertC(buf));
    }

}
#define DoWStrTest DoWStrTest_scanf_s

inline void DoNumTest_scanf_s(char *inputstr, const char *formatstr, int checknum)
{
    int num;
    int ret;

    ret = sscanf_s(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (checknum != num)
    {
        Fail("ERROR: scanned number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %d, got %d.\n", inputstr, formatstr, checknum, num);
    }
}
#define DoNumTest DoNumTest_scanf_s

inline void DoShortNumTest_scanf_s(char *inputstr, const char *formatstr, short checknum)
{
    short num;
    int ret;

    ret = sscanf_s(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (checknum != num)
    {
        Fail("ERROR: scanned number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %hd, got %hd.\n", inputstr, formatstr, checknum, num);
    }
}
#define DoShortNumTest DoShortNumTest_scanf_s

inline void DoI64NumTest_scanf_s(char *inputstr, const char *formatstr, INT64 checknum)
{
    char buf[256];
    char check[256];
    INT64 num;
    int ret;

    ret = sscanf_s(inputstr, formatstr, &num);
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (checknum != num)
    {
        sprintf_s(buf, ARRAY_SIZE(buf), "%I64d", num);
        sprintf_s(check, ARRAY_SIZE(check), "%I64d", checknum);
        Fail("ERROR: scanned I64 number incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", inputstr, formatstr, check, buf);
    }
}
#define DoI64NumTest DoI64NumTest_scanf_s

inline void DoCharTest_scanf_s(char *inputstr, const char *formatstr, char* checkchars, int numchars)
{
    char buf[256];
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (char)-1;

    ret = sscanf_s(inputstr, formatstr, buf, ARRAY_SIZE(buf));
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (memcmp(buf, checkchars, numchars) != 0)
    {
        buf[numchars] = 0;

        Fail("ERROR: scanned character(s) incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", inputstr, formatstr, checkchars,
            buf);
    }

    if (buf[numchars] != (char)-1)
    {
        Fail("ERROR: overflow occurred in scanning character(s) from \"%s\" "
            "using \"%s\".\nExpected %d character(s)\n", inputstr, formatstr,
            numchars);
    }
}
#define DoCharTest DoCharTest_scanf_s

inline void DoWCharTest_scanf_s(char *inputstr, const char *formatstr, WCHAR* checkchars, int numchars)
{
    WCHAR buf[256];
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (WCHAR)-1;

    ret = sscanf_s(inputstr, formatstr, buf, ARRAY_SIZE(buf));
    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (memcmp(buf, checkchars, numchars) != 0)
    {
        buf[numchars] = 0;

        Fail("ERROR: scanned wide character(s) incorrectly from \"%s\" using \"%s\".\n"
            "Expected %s, got %s.\n", inputstr, formatstr, convertC(checkchars),
            convertC(buf));
    }

    if (buf[numchars] != (WCHAR)-1)
    {
        Fail("ERROR: overflow occurred in scanning wide character(s) from \"%s\" "
            "using \"%s\".\nExpected %d character(s)\n", inputstr, formatstr,
            numchars);
    }
}
#define DoWCharTest DoWCharTest_scanf_s

inline void DoFloatTest_scanf_s(char *inputstr, const char *formatstr, float checkval)
{
    char buf[256] = { 0 };
    float val;
    int ret;
    int i;

    for (i=0; i<256; i++)
        buf[i] = (char)-1;

    ret = sscanf_s(inputstr, formatstr, buf);
    val = *(float*)buf;

    if (ret != 1)
    {
        Fail("ERROR: Expected sscanf_s to return 1, got %d.\n"
            "Using \"%s\" in \"%s\".\n", ret, inputstr, formatstr);
    }

    if (val != checkval)
    {
        Fail("ERROR: scanned float incorrectly from \"%s\" using \"%s\".\n"
            "Expected \"%f\", got \"%f\".\n", inputstr, formatstr, checkval,
            val);
    }

    if (buf[4] != (char)-1)
    {
        Fail("ERROR: overflow occurred in scanning float from \"%s\" "
            "using \"%s\".\n", inputstr, formatstr);

    }
}
#define DoFloatTest DoFloatTest_scanf_s

#endif
