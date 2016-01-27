// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that atan returns correct values.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
**               fabs
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
        /* Value                       test result */
        { -1.7976931348623158e+307,         -(pi/2) },
        { -25928.789941099276,     -1.570757759627 },
        { -10162.054261909849,     -1.570697921495 },
        { -7742.279976805932,      -1.570667165876 },
        { -4027.241187780389,      -1.570548017858 },
        { -2020.329477828303,      -1.570301358064 },
        { -928.811975463118,       -1.569719683039 },
        { -0.993835261086,         -0.782306273415 },
        { -0.936490981780,         -0.752613986257 },
        { -0.884395886105,         -0.724126849580 },
        { -0.728537858211,         -0.629623252584 },
        { -0.571733756523,         -0.519376146611 },
        { -0.370342112491,         -0.354680802574 },
        { -0.264778588214,         -0.258838835750 },
        { -0.199255348369,         -0.196679446239 },
        { -0.077700125126,         -0.077544322548 },
        { -0.036713766900,         -0.036697284724 },
        { 0,                        0},
        { 0.284035767693,           0.276747134475 },
        { 0.329264198737,           0.318083873607 },
        { 0.332560197760,           0.321054571066 },
        { 0.338206122013,           0.326129634676 },
        { 0.421887874996,           0.399231699263 },
        { 0.645222327342,           0.573009239835 },
        { 0.648396252327,           0.575246979388 },
        { 0.721427045503,           0.624962252188 },
        { 0.723685415204,           0.626445984185 },
        { 0.747856074709,           0.642127583998 },
        { 401.853083895383,         1.568307860298 },
        { 1579.672322763756,        1.570163284200 },
        { 2540.318308053835,        1.570402675359 },
        { 6105.661946470535,        1.570632544391 },
        { 10221.297708059939,       1.570698491860 },
        { 17155.000305185094,       1.570738034753 },
        { 20600.197424237798,       1.570747783571 },
        { 1.7976931348623158e+308,            pi/2 }
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

        result = atan( tests[i].value );

        /*
         * The test is valid when the difference between the
         * result and the expectation is less than DELTA
         */
        testDelta = fabs( result - tests[i].result );
        if( testDelta >= DELTA )
        {
            Fail( "atan(%g) returned %20.10f"
                  " when it should have returned %20.10f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    PAL_Terminate();
    return PASS;
}
