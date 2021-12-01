// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that acos return the correct values
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
 * acos_test1_validate
 *
 * test validation function
 */
void __cdecl acos_test1_validate(double value, double expected, double variance)
{
    double result = acos(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("acos(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * acos_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl acos_test1_validate_isnan(double value)
{
    double result = acos(value);

    if (!_isnan(result))
    {
        Fail("acos(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_acos_test1_paltest_acos_test1, "c_runtime/acos/test1/paltest_acos_test1")
{
    struct test tests[] = 
    {
        /* value                   expected                variance */
        { -1,                      3.1415926535897932,     PAL_EPSILON * 10 },      // expected:  pi
        { -0.91173391478696510,    2.7182818284590452,     PAL_EPSILON * 10 },      // expected:  e
        { -0.66820151019031295,    2.3025850929940457,     PAL_EPSILON * 10 },      // expected:  ln(10)
        {  0,                      1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
        {  0.12775121753523991,    1.4426950408889634,     PAL_EPSILON * 10 },      // expected:  log2(e)
        {  0.15594369476537447,    1.4142135623730950,     PAL_EPSILON * 10 },      // expected:  sqrt(2)
        {  0.42812514788535792,    1.1283791670955126,     PAL_EPSILON * 10 },      // expected:  2 / sqrt(pi)
        {  0.54030230586813972,    1,                      PAL_EPSILON * 10 },
        {  0.70710678118654752,    0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4,         value:  1 / sqrt(2)
        {  0.76024459707563015,    0.70710678118654752,    PAL_EPSILON },           // expected:  1 / sqrt(2)
        {  0.76923890136397213,    0.69314718055994531,    PAL_EPSILON },           // expected:  ln(2)
        {  0.80410982822879171,    0.63661977236758134,    PAL_EPSILON },           // expected:  2 / pi
        {  0.90716712923909839,    0.43429448190325183,    PAL_EPSILON },           // expected:  log10(e)
        {  0.94976571538163866,    0.31830988618379067,    PAL_EPSILON },           // expected:  1 / pi
        {  1,                      0,                      PAL_EPSILON },
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        acos_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }
    
    acos_test1_validate_isnan(PAL_NEGINF);
    acos_test1_validate_isnan(PAL_NAN);
    acos_test1_validate_isnan(PAL_POSINF);

    PAL_Terminate();
    return PASS;
}
