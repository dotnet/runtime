// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include "platformdefines.h"

extern "C" DLL_EXPORT BOOL Marshal_Ansi(LPCSTR expected, LPSTR actual, LPCSTR newValue)
{
    bool result = strcmp(expected, actual) == 0;

    strcpy_s(actual, strlen(actual), newValue);

    return result;
}

extern "C" DLL_EXPORT BOOL Marshal_Unicode(LPCWSTR expected, LPWSTR actual, LPCWSTR newValue)
{
    bool result = wcscmp(expected, actual) == 0;

    wcscpy_s(actual, wcslen(actual), newValue);

    return result;
}


extern "C" DLL_EXPORT BOOL Marshal_Invalid(LPCSTR value)
{
    return FALSE;
}
