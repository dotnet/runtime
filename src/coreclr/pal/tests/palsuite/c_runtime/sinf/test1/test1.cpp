// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that sinf return the correct values
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
 * sinf_test1_validate
 *
 * test validation function
 */
void __cdecl sinf_test1_validate(float value, float expected, float variance)
{
    float result = sinf(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("sinf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * sinf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl sinf_test1_validate_isnan(float value)
{
    float result = sinf(value);

    if (!_isnanf(result))
    {
        Fail("sinf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_sinf_test1_paltest_sinf_test1, "c_runtime/sinf/test1/paltest_sinf_test1")
{
    struct test tests[] = 
    {
        /* value            expected         variance */
        {  0,               0,               PAL_EPSILON },
        {  0.318309886f,    0.312961796f,    PAL_EPSILON },       // value:  1 / pi
        {  0.434294482f,    0.420770483f,    PAL_EPSILON },       // value:  log10f(e)
        {  0.636619772f,    0.594480769f,    PAL_EPSILON },       // value:  2 / pi
        {  0.693147181f,    0.638961276f,    PAL_EPSILON },       // value:  ln(2)
        {  0.707106781f,    0.649636939f,    PAL_EPSILON },       // value:  1 / sqrtf(2)
        {  0.785398163f,    0.707106781f,    PAL_EPSILON },       // value:  pi / 4,         expected: 1 / sqrtf(2)
        {  1,               0.841470985f,    PAL_EPSILON },
        {  1.12837917f,     0.903719457f,    PAL_EPSILON },       // value:  2 / sqrtf(pi)
        {  1.41421356f,     0.987765946f,    PAL_EPSILON },       // value:  sqrtf(2)
        {  1.44269504f,     0.991806244f,    PAL_EPSILON },       // value:  logf2(e)
        {  1.57079633f,     1,               PAL_EPSILON * 10 },  // value:  pi / 2
        {  2.30258509f,     0.743980337f,    PAL_EPSILON },       // value:  ln(10)
        {  2.71828183f,     0.410781291f,    PAL_EPSILON },       // value:  e
        {  3.14159265f,     0,               PAL_EPSILON },       // value:  pi
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        sinf_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        sinf_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }
    
    sinf_test1_validate_isnan(PAL_NEGINF);
    sinf_test1_validate_isnan(PAL_NAN);
    sinf_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
