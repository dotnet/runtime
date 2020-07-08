// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2C
{
    char16_t e00;
    char16_t e01;
};

static Point2C Point2CValue = { };

extern "C" DLL_EXPORT Point2C STDMETHODCALLTYPE GetPoint2C(char16_t e00, char16_t e01)
{
    throw "P/Invoke for Point2<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2COut(char16_t e00, char16_t e01, Point2C* pValue)
{
    throw "P/Invoke for Point2<char> should be unsupported.";
}

extern "C" DLL_EXPORT const Point2C* STDMETHODCALLTYPE GetPoint2CPtr(char16_t e00, char16_t e01)
{
    throw "P/Invoke for Point2<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point2C STDMETHODCALLTYPE AddPoint2C(Point2C lhs, Point2C rhs)
{
    throw "P/Invoke for Point2<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point2C STDMETHODCALLTYPE AddPoint2Cs(const Point2C* pValues, uint32_t count)
{
    throw "P/Invoke for Point2<char> should be unsupported.";
}
