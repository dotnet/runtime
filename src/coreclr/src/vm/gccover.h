// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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
    MethodDesc*   lastMD;           // Used to quickly figure out the culprite

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

    // Sloppy bitsets (will wrap, and not threadsafe) but best effort is OK
    // since we just need half decent coverage.  
    BOOL IsBitSetForOffset(unsigned offset) {
        unsigned dword = hasExecuted[(offset >> 5) % hasExecutedSize];
        return(dword & (1 << (offset & 0x1F)));
    }

    void SetBitForOffset(unsigned offset) {
        unsigned* dword = &hasExecuted[(offset >> 5) % hasExecutedSize];
        *dword |= (1 << (offset & 0x1F)) ;
    }

    void SprinkleBreakpoints(BYTE * saveAddr, PCODE codeStart, size_t codeSize, size_t regionOffsetAdj, BOOL fZapped);

};

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER


#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

#define INTERRUPT_INSTR                        0xF4    // X86 HLT instruction (any 1 byte illegal instruction will do)
#define INTERRUPT_INSTR_CALL                   0xFA    // X86 CLI instruction 
#define INTERRUPT_INSTR_PROTECT_FIRST_RET      0xFB    // X86 STI instruction, protect the first return register
#define INTERRUPT_INSTR_PROTECT_SECOND_RET     0xEC    // X86 IN instruction, protect the second return register
#define INTERRUPT_INSTR_PROTECT_BOTH_RET       0xED    // X86 IN instruction, protect both return registers

#elif defined(_TARGET_ARM_)

// 16-bit illegal instructions which will cause exception and cause 
// control to go to GcStress codepath
#define INTERRUPT_INSTR                 0xde00             
#define INTERRUPT_INSTR_CALL            0xde03  // 0xde01 generates SIGTRAP (breakpoint) instead of SIGILL on Unix             
#define INTERRUPT_INSTR_PROTECT_RET     0xde02      

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
#define INTERRUPT_INSTR_CALL_32         0xa002f7f0 // 0xf7f0a002
#define INTERRUPT_INSTR_PROTECT_RET_32  0xa003f7f0 // 0xf7f0a003

#elif defined(_TARGET_ARM64_)

// The following encodings are undefined. They fall into section C4.5.8 - Data processing (2 source) of 
// "Arm Architecture Reference Manual ARMv8"
//
#define INTERRUPT_INSTR                 0xBADC0DE0
#define INTERRUPT_INSTR_CALL            0xBADC0DE1         
#define INTERRUPT_INSTR_PROTECT_RET     0xBADC0DE2  

#endif // _TARGET_*

#endif // HAVE_GCCOVER

#endif // !__GCCOVER_H__
