// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DISASSEMBLER_H__
#define __DISASSEMBLER_H__

#include "switches.h"

#define USE_COREDISTOOLS_DISASSEMBLER 0
#define USE_MSVC_DISASSEMBLER 0
#ifdef HAVE_GCCOVER
    #if defined(FEATURE_CORECLR)
        // COREDISTOOLS disassembler only supports amd64, so if this is
        // CoreCLR but not amd64, we will fall out of this check and not
        // set USE_DISASSEMBLER.
        #if defined(_TARGET_AMD64_)
            #undef USE_COREDISTOOLS_DISASSEMBLER
            #define USE_COREDISTOOLS_DISASSEMBLER 1
        #endif
    #elif defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
        #undef USE_MSVC_DISASSEMBLER
        #define USE_MSVC_DISASSEMBLER 1
    #endif // defined(FEATURE_CORECLR) || defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
#endif // HAVE_GCCOVER

#if USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
    #define USE_DISASSEMBLER 1
#else
    #define USE_DISASSEMBLER 0
#endif

#if USE_DISASSEMBLER

#if USE_MSVC_DISASSEMBLER
#undef free

// This pragma is needed because public\vc\inc\xiosbase contains
// a static local variable
#pragma warning(disable : 4640)
#include "msvcdis.h"
#pragma warning(default : 4640)

#include "disx86.h"

#define free(memblock) Use_free(memblock)
#endif // USE_MSVC_DISASSEMBLER

enum class InstructionType : UINT8
{
    Unknown,
    Call_DirectUnconditional,
    Call_IndirectUnconditional,
    Branch_IndirectUnconditional
};

class InstructionInfo;

// Wraps the MSVC disassembler or the coredistools disassembler
class Disassembler
{
#if USE_COREDISTOOLS_DISASSEMBLER
private:
    class CorDisasm;
    typedef CorDisasm ExternalDisassembler;
#elif USE_MSVC_DISASSEMBLER
private:
    typedef DIS ExternalDisassembler;
#endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER

#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
public:
    static bool IsRexPrefix(UINT8 potentialRexByte);
    static UINT8 DecodeModFromModRm(UINT8 modRm);
    static UINT8 DecodeRegOrOpCodeFromModRm(UINT8 modRm);
    static UINT8 DecodeRmFromModRm(UINT8 modRm);
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_X86_)

public:
    static bool IsAvailable();
    static void StaticInitialize();
    static void StaticClose();

public:
    Disassembler();
    ~Disassembler();

public:
    SIZE_T DisassembleInstruction(const UINT8 *code, SIZE_T codeLength, InstructionType *instructionTypeRef) const;
    static InstructionType DetermineInstructionType(
    #if USE_COREDISTOOLS_DISASSEMBLER
        const UINT8 *instructionCode, SIZE_T instructionCodeLength
    #elif USE_MSVC_DISASSEMBLER
        ExternalDisassembler::TRMT terminationType
    #endif // USE_COREDISTOOLS_DISASSEMBLER || USE_MSVC_DISASSEMBLER
        );

#if USE_COREDISTOOLS_DISASSEMBLER
private:
    static HMODULE s_libraryHandle;

    // 'coredistools' library exports
private:

    enum TargetArch {
      Target_Host, // Target is the same as host architecture
      Target_X86,
      Target_X64,
      Target_Thumb,
      Target_Arm64
    };

    static CorDisasm *(*External_InitDisasm)(enum TargetArch Target);
    static SIZE_T (*External_DisasmInstruction)(const CorDisasm *Disasm, size_t Address,
                                  const uint8_t *Bytes, size_t Maxlength,
                                  bool PrintAssembly);
    static void (*External_FinishDisasm)(const CorDisasm *Disasm);
#endif // USE_COREDISTOOLS_DISASSEMBLER

private:
    static ExternalDisassembler *s_availableExternalDisassembler;
    ExternalDisassembler *m_externalDisassembler;
};

#endif // USE_DISASSEMBLER
#endif // __DISASSEMBLER_H__
