// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1L
{
    int64_t e00;
};

static Point1L Point1LValue = { };

extern "C" DLL_EXPORT Point1L STDMETHODCALLTYPE GetPoint1L(int64_t e00)
{
    return { e00 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1LOut(int64_t e00, Point1L* pValue)
{
    *pValue = GetPoint1L(e00);
}

extern "C" DLL_EXPORT const Point1L* STDMETHODCALLTYPE GetPoint1LPtr(int64_t e00)
{
    GetPoint1LOut(e00, &Point1LValue);
    return &Point1LValue;
}

extern "C" DLL_EXPORT Point1L STDMETHODCALLTYPE AddPoint1L(Point1L lhs, Point1L rhs)
{
    return { lhs.e00 + rhs.e00 };
}

extern "C" DLL_EXPORT Point1L STDMETHODCALLTYPE AddPoint1Ls(const Point1L* pValues, uint32_t count)
{
    Point1L result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint1L(result, pValues[i]);
        }
    }

    return result;
}
