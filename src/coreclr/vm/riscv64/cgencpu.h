// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// #error "TODO-RISCV64: missing implementation"

#ifndef TARGET_RISCV64
#error Should only include "cGenCpu.h" for RISCV64 builds
#endif

#ifndef __cgencpu_h__
#define __cgencpu_h__

#define INSTRFMT_K64
#include <stublink.h>

#ifndef TARGET_UNIX
#define USE_REDIRECT_FOR_GCSTRESS
#endif // TARGET_UNIX

EXTERN_C void getFPReturn(int fpSize, INT64 *pRetVal);
EXTERN_C void setFPReturn(int fpSize, INT64 retVal);


class ComCallMethodDesc;

extern PCODE GetPreStubEntryPoint();

#define COMMETHOD_PREPAD                        24   // # extra bytes to allocate in addition to sizeof(ComCallMethodDesc)
#ifdef FEATURE_COMINTEROP
#define COMMETHOD_CALL_PRESTUB_SIZE             24
#define COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET   16   // the offset of the call target address inside the prestub
#endif // FEATURE_COMINTEROP

#define STACK_ALIGN_SIZE                        16

#define JUMP_ALLOCATE_SIZE                      16  // # bytes to allocate for a jump instruction
#define BACK_TO_BACK_JUMP_ALLOCATE_SIZE         16  // # bytes to allocate for a back to back jump instruction

#define HAS_NDIRECT_IMPORT_PRECODE              1

#define USE_INDIRECT_CODEHEADER

#define HAS_FIXUP_PRECODE                       1

// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define CODE_SIZE_ALIGN                         8
#define CACHE_LINE_SIZE                         64
#define LOG2SLOT                                LOG2_PTRSIZE

#define ENREGISTERED_RETURNTYPE_MAXSIZE         16  // bytes (two FP registers: f10 and f11
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE 16  // bytes (two int registers: a0 and a1)
#define ENREGISTERED_PARAMTYPE_MAXSIZE          16  // bytes (max value type size that can be passed by value)

#define CALLDESCR_ARGREGS                       1   // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                     1   // CallDescrWorker has FloatArgumentRegisters parameter
// #define CALLDESCR_RETBUFFARGREG                 1   // CallDescrWorker has RetBuffArg parameter that's separate from arg regs // TODO RISCV64

#define FLOAT_REGISTER_SIZE 16 // each register in FloatArgumentRegisters is 16 bytes.

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to arrive at the callsite.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 4

//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType, bool isFloatHfa)
{
    const unsigned stackSlotSize = 8;
    return ALIGN_UP(parmSize, stackSlotSize);
}

//
// JIT HELPERS.
//
// Create alias for optimized implementations of helpers provided on this platform
//
#define JIT_GetSharedGCStaticBase           JIT_GetSharedGCStaticBase_SingleAppDomain
#define JIT_GetSharedNonGCStaticBase        JIT_GetSharedNonGCStaticBase_SingleAppDomain
#define JIT_GetSharedGCStaticBaseNoCtor     JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain
#define JIT_GetSharedNonGCStaticBaseNoCtor  JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain

//**********************************************************************
// Frames
//**********************************************************************

//--------------------------------------------------------------------
// This represents the callee saved (non-volatile) integer registers saved as
// of a FramedMethodFrame.
//--------------------------------------------------------------------
typedef DPTR(struct CalleeSavedRegisters) PTR_CalleeSavedRegisters;
struct CalleeSavedRegisters {
    INT64 sp; // stack pointer
    INT64 fp; // frame pointer
    INT64 s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11;
};

//--------------------------------------------------------------------
// This represents the arguments that are stored in volatile integer registers.
// This should not overlap the CalleeSavedRegisters since those are already
// saved separately and it would be wasteful to save the same register twice.
// If we do use a non-volatile register as an argument, then the ArgIterator
// will probably have to communicate this back to the PromoteCallerStack
// routine to avoid a double promotion.
//--------------------------------------------------------------------
#define NUM_ARGUMENT_REGISTERS 8
typedef DPTR(struct ArgumentRegisters) PTR_ArgumentRegisters;
struct ArgumentRegisters {
    INT64 a[8]; // a0 ....a7
};

#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)


