// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    bool result = TP_wcmp_s(expected, actual) == 0;

    TP_scpy_s(actual, TP_slen(actual), newValue);

    return result;
}


extern "C" DLL_EXPORT BOOL Marshal_Invalid(LPCSTR value)
{
    return FALSE;
}
