// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableC
{
    bool hasValue;
    char16_t value;
};

static NullableC NullableCValue = { };

extern "C" DLL_EXPORT NullableC STDMETHODCALLTYPE GetNullableC(bool hasValue, char16_t value)
{
    throw "P/Invoke for Nullable<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableCOut(bool hasValue, char16_t value, NullableC* pValue)
{
    throw "P/Invoke for Nullable<char> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableC* STDMETHODCALLTYPE GetNullableCPtr(bool hasValue, char16_t value)
{
    throw "P/Invoke for Nullable<char> should be unsupported.";
}

extern "C" DLL_EXPORT NullableC STDMETHODCALLTYPE AddNullableC(NullableC lhs, NullableC rhs)
{
    throw "P/Invoke for Nullable<char> should be unsupported.";
}

extern "C" DLL_EXPORT NullableC STDMETHODCALLTYPE AddNullableCs(const NullableC* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<char> should be unsupported.";
}
