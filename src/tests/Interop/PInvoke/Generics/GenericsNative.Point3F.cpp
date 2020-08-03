// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3F
{
    float e00;
    float e01;
    float e02;
};

static Point3F Point3FValue = { };

extern "C" DLL_EXPORT Point3F STDMETHODCALLTYPE GetPoint3F(float e00, float e01, float e02)
{
    return { e00, e01, e02 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3FOut(float e00, float e01, float e02, Point3F* pValue)
{
    *pValue = GetPoint3F(e00, e01, e02);
}

extern "C" DLL_EXPORT const Point3F* STDMETHODCALLTYPE GetPoint3FPtr(float e00, float e01, float e02)
{
    GetPoint3FOut(e00, e01, e02, &Point3FValue);
    return &Point3FValue;
}

extern "C" DLL_EXPORT Point3F STDMETHODCALLTYPE AddPoint3F(Point3F lhs, Point3F rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02
    };
}

extern "C" DLL_EXPORT Point3F STDMETHODCALLTYPE AddPoint3Fs(const Point3F* pValues, uint32_t count)
{
    Point3F result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint3F(result, pValues[i]);
        }
    }

    return result;
}
