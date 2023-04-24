// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentVariable() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

PALTEST(miscellaneous_GetEnvironmentVariableW_test1_paltest_getenvironmentvariablew_test1, "miscellaneous/GetEnvironmentVariableW/test1/paltest_getenvironmentvariablew_test1")
{

    /* Define some buffers needed for the function */
    WCHAR * pResultBuffer = NULL;
    int size = 0;

    /* A place to stash the returned values */
    int ReturnValueForLargeBuffer = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Receive and allocate the correct amount of memory for the buffer */
    size = ReturnValueForLargeBuffer =
        GetEnvironmentVariable(convert("PATH"),
                               pResultBuffer,
                               0);

    pResultBuffer = (WCHAR*)malloc(size*sizeof(WCHAR));
    if ( pResultBuffer == NULL )
    {
	Fail("ERROR: Failed to allocate memory for pResultBuffer pointer. "
             "Can't properly exec test case without this.\n");
    }


    /* Normal case, PATH should fit into this buffer */
    ReturnValueForLargeBuffer = GetEnvironmentVariable(convert("PATH"),
                                                       pResultBuffer,
                                                       size);
    free(pResultBuffer);

    /* Ensure that it returned a positive value */
    if(ReturnValueForLargeBuffer <= 0)
    {
        Fail("The return was %d, which indicates that the function failed.\n",
             ReturnValueForLargeBuffer);
    }

    /* Ensure that it succeeded and copied the correct number of characters.
       If this is true, then the return value should be one less of the
       size of the buffer.  (Doesn't include that NULL byte)
    */
    if(ReturnValueForLargeBuffer != size-1)
    {
        Fail("The value returned was %d when it should have been %d.  This "
             "should be the number of characters copied, "
	     "minus the NULL byte.\n", ReturnValueForLargeBuffer, size-1);
    }

    PAL_Terminate();
    return PASS;
}



