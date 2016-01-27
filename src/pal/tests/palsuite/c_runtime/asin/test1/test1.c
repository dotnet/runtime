// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that asin returns correct values.
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


#define DELTA 0.0000001 /* Error acceptance level to the 7th decimal */

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
        { 1,                pi/2.0 },
        { 0.9,              1.119769514999 },
        { 0.8,              0.927295218002 },
        { 0.7,              0.775397496611 },
        { 0.6,              0.643501108793 },
        { 0.5,              0.523598775598 },
        { 0.4,              0.411516846067 },
        { 0.3,              0.304692654015 },
        { 0.2,              0.201357920790 },
        { 0.1,              0.100167421162 },
        { 0,                0 },
        { -0.1,             -0.100167421162 },
        { -0.2,             -0.201357920790 },
        { -0.3,             -0.304692654015 },
        { -0.4,             -0.411516846067 },
        { -0.5,             -0.523598775598 },
        { -0.6,             -0.643501108793 },
        { -0.7,             -0.775397496611 },
        { -0.8,             -0.927295218002 },
        { -0.9,             -1.119769514999 },
        { -1,              -(pi/2.0) }
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

        result = asin( tests[i].value );

        /*
         * The test is valid when the difference between the
         * result and the expectation is less than DELTA
         */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "asin(%g) returned %20.10f"
                  " when it should have returned %20.10f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(outofrange) / sizeof(double); i++)
    {
        double result;

        result = asin( outofrange[i] );

        /* The test is valid when the function returns an infinite result */
        if( _finite( result ) )
        {
            Fail( "asin(%g) returned %20.10f"
                  " when it should have returned -1.#IND00000000",
                  outofrange[i],
                  result );
        }
    }
    
    PAL_Terminate();
    return PASS;
}













