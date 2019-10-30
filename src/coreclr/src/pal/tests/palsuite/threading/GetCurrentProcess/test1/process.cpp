// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: GetCurrentProcess/test1/process.c
**
** Purpose:  Test for to see if the process GetCurrentProcess
** returns a handle to the current process or not.
**
** Dependencies: TerminateProcess 
**
**
**=========================================================*/

#include <palsuite.h>

INT __cdecl main( int argc, char **argv ) 
{

    HANDLE hProcess; 
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
    
    hProcess = GetCurrentProcess();
    Trace ("Testing the handle returned by GetCurrentProcess\n");
    if ( 0 == ( TerminateProcess ( hProcess, PASS ) ) )
    {
	Fail ("Testing GetCurrentProcess, the TerminateProcess function "
		"failed.\n");
    }

    PAL_Terminate();
    return ( PASS );

}
