// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the strtod function.
**          Convert a number of strings to doubles.  Ensure they
**          convert correctly.
**
**
**===================================================================*/

#include <palsuite.h>


struct testCase
{
    double CorrectResult;  /* The returned double value */
    char ResultString[20]; /* The remainder string */
    char string[20];       /* The test string */
};


PALTEST(c_runtime_strtod_test1_paltest_strtod_test1, "c_runtime/strtod/test1/paltest_strtod_test1")
{

    char * endptr;
    double result;  
    int i;
  
    struct testCase testCases[] = 
        {
            {1234,"","1234"},
            {-1234,"","-1234"},
            {1234.44,"","1234.44"},
            {1234e-5,"","1234e-5"},
            {1234e+5,"","1234e+5"},
            {12345E5,"","12345e5"},
            {1234.657e-8,"","1234.657e-8"},
            {1234567e-8,"foo","1234567e-8foo"},
            {999,"foo","999 foo"},
            {7,"foo"," 7foo"},
            {0,"a7","a7"},
            {-777777,"z zz","-777777z zz"}
        };
  
    /*
     *  Initialize the PAL
     */
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    /* Loop through the structure to test each case */
    for(i = 0; i < sizeof(testCases) / sizeof(struct testCase); i++)
    {
        result = strtod(testCases[i].string,&endptr);
      
        /* need to check the result and the endptr result */
        if ((testCases[i].CorrectResult != result) &&
           (strcmp(testCases[i].ResultString,endptr)!=0))
        {
            Fail("ERROR:  strtod returned %f instead of %f and "
                 "\"%s\" instead of \"%s\" for the test of \"%s\"\n",
                   result, 
                 testCases[i].CorrectResult,
                 endptr,
                 testCases[i].ResultString,
                 testCases[i].string);
        }
      
    }      
  
    PAL_Terminate();
    return PASS;
} 













