// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/****************************************************************************/
/*                              gccover.cpp                                 */
/****************************************************************************/

/* This file holds code that is designed to test GC pointer tracking in
   fully interruptible code.  We basically do a GC everywhere we can in
   jitted code
 */
/****************************************************************************/


#include "common.h"

#ifdef HAVE_GCCOVER

#pragma warning(disable:4663)

#include "eeconfig.h"
#include "gms.h"
#include "utsem.h"
#include "gccover.h"
#include "virtualcallstub.h"
#include "threadsuspend.h"

#if defined(TARGET_AMD64) || defined(TARGET_ARM)
#include "gcinfodecoder.h"
#endif

#include "disassembler.h"

/****************************************************************************/

MethodDesc* AsMethodDesc(size_t addr);
static PBYTE getTargetOfCall(PBYTE instrPtr, PCONTEXT regs, PBYTE*nextInstr);
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
static void replaceSafePointInstructionWithGcStressInstr(UINT32 safePointOffset, LPVOID codeStart);
static bool replaceInterruptibleRangesWithGcStressInstr (UINT32 startOffset, UINT32 stopOffset, LPVOID codeStart);
#endif

// There is a call target instruction, try to find the MethodDesc for where target points to.
// Returns nullptr if it can't find it.
static MethodDesc* getTargetMethodDesc(PCODE target)
{
    MethodDesc* targetMD = ExecutionManager::GetCodeMethodDesc(target);
    if (targetMD != nullptr)
    {
        // It is JIT/NGened call.
        return targetMD;
    }

    auto stubKind = RangeSectionStubManager::GetStubKind(target);

    if ((stubKind == STUB_CODE_BLOCK_VSD_DISPATCH_STUB) ||
        (stubKind == STUB_CODE_BLOCK_VSD_RESOLVE_STUB) ||
        (stubKind == STUB_CODE_BLOCK_VSD_LOOKUP_STUB) ||
        (stubKind == STUB_CODE_BLOCK_VSD_VTABLE_STUB))
    {
        VirtualCallStubManager *pVSDStubManager = VirtualCallStubManager::FindStubManager(target, &stubKind);
        if (pVSDStubManager != NULL)
        {
            // It is a VSD stub manager.
            DispatchToken token(VirtualCallStubManager::GetTokenFromStubQuick(pVSDStubManager, target, stubKind));
            _ASSERTE(token.IsValid());
            return VirtualCallStubManager::GetInterfaceMethodDescFromToken(token);
        }
    }

    if (stubKind == STUB_CODE_BLOCK_PRECODE)
    {
        // The address looks like a value stub, try to get the method descriptor.
        return MethodDesc::GetMethodDescFromStubAddr(target, TRUE);
    }

    if (stubKind == STUB_CODE_BLOCK_STUBPRECODE)
    {
        return (MethodDesc*)((StubPrecode*)PCODEToPINSTR(target))->GetMethodDesc();
    }

    if (stubKind == STUB_CODE_BLOCK_FIXUPPRECODE)
    {
        if (!FixupPrecode::IsFixupPrecodeByASM(target))
        {
            // If the target slot points to the fixup part of the stub, the actual
            // stub starts FixupPrecode::FixupCodeOffset bytes below the target,
            // so we need to compensate for it.
            target -= FixupPrecode::FixupCodeOffset;
            if (!FixupPrecode::IsFixupPrecodeByASM(target))
            {
                _ASSERTE(!"Invalid FixupPrecode address"); // We should never get other precode type here
                return nullptr;
            }
        }

        return (MethodDesc*)((FixupPrecode*)PCODEToPINSTR(target))->GetMethodDesc();
    }

    return nullptr;
}

bool IsGcCoverageInterruptInstruction(PBYTE instrPtr)
{
    UINT32 instrVal;

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    instrVal = *reinterpret_cast<UINT32*>(instrPtr);
#elif defined(TARGET_ARM)
    size_t instrLen = GetARMInstructionLength(instrPtr);
    if (instrLen == 2)
    {
        instrVal = *reinterpret_cast<UINT16*>(instrPtr);
    }
    else
    {
        instrVal = *reinterpret_cast<UINT32*>(instrPtr);
    }
#else // x64 and x86
    instrVal = *instrPtr;
#endif

    return IsGcCoverageInterruptInstructionVal(instrVal);
}

bool IsOriginalInstruction(PBYTE instrPtr, GCCoverageInfo* gcCover, DWORD offset)
{
#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    UINT32 instrVal = *reinterpret_cast<UINT32*>(instrPtr);
    UINT32 origInstrVal = *reinterpret_cast<UINT32*>(gcCover->savedCode + offset);
    return (instrVal == origInstrVal);
#elif defined(TARGET_ARM)
    size_t instrLen = GetARMInstructionLength(instrPtr);
    if (instrLen == 2)
    {
        UINT16 instrVal = *reinterpret_cast<UINT16*>(instrPtr);
        UINT16 origInstrVal = *reinterpret_cast<UINT16*>(gcCover->savedCode + offset);
        return (instrVal == origInstrVal);
    }
    else
    {
        _ASSERTE(instrLen == 4);
        UINT32 instrVal = *reinterpret_cast<UINT32*>(instrPtr);
        UINT32 origInstrVal = *reinterpret_cast<UINT32*>(gcCover->savedCode + offset);
        return (instrVal == origInstrVal);
    }
#else // x64 and x86
    UINT8 instrVal = *reinterpret_cast<UINT8*>(instrPtr);
    UINT8 origInstrVal = gcCover->savedCode[offset];
    return (instrVal == origInstrVal);
#endif
}


void SetupAndSprinkleBreakpoints(
    NativeCodeVersion               nativeCodeVersion,
    EECodeInfo                    * pCodeInfo,
    IJitManager::MethodRegionInfo   methodRegionInfo,
    BOOL                            fZapped
    )
{
    _ASSERTE(!nativeCodeVersion.IsNull());

    // Allocate room for the GCCoverageInfo and copy of the method instructions
    MethodDesc *pMD = nativeCodeVersion.GetMethodDesc();
    size_t memSize = sizeof(GCCoverageInfo) + methodRegionInfo.hotSize + methodRegionInfo.coldSize;
    GCCoverageInfo* gcCover = (GCCoverageInfo*)(void*) pMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocAlignedMem(memSize, CODE_SIZE_ALIGN);

    memset(gcCover, 0, sizeof(GCCoverageInfo));

    gcCover->methodRegion      = methodRegionInfo;
    gcCover->codeMan           = pCodeInfo->GetCodeManager();
    gcCover->gcInfoToken       = pCodeInfo->GetGCInfoToken();
    gcCover->callerThread      = 0;
    gcCover->doingEpilogChecks = true;

    gcCover->SprinkleBreakpoints(gcCover->savedCode,
                                 gcCover->methodRegion.hotStartAddress,
                                 gcCover->methodRegion.hotSize,
                                 0,
                                 fZapped);

    // This is not required for ARM* as the above call does the work for both hot & cold regions
#if !defined(TARGET_ARM) && !defined(TARGET_ARM64)  && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    if (gcCover->methodRegion.coldSize != 0)
    {
        gcCover->SprinkleBreakpoints(gcCover->savedCode + gcCover->methodRegion.hotSize,
                                     gcCover->methodRegion.coldStartAddress,
                                     gcCover->methodRegion.coldSize,
                                     gcCover->methodRegion.hotSize,
                                     fZapped);
    }
#endif

    nativeCodeVersion.SetGCCoverageInfo(gcCover);
}

void SetupAndSprinkleBreakpointsForJittedMethod(NativeCodeVersion               nativeCodeVersion,
                                                PCODE                           codeStart
                                               )
{
    _ASSERTE(!nativeCodeVersion.IsNull());

    EECodeInfo codeInfo(codeStart);
    _ASSERTE(codeInfo.IsValid());
    _ASSERTE(codeInfo.GetRelOffset() == 0);

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    _ASSERTE(PCODEToPINSTR(codeStart) == methodRegionInfo.hotStartAddress);

#ifdef _DEBUG
    if (!g_pConfig->SkipGCCoverage(nativeCodeVersion.GetMethodDesc()->GetModule()->GetSimpleName()))
#endif
    SetupAndSprinkleBreakpoints(nativeCodeVersion,
                                &codeInfo,
                                methodRegionInfo,
                                FALSE
                               );
}

/****************************************************************************/
/* called when a method is first jitted when GCStress level 4 or 8 is on */

