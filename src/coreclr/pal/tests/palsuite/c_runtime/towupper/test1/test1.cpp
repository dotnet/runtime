// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:   test1.c(towupper)
**
**
** Purpose:  Tests the PAL implementation of the towupper function.
**           Check that the towupper function makes lower case
**           character a capital. Also check that it has no effect
**           on upper case letters and special characters.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    WCHAR upper;
    WCHAR start;
};

PALTEST(c_runtime_towupper_test1_paltest_towupper_test1, "c_runtime/towupper/test1/paltest_towupper_test1")
{
  
    int result;  
    int i;

    struct testCase testCases[] = 
        {
            {'A', 'a'},  /* Basic cases */
            {'Z', 'z'},
            {'B', 'B'},  /* Upper case */
            {'%', '%'},  /* Characters without case */
            {157,  157}
        };
  
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return FAIL;
    }


    /* Loop through each case.  Convert each character to upper case 
       and then compare to ensure that it is the correct value.
    */
  
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        /*Convert to upper case*/
        result = towupper(testCases[i].start);
     
        if (testCases[i].upper != result)
        {
            Fail("ERROR: towupper capitalized \"%c\" to %c instead of %c.\n",
                    testCases[i].start, result, testCases[i].upper);
        }
    }      
  
    PAL_Terminate();
    return PASS;
} 
