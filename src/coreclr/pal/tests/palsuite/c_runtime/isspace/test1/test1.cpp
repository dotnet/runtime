// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the isspace function
**
**
**==========================================================================*/



#include <palsuite.h>

struct testCase
{
    long result;
    char avalue;
};



PALTEST(c_runtime_isspace_test1_paltest_isspace_test1, "c_runtime/isspace/test1/paltest_isspace_test1")
{
    int i=0;
    long result = 0;

    /*
     * A structures of the testcases to be tested with
     * isspace function
     */
    struct testCase testCases[] =
    {
           {1,'\n'},
           {1,'\t'},
           {1,'\r'},
           {1,'\v'},
           {1,'\f'},
           {1,' '},
           {0,'a'},
           {0,'A'},
           {0,'z'},
           {0,'Z'},
           {0,'r'},
           {0,'R'},
           {0,'0'},
           {0,'*'},
           {0,3}
    };

    /*
     *  Initialize the PAL
     */
    if ( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* Loop through the testcases */
    for (i=0; i<sizeof(testCases)/sizeof(struct testCase); i++)
    {
        result = isspace(testCases[i].avalue);
        if ( ((testCases[i].result == 1) && (result==0)) ||
             ((testCases[i].result ==0) && (result !=0)) )
        {
            Fail("ERROR: isspace() returned %d for %c instead of %d\n",
                 result,
                 testCases[i].avalue,
                 testCases[i].result );
        }
    }


    PAL_Terminate();

    return PASS;
}

