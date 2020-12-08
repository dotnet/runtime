// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_initialize_twice.c
**
** Purpose: Positive test of PAL_Initialize and PAL_Terminate APIs.
**          Calls PAL_Initialize twice to ensure that doing so
**          will not cause unexpected failures in the PAL. 
**          Calls PAL_Terminate twice to clean up the PAL.
**          

**
**============================================================*/
#include <palsuite.h>

PALTEST(pal_specific_PAL_Initialize_Terminate_test2_paltest_pal_initialize_terminate_test2, "pal_specific/PAL_Initialize_Terminate/test2/paltest_pal_initialize_terminate_test2")
{
    /* Initialize the PAL environment */
    if (0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    } 

    /* Try calling PAL_Initialize again - should just increment the init_count. */
    if (0 != (PAL_Initialize(argc, argv)))
    {        
        // Call terminate due to the first PAL initialization.
        PAL_TerminateEx(FAIL);
        return FAIL;
    }        
      
    /* If both calls to PAL_Initialize succeed, then PAL_Terminate must be 
    called twice. The first call just decrements the init_count to 1. */

    PAL_Terminate();
    PAL_Terminate();
    return PASS;
}
