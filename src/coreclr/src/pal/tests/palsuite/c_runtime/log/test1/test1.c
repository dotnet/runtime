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
    int i;
    double result;

    struct test tests[] = 
    {
        /* Value               test result */
        { 4.812768944365,     1.571272582596 },
        { 25.873592333750,    3.253222847545 },
        { 30.301828058718,    3.411208042666 },
        { 53.752250740074,    3.984385540365 },
        { 65.282143620106,    4.178718547435 },
        { 74.843592638936,    4.315400504515 },
        { 92.446668904691,    4.526631925866 },
        { 99.832148197882,    4.603490257677 },
        { 1830.360576189459,  7.512268262595 },
        { 6524.430494094669,  8.783308947783 },
        { 12456.186254463331, 9.429972666394 },
        { 14183.841639454329, 9.559858682961 },
        { 18221.999603259377, 9.810384912501 },
        { 20792.917264320811, 9.942367691562 },
        { 26488.001312295906, 10.184447128770 },
        { 29724.154423657950, 10.299715274515 },
        { 27899211.434430983, 17.144108982393 },
        { 55048606.214117862, 17.823727102268 },
        { 66659312.564470351, 18.015105318226 },
        { 113314373.84325695, 18.545676583294 },
        { 201366015.49641407, 19.120634782682 },
        { 311568417.23368025, 19.557129510064 },
        { 486835298.54176462, 20.003436427833 },
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

        result = log( tests[i].value );

        /* 
         * The test is valid when the difference between the
         * result and the expectation is less than DELTA
         */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "log(%g) returned %20.10f"
                  " when it should have returned %20.10f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    for( i = 0; i < sizeof(indefinite) / sizeof(double); i++)
    {
        result = log( indefinite[i] );

        /* The test is valid when the function returns a defined result */
        if( ! _isnan( result ) )
        {
            Fail( "log(%g) returned %20.10f"
                  " when it should have returned -1.#IND00000000",
                  indefinite[i],
                  result );
        }
    }

    /* log(0) is a special case */
    result = log( 0.0 );
    if( _finite( result ) )
    {
            Fail( "log(%g) returned %20.10f"
                  " when it should have returned -1.#INF00000000",
                  0.0,
                  result );
    }

    PAL_Terminate();
    return PASS;
}
