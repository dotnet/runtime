// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#include "ecall.h"
#include "eetwain.h"
#include "dbginterface.h"
#include "gcenv.h"

#ifdef USE_GC_INFO_DECODER
#include "gcinfodecoder.h"
#endif

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#ifdef TARGET_X86
void promoteVarArgs(PTR_BYTE argsStart, PTR_VASigCookie varArgSig, GCCONTEXT* ctx);
#include "unwind_x86.inl"
#endif

#include "argdestination.h"

#ifndef DACCESS_COMPILE
#ifndef FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Setup context to enter an exception handler (a 'catch' block).
 *  This is the last chance for the runtime support to do fixups in
 *  the context before execution continues inside a filter, catch handler,
 *  or finally.
 */
void EECodeManager::FixContext( ContextType     ctxType,
                                EHContext      *ctx,
                                EECodeInfo     *pCodeInfo,
                                DWORD           dwRelOffset,
                                DWORD           nestingLevel,
                                OBJECTREF       thrownObject,
                                CodeManState   *pState,
                                size_t       ** ppShadowSP,
                                size_t       ** ppEndRegion)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE((ctxType == FINALLY_CONTEXT) == (thrownObject == NULL));

    _ASSERTE(sizeof(CodeManStateBuf) <= sizeof(pState->stateBuf));
    CodeManStateBuf * stateBuf = (CodeManStateBuf*)pState->stateBuf;

    /* Extract the necessary information from the info block header */

    stateBuf->hdrInfoSize = (DWORD)DecodeGCHdrInfo(pCodeInfo->GetGCInfoToken(),
                                       dwRelOffset,
                                       &stateBuf->hdrInfoBody);
    pState->dwIsSet = 1;

#ifdef  _DEBUG
    if (trFixContext) {
        printf("FixContext [%s][%s] for %s.%s: ",
               stateBuf->hdrInfoBody.ebpFrame?"ebp":"   ",
               stateBuf->hdrInfoBody.interruptible?"int":"   ",
               "UnknownClass","UnknownMethod");
        fflush(stdout);
    }
#endif

    /* make sure that we have an ebp stack frame */

    _ASSERTE(stateBuf->hdrInfoBody.ebpFrame);
    _ASSERTE(stateBuf->hdrInfoBody.handlers); // <TODO>@TODO : This will always be set. Remove it</TODO>

    TADDR      baseSP;
    GetHandlerFrameInfo(&stateBuf->hdrInfoBody, ctx->Ebp,
                                ctxType == FILTER_CONTEXT ? ctx->Esp : IGNORE_VAL,
                                ctxType == FILTER_CONTEXT ? (DWORD) IGNORE_VAL : nestingLevel,
                                &baseSP,
                                &nestingLevel);

    _ASSERTE((size_t)ctx->Ebp >= baseSP);
    _ASSERTE(baseSP >= (size_t)ctx->Esp);

    ctx->Esp = (DWORD)baseSP;

    // EE will write Esp to **pShadowSP before jumping to handler

    PTR_TADDR pBaseSPslots =
        GetFirstBaseSPslotPtr(ctx->Ebp, &stateBuf->hdrInfoBody);
    *ppShadowSP = (size_t *)&pBaseSPslots[-(int) nestingLevel   ];
                   pBaseSPslots[-(int)(nestingLevel+1)] = 0; // Zero out the next slot

    // EE will write the end offset of the filter
    if (ctxType == FILTER_CONTEXT)
        *ppEndRegion = (size_t *)pBaseSPslots + 1;

    /*  This is just a simple assignment of throwObject to ctx->Eax,
        just pretend the cast goo isn't there.
     */

    *((OBJECTREF*)&(ctx->Eax)) = thrownObject;
}

#endif // !FEATURE_EH_FUNCLETS





/*****************************************************************************/

bool        VarIsInReg(ICorDebugInfo::VarLoc varLoc)
{
    LIMITED_METHOD_CONTRACT;

    switch(varLoc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
    case ICorDebugInfo::VLT_REG_REG:
    case ICorDebugInfo::VLT_REG_STK:
        return true;

    default:
        return false;
    }
}

#ifdef FEATURE_REMAP_FUNCTION
/*****************************************************************************
 *  Last chance for the runtime support to do fixups in the context
 *  before execution continues inside an EnC updated function.
 *  It also adjusts ESP and munges on the stack. So the caller has to make
 *  sure that this stack region is not needed (by doing a localloc).
 *  Also, if this returns EnC_FAIL, we should not have munged the
 *  context ie. transcated commit
 *  The plan of attack is:
 *  1) Error checking up front.  If we get through here, everything
 *      else should work
 *  2) Get all the info about current variables, registers, etc
 *  3) zero out the stack frame - this'll initialize _all_ variables
 *  4) Put the variables from step 3 into their new locations.
 *
 *  Note that while we use the ShuffleVariablesGet/Set methods, they don't
 *  have any info/logic that's internal to the runtime: another codemanger
 *  could easily duplicate what they do, which is why we're calling into them.
 */

