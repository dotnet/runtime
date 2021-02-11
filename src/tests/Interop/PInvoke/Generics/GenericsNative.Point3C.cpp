// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3C
{
    char16_t e00;
    char16_t e01;
    char16_t e02;
};

static Point3C Point3CValue = { };

extern "C" DLL_EXPORT Point3C STDMETHODCALLTYPE GetPoint3C(char16_t e00, char16_t e01, char16_t e02)
{
    throw "P/Invoke for Point3<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3COut(char16_t e00, char16_t e01, char16_t e02, Point3C* pValue)
{
    throw "P/Invoke for Point3<char> should be unsupported.";
}

extern "C" DLL_EXPORT const Point3C* STDMETHODCALLTYPE GetPoint3CPtr(char16_t e00, char16_t e01, char16_t e02)
{
    throw "P/Invoke for Point3<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point3C STDMETHODCALLTYPE AddPoint3C(Point3C lhs, Point3C rhs)
{
    throw "P/Invoke for Point3<char> should be unsupported.";
}

extern "C" DLL_EXPORT Point3C STDMETHODCALLTYPE AddPoint3Cs(const Point3C* pValues, uint32_t count)
{
    throw "P/Invoke for Point3<char> should be unsupported.";
}
