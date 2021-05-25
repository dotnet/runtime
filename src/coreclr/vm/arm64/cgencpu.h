// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


#ifndef TARGET_ARM64
#error Should only include "cGenCpu.h" for ARM64 builds
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
#define HAS_FIXUP_PRECODE_CHUNKS                1

// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define CODE_SIZE_ALIGN                         8
#define CACHE_LINE_SIZE                         64
#define LOG2SLOT                                LOG2_PTRSIZE

#define ENREGISTERED_RETURNTYPE_MAXSIZE         64  // bytes (four vector registers: q0,q1,q2 and q3)
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE 16  // bytes (two int registers: x0 and x1)
#define ENREGISTERED_PARAMTYPE_MAXSIZE          16  // bytes (max value type size that can be passed by value)

#define CALLDESCR_ARGREGS                       1   // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                     1   // CallDescrWorker has FloatArgumentRegisters parameter
#define CALLDESCR_RETBUFFARGREG                 1   // CallDescrWorker has RetBuffArg parameter that's separate from arg regs

#define FLOAT_REGISTER_SIZE 16 // each register in FloatArgumentRegisters is 16 bytes.

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to arrive at the callsite.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 4

inline
ARG_SLOT FPSpillToR8(void* pSpillSlot)
{
    LIMITED_METHOD_CONTRACT;
    return *(SIZE_T*)pSpillSlot;
}

inline
void     R8ToFPSpill(void* pSpillSlot, SIZE_T  srcDoubleAsSIZE_T)
{
    LIMITED_METHOD_CONTRACT;
    *(SIZE_T*)pSpillSlot = srcDoubleAsSIZE_T;
    *((SIZE_T*)pSpillSlot + 1) = 0;
}

//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType, bool isFloatHfa)
{
#if defined(OSX_ARM64_ABI)
    if (!isValueType)
    {
        // The primitive types' sizes are expected to be powers of 2.
        _ASSERTE((parmSize & (parmSize - 1)) == 0);
        // No padding/alignment for primitive types.
        return parmSize;
    }
    if (isFloatHfa)
    {
        _ASSERTE((parmSize % 4) == 0);
        // float hfa is not considered a struct type and passed with 4-byte alignment.
        return parmSize;
    }
#endif

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
    INT64 x29; // frame pointer
    INT64 x30; // link register
    INT64 x19, x20, x21, x22, x23, x24, x25, x26, x27, x28;
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
    INT64 x[NUM_ARGUMENT_REGISTERS]; // x0 ....x7. Note that x8 (return buffer address) is not included.
};

#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)


//--------------------------------------------------------------------
// This represents the floating point argument registers which are saved
// as part of the NegInfo for a FramedMethodFrame. Note that these
// might not be saved by all stubs: typically only those that call into
// C++ helpers will need to preserve the values in these volatile
// registers.
//--------------------------------------------------------------------
typedef DPTR(struct FloatArgumentRegisters) PTR_FloatArgumentRegisters;
struct FloatArgumentRegisters {
    // armV8 supports 32 floating point registers. Each register is 128bits long.
    // It can be accessed as 128-bit value or 64-bit value(d0-d31) or as 32-bit value (s0-s31)
    // or as 16-bit value or as 8-bit values.
    // Although C# only has two builtin floating datatypes float(32-bit) and double(64-bit),
    // HW Intrinsics support using the full 128-bit value for passing Vectors.
    NEON128   q[8];  // q0-q7
};

#define NUM_FLOAT_ARGUMENT_REGISTERS 8

//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return context->Pc;
}

inline void SetIP(T_CONTEXT *context, PCODE eip) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Pc = eip;
}

inline TADDR GetSP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return TADDR(context->Sp);
}

inline TADDR GetLR(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return context->Lr;
}

inline void SetLR( T_CONTEXT * context, TADDR eip) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Lr = eip;
}

inline TADDR GetReg(T_CONTEXT * context, int Regnum)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(Regnum >= 0 && Regnum < 32 );
     return context->X[Regnum];
}

inline void SetReg(T_CONTEXT * context,  int Regnum, PCODE RegContent)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(Regnum >= 0 && Regnum <=28 );
    context->X[Regnum] = RegContent;
}
inline void SetSimdReg(T_CONTEXT * context, int Regnum, NEON128 RegContent)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(Regnum >= 0 && Regnum <= 28);
    context->V[Regnum] = RegContent;
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
            mem = *(uint32_t*)address;
            if (signExtend && (mem & 0x80000000))
                mem |= 0xffffffff00000000;
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
        _ASSERTE(!"Memory read within jitted Code Failed, this should not happen!!!!");
    }
    EX_END_CATCH(SwallowAllExceptions);
    return mem;
}