void SetupGcCoverage(NativeCodeVersion nativeCodeVersion, BYTE* methodStartPtr)
{
    _ASSERTE(!nativeCodeVersion.IsNull());

#ifdef _DEBUG
    if (!g_pConfig->ShouldGcCoverageOnMethod(nativeCodeVersion.GetMethodDesc()->m_pszDebugMethodName)) {
        return;
    }
#endif

    // Ideally we would assert here that m_GcCover is NULL.
    //
    // However, we can't do that (at least not yet), because we may
    // invoke this method more than once on a given
    // MethodDesc. Examples include prejitted methods and rejitted
    // methods.
    //
    // In the prejit case, we can't safely re-instrument an already
    // instrumented method. By bailing out here, we will use the
    // original instrumentation, which should still be valid as
    // the method code has not changed.
    //
    // In the rejit case, the old method code may still be active and
    // instrumented, so we need to preserve that gc cover info.  By
    // bailing out here we will skip instrumenting the rejitted native
    // code, and since the rejitted method does not get instrumented
    // we should be able to tolerate that the gc cover info does not
    // match.
    if (nativeCodeVersion.GetGCCoverageInfo() != NULL)
    {
        return;
    }

    PCODE codeStart = (PCODE) methodStartPtr;
    SetupAndSprinkleBreakpointsForJittedMethod(nativeCodeVersion, codeStart);
}

void ReplaceInstrAfterCall(PBYTE instrToReplace, MethodDesc* callMD)
{
    ReturnKind returnKind = callMD->GetReturnKind(true);
    _ASSERTE(IsValidReturnKind(returnKind));

    bool ispointerKind = IsPointerReturnKind(returnKind);
#ifdef TARGET_ARM
    size_t instrLen = GetARMInstructionLength(instrToReplace);
    bool protectReturn = ispointerKind;
    if (protectReturn)
        if (instrLen == 2)
            *(WORD*)instrToReplace = INTERRUPT_INSTR_PROTECT_RET;
        else
            *(DWORD*)instrToReplace = INTERRUPT_INSTR_PROTECT_RET_32;
    else
        if (instrLen == 2)
            *(WORD*)instrToReplace = INTERRUPT_INSTR;
        else
            *(DWORD*)instrToReplace = INTERRUPT_INSTR_32;
#elif defined(TARGET_ARM64)
    bool protectReturn = ispointerKind;
    if (protectReturn)
        *(DWORD*)instrToReplace = INTERRUPT_INSTR_PROTECT_RET;
    else
        *(DWORD*)instrToReplace = INTERRUPT_INSTR;
#elif defined(TARGET_AMD64) || defined(TARGET_X86)


    if (ispointerKind)
    {
        bool protectRegister[2] = { false, false };

        bool moreRegisters = false;

        ReturnKind fieldKind1 = ExtractRegReturnKind(returnKind, 0, moreRegisters);
        if (IsPointerFieldReturnKind(fieldKind1))
        {
            protectRegister[0] = true;
        }
        if (moreRegisters)
        {
            ReturnKind fieldKind2 = ExtractRegReturnKind(returnKind, 1, moreRegisters);
            if (IsPointerFieldReturnKind(fieldKind2))
            {
                protectRegister[1] = true;
            }
        }
        _ASSERTE(!moreRegisters);

        if (protectRegister[0] && !protectRegister[1])
        {
            *instrToReplace = INTERRUPT_INSTR_PROTECT_FIRST_RET;
        }
        else
        {
#if !defined(TARGET_AMD64) || !defined(TARGET_UNIX)
            _ASSERTE(!"Not expected multi reg return with pointers.");
#endif // !TARGET_AMD64 || !TARGET_UNIX
            if (!protectRegister[0] && protectRegister[1])
            {
                *instrToReplace = INTERRUPT_INSTR_PROTECT_SECOND_RET;
            }
            else
            {
                _ASSERTE(protectRegister[0] && protectRegister[1]);
                *instrToReplace = INTERRUPT_INSTR_PROTECT_BOTH_RET;
            }
        }
    }
    else
    {
        *instrToReplace = INTERRUPT_INSTR;
    }
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool protectReturn = ispointerKind;
    if (protectReturn)
        *(DWORD*)instrToReplace = INTERRUPT_INSTR_PROTECT_RET;
    else
        *(DWORD*)instrToReplace = INTERRUPT_INSTR;
#else
    _ASSERTE(!"not implemented for platform");
#endif
}

#ifdef TARGET_AMD64

class GCCoverageRangeEnumerator
{
private:

    ICodeManager *m_pCodeManager;
    GCInfoToken m_pvGCTable;
    BYTE *m_codeStart;
    BYTE *m_codeEnd;
    BYTE *m_curFuncletEnd;
    BYTE *m_nextFunclet;


    BYTE* GetNextFunclet ()
    {
        if (m_nextFunclet == NULL)
            return m_codeEnd;

        BYTE *pCurFunclet = (BYTE*)EECodeInfo::findNextFunclet(m_nextFunclet, m_codeEnd - m_nextFunclet, (LPVOID*)&m_curFuncletEnd);
        m_nextFunclet = (pCurFunclet != NULL) ? m_curFuncletEnd : NULL;

        if (pCurFunclet == NULL)
            return m_codeEnd;

        LOG((LF_JIT, LL_INFO1000, "funclet range %p-%p\n", pCurFunclet, m_curFuncletEnd));

        //
        // workaround - adjust the funclet end address to exclude uninterruptible
        // code at the end of each funclet.  The jit currently puts data like
        // jump tables in the code portion of the allocation, instead of the
        // read-only portion.
        //
        // TODO: If the entire range is uninterruptible, we should skip the
        // entire funclet.
        //
        unsigned ofsLastInterruptible = m_pCodeManager->FindEndOfLastInterruptibleRegion(
                static_cast<unsigned int>(pCurFunclet     - m_codeStart),
                static_cast<unsigned int>(m_curFuncletEnd - m_codeStart),
                m_pvGCTable);

        if (ofsLastInterruptible)
        {
            m_curFuncletEnd = m_codeStart + ofsLastInterruptible;
            LOG((LF_JIT, LL_INFO1000, "adjusted end to %p\n", m_curFuncletEnd));
        }

        return pCurFunclet;
    }


public:

    GCCoverageRangeEnumerator (ICodeManager *pCodeManager, GCInfoToken pvGCTable, BYTE *codeStart, SIZE_T codeSize)
    {
        m_pCodeManager = pCodeManager;
        m_pvGCTable = pvGCTable;
        m_codeStart = codeStart;
        m_codeEnd = codeStart + codeSize;
        m_nextFunclet = codeStart;

        GetNextFunclet();
    }

    // Checks that the given pointer is inside of a range where gc should be
    // tested.  If not, increments the pointer until it is, and returns the
    // new pointer.
    BYTE *EnsureInRange (BYTE *cur)
    {
        if (cur >= m_curFuncletEnd)
        {
            cur = GetNextFunclet();
        }

        return cur;
    }

    BYTE *SkipToNextRange ()
    {
        return GetNextFunclet();
    }
};

#endif // TARGET_AMD64

/****************************************************************************/
/* sprinkle interrupt instructions that will stop on every GCSafe location
   regionOffsetAdj - Represents the offset of the current region
                     from the beginning of the method (is 0 for hot region)
*/

