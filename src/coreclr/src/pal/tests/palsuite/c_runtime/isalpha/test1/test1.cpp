// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the isalpha function
**          Check that a number of characters return the correct
**          values for whether they are alpha or not.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    int CorrectResult;
    int character;
};

int __cdecl main(int argc, char **argv)
{
  
    int result;  
    int i;
  
    struct testCase testCases[] = 
        {
            {1,  'a'},  
            {1, 'z'},
            {1,  'B'},
            {0,  '5'},
            {0,  '?'},  
            {0,  230}
        };
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Loop through each case. Check to see if each is alpha or
       not.
    */
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
      
        result = isalpha(testCases[i].character);
     
        /* The return value is 'non-zero' for success.  This if condition
         * will still work if that non-zero isn't just 1 
         */ 
        if ( ((testCases[i].CorrectResult == 1) && (result == 0)) ||
             ( (testCases[i].CorrectResult == 0) && (result != 0) ))
        {
            Fail("ERROR: isalpha returned %i instead of %i for character "
                   "%c.\n",
                   result,
                   testCases[i].CorrectResult, 
                   testCases[i].character);
        }
    
    }      
  
  
    PAL_Terminate();
    return PASS;
} 













