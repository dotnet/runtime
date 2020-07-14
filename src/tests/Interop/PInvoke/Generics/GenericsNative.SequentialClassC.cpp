// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassC
{
    char16_t value;
};

static SequentialClassC SequentialClassCValue = { };

extern "C" DLL_EXPORT SequentialClassC* STDMETHODCALLTYPE GetSequentialClassC(char16_t value)
{
    throw "P/Invoke for SequentialClass<char> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassCOut(char16_t value, SequentialClassC** pValue)
{
    throw "P/Invoke for SequentialClass<char> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassC** STDMETHODCALLTYPE GetSequentialClassCPtr(char16_t value)
{
    throw "P/Invoke for SequentialClass<char> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassC* STDMETHODCALLTYPE AddSequentialClassC(SequentialClassC* lhs, SequentialClassC* rhs)
{
    throw "P/Invoke for SequentialClass<char> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassC* STDMETHODCALLTYPE AddSequentialClassCs(const SequentialClassC** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<char> should be unsupported.";
}
