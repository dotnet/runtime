// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure ExitThread returns the right 
** value when shutting down the last thread of a process.
** All this program does is call ExitThread() with a predefined
** value.
**
** Dependencies: none
** 

**
**=========================================================*/

#include <palsuite.h>
#include "myexitcode.h"

int __cdecl main( int argc, char **argv ) 
{
    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }
    
    /* exit the current thread with a magic test value -- it should */
    /* terminate the process and return that test value from this   */
    /* program.                                                     */
    ExitThread( TEST_EXIT_CODE );

    /* technically we should never get here */
    PAL_Terminate();
    
    /* return failure */
    return FAIL;
}
