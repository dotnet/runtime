// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point4U
{
    uint32_t e00;
    uint32_t e01;
    uint32_t e02;
    uint32_t e03;
};

static Point4U Point4UValue = { };

extern "C" DLL_EXPORT Point4U STDMETHODCALLTYPE GetPoint4U(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    return { e00, e01, e02, e03 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint4UOut(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, Point4U* pValue)
{
    *pValue = GetPoint4U(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const Point4U* STDMETHODCALLTYPE GetPoint4UPtr(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    GetPoint4UOut(e00, e01, e02, e03, &Point4UValue);
    return &Point4UValue;
}

extern "C" DLL_EXPORT Point4U STDMETHODCALLTYPE AddPoint4U(Point4U lhs, Point4U rhs)
{
    return {
        lhs.e00 + rhs.e00,
        lhs.e01 + rhs.e01,
        lhs.e02 + rhs.e02,
        lhs.e03 + rhs.e03
    };
}

extern "C" DLL_EXPORT Point4U STDMETHODCALLTYPE AddPoint4Us(const Point4U* pValues, uint32_t count)
{
    Point4U result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint4U(result, pValues[i]);
        }
    }

    return result;
}
