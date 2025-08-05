// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define INVALIDGCVALUE 0xCCCCCCCD

// This must match HwExceptionCode.STATUS_NATIVEAOT_THREAD_ABORT
#define STATUS_NATIVEAOT_THREAD_ABORT     0x43

// Enforce subsections via symbols to workaround bugs in Xcode 15 linker.
#if defined(__APPLE__)
.subsections_via_symbols
#endif

#if defined(__APPLE__)
#define C_FUNC(name) _##name
#define EXTERNAL_C_FUNC(name) C_FUNC(name)
#define LOCAL_LABEL(name) L##name
#else
#define C_FUNC(name) name
#define EXTERNAL_C_FUNC(name) C_FUNC(name)@plt
#define LOCAL_LABEL(name) .L##name
#endif

#if defined(__APPLE__)
#define C_PLTFUNC(name) _##name
#else
#define C_PLTFUNC(name) name@PLT
#endif

.macro END_PROLOGUE
.endm

.macro SETALIAS New, Old
        .equiv \New, \Old
.endm

// Rename offsets of nested structures
#define OFFSETOF__ee_alloc_context__alloc_ptr (OFFSETOF__ee_alloc_context__m_rgbAllocContextBuffer + OFFSETOF__gc_alloc_context__alloc_ptr)
#define OFFSETOF__ee_alloc_context            OFFSETOF__Thread__m_eeAllocContext

// GC type flags
#define GC_ALLOC_FINALIZE           1
#define GC_ALLOC_ALIGN8_BIAS        4
#define GC_ALLOC_ALIGN8             8

#define G_FREE_OBJECT_METHOD_TABLE  g_pFreeObjectEEType

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
