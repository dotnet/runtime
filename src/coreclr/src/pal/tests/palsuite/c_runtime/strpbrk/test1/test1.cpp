// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Search a string for characters in a given character set and ensure the 
** pointer returned points to the first occurrence.  Check to see that the
** function returns NULL if the character is not found.
**
**
**==========================================================================*/

#include <palsuite.h>

struct testCase
{
    char *result;
    char *string1;
    char *string2;
};

PALTEST(c_runtime_strpbrk_test1_paltest_strpbrk_test1, "c_runtime/strpbrk/test1/paltest_strpbrk_test1")
{
    char *ptr = NULL;
    int i = 0;

    /*
     * this structure includes several strings to be tested with
     * strpbk function and the expected results
     */

    struct testCase testCases[] =
    {
        {"t cream coast","corn cup cat cream coast","sit"},
        {"eam coast","corn cup cat cream coast","like"},
        {"is is a test","This is a test","circle"},
        {"a test","This is a test","way"},
        {NULL,"This is a test","boo"},
        {NULL,"This is a test","123"},
        {" is a test of the function","This is a test of the function",
         "zzz xx"}
    };

    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* A loop to go through the testcases in the structure */

    for (i=0; i< sizeof(testCases)/sizeof(struct testCase); i++)
    {
        ptr = strpbrk(testCases[i].string1,testCases[i].string2);
        if (ptr==NULL)
        {
            if (testCases[i].result != NULL)
            {
                Fail("Expected strpbrk() to return %s, got NULL!\n",
                     testCases[i].result);
            }
        }
        else
        {
            if (strcmp(ptr,testCases[i].result)!=0 )

            {
                Fail("Expected strpbrk() to return %s, got %s!\n",
                     testCases[i].result,ptr);
            }

        }

     }


    PAL_Terminate();

    return PASS;
}