HRESULT EECodeManager::FixContextForEnC(PCONTEXT         pCtx,
                                        EECodeInfo *     pOldCodeInfo,
                   const ICorDebugInfo::NativeVarInfo *  oldMethodVars,
                                        SIZE_T           oldMethodVarsCount,
                                        EECodeInfo *     pNewCodeInfo,
                   const ICorDebugInfo::NativeVarInfo *  newMethodVars,
                                        SIZE_T           newMethodVarsCount)
{
    CONTRACTL {
        DISABLED(NOTHROW);
        DISABLED(GC_NOTRIGGER);
    } CONTRACTL_END;

    HRESULT hr = S_OK;

     // Grab a copy of the context before the EnC update.
    T_CONTEXT oldCtx = *pCtx;

#if defined(TARGET_X86)

    /* Extract the necessary information from the info block header */

    hdrInfo  oldInfo, newInfo;

    DecodeGCHdrInfo(pOldCodeInfo->GetGCInfoToken(),
                       pOldCodeInfo->GetRelOffset(),
                       &oldInfo);

    DecodeGCHdrInfo(pNewCodeInfo->GetGCInfoToken(),
                       pNewCodeInfo->GetRelOffset(),
                       &newInfo);

    //1) Error checking up front.  If we get through here, everything
    //     else should work

    if (!oldInfo.editNcontinue || !newInfo.editNcontinue) {
        LOG((LF_ENC, LL_INFO100, "**Error** EECM::FixContextForEnC EnC_INFOLESS_METHOD\n"));
        return CORDBG_E_ENC_INFOLESS_METHOD;
    }

    if (!oldInfo.ebpFrame || !newInfo.ebpFrame) {
        LOG((LF_ENC, LL_INFO100, "**Error** EECM::FixContextForEnC Esp frames NYI\n"));
        return E_FAIL; // Esp frames NYI
    }

    if (pCtx->Esp != pCtx->Ebp - oldInfo.stackSize + sizeof(DWORD)) {
        LOG((LF_ENC, LL_INFO100, "**Error** EECM::FixContextForEnC stack should be empty\n"));
        return E_FAIL; // stack should be empty - <TODO> @TODO : Barring localloc</TODO>
    }

    if (oldInfo.handlers)
    {
        bool      hasInnerFilter;
        TADDR     baseSP;
        FrameType frameType = GetHandlerFrameInfo(&oldInfo, pCtx->Ebp,
                                                  pCtx->Esp, IGNORE_VAL,
                                                  &baseSP, NULL, &hasInnerFilter);
        _ASSERTE(frameType != FR_INVALID);
        _ASSERTE(!hasInnerFilter); // FixContextForEnC() is called for bottommost funclet

        // If the method is in a fuclet, and if the framesize grows, we are in trouble.

        if (frameType != FR_NORMAL)
        {
           /* <TODO> @TODO : What if the new method offset is in a fuclet,
              and the old is not, or the nesting level changed, etc </TODO> */

            if (oldInfo.stackSize != newInfo.stackSize) {
                LOG((LF_ENC, LL_INFO100, "**Error** EECM::FixContextForEnC stack size mismatch\n"));
                return CORDBG_E_ENC_IN_FUNCLET;
            }
        }
    }

    /* @TODO: Check if we have grown out of space for locals, in the face of localloc */
    _ASSERTE(!oldInfo.localloc && !newInfo.localloc);

    // @TODO: If nesting level grows above the MAX_EnC_HANDLER_NESTING_LEVEL,
    // we should return EnC_NESTED_HANLDERS
    _ASSERTE(oldInfo.handlers && newInfo.handlers);

    LOG((LF_ENC, LL_INFO100, "EECM::FixContextForEnC: Checks out\n"));

#elif defined(TARGET_AMD64) || defined(TARGET_ARM64)

    // Strategy for zeroing out the frame on x64:
    //
    // The stack frame looks like this (stack grows up)
    //
    // =======================================
    //             <--- RSP == RBP (invariant: localalloc disallowed before remap)
    // Arguments for next call (if there is one)
    // PSPSym (optional)
    // JIT temporaries (if any)
    // Security object (if any)
    // Local variables (if any)
    // ---------------------------------------
    // Frame header (stuff we must preserve, such as bool for synchronized
    // methods, saved FP, saved callee-preserved registers, etc.)
    // Return address (also included in frame header)
    // ---------------------------------------
    // Arguments for this frame (that's getting remapped).  Will naturally be preserved
    // since fixed-frame size doesn't include this.
    // =======================================
    //
    // Goal: Zero out everything AFTER (above) frame header.
    //
    // How do we find this stuff?
    //
    // EECodeInfo::GetFixedStackSize() gives us the full size from the top ("Arguments
    // for next call") all the way down to and including Return Address.
    //
    // GetSizeOfEditAndContinuePreservedArea() gives us the size in bytes of the
    // frame header at the bottom.
    //
    // So we start at RSP, and zero out:
    //     GetFixedStackSize() - GetSizeOfEditAndContinuePreservedArea() bytes.
    //
    // We'll need to restore PSPSym; location gotten from GCInfo.
    // We'll need to copy security object; location gotten from GCInfo.
    //
    // On ARM64 the JIT generates a slightly different frame and we do not have
    // the invariant FP == SP, since the FP needs to point at the saved fp/lr
    // pair for ETW stack walks. The frame there looks something like:
    // =======================================
    // Arguments for next call (if there is one)     <- SP
    // JIT temporaries
    // Locals
    // PSPSym
    // ---------------------------------------    ^ zeroed area
    // MonitorAcquired (for synchronized methods)
    // Saved FP                                      <- FP
    // Saved LR
    // ---------------------------------------    ^ preserved area
    // Arguments
    //
    // The JIT reports the size of the "preserved" area, which includes
    // MonitorAcquired when it is present. It could also include other local
    // values that need to be preserved across EnC transitions, but no explicit
    // treatment of these is necessary here beyond preserving the values in
    // this region.

    // GCInfo for old method
    GcInfoDecoder oldGcDecoder(
        pOldCodeInfo->GetGCInfoToken(),
        GcInfoDecoderFlags(DECODE_SECURITY_OBJECT | DECODE_PSP_SYM | DECODE_EDIT_AND_CONTINUE),
        0       // Instruction offset (not needed)
        );

    // GCInfo for new method
    GcInfoDecoder newGcDecoder(
        pNewCodeInfo->GetGCInfoToken(),
        GcInfoDecoderFlags(DECODE_SECURITY_OBJECT | DECODE_PSP_SYM | DECODE_EDIT_AND_CONTINUE),
        0       // Instruction offset (not needed)
        );

    UINT32 oldSizeOfPreservedArea = oldGcDecoder.GetSizeOfEditAndContinuePreservedArea();
    UINT32 newSizeOfPreservedArea = newGcDecoder.GetSizeOfEditAndContinuePreservedArea();

    LOG((LF_CORDB, LL_INFO100, "EECM::FixContextForEnC: Got old and new EnC preserved area sizes of %u and %u\n", oldSizeOfPreservedArea, newSizeOfPreservedArea));
    // This ensures the JIT generated EnC compliant code.
    if ((oldSizeOfPreservedArea == NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA) ||
        (newSizeOfPreservedArea == NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA))
    {
        _ASSERTE(!"FixContextForEnC called on a non-EnC-compliant method frame");
        return CORDBG_E_ENC_INFOLESS_METHOD;
    }

    TADDR oldStackBase = GetSP(&oldCtx);

    LOG((LF_CORDB, LL_INFO100, "EECM::FixContextForEnC: Old SP=%p, FP=%p\n", (void*)oldStackBase, (void*)GetFP(&oldCtx)));

#if defined(TARGET_AMD64)
    // Note: we cannot assert anything about the relationship between oldFixedStackSize
    // and newFixedStackSize.  It's possible the edited frame grows (new locals) or
    // shrinks (less temporaries).
    DWORD oldFixedStackSize = pOldCodeInfo->GetFixedStackSize();
    DWORD newFixedStackSize = pNewCodeInfo->GetFixedStackSize();

    // This verifies no localallocs were used in the old method.
    // JIT is required to emit frame register for EnC-compliant code
    _ASSERTE(pOldCodeInfo->HasFrameRegister());
    _ASSERTE(pNewCodeInfo->HasFrameRegister());

#elif defined(TARGET_ARM64)
    DWORD oldFixedStackSize = oldGcDecoder.GetSizeOfEditAndContinueFixedStackFrame();
    DWORD newFixedStackSize = newGcDecoder.GetSizeOfEditAndContinueFixedStackFrame();
#else
    PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif

    LOG((LF_CORDB, LL_INFO100, "EECM::FixContextForEnC: Old and new fixed stack sizes are %u and %u\n", oldFixedStackSize, newFixedStackSize));

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
    // win-x64: SP == FP before localloc
    if (oldStackBase != GetFP(&oldCtx))
    {
        return E_FAIL;
    }
#else
    // All other 64-bit targets use frame chaining with the FP stored right below the
    // return address (LR is always pushed on arm64). FP + 16 == SP + oldFixedStackSize
    // gives the caller's SP before stack alloc.
    if (GetFP(&oldCtx) + 16 != oldStackBase + oldFixedStackSize)
    {
        return E_FAIL;
    }
#endif

    // EnC remap inside handlers is not supported
    if (pOldCodeInfo->IsFunclet() || pNewCodeInfo->IsFunclet())
        return CORDBG_E_ENC_IN_FUNCLET;

    if (oldSizeOfPreservedArea != newSizeOfPreservedArea)
    {
        _ASSERTE(!"FixContextForEnC called with method whose frame header size changed from old to new version.");
        return E_FAIL;
    }

    TADDR callerSP = oldStackBase + oldFixedStackSize;

#ifdef _DEBUG
    // If the old method has a PSPSym, then its value should == initial-SP (i.e.
    // oldStackBase) for x64 and callerSP for arm64
    INT32 nOldPspSymStackSlot = oldGcDecoder.GetPSPSymStackSlot();
    if (nOldPspSymStackSlot != NO_PSP_SYM)
    {
#if defined(TARGET_AMD64)
        TADDR oldPSP = *PTR_TADDR(oldStackBase + nOldPspSymStackSlot);
        _ASSERTE(oldPSP == oldStackBase);
#else
        TADDR oldPSP = *PTR_TADDR(callerSP + nOldPspSymStackSlot);
        _ASSERTE(oldPSP == callerSP);
#endif
    }
#endif // _DEBUG

#else
    PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif

    // 2) Get all the info about current variables, registers, etc

    const ICorDebugInfo::NativeVarInfo *  pOldVar;

    // sorted by varNumber
    ICorDebugInfo::NativeVarInfo * oldMethodVarsSorted = NULL;
    ICorDebugInfo::NativeVarInfo * oldMethodVarsSortedBase = NULL;
    ICorDebugInfo::NativeVarInfo *newMethodVarsSorted = NULL;
    ICorDebugInfo::NativeVarInfo *newMethodVarsSortedBase = NULL;

    SIZE_T *rgVal1 = NULL;
    SIZE_T *rgVal2 = NULL;

    {
        SIZE_T local;

        // We'll need to sort the old native var info by variable number, since the
        // order of them isn't necc. the same.  We'll use the number as the key.
        // We will assume we may have hidden arguments (which have negative values as the index)

        unsigned oldNumVars = unsigned(-ICorDebugInfo::UNKNOWN_ILNUM);
        for (pOldVar = oldMethodVars, local = 0;
             local < oldMethodVarsCount;
             local++, pOldVar++)
        {
            DWORD varNumber = pOldVar->varNumber;
            if (signed(varNumber) >= 0)
            {
                // This is an explicit (not special) var, so add its varNumber + 1 to our
                // max count ("+1" because varNumber is zero-based).
                oldNumVars = max(oldNumVars, unsigned(-ICorDebugInfo::UNKNOWN_ILNUM) + varNumber + 1);
            }
        }

        oldMethodVarsSortedBase = new (nothrow) ICorDebugInfo::NativeVarInfo[oldNumVars];
        if (!oldMethodVarsSortedBase)
        {
            hr = E_FAIL;
            goto ErrExit;
        }
        oldMethodVarsSorted = oldMethodVarsSortedBase + (-ICorDebugInfo::UNKNOWN_ILNUM);

        memset((void *)oldMethodVarsSortedBase, 0, oldNumVars * sizeof(ICorDebugInfo::NativeVarInfo));

        for (local = 0; local < oldNumVars;local++)
             oldMethodVarsSortedBase[local].loc.vlType = ICorDebugInfo::VLT_INVALID;

        BYTE **rgVCs = NULL;
        DWORD oldMethodOffset = pOldCodeInfo->GetRelOffset();

        for (pOldVar = oldMethodVars, local = 0;
             local < oldMethodVarsCount;
             local++, pOldVar++)
        {
            DWORD varNumber = pOldVar->varNumber;

            _ASSERTE(varNumber + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM) < oldNumVars);

            // Only care about old local variables alive at oldMethodOffset
            if (pOldVar->startOffset <= oldMethodOffset &&
                pOldVar->endOffset   >  oldMethodOffset)
            {
                // Indexing should be performed with a signed value - could be negative.
                oldMethodVarsSorted[(int32_t)varNumber] = *pOldVar;
            }
        }

        // 3) Next sort the new var info by varNumber.  We want to do this here, since
        // we're allocating memory (which may fail) - do this before going to step 2

        // First, count the new vars the same way we did the old vars above.

        const ICorDebugInfo::NativeVarInfo * pNewVar;

        unsigned newNumVars = unsigned(-ICorDebugInfo::UNKNOWN_ILNUM);
        for (pNewVar = newMethodVars, local = 0;
             local < newMethodVarsCount;
             local++, pNewVar++)
        {
            DWORD varNumber = pNewVar->varNumber;
            if (signed(varNumber) >= 0)
            {
                // This is an explicit (not special) var, so add its varNumber + 1 to our
                // max count ("+1" because varNumber is zero-based).
                newNumVars = max(newNumVars, unsigned(-ICorDebugInfo::UNKNOWN_ILNUM) + varNumber + 1);
            }
        }

        // sorted by varNumber
        newMethodVarsSortedBase = new (nothrow) ICorDebugInfo::NativeVarInfo[newNumVars];
        if (!newMethodVarsSortedBase)
        {
            hr = E_FAIL;
            goto ErrExit;
        }
        newMethodVarsSorted = newMethodVarsSortedBase + (-ICorDebugInfo::UNKNOWN_ILNUM);

        memset(newMethodVarsSortedBase, 0, newNumVars * sizeof(ICorDebugInfo::NativeVarInfo));
        for (local = 0; local < newNumVars;local++)
             newMethodVarsSortedBase[local].loc.vlType = ICorDebugInfo::VLT_INVALID;

        DWORD newMethodOffset = pNewCodeInfo->GetRelOffset();

        for (pNewVar = newMethodVars, local = 0;
             local < newMethodVarsCount;
             local++, pNewVar++)
        {
            DWORD varNumber = pNewVar->varNumber;

            _ASSERTE(varNumber + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM) < newNumVars);

            // Only care about new local variables alive at newMethodOffset
            if (pNewVar->startOffset <= newMethodOffset &&
                pNewVar->endOffset   >  newMethodOffset)
            {
                // Indexing should be performed with a signed valued - could be negative.
                newMethodVarsSorted[(int32_t)varNumber] = *pNewVar;
            }
        }

        _ASSERTE(newNumVars >= oldNumVars ||
                 !"Not allowed to reduce the number of locals between versions!");

        LOG((LF_ENC, LL_INFO100, "EECM::FixContextForEnC: gathered info!\n"));

        rgVal1 = new (nothrow) SIZE_T[newNumVars];
        if (rgVal1 == NULL)
        {
            hr = E_FAIL;
            goto ErrExit;
        }

        rgVal2 = new (nothrow) SIZE_T[newNumVars];
        if (rgVal2 == NULL)
        {
            hr = E_FAIL;
            goto ErrExit;
        }

        // 4) Next we'll zero them out, so any variables that aren't in scope
        // in the old method, but are in scope in the new, will have the
        // default, zero, value.

        memset(rgVal1, 0, sizeof(SIZE_T) * newNumVars);
        memset(rgVal2, 0, sizeof(SIZE_T) * newNumVars);

        unsigned varsToGet = (oldNumVars > newNumVars)
                ? newNumVars
                : oldNumVars;

         //  2) Get all the info about current variables, registers, etc.

        hr = g_pDebugInterface->GetVariablesFromOffset(pOldCodeInfo->GetMethodDesc(),
                                                       varsToGet,
                                                       oldMethodVarsSortedBase,
                                                       oldMethodOffset,
                                                       &oldCtx,
                                                       rgVal1,
                                                       rgVal2,
                                                       newNumVars,
                                                       &rgVCs);
        if (FAILED(hr))
        {
            goto ErrExit;
        }


        LOG((LF_ENC, LL_INFO100, "EECM::FixContextForEnC: got vars!\n"));

        /*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*
         *  IMPORTANT : Once we start munging on the context, we cannot return
         *  EnC_FAIL, as this should be a transacted commit,
         **=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*/

