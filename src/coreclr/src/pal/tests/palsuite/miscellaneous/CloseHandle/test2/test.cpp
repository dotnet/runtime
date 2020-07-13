// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CloseHandle function, try to close an unopened HANDLE
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{

    HANDLE SomeHandle = NULL;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
 
    /* If the handle is already closed and you can close it again, 
     * something is wrong. 
     */
  
    if(CloseHandle(SomeHandle) != 0) 
    {
        Fail("ERROR: Called CloseHandle on an already closed Handle "
             "and it still returned as a success.\n");
    }
  
    
    PAL_Terminate();
    return PASS;
}



