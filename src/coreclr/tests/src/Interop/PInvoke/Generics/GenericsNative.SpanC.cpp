// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct ByReferenceC
{
    intptr_t value;
};

struct SpanC
{
    ByReferenceC pointer;
    int32_t length;
};

static SpanC SpanCValue = { };

extern "C" DLL_EXPORT SpanC STDMETHODCALLTYPE GetSpanC(char16_t e00)
{
    throw "P/Invoke for Span<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSpanCOut(char16_t e00, SpanC* pValue)
{
    throw "P/Invoke for Span<char> should be unsupported.";
}

extern "C" DLL_EXPORT const SpanC* STDMETHODCALLTYPE GetSpanCPtr(char16_t e00)
{
    throw "P/Invoke for Span<char> should be unsupported.";
}

extern "C" DLL_EXPORT SpanC STDMETHODCALLTYPE AddSpanC(SpanC lhs, SpanC rhs)
{
    throw "P/Invoke for Span<char> should be unsupported.";
}

extern "C" DLL_EXPORT SpanC STDMETHODCALLTYPE AddSpanCs(const SpanC* pValues, uint32_t count)
{
    throw "P/Invoke for Span<char> should be unsupported.";
}
