// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the isalnum function
**          Check that a number of characters return the correct
**          values for whether they are alpha/numeric or not.
**
**
**===================================================================*/

#include <palsuite.h>


struct testCase
{
    int CorrectResult;
    int character;
};

PALTEST(c_runtime_isalnum_test1_paltest_isalnum_test1, "c_runtime/isalnum/test1/paltest_isalnum_test1")
{
  
    int result;  
    int i;
  
    struct testCase testCases[] = 
        {
            {1,  'a'},  
            {1, 'z'},
            {1,  'B'},
            {1,  '5'},
            {1,  '0'},
            {0,  '?'},  
            {0,  230}
        };
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Loop through each case. Check to see if each is alpha/numeric or
       not.
    */
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
      
        result = isalnum(testCases[i].character);
     
        /* The return value is 'non-zero' for success.  This if condition
         * will still work if that non-zero isn't just 1 
         */ 
        if ( ((testCases[i].CorrectResult == 1) && (result == 0)) ||
             ( (testCases[i].CorrectResult == 0) && (result != 0) ))
        {
            Fail("ERROR: isalnum returned %i instead of %i for character "
                   " %c.\n",
                   result,
                   testCases[i].CorrectResult, 
                   testCases[i].character);
        }
    
    }      
  
  
    PAL_Terminate();
    return PASS;
} 













