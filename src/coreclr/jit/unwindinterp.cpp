// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef TARGET_INTERP
#error "This should be included only for the interpreter target"
#endif // TARGET_INTERP

// Interpreter frames are walked by the interpreter itself, so there is no native unwind info.

void Compiler::unwindBegProlog()
{
}

void Compiler::unwindEndProlog()
{
}

void Compiler::unwindReserve()
{
}

void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
}
