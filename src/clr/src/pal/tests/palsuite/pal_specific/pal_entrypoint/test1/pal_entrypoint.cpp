// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: pal_entrypoint.c
**
** Purpose: Positive test the PAL_EntryPoint API.
**
** Test the PAL_EntryPoint, Call a PAL function, and let main return
** as expected..
**          

**
**============================================================*/

#include "palstartup.h"

/* Test case copied and stream lined from isalpha\test1*/
struct testCase
{
    int CorrectResult;
    int character;
};

int __cdecl main(int argc, char *argv[])
{
    int result;  
    int i;
  
    struct testCase testCases[] = 
    {
        {1,  'a'}
    };
    
    
    i = 0;
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

    return PASS;
}
