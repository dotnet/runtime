// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  abs.c (test 1)
**
** Purpose: Tests the PAL implementation of the abs function.
**
**
**===================================================================*/

#include <palsuite.h>

struct TESTS 
{
    int nTest;
    int nResult;
};

PALTEST(c_runtime_abs_test1_paltest_abs_test1, "c_runtime/abs/test1/paltest_abs_test1")
{
    int i = 0;
    int nRc = 0;
    struct TESTS testCase[] = 
    {
        {0, 0},
        {1, 1},
        {-1, 1}
    };


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    for (i = 0; i < (sizeof(testCase)/sizeof(struct TESTS)); i++)
    {
        nRc = abs(testCase[i].nTest);
        if (nRc != testCase[i].nResult)
        {
            Fail("abs: ERROR -> abs(%d) returned %d "
                "when it was expected to return %d \n",
                testCase[i].nTest,
                nRc,
                testCase[i].nResult);
        }
    }
    
    PAL_Terminate();
    return PASS;
}
