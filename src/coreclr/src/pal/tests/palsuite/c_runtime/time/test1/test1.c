//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls the time function and verifies that the time returned
**          is at least a positive value.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    time_t t = 0;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    time(&t);
    /*I was going to test that the time returned didn't exceed some
      reasonable value, but decided not to, for fear of creating my own
      little Y2K-style disaster.*/

    if (t <= 0)
    {
        Fail("time() function doesn't return a time.\n");
    }
    t = 0;
    t = time(NULL);  
    if (t <= 0)
    {
        Fail("time() function doesn't return a time.\n");
    }
    PAL_Terminate();
    return PASS;
}







