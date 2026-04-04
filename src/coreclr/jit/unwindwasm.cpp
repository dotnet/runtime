// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//
// We don't have native-code-offset-based unwind info on WASM so the following functions are all no-ops.
//
void Compiler::unwindBegProlog()
{
}

void Compiler::unwindEndProlog()
{
}

void Compiler::unwindReserve()
{
}

void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
}

void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
}

void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
}
