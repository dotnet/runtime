// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test3.c
**
** Purpose: Positive test for ExitProcess.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
** 

**
**===========================================================================*/
#include <palsuite.h>



PALTEST(threading_ExitProcess_test3_paltest_exitprocess_test3, "threading/ExitProcess/test3/paltest_exitprocess_test3")

{
    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* terminate the PAL */
    PAL_Terminate();

    /* call ExitProcess() -- should work after PAL_Terminate() */
    ExitProcess( PASS );

    
    /* return failure if we reach here -- note no attempt at   */
    /* meaningful output because we've called PAL_Terminte().  */
    return FAIL; 
}
