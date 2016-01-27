// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test3.c
**
** Purpose: Test to ensure that exp returns correct values.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
**               fabs
**               _finite
**               _isnan
** 

**
**===========================================================================*/

#include <palsuite.h>

#define DELTA 0.0000001  /* Error acceptance level to the 7th decimal */

/**
 * Helper test structure
 */
struct test
{
    double value;   /* value to test the function with */
    double result;  /* expected result */
};

/**
 * main
 * 
 * executable entry point
 */
INT __cdecl main(INT argc, CHAR **argv)
{
    int i;
    double result;

    struct test tests[] = 
    {
        /* Value        test result  */
        { 0.100000000000, 1.105170918076 },
        { 1.000000000000, 2.718281828459 },
        { 2.400000000000, 11.023176380642 },
        { 3.750000000000, 42.521082000063 },
        { 7.630000000000, 2059.050019837344 },
        { 10.000000000000, 22026.465794806718 },
        { 13.260000000000, 573779.238840227250 },
        { 18.100000000000, 72565488.372322351000 },
        { 25.000000000000, 72004899337.385880000000 },
        { 29.310000000000, 5360079912775.353500000000 }
    };

    double infinite[] =
    {
        2215.8, 20554.1
    };


    /* PAL initialization */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return (FAIL);
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        double testDelta;
        double allowedDelta;

        result = exp( tests[i].value );

        /* The test is valid when the difference between the */
        /* result and the expectation is less than DELTA */
        allowedDelta = (tests[i].value > 1) ? 1 : DELTA;
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= allowedDelta )
        {
            Fail( "exp(%g) returned %20.10g"
                  " when it should have returned %20.10g\n",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(infinite) / sizeof(double); i++)
    {
        result = exp( infinite[i] );

        /* The test is valid when the function returns an infinite result */
        if( _finite( result ) )
        {
            Fail( "exp(%g) returned %20.10g"
                  " when it should have returned 1.#INF00000000",
                  infinite[i],
                  result );
        }
    }

    PAL_Terminate();
    return PASS;
}
