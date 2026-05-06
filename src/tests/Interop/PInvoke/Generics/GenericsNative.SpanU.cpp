// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceU
{
    intptr_t value;
};

struct SpanU
{
    ByReferenceU pointer;
    int32_t length;
};

static SpanU SpanUValue = { };

extern "C" DLL_EXPORT SpanU STDMETHODCALLTYPE GetSpanU(uint32_t e00)
{
    throw "P/Invoke for Span<uint> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanUOut(uint32_t e00, SpanU* pValue)
{
    throw "P/Invoke for Span<uint> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanU* STDMETHODCALLTYPE GetSpanUPtr(uint32_t e00)
{
    throw "P/Invoke for Span<uint> should be unsupported.";
}

extern "C" DLL_EXPORT SpanU STDMETHODCALLTYPE AddSpanU(SpanU lhs, SpanU rhs)
{
    throw "P/Invoke for Span<uint> should be unsupported.";
}

extern "C" DLL_EXPORT SpanU STDMETHODCALLTYPE AddSpanUs(const SpanU* pValues, uint32_t count)
{
    throw "P/Invoke for Span<uint> should be unsupported.";
}
