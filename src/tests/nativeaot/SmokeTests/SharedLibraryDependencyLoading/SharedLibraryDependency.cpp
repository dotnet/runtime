// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

extern "C" DLL_EXPORT int32_t STDMETHODCALLTYPE MultiplyIntegers(int32_t a, int32_t b)
{
    return a * b;
}
