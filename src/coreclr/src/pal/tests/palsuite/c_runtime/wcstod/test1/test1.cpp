// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests wcstod with a number of sample strings.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    double CorrectResult;
    char string[20];
    int stopChar;
};

PALTEST(c_runtime_wcstod_test1_paltest_wcstod_test1, "c_runtime/wcstod/test1/paltest_wcstod_test1")
{
    struct testCase testCases[] = 
    {
        {1234,"1234", 4},
        {-1234,"-1234", 5},
        {1234.44,"1234.44", 7},
        {1234e-5,"1234e-5", 7},
        {1234e+5,"1234e+5", 7},
        {1234E5,"1234E5", 6},
        {1234.657e-8,  "1234.657e-8", 11},
        {0,  "1e-800", 6},
        {0,  "-1e-800", 7},
        {1234567e-8,  "   1234567e-8 foo", 13},
        {0,     " foo 32 bar", 0},
    };

    WCHAR *wideStr;
    WCHAR *endptr;
    double result;  
    int i;
  
    if (PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        wideStr = convert(testCases[i].string);
        result = wcstod(wideStr, &endptr);
      
        if (testCases[i].CorrectResult != result)
        {
            free(wideStr);
            Fail("ERROR: wcstod misinterpreted \"%s\" as %g instead of "
                   "%g.\n", 
                   testCases[i].string, 
                   result, 
                   testCases[i].CorrectResult);
        }
      
        if (endptr != wideStr + testCases[i].stopChar)
        {
            free(wideStr);
            Fail("ERROR: wcstod stopped scanning \"%s\" at %p, "
                "instead of %p!\n", testCases[i].string, endptr,
                wideStr + testCases[i].stopChar);
        }

        free(wideStr);
    }      
  
  
    PAL_Terminate();
    return PASS;
}
