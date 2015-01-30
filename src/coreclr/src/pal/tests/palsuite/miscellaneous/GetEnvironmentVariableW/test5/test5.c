//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
** Source : test5.c
**
** Purpose: Test for GetEnvironmentVariableW() function
**          Create environment variables that differ only
**          in case and verify that they return the appropriate
**          value on the BSD environment.
**
**
===========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{

#if WIN32

    return PASS;

#else

    /* Define some buffers needed for the function */
    WCHAR * pResultBuffer = NULL;

    WCHAR FirstEnvironmentVariable[] = {'P','A','L','T','E','S','T','\0'};
    WCHAR FirstEnvironmentValue[] = {'F','I','R','S','T','\0'};

    WCHAR SecondEnvironmentVariable[] = {'p','a','l','t','e','s','t','\0'};
    WCHAR SecondEnvironmentValue[] = {'S','E','C','O','N','D','\0'};

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
    bRc = SetEnvironmentVariableW(FirstEnvironmentVariable,
                            FirstEnvironmentValue);

    if(!bRc)
    {
        Fail("ERROR: SetEnvironmentVariable failed to set a "
            "proper environment variable with error %u.\n", GetLastError());
    }

    /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariableW(FirstEnvironmentVariable,        
                                  pResultBuffer,    
                                  0);                 

    /* To account for the nul character at the end of the string */
    size = size + 1;
    
    pResultBuffer = malloc(sizeof(WCHAR)*size);
    if ( pResultBuffer == NULL )
    {
	    Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.\n");
    }

    /* Try to retrieve the value of the first environment variable */
    GetEnvironmentVariableW(FirstEnvironmentVariable,
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
    if(wcsncmp(pResultBuffer,FirstEnvironmentValue,wcslen(pResultBuffer)) != 0)
    {
        free(pResultBuffer);    
        Fail("ERROR: The value in the buffer should have been '%S' but "
             "was really '%S'.\n",FirstEnvironmentValue, pResultBuffer);          
    }

    free(pResultBuffer);

    /* Set the second environment Variable */
    bRc = SetEnvironmentVariableW(SecondEnvironmentVariable,
                            SecondEnvironmentValue);

    if(!bRc)
    {
        Fail("ERROR: SetEnvironmentVariable failed to set a "
            "proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Reallocate the memory for the string */
    pResultBuffer = malloc(sizeof(WCHAR)*size);
    if ( pResultBuffer == NULL )
    {
	    Fail("ERROR: Failed to allocate memory for pResultBuffer pointer.\n");
    }

    /* Try retrieving the value of the first variable, even though the
    second variable has the same spelling and only differs in case */
    GetEnvironmentVariableW(FirstEnvironmentVariable,
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
    if(wcsncmp(pResultBuffer,FirstEnvironmentValue,wcslen(pResultBuffer)) != 0) 
    {
        free(pResultBuffer);    
        Fail("ERROR: The value in the buffer should have been '%S' but "
             "was really '%S'.\n",FirstEnvironmentValue,pResultBuffer);          
    }
  
    free(pResultBuffer);
    
    PAL_Terminate();
    return PASS;

#endif
}



