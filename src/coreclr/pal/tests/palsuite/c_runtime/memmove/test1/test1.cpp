// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test that memmove correctly copies text from one buffer
**          to another even when the buffers overlap.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_memmove_test1_paltest_memmove_test1, "c_runtime/memmove/test1/paltest_memmove_test1")
{
    char testA[11] = "abcdefghij";
    char testB[15] = "aabbccddeeffgg";
    char testC[15] = "aabbccddeeffgg";
    char testD[15] = "aabbccddeeffgg";
    char insString[4] = "zzz";
    char *retVal;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* move a string onto itself */
    retVal = (char *)memmove(testA + 2, testA, 8);
    if (retVal != testA + 2)
    {
        Fail("The return value should have been the value of the destination"
             "pointer, but wasn't\n");
    }

    /*Check the most likely error*/
    if (memcmp(testA, "ababababab", 11) == 0)
    {
        Fail("memmove should have saved the characters in the region of"
             " overlap between source and destination, but didn't.\n");
    }

    if (memcmp(testA, "ababcdefgh", 11) != 0)
    {
        /* not sure what exactly went wrong. */
        Fail("memmove was called on a region containing the characters"
             " \"abcdefghij\".  It was to move the first 8 positions to"
             " the last 8 positions, giving the result \"ababcdefgh\". "
             " Instead, it gave the result \"%s\".\n", testA);
    }

    /* move a string to the front of testB */
    retVal = (char *)memmove(testB, insString, 3);
    if(retVal != testB)
    {
        Fail("memmove: The function did not return the correct "
        "string.\n");
    }

    if(memcmp(testB, "zzzbccddeeffgg",15) != 0)
    {
        Fail("memmove: The function failed to move the string "
        "correctly.\n");
    }


    /* move a string to the middle of testC */
    retVal = (char*)memmove(testC+5, insString, 3);
    if(retVal != testC+5)
    {
        Fail("memmove: The function did not return the correct "
        "string.\n");
    }

    if(memcmp(testC, "aabbczzzeeffgg",15) != 0)
    {
        Fail("memmove: The function failed to move the string "
        "correctly.\n");
    }


    /* move a string to the end of testD */
    retVal = (char*)memmove(testD+11, insString, 3);
    if(retVal != testD+11)
    {
        Fail("memmove: The function did not return the correct "
        "string.\n");
    }

    if(memcmp(testD, "aabbccddeefzzz",15) != 0)
    {
        Fail("memmove: The function failed to move the string "
        "correctly.\n");
    }

    PAL_Terminate();
    return PASS;

}














