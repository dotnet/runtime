// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "regdisplay.h"
#include "ICodeManager.h"
#include "UnixNativeCodeManager.h"
#include "varint.h"
#include "holder.h"

#include "CommonMacros.inl"

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.cpp"

#include "UnixContext.h"

#define UBF_FUNC_KIND_MASK      0x03
#define UBF_FUNC_KIND_ROOT      0x00
#define UBF_FUNC_KIND_HANDLER   0x01
#define UBF_FUNC_KIND_FILTER    0x02

#define UBF_FUNC_HAS_EHINFO             0x04
#define UBF_FUNC_REVERSE_PINVOKE        0x08
#define UBF_FUNC_HAS_ASSOCIATED_DATA    0x10

struct UnixNativeMethodInfo
{
    PTR_VOID pMethodStartAddress;
    PTR_UInt8 pMainLSDA;
    PTR_UInt8 pLSDA;
    bool executionAborted;
};

// Ensure that UnixNativeMethodInfo fits into the space reserved by MethodInfo
static_assert(sizeof(UnixNativeMethodInfo) <= sizeof(MethodInfo), "UnixNativeMethodInfo too big");

UnixNativeCodeManager::UnixNativeCodeManager(TADDR moduleBase,
                                             PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                                             PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions)
    : m_moduleBase(moduleBase),
      m_pvManagedCodeStartRange(pvManagedCodeStartRange), m_cbManagedCodeRange(cbManagedCodeRange),
      m_pClasslibFunctions(pClasslibFunctions), m_nClasslibFunctions(nClasslibFunctions)
{
}

UnixNativeCodeManager::~UnixNativeCodeManager()
{
}

bool UnixNativeCodeManager::FindMethodInfo(PTR_VOID        ControlPC,
                                           MethodInfo *    pMethodInfoOut)
{
    // Stackwalker may call this with ControlPC that does not belong to this code manager
    if (dac_cast<TADDR>(ControlPC) < dac_cast<TADDR>(m_pvManagedCodeStartRange) ||
        dac_cast<TADDR>(m_pvManagedCodeStartRange) + m_cbManagedCodeRange <= dac_cast<TADDR>(ControlPC))
    {
        return false;
    }

    UnixNativeMethodInfo * pMethodInfo = (UnixNativeMethodInfo *)pMethodInfoOut;
    uintptr_t startAddress, endAddress;
    uintptr_t lsda;

    if (!FindProcInfo((uintptr_t)ControlPC, &startAddress, &endAddress, &lsda))
    {
        return false;
    }

    PTR_UInt8 p = dac_cast<PTR_UInt8>(lsda);

    pMethodInfo->pLSDA = p;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
    {
        // Funclets just refer to the main function's blob
        pMethodInfo->pMainLSDA = p + *dac_cast<PTR_Int32>(p);
        p += sizeof(int32_t);

        pMethodInfo->pMethodStartAddress = dac_cast<PTR_VOID>(startAddress - *dac_cast<PTR_Int32>(p));
    }
    else
    {
        pMethodInfo->pMainLSDA = dac_cast<PTR_UInt8>(lsda);
        pMethodInfo->pMethodStartAddress = dac_cast<PTR_VOID>(startAddress);
    }

    pMethodInfo->executionAborted = false;

    return true;
}

bool UnixNativeCodeManager::IsFunclet(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT;
}

bool UnixNativeCodeManager::IsFilter(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) == UBF_FUNC_KIND_FILTER;
}

PTR_VOID UnixNativeCodeManager::GetFramePointer(MethodInfo *   pMethodInfo,
                                                REGDISPLAY *   pRegisterSet)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    // Return frame pointer for methods with EH and funclets
    uint8_t unwindBlockFlags = *(pNativeMethodInfo->pLSDA);
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0 || (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
    {
        return (PTR_VOID)pRegisterSet->GetFP();
    }

    return NULL;
}

