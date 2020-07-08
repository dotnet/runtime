// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3B
{
    bool e00;
    bool e01;
    bool e02;
};

static Point3B Point3BValue = { };

extern "C" DLL_EXPORT Point3B STDMETHODCALLTYPE GetPoint3B(bool e00, bool e01, bool e02)
{
    throw "P/Invoke for Point3<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3BOut(bool e00, bool e01, bool e02, Point3B* pValue)
{
    throw "P/Invoke for Point3<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const Point3B* STDMETHODCALLTYPE GetPoint3BPtr(bool e00, bool e01, bool e02)
{
    throw "P/Invoke for Point3<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point3B STDMETHODCALLTYPE AddPoint3B(Point3B lhs, Point3B rhs)
{
    throw "P/Invoke for Point3<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point3B STDMETHODCALLTYPE AddPoint3Bs(const Point3B* pValues, uint32_t count)
{
    throw "P/Invoke for Point3<bool> should be unsupported.";
}
