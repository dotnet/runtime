// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1B
{
    bool e00;
};

static Point1B Point1BValue = { };

extern "C" DLL_EXPORT Point1B STDMETHODCALLTYPE GetPoint1B(bool e00)
{
    throw "P/Invoke for Point1<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1BOut(bool e00, Point1B* pValue)
{
    throw "P/Invoke for Point1<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const Point1B* STDMETHODCALLTYPE GetPoint1BPtr(bool e00)
{
    throw "P/Invoke for Point1<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point1B STDMETHODCALLTYPE AddPoint1B(Point1B lhs, Point1B rhs)
{
    throw "P/Invoke for Point1<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Point1B STDMETHODCALLTYPE AddPoint1Bs(const Point1B* pValues, uint32_t count)
{
    throw "P/Invoke for Point1<bool> should be unsupported.";
}