#if defined(TARGET_X86)
        // Zero out all  the registers as some may hold new variables.
        pCtx->Eax = pCtx->Ecx = pCtx->Edx = pCtx->Ebx = pCtx->Esi = pCtx->Edi = 0;

        // 3) zero out the stack frame - this'll initialize _all_ variables

        /*-------------------------------------------------------------------------
         * Adjust the stack height
         */
        pCtx->Esp -= (newInfo.stackSize - oldInfo.stackSize);

        // Zero-init the local and tempory section of new stack frame being careful to avoid
        // touching anything in the frame header.
        // This is necessary to ensure that any JIT temporaries in the old version can't be mistaken
        // for ObjRefs now.
        size_t frameHeaderSize = GetSizeOfFrameHeaderForEnC( &newInfo );
        _ASSERTE( frameHeaderSize <= oldInfo.stackSize );
        _ASSERTE( GetSizeOfFrameHeaderForEnC( &oldInfo ) == frameHeaderSize );

#elif defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)

        // Next few statements zero out all registers that may end up holding new variables.

        // volatile int registers (JIT may use these to enregister variables)
        pCtx->Rax = pCtx->Rcx = pCtx->Rdx = pCtx->R8 = pCtx->R9 = pCtx->R10 = pCtx->R11 = 0;

        // volatile float registers
        pCtx->Xmm1.High = pCtx->Xmm1.Low = 0;
        pCtx->Xmm2.High = pCtx->Xmm2.Low = 0;
        pCtx->Xmm3.High = pCtx->Xmm3.Low = 0;
        pCtx->Xmm4.High = pCtx->Xmm4.Low = 0;
        pCtx->Xmm5.High = pCtx->Xmm5.Low = 0;

        // 3) zero out the stack frame - this'll initialize _all_ variables

        /*-------------------------------------------------------------------------
        * Adjust the stack height
        */

        TADDR newStackBase = callerSP - newFixedStackSize;

        SetSP(pCtx, newStackBase);

        // We want to zero-out everything pushed after the frame header. This way we'll zero
        // out locals (both old & new) and temporaries. This is necessary to ensure that any
        // JIT temporaries in the old version can't be mistaken for ObjRefs now. (I am told
        // this last point is less of an issue on x64 as it is on x86, but zeroing out the
        // temporaries is still the cleanest, most robust way to go.)
        size_t frameHeaderSize = newSizeOfPreservedArea;
        _ASSERTE(frameHeaderSize <= oldFixedStackSize);
        _ASSERTE(frameHeaderSize <= newFixedStackSize);

        // For EnC-compliant x64 code, FP == SP.  Since SP changed above, update FP now
        pCtx->Rbp = newStackBase;

#else
#if defined(TARGET_ARM64)
        // Zero out volatile part of stack frame
        // x0-x17
        memset(&pCtx->X[0], 0, sizeof(pCtx->X[0]) * 18);
        // v0-v7
        memset(&pCtx->V[0], 0, sizeof(pCtx->V[0]) * 8);
        // v16-v31
        memset(&pCtx->V[16], 0, sizeof(pCtx->V[0]) * 16);
#elif defined(TARGET_AMD64)
        // SysV ABI
        pCtx->Rax = pCtx->Rdi = pCtx->Rsi = pCtx->Rdx = pCtx->Rcx = pCtx->R8 = pCtx->R9 = 0;

        // volatile float registers
        memset(&pCtx->Xmm0, 0, sizeof(pCtx->Xmm0) * 16);
#else
        PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif

        TADDR newStackBase = callerSP - newFixedStackSize;

        SetSP(pCtx, newStackBase);

        size_t frameHeaderSize = newSizeOfPreservedArea;
        _ASSERTE(frameHeaderSize <= oldFixedStackSize);
        _ASSERTE(frameHeaderSize <= newFixedStackSize);

        // EnC prolog saves only FP (and LR on arm64), and FP points to saved FP for frame chaining.
        // These should already be set up from previous version.
        _ASSERTE(GetFP(pCtx) == callerSP - 16);
#endif

        // Perform some debug-only sanity checks on stack variables.  Some checks are
        // performed differently between X86/AMD64.

#ifdef _DEBUG
        for( unsigned i = 0; i < newNumVars; i++ )
        {
            // Make sure that stack variables existing in both old and new methods did not
            // move.  This matters if the address of a local is used in the remapped method.
            // For example:
            //
            //    static unsafe void Main(string[] args)
            //    {
            //        int x;
            //        int* p = &x;
            //                 <- Edit made here - cannot move address of x
            //        *p = 5;
            //    }
            //
            if ((i + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM) < oldNumVars) &&  // Does variable exist in old method?
                 (oldMethodVarsSorted[i].loc.vlType == ICorDebugInfo::VLT_STK) &&   // Is the variable on the stack?
                 (newMethodVarsSorted[i].loc.vlType == ICorDebugInfo::VLT_STK))
            {
                SIZE_T * pOldVarStackLocation = NativeVarStackAddr(oldMethodVarsSorted[i].loc, &oldCtx);
                SIZE_T * pNewVarStackLocation = NativeVarStackAddr(newMethodVarsSorted[i].loc, pCtx);
                _ASSERTE(pOldVarStackLocation == pNewVarStackLocation);
            }

            // Sanity-check that the range we're clearing contains all of the stack variables

#if defined(TARGET_X86)
            const ICorDebugInfo::VarLoc &varLoc = newMethodVarsSortedBase[i].loc;
            if( varLoc.vlType == ICorDebugInfo::VLT_STK )
            {
                // This is an EBP frame, all stack variables should be EBP relative
                _ASSERTE( varLoc.vlStk.vlsBaseReg == ICorDebugInfo::REGNUM_EBP );
                // Generic special args may show up as locals with positive offset from EBP, so skip them
                if( varLoc.vlStk.vlsOffset <= 0 )
                {
                    // Normal locals must occur after the header on the stack
                    _ASSERTE( unsigned(-varLoc.vlStk.vlsOffset) >= frameHeaderSize );
                    // Value must occur before the top of the stack
                    _ASSERTE( unsigned(-varLoc.vlStk.vlsOffset) < newInfo.stackSize );
                }

                // Ideally we'd like to verify that the stack locals (if any) start at exactly the end
                // of the header.  However, we can't easily determine the size of value classes here,
                // and so (since the stack grows towards 0) can't easily determine where the end of
                // the local lies.
            }
#elif defined(TARGET_AMD64) || defined(TARGET_ARM64)
            switch(newMethodVarsSortedBase[i].loc.vlType)
            {
            default:
                // No validation here for non-stack locals
                break;

            case ICorDebugInfo::VLT_STK_BYREF:
                {
                    // For byrefs, verify that the ptr will be zeroed out

                    SIZE_T regOffs = GetRegOffsInCONTEXT(newMethodVarsSortedBase[i].loc.vlStk.vlsBaseReg);
                    TADDR baseReg = *(TADDR *)(regOffs + (BYTE*)pCtx);
                    TADDR addrOfPtr = baseReg + newMethodVarsSortedBase[i].loc.vlStk.vlsOffset;

                    _ASSERTE(
                        // The ref must exist in the portion we'll zero-out
                        (
                            (newStackBase <= addrOfPtr) &&
                            (addrOfPtr < newStackBase + (newFixedStackSize - frameHeaderSize))
                        ) ||
                        // OR in the caller's frame (for parameters)
                        (addrOfPtr >= newStackBase + newFixedStackSize));

                    // Deliberately fall through, so that we also verify that the value that the ptr
                    // points to will be zeroed out
                    // ...
                }
                __fallthrough;

            case ICorDebugInfo::VLT_STK:
            case ICorDebugInfo::VLT_STK2:
            case ICorDebugInfo::VLT_REG_STK:
            case ICorDebugInfo::VLT_STK_REG:
                SIZE_T * pVarStackLocation = NativeVarStackAddr(newMethodVarsSortedBase[i].loc, pCtx);
                _ASSERTE (pVarStackLocation != NULL);
                _ASSERTE(
                    // The value must exist in the portion we'll zero-out
                    (
                        (newStackBase <= (TADDR) pVarStackLocation) &&
                        ((TADDR) pVarStackLocation < newStackBase + (newFixedStackSize - frameHeaderSize))
                    ) ||
                    // OR in the caller's frame (for parameters)
                    ((TADDR) pVarStackLocation >= newStackBase + newFixedStackSize));
                break;
            }
#else   // !X86, !X64, !ARM64
            PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif
        }

#endif // _DEBUG

        // Clear the local and temporary stack space

#if defined(TARGET_X86)
        memset((void*)(size_t)(pCtx->Esp), 0, newInfo.stackSize - frameHeaderSize );
