// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//===--------- coredistools.h - Disassembly tools for CoreClr ------------===//
//
//  Core Disassembly Tools API Version 1.4.0
//  Disassembly tools required by CoreCLR for utilities like
//  GCStress, SuperPMI, and R2RDump.
//===----------------------------------------------------------------------===//

#if !defined(_COREDISTOOLS_H_)
#define _COREDISTOOLS_H_

#include <stdint.h>

#if defined(__cplusplus)
#define EXTERN_C extern "C"
#else
#define EXTERN_C
#endif // defined(__cplusplus)

#if defined(_MSC_VER)
#if defined(DllInterfaceExporter)
#define DllIface EXTERN_C __declspec(dllexport)
#else
#define DllIface EXTERN_C __declspec(dllimport)
#endif // defined(DllInterfaceExporter)
#else
#if !defined(__cdecl)
#if defined(__i386__)
#define __cdecl __attribute__((cdecl))
#else
#define __cdecl
#endif
#endif
#define DllIface EXTERN_C
#endif // defined(_MSC_VER)

enum TargetArch {
    Target_Host, // Target is the same as host architecture
    Target_X86,
    Target_X64,
    Target_Thumb,
    Target_Arm64,
    Target_LoongArch64
};

struct CorDisasm;
struct CorAsmDiff;

// The custom print functionality to be provide by the
// users of this Library
typedef void(__cdecl *Printer)(const char *msg, ...);
struct PrintControl {
    const Printer Error;
    const Printer Warning;
    const Printer Log;
    const Printer Dump;
};

// The type of a custom function provided by the user to determine
// if two offsets are considered equivalent wrt diffing code blocks.
// Offset1 and Offset2 are the two offsets to be compared.
// BlockOffset is the offset of the instructions (that contain Offset1
// and Offset2) from the beginning of their respective code blocks.
// InstructionLength is the length of the current instruction being
// compared for equivalency.
typedef bool(__cdecl *OffsetComparator)(const void *UserData, size_t BlockOffset,
    size_t InstructionLength, uint64_t Offset1,
    uint64_t Offset2);

// If an OffsetMunger function is defined, it is called before the OffsetComparator.
// If it returns `true` then:
// 1. the instructions are considered equivalent
// 2. the offsets have been decoded and "munged" (changed), and
//    *Offset1 and *Offset2 are set to the values to use.
// 3. *SkipInstructions1 instructions in code stream 1 are skipped
// 4. *SkipInstructions2 instructions in code stream 2 are skipped
//
// This is typically used on arm32 to treat "movw/movt" as a single instruction
// generating a single constant. Similarly, for arm64 mov/movk/movk/movk sequences.
typedef bool(__cdecl *OffsetMunger)(const void *UserData, size_t BlockOffset,
    size_t InstructionLength, uint64_t* Offset1, uint64_t* Offset2,
    uint32_t* SkipInstructions1, uint32_t* SkipInstructions2);

// The Export/Import definitions for CoreDistools library are defined below.
// A typedef for each interface function's type is defined in order to aid
// the importer.

// Initialize the disassembler, using default print controls
typedef CorDisasm * __cdecl InitDisasm_t(enum TargetArch Target);
DllIface InitDisasm_t InitDisasm;

// Initialize the disassembler, using buffered print controls
typedef CorDisasm * __cdecl InitBufferedDisasm_t(enum TargetArch Target);
DllIface InitBufferedDisasm_t InitBufferedDisasm;

// Initialize the disassembler using custom print controls
typedef CorDisasm * __cdecl NewDisasm_t(enum TargetArch Target,
    const PrintControl *PControl);
DllIface NewDisasm_t NewDisasm;

// Delete the disassembler
typedef void __cdecl FinishDisasm_t(const CorDisasm *Disasm);
DllIface FinishDisasm_t FinishDisasm;

// Initialize a code differ using buffered output.
typedef CorDisasm * __cdecl InitBufferedDiffer_t(enum TargetArch Target,
                               const OffsetComparator Comparator);
DllIface InitBufferedDiffer_t InitBufferedDiffer;

