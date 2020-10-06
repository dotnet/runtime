// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that fma returns correct values for a subset of values.
**          Tests with positive and negative values of x, y, and z to ensure
**          fmaf is returning correct results.
**
**===================================================================*/

#include <palsuite.h>

// binary64 (double) has a machine epsilon of 2^-52 (approx. 2.22e-16). However, this 
// is slightly too accurate when writing tests meant to run against libm implementations
// for various platforms. 2^-50 (approx. 8.88e-16) seems to be as accurate as we can get.
//
// The tests themselves will take PAL_EPSILON and adjust it according to the expected result
// so that the delta used for comparison will compare the most significant digits and ignore
// any digits that are outside the double precision range (15-17 digits).

// For example, a test with an expect result in the format of 0.xxxxxxxxxxxxxxxxx will use
// PAL_EPSILON for the variance, while an expected result in the format of 0.0xxxxxxxxxxxxxxxxx
// will use PAL_EPSILON / 10 and and expected result in the format of x.xxxxxxxxxxxxxxxx will
// use PAL_EPSILON * 10.
#define PAL_EPSILON 8.8817841970012523e-16

#define PAL_NAN     sqrt(-1.0)
#define PAL_POSINF -log(0.0)
#define PAL_NEGINF  log(0.0)

/**
 * Helper test structure
 */
struct test
{
    double x;         /* first component of the value to test the function with */
    double y;         /* second component of the value to test the function with */
    double z;         /* third component of the value to test the function with */
    double expected;  /* expected result */
    double variance;  /* maximum delta between the expected and actual result */
};

/**
 * fma_test1_validate
 *
 * test validation function
 */
void __cdecl fma_test1_validate(double x, double y, double z, double expected, double variance)
{
    double result = fma(x, y, z);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("fma(%g, %g, %g) returned %20.17g when it should have returned %20.17g",
             x, y, z, result, expected);
    }
}

/**
 * fma_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl fma_test1_validate_isnan(double x, double y, double z)
{
    double result = fma(x, y, z);

    if (!_isnan(result))
    {
        Fail("fma(%g, %g, %g) returned %20.17g when it should have returned %20.17g",
             x, y, z, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_fma_test1_paltest_fma_test1, "c_runtime/fma/test1/paltest_fma_test1")
{
    struct test tests[] = 
    {
        /* x                       y                       z                       expected                   variance */
        {  PAL_NEGINF,             PAL_NEGINF,             PAL_NEGINF,             PAL_NEGINF,                0 },
        { -1e308,                  2,                      1e308,                 -1e308,                     0 },
        {  1e308,                  2,                     -1e308,                  1e308,                     0 },
        {  PAL_POSINF,             PAL_POSINF,             PAL_POSINF,             PAL_POSINF,                0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        fma_test1_validate(tests[i].x, tests[i].y, tests[i].z, tests[i].expected, tests[i].variance);
    }

    // Returns NaN if x or y is infinite, the other is zero, and z is NaN
    fma_test1_validate_isnan(PAL_NEGINF, 0, PAL_NAN);
    fma_test1_validate_isnan(PAL_POSINF, 0, PAL_NAN);
    fma_test1_validate_isnan(0, PAL_NEGINF, PAL_NAN);
    fma_test1_validate_isnan(0, PAL_POSINF, PAL_NAN);

    // Returns NaN if x or y is infinite, the other is zero, and z is not-NaN
    fma_test1_validate_isnan(PAL_POSINF, 0, PAL_NEGINF);
    fma_test1_validate_isnan(PAL_NEGINF, 0, PAL_NEGINF);
    fma_test1_validate_isnan(0, PAL_POSINF, PAL_NEGINF);
    fma_test1_validate_isnan(0, PAL_NEGINF, PAL_NEGINF);
    
    fma_test1_validate_isnan(PAL_POSINF, 0, 0);
    fma_test1_validate_isnan(PAL_NEGINF, 0, 0);
    fma_test1_validate_isnan(0, PAL_POSINF, 0);
    fma_test1_validate_isnan(0, PAL_NEGINF, 0);

    fma_test1_validate_isnan(PAL_POSINF, 0, PAL_POSINF);
    fma_test1_validate_isnan(PAL_NEGINF, 0, PAL_POSINF);
    fma_test1_validate_isnan(0, PAL_POSINF, PAL_POSINF);
    fma_test1_validate_isnan(0, PAL_NEGINF, PAL_POSINF);

    // Returns NaN if (x * y) is infinite, and z is an infinite of the opposite sign
    fma_test1_validate_isnan(PAL_POSINF, PAL_POSINF, PAL_NEGINF);
    fma_test1_validate_isnan(PAL_NEGINF, PAL_NEGINF, PAL_NEGINF);
    fma_test1_validate_isnan(PAL_POSINF, PAL_NEGINF, PAL_POSINF);
    fma_test1_validate_isnan(PAL_NEGINF, PAL_POSINF, PAL_POSINF);

    fma_test1_validate_isnan(PAL_POSINF, 1, PAL_NEGINF);
    fma_test1_validate_isnan(PAL_NEGINF, 1, PAL_POSINF);
    fma_test1_validate_isnan(1, PAL_POSINF, PAL_NEGINF);
    fma_test1_validate_isnan(1, PAL_NEGINF, PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
