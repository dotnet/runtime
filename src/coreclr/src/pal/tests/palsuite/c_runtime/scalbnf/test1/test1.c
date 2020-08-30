// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that scalbnf returns correct values.
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
    float value;     /* value to test the function with */
    int   exponent;  /* exponent to test the function with */
    float expected;  /* expected result */
    float variance;  /* maximum delta between the expected and actual result */
};

/**
 * validate
 *
 * test validation function
 */
void __cdecl validate(float value, int exponent, float expected, float variance)
{
    float result = scalbnf(value, exponent);

    /*
     * The test is valid when the difference between result
     * and expected is less than or equal to variance
     */
    float delta = fabsf(result - expected);

    if (delta > variance)
    {
        Fail("scalbnf(%g, %g) returned %10.9g when it should have returned %10.9g",
             value, exponent, result, expected);
    }
}

/**
 * validate
 *
 * test validation function for values returning NaN
 */
void __cdecl validate_isnan(float value, int exponent)
{
    float result = scalbnf(value, exponent);

    if (!_isnanf(result))
    {
        Fail("scalbnf(%g, %g) returned %10.9g when it should have returned %10.9g",
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
        /* value                exponent           expected        variance */
        {  PAL_NEGINF,          0x80000000,        PAL_NEGINF,     0 },
        {  0,                   0x80000000,        0,              0 },
        {  0.113314732f,       -3,                 0.0141643415f,  PAL_EPSILON / 10 },
        {  0.151955223f,       -2,                 0.0379888058f,  PAL_EPSILON / 10 },
        {  0.202699566f,       -2,                 0.0506748916f,  PAL_EPSILON / 10 },
        {  0.336622537f,       -1,                 0.168311268f,   PAL_EPSILON },
        {  0.367879441f,       -1,                 0.183939721f,   PAL_EPSILON },
        {  0.375214227f,       -1,                 0.187607114f,   PAL_EPSILON },
        {  0.457429347f,       -1,                 0.228714674f,   PAL_EPSILON },
        {  0.5f,               -1,                 0.25f,          PAL_EPSILON },
        {  0.580191810f,        0,                 0.580191810f,   PAL_EPSILON },
        {  0.612547327f,        0,                 0.612547327f,   PAL_EPSILON },
        {  0.618503138f,        0,                 0.618503138f,   PAL_EPSILON },
        {  0.643218242f,        0,                 0.643218242f,   PAL_EPSILON },
        {  0.740055574f,        0,                 0.740055574f,   PAL_EPSILON },
        {  0.802008879f,        0,                 0.802008879f,   PAL_EPSILON },
        {  1,                   0,                 1,              PAL_EPSILON * 10 },
        {  1.24686899f,         0,                 1.24686899f,    PAL_EPSILON * 10 },
        {  1.35124987f,         0,                 1.35124987f,    PAL_EPSILON * 10 },
        {  1.55468228f,         0,                 1.55468228f,    PAL_EPSILON * 10 },
        {  1.61680667f,         0,                 1.61680667f,    PAL_EPSILON * 10 },
        {  1.63252692f,         0,                 1.63252692f,    PAL_EPSILON * 10 },
        {  1.72356793f,         0,                 1.72356793f,    PAL_EPSILON * 10 },
        {  2,                   1,                 4,              PAL_EPSILON * 10 },
        {  2.18612996f,         1,                 4.37225992f,    PAL_EPSILON * 10 },
        {  2.66514414f,         1,                 5.33028829f,    PAL_EPSILON * 10 },
        {  2.71828183f,         1,                 5.43656366f,    PAL_EPSILON * 10 },
        {  2.97068642f,         1,                 5.94137285f,    PAL_EPSILON * 10 },
        {  4.93340967f,         2,                 19.7336387f,    PAL_EPSILON * 100 },
        {  6.58088599f,         2,                 26.3235440f,    PAL_EPSILON * 100 },
        {  8.82497783f,         3,                 70.5998226f,    PAL_EPSILON * 100 },
        {  PAL_POSINF,          0x80000000,        PAL_POSINF,     0 },
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
