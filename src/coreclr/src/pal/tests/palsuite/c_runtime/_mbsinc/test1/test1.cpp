// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Ensure that this function moves the string pointer ahead one character.
** First do a basic test to check that the pointer gets moved ahead the one
** character. Then try with an array of bytes and a NULL array.  Each of 
** these should still work by returning a pointer to thePointer+1.
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
    unsigned char *str1 = (unsigned char*) "foo";
    unsigned char str2[] = {0xC0, 0x80, 0xC0, 0x80, 0};
    unsigned char str3[] = {0};
    unsigned char *ret=NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ret = _mbsinc(str1);
    if (ret != str1 + 1)
    {
        Fail ("ERROR: _mbsinc returned %p. Expected %p\n", ret, str1);
    }

    ret = _mbsinc(str2);
    if (ret != str2 + 1)
    {
        Fail ("ERROR: _mbsinc returned %p. Expected %p\n", ret, str1);
    }

    ret = _mbsinc(str3);
    if (ret != str3 + 1)
    {
        Fail ("ERROR: _mbsinc returned %p. Expected %p\n", ret, str1);
    }

    PAL_Terminate();
    return PASS;
}

