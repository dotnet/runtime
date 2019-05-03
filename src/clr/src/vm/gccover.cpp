// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)
#include "gcinfodecoder.h"
#endif

#include "disassembler.h"

/****************************************************************************/

MethodDesc* AsMethodDesc(size_t addr);
static SLOT getTargetOfCall(SLOT instrPtr, PCONTEXT regs, SLOT*nextInstr);
bool isCallToStopForGCJitHelper(SLOT instrPtr);
#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
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

    VirtualCallStubManager::StubKind vsdStubKind = VirtualCallStubManager::SK_UNKNOWN;
    VirtualCallStubManager *pVSDStubManager = VirtualCallStubManager::FindStubManager(target, &vsdStubKind);
    if (vsdStubKind != VirtualCallStubManager::SK_BREAKPOINT && vsdStubKind != VirtualCallStubManager::SK_UNKNOWN)
    {
        // It is a VSD stub manager.
        DispatchToken token(VirtualCallStubManager::GetTokenFromStubQuick(pVSDStubManager, target, vsdStubKind));
        _ASSERTE(token.IsValid());
        return VirtualCallStubManager::GetInterfaceMethodDescFromToken(token);
    }
    if (RangeSectionStubManager::GetStubKind(target) == STUB_CODE_BLOCK_PRECODE)
    {
        // The address looks like a value stub, try to get the method descriptor.
        return MethodDesc::GetMethodDescFromStubAddr(target, TRUE);
    }

    return nullptr;
}


void SetupAndSprinkleBreakpoints(
    MethodDesc                    * pMD,     
    EECodeInfo                    * pCodeInfo,
    IJitManager::MethodRegionInfo   methodRegionInfo,
    BOOL                            fZapped
    )
{
    // Allocate room for the GCCoverageInfo and copy of the method instructions
    size_t memSize = sizeof(GCCoverageInfo) + methodRegionInfo.hotSize + methodRegionInfo.coldSize;
    GCCoverageInfo* gcCover = (GCCoverageInfo*)(void*) pMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocAlignedMem(memSize, CODE_SIZE_ALIGN);

    memset(gcCover, 0, sizeof(GCCoverageInfo));

    gcCover->methodRegion      = methodRegionInfo;
    gcCover->codeMan           = pCodeInfo->GetCodeManager();
    gcCover->gcInfoToken       = pCodeInfo->GetGCInfoToken();
    gcCover->callerThread      = 0;
    gcCover->doingEpilogChecks = true;    

    gcCover->lastMD            = pMD;   /* pass pMD to SprinkleBreakpoints */

    gcCover->SprinkleBreakpoints(gcCover->savedCode, 
                                 gcCover->methodRegion.hotStartAddress,  
                                 gcCover->methodRegion.hotSize,
                                 0,
                                 fZapped);

    // This is not required for ARM* as the above call does the work for both hot & cold regions
#if !defined(_TARGET_ARM_) && !defined(_TARGET_ARM64_)
    if (gcCover->methodRegion.coldSize != 0)
    {
        gcCover->SprinkleBreakpoints(gcCover->savedCode + gcCover->methodRegion.hotSize, 
                                     gcCover->methodRegion.coldStartAddress,  
                                     gcCover->methodRegion.coldSize,
                                     gcCover->methodRegion.hotSize,
                                     fZapped);
    }
#endif

    gcCover->lastMD = NULL;     /* clear lastMD */

    _ASSERTE(!pMD->m_GcCover);
    *EnsureWritablePages(&pMD->m_GcCover) = gcCover;
}

void SetupAndSprinkleBreakpointsForJittedMethod(MethodDesc                    * pMD, 
                                                PCODE                           codeStart
                                               )
{
    EECodeInfo codeInfo(codeStart);
    _ASSERTE(codeInfo.IsValid());
    _ASSERTE(codeInfo.GetRelOffset() == 0);

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    _ASSERTE(PCODEToPINSTR(codeStart) == methodRegionInfo.hotStartAddress);

#ifdef _DEBUG
    if (!g_pConfig->SkipGCCoverage(pMD->GetModule()->GetSimpleName()))
#endif
    SetupAndSprinkleBreakpoints(pMD,
                                &codeInfo,
                                methodRegionInfo,
                                FALSE
                               );
}

/****************************************************************************/
/* called when a method is first jitted when GCStress level 4 or 8 is on */

