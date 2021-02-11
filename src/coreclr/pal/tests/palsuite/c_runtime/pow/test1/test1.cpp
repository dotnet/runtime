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

/**
 * Helper test structure
 */
struct test
{
    double x;         /* first component of the value to test the function with */
    double y;         /* second component of the value to test the function with */
    double expected;  /* expected result */
    double variance;  /* maximum delta between the expected and actual result */
};

/**
 * pow_test1_validate
 *
 * test validation function
 */
void __cdecl pow_test1_validate(double x, double y, double expected, double variance)
{
    double result = pow(x, y);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("pow(%g, %g) returned %20.17g when it should have returned %20.17g",
             x, y, result, expected);
    }
}

/**
 * pow_test1_validate
 *
 * test validation function for values returning NaN
 */
void __cdecl pow_test1_validate_isnan(double x, double y)
{
    double result = pow(x, y);

    if (!_isnan(result))
    {
        Fail("pow(%g, %g) returned %20.17g when it should have returned %20.17g",
             x, y, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_pow_test1_paltest_pow_test1, "c_runtime/pow/test1/paltest_pow_test1")
{
    struct test tests[] = 
    {
        /* x                       y                       expected                   variance */
        {  PAL_NEGINF,             PAL_NEGINF,             0,                         PAL_EPSILON },
        {  PAL_NEGINF,             PAL_POSINF,             PAL_POSINF,                0 },

        { -10,                     PAL_NEGINF,             0,                         PAL_EPSILON },
        { -10,                    -1,                     -0.1,                       PAL_EPSILON },
        { -10,                     0,                      1,                         PAL_EPSILON * 10 },
        { -10,                     1,                     -10,                        PAL_EPSILON * 100 },
        { -10,                     PAL_POSINF,             PAL_POSINF,                0 },

        { -2.7182818284590452,     PAL_NEGINF,             0,                         PAL_EPSILON },          // x: -(e)
        { -2.7182818284590452,    -1,                     -0.36787944117144232,       PAL_EPSILON },          // x: -(e)
        { -2.7182818284590452,     0,                      1,                         PAL_EPSILON * 10 },     // x: -(e)
        { -2.7182818284590452,     1,                     -2.7182818284590452,        PAL_EPSILON * 10 },     // x: -(e)                       expected: e
        { -2.7182818284590452,     PAL_POSINF,             PAL_POSINF,                0 },                    // x: -(e)

        { -1.0,                    PAL_NEGINF,             1.0,                       PAL_EPSILON * 10 },
        { -1.0,                    PAL_POSINF,             1.0,                       PAL_EPSILON * 10 },

        { -0.0,                    PAL_NEGINF,             PAL_POSINF,                0 },
        { -0.0,                   -1,                      PAL_NEGINF,                0 },
        { -0.0,                   -0.0,                    1,                         PAL_EPSILON * 10 },
        { -0.0,                    0,                      1,                         PAL_EPSILON * 10 },
        { -0.0,                    1,                     -0.0,                       PAL_EPSILON },
        { -0.0,                    PAL_POSINF,             0,                         PAL_EPSILON },

        {  PAL_NAN,               -0.0,                    1.0,                       PAL_EPSILON * 10 },
        {  PAL_NAN,                0,                      1.0,                       PAL_EPSILON * 10 },

        {  0.0,                    PAL_NEGINF,             PAL_POSINF,                0 },
        {  0.0,                   -1,                      PAL_POSINF,                0 },
        {  0,                     -0.0,                    1,                         PAL_EPSILON * 10 },
        {  0,                      0,                      1,                         PAL_EPSILON * 10 },
        {  0.0,                    1,                      0,                         PAL_EPSILON },
        {  0.0,                    PAL_POSINF,             0,                         PAL_EPSILON },

        {  1,                      PAL_NEGINF,             1,                         PAL_EPSILON * 10 },
        {  1,                      PAL_POSINF,             1,                         PAL_EPSILON * 10 },

        {  2.7182818284590452,     PAL_NEGINF,             0,                         PAL_EPSILON },
        {  2.7182818284590452,    -3.1415926535897932,     0.043213918263772250,      PAL_EPSILON / 10 },     // x: e     y: -(pi)
        {  2.7182818284590452,    -2.7182818284590452,     0.065988035845312537,      PAL_EPSILON / 10 },     // x: e     y: -(e)
        {  2.7182818284590452,    -2.3025850929940457,     0.1,                       PAL_EPSILON },          // x: e     y: -(ln(10))
        {  2.7182818284590452,    -1.5707963267948966,     0.20787957635076191,       PAL_EPSILON },          // x: e     y: -(pi / 2)
        {  2.7182818284590452,    -1.4426950408889634,     0.23629008834452270,       PAL_EPSILON },          // x: e     y: -(log2(e))
        {  2.7182818284590452,    -1.4142135623730950,     0.24311673443421421,       PAL_EPSILON },          // x: e     y: -(sqrt(2))
        {  2.7182818284590452,    -1.1283791670955126,     0.32355726390307110,       PAL_EPSILON },          // x: e     y: -(2 / sqrt(pi))
        {  2.7182818284590452,    -1,                      0.36787944117144232,       PAL_EPSILON },          // x: e     y: -(1)
        {  2.7182818284590452,    -0.78539816339744831,    0.45593812776599624,       PAL_EPSILON },          // x: e     y: -(pi / 4)
        {  2.7182818284590452,    -0.70710678118654752,    0.49306869139523979,       PAL_EPSILON },          // x: e     y: -(1 / sqrt(2))
        {  2.7182818284590452,    -0.69314718055994531,    0.5,                       PAL_EPSILON },          // x: e     y: -(ln(2))
        {  2.7182818284590452,    -0.63661977236758134,    0.52907780826773535,       PAL_EPSILON },          // x: e     y: -(2 / pi)
        {  2.7182818284590452,    -0.43429448190325183,    0.64772148514180065,       PAL_EPSILON },          // x: e     y: -(log10(e))
        {  2.7182818284590452,    -0.31830988618379067,    0.72737734929521647,       PAL_EPSILON },          // x: e     y: -(1 / pi)
        {  2.7182818284590452,     0,                      1,                         PAL_EPSILON * 10 },     // x: e
        {  2.7182818284590452,     0.31830988618379067,    1.3748022274393586,        PAL_EPSILON * 10 },     // x: e     y:  1 / pi
        {  2.7182818284590452,     0.43429448190325183,    1.5438734439711811,        PAL_EPSILON * 10 },     // x: e     y:  log10(e)
        {  2.7182818284590452,     0.63661977236758134,    1.8900811645722220,        PAL_EPSILON * 10 },     // x: e     y:  2 / pi
        {  2.7182818284590452,     0.69314718055994531,    2,                         PAL_EPSILON * 10 },     // x: e     y:  ln(2)
        {  2.7182818284590452,     0.70710678118654752,    2.0281149816474725,        PAL_EPSILON * 10 },     // x: e     y:  1 / sqrt(2)
        {  2.7182818284590452,     0.78539816339744831,    2.1932800507380155,        PAL_EPSILON * 10 },     // x: e     y:  pi / 4
        {  2.7182818284590452,     1,                      2.7182818284590452,        PAL_EPSILON * 10 },     // x: e                         expected: e
        {  2.7182818284590452,     1.1283791670955126,     3.0906430223107976,        PAL_EPSILON * 10 },     // x: e     y:  2 / sqrt(pi)
        {  2.7182818284590452,     1.4142135623730950,     4.1132503787829275,        PAL_EPSILON * 10 },     // x: e     y:  sqrt(2)
        {  2.7182818284590452,     1.4426950408889634,     4.2320861065570819,        PAL_EPSILON * 10 },     // x: e     y:  log2(e)
        {  2.7182818284590452,     1.5707963267948966,     4.8104773809653517,        PAL_EPSILON * 10 },     // x: e     y:  pi / 2
        {  2.7182818284590452,     2.3025850929940457,     10,                        PAL_EPSILON * 100 },    // x: e     y:  ln(10)
        {  2.7182818284590452,     2.7182818284590452,     15.154262241479264,        PAL_EPSILON * 100 },    // x: e     y:  e
        {  2.7182818284590452,     3.1415926535897932,     23.140692632779269,        PAL_EPSILON * 100 },    // x: e     y:  pi
        {  2.7182818284590452,     PAL_POSINF,             PAL_POSINF,                0 },                    // x: e
        
        {  10,                     PAL_NEGINF,             0,                         0 },
        {  10,                    -3.1415926535897932,     0.00072178415907472774,    PAL_EPSILON / 1000 },   //          y: -(pi)
        {  10,                    -2.7182818284590452,     0.0019130141022243176,     PAL_EPSILON / 100 },    //          y: -(e)
        {  10,                    -2.3025850929940457,     0.0049821282964407206,     PAL_EPSILON / 100 },    //          y: -(ln(10))
        {  10,                    -1.5707963267948966,     0.026866041001136132,      PAL_EPSILON / 10 },     //          y: -(pi / 2)
        {  10,                    -1.4426950408889634,     0.036083192820787210,      PAL_EPSILON / 10 },     //          y: -(log2(e))
        {  10,                    -1.4142135623730950,     0.038528884700322026,      PAL_EPSILON / 10 },     //          y: -(sqrt(2))
        {  10,                    -1.1283791670955126,     0.074408205860642723,      PAL_EPSILON / 10 },     //          y: -(2 / sqrt(pi))
        {  10,                    -1,                      0.1,                       PAL_EPSILON },          //          y: -(1)
        {  10,                    -0.78539816339744831,    0.16390863613957665,       PAL_EPSILON },          //          y: -(pi / 4)
        {  10,                    -0.70710678118654752,    0.19628775993505562,       PAL_EPSILON },          //          y: -(1 / sqrt(2))
        {  10,                    -0.69314718055994531,    0.20269956628651730,       PAL_EPSILON },          //          y: -(ln(2))
        {  10,                    -0.63661977236758134,    0.23087676451600055,       PAL_EPSILON },          //          y: -(2 / pi)
        {  10,                    -0.43429448190325183,    0.36787944117144232,       PAL_EPSILON },          //          y: -(log10(e))
        {  10,                    -0.31830988618379067,    0.48049637305186868,       PAL_EPSILON },          //          y: -(1 / pi)
        {  10,                     0,                      1,                         PAL_EPSILON * 10 },
        {  10,                     0.31830988618379067,    2.0811811619898573,        PAL_EPSILON * 10 },     //          y:  1 / pi
        {  10,                     0.43429448190325183,    2.7182818284590452,        PAL_EPSILON * 10 },     //          y:  log10(e)           expected: e
        {  10,                     0.63661977236758134,    4.3313150290214525,        PAL_EPSILON * 10 },     //          y:  2 / pi
        {  10,                     0.69314718055994531,    4.9334096679145963,        PAL_EPSILON * 10 },     //          y:  ln(2)
        {  10,                     0.70710678118654752,    5.0945611704512962,        PAL_EPSILON * 10 },     //          y:  1 / sqrt(2)
        {  10,                     0.78539816339744831,    6.1009598002416937,        PAL_EPSILON * 10 },     //          y:  pi / 4
        {  10,                     1,                      10,                        PAL_EPSILON * 100 },
        {  10,                     1.1283791670955126,     13.439377934644400,        PAL_EPSILON * 100 },    //          y:  2 / sqrt(pi)
        {  10,                     1.4142135623730950,     25.954553519470081,        PAL_EPSILON * 100 },    //          y:  sqrt(2)
        {  10,                     1.4426950408889634,     27.713733786437790,        PAL_EPSILON * 100 },    //          y:  log2(e)
        {  10,                     1.5707963267948966,     37.221710484165167,        PAL_EPSILON * 100 },    //          y:  pi / 2
        {  10,                     2.3025850929940457,     200.71743249053009,        PAL_EPSILON * 1000 },   //          y:  ln(10)
        {  10,                     2.7182818284590452,     522.73529967043665,        PAL_EPSILON * 1000 },   //          y:  e
        {  10,                     3.1415926535897932,     1385.4557313670111,        PAL_EPSILON * 10000 },  //          y:  pi
        {  10,                     PAL_POSINF,             PAL_POSINF,                0 },
        
        {  PAL_POSINF,             PAL_NEGINF,             0,                         PAL_EPSILON },
        {  PAL_POSINF,             PAL_POSINF,             PAL_POSINF,                0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        pow_test1_validate(tests[i].x, tests[i].y, tests[i].expected, tests[i].variance);
    }

    pow_test1_validate_isnan(-10, -1.5707963267948966);                                                                 //          y: -(pi / 2)
    pow_test1_validate_isnan(-10, -0.78539816339744828);                                                                //          y: -(pi / 4)
    pow_test1_validate_isnan(-10,  0.78539816339744828);                                                                //          y:   pi / 4
    pow_test1_validate_isnan(-10,  1.5707963267948966);                                                                 //          y:   pi / 2
    
    pow_test1_validate_isnan(-2.7182818284590452, -1.5707963267948966);                                                 // x: -(e)  y: -(pi / 2)
    pow_test1_validate_isnan(-2.7182818284590452, -0.78539816339744828);                                                // x: -(e)  y: -(pi / 4)
    pow_test1_validate_isnan(-2.7182818284590452,  0.78539816339744828);                                                // x: -(e)  y:   pi / 4
    pow_test1_validate_isnan(-2.7182818284590452,  1.5707963267948966);                                                 // x: -(e)  y:   pi / 2

    pow_test1_validate_isnan(PAL_NEGINF, PAL_NAN);
    pow_test1_validate_isnan(PAL_NAN,    PAL_NEGINF);
    
    pow_test1_validate_isnan(PAL_POSINF, PAL_NAN);
    pow_test1_validate_isnan(PAL_NAN,    PAL_POSINF);
    
    pow_test1_validate_isnan(PAL_NAN, PAL_NAN);

    PAL_Terminate();
    return PASS;
}