uint32_t UnixNativeCodeManager::GetCodeOffset(MethodInfo* pMethodInfo, PTR_VOID address, /*out*/ PTR_UInt8* gcInfo)
{
    UnixNativeMethodInfo* pNativeMethodInfo = (UnixNativeMethodInfo*)pMethodInfo;

    PTR_UInt8 p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    *gcInfo = p;

    uint32_t codeOffset = (uint32_t)(PINSTRToPCODE(dac_cast<TADDR>(address)) - PINSTRToPCODE(dac_cast<TADDR>(pNativeMethodInfo->pMethodStartAddress)));
    return codeOffset;
}

bool UnixNativeCodeManager::IsSafePoint(PTR_VOID pvAddress)
{
    MethodInfo pMethodInfo;
    if (!FindMethodInfo(pvAddress, &pMethodInfo))
    {
        return false;
    }

    PTR_UInt8 gcInfo;
    uint32_t codeOffset = GetCodeOffset(&pMethodInfo, pvAddress, &gcInfo);

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_INTERRUPTIBILITY),
        codeOffset
    );

    return decoder.IsInterruptible();
}

void UnixNativeCodeManager::EnumGcRefs(MethodInfo *    pMethodInfo,
                                       PTR_VOID        safePointAddress,
                                       REGDISPLAY *    pRegisterSet,
                                       GCEnumContext * hCallback,
                                       bool            isActiveStackFrame)
{
    PTR_UInt8 gcInfo;
    uint32_t codeOffset = GetCodeOffset(pMethodInfo, safePointAddress, &gcInfo);

    if (!isActiveStackFrame)
    {
        // If we are not in the active method, we are currently pointing
        // to the return address. That may not be reachable after a call (if call does not return)
        // or reachable via a jump and thus have a different live set.
        // Therefore we simply adjust the offset to inside of call instruction.
        // NOTE: The GcInfoDecoder depends on this; if you change it, you must
        // revisit the GcInfoEncoder/Decoder
        codeOffset--;
    }

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG),
        codeOffset
    );

    ICodeManagerFlags flags = (ICodeManagerFlags)0;
    if (((UnixNativeMethodInfo*)pMethodInfo)->executionAborted)
        flags = ICodeManagerFlags::ExecutionAborted;

    if (IsFilter(pMethodInfo))
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::NoReportUntracked);

    if (isActiveStackFrame)
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::ActiveStackFrame);

    if (!decoder.EnumerateLiveSlots(
        pRegisterSet,
        isActiveStackFrame /* reportScratchSlots */,
        flags,
        hCallback->pCallback,
        hCallback
        ))
    {
        assert(false);
    }
}

uintptr_t UnixNativeCodeManager::GetConservativeUpperBoundForOutgoingArgs(MethodInfo * pMethodInfo, REGDISPLAY * pRegisterSet)
{
    // Return value
    uintptr_t upperBound;

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_UInt8 p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->pMainLSDA == pNativeMethodInfo->pLSDA);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer = (TADDR)NULL;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();
        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        // Reverse PInvoke case.  The embedded reverse PInvoke frame is guaranteed to reside above
        // all outgoing arguments.
        upperBound = (uintptr_t)dac_cast<TADDR>(basePointer + slot);
    }
    else
    {
        // The passed in pRegisterSet should be left intact
        REGDISPLAY localRegisterSet = *pRegisterSet;

        bool result = VirtualUnwind(&localRegisterSet);
        assert(result);

        // All common ABIs have outgoing arguments under caller SP (minus slot reserved for return address).
        // There are ABI-specific optimizations that could applied here, but they are not worth the complexity
        // given that this path is used rarely.
#if defined(TARGET_X86) || defined(TARGET_AMD64)
        upperBound = dac_cast<TADDR>(localRegisterSet.GetSP() - sizeof(TADDR));
#else
        upperBound = dac_cast<TADDR>(localRegisterSet.GetSP());
#endif
    }

    return upperBound;
}

