// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the vprintf function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vprintf.h"

PALTEST(c_runtime_vprintf_test1_paltest_vprintf_test1, "c_runtime/vprintf/test1/paltest_vprintf_test1")
{
    char checkstr[] = "hello world";
    int ret;


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = vprintf("hello world", NULL);

    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);

    }

    PAL_Terminate();
    return PASS;
}

