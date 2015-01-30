//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char **argv)
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
