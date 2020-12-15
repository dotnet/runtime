// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Tests stroul with different bases and overflows, as well as valid input.  
** Makes sure that the end pointer is correct.
**
**
**==========================================================================*/

#include <palsuite.h>

char teststr1[] = "12345";
char teststr2[] = "Z";
char teststr3[] = "4294967295";
char teststr4[] = "4294967296";

typedef struct
{
    char *str;
    char *end;
    int base;
    ULONG result;
} TestCase;

PALTEST(c_runtime_strtoul_test1_paltest_strtoul_test1, "c_runtime/strtoul/test1/paltest_strtoul_test1")
{
    TestCase TestCases[] = 
    {
        { teststr1, teststr1 + 3, 4, 27},
        { teststr1, teststr1 + 5, 10, 12345},
        { teststr2, teststr2, 10, 0},
        { teststr3, teststr3+10, 10, 4294967295ul},
        { teststr4, teststr4+10, 10, 4294967295ul}
    };

    int NumCases = sizeof(TestCases) / sizeof(TestCases[0]);

    char *end;
    ULONG l;
    int i;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    for (i=0; i<NumCases; i++)
    {
        l = strtoul(TestCases[i].str, &end, TestCases[i].base);

        if (l != TestCases[i].result)
        {
            Fail("ERROR: Expected strtoul to return %u, got %u\n", 
                TestCases[i].result, l);
        }

        if (end != TestCases[i].end)
        {
            Fail("ERROR: Expected strtoul to give an end value of %p, got %p\n",
                TestCases[i].end, end);
        }
    }
    PAL_Terminate();
    return PASS;
}
