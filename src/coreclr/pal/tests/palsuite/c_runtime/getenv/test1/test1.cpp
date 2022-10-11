// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Create an environment variable and then use getenv to get
** a pointer to it.  Check that the pointer is valid and that the string
** is what we expected.  Also check that searching for a non-existent
** variable will cause getenv to return NULL. Also check that function
** passes when the parameter has it's casing changed (e.g upper case)
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_getenv_test1_paltest_getenv_test1, "c_runtime/getenv/test1/paltest_getenv_test1")
{

    const char* SetVariable = "PalTestingEnvironmentVariable=The value";
    const char* VariableName = "PalTestingEnvironmentVariable";
    const char* VariableValue = "The value";
    char* result;


    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Use _putenv to set an environment variable.  This ensures that the
       variable we're testing on is always present.
    */

    if(_putenv(SetVariable) == -1)
    {
        Fail("ERROR: _putenv failed to set an environment variable that "
             "getenv will be using for testing.\n");
    }

    /* Call getenv -- ensure it doesn't return NULL and the string it returns
       is the value we set above.
    */

    result = getenv(VariableName);
    if(result == NULL)
    {
        Fail("ERROR: The result of getenv on a valid Environment Variable "
             "was NULL, which indicates the environment variable was not "
             "found.\n");
    }

    if(strcmp(result, VariableValue) != 0)
    {
        Fail("ERROR: The value obtained by getenv() was not equal to the "
             "correct value of the environment variable.  The correct "
             "value is '%s' and the function returned '%s'.\n",
             VariableValue,
             result);
    }

    /* Try calling getenv on an environment variable which doesn't
       exist.
    */
    result = getenv("SomeEnvironmentVariableThatReallyDoesNotExist");

    if(result != NULL)
    {
        Fail("ERROR: Called getenv on an environment variable which "
             "doesn't exist and it returned '%s' instead of NULL.\n",result);
    }

    PAL_Terminate();
    return PASS;
}
