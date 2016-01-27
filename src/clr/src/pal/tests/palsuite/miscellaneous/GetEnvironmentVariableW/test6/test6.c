// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
** Source : test6.c
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

    /* Define some buffers needed for the function */
    WCHAR * pResultBuffer = NULL;

    WCHAR FirstEnvironmentVariable[] = {'P','A','L','T','E','S','T','\0'};
    WCHAR FirstEnvironmentValue[] = {'F','I','R','S','T','\0'};

    WCHAR ModifiedEnvironmentVariable[] = {'p','a','l','t','e','s','t','\0'};

    DWORD  size = 0;
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
            "proper environment variable with error %u.\n",
            GetLastError());
    }

    /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariableW(ModifiedEnvironmentVariable,        
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
    GetEnvironmentVariableW(ModifiedEnvironmentVariable,
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
  
    PAL_Terminate();
    return PASS;


#else

    return PASS;
#endif
}
