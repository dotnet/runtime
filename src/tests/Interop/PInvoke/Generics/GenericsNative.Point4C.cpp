// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4C
{
    char16_t e00;
    char16_t e01;
    char16_t e02;
    char16_t e03;
};

static Point4C Point4CValue = { };

extern "C" DLL_EXPORT Point4C STDMETHODCALLTYPE GetPoint4C(char16_t e00, char16_t e01, char16_t e02, char16_t e03)
{
    throw "P/Invoke for Point4<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4COut(char16_t e00, char16_t e01, char16_t e02, char16_t e03, Point4C* pValue)
{
    throw "P/Invoke for Point4<char> should be unsupported.";
}

extern "C" DLL_EXPORT const Point4C* STDMETHODCALLTYPE GetPoint4CPtr(char16_t e00, char16_t e01, char16_t e02, char16_t e03)
{
    throw "P/Invoke for Point4<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point4C STDMETHODCALLTYPE AddPoint4C(Point4C lhs, Point4C rhs)
{
    throw "P/Invoke for Point4<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point4C STDMETHODCALLTYPE AddPoint4Cs(const Point4C* pValues, uint32_t count)
{
    throw "P/Invoke for Point4<char> should be unsupported.";
}
