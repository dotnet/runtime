//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Test to ensure all three possible return values are given under the 
** appropriate circumstance.  Also, uses different sizes, to only compare
** portions of strings, checking to make sure these return the correct value.
**
**
**==========================================================================*/

#include <palsuite.h>

typedef struct
{
    int result;
    char string1[50];
    char string2[50];
    int number;
} testCase;

testCase testCases[]=
{
     {0,"Hello","Hello",5},
     {1,"hello","Hello",3},
     {-1,"Hello","hello",5},
     {0,"heLLo","heLLo",5},
     {1,"hello","heLlo",5},
     {-1,"heLlo","hello",5},
     {0,"0Test","0Test",5},
     {0,"***???","***???",6},
     {0,"Testing the string for string comparison","Testing the string for "
        "string comparison",40},
     {-1,"Testing the string for string comparison","Testing the string for "
         "string comparsioa",40},
     {1,"Testing the string for string comparison","Testing the string for "
        "comparison",34},
     {0,"aaaabbbbb","aabcdefeccg",2},
     {0,"abcd","abcd",10}
};


int __cdecl main(int argc, char *argv[])
{
    int i=0;
    int iresult=0;
    
    /*
     *  Initialize the PAL
     */
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    for (i=0; i< sizeof(testCases)/sizeof(testCase); i++)
    {
        iresult = strncmp(testCases[i].string1,testCases[i].string2,
                          testCases[i].number);

        if( ((iresult == 0) && (testCases[i].result !=0)) ||
            ((iresult <0) && (testCases[i].result !=-1)) ||
            ((iresult >0) && (testCases[i].result !=1)) )

    {
             Fail("ERROR: strncmp returned %d instead of %d\n",
                  iresult, testCases[i].result);
    }

    }

    PAL_Terminate();
    return PASS;
}
