// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that atan2f returns correct values for a subset of values.
**          Tests with positive and negative values of x and y to ensure
**          atan2f is returning results from the correct quadrant.
**
**===================================================================*/

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
    float x;         /* first component of the value to test the function with */
    float y;         /* second component of the value to test the function with */
    float expected;  /* expected result */
    float variance;  /* maximum delta between the expected and actual result */
};

/**
 * powf_test1_validate
 *
 * test validation function
 */
void __cdecl powf_test1_validate(float x, float y, float expected, float variance)
{
    float result = powf(x, y);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("powf(%g, %g) returned %10.9g when it should have returned %10.9g",
             x, y, result, expected);
    }
}

/**
 * powf_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl powf_test1_validate_isnan(float x, float y)
{
    float result = powf(x, y);

    if (!_isnanf(result))
    {
        Fail("powf(%g, %g) returned %10.9g when it should have returned %10.9g",
             x, y, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_powf_test1_paltest_powf_test1, "c_runtime/powf/test1/paltest_powf_test1")
{
    struct test tests[] = 
    {
        /* x                y                expected            variance */
        {  PAL_NEGINF,      PAL_NEGINF,      0,                  PAL_EPSILON },
        {  PAL_NEGINF,      PAL_POSINF,      PAL_POSINF,         0 },

        { -10,              PAL_NEGINF,      0,                  PAL_EPSILON },
        { -10,             -1,              -0.1f,               PAL_EPSILON },
        { -10,              0,               1,                  PAL_EPSILON * 10 },
        { -10,              1,              -10,                 PAL_EPSILON * 100 },
        { -10,              PAL_POSINF,      PAL_POSINF,         0 },

        { -2.71828183f,     PAL_NEGINF,      0,                  PAL_EPSILON },          // x: -(e)
        { -2.71828183f,    -1,              -0.367879441f,       PAL_EPSILON },          // x: -(e)
        { -2.71828183f,     0,               1,                  PAL_EPSILON * 10 },     // x: -(e)
        { -2.71828183f,     1,              -2.71828183f,        PAL_EPSILON * 10 },     // x: -(e)                       expected: e
        { -2.71828183f,     PAL_POSINF,      PAL_POSINF,         0 },                    // x: -(e)

        { -1.0,             PAL_NEGINF,      1.0,                PAL_EPSILON * 10 },
        { -1.0,             PAL_POSINF,      1.0,                PAL_EPSILON * 10 },

        { -0.0,             PAL_NEGINF,      PAL_POSINF,         0 },
        { -0.0,            -1,               PAL_NEGINF,         0 },
        { -0.0f,           -0.0f,            1,                  PAL_EPSILON * 10 },
        { -0.0f,            0,               1,                  PAL_EPSILON * 10 },
        { -0.0,             1,              -0.0,                PAL_EPSILON },
        { -0.0,             PAL_POSINF,      0,                  PAL_EPSILON },

        {  PAL_NAN,        -0.0,             1.0,                PAL_EPSILON * 10 },
        {  PAL_NAN,         0,               1.0,                PAL_EPSILON * 10 },

        {  0.0,             PAL_NEGINF,      PAL_POSINF,         0 },
        {  0.0,            -1,               PAL_POSINF,         0 },
        {  0,              -0.0f,            1,                  PAL_EPSILON * 10 },
        {  0,               0,               1,                  PAL_EPSILON * 10 },
        {  0.0,             1,               0,                  PAL_EPSILON },
        {  0.0,             PAL_POSINF,      0,                  PAL_EPSILON },

        {  1,               PAL_NEGINF,      1,                  PAL_EPSILON * 10 },
        {  1,               PAL_POSINF,      1,                  PAL_EPSILON * 10 },

        {  2.71828183f,     PAL_NEGINF,      0,                  PAL_EPSILON },
        {  2.71828183f,    -3.14159265f,     0.0432139183f,      PAL_EPSILON / 10 },     // x: e     y: -(pi)
        {  2.71828183f,    -2.71828183f,     0.0659880358f,      PAL_EPSILON / 10 },     // x: e     y: -(e)
        {  2.71828183f,    -2.30258509f,     0.1f,               PAL_EPSILON },          // x: e     y: -(ln(10))
        {  2.71828183f,    -1.57079633f,     0.207879576f,       PAL_EPSILON },          // x: e     y: -(pi / 2)
        {  2.71828183f,    -1.44269504f,     0.236290088f,       PAL_EPSILON },          // x: e     y: -(logf2(e))
        {  2.71828183f,    -1.41421356f,     0.243116734f,       PAL_EPSILON },          // x: e     y: -(sqrtf(2))
        {  2.71828183f,    -1.12837917f,     0.323557264f,       PAL_EPSILON },          // x: e     y: -(2 / sqrtf(pi))
        {  2.71828183f,    -1,               0.367879441f,       PAL_EPSILON },          // x: e     y: -(1)
        {  2.71828183f,    -0.785398163f,    0.455938128f,       PAL_EPSILON },          // x: e     y: -(pi / 4)
        {  2.71828183f,    -0.707106781f,    0.493068691f,       PAL_EPSILON },          // x: e     y: -(1 / sqrtf(2))
        {  2.71828183f,    -0.693147181f,    0.5f,               PAL_EPSILON },          // x: e     y: -(ln(2))
        {  2.71828183f,    -0.636619772f,    0.529077808f,       PAL_EPSILON },          // x: e     y: -(2 / pi)
        {  2.71828183f,    -0.434294482f,    0.647721485f,       PAL_EPSILON },          // x: e     y: -(log10f(e))
        {  2.71828183f,    -0.318309886f,    0.727377349f,       PAL_EPSILON },          // x: e     y: -(1 / pi)
        {  2.71828183f,     0,               1,                  PAL_EPSILON * 10 },     // x: e
        {  2.71828183f,     0.318309886f,    1.37480223f,        PAL_EPSILON * 10 },     // x: e     y:  1 / pi
        {  2.71828183f,     0.434294482f,    1.54387344f,        PAL_EPSILON * 10 },     // x: e     y:  log10f(e)
        {  2.71828183f,     0.636619772f,    1.89008116f,        PAL_EPSILON * 10 },     // x: e     y:  2 / pi
        {  2.71828183f,     0.693147181f,    2,                  PAL_EPSILON * 10 },     // x: e     y:  ln(2)
        {  2.71828183f,     0.707106781f,    2.02811498f,        PAL_EPSILON * 10 },     // x: e     y:  1 / sqrtf(2)
        {  2.71828183f,     0.785398163f,    2.19328005f,        PAL_EPSILON * 10 },     // x: e     y:  pi / 4
        {  2.71828183f,     1,               2.71828183f,        PAL_EPSILON * 10 },     // x: e                         expected: e
        {  2.71828183f,     1.12837917f,     3.09064302f,        PAL_EPSILON * 10 },     // x: e     y:  2 / sqrtf(pi)
        {  2.71828183f,     1.41421356f,     4.11325038f,        PAL_EPSILON * 10 },     // x: e     y:  sqrtf(2)
        {  2.71828183f,     1.44269504f,     4.23208611f,        PAL_EPSILON * 10 },     // x: e     y:  logf2(e)
        {  2.71828183f,     1.57079633f,     4.81047738f,        PAL_EPSILON * 10 },     // x: e     y:  pi / 2
        {  2.71828183f,     2.30258509f,     10,                 PAL_EPSILON * 100 },    // x: e     y:  ln(10)
        {  2.71828183f,     2.71828183f,     15.1542622f,        PAL_EPSILON * 100 },    // x: e     y:  e
        {  2.71828183f,     3.14159265f,     23.1406926f,        PAL_EPSILON * 100 },    // x: e     y:  pi
        {  2.71828183f,     PAL_POSINF,      PAL_POSINF,         0 },                    // x: e
        
        {  10,              PAL_NEGINF,      0,                  0 },
        {  10,             -3.14159265f,     0.000721784159f,    PAL_EPSILON / 1000 },   //          y: -(pi)
        {  10,             -2.71828183f,     0.00191301410f,     PAL_EPSILON / 100 },    //          y: -(e)
        {  10,             -2.30258509f,     0.00498212830f,     PAL_EPSILON / 100 },    //          y: -(ln(10))
        {  10,             -1.57079633f,     0.0268660410f,      PAL_EPSILON / 10 },     //          y: -(pi / 2)
        {  10,             -1.44269504f,     0.0360831928f,      PAL_EPSILON / 10 },     //          y: -(logf2(e))
        {  10,             -1.41421356f,     0.0385288847f,      PAL_EPSILON / 10 },     //          y: -(sqrtf(2))
        {  10,             -1.12837917f,     0.0744082059f,      PAL_EPSILON / 10 },     //          y: -(2 / sqrtf(pi))
        {  10,             -1,               0.1f,               PAL_EPSILON },          //          y: -(1)
        {  10,             -0.785398163f,    0.163908636f,       PAL_EPSILON },          //          y: -(pi / 4)
        {  10,             -0.707106781f,    0.196287760f,       PAL_EPSILON },          //          y: -(1 / sqrtf(2))
        {  10,             -0.693147181f,    0.202699566f,       PAL_EPSILON },          //          y: -(ln(2))
        {  10,             -0.636619772f,    0.230876765f,       PAL_EPSILON },          //          y: -(2 / pi)
        {  10,             -0.434294482f,    0.367879441f,       PAL_EPSILON },          //          y: -(log10f(e))
        {  10,             -0.318309886f,    0.480496373f,       PAL_EPSILON },          //          y: -(1 / pi)
        {  10,              0,               1,                  PAL_EPSILON * 10 },
        {  10,              0.318309886f,    2.08118116f,        PAL_EPSILON * 10 },     //          y:  1 / pi
        {  10,              0.434294482f,    2.71828183f,        PAL_EPSILON * 10 },     //          y:  log10f(e)           expected: e
        {  10,              0.636619772f,    4.33131503f,        PAL_EPSILON * 10 },     //          y:  2 / pi
        {  10,              0.693147181f,    4.93340967f,        PAL_EPSILON * 10 },     //          y:  ln(2)
        {  10,              0.707106781f,    5.09456117f,        PAL_EPSILON * 10 },     //          y:  1 / sqrtf(2)
        {  10,              0.785398163f,    6.10095980f,        PAL_EPSILON * 10 },     //          y:  pi / 4
        {  10,              1,               10,                 PAL_EPSILON * 100 },
        {  10,              1.12837917f,     13.4393779f,        PAL_EPSILON * 100 },    //          y:  2 / sqrtf(pi)
        {  10,              1.41421356f,     25.9545535f,        PAL_EPSILON * 100 },    //          y:  sqrtf(2)
        {  10,              1.44269504f,     27.7137338f,        PAL_EPSILON * 100 },    //          y:  logf2(e)
        {  10,              1.57079633f,     37.2217105f,        PAL_EPSILON * 100 },    //          y:  pi / 2
        {  10,              2.30258509f,     200.717432f,        PAL_EPSILON * 1000 },   //          y:  ln(10)
        {  10,              2.71828183f,     522.735300f,        PAL_EPSILON * 1000 },   //          y:  e
        {  10,              3.14159265f,     1385.45573f,        PAL_EPSILON * 10000 },  //          y:  pi
        {  10,              PAL_POSINF,      PAL_POSINF,         0 },
        
        {  PAL_POSINF,      PAL_NEGINF,      0,                  PAL_EPSILON },
        {  PAL_POSINF,      PAL_POSINF,      PAL_POSINF,         0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        powf_test1_validate(tests[i].x, tests[i].y, tests[i].expected, tests[i].variance);
    }

    powf_test1_validate_isnan(-10, -1.57079633f);                                                   //          y: -(pi / 2)
    powf_test1_validate_isnan(-10, -0.785398163f);                                                  //          y: -(pi / 4)
    powf_test1_validate_isnan(-10,  0.785398163f);                                                  //          y:   pi / 4
    powf_test1_validate_isnan(-10,  1.57079633f);                                                   //          y:   pi / 2
    
    powf_test1_validate_isnan(-2.71828183f, -1.57079633f);                                          // x: -(e)  y: -(pi / 2)
    powf_test1_validate_isnan(-2.71828183f, -0.785398163f);                                         // x: -(e)  y: -(pi / 4)
    powf_test1_validate_isnan(-2.71828183f,  0.785398163f);                                         // x: -(e)  y:   pi / 4
    powf_test1_validate_isnan(-2.71828183f,  1.57079633f);                                          // x: -(e)  y:   pi / 2

    powf_test1_validate_isnan(PAL_NEGINF, PAL_NAN);
    powf_test1_validate_isnan(PAL_NAN,    PAL_NEGINF);
    
    powf_test1_validate_isnan(PAL_POSINF, PAL_NAN);
    powf_test1_validate_isnan(PAL_NAN,    PAL_POSINF);
    
    powf_test1_validate_isnan(PAL_NAN, PAL_NAN);

    PAL_Terminate();
    return PASS;
}
