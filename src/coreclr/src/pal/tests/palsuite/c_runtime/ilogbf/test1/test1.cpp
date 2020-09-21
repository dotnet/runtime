// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that ilogbf returns correct values.
**
**===================================================================*/

#include <palsuite.h>

#define PAL_NAN     sqrtf(-1.0f)
#define PAL_POSINF -logf(0.0f)
#define PAL_NEGINF  logf(0.0f)

/**
 * Helper test structure
 */
struct test
{
    float value;     /* value to test the function with */
    int expected;    /* expected result */
};

/**
 * ilogbf_test1_validate
 *
 * test validation function
 */
void __cdecl ilogbf_test1_validate(float value, int expected)
{
    int result = ilogbf(value);

    if (result != expected)
    {
        Fail("ilogbf(%g) returned %d when it should have returned %d",
             value, result, expected);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_ilogbf_test1_paltest_ilogbf_test1, "c_runtime/ilogbf/test1/paltest_ilogbf_test1")
{
    struct test tests[] = 
    {
        /* value                expected */
        {  PAL_NEGINF,          2147483647 },
        {  0,                  -2147483648 },
        {  PAL_POSINF,          2147483647 },
        {  0.113314732f,       -4          },   // expected: -(pi)
        {  0.151955223f,       -3          },   // expected: -(e)
        {  0.202699566f,       -3          },   // expected: -(ln(10))
        {  0.336622537f,       -2          },   // expected: -(pi / 2)
        {  0.367879441f,       -2          },   // expected: -(log2(e))
        {  0.375214227f,       -2          },   // expected: -(sqrt(2))
        {  0.457429347f,       -2          },   // expected: -(2 / sqrt(pi))
        {  0.5f,               -1          },   // expected: -(1)
        {  0.580191810f,       -1          },   // expected: -(pi / 4)
        {  0.612547327f,       -1          },   // expected: -(1 / sqrt(2))
        {  0.618503138f,       -1          },   // expected: -(ln(2))
        {  0.643218242f,       -1          },   // expected: -(2 / pi)
        {  0.740055574f,       -1          },   // expected: -(log10(e))
        {  0.802008879f,       -1          },   // expected: -(1 / pi)
        {  1,                   0          },
        {  1.24686899f,         0          },   // expected:  1 / pi
        {  1.35124987f,         0          },   // expected:  log10(e)
        {  1.55468228f,         0          },   // expected:  2 / pi
        {  1.61680667f,         0          },   // expected:  ln(2)
        {  1.63252692f,         0          },   // expected:  1 / sqrt(2)
        {  1.72356793f,         0          },   // expected:  pi / 4
        {  2,                   1          },
        {  2.18612996f,         1          },   // expected:  2 / sqrt(pi)
        {  2.66514414f,         1          },   // expected:  sqrt(2)
        {  2.71828183f,         1          },   // expected:  log2(e)             value: e
        {  2.97068642f,         1          },   // expected:  pi / 2
        {  4.93340967f,         2          },   // expected:  ln(10)
        {  6.58088599f,         2          },   // expected:  e
        {  8.82497783f,         3          },   // expected:  pi
        {  PAL_NAN,             2147483647 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        ilogbf_test1_validate(tests[i].value, tests[i].expected);
    }

    PAL_Terminate();
    return PASS;
}
