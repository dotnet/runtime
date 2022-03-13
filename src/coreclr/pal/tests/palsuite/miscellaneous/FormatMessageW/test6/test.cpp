// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for FormatMessageW() function
**
**
**=========================================================*/


#define UNICODE
#include <palsuite.h>


PALTEST(miscellaneous_FormatMessageW_test6_paltest_formatmessagew_test6, "miscellaneous/FormatMessageW/test6/paltest_formatmessagew_test6")
{


    LPWSTR OutBuffer;
    int ReturnResult;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


    /* This is testing the use of FROM_SYSTEM.  We can't check to ensure
       the error message it extracts is correct, only that it does place some
       information into the buffer when it is called.
    */

    /*

        ERROR_SUCCESS (0L) is normally returned by GetLastError,
        But, the  ERROR_SUCCESS is removed from messages for Unix based Systems
        To ensure that we have some information into the buffer we are using the message
        identifier value 2L (ERROR_FILE_NOT_FOUND)
    */
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_ALLOCATE_BUFFER,  /* source and processing options */
        NULL,                            /* message source */
        2L,                              /* message identifier */
        0,                               /* language identifier */
        (LPWSTR)&OutBuffer,              /* message buffer */
        0,                               /* maximum size of message buffer */
        NULL                            /* array of message inserts */
        );

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure. The "
             "function failed when trying to Format a FROM_SYSTEM message.");
    }

    if(wcslen(OutBuffer) <= 0)
    {
        Fail("ERROR: There are no characters in the buffer, and when the "
             "FORMAT_MESSAGE_FROM_SYSTEM flag is used with ERROR_FILE_NOT_FOUND error, "
             "something should be put into the buffer.");
    }

    free(OutBuffer);

    PAL_Terminate();
    return PASS;

}


