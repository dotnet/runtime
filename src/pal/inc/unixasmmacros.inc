// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define INVALIDGCVALUE -0x33333333 // 0CCCCCCCDh - the assembler considers it to be a signed integer constant

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

.macro LEAF_END Name, Section
        LEAF_END_MARKED \Name, \Section
.endm

.macro END_PROLOGUE
.endm

.macro SETALIAS New, Old
        .equiv \New, \Old
.endm

#if defined(_AMD64_)
#include "unixasmmacrosamd64.inc"
#elif defined(_ARM_)
#include "unixasmmacrosarm.inc"
#elif defined(_ARM64_)
#include "unixasmmacrosarm64.inc"
#endif
