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

    // CONSIDER: does anyone call this with fZapped == true ? are there plans?
    _ASSERTE(!fZapped);

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

    // This is only required for X86, since otherwise the above call does the work for both hot & cold regions
#if defined(TARGET_X86)
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

// There are some code path in DoGcStress to return without doing a GC but
// now relies on EE suspension to update the GC STRESS instruction.
// We need to do a extra EE suspension/resume even without GC.
FORCEINLINE void UpdateGCStressInstructionWithoutGC ()
{
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
    ThreadSuspend::RestartEE(TRUE, TRUE);
}

#if defined(TARGET_X86)
/////////////////////////////////////////////////////////////////////////////
////////////////////////////// x86-specific /////////////////////////////////
/////////////////////////////////////////////////////////////////////////////

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
                    result = 0;
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
        Precode* pPrecode = Precode::GetPrecodeFromEntryPoint(target);
        switch (pPrecode->GetType())
        {
            case PRECODE_STUB:
            case PRECODE_NDIRECT_IMPORT:
                return dac_cast<PTR_MethodDesc>(pPrecode->AsStubPrecode()->GetMethodDesc());
            default:
                return nullptr;
        }
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

void ReplaceInstrAfterCall(PBYTE instrToReplace, MethodDesc* callMD)
{
    ReturnKind returnKind = callMD->GetReturnKind(true);
    if (!IsValidReturnKind(returnKind))
    {
        // SKip GC coverage after the call.
        return;
    }
    _ASSERTE(IsValidReturnKind(returnKind));

    bool ispointerKind = IsPointerReturnKind(returnKind);
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
            _ASSERTE(!"Not expected multi reg return with pointers.");
        }
    }
    else
    {
        *instrToReplace = INTERRUPT_INSTR;
    }
}

void GCCoverageInfo::SprinkleBreakpoints(
        BYTE * saveAddr,
        PCODE  pCode,
        size_t codeSize,
        size_t regionOffsetAdj,
        BOOL   fZapped)
{
#if USE_DISASSEMBLER

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

        _ASSERTE(len > 0);
        _ASSERTE(len <= (size_t)(codeEnd-cur));

        // For non-fully interruptible code, we want to at least
        // patch the return sites after the call instructions.
        // Specially so that we can verify stack-walking through the call site via a simulated hijack.
        // We would need to know the return kind of the callee, so this may not always be possible.
        switch(instructionType)
        {
        case InstructionType::Call_IndirectUnconditional:
            *(cur + writeableOffset) = INTERRUPT_INSTR_CALL;        // return value.  May need to protect
            break;

        case InstructionType::Call_DirectUnconditional:
            // NB: turned off by default
            if(fGcStressOnDirectCalls.val(CLRConfig::INTERNAL_GcStressOnDirectCalls))
            {
                PBYTE nextInstr;
                PBYTE target = getTargetOfCall(cur, NULL, &nextInstr);

                if (target != 0)
                {
                    targetMD = getTargetMethodDesc((PCODE)target);
                }
            }
            break;

        default:
            // Clang issues an error saying that some enum values are not handled in the switch, that's intended
            break;
        }

        if (prevDirectCallTargetMD != 0)
        {
            ReplaceInstrAfterCall(cur + writeableOffset, prevDirectCallTargetMD);
        }

        // For fully interruptible locations, we end up whacking every instruction
        // to INTERRUPT_INSTR.
        size_t dwRelOffset = (cur - codeStart) + regionOffsetAdj;
        _ASSERTE(FitsIn<DWORD>(dwRelOffset));
        if (codeMan->IsGcSafe(&codeInfo, static_cast<DWORD>(dwRelOffset)))
        {
            *(cur + writeableOffset) = INTERRUPT_INSTR;
        }

        // we will whack every instruction in the prolog and epilog to make certain
        // our unwinding logic works there.
        if (codeMan->IsInPrologOrEpilog((cur - codeStart) + (DWORD)regionOffsetAdj, gcInfoToken, NULL))
        {
            *(cur + writeableOffset) = INTERRUPT_INSTR;
        }

        // If we couldn't find the method desc targetMD is zero
        prevDirectCallTargetMD = targetMD;

        cur += len;
    }

    // If we are not able to place an interrupt at the first instruction, this means that
    // we are partially interruptible with no prolog.  Just don't bother to do the
    // the epilog checks, since the epilog will be trivial (a single return instr)
    assert(codeSize > 0);
    if ((regionOffsetAdj==0) && (*codeStart != INTERRUPT_INSTR))
        doingEpilogChecks = false;

