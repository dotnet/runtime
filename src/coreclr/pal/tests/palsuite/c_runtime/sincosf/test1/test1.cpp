// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that sincosf return the correct values
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
    float value;         /* value to test the function with */
    float expected_sin;  /* expected sin result */
    float expected_cos;  /* expected cos result */
    float variance_sin;  /* maximum delta between the expected and actual sin result */
    float variance_cos;  /* maximum delta between the expected and actual cos result */
};

/**
 * sincosf_test1_validate
 *
 * test validation function
 */
void __cdecl sincosf_test1_validate(float value, float expected_sin, float expected_cos, float variance_sin, float variance_cos)
{
    float result_sin, result_cos;
    sincosf(value, &result_sin, &result_cos);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta_sin = fabsf(result_sin - expected_sin);
    float delta_cos = fabsf(result_cos - expected_cos);

    if ((delta_sin > variance_sin) || (delta_cos > variance_cos))
    {
        Fail("sincosf(%g) returned (%10.9g, %10.9g) when it should have returned (%10.9g, %10.9g)",
             value, result_sin, result_cos, expected_sin, expected_cos);
    }
}

/**
 * sincosf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl sincosf_test1_validate_isnan(float value)
{
    float result_sin, result_cos;
    sincosf(value, &result_sin, &result_cos);

    if (!_isnanf(result_sin) || !_isnanf(result_cos))
    {
        Fail("sincosf(%g) returned (%10.9g, %10.9g) when it should have returned (%10.9g, %10.9g)",
             value, result_sin, result_cos, PAL_NAN, PAL_NAN);
    }
}

/**
 * main
 *
 * executable entry point
 */
PALTEST(c_runtime_sincosf_test1_paltest_sincosf_test1, "c_runtime/sincosf/test1/paltest_sincosf_test1")
{
    struct test tests[] =
    {
        /* value            expected_sin      expected_cos     variance_sin         variance_cos */
        {  0,               0,                1,               PAL_EPSILON,         PAL_EPSILON * 10 },
        {  0.318309886f,    0.312961796f,     0.949765715f,    PAL_EPSILON,         PAL_EPSILON },       // value:  1 / pi
        {  0.434294482f,    0.420770483f,     0.907167129f,    PAL_EPSILON,         PAL_EPSILON },       // value:  log10f(e)
        {  0.636619772f,    0.594480769f,     0.804109828f,    PAL_EPSILON,         PAL_EPSILON },       // value:  2 / pi
        {  0.693147181f,    0.638961276f,     0.769238901f,    PAL_EPSILON,         PAL_EPSILON },       // value:  ln(2)
        {  0.707106781f,    0.649636939f,     0.760244597f,    PAL_EPSILON,         PAL_EPSILON },       // value:  1 / sqrtf(2)
        {  0.785398163f,    0.707106781f,     0.707106781f,    PAL_EPSILON,         PAL_EPSILON },       // value:  pi / 4,         expected_sin: 1 / sqrtf(2),    expected_cos:  1
        {  1,               0.841470985f,     0.540302306f,    PAL_EPSILON,         PAL_EPSILON },
        {  1.12837917f,     0.903719457f,     0.428125148f,    PAL_EPSILON,         PAL_EPSILON },       // value:  2 / sqrtf(pi)
        {  1.41421356f,     0.987765946f,     0.155943695f,    PAL_EPSILON,         PAL_EPSILON },       // value:  sqrtf(2)
        {  1.44269504f,     0.991806244f,     0.127751218f,    PAL_EPSILON,         PAL_EPSILON },       // value:  logf2(e)
        {  1.57079633f,     1,                0,               PAL_EPSILON * 10,    PAL_EPSILON },       // value:  pi / 2
        {  2.30258509f,     0.743980337f,    -0.668201510f,    PAL_EPSILON,         PAL_EPSILON },       // value:  ln(10)
        {  2.71828183f,     0.410781291f,    -0.911733918f,    PAL_EPSILON,         PAL_EPSILON },       // value:  e
        {  3.14159265f,     0,               -1,               PAL_EPSILON,         PAL_EPSILON * 10 },  // value:  pi
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        sincosf_test1_validate( tests[i].value,  tests[i].expected_sin, tests[i].expected_cos, tests[i].variance_sin, tests[i].variance_cos);
        sincosf_test1_validate(-tests[i].value, -tests[i].expected_sin, tests[i].expected_cos, tests[i].variance_sin, tests[i].variance_cos);
    }

    sincosf_test1_validate_isnan(PAL_NEGINF);
    sincosf_test1_validate_isnan(PAL_NAN);
    sincosf_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
