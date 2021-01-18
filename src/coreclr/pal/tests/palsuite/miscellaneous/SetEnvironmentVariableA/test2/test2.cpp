// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================  
**
** Source: test2.c
**
** Purpose: Test for SetEnvironmentVariableA() function
** Test to see that passing NULL to the first param fails.
** Test that passing NULL to both params fails.
** Set an environment variable, then pass NULL to the second param
** to delete it.  Then make the same call again, to check that it fails.
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_SetEnvironmentVariableA_test2_paltest_setenvironmentvariablea_test2, "miscellaneous/SetEnvironmentVariableA/test2/paltest_setenvironmentvariablea_test2")
{
  
    /* Define some buffers needed for the function */
    char* VariableBuffer = "PALTEST";
    char* ValueBuffer = "testing";
    int SetResult;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Check that it fails if the first param is NULL */
    
    SetResult = SetEnvironmentVariable(NULL,ValueBuffer);
 
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