void GCCoverageInfo::SprinkleBreakpoints(
        BYTE * saveAddr,
        PCODE  pCode,
        size_t codeSize,
        size_t regionOffsetAdj,
        BOOL   fZapped)
{
#if (defined(TARGET_X86) || defined(TARGET_AMD64)) && USE_DISASSEMBLER

    BYTE * codeStart = (BYTE *)pCode;
    ExecutableWriterHolderNoLog<BYTE> codeWriterHolder;
    size_t writeableOffset;

    memcpy(saveAddr, codeStart, codeSize);

    // For prejitted code we have to remove the write-protect on the code page
    if (fZapped)
    {
        DWORD oldProtect;
        ClrVirtualProtect(codeStart, codeSize, PAGE_EXECUTE_READWRITE, &oldProtect);
        writeableOffset = 0;
    }
    else
    {
        codeWriterHolder.AssignExecutableWriterHolder(codeStart, codeSize);
        writeableOffset = codeWriterHolder.GetRW() - codeStart;
    }

    PBYTE cur;
    BYTE* codeEnd = codeStart + codeSize;

    EECodeInfo codeInfo((PCODE)codeStart);

    static ConfigDWORD fGcStressOnDirectCalls; // ConfigDWORD must be a static variable


#ifdef TARGET_AMD64
    GCCoverageRangeEnumerator rangeEnum(codeMan, gcInfoToken, codeStart, codeSize);

    GcInfoDecoder safePointDecoder(gcInfoToken, (GcInfoDecoderFlags)0, 0);
    bool fSawPossibleSwitch = false;
#endif

    cur = codeStart;
    Disassembler disassembler;

    // When we find a direct call instruction and we are partially-interruptible
    //  we determine the target and place a breakpoint after the call
    //  to simulate the hijack
    // However, we need to wait until we disassemble the instruction
    //  after the call in order to put the breakpoint or we'll mess up
    //  the disassembly
    // This variable is non-null if the previous instruction was a direct call,
    //  and we have found it's target MethodDesc
    MethodDesc* prevDirectCallTargetMD = NULL;

    /* TODO. Simulating the hijack could cause problems in cases where the
       return register is not always a valid GC ref on the return offset.
       That could happen if we got to the return offset via a branch
       and not via return from the preceding call. However, this has not been
       an issue so far.

       Example:
        mov eax, someval
        test eax, eax
        jCC AFTERCALL
        call MethodWhichReturnsGCobject // return value is not used
        AFTERCALL:
    */

    while (cur < codeEnd)
    {
        _ASSERTE(*cur != INTERRUPT_INSTR && *cur != INTERRUPT_INSTR_CALL);

        MethodDesc* targetMD = NULL;
        InstructionType instructionType;
        size_t len = disassembler.DisassembleInstruction(cur, codeEnd - cur, &instructionType);

#ifdef TARGET_AMD64
        // REVISIT_TODO apparently the jit does not use the entire RUNTIME_FUNCTION range
        // for code.  It uses some for switch tables.  Because the first few offsets
        // may be decodable as instructions, we can't reason about where we should
        // encounter invalid instructions.  However, we do not want to silently skip
        // large chunks of methods just because the JIT started emitting a new
        // instruction, so only assume it is a switch table if we've seen the switch
        // code (an indirect unconditional jump)
        if ((len == 0) && fSawPossibleSwitch)
        {
            LOG((LF_JIT, LL_WARNING, "invalid instruction at %p (possibly start of switch table)\n", cur));
            cur = rangeEnum.SkipToNextRange();
            prevDirectCallTargetMD = NULL;
            fSawPossibleSwitch = false;
            continue;
        }
#endif

        _ASSERTE(len > 0);
        _ASSERTE(len <= (size_t)(codeEnd-cur));

        switch(instructionType)
        {
        case InstructionType::Call_IndirectUnconditional:
#ifdef TARGET_AMD64
            if(!safePointDecoder.AreSafePointsInterruptible() && 
                safePointDecoder.IsSafePoint((UINT32)(cur + len - codeStart + regionOffsetAdj)))
#endif
            {
               *(cur + writeableOffset) = INTERRUPT_INSTR_CALL;        // return value.  May need to protect
            }
            break;

        case InstructionType::Call_DirectUnconditional:
            if(fGcStressOnDirectCalls.val(CLRConfig::INTERNAL_GcStressOnDirectCalls))
            {
#ifdef TARGET_AMD64
                if(!safePointDecoder.AreSafePointsInterruptible() &&
                   safePointDecoder.IsSafePoint((UINT32)(cur + len - codeStart + regionOffsetAdj)))
#endif
                {
                    PBYTE nextInstr;
                    PBYTE target = getTargetOfCall(cur, NULL, &nextInstr);

                    if (target != 0)
                    {
                        targetMD = getTargetMethodDesc((PCODE)target);
                    }
                }
            }
            break;

#ifdef TARGET_AMD64
        case InstructionType::Branch_IndirectUnconditional:
            fSawPossibleSwitch = true;
            break;
#endif

        default:
            // Clang issues an error saying that some enum values are not handled in the switch, that's intended
            break;
        }

        if (prevDirectCallTargetMD != 0)
        {
            ReplaceInstrAfterCall(cur + writeableOffset, prevDirectCallTargetMD);
        }

        // For fully interruptible code, we end up whacking every instruction
        // to INTERRUPT_INSTR.  For non-fully interruptible code, we end
        // up only touching the call instructions (specially so that we
        // can really do the GC on the instruction just after the call).
        size_t dwRelOffset = (cur - codeStart) + regionOffsetAdj;
        _ASSERTE(FitsIn<DWORD>(dwRelOffset));
        if (codeMan->IsGcSafe(&codeInfo, static_cast<DWORD>(dwRelOffset)))
        {
            *(cur + writeableOffset) = INTERRUPT_INSTR;
        }
#ifdef TARGET_AMD64
        else if (safePointDecoder.AreSafePointsInterruptible() &&
            safePointDecoder.IsSafePoint((UINT32)dwRelOffset))
        {
            *(cur + writeableOffset) = INTERRUPT_INSTR;
        }
#endif

#ifdef TARGET_X86
        // we will whack every instruction in the prolog and epilog to make certain
        // our unwinding logic works there.
        if (codeMan->IsInPrologOrEpilog((cur - codeStart) + (DWORD)regionOffsetAdj, gcInfoToken, NULL))
        {
            *(cur + writeableOffset) = INTERRUPT_INSTR;
        }
#endif

        // If we couldn't find the method desc targetMD is zero
        prevDirectCallTargetMD = targetMD;

        cur += len;

#ifdef TARGET_AMD64
        PBYTE newCur = rangeEnum.EnsureInRange(cur);
        if(newCur != cur)
        {
            prevDirectCallTargetMD = NULL;
            cur = newCur;
            fSawPossibleSwitch = false;
        }
#endif
    }

    // If we are not able to place an interrupt at the first instruction, this means that
    // we are partially interruptible with no prolog.  Just don't bother to do the
    // the epilog checks, since the epilog will be trivial (a single return instr)
    assert(codeSize > 0);
    if ((regionOffsetAdj==0) && (*codeStart != INTERRUPT_INSTR))
        doingEpilogChecks = false;

#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    //Save the method code from hotRegion
    memcpy(saveAddr, (BYTE*)methodRegion.hotStartAddress, methodRegion.hotSize);

    if (methodRegion.coldSize > 0)
    {
        //Save the method code from coldRegion
        memcpy(saveAddr+methodRegion.hotSize, (BYTE*)methodRegion.coldStartAddress, methodRegion.coldSize);
    }

    // For prejitted code we have to remove the write-protect on the code page
    if (fZapped)
    {
        DWORD oldProtect;
        ClrVirtualProtect((BYTE*)methodRegion.hotStartAddress, methodRegion.hotSize, PAGE_EXECUTE_READWRITE, &oldProtect);

        if (methodRegion.coldSize > 0)
        {
            ClrVirtualProtect((BYTE*)methodRegion.coldStartAddress, methodRegion.coldSize, PAGE_EXECUTE_READWRITE, &oldProtect);
        }
    }

    GcInfoDecoder safePointDecoder(gcInfoToken, (GcInfoDecoderFlags)0, 0);

    assert(methodRegion.hotSize > 0);

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    safePointDecoder.EnumerateSafePoints(&replaceSafePointInstructionWithGcStressInstr,this);
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    safePointDecoder.EnumerateInterruptibleRanges(&replaceInterruptibleRangesWithGcStressInstr, this);

    FlushInstructionCache(GetCurrentProcess(), (BYTE*)methodRegion.hotStartAddress, methodRegion.hotSize);

    if (methodRegion.coldSize > 0)
    {
        FlushInstructionCache(GetCurrentProcess(), (BYTE*)methodRegion.coldStartAddress, methodRegion.coldSize);
    }

#else
    _ASSERTE(!"not implemented for platform");
#endif // TARGET_X86
}

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#ifdef TARGET_RISCV64
enum
{
    REG_RA = 1,
    JAL = 0x6f,
    JALR = 0x67,
};
#endif

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

