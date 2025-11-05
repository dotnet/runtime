// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// CGENCPU.H -
//
// Various helper routines for generating S390X assembly code.
//
// DO NOT INCLUDE THIS FILE DIRECTLY - ALWAYS USE CGENSYS.H INSTEAD
//

#ifndef TARGET_S390X
#error Should only include "S390X\cgencpu.h" for S390X builds
#endif

#ifndef __cgencpu_h__
#define __cgencpu_h__

#include "stublinkers390x.h"

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 1

// preferred alignment for data
#define DATA_ALIGNMENT 8

class MethodDesc;

//
// functions implemented in S390X assembly
//
EXTERN_C void SinglecastDelegateInvokeStub();

#define STACK_ALIGN_SIZE                        8

#define JUMP_ALLOCATE_SIZE                      16   // # bytes to allocate for a 64-bit jump instruction
#define BACK_TO_BACK_JUMP_ALLOCATE_SIZE         16   // # bytes to allocate for a back to back 64-bit jump instruction

// Also in CorCompile.h, FnTableAccess.h
#define USE_INDIRECT_CODEHEADER                 // use CodeHeader, RealCodeHeader construct

#define HAS_NDIRECT_IMPORT_PRECODE              1
#define HAS_FIXUP_PRECODE                       1

// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define CODE_SIZE_ALIGN                         16   // must alloc code blocks on 2-byte boundaries; for perf reasons we use 16 byte boundaries
#define CACHE_LINE_SIZE                         256  // IBM Z processors have 256-byte cache lines
#define LOG2SLOT                                LOG2_PTRSIZE


// #undef ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE   // All structures are returned via implicit pointer
// #undef ENREGISTERED_RETURNTYPE_MAXSIZE           // All structures are returned via implicit pointer
#define ENREGISTERED_PARAMTYPE_MAXSIZE          8   // bytes
#define CALLDESCR_ARGREGS                       1   // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                     1   // CallDescrWorker has FloatArgumentRegisters parameter

#define FLOAT_REGISTER_SIZE 8 // each register in FloatArgumentRegisters is 8 bytes.

#define GetEEFuncEntryPoint(pfn) GFN_TADDR(pfn)


//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType = false /* unused */, bool isFloatHfa = false /* unused */)
{
    const unsigned stackSlotSize = 8;
    return ALIGN_UP(parmSize, stackSlotSize);
}

//**********************************************************************
// Frames
//**********************************************************************
//--------------------------------------------------------------------
// This represents some of the TransitionFrame fields that are
// stored at negative offsets.
//--------------------------------------------------------------------
struct REGDISPLAY;

//--------------------------------------------------------------------
// This represents the arguments that are stored in volatile registers.
// This should not overlap the CalleeSavedRegisters since those are already
// saved separately and it would be wasteful to save the same register twice.
// If we do use a non-volatile register as an argument, then the ArgIterator
// will probably have to communicate this back to the PromoteCallerStack
// routine to avoid a double promotion.
//--------------------------------------------------------------------
#define ENUM_ARGUMENT_REGISTERS() \
    ARGUMENT_REGISTER(R2) \
    ARGUMENT_REGISTER(R3) \
    ARGUMENT_REGISTER(R4) \
    ARGUMENT_REGISTER(R5)

// NOTE: R6 is a non-volatile argument register!
#define NUM_ARGUMENT_REGISTERS 5

// The order of registers in this macro is hardcoded in assembly code
// at number of places
#define ENUM_CALLEE_SAVED_REGISTERS() \
    CALLEE_SAVED_REGISTER(R6) \
    CALLEE_SAVED_REGISTER(R7) \
    CALLEE_SAVED_REGISTER(R8) \
    CALLEE_SAVED_REGISTER(R9) \
    CALLEE_SAVED_REGISTER(R10) \
    CALLEE_SAVED_REGISTER(R11) \
    CALLEE_SAVED_REGISTER(R12) \
    CALLEE_SAVED_REGISTER(R13)

#define NUM_CALLEE_SAVED_REGISTERS 8

#define ENUM_FP_CALLEE_SAVED_REGISTERS() \
    CALLEE_SAVED_REGISTER(F8) \
    CALLEE_SAVED_REGISTER(F9) \
    CALLEE_SAVED_REGISTER(F10) \
    CALLEE_SAVED_REGISTER(F11) \
    CALLEE_SAVED_REGISTER(F12) \
    CALLEE_SAVED_REGISTER(F13) \
    CALLEE_SAVED_REGISTER(F14) \
    CALLEE_SAVED_REGISTER(F15)

typedef DPTR(struct ArgumentRegisters) PTR_ArgumentRegisters;
struct ArgumentRegisters {
    #define ARGUMENT_REGISTER(regname) INT_PTR regname;
    ENUM_ARGUMENT_REGISTERS();
    #undef ARGUMENT_REGISTER
};

typedef DPTR(struct CalleeSavedRegisters) PTR_CalleeSavedRegisters;
struct CalleeSavedRegisters {
    #define CALLEE_SAVED_REGISTER(regname) INT_PTR regname;
    ENUM_CALLEE_SAVED_REGISTERS();
    #undef CALLEE_SAVED_REGISTER
};

struct CalleeSavedRegistersPointers {
    #define CALLEE_SAVED_REGISTER(regname) PTR_TADDR p##regname;
    ENUM_CALLEE_SAVED_REGISTERS();
    #undef CALLEE_SAVED_REGISTER
};

#define THIS_REG R2
#define THIS_kREG 2

#define ARGUMENT_kREG1 2
#define ARGUMENT_kREG2 3

