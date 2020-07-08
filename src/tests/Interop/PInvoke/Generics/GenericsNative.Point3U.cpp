// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point3U
{
    uint32_t e00;
    uint32_t e01;
    uint32_t e02;
};

static Point3U Point3UValue = { };

extern "C" DLL_EXPORT Point3U STDMETHODCALLTYPE GetPoint3U(uint32_t e00, uint32_t e01, uint32_t e02)
{
    return { e00, e01, e02 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint3UOut(uint32_t e00, uint32_t e01, uint32_t e02, Point3U* pValue)
{
    *pValue = GetPoint3U(e00, e01, e02);
}

extern "C" DLL_EXPORT const Point3U* STDMETHODCALLTYPE GetPoint3UPtr(uint32_t e00, uint32_t e01, uint32_t e02)
{
    GetPoint3UOut(e00, e01, e02, &Point3UValue);
    return &Point3UValue;
}

extern "C" DLL_EXPORT Point3U STDMETHODCALLTYPE AddPoint3U(Point3U lhs, Point3U rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02
    };
}

extern "C" DLL_EXPORT Point3U STDMETHODCALLTYPE AddPoint3Us(const Point3U* pValues, uint32_t count)
{
    Point3U result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint3U(result, pValues[i]);
        }
    }

    return result;
}
