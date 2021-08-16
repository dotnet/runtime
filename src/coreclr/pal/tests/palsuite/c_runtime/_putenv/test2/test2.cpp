// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose:  Create an environment variable with _putenv and then use getenv 
** to check it.  This test resets an environment variable.
**
**
**===================================================================*/

#include <palsuite.h>

const char *_putenvString0 = "AnUnusualVariable=AnUnusualValue";
const char *_putenvString1 = "AnUnusualVariable=";
const char *variable = "AnUnusualVariable";
const char *value = "AnUnusualValue";

PALTEST(c_runtime__putenv_test2_paltest_putenv_test2, "c_runtime/_putenv/test2/paltest_putenv_test2")
{
   
    char *variableValue;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if(_putenv(_putenvString0) == -1)
    {
        Fail("ERROR: _putenv failed to set an environment "
             "variable with a valid format.\n  Call was"
             "_putenv(%s)\n", _putenvString0);
    }

    variableValue = getenv(variable);
    
    if (variableValue == NULL)
    { 
        Fail("ERROR: getenv(%s) call returned NULL\nThe call "
             "should have returned '%s'\n", variable, value);
    }  
    else 
    {
        if ( strcmp(variableValue, value) != 0 ) 
        {
            Fail("ERROR: _putenv(%s)\nshould have set the variable "
                 "'%s'\n to '%s'.\nA subsequent call to getenv(%s)\n"
                 "returned '%s' instead.\n", _putenvString0,
                 variable, value, variable, variableValue);
        }
        else 
        {
            if(_putenv(_putenvString1) == -1)
            {
                Fail("ERROR: _putenv failed to set an environment "
                     "variable with a valid format.\n  Call was"
                     "_putenv(%s)\n", _putenvString1);
            }

            variableValue = getenv(variable);

            if (variableValue != NULL)
            { 
                Fail("ERROR: getenv(%s) call did not return NULL.\nThe call "
                     "returned '%s'.\n", variable, value);
            }
        }
    }
    
    PAL_Terminate();
    return PASS;
}
