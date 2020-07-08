// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct NullableD
{
    bool hasValue;
    double value;
};

static NullableD NullableDValue = { };

extern "C" DLL_EXPORT NullableD STDMETHODCALLTYPE GetNullableD(bool hasValue, double value)
{
    throw "P/Invoke for Nullable<double> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNullableDOut(bool hasValue, double value, NullableD* pValue)
{
    throw "P/Invoke for Nullable<double> should be unsupported.";
}

extern "C" DLL_EXPORT const NullableD* STDMETHODCALLTYPE GetNullableDPtr(bool hasValue, double value)
{
    throw "P/Invoke for Nullable<double> should be unsupported.";
}

extern "C" DLL_EXPORT NullableD STDMETHODCALLTYPE AddNullableD(NullableD lhs, NullableD rhs)
{
    throw "P/Invoke for Nullable<double> should be unsupported.";
}

extern "C" DLL_EXPORT NullableD STDMETHODCALLTYPE AddNullableDs(const NullableD* pValues, uint32_t count)
{
    throw "P/Invoke for Nullable<double> should be unsupported.";
}
