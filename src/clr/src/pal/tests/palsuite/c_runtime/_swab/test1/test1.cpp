// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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





