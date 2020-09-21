// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls bsearch to find a character in a sorted buffer, 
**          that does not exist.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl charcmp_bsearch_test2(const void *pa, const void *pb)
{
    return *(const char *)pa - *(const char *)pb;
}

PALTEST(c_runtime_bsearch_test2_paltest_bsearch_test2, "c_runtime/bsearch/test2/paltest_bsearch_test2")
{

    const char array[] = "abcefghij";
    const char missing[] = "0dz";
    char * found=NULL;
    const char * candidate = missing;

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    while (*candidate) {
        found = (char *)bsearch(candidate, array, sizeof(array) - 1,
                                (sizeof(char)), charcmp_bsearch_test2);
        if (found != NULL)
        {
            Fail ("ERROR: bsearch was able to find a specified character '%c' "
                  "in a sorted list '%s' as '%c' "
                  "even though the character is not in the list.\n",
                  *candidate, array, *found);
        }

	candidate++;
    }

    PAL_Terminate();
    return PASS;
}



