// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2F
{
    float e00;
    float e01;
};

static Point2F Point2FValue = { };

extern "C" DLL_EXPORT Point2F STDMETHODCALLTYPE GetPoint2F(float e00, float e01)
{
    return { e00, e01 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2FOut(float e00, float e01, Point2F* pValue)
{
    *pValue = GetPoint2F(e00, e01);
}

extern "C" DLL_EXPORT const Point2F* STDMETHODCALLTYPE GetPoint2FPtr(float e00, float e01)
{
    GetPoint2FOut(e00, e01, &Point2FValue);
    return &Point2FValue;
}

extern "C" DLL_EXPORT Point2F STDMETHODCALLTYPE AddPoint2F(Point2F lhs, Point2F rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01
    };
}

extern "C" DLL_EXPORT Point2F STDMETHODCALLTYPE AddPoint2Fs(const Point2F* pValues, uint32_t count)
{
    Point2F result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint2F(result, pValues[i]);
        }
    }

    return result;
}
