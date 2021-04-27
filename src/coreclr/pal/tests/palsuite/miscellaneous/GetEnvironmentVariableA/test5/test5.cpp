// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** Source : test5.c
**
** Purpose: Test for GetEnvironmentVariableA() function
**          Create environment variables that differ only
**          in case and verify that they return the appropriate
**          value on the BSD environment.
**
**
===========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_GetEnvironmentVariableA_test5_paltest_getenvironmentvariablea_test5, "miscellaneous/GetEnvironmentVariableA/test5/paltest_getenvironmentvariablea_test5")
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
            "proper environment variable with error %u.\n", GetLastError());
    }

 
    /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariableA(FirstEnvironmentVariable,        
                                  pResultBuffer,    
                                  0);                 

    /* To account for the null character at the end of the string */
    size = size + 1;
    
    pResultBuffer = (char*)malloc(sizeof(char)*size);
    if ( pResultBuffer == NULL )
    {
	    Fail("ERROR: Failed to allocate memory for pResultBuffer pointer\n.");
    }

    /* Try to retrieve the value of the first environment variable */
    GetEnvironmentVariableA(FirstEnvironmentVariable,
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
        free(pResultBuffer);    
        Fail("ERROR: The value in the buffer should have been '%s' but "
             "was really '%s'.\n",FirstEnvironmentValue, pResultBuffer);          
    }

    free(pResultBuffer);

    /* Set the second environment Variable */
    bRc = SetEnvironmentVariableA(SecondEnvironmentVariable,
                            SecondEnvironmentValue);

    if(!bRc)
    {
        Fail("ERROR: SetEnvironmentVariable failed to set a "
            "proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Reallocate the memory for the string */
    pResultBuffer = (char*)malloc(sizeof(char)*size);
    if ( pResultBuffer == NULL )
    {
	    Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.");
    }

    /* Try retrieving the value of the first variable, even though the
    second variable has the same spelling and only differs in case */
    GetEnvironmentVariableA(FirstEnvironmentVariable,
                           pResultBuffer,
                           size);

    if ( pResultBuffer == NULL )
    {
	    free(pResultBuffer);
        Fail("ERROR: GetEnvironmentVariable failed to return a value "
            "from a proper environment variable with error %u.\n", 
            GetLastError());
    }

    /* Compare the two strings to confirm that the right value is returned */
    if(strcmp(pResultBuffer,FirstEnvironmentValue) != 0) 
    {
        free(pResultBuffer);    
        Fail("ERROR: The value in the buffer should have been '%s' but "
             "was really '%s'.\n",FirstEnvironmentValue,pResultBuffer);          
    }
  
    free(pResultBuffer);
    
    PAL_Terminate();
    return PASS;

#endif
}