#endif // USE_DISASSEMBLER
}

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
    bool afterCallProtect = false;

    BYTE instrVal = *instrPtr;
    atCall = (instrVal == INTERRUPT_INSTR_CALL);

    if (instrVal == INTERRUPT_INSTR_PROTECT_FIRST_RET)
    {
        afterCallProtect = true;
    }

    if (!IsGcCoverageInterruptInstruction(instrPtr))
    {
        // This assert can fail if another thread changed original instruction to
        // GCCoverage Interrupt instruction between these two commands. Uncomment it
        // when threading issue gets resolved.
        // _ASSERTE(IsOriginalInstruction(instrPtr, gcCover, offset));

        // Someone beat us to it, just go on running.
        return;
    }

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
                // it is hard to ensure that the saved caller register state is correct
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

    // only restore the original instruction if:
    //    this is not the first instruction in the method's prolog, or
    //    if it is, only if this is the second time we run in this method
    // note that if this is the second time in the prolog we've already disabled epilog checks
    if (offset != 0 || bShouldUpdateProlog)
        pThread->PostGCStressInstructionUpdate((BYTE*)instrPtr, &gcCover->savedCode[offset]);

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
                &codeInfo, UpdateAllRegs, &codeManState);

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
                *nextInstrWriterHolder.GetRW() = INTERRUPT_INSTR;
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

    bool enableWhenDone = false;
    if (!pThread->PreemptiveGCDisabled())
    {
        pThread->DisablePreemptiveGC();
        enableWhenDone = true;
    }

    //
    // If we redirect for gc stress, we don't need this frame on the stack,
    // the redirection will push a resumable frame.
    //
    ResumableFrame frame(regs);
    if (!Thread::UseRedirectForGcStress())
    {
        frame.Push(pThread);
    }

    // The legacy X86 GC encoder does not encode the state of return registers at
    // call sites, so we must add an extra frame to protect returns.
    DWORD_PTR retValReg = 0;

    if (afterCallProtect)
    {
        retValReg = regs->Eax;
    }

    _ASSERTE(sizeof(OBJECTREF) == sizeof(DWORD_PTR));
    GCFrame gcFrame(pThread, (OBJECTREF*)&retValReg, 1, TRUE);

    MethodDesc *pMD = nativeCodeVersion.GetMethodDesc();
    LOG((LF_GCROOTS, LL_EVERYTHING, "GCCOVER: Doing GC at method %s::%s offset 0x%x\n",
            pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, offset));

    //-------------------------------------------------------------------------
    // Do the actual stress work
    //

    // BUG(github #10318) - when not using allocation contexts, the alloc lock
    // must be acquired here. Until fixed, this assert prevents random heap corruption.
    assert(GCHeapUtilities::UseThreadAllocationContexts());
    GCHeapUtilities::GetGCHeap()->StressHeap(&t_runtime_thread_locals.alloc_context.m_GCAllocContext);

    // StressHeap can exit early w/o forcing a SuspendEE to trigger the instruction update
    // We can not rely on the return code to determine if the instruction update happened
    // Use HasPendingGCStressInstructionUpdate() to be certain.
    if(pThread->HasPendingGCStressInstructionUpdate())
        UpdateGCStressInstructionWithoutGC ();

    // Must flush instruction cache before returning as instruction has been modified.
    FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 4);

    CONSISTENCY_CHECK(!pThread->HasPendingGCStressInstructionUpdate());

    if (afterCallProtect)
    {
        regs->Eax = retValReg;
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

#elif defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
/////////////////////////////////////////////////////////////////////////////
////////////////////////////// end of x86-specific //////////////////////////
/////////////////////////////////////////////////////////////////////////////

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
void replaceSafePointInstructionWithGcStressInstr(GcInfoDecoder* decoder, UINT32 safePointOffset, LPVOID pGCCover)
{
    PCODE pCode = (PCODE)NULL;
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
        _ASSERTE(safePointOffset - ptr->hotSize < ptr->coldSize);
        return;
    }

    PBYTE instrPtr = (BYTE*)PCODEToPINSTR(pCode);

    // if this is an interruptible safe point, just replace it with an interrupt instr and we are done.

    // The instruction about to be replaced cannot already be a gcstress instruction
    _ASSERTE(!IsGcCoverageInterruptInstruction(instrPtr));

    ExecutableWriterHolder<BYTE> instrPtrWriterHolder(instrPtr, sizeof(DWORD));
#if defined(TARGET_ARM)
    size_t instrLen = GetARMInstructionLength(instrPtr);

    if (instrLen == 2)
    {
        *((WORD*)instrPtrWriterHolder.GetRW())  = INTERRUPT_INSTR;
    }
    else
    {
        *((DWORD*)instrPtrWriterHolder.GetRW()) = INTERRUPT_INSTR_32;
    }
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    *((DWORD*)instrPtrWriterHolder.GetRW()) = INTERRUPT_INSTR;
#else
    *((BYTE*)instrPtrWriterHolder.GetRW()) = INTERRUPT_INSTR;
#endif // TARGET_XXXX_
}
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

