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


PALTEST(miscellaneous_FormatMessageW_test1_paltest_formatmessagew_test1, "miscellaneous/FormatMessageW/test1/paltest_formatmessagew_test1")
{

    WCHAR TheString[] = {'P','a','l',' ','T','e','s','t','\0'};
    WCHAR OutBuffer[128];
    int ReturnResult;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
 

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING, /* source and processing options */
        TheString,                  /* message source */
        0,                          /* message identifier */
        0,                          /* language identifier */
        OutBuffer,                  /* message buffer */
        1024,                       /* maximum size of message buffer */
        NULL                        /* array of message inserts */
        );
  
  
    if(ReturnResult == 0) 
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string"
             ", with no formatters in it.");      
    }
  
    if(memcmp(OutBuffer,TheString,wcslen(OutBuffer)*2+2) != 0) 
    {
        Fail("ERROR: The formatted string should be %s but is really %s.",
             convertC(TheString),
             convertC(OutBuffer));
    }

    PAL_Terminate();
    return PASS;
}


