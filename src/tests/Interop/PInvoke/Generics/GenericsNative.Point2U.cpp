// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2U
{
    uint32_t e00;
    uint32_t e01;
};

static Point2U Point2UValue = { };

extern "C" DLL_EXPORT Point2U STDMETHODCALLTYPE GetPoint2U(uint32_t e00, uint32_t e01)
{
    return { e00, e01 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2UOut(uint32_t e00, uint32_t e01, Point2U* pValue)
{
    *pValue = GetPoint2U(e00, e01);
}

extern "C" DLL_EXPORT const Point2U* STDMETHODCALLTYPE GetPoint2UPtr(uint32_t e00, uint32_t e01)
{
    GetPoint2UOut(e00, e01, &Point2UValue);
    return &Point2UValue;
}

extern "C" DLL_EXPORT Point2U STDMETHODCALLTYPE AddPoint2U(Point2U lhs, Point2U rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01
    };
}

extern "C" DLL_EXPORT Point2U STDMETHODCALLTYPE AddPoint2Us(const Point2U* pValues, uint32_t count)
{
    Point2U result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint2U(result, pValues[i]);
        }
    }

    return result;
}
