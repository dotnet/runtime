// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
**      Using memcmp, check to see that after changing a string into all lowercase
**      that it is the lowercase string that was expected.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    char string[] = "aASdff";
    char checkstr[] = "aasdff";
    char *ret=NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ret = _strlwr(string);

    if (memcmp(ret, checkstr, sizeof(checkstr)) != 0)
    {
        Fail ("ERROR: _strlwr returning incorrect value\n"
                "Expected %s, got %s\n", checkstr, ret);
    }
    
    PAL_Terminate();
    return PASS;
}
