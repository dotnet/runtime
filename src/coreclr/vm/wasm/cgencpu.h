// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __cgenwasm_h__
#define __cgenwasm_h__

#include "stublink.h"
#include "utilcode.h"

// preferred alignment for data
#define DATA_ALIGNMENT 4

#define CODE_SIZE_ALIGN                         4
#define LOG2SLOT                                LOG2_PTRSIZE

// looks like this is mandatory for now
#define HAS_NDIRECT_IMPORT_PRECODE              1
#define HAS_FIXUP_PRECODE                       1
// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define BACK_TO_BACK_JUMP_ALLOCATE_SIZE         8   // # bytes to allocate for a back to back jump instruction

//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType = false /* unused */, bool isFloatHfa = false /* unused */)
{
    _ASSERTE("The function is not implemented on wasm");
    return 0;
}

inline TADDR GetSP(const T_CONTEXT * context)
{
    _ASSERTE("The function is not implemented on wasm, it lacks registers");
    return 0;
}

struct HijackArgs
{
};

inline LPVOID STDCALL GetCurrentSP()
{
    _ASSERTE("The function is not implemented on wasm, it lacks registers");
    return nullptr;
}

extern PCODE GetPreStubEntryPoint();

#define GetEEFuncEntryPoint(pfn) GFN_TADDR(pfn)

//**********************************************************************
// Frames
//**********************************************************************

//--------------------------------------------------------------------
// This represents the callee saved (non-volatile) registers saved as
// of a FramedMethodFrame.
//--------------------------------------------------------------------
typedef DPTR(struct CalleeSavedRegisters) PTR_CalleeSavedRegisters;
struct CalleeSavedRegisters {
};

//--------------------------------------------------------------------
// This represents the arguments that are stored in volatile registers.
// This should not overlap the CalleeSavedRegisters since those are already
// saved separately and it would be wasteful to save the same register twice.
// If we do use a non-volatile register as an argument, then the ArgIterator
// will probably have to communicate this back to the PromoteCallerStack
// routine to avoid a double promotion.
//--------------------------------------------------------------------
typedef DPTR(struct ArgumentRegisters) PTR_ArgumentRegisters;
struct ArgumentRegisters {
};
#define NUM_ARGUMENT_REGISTERS 0
#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)

#define ENREGISTERED_RETURNTYPE_MAXSIZE         16  // not sure here, 16 bytes is v128

#define STACKWALK_CONTROLPC_ADJUST_OFFSET 0

class StubLinkerCPU : public StubLinker
{
public:
    static void Init();
    void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);
    VOID EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
};

//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const T_CONTEXT * context) {
    _ASSERT("GetIP is not implemented on wasm, it lacks registers");
    return 0;
}

inline void SetIP(T_CONTEXT *context, PCODE eip) {
    _ASSERT("SetIP is not implemented on wasm, it lacks registers");
}

inline void SetSP(T_CONTEXT *context, TADDR esp) {
    _ASSERT("SetSP is not implemented on wasm, it lacks registers");
}

inline void SetFP(T_CONTEXT *context, TADDR ebp) {
    _ASSERT("SetFP is not implemented on wasm, it lacks registers");
}

inline TADDR GetFP(const T_CONTEXT * context)
{
    _ASSERT("GetFP is not implemented on wasm, it lacks registers");
    return 0;
}

#define ENUM_CALLEE_SAVED_REGISTERS()

#define ENUM_FP_CALLEE_SAVED_REGISTERS()

// ClrFlushInstructionCache is used when we want to call FlushInstructionCache
// for a specific architecture in the common code, but not for other architectures.
// On IA64 ClrFlushInstructionCache calls the Kernel FlushInstructionCache function
// to flush the instruction cache.
// We call ClrFlushInstructionCache whenever we create or modify code in the heap.
// Currently ClrFlushInstructionCache has no effect on X86
//

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode, bool hasCodeExecutedBefore = false)
{
    // no-op on wasm
    return true;
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
    _ASSERTE("emitBackToBackJump is not implemented on wasm");
}

//------------------------------------------------------------------------
inline PCODE decodeBackToBackJump(PCODE pBuffer)
{
    _ASSERTE("decodeBackToBackJump is not implemented on wasm");
    return 0;
}

FORCEINLINE int64_t PalInterlockedCompareExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue, int64_t iComparand)
{
    int64_t result = __sync_val_compare_and_swap(pDst, iComparand, iValue);
    __sync_synchronize();
    return result;
}

#endif // __cgenwasm_h__
