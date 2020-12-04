// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose:  Create an environment variable with _putenv and then use getenv 
** to check it.  Check that we get the expected errors with invalid input.
**
**
**===================================================================*/

#include <palsuite.h>

struct TestElement
{
    char _putenvString[1024];    /* argument string sent to putenv        */  
    char varName[1024];          /* variable component of argument string */
    char varValue[1024];         /* value component of argument string    */
    BOOL bValidString;           /* valid argument string identifier      */
};

PALTEST(c_runtime__putenv_test1_paltest_putenv_test1, "c_runtime/_putenv/test1/paltest_putenv_test1")
{
    struct TestElement TestCases[] = 
    {
        {"PalTestingEnvironmentVariable=A value", "PalTestingEnvironmentVariable",
        "A value", TRUE},
        {"AnotherVariable=", "AnotherVariable", "", TRUE},
        {"YetAnotherVariable", "", "", FALSE},
        {"=ADifferentVariable", "", "ADifferentVariable", FALSE},
        {"", "", "", FALSE}

    };

    int i;
    char *variableValue;
   
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    for (i = 0; i < (sizeof(TestCases)/sizeof(struct TestElement)) ; i++) 
    {
        if((_putenv(TestCases[i]._putenvString) == -1) && 
           ( TestCases[i].bValidString == TRUE))
        {
            Fail("ERROR: _putenv failed to set an environment "
                 "variable with a valid format.\n  Call was"
                 "_putenv(%s)\n", TestCases[i]._putenvString);
        }
        /* 
         * For valid _putenvString values, check to see the variable was set
         */
        if (TestCases[i].bValidString == TRUE)
        {       
            variableValue = getenv(TestCases[i].varName);
        
            if (variableValue == NULL)
            { 
                if (*TestCases[i].varValue != '\0')
                {
                    Fail("ERROR: getenv(%s) call returned NULL.\nThe call "
                         "should have returned \"%s\"\n", TestCases[i].varName
                         , TestCases[i].varValue);
                }
            }  
            else if ( strcmp(variableValue, TestCases[i].varValue) != 0) 
            {
                Fail("ERROR: _putenv(%s)\nshould have set the variable "
                     "%s\n to \"%s\".\nA subsequent call to getenv(%s)\n"
                     "returned \"%s\" instead.\n", TestCases[i]._putenvString
                     , TestCases[i].varName, TestCases[i].varValue
                     , TestCases[i].varName, variableValue);
            }
        }
        else 
            /*
             * Check to see that putenv fails for malformed _putenvString values
             */
        {
            variableValue = getenv(TestCases[i].varName);
        
            if (variableValue != NULL)
            { 
                Fail("ERROR: getenv(%s) call should have returned NULL.\n"
                     "Instead it returned \"%s\".\n", TestCases[i].varName
                     , TestCases[i].varValue);
            }
        }
    }

    PAL_Terminate();
    return PASS;
}
