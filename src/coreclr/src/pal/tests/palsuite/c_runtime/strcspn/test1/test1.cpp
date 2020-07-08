// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests strcspn with a character set that should give an index into 
**          the middle of the original string.  Also tests with character sets
**          that are not in the string at all, and character sets that match 
**          with the very first character.
**
**
**==========================================================================*/

#include <palsuite.h>

struct testCase
{
    long result;
    char *string1;
    char *string2;
};

int __cdecl main(int argc, char *argv[])
{
    int i=0;
    long TheResult = 0;
    
    struct testCase testCases[]=
    {
        {4,"abcdefg12345678hijklmnopqrst","t8m1sBe"},
        {23,"This is a test, testing", "X\tylM"},
        {0,"foobar","tzkfb"},
    };

    /*
     *  Initialize the PAL
     */
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<sizeof(testCases)/sizeof(struct testCase); i++)
    {
         TheResult = strcspn(testCases[i].string1,testCases[i].string2);
         if (TheResult != testCases[i].result)
         {
            Fail("Expected strcspn to return %d, got %d!\n",
                 testCases[i].result,TheResult);
         }

    }

    PAL_Terminate();
    return PASS;
}
