// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Compare a number of different strings against each other, ensure that the
** three return values are given at the appropriate times.
**
**
**==========================================================================*/

#include <palsuite.h>

typedef struct
{
    int result;
    char string1[50];
    char string2[50];
} testCase;

PALTEST(c_runtime_strcmp_test1_paltest_strcmp_test1, "c_runtime/strcmp/test1/paltest_strcmp_test1")
{
    testCase testCases[]=
    {
        {0,"Hello","Hello"},
        {1,"hello","Hello"},
        {-1,"Hello","hello"},
        {0,"0Test","0Test"},
        {0,"***???","***???"},
        {0,"Testing the string for string comparison","Testing the string for "
            "string comparison"},
        {-1,"Testing the string for string comparison","Testing the string for "
            "string comparsioa"},
        {1,"Testing the string for string comparison","Testing the string for "
            "comparison"},
        {-1,"aaaabbbbb","aabcdefeccg"}
    };

    int i = 0;
    int result = 0;
    
    /*
     *  Initialize the PAL
     */
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Loop through structure and test each case */
    for (i=0; i < sizeof(testCases)/sizeof(testCase); i++)
    {
        result = strcmp(testCases[i].string1,testCases[i].string2);

        /* Compare returned value */
        if( ((result == 0) && (testCases[i].result !=0)) ||
            ((result <0) && (testCases[i].result !=-1)) ||
            ((result >0) && (testCases[i].result !=1)) )
        {
           Fail("ERROR:  strcmp returned %d instead of %d\n",
                result, testCases[i].result);
        }

    }

    PAL_Terminate();

    return PASS;
}
