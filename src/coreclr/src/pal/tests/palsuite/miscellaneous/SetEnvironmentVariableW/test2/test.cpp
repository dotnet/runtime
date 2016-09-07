// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for SetEnvironmentVariableW() function
** Test to see that passing NULL to the first param fails.
** Test that passing NULL to both params fails.
** Set an environment variable, then pass NULL to the second param
** to delete it.  Then make the same call again, to check that it fails.
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{
  
    /* Define some buffers needed for the function */
    WCHAR VariableBuffer[] = {'P','A','L','T','E','S','T','\0'};
    WCHAR ValueBuffer[] = {'T','e','s','t','i','n','g','\0'};
    int SetResult;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    SetResult = SetEnvironmentVariable(NULL,ValueBuffer);
 
    /* Check that it fails if the first param is NULL */
    if(SetResult != 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned a success value, "
             "even though it was passed NULL as the first parameter and "
             "should have failed.\n");    
    }

    /* Check that it fails when both params are NULL */
    SetResult = SetEnvironmentVariable(NULL,NULL);
    if(SetResult != 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned a success value, even "
             "though it was passed NULL as the first and second parameter and "
             "should have failed.\n");
    }
  
    /* First, set the variable, which should be ok.  Then call the 
       function with the second parameter NULL twice -- the first call should
       pass, the second should fail.
    */
    SetResult = SetEnvironmentVariable(VariableBuffer,ValueBuffer);
    if(SetResult == 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned failure, when "
             "attempting to set a valid variable.\n");
    }

    SetResult = SetEnvironmentVariable(VariableBuffer,NULL);
    if(SetResult == 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned failure, when "
             "attempting to delete a variable.\n");
    }
  
    SetResult = SetEnvironmentVariable(VariableBuffer,NULL);
    if(SetResult != 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned success, when "
             "attempting to delete a variable which doesn't exist.\n");
    }
    
    PAL_Terminate();
    return PASS;
}



