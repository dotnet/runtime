// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the fread function.
**          Open a file in READ mode, and then try to read all
**          the characters, more than all the characters,
**          0 characters and 0 sized characters and check that
**          the return values are correct.
**
** Depends:
**     fopen
**     fseek
**     fclose
**
**
**===================================================================*/

/* Note: testfile should exist in the directory with 15 characters 
   in it ... something got lost if it isn't here.
*/

/* Note: Under win32, fread() crashes when passed NULL.  The test to ensure that
   it returns 0 has been removed to reflect this.
*/

#include <palsuite.h>

PALTEST(c_runtime_fread_test1_paltest_fread_test1, "c_runtime/fread/test1/paltest_fread_test1")
{
    const char filename[] = "testfile";
    char buffer[128];
    FILE * fp = NULL;
    int result;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }
  
    /* Open a file in READ mode */

    if((fp = fopen(filename, "r")) == NULL)
    {
        Fail("Unable to open a file for reading.  Is the file "
               "in the directory?  It should be.");
    }

    /* Read 15 characters from the file.  The file has exactly this many 
       in it.
    */
    if((result = fread(buffer,1,15,fp)) == 0)
    {
        Fail("ERROR: Zero characters read from the file.  It should have "
               "15 characters in it.");
    }

    if(result != 15)
    {
        Fail("ERROR: The fread function should have returned that it read "
               "in 15 characters from the file.  But it indicates having "
               "read %i characters.",result);
    }

    /* Go back to the start of the file */

    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }

    /* Attempt to read 17 characters, the return should still be 15 */

    if((result = fread(buffer,1,17,fp)) == 0)
    {
        Fail("ERROR: Zero characters read from the file.  It should have "
               "15 characters in it.  Though, it attempted to read 17.");
    }  
  
    if(result != 15)
    {
        Fail("ERROR: The fread function should have returned that it read "
               "in 15 characters from the file.  "
               "But it indicates having read  %i characters.",result);
    }
  
    /* Back to the start of the file */
  
    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }

    /* Read 0 characters and ensure the function returns 0 */

    if((result = fread(buffer,1,0,fp)) != 0)
    {
        Fail("ERROR: The return value should be 0, as we attempted to "
               "read 0 characters.");
    } 

    /* Read characters of 0 size and ensure the return value is 0 */

    if((result = fread(buffer,0,5,fp)) != 0)
    {
        Fail("ERROR: The return value should be 0, as we attempted to "
               "read 0 sized data.");
    } 

    /* Close the file */

    if(fclose(fp))
    {
        Fail("ERROR: fclose failed.  Test depends on it.");
    }

    /* Read 5 characters of 1 size from a closed file pointer 
       and ensure the return value is 0 
    */

    if((result = fread(buffer,1,5,fp)) != 0)
    {
        Fail("ERROR: The return value should be 0, as we attempted to "
               "read data from a closed file pointer.");
    } 

    PAL_Terminate();
    return PASS;
}
   

