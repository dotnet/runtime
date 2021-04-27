// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Write a short string to a file and check that it was written
**          properly.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_fwrite_test1_paltest_fwrite_test1, "c_runtime/fwrite/test1/paltest_fwrite_test1")
{
    const char filename[] = "testfile.tmp";
    const char outBuffer[] = "This is a test.";
    char inBuffer[sizeof(outBuffer) + 10];
    int itemsExpected;
    int itemsWritten;
    FILE * fp = NULL;
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    if((fp = fopen(filename, "w")) == NULL)
    {
        Fail("Unable to open a file for write.\n");
    }

    itemsExpected = sizeof(outBuffer);
    itemsWritten = fwrite(outBuffer, 
                          sizeof(outBuffer[0]), 
                          sizeof(outBuffer), 
                          fp);  
    
    if (itemsWritten == 0)
    {
        if(fclose(fp) != 0)
        {
            Fail("fwrite: Error occurred during the closing of a file.\n");
        }

        Fail("fwrite() couldn't write to a stream at all\n");
    }
    else if (itemsWritten != itemsExpected) 
    {
        if(fclose(fp) != 0)
        {
            Fail("fwrite: Error occurred during the closing of a file.\n");
        }

        Fail("fwrite() produced errors writing to a stream.\n");
    }
      
    if(fclose(fp) != 0)
    {
        Fail("fwrite: Error occurred during the closing of a file.\n");
    }

    /* open the file to verify what was written to the file */
    if ((fp = fopen(filename, "r")) == NULL)
    {
        Fail("Couldn't open newly written file for read.\n");
    }

    if (fgets(inBuffer, sizeof(inBuffer), fp) == NULL)
    {
        if(fclose(fp) != 0)
        {
            Fail("fwrite: Error occurred during the closing of a file.\n");
        }

        Fail("We wrote something to a file using fwrite() and got errors"
             " when we tried to read it back using fgets().  Either "
             "fwrite() or fgets() is broken.\n");
    }

    if (strcmp(inBuffer, outBuffer) != 0)
    {
        if(fclose(fp) != 0)
        {
            Fail("fwrite: Error occurred during the closing of a file.\n");
        }

        Fail("fwrite() (or fgets()) is broken.  The string read back from"
             " the file does not match the string written.\n");
    }

    if(fclose(fp) != 0)
    {
        Fail("fwrite: Error occurred during the closing of a file.\n");
    }

    PAL_Terminate();
    return PASS;
}
   

