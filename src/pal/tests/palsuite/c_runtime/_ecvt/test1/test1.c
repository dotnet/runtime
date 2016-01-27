// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  c_runtime/_ecvt/test1/test1.c
**
** Purpose:  Call the _ecvt function on a number of cases.  Check that it
** handles negatives, positives and double bounds correctly.  Also check that 
** the 'digit' specification works.
**
**
**===================================================================*/

#include <palsuite.h>

#define INT64_TO_DOUBLE(a) (*(double*)&a)

INT64 NaN = 0x7ff8000000000000;
INT64 NegativeInfinity = 0xfff0000000000000;
INT64 NegativeSmall = 0x8000000000000001;
INT64 PositiveInfinity = 0x7ff0000000000000;
INT64 PositiveSmall = 0x0000000000000001;

struct testCase
{
    double value;        /* number to be converted */ 
    int precision;       /* number of digits to be stored */
    int decimal;         /* (expected) decimal point position for stored 
                          * number */
    int sign;            /* (expected) return value */
    char expResult[256]; /* (expected) character array to be returned
                          * NOTE: this necessarily limits precision 
                          * to a value between 0 and 255 */
    char bsdExpResult[256]; /* (expected) character array to be returned
                          * NOTE: this necessarily limits precision 
                          * to a value between 0 and 255 */
};

int __cdecl main(int argc, char **argv)
{
    char *result;
    int testDecimal;
    int testSign;
    int i=0;

    struct testCase testCases[] =
        {
            /* odd ball values */
            {INT64_TO_DOUBLE(NaN), 7, 1, 0, "1#QNAN0" },
            /* positive values */
            {0, 0, 0, 0, ""},
            {INT64_TO_DOUBLE(PositiveSmall), 17, -323, 0, 
             "49406564584124654"},
            {.00123, 3, -2, 0, "123"},
            {.123, 3, 0, 0, "123"},
            {123, 3, 3, 0, "123"},
            {3.1415926535, 9, 1, 0, "314159265"},
            {3.1415926535, 10, 1, 0, "3141592654"},
            {3.1415926535, 11, 1, 0, "31415926535"},
            {3.1415926535, 12, 1, 0, "314159265350"},
            {184467444073709570000.0, 21, 21, 0, "184467444073709570000", 
                "184467444073709568000" },  
            {184467444073709570000.0, 22, 21, 0, "1844674440737095700000", 
                "1844674440737095680000" }, 
            {INT64_TO_DOUBLE(PositiveInfinity), 7, 1, 0, "1#INF00" },
            /* negative values */
            {-0, 0, 0, 0, ""},
            {INT64_TO_DOUBLE(NegativeSmall), 17, -323, 1, 
            "49406564584124654"},
            {-.00123, 3, -2, 1, "123"},
            {-.123, 3, 0, 1, "123"},
            {-123, 3, 3, 1, "123"},
            {-3.1415926535, 9, 1, 1, "314159265"},
            {-3.1415926535, 10, 1, 1, "3141592654"},
            {-3.1415926535, 11, 1, 1, "31415926535"},
            {-3.1415926535, 12, 1, 1, "314159265350"},
            {-184467444073709570000.0, 21, 21, 1, "184467444073709570000", 
                "184467444073709568000" },  
            {-184467444073709570000.0, 22, 21, 1, "1844674440737095700000", 
                "1844674440737095680000" },
            {INT64_TO_DOUBLE(NegativeInfinity), 7, 1, 1, "1#INF00"}

        };

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Loop through each case. Call _ecvt on each test case and check the
       result.
    */

    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        result = _ecvt(testCases[i].value, 
                       testCases[i].precision,
                       &testDecimal,
                       &testSign);
        
        if (( strcmp(testCases[i].expResult, result) != 0 && 
              strcmp(testCases[i].bsdExpResult, result) != 0 ) || 

            ( testCases[i].sign != testSign ) || 
            ( testCases[i].decimal != testDecimal ))
        
        {
            Fail("PALSUITE ERROR: Test %d\n"
                 "-----------------------\n"
                 "testCases[i].value = '%f'\n"
                 "testCases[i].precision = '%d'\n"
                 "testCases[i].decimal = '%d'\n"
                 "testCases[i].sign = '%d'\n"
                 "testCases[i].expResult = '%s'\n"
                 "result = '%s'\n"
                 "testDecimal = '%d'\n"
                 "testSign = '%d'\n\n",
                 i,
                 testCases[i].value,
                 testCases[i].precision,
                 testCases[i].decimal,
                 testCases[i].sign,
                 testCases[i].expResult,
                 result,
                 testDecimal,
                 testSign);
        }

    }

    PAL_Terminate();
    return PASS;
}
