//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Tests that wcsstr correctly find substrings in wide stings, including 
** returning NULL when the substring can't be found.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    WCHAR *string;
    WCHAR *key1;
    WCHAR *key2;
    WCHAR key3[] = { 0 };
    WCHAR *result;
        
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    string = convert("foo bar baz bar");
    key1 = convert("bar");
    key2 = convert("Bar");

    result = wcsstr(string, key1);
    if (result != string + 4)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" for \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key1), string + 4, result);
    }


    result = wcsstr(string, key2);
    if (result != NULL)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" for \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key2), NULL, result);
    }

    result = wcsstr(string, key3);
    if (result != string)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" for \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key3), string, result);
    }

    PAL_Terminate();
    return PASS;
}
