//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: pal_initializedebug.c
**
** Purpose: Positive test the PAL_InitializeDebug API.
**
** Test the PAL_InitializeDebug, it will be NOPs for all
** platforms other than Mac. There is no other way of testing it
** currently
**          

**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
  
    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    
    if(0 != err)
    {
        return FAIL;
    }
    
    PAL_InitializeDebug();

    PAL_Terminate();
    return 0;
}
