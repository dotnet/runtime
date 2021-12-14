// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define INVALIDGCVALUE 0xCCCCCCCD

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

#if defined(HOST_AMD64)
#include "unixasmmacrosamd64.inc"
#elif defined(HOST_ARM)
#include "unixasmmacrosarm.inc"
#elif defined(HOST_ARM64)
#include "unixasmmacrosarm64.inc"
#elif defined(HOST_X86)
#include "unixasmmacrosx86.inc"
#endif