void replaceSafePointInstructionWithGcStressInstr(UINT32 safePointOffset, LPVOID pGCCover)
{
    PCODE pCode = NULL;
    IJitManager::MethodRegionInfo *ptr = &(((GCCoverageInfo*)pGCCover)->methodRegion);

    //Get code address from offset
    if (safePointOffset < ptr->hotSize)
        pCode = ptr->hotStartAddress + safePointOffset;
    else if(safePointOffset - ptr->hotSize < ptr->coldSize)
    {
        SIZE_T coldOffset = safePointOffset - ptr->hotSize;
        pCode = ptr->coldStartAddress + coldOffset;
    }
    else
    {
        //For some methods( eg MCCTest.MyClass.GetSum2 in test file jit\jit64\mcc\interop\mcc_i07.il) gcinfo points to a safepoint
        //beyond the length of the method. So commenting the below assert.
        //_ASSERTE(safePointOffset - ptr->hotSize < ptr->coldSize);
        return;
    }

    PBYTE instrPtr = (BYTE*)PCODEToPINSTR(pCode);

    // For code sequences of the type
    // BL func1
    // BL func2    // Safe point 1
    // mov r1 r0  // Safe point 2
    // Both the above safe points instruction must be replaced with gcStress instruction.
    // However as the first safe point is already replaced with gcstress instruction, decoding of the call
    // instruction will fail when processing for the 2nd safe point. Therefore saved instruction must be used instead of
    // instrPtr for decoding the call instruction.
    PBYTE savedInstrPtr = ((GCCoverageInfo*)pGCCover)->savedCode + safePointOffset;

    //Determine if instruction before the safe point is call using immediate (BLX Imm)  or call by register (BLX Rm)
    BOOL  instructionIsACallThroughRegister = FALSE;
    BOOL instructionIsACallThroughImmediate = FALSE;

#if defined(TARGET_ARM)

    // POSSIBLE BUG: Note that we are looking backwards by 2 or 4 bytes, looking for particular call instruction encodings.
    // However, we don't know if the previous instruction is 2 bytes or 4 bytes. Looking back 2 bytes could be looking into
    // the middle of a 4-byte instruction. The only safe way to do this is by walking forward from the first instruction of
    // the function.

    // call by register instruction is two bytes (BL<c> Reg T1 encoding)
    WORD instr = *((WORD*)savedInstrPtr - 1);
    instr = instr & 0xff87;
    if ((instr ^ 0x4780) == 0)
    {
        // It is call by register
        instructionIsACallThroughRegister = TRUE;
    }
    else
    {
        // call using immediate instructions are 4 bytes (BL<c> <label> T1 encoding)
        instr = *((WORD*)savedInstrPtr - 2);
        instr = instr & 0xf800;
        if ((instr ^ 0xf000) == 0)
        {
            if ((*(((WORD*)savedInstrPtr) - 1) & 0xd000) == 0xd000)
            {
                // It is call by immediate
                instructionIsACallThroughImmediate = TRUE;
            }
        }
    }

#elif defined(TARGET_ARM64)
    DWORD instr = *((DWORD*)savedInstrPtr - 1);

    // Is the call through a register or an immediate offset
    // BL
    // Encoding: 0x94000000 & [imm26]
    if ((instr & 0xFC000000) == 0x94000000)
    {
        instructionIsACallThroughImmediate = TRUE;
    }
    // BLR
    // Encoding: 0xD63F0000 & (Rn<<5)
    else if ((instr & 0xFFFFFC1F) == 0xD63F0000)
    {
        instructionIsACallThroughRegister = TRUE;
    }
#elif defined(TARGET_LOONGARCH64)
    DWORD instr = *((DWORD*)savedInstrPtr - 1);

    // Is the call through a register or an immediate offset
    // bl
    if (((instr >> 26) & 0x3F) == 0x15)
    {
        instructionIsACallThroughImmediate = TRUE;
    }
    // jirl ra, target, offs
    else if ((((instr >> 26) & 0x3F) == 0x13) && ((instr & 0x1F) == 1))
    {
        instructionIsACallThroughRegister = TRUE;
    }
#elif defined(TARGET_RISCV64)
    const INT32 instr = *((INT32*)savedInstrPtr - 1);

    int opcode = instr & ~(-1 << 7);
    int linkReg = (instr >> 7) & ~(-1 << 5);

    if ((opcode == JAL) && (linkReg == REG_RA))
        instructionIsACallThroughImmediate = TRUE;
    else if ((opcode == JALR) && (linkReg == REG_RA))
        instructionIsACallThroughRegister = TRUE;
#endif  // _TARGET_XXXX_

    // safe point must always be after a call instruction
    // and cannot be both call by register & immediate
    // The safe points are also marked at jump calls( a special variant of
    // tail call). However that call site will never appear on the stack.
    // So commenting the assert for now. As for such places the previous
    // instruction will not be a call instruction.
    //_ASSERTE(instructionIsACallThroughRegister ^ instructionIsACallThroughImmediate);

#if defined(TARGET_ARM)
    size_t instrLen = sizeof(WORD);
#else
    size_t instrLen = sizeof(DWORD);
#endif

    ExecutableWriterHolder<BYTE> instrPtrWriterHolder(instrPtr - instrLen, 2 * instrLen);
    if(instructionIsACallThroughRegister)
    {
        // If it is call by register then cannot know MethodDesc so replace the call instruction with illegal instruction
        // safe point will be replaced with appropriate illegal instruction at execution time when reg value is known
#if defined(TARGET_ARM)
        *((WORD*)instrPtrWriterHolder.GetRW()) = INTERRUPT_INSTR_CALL;
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        *((DWORD*)instrPtrWriterHolder.GetRW()) = INTERRUPT_INSTR_CALL;
#endif // _TARGET_XXXX_
    }
    else if(instructionIsACallThroughImmediate)
    {
        // If it is call by immediate then find the methodDesc
        PBYTE nextInstr;
        PBYTE target = getTargetOfCall((PBYTE)((WORD*)savedInstrPtr-2), NULL, &nextInstr);

        if (target != 0)
        {
            //Target is calculated wrt the saved instruction pointer
            //Find the real target wrt the real instruction pointer
            int delta = static_cast<int>(target - savedInstrPtr);
            target = delta + instrPtr;

            MethodDesc* targetMD = getTargetMethodDesc((PCODE)target);

            if (targetMD != 0)
            {

                // The instruction about to be replaced cannot already be a gcstress instruction
                _ASSERTE(!IsGcCoverageInterruptInstruction(instrPtr));

                //
                // When applying GC coverage breakpoints at native image load time, the code here runs
                // before eager fixups are applied for the module being loaded.  The direct call target
                // never requires restore, however it is possible that it is initially in an invalid state
                // and remains invalid until one or more eager fixups are applied.
                //
                // ReplaceInstrAfterCall consults the method signature, meaning it consults the
                // metadata in the owning module.  For generic instantiations stored in non-preferred
                // modules, reaching the owning module requires following the module override pointer for
                // the enclosing MethodTable.  In this case, the module override pointer is generally
                // invalid until an associated eager fixup is applied.
                //
                // In situations like this, ReplaceInstrAfterCall will try to dereference an
                // unresolved fixup and will AV.
                //
                // Given all of this, skip the ReplaceInstrAfterCall call by default to avoid
                // unexpected AVs.  This implies leaving out the GC coverage breakpoints for direct calls
                // unless DOTNET_GcStressOnDirectCalls=1 is explicitly set in the environment.
                //

                static ConfigDWORD fGcStressOnDirectCalls;

                if (fGcStressOnDirectCalls.val(CLRConfig::INTERNAL_GcStressOnDirectCalls))
                {
                    ReplaceInstrAfterCall(instrPtrWriterHolder.GetRW() + instrLen, targetMD);
                }
            }
        }
    }
}
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

//Replaces the provided interruptible range with corresponding 2 or 4 byte gcStress illegal instruction
bool replaceInterruptibleRangesWithGcStressInstr (UINT32 startOffset, UINT32 stopOffset, LPVOID pGCCover)
{
    PCODE pCode = NULL;
    PBYTE rangeStart = NULL;
    PBYTE rangeStop = NULL;

    //Interruptible range can span across hot & cold region
    int acrossHotRegion = 1; // 1 means range is not across end of hot region & 2 is when it is across end of hot region

    //Find the code addresses from offsets
    IJitManager::MethodRegionInfo *ptr = &(((GCCoverageInfo*)pGCCover)->methodRegion);
    if (startOffset < ptr->hotSize)
    {
        pCode = ptr->hotStartAddress + startOffset;
        rangeStart = (BYTE*)PCODEToPINSTR(pCode);

        if(stopOffset <= ptr->hotSize)
        {
            pCode = ptr->hotStartAddress + stopOffset;
            rangeStop = (BYTE*)PCODEToPINSTR(pCode);
        }
        else
        {
            //Interruptible range is spanning across hot & cold region
            pCode = ptr->hotStartAddress + ptr->hotSize;
            rangeStop = (BYTE*)PCODEToPINSTR(pCode);
            acrossHotRegion++;
        }
    }
    else
    {
        SIZE_T coldOffset = startOffset - ptr->hotSize;
        _ASSERTE(coldOffset < ptr->coldSize);
        pCode = ptr->coldStartAddress + coldOffset;
        rangeStart = (BYTE*)PCODEToPINSTR(pCode);

        coldOffset = stopOffset - ptr->hotSize;
        _ASSERTE(coldOffset <= ptr->coldSize);
        pCode = ptr->coldStartAddress + coldOffset;
        rangeStop = (BYTE*)PCODEToPINSTR(pCode);
    }

    // Need to do two iterations if interruptible range spans across hot & cold region
    while(acrossHotRegion--)
    {
        ExecutableWriterHolder<BYTE> instrPtrWriterHolder(rangeStart, rangeStop - rangeStart);
        PBYTE instrPtrRW =  instrPtrWriterHolder.GetRW();
        PBYTE rangeStopRW = instrPtrRW + (rangeStop - rangeStart);
        while(instrPtrRW < rangeStopRW)
        {
            // The instruction about to be replaced cannot already be a gcstress instruction
            _ASSERTE(!IsGcCoverageInterruptInstruction(instrPtrRW));
#if defined(TARGET_ARM)
            size_t instrLen = GetARMInstructionLength(instrPtrRW);

            if (instrLen == 2)
                *((WORD*)instrPtrRW)  = INTERRUPT_INSTR;
            else
            {
                *((DWORD*)instrPtrRW) = INTERRUPT_INSTR_32;
            }

            instrPtrRW += instrLen;
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            *((DWORD*)instrPtrRW) = INTERRUPT_INSTR;
            instrPtrRW += 4;
#endif // TARGET_XXXX_

        }

        if(acrossHotRegion)
        {
            //Set rangeStart & rangeStop for the second iteration
            _ASSERTE(acrossHotRegion==1);
            rangeStart = (BYTE*)PCODEToPINSTR(ptr->coldStartAddress);
            pCode = ptr->coldStartAddress + stopOffset - ptr->hotSize;
            rangeStop = (BYTE*)PCODEToPINSTR(pCode);
        }
    }
    return FALSE;
}
#endif // defined(TARGET_ARM) || defined(TARGET_ARM64)

