// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4L
{
    int64_t e00;
    int64_t e01;
    int64_t e02;
    int64_t e03;
};

static Point4L Point4LValue = { };

extern "C" DLL_EXPORT Point4L STDMETHODCALLTYPE GetPoint4L(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    return { e00, e01, e02, e03 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4LOut(int64_t e00, int64_t e01, int64_t e02, int64_t e03, Point4L* pValue)
{
    *pValue = GetPoint4L(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const Point4L* STDMETHODCALLTYPE GetPoint4LPtr(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    GetPoint4LOut(e00, e01, e02, e03, &Point4LValue);
    return &Point4LValue;
}

extern "C" DLL_EXPORT Point4L STDMETHODCALLTYPE AddPoint4L(Point4L lhs, Point4L rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02,
        lhs.e03 + rhs.e03
    };
}

extern "C" DLL_EXPORT Point4L STDMETHODCALLTYPE AddPoint4Ls(const Point4L* pValues, uint32_t count)
{
    Point4L result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint4L(result, pValues[i]);
        }
    }

    return result;
}
