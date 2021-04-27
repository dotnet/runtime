// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the isupper function
**          Check that a number of characters return the correct
**          values for whether they are upper case or not.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    int CorrectResult;
    int character;
};

PALTEST(c_runtime_isupper_test1_paltest_isupper_test1, "c_runtime/isupper/test1/paltest_isupper_test1")
{
  
    int result;  
    int i;

    /* Note: 1 iff char =  A..Z
             0 iff char =~ A..Z
    */
  
    struct testCase testCases[] = 
        {
            {1,  'A'},  /* Basic cases */
            {1, 'Z'},
            {0,  'b'},  /* Lower case */
            {0,  '?'},  /* Characters without case */
            {0,  230},
            {0, '5'}
        };
  
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

  
    /* Loop through each case. Check to see if each is upper case or
       not.
    */
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        result = isupper(testCases[i].character);
     
        /* The return value is 'non-zero' for success.  This if condition
         * will still work if that non-zero isn't just 1 
         */ 
        if ( ((testCases[i].CorrectResult == 1) && (result == 0)) ||
             ( (testCases[i].CorrectResult == 0) && (result != 0) ))
        {
            Fail("ERROR: isupper returned %i instead of %i for "
                   "character %c.\n",
                   result,testCases[i].CorrectResult, 
                   testCases[i].character);
        }
    
    }      
  
  
    PAL_Terminate();
    return PASS;
} 













