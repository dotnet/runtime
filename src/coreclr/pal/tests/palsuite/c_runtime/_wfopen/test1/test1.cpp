// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the _wfopen function. 
**          This test simply attempts to open a number of files with
**          different modes.  It checks to ensure a valid file
**          pointer is returned.  It doesn't do any checking to
**          ensure the mode is really what it claims. 
**  

**
**===================================================================*/


#define UNICODE                                  
#include <palsuite.h>

struct testCase
{
    int CorrectResult;
    WCHAR mode[20];
};

PALTEST(c_runtime__wfopen_test1_paltest_wfopen_test1, "c_runtime/_wfopen/test1/paltest_wfopen_test1")
{
  
    FILE *fp;
    WCHAR name[128];
    WCHAR base[] = {'t','e','s','t','f','i','l','e','s','\0'};
    char * PrintResult;
    int i;

    struct testCase testCases[] = 
        {
            {0,  {'r','\0'    }}, {1, {'w','\0'}},     {1,  {'a','\0'}},
            {0,  {'r','+','\0'}}, {1, {'w','+','\0'}}, {1,  {'a','+','\0'}},
            {1,  {'w','t','\0'}}, {1, {'w','b','\0'}}, {1,  {'w','S','\0'}},
            {1,  {'w','c','\0'}}, {1, {'w','n','\0'}}, {1,  {'w', 'R','\0'}}, 
            {1,  {'w','T','\0'}}, {0, {'t','w','\0'}}, {0,  {'.','\0'}}
        };

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        wcscpy(name,base);
        wcscat(name,testCases[i].mode);
      
        fp = _wfopen(name,testCases[i].mode);
      
        if ((fp == 0 && testCases[i].CorrectResult != 0)  ||
            (testCases[i].CorrectResult == 0 && fp != 0) )
        {
            PrintResult = convertC(testCases[i].mode);
            Fail("ERROR: fopen returned incorrectly "
                   "opening a file in %s mode.  Perhaps it opened a "
                   "read only file which didn't exist and returned a correct "
                   "pointer?",PrintResult);
            free(PrintResult);
        }    

        memset(name, '\0', 128 * sizeof(name[0]));
    }      
  
    PAL_Terminate();
    return PASS;
}
   

