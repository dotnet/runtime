// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests logf with a normal set of values.
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
    float value;     /* value to test the function with */
    float expected;  /* expected result */
    float variance;  /* maximum delta between the expected and actual result */
};

/**
 * logf_test1_validate
 *
 * test validation function
 */
void __cdecl logf_test1_validate(float value, float expected, float variance)
{
    float result = logf(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("logf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * logf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl logf_test1_validate_isnan(float value)
{
    float result = logf(value);

    if (!_isnanf(result))
    {
        Fail("logf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_logf_test1_paltest_logf_test1, "c_runtime/logf/test1/paltest_logf_test1")
{
    struct test tests[] = 
    {
        /* value              expected        variance */
        {  0,                 PAL_NEGINF,     0 },
        {  0.0432139183f,    -3.14159265f,    PAL_EPSILON * 10 },   // expected: -(pi)
        {  0.0659880358f,    -2.71828183f,    PAL_EPSILON * 10 },   // expected: -(e)
        {  0.1f,             -2.30258509f,    PAL_EPSILON * 10 },   // expected: -(ln(10))
        {  0.207879576f,     -1.57079633f,    PAL_EPSILON * 10 },   // expected: -(pi / 2)
        {  0.236290088f,     -1.44269504f,    PAL_EPSILON * 10 },   // expected: -(logf2(e))
        {  0.243116734f,     -1.41421356f,    PAL_EPSILON * 10 },   // expected: -(sqrtf(2))
        {  0.323557264f,     -1.12837917f,    PAL_EPSILON * 10 },   // expected: -(2 / sqrtf(pi))
        {  0.367879441f,     -1,              PAL_EPSILON * 10 },   // expected: -(1)
        {  0.455938128f,     -0.785398163f,   PAL_EPSILON },        // expected: -(pi / 4)
        {  0.493068691f,     -0.707106781f,   PAL_EPSILON },        // expected: -(1 / sqrtf(2))
        {  0.5f,             -0.693147181f,   PAL_EPSILON },        // expected: -(ln(2))
        {  0.529077808f,     -0.636619772f,   PAL_EPSILON },        // expected: -(2 / pi)
        {  0.647721485f,     -0.434294482f,   PAL_EPSILON },        // expected: -(log10f(e))
        {  0.727377349f,     -0.318309886f,   PAL_EPSILON },        // expected: -(1 / pi)
        {  1,                 0,              PAL_EPSILON },
        {  1.37480223f,       0.318309886f,   PAL_EPSILON },        // expected:  1 / pi
        {  1.54387344f,       0.434294482f,   PAL_EPSILON },        // expected:  log10f(e)
        {  1.89008116f,       0.636619772f,   PAL_EPSILON },        // expected:  2 / pi
        {  2,                 0.693147181f,   PAL_EPSILON },        // expected:  ln(2)
        {  2.02811498f,       0.707106781f,   PAL_EPSILON },        // expected:  1 / sqrtf(2)
        {  2.19328005f,       0.785398163f,   PAL_EPSILON },        // expected:  pi / 4
        {  2.71828183f,       1,              PAL_EPSILON * 10 },   //                               value: e
        {  3.09064302f,       1.12837917f,    PAL_EPSILON * 10 },   // expected:  2 / sqrtf(pi)
        {  4.11325038f,       1.41421356f,    PAL_EPSILON * 10 },   // expected:  sqrtf(2)
        {  4.23208611f,       1.44269504f,    PAL_EPSILON * 10 },   // expected:  logf2(e)
        {  4.81047738f,       1.57079633f,    PAL_EPSILON * 10 },   // expected:  pi / 2
        {  10,                2.30258509f,    PAL_EPSILON * 10 },   // expected:  ln(10)
        {  15.1542622f,       2.71828183f,    PAL_EPSILON * 10 },   // expected:  e
        {  23.1406926f,       3.14159265f,    PAL_EPSILON * 10 },   // expected:  pi
        {  PAL_POSINF,        PAL_POSINF,     0 },
    };


    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        logf_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }
    
    logf_test1_validate_isnan(PAL_NEGINF);
    logf_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
