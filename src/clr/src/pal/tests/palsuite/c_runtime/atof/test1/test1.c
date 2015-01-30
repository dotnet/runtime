//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Passes to atof() a series of strings containing floats,
**          checking that each one is correctly extracted.
**
**
**==========================================================================*/

#include <palsuite.h>
#include <float.h>

struct testCase
{
    float fvalue;
    char avalue[20];
};

int __cdecl main(int argc, char **argv)
{
    int i = 0;
    double f = 0;
    struct testCase testCases[] =
    {
        {1234, "1234"},
        {-1234, "-1234"},
        {1234e-5, "1234e-5"},
        {1234e+5, "1234e+5"},
        {1234e5, "1234E5"},
        {1234.567e-8, "1234.567e-8"},
        {1234.567e-8, "   1234.567e-8 foo"},
        {0,"a12"}
    };

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /*Convert the string to a float.*/
        f = atof(testCases[i].avalue);
        double result = f - testCases[i].fvalue;

        if (fabs(result) > FLT_EPSILON)
        {
            Fail ("atof misinterpreted \"%s\" as %g instead of %g. result %g fabs %g\n",
                testCases[i].avalue, f, testCases[i].fvalue, result, fabs(result));
        }
    }
    PAL_Terminate();
    return PASS;
}













