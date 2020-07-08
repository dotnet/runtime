// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Using memcmp to check the result, convert a wide character string 
** with capitals, to all lowercase using this function. Test #1 for the 
** wcslwr function
**
**
**==========================================================================*/

#include <palsuite.h>

/* uses memcmp,wcslen */

int __cdecl main(int argc, char *argv[])
{
    WCHAR *test_str   = NULL;
    WCHAR *expect_str = NULL;
    WCHAR *result_str = NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
	
	test_str   = convert("aSdF 1#");
	expect_str = convert("asdf 1#");

    result_str = _wcslwr(test_str);
    if (memcmp(result_str, expect_str, wcslen(expect_str)*2 + 2) != 0)
    {
        Fail ("ERROR: Expected to get \"%s\", got \"%s\".\n",
                convertC(expect_str), convertC(result_str));
    }

	free(result_str);
	free(expect_str);

    PAL_Terminate();
    return PASS;
}

