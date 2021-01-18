// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that acosf return the correct values
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
 * acosf_test1_validate
 *
 * test validation function
 */
void __cdecl acosf_test1_validate(float value, float expected, float variance)
{
    float result = acosf(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("acosf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * acosf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl acosf_test1_validate_isnan(float value)
{
    float result = acosf(value);

    if (!_isnanf(result))
    {
        Fail("acosf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_acosf_test1_paltest_acosf_test1, "c_runtime/acosf/test1/paltest_acosf_test1")
{
    struct test tests[] = 
    {
        /* value            expected         variance */
        { -1,               3.14159265f,     PAL_EPSILON * 10 },      // expected:  pi
        { -0.911733915f,    2.71828183f,     PAL_EPSILON * 10 },      // expected:  e
        { -0.668201510f,    2.30258509f,     PAL_EPSILON * 10 },      // expected:  ln(10)
        {  0,               1.57079633f,     PAL_EPSILON * 10 },      // expected:  pi / 2
        {  0.127751218f,    1.44269504f,     PAL_EPSILON * 10 },      // expected:  logf2(e)
        {  0.155943695f,    1.41421356f,     PAL_EPSILON * 10 },      // expected:  sqrtf(2)
        {  0.428125148f,    1.12837917f,     PAL_EPSILON * 10 },      // expected:  2 / sqrtf(pi)
        {  0.540302306f,    1,               PAL_EPSILON * 10 },
        {  0.707106781f,    0.785398163f,    PAL_EPSILON },           // expected:  pi / 4,         value:  1 / sqrtf(2)
        {  0.760244597f,    0.707106781f,    PAL_EPSILON },           // expected:  1 / sqrtf(2)
        {  0.769238901f,    0.693147181f,    PAL_EPSILON },           // expected:  ln(2)
        {  0.804109828f,    0.636619772f,    PAL_EPSILON },           // expected:  2 / pi
        {  0.907167129f,    0.434294482f,    PAL_EPSILON },           // expected:  logf10f(e)
        {  0.949765715f,    0.318309886f,    PAL_EPSILON },           // expected:  1 / pi
        {  1,               0,               PAL_EPSILON },
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        acosf_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }
    
    acosf_test1_validate_isnan(PAL_NEGINF);
    acosf_test1_validate_isnan(PAL_NAN);
    acosf_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
