// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose:  Call the _gcvt function on a number of cases.  Check that it
** handles negatives, exponents and hex digits properly.  Also check that 
** the 'digit' specification works. (And that it doesn't truncate negative
** signs or decimals)  
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    double Value;
    int Digits;
    char WinCorrectResult[128];
    char BsdCorrectResult[128]; /* for the odd case where bsd sprintf 
                                    varies from windows sprintf */
};

PALTEST(c_runtime__gcvt_test2_paltest_gcvt_test2, "c_runtime/_gcvt/test2/paltest_gcvt_test2")
{
    char result[128];
    int i=0;

    struct testCase testCases[] =
    {
        {1234567,  7, "1234567"},
        {1234.123, 7, "1234.123"},
        {1234.1234, 7, "1234.123"},
        {12.325678e+2, 7, "1232.568"},
        {-12.3233333, 8, "-12.323333"},
        {-12.32, 8, "-12.32"},
        {-12.32e+2, 8, "-1232.", "-1232" },
        {0x21DDFABC, 8, "5.6819577e+008", "5.6819577e+08" },
        {123456789012345.0, 15, "123456789012345" },
        {12340000.0, 8, "12340000"},
        {12340000000000000.0, 15, "1.234e+016", "1.234e+16" },
        {12340000000000000.0, 17, "12340000000000000"  },
        
    };

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call _gcvt on each test case and check the
       result.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        _gcvt(testCases[i].Value, testCases[i].Digits, result);

        if (strcmp(testCases[i].WinCorrectResult, result) != 0 && 
            
            ( testCases[i].BsdCorrectResult && 
              strcmp(testCases[i].BsdCorrectResult, result) != 0 ) )
        {
            Fail("ERROR: _gcvt attempted to convert %f with %d digits "
                 "signifigant, which resulted in "
                 "the string '%s' instead of the correct(Win) string '%s' or the"
                 "correct(bsd) string '%s'.\n",
                 testCases[i].Value,
                 testCases[i].Digits,
                 result,
                 testCases[i].WinCorrectResult,
                 testCases[i].BsdCorrectResult);
        }

        memset(result, '\0', 128);
    }
    PAL_Terminate();
    return PASS;
}