void SetupGcCoverage(MethodDesc* pMD, BYTE* methodStartPtr) {

#ifdef _DEBUG
    if (!g_pConfig->ShouldGcCoverageOnMethod(pMD->m_pszDebugMethodName)) {
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
    if (pMD->m_GcCover)
    {
        return;
    }

    PCODE codeStart = (PCODE) methodStartPtr;
    SetupAndSprinkleBreakpointsForJittedMethod(pMD, codeStart);
}

#ifdef FEATURE_PREJIT

void SetupGcCoverageForNativeMethod(MethodDesc* pMD, 
                                    PCODE codeStart, 
                                     IJitManager::MethodRegionInfo& methodRegionInfo
                                   ) 
{

    EECodeInfo codeInfo(codeStart);
    _ASSERTE(codeInfo.IsValid());
    _ASSERTE(codeInfo.GetRelOffset() == 0);

    _ASSERTE(PCODEToPINSTR(codeStart) == methodRegionInfo.hotStartAddress);

    SetupAndSprinkleBreakpoints(pMD,
                                &codeInfo,
                                methodRegionInfo,
                                TRUE
                               );
}

void SetupGcCoverageForNativeImage(Module* module)
{
    // Disable IBC logging here because of NGen image is not fully initialized yet. Eager bound 
    // indirection cells are not initialized yet and so IBC logging would crash while attempting to dereference them.
    IBCLoggingDisabler disableLogging;

#if 0
    // Debug code
    LPWSTR wszSetupGcCoverage = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SetupGcCoverage);

    if (!wszSetupGcCoverage)
    {
        printf("wszSetupGcCoverage is NULL. Will not SetupGcCoverage for any module.\n");
        return;
    }
    else 
    {
        if ((wcscmp(W("*"), wszSetupGcCoverage) == 0) ||  // "*" means will gcstress all modules
            (wcsstr(module->GetDebugName(), wszSetupGcCoverage) != NULL)) 
        {
            printf("[%ws] matched %ws\n", wszSetupGcCoverage, module->GetDebugName());
            // Fall through
        }
        else
        {
            printf("[%ws] NOT match %ws\n", wszSetupGcCoverage, module->GetDebugName());
            return;
        }
    }
#endif

#ifdef _DEBUG
    if (g_pConfig->SkipGCCoverage(module->GetSimpleName()))
        return;
#endif
    
    MethodIterator mi(module);    
    while (mi.Next())
    {
        PTR_MethodDesc pMD = mi.GetMethodDesc();
        PCODE pMethodStart = mi.GetMethodStartAddress();
        
        IJitManager::MethodRegionInfo methodRegionInfo;
        mi.GetMethodRegionInfo(&methodRegionInfo);

        SetupGcCoverageForNativeMethod(pMD, pMethodStart, methodRegionInfo);
    }
}
#endif

#ifdef _TARGET_AMD64_

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

#endif // _TARGET_AMD64_

// When Sprinking break points, we must make sure that certain calls to 
// Thread-suspension routines inlined into the managed method are not 
// converted to GC-Stress points. Otherwise, this will lead to race 
// conditions with the GC.
//
// For example, for an inlined PInvoke stub, the JIT generates the following code
// 
//    call    CORINFO_HELP_INIT_PINVOKE_FRAME // Obtain the thread pointer
//
//    mov      byte  ptr[rsi + 12], 0   // Switch to preemptive mode [thread->premptiveGcDisabled = 0]
//    call     rax                      // The actual native call, in preemptive mode
//    mov      byte  ptr[rsi + 12], 1   // Switch the thread to Cooperative mode
//    cmp      dword ptr[(reloc 0x7ffd1bb77148)], 0  // if(g_TrapReturningThreads)
//    je       SHORT G_M40565_IG05
//    call[CORINFO_HELP_STOP_FOR_GC]             // Call JIT_RareDisableHelper()
//
//
// For the SprinkleBreakPoints() routine, the JIT_RareDisableHelper() itself will 
// look like an ordinary indirect call/safepoint. So, it may rewrite it with 
// a TRAP to perform GC
//
//    call    CORINFO_HELP_INIT_PINVOKE_FRAME // Obtain the thread pointer
//
//    mov      byte  ptr[rsi + 12], 0   // Switch to preemptive mode [thread->premptiveGcDisabled = 0]
//    cli                               // INTERRUPT_INSTR_CALL
//    mov      byte  ptr[rsi + 12], 1   // Switch the thread to Cooperative mode
//    cmp      dword ptr[(reloc 0x7ffd1bb77148)], 0  // if(g_TrapReturningThreads)
//    je       SHORT G_M40565_IG05
//    cli                               // INTERRUPT_INSTR_CALL
//
//
//  Now, a managed thread (T) can race with the GC as follows:
// 1)	At the first safepoint, we notice that T is in preemptive mode during the call for GCStress
//      So, it is put it in cooperative mode for the purpose of GCStress(fPremptiveGcDisabledForGcStress)
// 2)	We DoGCStress(). Start off background GC in a different thread.
// 3)	Then the thread T is put back to preemptive mode (because that's where it was).
//      Thread T continues execution along with the GC thread.
// 4)	The Jitted code puts thread T to cooperative mode, as part of PInvoke epilog
// 5)	Now instead of CORINFO_HELP_STOP_FOR_GC(), we hit the GCStress trap and start 
//      another round of GCStress while in Cooperative mode.
// 6)	Now, thread T can modify the stack (ex: RedirectionFrame setup) while the GC thread is scanning it.
// 
// This problem can be avoided by not inserting traps-for-GC in place of calls to CORINFO_HELP_STOP_FOR_GC()
//
// How do we identify the calls to CORINFO_HELP_STOP_FOR_GC()?
// Since this is a GCStress only requirement, its not worth special identification in the GcInfo 
// Since CORINFO_HELP_STOP_FOR_GC() calls are realized as indirect calls by the JIT, we cannot identify 
// them by address at the time of SprinkleBreakpoints().
// So, we actually let the SprinkleBreakpoints() replace the call to CORINFO_HELP_STOP_FOR_GC() with a trap,
// and revert it back to the original instruction the first time we hit the trap in OnGcCoverageInterrupt().
//
// Similarly, inserting breakpoints can be avoided for JIT_PollGC() and JIT_StressGC().

extern "C" FCDECL0(VOID, JIT_RareDisableHelper);

