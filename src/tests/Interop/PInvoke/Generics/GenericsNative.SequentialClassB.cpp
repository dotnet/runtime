// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassB
{
    bool value;
};

static SequentialClassB SequentialClassBValue = { };

extern "C" DLL_EXPORT SequentialClassB* STDMETHODCALLTYPE GetSequentialClassB(bool value)
{
    throw "P/Invoke for SequentialClass<bool> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassBOut(bool value, SequentialClassB** pValue)
{
    throw "P/Invoke for SequentialClass<bool> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassB** STDMETHODCALLTYPE GetSequentialClassBPtr(bool value)
{
    throw "P/Invoke for SequentialClass<bool> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassB* STDMETHODCALLTYPE AddSequentialClassB(SequentialClassB* lhs, SequentialClassB* rhs)
{
    throw "P/Invoke for SequentialClass<bool> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassB* STDMETHODCALLTYPE AddSequentialClassBs(const SequentialClassB** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<bool> should be unsupported.";
}
