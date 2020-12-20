// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that ilogb returns correct values.
**
**===================================================================*/

#include <palsuite.h>

#define PAL_NAN     sqrt(-1.0)
#define PAL_POSINF -log(0.0)
#define PAL_NEGINF  log(0.0)

/**
 * Helper test structure
 */
struct test
{
    double value;     /* value to test the function with */
    int expected;     /* expected result */
};

/**
 * ilogb_test1_validate
 *
 * test validation function
 */
void __cdecl ilogb_test1_validate(double value, int expected)
{
    int result = ilogb(value);

    if (result != expected)
    {
        Fail("ilogb(%g) returned %d when it should have returned %d",
             value, result, expected);
    }
}

/**
 * main
 * 
 * executable entry point
 */
PALTEST(c_runtime_ilogb_test1_paltest_ilogb_test1, "c_runtime/ilogb/test1/paltest_ilogb_test1")
{
    struct test tests[] = 
    {
        /* value                       expected */
        {  PAL_NEGINF,                 2147483647 },
        {  0,                         -2147483648 },
        {  PAL_POSINF,                 2147483647 },
        {  0.11331473229676087,       -4          },   // expected: -(pi)
        {  0.15195522325791297,       -3          },   // expected: -(e)
        {  0.20269956628651730,       -3          },   // expected: -(ln(10))
        {  0.33662253682241906,       -2          },   // expected: -(pi / 2)
        {  0.36787944117144232,       -2          },   // expected: -(log2(e))
        {  0.37521422724648177,       -2          },   // expected: -(sqrt(2))
        {  0.45742934732229695,       -2          },   // expected: -(2 / sqrt(pi))
        {  0.5,                       -1          },   // expected: -(1)
        {  0.58019181037172444,       -1          },   // expected: -(pi / 4)
        {  0.61254732653606592,       -1          },   // expected: -(1 / sqrt(2))
        {  0.61850313780157598,       -1          },   // expected: -(ln(2))
        {  0.64321824193300488,       -1          },   // expected: -(2 / pi)
        {  0.74005557395545179,       -1          },   // expected: -(log10(e))
        {  0.80200887896145195,       -1          },   // expected: -(1 / pi)
        {  1,                          0          },
        {  1.2468689889006383,         0          },   // expected:  1 / pi
        {  1.3512498725672678,         0          },   // expected:  log10(e)
        {  1.5546822754821001,         0          },   // expected:  2 / pi
        {  1.6168066722416747,         0          },   // expected:  ln(2)
        {  1.6325269194381528,         0          },   // expected:  1 / sqrt(2)
        {  1.7235679341273495,         0          },   // expected:  pi / 4
        {  2,                          1          },
        {  2.1861299583286618,         1          },   // expected:  2 / sqrt(pi)
        {  2.6651441426902252,         1          },   // expected:  sqrt(2)
        {  2.7182818284590452,         1          },   // expected:  log2(e)             value: e
        {  2.9706864235520193,         1          },   // expected:  pi / 2
        {  4.9334096679145963,         2          },   // expected:  ln(10)
        {  6.5808859910179210,         2          },   // expected:  e
        {  8.8249778270762876,         3          },   // expected:  pi
        {  PAL_NAN,                    2147483647 },
    };

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    for (int i = 0; i < (sizeof(tests) / sizeof(struct test)); i++)
    {
        ilogb_test1_validate(tests[i].value, tests[i].expected);
    }

    PAL_Terminate();
    return PASS;
}
