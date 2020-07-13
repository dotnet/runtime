// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the towlower function.
**          Check that the tolower function makes capital character
**          lower case. Also check that it has no effect on lower
**          case letters and special characters.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    WCHAR lower;
    WCHAR start;
};

int __cdecl main(int argc, char **argv)
{
  
    int result;  
    int i;

    struct testCase testCases[] = 
        {
            {'a',  'A'},  /* Basic cases */
            {'z', 'Z'},
            {'b',  'b'},  /* Lower case */
            {'?',  '?'},  /* Characters without case */
            {230,  230}
        };
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* Loop through each case.  Convert each character to lower case 
       and then compare to ensure that it is the correct value.
    */
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /*Convert to lower case*/
        result = towlower(testCases[i].start);
     
        if (testCases[i].lower != result)
        {
            Fail("ERROR: towlower lowered \"%c\" to %c instead of %c.\n", 
                testCases[i].start, result, testCases[i].lower);
        }
    
    }      
  
  
    PAL_Terminate();
    return PASS;
} 













