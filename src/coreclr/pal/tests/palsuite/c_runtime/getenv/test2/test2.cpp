// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Create environment variables that differ only in Case, and
**          verify that the BSD operating system treats the variables
**          differently.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_getenv_test2_paltest_getenv_test2, "c_runtime/getenv/test2/paltest_getenv_test2")
{
#if WIN32

    return PASS;

#else

    const char* FirstVariable = "PalTestingEnvironmentVariable=The value";
    const char* SecondVariable = "PALTESTINGEnvironmentVariable=Different value";
    const char* FirstVarName = "PalTestingEnvironmentVariable";
    const char* SecondVarName = "PALTESTINGEnvironmentVariable";
    const char* FirstVarValue = "The value";
    const char* SecondVarValue = "Different value";
    char* result;


    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Use _putenv to set the environment variables.  This ensures that the
       variables we're testing with are always present.
    */
    if(_putenv(FirstVariable) != 0)
    {
        Fail("ERROR: _putenv failed to set an environment variable that "
             "getenv will be using for testing.\n");
    }

    if(_putenv(SecondVariable) != 0)
    {
        Fail("ERROR: _putenv failed to set an environment variable that "
             "getenv will be using for testing.\n");
    }


    /* Call getenv -- ensure it doesn't return NULL and the string it returns
       is the value we set above. Also make sure that each environment variable,
       differing only by case, returns it's own value.
    */

    result = getenv(FirstVarName);
    if(result == NULL)
    {
        Fail("ERROR: The result of getenv on a valid Environment Variable "
             "was NULL, which indicates the environment variable was not "
             "found.\n");
    }

    if(strcmp(result, FirstVarValue) != 0)
    {
        Fail("ERROR: The value obtained by getenv() was not equal to the "
             "correct value of the environment variable.  The correct "
             "value is '%s' and the function returned '%s'.\n",
             FirstVarValue,
             result);
    }


    result = getenv(SecondVarName);
    if(result == NULL)
    {
        Fail("ERROR: The result of getenv on a valid Environment Variable "
             "was NULL, which indicates the environment variable was not "
             "found.\n");
    }

    if(strcmp(result, SecondVarValue) != 0)
    {
        Fail("ERROR: The value obtained by getenv() was not equal to the "
             "correct value of the environment variable.  The correct "
             "value is '%s' and the function returned '%s'.\n",
             SecondVarValue,
             result);
    }


    PAL_Terminate();
    return PASS;

#endif
}
