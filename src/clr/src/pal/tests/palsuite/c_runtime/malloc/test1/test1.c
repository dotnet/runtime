//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test that malloc returns useable memory
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    char *testA;
    int i;
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* check that malloc really gives us addressable memory */
    testA = (char *)malloc(20 * sizeof(char));
    if (testA == NULL)
    {
        Fail("Call to malloc failed.\n");
    }
    for (i = 0; i < 20; i++)
    {
        testA[i] = 'a';
    }
    for (i = 0; i < 20; i++)
    {
        if (testA[i] != 'a')
        {
            Fail("The memory doesn't seem to be properly allocated.\n");
        }
    }
    free(testA);

    PAL_Terminate();

    return PASS;
}



