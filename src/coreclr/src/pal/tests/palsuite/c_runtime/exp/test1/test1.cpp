// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests exp with a normal set of values.
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
 * exp_test1_validate
 *
 * test validation function
 */
void __cdecl exp_test1_validate(double value, double expected, double variance)
{
    double result = exp(value);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("exp(%g) returned %20.17g when it should have returned %20.17g",
             value, result, expected);
    }
}

/**
 * exp_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl exp_test1_validate_isnan(double value)
{
    double result = exp(value);

    if (!_isnan(result))
    {
        Fail("exp(%g) returned %20.17g when it should have returned %20.17g",
             value, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_exp_test1_paltest_exp_test1, "c_runtime/exp/test1/paltest_exp_test1")
{
    struct test tests[] = 
    {
        /* value                   expected                 variance */
        { PAL_NEGINF,              0,                       PAL_EPSILON },
        { -3.1415926535897932,     0.043213918263772250,    PAL_EPSILON / 10 },   // value: -(pi)
        { -2.7182818284590452,     0.065988035845312537,    PAL_EPSILON / 10 },   // value: -(e)
        { -2.3025850929940457,     0.1,                     PAL_EPSILON },        // value: -(ln(10))
        { -1.5707963267948966,     0.20787957635076191,     PAL_EPSILON },        // value: -(pi / 2)
        { -1.4426950408889634,     0.23629008834452270,     PAL_EPSILON },        // value: -(log2(e))
        { -1.4142135623730950,     0.24311673443421421,     PAL_EPSILON },        // value: -(sqrt(2))
        { -1.1283791670955126,     0.32355726390307110,     PAL_EPSILON },        // value: -(2 / sqrt(pi))
        { -1,                      0.36787944117144232,     PAL_EPSILON },        // value: -(1)
        { -0.78539816339744831,    0.45593812776599624,     PAL_EPSILON },        // value: -(pi / 4)
        { -0.70710678118654752,    0.49306869139523979,     PAL_EPSILON },        // value: -(1 / sqrt(2))
        { -0.69314718055994531,    0.5,                     PAL_EPSILON },        // value: -(ln(2))
        { -0.63661977236758134,    0.52907780826773535,     PAL_EPSILON },        // value: -(2 / pi)
        { -0.43429448190325183,    0.64772148514180065,     PAL_EPSILON },        // value: -(log10(e))
        { -0.31830988618379067,    0.72737734929521647,     PAL_EPSILON },        // value: -(1 / pi)
        {  0,                      1,                       PAL_EPSILON * 10 },
        {  0.31830988618379067,    1.3748022274393586,      PAL_EPSILON * 10 },   // value:  1 / pi
        {  0.43429448190325183,    1.5438734439711811,      PAL_EPSILON * 10 },   // value:  log10(e)
        {  0.63661977236758134,    1.8900811645722220,      PAL_EPSILON * 10 },   // value:  2 / pi
        {  0.69314718055994531,    2,                       PAL_EPSILON * 10 },   // value:  ln(2)
        {  0.70710678118654752,    2.0281149816474725,      PAL_EPSILON * 10 },   // value:  1 / sqrt(2)
        {  0.78539816339744831,    2.1932800507380155,      PAL_EPSILON * 10 },   // value:  pi / 4
        {  1,                      2.7182818284590452,      PAL_EPSILON * 10 },   //                           expected: e
        {  1.1283791670955126,     3.0906430223107976,      PAL_EPSILON * 10 },   // value:  2 / sqrt(pi)
        {  1.4142135623730950,     4.1132503787829275,      PAL_EPSILON * 10 },   // value:  sqrt(2)
        {  1.4426950408889634,     4.2320861065570819,      PAL_EPSILON * 10 },   // value:  log2(e)
        {  1.5707963267948966,     4.8104773809653517,      PAL_EPSILON * 10 },   // value:  pi / 2
        {  2.3025850929940457,     10,                      PAL_EPSILON * 100 },  // value:  ln(10)
        {  2.7182818284590452,     15.154262241479264,      PAL_EPSILON * 100 },  // value:  e
        {  3.1415926535897932,     23.140692632779269,      PAL_EPSILON * 100 },  // value:  pi
        {  PAL_POSINF,             PAL_POSINF,              0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        exp_test1_validate(tests[i].value, tests[i].expected, tests[i].variance);
    }

    exp_test1_validate_isnan(PAL_NAN);

    PAL_Terminate();
    return PASS;
}
