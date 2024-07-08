// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <limits>

extern "C" DLL_EXPORT void STDMETHODCALLTYPE Negate(bool* ptr)
{
    *ptr = !*ptr;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNaN(float* ptr)
{
    *ptr = std::numeric_limits<float>::quiet_NaN();
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE NegateDecimal(DECIMAL* ptr)
{
    ptr->sign = ptr->sign == 0 ? 0x80 : 0;
}
