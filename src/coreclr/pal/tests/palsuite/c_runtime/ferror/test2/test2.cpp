// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Open a read-only file and attempt to write some data to it.
** Check to ensure that an ferror occurs.
**
** Depends:
**     fopen
**     fwrite
**     fclose
**     
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_ferror_test2_paltest_ferror_test2, "c_runtime/ferror/test2/paltest_ferror_test2")
{
    const char filename[] = "testfile";
    FILE * fp = NULL;
    int result;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Open a file in READONLY mode */
  
    if((fp = fopen(filename, "r")) == NULL)
    {
        Fail("Unable to open a file for reading.");
    }

    /* Attempt to write 14 characters to the file. */
  
    if((result = fwrite("This is a test",1,14,fp)) != 0)
    {
        Fail("ERROR: %d characters written.  0 characters should "
             "have been written, since this file is read-only.", result);
    }
  
    if(ferror(fp) == 0)
    {
        Fail("ERROR:  ferror should have generated an error when "
             "write was called on a read-only file.  But, it "
             "retured 0, indicating no error.\n");
    }
  
    /* Close the file. */

    if(fclose(fp) != 0)
    {
        Fail("ERROR: fclose failed when trying to close a file pointer. "
	     "This test depends on fclose working properly.");
    }

   
    PAL_Terminate();
    return PASS;
}
   

