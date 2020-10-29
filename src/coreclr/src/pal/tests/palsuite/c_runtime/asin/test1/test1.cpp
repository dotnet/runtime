// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that asin return the correct values
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               fabs
**
**===========================================================================*/

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
 * asin_test1_validate
 *
 * test validation function
 */
void __cdecl asin_test1_validate(double value, double expected, double variance)
{
    double result = asin(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("asin(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * asin_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl asin_test1_validate_isnan(double value)
{
    double result = asin(value);

    if (!_isnan(result))
    {
        Fail("asin(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * asin_test1_validate
 *
 * test validation function for values returning +INF
 */
void __cdecl asin_test1_validate_isinf_positive(double value)
{
    double result = asin(value);

    if (result != PAL_POSINF)
    {
        Fail("asin(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_POSINF);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_asin_test1_paltest_asin_test1, "c_runtime/asin/test1/paltest_asin_test1")
{
    struct test tests[] = 
    {
        /* value                   expected                variance */
        {  0,                      0,                      PAL_EPSILON },
        {  0.31296179620778659,    0.31830988618379067,    PAL_EPSILON },           // expected:  1 / pi
        {  0.41078129050290870,    0.42331082513074800,    PAL_EPSILON },           // expected:  pi - e
        {  0.42077048331375735,    0.43429448190325183,    PAL_EPSILON },           // expected:  log10(e)
        {  0.59448076852482208,    0.63661977236758134,    PAL_EPSILON },           // expected:  2 / pi
        {  0.63896127631363480,    0.69314718055994531,    PAL_EPSILON },           // expected:  ln(2)
        {  0.64963693908006244,    0.70710678118654752,    PAL_EPSILON },           // expected:  1 / sqrt(2)
        {  0.70710678118654752,    0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4,         value: 1 / sqrt(2)
        {  0.74398033695749319,    0.83900756059574755,    PAL_EPSILON },           // expected:  pi - ln(10)
        {  0.84147098480789651,    1,                      PAL_EPSILON * 10 },
        {  0.90371945743584630,    1.1283791670955126,     PAL_EPSILON * 10 },      // expected:  2 / sqrt(pi)
        {  0.98776594599273553,    1.4142135623730950,     PAL_EPSILON * 10 },      // expected:  sqrt(2)
        {  0.99180624439366372,    1.4426950408889634,     PAL_EPSILON * 10 },      // expected:  log2(e)
        {  1,                      1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        asin_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        asin_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }
    
    asin_test1_validate_isnan(PAL_NEGINF);
    asin_test1_validate_isnan(PAL_NAN);
    asin_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
