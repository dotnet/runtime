// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ICorJitCompiler
#define _ICorJitCompiler

#include "runtimedetails.h"
#include "methodcallsummarizer.h"

class interceptor_ICJC : public ICorJitCompiler
{

#include "icorjitcompilerimpl.h"

public:
    // Added to help us track the original icjc and be able to easily indirect to it.
    ICorJitCompiler*      original_ICorJitCompiler;
    MethodCallSummarizer* mcs;
};

#endif
