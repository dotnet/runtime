// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ICorJitCompiler
#define _ICorJitCompiler

#include "runtimedetails.h"

class MethodContext;

class interceptor_ICJC : public ICorJitCompiler
{

#include "icorjitcompilerimpl.h"

public:
    // Added to help us track the original icjc and be able to easily indirect to it.
    ICorJitCompiler* original_ICorJitCompiler;
    HANDLE           hFile;
    CORINFO_OS       currentOs;

    void finalizeAndCommitCollection(MethodContext* mc, CorJitResult result, uint8_t* nativeEntry, uint32_t nativeSizeOfCode);
};

#endif