//Replaces the provided interruptible range with corresponding 2 or 4 byte gcStress illegal instruction
bool replaceInterruptibleRangesWithGcStressInstr (UINT32 startOffset, UINT32 stopOffset, LPVOID pGCCover)
{
#if defined(TARGET_AMD64)
#if defined(USE_DISASSEMBLER)
    Disassembler disassembler;
#else
    // we can't instrument fully interruptible ranges in x64 without disassembling
    return;
#endif // USE_DISASSEMBLER
#endif // TARGET_AMD64

    PCODE pCode = (PCODE)NULL;
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
            {
                *((WORD*)instrPtrRW)  = INTERRUPT_INSTR;
            }
            else
            {
                *((DWORD*)instrPtrRW) = INTERRUPT_INSTR_32;
            }

            instrPtrRW += instrLen;
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            *((DWORD*)instrPtrRW) = INTERRUPT_INSTR;
            instrPtrRW += 4;
#else // AMD64
            InstructionType instructionType;
            size_t instrLen = disassembler.DisassembleInstruction(instrPtrRW, rangeStopRW - instrPtrRW, &instructionType);
            *((BYTE*)instrPtrRW) = INTERRUPT_INSTR;
            instrPtrRW += instrLen;
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

    if (!IsGcCoverageInterruptInstruction(instrPtr))
    {
        _ASSERTE(IsOriginalInstruction(instrPtr, gcCover, offset));

        // Someone beat us to it, just go on running.
        // one reason for this race is RemoveGcCoverageInterrupt case
        // where we find us unable to do stress.
        return;
    }

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
    pThread->PostGCStressInstructionUpdate((BYTE*)instrPtr, &gcCover->savedCode[offset]);

    // we should be in coop mode.
    _ASSERTE(pThread->PreemptiveGCDisabled());

    //
    // If we redirect for gc stress, we don't need this frame on the stack,
    // the redirection will push a resumable frame.
    //
    ResumableFrame frame(regs);
    if (!Thread::UseRedirectForGcStress())
    {
        frame.Push(pThread);
    }

    MethodDesc *pMD = nativeCodeVersion.GetMethodDesc();
    LOG((LF_GCROOTS, LL_EVERYTHING, "GCCOVER: Doing GC at method %s::%s offset 0x%x\n",
            pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, offset));

    //-------------------------------------------------------------------------
    // Do the actual stress work
    //

    // BUG(github #10318) - when not using allocation contexts, the alloc lock
    // must be acquired here. Until fixed, this assert prevents random heap corruption.
    assert(GCHeapUtilities::UseThreadAllocationContexts());
    GCHeapUtilities::GetGCHeap()->StressHeap(&t_runtime_thread_locals.alloc_context.m_GCAllocContext);

    // StressHeap can exit early w/o forcing a SuspendEE to trigger the instruction update
    // We can not rely on the return code to determine if the instruction update happened
    // Use HasPendingGCStressInstructionUpdate() to be certain.
    if(pThread->HasPendingGCStressInstructionUpdate())
        UpdateGCStressInstructionWithoutGC ();

    // Must flush instruction cache before returning as instruction has been modified.
    FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 4);

    assert(!pThread->HasPendingGCStressInstructionUpdate());

    if (!Thread::UseRedirectForGcStress())
    {
        frame.Pop(pThread);
    }

    return;
}

