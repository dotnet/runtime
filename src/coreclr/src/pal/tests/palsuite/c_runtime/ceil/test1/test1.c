// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests ceil with simple positive and negative values.  Also tests 
**          extreme cases like extremely small values and positive and 
**          negative infinity.  Makes sure that calling ceil on NaN returns 
**          NaN
**
**
**==========================================================================*/

#include <palsuite.h>

typedef struct
{
    double value;
    double ceilVal;
} testCase;

typedef struct
{
    INT64 value;
    INT64 ceilVal;
} extremeCase;

#define INT64_TO_DOUBLE(a) (*(double*)&a)

int __cdecl main(int argc, char *argv[])
{
    INT64 NaN = I64(0x7ff8000000000000);
    double value;
    double ceilVal;
    double ret;
    int i;

    testCase testCases[] =
    {
        {2.8, 3},
        {-2.8, -2},
        {5.5, 6},
        {-5.5, -5},
        {3, 3},
        {-3, -3},
        {0, 0},
    };
    extremeCase extremeCases[] =
    {
        /* Positive Infinity */
        {I64(0x7ff0000000000000), I64(0x7ff0000000000000)},
        /* Negative Infinitey */
        {I64(0xfff0000000000000), I64(0xfff0000000000000)},   
        /* Smallest possible +value */
        {I64(0x0000000000000001), I64(0x3FF0000000000000)},   
        /* Smallest possible -value */
        {I64(0x8000000000000001), I64(0x8000000000000000)},   
    };


    /*
     * Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


    for (i=0; i<sizeof(testCases) / sizeof(testCase); i++)
    {
        ret = ceil(testCases[i].value);

        if (ret != testCases[i].ceilVal)
        {
            Fail("Expected ceil(%g) to return %g, got %g\n", 
                testCases[i].value, testCases[i].ceilVal, ret);
        }
    }
    
    for (i=0; i<sizeof(extremeCases) / sizeof(extremeCase); i++)
    {
        value = INT64_TO_DOUBLE(extremeCases[i].value);
        ceilVal = INT64_TO_DOUBLE(extremeCases[i].ceilVal);

        ret = ceil(value);

        if (ret != ceilVal)
        {
            Fail("Expected ceil(%g) to return %g, got %I64X\n", 
                value, ceilVal, ret);
        }
    }

    /* Test how ceil handles NaN */
    if (!_isnan(ceil(INT64_TO_DOUBLE(NaN))))
    {
        Fail("Passing NaN to ceil did not return NaN!\n");
    }

    PAL_Terminate();
    return PASS;
}

