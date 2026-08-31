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


PALTEST(miscellaneous_FormatMessageW_test3_paltest_formatmessagew_test3, "miscellaneous/FormatMessageW/test3/paltest_formatmessagew_test3")
{
    WCHAR OutBuffer[1024];

    WCHAR *  TheString; 
    WCHAR * CorrectString;
    int ReturnResult;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    TheString = convert("Pal %1!u! %2!i! %3!s! Testing");

    /* The resulting value in the buffer shouldn't be formatted at all, 
       because the inserts are being ignored.
    */
  
    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING | 
        FORMAT_MESSAGE_IGNORE_INSERTS,    /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        NULL                             /* array of message inserts */
        );
  
  
  
    if(ReturnResult == 0) 
    {
        free(TheString);
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "using the IGNORE_INSERTS flag.\n");    
    }
  

    /* Note: Since 's' is the default insert, when this function is called
       with ignore inserts, it strips %3!s! down to just %3 -- as they're
       equal.
    */
    if(memcmp(OutBuffer,
              (CorrectString = convert("Pal %1!u! %2!i! %3 Testing")),
              wcslen(OutBuffer)*2+2) != 0) 
    {   
        free(TheString);
        free(CorrectString);
        Fail("ERROR:  Since the IGNORE_INSERTS flag was set, the result "
             "should have been 'Pal %%1!u! %%2!i! %%3 Testing' but was "
             "really '%S'.\n",OutBuffer);
    }
   
    free(TheString);
    free(CorrectString);
    PAL_Terminate();
    return PASS;
 
}


