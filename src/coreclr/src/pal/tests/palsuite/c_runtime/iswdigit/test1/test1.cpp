// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c (iswdigit)
**
** Purpose: Tests the PAL implementation of the iswdigit function.
**          Tests the passed parameter to iswdigit for being a 
**          digit ('0' - '9'). Also passes non-digits to make sure
**          iswdigit picks them up. 
**          NOTE: There are three ASCII values that under Windows,
**              iswdigit will return non-zero, indicating a digit. 
**              These values are quite apparently not digits:
**              178, 179, 185. 
**          These are not tested.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_iswdigit_test1_paltest_iswdigit_test1, "c_runtime/iswdigit/test1/paltest_iswdigit_test1")
{
  
    int result;  
    int i;
  
    char16_t passTestCases[] = {'1','2','3','4','5','6','7','8','9'};
    char16_t failTestCases[] = {'a','b','p','$','?',234};
  
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }
  
    /* Loop through each case. Testing if each is a digit. */
    for(i = 0; i < sizeof(passTestCases) / sizeof(char16_t); i++)
    {
        result = iswdigit(passTestCases[i]);
     
        /* The return value is 'non-zero' indicates digit*/
        if (result == 0)
        {
            Fail("ERROR: iswdigit returned \"%d\" instead indicating"
                    " \"%c\" is not a digit\n",
                    result,
                    passTestCases[i]);
        }
    }      

    /* Loop through each case. Testing if each is a not a digit. */
    for(i = 0; i < sizeof(failTestCases) / sizeof(char16_t); i++)
    {
        result = iswdigit(failTestCases[i]);
             
        /* The return value is 'zero' indicates non-digit*/
        if (result != 0)
        {
            Fail("ERROR: iswdigit returned \"%d\", indicating"
                    " \"%c\" is a digit\n",
                    result,
                    failTestCases[i]);
        }
    }      
    PAL_Terminate();
    return (PASS);
} 
