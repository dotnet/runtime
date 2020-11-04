// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that atan2 returns correct values for a subset of values.
**          Tests with positive and negative values of x and y to ensure
**          atan2 is returning results from the correct quadrant.
**
**===================================================================*/

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

struct test
{
    double y;         /* second component of the value to test the function with */
    double x;         /* first component of the value to test the function with */
    double expected;  /* expected result */
    double variance;  /* maximum delta between the expected and actual result */
};

/**
 * atan2_test1_validate
 *
 * test validation function
 */
void __cdecl atan2_test1_validate(double y, double x, double expected, double variance)
{
    double result = atan2(y, x);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("atan2(%g, %g) returned %20.17g when it should have returned %20.17g",
             y, x, result, expected);
    }
}

/**
 * atan2_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl atan2_test1_validate_isnan(double y, double x)
{
    double result = atan2(y, x);

    if (!_isnan(result))
    {
        Fail("atan2(%g, %g) returned %20.17g when it should have returned %20.17g",
             y, x, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_atan2_test1_paltest_atan2_test1, "c_runtime/atan2/test1/paltest_atan2_test1")
{
    struct test tests[] = 
    {
        /* y                       x                       expected                variance */
        {  0,                      PAL_POSINF,             0,                      PAL_EPSILON },
        {  0,                      0,                      0,                      PAL_EPSILON },
        {  0.31296179620778659,    0.94976571538163866,    0.31830988618379067,    PAL_EPSILON },           // expected:  1 / pi
        {  0.42077048331375735,    0.90716712923909839,    0.43429448190325183,    PAL_EPSILON },           // expected:  log10(e)
        {  0.59448076852482208,    0.80410982822879171,    0.63661977236758134,    PAL_EPSILON },           // expected:  2 / pi
        {  0.63896127631363480,    0.76923890136397213,    0.69314718055994531,    PAL_EPSILON },           // expected:  ln(2)
        {  0.64963693908006244,    0.76024459707563015,    0.70710678118654752,    PAL_EPSILON },           // expected:  1 / sqrt(2)
        {  0.70710678118654752,    0.70710678118654752,    0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4,         value:  1 / sqrt(2)
        {  1,                      1,                      0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4
        {  PAL_POSINF,             PAL_POSINF,             0.78539816339744831,    PAL_EPSILON },           // expected:  pi / 4
        {  0.84147098480789651,    0.54030230586813972,    1,                      PAL_EPSILON * 10 },
        {  0.90371945743584630,    0.42812514788535792,    1.1283791670955126,     PAL_EPSILON * 10 },      // expected:  2 / sqrt(pi)
        {  0.98776594599273553,    0.15594369476537447,    1.4142135623730950,     PAL_EPSILON * 10 },      // expected:  sqrt(2)
        {  0.99180624439366372,    0.12775121753523991,    1.4426950408889634,     PAL_EPSILON * 10 },      // expected:  log2(e)
        {  1,                      0,                      1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
        {  PAL_POSINF,             0,                      1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
        {  PAL_POSINF,             1,                      1.5707963267948966,     PAL_EPSILON * 10 },      // expected:  pi / 2
        {  0.74398033695749319,   -0.66820151019031295,    2.3025850929940457,     PAL_EPSILON * 10 },      // expected:  ln(10)
        {  0.41078129050290870,   -0.91173391478696510,    2.7182818284590452,     PAL_EPSILON * 10 },      // expected:  e
        {  0,                     -1,                      3.1415926535897932,     PAL_EPSILON * 10 },      // expected:  pi
        {  1,                      PAL_POSINF,             0,                      PAL_EPSILON },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        const double pi = 3.1415926535897932;
        
        atan2_test1_validate( tests[i].y,  tests[i].x,  tests[i].expected,      tests[i].variance);
        atan2_test1_validate(-tests[i].y,  tests[i].x, -tests[i].expected,      tests[i].variance);
        atan2_test1_validate( tests[i].y, -tests[i].x,  pi - tests[i].expected, tests[i].variance);
        atan2_test1_validate(-tests[i].y, -tests[i].x,  tests[i].expected - pi, tests[i].variance);
    }
    
    atan2_test1_validate_isnan(PAL_NEGINF, PAL_NAN);
    atan2_test1_validate_isnan(PAL_NAN,    PAL_NEGINF);
    atan2_test1_validate_isnan(PAL_NAN,    PAL_POSINF);
    atan2_test1_validate_isnan(PAL_POSINF, PAL_NAN);
    
    atan2_test1_validate_isnan(PAL_NAN, -1);
    atan2_test1_validate_isnan(PAL_NAN, -0.0);
    atan2_test1_validate_isnan(PAL_NAN,  0);
    atan2_test1_validate_isnan(PAL_NAN,  1);
    
    atan2_test1_validate_isnan(-1,   PAL_NAN);
    atan2_test1_validate_isnan(-0.0, PAL_NAN);
    atan2_test1_validate_isnan( 0,   PAL_NAN);
    atan2_test1_validate_isnan( 1,   PAL_NAN);
    
    atan2_test1_validate_isnan(PAL_NAN, PAL_NAN);

    PAL_Terminate();
    return PASS;
}
