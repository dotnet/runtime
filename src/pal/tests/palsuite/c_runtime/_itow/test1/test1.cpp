// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the _itow function.
**          Test a number of ints with different radix on each,
**          to ensure that the string returned is correct.
**
**
**===================================================================*/

#define UNICODE

#include <palsuite.h>

struct testCase
{
    wchar_t *CorrectResult;
    int value;
    int radix;
};

int __cdecl main(int argc, char **argv)
{

    wchar_t result[20];
    wchar_t *pResult = NULL;
    char *PrintResult = NULL;        /* Use with convertC so we can */
    char *PrintCorrectResult = NULL; /* print out the results       */
    int i = 0;

    WCHAR case1[] = {'5','0','\0'};
    WCHAR case2[] = {'5','5','5','\0'};
    WCHAR case3[] = {'1','0','1','0','\0'};
    WCHAR case4[] = {'2','2','\0'};
    WCHAR case5[] = {'a','\0'};
    WCHAR case6[] = {'c','g','\0'};

    /* Correct Result, Value to Convert, Radix to use */
    struct testCase testCases[] =
        {
            {case1, 50,  10},
            {case2,555,10},
            {case3,10,2},
            {case4,10,4},
            {case5,10,16},
            {case6,400,32}
        };

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Convert the ints to strings.  Check
       to ensure they were converted properly.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        pResult = _itow(testCases[i].value,result,testCases[i].radix);

        if(pResult != &result[0])
        {
            Fail("ERROR: _itow didn't return a correct pointer to the "
                   "newly formed string.\n");
        }

        if (0 != wcscmp(testCases[i].CorrectResult,pResult))
        {
            PrintResult = convertC(pResult);
            PrintCorrectResult = convertC(testCases[i].CorrectResult);
            Fail("ERROR: _itow was called on %i, returning the string %s "
                   "when it should have returned the string %s.\n"
                   , testCases[i].value, PrintResult, PrintCorrectResult);
        }

    }

    PAL_Terminate();
    return PASS;
}













