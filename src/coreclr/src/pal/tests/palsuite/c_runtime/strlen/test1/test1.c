//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Check the length of a string and the length of a 0 character string to 
** see that this function returns the correct values for each.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    if (strlen("foo") != 3)
        Fail("ERROR: strlen(\"foo\") != 3\n");

    if (strlen("") != 0)
        Fail("ERROR: strlen(\"\") != 0\n");

    PAL_Terminate();
    return PASS;
}
