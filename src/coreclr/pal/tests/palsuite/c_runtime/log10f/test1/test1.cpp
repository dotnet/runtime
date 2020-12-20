// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that log10f returns correct values.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               fabs
**               _isnanf
** 
**===========================================================================*/

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
 * log10f_test1_validate
 *
 * test validation function
 */
void __cdecl log10f_test1_validate(float value, float expected, float variance)
{
    float result = log10f(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("log10f(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * log10f_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl log10f_test1_validate_isnan(float value)
{
    float result = log10f(value);

    if (!_isnanf(result))
    {
        Fail("log10f(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_log10f_test1_paltest_log10f_test1, "c_runtime/log10f/test1/paltest_log10f_test1")
{
    struct test tests[] = 
    {
        /* value                expected        variance */
        {  0,                   PAL_NEGINF,     0 },
        {  0.000721784159f,    -3.14159265f,    PAL_EPSILON * 10 },   // expected: -(pi)
        {  0.00191301410f,     -2.71828183f,    PAL_EPSILON * 10 },   // expected: -(e)
        {  0.00498212830f,     -2.30258509f,    PAL_EPSILON * 10 },   // expected: -(ln(10))
        {  0.0268660410f,      -1.57079633f,    PAL_EPSILON * 10 },   // expected: -(pi / 2)
        {  0.0360831928f,      -1.44269504f,    PAL_EPSILON * 10 },   // expected: -(logf2(e))
        {  0.0385288847f,      -1.41421356f,    PAL_EPSILON * 10 },   // expected: -(sqrtf(2))
        {  0.0744082059f,      -1.12837917f,    PAL_EPSILON * 10 },   // expected: -(2 / sqrtf(pi))
        {  0.1f,               -1,              PAL_EPSILON * 10 },   // expected: -(1)
        {  0.163908636f,       -0.785398163f,   PAL_EPSILON },        // expected: -(pi / 4)
        {  0.196287760f,       -0.707106781f,   PAL_EPSILON },        // expected: -(1 / sqrtf(2))
        {  0.202699566f,       -0.693147181f,   PAL_EPSILON },        // expected: -(ln(2))
        {  0.230876765f,       -0.636619772f,   PAL_EPSILON },        // expected: -(2 / pi)
        {  0.367879441f,       -0.434294482f,   PAL_EPSILON },        // expected: -(log10f(e))
        {  0.480496373f,       -0.318309886f,   PAL_EPSILON },        // expected: -(1 / pi)
        {  1,                   0,              PAL_EPSILON },
        {  2.08118116f,         0.318309886f,   PAL_EPSILON },        // expected:  1 / pi
        {  2.71828183f,         0.434294482f,   PAL_EPSILON },        // expected:  log10f(e)           value: e
        {  4.33131503f,         0.636619772f,   PAL_EPSILON },        // expected:  2 / pi
        {  4.93340967f,         0.693147181f,   PAL_EPSILON },        // expected:  ln(2)
        {  5.09456117f,         0.707106781f,   PAL_EPSILON },        // expected:  1 / sqrtf(2)
        {  6.10095980f,         0.785398163f,   PAL_EPSILON },        // expected:  pi / 4
        {  10,                  1,              PAL_EPSILON * 10 },
        {  13.4393779f,         1.12837917f,    PAL_EPSILON * 10 },   // expected:  2 / sqrtf(pi)
        {  25.9545535f,         1.41421356f,    PAL_EPSILON * 10 },   // expected:  sqrtf(2)
        {  27.7137338f,         1.44269504f,    PAL_EPSILON * 10 },   // expected:  logf2(e)
        {  37.2217105f,         1.57079633f,    PAL_EPSILON * 10 },   // expected:  pi / 2
        {  200.717432f,         2.30258509f,    PAL_EPSILON * 10 },   // expected:  ln(10)
        {  522.735300f,         2.71828183f,    PAL_EPSILON * 10 },   // expected:  e
        {  1385.45573f,         3.14159265f,    PAL_EPSILON * 10 },   // expected:  pi
        {  PAL_POSINF,          PAL_POSINF,     0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        log10f_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }
    
    log10f_test1_validate_isnan(PAL_NEGINF);
    log10f_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
