// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CloseHandle function
**
**
**=========================================================*/

/* Depends on: CreateFile and WriteFile */

#include <palsuite.h>

PALTEST(miscellaneous_CloseHandle_test1_paltest_closehandle_test1, "miscellaneous/CloseHandle/test1/paltest_closehandle_test1")
{

    HANDLE FileHandle = NULL;
    LPDWORD WriteBuffer; /* Used with WriteFile */

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    WriteBuffer = (LPDWORD)malloc(sizeof(WORD));
    
    if ( WriteBuffer == NULL )
    {
        Fail("ERROR: Failed to allocate memory for WriteBuffer pointer. "
             "Can't properly exec test case without this.\n");
    }
                                     
 
    /* Create a file, since this returns to us a HANDLE we can use */
    FileHandle = CreateFile("testfile",   
                            GENERIC_READ | GENERIC_WRITE,0,NULL,CREATE_ALWAYS,  
                            FILE_ATTRIBUTE_NORMAL,          
                            NULL);
  
    /* Should be able to close this handle */
    if(CloseHandle(FileHandle) == 0)
    {
	free(WriteBuffer);
        Fail("ERROR: (Test 1) Attempted to close a HANDLE on a file, but the "
             "return value was <=0, indicating failure.\n");    
    }
  
    free(WriteBuffer);
    
    PAL_Terminate();
    return PASS;
}




