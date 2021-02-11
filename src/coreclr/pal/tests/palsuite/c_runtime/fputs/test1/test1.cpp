// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose:  Call fputs twice and write two strings to a file.  Then
** call fread on the file and check that the data which was written is what
** we expect it to be.
**  

**
**===================================================================*/

                                 
#include <palsuite.h>


PALTEST(c_runtime_fputs_test1_paltest_fputs_test1, "c_runtime/fputs/test1/paltest_fputs_test1")
{
    
    FILE* TheFile;
    char* StringOne = "FooBar";
    char* StringTwo = "BarFoo";
    char* CompleteString = "FooBarBarFoo";
    char ReadBuffer[64];
    int ret;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Open the file that we'll be working with */

    TheFile = fopen("TestFile", "w+");
  
    if(TheFile == NULL)
    {
        Fail("ERROR: fopen failed to open the file 'TestFile' in read/write "
             "mode.\n");
    }

    /* Call fputs twice to write two strings to the file stream */
    
    if(fputs(StringOne, TheFile) < 0)
    {
        Fail("ERROR: fputs returned a negative value when attempting to "
             "put the string '%s' to the file.\n",StringOne);
    }
    
    if(fputs(StringTwo, TheFile) < 0)
    {
        Fail("ERROR: fputs returned a negative value when attempting to "
             "put the string '%s' to the file.\n",StringTwo);
    }
    
    /* Flush the buffers */
    if(fflush(TheFile) != 0)
    {
        Fail("ERROR: fflush failed to properly flush the buffers.\n");
    }

    /* Now read from the file to ensure the data was written correctly. 
       Note: We read more than what was written to make sure nothing extra
       was written.
    */

    if(fseek(TheFile, 0, SEEK_SET) != 0)
    {
        Fail("ERROR: fseek failed to set the file pointer back to the start "
             "of the file.\n");
    }


    if((ret = fread(ReadBuffer, 1, 20, TheFile)) != 12)
    {
        Fail("ERROR: fread should have returned that it read in 12 characters "
             "from the file, but instead it returned %d.\n", ret);
    }

    ReadBuffer[ret] = '\0';

    if(strcmp(ReadBuffer, CompleteString) != 0)
    {
        Fail("ERROR: The data read back from the file is not exactly the same "
             "as the data that was written by fputs.  The file contains '%s' "
             "instead of '%s'.\n",ReadBuffer, CompleteString);
    }

    if(fclose(TheFile) != 0)
    {
        Fail("ERROR: fclose failed to close the file stream.\n");
    }
     
    PAL_Terminate();
    return PASS;
}
