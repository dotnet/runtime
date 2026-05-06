// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4D
{
    double e00;
    double e01;
    double e02;
    double e03;
};

static Point4D Point4DValue = { };

extern "C" DLL_EXPORT Point4D STDMETHODCALLTYPE GetPoint4D(double e00, double e01, double e02, double e03)
{
    return { e00, e01, e02, e03 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4DOut(double e00, double e01, double e02, double e03, Point4D* pValue)
{
    *pValue = GetPoint4D(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const Point4D* STDMETHODCALLTYPE GetPoint4DPtr(double e00, double e01, double e02, double e03)
{
    GetPoint4DOut(e00, e01, e02, e03, &Point4DValue);
    return &Point4DValue;
}

extern "C" DLL_EXPORT Point4D STDMETHODCALLTYPE AddPoint4D(Point4D lhs, Point4D rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02,
        lhs.e03 + rhs.e03
    };
}

extern "C" DLL_EXPORT Point4D STDMETHODCALLTYPE AddPoint4Ds(const Point4D* pValues, uint32_t count)
{
    Point4D result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint4D(result, pValues[i]);
        }
    }

    return result;
}
