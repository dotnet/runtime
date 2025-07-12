// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FAILURES_H_
#define _FAILURES_H_

#if defined(__GNUC__) || defined(__clang__)
#define INTERPRETER_NORETURN    __attribute__((noreturn))
#else
#define INTERPRETER_NORETURN    __declspec(noreturn)
#endif

INTERPRETER_NORETURN void NO_WAY(const char* message);
INTERPRETER_NORETURN void BADCODE(const char* message);
INTERPRETER_NORETURN void NOMEM();

#endif
