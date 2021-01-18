// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(debug_api_OutputDebugStringW_test1_paltest_outputdebugstringw_test1, "debug_api/OutputDebugStringW/test1/paltest_outputdebugstringw_test1")
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
