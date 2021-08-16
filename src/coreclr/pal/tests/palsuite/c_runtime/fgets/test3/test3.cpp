// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Tries to read from an empty file using fgets(), to verify
**          handling of EOF condition.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_fgets_test3_paltest_fgets_test3, "c_runtime/fgets/test3/paltest_fgets_test3")
{
    char inBuf[10];
    const char filename[] = "testfile.tmp";

    FILE * fp;
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*write the empty file that we will use to test */
    fp = fopen(filename, "w");
    if (fp == NULL)
    {
        Fail("Unable to open file for write.\n");
    }

    /*Don't write anything*/
  
    if (fclose(fp) != 0) 
    {
        Fail("Error closing stream opened for write.\n");
    }


    /*Open the file and try to read.*/
    fp = fopen(filename, "r");
    if (fp == NULL)
    {
        Fail("Unable to open file for read.\n");
    }

  
    if (fgets(inBuf, sizeof(inBuf) , fp) != NULL)
    {
        /*NULL could also mean an error condition, but since the PAL
          doesn't supply feof or ferror, we can't distinguish between
          the two.*/
        Fail("fgets doesn't handle EOF properly.  When asked to read from "
             "an empty file, it didn't return NULL as it should have.\n");
    }

    if (fclose(fp) != 0)
    {
        Fail("Error closing an empty file after trying to use fgets().\n");
    }
    PAL_Terminate();
    return PASS;

}

  



