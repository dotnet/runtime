// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that floor return the correct values
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
    double value; // floor param 1
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
      // param 1         result
        { 3,                3 },
        { -10,            -10 },
        { 0,                0 },
        { 1.7e308,          1.7e308 },
        { -1.7e308,        -1.7e308 },
        { 4.94e-324,        0 },
        { -4.94e-324,      -1 },
        { 1234.1234,     1234 },
        { -1234.1234,   -1235 },
        {-0,                0 }
    };


    // PAL initialization
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return FAIL;
    }

    for( i = 0; i < sizeof(tests) / sizeof(struct test); i++)
    {
        double result;

        result = floor( tests[i].value );

        if( result != tests[i].result )
        {
            Fail( "floor(%f) returned %f"
                  " when it should have returned %f",
                  tests[i].value,
                  result, 
                  tests[i].result );
        }
    }

    PAL_Terminate();
    return PASS;
}













