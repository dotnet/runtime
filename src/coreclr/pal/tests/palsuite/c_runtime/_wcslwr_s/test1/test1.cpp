// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Using memcmp to check the result, convert a wide character string
** with capitals, to all lowercase using this function. Test #1 for the
** _wcslwr_s function
**
**
**==========================================================================*/

#include <palsuite.h>

/* uses memcmp,wcslen */

PALTEST(c_runtime__wcslwr_s_test1_paltest_wcslwr_s_test1, "c_runtime/_wcslwr_s/test1/paltest_wcslwr_s_test1")
{
    WCHAR *test_str   = NULL;
    WCHAR *expect_str = NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    test_str   = convert("aSdF 1#");
    expect_str = convert("asdf 1#");

    errno_t ret = _wcslwr_s(test_str, 8);
    if (ret != 0 || memcmp(test_str, expect_str, wcslen(expect_str)*2 + 2) != 0)
    {
        Fail ("ERROR: Expected to get \"%s\", got \"%s\".\n",
                convertC(expect_str), convertC(test_str));
    }

    free(test_str);
    free(expect_str);

    PAL_Terminate();
    return PASS;
}

