// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(c_runtime_wcsstr_test1_paltest_wcsstr_test1, "c_runtime/wcsstr/test1/paltest_wcsstr_test1")
{
    WCHAR *string;
    WCHAR *key1;
    WCHAR *key2;
    WCHAR key3[] = { 0 };
    WCHAR *key4;
    WCHAR *result;
        
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    string = convert("foo bar baz bar");
    key1 = convert("bar");
    key2 = convert("Bar");
    key4 = convert("arggggh!");

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

    result = wcsstr(string, key4);
    if (result != nullptr)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" for \"%s\".\n"
            "Expected to get pointer to null, got %#p\n", convertC(string),
            convertC(key4), result);
    }

    PAL_Terminate();
    return PASS;
}
