// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: terminateprocess/test1/terminateprocess.c
**
** Purpose:  Test to see if TerminateProcess will 
**           terminate the current process.  
**
** Dependencies: GetCurrentProcess
**
**
**=========================================================*/

#include <palsuite.h>

INT __cdecl main( int argc, char **argv ) 
{

    HANDLE hProcess; 
         
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }
    
    hProcess = GetCurrentProcess();
    
    Trace ("Testing TerminateProcess function.\n");
    
    if ( 0 == ( TerminateProcess ( hProcess, PASS ) ) )
    {
        Fail ("TerminateProcess failed.\n");
    }

    PAL_TerminateEx(FAIL);
    return (FAIL);

}