#elif defined(TARGET_AMD64) || defined(TARGET_ARM64)
        memset((void*)newStackBase, 0, newFixedStackSize - frameHeaderSize);

        // Restore PSPSym for the new function. Its value should be set to our new FP. But
        // first, we gotta find PSPSym's location on the stack
        INT32 nNewPspSymStackSlot = newGcDecoder.GetPSPSymStackSlot();
        if (nNewPspSymStackSlot != NO_PSP_SYM)
        {
#if defined(TARGET_AMD64)
            *PTR_TADDR(newStackBase + nNewPspSymStackSlot) = newStackBase;
#elif defined(TARGET_ARM64)
            *PTR_TADDR(callerSP + nNewPspSymStackSlot) = callerSP;
#else
            PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif
        }
#else   // !X86, !X64, !ARM64
        PORTABILITY_ASSERT("Edit-and-continue not enabled on this platform.");
#endif

        // 4) Put the variables from step 3 into their new locations.

        LOG((LF_ENC, LL_INFO100, "EECM::FixContextForEnC: set vars!\n"));

        // Move the old variables into their new places.

        hr = g_pDebugInterface->SetVariablesAtOffset(pNewCodeInfo->GetMethodDesc(),
                                                     newNumVars,
                                                     newMethodVarsSortedBase,
                                                     newMethodOffset,
                                                     pCtx, // place them into the new context
                                                     rgVal1,
                                                     rgVal2,
                                                     rgVCs);

        /*-----------------------------------------------------------------------*/
    }
ErrExit:
    if (oldMethodVarsSortedBase)
        delete[] oldMethodVarsSortedBase;
    if (newMethodVarsSortedBase)
        delete[] newMethodVarsSortedBase;
    if (rgVal1 != NULL)
        delete[] rgVal1;
    if (rgVal2 != NULL)
        delete[] rgVal2;

    LOG((LF_ENC, LL_INFO100, "EECM::FixContextForEnC: exiting!\n"));

    return hr;
}
#endif // !FEATURE_METADATA_UPDATER

#endif // #ifndef DACCESS_COMPILE

#ifdef USE_GC_INFO_DECODER
/*****************************************************************************
 *
 *  Is the function currently at a "GC safe point" ?
 */
bool EECodeManager::IsGcSafe( EECodeInfo     *pCodeInfo,
                              DWORD           dwRelOffset)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            DECODE_INTERRUPTIBILITY,
            dwRelOffset
            );

    return gcInfoDecoder.IsInterruptible();
}

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
bool EECodeManager::HasTailCalls( EECodeInfo     *pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            DECODE_HAS_TAILCALLS,
            0
            );

    return gcInfoDecoder.HasTailCalls();
}
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

#if defined(TARGET_AMD64) && defined(_DEBUG)

struct FindEndOfLastInterruptibleRegionState
{
    unsigned curOffset;
    unsigned endOffset;
    unsigned lastRangeOffset;
};

bool FindEndOfLastInterruptibleRegionCB (
        UINT32 startOffset,
        UINT32 stopOffset,
        LPVOID hCallback)
{
    FindEndOfLastInterruptibleRegionState *pState = (FindEndOfLastInterruptibleRegionState*)hCallback;

    //
    // If the current range doesn't overlap the given range, keep searching.
    //
    if (   startOffset >= pState->endOffset
        || stopOffset < pState->curOffset)
    {
        return false;
    }

    //
    // If the range overlaps the end, then the last point is the end.
    //
    if (   stopOffset > pState->endOffset
        /*&& startOffset < pState->endOffset*/)
    {
        // The ranges should be sorted in increasing order.
        CONSISTENCY_CHECK(startOffset >= pState->lastRangeOffset);

        pState->lastRangeOffset = pState->endOffset;
        return true;
    }

    //
    // See if the end of this range is the closet to the end that we've found
    // so far.
    //
    if (stopOffset > pState->lastRangeOffset)
        pState->lastRangeOffset = stopOffset;

    return false;
}

/*
    Locates the end of the last interruptible region in the given code range.
    Returns 0 if the entire range is uninterruptible.  Returns the end point
    if the entire range is interruptible.
*/
unsigned EECodeManager::FindEndOfLastInterruptibleRegion(unsigned curOffset,
                                                         unsigned endOffset,
                                                         GCInfoToken gcInfoToken)
{
#ifndef DACCESS_COMPILE
    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            DECODE_FOR_RANGES_CALLBACK
            );

    FindEndOfLastInterruptibleRegionState state;
    state.curOffset = curOffset;
    state.endOffset = endOffset;
    state.lastRangeOffset = 0;

    gcInfoDecoder.EnumerateInterruptibleRanges(&FindEndOfLastInterruptibleRegionCB, &state);

    return state.lastRangeOffset;
#else
    DacNotImpl();
    return NULL;
#endif // #ifndef DACCESS_COMPILE
}

#endif // TARGET_AMD64 && _DEBUG


#else // !USE_GC_INFO_DECODER

/*****************************************************************************
 *
 *  Is the function currently at a "GC safe point" ?
 */
bool EECodeManager::IsGcSafe( EECodeInfo     *pCodeInfo,
                              DWORD           dwRelOffset)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    hdrInfo         info;
    BYTE    *       table;

    /* Extract the necessary information from the info block header */

    table = (BYTE *)DecodeGCHdrInfo(pCodeInfo->GetGCInfoToken(),
                                       dwRelOffset,
                                       &info);

    /* workaround: prevent interruption within prolog/epilog */

    if  (info.prologOffs != hdrInfo::NOT_IN_PROLOG || info.epilogOffs != hdrInfo::NOT_IN_EPILOG)
        return false;

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEF);
#endif

    return (info.interruptible);
}

#endif // !USE_GC_INFO_DECODER


#if defined(FEATURE_EH_FUNCLETS)

void EECodeManager::EnsureCallerContextIsValid( PREGDISPLAY  pRD, StackwalkCacheEntry* pCacheEntry, EECodeInfo * pCodeInfo /*= NULL*/ )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if( !pRD->IsCallerContextValid )
    {
#if !defined(DACCESS_COMPILE) && defined(HAS_QUICKUNWIND)
        if (pCacheEntry != NULL)
        {
            // lightened schema: take stack unwind info from stackwalk cache
            QuickUnwindStackFrame(pRD, pCacheEntry, EnsureCallerStackFrameIsValid);
        }
        else
#endif // !DACCESS_COMPILE
        {
            // We need to make a copy here (instead of switching the pointers), in order to preserve the current context
            *(pRD->pCallerContext) = *(pRD->pCurrentContext);
            *(pRD->pCallerContextPointers) = *(pRD->pCurrentContextPointers);

            Thread::VirtualUnwindCallFrame(pRD->pCallerContext, pRD->pCallerContextPointers, pCodeInfo);
        }

        pRD->IsCallerContextValid = TRUE;
    }

    _ASSERTE( pRD->IsCallerContextValid );
}

size_t EECodeManager::GetCallerSp( PREGDISPLAY  pRD )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // Don't add usage of this field.  This is only temporary.
    // See ExceptionTracker::InitializeCrawlFrame() for more information.
    if (!pRD->IsCallerSPValid)
    {
        EnsureCallerContextIsValid(pRD, NULL);
    }

    return GetSP(pRD->pCallerContext);
}

#endif // FEATURE_EH_FUNCLETS

#ifdef HAS_QUICKUNWIND
/*
  *  Light unwind the current stack frame, using provided cache entry.
  *  pPC, Esp and pEbp of pContext are updated.
  */

// static
void EECodeManager::QuickUnwindStackFrame(PREGDISPLAY pRD, StackwalkCacheEntry *pCacheEntry, QuickUnwindFlag flag)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(pCacheEntry);
    _ASSERTE(GetControlPC(pRD) == (PCODE)(pCacheEntry->IP));

#if defined(TARGET_X86)
    _ASSERTE(flag == UnwindCurrentStackFrame);

    _ASSERTE(!pCacheEntry->fUseEbp || pCacheEntry->fUseEbpAsFrameReg);

    if (pCacheEntry->fUseEbpAsFrameReg)
    {
        _ASSERTE(pCacheEntry->fUseEbp);
        TADDR curEBP = GetRegdisplayFP(pRD);

        // EBP frame, update ESP through EBP, since ESPOffset may vary
        pRD->SetEbpLocation(PTR_DWORD(curEBP));
        pRD->SP = curEBP + sizeof(void*);
    }
    else
    {
        _ASSERTE(!pCacheEntry->fUseEbp);
        // ESP frame, update up to retAddr using ESPOffset
        pRD->SP += pCacheEntry->ESPOffset;
    }
    pRD->PCTAddr  = (TADDR)pRD->SP;
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->SP     += sizeof(void*) + pCacheEntry->argSize;

#elif defined(TARGET_AMD64)
    if (pRD->IsCallerContextValid)
    {
        pRD->pCurrentContext->Rbp = pRD->pCallerContext->Rbp;
        pRD->pCurrentContext->Rsp = pRD->pCallerContext->Rsp;
        pRD->pCurrentContext->Rip = pRD->pCallerContext->Rip;
    }
    else
    {
        PCONTEXT pSourceCtx = NULL;
        PCONTEXT pTargetCtx = NULL;
        if (flag == UnwindCurrentStackFrame)
        {
            pTargetCtx = pRD->pCurrentContext;
            pSourceCtx = pRD->pCurrentContext;
        }
        else
        {
            pTargetCtx = pRD->pCallerContext;
            pSourceCtx = pRD->pCurrentContext;
        }

        // Unwind RBP.  The offset is relative to the current sp.
        if (pCacheEntry->RBPOffset == 0)
        {
            pTargetCtx->Rbp = pSourceCtx->Rbp;
        }
        else
        {
            pTargetCtx->Rbp = *(UINT_PTR*)(pSourceCtx->Rsp + pCacheEntry->RBPOffset);
        }

        // Adjust the sp.  From this pointer onwards pCurrentContext->Rsp is the caller sp.
        pTargetCtx->Rsp = pSourceCtx->Rsp + pCacheEntry->RSPOffset;

        // Retrieve the return address.
        pTargetCtx->Rip = *(UINT_PTR*)((pTargetCtx->Rsp) - sizeof(UINT_PTR));
    }

    if (flag == UnwindCurrentStackFrame)
    {
        SyncRegDisplayToCurrentContext(pRD);
        pRD->IsCallerContextValid = FALSE;
        pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
    }

#else  // !TARGET_X86 && !TARGET_AMD64
    PORTABILITY_ASSERT("EECodeManager::QuickUnwindStackFrame is not implemented on this platform.");
#endif // !TARGET_X86 && !TARGET_AMD64
}
#endif // HAS_QUICKUNWIND

