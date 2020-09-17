// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that atan return the correct values
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
 * atan_test1_validate
 *
 * test validation function
 */
void __cdecl atan_test1_validate(double value, double expected, double variance)
{
    double result = atan(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("atan(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * atan_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl atan_test1_validate_isnan(double value)
{
    double result = atan(value);

    if (!_isnan(result))
    {
        Fail("atan(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_atan_test1_paltest_atan_test1, "c_runtime/atan/test1/paltest_atan_test1")
{
    struct test tests[] = 
    {
        /* value                   expected                variance */
        {  0,                      0,                      PAL_EPSILON },
        {  0.32951473309607836,    0.31830988618379067,    PAL_EPSILON },           // expected:  1 / pi
        {  0.45054953406980750,    0.42331082513074800,    PAL_EPSILON },           // expected:  pi - e
        {  0.46382906716062964,    0.43429448190325183,    PAL_EPSILON },           // expected:  log10(e)
        {  0.73930295048660405,    0.63661977236758134,    PAL_EPSILON },           // expected:  2 / pi
        {  0.83064087786078395,    0.69314718055994531,    PAL_EPSILON },           // expected:  ln(2)
        {  0.85451043200960189,    0.70710678118654752,    PAL_EPSILON },           // expected:  1 / sqrt(2)
        {  1,                      0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4
        {  1.1134071468135374,     0.83900756059574755,    PAL_EPSILON },           // expected:  pi - ln(10)
        {  1.5574077246549022,     1,                      PAL_EPSILON * 10 },
        {  2.1108768356626451,     1.1283791670955126,     PAL_EPSILON * 10 },      // expected:  2 / sqrt(pi)
        {  6.3341191670421916,     1.4142135623730950,     PAL_EPSILON * 10 },      // expected:  sqrt(2)
        {  7.7635756709721848,     1.4426950408889634,     PAL_EPSILON * 10 },      // expected:  log2(e)
        {  PAL_POSINF,             1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        atan_test1_validate( tests[i].value,  tests[i].expected, tests[i].variance);
        atan_test1_validate(-tests[i].value, -tests[i].expected, tests[i].variance);
    }

    atan_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
