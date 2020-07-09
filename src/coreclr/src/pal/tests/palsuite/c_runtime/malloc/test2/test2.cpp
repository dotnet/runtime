// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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



