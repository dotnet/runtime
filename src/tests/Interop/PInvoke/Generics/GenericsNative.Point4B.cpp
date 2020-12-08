// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4B
{
    bool e00;
    bool e01;
    bool e02;
    bool e03;
};

static Point4B Point4BValue = { };

extern "C" DLL_EXPORT Point4B STDMETHODCALLTYPE GetPoint4B(bool e00, bool e01, bool e02, bool e03)
{
    throw "P/Invoke for Point4<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4BOut(bool e00, bool e01, bool e02, bool e03, Point4B* pValue)
{
    throw "P/Invoke for Point4<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const Point4B* STDMETHODCALLTYPE GetPoint4BPtr(bool e00, bool e01, bool e02, bool e03)
{
    throw "P/Invoke for Point4<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point4B STDMETHODCALLTYPE AddPoint4B(Point4B lhs, Point4B rhs)
{
    throw "P/Invoke for Point4<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point4B STDMETHODCALLTYPE AddPoint4Bs(const Point4B* pValues, uint32_t count)
{
    throw "P/Invoke for Point4<bool> should be unsupported.";
}
