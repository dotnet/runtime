// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CharNextA, ensures it returns an LPTSTR
**
**
**=========================================================*/

/* Depends on strcmp() */

#include <palsuite.h>

void testString(LPSTR input, LPSTR expected)
{    

    LPTSTR pReturned = NULL;

    pReturned = CharNextA(input);

    /* Compare the Returned String to what it should be */
    if(strcmp(expected,pReturned) != 0) 
    {
        Fail("ERROR: CharNextA Failed: [%s] and [%s] are not equal, "
            "they should be after calling CharNextA.\n",
            pReturned,expected);
    }


}

int __cdecl main(int argc, char *argv[]) 
{

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
      return FAIL;
    }
  
    /* test several Strings */
    testString("this is the string", "his is the string");  
    testString("t", "");  
    testString("", ""); 
    testString("a\t", "\t"); 
    testString("a\a", "\a");
    testString("a\b", "\b");
    testString("a\"", "\"");
    testString("a\\", "\\");
    testString("\\", "");
    testString("\f", "");
    testString("\b", "");
    
    PAL_Terminate();
    return PASS;
}