bool UnixNativeCodeManager::UnwindStackFrame(MethodInfo *    pMethodInfo,
                                             REGDISPLAY *    pRegisterSet,                 // in/out
                                             PInvokeTransitionFrame**      ppPreviousTransitionFrame)    // out
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_UInt8 p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->pMainLSDA == pNativeMethodInfo->pLSDA);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer = (TADDR)NULL;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();
        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        *ppPreviousTransitionFrame = *(PInvokeTransitionFrame**)(basePointer + slot);
        return true;
    }

    *ppPreviousTransitionFrame = NULL;

    if (!VirtualUnwind(pRegisterSet)) 
    {
        return false;
    }

    return true;
}

bool UnixNativeCodeManager::IsUnwindable(PTR_VOID pvAddress)
{
    // VirtualUnwind can't unwind epilogues.
    return TrailingEpilogueInstructionsCount(pvAddress) == 0;
}

// when stopped in an epilogue, returns the count of remaining stack-consuming instructions
// otherwise returns
//  0 - not in epilogue,
// -1 - unknown.
int UnixNativeCodeManager::TrailingEpilogueInstructionsCount(PTR_VOID pvAddress)
{
#ifdef TARGET_AMD64

#define SIZE64_PREFIX 0x48
#define ADD_IMM8_OP 0x83
#define ADD_IMM32_OP 0x81
#define JMP_IMM8_OP 0xeb
#define JMP_IMM32_OP 0xe9
#define JMP_IND_OP 0xff
#define LEA_OP 0x8d
#define REPNE_PREFIX 0xf2
#define REP_PREFIX 0xf3
#define POP_OP 0x58
#define RET_OP 0xc3
#define RET_OP_2 0xc2
#define INT3_OP 0xcc

#define IS_REX_PREFIX(x) (((x) & 0xf0) == 0x40)

    //
    // Everything below is inspired by the code in minkernel\ntos\rtl\amd64\exdsptch.c file from Windows
    // For details see similar code in OOPStackUnwinderAMD64::UnwindEpilogue
    //
    //
    //    
    // A canonical epilogue sequence consists of the following operations:
    //
    // 1. Optional cleanup of fixed and dynamic stack allocations, which is
    //    considered to be outside of the epilogue region.
    //
    //    add rsp, imm
    //        or
    //    lea rsp, disp[fp]
    //
    // 2. Zero or more pop nonvolatile-integer-register[0..15] instructions.
    //
    //    pop r64
    //        or
    //    REX.R pop r64
    //
    // 3. An optional one-byte pop r64 to a volatile register to clean up an
    //    RFLAGS register pushed with pushfq.
    //
    //    pop rcx
    //
    // 4. A control transfer instruction (ret or jump, in a case of a tailcall)
    //    For the purpose of inferring the state of the stack, ret and jump can be
    //    considered the same.
    //
    //    ret 0
    //        or
    //    jmp imm
    //        or
    //    jmp [target]
    //
    // 5. Occasionally we may see a breakpoint, possibly placed by the debugger.
    //    In such case we do not know what instruction it was and return -1 (unknown)
    //
    //    int 3
    //

    // if we are in an epilogue, there will be at least one instruction left.
    int trailingEpilogueInstructions = 1;
    uint8_t* pNextByte = (uint8_t*)pvAddress;

    //
    // Check for any number of:
    //
    //   pop nonvolatile-integer-register[0..15].
    //

    while (true)
    {
        if ((pNextByte[0] & 0xf8) == POP_OP)
        {
            pNextByte += 1;
            trailingEpilogueInstructions++;
        }
        else if (IS_REX_PREFIX(pNextByte[0]) && ((pNextByte[1] & 0xf8) == POP_OP))
        {
            pNextByte += 2;
            trailingEpilogueInstructions++;
        }
        else
        {
            break;
        }
    }

    //
    // A REPNE prefix may optionally precede a control transfer
    // instruction with no effect on unwinding.
    //

    if (pNextByte[0] == REPNE_PREFIX)
    {
        pNextByte += 1;
    }

    if (((pNextByte[0] == RET_OP) ||
        (pNextByte[0] == RET_OP_2)) ||
        (((pNextByte[0] == REP_PREFIX) && (pNextByte[1] == RET_OP))))
    {
        //
        // A return is an unambiguous indication of an epilogue.
        //
        return trailingEpilogueInstructions;
    }

    if ((pNextByte[0] == JMP_IMM8_OP) ||
        (pNextByte[0] == JMP_IMM32_OP))
    {
        //
        // An unconditional branch to a target that is equal to the start of
        // or outside of this routine is logically a call to another function.
        //

        size_t branchTarget = (size_t)pNextByte;
        if (pNextByte[0] == JMP_IMM8_OP)
        {
            branchTarget += 2 + (int8_t)pNextByte[1];
        }
        else
        {
            uint32_t delta =
                (uint32_t)pNextByte[1] |
                ((uint32_t)pNextByte[2] << 8) |
                ((uint32_t)pNextByte[3] << 16) |
                ((uint32_t)pNextByte[4] << 24);

            branchTarget += 5 + (int32_t)delta;
        }

        //
        // Determine whether the branch target refers to code within this
        // function. If not, then it is an epilogue indicator.
        //
        // A branch to the start of self implies a recursive call, so
        // is treated as an epilogue.
        //

        size_t startAddress;
        size_t endAddress;
        uintptr_t lsda;

        bool result = FindProcInfo((uintptr_t)pvAddress, &startAddress, &endAddress, &lsda);
        ASSERT(result);

        if (branchTarget < startAddress || branchTarget >= endAddress)
        {
            return trailingEpilogueInstructions;
        }
    }
    else if ((pNextByte[0] == JMP_IND_OP) && (pNextByte[1] == 0x25))
    {
        //
        // An unconditional jump indirect.
        //
        // This is a jmp outside of the function, probably a tail call
        // to an import function.
        //

        return trailingEpilogueInstructions;
    }
    else if (((pNextByte[0] & 0xf8) == SIZE64_PREFIX) &&
        (pNextByte[1] == 0xff) &&
        (pNextByte[2] & 0x38) == 0x20)
    {
        //
        // This is an indirect jump opcode: 0x48 0xff /4.  The 64-bit
        // flag (REX.W) is always redundant here, so its presence is
        // overloaded to indicate a branch out of the function - a tail
        // call.
        //
        // Such an opcode is an unambiguous epilogue indication.
        //

        return trailingEpilogueInstructions;
    }
    else if (pNextByte[0] == INT3_OP)
    {
        //
        // A breakpoint, possibly placed by the debugger - we do not know what was here.
        //
        return -1;
    }

#elif defined(TARGET_ARM64)

// ldr with unsigned immediate
// 1x11 1001 x1xx xxxx xxxx xxxx xxxx xxxx
#define LDR_BITS1 0xB9400000
#define LDR_MASK1 0xBF400000

// ldr with pre/post/no offset
// 1x11 1000 010x xxxx xxxx xxxx xxxx xxxx
#define LDR_BITS2 0xB8400000
#define LDR_MASK2 0xBFE00000

// ldr with register offset
// 1x11 1000 011x xxxx xxxx 10xx xxxx xxxx
#define LDR_BITS3 0xB8600800
#define LDR_MASK3 0xBFE00C00

// ldp with signed offset
// x010 1001 01xx xxxx xxxx xxxx xxxx xxxx
#define LDP_BITS1 0x29400000
#define LDP_MASK1 0x7FC00000

// ldp with pre/post/no offset
// x010 100x x1xx xxxx xxxx xxxx xxxx xxxx
#define LDP_BITS2 0x28400000
#define LDP_MASK2 0x7E400000

// Branches, Exception Generating and System instruction group
// xxx1 01xx xxxx xxxx xxxx xxxx xxxx xxxx
#define BEGS_BITS 0x14000000
#define BEGS_MASK 0x1C000000

    MethodInfo pMethodInfo;
    FindMethodInfo(pvAddress, &pMethodInfo);
    UnixNativeMethodInfo* pNativeMethodInfo = (UnixNativeMethodInfo*)&pMethodInfo;

    uint32_t* start  = (uint32_t*)pNativeMethodInfo->pMethodStartAddress;

    // Since we stop on branches, the search is roughly limited by the containing basic block.
    // We typically examine just 1-5 instructions and in rare cases up to 30.
    // 
    // TODO: we can also limit the search by the longest possible epilogue length, but
    // we must be sure the longest length considers all possibilities,
    // which is somewhat nontrivial to derive/prove.
    // It does not seem urgent, but it could be nice to have a constant upper bound.
    for (uint32_t* pInstr = (uint32_t*)pvAddress - 1; pInstr > start; pInstr--)
    {
        uint32_t instr = *pInstr;
    
        // check for Branches, Exception Generating and System instruction group.
        // If we see such instruction before seeing FP or LR restored, we are not in an epilog.
        // Note: this includes RET, BRK, branches, calls, tailcalls, fences, etc...
        if ((instr & BEGS_MASK) == BEGS_BITS)
        {
            // not in an epilogue
            break;
        }

        // check for restoring FP or LR with ldr or ldp
        int operand = instr & 0x1f;
        if (operand == 30 || operand == 29)
        {
            if ((instr & LDP_MASK1) == LDP_BITS1 ||
                (instr & LDP_MASK2) == LDP_BITS2 ||
                (instr & LDR_MASK1) == LDR_BITS1 ||
                (instr & LDR_MASK2) == LDR_BITS2 ||
                (instr & LDR_MASK3) == LDR_BITS3)
            {
                return -1;
            }
        }

        // check for restoring FP or LR with ldp (as Rt2)
        operand = (instr >> 10) & 0x1f;
        if (operand == 30 || operand == 29)
        {
            if ((instr & LDP_MASK1) == LDP_BITS1 ||
                (instr & LDP_MASK2) == LDP_BITS2)
            {
                return -1;
            }
        }
    }

#endif

    return 0;
}

