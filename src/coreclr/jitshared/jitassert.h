// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JITASSERT_H_
#define _JITASSERT_H_

#include "minipal/utils.h"

// assertAbort is the assert failure handler shared between JIT and interpreter.
// It is defined in error.cpp (JIT) and compiler.cpp (interpreter).
#ifdef DEBUG

extern "C" void ANALYZER_NORETURN assertAbort(const char* why, const char* file, unsigned line);

#undef assert
#define assert(p) (void)((p) || (assertAbort(#p, __FILE__, __LINE__), 0))

#else // !DEBUG

#undef assert
#define assert(p) ((void)0)

#endif // !DEBUG

#endif // _JITASSERT_H_
