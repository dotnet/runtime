// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// CGENX86.H -
//
// Various helper routines for generating x86 assembly code.
//
// DO NOT INCLUDE THIS FILE DIRECTLY - ALWAYS USE CGENSYS.H INSTEAD
//



#ifndef TARGET_X86
#error Should only include "cgenx86.h" for X86 builds
#endif // TARGET_X86

#ifndef __cgenx86_h__
#define __cgenx86_h__

#include "utilcode.h"

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 1

// preferred alignment for data
#define DATA_ALIGNMENT 4

class MethodDesc;
class FramedMethodFrame;
class Module;
class ComCallMethodDesc;
class BaseDomain;

// CPU-dependent functions
Stub * GenerateInitPInvokeFrameHelper();

#ifdef FEATURE_STUBS_AS_IL
EXTERN_C void SinglecastDelegateInvokeStub();
#endif // FEATURE_STUBS_AS_IL

#ifdef CROSSGEN_COMPILE
#define GetEEFuncEntryPoint(pfn) 0x1001
#else
#define GetEEFuncEntryPoint(pfn) GFN_TADDR(pfn)
#endif

//**********************************************************************
// To be used with GetSpecificCpuInfo()

#define CPU_X86_FAMILY(cpuType)     (((cpuType) & 0x0F00) >> 8)
#define CPU_X86_MODEL(cpuType)      (((cpuType) & 0x00F0) >> 4)
// Stepping is masked out by GetSpecificCpuInfo()
// #define CPU_X86_STEPPING(cpuType)   (((cpuType) & 0x000F)     )

#define CPU_X86_USE_CMOV(cpuFeat)   ((cpuFeat & 0x00008001) == 0x00008001)
#define CPU_X86_USE_SSE2(cpuFeat)   ((cpuFeat & 0x04000000) == 0x04000000)

// Values for CPU_X86_FAMILY(cpuType)
#define CPU_X86_486                 4
#define CPU_X86_PENTIUM             5
#define CPU_X86_PENTIUM_PRO         6
#define CPU_X86_PENTIUM_4           0xF

// Values for CPU_X86_MODEL(cpuType) for CPU_X86_PENTIUM_PRO
#define CPU_X86_MODEL_PENTIUM_PRO_BANIAS    9 // Pentium M (Mobile PPro with P4 feautres)

#define COMMETHOD_PREPAD                        8   // # extra bytes to allocate in addition to sizeof(ComCallMethodDesc)
#ifdef FEATURE_COMINTEROP
#define COMMETHOD_CALL_PRESTUB_SIZE             5   // x86: CALL(E8) xx xx xx xx
#define COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET   1   // the offset of the call target address inside the prestub
#endif // FEATURE_COMINTEROP

#define STACK_ALIGN_SIZE                        4

#define JUMP_ALLOCATE_SIZE                      8   // # bytes to allocate for a jump instruction
#define BACK_TO_BACK_JUMP_ALLOCATE_SIZE         8   // # bytes to allocate for a back to back jump instruction

#ifdef FEATURE_EH_FUNCLETS
#define USE_INDIRECT_CODEHEADER
#endif // FEATURE_EH_FUNCLETS

#define HAS_COMPACT_ENTRYPOINTS                 1

// Needed for PInvoke inlining in ngened images
#define HAS_NDIRECT_IMPORT_PRECODE              1

#define HAS_FIXUP_PRECODE                       1
#define HAS_FIXUP_PRECODE_CHUNKS                1

// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define CODE_SIZE_ALIGN                         4
#define CACHE_LINE_SIZE                         32  // As per Intel Optimization Manual the cache line size is 32 bytes
#define LOG2SLOT                                LOG2_PTRSIZE

#define ENREGISTERED_RETURNTYPE_MAXSIZE         8
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE 4
#define CALLDESCR_ARGREGS                       1   // CallDescrWorker has ArgumentRegister parameter

//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType = false /* unused */, bool isFloatHfa = false /* unused */)
{
    const unsigned stackSlotSize = 4;
    return ALIGN_UP(parmSize, stackSlotSize);
}

#include "stublinkerx86.h"

