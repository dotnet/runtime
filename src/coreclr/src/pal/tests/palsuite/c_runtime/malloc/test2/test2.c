//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test that malloc(0) returns non-zero value
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    char *testA;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* check that malloc(0) returns non-zero value */
    testA = (char *)malloc(0);
    if (testA == NULL)
    {
        Fail("Call to malloc(0) failed.\n");
    }

    free(testA);

    PAL_Terminate();

    return PASS;
}



