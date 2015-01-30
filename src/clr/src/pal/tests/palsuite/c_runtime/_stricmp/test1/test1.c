//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Do a lower case compare.  Check two strings, only different 
** because they have different capitalization, and they should return 0. Try 
** two strings which will return less than 0 (one is smaller than the other).
** Also try the opposite, to get a return value greater than 0.
**
**
**==========================================================================*/

#include <palsuite.h>

/*
 * Note: The _stricmp is dependent on the LC_CTYPE category of the locale,
 *      and this is ignored by these tests.
 */
int __cdecl main(int argc, char *argv[])
{
    char *str1 = "foo";
    char *str2 = "fOo";
    char *str3 = "foo_bar";
    char *str4 = "foobar";

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (_stricmp(str1, str2) != 0)
    {
        Fail ("ERROR: _stricmp returning incorrect value:\n"
                "_stricmp(\"%s\", \"%s\") != 0\n", str1, str2);
    }

    if (_stricmp(str2, str3) >= 0)
    {
        Fail ("ERROR: _stricmp returning incorrect value:\n"
                "_stricmp(\"%s\", \"%s\") >= 0\n", str2, str3);
    }

    if (_stricmp(str3, str4) >= 0)
    {
        Fail ("ERROR: _stricmp returning incorrect value:\n"
                "_stricmp(\"%s\", \"%s\") >= 0\n", str3, str4);
    }

    if (_stricmp(str4, str1) <= 0)
    {
        Fail ("ERROR: _stricmp returning incorrect value:\n"
                "_stricmp(\"%s\", \"%s\") <= 0\n", str4, str1);
    }

    if (_stricmp(str3, str2) <= 0)
    {
        Fail ("ERROR: _stricmp returning incorrect value:\n"
                "_stricmp(\"%s\", \"%s\") <= 0\n", str2, str3);
    }

    PAL_Terminate();
    return PASS;
}