//**********************************************************************
// Frames
//**********************************************************************
//--------------------------------------------------------------------
// This represents some of the FramedMethodFrame fields that are
// stored at negative offsets.
//--------------------------------------------------------------------
#define ENUM_ARGUMENT_AND_SCRATCH_REGISTERS() \
    ARGUMENT_AND_SCRATCH_REGISTER(Eax) \
    ARGUMENT_AND_SCRATCH_REGISTER(Ecx) \
    ARGUMENT_AND_SCRATCH_REGISTER(Edx)

#define ENUM_CALLEE_SAVED_REGISTERS() \
    CALLEE_SAVED_REGISTER(Edi) \
    CALLEE_SAVED_REGISTER(Esi) \
    CALLEE_SAVED_REGISTER(Ebx) \
    CALLEE_SAVED_REGISTER(Ebp)

typedef DPTR(struct CalleeSavedRegisters) PTR_CalleeSavedRegisters;
struct CalleeSavedRegisters {
#define CALLEE_SAVED_REGISTER(regname) INT32 regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
};

//--------------------------------------------------------------------
// This represents the arguments that are stored in volatile registers.
// This should not overlap the CalleeSavedRegisters since those are already
// saved separately and it would be wasteful to save the same register twice.
// If we do use a non-volatile register as an argument, then the ArgIterator
// will probably have to communicate this back to the PromoteCallerStack
// routine to avoid a double promotion.
//--------------------------------------------------------------------
#define ENUM_ARGUMENT_REGISTERS() \
    ARGUMENT_REGISTER(ECX) \
    ARGUMENT_REGISTER(EDX)

#define ENUM_ARGUMENT_REGISTERS_BACKWARD() \
    ARGUMENT_REGISTER(EDX) \
    ARGUMENT_REGISTER(ECX)

typedef DPTR(struct ArgumentRegisters) PTR_ArgumentRegisters;
struct ArgumentRegisters {
    #define ARGUMENT_REGISTER(regname) INT32 regname;
    ENUM_ARGUMENT_REGISTERS_BACKWARD()
    #undef ARGUMENT_REGISTER
};
#define NUM_ARGUMENT_REGISTERS 2

#define SCRATCH_REGISTER_X86REG kEAX

#define THIS_REG ECX
#define THIS_kREG kECX

#define ARGUMENT_REG1   ECX
#define ARGUMENT_REG2   EDX

// forward decl
struct REGDISPLAY;
typedef REGDISPLAY *PREGDISPLAY;

#ifndef FEATURE_EH_FUNCLETS
// Sufficient context for Try/Catch restoration.
struct EHContext {
    INT32       Eax;
    INT32       Ebx;
    INT32       Ecx;
    INT32       Edx;
    INT32       Esi;
    INT32       Edi;
    INT32       Ebp;
    INT32       Esp;
    INT32       Eip;

    void Setup(PCODE resumePC, PREGDISPLAY regs);
    void UpdateFrame(PREGDISPLAY regs);

    inline TADDR GetSP() {
        LIMITED_METHOD_CONTRACT;
        return (TADDR)Esp;
    }
    inline void SetSP(LPVOID esp) {
        LIMITED_METHOD_CONTRACT;
        Esp = (INT32)(size_t)esp;
    }

    inline LPVOID GetFP() {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)(UINT_PTR)Ebp;
    }

    inline void SetArg(LPVOID arg) {
        LIMITED_METHOD_CONTRACT;
        Eax = (INT32)(size_t)arg;
    }

    inline void Init()
    {
        LIMITED_METHOD_CONTRACT;
        Eax = 0;
        Ebx = 0;
        Ecx = 0;
        Edx = 0;
        Esi = 0;
        Edi = 0;
        Ebp = 0;
        Esp = 0;
        Eip = 0;
    }
};
#endif // !FEATURE_EH_FUNCLETS

#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)

//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;

    return PCODE(context->Eip);
}

inline void SetIP(CONTEXT *context, PCODE eip) {
    LIMITED_METHOD_DAC_CONTRACT;

    context->Eip = (DWORD)eip;
}

inline TADDR GetSP(const CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;

    return (TADDR)(context->Esp);
}

EXTERN_C LPVOID STDCALL GetCurrentSP();

inline void SetSP(CONTEXT *context, TADDR esp) {
    LIMITED_METHOD_DAC_CONTRACT;

    context->Esp = (DWORD)esp;
}