// Convert the return kind that was encoded by RyuJIT to the
// enum used by the runtime.
GCRefKind GetGcRefKind(ReturnKind returnKind)
{
    ASSERT((returnKind >= RT_Scalar) && (returnKind <= RT_ByRef_ByRef));

    return (GCRefKind)returnKind;
}

bool UnixNativeCodeManager::GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                                       REGDISPLAY *    pRegisterSet,       // in
                                                       PTR_PTR_VOID *  ppvRetAddrLocation, // out
                                                       GCRefKind *     pRetValueKind)      // out
{
    UnixNativeMethodInfo* pNativeMethodInfo = (UnixNativeMethodInfo*)pMethodInfo;

    PTR_UInt8 p = pNativeMethodInfo->pLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    // Check whether this is a funclet
    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
        return false;

    // Skip hijacking a reverse-pinvoke method - it doesn't get us much because we already synchronize
    // with the GC on the way back to native code.
    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
        return false;

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    // Decode the GC info for the current method to determine its return type
    GcInfoDecoderFlags flags = DECODE_RETURN_KIND;
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    flags = (GcInfoDecoderFlags)(flags | DECODE_HAS_TAILCALLS);
#endif // TARGET_ARM || TARGET_ARM64

    GcInfoDecoder decoder(GCInfoToken(p), flags);
    *pRetValueKind = GetGcRefKind(decoder.GetReturnKind());

    int epilogueInstructions = TrailingEpilogueInstructionsCount((PTR_VOID)pRegisterSet->IP);
    if (epilogueInstructions < 0)
    {
        // can't figure, possibly a breakpoint instruction
        return false;
    }
    else if (epilogueInstructions > 0)
    {
        *ppvRetAddrLocation = (PTR_PTR_VOID)(pRegisterSet->GetSP() + (sizeof(TADDR) * (epilogueInstructions - 1)));
        return true;
    }

    ASSERT(IsUnwindable((PTR_VOID)pRegisterSet->IP));

    // Unwind the current method context to the caller's context to get its stack pointer
    // and obtain the location of the return address on the stack
#if defined(TARGET_AMD64)

    if (!VirtualUnwind(pRegisterSet))
    {
        return false;
    }

    *ppvRetAddrLocation = (PTR_PTR_VOID)(pRegisterSet->GetSP() - sizeof(TADDR));
    return true;

#elif defined(TARGET_ARM64)

    if (decoder.HasTailCalls())
    {
        // Do not hijack functions that have tail calls, since there are two problems:
        // 1. When a function that tail calls another one is hijacked, the LR may be
        //    stored at a different location in the stack frame of the tail call target.
        //    So just by performing tail call, the hijacked location becomes invalid and
        //    unhijacking would corrupt stack by writing to that location.
        // 2. There is a small window after the caller pops LR from the stack in its
        //    epilog and before the tail called function pushes LR in its prolog when
        //    the hijacked return address would not be not on the stack and so we would
        //    not be able to unhijack.
        return false;
    }

    PTR_UIntNative pLR = pRegisterSet->pLR;
    if (!VirtualUnwind(pRegisterSet))
    {
        return false;
    }

    if (pRegisterSet->pLR == pLR)
    {
        // This is the case when we are either:
        //
        // 1) In a leaf method that does not push LR on stack, OR
        // 2) In the prolog/epilog of a non-leaf method that has not yet pushed LR on stack
        //    or has LR already popped off.
        return false;
    }

    *ppvRetAddrLocation = (PTR_PTR_VOID)pRegisterSet->pLR;
    return true;
#else
    return false;
#endif // defined(TARGET_AMD64)
}

