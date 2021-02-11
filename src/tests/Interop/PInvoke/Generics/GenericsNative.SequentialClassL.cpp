// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassL
{
    int64_t value;
};

static SequentialClassL SequentialClassLValue = { };

extern "C" DLL_EXPORT SequentialClassL* STDMETHODCALLTYPE GetSequentialClassL(int64_t value)
{
    throw "P/Invoke for SequentialClass<long> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassLOut(int64_t value, SequentialClassL** pValue)
{
    throw "P/Invoke for SequentialClass<long> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassL** STDMETHODCALLTYPE GetSequentialClassLPtr(int64_t value)
{
    throw "P/Invoke for SequentialClass<long> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassL* STDMETHODCALLTYPE AddSequentialClassL(SequentialClassL* lhs, SequentialClassL* rhs)
{
    throw "P/Invoke for SequentialClass<long> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassL* STDMETHODCALLTYPE AddSequentialClassLs(const SequentialClassL** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<long> should be unsupported.";
}
