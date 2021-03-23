// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test5.c
**
** Purpose: Tests the PAL implementation of the fopen function. 
**          Test to ensure that you can write to a 'r+' mode file.
**          And that you can read from a 'r+' mode file.
**
** Depends:
**      fprintf
**      fclose
**      fgets
**      fseek
**  

**
**===================================================================*/
                                     
#include <palsuite.h>

PALTEST(c_runtime_fopen_test5_paltest_fopen_test5, "c_runtime/fopen/test5/paltest_fopen_test5")
{
  
  FILE *fp;
  char buffer[128];
  
  if (PAL_Initialize(argc, argv))
  {
    return FAIL;
  }

  
  /* Open a file with 'w' mode */
  if( (fp = fopen( "testfile", "w" )) == NULL )
    {
      Fail( "ERROR: The file failed to open with 'w' mode.\n" );
    }  
  
  if(fclose(fp))
    {
      Fail("ERROR: Attempted to close a file, but fclose failed. "
         "This test depends upon it.");
    }

  if( (fp = fopen( "testfile", "r+" )) == NULL )
    {
      Fail( "ERROR: The file failed to open with 'r+' mode.\n" );
    } 
  
  /* Write some text to the file */
  if(fprintf(fp,"%s","some text") <= 0) 
    {
      Fail("ERROR: Attempted to WRITE to a file opened with 'r+' mode "
         "but fprintf failed.  Either fopen or fprintf have problems.");
    }
  
   if(fseek(fp, 0, SEEK_SET)) 
    {
      Fail("ERROR: fseek failed, and this test depends on it.");
    }
  
  /* Attempt to read from the 'r+' only file, should pass */
  if(fgets(buffer,10,fp) == NULL)
    {
      Fail("ERROR: Tried to READ from a file with 'r+' mode set. "
         "This should succeed, but fgets returned NULL.  Either fgets "
         "or fopen is broken.");
    }  

  PAL_Terminate();
  return PASS;
}
   