PTR_VOID UnixNativeCodeManager::RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC)
{
    // GCInfo decoder needs to know whether execution of the method is aborted
    // while querying for gc-info.  But ICodeManager::EnumGCRef() doesn't receive any
    // flags from mrt. Call to this method is used as a cue to mark the method info
    // as execution aborted. Note - if pMethodInfo was cached, this scheme would not work.
    //
    // If the method has EH, then JIT will make sure the method is fully interruptible
    // and we will have GC-info available at the faulting address as well.

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    pNativeMethodInfo->executionAborted = true;

    return controlPC;
}

struct UnixEHEnumState
{
    PTR_UInt8 pMethodStartAddress;
    PTR_UInt8 pEHInfo;
    uint32_t uClause;
    uint32_t nClauses;
};

// Ensure that UnixEHEnumState fits into the space reserved by EHEnumState
static_assert(sizeof(UnixEHEnumState) <= sizeof(EHEnumState), "UnixEHEnumState too big");

bool UnixNativeCodeManager::EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumStateOut)
{
    assert(pMethodInfo != NULL);
    assert(pMethodStartAddress != NULL);
    assert(pEHEnumStateOut != NULL);

    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;

    PTR_UInt8 p = pNativeMethodInfo->pMainLSDA;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    // return if there is no EH info associated with this method
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) == 0)
    {
        return false;
    }

    UnixEHEnumState * pEnumState = (UnixEHEnumState *)pEHEnumStateOut;

    *pMethodStartAddress = pNativeMethodInfo->pMethodStartAddress;

    pEnumState->pMethodStartAddress = dac_cast<PTR_UInt8>(pNativeMethodInfo->pMethodStartAddress);
    pEnumState->pEHInfo = dac_cast<PTR_UInt8>(p + *dac_cast<PTR_Int32>(p));
    pEnumState->uClause = 0;
    pEnumState->nClauses = VarInt::ReadUnsigned(pEnumState->pEHInfo);

    return true;
}

