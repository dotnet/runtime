// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1F
{
    float e00;
};

static Point1F Point1FValue = { };

extern "C" DLL_EXPORT Point1F STDMETHODCALLTYPE GetPoint1F(float e00)
{
    return { e00 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1FOut(float e00, Point1F* pValue)
{
    *pValue = GetPoint1F(e00);
}

extern "C" DLL_EXPORT const Point1F* STDMETHODCALLTYPE GetPoint1FPtr(float e00)
{
    GetPoint1FOut(e00, &Point1FValue);
    return &Point1FValue;
}

extern "C" DLL_EXPORT Point1F STDMETHODCALLTYPE AddPoint1F(Point1F lhs, Point1F rhs)
{
    return { lhs.e00 + rhs.e00 };
}

extern "C" DLL_EXPORT Point1F STDMETHODCALLTYPE AddPoint1Fs(const Point1F* pValues, uint32_t count)
{
    Point1F result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint1F(result, pValues[i]);
        }
    }

    return result;
}
