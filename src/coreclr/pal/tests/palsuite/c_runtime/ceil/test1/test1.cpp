// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests ceil with simple positive and negative values.  Also tests 
**          extreme cases like extremely small values and positive and 
**          negative infinity.  Makes sure that calling ceil on NaN returns 
**          NaN
**
**==========================================================================*/

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
 * ceil_test1_validate
 *
 * test validation function
 */
void __cdecl ceil_test1_validate(double value, double expected, double variance)
{
    double result = ceil(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("ceil(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * ceil_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl ceil_test1_validate_isnan(double value)
{
    double result = ceil(value);

    if (!_isnan(result))
    {
        Fail("ceil(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_ceil_test1_paltest_ceil_test1, "c_runtime/ceil/test1/paltest_ceil_test1")
{
    struct test tests[] = 
    {
        /* value                   expected           variance */
        {  0.31830988618379067,    1,                 PAL_EPSILON * 10 },     // value:  1 / pi
        {  0.43429448190325183,    1,                 PAL_EPSILON * 10 },     // value:  log10(e)
        {  0.63661977236758134,    1,                 PAL_EPSILON * 10 },     // value:  2 / pi
        {  0.69314718055994531,    1,                 PAL_EPSILON * 10 },     // value:  ln(2)
        {  0.70710678118654752,    1,                 PAL_EPSILON * 10 },     // value:  1 / sqrt(2)
        {  0.78539816339744831,    1,                 PAL_EPSILON * 10 },     // value:  pi / 4
        {  1.1283791670955126,     2,                 PAL_EPSILON * 10 },     // value:  2 / sqrt(pi)
        {  1.4142135623730950,     2,                 PAL_EPSILON * 10 },     // value:  sqrt(2)
        {  1.4426950408889634,     2,                 PAL_EPSILON * 10 },     // value:  log2(e)
        {  1.5707963267948966,     2,                 PAL_EPSILON * 10 },     // value:  pi / 2
        {  2.3025850929940457,     3,                 PAL_EPSILON * 10 },     // value:  ln(10)
        {  2.7182818284590452,     3,                 PAL_EPSILON * 10 },     // value:  e
        {  3.1415926535897932,     4,                 PAL_EPSILON * 10 },     // value:  pi
        {  PAL_POSINF,             PAL_POSINF,        0 }
    };

    /* PAL initialization */
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }
    
    ceil_test1_validate( 0,    0, PAL_EPSILON);
    ceil_test1_validate(-0.0,  0, PAL_EPSILON);
    
    ceil_test1_validate( 1,    1, PAL_EPSILON * 10);
    ceil_test1_validate(-1.0, -1, PAL_EPSILON * 10);
    
    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        ceil_test1_validate( tests[i].value, tests[i].expected,     tests[i].variance);
        ceil_test1_validate(-tests[i].value, 1 - tests[i].expected, tests[i].variance);
    }
    
    ceil_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
