// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the ferror function.
**
** Depends:
**     fopen
**     fread
**     fclose
**     
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_ferror_test1_paltest_ferror_test1, "c_runtime/ferror/test1/paltest_ferror_test1")
{
    const char filename[] = "testfile";
    char buffer[128];
    FILE * fp = NULL;
    int result;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Open a file in READ mode */

    if((fp = fopen(filename, "r")) == NULL)
    {
        Fail("Unable to open a file for reading.  Is the file "
               "in the directory?  It should be.");
    }

    /* Read 10 characters from the file.  The file has 15 
       characters in it.
    */
  
    if((result = fread(buffer,1,10,fp)) == 0)
    {
        Fail("ERROR: Zero characters read from the file.  It should have "
               "read 10 character in from a 15 character file.");
    }
  
    if(ferror(fp) != 0)
    {
        Fail("ERROR:  ferror returned a value not equal to 0. The read "
               "operation shouldn't have caused an error, and ferror should "
               "return 0 still.");
    }
  
    /* 
       Close the open file and end the test.
    */

    if(fclose(fp) != 0)
    {
        Fail("ERROR: fclose failed when trying to close a file pointer. "
               "This test depends on fclose working properly.");
    }

    PAL_Terminate();
    return PASS;
}
   