inline NEON128 GetSimdMem(PCODE ip)
{
    NEON128 mem;
    LIMITED_METHOD_DAC_CONTRACT;

    mem.Low = GetMem(ip, 8, false);
    mem.High = GetMem(ip + 1, 8, false);

    return mem;
}

#ifdef FEATURE_COMINTEROP
void emitCOMStubCall (ComCallMethodDesc *pCOMMethod, PCODE target);
#endif // FEATURE_COMINTEROP

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode)
{
#ifdef CROSSGEN_COMPILE
    // The code won't be executed when we are cross-compiling so flush instruction cache is unnecessary
    return TRUE;
#else
    return FlushInstructionCache(GetCurrentProcess(), pCodeAddr, sizeOfCode);
#endif
}

//------------------------------------------------------------------------
inline void emitJump(LPBYTE pBuffer, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;
    UINT32* pCode = (UINT32*)pBuffer;

    // We require 8-byte alignment so the LDR instruction is aligned properly
    _ASSERTE(((UINT_PTR)pCode & 7) == 0);

    // +0:   ldr x16, [pc, #8]
    // +4:   br  x16
    // +8:   [target address]

    pCode[0] = 0x58000050UL;   // ldr x16, [pc, #8]
    pCode[1] = 0xD61F0200UL;   // br  x16

    // Ensure that the updated instructions get updated in the I-Cache
    ClrFlushInstructionCache(pCode, 8);

    *((LPVOID *)(pCode + 2)) = target;   // 64-bit target address

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
inline void emitBackToBackJump(LPBYTE pBuffer, LPVOID target)
{
    WRAPPER_NO_CONTRACT;
    emitJump(pBuffer, target);
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

const IntReg RegTeb = IntReg(18);
const IntReg RegFp  = IntReg(29);
const IntReg RegLr  = IntReg(30);
// Note that stack pointer and zero register share the same encoding, 31
const IntReg RegSp  = IntReg(31);

const CondCode CondEq = CondCode(0);
const CondCode CondNe = CondCode(1);
const CondCode CondCs = CondCode(2);
const CondCode CondCc = CondCode(3);
const CondCode CondMi = CondCode(4);
const CondCode CondPl = CondCode(5);
const CondCode CondVs = CondCode(6);
const CondCode CondVc = CondCode(7);
const CondCode CondHi = CondCode(8);
const CondCode CondLs = CondCode(9);
const CondCode CondGe = CondCode(10);
const CondCode CondLt = CondCode(11);
const CondCode CondGt = CondCode(12);
const CondCode CondLe = CondCode(13);
const CondCode CondAl = CondCode(14);
const CondCode CondNv = CondCode(15);


#define PRECODE_ALIGNMENT           CODE_SIZE_ALIGN
#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0

#ifdef CROSSGEN_COMPILE
#define GetEEFuncEntryPoint(pfn) 0x1001
#else
#define GetEEFuncEntryPoint(pfn) GFN_TADDR(pfn)
#endif

class StubLinkerCPU : public StubLinker
{

private:
    void EmitLoadStoreRegPairImm(DWORD flags, int regNum1, int regNum2, IntReg Xn, int offset, BOOL isVec);
    void EmitLoadStoreRegImm(DWORD flags, int regNum, IntReg Xn, int offset, BOOL isVec, int log2Size = 3);
public:

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


    static void Init();

    void EmitCallManagedMethod(MethodDesc *pMD, BOOL fTailCall);
    void EmitCallLabel(CodeLabel *target, BOOL fTailCall, BOOL fIndirect);

    void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);

#if defined(FEATURE_SHARE_GENERIC_CODE)
    void EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
#endif // FEATURE_SHARE_GENERIC_CODE

#ifdef _DEBUG
    void EmitNop() { Emit32(0xD503201F); }
#endif
    void EmitBreakPoint() { Emit32(0xD43E0000); }
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

extern "C" void SinglecastDelegateInvokeStub();


// preferred alignment for data
#define DATA_ALIGNMENT 8

struct DECLSPEC_ALIGN(16) UMEntryThunkCode
{
    DWORD        m_code[4];

    TADDR       m_pTargetCode;
    TADDR       m_pvSecretParam;

    void Encode(BYTE* pTargetCode, void* pvSecretParam);
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
    DWORD64 X29; // frame pointer
    union
    {
        DWORD64 Lr;
        size_t ReturnAddress;
    };
    DWORD64 X19, X20, X21, X22, X23, X24, X25, X26, X27, X28;
    union
    {
        struct {
             DWORD64 X0;
             DWORD64 X1;
         };
        size_t ReturnValue[2];
    };
    union
    {
        struct {
             NEON128 Q0;
             NEON128 Q1;
             NEON128 Q2;
             NEON128 Q3;
         };
        NEON128 FPReturnValue[4];
    };
};

EXTERN_C VOID STDCALL PrecodeFixupThunk();

// Invalid precode type
struct InvalidPrecode {
    static const int Type = 0;
};

struct StubPrecode {

    static const int Type = 0x89;

    // adr x9, #16
    // ldp x10,x12,[x9]      ; =m_pTarget,m_pMethodDesc
    // br x10
    // 4 byte padding for 8 byte allignement
    // dcd pTarget
    // dcd pMethodDesc
    DWORD   m_rgCode[4];
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

    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        InterlockedExchange64((LONGLONG*)&m_pTarget, (TADDR)GetPreStubEntryPoint());
    }

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        return (TADDR)InterlockedCompareExchange64(
            (LONGLONG*)&m_pTarget, (TADDR)target, (TADDR)expected) == expected;
    }

#ifdef FEATURE_PREJIT
    void Fixup(DataImage *image);
#endif
};
typedef DPTR(StubPrecode) PTR_StubPrecode;


