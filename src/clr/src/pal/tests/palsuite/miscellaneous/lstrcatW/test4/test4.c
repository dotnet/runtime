// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source:    test4.c
**
** Purpose:   Testing the behaviour of lstrcatw when string2 contains 
**            special characters, this test case depends on:
**            memcmp
**            wcslen
**            lstrcpyn
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

struct testCase
{
    WCHAR SecondString[5];
    WCHAR CorrectString[10];
};

int __cdecl main(int argc, char *argv[])
{

    WCHAR FirstString[10] = {'T','E','S','T','\0'};
    WCHAR TestString[10] = {'T','E','S','T','\0'};
    int i = 0;

    /*
     * this structure includes several strings to be tested with
     * lstrcatW function and the expected results
     */

    struct testCase testCases[]=
    {
        {{'\t','T','A','B','\0'},
        {'T','E','S','T','\t','T','A','B','\0'}},
        {{'2','T','\?','B','\0'},
        {'T','E','S','T','2','T','\?','B','\0'}},
        {{'\v','T','E','\v','\0'},
        {'T','E','S','T','\v','T','E','\v','\0'}},
        {{'T','\a','E','\a','\0'},
        {'T','E','S','T','T','\a','E','\a','\0'}},
        {{'0','\f','Z','\f','\0'},
        {'T','E','S','T','0','\f','Z','\f','\0'}},
        {{'\r','H','I','\r','\0'},
        {'T','E','S','T','\r','H','I','\r','\0'}},
        {{'H','I','\"','\"','\0'},
        {'T','E','S','T','H','I','\"','\"','\0'}},
        {{'H','\b','I','\b','\0'},
        {'T','E','S','T','H','\b','I','\b','\0'}},
        {{'H','\n','I','\n','\0'},
        {'T','E','S','T','H','\n','I','\n','\0'}}
    };

  


    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


    /* Loop through the struct and validate the resulted string */
    for( i = 0; i < sizeof(testCases)/sizeof(struct testCase); i++)
    {

        lstrcat(FirstString, testCases[i].SecondString);
        
        if(memcmp(FirstString,testCases[i].CorrectString,
            wcslen(FirstString)*sizeof(WCHAR)))
        {
            
            Fail("ERROR: the function failed with a special character.\n");
        }

        /* reinitialize the first string */        
        lstrcpyn(FirstString,TestString,10); 

    }


    

    PAL_Terminate();
    return PASS;
}



