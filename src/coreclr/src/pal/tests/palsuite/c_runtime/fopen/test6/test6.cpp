// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test6.c
**
** Purpose: Tests the PAL implementation of the fopen function. 
**          Test to ensure that you can write to an 'a' mode file.
**          And that you can't read from a 'a' mode file. Also ensure
**          that you can use fseek and still write to the end of a file.
**
** Depends:
**      fprintf
**      fgets
**      fseek
**      fclose
**  

**
**===================================================================*/
                                                                         
#include <palsuite.h>

PALTEST(c_runtime_fopen_test6_paltest_fopen_test6, "c_runtime/fopen/test6/paltest_fopen_test6")
{
  
    FILE *fp;
    char buffer[128];
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Open a file with 'a+' mode */
    if( (fp = fopen( "testfile", "a" )) == NULL )
    {
        Fail( "ERROR: The file failed to open with 'a' mode.\n" );
    }  
    
    /* Write some text to the file */
    if(fprintf(fp,"%s","some text") <= 0) 
    {
        Fail("ERROR: Attempted to WRITE to a file opened with 'a' mode "
               "but fprintf failed.  Either fopen or fprintf have problems.");
    }
  
    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }
  
    /* Attempt to read from the 'a' only file, should fail */
    if(fgets(buffer,10,fp) != NULL)
    {
        Fail("ERROR: Tried to READ from a file with 'a' mode set. "
               "This should fail, but fgets returned success.  Either fgets "
               "or fopen is broken.");
    }

    
    /* Attempt to write to a file after using 'a' and fseek */
    fp = fopen("testfile2", "a");
    if(fp == NULL)
    {
        Fail("ERROR: The file failed to be created with 'a' mode.\n");
    }

    /* write text to the file initially */
    if(fprintf(fp,"%s","abcd") <= 0)
    {
        Fail("ERROR: Attempted to WRITE to a file opened with 'a' mode "
            "but fprintf failed.  Either fopen or fprintf have problems.\n");
    }

    /* set the pointer to the front of the file */
    if(fseek(fp, 0, SEEK_SET))
    {
        Fail("ERROR: fseek failed, and this test depends on it.\n");
    }

    /* using 'a' should still write to the end of the file, not the front */
    if(fputs("efgh",fp) < 0)
    {
        Fail("ERROR: Attempted to WRITE to a file opened with 'a' mode "
            "but fputs failed.\n");
    }

    /* set the pointer to the front of the file */
    if(fseek(fp, 0, SEEK_SET))
    {
        Fail("ERROR: fseek failed, and this test depends on it.\n");
    }

    /* a file with 'a' mode can only write, so close the file before reading */
    if(fclose(fp))
    {
        Fail("ERROR: fclose failed when it should have succeeded.\n");
    }

    /* open the file again to read */
    fp = fopen("testfile2","r");
    if(fp == NULL)
    {
        Fail("ERROR: fopen failed to open the file using 'r' mode");
    }

    /* Attempt to read from the 'a' only file, should succeed */
    if(fgets(buffer,10,fp) == NULL)
    {
        Fail("ERROR: Tried to READ from a file with 'a' mode set. "
               "This should pass, but fgets returned failure.  Either fgets "
               "or fopen is broken.\n");
    }

    /* Compare what was read and what should have been in the file */
    if(memcmp(buffer,"abcdefgh",8))
    {
        Fail("ERROR: The string read should have equaled 'abcdefgh' "
            "but instead it is %s\n", buffer);
    }


    PAL_Terminate();
    return PASS;
}
   

