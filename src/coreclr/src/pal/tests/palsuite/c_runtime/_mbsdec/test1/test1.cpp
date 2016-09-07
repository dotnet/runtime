// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Ensure that this function moves the string pointer back one character.   
** First do a basic test to check that the pointer gets moved back the one
** character, given str1 and str+1 as params.  Then try with both 
** params being the same pointer, which should return NULL.  Also test 
** when the first pointer is past the second pointer, which should 
** return null. Finally try this function on an array of single bytes, 
** which it assumes are characters and should work in the same fashion.
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
    unsigned char *ret = NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ret = _mbsdec(str1,str1+1);
    if (ret != str1)
    {
        Fail ("ERROR: _mbsdec returned %p. Expected %p\n", ret, str1);
    }

    ret = _mbsdec(str1,str1);
    if (ret != NULL)
    {
        Fail ("ERROR: _mbsdec returned %p. Expected %p\n", ret, NULL);
    }

    ret = _mbsdec(str1+100,str1);
    if (ret != NULL)
    {
        Fail ("ERROR: _mbsdec returned %p. Expected %p\n", ret, NULL);
    }

    ret = _mbsdec(str2,str2+1);
    if (ret != str2)
    {
        Fail ("ERROR: _mbsdec returned %p. Expected %p\n", ret, str2+1);
    }

    ret = _mbsdec(str3,str3+10);
    if (ret != str3+9)
    {
        Fail ("ERROR: _mbsdec returned %p. Expected %p\n", ret, str3+9);
    }

    PAL_Terminate();
    return PASS;
}