#define NUM_FLOAT_ARGUMENT_REGISTERS 4

typedef DPTR(struct FloatArgumentRegisters) PTR_FloatArgumentRegisters;
struct FloatArgumentRegisters {
     DWORD64 d[NUM_FLOAT_ARGUMENT_REGISTERS];   // f0, f2, f4, f6
};


void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pRegs);


// Sufficient context for Try/Catch restoration.
struct EHContext {
    // Not used
};

#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)




//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const CONTEXT * context)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    return PCODE(context->PSWAddr);
}

inline void SetIP(CONTEXT* context, PCODE rip)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    context->PSWAddr = (DWORD64) rip;
}

inline TADDR GetSP(const CONTEXT * context)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    return (TADDR)context->R15;
}
inline void SetSP(CONTEXT *context, TADDR rsp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    context->R15 = rsp;
}

#define SetFP(context, ebp)
inline TADDR GetFP(const CONTEXT * context)
{
    LIMITED_METHOD_CONTRACT;

    return (TADDR)(context->R11);
}

extern "C" TADDR GetCurrentSP();

extern "C" void InterpreterStubThunk();


//**********************************************************************
// Jump thunks
//**********************************************************************

inline void emitJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;
    UINT16* pCode = (UINT16*)pBufferRW;

    pCode[0] = 0xc418;  // lgrl %r1, <target>
    pCode[1] = 0x0000;
    pCode[2] = 0x0004;
    pCode[3] = 0x07f1;  // br %r1

    *((LPVOID *)(pCode + 4)) = target;   // 64-bit target address
}

inline PCODE decodeJump(PCODE pCode)
{
    LIMITED_METHOD_CONTRACT;

    TADDR pInstr = PCODEToPINSTR(pCode);

    return *dac_cast<PTR_PCODE>(pInstr + 4 * sizeof(UINT16));
}

inline void emitBackToBackJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    WRAPPER_NO_CONTRACT;
    emitJump(pBufferRX, pBufferRW, target);
}

inline PCODE decodeBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    return decodeJump(pBuffer);
}


struct DECLSPEC_ALIGN(8) UMEntryThunkCode
{
    UINT16      m_code[8];
    TADDR       m_pTargetCode;
    TADDR       m_pvSecretParam;

    void Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam);
    void Poison();

    LPCBYTE GetEntryPoint() const
    {
        LIMITED_METHOD_CONTRACT;

        return (LPCBYTE)this;
    }

    static int GetEntryPointOffset()
    {
        LIMITED_METHOD_CONTRACT;

        return 0;
    }
};

struct HijackArgs
{
    union
    {
        ULONG64 R2;
        ULONG64 ReturnValue[1];
    };
    CalleeSavedRegisters Regs;
    union
    {
        ULONG64 Rip;
        size_t ReturnAddress;
    };
};

extern PCODE GetPreStubEntryPoint();

// Precode to shuffle this and retbuf for closed delegates over static methods with return buffer
struct ThisPtrRetBufPrecode {

    static const int Type = 0x04;

    UINT16  m_rgCode[12];
    TADDR   m_pTarget;
    TADDR   m_pMethodDesc;

    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
    {
        WRAPPER_NO_CONTRACT;

        int n = 0;
        // Initially
        // %r2 - This ptr
        // %r3 - ReturnBuffer
        m_rgCode[n++] = 0xb904;  // lgr %r1, %r2
        m_rgCode[n++] = 0x0012;
        m_rgCode[n++] = 0xb904;  // lgr %r2, %r3
        m_rgCode[n++] = 0x0023;
        m_rgCode[n++] = 0xb904;  // lgr %r3, %r1
        m_rgCode[n++] = 0x0031;
        _ASSERTE((UINT16*)&m_pTarget == &m_rgCode[n + 6]);
        m_rgCode[n++] = 0xc418;  // lgrl %r1, <target>
        m_rgCode[n++] = 0x0000;
        m_rgCode[n++] = 0x0006;
        m_rgCode[n++] = 0x07f1;  // br %r1
        m_rgCode[n++] = 0x0707;  // nop for data alignment below
        m_rgCode[n++] = 0x0707;  // nop for data alignment below
        _ASSERTE(n == ARRAY_SIZE(m_rgCode));

        m_pTarget = GetPreStubEntryPoint();
        m_pMethodDesc = (TADDR)pMD;
    }

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pMethodDesc;
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pTarget;
    }

#ifndef DACCESS_COMPILE
    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        ExecutableWriterHolder<ThisPtrRetBufPrecode> precodeWriterHolder(this, sizeof(ThisPtrRetBufPrecode));
        return (TADDR)InterlockedCompareExchange64(
            (LONGLONG*)&precodeWriterHolder.GetRW()->m_pTarget, (TADDR)target, (TADDR)expected) == expected;
    }
#endif // !DACCESS_COMPILE
};
typedef DPTR(ThisPtrRetBufPrecode) PTR_ThisPtrRetBufPrecode;


// ClrFlushInstructionCache is used when we want to call FlushInstructionCache
// for a specific architecture in the common code, but not for other architectures.
// We call ClrFlushInstructionCache whenever we create or modify code in the heap.
// Currently ClrFlushInstructionCache has no effect on S390X
//

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode, bool hasCodeExecutedBefore = false)
{
//    if (hasCodeExecutedBefore)
//    {
//        FlushInstructionCache(GetCurrentProcess(), pCodeAddr, sizeOfCode);
//    }
//    else
//    {
//        MemoryBarrier();
//    }
    return TRUE;
}

#endif // __cgencpu_h__
