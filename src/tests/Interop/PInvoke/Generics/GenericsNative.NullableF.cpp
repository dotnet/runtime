// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableF
{
    bool hasValue;
    float value;
};

static NullableF NullableFValue = { };

extern "C" DLL_EXPORT NullableF STDMETHODCALLTYPE GetNullableF(bool hasValue, float value)
{
    throw "P/Invoke for Nullable<float> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableFOut(bool hasValue, float value, NullableF* pValue)
{
    throw "P/Invoke for Nullable<float> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableF* STDMETHODCALLTYPE GetNullableFPtr(bool hasValue, float value)
{
    throw "P/Invoke for Nullable<float> should be unsupported.";
}

extern "C" DLL_EXPORT NullableF STDMETHODCALLTYPE AddNullableF(NullableF lhs, NullableF rhs)
{
    throw "P/Invoke for Nullable<float> should be unsupported.";
}

extern "C" DLL_EXPORT NullableF STDMETHODCALLTYPE AddNullableFs(const NullableF* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<float> should be unsupported.";
}
