// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Call llabs on a series of values -- negative, positive,
** zero, and the largest negative value of an __int64.  Ensure that
** they are all changed properly to their absolute value.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    __int64 LongLongValue;
    __int64 AbsoluteLongLongValue;
};

PALTEST(c_runtime_llabs_test1_paltest_llabs_test1, "c_runtime/llabs/test1/paltest_llabs_test1")
{

    __int64 result=0;
    int i=0;

    struct testCase testCases[] =
        {
            {1234,  1234},
            {-1234, 1234},
            {0,     0},
            {-9223372036854775807LL, 9223372036854775807LL},  /* Max value to abs */
            {9223372036854775807LL, 9223372036854775807LL}
        };

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call llabs on each __int64 and ensure that
       the resulting value is correct.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /* Absolute value on an __int64 */
        result = llabs(testCases[i].LongLongValue);

        if (testCases[i].AbsoluteLongLongValue != result)
        {
            Fail("ERROR: llabs took the absolute value of '%d' to be '%d' "
                 "instead of %d.\n",
                 testCases[i].LongLongValue,
                 result,
                 testCases[i].AbsoluteLongLongValue);
        }
    }

    PAL_Terminate();
    return PASS;
}
