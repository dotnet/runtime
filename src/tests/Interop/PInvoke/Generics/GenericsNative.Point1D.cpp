// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1D
{
    double e00;
};

static Point1D Point1DValue = { };

extern "C" DLL_EXPORT Point1D STDMETHODCALLTYPE GetPoint1D(double e00)
{
    return { e00 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1DOut(double e00, Point1D* pValue)
{
    *pValue = GetPoint1D(e00);
}

extern "C" DLL_EXPORT const Point1D* STDMETHODCALLTYPE GetPoint1DPtr(double e00)
{
    GetPoint1DOut(e00, &Point1DValue);
    return &Point1DValue;
}

extern "C" DLL_EXPORT Point1D STDMETHODCALLTYPE AddPoint1D(Point1D lhs, Point1D rhs)
{
    return { lhs.e00 + rhs.e00 };
}

extern "C" DLL_EXPORT Point1D STDMETHODCALLTYPE AddPoint1Ds(const Point1D* pValues, uint32_t count)
{
    Point1D result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint1D(result, pValues[i]);
        }
    }

    return result;
}
