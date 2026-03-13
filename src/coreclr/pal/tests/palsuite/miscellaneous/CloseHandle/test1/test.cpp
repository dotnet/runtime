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

/* Depends on: CreateFile */

#include <palsuite.h>

PALTEST(miscellaneous_CloseHandle_test1_paltest_closehandle_test1, "miscellaneous/CloseHandle/test1/paltest_closehandle_test1")
{

    HANDLE FileHandle = NULL;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Create a file, since this returns to us a HANDLE we can use */
    FileHandle = CreateFile("testfile",
                            GENERIC_READ | GENERIC_WRITE,0,NULL,CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL,
                            NULL);

    /* Should be able to close this handle */
    if(CloseHandle(FileHandle) == 0)
    {
        Fail("ERROR: (Test 1) Attempted to close a HANDLE on a file, but the "
             "return value was <=0, indicating failure.\n");
    }

    PAL_Terminate();
    return PASS;
}

