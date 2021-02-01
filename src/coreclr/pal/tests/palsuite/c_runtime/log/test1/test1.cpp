// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests log with a normal set of values.
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
 * log_test1_validate
 *
 * test validation function
 */
void __cdecl log_test1_validate(double value, double expected, double variance)
{
    double result = log(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("log(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * log_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl log_test1_validate_isnan(double value)
{
    double result = log(value);

    if (!_isnan(result))
    {
        Fail("log(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_log_test1_paltest_log_test1, "c_runtime/log/test1/paltest_log_test1")
{
    struct test tests[] = 
    {
        /* value                       expected               variance */
        {  0,                          PAL_NEGINF,            0 },
        {  0.043213918263772250,      -3.1415926535897932,    PAL_EPSILON * 10 },   // expected: -(pi)
        {  0.065988035845312537,      -2.7182818284590452,    PAL_EPSILON * 10 },   // expected: -(e)
        {  0.1,                       -2.3025850929940457,    PAL_EPSILON * 10 },   // expected: -(ln(10))
        {  0.20787957635076191,       -1.5707963267948966,    PAL_EPSILON * 10 },   // expected: -(pi / 2)
        {  0.23629008834452270,       -1.4426950408889634,    PAL_EPSILON * 10 },   // expected: -(log2(e))
        {  0.24311673443421421,       -1.4142135623730950,    PAL_EPSILON * 10 },   // expected: -(sqrt(2))
        {  0.32355726390307110,       -1.1283791670955126,    PAL_EPSILON * 10 },   // expected: -(2 / sqrt(pi))
        {  0.36787944117144232,       -1,                     PAL_EPSILON * 10 },   // expected: -(1)
        {  0.45593812776599624,       -0.78539816339744831,   PAL_EPSILON },        // expected: -(pi / 4)
        {  0.49306869139523979,       -0.70710678118654752,   PAL_EPSILON },        // expected: -(1 / sqrt(2))
        {  0.5,                       -0.69314718055994531,   PAL_EPSILON },        // expected: -(ln(2))
        {  0.52907780826773535,       -0.63661977236758134,   PAL_EPSILON },        // expected: -(2 / pi)
        {  0.64772148514180065,       -0.43429448190325183,   PAL_EPSILON },        // expected: -(log10(e))
        {  0.72737734929521647,       -0.31830988618379067,   PAL_EPSILON },        // expected: -(1 / pi)
        {  1,                          0,                     PAL_EPSILON },
        {  1.3748022274393586,         0.31830988618379067,   PAL_EPSILON },        // expected:  1 / pi
        {  1.5438734439711811,         0.43429448190325183,   PAL_EPSILON },        // expected:  log10(e)
        {  1.8900811645722220,         0.63661977236758134,   PAL_EPSILON },        // expected:  2 / pi
        {  2,                          0.69314718055994531,   PAL_EPSILON },        // expected:  ln(2)
        {  2.0281149816474725,         0.70710678118654752,   PAL_EPSILON },        // expected:  1 / sqrt(2)
        {  2.1932800507380155,         0.78539816339744831,   PAL_EPSILON },        // expected:  pi / 4
        {  2.7182818284590452,         1,                     PAL_EPSILON * 10 },   //                               value: e
        {  3.0906430223107976,         1.1283791670955126,    PAL_EPSILON * 10 },   // expected:  2 / sqrt(pi)
        {  4.1132503787829275,         1.4142135623730950,    PAL_EPSILON * 10 },   // expected:  sqrt(2)
        {  4.2320861065570819,         1.4426950408889634,    PAL_EPSILON * 10 },   // expected:  log2(e)
        {  4.8104773809653517,         1.5707963267948966,    PAL_EPSILON * 10 },   // expected:  pi / 2
        {  10,                         2.3025850929940457,    PAL_EPSILON * 10 },   // expected:  ln(10)
        {  15.154262241479264,         2.7182818284590452,    PAL_EPSILON * 10 },   // expected:  e
        {  23.140692632779269,         3.1415926535897932,    PAL_EPSILON * 10 },   // expected:  pi
        {  PAL_POSINF,                 PAL_POSINF,            0 },
    };


    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        log_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }
    
    log_test1_validate_isnan(PAL_NEGINF);
    log_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