inline void SetFP(CONTEXT *context, TADDR ebp) {
    LIMITED_METHOD_DAC_CONTRACT;

    context->Ebp = (INT32)ebp;
}

inline TADDR GetFP(const CONTEXT * context)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (TADDR)context->Ebp;
}

// Get Rel32 destination, emit jumpStub if necessary
inline INT32 rel32UsingJumpStub(INT32 UNALIGNED * pRel32, PCODE target, MethodDesc *pMethod = NULL, LoaderAllocator *pLoaderAllocator = NULL)
{
    // We do not need jump stubs on i386
    LIMITED_METHOD_CONTRACT;

    TADDR baseAddr = (TADDR)pRel32 + 4;
    return (INT32)(target - baseAddr);
}

#ifdef FEATURE_COMINTEROP
inline void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target)
{
    WRAPPER_NO_CONTRACT;

    BYTE *pBufferRW = (BYTE*)pCOMMethodRW - COMMETHOD_CALL_PRESTUB_SIZE;
    BYTE *pBufferRX = (BYTE*)pCOMMethodRX - COMMETHOD_CALL_PRESTUB_SIZE;

    pBufferRW[0] = X86_INSTR_CALL_REL32; //CALLNEAR32
    *((LPVOID*)(1+pBufferRW)) = (LPVOID) (((LPBYTE)target) - (pBufferRX+5));

    _ASSERTE(IS_ALIGNED(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET, sizeof(void*)) &&
        *((SSIZE_T*)(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET)) == ((LPBYTE)target - (LPBYTE)pCOMMethodRX));
}
#endif // FEATURE_COMINTEROP

//------------------------------------------------------------------------
WORD GetUnpatchedCodeData(LPCBYTE pAddr);

//------------------------------------------------------------------------
inline WORD GetUnpatchedOpcodeWORD(LPCBYTE pAddr)
{
    WRAPPER_NO_CONTRACT;
    if (CORDebuggerAttached())
    {
        return GetUnpatchedCodeData(pAddr);
    }
    else
    {
        return *((WORD *)pAddr);
    }
}

//------------------------------------------------------------------------
inline BYTE GetUnpatchedOpcodeBYTE(LPCBYTE pAddr)
{
    WRAPPER_NO_CONTRACT;
    if (CORDebuggerAttached())
    {
        return (BYTE) GetUnpatchedCodeData(pAddr);
    }
    else
    {
        return *pAddr;
    }
}

 //------------------------------------------------------------------------
// The following must be a distinguishable set of instruction sequences for
// various stub dispatch calls.
//
// An x86 JIT which uses full stub dispatch must generate only
// the following stub dispatch calls:
//
// (1) isCallRelativeIndirect:
//        call dword ptr [rel32]  ;  FF 15 ---rel32----
// (2) isCallRelative:
//        call abc                ;     E8 ---rel32----
// (3) isCallRegisterIndirect:
//     3-byte nop                 ;
//     call dword ptr [eax]       ;     FF 10
//
// NOTE: You must be sure that pRetAddr is a true return address for
// a stub dispatch call.

BOOL isCallRelativeIndirect(const BYTE *pRetAddr);
BOOL isCallRelative(const BYTE *pRetAddr);
BOOL isCallRegisterIndirect(const BYTE *pRetAddr);

inline BOOL isCallRelativeIndirect(const BYTE *pRetAddr)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fRet = (GetUnpatchedOpcodeWORD(&pRetAddr[-6]) == X86_INSTR_CALL_IND);
    _ASSERTE(!fRet || !isCallRelative(pRetAddr));
    _ASSERTE(!fRet || !isCallRegisterIndirect(pRetAddr));
    return fRet;
}

inline BOOL isCallRelative(const BYTE *pRetAddr)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fRet = (GetUnpatchedOpcodeBYTE(&pRetAddr[-5]) == X86_INSTR_CALL_REL32);
    _ASSERTE(!fRet || !isCallRelativeIndirect(pRetAddr));
    _ASSERTE(!fRet || !isCallRegisterIndirect(pRetAddr));
    return fRet;
}

