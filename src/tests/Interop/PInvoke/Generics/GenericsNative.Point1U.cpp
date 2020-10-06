// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct Point1U
{
    uint32_t e00;
};

static Point1U Point1UValue = { };

extern "C" DLL_EXPORT Point1U STDMETHODCALLTYPE GetPoint1U(uint32_t e00)
{
    return { e00 };
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetPoint1UOut(uint32_t e00, Point1U* pValue)
{
    *pValue = GetPoint1U(e00);
}

extern "C" DLL_EXPORT const Point1U* STDMETHODCALLTYPE GetPoint1UPtr(uint32_t e00)
{
    GetPoint1UOut(e00, &Point1UValue);
    return &Point1UValue;
}

extern "C" DLL_EXPORT Point1U STDMETHODCALLTYPE AddPoint1U(Point1U lhs, Point1U rhs)
{
    return { lhs.e00 + rhs.e00 };
}

extern "C" DLL_EXPORT Point1U STDMETHODCALLTYPE AddPoint1Us(const Point1U* pValues, uint32_t count)
{
    Point1U result = { };

    if (pValues != nullptr)
    {
        for (uint32_t i = 0; i < count; i++)
        {
            result = AddPoint1U(result, pValues[i]);
        }
    }

    return result;
}
