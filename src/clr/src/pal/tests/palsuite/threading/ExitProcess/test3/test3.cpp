// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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



int __cdecl main( int argc, char **argv ) 

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
