// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceL
{
    intptr_t value;
};

struct SpanL
{
    ByReferenceL pointer;
    int32_t length;
};

static SpanL SpanLValue = { };

extern "C" DLL_EXPORT SpanL STDMETHODCALLTYPE GetSpanL(int64_t e00)
{
    throw "P/Invoke for Span<long> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanLOut(int64_t e00, SpanL* pValue)
{
    throw "P/Invoke for Span<long> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanL* STDMETHODCALLTYPE GetSpanLPtr(int64_t e00)
{
    throw "P/Invoke for Span<long> should be unsupported.";
}

extern "C" DLL_EXPORT SpanL STDMETHODCALLTYPE AddSpanL(SpanL lhs, SpanL rhs)
{
    throw "P/Invoke for Span<long> should be unsupported.";
}

extern "C" DLL_EXPORT SpanL STDMETHODCALLTYPE AddSpanLs(const SpanL* pValues, uint32_t count)
{
    throw "P/Invoke for Span<long> should be unsupported.";
}
