// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: \
** Search a string for a given character.  Search for a character contained
** in the string, and ensure the pointer returned points to it.  Then search
** for the null character, and ensure the pointer points to that.  Finally 
** search for a character which is not in the string and ensure that it 
** returns NULL.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    char *str = "foo bar baz";
    char *ptr;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ptr = strrchr(str, 'b');
    if (ptr != str + 8)
    {
        Fail("Expected strrchr() to return %p, got %p!\n", str + 8, ptr);
    }

    ptr = strrchr(str, 0);
    if (ptr != str + 11)
    {
        Fail("Expected strrchr() to return %p, got %p!\n", str + 11, ptr);
    }

    ptr = strrchr(str, 'x');
    if (ptr != NULL)
    {
        Fail("Expected strrchr() to return NULL, got %p!\n", ptr);
    }

    PAL_Terminate();

    return PASS;
}
