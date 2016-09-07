// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Search for a number of tokens within strings.  Check that the return values
** are what is expect, and also that the strings match up with our expected
** results.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    char str[] = "foo bar baz";
    char *result1= "foo \0ar baz";
    char *result2= "foo \0a\0 baz";
    int len = strlen(str) + 1;
    char *ptr;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ptr = strtok(str, "bz");
    if (ptr != str)
    {
        Fail("Expected strtok() to return %p, got %p!\n", str, ptr);
    }
    if (memcmp(str, result1, len) != 0)
    {
        Fail("strtok altered the string in an unexpeced way!\n");
    }

    ptr = strtok(NULL, "r ");
    if (ptr != str + 5)
    {
        Fail("Expected strtok() to return %p, got %p!\n", str+5, ptr);
    }
    if (memcmp(str, result2, len) != 0)
    {
        Fail("strtok altered the string in an unexpeced way!\n");
    }


    ptr = strtok(NULL, "X");
    if (ptr != str + 7)
    {
        Fail("Expected strtok() to return %p, got %p!\n", str + 7, ptr);
    }
    if (memcmp(str, result2, len) != 0)
    {
        Fail("strtok altered the string in an unexpeced way!\n");
    }

    ptr = strtok(NULL, "X");
    if (ptr != NULL)
    {
        Fail("Expected strtok() to return %p, got %p!\n", NULL, ptr);
    }
    if (memcmp(str, result2, len) != 0)
    {
        Fail("strtok altered the string in an unexpeced way!\n");
    }

    PAL_Terminate();
    return PASS;
}