// Initialize the Code Differ
typedef CorAsmDiff * __cdecl NewDiffer_t(enum TargetArch Target,
    const PrintControl *PControl,
    const OffsetComparator Comparator);
DllIface NewDiffer_t NewDiffer;

// Initialize the Code Differ, with an offset munger.
typedef CorAsmDiff * __cdecl NewDiffer2_t(enum TargetArch Target,
    const PrintControl *PControl,
    const OffsetComparator Comparator,
    const OffsetMunger Munger);
DllIface NewDiffer2_t NewDiffer2;

// Delete the Code Differ
typedef void __cdecl FinishDiff_t(const CorAsmDiff *AsmDiff);
DllIface FinishDiff_t FinishDiff;

// DisasmInstruction -- Disassemble one instruction
// Arguments:
// Disasm -- The Disassembler
// Address -- The address at which the bytes of the instruction
//            are intended to execute
// Bytes -- Pointer to the actual bytes which need to be disassembled
// MaxLength -- Number of bytes available in Bytes buffer
// Returns:
//   -- The Size of the disassembled instruction
//   -- Zero on failure
typedef size_t __cdecl DisasmInstruction_t(const CorDisasm *Disasm,
    const uint8_t *Address,
    const uint8_t *Bytes, size_t Maxlength);
DllIface DisasmInstruction_t DisasmInstruction;

// DumpInstruction -- Disassemble one instruction and output it
// Arguments:
// Disasm -- The Disassembler
// Address -- The address at which the bytes of the instruction
//            are intended to execute
// Bytes -- Pointer to the actual bytes which need to be disassembled
// MaxLength -- Number of bytes available in Bytes buffer
// Returns:
//   -- The Size of the disassembled instruction
//   -- Zero on failure
typedef size_t __cdecl DumpInstruction_t(const CorDisasm *Disasm,
	const uint8_t *Address, const uint8_t *Bytes,
	size_t Maxlength);
DllIface DumpInstruction_t DumpInstruction;

// NearDiffCodeBlocks -- Compare two code blocks for semantic
//                       equivalence
// Arguments:
// AsmDiff -- The Asm-differ
// UserData -- Any data the user wishes to pass through into
//             the OffsetComparator/OffsetMunger
// Address1 -- Address at which first block will execute
// Bytes1 -- Pointer to the actual bytes of the first block
// Size1 -- The size of the first block
// Address2 -- Address at which second block will execute
// Bytes2 -- Pointer to the actual bytes of the second block
// Size2 -- The size of the second block
// Returns:
//   -- true if the two blocks are equivalent, false if not.
typedef bool __cdecl NearDiffCodeBlocks_t(const CorAsmDiff *AsmDiff,
    const void *UserData,
    const uint8_t *Address1,
    const uint8_t *Bytes1, size_t Size1,
    const uint8_t *Address2,
    const uint8_t *Bytes2, size_t Size2);
DllIface NearDiffCodeBlocks_t NearDiffCodeBlocks;

// Print a code block according to the Disassembler's Print Controls
typedef void __cdecl DumpCodeBlock_t(const CorDisasm *Disasm, const uint8_t *Address,
    const uint8_t *Bytes, size_t Size);
DllIface DumpCodeBlock_t DumpCodeBlock;

// Print the two code blocks being diffed, according to
// AsmDiff's PrintControls.
typedef void __cdecl DumpDiffBlocks_t(const CorAsmDiff *AsmDiff,
    const uint8_t *Address1, const uint8_t *Bytes1,
    size_t Size1, const uint8_t *Address2,
    const uint8_t *Bytes2, size_t Size2);
DllIface DumpDiffBlocks_t DumpDiffBlocks;

// Get a pointer to the buffered output buffer.
typedef const char* __cdecl GetOutputBuffer_t();
DllIface GetOutputBuffer_t GetOutputBuffer;

// Clear the buffered output buffer.
typedef void __cdecl ClearOutputBuffer_t();
DllIface ClearOutputBuffer_t ClearOutputBuffer;

#endif // !defined(_COREDISTOOLS_H_)
