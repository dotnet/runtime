// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassF
{
    float value;
};

static SequentialClassF SequentialClassFValue = { };

extern "C" DLL_EXPORT SequentialClassF* STDMETHODCALLTYPE GetSequentialClassF(float value)
{
    throw "P/Invoke for SequentialClass<float> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassFOut(float value, SequentialClassF** pValue)
{
    throw "P/Invoke for SequentialClass<float> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassF** STDMETHODCALLTYPE GetSequentialClassFPtr(float value)
{
    throw "P/Invoke for SequentialClass<float> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassF* STDMETHODCALLTYPE AddSequentialClassF(SequentialClassF* lhs, SequentialClassF* rhs)
{
    throw "P/Invoke for SequentialClass<float> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassF* STDMETHODCALLTYPE AddSequentialClassFs(const SequentialClassF** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<float> should be unsupported.";
}
