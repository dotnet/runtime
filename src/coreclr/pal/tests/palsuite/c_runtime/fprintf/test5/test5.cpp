// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test5.c (fprintf)
**
** Purpose:     Tests the count specifier (%n).
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

static void DoTest(char *formatstr, int param, char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };
    int n = -1;
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: fprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }
    
    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
 param, n);
    }

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }
    

    if ((fclose( fp )) != 0)

    {

        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");

    }
}

static void DoShortTest(char *formatstr, int param, char *checkstr)
{
    FILE *fp;
    char buf[256] = { 0 };
    short int n = -1;
    
    if ((fp = fopen("testfile.txt", "w+")) == NULL )
    {
        Fail("ERROR: fopen failed to create testfile\n");
    }

    if ((fprintf(fp, formatstr, &n)) < 0)
    {
        Fail("ERROR: fprintf failed\n");
    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
        Fail("ERROR: fseek failed\n");
    }

    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fseek failed\n");
    }
    
    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
 param, n);
    }

    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }

    if ((fclose( fp )) != 0)
    {
        Fail("ERROR: fclose failed to close \"testfile.txt\"\n");
    }
}

PALTEST(c_runtime_fprintf_test5_paltest_fprintf_test5, "c_runtime/fprintf/test5/paltest_fprintf_test5")
{    
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoTest("foo %n bar", 4, "foo  bar");
    DoTest("foo %#n bar", 4, "foo  bar");
    DoTest("foo % n bar", 4, "foo  bar");
    DoTest("foo %+n bar", 4, "foo  bar");
    DoTest("foo %-n bar", 4, "foo  bar");
    DoTest("foo %0n bar", 4, "foo  bar");
    DoShortTest("foo %hn bar", 4, "foo  bar");
    DoTest("foo %ln bar", 4, "foo  bar");
    DoTest("foo %Ln bar", 4, "foo  bar");
    DoTest("foo %I64n bar", 4, "foo  bar");
    DoTest("foo %20.3n bar", 4, "foo  bar");

    PAL_Terminate();
    return PASS;
}
