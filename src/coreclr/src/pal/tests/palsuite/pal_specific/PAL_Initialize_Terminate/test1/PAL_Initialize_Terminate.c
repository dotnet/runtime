//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: pal_initialize_terminate.c
**
** Purpose: Positive test the PAL_Initialize and PAL_Terminate API.
**          Call PAL_Initialize to initialize the PAL 
**          environment and call PAL_Terminate to clean up the PAL
**          environment.
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
        ExitProcess(1);
    }
    
    PAL_Terminate();
    return 0;
}