bool UnixNativeCodeManager::EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClauseOut)
{
    assert(pEHEnumState != NULL);
    assert(pEHClauseOut != NULL);

    UnixEHEnumState * pEnumState = (UnixEHEnumState *)pEHEnumState;
    if (pEnumState->uClause >= pEnumState->nClauses)
    {
        return false;
    }

    pEnumState->uClause++;

    pEHClauseOut->m_tryStartOffset = VarInt::ReadUnsigned(pEnumState->pEHInfo);

    uint32_t tryEndDeltaAndClauseKind = VarInt::ReadUnsigned(pEnumState->pEHInfo);
    pEHClauseOut->m_clauseKind = (EHClauseKind)(tryEndDeltaAndClauseKind & 0x3);
    pEHClauseOut->m_tryEndOffset = pEHClauseOut->m_tryStartOffset + (tryEndDeltaAndClauseKind >> 2);

    // For each clause, we have up to 4 integers:
    //      1)  try start offset
    //      2)  (try length << 2) | clauseKind
    //      3)  if (typed || fault || filter)    { handler start offset }
    //      4a) if (typed)                       { type RVA }
    //      4b) if (filter)                      { filter start offset }
    //
    // The first two integers have already been decoded

    switch (pEHClauseOut->m_clauseKind)
    {
    case EH_CLAUSE_TYPED:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + VarInt::ReadUnsigned(pEnumState->pEHInfo);

        // Read target type
        {
            // @TODO: Compress EHInfo using type table index scheme
            // https://github.com/dotnet/corert/issues/972
            int32_t typeRelAddr = *((PTR_Int32&)pEnumState->pEHInfo);
            pEHClauseOut->m_pTargetType = dac_cast<PTR_VOID>(pEnumState->pEHInfo + typeRelAddr);
            pEnumState->pEHInfo += 4;
        }
        break;
    case EH_CLAUSE_FAULT:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        break;
    case EH_CLAUSE_FILTER:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        pEHClauseOut->m_filterAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        break;
    default:
        UNREACHABLE_MSG("unexpected EHClauseKind");
    }

    return true;
}

