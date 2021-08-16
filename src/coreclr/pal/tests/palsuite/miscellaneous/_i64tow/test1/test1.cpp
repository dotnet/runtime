// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  test1.c
**
** Purpose: Tests _i64tow_s with normal values and different radices, negative 
**          values, as well as the highest and lowest values.
**
**
**============================================================*/

#include <palsuite.h>

typedef struct
{
    INT64 value;
    int radix;
    char *result;
} testCase;


PALTEST(miscellaneous__i64tow_test1_paltest_i64tow_test1, "miscellaneous/_i64tow/test1/paltest_i64tow_test1")
{
    WCHAR buffer[256];
    WCHAR *testStr;
    WCHAR *ret;
    int i;
    testCase testCases[] = 
    {
        {42, 10, "42"},
        {42, 2, "101010"},
        {29, 32, "t"},
        {-1, 10, "-1"},
        {-1, 8, "1777777777777777777777"},
        {-1, 32, "fvvvvvvvvvvvv"},
        {I64(0x7FFFFFFFFFFFFFFF), 10, "9223372036854775807"},
        {I64(0x8000000000000000), 10, "-9223372036854775808"},
        {0,2,"0"},
        {0,16,"0"},
        {3,16,"3"},
        {15,16,"f"},
        {16,16,"10"},
        
    };


    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for (i=0; i<sizeof(testCases) / sizeof(testCase); i++)
    {
        errno_t err = _i64tow_s(testCases[i].value, buffer, sizeof(buffer) / sizeof(buffer[0]), testCases[i].radix);

        if(err != 0)
        {
            Fail("ERROR: _i64tow_s didn't return success, error code %d.\n", err);
        }

        testStr = convert(testCases[i].result);
        if (wcscmp(testStr, buffer) != 0)
        {
            Fail("_i64tow_s did not give the correct string.\n"
                "Expected %S, got %S\n", testStr, buffer);
        }
        free(testStr);
    }
    
   
    PAL_Terminate();
    return PASS;
}
