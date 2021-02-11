// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3D
{
    double e00;
    double e01;
    double e02;
};

static Point3D Point3DValue = { };

extern "C" DLL_EXPORT Point3D STDMETHODCALLTYPE GetPoint3D(double e00, double e01, double e02)
{
    return { e00, e01, e02 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3DOut(double e00, double e01, double e02, Point3D* pValue)
{
    *pValue = GetPoint3D(e00, e01, e02);
}

extern "C" DLL_EXPORT const Point3D* STDMETHODCALLTYPE GetPoint3DPtr(double e00, double e01, double e02)
{
    GetPoint3DOut(e00, e01, e02, &Point3DValue);
    return &Point3DValue;
}

extern "C" DLL_EXPORT Point3D STDMETHODCALLTYPE AddPoint3D(Point3D lhs, Point3D rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02
    };
}

extern "C" DLL_EXPORT Point3D STDMETHODCALLTYPE AddPoint3Ds(const Point3D* pValues, uint32_t count)
{
    Point3D result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint3D(result, pValues[i]);
        }
    }

    return result;
}
