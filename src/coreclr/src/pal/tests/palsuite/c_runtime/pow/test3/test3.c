// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test3.c
**
** Purpose: Test to ensure that pow returns correct values.
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
struct test1
{
    double value;       /* value to test the function with */
    double exponent;    /* exponent to test */
    double result;      /* expected result */
};

struct test2
{
    double value;       /* value to test the function with */
    double exponent;    /* exponent to test */
};


/**
 * main
 * 
 * executable entry point
 */
int __cdecl main(int argc, char **argv)
{
    int i;
    double result;

    struct test1 tests[] = 
    {
        /* Value        test result */
        { 0.0, 0.0, 1.0 },
        { 4.2, 0.0, 1.0 },
        { 2.0, 3.0, 8.0 },
        { 0.1, 3.25, 0.000562341325 },
        { 1.0, 4.0, 1.000000000000 },
        { 2.4, -6.8, 0.002597547849 },
        { 3.75, 10.4, 933093.543524607670 },
        { 7.63, -4.521, 0.000102354411 },
        { 10, 5, 100000.000000000000 },
        { 13.26, -2.11, 0.004279895490 },
        { 18.1, 3.763, 54031.183101303657 },
        { 25, 4.0001, 390750.757575723810 },
        { 29.31, -5.997, 0.000000001593 }
    };

    struct test2 infinite[] =
    {
        { 0.0, -2.5 },
        { 0.0, -1 }
    };


    /* PAL initialization */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return (FAIL);
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test1); i++)
    {
        double testDelta;

        result = pow( tests[i].value, tests[i].exponent );

        /* The test is valid when the difference between the */
        /* result and the expectation is less than DELTA */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "pow(%g,%g) returned %20.10g"
                  " when it should have returned %20.10g\n",
                  tests[i].value,
                  tests[i].exponent,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(infinite) / sizeof(struct test2); i++)
    {
        result = pow( infinite[i].value, infinite[i].exponent );

        /* The test is valid when the function returns an infinite result */
        if( _finite( result ) )
        {
            Fail( "pow(%g,%g) returned %20.10g"
                  " when it should have returned 1.#INF00000000",
                  infinite[i].value,
                  infinite[i].exponent,
                  result );
        }
    }

    PAL_Terminate();
    return PASS;
}
