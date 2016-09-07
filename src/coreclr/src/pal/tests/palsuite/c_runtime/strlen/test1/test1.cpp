// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
