// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the _itow_s function.
**          Test a number of ints with different radix on each,
**          to ensure that the string returned is correct.
**
**
**===================================================================*/

#define UNICODE

#include <palsuite.h>

struct testCase
{
    char16_t *CorrectResult;
    int value;
    int radix;
};

PALTEST(c_runtime__itow_test1_paltest_itow_test1, "c_runtime/_itow/test1/paltest_itow_test1")
{

    char16_t result[20];
    char16_t *pResult = NULL;
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
        errno_t err = _itow_s(testCases[i].value, result, sizeof(result) / sizeof(result[0]), testCases[i].radix);

        if(err != 0)
        {
            Fail("ERROR: _itow_s didn't return success, error code %d.\n", err);
        }

        if (0 != wcscmp(testCases[i].CorrectResult, result))
        {
            PrintResult = convertC(pResult);
            PrintCorrectResult = convertC(testCases[i].CorrectResult);
            Fail("ERROR: _itow_s was called on %i, returning the string %s "
                   "when it should have returned the string %s.\n"
                   , testCases[i].value, PrintResult, PrintCorrectResult);
        }

    }

    PAL_Terminate();
    return PASS;
}