#ifdef FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
size_t EECodeManager::GetResumeSp( PCONTEXT  pContext )
{
    PCODE currentPc = PCODE(pContext->Eip);

    _ASSERTE(ExecutionManager::IsManagedCode(currentPc));

    EECodeInfo codeInfo(currentPc);

    PTR_CBYTE methodStart = PTR_CBYTE(codeInfo.GetSavedMethodCode());

    GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();
    PTR_VOID    methodInfoPtr = gcInfoToken.Info;
    DWORD       curOffs = codeInfo.GetRelOffset();

    CodeManStateBuf stateBuf;

    stateBuf.hdrInfoSize = (DWORD)DecodeGCHdrInfo(gcInfoToken,
                                                  curOffs,
                                                  &stateBuf.hdrInfoBody);

    PTR_CBYTE table = dac_cast<PTR_CBYTE>(methodInfoPtr) + stateBuf.hdrInfoSize;

    hdrInfo *info = &stateBuf.hdrInfoBody;

    _ASSERTE(info->epilogOffs == hdrInfo::NOT_IN_EPILOG && info->prologOffs == hdrInfo::NOT_IN_PROLOG);

    bool isESPFrame = !info->ebpFrame && !info->doubleAlign;

    if (codeInfo.IsFunclet())
    {
        // Treat funclet's frame as ESP frame
        isESPFrame = true;
    }

    if (isESPFrame)
    {
        const size_t curESP = (size_t)(pContext->Esp);
        return curESP + GetPushedArgSize(info, table, curOffs);
    }

    const size_t curEBP = (size_t)(pContext->Ebp);
    return GetOutermostBaseFP(curEBP, info);
}
#endif // TARGET_X86
#endif // FEATURE_EH_FUNCLETS

#ifndef FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Unwind the current stack frame, i.e. update the virtual register
 *  set in pContext. This will be similar to the state after the function
 *  returns back to caller (IP points to after the call, Frame and Stack
 *  pointer has been reset, callee-saved registers restored (if UpdateAllRegs),
 *  callee-unsaved registers are trashed.
 *  Returns success of operation.
 */

bool EECodeManager::UnwindStackFrame(PREGDISPLAY     pContext,
                                     EECodeInfo     *pCodeInfo,
                                     unsigned        flags,
                                     CodeManState   *pState,
                                     StackwalkCacheUnwindInfo  *pUnwindInfo /* out-only, perf improvement */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#ifdef TARGET_X86
    return ::UnwindStackFrame(pContext, pCodeInfo, flags, pState, pUnwindInfo);
#else // TARGET_X86
    PORTABILITY_ASSERT("EECodeManager::UnwindStackFrame");
    return false;
#endif // _TARGET_???_
}

/*****************************************************************************/
#else // !FEATURE_EH_FUNCLETS
/*****************************************************************************/

bool EECodeManager::UnwindStackFrame(PREGDISPLAY     pContext,
                                     EECodeInfo     *pCodeInfo,
                                     unsigned        flags,
                                     CodeManState   *pState,
                                     StackwalkCacheUnwindInfo  *pUnwindInfo /* out-only, perf improvement */)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#if defined(TARGET_AMD64)
    // To avoid unnecessary computation, we only crack the unwind info if pUnwindInfo is not NULL, which only happens
    // if the LIGHTUNWIND flag is passed to StackWalkFramesEx().
    if (pUnwindInfo != NULL)
    {
        pCodeInfo->GetOffsetsFromUnwindInfo(&(pUnwindInfo->RSPOffsetFromUnwindInfo),
                                            &(pUnwindInfo->RBPOffset));
    }
#endif // TARGET_AMD64

    _ASSERTE(pCodeInfo != NULL);
    Thread::VirtualUnwindCallFrame(pContext, pCodeInfo);
    return true;
}

/*****************************************************************************/
#endif // FEATURE_EH_FUNCLETS

/*****************************************************************************/

/* report args in 'msig' to the GC.
   'argsStart' is start of the stack-based arguments
   'varArgSig' describes the arguments
   'ctx' has the GC reporting info
*/
void promoteVarArgs(PTR_BYTE argsStart, PTR_VASigCookie varArgSig, GCCONTEXT* ctx)
{
    WRAPPER_NO_CONTRACT;

    //Note: no instantiations needed for varargs
    MetaSig msig(varArgSig->signature,
                 varArgSig->pModule,
                 NULL);

    PTR_BYTE pFrameBase = argsStart - TransitionBlock::GetOffsetOfArgs();

    ArgIterator argit(&msig);

#ifdef TARGET_X86
    // For the X86 target the JIT does not report any of the fixed args for a varargs method
    // So we report the fixed args via the promoteArgs call below
    bool skipFixedArgs = false;
#else
    // For other platforms the JITs do report the fixed args of a varargs method
    // So we must tell promoteArgs to skip to the end of the fixed args
    bool skipFixedArgs = true;
#endif

    bool inVarArgs = false;

    int argOffset;
    while ((argOffset = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        if (msig.GetArgProps().AtSentinel())
            inVarArgs = true;

        // if skipFixedArgs is false we report all arguments
        //  otherwise we just report the varargs.
        if (!skipFixedArgs || inVarArgs)
        {
            ArgDestination argDest(pFrameBase, argOffset, argit.GetArgLocDescForStructInRegs());
            msig.GcScanRoots(&argDest, ctx->f, ctx->sc);
        }
    }
}

#ifndef DACCESS_COMPILE
FCIMPL1(void, GCReporting::Register, GCFrame* frame)
{
    FCALL_CONTRACT;

    // Construct a GCFrame.
    _ASSERTE(frame != NULL);
    frame->Push(GetThread());
}
FCIMPLEND

FCIMPL1(void, GCReporting::Unregister, GCFrame* frame)
{
    FCALL_CONTRACT;

    // Destroy the GCFrame.
    _ASSERTE(frame != NULL);
    frame->Remove();
}
FCIMPLEND
#endif // !DACCESS_COMPILE

#ifndef USE_GC_INFO_DECODER

/*****************************************************************************
 *
 *  Enumerate all live object references in that function using
 *  the virtual register set.
 *  Returns success of operation.
 */

bool EECodeManager::EnumGcRefs( PREGDISPLAY     pContext,
                                EECodeInfo     *pCodeInfo,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack,
                                DWORD           relOffsetOverride)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    PTR_CBYTE methodStart = PTR_CBYTE(pCodeInfo->GetSavedMethodCode());
    unsigned  curOffs = pCodeInfo->GetRelOffset();
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    if (relOffsetOverride != NO_OVERRIDE_OFFSET)
    {
        curOffs = relOffsetOverride;
    }

#ifdef FEATURE_EH_FUNCLETS
    return ::EnumGcRefs(pContext,
                        methodStart,
                        curOffs,
                        gcInfoToken,
                        pCodeInfo->IsFunclet(),
                        pCodeInfo->IsFilterFunclet(),
                        flags,
                        pCallBack,
                        hCallBack);
#else
    return ::EnumGcRefs(pContext,
                        methodStart,
                        curOffs,
                        gcInfoToken,
                        false,
                        false,
                        flags,
                        pCallBack,
                        hCallBack);
#endif
}

#else // !USE_GC_INFO_DECODER


/*****************************************************************************
 *
 *  Enumerate all live object references in that function using
 *  the virtual register set.
 *  Returns success of operation.
 */

bool EECodeManager::EnumGcRefs( PREGDISPLAY     pRD,
                                EECodeInfo     *pCodeInfo,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack,
                                DWORD           relOffsetOverride)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    unsigned curOffs = pCodeInfo->GetRelOffset();

#ifdef TARGET_ARM
    // On ARM, the low-order bit of an instruction pointer indicates Thumb vs. ARM mode.
    // Mask this off; all instructions are two-byte aligned.
    curOffs &= (~THUMB_CODE);
#endif // TARGET_ARM

#ifdef _DEBUG
    // Get the name of the current method
    const char * methodName = pCodeInfo->GetMethodDesc()->GetName();
    LOG((LF_GCINFO, LL_INFO1000, "Reporting GC refs for %s at offset %04x.\n",
        methodName, curOffs));
#endif

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

#if defined(STRESS_HEAP) && defined(PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
    // When we simulate a hijack during gcstress
    //  we start with ActiveStackFrame and the offset
    //  after the call
    // We need to make it look like a non-leaf frame
    //  so that it's treated like a regular hijack
    if (flags & ActiveStackFrame)
    {
        GcInfoDecoder _gcInfoDecoder(
                            gcInfoToken,
                            DECODE_INTERRUPTIBILITY,
                            curOffs
                            );
        if(!_gcInfoDecoder.IsInterruptible())
        {
            // This must be the offset after a call
#ifdef _DEBUG
            GcInfoDecoder _safePointDecoder(gcInfoToken, (GcInfoDecoderFlags)0, 0);
            _ASSERTE(_safePointDecoder.IsSafePoint(curOffs));
#endif
            flags &= ~((unsigned)ActiveStackFrame);
        }
    }
#endif // STRESS_HEAP && PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

#ifdef _DEBUG
    if (flags & ActiveStackFrame)
    {
        GcInfoDecoder _gcInfoDecoder(
                            gcInfoToken,
                            DECODE_INTERRUPTIBILITY,
                            curOffs
                            );
        _ASSERTE(_gcInfoDecoder.IsInterruptible());
    }
