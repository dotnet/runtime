//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
#if defined(__APPLE__)
        .cfi_personality 0x9b, C_FUNC(\Handler) // 0x9b == DW_EH_PE_indirect | DW_EH_PE_pcrel | DW_EH_PE_sdata4
#else
        .cfi_personality 0, C_FUNC(\Handler) // 0 == DW_EH_PE_absptr
#endif
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
#if defined(__APPLE__)
        .section __LD,__compact_unwind,regular,debug
        .quad C_FUNC(\Name)
        .set C_FUNC(\Name\()_Size), C_FUNC(\Name\()_End) - C_FUNC(\Name)
        .long C_FUNC(\Name\()_Size)
        .long 0x04000000 # DWARF
        .quad 0
        .quad 0
#endif
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
#endif