static size_t getRegVal(unsigned regNum, PCONTEXT regs)
{
    return *getRegAddr(regNum, regs);
}

/****************************************************************************/
static PBYTE getTargetOfCall(PBYTE instrPtr, PCONTEXT regs, PBYTE* nextInstr) {

    BYTE sibindexadj = 0;
    BYTE baseadj = 0;
    WORD displace = 0;

    // In certain situations, the instruction bytes are read from a different
    // location than the actual bytes being executed.
    // When decoding the instructions of a method which is sprinkled with
    // TRAP instructions for GCStress, we decode the bytes from a copy
    // of the instructions stored before the traps-for-gc were inserted.
    // However, the PC-relative addressing/displacement of the CALL-target
    // will still be with respect to the currently executing PC.
    // So, if a register context is available, we pick the PC from it
    // (for address calculation purposes only).

    PBYTE PC = (regs) ? (PBYTE)GetIP(regs) : instrPtr;

#ifdef TARGET_ARM
    if((instrPtr[1] & 0xf0) == 0xf0) // direct call
    {
        int imm32 = GetThumb2BlRel24((UINT16 *)instrPtr);
        *nextInstr = instrPtr + 4;
        return PC + 4 + imm32;
    }
    else if(((instrPtr[1] & 0x47) == 0x47) & ((instrPtr[0] & 0x80) == 0x80)) // indirect call
    {
        *nextInstr = instrPtr + 2;
        unsigned int regnum = (instrPtr[0] & 0x78) >> 3;
        return (BYTE *)getRegVal(regnum, regs);
    }
    else
    {
        return 0; // Not a call.
    }
#elif defined(TARGET_ARM64)
   if (((*reinterpret_cast<DWORD*>(instrPtr)) & 0xFC000000) == 0x94000000)
   {
       // call through immediate
       int imm26 = ((*((DWORD*)instrPtr)) & 0x03FFFFFF)<<2;
       // SignExtend the immediate value.
       imm26 = (imm26 << 4) >> 4;
       *nextInstr = instrPtr + 4;
       return PC + imm26;
   }
   else if (((*reinterpret_cast<DWORD*>(instrPtr)) & 0xFFFFC1F) == 0xD63F0000)
   {
       // call through register
       *nextInstr = instrPtr + 4;
       unsigned int regnum = ((*(DWORD*)instrPtr) >> 5) & 0x1F;
       return (BYTE *)getRegVal(regnum, regs);
   }
   else
   {
       return 0; // Fail
   }
#elif defined(TARGET_LOONGARCH64)
    if ((((*reinterpret_cast<DWORD*>(instrPtr)) >> 26) & 0x3F) == 0x15)
    {
        int imm26 = (((*reinterpret_cast<DWORD*>(instrPtr)) & 0x3ff) << 16) | (((*reinterpret_cast<DWORD*>(instrPtr)) >> 10) & 0xffff);
        *nextInstr = instrPtr + 4;
        return PC + imm26;
    }
    else if (((((*reinterpret_cast<DWORD*>(instrPtr)) >> 26) & 0x3F) == 0x13) && (((*reinterpret_cast<DWORD*>(instrPtr)) & 0x1F) == 1))
    {
        // call through register
        *nextInstr = instrPtr + 4;

        assert((((*reinterpret_cast<DWORD*>(instrPtr)) >> 10) & 0xffff) == 0);
        unsigned int regnum = ((*reinterpret_cast<DWORD*>(instrPtr)) >> 5) & 0x1F;
        return (BYTE *)getRegVal(regnum, regs);
    }
    else
    {
        return 0; // Fail
    }
#elif defined(TARGET_RISCV64)
    INT32 instr = *reinterpret_cast<INT32*>(instrPtr);
    int opcode = instr & ~(-1 << 7);
    int linkReg = (instr >> 7) & ~(-1 << 5);

    if ((opcode == JAL) && (linkReg == REG_RA))
    {
        // call through immediate
        int imm = (instr >> 12);

        int bits12to19 = imm & ~(-1 << 8);
        imm >>= 8;
        int bit11 = imm & ~(-1 << 1);
        imm >>= 1;
        int bits1to10 = imm & ~(-1 << 10);
        imm >>= 10;
        int signBits = imm;

        int offset = (bits1to10 << 1) | (bit11 << 11) | (bits12to19 << 12) | (signBits << 20);

        *nextInstr = instrPtr + 4;
        return PC + offset;
    }
    else if ((opcode == JALR) && (linkReg == REG_RA))
    {
        // call through register
        *nextInstr = instrPtr + 4;  // TODO: adjust once we support "C" (compressed instructions)

        int offset = (instr >> 20);
        int jumpBaseReg = (instr >> 15) & ~(-1 << 5);
        size_t value = (getRegVal(jumpBaseReg, regs) + offset) & ~(size_t)1;
        return (BYTE *)value;
    }
#endif

#ifdef TARGET_AMD64

    if ((instrPtr[0] & 0xf0) == REX_PREFIX_BASE)
    {
        static_assert_no_msg(REX_SIB_BASE_EXT == REX_MODRM_RM_EXT);
        if (instrPtr[0] & REX_SIB_BASE_EXT)
            baseadj = 8;

        if (instrPtr[0] & REX_SIB_INDEX_EXT)
            sibindexadj = 8;

        instrPtr++;
    }

#endif // TARGET_AMD64

    if (instrPtr[0] == 0xE8) {  // Direct Relative Near
        *nextInstr = instrPtr + 5;

        size_t base = (size_t) PC + 5;

        INT32 displacement = (INT32) (
            ((UINT32)instrPtr[1]) +
            (((UINT32)instrPtr[2]) << 8) +
            (((UINT32)instrPtr[3]) << 16) +
            (((UINT32)instrPtr[4]) << 24)
            );

        // Note that the signed displacement is sign-extended
        //  to 64-bit on AMD64
        return((PBYTE)(base + (SSIZE_T)displacement));
    }

    if (instrPtr[0] == 0xFF) { // Indirect Absolute Near

        _ASSERTE(regs);

        BYTE mod = (instrPtr[1] & 0xC0) >> 6;
        BYTE rm  = (instrPtr[1] & 0x7);
        PBYTE result;

        switch (mod) {
        case 0:
        case 1:
        case 2:

            if (rm == 4) {

                //
                // Get values from the SIB byte
                //
                BYTE ss    = (instrPtr[2] & 0xC0) >> 6;
                BYTE index = (instrPtr[2] & 0x38) >> 3;
                BYTE base  = (instrPtr[2] & 0x7);

                //
                // Get starting value
                //
                if ((mod == 0) && (base == 5)) {
                    result = 0;
                } else {
                    result = (BYTE *)getRegVal(baseadj + base, regs);
                }

                //
                // Add in the [index]
                //
                if (index != 0x4) {
                    result = result + (getRegVal(sibindexadj + index, regs) << ss);
                }

                //
                // Finally add in the offset
                //
                if (mod == 0) {

                    if (base == 5) {
                        result = result + *((int *)&instrPtr[3]);
                        displace += 7;
                    } else {
                        displace += 3;
                    }

                } else if (mod == 1) {

                    result = result + *((char *)&instrPtr[3]);
                    displace += 4;

                } else { // == 2

                    result = result + *((int *)&instrPtr[3]);
                    displace += 7;

                }

            } else {

                //
                // Get the value we need from the register.
                //

                if ((mod == 0) && (rm == 5)) {
#ifdef TARGET_AMD64
                    // at this point instrPtr should be pointing at the beginning
                    // of the byte sequence for the call instruction.  the operand
                    // is a RIP-relative address from the next instruction, so to
                    // calculate the address of the next instruction we need to
                    // jump forward 6 bytes: 1 for the opcode, 1 for the ModRM byte,
                    // and 4 for the operand.  see AMD64 Programmer's Manual Vol 3.
                    result = PC + 6;
#else
                    result = 0;
#endif // TARGET_AMD64
                } else {
                    result = (PBYTE)getRegVal(baseadj + rm, regs);
                }

                if (mod == 0) {

                    if (rm == 5) {
                        result = result + *((int *)&instrPtr[2]);
                        displace += 6;
                    } else {
                        displace += 2;
                    }

                } else if (mod == 1) {

                    result = result + *((char *)&instrPtr[2]);
                    displace += 3;

                } else { // == 2

                    result = result + *((int *)&instrPtr[2]);
                    displace += 6;

                }

            }

            //
            // Now dereference thru the result to get the resulting IP.
            //
            result = (PBYTE)(*((PBYTE *)result));

            break;

        case 3:
        default:

            result = (PBYTE)getRegVal(baseadj + rm, regs);
            displace += 2;
            break;

        }

        *nextInstr = instrPtr + displace;
        return result;

    }

    return(0);      // Fail
}

