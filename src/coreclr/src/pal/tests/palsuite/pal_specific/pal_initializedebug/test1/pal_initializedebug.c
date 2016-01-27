// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
