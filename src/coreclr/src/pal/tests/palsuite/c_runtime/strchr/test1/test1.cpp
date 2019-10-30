// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Test this on a character which is in a string, and ensure the pointer
** points to that character.  Then check the string for the null character,
** which the return pointer should point to.  Then search for a character not
** in the string and check that the return value is NULL.
**
**
**==========================================================================*/

#include <palsuite.h>

struct testCase
{
    int result;
    char string[50];
    int character;
};



int __cdecl main(int argc, char *argv[])
{
    int i = 0;
    char *result;

    /*
     * this structure includes several strings to be tested with
     * strchr function and the expected results
     */

    struct testCase testCases[]=
    {
        {22,"corn cup cat cream coast",'s'},
        {10,"corn cup cat cream coast",'a'},
        {2,"This is a test",'i'},
        {10,"This is a test",'t'},
        {'\0',"This is a test",'b'},/* zero used instead of NULL */
        {'\0',"This is a test",121},/* zero used instead of NULL */
        {4,"This is a test of the function",' '},
        {25,"This is a test of the function",'c'},
        {'\0',"This is a test of the function",'C'},
        {24,"corn cup cat cream coast", '\0'}/* zero used instead of NULL */
    };

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* Loop through the structure and test each case */

    for (i=0; i< sizeof(testCases)/sizeof(struct testCase); i++)
    {
       result = strchr(testCases[i].string,testCases[i].character);
       if (result==NULL)
       {
          if (testCases[i].result != (int) NULL)
          {
              Fail("Expected strchr() to return \"%s\" instead of NULL!\n",
                   testCases[i].string + testCases[i].result);
          }
       }
       else
       {
          if (result != testCases[i].string + testCases[i].result)
          {
              Fail("Expected strchr() to return \"%s\" instead of \"%s\"!\n",
                   testCases[i].string + testCases[i].result, result);
          }
        }

    }

    PAL_Terminate();

    return PASS;
}
