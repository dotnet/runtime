// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Check that memset correctly fills a destination buffer
**          without overflowing it.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{

    char testA[22] = "bbbbbbbbbbbbbbbbbbbbb";
    char *retVal;

    int i;
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    retVal = (char *)memset(testA, 'a', 20);
    if (retVal != testA) 
    {
        Fail("memset should have returned the value of the destination"
             "pointer, but didn't");
    }

    for(i = 0; i<20; i++)
    {
        if (testA[i]!= 'a')
        {
            Fail("memset didn't set the destination bytes.\n");
        }
    }
    if (testA[20] == 'a')
    {
        Fail("memset overfilled the destination buffer.\n");
    }

    PAL_Terminate();
    return PASS;
}










