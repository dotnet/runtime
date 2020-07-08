// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2B
{
    bool e00;
    bool e01;
};

static Point2B Point2BValue = { };

extern "C" DLL_EXPORT Point2B STDMETHODCALLTYPE GetPoint2B(bool e00, bool e01)
{
    throw "P/Invoke for Point2<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2BOut(bool e00, bool e01, Point2B* pValue)
{
    throw "P/Invoke for Point2<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const Point2B* STDMETHODCALLTYPE GetPoint2BPtr(bool e00, bool e01)
{
    throw "P/Invoke for Point2<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point2B STDMETHODCALLTYPE AddPoint2B(Point2B lhs, Point2B rhs)
{
    throw "P/Invoke for Point2<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point2B STDMETHODCALLTYPE AddPoint2Bs(const Point2B* pValues, uint32_t count)
{
    throw "P/Invoke for Point2<bool> should be unsupported.";
}
