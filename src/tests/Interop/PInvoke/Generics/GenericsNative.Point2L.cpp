// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point2L
{
    int64_t e00;
    int64_t e01;
};

static Point2L Point2LValue = { };

extern "C" DLL_EXPORT Point2L STDMETHODCALLTYPE GetPoint2L(int64_t e00, int64_t e01)
{
    return { e00, e01 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint2LOut(int64_t e00, int64_t e01, Point2L* pValue)
{
    *pValue = GetPoint2L(e00, e01);
}

extern "C" DLL_EXPORT const Point2L* STDMETHODCALLTYPE GetPoint2LPtr(int64_t e00, int64_t e01)
{
    GetPoint2LOut(e00, e01, &Point2LValue);
    return &Point2LValue;
}

extern "C" DLL_EXPORT Point2L STDMETHODCALLTYPE AddPoint2L(Point2L lhs, Point2L rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01
    };
}

extern "C" DLL_EXPORT Point2L STDMETHODCALLTYPE AddPoint2Ls(const Point2L* pValues, uint32_t count)
{
    Point2L result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint2L(result, pValues[i]);
        }
    }

    return result;
}