//--------------------------------------------------------------------
// This represents the floating point argument registers which are saved
// as part of the NegInfo for a FramedMethodFrame. Note that these
// might not be saved by all stubs: typically only those that call into
// C++ helpers will need to preserve the values in these volatile
// registers.
//--------------------------------------------------------------------
#define NUM_FLOAT_ARGUMENT_REGISTERS 8
typedef DPTR(struct FloatArgumentRegisters) PTR_FloatArgumentRegisters;
struct FloatArgumentRegisters {
    //TODO: not supports RISCV64-SIMD.
    double  f[8];  // f0-f7
};


//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return context->Pc;
}

inline void SetIP(T_CONTEXT *context, PCODE ip) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Pc = ip;
}

inline TADDR GetSP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return TADDR(context->Sp);
}

inline TADDR GetRA(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return context->Ra;
}

inline void SetRA( T_CONTEXT * context, TADDR ip) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Ra = ip;
}

inline TADDR GetReg(T_CONTEXT * context, int Regnum)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(Regnum >= 0 && Regnum < 32 );
     return (TADDR)(&context->R0 + Regnum);
}

inline void SetReg(T_CONTEXT * context, int Regnum, PCODE RegContent)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(Regnum >= 0 && Regnum <=28 );
    *(&context->R0 + Regnum) = RegContent;
}

extern "C" LPVOID __stdcall GetCurrentSP();

inline void SetSP(T_CONTEXT *context, TADDR sp) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Sp = DWORD64(sp);
}

inline void SetFP(T_CONTEXT *context, TADDR fp) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Fp = DWORD64(fp);
}

inline TADDR GetFP(const T_CONTEXT * context)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (TADDR)(context->Fp);
}


inline TADDR GetMem(PCODE address, SIZE_T size, bool signExtend)
{
    TADDR mem;
    LIMITED_METHOD_DAC_CONTRACT;
    EX_TRY
    {
        switch (size)
        {
        case 4:
            if (signExtend)
                mem = *(int32_t*)address;
            else
                mem = *(uint32_t*)address;
            break;
        case 8:
            mem = *(uint64_t*)address;
            break;
        default:
            UNREACHABLE();
        }
    }
    EX_CATCH
    {
        mem = NULL;
        _ASSERTE(!"Memory read within jitted Code Failed, this should not happen!!!!");
    }
    EX_END_CATCH(SwallowAllExceptions);
    return mem;
}

#ifdef FEATURE_COMINTEROP
void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target);
#endif // FEATURE_COMINTEROP

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode, bool hasCodeExecutedBefore = false)
{
    return FlushInstructionCache(GetCurrentProcess(), pCodeAddr, sizeOfCode);
}

//------------------------------------------------------------------------
inline void emitJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;
    UINT32* pCode = (UINT32*)pBufferRW;

    _ASSERTE(!"TODO RISCV64 NYI");
}

//------------------------------------------------------------------------
//  Given the same pBuffer that was used by emitJump this method
//  decodes the instructions and returns the jump target
inline PCODE decodeJump(PCODE pCode)
{
    LIMITED_METHOD_CONTRACT;

    TADDR pInstr = PCODEToPINSTR(pCode);

    return *dac_cast<PTR_PCODE>(pInstr + 2*sizeof(DWORD));
}

//------------------------------------------------------------------------
inline BOOL isJump(PCODE pCode)
{
    LIMITED_METHOD_DAC_CONTRACT;

    TADDR pInstr = PCODEToPINSTR(pCode);

    return *dac_cast<PTR_DWORD>(pInstr) == 0x58000050;
}

//------------------------------------------------------------------------
inline BOOL isBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return isJump(pBuffer);
}

//------------------------------------------------------------------------
inline void emitBackToBackJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    WRAPPER_NO_CONTRACT;
    emitJump(pBufferRX, pBufferRW, target);
}

//------------------------------------------------------------------------
inline PCODE decodeBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    return decodeJump(pBuffer);
}

//----------------------------------------------------------------------

struct IntReg
{
    int reg;
    IntReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 32);
    }

    operator int () { return reg; }
    operator int () const { return reg; }
    int operator == (IntReg other) { return reg == other.reg; }
    int operator != (IntReg other) { return reg != other.reg; }
    WORD Mask() const { return 1 << reg; }
};

struct VecReg
{
    int reg;
    VecReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 32);
    }

    operator int() { return reg; }
    int operator == (VecReg other) { return reg == other.reg; }
    int operator != (VecReg other) { return reg != other.reg; }
    WORD Mask() const { return 1 << reg; }
};

struct CondCode
{
    int cond;
    CondCode(int cond):cond(cond)
    {
        _ASSERTE(0 <= cond && cond < 16);
    }
};

