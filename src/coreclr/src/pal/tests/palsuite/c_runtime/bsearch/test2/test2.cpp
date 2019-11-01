// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int __cdecl charcmp(const void *pa, const void *pb)
{
    return *(const char *)pa - *(const char *)pb;
}

int __cdecl main(int argc, char **argv)
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
                                (sizeof(char)), charcmp);
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



