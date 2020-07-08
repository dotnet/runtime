// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1C
{
    char16_t e00;
};

static Point1C Point1CValue = { };

extern "C" DLL_EXPORT Point1C STDMETHODCALLTYPE GetPoint1C(char16_t e00)
{
    throw "P/Invoke for Point1<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1COut(char16_t e00, Point1C* pValue)
{
    throw "P/Invoke for Point1<char> should be unsupported.";
}

extern "C" DLL_EXPORT const Point1C* STDMETHODCALLTYPE GetPoint1CPtr(char16_t e00)
{
    throw "P/Invoke for Point1<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point1C STDMETHODCALLTYPE AddPoint1C(Point1C lhs, Point1C rhs)
{
    throw "P/Invoke for Point1<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point1C STDMETHODCALLTYPE AddPoint1Cs(const Point1C* pValues, uint32_t count)
{
    throw "P/Invoke for Point1<char> should be unsupported.";
}
