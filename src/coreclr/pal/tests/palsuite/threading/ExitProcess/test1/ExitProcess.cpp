// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(threading_ExitProcess_test1_paltest_exitprocess_test1, "threading/ExitProcess/test1/paltest_exitprocess_test1")

{
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
	return ( FAIL );
    }
 
    ExitProcess(PASS);

    Fail ("ExitProcess(0) failed to exit.\n  Test Failed.\n");

    return ( FAIL);

}