struct NDirectImportPrecode {

    static const int Type = 0x8B;

    // adr x11, #16             ; Notice that x11 register is used to differentiate the stub from StubPrecode which uses x9
    // ldp x10,x12,[x11]      ; =m_pTarget,m_pMethodDesc
    // br x10
    // 4 byte padding for 8 byte allignement
    // dcd pTarget
    // dcd pMethodDesc
    DWORD    m_rgCode[4];
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

    LPVOID GetEntrypoint()
    {
        LIMITED_METHOD_CONTRACT;
        return this;
    }

#ifdef FEATURE_PREJIT
    void Fixup(DataImage *image);
#endif
};
typedef DPTR(NDirectImportPrecode) PTR_NDirectImportPrecode;


struct FixupPrecode {

    static const int Type = 0x0C;

    // adr x12, #0
    // ldr x11, [pc, #12]     ; =m_pTarget
    // br  x11
    // dcb m_MethodDescChunkIndex
    // dcb m_PrecodeChunkIndex
    // 2 byte padding
    // dcd m_pTarget


    UINT32  m_rgCode[3];
    BYTE    padding[2];
    BYTE    m_MethodDescChunkIndex;
    BYTE    m_PrecodeChunkIndex;
    TADDR   m_pTarget;

    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator, int iMethodDescChunkIndex = 0, int iPrecodeChunkIndex = 0);
    void InitCommon()
    {
        WRAPPER_NO_CONTRACT;
        int n = 0;

        m_rgCode[n++] = 0x1000000C;   // adr x12, #0
        m_rgCode[n++] = 0x5800006B;   // ldr x11, [pc, #12]     ; =m_pTarget

        _ASSERTE((UINT32*)&m_pTarget == &m_rgCode[n + 2]);

        m_rgCode[n++] = 0xD61F0160;   // br  x11

        _ASSERTE(n == _countof(m_rgCode));
    }

    TADDR GetBase()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<TADDR>(this) + (m_PrecodeChunkIndex + 1) * sizeof(FixupPrecode);
    }

    TADDR GetMethodDesc();

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pTarget;
    }

    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        InterlockedExchange64((LONGLONG*)&m_pTarget, (TADDR)GetEEFuncEntryPoint(PrecodeFixupThunk));
    }

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        return (TADDR)InterlockedCompareExchange64(
            (LONGLONG*)&m_pTarget, (TADDR)target, (TADDR)expected) == expected;
    }

    static BOOL IsFixupPrecodeByASM(PCODE addr)
    {
        PTR_DWORD pInstr = dac_cast<PTR_DWORD>(PCODEToPINSTR(addr));
        return
            (pInstr[0] == 0x1000000C) &&
            (pInstr[1] == 0x5800006B) &&
            (pInstr[2] == 0xD61F0160);
    }

#ifdef FEATURE_PREJIT
    // Partial initialization. Used to save regrouped chunks.
    void InitForSave(int iPrecodeChunkIndex);

    void Fixup(DataImage *image, MethodDesc * pMD);
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};
typedef DPTR(FixupPrecode) PTR_FixupPrecode;


// Precode to shuffle this and retbuf for closed delegates over static methods with return buffer
struct ThisPtrRetBufPrecode {

    static const int Type = 0x10;

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

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        return (TADDR)InterlockedCompareExchange64(
            (LONGLONG*)&m_pTarget, (TADDR)target, (TADDR)expected) == expected;
    }
};
typedef DPTR(ThisPtrRetBufPrecode) PTR_ThisPtrRetBufPrecode;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Call counting

#ifdef FEATURE_TIERED_COMPILATION

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

