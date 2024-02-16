// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define INVALIDGCVALUE 0xCCCCCCCD

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
#if defined(HOST_AMD64) || defined(HOST_X86) || defined(HOST_S390X)
#define EXTERNAL_C_FUNC(name) C_FUNC(name)@plt
#else
#define EXTERNAL_C_FUNC(name) C_FUNC(name)
#endif
#define LOCAL_LABEL(name) .L##name
#endif

#if defined(__APPLE__)
#define C_PLTFUNC(name) _##name
#elif defined(HOST_ARM64) || defined(HOST_POWERPC64)
#define C_PLTFUNC(name) name
#else
#define C_PLTFUNC(name) name@PLT
#endif


.macro END_PROLOGUE
.endm

.macro SETALIAS New, Old
        .equiv \New, \Old
.endm

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
