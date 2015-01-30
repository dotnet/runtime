//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Create environment variables that differ only in Case, and
**          verify that the BSD operating system treats the variables
**          differently.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
#if WIN32

    return PASS;

#else

    const char* FirstVariable = "PalTestingEnvironmentVariable=The value";
    const char* SecondVariable = "PALTESTINGEnvironmentVariable=Different value";
    const char* FirstVarName = "PalTestingEnvironmentVariable";
    const char* FirstVarValue = "The value";
    char* result;

   
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Use _putenv to set an environment variable.  This ensures that the
       variable we're testing on is always present.
    */

    if(_putenv(FirstVariable) != 0)
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
             "was NULL, which indicates the environment varaible was not "
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
    
    /* Set the second environment variable, which only differs in Case */
    if(_putenv(SecondVariable) != 0)
    {
        Fail("ERROR: _putenv failed to set an environment variable that "
             "getenv will be using for testing.\n");
    }

    /* Verify that the environment variables
    */
    
    result = getenv(FirstVarName);
    if(result == NULL)
    {
        Fail("ERROR: The result of getenv on a valid Environment Variable "
             "was NULL, which indicates the environment varaible was not "
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

    PAL_Terminate();
    return PASS;

#endif
}
