//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
