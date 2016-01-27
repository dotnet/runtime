// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Call labs on a series of values -- negative, positive, zero,
** and the largest negative value of a LONG.  Ensure that they are all
** changed properly to their absoulte value. 
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    LONG LongValue;
    LONG AbsoluteLongValue;
};

int __cdecl main(int argc, char **argv)
{

    LONG result=0;
    int i=0;

    struct testCase testCases[] =
        {
            {1234,  1234},
            {-1234, 1234},
            {0,     0},
            {-2147483647, 2147483647},  /* Max value to abs */
            {2147483647, 2147483647}
        };

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call labs on each LONG and ensure that
       the resulting value is correct.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /* Absolute value on a LONG */ 
        result = labs(testCases[i].LongValue);

        if (testCases[i].AbsoluteLongValue != result)
        {
            Fail("ERROR: labs took the absoulte value of '%d' to be '%d' "
                 "instead of %d.\n", 
                 testCases[i].LongValue, 
                 result, 
                 testCases[i].AbsoluteLongValue);
        }
    }

    PAL_Terminate();
    return PASS;
}
