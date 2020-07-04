// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4F
{
    float e00;
    float e01;
    float e02;
    float e03;
};

static Point4F Point4FValue = { };

extern "C" DLL_EXPORT Point4F STDMETHODCALLTYPE GetPoint4F(float e00, float e01, float e02, float e03)
{
    return { e00, e01, e02, e03 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4FOut(float e00, float e01, float e02, float e03, Point4F* pValue)
{
    *pValue = GetPoint4F(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const Point4F* STDMETHODCALLTYPE GetPoint4FPtr(float e00, float e01, float e02, float e03)
{
    GetPoint4FOut(e00, e01, e02, e03, &Point4FValue);
    return &Point4FValue;
}

extern "C" DLL_EXPORT Point4F STDMETHODCALLTYPE AddPoint4F(Point4F lhs, Point4F rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02,
        lhs.e03 + rhs.e03
    };
}

extern "C" DLL_EXPORT Point4F STDMETHODCALLTYPE AddPoint4Fs(const Point4F* pValues, uint32_t count)
{
    Point4F result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint4F(result, pValues[i]);
        }
    }

    return result;
}
