// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests the PAL implementation of the fread function.
**          Open a file in READ mode, and then try to read all
**          the characters, more than all the characters,
**          0 characters and 0 sized characters and check that
**          the strings read in are correct.
**
** Depends:
**     fopen
**     fseek
**     fclose
**     strcmp
**     memset
**
**
**===================================================================*/

/* Note: testfile should exist in the directory with 15 characters 
   in it ... something got lost if it isn't here.
*/

/* Note: The behaviour in win32 is to crash if a NULL pointer is passed to
   fread, so the test to check that it returns 0 has been removed.
*/

#include <palsuite.h>

PALTEST(c_runtime_fread_test2_paltest_fread_test2, "c_runtime/fread/test2/paltest_fread_test2")
{
    const char filename[] = "testfile";
    char buffer[128];
    FILE * fp = NULL;

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
       in it. Then check to see that the data read in is correct.
       Note: The 'testfile' should have "This is a test." written in it.
    */
    memset(buffer,'\0',128);
    fread(buffer,1,15,fp);

    if(strcmp(buffer,"This is a test.") != 0)
    {
	Fail("ERROR: The data read in should have been "
	     "'This is a test.' but, the buffer contains '%s'.",
	     buffer);
    }
    
    /* Go back to the start of the file */

    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }

    /* Attempt to read 17 characters. The same 15 characters should
       be in the buffer.
    */
    
    memset(buffer,'\0',128);
    fread(buffer,1,17,fp);

    if(strcmp(buffer,"This is a test.") != 0)
    {
	Fail("ERROR: The data read in should have been "
	     "'This is a test.' but, the buffer contains '%s'.",
	     buffer);
    }
      
    /* Back to the start of the file */
  
    if(fseek(fp, 0, SEEK_SET)) 
    {
        Fail("ERROR: fseek failed, and this test depends on it.");
    }

    /* Read 0 characters and ensure the buffer is empty */
    
    memset(buffer,'\0',128);
    fread(buffer,1,0,fp);
     
    if(strcmp(buffer,"\0") != 0)
    {
	Fail("ERROR: The data read in should have been "
	     "NULL but, the buffer contains '%s'.",
	     buffer);
    }
   
    /* Read characters of 0 size and ensure the buffer is empty */
    
    memset(buffer,'\0',128);
    fread(buffer,0,5,fp);
   
    if(strcmp(buffer,"\0") != 0)
    {
	Fail("ERROR: The data read in should have been "
	     "NULL but, the buffer contains '%s'.",
	     buffer);
    }
   
    /* Close the file */

    if(fclose(fp))
    {
        Fail("ERROR: fclose failed.  Test depends on it.");
    }

    /* Read 5 characters of 1 size from a closed file pointer 
       and ensure the buffer is empty 
    */
    memset(buffer,'\0',128);
    fread(buffer,1,5,fp);
    if(strcmp(buffer,"\0") != 0)
    {
	Fail("ERROR: The data read in should have been "
	     "NULL but, the buffer contains '%s'.",
	     buffer);
    }

    PAL_Terminate();
    return PASS;
}
   