/****************************************************************************/

#ifdef TARGET_X86

void checkAndUpdateReg(DWORD& origVal, DWORD curVal, bool gcHappened) {
    if (origVal == curVal)
        return;

    // If these asserts go off, they indicate either that unwinding out of a epilog is wrong or that
    // the validation infrastructure has got a bug.

    _ASSERTE(gcHappened);    // If the register values are different, a GC must have happened
    _ASSERTE(GCHeapUtilities::GetGCHeap()->IsHeapPointer((BYTE*) size_t(origVal)));    // And the pointers involved are on the GCHeap
    _ASSERTE(GCHeapUtilities::GetGCHeap()->IsHeapPointer((BYTE*) size_t(curVal)));
    origVal = curVal;       // this is now the best estimate of what should be returned.
}

#endif // TARGET_X86


int GCcoverCount = 0;

void* forceStack[8];

/****************************************************************************/

bool IsGcCoverageInterrupt(LPVOID ip)
{
    // Determine if the IP is valid for a GC marker first, before trying to dereference it to check the instruction

    EECodeInfo codeInfo(reinterpret_cast<PCODE>(ip));
    if (!codeInfo.IsValid())
    {
        return false;
    }

    NativeCodeVersion nativeCodeVersion = codeInfo.GetNativeCodeVersion();
    _ASSERTE(!nativeCodeVersion.IsNull());
    GCCoverageInfo *gcCover = nativeCodeVersion.GetGCCoverageInfo();
    if (gcCover == nullptr)
    {
        return false;
    }

    PBYTE instrPtr = reinterpret_cast<PBYTE>(ip);

    if (IsGcCoverageInterruptInstruction(instrPtr))
    {
        return true;
    }

    if (IsOriginalInstruction(instrPtr, gcCover, codeInfo.GetRelOffset()))
    {
        // Another thread may have already changed the code back to the original.
        return true;
    }
    return false;
}

// Remove the GcCoverage interrupt instruction, and restore the
// original instruction. Only one instruction must be used,
// because multiple threads can be executing the same code stream.

void RemoveGcCoverageInterrupt(TADDR instrPtr, BYTE * savedInstrPtr, GCCoverageInfo* gcCover, DWORD offset)
{
    ExecutableWriterHolder<void> instrPtrWriterHolder((void*)instrPtr, 4);
#ifdef TARGET_ARM
    if (GetARMInstructionLength(savedInstrPtr) == 2)
        *(WORD *)instrPtrWriterHolder.GetRW()  = *(WORD *)savedInstrPtr;
    else
        *(DWORD *)instrPtrWriterHolder.GetRW() = *(DWORD *)savedInstrPtr;
#elif defined(TARGET_ARM64)
    *(DWORD *)instrPtrWriterHolder.GetRW() = *(DWORD *)savedInstrPtr;
#elif defined(TARGET_LOONGARCH64)
    *(DWORD *)instrPtrWriterHolder.GetRW() = *(DWORD *)savedInstrPtr;
#elif defined(TARGET_RISCV64)
    *(DWORD *)instrPtrWriterHolder.GetRW() = *(DWORD *)savedInstrPtr;
#else
    *(BYTE *)instrPtrWriterHolder.GetRW() = *savedInstrPtr;
#endif

#ifdef TARGET_X86
    // Epilog checking relies on precise control of when instrumentation for the  first prolog 
    // instruction is enabled or disabled. In particular, if a function has multiple epilogs, or
    // the first execution of the function terminates via an exception, and subsequent completions
    // do not, then the function may trigger a false stress fault if epilog checks are not disabled.
    if (offset == 0)
    {
        gcCover->doingEpilogChecks = false;
    }
#endif // TARGET_X86

        FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 4);
}

// A managed thread (T) can race with the GC as follows:
// 1)	At the first safepoint, we notice that T is in preemptive mode during the call for GCStress
//      So, it is put it in cooperative mode for the purpose of GCStress(fPreemptiveGcDisabledForGcStress)
// 2)	We DoGCStress(). Start off background GC in a different thread.
// 3)	Then the thread T is put back to preemptive mode (because that's where it was).
//      Thread T continues execution along with the GC thread.
// 4)	The Jitted code puts thread T to cooperative mode, as part of PInvoke epilog
// 5)	Now instead of CORINFO_HELP_STOP_FOR_GC(), we hit the GCStress trap and start
//      another round of GCStress while in Cooperative mode.
// 6)	Now, thread T can modify the stack (ex: RedirectionFrame setup) while the GC thread is scanning it.
//
// This race is now mitigated below. Where we won't initiate a stress mode GC
// for a thread in cooperative mode with an active ICF, if g_TrapReturningThreads is true.

BOOL OnGcCoverageInterrupt(PCONTEXT regs)
{
    // So that you can set counted breakpoint easily;
    GCcoverCount++;
    forceStack[0]= &regs;                // This is so I can see it fastchecked

    PCODE controlPc = GetIP(regs);
    TADDR instrPtr = PCODEToPINSTR(controlPc);

    forceStack[0] = &instrPtr;            // This is so I can see it fastchecked

    EECodeInfo codeInfo(controlPc);
    if (!codeInfo.IsValid())
        return(FALSE);

    MethodDesc* pMD = codeInfo.GetMethodDesc();
    DWORD offset = codeInfo.GetRelOffset();

    forceStack[1] = &pMD;                // This is so I can see it fastchecked
    forceStack[2] = &offset;             // This is so I can see it fastchecked

    NativeCodeVersion nativeCodeVersion = codeInfo.GetNativeCodeVersion();
    _ASSERTE(!nativeCodeVersion.IsNull());
    GCCoverageInfo* gcCover = nativeCodeVersion.GetGCCoverageInfo();
    forceStack[3] = &gcCover;            // This is so I can see it fastchecked
    if (gcCover == 0)
        return(FALSE);        // we aren't doing code gcCoverage on this function

    BYTE * savedInstrPtr = &gcCover->savedCode[offset];

    Thread* pThread = GetThreadNULLOk();
    if (!pThread)
    {
        // No thread at the moment so we aren't doing coverage for this function.
        // This should only occur for methods with the UnmanagedCallersOnlyAttribute,
        // where the call could be coming from a thread unknown to the CLR and
        // we haven't created a thread yet - see PreStubWorker_Preemptive().
        _ASSERTE(pMD->HasUnmanagedCallersOnlyAttribute());
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        return TRUE;
    }

    // If the thread is in preemptive mode then we must be in a
    // PInvoke stub, a method that has an inline PInvoke frame,
    // or be in a reverse PInvoke stub that's about to return.
    //
    // The PInvoke cases should should properly report GC refs if we
    // trigger GC here. But a reverse PInvoke stub may over-report
    // leading to spurious failures, as we would not normally report
    // anything for this method at this point.
    if (!pThread->PreemptiveGCDisabled() && pMD->HasUnmanagedCallersOnlyAttribute())
    {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        return TRUE;
    }

    // If we're in cooperative mode, we're supposed to stop for GC,
    // and there's an active ICF, don't initiate a stress GC.
    if (g_TrapReturningThreads && pThread->PreemptiveGCDisabled())
    {
        Frame* pFrame = pThread->GetFrame();
        if (InlinedCallFrame::FrameHasActiveCall(pFrame))
        {
            RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
            return TRUE;
        }
    }

#if defined(USE_REDIRECT_FOR_GCSTRESS) && !defined(TARGET_UNIX)
    // If we're unable to redirect, then we simply won't test GC at this
    // location.
    if (Thread::UseRedirectForGcStress())
    {
        if (!pThread->CheckForAndDoRedirectForGCStress(regs))
        {
            RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        }
    }
    else
#endif // !USE_REDIRECT_FOR_GCSTRESS
    {
#ifdef _DEBUG
        if (!g_pConfig->SkipGCCoverage(pMD->GetModule()->GetSimpleName()))
#endif
        DoGcStress(regs, codeInfo.GetNativeCodeVersion());
    }

    return TRUE;
}

// There are some code path in DoGcStress to return without doing a GC but we
// now relies on EE suspension to update the GC STRESS instruction.
// We need to do a extra EE suspension/resume even without GC.
FORCEINLINE void UpdateGCStressInstructionWithoutGC ()
{
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
    ThreadSuspend::RestartEE(TRUE, TRUE);
}

/****************************************************************************/