const IntReg RegSp  = IntReg(2);
const IntReg RegFp  = IntReg(8);
const IntReg RegRa  = IntReg(1);

#define GetEEFuncEntryPoint(pfn) GFN_TADDR(pfn)

#if 1 // TODO RISCV64
class StubLinkerCPU : public StubLinker
{

private:
    void EmitLoadStoreRegPairImm(DWORD flags, int regNum1, int regNum2, IntReg Xn, int offset, BOOL isVec);
    void EmitLoadStoreRegImm(DWORD flags, int regNum, IntReg Xn, int offset, BOOL isVec, int log2Size = 3);
public:

#if 0
    // BitFlags for EmitLoadStoreReg(Pair)Imm methods
    enum {
        eSTORE      =   0x0,
        eLOAD       =   0x1,
        eWRITEBACK  =   0x2,
        ePOSTINDEX  =   0x4,
        eFLAGMASK   =   0x7
    };

    // BitFlags for Register offsetted loads/stores
    // Bits(1-3) indicate the <extend> encoding, while the bits(0) indicate the shift
    enum {
        eSHIFT      =   0x1, // 0y0001
        eUXTW       =   0x4, // 0y0100
        eSXTW       =   0xC, // 0y1100
        eLSL        =   0x7, // 0y0111
        eSXTX       =   0xD, // 0y1110
    };
#endif


    static void Init();

    void EmitCallManagedMethod(MethodDesc *pMD, BOOL fTailCall);
    void EmitCallLabel(CodeLabel *target, BOOL fTailCall, BOOL fIndirect);

    void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);

#if defined(FEATURE_SHARE_GENERIC_CODE)
    void EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
#endif // FEATURE_SHARE_GENERIC_CODE

#ifdef _DEBUG
    void EmitNop() { _ASSERTE(!"RISCV64: not implementation on riscv64!!!"); }
#endif
    void EmitBreakPoint() { _ASSERTE(!"RISCV64: not implementation on riscv64!!!"); }
    void EmitMovConstant(IntReg target, UINT64 constant);
    void EmitCmpImm(IntReg reg, int imm);
    void EmitCmpReg(IntReg Xn, IntReg Xm);
    void EmitCondFlagJump(CodeLabel * target, UINT cond);
    void EmitJumpRegister(IntReg regTarget);
    void EmitMovReg(IntReg dest, IntReg source);

    void EmitSubImm(IntReg Xd, IntReg Xn, unsigned int value);
    void EmitAddImm(IntReg Xd, IntReg Xn, unsigned int value);

    void EmitLoadStoreRegPairImm(DWORD flags, IntReg Xt1, IntReg Xt2, IntReg Xn, int offset=0);
    void EmitLoadStoreRegPairImm(DWORD flags, VecReg Vt1, VecReg Vt2, IntReg Xn, int offset=0);

    void EmitLoadStoreRegImm(DWORD flags, IntReg Xt, IntReg Xn, int offset=0, int log2Size = 3);
    void EmitLoadStoreRegImm(DWORD flags, VecReg Vt, IntReg Xn, int offset=0);

    void EmitLoadRegReg(IntReg Xt, IntReg Xn, IntReg Xm, DWORD option);

    void EmitCallRegister(IntReg reg);
    void EmitProlog(unsigned short cIntRegArgs,
                    unsigned short cVecRegArgs,
                    unsigned short cCalleeSavedRegs,
                    unsigned short cbStackSpace = 0);

    void EmitEpilog();

    void EmitRet(IntReg reg);


};
#endif

extern "C" void SinglecastDelegateInvokeStub();


// preferred alignment for data
#define DATA_ALIGNMENT 8

// TODO RISCV64
struct DECLSPEC_ALIGN(16) UMEntryThunkCode
{
    DWORD        m_code[4];

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
        DWORD64 Ra;
        size_t ReturnAddress;
    };
    union
    {
        struct {
             DWORD64 A0;
             DWORD64 A1;
         };
        size_t ReturnValue[2];
    };
    DWORD64 Fp; // frame pointer
    DWORD64 Tp, S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11;
 };

// Precode to shuffle this and retbuf for closed delegates over static methods with return buffer
struct ThisPtrRetBufPrecode {

    static const int Type = 0x53;

    UINT32  m_rgCode[6];
    TADDR   m_pTarget;
    TADDR   m_pMethodDesc;

    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

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

#endif // __cgencpu_h__
