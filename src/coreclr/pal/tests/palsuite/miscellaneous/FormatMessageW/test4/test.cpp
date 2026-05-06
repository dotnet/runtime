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


PALTEST(miscellaneous_FormatMessageW_test4_paltest_formatmessagew_test4, "miscellaneous/FormatMessageW/test4/paltest_formatmessagew_test4")
{
    WCHAR OutBuffer[1024];
    
    WCHAR *  TheString;
    WCHAR* TheArray[3];
    int ReturnResult;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    TheString = convert("Pal %1 %2 %3 Testing");
    TheArray[0] = convert("Foo");
    TheArray[1] = convert("Bar");
    TheArray[2] = convert("FooBar");

    /* This should just use the 3 strings in the array to replace
       inserts in the given string.
    */

    ReturnResult = FormatMessage(
        FORMAT_MESSAGE_FROM_STRING | 
        FORMAT_MESSAGE_ARGUMENT_ARRAY,      /* source and processing options */
        TheString,                       /* message source */
        0,                               /* message identifier */
        0,                               /* language identifier */
        OutBuffer,                       /* message buffer */
        1024,                            /* maximum size of message buffer */
        (va_list *) TheArray             /* array of message inserts */
        );
  
    if(ReturnResult == 0) 
    {
        Fail("ERROR: The return value was 0, which indicates failure.  "
             "The function failed when trying to Format a simple string, "
             "usin gthe ARGUMENT_ARRAY flag.");
    }
  
    if(memcmp(OutBuffer,
              convert("Pal Foo Bar FooBar Testing"),
              wcslen(OutBuffer)*2+2) != 0) 
    {
        Fail("ERROR:  Since the FORMAT_MESSAGE_ARGUMENT_ARRAY flag was set, "
             "the result should have been 'Pal Foo Bar FooBar Testing' but was"
             " really '%s'.",convertC(OutBuffer));
    }
  
    
    PAL_Terminate();
    return PASS;
 
}


