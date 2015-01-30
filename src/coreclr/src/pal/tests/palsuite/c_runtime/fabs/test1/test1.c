//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that fabs return the correct values
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
** 

**
**===========================================================================*/

#include <palsuite.h>

/**
 * Helper test structure
 */
struct test
{
    double value;  // param 1
    double result; // expected result
};

/**
 * main
 * 
 * executable entry point
 */
INT __cdecl main(INT argc, CHAR **argv)
{
    int i;

    struct test tests[] = 
    {
      // param 1       result
        { 3,           3 },
        { -10,         10 },
        { 0,           0 },
        { 1.7e308,     1.7e308 },
        { -1.7e308,    1.7e308 },
        { 4.94e-324,   4.94e-324 },
        { -4.94e-324,  4.94e-324 }
    };


    // PAL initialization
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return FAIL;
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++ )
    {
        double result;

        result = fabs( tests[i].value );

        if( result != tests[i].result )
        {
            Fail( "fabs(%f) returned %f"
                  " when it should have returned %f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    PAL_Terminate();
    return PASS;
}













