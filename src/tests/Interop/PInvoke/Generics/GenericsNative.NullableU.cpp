// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableU
{
    bool hasValue;
    uint32_t value;
};

static NullableU NullableUValue = { };

extern "C" DLL_EXPORT NullableU STDMETHODCALLTYPE GetNullableU(bool hasValue, uint32_t value)
{
    throw "P/Invoke for Nullable<uint> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableUOut(bool hasValue, uint32_t value, NullableU* pValue)
{
    throw "P/Invoke for Nullable<uint> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableU* STDMETHODCALLTYPE GetNullableUPtr(bool hasValue, uint32_t value)
{
    throw "P/Invoke for Nullable<uint> should be unsupported.";
}

extern "C" DLL_EXPORT NullableU STDMETHODCALLTYPE AddNullableU(NullableU lhs, NullableU rhs)
{
    throw "P/Invoke for Nullable<uint> should be unsupported.";
}

extern "C" DLL_EXPORT NullableU STDMETHODCALLTYPE AddNullableUs(const NullableU* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<uint> should be unsupported.";
}
