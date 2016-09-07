// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for lstrcpynW() function
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {

    WCHAR FirstString[5] = {'T','E','S','T','\0'};
    WCHAR CorrectBuffer[3] = {'T','E','\0'};
    WCHAR ResultBuffer[5];
    WCHAR* ResultPointer = NULL;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* A straight copy, the values should be equal. */
    ResultPointer = lstrcpyn(ResultBuffer,FirstString,3);
  
    /* Make sure the returned pointer is to the result buffer */
    if(ResultPointer != &ResultBuffer[0]) 
    {
        Fail("ERROR: The function didn't return a pointer which points to the "
             "location of the buffer which was copied into.\n");
    }

    /* Check to see that values are equal */
    if(memcmp(ResultBuffer,
              CorrectBuffer,
              wcslen(ResultBuffer)*sizeof(WCHAR)) != 0) 
    {
        Fail("ERROR: '%s' was the result and it should have been '%s' when "
             "this copy was performed.\n",
             convertC(ResultBuffer),convertC(CorrectBuffer));
    }

    /* Null values should get Null results */
    if(lstrcpyn(ResultBuffer,NULL,3) != NULL) 
    {
        Fail("ERROR: When the second parameter was set to NULL, the return "
             "value should have been NULL, but it was not.\n");
    }

    if(lstrcpyn(NULL,FirstString,3) != NULL) 
    {
        Fail("ERROR: When the first parameter was set to NULL, the return "
             "value should have been NULL, but it was not.\n");    
    }
  
  
    PAL_Terminate();
    return PASS;
}



