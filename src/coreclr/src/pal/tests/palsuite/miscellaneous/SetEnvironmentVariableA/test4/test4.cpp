// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** Source : test4.c
**
** Purpose: Test for SetEnvironmentVariableA() function
**          Create environment variables that differ only
**          in case and verify that they return the appropriate
**          value in the WIN32 Environment
** 

**
===========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_SetEnvironmentVariableA_test4_paltest_setenvironmentvariablea_test4, "miscellaneous/SetEnvironmentVariableA/test4/paltest_setenvironmentvariablea_test4")
{

#if WIN32

    /* Define some buffers needed for the function */
    char * pResultBuffer = NULL;

    char FirstEnvironmentVariable[] = {"PALTEST"};
    char FirstEnvironmentValue[] = {"FIRST"};
    char ModifiedEnvVar[] = {"paltest"};

    DWORD size = 0;
    BOOL bRc = TRUE;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    /* Set the first environment variable */
    bRc = SetEnvironmentVariableA(FirstEnvironmentVariable,
                            FirstEnvironmentValue);

    if(!bRc)
    {
        Fail("ERROR: SetEnvironmentVariable failed to set a "
            "proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariableA(ModifiedEnvVar,        
                                  pResultBuffer,    
                                  0);
    
    /* To account for the null character at the end of the string */
    size = size + 1;
    
    pResultBuffer = (char*)malloc(sizeof(char)*size);
    if ( pResultBuffer == NULL )
    {
        Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.\n");
    }

    /* Try to retrieve the value of the first environment variable */
    GetEnvironmentVariableA(ModifiedEnvVar,
                           pResultBuffer,
                           size);

    if ( pResultBuffer == NULL )
    {
        free(pResultBuffer);
        Fail("ERROR: GetEnvironmentVariable failed to return a value "
            "from a proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Compare the strings to see that the correct variable was returned */
    if(strcmp(pResultBuffer,FirstEnvironmentValue) != 0) 
    {
        Trace("ERROR: The value in the buffer should have been '%s' but "
             "was really '%s'.\n",FirstEnvironmentValue, pResultBuffer);
        free(pResultBuffer);
        Fail("");
    }

    free(pResultBuffer);
  
    PAL_Terminate();
    return PASS;


#else

    return PASS;
#endif
}
