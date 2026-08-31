// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentStringsW() function
**
**
**=========================================================*/

/* Depends on SetEnvironmentVariable(), wcsstr() and wcslen() */

#define UNICODE
#include <palsuite.h>

PALTEST(miscellaneous_GetEnvironmentStringsW_test1_paltest_getenvironmentstringsw_test1, "miscellaneous/GetEnvironmentStringsW/test1/paltest_getenvironmentstringsw_test1")
{

    LPWSTR CapturedEnvironmentStrings = NULL;
    LPWSTR EnviroStringReturned = NULL;
    WCHAR EnvironmentVariableBuffer[] =
        {'P','A','L','T','E','S','T','I','N','G','\0'};
    WCHAR EnvironmentValueBuffer[] = {'T','e','s','t','i','n','g','\0'};

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* This test depends on SetEnvironmentVariableW working.
       We need to set a variable so we can test and ensure it's there
       when we get them back
    */

    SetEnvironmentVariable(EnvironmentVariableBuffer,EnvironmentValueBuffer);

    CapturedEnvironmentStrings = GetEnvironmentStrings();

    /* If it's pointing to NULL, it failed. */
    if(CapturedEnvironmentStrings == NULL) {
        Fail("The function returned a pointer to NULL, which it shouldn't do. "
             "It should point to a block of Environment Strings.\n");
    }

    /* Now that we've grabbed the list of environment strings, go through
       each one, and check for a match to 'PALTESTING'.  If this is missing
       it's not pointing at the environment block.
    */

    while(*CapturedEnvironmentStrings != 0)
    {
        EnviroStringReturned = wcsstr(CapturedEnvironmentStrings,
                                      EnvironmentVariableBuffer);
        CapturedEnvironmentStrings += wcslen(CapturedEnvironmentStrings)+1;
        if(EnviroStringReturned != NULL)
        {
            break;
        }
    }

    if(EnviroStringReturned == NULL)
    {
        Fail("The memory block returned was searched, but nothing was found to "
             "prove this was really the environment block.  Either this "
             "function, SetEnvironmentVariable or wcsstr() is broken.\n");
    }


    PAL_Terminate();
    return PASS;
}




