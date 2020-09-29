// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that asinf return the correct values
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               fabs
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
 * asinf_test1_validate
 *
 * test validation function
 */
void __cdecl asinf_test1_validate(float value, float expected, float variance)
{
    float result = asinf(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("asinf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * asinf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl asinf_test1_validate_isnan(float value)
{
    float result = asinf(value);

    if (!_isnanf(result))
    {
        Fail("asinf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * asinf_test1_validate
 *
 * test validation function for values returning +INF
 */
void __cdecl asinf_test1_validate_isinf_positive(float value)
{
    float result = asinf(value);

    if (result != PAL_POSINF)
    {
        Fail("asinf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_POSINF);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_asinf_test1_paltest_asinf_test1, "c_runtime/asinf/test1/paltest_asinf_test1")
{
    struct test tests[] = 
    {
        /* value            expected         variance */
        {  0,               0,               PAL_EPSILON },
        {  0.312961796f,    0.318309886f,    PAL_EPSILON },           // expected:  1 / pi
        {  0.410781291f,    0.423310825f,    PAL_EPSILON },           // expected:  pi - e
        {  0.420770483f,    0.434294482f,    PAL_EPSILON },           // expected:  logf10f(e)
        {  0.594480769f,    0.636619772f,    PAL_EPSILON },           // expected:  2 / pi
        {  0.638961276f,    0.693147181f,    PAL_EPSILON },           // expected:  ln(2)
        {  0.649636939f,    0.707106781f,    PAL_EPSILON },           // expected:  1 / sqrtf(2)
        {  0.707106781f,    0.785398163f,    PAL_EPSILON },           // expected:  pi / 4,         value: 1 / sqrtf(2)
        {  0.743980337f,    0.839007561f,    PAL_EPSILON },           // expected:  pi - ln(10)
        {  0.841470985f,    1,               PAL_EPSILON * 10 },
        {  0.903719457f,    1.12837917f,     PAL_EPSILON * 10 },      // expected:  2 / sqrtf(pi)
        {  0.987765946f,    1.41421356f,     PAL_EPSILON * 10 },      // expected:  sqrtf(2)
        {  0.991806244f,    1.44269504f,     PAL_EPSILON * 10 },      // expected:  logf2(e)
        {  1,               1.57079633f,     PAL_EPSILON * 10 },      // expected:  pi / 2
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        asinf_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        asinf_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }
    
    asinf_test1_validate_isnan(PAL_NEGINF);
    asinf_test1_validate_isnan(PAL_NAN);
    asinf_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
