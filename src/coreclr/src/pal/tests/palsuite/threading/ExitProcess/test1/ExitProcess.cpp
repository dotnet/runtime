// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: exitprocess/test1/exitprocess.c
**
** Purpose: Test to ensure ExitProcess returns the argument given
**          to it. 
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main( int argc, char **argv ) 

{
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
	return ( FAIL );
    }
 
    ExitProcess(PASS);

    Fail ("ExitProcess(0) failed to exit.\n  Test Failed.\n");

    return ( FAIL);

}
