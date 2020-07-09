// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Check that memcmp find identical buffers to be identical,
**          and that it correctly orders different buffers.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{

    char testA[] = "aaaaaaaaaaaaaaaaaaaa";
    char testB[] = "aaaaaaaaaaaaaaaaaaaa";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    if (!(memcmp(testA, testB, 20) == 0))
    {
        Fail("memcmp compared two identical buffers and found them to "
             "differ.\n");
    }
    testB[3] = 'b';

    if (!(memcmp(testA, testB, 20) < 0)
    || !(memcmp(testB, testA, 20) >0 ))
    {
        Fail("memcmp compared two buffers with different contents, and"
             " did not order them correctly.\n");
    }
    
    if (memcmp(testA, testB, 0) != 0)
    {
        Fail("memcmp didn't return 0 when comparing buffers of length 0.\n");
    }
  
    PAL_Terminate();
    return PASS;
}







