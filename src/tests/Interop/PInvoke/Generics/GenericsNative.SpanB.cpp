// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceB
{
    intptr_t value;
};

struct SpanB
{
    ByReferenceB pointer;
    int32_t length;
};

static SpanB SpanBValue = { };

extern "C" DLL_EXPORT SpanB STDMETHODCALLTYPE GetSpanB(bool e00)
{
    throw "P/Invoke for Span<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanBOut(bool e00, SpanB* pValue)
{
    throw "P/Invoke for Span<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanB* STDMETHODCALLTYPE GetSpanBPtr(bool e00)
{
    throw "P/Invoke for Span<bool> should be unsupported.";
}

extern "C" DLL_EXPORT SpanB STDMETHODCALLTYPE AddSpanB(SpanB lhs, SpanB rhs)
{
    throw "P/Invoke for Span<bool> should be unsupported.";
}

extern "C" DLL_EXPORT SpanB STDMETHODCALLTYPE AddSpanBs(const SpanB* pValues, uint32_t count)
{
    throw "P/Invoke for Span<bool> should be unsupported.";
}
