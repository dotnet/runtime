// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceD
{
    intptr_t value;
};

struct SpanD
{
    ByReferenceD pointer;
    int32_t length;
};

static SpanD SpanDValue = { };

extern "C" DLL_EXPORT SpanD STDMETHODCALLTYPE GetSpanD(double e00)
{
    throw "P/Invoke for Span<double> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanDOut(double e00, SpanD* pValue)
{
    throw "P/Invoke for Span<double> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanD* STDMETHODCALLTYPE GetSpanDPtr(double e00)
{
    throw "P/Invoke for Span<double> should be unsupported.";
}

extern "C" DLL_EXPORT SpanD STDMETHODCALLTYPE AddSpanD(SpanD lhs, SpanD rhs)
{
    throw "P/Invoke for Span<double> should be unsupported.";
}

extern "C" DLL_EXPORT SpanD STDMETHODCALLTYPE AddSpanDs(const SpanD* pValues, uint32_t count)
{
    throw "P/Invoke for Span<double> should be unsupported.";
}
