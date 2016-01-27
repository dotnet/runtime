// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c (modf)
**
** Purpose: Test to ensure that modf return the correct values
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
**               fabs
** 

**
**===========================================================================*/

#include <palsuite.h>

#define DELTA 0.0000001 //Error acceptance level to the 7th decimal

/**
 * Helper test structure
 */
struct test
{
    double value;   // fmodf param 1
    double result1; // expected result (fractional portion)
    double result2; // expected result (integer portion)
};

/**
 * main
 * 
 * executable entry point
 */
INT __cdecl main(INT argc, CHAR **argv)
{
    INT i;

    struct test tests[] = 
    {
      // param 1   fractional   integer
        { 3,        0,          3  },
        { -10,      0,         -10 },
        { 1.1234,   0.1234,     1 },
        { -1.1234,  -0.1234,    -1 },
        { 1.7e308,  0,          1.7e308 },
        { -1.7e308, 0,         -1.7e308 },
        { 1.7e-30,  1.7e-30,    0 },
        { 0,        0,          0 }
    };


    // PAL initialization
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return FAIL;
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++ )
    {
        double fractionalResult;
        double integerResult;
        double testDelta;

        fractionalResult = modf( tests[i].value, &integerResult );

        // The test is valid when the difference between the
        // result and the expectation is less than DELTA
        
        testDelta = fabs( fractionalResult - tests[i].result1 );

        if( (testDelta >= DELTA) ||
            (integerResult != tests[i].result2) )

        {
            Fail( "ERROR: "
                  "modf(%f) returned "
                  "fraction=%20.20f and integer=%20.20f "
                  "when it should have returned "
                  "fraction=%20.20f and integer=%20.20f ",
                  tests[i].value,
                  fractionalResult,
                  integerResult, 
                  tests[i].result1,
                  tests[i].result2 );
        }
    }

    PAL_Terminate();
    return PASS;
}













