// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Calls exit on fail, and verifies that it actually
**          stops program execution and return 1.

**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /*should return 1*/
    exit(1);

}








