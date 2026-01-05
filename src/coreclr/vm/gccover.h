// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __GCCOVER_H__
#define __GCCOVER_H__

#ifdef HAVE_GCCOVER

/****************************************************************************/
/* GCCOverageInfo holds the state of which instructions have been visited by
   a GC and which ones have not */

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4200 )  // zero-sized array
#endif // _MSC_VER

class GCCoverageInfo {
public:
    IJitManager::MethodRegionInfo methodRegion;
    BYTE*         curInstr;         // The last instruction that was able to execute

        // Following 6 variables are for prolog / epilog walking coverage
    ICodeManager* codeMan;          // CodeMan for this method
    GCInfoToken gcInfoToken;             // gcInfo for this method

    Thread* callerThread;           // Thread associated with context callerRegs
    T_CONTEXT callerRegs;             // register state when method was entered
    unsigned gcCount;               // GC count at the time we caputured the regs
    bool    doingEpilogChecks;      // are we doing epilog unwind checks? (do we care about callerRegs?)

    enum { hasExecutedSize = 4 };
    unsigned hasExecuted[hasExecutedSize];
    unsigned totalCount;

    union
    {
        BYTE savedCode[0];              // really variable sized
                                        // Note that DAC doesn't marshal the entire byte array automatically.
                                        // Any client of this field needs to get the TADDR of this field and
                                        // marshal over the bytes properly.
    };

    void SprinkleBreakpoints(BYTE * saveAddr, PCODE codeStart, size_t codeSize, size_t regionOffsetAdj, BOOL fZapped);
};

typedef DPTR(GCCoverageInfo) PTR_GCCoverageInfo; // see code:GCCoverageInfo::savedCode

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#define INTERRUPT_INSTR                        0xF4    // X86 HLT instruction (any 1 byte illegal instruction will do)

#if defined(TARGET_X86)
#define INTERRUPT_INSTR_CALL                   0xFA    // X86 CLI instruction
#define INTERRUPT_INSTR_PROTECT_RET            0xFB    // X86 STI instruction, protect the first return register
#define INTERRUPT_INSTR_PROTECT_CONT           0xEC    // X86 IN instruction, protect the continuation register
#define INTERRUPT_INSTR_PROTECT_CONT_AND_RET   0xED    // X86 IN instruction, protect both continuation and return registers
#endif

#elif defined(TARGET_ARM)

// 16-bit illegal instructions which will cause exception and cause
// control to go to GcStress codepath
#define INTERRUPT_INSTR                 0xde00

// 32-bit illegal instructions. It is necessary to replace a 16-bit instruction
// with a 16-bit illegal instruction, and a 32-bit instruction with a 32-bit
// illegal instruction, to make GC stress with the "IT" instruction work, since
// it counts the number of instructions that follow it, so we can't change that
// number by replacing a 32-bit instruction with a 16-bit illegal instruction
// followed by 16 bits of junk that might end up being a legal instruction.
// Use the "Permanently UNDEFINED" section in the "ARM Architecture Reference Manual",
// section A6.3.4 "Branches and miscellaneous control" table.
// Note that we write these as a single 32-bit write, not two 16-bit writes, so the values
// need to be arranged as the ARM decoder wants them, with the high-order halfword first
// (in little-endian order).
#define INTERRUPT_INSTR_32              0xa001f7f0 // 0xf7f0a001

#elif defined(TARGET_ARM64)

// The following encodings are undefined. They fall into section C4.5.8 - Data processing (2 source) of
// "Arm Architecture Reference Manual ARMv8"
//
#define INTERRUPT_INSTR                 0xBADC0DE0

#elif defined(TARGET_LOONGARCH64)

// The following encodings are undefined.
#define INTERRUPT_INSTR                 0xffffff0f

#elif defined(TARGET_RISCV64)
// The following encodings are undefined.
#define INTERRUPT_INSTR                 0x20000000  // unimp, fld

#endif // _TARGET_*

// The body of this method is in this header file to allow
// mscordaccore.dll to link without getting an unsat symbol
//
inline bool IsGcCoverageInterruptInstructionVal(UINT32 instrVal)
{
#if defined(TARGET_ARM)

    UINT16 instrVal16 = static_cast<UINT16>(instrVal);
    size_t instrLen = GetARMInstructionLength(instrVal16);

    return (instrLen == 2 && instrVal16 == INTERRUPT_INSTR) ||
        (instrLen == 4 && instrVal == INTERRUPT_INSTR_32);

#elif defined(TARGET_X86)

    switch (instrVal)
    {
    case INTERRUPT_INSTR:
    case INTERRUPT_INSTR_CALL:
    case INTERRUPT_INSTR_PROTECT_RET:
    case INTERRUPT_INSTR_PROTECT_CONT:
    case INTERRUPT_INSTR_PROTECT_CONT_AND_RET:
        return true;
    default:
        return false;
    }
#else

     return instrVal == INTERRUPT_INSTR;
#endif  // _TARGET_XXXX_
}

bool IsGcCoverageInterruptInstruction(PBYTE instrPtr);
bool IsGcCoverageInterrupt(LPVOID ip);

#endif // HAVE_GCCOVER

#endif // !__GCCOVER_H__
