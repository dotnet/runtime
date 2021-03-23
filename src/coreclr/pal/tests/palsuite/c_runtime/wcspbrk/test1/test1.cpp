// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Tests that wcspbrk returns a pointer to the first element in the first 
** string that matches a character in the second (or NULL).
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_wcspbrk_test1_paltest_wcspbrk_test1, "c_runtime/wcspbrk/test1/paltest_wcspbrk_test1")
{
    WCHAR *string;
    WCHAR *key1;
    WCHAR *key2;
    WCHAR key3[] = {0};
    WCHAR *result;
            
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    string = convert("foo bar baz bar");
    key1 = convert("z ");
    key2 = convert("Q");

    result = wcspbrk(string, key1);
    if (result != string + 3)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" with the set \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key1), string + 3, result);
    }

    result = wcspbrk(string, key2);
    if (result != NULL)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" with the set \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key2), NULL, result);
    }

    result = wcspbrk(string, key3);
    if (result != NULL)
    {
        Fail("ERROR: Got incorrect result in scanning \"%s\" with the set \"%s\".\n"
            "Expected to get pointer to %#p, got %#p\n", convertC(string),
            convertC(key3), NULL, result);
    }

    PAL_Terminate();
    return PASS;
}