/****************************************************************************/
/* sprinkle interupt instructions that will stop on every GCSafe location
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
#if (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)) && USE_DISASSEMBLER

    BYTE * codeStart = (BYTE *)pCode;

    memcpy(saveAddr, codeStart, codeSize);

    // For prejitted code we have to remove the write-protect on the code page
    if (fZapped)
    {
        DWORD oldProtect;
        ClrVirtualProtect(codeStart, codeSize, PAGE_EXECUTE_READWRITE, &oldProtect);
    }

    SLOT cur;
    BYTE* codeEnd = codeStart + codeSize;

    EECodeInfo codeInfo((PCODE)codeStart);

    static ConfigDWORD fGcStressOnDirectCalls; // ConfigDWORD must be a static variable


#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
        // REVISIT_TODO apparently the jit does not use the entire RUNTIME_FUNCTION range
        // for code.  It uses some for switch tables.  Because the first few offsets
        // may be decodable as instructions, we can't reason about where we should
        // encounter invalid instructions.  However, we do not want to silently skip
        // large chunks of methods just becuase the JIT started emitting a new
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
#ifdef _TARGET_AMD64_
            if(safePointDecoder.IsSafePoint((UINT32)(cur + len - codeStart + regionOffsetAdj)))
#endif
            {
               *cur = INTERRUPT_INSTR_CALL;        // return value.  May need to protect
            }
            break;

        case InstructionType::Call_DirectUnconditional:
            if(fGcStressOnDirectCalls.val(CLRConfig::INTERNAL_GcStressOnDirectCalls))
            {       
#ifdef _TARGET_AMD64_
                if(safePointDecoder.IsSafePoint((UINT32)(cur + len - codeStart + regionOffsetAdj)))
#endif
                {
                    SLOT nextInstr;
                    SLOT target = getTargetOfCall(cur, NULL, &nextInstr);

                    if (target != 0)
                    {
                        // JIT_RareDisableHelper() is expected to be an indirect call.
                        // If we encounter a direct call (in future), skip the call 
                        _ASSERTE(target != (SLOT)JIT_RareDisableHelper); 
                        targetMD = getTargetMethodDesc((PCODE)target);
                    }
                }
            }
            break;

#ifdef _TARGET_AMD64_
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
            if (prevDirectCallTargetMD->ReturnsObject(true) != MetaSig::RETNONOBJ)
                *cur = INTERRUPT_INSTR_PROTECT_RET;  
            else
                *cur = INTERRUPT_INSTR;
        }

        // For fully interruptible code, we end up whacking every instruction
        // to INTERRUPT_INSTR.  For non-fully interruptible code, we end
        // up only touching the call instructions (specially so that we
        // can really do the GC on the instruction just after the call).
        _ASSERTE(FitsIn<DWORD>((cur - codeStart) + regionOffsetAdj));
        if (codeMan->IsGcSafe(&codeInfo, static_cast<DWORD>((cur - codeStart) + regionOffsetAdj)))
            *cur = INTERRUPT_INSTR;

#ifdef _TARGET_X86_
        // we will whack every instruction in the prolog and epilog to make certain
        // our unwinding logic works there.  
        if (codeMan->IsInPrologOrEpilog((cur - codeStart) + (DWORD)regionOffsetAdj, gcInfoToken, NULL)) {
            *cur = INTERRUPT_INSTR;
        }
#endif

        // If we couldn't find the method desc targetMD is zero
        prevDirectCallTargetMD = targetMD;                        

        cur += len;

#ifdef _TARGET_AMD64_
        SLOT newCur = rangeEnum.EnsureInRange(cur);
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
    // the epilog checks, since the epilog will be trival (a single return instr)
    assert(codeSize > 0);
    if ((regionOffsetAdj==0) && (*codeStart != INTERRUPT_INSTR))
        doingEpilogChecks = false;

#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
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
#endif // _TARGET_X86_
}

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)

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

    SLOT instrPtr = (BYTE*)PCODEToPINSTR(pCode);

    // For code sequences of the type
    // BL func1 
    // BL func2    // Safe point 1
    // mov r1 r0  // Safe point 2
    // Both the above safe points instruction must be replaced with gcStress instruction.
    // However as the first safe point is already replaced with gcstress instruction, decoding of the call
    // instruction will fail when processing for the 2nd safe point. Therefore saved instruction must be used instead of
    // instrPtr for decoding the call instruction.
    SLOT savedInstrPtr = ((GCCoverageInfo*)pGCCover)->savedCode + safePointOffset;

    //Determine if instruction before the safe point is call using immediate (BLX Imm)  or call by register (BLX Rm)
    BOOL  instructionIsACallThroughRegister = FALSE;
    BOOL instructionIsACallThroughImmediate = FALSE;
#if defined(_TARGET_ARM_)

    // call by register instruction is two bytes (BL<c> Reg T1 encoding)
    WORD instr = *((WORD*)savedInstrPtr - 1);

    instr = instr & 0xff87;
    if((instr ^ 0x4780) == 0)
        // It is call by register
        instructionIsACallThroughRegister = TRUE;

    // call using immediate instructions are 4 bytes (BL<c> <label> T1 encoding)
    instr = *((WORD*)savedInstrPtr - 2);
    instr = instr & 0xf800;
    if((instr ^ 0xf000) == 0)
        if((*(((WORD*)savedInstrPtr)-1) & 0xd000) == 0xd000)
            // It is call by immediate
            instructionIsACallThroughImmediate = TRUE;
#elif defined(_TARGET_ARM64_)
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
#endif
    // safe point must always be after a call instruction 
    // and cannot be both call by register & immediate
    // The safe points are also marked at jump calls( a special variant of 
    // tail call). However that call site will never appear on the stack.
    // So commenting the assert for now. As for such places the previous
    // instruction will not be a call instruction.
    //_ASSERTE(instructionIsACallThroughRegister ^ instructionIsACallThroughImmediate);

    if(instructionIsACallThroughRegister)
    {
        // If it is call by register then cannot know MethodDesc so replace the call instruction with illegal instruction
        // safe point will be replaced with appropiate illegal instruction at execution time when reg value is known
#if defined(_TARGET_ARM_)
        *((WORD*)instrPtr - 1) = INTERRUPT_INSTR_CALL;
#elif defined(_TARGET_ARM64_)
        *((DWORD*)instrPtr - 1) = INTERRUPT_INSTR_CALL;
#endif
    }
    else if(instructionIsACallThroughImmediate)
    {
        // If it is call by immediate then find the methodDesc
        SLOT nextInstr;
        SLOT target = getTargetOfCall((SLOT)((WORD*)savedInstrPtr-2), NULL, &nextInstr);

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
#if defined(_TARGET_ARM_)
                size_t instrLen = GetARMInstructionLength(instrPtr);
                if (instrLen == 2)
                {
                    _ASSERTE(*((WORD*)instrPtr) != INTERRUPT_INSTR && 
                             *((WORD*)instrPtr) != INTERRUPT_INSTR_CALL &&
                             *((WORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET);
                }
                else 
                {
                    _ASSERTE(*((DWORD*)instrPtr) != INTERRUPT_INSTR_32 && 
                             *((DWORD*)instrPtr) != INTERRUPT_INSTR_CALL_32 &&
                             *((DWORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET_32);
                }
#elif defined(_TARGET_ARM64_)
                {
                    _ASSERTE(*((DWORD*)instrPtr) != INTERRUPT_INSTR && 
                             *((DWORD*)instrPtr) != INTERRUPT_INSTR_CALL &&
                             *((DWORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET);
                }
#endif
                //
                // When applying GC coverage breakpoints at native image load time, the code here runs
                // before eager fixups are applied for the module being loaded.  The direct call target
                // never requires restore, however it is possible that it is initially in an invalid state
                // and remains invalid until one or more eager fixups are applied.
                //
                // MethodDesc::ReturnsObject() consults the method signature, meaning it consults the
                // metadata in the owning module.  For generic instantiations stored in non-preferred
                // modules, reaching the owning module requires following the module override pointer for
                // the enclosing MethodTable.  In this case, the module override pointer is generally
                // invalid until an associated eager fixup is applied.
                //
                // In situations like this, MethodDesc::ReturnsObject() will try to dereference an
                // unresolved fixup and will AV.
                //
                // Given all of this, skip the MethodDesc::ReturnsObject() call by default to avoid
                // unexpected AVs.  This implies leaving out the GC coverage breakpoints for direct calls
                // unless COMPlus_GcStressOnDirectCalls=1 is explicitly set in the environment.
                //

                static ConfigDWORD fGcStressOnDirectCalls;

                if (fGcStressOnDirectCalls.val(CLRConfig::INTERNAL_GcStressOnDirectCalls))
                {
                    // If the method returns an object then should protect the return object
                    if (targetMD->ReturnsObject(true) != MetaSig::RETNONOBJ)
                    {
                        // replace with corresponding 2 or 4 byte illegal instruction (which roots the return value)
#if defined(_TARGET_ARM_)
                        if (instrLen == 2)
                            *((WORD*)instrPtr)  = INTERRUPT_INSTR_PROTECT_RET;
                        else
                            *((DWORD*)instrPtr) = INTERRUPT_INSTR_PROTECT_RET_32;
#elif defined(_TARGET_ARM64_)
                        *((DWORD*)instrPtr) = INTERRUPT_INSTR_PROTECT_RET;
#endif
                    }
                    else // method does not return an objectref
                    {
                        // replace with corresponding 2 or 4 byte illegal instruction
#if defined(_TARGET_ARM_)
                        if (instrLen == 2)
                            *((WORD*)instrPtr)  = INTERRUPT_INSTR;
                        else
                            *((DWORD*)instrPtr) = INTERRUPT_INSTR_32;
#elif defined(_TARGET_ARM64_)
                        *((DWORD*)instrPtr) = INTERRUPT_INSTR;
#endif
                    }
                }
            }
        }
    }
}
#endif

//Replaces the provided interruptible range with corresponding 2 or 4 byte gcStress illegal instruction
bool replaceInterruptibleRangesWithGcStressInstr (UINT32 startOffset, UINT32 stopOffset, LPVOID pGCCover)
{
    PCODE pCode = NULL;
    SLOT rangeStart = NULL;
    SLOT rangeStop = NULL;

    //Interruptible range can span accross hot & cold region
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
        SLOT instrPtr = rangeStart;
        while(instrPtr < rangeStop)
        {

            // The instruction about to be replaced cannot already be a gcstress instruction
#if defined(_TARGET_ARM_)
            size_t instrLen = GetARMInstructionLength(instrPtr);
            if (instrLen == 2)
            {
                _ASSERTE(*((WORD*)instrPtr) != INTERRUPT_INSTR && 
                         *((WORD*)instrPtr) != INTERRUPT_INSTR_CALL &&
                         *((WORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET);
            }
            else 
            {
                _ASSERTE(*((DWORD*)instrPtr) != INTERRUPT_INSTR_32 && 
                         *((DWORD*)instrPtr) != INTERRUPT_INSTR_CALL_32 &&
                         *((DWORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET_32);
            }

            if (instrLen == 2)
                *((WORD*)instrPtr)  = INTERRUPT_INSTR;
            else
            {
                // Do not replace with gcstress interrupt instruction at call to JIT_RareDisableHelper
                if(!isCallToStopForGCJitHelper(instrPtr))
                    *((DWORD*)instrPtr) = INTERRUPT_INSTR_32;
            }

            instrPtr += instrLen;
#elif defined(_TARGET_ARM64_)
            {
                _ASSERTE(*((DWORD*)instrPtr) != INTERRUPT_INSTR && 
                         *((DWORD*)instrPtr) != INTERRUPT_INSTR_CALL &&
                         *((DWORD*)instrPtr) != INTERRUPT_INSTR_PROTECT_RET);
            }
            
            // Do not replace with gcstress interrupt instruction at call to JIT_RareDisableHelper
            if(!isCallToStopForGCJitHelper(instrPtr))
                *((DWORD*)instrPtr) = INTERRUPT_INSTR;
            instrPtr += 4;
#endif

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
#endif

// Is this a call instruction to JIT_RareDisableHelper()
// We cannot insert GCStress instruction at this call
// For arm64 & arm (R2R) call to jithelpers happens via a stub.
// For other architectures call does not happen via stub.
// For other architectures we can get the target directly by calling getTargetOfCall().
// This is not the case for arm64/arm so need to decode the stub
// instruction to find the actual jithelper target. 
// For other architecture we detect call to JIT_RareDisableHelper 
// in function OnGcCoverageInterrupt() since getTargetOfCall() can
// get the actual jithelper target.
bool isCallToStopForGCJitHelper(SLOT instrPtr)
{
#if defined(_TARGET_ARM64_)    
   if (((*reinterpret_cast<DWORD*>(instrPtr)) & 0xFC000000) == 0x94000000) // Do we have a BL instruction?
   {
       // call through immediate
       int imm26 = ((*((DWORD*)instrPtr)) & 0x03FFFFFF)<<2;
       // SignExtend the immediate value.
       imm26 = (imm26 << 4) >> 4;
       DWORD* target = (DWORD*) (instrPtr + imm26);
       // Call to jithelpers happens via jumpstub
       if(*target == 0x58000050 /* ldr xip0, PC+8*/ && *(target+1) == 0xd61f0200 /* br xip0 */)
       {
           // get the actual jithelper target
           target = *(((DWORD**)target) + 1);
           if((TADDR)target == GetEEFuncEntryPoint(JIT_RareDisableHelper))
           {
               return true;
           }
       }
   }
