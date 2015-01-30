//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls exit, and verifies that it actually stops program execution.
**
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

    /*should return 0*/
    exit(0);

    Fail ("Exit didn't actually stop execution.\n");

    return FAIL;
}





