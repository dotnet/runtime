// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that fmaf returns correct values for a subset of values.
**          Tests with positive and negative values of x, y, and z to ensure
**          fmaf is returning correct results.
**
**===================================================================*/

#include <palsuite.h>

// binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this 
// is slightly too accurate when writing tests meant to run against libm implementations
// for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get.
//
// The tests themselves will take PAL_EPSILON and adjust it according to the expected result
// so that the delta used for comparison will compare the most significant digits and ignore
// any digits that are outside the double precision range (6-9 digits).

// For example, a test with an expect result in the format of 0.xxxxxxxxx will use PAL_EPSILON
// for the variance, while an expected result in the format of 0.0xxxxxxxxx will use
// PAL_EPSILON / 10 and and expected result in the format of x.xxxxxx will use PAL_EPSILON * 10.
#define PAL_EPSILON 4.76837158e-07

#define PAL_NAN     sqrtf(-1.0f)
#define PAL_POSINF -logf(0.0f)
#define PAL_NEGINF  logf(0.0f)

/**
 * Helper test structure
 */
struct test
{
    float x;         /* first component of the value to test the function with */
    float y;         /* second component of the value to test the function with */
    float z;         /* third component of the value to test the function with */
    float expected;  /* expected result */
    float variance;  /* maximum delta between the expected and actual result */
};

/**
 * fmaf_test1_validate
 *
 * test validation function
 */
void __cdecl fmaf_test1_validate(float x, float y, float z, float expected, float variance)
{
    float result = fmaf(x, y, z);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("fmaf(%g, %g, %g) returned %10.9g when it should have returned %10.9g",
             x, y, z, result, expected);
    }
}

/**
 * fmaf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl fmaf_test1_validate_isnan(float x, float y, float z)
{
    float result = fmaf(x, y, z);

    if (!_isnanf(result))
    {
        Fail("fmaf(%g, %g, %g) returned %10.9g when it should have returned %10.9g",
             x, y, z, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_fmaf_test1_paltest_fmaf_test1, "c_runtime/fmaf/test1/paltest_fmaf_test1")
{
    struct test tests[] = 
    {
        /* x                y                z                expected            variance */
        {  PAL_NEGINF,      PAL_NEGINF,      PAL_NEGINF,      PAL_NEGINF,         0 },
        { -1e38,            2,               1e38,           -1e38,               0 },
        {  1e38,            2,              -1e38,            1e38,               0 },
        {  PAL_POSINF,      PAL_POSINF,      PAL_POSINF,      PAL_POSINF,         0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        fmaf_test1_validate(tests[i].x, tests[i].y, tests[i].z, tests[i].expected, tests[i].variance);
    }

    // Returns NaN if x or y is infinite, the other is zero, and z is NaN
    fmaf_test1_validate_isnan(PAL_NEGINF, 0, PAL_NAN);
    fmaf_test1_validate_isnan(PAL_POSINF, 0, PAL_NAN);
    fmaf_test1_validate_isnan(0, PAL_NEGINF, PAL_NAN);
    fmaf_test1_validate_isnan(0, PAL_POSINF, PAL_NAN);

    // Returns NaN if x or y is infinite, the other is zero, and z is not-NaN
    fmaf_test1_validate_isnan(PAL_POSINF, 0, PAL_NEGINF);
    fmaf_test1_validate_isnan(PAL_NEGINF, 0, PAL_NEGINF);
    fmaf_test1_validate_isnan(0, PAL_POSINF, PAL_NEGINF);
    fmaf_test1_validate_isnan(0, PAL_NEGINF, PAL_NEGINF);
    
    fmaf_test1_validate_isnan(PAL_POSINF, 0, 0);
    fmaf_test1_validate_isnan(PAL_NEGINF, 0, 0);
    fmaf_test1_validate_isnan(0, PAL_POSINF, 0);
    fmaf_test1_validate_isnan(0, PAL_NEGINF, 0);

    fmaf_test1_validate_isnan(PAL_POSINF, 0, PAL_POSINF);
    fmaf_test1_validate_isnan(PAL_NEGINF, 0, PAL_POSINF);
    fmaf_test1_validate_isnan(0, PAL_POSINF, PAL_POSINF);
    fmaf_test1_validate_isnan(0, PAL_NEGINF, PAL_POSINF);

    // Returns NaN if (x * y) is infinite, and z is an infinite of the opposite sign
    fmaf_test1_validate_isnan(PAL_POSINF, PAL_POSINF, PAL_NEGINF);
    fmaf_test1_validate_isnan(PAL_NEGINF, PAL_NEGINF, PAL_NEGINF);
    fmaf_test1_validate_isnan(PAL_POSINF, PAL_NEGINF, PAL_POSINF);
    fmaf_test1_validate_isnan(PAL_NEGINF, PAL_POSINF, PAL_POSINF);

    fmaf_test1_validate_isnan(PAL_POSINF, 1, PAL_NEGINF);
    fmaf_test1_validate_isnan(PAL_NEGINF, 1, PAL_POSINF);
    fmaf_test1_validate_isnan(1, PAL_POSINF, PAL_NEGINF);
    fmaf_test1_validate_isnan(1, PAL_NEGINF, PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
