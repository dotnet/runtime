// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Checks that _alloca allocates memory, and that the memory is
**          readable and writeable.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{

    char *testA = NULL;
    int i = 0;

    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


    /* check that _alloca really gives us addressable memory */
    testA = (char *)_alloca(20 * sizeof(char));
    if (testA == NULL)
    {
        Fail ("The call to _alloca failed\n");
    }

    memset(testA, 'a', 20);

    for (i = 0; i < 20; i++)
    {
        if (testA[i] != 'a')
        {
            Fail ("The memory returned by _alloca doesn't seem to be"
                    " properly allocated\n");
        }
    }
    
    PAL_Terminate();
    return PASS;
}










