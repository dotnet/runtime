// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that acos returns correct values.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
**               fabs
**               _finite
** 

**
**===========================================================================*/

#include <palsuite.h>

/* Error acceptance level to the 7th decimal */
#define DELTA 0.0000001 

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
    double pi = 3.1415926535;
    int i;

    struct test tests[] = 
    {
        /* Value        test result */
        { 1,                0 },
        { 0.9,              0.451026811796 },
        { 0.8,              0.643501108793 },
        { 0.7,              0.795398830184 },
        { 0.6,              0.927295218002 },
        { 0.5,              1.047197551197 },
        { 0.4,              1.159279480727 },
        { 0.3,              1.266103672779 },
        { 0.2,              1.369438406005 },
        { 0.1,              1.470628905633 },
        { 0,                pi/2.0 },
        { -0.1,             1.670963747956 },
        { -0.2,             1.772154247585 },
        { -0.3,             1.875488980810 },
        { -0.4,             1.982313172862 },
        { -0.5,             2.094395102393 },
        { -0.6,             2.214297435588 },
        { -0.7,             2.346193823406 },
        { -0.8,             2.498091544797 },
        { -0.9,             2.690565841794 },
        { -1,              pi }
    };

    double outofrange[] =
    {
        -864278.51, -1000.2, -2, 2, 10, 100, 1234567.8, 1.7e308
    };


    /* PAL initialization */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return (FAIL);
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        double result;
        double testDelta;

        result = acos( tests[i].value );

        /*
         * The test is valid when the difference between the
         * result and the expectation is less than DELTA
         */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "acos(%g) returned %20.10f"
                  " when it should have returned %20.10f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(outofrange) / sizeof(double); i++)
    {
        double result;

        result = acos( outofrange[i] );

        /* The test is valid when the function returns an infinite result */
        if( _finite( result ) )
        {
            Fail( "acos(%g) returned %20.10f"
                  " when it should have returned -1.#IND00000000",
                  outofrange[i],
                  result );
        }
    }

    PAL_Terminate();
    return PASS;
}
