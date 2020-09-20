// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that fmod return the correct values
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
    double numerator;    /* second component of the value to test the function with */
    double denominator;  /* first component of the value to test the function with */
    double expected;     /* expected result */
    double variance;     /* maximum delta between the expected and actual result */
};

/**
 * fmod_test1_validate
 *
 * test validation function
 */
void __cdecl fmod_test1_validate(double numerator, double denominator, double expected, double variance)
{
    double result = fmod(numerator, denominator);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("fmod(%g, %g) returned %20.17g when it should have returned %20.17g",
             numerator, denominator, result, expected);
    }
}

/**
 * fmod_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl fmod_test1_validate_isnan(double numerator, double denominator)
{
    double result = fmod(numerator, denominator);

    if (!_isnan(result))
    {
        Fail("fmod(%g, %g) returned %20.17g when it should have returned %20.17g",
             numerator, denominator, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_fmod_test1_paltest_fmod_test1, "c_runtime/fmod/test1/paltest_fmod_test1")
{
    struct test tests[] = 
    {
        /* numerator               denominator             expected                 variance */
        {  0,                      PAL_POSINF,             0,                       PAL_EPSILON },
        {  0.31296179620778659,    0.94976571538163866,    0.31296179620778658,     PAL_EPSILON },
        {  0.42077048331375735,    0.90716712923909839,    0.42077048331375733,     PAL_EPSILON },
        {  0.59448076852482208,    0.80410982822879171,    0.59448076852482212,     PAL_EPSILON },
        {  0.63896127631363480,    0.76923890136397213,    0.63896127631363475,     PAL_EPSILON },
        {  0.64963693908006244,    0.76024459707563015,    0.64963693908006248,     PAL_EPSILON },
        {  0.70710678118654752,    0.70710678118654752,    0,                       PAL_EPSILON },
        {  1,                      1,                      0,                       PAL_EPSILON },
        {  0.84147098480789651,    0.54030230586813972,    0.30116867893975674,     PAL_EPSILON },
        {  0.90371945743584630,    0.42812514788535792,    0.047469161665130377,    PAL_EPSILON / 10 },
        {  0.98776594599273553,    0.15594369476537447,    0.052103777400488605,    PAL_EPSILON / 10 },
        {  0.99180624439366372,    0.12775121753523991,    0.097547721646984359,    PAL_EPSILON / 10 },
        {  0.74398033695749319,   -0.66820151019031295,    0.075778826767180285,    PAL_EPSILON / 10 },
        {  0.41078129050290870,   -0.91173391478696510,    0.41078129050290868,     PAL_EPSILON },
        {  0,                     -1,                      0,                       PAL_EPSILON },
        {  1,                      PAL_POSINF,             1,                       PAL_EPSILON * 10 },
    };


    // PAL initialization
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        fmod_test1_validate( tests[i].numerator,  tests[i].denominator,  tests[i].expected, tests[i].variance);
        fmod_test1_validate(-tests[i].numerator,  tests[i].denominator, -tests[i].expected, tests[i].variance);
        fmod_test1_validate( tests[i].numerator, -tests[i].denominator,  tests[i].expected, tests[i].variance);
        fmod_test1_validate(-tests[i].numerator, -tests[i].denominator, -tests[i].expected, tests[i].variance);
    }

    fmod_test1_validate_isnan( 0,    0);
    fmod_test1_validate_isnan(-0.0,  0);
    fmod_test1_validate_isnan( 0,   -0.0);
    fmod_test1_validate_isnan(-0.0, -0.0);
    
    fmod_test1_validate_isnan( 1,    0);
    fmod_test1_validate_isnan(-1.0,  0);
    fmod_test1_validate_isnan( 1,   -0.0);
    fmod_test1_validate_isnan(-1.0, -0.0);
    
    fmod_test1_validate_isnan(PAL_POSINF,  PAL_POSINF);
    fmod_test1_validate_isnan(PAL_NEGINF,  PAL_POSINF);
    fmod_test1_validate_isnan(PAL_POSINF, PAL_NEGINF);
    fmod_test1_validate_isnan(PAL_NEGINF, PAL_NEGINF);
    
    fmod_test1_validate_isnan(PAL_POSINF,  0);
    fmod_test1_validate_isnan(PAL_NEGINF,  0);
    fmod_test1_validate_isnan(PAL_POSINF, -0.0);
    fmod_test1_validate_isnan(PAL_NEGINF, -0.0);
    
    fmod_test1_validate_isnan(PAL_POSINF,  1);
    fmod_test1_validate_isnan(PAL_NEGINF,  1);
    fmod_test1_validate_isnan(PAL_POSINF, -1.0);
    fmod_test1_validate_isnan(PAL_NEGINF, -1.0);
    
    PAL_Terminate();
    return PASS;
}
