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

WCHAR OutBuffer_FormatMessageW_test2[1024];

/* Pass this test the string "INSERT" and it will succeed */

int test1(int num, ...)
{

    WCHAR * TheString = convert("Pal %1 Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);
    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult == 0)
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string,"
             " with the 's' formatter.");

    }

    if(memcmp(OutBuffer_FormatMessageW_test2, convert("Pal INSERT Testing"),
              wcslen(OutBuffer_FormatMessageW_test2)*2+2) != 0)
    {
        Fail("ERROR:  The formatted string should have been 'Pal INSERT "
             "Testing' but '%s' was returned.",
             convertC(OutBuffer_FormatMessageW_test2));
    }


    return PASS;
}

int test2(int num, ...)
{

    WCHAR * TheString = convert("Pal %1!i! Testing");
    int ReturnResult;
    va_list TheList;
    va_start(TheList,num);

    memset( OutBuffer_FormatMessageW_test2, 0, 1024 * sizeof(OutBuffer_FormatMessageW_test2[0]) );

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer_FormatMessageW_test2,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        &TheList                             /* array of message inserts */
        );

    va_end(TheList);

    if(ReturnResult != 0)
    {
        Fail("ERROR: The return value was non-0, which indicates success.  "
             "The function should fail when trying to Format with embedded printf formats.");
    }

    return PASS;
}

PALTEST(miscellaneous_FormatMessageW_test2_paltest_formatmessagew_test2, "miscellaneous/FormatMessageW/test2/paltest_formatmessagew_test2")
{
    WCHAR szwInsert[] = {'I','N','S','E','R','T','\0'};

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if(test1(0,szwInsert) ||    /* Test %s */
       test2(0,40))             /* Test embedded printf format fails */
    {


    }

    PAL_Terminate();
    return PASS;

}



