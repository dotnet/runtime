// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(pal_specific_PAL_Initialize_Terminate_test1_paltest_pal_initialize_terminate_test1, "pal_specific/PAL_Initialize_Terminate/test1/paltest_pal_initialize_terminate_test1")
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
