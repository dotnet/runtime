// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int __cdecl main(int argc, char *argv[])
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
