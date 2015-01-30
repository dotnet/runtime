//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