inline BOOL isCallRegisterIndirect(const BYTE *pRetAddr)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fRet = (GetUnpatchedOpcodeWORD(&pRetAddr[-5]) == X86_INSTR_NOP3_1)
             && (GetUnpatchedOpcodeBYTE(&pRetAddr[-3]) == X86_INSTR_NOP3_3)
             && (GetUnpatchedOpcodeWORD(&pRetAddr[-2]) == X86_INSTR_CALL_IND_EAX);
    _ASSERTE(!fRet || !isCallRelative(pRetAddr));
    _ASSERTE(!fRet || !isCallRelativeIndirect(pRetAddr));
    return fRet;
}

//------------------------------------------------------------------------
inline void emitJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;

    pBufferRW[0] = X86_INSTR_JMP_REL32; //JUMPNEAR32
    *((LPVOID*)(1+pBufferRW)) = (LPVOID) (((LPBYTE)target) - (pBufferRX+5));
}

//------------------------------------------------------------------------
inline void emitJumpInd(LPBYTE pBuffer, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;

    *((WORD*)pBuffer) = X86_INSTR_JMP_IND; // 0x25FF  jmp dword ptr[addr32]
    *((LPVOID*)(2+pBuffer)) = target;
}

//------------------------------------------------------------------------
inline PCODE isJump(PCODE pCode)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return *PTR_BYTE(pCode) == X86_INSTR_JMP_REL32;
}

//------------------------------------------------------------------------
//  Given the same pBuffer that was used by emitJump this method
//  decodes the instructions and returns the jump target
inline PCODE decodeJump(PCODE pCode)
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(*PTR_BYTE(pCode) == X86_INSTR_JMP_REL32);
    return rel32Decode(pCode+1);
}

//
// On IA64 back to back jumps should be separated by a nop bundle to get
// the best performance from the hardware's branch prediction logic.
// For all other platforms back to back jumps don't require anything special
// That is why we have these two wrapper functions that call emitJump and decodeJump
//

//------------------------------------------------------------------------
inline void emitBackToBackJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    WRAPPER_NO_CONTRACT;
    emitJump(pBufferRX, pBufferRW, target);
}

//------------------------------------------------------------------------
inline PCODE isBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return isJump(pBuffer);
}

//------------------------------------------------------------------------
inline PCODE decodeBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return decodeJump(pBuffer);
}

EXTERN_C void __stdcall setFPReturn(int fpSize, INT64 retVal);
EXTERN_C void __stdcall getFPReturn(int fpSize, INT64 *pretval);


// SEH info forward declarations

#include <pshpack1.h>
struct DECLSPEC_ALIGN(4) UMEntryThunkCode
{
    BYTE            m_alignpad[2];  // used to guarantee alignment of backpactched portion
    BYTE            m_movEAX;   //MOV EAX,imm32
    LPVOID          m_uet;      // pointer to start of this structure
    BYTE            m_jmp;      //JMP NEAR32
    const BYTE *    m_execstub; // pointer to destination code  // make sure the backpatched portion is dword aligned.

    void Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam);
    void Poison();

    LPCBYTE GetEntryPoint() const
    {
        LIMITED_METHOD_CONTRACT;

        return (LPCBYTE)&m_movEAX;
    }

    static int GetEntryPointOffset()
    {
        LIMITED_METHOD_CONTRACT;

        return 2;
    }
};
#include <poppack.h>

struct HijackArgs
{
    DWORD FPUState[3]; // 12 bytes for FPU state (10 bytes for FP top-of-stack + 2 bytes padding)
    DWORD Edi;
    DWORD Esi;
    DWORD Ebx;
    DWORD Edx;
    DWORD Ecx;
    union
    {
        DWORD Eax;
        size_t ReturnValue[1];
    };
    DWORD Ebp;
    union
    {
        DWORD Eip;
        size_t ReturnAddress;
    };
};

// ClrFlushInstructionCache is used when we want to call FlushInstructionCache
// for a specific architecture in the common code, but not for other architectures.
// On IA64 ClrFlushInstructionCache calls the Kernel FlushInstructionCache function
// to flush the instruction cache.
// We call ClrFlushInstructionCache whenever we create or modify code in the heap.
// Currently ClrFlushInstructionCache has no effect on X86
//

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode)
{
    // FlushInstructionCache(GetCurrentProcess(), pCodeAddr, sizeOfCode);
    MemoryBarrier();
    return TRUE;
}

