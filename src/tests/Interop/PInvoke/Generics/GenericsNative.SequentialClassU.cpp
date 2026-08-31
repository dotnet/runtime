// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassU
{
    uint32_t value;
};

static SequentialClassU SequentialClassUValue = { };

extern "C" DLL_EXPORT SequentialClassU* STDMETHODCALLTYPE GetSequentialClassU(uint32_t value)
{
    throw "P/Invoke for SequentialClass<uint> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassUOut(uint32_t value, SequentialClassU** pValue)
{
    throw "P/Invoke for SequentialClass<uint> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassU** STDMETHODCALLTYPE GetSequentialClassUPtr(uint32_t value)
{
    throw "P/Invoke for SequentialClass<uint> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassU* STDMETHODCALLTYPE AddSequentialClassU(SequentialClassU* lhs, SequentialClassU* rhs)
{
    throw "P/Invoke for SequentialClass<uint> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassU* STDMETHODCALLTYPE AddSequentialClassUs(const SequentialClassU** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<uint> should be unsupported.";
}
