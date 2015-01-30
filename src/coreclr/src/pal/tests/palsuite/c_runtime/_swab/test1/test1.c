//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls _swab on a buffer, and checks that it has correctly
**          swapped adjacent bytes
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    char before[] = "abcdefghijklmn";
    char after[] =  "--------------";
    const char check[] = "badcfehgjilknm";

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    _swab(before, after, sizeof(before));
    if (memcmp(after, check, sizeof(after)) != 0)
    {
        Fail ("_swab did not correctly swap adjacent bytes in a buffer.\n");
    }

    PAL_Terminate();
    return PASS;

}





