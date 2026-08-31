// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test1.c (toupper)
**
**
** Purpose:   Tests the PAL implementation of the toupper function.
**            Check that the toupper function makes lower case
**            character a capital. Also check that it has no effect
**            on upper case letters and special characters.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
    int upper;
    int start;
};

PALTEST(c_runtime_toupper_test1_paltest_toupper_test1, "c_runtime/toupper/test1/paltest_toupper_test1")
{
  
    int result;  
    int i;

    struct testCase testCases[] = 
        {
            {'A', 'a'},   /* Basic cases */
            {'Z', 'z'},
            {'B', 'B'},   /* Upper case */
            {'%',  '%'},  /* Characters without case */
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
        result = toupper(testCases[i].start);
     
        if (testCases[i].upper != result)
        {
            Fail("ERROR: toupper capitalized \"%c\" to %c instead of %c.\n",
                   testCases[i].start, result, testCases[i].upper);
        }
    }
  
    PAL_Terminate();
    return PASS;
} 
