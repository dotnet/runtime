// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Check a character set against a string to see that the function returns
** the length of the substring which consists of all characters in the string.
** Also check that if the character set doesn't match the string at all, that
** the value is 0.
**
**
**==========================================================================*/

#include <palsuite.h>

struct testCase
{
    long result;
    char *string1;
    char *string2;
};

PALTEST(c_runtime_strspn_test1_paltest_strspn_test1, "c_runtime/strspn/test1/paltest_strspn_test1")
{
    int i=0;
    long TheResult = 0;
    
    struct testCase testCases[]=
    {
        {4,"abcdefg12345678hijklmnopqrst","a2bjk341cd"},
        {14,"This is a test, testing", "aeioTts rh"},
        {0,"foobar","kpzt"}
    };

    /*
     *  Initialize the PAL
     */
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<sizeof(testCases)/sizeof(struct testCase);i++)
    {
         TheResult = strspn(testCases[i].string1,testCases[i].string2);
         if (TheResult != testCases[i].result)
         {
            Fail("Expected strspn to return %d, got %d!\n",
                 testCases[i].result,TheResult);
         }

    }

    PAL_Terminate();
    return PASS;
}