#elif defined(_TARGET_ARM_)
    if((instrPtr[1] & 0xf8) == 0xf0 && (instrPtr[3] & 0xc0) == 0xc0) // call using imm
    {
        int imm32 = GetThumb2BlRel24((UINT16 *)instrPtr);
        WORD* target = (WORD*) (instrPtr + 4 + imm32);
        // Is target a stub
        if(*target == 0xf8df && *(target+1) == 0xf000) // ldr pc, [pc+4]
        {
            //get actual target
            target = *((WORD**)target + 1);
            if((TADDR)target == GetEEFuncEntryPoint(JIT_RareDisableHelper))
            {
                return true;
            }
        }
    }
#endif
   return false;
}

static size_t getRegVal(unsigned regNum, PCONTEXT regs)
{
    return *getRegAddr(regNum, regs);
}

/****************************************************************************/
static SLOT getTargetOfCall(SLOT instrPtr, PCONTEXT regs, SLOT*nextInstr) {

    BYTE sibindexadj = 0;
    BYTE baseadj = 0;
    WORD displace = 0;

    // In certain situations, the instruction bytes are read from a different
    // location than the actual bytes being executed.
    // When decoding the instructions of a method which is sprinkled with 
    // TRAP instructions for GCStress, we decode the bytes from a copy 
    // of the instructions stored before the traps-for-gc were inserted.
    // Hoiwever, the PC-relative addressing/displacement of the CALL-target
    // will still be with respect to the currently executing PC.
    // So, if a register context is available, we pick the PC from it 
    // (for address calculation purposes only). 

    SLOT PC = (regs) ? (SLOT)GetIP(regs) : instrPtr;

#ifdef _TARGET_ARM_
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
#elif defined(_TARGET_ARM64_)
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
#endif

#ifdef _TARGET_AMD64_

    if ((instrPtr[0] & 0xf0) == REX_PREFIX_BASE)
    {
        static_assert_no_msg(REX_SIB_BASE_EXT == REX_MODRM_RM_EXT);
        if (instrPtr[0] & REX_SIB_BASE_EXT)
            baseadj = 8;

        if (instrPtr[0] & REX_SIB_INDEX_EXT)
            sibindexadj = 8;

        instrPtr++;
    }

#endif // _TARGET_AMD64_

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
        return((SLOT)(base + (SSIZE_T)displacement));
    }

    if (instrPtr[0] == 0xFF) { // Indirect Absolute Near

        _ASSERTE(regs);

        BYTE mod = (instrPtr[1] & 0xC0) >> 6;
        BYTE rm  = (instrPtr[1] & 0x7);
        SLOT result;

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
#ifdef _TARGET_AMD64_
                    // at this point instrPtr should be pointing at the beginning
                    // of the byte sequence for the call instruction.  the operand
                    // is a RIP-relative address from the next instruction, so to
                    // calculate the address of the next instruction we need to
                    // jump forward 6 bytes: 1 for the opcode, 1 for the ModRM byte,
                    // and 4 for the operand.  see AMD64 Programmer's Manual Vol 3.
                    result = PC + 6;
#else
                    result = 0;
#endif // _TARGET_AMD64_
                } else {
                    result = (SLOT)getRegVal(baseadj + rm, regs);
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
            result = (SLOT)(*((SLOT *)result));

            break;

        case 3:
        default:

            result = (SLOT)getRegVal(baseadj + rm, regs);
            displace += 2;
            break;

        }

        *nextInstr = instrPtr + displace;
        return result;

    }

    return(0);      // Fail
}

