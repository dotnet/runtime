// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Ensure that this functions increases a string pointer by n characters.
** Use a for loop, and increase the pointer by a different number of characters
** on each iteration, ensure that it is indeed pointing to the correct location
** each time.  The second test checks to see if you attempt to increase the 
** pointer past the end of the string, the pointer should just point at the 
** last character.
**
**
**==========================================================================*/

#include <palsuite.h>

/*
 * Note: it seems like these functions would only be useful if they
 *   didn't assume a character was equivalent to a single byte. Be that
 *   as it may, I haven't seen a way to get it to behave otherwise.
 */

int __cdecl main(int argc, char *argv[])
{
    unsigned char str[] = {0xC0, 0x80, 0xC0, 0x80, 0};
    int i=0;
    unsigned char *ret=NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for (i=0; i<5; i++)
    {
        ret = _mbsninc(str, i);
        if (ret != str + i)
        {
            Fail ("ERROR: _mbsninc returned %p. Expected %p\n", ret, str+i);
        }
    }

    /* 
     * trying to advance past the end of the string should just 
     * return the end. 
     */
    ret = _mbsninc(str, 5);
    if (ret != str + 4)
    {
        Fail ("ERROR: _mbsninc returned %p. Expected %p\n", ret, str+4);
    }


    PAL_Terminate();
    return PASS;
}

