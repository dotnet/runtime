// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <AsmMacros_Unix.inc>

// Rename offsets of nested structures
#define OFFSETOF__ee_alloc_context__alloc_ptr (OFFSETOF__ee_alloc_context__m_rgbAllocContextBuffer + OFFSETOF__gc_alloc_context__alloc_ptr)
#define OFFSETOF__ee_alloc_context            OFFSETOF__Thread__m_eeAllocContext

#define G_TRAP_THREADS              RhpTrapThreads

#if defined(HOST_AMD64)
#include "unixasmmacrosamd64.inc"
#elif defined(HOST_ARM)
#include "unixasmmacrosarm.inc"
#elif defined(HOST_ARM64)
#include "unixasmmacrosarm64.inc"
#elif defined(HOST_X86)
#include "unixasmmacrosx86.inc"
#elif defined(HOST_LOONGARCH64)
#include "unixasmmacrosloongarch64.inc"
#elif defined(HOST_RISCV64)
#include "unixasmmacrosriscv64.inc"
#endif

// Mark the stack as non-executable for ELF targets.
// https://github.com/dotnet/runtime/issues/125469
#if !defined(__APPLE__)
.pushsection .note.GNU-stack,"",%progbits
.popsection
#endif