//
// JIT HELPER ALIASING FOR PORTABILITY.
//
// Create alias for optimized implementations of helpers provided on this platform
//

// optimized static helpers generated dynamically at runtime
// #define JIT_GetSharedGCStaticBase
// #define JIT_GetSharedNonGCStaticBase
// #define JIT_GetSharedGCStaticBaseNoCtor
// #define JIT_GetSharedNonGCStaticBaseNoCtor

#ifndef TARGET_UNIX
#define JIT_NewCrossContext         JIT_NewCrossContext
#endif // TARGET_UNIX

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

#ifndef DACCESS_COMPILE
protected:
    template<class T> static INT_PTR GetRelativeOffset(const T *relRef, PCODE target)
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(sizeof(T) != 0);
        static_assert_no_msg(sizeof(T) <= sizeof(void *));
        static_assert_no_msg((sizeof(T) & (sizeof(T) - 1)) == 0); // is a power of 2
        _ASSERTE(relRef != nullptr);

        TADDR targetAddress = PCODEToPINSTR(target);
        _ASSERTE(targetAddress != NULL);
        return (INT_PTR)targetAddress - (INT_PTR)(relRef + 1);
    }
#endif

protected:
    template<class T> static PCODE GetTarget(const T *relRef)
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(sizeof(T) == 1 || sizeof(T) == 2 || sizeof(T) == 4 || sizeof(T) == 8);
        _ASSERTE(relRef != nullptr);

        return PINSTRToPCODE((INT_PTR)(relRef + 1) + *relRef);
    }

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
    const UINT8 m_part0[1];
    CallCount *const m_remainingCallCountCell;
    const UINT8 m_part1[5];
    const INT32 m_rel32TargetForMethod;
    const UINT8 m_part2[1];
    const INT32 m_rel32TargetForThresholdReached;
    const UINT8 m_alignmentPadding[1];

#ifndef DACCESS_COMPILE
public:
    CallCountingStubShort(CallCountingStubShort* stubRX, CallCount *remainingCallCountCell, PCODE targetForMethod)
        : m_part0{                                              0xb8},                  //     mov  eax,
        m_remainingCallCountCell(remainingCallCountCell),                               //               <imm32>
        m_part1{                                                0x66, 0xff, 0x08,       //     dec  word ptr [eax]
                                                                0x0f, 0x85},            //     jnz  
        m_rel32TargetForMethod(                                                         //          <rel32>
            GetRelative32BitOffset(
                &stubRX->m_rel32TargetForMethod,
                targetForMethod)),
        m_part2{                                                0xe8},                  //     call
        m_rel32TargetForThresholdReached(                                               //          <rel32>
            GetRelative32BitOffset(
                &stubRX->m_rel32TargetForThresholdReached,
                TargetForThresholdReached)),
                                                                                        // (eip == stub-identifying token)
        m_alignmentPadding{                                     0xcc}                   //     int  3
    {
        WRAPPER_NO_CONTRACT;
        static_assert_no_msg(sizeof(CallCountingStubShort) % Alignment == 0);
        _ASSERTE(remainingCallCountCell != nullptr);
        _ASSERTE(PCODEToPINSTR(targetForMethod) != NULL);
    }

public:
    static bool Is(TADDR stubIdentifyingToken)
    {
        WRAPPER_NO_CONTRACT;
        return true;
    }

    static const CallCountingStubShort *From(TADDR stubIdentifyingToken)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(Is(stubIdentifyingToken));
        _ASSERTE(stubIdentifyingToken % Alignment == offsetof(CallCountingStubShort, m_alignmentPadding[0]) % Alignment);

        const CallCountingStubShort *stub =
            (const CallCountingStubShort *)(stubIdentifyingToken - offsetof(CallCountingStubShort, m_alignmentPadding[0]));
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
        return GetTarget(&m_rel32TargetForMethod);
    }

#ifndef DACCESS_COMPILE
private:
    static INT32 GetRelative32BitOffset(const INT32 *rel32Ref, PCODE target)
    {
        WRAPPER_NO_CONTRACT;

        INT_PTR relativeOffset = GetRelativeOffset(rel32Ref, target);
        _ASSERTE((INT32)relativeOffset == relativeOffset);
        return (INT32)relativeOffset;
    }
#endif

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

#endif // __cgenx86_h__
