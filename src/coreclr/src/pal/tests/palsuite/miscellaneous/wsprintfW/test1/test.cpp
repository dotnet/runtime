// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for wsprintfW() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>


char * ErrorMessage = NULL; 
WCHAR * BadResult = NULL; 

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with strlen */


BOOL test1()
{
    char checkstr[] = "hello world";

    WCHAR buf[256];

    /* Test a simple string */
    wsprintf(buf, convert("hello world"));

    /* Error message */
    ErrorMessage = "ERROR: (Test 1) Failed on 'hello world' test. "
        "The correct string is 'hello world' and the result returned was ";
    BadResult = buf;

    return (memcmp(convert(checkstr), buf, wcslen(buf)*2+2) != 0);
}

int __cdecl main(int argc, char *argv[]) {

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if(test1()) {
        Fail("%s '%s'\n",ErrorMessage,convertC(BadResult));

    }
  
    PAL_Terminate();  
    return PASS;

}