typedef UINT16 CallCount;
typedef DPTR(CallCount) PTR_CallCount;

////////////////////////////////////////////////////////////////
// CallCountingStub

class CallCountingStub;
typedef DPTR(const CallCountingStub) PTR_CallCountingStub;

class CallCountingStub
{
public:
    static const SIZE_T Alignment = sizeof(void *);

#ifndef DACCESS_COMPILE
protected:
    static const PCODE TargetForThresholdReached;

    CallCountingStub() = default;

public:
    static const CallCountingStub *From(TADDR stubIdentifyingToken);

    PCODE GetEntryPoint() const
    {
        WRAPPER_NO_CONTRACT;
        return PINSTRToPCODE((TADDR)this);
    }
#endif // !DACCESS_COMPILE

public:
    PTR_CallCount GetRemainingCallCountCell() const;
    PCODE GetTargetForMethod() const;

    DISABLE_COPY(CallCountingStub);
};

////////////////////////////////////////////////////////////////
// CallCountingStubShort

class CallCountingStubShort;
typedef DPTR(const CallCountingStubShort) PTR_CallCountingStubShort;

#pragma pack(push, 1)
class CallCountingStubShort : public CallCountingStub
{
private:
    const UINT32 m_part0[10];
    CallCount *const m_remainingCallCountCell;
    const PCODE m_targetForMethod;
    const PCODE m_targetForThresholdReached;

#ifndef DACCESS_COMPILE
public:
    CallCountingStubShort(CallCount *remainingCallCountCell, PCODE targetForMethod)
        : m_part0{  0x58000149,             //     ldr  x9, [pc, #(m_remainingCallCountCell)]
                    0x7940012a,             //     ldrh w10, [x9]
                    0x7100054a,             //     subs w10, w10, #1
                    0x7900012a,             //     strh w10, [x9]
                    0x54000060,             //     beq  L0
                    0x580000e9,             //     ldr  x9, [pc, #(m_targetForMethod)]
                    0xd61f0120,             //     br   x9
                    0x10ffff2a,             // L0: adr  x10, #(this)
                                            // (x10 == stub-identifying token == this)
                    0x580000c9,             //     ldr  x9, [pc, #(m_targetForThresholdReached)]
                    0xd61f0120},            //     br   x9
        m_remainingCallCountCell(remainingCallCountCell),
        m_targetForMethod(targetForMethod),
        m_targetForThresholdReached(TargetForThresholdReached)
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(sizeof(CallCountingStubShort) % Alignment == 0);
        _ASSERTE(remainingCallCountCell != nullptr);
        _ASSERTE(PCODEToPINSTR(targetForMethod) != NULL);
    }

    static bool Is(TADDR stubIdentifyingToken)
    {
        WRAPPER_NO_CONTRACT;
        return true;
    }

    static const CallCountingStubShort *From(TADDR stubIdentifyingToken)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(Is(stubIdentifyingToken));

        const CallCountingStubShort *stub = (const CallCountingStubShort *)stubIdentifyingToken;
        _ASSERTE(IS_ALIGNED(stub, Alignment));
        return stub;
    }
#endif // !DACCESS_COMPILE

public:
    static bool Is(PTR_CallCountingStub callCountingStub)
    {
        WRAPPER_NO_CONTRACT;
        return true;
    }

    static PTR_CallCountingStubShort From(PTR_CallCountingStub callCountingStub)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(Is(callCountingStub));

        return dac_cast<PTR_CallCountingStubShort>(callCountingStub);
    }

    PCODE GetTargetForMethod() const
    {
        WRAPPER_NO_CONTRACT;
        return m_targetForMethod;
    }

    friend CallCountingStub;
    DISABLE_COPY(CallCountingStubShort);
};
#pragma pack(pop)

////////////////////////////////////////////////////////////////
// CallCountingStub definitions

#ifndef DACCESS_COMPILE
inline const CallCountingStub *CallCountingStub::From(TADDR stubIdentifyingToken)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(stubIdentifyingToken != NULL);

    return CallCountingStubShort::From(stubIdentifyingToken);
}
#endif

inline PTR_CallCount CallCountingStub::GetRemainingCallCountCell() const
{
    WRAPPER_NO_CONTRACT;
    return PTR_CallCount(dac_cast<PTR_CallCountingStubShort>(this)->m_remainingCallCountCell);
}

inline PCODE CallCountingStub::GetTargetForMethod() const
{
    WRAPPER_NO_CONTRACT;
    return CallCountingStubShort::From(PTR_CallCountingStub(this))->GetTargetForMethod();
}

////////////////////////////////////////////////////////////////

#undef DISABLE_COPY

#endif // FEATURE_TIERED_COMPILATION

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#endif // __cgencpu_h__
