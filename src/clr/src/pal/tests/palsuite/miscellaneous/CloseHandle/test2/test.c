//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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