void DoGcStress (PCONTEXT regs, NativeCodeVersion nativeCodeVersion)
{
    PCODE controlPc = GetIP(regs);
    PBYTE instrPtr = reinterpret_cast<PBYTE>(PCODEToPINSTR(controlPc));

    if (nativeCodeVersion.IsNull())
    {
        nativeCodeVersion = ExecutionManager::GetNativeCodeVersion(controlPc);
        if (nativeCodeVersion.IsNull())
            return;
    }

    GCCoverageInfo *gcCover = nativeCodeVersion.GetGCCoverageInfo();

    EECodeInfo codeInfo(controlPc);
    _ASSERTE(codeInfo.GetNativeCodeVersion() == nativeCodeVersion);
    DWORD offset = codeInfo.GetRelOffset();

    Thread *pThread = GetThread();

    // There is a race condition with the computation of `atCall`. Multiple threads could enter
    // this function (DoGcStress) at the same time. If one reads `*instrPtr` and sets `atCall`
    // to `true`, it will proceed to, lower down in this function, call `pThread->CommitGCStressInstructionUpdate()`
    // to replace the GCStress instruction at the call back to the original call instruction.
    // Other threads could then read `*instrPtr` and see the actual call instruction instead of the
    // call-specific GCStress instruction (INTERRUPT_INSTR_CALL[_32]). If `atCall` is set to false as
    // a result, then we'll do a GCStress as if this is a fully-interruptible code site, which is isn't,
    // which can leads to asserts (or, presumably, other failures). So, we have to check
    // `if (!IsGcCoverageInterruptInstruction(instrPtr))` after we read `*instrPtr`.

    bool atCall;
    bool afterCallProtect[2] = { false, false };

#if defined(TARGET_X86) || defined(TARGET_AMD64)

    BYTE instrVal = *instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    atCall = (instrVal == INTERRUPT_INSTR_CALL);

    if (instrVal == INTERRUPT_INSTR_PROTECT_BOTH_RET)
    {
        afterCallProtect[0] = afterCallProtect[1] = true;
    }
    else if (instrVal == INTERRUPT_INSTR_PROTECT_FIRST_RET)
    {
        afterCallProtect[0] = true;
    }
    else if (instrVal == INTERRUPT_INSTR_PROTECT_SECOND_RET)
    {
        afterCallProtect[1] = true;
    }

#elif defined(TARGET_ARM)

    forceStack[6] = (WORD*)instrPtr;            // This is so I can see it fastchecked

    size_t instrLen = GetARMInstructionLength(instrPtr);

    if (instrLen == 2)
    {
        WORD instrVal = *(WORD*)instrPtr;
        atCall              = (instrVal == INTERRUPT_INSTR_CALL);
        afterCallProtect[0] = (instrVal == INTERRUPT_INSTR_PROTECT_RET);
    }
    else
    {
        _ASSERTE(instrLen == 4);

        DWORD instrVal32 = *(DWORD*)instrPtr;

        atCall              = (instrVal32 == INTERRUPT_INSTR_CALL_32);
        afterCallProtect[0] = (instrVal32 == INTERRUPT_INSTR_PROTECT_RET_32);
    }

#elif defined(TARGET_ARM64)

    DWORD instrVal = *(DWORD *)instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    atCall = (instrVal == INTERRUPT_INSTR_CALL);
    afterCallProtect[0] = (instrVal == INTERRUPT_INSTR_PROTECT_RET);

#elif defined(TARGET_LOONGARCH64)
    DWORD instrVal = *(DWORD *)instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    atCall = (instrVal == INTERRUPT_INSTR_CALL);
    afterCallProtect[0] = (instrVal == INTERRUPT_INSTR_PROTECT_RET);
#elif defined(TARGET_RISCV64)

    DWORD instrVal = *(DWORD *)instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    atCall = (instrVal == INTERRUPT_INSTR_CALL);
    afterCallProtect[0] = (instrVal == INTERRUPT_INSTR_PROTECT_RET);
#endif // _TARGET_*

    if (!IsGcCoverageInterruptInstruction(instrPtr))
    {
        // This assert can fail if another thread changed original instruction to
        // GCCoverage Interrupt instruction between these two commands. Uncomment it
        // when threading issue gets resolved.
        // _ASSERTE(IsOriginalInstruction(instrPtr, gcCover, offset));

        // Someone beat us to it, just go on running.
        return;
    }

#ifdef TARGET_X86
    /* are we at the very first instruction?  If so, capture the register state */
    bool bShouldUpdateProlog = true;
    if (gcCover->doingEpilogChecks) {
        if (offset == 0) {
            if ((gcCover->callerThread == 0) && (InterlockedCompareExchangeT(&gcCover->callerThread, pThread, 0) == 0)) {
                gcCover->callerRegs = *regs;
                gcCover->gcCount = GCHeapUtilities::GetGCHeap()->GetGcCount();
                bShouldUpdateProlog = false;
            }
            else {
                // We have been in this routine before.  Give up on epilog checking because
                // it is hard to insure that the saved caller register state is correct
                // This also has the effect of only doing the checking once per routine
                // (Even if there are multiple epilogs)
                gcCover->doingEpilogChecks = false;
            }
        }

        // If some other thread removes interrupt points, we abandon epilog testing
        // for this routine since the barrier at the beginning of the routine may not
        // be up anymore, and thus the caller context is now not guaranteed to be correct.
        // This should happen only very rarely so is not a big deal.
        if (gcCover->callerThread != pThread)
            gcCover->doingEpilogChecks = false;
    }

    instrVal = gcCover->savedCode[offset];
#endif // TARGET_X86


    // <GCStress instruction update race>
    // Remove the interrupt instruction the next time we suspend the EE,
    // which should happen below in the call to StressHeap().  This is
    // done with the EE suspended so that we do not race with the executing
    // code on some other thread.  If we allow that race, we may sometimes
    // get a STATUS_ACCESS_VIOLATION instead of the expected
    // STATUS_PRIVILEGED_INSTRUCTION because the OS has to inspect the code
    // stream to determine which exception code to raise.  As a result, some
    // thread may take the exception due to the HLT, but by the time the OS
    // inspects the code stream, the HLT may be replaced with the original
    // code and it will just raise a STATUS_ACCESS_VIOLATION.
#ifdef TARGET_X86
    // only restore the original instruction if:
    //    this is not the first instruction in the method's prolog, or
    //    if it is, only if this is the second time we run in this method
    // note that if this is the second time in the prolog we've already disabled epilog checks
    if (offset != 0 || bShouldUpdateProlog)
#endif
    pThread->PostGCStressInstructionUpdate((BYTE*)instrPtr, &gcCover->savedCode[offset]);

#ifdef TARGET_X86
    /* are we in a prolog or epilog?  If so just test the unwind logic
       but don't actually do a GC since the prolog and epilog are not
       GC safe points */
    if (gcCover->codeMan->IsInPrologOrEpilog(offset, gcCover->gcInfoToken, NULL))
    {
        // We are not at a GC safe point so we can't Suspend EE (Suspend EE will yield to GC).
        // But we still have to update the GC Stress instruction. We do it directly without suspending
        // other threads, which means a race on updating is still possible. But for X86 the window of
        // race is so small that we could ignore it. We need a better solution if the race becomes a real problem.
        // see details about <GCStress instruction update race> in comments above
        pThread->CommitGCStressInstructionUpdate ();

        REGDISPLAY regDisp;
        CONTEXT copyRegs = *regs;

        pThread->Thread::InitRegDisplay(&regDisp, &copyRegs, true);
        pThread->UnhijackThread();

        CodeManState codeManState;
        codeManState.dwIsSet = 0;

        // unwind out of the prolog or epilog
        gcCover->codeMan->UnwindStackFrame(&regDisp,
                &codeInfo, UpdateAllRegs, &codeManState, NULL);

        // Note we always doing the unwind, since that at does some checking (that we
        // unwind to a valid return address), but we only do the precise checking when
        // we are certain we have a good caller state
        if (gcCover->doingEpilogChecks) {
            // Confirm that we recovered our register state properly
            _ASSERTE(regDisp.PCTAddr == TADDR(gcCover->callerRegs.Esp));

            // If a GC happened in this function, then the registers will not match
            // precisely.  However there is still checks we can do.  Also we can update
            // the saved register to its new value so that if a GC does not happen between
            // instructions we can recover (and since GCs are not allowed in the
            // prologs and epilogs, we get get complete coverage except for the first
            // instruction in the epilog  (TODO: fix it for the first instr Case)

            _ASSERTE(pThread->PreemptiveGCDisabled());    // Epilogs should be in cooperative mode, no GC can happen right now.
            bool gcHappened = gcCover->gcCount != GCHeapUtilities::GetGCHeap()->GetGcCount();
            checkAndUpdateReg(gcCover->callerRegs.Edi, *regDisp.GetEdiLocation(), gcHappened);
            checkAndUpdateReg(gcCover->callerRegs.Esi, *regDisp.GetEsiLocation(), gcHappened);
            checkAndUpdateReg(gcCover->callerRegs.Ebx, *regDisp.GetEbxLocation(), gcHappened);
            checkAndUpdateReg(gcCover->callerRegs.Ebp, *regDisp.GetEbpLocation(), gcHappened);

            gcCover->gcCount = GCHeapUtilities::GetGCHeap()->GetGcCount();

        }
        return;
    }
#endif // TARGET_X86

#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    /* In non-fully interruptible code, if the EIP is just after a call instr
       means something different because it expects that we are IN the
       called method, not actually at the instruction just after the call. This
       is important, because until the called method returns, IT is responsible
       for protecting the return value.  Thus just after a call instruction
       we have to protect EAX if the method being called returns a GC pointer.

       To figure this out, we need to stop AT the call so we can determine the
       target (and thus whether it returns one or more GC pointers), and then place
       a different interrupt instruction so that the GCCover harness protects
       the return value register(s) before doing the GC. This effectively simulates
       a hijack in non-fully interruptible code */

    /* TODO. Simulating the hijack could cause problems in cases where the
       return register is not always a valid GC ref on the return offset.
       That could happen if we got to the return offset via a branch
       and not via return from the preceding call. However, this has not been
       an issue so far.

       Example:
        mov eax, someval
        test eax, eax
        jCC AFTERCALL
        call MethodWhichReturnsGCobject // return value is not used
        AFTERCALL:
    */

    if (atCall) {
        // We need to update the GC Stress instruction. With partially-interruptible code
        // the call instruction is not a GC safe point so we can't use
        // StressHeap or UpdateGCStressInstructionWithoutGC to take care of updating;
        // So we just update the instruction directly. There are still chances for a race,
        // but it's not been a problem so far.
        // see details about <GCStress instruction update race> in comments above
        pThread->CommitGCStressInstructionUpdate ();
        PBYTE nextInstr;
        PBYTE target = getTargetOfCall((BYTE*) instrPtr, regs, (BYTE**)&nextInstr);
        if (target != 0)
        {
            ExecutableWriterHolder<BYTE> nextInstrWriterHolder(nextInstr, sizeof(DWORD));
            if (!pThread->PreemptiveGCDisabled())
            {
                // We are in preemptive mode in JITTed code. This implies that we are into IL stub
                // close to PINVOKE method. This call will never return objectrefs.
#ifdef TARGET_ARM
                size_t instrLen = GetARMInstructionLength(nextInstr);
                if (instrLen == 2)
                    *(WORD*)nextInstrWriterHolder.GetRW()  = INTERRUPT_INSTR;
                else
                    *(DWORD*)nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR_32;
#elif defined(TARGET_ARM64)
                *(DWORD*)nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR;
#elif defined(TARGET_LOONGARCH64)
                *(DWORD*)nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR;
#elif defined(TARGET_RISCV64)
                *(DWORD*)nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR;
#else
                *nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR;
#endif
            }
            else
            {
                MethodDesc* targetMD = getTargetMethodDesc((PCODE)target);

                if (targetMD != 0)
                {
                    // @Todo: possible race here, might need to be fixed  if it become a problem.
                    // It could become a problem if 64bit does partially interrupt work.
                    // OK, we have the MD, mark the instruction after the CALL
                    // appropriately
                    ReplaceInstrAfterCall(nextInstrWriterHolder.GetRW(), targetMD);
                }
            }
        }

        // Must flush instruction cache before returning as instruction has been modified.
        // Note this needs to reach beyond the call by up to 4 bytes.
        FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 10);

        // It's not GC safe point, the GC Stress instruction is
        // already committed and interrupt is already put at next instruction so we just return.
        return;
    }