#endif

    /* If we are not in the active method, we are currently pointing
         * to the return address; at the return address stack variables
         * can become dead if the call is the last instruction of a try block
         * and the return address is the jump around the catch block. Therefore
         * we simply assume an offset inside of call instruction.
         * NOTE: The GcInfoDecoder depends on this; if you change it, you must
         * revisit the GcInfoEncoder/Decoder
         */

    if (!(flags & ExecutionAborted))
    {
        if (!(flags & ActiveStackFrame))
        {
            curOffs--;
            LOG((LF_GCINFO, LL_INFO1000, "Adjusted GC reporting offset due to flags !ExecutionAborted && !ActiveStackFrame. Now reporting GC refs for %s at offset %04x.\n",
                methodName, curOffs));
        }
    }
    else
    {
        /* However if ExecutionAborted, then this must be one of the
         * ExceptionFrames. Handle accordingly
         */
        _ASSERTE(!(flags & AbortingCall) || !(flags & ActiveStackFrame));

        if (flags & AbortingCall)
        {
            curOffs--;
            LOG((LF_GCINFO, LL_INFO1000, "Adjusted GC reporting offset due to flags ExecutionAborted && AbortingCall. Now reporting GC refs for %s at offset %04x.\n",
                methodName, curOffs));
        }
    }

    // Check if we have been given an override value for relOffset
    if (relOffsetOverride != NO_OVERRIDE_OFFSET)
    {
        // We've been given an override offset for GC Info
#ifdef _DEBUG
        GcInfoDecoder _gcInfoDecoder(
                            gcInfoToken,
                            DECODE_CODE_LENGTH
                      );

        // We only use override offset for wantsReportOnlyLeaf
        _ASSERTE(_gcInfoDecoder.WantsReportOnlyLeaf());
#endif // _DEBUG

        curOffs = relOffsetOverride;

#ifdef TARGET_ARM
        // On ARM, the low-order bit of an instruction pointer indicates Thumb vs. ARM mode.
        // Mask this off; all instructions are two-byte aligned.
        curOffs &= (~THUMB_CODE);
#endif // TARGET_ARM

        LOG((LF_GCINFO, LL_INFO1000, "Adjusted GC reporting offset to provided override offset. Now reporting GC refs for %s at offset %04x.\n",
            methodName, curOffs));
    }


#if defined(FEATURE_EH_FUNCLETS)   // funclets
    if (pCodeInfo->GetJitManager()->IsFilterFunclet(pCodeInfo))
    {
        // Filters are the only funclet that run during the 1st pass, and must have
        // both the leaf and the parent frame reported.  In order to avoid double
        // reporting of the untracked variables, do not report them for the filter.
        flags |= NoReportUntracked;
    }
#endif // FEATURE_EH_FUNCLETS

    bool reportScratchSlots;

    // We report scratch slots only for leaf frames.
    // A frame is non-leaf if we are executing a call, or a fault occurred in the function.
    // The only case in which we need to report scratch slots for a non-leaf frame
    //   is when execution has to be resumed at the point of interruption (via ResumableFrame)
    //<TODO>Implement ResumableFrame</TODO>
    _ASSERTE( sizeof( BOOL ) >= sizeof( ActiveStackFrame ) );
    reportScratchSlots = (flags & ActiveStackFrame) != 0;


    GcInfoDecoder gcInfoDecoder(
                        gcInfoToken,
                        GcInfoDecoderFlags (DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG),
                        curOffs
                        );

    if (!gcInfoDecoder.EnumerateLiveSlots(
                        pRD,
                        reportScratchSlots,
                        flags,
                        pCallBack,
                        hCallBack
                        ))
    {
        return false;
    }

#ifdef FEATURE_EH_FUNCLETS   // funclets
    //
    // If we're in a funclet, we do not want to report the incoming varargs.  This is
    // taken care of by the parent method and the funclet should access those arguments
    // by way of the parent method's stack frame.
    //
    if(pCodeInfo->IsFunclet())
    {
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    if (gcInfoDecoder.GetIsVarArg())
    {
        MethodDesc* pMD = pCodeInfo->GetMethodDesc();
        _ASSERTE(pMD != NULL);

        // This does not apply to x86 because of how it handles varargs (it never
        // reports the arguments from the explicit method signature).
        //
#ifndef TARGET_X86
        //
        // SPECIAL CASE:
        //      IL marshaling stubs have signatures that are marked as vararg,
        //      but they are callsite sigs that actually contain complete sig
        //      info.  There are two reasons for this:
        //          1) the stub callsites expect the method to be vararg
        //          2) the marshaling stub must have full sig info so that
        //             it can do a ldarg.N on the arguments it needs to marshal.
        //      The result of this is that the code below will report the
        //      variable arguments twice--once from the va sig cookie and once
        //      from the explicit method signature (in the method's gc info).
        //
        //      This fix to this is to early out of the va sig cookie reporting
        //      in this special case.
        //
        if (pMD->IsILStub())
        {
            return true;
        }
#endif // !TARGET_X86

        LOG((LF_GCINFO, LL_INFO100, "Reporting incoming vararg GC refs\n"));

        // Find the offset of the VASigCookie.  It's offsets are relative to
        // the base of a FramedMethodFrame.
        int VASigCookieOffset;

        {
            MetaSig msigFindVASig(pMD);
            ArgIterator argit(&msigFindVASig);
            VASigCookieOffset = argit.GetVASigCookieOffset() - TransitionBlock::GetOffsetOfArgs();
        }

        PTR_BYTE prevSP = dac_cast<PTR_BYTE>(GetCallerSp(pRD));

        _ASSERTE(prevSP + VASigCookieOffset >= dac_cast<PTR_BYTE>(GetSP(pRD->pCurrentContext)));

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        // Note that I really want to say hCallBack is a GCCONTEXT, but this is pretty close
        extern void GcEnumObject(LPVOID pData, OBJECTREF *pObj, uint32_t flags);
        _ASSERTE((void*) GcEnumObject == pCallBack);
#endif // _DEBUG && !DACCESS_COMPILE
        GCCONTEXT   *pCtx = (GCCONTEXT *) hCallBack;

        // For varargs, look up the signature using the varArgSig token passed on the stack
        PTR_VASigCookie varArgSig = *PTR_PTR_VASigCookie(prevSP + VASigCookieOffset);

        promoteVarArgs(prevSP, varArgSig, pCtx);
    }

    return true;

}

#endif // USE_GC_INFO_DECODER

/*****************************************************************************
 *
 *  Returns "this" pointer if it is a non-static method
 *  AND the object is still alive.
 *  Returns NULL in all other cases.
 *  Unfortunately, the semantics of this call currently depend on the architecture.
 *  On non-x86 architectures, where we use GcInfo{En,De}Coder, this returns NULL for
 *  all cases except the case where the GenericsContext is determined via "this."  On x86,
 *  it will definitely return a non-NULL value in that case, and for synchronized methods;
 *  it may also return a non-NULL value for other cases, depending on how the method is compiled.
 */
OBJECTREF EECodeManager::GetInstance( PREGDISPLAY    pContext,
                                      EECodeInfo*   pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#ifndef USE_GC_INFO_DECODER
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    unsigned    relOffset = pCodeInfo->GetRelOffset();

    PTR_CBYTE   table = PTR_CBYTE(gcInfoToken.Info);
    hdrInfo     info;
    unsigned    stackDepth;
    TADDR       taArgBase;
    unsigned    count;

    /* Extract the necessary information from the info block header */

    table += DecodeGCHdrInfo(gcInfoToken,
                             relOffset,
                             &info);

    // We do not have accurate information in the prolog or the epilog
    if (info.prologOffs != hdrInfo::NOT_IN_PROLOG ||
        info.epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {
        return NULL;
    }

    if  (info.interruptible)
    {
        stackDepth = scanArgRegTableI(skipToArgReg(info, table), relOffset, relOffset, &info);
    }
    else
    {
        stackDepth = scanArgRegTable (skipToArgReg(info, table), (unsigned)relOffset, &info);
    }

    if (info.ebpFrame)
    {
        _ASSERTE(stackDepth == 0);
        taArgBase = GetRegdisplayFP(pContext);
    }
    else
    {
        taArgBase =  pContext->SP + stackDepth;
    }

    // Only synchronized methods and generic code that accesses
    // the type context via "this" need to report "this".
    // If it's reported for other methods, it's probably
    // done incorrectly. So flag such cases.
    _ASSERTE(info.thisPtrResult == REGI_NA ||
             pCodeInfo->GetMethodDesc()->IsSynchronized() ||
             pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis());

    if (info.thisPtrResult != REGI_NA)
    {
        // the register contains the Object pointer.
        TADDR uRegValue = *(reinterpret_cast<TADDR *>(getCalleeSavedReg(pContext, info.thisPtrResult)));
        return ObjectToOBJECTREF(PTR_Object(uRegValue));
    }

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEF);
#endif

#ifndef FEATURE_EH_FUNCLETS
    /* Parse the untracked frame variable table */

    /* The 'this' pointer can never be located in the untracked table */
    /* as we only allow pinned and byrefs in the untracked table      */

    count = info.untrackedCnt;
    while (count-- > 0)
    {
        fastSkipSigned(table);
    }

    /* Look for the 'this' pointer in the frame variable lifetime table     */

    count = info.varPtrTableSize;
    unsigned tmpOffs = 0;
    while (count-- > 0)
    {
        unsigned varOfs = fastDecodeUnsigned(table);
        unsigned begOfs = tmpOffs + fastDecodeUnsigned(table);
        unsigned endOfs = begOfs + fastDecodeUnsigned(table);
        _ASSERTE(!info.ebpFrame || (varOfs!=0));
        /* Is this variable live right now? */
        if (((unsigned)relOffset >= begOfs) && ((unsigned)relOffset < endOfs))
        {
            /* Does it contain the 'this' pointer */
            if (varOfs & this_OFFSET_FLAG)
            {
                unsigned ofs = varOfs & ~OFFSET_MASK;

                /* Tracked locals for EBP frames are always at negative offsets */

                if (info.ebpFrame)
                    taArgBase -= ofs;
                else
                    taArgBase += ofs;

                return (OBJECTREF)(size_t)(*PTR_DWORD(taArgBase));
            }
        }
        tmpOffs = begOfs;
    }

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *) == 0xBABE);
#endif

#else // FEATURE_EH_FUNCLETS
    if (pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis()) // Generic Context is "this"
    {
        // Untracked table must have at least one entry - this pointer
        _ASSERTE(info.untrackedCnt > 0);

        // The first entry must be "this" pointer
        int stkOffs = fastDecodeSigned(table);
        taArgBase -= stkOffs & ~OFFSET_MASK;
        return (OBJECTREF)(size_t)(*PTR_DWORD(taArgBase));
    }
#endif // FEATURE_EH_FUNCLETS

    return NULL;
#else // !USE_GC_INFO_DECODER
    PTR_VOID token = EECodeManager::GetExactGenericsToken(pContext, pCodeInfo);

    OBJECTREF oRef = ObjectToOBJECTREF(PTR_Object(dac_cast<TADDR>(token)));
    VALIDATEOBJECTREF(oRef);
    return oRef;
#endif // USE_GC_INFO_DECODER
}

