// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that log returns correct values.
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

#define DELTA 0.0000001 /*Error acceptance level to the 7th decimal */

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
        /* Value        test result */
        { 6.704916531877, 0.826393375779 },
        { 15.823847163305, 1.199312079583 },
        { 21.658986175115, 1.335638124087 },
        { 34.134952848903, 1.533199307291 },
        { 42.857142857143, 1.632023214705 },
        { 57.573168126469, 1.760220128395 },
        { 67.363505966369, 1.828424682147 },
        { 75.649281289102, 1.878804806336 },
        { 99.877925962096, 1.999469515332 },
        { 1706.203161717582, 3.232030742400 },
        { 3016.598101748711, 3.479517453423 },
        { 4017.596636860256, 3.603966331820 },
        { 5462.646626178777, 3.737403107294 },
        { 7199.790368358409, 3.857319851544 },
        { 13577.991882076480, 4.132835544685 },
        { 19235.535721915341, 4.284104286189 },
        { 24904.989593188267, 4.396286364594 },
        { 10690368.906277657000, 7.028992692224 },
        { 40653667.728385270000, 7.609099733260 },
        { 71100035.987914667000, 7.851869820552 },
        { 111208971.628284560000, 8.046139824759 },
        { 172499991.153172400000, 8.236789077136 },
        { 244361677.392925830000, 8.388033097635 },
        { 292552744.803003010000, 8.466204177125 },
        { 317831243.774193530000, 8.502196587433 },
        { 501815331.063020770000, 8.700543925401 },
    };

    double indefinite[] =
    {
        -864278.51, -1000.2, -2
    };


    /* PAL initialization */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return (FAIL);
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        double testDelta;

        result = log10( tests[i].value );

        /*
         * The test is valid when the difference between the
         * result and the expectation is less than DELTA
         */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "log10(%g) returned %20.10f"
                  " when it should have returned %20.10f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(indefinite) / sizeof(double); i++)
    {
        result = log10( indefinite[i] );

        /* The test is valid when the function returns a defined result */
        if( ! _isnan( result ) )
        {
            Fail( "log10(%g) returned %20.10f"
                  " when it should have returned -1.#IND00000000",
                  indefinite[i],
                  result );
        }
    }

    /* log(0) is a special case */
    result = log10( 0.0 );
    if( _finite( result ) )
    {
            Fail( "log10(%g) returned %20.10f"
                  " when it should have returned -1.#INF00000000",
                  0.0,
                  result );
    }

    PAL_Terminate();
    return PASS;
}
