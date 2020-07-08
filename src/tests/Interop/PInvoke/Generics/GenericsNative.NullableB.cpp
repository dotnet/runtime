// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableB
{
    bool hasValue;
    bool value;
};

static NullableB NullableBValue = { };

extern "C" DLL_EXPORT NullableB STDMETHODCALLTYPE GetNullableB(bool hasValue, bool value)
{
    throw "P/Invoke for Nullable<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableBOut(bool hasValue, bool value, NullableB* pValue)
{
    throw "P/Invoke for Nullable<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableB* STDMETHODCALLTYPE GetNullableBPtr(bool hasValue, bool value)
{
    throw "P/Invoke for Nullable<bool> should be unsupported.";
}

extern "C" DLL_EXPORT NullableB STDMETHODCALLTYPE AddNullableB(NullableB lhs, NullableB rhs)
{
    throw "P/Invoke for Nullable<bool> should be unsupported.";
}

extern "C" DLL_EXPORT NullableB STDMETHODCALLTYPE AddNullableBs(const NullableB* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<bool> should be unsupported.";
}