GenericParamContextType EECodeManager::GetParamContextType(PREGDISPLAY     pContext,
                                                           EECodeInfo *    pCodeInfo)
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifndef USE_GC_INFO_DECODER
    /* Extract the necessary information from the info block header */
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    PTR_VOID    methodInfoPtr = pCodeInfo->GetGCInfo();
    unsigned    relOffset = pCodeInfo->GetRelOffset();

    hdrInfo     info;
    PTR_CBYTE   table = PTR_CBYTE(gcInfoToken.Info);
    table += DecodeGCHdrInfo(gcInfoToken,
                             relOffset,
                             &info);

    if (!info.genericsContext ||
        info.prologOffs != hdrInfo::NOT_IN_PROLOG ||
        info.epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {
        return GENERIC_PARAM_CONTEXT_NONE;
    }

    if (info.genericsContextIsMethodDesc)
    {
        return GENERIC_PARAM_CONTEXT_METHODDESC;
    }

    return GENERIC_PARAM_CONTEXT_METHODTABLE;

    // On x86 the generic param context parameter is never this.
#else // !USE_GC_INFO_DECODER
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            GcInfoDecoderFlags (DECODE_GENERICS_INST_CONTEXT)
            );

    INT32 spOffsetGenericsContext = gcInfoDecoder.GetGenericsInstContextStackSlot();
    if (spOffsetGenericsContext != NO_GENERICS_INST_CONTEXT)
    {
        if (gcInfoDecoder.HasMethodDescGenericsInstContext())
        {
            return GENERIC_PARAM_CONTEXT_METHODDESC;
        }
        else if (gcInfoDecoder.HasMethodTableGenericsInstContext())
        {
            return GENERIC_PARAM_CONTEXT_METHODTABLE;
        }
        return GENERIC_PARAM_CONTEXT_THIS;
    }
    return GENERIC_PARAM_CONTEXT_NONE;
#endif // USE_GC_INFO_DECODER
}

/*****************************************************************************
 *
 *  Returns the extra argument passed to shared generic code if it is still alive.
 *  Returns NULL in all other cases.
 */
PTR_VOID EECodeManager::GetParamTypeArg(PREGDISPLAY     pContext,
                                        EECodeInfo *    pCodeInfo)

{
    LIMITED_METHOD_DAC_CONTRACT;

#ifndef USE_GC_INFO_DECODER
    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    PTR_VOID    methodInfoPtr = pCodeInfo->GetGCInfo();
    unsigned    relOffset = pCodeInfo->GetRelOffset();

    /* Extract the necessary information from the info block header */
    hdrInfo     info;
    PTR_CBYTE   table = PTR_CBYTE(gcInfoToken.Info);
    table += DecodeGCHdrInfo(gcInfoToken,
                             relOffset,
                             &info);

    if (!info.genericsContext ||
        info.prologOffs != hdrInfo::NOT_IN_PROLOG ||
        info.epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {
        return NULL;
    }

    TADDR fp = GetRegdisplayFP(pContext);
    TADDR taParamTypeArg = *PTR_TADDR(fp - GetParamTypeArgOffset(&info));
    return PTR_VOID(taParamTypeArg);

#else // !USE_GC_INFO_DECODER
    return EECodeManager::GetExactGenericsToken(pContext, pCodeInfo);

#endif // USE_GC_INFO_DECODER
}

#if defined(FEATURE_EH_FUNCLETS) && defined(USE_GC_INFO_DECODER)
/*
    Returns the generics token.  This is used by GetInstance() and GetParamTypeArg() on WIN64.
*/
//static
PTR_VOID EECodeManager::GetExactGenericsToken(PREGDISPLAY     pContext,
                                              EECodeInfo *    pCodeInfo)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return EECodeManager::GetExactGenericsToken(GetCallerSp(pContext), pCodeInfo);
}

//static
PTR_VOID EECodeManager::GetExactGenericsToken(SIZE_T          baseStackSlot,
                                              EECodeInfo *    pCodeInfo)
{
    LIMITED_METHOD_DAC_CONTRACT;

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            GcInfoDecoderFlags (DECODE_PSP_SYM | DECODE_GENERICS_INST_CONTEXT)
            );

    INT32 spOffsetGenericsContext = gcInfoDecoder.GetGenericsInstContextStackSlot();
    if (spOffsetGenericsContext != NO_GENERICS_INST_CONTEXT)
    {

        TADDR taSlot;
        if (pCodeInfo->IsFunclet())
        {
            INT32 spOffsetPSPSym = gcInfoDecoder.GetPSPSymStackSlot();
            _ASSERTE(spOffsetPSPSym != NO_PSP_SYM);

#ifdef TARGET_AMD64
            // On AMD64 the spOffsetPSPSym is relative to the "Initial SP": the stack
            // pointer at the end of the prolog before and dynamic allocations, so it
            // can be the same for funclets and the main function.
            // However, we have a caller SP, so we need to convert
            baseStackSlot -= pCodeInfo->GetFixedStackSize();

#endif // TARGET_AMD64

            // For funclets we have to do an extra dereference to get the PSPSym first.
            TADDR newBaseStackSlot = *PTR_TADDR(baseStackSlot + spOffsetPSPSym);

#ifdef TARGET_AMD64
            // On AMD64 the PSPSym stores the "Initial SP": the stack pointer at the end of
            // prolog, before any dynamic allocations.
            // However, the GenericsContext offset is relative to the caller SP for all
            // platforms.  So here we adjust to convert AMD64's initial sp to a caller SP.
            // But we have to be careful to use the main function's EECodeInfo, not the
            // funclet's EECodeInfo because they have different stack sizes!
            newBaseStackSlot += pCodeInfo->GetMainFunctionInfo().GetFixedStackSize();
#endif // TARGET_AMD64

            taSlot = (TADDR)( spOffsetGenericsContext + newBaseStackSlot );
        }
        else
        {
            taSlot = (TADDR)( spOffsetGenericsContext + baseStackSlot );
        }
        TADDR taExactGenericsToken = *PTR_TADDR(taSlot);
        return PTR_VOID(taExactGenericsToken);
    }
    return NULL;
}


#endif // FEATURE_EH_FUNCLETS && USE_GC_INFO_DECODER

/*****************************************************************************/

void * EECodeManager::GetGSCookieAddr(PREGDISPLAY     pContext,
                                      EECodeInfo *    pCodeInfo,
                                      CodeManState  * pState)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(sizeof(CodeManStateBuf) <= sizeof(pState->stateBuf));

    GCInfoToken    gcInfoToken = pCodeInfo->GetGCInfoToken();
    unsigned       relOffset = pCodeInfo->GetRelOffset();

#ifdef FEATURE_EH_FUNCLETS
    if (pCodeInfo->IsFunclet())
    {
        return NULL;
    }
#endif

#ifndef USE_GC_INFO_DECODER
    CodeManStateBuf * stateBuf = (CodeManStateBuf*)pState->stateBuf;

    /* Extract the necessary information from the info block header */
    hdrInfo * info = &stateBuf->hdrInfoBody;
    stateBuf->hdrInfoSize = (DWORD)DecodeGCHdrInfo(gcInfoToken, // <TODO>truncation</TODO>
                                                   relOffset,
                                                   info);

    pState->dwIsSet = 1;

    if (info->prologOffs != hdrInfo::NOT_IN_PROLOG ||
        info->epilogOffs != hdrInfo::NOT_IN_EPILOG ||
        info->gsCookieOffset == INVALID_GS_COOKIE_OFFSET)
    {
        return NULL;
    }

    if  (info->ebpFrame)
    {
        DWORD curEBP = GetRegdisplayFP(pContext);

        return PVOID(SIZE_T(curEBP - info->gsCookieOffset));
    }
    else
    {
        PTR_CBYTE table = PTR_CBYTE(gcInfoToken.Info) + stateBuf->hdrInfoSize;
        unsigned argSize = GetPushedArgSize(info, table, relOffset);

        return PVOID(SIZE_T(pContext->SP + argSize + info->gsCookieOffset));
    }

#else // !USE_GC_INFO_DECODER
    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            DECODE_GS_COOKIE
            );

    INT32 spOffsetGSCookie = gcInfoDecoder.GetGSCookieStackSlot();
    if (spOffsetGSCookie != NO_GS_COOKIE)
    {
        if(relOffset >= gcInfoDecoder.GetGSCookieValidRangeStart())
        {
            TADDR ptr = GetCallerSp(pContext) + spOffsetGSCookie;

            // Detect the end of GS cookie scope by comparing its address with SP
            // gcInfoDecoder.GetGSCookieValidRangeEnd() is not accurate. It does not
            // account for GS cookie going out of scope inside epilog or multiple epilogs.
            return (LPVOID) ((ptr >= pContext->SP) ? ptr : NULL);
        }
    }
    return NULL;

#endif // USE_GC_INFO_DECODER
}

#ifndef USE_GC_INFO_DECODER
/*****************************************************************************
 *
 *  Returns true if the given IP is in the given method's prolog or epilog.
 */
bool EECodeManager::IsInPrologOrEpilog(DWORD       relPCoffset,
                                       GCInfoToken gcInfoToken,
                                       size_t*     prologSize)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken, relPCoffset, &info);

    if (prologSize)
        *prologSize = info.prologSize;

    return ((info.prologOffs != hdrInfo::NOT_IN_PROLOG) ||
            (info.epilogOffs != hdrInfo::NOT_IN_EPILOG));
}

/*****************************************************************************
 *
 *  Returns true if the given IP is in the synchronized region of the method (valid for synchronized functions only)
*/
bool  EECodeManager::IsInSynchronizedRegion(DWORD       relOffset,
                                            GCInfoToken gcInfoToken,
                                            unsigned    flags)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken, relOffset, &info);

    // We should be called only for synchronized methods
    _ASSERTE(info.syncStartOffset != INVALID_SYNC_OFFSET && info.syncEndOffset != INVALID_SYNC_OFFSET);

    _ASSERTE(info.syncStartOffset < info.syncEndOffset);
    _ASSERTE(info.epilogCnt <= 1);
    _ASSERTE(info.epilogCnt == 0 || info.syncEndOffset <= info.syncEpilogStart);

    return (info.syncStartOffset < relOffset && relOffset < info.syncEndOffset) ||
        (info.syncStartOffset == relOffset && (flags & (ActiveStackFrame|ExecutionAborted))) ||
        // Synchronized methods have at most one epilog. The epilog does not have to be at the end of the method though.
        // Everything after the epilog is also in synchronized region.
        (info.epilogCnt != 0 && info.syncEpilogStart + info.epilogSize <= relOffset);
}
#endif // !USE_GC_INFO_DECODER

