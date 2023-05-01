// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

extern "C" DLL_EXPORT int32_t STDMETHODCALLTYPE E()
{
    return 4;
}

extern "C" DLL_EXPORT int32_t STDMETHODCALLTYPE EParams(int32_t a, int32_t b)
{
    return a + b + 4;
}

extern "C" DLL_EXPORT int32_t STDMETHODCALLTYPE EPtrs(int32_t* a, int32_t* b)
{
    return *a + *b + 4;
}
