// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <AsmMacros_Unix.inc>

#define G_TRAP_THREADS              g_TrapReturningThreads

// Offset of ee_alloc_context relative to INLINE_GET_ALLOC_CONTEXT_BASE.
//
// Since we have a disparity on how thread locals are accessed on various platforms and
// the current value of OFFSETOF__RuntimeThreadLocals__ee_alloc_context is zero we expect
// the helpers to add the OFFSETOF__RuntimeThreadLocals__ee_alloc_context constant to the
// base and set the additional offset to zero.
// In other words, we treat INLINE_GET_ALLOC_CONTEXT_BASE as returning the same value as
// GetThreadEEAllocContext.
#define OFFSETOF__ee_alloc_context            0

#if defined(HOST_X86)
#include "unixasmmacrosx86.inc"
#elif defined(HOST_AMD64)
#include "unixasmmacrosamd64.inc"
#elif defined(HOST_ARM)
#include "unixasmmacrosarm.inc"
#elif defined(HOST_ARMV6)
#include "unixasmmacrosarm.inc"
#elif defined(HOST_ARM64)
#include "unixasmmacrosarm64.inc"
#elif defined(HOST_S390X)
#include "unixasmmacross390x.inc"
#elif defined(HOST_LOONGARCH64)
#include "unixasmmacrosloongarch64.inc"
#elif defined(HOST_RISCV64)
#include "unixasmmacrosriscv64.inc"
#elif defined(HOST_POWERPC64)
#include "unixasmmacrosppc64le.inc"
#endif

// Mark the stack as non-executable for ELF targets.
// https://github.com/dotnet/runtime/issues/125469
#if !defined(__APPLE__)
.pushsection .note.GNU-stack,"",%progbits
.popsection
#endif
