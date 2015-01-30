//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char **argv)
{
  
    int result;  
    int i;
  
    wchar_t passTestCases[] = {'1','2','3','4','5','6','7','8','9'};
    wchar_t failTestCases[] = {'a','b','p','$','?',234};
  
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }
  
    /* Loop through each case. Testing if each is a digit. */
    for(i = 0; i < sizeof(passTestCases) / sizeof(wchar_t); i++)
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
    for(i = 0; i < sizeof(failTestCases) / sizeof(wchar_t); i++)
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
