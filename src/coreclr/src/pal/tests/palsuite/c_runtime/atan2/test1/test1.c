// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that atan2 returns correct values for a subset of values.
**          Tests with positive and negative values of x and y to ensure
**          atan2 is returning results from the correct quadrant.
**
**
**===================================================================*/

#include <palsuite.h>

#define DELTA 0.0000001 //Error acceptance level to the 7th decimal


struct test
{
    double x;
    double y;
    double result;  // expected result
};


void DoTest(double x, double y, double expected)
{
    double result;
    double testDelta;

    result = atan2(x, y);

    // The test is valid when the difference between the
    // result and the expectation is less than DELTA
    testDelta = fabs(result - expected);
    if( testDelta >= DELTA )
    {
        Fail( "atan2(%g, %g) returned %g when it should have returned %g",
                x, y, result, expected);
    }
}

int __cdecl main(int argc, char **argv)
{
    double pi = 3.1415926535;
    int i;

    struct test tests[] = 
    {
        {0, 0, 0},
        {0, 1, 0},
        {0.104528463, 0.994521895, 0.104719755},
        {0.207911691, 0.978147601, 0.20943951},
        {0.309016994, 0.951056516, 0.314159265},
        {0.406736643, 0.913545458, 0.41887902},
        {0.5, 0.866025404, 0.523598776},
        {0.587785252, 0.809016994, 0.628318531},
        {0.669130606, 0.743144825, 0.733038286},
        {0.743144825, 0.669130606, 0.837758041},
        {0.809016994, 0.587785252, 0.942477796},
        {0.866025404, 0.5, 1.04719755},
        {0.913545458, 0.406736643, 1.15191731},
        {0.951056516, 0.309016994, 1.25663706},
        {0.978147601, 0.207911691, 1.36135682},
        {0.994521895, 0.104528463, 1.46607657},
        {1, 4.48965922e-011, 1.57079633},    
    };


    if (PAL_Initialize(argc, argv) != 0)
    {
	    return FAIL;
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        DoTest(tests[i].x, tests[i].y, tests[i].result);
        DoTest(-tests[i].x, tests[i].y, -tests[i].result);
        DoTest(tests[i].x, -tests[i].y, pi - tests[i].result);
        DoTest(-tests[i].x, -tests[i].y, tests[i].result - pi);
    }

    PAL_Terminate();
    return PASS;
}
