// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableL
{
    bool hasValue;
    int64_t value;
};

static NullableL NullableLValue = { };

extern "C" DLL_EXPORT NullableL STDMETHODCALLTYPE GetNullableL(bool hasValue, int64_t value)
{
    throw "P/Invoke for Nullable<long> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableLOut(bool hasValue, int64_t value, NullableL* pValue)
{
    throw "P/Invoke for Nullable<long> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableL* STDMETHODCALLTYPE GetNullableLPtr(bool hasValue, int64_t value)
{
    throw "P/Invoke for Nullable<long> should be unsupported.";
}

extern "C" DLL_EXPORT NullableL STDMETHODCALLTYPE AddNullableL(NullableL lhs, NullableL rhs)
{
    throw "P/Invoke for Nullable<long> should be unsupported.";
}

extern "C" DLL_EXPORT NullableL STDMETHODCALLTYPE AddNullableLs(const NullableL* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<long> should be unsupported.";
}
