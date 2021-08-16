// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls bsearch to find a character in a sorted buffer, and
**          verifies that the correct position is returned.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl charcmp_bsearch_test1(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 1);
}

PALTEST(c_runtime_bsearch_test1_paltest_bsearch_test1, "c_runtime/bsearch/test1/paltest_bsearch_test1")
{

    const char array[] = "abcdefghij";
    char * found=NULL;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    found = (char *)bsearch(&"d", array, sizeof(array) - 1, (sizeof(char))
                            , charcmp_bsearch_test1);
    if (found != array + 3)
    {
        Fail ("bsearch was unable to find a specified character in a "
                "sorted list.\n");
    }
    PAL_Terminate();
    return PASS;
}



