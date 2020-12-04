// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the memchr function.
**          Create a string buffer, and check for a number of
**          characters in it. Test to ensure it returns NULL if
**          it can't find the character, and that the size argument
**          works properly.
**
**
**===================================================================*/

#include <palsuite.h>

struct testCase
{
   char *result;
   char string[50];
   int character;
   int length;
};


PALTEST(c_runtime_memchr_test1_paltest_memchr_test1, "c_runtime/memchr/test1/paltest_memchr_test1")
{
    int i = 0;
    char *result = NULL;

    /*
     * this structure includes several strings to be tested with
     * memchr function and the expected results
     */

    struct testCase testCases[]=
    {
        {"st","corn cup cat cream coast",'s',23}, 
                                   /* single instance of char                 */
        {"st","corn cup cat cream coast",'s',24}, 
                                   /* single inst, inst< exact length         */
        {"q","corn cup cat cream coastq",'q',25},
                                   /* single inst at end, inst=exact length   */
        {"q","corn cup cat cream coastq",'q',26},
                                   /* single inst at end, inst<length, 
                                                           length>len(string) */
        {"st","corn cup cat cream coast",115,24},
                                   /* single int inst, inst<exact length      */
        {"corn cup cat cream coast","corn cup cat cream coast",'c',24},
                                   /* multi-inst, inst=1, exact length        */
        {"corn cup cat cream coast","corn cup cat cream coast",'c',1},
                                   /* multi-inst, inst = length, length=1     */
        {"is is a test","This is a test",105,14},  
                                   /* single int inst, exact length           */
        {"is is a test","This is a test",'i',14},  
                                   /* double inst, exact length               */
        {"a test","This is a test",'a',9}, 
                                   /* single instance instance = length       */
        {NULL,"This is a test",'b',14}, 
                                   /* no instance exact length                */
        {NULL,"This is a test",'a',8},  
                                   /* single instance - < length              */
        {NULL,"This is a test",121,14}, 
                                   /* single instance - exact length          */
        {" is a test of the function","This is a test of the function",
         ' ',17}                   /* single inst<length, len(string)>length  */
    };


    /* Initialize the PAL */
    if ( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Loop through the testcases in the structure */
    for (i=0; i< sizeof(testCases)/sizeof(struct testCase); i++)
    {
        /* Need to type cast function in order to compare the result */
        result = (char *)memchr(testCases[i].string,
                 testCases[i].character,testCases[i].length);

        if (result==NULL)
        {
           if (testCases[i].result != NULL)
           {
               Fail("ERROR:  Expected memcmp to return \"%s\" instead of"
                    " NULL\n", testCases[i].result);
           }
        }
        else
        {
           if (strcmp(result,testCases[i].result)!=0 )

           {
              Fail("ERROR:  Expected memcmp to return \"%s\" instead of"
                    " \"%s\"\n", testCases[i].result, result);
           }

        }
     }

    PAL_Terminate();

    return PASS;
}













