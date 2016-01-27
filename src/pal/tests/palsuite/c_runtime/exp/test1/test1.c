// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests exp with a normal set of values.
**
**
**===================================================================*/

#include <palsuite.h>

struct test
{
    double value;
    double result;
    double delta;
};


int __cdecl main(int argc, char **argv)
{    
    double value;
    double testDelta;
    int i;

    struct test tests[] = 
    {
        {0, 1, 1e-7},
        {0.5, 1.64872127, 1e-7},
        {-0.5, 0.60653066, 1e-7},
        {1, 2.71828183, 1e-7},
        {-1, 0.367879441, 1e-7},
        {10, 2.202646579481e+004, 1e-4},
        {-10, 4.53999298e-005, 1e-13},

        {600, 3.7730203009299397e+260, 1e+252},
        {-600, 2.6503965530043108e-261, 1e-269}
    };


    if (PAL_Initialize(argc, argv) != 0)
    {
	    return FAIL;
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        value = exp(tests[i].value);

        testDelta = fabs(value - tests[i].result);
        if( testDelta >= tests[i].delta )
        {
            Fail("exp(%g) returned %g when it should have returned %g\n",
                    tests[i].value, value, tests[i].result);
        }    
    }

    PAL_Terminate();
    return PASS;
}
