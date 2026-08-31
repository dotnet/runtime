// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceF
{
    intptr_t value;
};

struct SpanF
{
    ByReferenceF pointer;
    int32_t length;
};

static SpanF SpanFValue = { };

extern "C" DLL_EXPORT SpanF STDMETHODCALLTYPE GetSpanF(float e00)
{
    throw "P/Invoke for Span<float> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanFOut(float e00, SpanF* pValue)
{
    throw "P/Invoke for Span<float> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanF* STDMETHODCALLTYPE GetSpanFPtr(float e00)
{
    throw "P/Invoke for Span<float> should be unsupported.";
}

extern "C" DLL_EXPORT SpanF STDMETHODCALLTYPE AddSpanF(SpanF lhs, SpanF rhs)
{
    throw "P/Invoke for Span<float> should be unsupported.";
}

extern "C" DLL_EXPORT SpanF STDMETHODCALLTYPE AddSpanFs(const SpanF* pValues, uint32_t count)
{
    throw "P/Invoke for Span<float> should be unsupported.";
}
