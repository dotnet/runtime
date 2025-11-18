// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocwasm.h"

LinearScanInterface* getLinearScanAllocator(Compiler* compiler)
{
    NYI_WASM("getLinearScanAllocator");
    return nullptr;
}

bool LinearScan::isRegCandidate(LclVarDsc* varDsc)
{
    NYI_WASM("isRegCandidate");
    return false;
}

bool LinearScan::isContainableMemoryOp(GenTree* node)
{
    NYI_WASM("isContainableMemoryOp");
    return false;
}
