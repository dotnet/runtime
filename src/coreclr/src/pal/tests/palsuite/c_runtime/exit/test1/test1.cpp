// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls exit, and verifies that it actually stops program execution.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_exit_test1_paltest_exit_test1, "c_runtime/exit/test1/paltest_exit_test1")
{
    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /*should return 0*/
    exit(0);

    Fail ("Exit didn't actually stop execution.\n");

    return FAIL;
}





