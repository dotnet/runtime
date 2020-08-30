// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: getcurrentprocessid/test1/processid.c
**
** Purpose: Test to ensure GetCurrentProcessId returns the current 
** process id number.  This test compares the result of 
** GetCurrentProcessId to getpid.
**
**
**=========================================================*/

#include <palsuite.h>

INT __cdecl main( int argc, char **argv ) 
{

    DWORD dwProcessId; 

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    dwProcessId = GetCurrentProcessId();
    
    if ( 0 >= dwProcessId ) 
    {
        Fail ("%s has dwProcessId has id value %d\n", argv[0], 
		dwProcessId );
    }
    Trace ("%s has dwProcessId %d\nPassing test as dwProcessId is > 0\n"
	    , argv[0], dwProcessId);

    PAL_Terminate();
    return ( PASS ); 

}
