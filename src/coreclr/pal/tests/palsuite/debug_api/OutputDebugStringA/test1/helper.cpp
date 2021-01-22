// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: helper.c
**
** Purpose: Intended to be the child process of a debugger.  Calls 
**          OutputDebugStringA once with a normal string, once with an empty
**          string
**
**
**============================================================*/

#include <palsuite.h>

PALTEST(debug_api_OutputDebugStringA_test1_paltest_outputdebugstringa_test1_helper, "debug_api/OutputDebugStringA/test1/paltest_outputdebugstringa_test1_helper")
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    OutputDebugStringA("Foo!\n");

    OutputDebugStringA("");

    /* give a chance to the debugger process to read the debug string before 
       exiting */
    Sleep(1000);

    PAL_Terminate();
    return PASS;
}