#else
    _ASSERTE(!"not implemented for platform");
#endif // TARGET_X86


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
// 1) Current thread T is in preemptive mode, GC happens, starts scanning T's stack.
// 2) The Jitted code puts thread T to cooperative mode, as part of PInvoke epilog.
// 3) Jitted code checks g_TrapReturningThreads, calls CORINFO_HELP_STOP_FOR_GC()
// 4) If any of the code that does #3 is interruptible, we may hit GCStress trap and start
//    another round of GCStress while in Cooperative mode.
// 5) Now, thread T can modify the stack (ex: RedirectionFrame setup) while the GC thread is scanning it.
//
// This race is now mitigated below. Where we won't initiate a stress mode GC
// for a thread in cooperative mode with an active ICF, if g_TrapReturningThreads is true.
BOOL OnGcCoverageInterrupt(PCONTEXT regs)
{
    PCODE controlPc = GetIP(regs);
    TADDR instrPtr = PCODEToPINSTR(controlPc);
    EECodeInfo codeInfo(controlPc);
    if (!codeInfo.IsValid())
        return(FALSE);

    MethodDesc* pMD = codeInfo.GetMethodDesc();
    DWORD offset = codeInfo.GetRelOffset();
    NativeCodeVersion nativeCodeVersion = codeInfo.GetNativeCodeVersion();
    _ASSERTE(!nativeCodeVersion.IsNull());
    GCCoverageInfo* gcCover = nativeCodeVersion.GetGCCoverageInfo();
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
    
    // The thread is in preemptive mode. Normally, it should not be able to trigger GC.
    // Besides the GC may be already happening and scanning our stack.
    if (!pThread->PreemptiveGCDisabled())
    {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        return TRUE;
    }

    // If we're supposed to stop for GC,
    // and there's an active ICF, don't initiate a stress GC.
    Frame* pFrame = pThread->GetFrame();
    if (g_TrapReturningThreads && InlinedCallFrame::FrameHasActiveCall(pFrame))
    {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        return TRUE;
    }

#ifdef _DEBUG
    if (g_pConfig->SkipGCCoverage(pMD->GetModule()->GetSimpleName()))
    {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        return TRUE;
    }
#endif

#ifdef USE_REDIRECT_FOR_GCSTRESS
    // If we're unable to redirect, then we simply won't test GC at this location.
    if (Thread::UseRedirectForGcStress())
    {
        if (!pThread->CheckForAndDoRedirectForGCStress(regs))
        {
            RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr, gcCover, offset);
        }

        return TRUE;
    }
#endif // !USE_REDIRECT_FOR_GCSTRESS

    DoGcStress(regs, codeInfo.GetNativeCodeVersion());
    return TRUE;
}

#endif // HAVE_GCCOVER
