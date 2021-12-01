// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests the PAL implementation of the _wfopen function. 
**          Test to ensure that you can write to a 'w+' mode file.
**          And that you can read from a 'w+' mode file.
**
** Depends:
**      fprintf
**      fseek
**      fgets
**  

**
**===================================================================*/

#define UNICODE                                                              
  
#include <palsuite.h>

PALTEST(c_runtime__wfopen_test3_paltest_wfopen_test3, "c_runtime/_wfopen/test3/paltest_wfopen_test3")
{
  
    FILE *fp;
    char buffer[128];
    WCHAR filename[] = {'t','e','s','t','f','i','l','e','\0'};
    WCHAR writeplus[] = {'w','+','\0'};

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Open a file with 'w+' mode */
    if( (fp = _wfopen( filename, writeplus )) == NULL )
    {
        Fail( "ERROR: The file failed to open with 'w+' mode.\n" );
    }  
  
    /* Write some text to the file */
    if(fprintf(fp,"%s","some text") <= 0) 
    {
        Fail("ERROR: Attempted to WRITE to a file opened with 'w+' mode "
               "but fprintf failed.  Either fopen or fprintf have problems.");
    }

    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }
  
    /* Attempt to read from the 'w+' only file, should pass */
    if(fgets(buffer,10,fp) == NULL)
    {
        Fail("ERROR: Tried to READ from a file with 'w+' mode set. "
               "This should succeed, but fgets returned NULL.  Either fgets "
               "or fopen is broken.");
    }

    PAL_Terminate();
    return PASS;
}
   

