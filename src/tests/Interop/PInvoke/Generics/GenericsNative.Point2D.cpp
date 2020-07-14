// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2D
{
    double e00;
    double e01;
};

static Point2D Point2DValue = { };

extern "C" DLL_EXPORT Point2D STDMETHODCALLTYPE GetPoint2D(double e00, double e01)
{
    return { e00, e01 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2DOut(double e00, double e01, Point2D* pValue)
{
    *pValue = GetPoint2D(e00, e01);
}

extern "C" DLL_EXPORT const Point2D* STDMETHODCALLTYPE GetPoint2DPtr(double e00, double e01)
{
    GetPoint2DOut(e00, e01, &Point2DValue);
    return &Point2DValue;
}

extern "C" DLL_EXPORT Point2D STDMETHODCALLTYPE AddPoint2D(Point2D lhs, Point2D rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01
    };
}

extern "C" DLL_EXPORT Point2D STDMETHODCALLTYPE AddPoint2Ds(const Point2D* pValues, uint32_t count)
{
    Point2D result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint2D(result, pValues[i]);
        }
    }

    return result;
}