#else
    PORTABILITY_ASSERT("DoGcStress - NYI on this platform");
#endif // _TARGET_*

    bool enableWhenDone = false;
    if (!pThread->PreemptiveGCDisabled())
    {
        pThread->DisablePreemptiveGC();
        enableWhenDone = true;
    }


#if 0
    // TODO currently disabled.  we only do a GC once per instruction location.

  /* note that for multiple threads, we can loose track and
       forget to set reset the interrupt after we executed
       an instruction, so some instruction points will not be
       executed twice, but we still ge350t very good coverage
       (perfect for single threaded cases) */

    /* if we have not run this instruction in the past */
    /* remember to wack it to an INTERUPT_INSTR again */

    if (!gcCover->IsBitSetForOffset(offset))  {
        // gcCover->curInstr = instrPtr;
        gcCover->SetBitForOffset(offset);
    }
#endif // 0

    //
    // If we redirect for gc stress, we don't need this frame on the stack,
    // the redirection will push a resumable frame.
    //
    FrameWithCookie<ResumableFrame> frame(regs);
    if (!Thread::UseRedirectForGcStress())
    {
        frame.Push(pThread);
    }

    DWORD_PTR retValRegs[2] = { 0 };
    UINT  numberOfRegs = 0;

    if (afterCallProtect[0])
    {
#if defined(TARGET_AMD64)
        retValRegs[numberOfRegs++] = regs->Rax;
#elif defined(TARGET_X86)
        retValRegs[numberOfRegs++] = regs->Eax;
#elif  defined(TARGET_ARM)
        retValRegs[numberOfRegs++] = regs->R0;
#elif defined(TARGET_ARM64)
        retValRegs[numberOfRegs++] = regs->X0;
#elif defined(TARGET_LOONGARCH64)
        retValRegs[numberOfRegs++] = regs->A0;
#elif defined(TARGET_RISCV64)
        retValRegs[numberOfRegs++] = regs->A0;
#endif // TARGET_ARM64
    }

    if (afterCallProtect[1])
    {
#if defined(TARGET_AMD64) && defined(TARGET_UNIX)
        retValRegs[numberOfRegs++] = regs->Rdx;
#else // !TARGET_AMD64 || !TARGET_UNIX
        _ASSERTE(!"Not expected multi reg return with pointers.");
#endif // !TARGET_AMD64 || !TARGET_UNIX
    }

    _ASSERTE(sizeof(OBJECTREF) == sizeof(DWORD_PTR));
    GCFrame gcFrame(pThread, (OBJECTREF*)retValRegs, numberOfRegs, TRUE);

    MethodDesc *pMD = nativeCodeVersion.GetMethodDesc();
    LOG((LF_GCROOTS, LL_EVERYTHING, "GCCOVER: Doing GC at method %s::%s offset 0x%x\n",
            pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, offset));

    //-------------------------------------------------------------------------
    // Do the actual stress work
    //

    // BUG(github #10318) - when not using allocation contexts, the alloc lock
    // must be acquired here. Until fixed, this assert prevents random heap corruption.
    assert(GCHeapUtilities::UseThreadAllocationContexts());
    GCHeapUtilities::GetGCHeap()->StressHeap(GetThread()->GetAllocContext());

    // StressHeap can exit early w/o forcing a SuspendEE to trigger the instruction update
    // We can not rely on the return code to determine if the instruction update happened
    // Use HasPendingGCStressInstructionUpdate() to be certain.
    if(pThread->HasPendingGCStressInstructionUpdate())
        UpdateGCStressInstructionWithoutGC ();

    // Must flush instruction cache before returning as instruction has been modified.
    FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 4);

    CONSISTENCY_CHECK(!pThread->HasPendingGCStressInstructionUpdate());

    if (numberOfRegs != 0)
    {
        if (afterCallProtect[0])
        {
#if defined(TARGET_AMD64)
            regs->Rax = retValRegs[0];
#elif defined(TARGET_X86)
            regs->Eax = retValRegs[0];
#elif defined(TARGET_ARM)
            regs->R0 = retValRegs[0];
#elif defined(TARGET_ARM64)
            regs->X[0] = retValRegs[0];
#elif defined(TARGET_LOONGARCH64)
            regs->A0 = retValRegs[0];
#elif defined(TARGET_RISCV64)
            regs->A0 = retValRegs[0];
#else
            PORTABILITY_ASSERT("DoGCStress - return register");
#endif
        }

        if (afterCallProtect[1])
        {
#if defined(TARGET_AMD64) && defined(TARGET_UNIX)
            regs->Rdx = retValRegs[numberOfRegs - 1];
#else // !TARGET_AMD64 || !TARGET_UNIX
            _ASSERTE(!"Not expected multi reg return with pointers.");
#endif // !TARGET_AMD64 || !TARGET_UNIX
        }
    }

    if (!Thread::UseRedirectForGcStress())
    {
        frame.Pop(pThread);
    }

    if (enableWhenDone)
    {
        BOOL b = GC_ON_TRANSITIONS(FALSE);      // Don't do a GCStress 3 GC here
        pThread->EnablePreemptiveGC();
        GC_ON_TRANSITIONS(b);
    }

    return;

}

#endif // HAVE_GCCOVER
