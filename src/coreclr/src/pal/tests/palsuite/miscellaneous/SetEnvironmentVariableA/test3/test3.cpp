// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** Source : test3.c
**
** Purpose: Test for SetEnvironmentVariableA() function
**          Create environment variables that differ only
**          in case and verify that they return the appropriate
**          value on the BSD environment.
**
**
===========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_SetEnvironmentVariableA_test3_paltest_setenvironmentvariablea_test3, "miscellaneous/SetEnvironmentVariableA/test3/paltest_setenvironmentvariablea_test3")
{

#if WIN32

    return PASS;

#else

    /* Define some buffers needed for the function */
    char * pResultBuffer = NULL;

    char FirstEnvironmentVariable[] = {"PALTEST"};
    char FirstEnvironmentValue[] = {"FIRST"};

    char SecondEnvironmentVariable[] = {"paltest"};
    char SecondEnvironmentValue[] = {"SECOND"};

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

     /* Set the second environment Variable */
    bRc = SetEnvironmentVariableA(SecondEnvironmentVariable,
                            SecondEnvironmentValue);

    if(!bRc)
    {
        Fail("ERROR: SetEnvironmentVariable failed to set a "
            "proper environment variable with error %u.\n",
            GetLastError());
    }


   /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariableA(FirstEnvironmentVariable,        
                                  pResultBuffer,    
                                  0);       

    /* increase size to account for the null char at the end */
    size = size + 1;
    
    pResultBuffer = (char*)malloc(sizeof(char)*size);
    if ( pResultBuffer == NULL )
    {
        Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.\n");
    }

    /* Try to retrieve the value of the first environment variable */
    GetEnvironmentVariable(FirstEnvironmentVariable,
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

    /* Reallocate the memory for the string */
    pResultBuffer = (char*)malloc(sizeof(char)*size);
    if ( pResultBuffer == NULL )
    {
        Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.\n");
    }

    /* Try retrieving the value of the first variable, even though the
    second variable has the same spelling and only differs in case */
    GetEnvironmentVariable(SecondEnvironmentVariable,
                           pResultBuffer,
                           size);

    if ( pResultBuffer == NULL )
    {
        Fail("ERROR: GetEnvironmentVariable failed to return a value "
            "from a proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Compare the two strings to confirm that the right value is returned */
    if(strcmp(pResultBuffer,SecondEnvironmentValue) != 0) 
    {
        Trace("ERROR: The value in the buffer should have been '%s' but "
             "was really '%s'.\n",SecondEnvironmentValue,pResultBuffer);
        free(pResultBuffer);    
        Fail("");
    }
  
    free(pResultBuffer);
    
    PAL_Terminate();
    return PASS;

#endif
}
