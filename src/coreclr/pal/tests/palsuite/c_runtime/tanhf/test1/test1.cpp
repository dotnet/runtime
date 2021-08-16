// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that tanhf return the correct values
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
 * tanhf_test1_validate
 *
 * test validation function
 */
void __cdecl tanhf_test1_validate(float value, float expected, float variance)
{
    float result = tanhf(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("tanhf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, expected);
    }
}

/**
 * tanhf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl tanhf_test1_validate_isnan(float value)
{
    float result = tanhf(value);

    if (!_isnanf(result))
    {
        Fail("tanhf(%g) returned %10.9g when it should have returned %10.9g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_tanhf_test1_paltest_tanhf_test1, "c_runtime/tanhf/test1/paltest_tanhf_test1")
{
    struct test tests[] = 
    {
        /* value            expected         variance */
        {  0,               0,               PAL_EPSILON },
        {  0.318309886f,    0.307977913f,    PAL_EPSILON },       // value:  1 / pi
        {  0.434294482f,    0.408904012f,    PAL_EPSILON },       // value:  log10f(e)
        {  0.636619772f,    0.562593600f,    PAL_EPSILON },       // value:  2 / pi
        {  0.693147181f,    0.6f,            PAL_EPSILON },       // value:  ln(2)
        {  0.707106781f,    0.608859365f,    PAL_EPSILON },       // value:  1 / sqrtf(2)
        {  0.785398163f,    0.655794203f,    PAL_EPSILON },       // value:  pi / 4
        {  1,               0.761594156f,    PAL_EPSILON },
        {  1.12837917f,     0.810463806f,    PAL_EPSILON },       // value:  2 / sqrtf(pi)
        {  1.41421356f,     0.888385562f,    PAL_EPSILON },       // value:  sqrtf(2)
        {  1.44269504f,     0.894238946f,    PAL_EPSILON },       // value:  logf2(e)
        {  1.57079633f,     0.917152336f,    PAL_EPSILON },       // value:  pi / 2
        {  2.30258509f,     0.980198020f,    PAL_EPSILON },       // value:  ln(10)
        {  2.71828183f,     0.991328916f,    PAL_EPSILON },       // value:  e
        {  3.14159265f,     0.996272076f,    PAL_EPSILON },       // value:  pi
        {  PAL_POSINF,      1,               PAL_EPSILON * 10 }
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        tanhf_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        tanhf_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }
    
    tanhf_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
