// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose:  Call the sqrt function on a positive value, a positive value
** with a decimal and on the maximum possible double value.
**
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
    double value;     /* value to test the function with */
    double expected;  /* expected result */
    double variance;  /* maximum delta between the expected and actual result */
};

/**
 * sqrt_test1_validate
 *
 * test validation function
 */
void __cdecl sqrt_test1_validate(double value, double expected, double variance)
{
    double result = sqrt(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("sqrt(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * sqrt_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl sqrt_test1_validate_isnan(double value)
{
    double result = sqrt(value);

    if (!_isnan(result))
    {
        Fail("sqrt(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

PALTEST(c_runtime_sqrt_test1_paltest_sqrt_test1, "c_runtime/sqrt/test1/paltest_sqrt_test1")
{
    struct test tests[] =
    {
        /* value                   expected                variance */
        {  0.31830988618379067,    0.56418958354775629,    PAL_EPSILON },       // value:  1 / pi
        {  0.43429448190325183,    0.65901022898226081,    PAL_EPSILON },       // value:  log10(e)
        {  0.63661977236758134,    0.79788456080286536,    PAL_EPSILON },       // value:  2 / pi
        {  0.69314718055994531,    0.83255461115769776,    PAL_EPSILON },       // value:  ln(2)
        {  0.70710678118654752,    0.84089641525371454,    PAL_EPSILON },       // value:  1 / sqrt(2)
        {  0.78539816339744831,    0.88622692545275801,    PAL_EPSILON },       // value:  pi / 4
        {  1,                      1,                      PAL_EPSILON * 10 },
        {  1.1283791670955126,     1.0622519320271969,     PAL_EPSILON * 10 },  // value:  2 / sqrt(pi)
        {  1.4142135623730950,     1.1892071150027211,     PAL_EPSILON * 10 },  // value:  sqrt(2)
        {  1.4426950408889634,     1.2011224087864498,     PAL_EPSILON * 10 },  // value:  log2(e)
        {  1.5707963267948966,     1.2533141373155003,     PAL_EPSILON * 10 },  // value:  pi / 2
        {  2.3025850929940457,     1.5174271293851464,     PAL_EPSILON * 10 },  // value:  ln(10)
        {  2.7182818284590452,     1.6487212707001281,     PAL_EPSILON * 10 },  // value:  e
        {  3.1415926535897932,     1.7724538509055160,     PAL_EPSILON * 10 },  // value:  pi
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    sqrt_test1_validate(-0.0, -0.0, PAL_EPSILON);
    sqrt_test1_validate( 0.0,  0.0, PAL_EPSILON);

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        sqrt_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
        sqrt_test1_validate_isnan(-tests[i].value);
    }

    sqrt_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