/*****************************************************************************
 *
 *  Returns the size of a given function.
 */
size_t EECodeManager::GetFunctionSize(GCInfoToken gcInfoToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#ifndef USE_GC_INFO_DECODER
    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken, 0, &info);

    return info.methodSize;
#else // !USE_GC_INFO_DECODER

    GcInfoDecoder gcInfoDecoder(
            gcInfoToken,
            DECODE_CODE_LENGTH
            );

    UINT32 codeLength = gcInfoDecoder.GetCodeLength();
    _ASSERTE( codeLength > 0 );
    return codeLength;

#endif // USE_GC_INFO_DECODER
}

/*****************************************************************************
*
*  Get information necessary for return address hijacking of the method represented by the gcInfoToken.
*  If it can be hijacked, it sets the returnKind output parameter to the kind of the return value and
*  returns true.
*  If hijacking is not possible for some reason, it return false.
*/
bool EECodeManager::GetReturnAddressHijackInfo(GCInfoToken gcInfoToken, ReturnKind * returnKind)
{
    CONTRACTL{
        NOTHROW;
    GC_NOTRIGGER;
    SUPPORTS_DAC;
    } CONTRACTL_END;

#ifndef USE_GC_INFO_DECODER
    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken, 0, &info);

    if (info.revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET)
    {
        // Hijacking of UnmanagedCallersOnly method is not allowed
        return false;
    }

    *returnKind = info.returnKind;
    return true;
#else // !USE_GC_INFO_DECODER

    GcInfoDecoder gcInfoDecoder(gcInfoToken, GcInfoDecoderFlags(DECODE_RETURN_KIND | DECODE_REVERSE_PINVOKE_VAR));

    if (gcInfoDecoder.GetReversePInvokeFrameStackSlot() != NO_REVERSE_PINVOKE_FRAME)
    {
        // Hijacking of UnmanagedCallersOnly method is not allowed
        return false;
    }

    *returnKind = gcInfoDecoder.GetReturnKind();
    return true;
#endif // USE_GC_INFO_DECODER
}

#ifndef USE_GC_INFO_DECODER
/*****************************************************************************
 *
 *  Returns the size of the frame of the given function.
 */
unsigned int EECodeManager::GetFrameSize(GCInfoToken gcInfoToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken, 0, &info);

    // currently only used by E&C callers need to know about doubleAlign
    // in all likelihood
    _ASSERTE(!info.doubleAlign);
    return info.stackSize;
}
#endif // USE_GC_INFO_DECODER

#ifndef DACCESS_COMPILE

/*****************************************************************************/

#ifndef FEATURE_EH_FUNCLETS
const BYTE* EECodeManager::GetFinallyReturnAddr(PREGDISPLAY pReg)
{
    LIMITED_METHOD_CONTRACT;

    return *(const BYTE**)(size_t)(GetRegdisplaySP(pReg));
}

BOOL EECodeManager::IsInFilter(GCInfoToken gcInfoToken,
                               unsigned offset,
                               PCONTEXT pCtx,
                               DWORD curNestLevel)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    /* Extract the necessary information from the info block header */

    hdrInfo     info;

    DecodeGCHdrInfo(gcInfoToken,
                    offset,
                    &info);

    /* make sure that we have an ebp stack frame */

    _ASSERTE(info.ebpFrame);
    _ASSERTE(info.handlers); // <TODO> This will always be set. Remove it</TODO>

    TADDR       baseSP;
    DWORD       nestingLevel;

    FrameType   frameType = GetHandlerFrameInfo(&info, pCtx->Ebp,
                                                pCtx->Esp, (DWORD) IGNORE_VAL,
                                                &baseSP, &nestingLevel);
    _ASSERTE(frameType != FR_INVALID);

//    _ASSERTE(nestingLevel == curNestLevel);

    return frameType == FR_FILTER;
}


BOOL EECodeManager::LeaveFinally(GCInfoToken gcInfoToken,
                                unsigned offset,
                                PCONTEXT pCtx)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;


    hdrInfo info;

    DecodeGCHdrInfo(gcInfoToken,
                    offset,
                    &info);

    DWORD       nestingLevel;
    GetHandlerFrameInfo(&info, pCtx->Ebp, pCtx->Esp, (DWORD) IGNORE_VAL, NULL, &nestingLevel);

    // Compute an index into the stack-based table of esp values from
    // each level of catch block.
    PTR_TADDR pBaseSPslots = GetFirstBaseSPslotPtr(pCtx->Ebp, &info);
    PTR_TADDR pPrevSlot    = pBaseSPslots - (nestingLevel - 1);

    /* Currently, LeaveFinally() is not used if the finally is invoked in the
       second pass for unwinding. So we expect the finally to be called locally */
    _ASSERTE(*pPrevSlot == LCL_FINALLY_MARK);

    *pPrevSlot = 0; // Zero out the previous shadow ESP

    pCtx->Esp += sizeof(TADDR); // Pop the return value off the stack
    return TRUE;
}

void EECodeManager::LeaveCatch(GCInfoToken gcInfoToken,
                                unsigned offset,
                                PCONTEXT pCtx)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef _DEBUG
    TADDR       baseSP;
    DWORD       nestingLevel;
    bool        hasInnerFilter;
    hdrInfo     info;

    DecodeGCHdrInfo(gcInfoToken, offset, &info);
    GetHandlerFrameInfo(&info, pCtx->Ebp, pCtx->Esp, (DWORD) IGNORE_VAL,
                        &baseSP, &nestingLevel, &hasInnerFilter);
//    _ASSERTE(frameType == FR_HANDLER);
//    _ASSERTE(pCtx->Esp == baseSP);
#endif

    return;
}
#endif // !FEATURE_EH_FUNCLETS
#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void EECodeManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_VTHIS();
}

#endif // #ifdef DACCESS_COMPILE


#ifdef TARGET_X86
/*
 *  GetAmbientSP
 *
 *  This function computes the zero-depth stack pointer for the given nesting
 *  level within the method given.  Nesting level is the depth within
 *  try-catch-finally blocks, and is zero based.  It is up to the caller to
 *  supply a valid nesting level value.
 *
 */

TADDR EECodeManager::GetAmbientSP(PREGDISPLAY     pContext,
                                  EECodeInfo     *pCodeInfo,
                                  DWORD           dwRelOffset,
                                  DWORD           nestingLevel,
                                  CodeManState   *pState)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();

    _ASSERTE(sizeof(CodeManStateBuf) <= sizeof(pState->stateBuf));
    CodeManStateBuf * stateBuf = (CodeManStateBuf*)pState->stateBuf;
    PTR_CBYTE table = PTR_CBYTE(gcInfoToken.Info);

    /* Extract the necessary information from the info block header */

    stateBuf->hdrInfoSize = (DWORD)DecodeGCHdrInfo(gcInfoToken,
                                                   dwRelOffset,
                                                   &stateBuf->hdrInfoBody);
    table += stateBuf->hdrInfoSize;

    pState->dwIsSet = 1;

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (trFixContext)
    {
        printf("GetAmbientSP [%s][%s] for %s.%s: ",
               stateBuf->hdrInfoBody.ebpFrame?"ebp":"   ",
               stateBuf->hdrInfoBody.interruptible?"int":"   ",
               "UnknownClass","UnknownMethod");
        fflush(stdout);
    }
#endif // _DEBUG && !DACCESS_COMPILE

    if ((stateBuf->hdrInfoBody.prologOffs != hdrInfo::NOT_IN_PROLOG) ||
        (stateBuf->hdrInfoBody.epilogOffs != hdrInfo::NOT_IN_EPILOG))
    {
        return NULL;
    }

    /* make sure that we have an ebp stack frame */

    if (stateBuf->hdrInfoBody.handlers)
    {
        _ASSERTE(stateBuf->hdrInfoBody.ebpFrame);

        TADDR      baseSP;
        GetHandlerFrameInfo(&stateBuf->hdrInfoBody,
                            GetRegdisplayFP(pContext),
                            (DWORD) IGNORE_VAL,
                            nestingLevel,
                            &baseSP);

        _ASSERTE((GetRegdisplayFP(pContext) >= baseSP) && (baseSP >= GetRegdisplaySP(pContext)));

        return baseSP;
    }

    _ASSERTE(nestingLevel == 0);

    if (stateBuf->hdrInfoBody.ebpFrame)
    {
        return GetOutermostBaseFP(GetRegdisplayFP(pContext), &stateBuf->hdrInfoBody);
    }

    TADDR baseSP = GetRegdisplaySP(pContext);
    if  (stateBuf->hdrInfoBody.interruptible)
    {
        baseSP += scanArgRegTableI(skipToArgReg(stateBuf->hdrInfoBody, table),
                                   dwRelOffset,
                                   dwRelOffset,
                                   &stateBuf->hdrInfoBody);
    }
    else
    {
        baseSP += scanArgRegTable(skipToArgReg(stateBuf->hdrInfoBody, table),
                                  dwRelOffset,
                                  &stateBuf->hdrInfoBody);
    }

    return baseSP;
}
#endif // TARGET_X86

/*
    Get the number of bytes used for stack parameters.
    This is currently only used on x86.
 */

// virtual
ULONG32 EECodeManager::GetStackParameterSize(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#if defined(TARGET_X86)
#if defined(FEATURE_EH_FUNCLETS)
    if (pCodeInfo->IsFunclet())
    {
        // Funclet has no stack argument
        return 0;
    }
#endif // FEATURE_EH_FUNCLETS

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    unsigned    dwOffset = pCodeInfo->GetRelOffset();

    CodeManState state;
    state.dwIsSet = 0;

    _ASSERTE(sizeof(CodeManStateBuf) <= sizeof(state.stateBuf));
    CodeManStateBuf * pStateBuf = reinterpret_cast<CodeManStateBuf *>(state.stateBuf);

    hdrInfo * pHdrInfo = &(pStateBuf->hdrInfoBody);
    pStateBuf->hdrInfoSize = (DWORD)DecodeGCHdrInfo(gcInfoToken, dwOffset, pHdrInfo);

    // We need to subtract 4 here because ESPIncrOnReturn() includes the stack slot containing the return
    // address.
    return (ULONG32)::GetStackParameterSize(pHdrInfo);

#else
    return 0;

#endif // TARGET_X86
}

