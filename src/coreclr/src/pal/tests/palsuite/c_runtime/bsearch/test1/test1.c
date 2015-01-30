//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl charcmp(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 1);
}

int __cdecl main(int argc, char **argv)
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
                            , charcmp);
    if (found != array + 3)
    {
        Fail ("bsearch was unable to find a specified character in a "
                "sorted list.\n");
    }
    PAL_Terminate();
    return PASS;
}



