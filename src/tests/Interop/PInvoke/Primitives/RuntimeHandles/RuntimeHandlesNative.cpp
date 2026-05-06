// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <platformdefines.h>

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_In(HANDLE expected, HANDLE actual)
{
    return expected == actual ? TRUE : FALSE;
}
