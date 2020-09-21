// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Calls fgets to read a full line from a file.  A maximum length
**          parameter greater than the length of the line is passed.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_fgets_test2_paltest_fgets_test2, "c_runtime/fgets/test2/paltest_fgets_test2")
{
    const char outBuf1[] = "This is a test.\n";  
    const char outBuf2[] = "This is too.";

    char inBuf[sizeof(outBuf1) + sizeof(outBuf2)];
    const char filename[] = "testfile.tmp";
    const int offset = 5;  /*value chosen arbitrarily*/
    int expectedLen;
    int actualLen;

    FILE * fp;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*write the file that we will use to test */
    fp = fopen(filename, "w"); 
    if (fp == NULL)
    {
        Fail("Unable to open file for write.\n");
    }
  
    fwrite(outBuf1, sizeof(outBuf1[0]), sizeof(outBuf1), fp);
    fwrite(outBuf2, sizeof(outBuf2[0]), sizeof(outBuf2), fp);

    if (fclose(fp) != 0) 
    {
        Fail("error closing stream opened for write.\n");
    }

    /*Read until the first linebreak*/
    fp = fopen(filename, "r");
    if (fp == NULL)
    {
        Fail("Unable to open file for read.\n");
    }
  
  
    if (fgets(inBuf, sizeof(outBuf1) + offset , fp) != inBuf)
    {
        Fail("Error reading from file using fgets.\n");
    }

    /*note: -1 because strlen returns the length of a string _not_
      including the NULL, while fgets returns a string of specified
      maximum length _including_ the NULL.*/
    expectedLen = strlen(outBuf1);  
    actualLen = strlen(inBuf);
    if (actualLen > expectedLen)
    {
        Fail("fgets() was asked to read the first line of a file, but did "
             "not stop at the end of the line.\n");
    }
    else if (actualLen < expectedLen)
    {
        Fail("fgets() was asked to read the first line of a file, but did "
             "not read the entire line.\n");
    }
    else if (memcmp(inBuf, outBuf1, actualLen) != 0)
    {
        /*We didn't read back exactly outBuf1*/
        Fail("fgets() was asked to read the first line of a file.  It "
             "has read back a string of the correct length, but the"
             " contents are not correct.\n");
    }
    
    if (fclose(fp) != 0)
    {
        Fail("Error closing file after using fgets().\n");
    }

    PAL_Terminate();
    return PASS;

}

  