PTR_VOID UnixNativeCodeManager::GetOsModuleHandle()
{
    return (PTR_VOID)m_moduleBase;
}

PTR_VOID UnixNativeCodeManager::GetMethodStartAddress(MethodInfo * pMethodInfo)
{
    UnixNativeMethodInfo * pNativeMethodInfo = (UnixNativeMethodInfo *)pMethodInfo;
    return pNativeMethodInfo->pMethodStartAddress;
}

void * UnixNativeCodeManager::GetClasslibFunction(ClasslibFunctionId functionId)
{
    uint32_t id = (uint32_t)functionId;

    if (id >= m_nClasslibFunctions)
    {
        return nullptr;
    }

    return m_pClasslibFunctions[id];
}

PTR_VOID UnixNativeCodeManager::GetAssociatedData(PTR_VOID ControlPC)
{
    UnixNativeMethodInfo methodInfo;
    if (!FindMethodInfo(ControlPC, (MethodInfo*)&methodInfo))
        return NULL;

    PTR_UInt8 p = methodInfo.pMainLSDA;

    uint8_t unwindBlockFlags = *p++;
    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) == 0)
        return NULL;

    return dac_cast<PTR_VOID>(p + *dac_cast<PTR_Int32>(p));
}

extern "C" void RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);
extern "C" bool RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange);

extern "C"
bool RhRegisterOSModule(void * pModule,
                        void * pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                        void * pvUnboxingStubsStartRange, uint32_t cbUnboxingStubsRange,
                        void ** pClasslibFunctions, uint32_t nClasslibFunctions)
{
    NewHolder<UnixNativeCodeManager> pUnixNativeCodeManager = new (nothrow) UnixNativeCodeManager((TADDR)pModule,
        pvManagedCodeStartRange, cbManagedCodeRange,
        pClasslibFunctions, nClasslibFunctions);

    if (pUnixNativeCodeManager == nullptr)
        return false;

    RegisterCodeManager(pUnixNativeCodeManager, pvManagedCodeStartRange, cbManagedCodeRange);

    if (!RegisterUnboxingStubs(pvUnboxingStubsStartRange, cbUnboxingStubsRange))
    {
        return false;
    }

    pUnixNativeCodeManager.SuppressRelease();

    return true;
}
