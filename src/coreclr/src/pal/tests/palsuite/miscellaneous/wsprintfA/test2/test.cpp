// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for wsprintfA() function
**
**
**=========================================================*/

#include <palsuite.h>

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with strlen */

char * ErrorMessage; 
char * BadResult;
char buf[256];

BOOL test1()
{

  
    /* Test 1 */
    wsprintf(buf, "foo %s", "bar");
    if (memcmp(buf, "foo bar", strlen(buf) + 1) != 0) 
    {
        ErrorMessage = "ERROR: (Test 1) Failed. The correct string is "
            "'foo bar' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }
  
    /* Test 2 */
    wsprintf(buf, "foo %hs", "bar");
    if (memcmp(buf, "foo bar", strlen(buf) + 1) != 0) 
    {
        ErrorMessage = "ERROR: (Test 2) Failed. The correct string is "
            "'foo bar' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }

  
    /* Test 3 */
    wsprintf(buf, "foo %5s", "bar");
    if (memcmp(buf, "foo   bar", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 3) Failed. The correct string is "
            "'foo   bar' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }

    /* Test 4 */
    wsprintf(buf, "foo %.2s", "bar");
    if (memcmp(buf, "foo ba", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 4) Failed. The correct string is "
            "'foo ba' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }

    /* Test 5 */
    wsprintf(buf, "foo %5.2s", "bar");
    if (memcmp(buf, "foo    ba", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 5) Failed. The correct string is "
            "'foo    ba' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }

    /* Test 6 */
    wsprintf(buf, "foo %-5s", "bar");
    if (memcmp(buf, "foo bar  ", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 6) Failed. The correct string is "
            "'foo bar' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }

    /* Test 7 */
    wsprintf(buf, "foo %05s", "bar");
    if (memcmp(buf, "foo 00bar", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 7) Failed. The correct string is "
            "'foo 00bar' and the result returned was ";
        BadResult = buf;
        return FAIL;
    }
    return PASS;
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

    if(test1())
    {
        Fail("%s '%s'\n",ErrorMessage,BadResult);
    }

    PAL_Terminate();
    return PASS;
  
}


