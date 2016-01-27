// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for lstrcpyW() function
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    WCHAR FirstString[5] = {'T','E','S','T','\0'};
    WCHAR ResultBuffer[5];
    WCHAR* ResultPointer = NULL;
	
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ResultPointer = lstrcpy(ResultBuffer,FirstString);

    /* Check the return value */
    if(ResultPointer != &ResultBuffer[0]) 
    {
        Fail("ERROR: The function did not return a pointer to the Result "
             "Buffer after being called.\n");   
    }

    /* A straight copy, the values should be equal. */	
    if(memcmp(ResultBuffer,FirstString,wcslen(ResultBuffer)*2+2) != 0) 
    {
        Fail("ERROR: The result of the copy was '%s' when it should have "
             "been '%s'.\n",convertC(ResultBuffer),convertC(FirstString));
    }
  
    /* If either param is NULL, it should return NULL. */
    if(lstrcpy(ResultBuffer,NULL) != NULL)  
    {
        Fail("ERROR: The second parameter was NULL, so the function should "
             "fail and return NULL.\n");    
    }
    if(lstrcpy(NULL,FirstString) != NULL) 
    {
        Fail("ERROR: The first parameter was NULL, so the function should "
             "fail and return NULL.\n");
    }
    
    
    PAL_Terminate();
    return PASS;
}


