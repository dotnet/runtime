// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

extern "C" int STDMETHODCALLTYPE Sum(int a, int b);

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallDependencySum(int a, int b)
{
    return Sum(a, b);
}

