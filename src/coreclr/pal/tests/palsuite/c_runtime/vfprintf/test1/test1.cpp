// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the vfprintf function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vfprintf.h"

PALTEST(c_runtime_vfprintf_test1_paltest_vfprintf_test1, "c_runtime/vfprintf/test1/paltest_vfprintf_test1")
{
    FILE *fp;
    char testfile[] = "testfile.txt";
    char buf[256];
    char checkstr[] = "hello world";
    int ret;


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    if ((fp = fopen(testfile, "w+")) == NULL)
    {
        Fail("ERROR: fopen failed to create \"%s\"\n", testfile);
    }

    ret = DoVfprintf(fp, "hello world");

    if (ret != strlen(checkstr))
    {
        Fail("Expected vfprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);

    }

    if ((fseek(fp, 0, SEEK_SET)) != 0)
    {
         Fail("ERROR: Fseek failed to set pointer to beginning of file\n" );
    }
 
    if ((fgets(buf, 100, fp)) == NULL)
    {
        Fail("ERROR: fgets failed\n");
    }    
    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected %s, got %s\n", checkstr, buf);
    }
    if ((fclose(fp)) != 0)
    {
        Fail("ERROR: fclose failed to close \"%s\"\n", testfile);
    }

    PAL_Terminate();
    return PASS;
}
