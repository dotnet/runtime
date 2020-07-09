// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that scalbn returns correct values.
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
    int   exponent;  /* exponent to test the function with */
    double expected;  /* expected result */
    double variance;  /* maximum delta between the expected and actual result */
};

/**
 * validate
 *
 * test validation function
 */
void __cdecl validate(double value, int exponent, double expected, double variance)
{
    double result = scalbn(value, exponent);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    double delta = fabs(result - expected);

    if (delta > variance)
    {
        Fail("scalbn(%g, %d) returned %20.17g when it should have returned %20.17g\n",
             value, exponent, result, expected);
    }
}

/**
 * validate
 *
 * test validation function for values returning NaN
 */
void __cdecl validate_isnan(double value, int exponent)
{
    double result = scalbn(value, exponent);

    if (!_isnan(result))
    {
        Fail("scalbn(%g, %d) returned %20.17g when it should have returned %20.17g\n",
             value, exponent, result, PAL_NAN);
    }
}

/**
 * main
 * 
 * executable entry point
 */
int __cdecl main(int argc, char **argv)
{
    struct test tests[] = 
    {
        /* value                       exponent           expected               variance */
        {  PAL_NEGINF,                 0x80000000,        PAL_NEGINF,            0 },
        {  0,                          0x80000000,        0,                     0 },
        {  0.11331473229676087,       -3,                 0.014164341537095108,  PAL_EPSILON / 10 },
        {  0.15195522325791297,       -2,                 0.037988805814478242,  PAL_EPSILON / 10 },
        {  0.20269956628651730,       -2,                 0.050674891571629327,  PAL_EPSILON / 10 },
        {  0.33662253682241906,       -1,                 0.16831126841120952,   PAL_EPSILON },
        {  0.36787944117144232,       -1,                 0.18393972058572117,   PAL_EPSILON },
        {  0.37521422724648177,       -1,                 0.1876071136232409,    PAL_EPSILON },
        {  0.45742934732229695,       -1,                 0.22871467366114848,   PAL_EPSILON },
        {  0.5,                       -1,                 0.25,                  PAL_EPSILON },
        {  0.58019181037172444,        0,                 0.5801918103717244,    PAL_EPSILON },
        {  0.61254732653606592,        0,                 0.61254732653606592,   PAL_EPSILON },
        {  0.61850313780157598,        0,                 0.61850313780157595,   PAL_EPSILON },
        {  0.64321824193300488,        0,                 0.64321824193300492,   PAL_EPSILON },
        {  0.74005557395545179,        0,                 0.74005557395545174,   PAL_EPSILON },
        {  0.80200887896145195,        0,                 0.8020088789614519,    PAL_EPSILON },
        {  1,                          0,                 1,                     PAL_EPSILON * 10 },
        {  1.2468689889006383,         0,                 1.2468689889006384,    PAL_EPSILON * 10 },
        {  1.3512498725672678,         0,                 1.3512498725672677,    PAL_EPSILON * 10 },
        {  1.5546822754821001,         0,                 1.5546822754821001,    PAL_EPSILON * 10 },
        {  1.6168066722416747,         0,                 1.6168066722416747,    PAL_EPSILON * 10 },
        {  1.6325269194381528,         0,                 1.6325269194381529,    PAL_EPSILON * 10 },
        {  1.7235679341273495,         0,                 1.7235679341273495,    PAL_EPSILON * 10 },
        {  2,                          1,                 4,                     PAL_EPSILON * 10 },
        {  2.1861299583286618,         1,                 4.3722599166573239,    PAL_EPSILON * 10 },
        {  2.6651441426902252,         1,                 5.3302882853804503,    PAL_EPSILON * 10 },
        {  2.7182818284590452,         1,                 5.4365636569180902,    PAL_EPSILON * 10 },
        {  2.9706864235520193,         1,                 5.9413728471040388,    PAL_EPSILON * 10 },
        {  4.9334096679145963,         2,                 19.733638671658387,    PAL_EPSILON * 100 },
        {  6.5808859910179210,         2,                 26.323543964071686,    PAL_EPSILON * 100 },
        {  8.8249778270762876,         3,                 70.599822616610297,    PAL_EPSILON * 100 },
        {  PAL_POSINF,                 0x80000000,        PAL_POSINF,            0 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        validate(tests[i].value, tests[i].exponent, tests[i].expected, tests[i].variance);
    }

    validate_isnan(PAL_NAN, 2147483647);

    PAL_Terminate();
    return PASS;
}
