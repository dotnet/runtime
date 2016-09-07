// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the atol function.
**          Check to ensure that the different ints (normal,
**          negative, decimal,exponent), all work as expected with
**          this function.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    LONG LongValue;
    char avalue[20];
};

int __cdecl main(int argc, char **argv)
{

    LONG result=0;
    int i=0;

    struct testCase testCases[] =
        {
            {1234,  "1234"},
            {-1234, "-1234"},
            {1234,  "1234.44"},
            {1234,  "1234e-5"},
            {1234,  "1234e+5"},
            {1234,  "1234E5"},
            {1234,  "1234.657e-8"},
            {1234,  "1234d-5"},
            {1234,  "1234d+5"},
            {1234,  "1234D5"},
            {1234567,  "   1234567e-8 foo"},
            {0,     "aaa 32 test"}
        };

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case.  Convert the string to a LONG
       and then compare to ensure that it is the correct value.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /*Convert the string to a LONG.*/
        result = atol(testCases[i].avalue);

        if (testCases[i].LongValue != result)
        {
            Fail("ERROR: atol misinterpreted \"%s\" as %d instead of %d.\n"
                   , testCases[i].avalue, result, testCases[i].LongValue);
        }

    }


    PAL_Terminate();
    return PASS;
}













