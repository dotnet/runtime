// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Intended to be the child process of a debugger.  Calls 
**          OutputDebugStringW once with a normal string, once with an empty
**          string
**
**
**============================================================*/

#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    WCHAR *str1;
    WCHAR *str2;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    str1 = convert("Foo!");
    str2 = convert("");

    OutputDebugStringW(str1);

    OutputDebugStringW(str2);

    free(str1);
    free(str2);

    PAL_Terminate();
    return PASS;
}
