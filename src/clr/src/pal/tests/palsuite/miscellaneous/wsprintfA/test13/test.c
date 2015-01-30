//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
char buf[256];

BOOL test1()
{
    int pos = 0x1234ab;

    /* Test 1 */
    wsprintf(buf, "foo %X", pos);
    if (memcmp(buf, "foo 1234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 1) Failed. The correct string is "
            " 'foo 1234AB'  and the result returned was ";
        return FAIL;
    }

    /* Test 2 */
    wsprintf(buf, "foo %lX", pos);
    if (memcmp(buf, "foo 1234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 2) Failed. The correct string is "
            "'foo 1234AB' and the result returned was ";
        return FAIL;
    }


    /* Test 3 */
    wsprintf(buf, "foo %7X", pos);
    if (memcmp(buf, "foo  1234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 3) Failed. The correct string is "
            "'foo  1234AB' and the result returned was ";
        return FAIL;
    }

    /* Test 4 */
    wsprintf(buf, "foo %-7X", pos);
    if (memcmp(buf, "foo 1234AB ", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 4) Failed. The correct string is "
            "'foo 1234AB' and the result returned was ";
        return FAIL;
    }

    /* Test 5 */
    wsprintf(buf, "foo %.1X", pos);
    if (memcmp(buf, "foo 1234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 5) Failed. The correct string is "
            "'foo 1234AB' and the result returned was ";
        return FAIL;
    }

    /* Test 6 */
    wsprintf(buf, "foo %.7X", pos);
    if (memcmp(buf, "foo 01234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 6) Failed. The correct string is "
            "'foo 01234AB' and the result returned was ";
        return FAIL;
    }

    /* Test 7 */
    wsprintf(buf, "foo %07X", pos);
    if (memcmp(buf, "foo 01234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 7) Failed. The correct string is "
            "'foo 01234AB' and the result returned was ";
        return FAIL;
    }

    /* Test 8 */
    wsprintf(buf, "foo %#X", pos);
    if (memcmp(buf, "foo 0X1234AB", strlen(buf) + 1) != 0)
    {
        ErrorMessage = "ERROR: (Test 8) Failed. The correct string is "
            "'foo 0X1234AB' and the result returned was ";
        return FAIL;
    }

    return PASS;
}

int __cdecl main(int argc, char *argv[])
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
       return FAIL;
    }

    if(test1())
    {
       Fail("%s '%s'\n",ErrorMessage,buf);
    }

  PAL_Terminate();
  return PASS;

}


