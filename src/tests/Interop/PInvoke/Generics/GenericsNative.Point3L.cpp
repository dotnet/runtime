// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3L
{
    int64_t e00;
    int64_t e01;
    int64_t e02;
};

static Point3L Point3LValue = { };

extern "C" DLL_EXPORT Point3L STDMETHODCALLTYPE GetPoint3L(int64_t e00, int64_t e01, int64_t e02)
{
    return { e00, e01, e02 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3LOut(int64_t e00, int64_t e01, int64_t e02, Point3L* pValue)
{
    *pValue = GetPoint3L(e00, e01, e02);
}

extern "C" DLL_EXPORT const Point3L* STDMETHODCALLTYPE GetPoint3LPtr(int64_t e00, int64_t e01, int64_t e02)
{
    GetPoint3LOut(e00, e01, e02, &Point3LValue);
    return &Point3LValue;
}

extern "C" DLL_EXPORT Point3L STDMETHODCALLTYPE AddPoint3L(Point3L lhs, Point3L rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02
    };
}

extern "C" DLL_EXPORT Point3L STDMETHODCALLTYPE AddPoint3Ls(const Point3L* pValues, uint32_t count)
{
    Point3L result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint3L(result, pValues[i]);
        }
    }

    return result;
}
