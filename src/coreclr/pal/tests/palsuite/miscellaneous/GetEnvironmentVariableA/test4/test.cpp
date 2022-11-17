// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentVariable() function
** Set an Environment Variable, then use GetEnvironmentVariable to
** retrieve it -- ensure that it retrieves properly.
**
**
**=========================================================*/

/* Depends on SetEnvironmentVariableW (because we're implementing
   the wide version) and strcmp()
*/

#include <palsuite.h>

PALTEST(miscellaneous_GetEnvironmentVariableA_test4_paltest_getenvironmentvariablea_test4, "miscellaneous/GetEnvironmentVariableA/test4/paltest_getenvironmentvariablea_test4")
{

    /* Define some buffers needed for the function */
    char * pResultBuffer = NULL;
    WCHAR SomeEnvironmentVariable[] = {'P','A','L','T','E','S','T','\0'};
    WCHAR TheEnvironmentValue[] = {'T','E','S','T','\0'};
    int size = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    SetEnvironmentVariableW(SomeEnvironmentVariable,
                            TheEnvironmentValue);


    /* Normal case, PATH should fit into this buffer */
    size = GetEnvironmentVariable("PALTEST",         // Variable Name
                                  pResultBuffer,     // Buffer for Value
                                  0);                // Buffer size

    pResultBuffer = (char*)malloc(size);
    if ( pResultBuffer == NULL )
     {
	Fail("ERROR: Failed to allocate memory for pResultBuffer pointer. "
	       "Can't properly exec test case without this.\n");
     }


    GetEnvironmentVariable("PALTEST",
                           pResultBuffer,
                           size);

    if(strcmp(pResultBuffer,"TEST") != 0)
    {
        free(pResultBuffer);
        Fail("ERROR: The value in the buffer should have been 'TEST' but "
             "was really '%s'.\n",pResultBuffer);

    }

    free(pResultBuffer);

    PAL_Terminate();
    return PASS;
}



