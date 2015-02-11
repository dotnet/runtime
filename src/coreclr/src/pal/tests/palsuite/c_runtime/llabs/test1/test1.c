//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Call llabs on a series of values -- negative, positive,
** zero, and the largest negative value of an __int64.  Ensure that
** they are all changed properly to their absoulte value. 
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    __int64 LongLongValue;
    __int64 AbsoluteLongLongValue;
};

int __cdecl main(int argc, char **argv)
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

    /* Loop through each case. Call labs on each LONG and ensure that
       the resulting value is correct.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /* Absolute value on a LONG */ 
        result = llabs(testCases[i].LongLongValue);

        if (testCases[i].AbsoluteLongLongValue != result)
        {
            Fail("ERROR: labs took the absoulte value of '%d' to be '%d' "
                 "instead of %d.\n", 
                 testCases[i].LongLongValue, 
                 result, 
                 testCases[i].AbsoluteLongLongValue);
        }
    }

    PAL_Terminate();
    return PASS;
}
