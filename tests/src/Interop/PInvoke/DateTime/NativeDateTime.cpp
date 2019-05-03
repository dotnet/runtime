// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>

extern "C" DLL_EXPORT DATE STDMETHODCALLTYPE GetTomorrow(DATE today)
{
    return today + 1;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetTomorrowByRef(DATE today, DATE* tomorrow)
{
    *tomorrow = today + 1;
}

struct DateWrapper
{
    DATE date;
};

extern "C" DLL_EXPORT DateWrapper STDMETHODCALLTYPE GetTomorrowWrapped(DateWrapper today)
{
    return { today.date + 1 };
}
