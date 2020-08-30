// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

struct SequentialClassD
{
    double value;
};

static SequentialClassD SequentialClassDValue = { };

extern "C" DLL_EXPORT SequentialClassD* STDMETHODCALLTYPE GetSequentialClassD(double value)
{
    throw "P/Invoke for SequentialClass<double> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetSequentialClassDOut(double value, SequentialClassD** pValue)
{
    throw "P/Invoke for SequentialClass<double> should be unsupported.";
}

extern "C" DLL_EXPORT const SequentialClassD** STDMETHODCALLTYPE GetSequentialClassDPtr(double value)
{
    throw "P/Invoke for SequentialClass<double> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassD* STDMETHODCALLTYPE AddSequentialClassD(SequentialClassD* lhs, SequentialClassD* rhs)
{
    throw "P/Invoke for SequentialClass<double> should be unsupported.";
}

extern "C" DLL_EXPORT SequentialClassD* STDMETHODCALLTYPE AddSequentialClassDs(const SequentialClassD** pValues, uint32_t count)
{
    throw "P/Invoke for SequentialClass<double> should be unsupported.";
}
