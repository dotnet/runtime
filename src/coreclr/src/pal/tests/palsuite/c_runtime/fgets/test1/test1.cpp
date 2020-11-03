// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Writes a simple file and calls fgets() to get a string shorter
**          than the first line of the file.  Verifies that the correct
**          string is returned.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_fgets_test1_paltest_fgets_test1, "c_runtime/fgets/test1/paltest_fgets_test1")
{
    const char outBuf1[] = "This is a test.\n";
    const char outBuf2[] = "This is too.";
    char inBuf[sizeof(outBuf1) + sizeof(outBuf2)];
    const char filename[] = "testfile.tmp";
    const int offset = 5;  /* value chosen arbitrarily */
    int actualLen;
    int expectedLen;
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
        Fail("Error closing a file opened for write.\n");
    }


    /*now read back the entire first string*/
    fp = fopen(filename, "r"); 
    if (fp == NULL)
    {
        Fail("Unable to open file for read.\n");
    }

    /*note: +1 because strlen() returns the length of a string _not_
      including the NULL, while fgets() returns a string of specified
      maximum length _including_ the NULL.*/
    if (fgets(inBuf, strlen(outBuf1) - offset + 1, fp) != inBuf)
    {
        Fail("Error reading from file using fgets.\n");
    }


    expectedLen = strlen(outBuf1) - offset;
    actualLen = strlen(inBuf);

    if (actualLen < expectedLen)
    {
        Fail("fgets() was asked to read a one-line string and given the "
             "length of the string as a parameter.  The string it has "
             "read is too short.\n");
    }
    if (actualLen > expectedLen)
    {
        Fail("fgets() was asked to read a one-line string and given the "
             "length of the string as a parameter.  The string it has "
             "read is too long.\n");
    }
    if (memcmp(inBuf, outBuf1, actualLen) != 0)
    {
        /*We didn't read back exactly outBuf1*/
        Fail("fgets() was asked to read a one-line string, and given the "
             "length of the string as an parameter.  It has returned a "
             "string of the correct length, but the contents are not "
             "correct.\n");
    }
    
    if (fclose(fp) != 0)
    {
        Fail("Error closing file after using fgets().\n");
    }


    PAL_Terminate();
    return PASS;

}

  
