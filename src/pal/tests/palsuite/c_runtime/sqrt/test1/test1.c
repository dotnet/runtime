// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose:  Call the sqrt function on a positive value, a positive value
** with a decimal and on the maxium possible double value.  
**
**
**===================================================================*/

/* Note: Calling sqrt on anything negative gives indefinite results. */

#include <palsuite.h>

struct testCase
{
    double Value;
    double CorrectValue;
};

int __cdecl main(int argc, char **argv)
{
    double delta;
    double result=0;
    int i=0;

    struct testCase testCases[] =
        {
            {100,  10},
            {6.25, 2.5},
            {0,    0},
            {1.7e+308, 1.3038404810405297e+154} /* Max Double value */
        };

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call sqrt on each value and check the 
       result.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        result = sqrt(testCases[i].Value);
        delta = pow(10, log10(testCases[i].Value) - 7);

        if (fabs(testCases[i].CorrectValue - result) > delta)
        {
            Fail("ERROR: sqrt took the square root of '%f' to be '%f' "
                 "instead of %f.\n", 
                 testCases[i].Value, 
                 result, 
                 testCases[i].CorrectValue);
        }
    }

    PAL_Terminate();
    return PASS;
}













