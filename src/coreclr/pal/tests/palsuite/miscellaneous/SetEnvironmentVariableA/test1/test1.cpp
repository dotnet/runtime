// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================ 
**
** Source: test1.c
**
** Purpose: Test for SetEnvironmentVariableA() function. Set an
** environment variable and check to ensure success was returned.  Then
** get the environment variable and compare to the correct value.  Also, check
** that calling the function again, resets the variable properly.  And that
** calling with NULL deletes the variable.
**
** Depends:
**     GetEnvironmentVariable
**     memcmp
**     memset
**     strlen
**
**
**=========================================================*/

#include <palsuite.h>
#define BUF_SIZE 128

PALTEST(miscellaneous_SetEnvironmentVariableA_test1_paltest_setenvironmentvariablea_test1, "miscellaneous/SetEnvironmentVariableA/test1/paltest_setenvironmentvariablea_test1")
{
  
    /* Define some buffers needed for the function */
    char* VariableBuffer = "PALTEST";
    char* ValueBuffer = "Testing";
    char* SecondValueBuffer = "SecondTest";
    char NewValue[BUF_SIZE];
    int SetResult = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }



    /*
      Test #1
      =======
    */
    
    SetResult = SetEnvironmentVariable(VariableBuffer,
                                       ValueBuffer); 

    /* If result is 0, the SetEnviron function failed */
    if(SetResult == 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned 0, which indicates that "
             "it failed, even though it should have succeeded in setting the "
             "variable PALTEST.\n");    
    }

    memset(NewValue,0,BUF_SIZE);

    /* Grab the Environment variable we just set */
    if(GetEnvironmentVariable(VariableBuffer,NewValue,BUF_SIZE) <= 0)
    {
        Fail("ERROR: GetEnvironmentVariable returned 0 or less, which "
             "indicates that no value was read in from the given variable.");
    }

    /* Make sure that the value put into NewValue was indeed the environment 
       variable we set. 
    */
 
    if(memcmp(NewValue,ValueBuffer,strlen(ValueBuffer)+1) != 0) 
    {
        Fail("ERROR:  When retrieving the variable that was just set, a "
             "difference was found. Instead of the value being '%s' it "
             "was instead '%s'.\n",ValueBuffer,NewValue);
    }



    /*
      Test #2
      =======
    */
    
    /* If we set the same environment variable with a different value, the
       old value should be replaced.
    */

    SetResult = SetEnvironmentVariable(VariableBuffer,
                                       SecondValueBuffer);
 
    /* If result is 0, the SetEnviron function failed */
    if(SetResult == 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned 0, which indicates that "
             "it failed, even though it should have succeeded in re-setting "
             "the variable PALTEST.\n");    
    }

    memset(NewValue,0,BUF_SIZE);

    /* Grab the Environment variable we just set */
    if(GetEnvironmentVariable(VariableBuffer,NewValue,BUF_SIZE) <= 0)
    {
        Fail("ERROR: GetEnvironmentVariable returned 0 or less, which "
             "indicates that no value was read in from the given variable.");
    }
  
    /* Make sure that the value put into NewValue was indeed the environment 
       variable we set. 
    */
 
    if(memcmp(NewValue,SecondValueBuffer,strlen(SecondValueBuffer)+1) != 0) 
    {
        Fail("ERROR:  When retrieving the variable that was just set, a "
             "difference was found. Instead of the value being '%s' it "
             "was instead '%s'.\n",SecondValueBuffer,NewValue);
    }



    /*
      Test #3
      =======
    */
    
    /* Finally, set this variable with NULL, which should delete it from the
       current environment.
    */

    SetResult = SetEnvironmentVariable(VariableBuffer, NULL);
    
    /* If result is 0, the SetEnviron function failed */
    if(SetResult == 0) 
    {
        Fail("ERROR: SetEnvironmentVariable returned 0, which indicates that "
             "it failed, even though it should have succeeded in deleting "
             "the variable PALTEST.\n");    
    }

    memset(NewValue,0,BUF_SIZE);
    
    /* Grab the Environment variable we just set, ensure that it's 
       empty now.
    */
    if(GetEnvironmentVariable(VariableBuffer,NewValue,BUF_SIZE) != 0)
    {
        Fail("ERROR: GetEnvironmentVariable returned a non-zero value, "
             "even though the environment variable which was checked should "
             "have been empty.");
    }
        

    /*
      Clean Up
    */
    PAL_Terminate();
    return PASS;
}