/****************************************************************************/

#ifdef _TARGET_X86_

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

#endif // _TARGET_X86_


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

    GCCoverageInfo *gcCover = codeInfo.GetMethodDesc()->m_GcCover;
    if (gcCover == nullptr)
    {
        return false;
    }

    // Now it's safe to dereference the IP to check the instruction
#if defined(_TARGET_ARM64_)
    UINT32 instructionCode = *reinterpret_cast<UINT32 *>(ip);
#elif defined(_TARGET_ARM_)
    UINT16 instructionCode = *reinterpret_cast<UINT16 *>(ip);
#else
    UINT8 instructionCode = *reinterpret_cast<UINT8 *>(ip);
#endif
    switch (instructionCode)
    {
        case INTERRUPT_INSTR:
        case INTERRUPT_INSTR_CALL:
        case INTERRUPT_INSTR_PROTECT_RET:
            return true;

        default:
            // Another thread may have already changed the code back to the original
            return instructionCode == gcCover->savedCode[codeInfo.GetRelOffset()];
    }
}

// Remove the GcCoverage interrupt instruction, and restore the 
// original instruction. Only one instruction must be used, 
// because multiple threads can be executing the same code stream.

void RemoveGcCoverageInterrupt(TADDR instrPtr, BYTE * savedInstrPtr)
{
#ifdef _TARGET_ARM_
        if (GetARMInstructionLength(savedInstrPtr) == 2)
            *(WORD *)instrPtr  = *(WORD *)savedInstrPtr;
        else
            *(DWORD *)instrPtr = *(DWORD *)savedInstrPtr;
#elif defined(_TARGET_ARM64_)
        *(DWORD *)instrPtr = *(DWORD *)savedInstrPtr;
#else
        *(BYTE *)instrPtr = *savedInstrPtr;
#endif

        FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 4);
}

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

    GCCoverageInfo* gcCover = pMD->m_GcCover;
    forceStack[3] = &gcCover;            // This is so I can see it fastchecked
    if (gcCover == 0)
        return(FALSE);        // we aren't doing code gcCoverage on this function

    BYTE * savedInstrPtr = &gcCover->savedCode[offset];

    // If this trap instruction is taken in place of CORINFO_HELP_STOP_FOR_GC()
    // Do not start a GC, but continue with the original instruction.
    // See the comments above SprinkleBreakpoints() function.
    SLOT nextInstr;
    SLOT target = getTargetOfCall(savedInstrPtr, regs, &nextInstr);

    if (target == (SLOT)JIT_RareDisableHelper) {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr);
        return TRUE;
    }

    Thread* pThread = GetThread();
    _ASSERTE(pThread);

