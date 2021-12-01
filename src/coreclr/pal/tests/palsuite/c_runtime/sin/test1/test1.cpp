// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that sin return the correct values
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
 * sin_test1_validate
 *
 * test validation function
 */
void __cdecl sin_test1_validate(double value, double expected, double variance)
{
    double result = sin(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("sin(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * sin_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl sin_test1_validate_isnan(double value)
{
    double result = sin(value);

    if (!_isnan(result))
    {
        Fail("sin(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_sin_test1_paltest_sin_test1, "c_runtime/sin/test1/paltest_sin_test1")
{
    struct test tests[] = 
    {
        /* value                   expected                variance */
        {  0,                      0,                      PAL_EPSILON },
        {  0.31830988618379067,    0.31296179620778659,    PAL_EPSILON },       // value:  1 / pi
        {  0.43429448190325183,    0.42077048331375735,    PAL_EPSILON },       // value:  log10(e)
        {  0.63661977236758134,    0.59448076852482208,    PAL_EPSILON },       // value:  2 / pi
        {  0.69314718055994531,    0.63896127631363480,    PAL_EPSILON },       // value:  ln(2)
        {  0.70710678118654752,    0.64963693908006244,    PAL_EPSILON },       // value:  1 / sqrt(2)
        {  0.78539816339744831,    0.70710678118654752,    PAL_EPSILON },       // value:  pi / 4,         expected: 1 / sqrt(2)
        {  1,                      0.84147098480789651,    PAL_EPSILON },
        {  1.1283791670955126,     0.90371945743584630,    PAL_EPSILON },       // value:  2 / sqrt(pi)
        {  1.4142135623730950,     0.98776594599273553,    PAL_EPSILON },       // value:  sqrt(2)
        {  1.4426950408889634,     0.99180624439366372,    PAL_EPSILON },       // value:  log2(e)
        {  1.5707963267948966,     1,                      PAL_EPSILON * 10 },  // value:  pi / 2
        {  2.3025850929940457,     0.74398033695749319,    PAL_EPSILON },       // value:  ln(10)
        {  2.7182818284590452,     0.41078129050290870,    PAL_EPSILON },       // value:  e
        {  3.1415926535897932,     0,                      PAL_EPSILON },       // value:  pi
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        sin_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        sin_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }
    
    sin_test1_validate_isnan(PAL_NEGINF);
    sin_test1_validate_isnan(PAL_NAN);
    sin_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
