// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that log2 returns correct values.
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
 * log2_test1_validate
 *
 * test validation function
 */
void __cdecl log2_test1_validate(double value, double expected, double variance)
{
    double result = log2(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("log2(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * log2_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl log2_test1_validate_isnan(double value)
{
    double result = log2(value);

    if (!_isnan(result))
    {
        Fail("log2(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_log2_test1_paltest_log2_test1, "c_runtime/log2/test1/paltest_log2_test1")
{
    struct test tests[] = 
    {
        /* value                       expected               variance */
        {  0,                          PAL_NEGINF,            0 },
        {  0.11331473229676087,       -3.1415926535897932,    PAL_EPSILON * 10 },   // expected: -(pi)
        {  0.15195522325791297,       -2.7182818284590452,    PAL_EPSILON * 10 },   // expected: -(e)
        {  0.20269956628651730,       -2.3025850929940457,    PAL_EPSILON * 10 },   // expected: -(ln(10))
        {  0.33662253682241906,       -1.5707963267948966,    PAL_EPSILON * 10 },   // expected: -(pi / 2)
        {  0.36787944117144232,       -1.4426950408889634,    PAL_EPSILON * 10 },   // expected: -(log2(e))
        {  0.37521422724648177,       -1.4142135623730950,    PAL_EPSILON * 10 },   // expected: -(sqrt(2))
        {  0.45742934732229695,       -1.1283791670955126,    PAL_EPSILON * 10 },   // expected: -(2 / sqrt(pi))
        {  0.5,                       -1,                     PAL_EPSILON * 10 },   // expected: -(1)
        {  0.58019181037172444,       -0.78539816339744831,   PAL_EPSILON },        // expected: -(pi / 4)
        {  0.61254732653606592,       -0.70710678118654752,   PAL_EPSILON },        // expected: -(1 / sqrt(2))
        {  0.61850313780157598,       -0.69314718055994531,   PAL_EPSILON },        // expected: -(ln(2))
        {  0.64321824193300488,       -0.63661977236758134,   PAL_EPSILON },        // expected: -(2 / pi)
        {  0.74005557395545179,       -0.43429448190325183,   PAL_EPSILON },        // expected: -(log10(e))
        {  0.80200887896145195,       -0.31830988618379067,   PAL_EPSILON },        // expected: -(1 / pi)
        {  1,                          0,                     PAL_EPSILON },
        {  1.2468689889006383,         0.31830988618379067,   PAL_EPSILON },        // expected:  1 / pi
        {  1.3512498725672678,         0.43429448190325183,   PAL_EPSILON },        // expected:  log10(e)
        {  1.5546822754821001,         0.63661977236758134,   PAL_EPSILON },        // expected:  2 / pi
        {  1.6168066722416747,         0.69314718055994531,   PAL_EPSILON },        // expected:  ln(2)
        {  1.6325269194381528,         0.70710678118654752,   PAL_EPSILON },        // expected:  1 / sqrt(2)
        {  1.7235679341273495,         0.78539816339744831,   PAL_EPSILON },        // expected:  pi / 4
        {  2,                          1,                     PAL_EPSILON * 10 },
        {  2.1861299583286618,         1.1283791670955126,    PAL_EPSILON * 10 },   // expected:  2 / sqrt(pi)
        {  2.6651441426902252,         1.4142135623730950,    PAL_EPSILON * 10 },   // expected:  sqrt(2)
        {  2.7182818284590452,         1.4426950408889634,    PAL_EPSILON * 10 },   // expected:  log2(e)             value: e
        {  2.9706864235520193,         1.5707963267948966,    PAL_EPSILON * 10 },   // expected:  pi / 2
        {  4.9334096679145963,         2.3025850929940457,    PAL_EPSILON * 10 },   // expected:  ln(10)
        {  6.5808859910179210,         2.7182818284590452,    PAL_EPSILON * 10 },   // expected:  e
        {  8.8249778270762876,         3.1415926535897932,    PAL_EPSILON * 10 },   // expected:  pi
        {  PAL_POSINF,                 PAL_POSINF,            0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        log2_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }

    log2_test1_validate_isnan(PAL_NEGINF);
    log2_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