#if defined(USE_REDIRECT_FOR_GCSTRESS) && !defined(PLATFORM_UNIX)
    // If we're unable to redirect, then we simply won't test GC at this
    // location.
    if (!pThread->CheckForAndDoRedirectForGCStress(regs))
    {
        RemoveGcCoverageInterrupt(instrPtr, savedInstrPtr);
    }

#else // !USE_REDIRECT_FOR_GCSTRESS

#ifdef _DEBUG
    if (!g_pConfig->SkipGCCoverage(pMD->GetModule()->GetSimpleName()))
#endif
    DoGcStress(regs, pMD);

#endif // !USE_REDIRECT_FOR_GCSTRESS

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

void DoGcStress (PCONTEXT regs, MethodDesc *pMD)
{
    PCODE controlPc = GetIP(regs);
    TADDR instrPtr = PCODEToPINSTR(controlPc);

    if (!pMD)
    {
        pMD = ExecutionManager::GetCodeMethodDesc(controlPc);
        if (!pMD)
            return;
    }

    GCCoverageInfo *gcCover = pMD->m_GcCover;

    EECodeInfo codeInfo(controlPc);
    _ASSERTE(codeInfo.GetMethodDesc() == pMD);
    DWORD offset = codeInfo.GetRelOffset();

    Thread *pThread = GetThread();

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

    BYTE instrVal = *(BYTE *)instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked
    
    if (instrVal != INTERRUPT_INSTR && 
        instrVal != INTERRUPT_INSTR_CALL && 
        instrVal != INTERRUPT_INSTR_PROTECT_RET) {
        _ASSERTE(instrVal == gcCover->savedCode[offset]);  // someone beat us to it.
        return;       // Someone beat us to it, just go on running
    }

    bool atCall = (instrVal == INTERRUPT_INSTR_CALL);
    bool afterCallProtect = (instrVal == INTERRUPT_INSTR_PROTECT_RET);

#elif defined(_TARGET_ARM_)

    WORD instrVal = *(WORD*)instrPtr;
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    size_t instrLen = GetARMInstructionLength(instrVal);

    bool atCall;
    bool afterCallProtect;

    if (instrLen == 2)
    {
        if (instrVal != INTERRUPT_INSTR && 
            instrVal != INTERRUPT_INSTR_CALL && 
            instrVal != INTERRUPT_INSTR_PROTECT_RET) {
            _ASSERTE(instrVal == *(WORD*)(gcCover->savedCode + offset));  // someone beat us to it.
            return;       // Someone beat us to it, just go on running
        }

        atCall           = (instrVal == INTERRUPT_INSTR_CALL);
        afterCallProtect = (instrVal == INTERRUPT_INSTR_PROTECT_RET);
    }
    else
    {
        _ASSERTE(instrLen == 4);

        DWORD instrVal32 = *(DWORD*)instrPtr;

        if (instrVal32 != INTERRUPT_INSTR_32 && 
            instrVal32 != INTERRUPT_INSTR_CALL_32 && 
            instrVal32 != INTERRUPT_INSTR_PROTECT_RET_32) {
            _ASSERTE(instrVal32 == *(DWORD*)(gcCover->savedCode + offset));  // someone beat us to it.
            return;       // Someone beat us to it, just go on running
        }

        atCall           = (instrVal32 == INTERRUPT_INSTR_CALL_32);
        afterCallProtect = (instrVal32 == INTERRUPT_INSTR_PROTECT_RET_32);
    }
#elif defined(_TARGET_ARM64_)
    DWORD instrVal = *(DWORD *)instrPtr; 
    forceStack[6] = &instrVal;            // This is so I can see it fastchecked

    if (instrVal != INTERRUPT_INSTR && 
        instrVal != INTERRUPT_INSTR_CALL && 
        instrVal != INTERRUPT_INSTR_PROTECT_RET) {
        _ASSERTE(instrVal == *(DWORD *)(gcCover->savedCode + offset));  // someone beat us to it.
        return;       // Someone beat us to it, just go on running
    }

    bool atCall = (instrVal == INTERRUPT_INSTR_CALL);
    bool afterCallProtect = (instrVal == INTERRUPT_INSTR_PROTECT_RET);

#endif // _TARGET_*

#ifdef _TARGET_X86_
    /* are we at the very first instruction?  If so, capture the register state */
    bool bShouldUpdateProlog = true;
    if (gcCover->doingEpilogChecks) {
        if (offset == 0) {
            if (gcCover->callerThread == 0) {
                if (FastInterlockCompareExchangePointer(&gcCover->callerThread, pThread, 0) == 0) {
                    gcCover->callerRegs = *regs;
                    gcCover->gcCount = GCHeapUtilities::GetGCHeap()->GetGcCount();
                    bShouldUpdateProlog = false;
                }
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
        // for this routine since the barrier at the begining of the routine may not
        // be up anymore, and thus the caller context is now not guaranteed to be correct.  
        // This should happen only very rarely so is not a big deal.
        if (gcCover->callerThread != pThread)
            gcCover->doingEpilogChecks = false;
    }

    instrVal = gcCover->savedCode[offset];
#endif // _TARGET_X86_    


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
#ifdef _TARGET_X86_
    // only restore the original instruction if: 
    //    this is not the first instruction in the method's prolog, or
    //    if it is, only if this is the second time we run in this method
    // note that if this is the second time in the prolog we've already disabled epilog checks
    if (offset != 0 || bShouldUpdateProlog)
#endif
    pThread->PostGCStressInstructionUpdate((BYTE*)instrPtr, &gcCover->savedCode[offset]);
    
#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)

    /* In non-fully interrruptable code, if the EIP is just after a call instr
       means something different because it expects that that we are IN the 
       called method, not actually at the instruction just after the call. This
       is important, because until the called method returns, IT is responsible
       for protecting the return value.  Thus just after a call instruction
       we have to protect EAX if the method being called returns a GC pointer.

       To figure this out, we need to stop AT the call so we can determine the
       target (and thus whether it returns a GC pointer), and then place the
       a different interrupt instruction so that the GCCover harness protects
       EAX before doing the GC).  This effectively simulates a hijack in 
       non-fully interruptible code */

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
        BYTE* nextInstr;        
        SLOT target = getTargetOfCall((BYTE*) instrPtr, regs, (BYTE**)&nextInstr);
        if (target != 0)
        {
            if (!pThread->PreemptiveGCDisabled())
            {
                // We are in preemtive mode in JITTed code. This implies that we are into IL stub 
                // close to PINVOKE method. This call will never return objectrefs.
#ifdef _TARGET_ARM_
                    size_t instrLen = GetARMInstructionLength(nextInstr);
                    if (instrLen == 2)
                        *(WORD*)nextInstr  = INTERRUPT_INSTR;
                    else
                        *(DWORD*)nextInstr = INTERRUPT_INSTR_32;
#elif defined(_TARGET_ARM64_)
                        *(DWORD*)nextInstr = INTERRUPT_INSTR;
#else
                        *nextInstr = INTERRUPT_INSTR;
#endif
            }
            else
            {
                MethodDesc* targetMD = getTargetMethodDesc((PCODE)target);

                if (targetMD != 0)
                {
                    // Mark that we are performing a stackwalker like operation on the current thread.
                    // This is necessary to allow the ReturnsObject function to work without triggering any loads
                    ClrFlsValueSwitch _threadStackWalking(TlsIdx_StackWalkerWalkingThread, pThread);

                    // @Todo: possible race here, might need to be fixed  if it become a problem.
                    // It could become a problem if 64bit does partially interrupt work.
                    // OK, we have the MD, mark the instruction after the CALL
                    // appropriately
#ifdef _TARGET_ARM_
                    size_t instrLen = GetARMInstructionLength(nextInstr);
                    if (targetMD->ReturnsObject(true) != MetaSig::RETNONOBJ)
                        if (instrLen == 2)
                            *(WORD*)nextInstr  = INTERRUPT_INSTR_PROTECT_RET;
                        else
                            *(DWORD*)nextInstr = INTERRUPT_INSTR_PROTECT_RET_32;
                    else
                        if (instrLen == 2)
                            *(WORD*)nextInstr  = INTERRUPT_INSTR;
                        else
                            *(DWORD*)nextInstr = INTERRUPT_INSTR_32;
#elif defined(_TARGET_ARM64_)
                    if (targetMD->ReturnsObject(true) != MetaSig::RETNONOBJ)
                        *(DWORD *)nextInstr = INTERRUPT_INSTR_PROTECT_RET;  
                    else
                        *(DWORD *)nextInstr = INTERRUPT_INSTR;
#else
                    if (targetMD->ReturnsObject(true) != MetaSig::RETNONOBJ)
                        *nextInstr = INTERRUPT_INSTR_PROTECT_RET;  
                    else
                        *nextInstr = INTERRUPT_INSTR;
#endif
                }
            }
        }

        // Must flush instruction cache before returning as instruction has been modified.
        // Note this needs to reach beyond the call by up to 4 bytes.
        FlushInstructionCache(GetCurrentProcess(), (LPCVOID)instrPtr, 10);

        // It's not GC safe point, the GC Stress instruction is 
        // already commited and interrupt is already put at next instruction so we just return.
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


#if !defined(USE_REDIRECT_FOR_GCSTRESS)
    //
    // If we redirect for gc stress, we don't need this frame on the stack,
    // the redirection will push a resumable frame.
    //
    FrameWithCookie<ResumableFrame> frame(regs);
    frame.Push(pThread);
#endif // USE_REDIRECT_FOR_GCSTRESS

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    FrameWithCookie<GCFrame> gcFrame;

    OBJECTREF* retPointer = 0;
    UINT numObjRefs = 1;
#if defined(UNIX_AMD64_ABI) || defined(_TARGET_ARM64_)
    // These targets support multireg returns.
    DWORD_PTR retValArray[2];
    retPointer = (OBJECTREF*)retValArray;
#else // ARM32, x86, AMD64 Windows.
    DWORD_PTR retVal;
    retPointer = (OBJECTREF*)&retVal;
#endif

    if (afterCallProtect)   // Do I need to protect return value?
    {
#if defined(UNIX_AMD64_ABI)
        numObjRefs = 2;
        retValArray[0] = regs->Rax;
        retValArray[1] = regs->Rdx;
#elif defined(_TARGET_AMD64_)
        retVal = regs->Rax;
#elif defined(_TARGET_X86_)
        retVal = regs->Eax;
#elif defined(_TARGET_ARM_)
        retVal = regs->R0;
#elif defined(_TARGET_ARM64_)
        numObjRefs = 2;
        retValArray[0] = regs->X0;
        retValArray[1] = regs->X1;
#else
        PORTABILITY_ASSERT("DoGCStress - return register");
#endif
        gcFrame.Init(pThread, retPointer, numObjRefs, TRUE);
    }
#endif // _TARGET_*

    if (gcCover->lastMD != pMD) 
    {
        LOG((LF_GCROOTS, LL_INFO100000, "GCCOVER: Doing GC at method %s::%s offset 0x%x\n",
                 pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, offset));
        gcCover->lastMD =pMD;
    } 
    else 
    {
        LOG((LF_GCROOTS, LL_EVERYTHING, "GCCOVER: Doing GC at method %s::%s offset 0x%x\n",
                pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, offset));
    }

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

    if (afterCallProtect) 
    {
#if defined(UNIX_AMD64_ABI)
        regs->Rax = retValArray[0];
        regs->Rdx = retValArray[1];
#elif  _TARGET_AMD64_
        regs->Rax = retVal;
#elif defined(_TARGET_X86_)
        regs->Eax = retVal;
#elif defined(_TARGET_ARM_)
        regs->R0 = retVal;
#elif defined(_TARGET_ARM64_)
        regs->X0 = retValArray[0];
        regs->X1 = retValArray[1];
#else
        PORTABILITY_ASSERT("DoGCStress - return register");
#endif
        gcFrame.Pop();
    }

#if !defined(USE_REDIRECT_FOR_GCSTRESS)
    frame.Pop(pThread);
#endif // USE_REDIRECT_FOR_GCSTRESS

    if (enableWhenDone)
    {
        BOOL b = GC_ON_TRANSITIONS(FALSE);      // Don't do a GCStress 3 GC here
        pThread->EnablePreemptiveGC();
        GC_ON_TRANSITIONS(b);
    }

    return;
   
}

#endif // HAVE_GCCOVER

