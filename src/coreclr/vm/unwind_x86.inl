// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define RETURN_ADDR_OFFS        1       // in DWORDS

#define X86_INSTR_TEST_ESP_SIB          0x24
#define X86_INSTR_PUSH_0                0x6A    // push 00, entire instruction is 0x6A00
#define X86_INSTR_PUSH_IMM              0x68    // push NNNN,
#define X86_INSTR_W_PUSH_IND_IMM        0x35FF  // push [NNNN]
#define X86_INSTR_CALL_REL32            0xE8    // call rel32
#define X86_INSTR_W_CALL_IND_IMM        0x15FF  // call [addr32]
#define X86_INSTR_NOP                   0x90    // nop
#define X86_INSTR_NOP2                  0x9090  // 2-byte nop
#define X86_INSTR_NOP3_1                0x9090  // 1st word of 3-byte nop
#define X86_INSTR_NOP3_3                0x90    // 3rd byte of 3-byte nop
#define X86_INSTR_NOP4                  0x90909090 // 4-byte nop
#define X86_INSTR_NOP5_1                0x90909090 // 1st dword of 5-byte nop
#define X86_INSTR_NOP5_5                0x90    // 5th byte of 5-byte nop
#define X86_INSTR_INT3                  0xCC    // int3
#define X86_INSTR_HLT                   0xF4    // hlt
#define X86_INSTR_PUSH_EAX              0x50    // push eax
#define X86_INSTR_PUSH_EBP              0x55    // push ebp
#define X86_INSTR_W_MOV_EBP_ESP         0xEC8B  // mov ebp, esp
#define X86_INSTR_POP_ECX               0x59    // pop ecx
#define X86_INSTR_RET                   0xC2    // ret imm16
#define X86_INSTR_RETN                  0xC3    // ret
#define X86_INSTR_XOR                   0x33    // xor
#define X86_INSTR_w_TEST_ESP_EAX        0x0485  // test [esp], eax
#define X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX   0x8485      // test [esp-dwOffset], eax
#define X86_INSTR_w_LEA_ESP_EBP_BYTE_OFFSET     0x658d      // lea esp, [ebp-bOffset]
#define X86_INSTR_w_LEA_ESP_EBP_DWORD_OFFSET    0xa58d      // lea esp, [ebp-dwOffset]
#define X86_INSTR_w_LEA_EAX_ESP_BYTE_OFFSET     0x448d      // lea eax, [esp-bOffset]
#define X86_INSTR_w_LEA_EAX_ESP_DWORD_OFFSET    0x848d      // lea eax, [esp-dwOffset]
#define X86_INSTR_JMP_NEAR_REL32        0xE9    // near jmp rel32
#define X86_INSTR_w_JMP_FAR_IND_IMM     0x25FF  // far jmp [addr32]

#ifdef  _DEBUG
// For dumping of verbose info.
#ifndef DACCESS_COMPILE
static  bool  trFixContext          = false;
#endif
static  bool  trEnumGCRefs          = false;
static  bool  dspPtr                = false; // prints the live ptrs as reported
#endif

// NOTE: enabling compiler optimizations, even for debug builds.
// Comment this out in order to be able to fully debug methods here.
#if defined(_MSC_VER)
#pragma optimize("tg", on)
#endif

__forceinline unsigned decodeUnsigned(PTR_CBYTE& src)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

#ifdef DACCESS_COMPILE
    PTR_CBYTE begin = src;
#endif

    BYTE     byte  = *src++;
    unsigned value = byte & 0x7f;
    while (byte & 0x80)
    {
#ifdef DACCESS_COMPILE
        // In DAC builds, the target data may be corrupt.  Rather than return incorrect data
        // and risk wasting time in a potentially long loop, we want to fail early and gracefully.
        // The data is encoded with 7 value-bits per byte, and so we may need to read a maximum
        // of 5 bytes (7*5=35) to read a full 32-bit integer.
        if ((src - begin) > 5)
        {
            DacError(CORDBG_E_TARGET_INCONSISTENT);
        }
#endif

        byte    = *src++;
        value <<= 7;
        value  += byte & 0x7f;
    }
    return value;
}

__forceinline int decodeSigned(PTR_CBYTE& src)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

#ifdef DACCESS_COMPILE
    PTR_CBYTE begin = src;
#endif

    BYTE     byte  = *src++;
    BYTE     first = byte;
    int      value = byte & 0x3f;
    while (byte & 0x80)
    {
#ifdef DACCESS_COMPILE
        // In DAC builds, the target data may be corrupt.  Rather than return incorrect data
        // and risk wasting time in a potentially long loop, we want to fail early and gracefully.
        // The data is encoded with 7 value-bits per byte, and so we may need to read a maximum
        // of 5 bytes (7*5=35) to read a full 32-bit integer.
        if ((src - begin) > 5)
        {
            DacError(CORDBG_E_TARGET_INCONSISTENT);
        }
#endif

        byte = *src++;
        value <<= 7;
        value += byte & 0x7f;
    }
    if (first & 0x40)
        value = -value;
    return value;
}

// Fast versions of the above, with one iteration of the loop unrolled
#define fastDecodeUnsigned(src) (((*(src) & 0x80) == 0) ? (unsigned) (*(src)++) : decodeUnsigned((src)))
#define fastDecodeSigned(src)   (((*(src) & 0xC0) == 0) ? (unsigned) (*(src)++) : decodeSigned((src)))

// Fast skipping past encoded integers
#ifndef DACCESS_COMPILE
#define fastSkipUnsigned(src) { while ((*(src)++) & 0x80) { } }
#define fastSkipSigned(src)   { while ((*(src)++) & 0x80) { } }
#else
// In DAC builds we want to trade-off a little perf in the common case for reliaiblity against corrupt data.
#define fastSkipUnsigned(src) (decodeUnsigned(src))
#define fastSkipSigned(src) (decodeSigned(src))
#endif


/*****************************************************************************
 *
 *  Decodes the X86 GcInfo header and returns the decoded information
 *  in the hdrInfo struct.
 *  curOffset is the code offset within the active method used in the
 *  computation of PrologOffs/EpilogOffs.
 *  Returns the size of the header (number of bytes decoded).
 */
size_t DecodeGCHdrInfo(GCInfoToken gcInfoToken,
                       unsigned    curOffset,
                       hdrInfo   * infoPtr)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    PTR_CBYTE table = (PTR_CBYTE) gcInfoToken.Info;
#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xFEEF);
#endif

    infoPtr->methodSize = fastDecodeUnsigned(table);

    _ASSERTE(curOffset >= 0);
    _ASSERTE(curOffset <= infoPtr->methodSize);

    /* Decode the InfoHdr */

    InfoHdr header;
    table = decodeHeader(table, gcInfoToken.Version, &header);

    BOOL hasArgTabOffset = FALSE;
    if (header.untrackedCnt == HAS_UNTRACKED)
    {
        hasArgTabOffset = TRUE;
        header.untrackedCnt = fastDecodeUnsigned(table);
    }

    if (header.varPtrTableSize == HAS_VARPTR)
    {
        hasArgTabOffset = TRUE;
        header.varPtrTableSize = fastDecodeUnsigned(table);
    }

    if (header.gsCookieOffset == HAS_GS_COOKIE_OFFSET)
    {
        header.gsCookieOffset = fastDecodeUnsigned(table);
    }

    if (header.syncStartOffset == HAS_SYNC_OFFSET)
    {
        header.syncStartOffset = decodeUnsigned(table);
        header.syncEndOffset = decodeUnsigned(table);

        _ASSERTE(header.syncStartOffset != INVALID_SYNC_OFFSET && header.syncEndOffset != INVALID_SYNC_OFFSET);
        _ASSERTE(header.syncStartOffset < header.syncEndOffset);
    }

    if (header.revPInvokeOffset == HAS_REV_PINVOKE_FRAME_OFFSET)
    {
        header.revPInvokeOffset = fastDecodeUnsigned(table);
    }

    /* Some sanity checks on header */

    _ASSERTE( header.prologSize +
           (size_t)(header.epilogCount*header.epilogSize) <= infoPtr->methodSize);
    _ASSERTE( header.epilogCount == 1 || !header.epilogAtEnd);

    _ASSERTE( header.untrackedCnt <= header.argCount+header.frameSize);

    _ASSERTE( header.ebpSaved || !(header.ebpFrame || header.doubleAlign));
    _ASSERTE(!header.ebpFrame || !header.doubleAlign  );
    _ASSERTE( header.ebpFrame || !header.security     );
    _ASSERTE( header.ebpFrame || !header.handlers     );
    _ASSERTE( header.ebpFrame || !header.localloc     );
    _ASSERTE( header.ebpFrame || !header.editNcontinue);  // <TODO> : Esp frames NYI for EnC</TODO>

    /* Initialize the infoPtr struct */

    infoPtr->argSize         = header.argCount * 4;
    infoPtr->ebpFrame        = header.ebpFrame;
    infoPtr->interruptible   = header.interruptible;
    infoPtr->returnKind      = (ReturnKind) header.returnKind;

    infoPtr->prologSize      = header.prologSize;
    infoPtr->epilogSize      = header.epilogSize;
    infoPtr->epilogCnt       = header.epilogCount;
    infoPtr->epilogEnd       = header.epilogAtEnd;

    infoPtr->untrackedCnt    = header.untrackedCnt;
    infoPtr->varPtrTableSize = header.varPtrTableSize;
    infoPtr->gsCookieOffset  = header.gsCookieOffset;

    infoPtr->syncStartOffset = header.syncStartOffset;
    infoPtr->syncEndOffset   = header.syncEndOffset;
    infoPtr->revPInvokeOffset = header.revPInvokeOffset;

    infoPtr->doubleAlign     = header.doubleAlign;
    infoPtr->handlers        = header.handlers;
    infoPtr->localloc        = header.localloc;
    infoPtr->editNcontinue   = header.editNcontinue;
    infoPtr->varargs         = header.varargs;
    infoPtr->profCallbacks   = header.profCallbacks;
    infoPtr->genericsContext = header.genericsContext;
    infoPtr->genericsContextIsMethodDesc = header.genericsContextIsMethodDesc;
    infoPtr->isSpeculativeStackWalk = false;

    /* Are we within the prolog of the method? */

    if  (curOffset < infoPtr->prologSize)
    {
        infoPtr->prologOffs = curOffset;
    }
    else
    {
        infoPtr->prologOffs = hdrInfo::NOT_IN_PROLOG;
    }

    /* Assume we're not in the epilog of the method */

    infoPtr->epilogOffs = hdrInfo::NOT_IN_EPILOG;

    /* Are we within an epilog of the method? */

    if  (infoPtr->epilogCnt)
    {
        unsigned epilogStart;

        if  (infoPtr->epilogCnt > 1 || !infoPtr->epilogEnd)
        {
#if VERIFY_GC_TABLES
            _ASSERTE(*castto(table, unsigned short *)++ == 0xFACE);
#endif
            epilogStart = 0;
            for (unsigned i = 0; i < infoPtr->epilogCnt; i++)
            {
                epilogStart += fastDecodeUnsigned(table);
                if  (curOffset > epilogStart &&
                     curOffset < epilogStart + infoPtr->epilogSize)
                {
                    infoPtr->epilogOffs = curOffset - epilogStart;
                }
            }
        }
        else
        {
            epilogStart = infoPtr->methodSize - infoPtr->epilogSize;

            if  (curOffset > epilogStart &&
                 curOffset < epilogStart + infoPtr->epilogSize)
            {
                infoPtr->epilogOffs = curOffset - epilogStart;
            }
        }

        infoPtr->syncEpilogStart = epilogStart;
    }

    unsigned argTabOffset = INVALID_ARGTAB_OFFSET;
    if (hasArgTabOffset)
    {
        argTabOffset = fastDecodeUnsigned(table);
    }
    infoPtr->argTabOffset    = argTabOffset;

    size_t frameDwordCount = header.frameSize;

    /* Set the rawStackSize to the number of bytes that it bumps ESP */

    infoPtr->rawStkSize = (UINT)(frameDwordCount * sizeof(size_t));

    /* Calculate the callee saves regMask and adjust stackSize to */
    /* include the callee saves register spills                   */

    unsigned savedRegs = RM_NONE;
    unsigned savedRegsCount = 0;

    if  (header.ediSaved)
    {
        savedRegsCount++;
        savedRegs |= RM_EDI;
    }
    if  (header.esiSaved)
    {
        savedRegsCount++;
        savedRegs |= RM_ESI;
    }
    if  (header.ebxSaved)
    {
        savedRegsCount++;
        savedRegs |= RM_EBX;
    }
    if  (header.ebpSaved)
    {
        savedRegsCount++;
        savedRegs |= RM_EBP;
    }

    infoPtr->savedRegMask = (RegMask)savedRegs;

    infoPtr->savedRegsCountExclFP = savedRegsCount;
    if (header.ebpFrame || header.doubleAlign)
    {
        _ASSERTE(header.ebpSaved);
        infoPtr->savedRegsCountExclFP = savedRegsCount - 1;
    }

    frameDwordCount += savedRegsCount;

    infoPtr->stackSize  =  (UINT)(frameDwordCount * sizeof(size_t));

    _ASSERTE(infoPtr->gsCookieOffset == INVALID_GS_COOKIE_OFFSET ||
             (infoPtr->gsCookieOffset < infoPtr->stackSize) &&
             ((header.gsCookieOffset % sizeof(void*)) == 0));

    return  table - PTR_CBYTE(gcInfoToken.Info);
}

/*****************************************************************************/

// We do a "pop eax; jmp eax" to return from a fault or finally handler
const size_t END_FIN_POP_STACK = sizeof(TADDR);

inline
size_t GetLocallocSPOffset(hdrInfo * info)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(info->localloc && info->ebpFrame);

    unsigned position = info->savedRegsCountExclFP +
                        1;
    return position * sizeof(TADDR);
}

#ifndef FEATURE_NATIVEAOT
inline
size_t GetParamTypeArgOffset(hdrInfo * info)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE((info->genericsContext || info->handlers) && info->ebpFrame);

    unsigned position = info->savedRegsCountExclFP +
                        info->localloc +
                        1;  // For CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
    return position * sizeof(TADDR);
}

inline size_t GetStartShadowSPSlotsOffset(hdrInfo * info)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(info->handlers && info->ebpFrame);

    return GetParamTypeArgOffset(info) +
           sizeof(TADDR); // Slot for end-of-last-executed-filter
}

/*****************************************************************************
 *  Returns the start of the hidden slots for the shadowSP for functions
 *  with exception handlers. There is one slot per nesting level starting
 *  near Ebp and is zero-terminated after the active slots.
 */

inline
PTR_TADDR GetFirstBaseSPslotPtr(TADDR ebp, hdrInfo * info)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(info->handlers && info->ebpFrame);

    size_t offsetFromEBP = GetStartShadowSPSlotsOffset(info)
                        + sizeof(TADDR); // to get to the *start* of the next slot

    return PTR_TADDR(ebp - offsetFromEBP);
}

inline size_t GetEndShadowSPSlotsOffset(hdrInfo * info, unsigned maxHandlerNestingLevel)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(info->handlers && info->ebpFrame);

    unsigned numberOfShadowSPSlots = maxHandlerNestingLevel +
                                     1 + // For zero-termination
                                     1; // For a filter (which can be active at the same time as a catch/finally handler

    return GetStartShadowSPSlotsOffset(info) +
           (numberOfShadowSPSlots * sizeof(TADDR));
}

/*****************************************************************************
 *    returns the base frame pointer corresponding to the target nesting level.
 */

inline
TADDR GetOutermostBaseFP(TADDR ebp, hdrInfo * info)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // we are not taking into account double alignment.  We are
    // safe because the jit currently bails on double alignment if there
    // are handles or localalloc
    _ASSERTE(!info->doubleAlign);
    if (info->localloc)
    {
        // If the function uses localloc we will fetch the ESP from the localloc
        // slot.
        PTR_TADDR pLocalloc = PTR_TADDR(ebp - GetLocallocSPOffset(info));

        return (*pLocalloc);
    }
    else
    {
        // Default, go back all the method's local stack size
        return ebp - info->stackSize + sizeof(int);
    }
}

/*****************************************************************************
 *
 *  For functions with handlers, checks if it is currently in a handler.
 *  Either of unwindESP or unwindLevel will specify the target nesting level.
 *  If unwindLevel is specified, info about the funclet at that nesting level
 *    will be returned. (Use if you are interested in a specific nesting level.)
 *  If unwindESP is specified, info for nesting level invoked before the stack
 *   reached unwindESP will be returned. (Use if you have a specific ESP value
 *   during stack walking.)
 *
 *  *pBaseSP is set to the base SP (base of the stack on entry to
 *    the current funclet) corresponding to the target nesting level.
 *  *pNestLevel is set to the nesting level of the target nesting level (useful
 *    if unwindESP!=IGNORE_VAL
 *  *pHasInnerFilter will be set to true (only when unwindESP!=IGNORE_VAL) if a filter
 *    is currently active, but the target nesting level is an outer nesting level.
 *  *pHadInnerFilter - was the last use of the frame to execute a filter.
 *    This mainly affects GC lifetime reporting.
 */

enum FrameType
{
    FR_NORMAL,              // Normal method frame - no exceptions currently active
    FR_FILTER,              // Frame-let of a filter
    FR_HANDLER,             // Frame-let of a callable catch/fault/finally

    FR_INVALID,             // Invalid frame (for speculative stackwalks)
};

enum { IGNORE_VAL = -1 };

FrameType   GetHandlerFrameInfo(hdrInfo   * info,
                                TADDR       frameEBP,
                                TADDR       unwindESP,
                                DWORD       unwindLevel,
                                TADDR     * pBaseSP = NULL,         /* OUT */
                                DWORD     * pNestLevel = NULL,      /* OUT */
                                bool      * pHasInnerFilter = NULL, /* OUT */
                                bool      * pHadInnerFilter = NULL) /* OUT */
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    _ASSERTE(info->ebpFrame && info->handlers);
    // One and only one of them should be IGNORE_VAL
    _ASSERTE((unwindESP == (TADDR) IGNORE_VAL) !=
           (unwindLevel == (DWORD) IGNORE_VAL));
    _ASSERTE(pHasInnerFilter == NULL || unwindESP != (TADDR) IGNORE_VAL);

    // Many of the conditions that we'd like to assert cannot be asserted in the case that we're
    // in the middle of a stackwalk seeded by a profiler, since such seeds can't be trusted
    // (profilers are external, untrusted sources).  So during profiler walks, we test the condition
    // and throw an exception if it's not met.  Otherwise, we just assert the condition.
    #define FAIL_IF_SPECULATIVE_WALK(condition)         \
        if (info->isSpeculativeStackWalk)               \
        {                                               \
            if (!(condition))                           \
            {                                           \
                return FR_INVALID;                      \
            }                                           \
        }                                               \
        else                                            \
        {                                               \
            _ASSERTE(condition);                        \
        }

    PTR_TADDR pFirstBaseSPslot = GetFirstBaseSPslotPtr(frameEBP, info);
    TADDR  baseSP            = GetOutermostBaseFP(frameEBP, info);
    bool    nonLocalHandlers = false; // Are the funclets invoked by EE (instead of managed code itself)
    bool    hasInnerFilter   = false;
    bool    hadInnerFilter   = false;

    /* Get the last non-zero slot >= unwindESP, or lvl<unwindLevel.
       Also do some sanity checks */

    // The shadow slots contain the SP of the nested EH clauses currently active on the stack.
    // The slots grow towards lower address on the stack and is terminted by a NULL entry.
    // Since each subsequent slot contains the SP of a more nested EH clause, the contents of the slots are
    // expected to be in decreasing order.
    size_t lvl = 0;
#ifndef FEATURE_EH_FUNCLETS
    PTR_TADDR pSlot;
    for(lvl = 0, pSlot = pFirstBaseSPslot;
        *pSlot && lvl < unwindLevel;
        pSlot--, lvl++)
    {
        // Filters cant have inner funclets
        FAIL_IF_SPECULATIVE_WALK(!(baseSP & ICodeManager::SHADOW_SP_IN_FILTER));

        TADDR curSlotVal = *pSlot;

        // The shadowSPs have to be less unless the stack has been unwound.
        FAIL_IF_SPECULATIVE_WALK(baseSP >  curSlotVal ||
               (baseSP == curSlotVal && pSlot == pFirstBaseSPslot));

        if (curSlotVal == LCL_FINALLY_MARK)
        {
            // Locally called finally
            baseSP -= sizeof(TADDR);
        }
        else
        {
            // Is this a funclet we unwound before (can only happen with filters) ?
            // If unwindESP is specified, normally we expect it to be the last entry in the shadow slot array.
            // Or, if there is a filter, we expect unwindESP to be the second last entry.  However, this may
            // not be the case in DAC builds.  For example, the user can use .cxr in an EH clause to set a
            // CONTEXT captured in the try clause.  In this case, unwindESP will be the ESP of the parent
            // function, but the shadow slot array will contain the SP of the EH clause, which is closer to
            // the leaf than the parent method.

            if (unwindESP != (TADDR) IGNORE_VAL &&
                unwindESP > END_FIN_POP_STACK +
                (curSlotVal & ~ICodeManager::SHADOW_SP_BITS))
            {
                // In non-DAC builds, the only time unwindESP is closer to the root than entries in the shadow
                // slot array is when the last entry in the array is for a filter.  Also, filters can't have
                // nested handlers.
                if ((pSlot[0] & ICodeManager::SHADOW_SP_IN_FILTER) &&
                    (pSlot[-1] == 0) &&
                    !(baseSP & ICodeManager::SHADOW_SP_IN_FILTER))
                {
                    if (pSlot[0] & ICodeManager::SHADOW_SP_FILTER_DONE)
                        hadInnerFilter = true;
                    else
                        hasInnerFilter = true;
                    break;
                }
                else
                {
#if defined(DACCESS_COMPILE)
                    // In DAC builds, this could happen.  We just need to bail out of this loop early.
                    break;
#else  // !DACCESS_COMPILE
                    // In non-DAC builds, this is an error.
                    FAIL_IF_SPECULATIVE_WALK(FALSE);
#endif // DACCESS_COMPILE
                }
            }

            nonLocalHandlers = true;
            baseSP = curSlotVal;
        }
    }
#endif // FEATURE_EH_FUNCLETS

    if (unwindESP != (TADDR) IGNORE_VAL)
    {
        FAIL_IF_SPECULATIVE_WALK(baseSP >= unwindESP ||
               baseSP == unwindESP - sizeof(TADDR));  // About to locally call a finally

        if (baseSP < unwindESP)                       // About to locally call a finally
            baseSP = unwindESP;
    }
    else
    {
        FAIL_IF_SPECULATIVE_WALK(lvl == unwindLevel); // unwindLevel must be currently active on stack
    }

    if (pBaseSP)
        *pBaseSP = baseSP & ~ICodeManager::SHADOW_SP_BITS;

    if (pNestLevel)
    {
        *pNestLevel = (DWORD)lvl;
    }

    if (pHasInnerFilter)
        *pHasInnerFilter = hasInnerFilter;

    if (pHadInnerFilter)
        *pHadInnerFilter = hadInnerFilter;

    if (baseSP & ICodeManager::SHADOW_SP_IN_FILTER)
    {
        FAIL_IF_SPECULATIVE_WALK(!hasInnerFilter); // nested filters not allowed
        return FR_FILTER;
    }
    else if (nonLocalHandlers)
    {
        return FR_HANDLER;
    }
    else
    {
        return FR_NORMAL;
    }

    #undef FAIL_IF_SPECULATIVE_WALK
}

// Returns the number of bytes at the beginning of the stack frame that shouldn't be
// modified by an EnC.  This is everything except the space for locals and temporaries.
inline size_t GetSizeOfFrameHeaderForEnC(hdrInfo * info)
{
    WRAPPER_NO_CONTRACT;

    // See comment above Compiler::lvaAssignFrameOffsets() in src\jit\il\lclVars.cpp
    // for frame layout

    // EnC supports increasing the maximum handler nesting level by always
    // assuming that the max is MAX_EnC_HANDLER_NESTING_LEVEL. Methods with
    // a higher max cannot be updated by EnC

    // Take the offset (from EBP) of the last slot of the header, plus one for the EBP slot itself
    // to get the total size of the header.
    return sizeof(TADDR) +
            GetEndShadowSPSlotsOffset(info, MAX_EnC_HANDLER_NESTING_LEVEL);
}
#endif

/*****************************************************************************/
static
PTR_CBYTE skipToArgReg(const hdrInfo& info, PTR_CBYTE table)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#ifdef _DEBUG
    PTR_CBYTE tableStart = table;
#else
    if (info.argTabOffset != INVALID_ARGTAB_OFFSET)
    {
        return table + info.argTabOffset;
    }
#endif

    unsigned count;

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEF);
#endif

    /* Skip over the untracked frame variable table */

    count = info.untrackedCnt;
    while (count-- > 0) {
        fastSkipSigned(table);
    }

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xCAFE);
#endif

    /* Skip over the frame variable lifetime table */

    count = info.varPtrTableSize;
    while (count-- > 0) {
        fastSkipUnsigned(table); fastSkipUnsigned(table); fastSkipUnsigned(table);
    }

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *) == 0xBABE);
#endif

#if defined(_DEBUG) && defined(CONSISTENCY_CHECK_MSGF)
    if (info.argTabOffset != INVALID_ARGTAB_OFFSET)
    {
        CONSISTENCY_CHECK_MSGF((info.argTabOffset == (unsigned) (table - tableStart)),
          ("table = %p, tableStart = %p, info.argTabOffset = %d", table, tableStart, info.argTabOffset));
    }
#endif

    return table;
}

/*****************************************************************************/

#define regNumToMask(regNum) RegMask(1<<(regNum))

/*****************************************************************************
 Helper for scanArgRegTable() and scanArgRegTableI() for regMasks
 */

void *      getCalleeSavedReg(PREGDISPLAY pContext, regNum reg)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    switch (reg)
    {
        case REGI_EBP: return pContext->GetEbpLocation();
        case REGI_EBX: return pContext->GetEbxLocation();
        case REGI_ESI: return pContext->GetEsiLocation();
        case REGI_EDI: return pContext->GetEdiLocation();

        default: _ASSERTE(!"bad info.thisPtrResult"); return NULL;
    }
}

/*****************************************************************************
 These functions converts the bits in the GC encoding to RegMask
 */

inline
RegMask     convertCalleeSavedRegsMask(unsigned inMask) // EBP,EBX,ESI,EDI
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE((inMask & 0x0F) == inMask);

    unsigned outMask = RM_NONE;
    if (inMask & 0x1) outMask |= RM_EDI;
    if (inMask & 0x2) outMask |= RM_ESI;
    if (inMask & 0x4) outMask |= RM_EBX;
    if (inMask & 0x8) outMask |= RM_EBP;

    return (RegMask) outMask;
}

inline
RegMask     convertAllRegsMask(unsigned inMask) // EAX,ECX,EDX,EBX, EBP,ESI,EDI
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE((inMask & 0xEF) == inMask);

    unsigned outMask = RM_NONE;
    if (inMask & 0x01) outMask |= RM_EAX;
    if (inMask & 0x02) outMask |= RM_ECX;
    if (inMask & 0x04) outMask |= RM_EDX;
    if (inMask & 0x08) outMask |= RM_EBX;
    if (inMask & 0x20) outMask |= RM_EBP;
    if (inMask & 0x40) outMask |= RM_ESI;
    if (inMask & 0x80) outMask |= RM_EDI;

    return (RegMask)outMask;
}

/*****************************************************************************
 * scan the register argument table for the not fully interruptible case.
   this function is called to find all live objects (pushed arguments)
   and to get the stack base for EBP-less methods.

   NOTE: If info->argTabResult is NULL, info->argHnumResult indicates
         how many bits in argMask are valid
         If info->argTabResult is non-NULL, then the argMask field does
         not fit in 32-bits and the value in argMask meaningless.
         Instead argHnum specifies the number of (variable-length) elements
         in the array, and argTabBytes specifies the total byte size of the
         array. [ Note this is an extremely rare case ]
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
static
unsigned scanArgRegTable(PTR_CBYTE    table,
                         unsigned     curOffs,
                         hdrInfo    * info)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    regNum    thisPtrReg    = REGI_NA;
#ifdef _DEBUG
    bool      isCall        = false;
#endif
    unsigned  regMask       = 0;    // EBP,EBX,ESI,EDI
    unsigned  argMask       = 0;
    unsigned  argHnum       = 0;
    PTR_CBYTE argTab        = 0;
    unsigned  argTabBytes   = 0;
    unsigned  stackDepth    = 0;

    unsigned  iregMask      = 0;    // EBP,EBX,ESI,EDI
    unsigned  iargMask      = 0;
    unsigned  iptrMask      = 0;

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBABE);
#endif

    unsigned scanOffs = 0;

    _ASSERTE(scanOffs <= info->methodSize);

    if (info->ebpFrame) {
  /*
      Encoding table for methods with an EBP frame and
                         that are not fully interruptible

      The encoding used is as follows:

      this pointer encodings:

         01000000          this pointer in EBX
         00100000          this pointer in ESI
         00010000          this pointer in EDI

      tiny encoding:

         0bsdDDDD
                           requires code delta     < 16 (4-bits)
                           requires pushed argmask == 0

           where    DDDD   is code delta
                       b   indicates that register EBX is a live pointer
                       s   indicates that register ESI is a live pointer
                       d   indicates that register EDI is a live pointer

      small encoding:

         1DDDDDDD bsdAAAAA

                           requires code delta     < 120 (7-bits)
                           requires pushed argmask <  64 (5-bits)

           where DDDDDDD   is code delta
                   AAAAA   is the pushed args mask
                       b   indicates that register EBX is a live pointer
                       s   indicates that register ESI is a live pointer
                       d   indicates that register EDI is a live pointer

      medium encoding

         0xFD aaaaaaaa AAAAdddd bseDDDDD

                           requires code delta     <    0x1000000000  (9-bits)
                           requires pushed argmask < 0x1000000000000 (12-bits)

           where    DDDDD  is the upper 5-bits of the code delta
                     dddd  is the low   4-bits of the code delta
                     AAAA  is the upper 4-bits of the pushed arg mask
                 aaaaaaaa  is the low   8-bits of the pushed arg mask
                        b  indicates that register EBX is a live pointer
                        s  indicates that register ESI is a live pointer
                        e  indicates that register EDI is a live pointer

      medium encoding with interior pointers

         0xF9 DDDDDDDD bsdAAAAAA iiiIIIII

                           requires code delta     < (8-bits)
                           requires pushed argmask < (5-bits)

           where  DDDDDDD  is the code delta
                        b  indicates that register EBX is a live pointer
                        s  indicates that register ESI is a live pointer
                        d  indicates that register EDI is a live pointer
                    AAAAA  is the pushed arg mask
                      iii  indicates that EBX,EDI,ESI are interior pointers
                    IIIII  indicates that bits is the arg mask are interior
                           pointers

      large encoding

         0xFE [0BSD0bsd][32-bit code delta][32-bit argMask]

                        b  indicates that register EBX is a live pointer
                        s  indicates that register ESI is a live pointer
                        d  indicates that register EDI is a live pointer
                        B  indicates that register EBX is an interior pointer
                        S  indicates that register ESI is an interior pointer
                        D  indicates that register EDI is an interior pointer
                           requires pushed  argmask < 32-bits

      large encoding  with interior pointers

         0xFA [0BSD0bsd][32-bit code delta][32-bit argMask][32-bit interior pointer mask]


                        b  indicates that register EBX is a live pointer
                        s  indicates that register ESI is a live pointer
                        d  indicates that register EDI is a live pointer
                        B  indicates that register EBX is an interior pointer
                        S  indicates that register ESI is an interior pointer
                        D  indicates that register EDI is an interior pointer
                           requires pushed  argmask < 32-bits
                           requires pushed iArgmask < 32-bits

      huge encoding        This is the only encoding that supports
                           a pushed argmask which is greater than
                           32-bits.

         0xFB [0BSD0bsd][32-bit code delta]
              [32-bit table count][32-bit table size]
              [pushed ptr offsets table...]

                       b   indicates that register EBX is a live pointer
                       s   indicates that register ESI is a live pointer
                       d   indicates that register EDI is a live pointer
                       B   indicates that register EBX is an interior pointer
                       S   indicates that register ESI is an interior pointer
                       D   indicates that register EDI is an interior pointer
                       the list count is the number of entries in the list
                       the list size gives the byte-length of the list
                       the offsets in the list are variable-length
  */
        while (scanOffs < curOffs)
        {
            iregMask = 0;
            iargMask = 0;
            argTab = NULL;
#ifdef _DEBUG
            isCall = true;
#endif

            /* Get the next byte and check for a 'special' entry */

            unsigned encType = *table++;
#if defined(DACCESS_COMPILE)
            // In this scenario, it is invalid to have a zero byte in the GC info encoding (refer to the
            // comments above). At least one bit has to be set.  For example, a byte can represent which
            // register is the "this" pointer, and this byte has to be 0x10, 0x20, or 0x40.  Having a zero
            // byte indicates there is most likely some sort of DAC error, and it may lead to problems such as
            // infinite loops.  So we bail out early instead.
            if (encType == 0)
            {
                DacError(CORDBG_E_TARGET_INCONSISTENT);
                UNREACHABLE();
            }
#endif // DACCESS_COMPILE

            switch (encType)
            {
                unsigned    val, nxt;

            default:

                /* A tiny or small call entry */
                val = encType;
                if ((val & 0x80) == 0x00) {
                    if (val & 0x0F) {
                        /* A tiny call entry */
                        scanOffs += (val & 0x0F);
                        regMask   = (val & 0x70) >> 4;
                        argMask   = 0;
                        argHnum   = 0;
                    }
                    else {
                        /* This pointer liveness encoding */
                        regMask   = (val & 0x70) >> 4;
                        if (regMask == 0x1)
                            thisPtrReg = REGI_EDI;
                        else if (regMask == 0x2)
                            thisPtrReg = REGI_ESI;
                        else if (regMask == 0x4)
                            thisPtrReg = REGI_EBX;
                        else
                           _ASSERTE(!"illegal encoding for 'this' pointer liveness");
                    }
                }
                else {
                    /* A small call entry */
                    scanOffs += (val & 0x7F);
                    val       = *table++;
                    regMask   = val >> 5;
                    argMask   = val & 0x1F;
                    argHnum   = 5;
                }
                break;

            case 0xFD:  // medium encoding

                argMask   = *table++;
                val       = *table++;
                argMask  |= ((val & 0xF0) << 4);
                argHnum   = 12;
                nxt       = *table++;
                scanOffs += (val & 0x0F) + ((nxt & 0x1F) << 4);
                regMask   = nxt >> 5;                   // EBX,ESI,EDI

                break;

            case 0xF9:  // medium encoding with interior pointers

                scanOffs   += *table++;
                val         = *table++;
                argMask     = val & 0x1F;
                argHnum     = 5;
                regMask     = val >> 5;
                val         = *table++;
                iargMask    = val & 0x1F;
                iregMask    = val >> 5;

                break;

            case 0xFE:  // large encoding
            case 0xFA:  // large encoding with interior pointers

                val         = *table++;
                regMask     = val & 0x7;
                iregMask    = val >> 4;
                scanOffs   += *dac_cast<PTR_DWORD>(table);  table += sizeof(DWORD);
                argMask     = *dac_cast<PTR_DWORD>(table);  table += sizeof(DWORD);
                argHnum     = 31;
                if (encType == 0xFA) // read iargMask
                {
                    iargMask = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                }
                break;

            case 0xFB:  // huge encoding        This is the only partially interruptible
                        //                      encoding that supports a pushed ArgMask
                        //                      which is greater than 32-bits.
                        //                      The ArgMask is encoded using the argTab
                val         = *table++;
                regMask     = val & 0x7;
                iregMask    = val >> 4;
                scanOffs   += *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                argHnum     = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                argTabBytes = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                argTab      = table;                       table += argTabBytes;

                argMask     = 0;
                break;

            case 0xFF:
                scanOffs = curOffs + 1;
                break;

            } // end case

            // iregMask & iargMask are subsets of regMask & argMask respectively

            _ASSERTE((iregMask & regMask) == iregMask);
            _ASSERTE((iargMask & argMask) == iargMask);

        } // end while

    }
    else {

/*
 *    Encoding table for methods with an ESP frame and are not fully interruptible
 *    This encoding does not support a pushed ArgMask greater than 32
 *
 *               The encoding used is as follows:
 *
 *  push     000DDDDD                     ESP push one item with 5-bit delta
 *  push     00100000 [pushCount]         ESP push multiple items
 *  reserved 0011xxxx
 *  skip     01000000 [Delta]             Skip Delta, arbitrary sized delta
 *  skip     0100DDDD                     Skip small Delta, for call (DDDD != 0)
 *  pop      01CCDDDD                     ESP pop  CC items with 4-bit delta (CC != 00)
 *  call     1PPPPPPP                     Call Pattern, P=[0..79]
 *  call     1101pbsd DDCCCMMM            Call RegMask=pbsd,ArgCnt=CCC,
 *                                        ArgMask=MMM Delta=commonDelta[DD]
 *  call     1110pbsd [ArgCnt] [ArgMask]  Call ArgCnt,RegMask=pbsd,[32-bit ArgMask]
 *  call     11111000 [PBSDpbsd][32-bit delta][32-bit ArgCnt]
 *                    [32-bit PndCnt][32-bit PndSize][PndOffs...]
 *  iptr     11110000 [IPtrMask]          Arbitrary 32-bit Interior Pointer Mask
 *  thisptr  111101RR                     This pointer is in Register RR
 *                                        00=EDI,01=ESI,10=EBX,11=EBP
 *  reserved 111100xx                     xx  != 00
 *  reserved 111110xx                     xx  != 00
 *  reserved 11111xxx                     xxx != 000 && xxx != 111(EOT)
 *
 *   The value 11111111 [0xFF] indicates the end of the table.
 *
 *  An offset (at which stack-walking is performed) without an explicit encoding
 *  is assumed to be a trivial call-site (no GC registers, stack empty before and
 *  after) to avoid having to encode all trivial calls.
 *
 * Note on the encoding used for interior pointers
 *
 *   The iptr encoding must immediately precede a call encoding.  It is used to
 *   transform a normal GC pointer addresses into an interior pointers for GC purposes.
 *   The mask supplied to the iptr encoding is read from the least signicant bit
 *   to the most signicant bit. (i.e the lowest bit is read first)
 *
 *   p   indicates that register EBP is a live pointer
 *   b   indicates that register EBX is a live pointer
 *   s   indicates that register ESI is a live pointer
 *   d   indicates that register EDI is a live pointer
 *   P   indicates that register EBP is an interior pointer
 *   B   indicates that register EBX is an interior pointer
 *   S   indicates that register ESI is an interior pointer
 *   D   indicates that register EDI is an interior pointer
 *
 *   As an example the following sequence indicates that EDI.ESI and the 2nd pushed pointer
 *   in ArgMask are really interior pointers.  The pointer in ESI in a normal pointer:
 *
 *   iptr 11110000 00010011           => read Interior Ptr, Interior Ptr, Normal Ptr, Normal Ptr, Interior Ptr
 *   call 11010011 DDCCC011 RRRR=1011 => read EDI is a GC-pointer, ESI is a GC-pointer. EBP is a GC-pointer
 *                           MMM=0011 => read two GC-pointers arguments on the stack (nested call)
 *
 *   Since the call instruction mentions 5 GC-pointers we list them in the required order:
 *   EDI, ESI, EBP, 1st-pushed pointer, 2nd-pushed pointer
 *
 *   And we apply the Interior Pointer mask mmmm=10011 to the above five ordered GC-pointers
 *   we learn that EDI and ESI are interior GC-pointers and that the second push arg is an
 *   interior GC-pointer.
 */

#if defined(DACCESS_COMPILE)
        DWORD cbZeroBytes = 0;
#endif // DACCESS_COMPILE

        while (scanOffs <= curOffs)
        {
            unsigned callArgCnt;
            unsigned skip;
            unsigned newRegMask, inewRegMask;
            unsigned newArgMask, inewArgMask;
            unsigned oldScanOffs = scanOffs;

            if (iptrMask)
            {
                // We found this iptrMask in the previous iteration.
                // This iteration must be for a call. Set these variables
                // so that they are available at the end of the loop

                inewRegMask   = iptrMask & 0x0F; // EBP,EBX,ESI,EDI
                inewArgMask   = iptrMask >> 4;

                iptrMask      = 0;
            }
            else
            {
                // Zero out any stale values.

                inewRegMask = 0;
                inewArgMask = 0;
            }

            /* Get the next byte and decode it */

            unsigned val = *table++;
#if defined(DACCESS_COMPILE)
            // In this scenario, a 0 means that there is a push at the current offset.  For a struct with
            // two double fields, the JIT may use two movq instructions to push the struct onto the stack, and
            // the JIT will encode 4 pushes at the same code offset.  This means that we can have up to 4
            // consecutive bytes of 0 without changing the code offset.  Having more than 4 consecutive bytes
            // of zero indicates that there is most likely some sort of DAC error, and it may lead to problems
            // such as infinite loops.  So we bail out early instead.
            if (val == 0)
            {
                cbZeroBytes += 1;
                if (cbZeroBytes > 4)
                {
                    DacError(CORDBG_E_TARGET_INCONSISTENT);
                    UNREACHABLE();
                }
            }
            else
            {
                cbZeroBytes = 0;
            }
#endif // DACCESS_COMPILE

#ifdef _DEBUG
            if (scanOffs != curOffs)
                isCall = false;
#endif

            /* Check pushes, pops, and skips */

            if  (!(val & 0x80)) {

                //  iptrMask can immediately precede only calls

                _ASSERTE(inewRegMask == 0);
                _ASSERTE(inewArgMask == 0);

                if (!(val & 0x40)) {

                    unsigned pushCount;

                    if (!(val & 0x20))
                    {
                        //
                        // push    000DDDDD                 ESP push one item, 5-bit delta
                        //
                        pushCount   = 1;
                        scanOffs   += val & 0x1f;
                    }
                    else
                    {
                        //
                        // push    00100000 [pushCount]     ESP push multiple items
                        //
                        _ASSERTE(val == 0x20);
                        pushCount = fastDecodeUnsigned(table);
                    }

                    if (scanOffs > curOffs)
                    {
                        scanOffs = oldScanOffs;
                        goto FINISHED;
                    }

                    stackDepth +=  pushCount;
                }
                else if ((val & 0x3f) != 0) {
                    //
                    //  pop     01CCDDDD         pop CC items, 4-bit delta
                    //
                    scanOffs   +=  val & 0x0f;
                    if (scanOffs > curOffs)
                    {
                        scanOffs = oldScanOffs;
                        goto FINISHED;
                    }
                    stackDepth -= (val & 0x30) >> 4;

                } else if (scanOffs < curOffs) {
                    //
                    // skip    01000000 [Delta]  Skip arbitrary sized delta
                    //
                    skip = fastDecodeUnsigned(table);
                    scanOffs += skip;
                }
                else // don't process a skip if we are already at curOffs
                    goto FINISHED;

                /* reset regs and args state since we advance past last call site */

                 regMask    = 0;
                iregMask    = 0;
                 argMask    = 0;
                iargMask    = 0;
                argHnum     = 0;

            }
            else /* It must be a call, thisptr, or iptr */
            {
                switch ((val & 0x70) >> 4) {
                default:    // case 0-4, 1000xxxx through 1100xxxx
                    //
                    // call    1PPPPPPP          Call Pattern, P=[0..79]
                    //
                    decodeCallPattern((val & 0x7f), &callArgCnt,
                                      &newRegMask, &newArgMask, &skip);
                    // If we've already reached curOffs and the skip amount
                    // is non-zero then we are done
                    if ((scanOffs == curOffs) && (skip > 0))
                        goto FINISHED;
                    // otherwise process this call pattern
                    scanOffs   += skip;
                    if (scanOffs > curOffs)
                        goto FINISHED;
#ifdef _DEBUG
                    isCall      = true;
#endif
                    regMask     = newRegMask;
                    argMask     = newArgMask;   argTab = NULL;
                    iregMask    = inewRegMask;
                    iargMask    = inewArgMask;
                    stackDepth -= callArgCnt;
                    argHnum     = 2;             // argMask is known to be <= 3
                    break;

                  case 5:
                    //
                    // call    1101RRRR DDCCCMMM  Call RegMask=RRRR,ArgCnt=CCC,
                    //                        ArgMask=MMM Delta=commonDelta[DD]
                    //
                    newRegMask  = val & 0xf;    // EBP,EBX,ESI,EDI
                    val         = *table++;     // read next byte
                    skip        = callCommonDelta[val>>6];
                    // If we've already reached curOffs and the skip amount
                    // is non-zero then we are done
                    if ((scanOffs == curOffs) && (skip > 0))
                        goto FINISHED;
                    // otherwise process this call encoding
                    scanOffs   += skip;
                    if (scanOffs > curOffs)
                        goto FINISHED;
#ifdef _DEBUG
                    isCall      = true;
#endif
                    regMask     = newRegMask;
                    iregMask    = inewRegMask;
                    callArgCnt  = (val >> 3) & 0x7;
                    stackDepth -= callArgCnt;
                    argMask     = (val & 0x7);  argTab = NULL;
                    iargMask    = inewArgMask;
                    argHnum     = 3;
                    break;

                  case 6:
                    //
                    // call    1110RRRR [ArgCnt] [ArgMask]
                    //                          Call ArgCnt,RegMask=RRR,ArgMask
                    //
#ifdef _DEBUG
                    isCall      = true;
#endif
                    regMask     = val & 0xf;    // EBP,EBX,ESI,EDI
                    iregMask    = inewRegMask;
                    callArgCnt  = fastDecodeUnsigned(table);
                    stackDepth -= callArgCnt;
                    argMask     = fastDecodeUnsigned(table);  argTab = NULL;
                    iargMask    = inewArgMask;
                    argHnum     = sizeof(argMask) * 8;  // The size of argMask in bits
                    break;

                  case 7:
                    switch (val & 0x0C)
                    {
                      case 0x00:
                        //
                        // 0xF0   iptr     11110000   [IPtrMask] Arbitrary Interior Pointer Mask
                        //
                        iptrMask = fastDecodeUnsigned(table);
                        break;

                      case 0x04:
                        //
                        // 0xF4   thisptr  111101RR   This pointer is in Register RR
                        //                            00=EDI,01=ESI,10=EBX,11=EBP
                        //
                        {
                            static const regNum calleeSavedRegs[] =
                                { REGI_EDI, REGI_ESI, REGI_EBX, REGI_EBP };
                            thisPtrReg = calleeSavedRegs[val&0x3];
                        }
                        break;

                      case 0x08:
                        //
                        // 0xF8   call     11111000   [PBSDpbsd][32-bit delta][32-bit ArgCnt]
                        //                            [32-bit PndCnt][32-bit PndSize][PndOffs...]
                        //
                        val         = *table++;
                        skip        = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
// [VSUQFE 4670]
                        // If we've already reached curOffs and the skip amount
                        // is non-zero then we are done
                        if ((scanOffs == curOffs) && (skip > 0))
                            goto FINISHED;
// [VSUQFE 4670]
                        scanOffs   += skip;
                        if (scanOffs > curOffs)
                            goto FINISHED;
#ifdef _DEBUG
                        isCall      = true;
#endif
                        regMask     = val & 0xF;
                        iregMask    = val >> 4;
                        callArgCnt  = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                        stackDepth -= callArgCnt;
                        argHnum     = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                        argTabBytes = *dac_cast<PTR_DWORD>(table); table += sizeof(DWORD);
                        argTab      = table;
                        table      += argTabBytes;
                        break;

                      case 0x0C:
                        //
                        // 0xFF   end      11111111   End of table marker
                        //
                        _ASSERTE(val==0xff);
                        goto FINISHED;

                      default:
                        _ASSERTE(!"reserved GC encoding");
                        break;
                    }
                    break;

                } // end switch

            } // end else (!(val & 0x80))

            // iregMask & iargMask are subsets of regMask & argMask respectively

            _ASSERTE((iregMask & regMask) == iregMask);
            _ASSERTE((iargMask & argMask) == iargMask);

        } // end while

    } // end else ebp-less frame

FINISHED:

    // iregMask & iargMask are subsets of regMask & argMask respectively

    _ASSERTE((iregMask & regMask) == iregMask);
    _ASSERTE((iargMask & argMask) == iargMask);

    if (scanOffs != curOffs)
    {
        /* must have been a boring call */
        info->regMaskResult  = RM_NONE;
        info->argMaskResult  = ptrArgTP(0);
        info->iregMaskResult = RM_NONE;
        info->iargMaskResult = ptrArgTP(0);
        info->argHnumResult  = 0;
        info->argTabResult   = NULL;
        info->argTabBytes    = 0;
    }
    else
    {
        info->regMaskResult  = convertCalleeSavedRegsMask(regMask);
        info->argMaskResult  = ptrArgTP(argMask);
        info->argHnumResult  = argHnum;
        info->iregMaskResult = convertCalleeSavedRegsMask(iregMask);
        info->iargMaskResult = ptrArgTP(iargMask);
        info->argTabResult   = argTab;
        info->argTabBytes    = argTabBytes;
    }

#ifdef _DEBUG
    if (scanOffs != curOffs) {
        isCall = false;
    }
    _ASSERTE(thisPtrReg == REGI_NA || (!isCall || (regNumToMask(thisPtrReg) & info->regMaskResult)));
#endif
    info->thisPtrResult  = thisPtrReg;

    _ASSERTE(int(stackDepth) < INT_MAX); // check that it did not underflow
    return (stackDepth * sizeof(unsigned));
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


/*****************************************************************************
 * scan the register argument table for the fully interruptible case.
   this function is called to find all live objects (pushed arguments)
   and to get the stack base for fully interruptible methods.
   Returns size of things pushed on the stack for ESP frames

   Arguments:
      table       - The pointer table
      curOffsRegs - The current code offset that should be used for reporting registers
      curOffsArgs - The current code offset that should be used for reporting args
      info        - Incoming arg used to determine if there's a frame, and to save results
 */

static
unsigned scanArgRegTableI(PTR_CBYTE    table,
                          unsigned     curOffsRegs,
                          unsigned     curOffsArgs,
                          hdrInfo   *  info)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    regNum thisPtrReg = REGI_NA;
    unsigned  ptrRegs    = 0;    // The mask of registers that contain pointers
    unsigned iptrRegs    = 0;    // The subset of ptrRegs that are interior pointers
    unsigned  ptrOffs    = 0;    // The code offset of the table entry we are currently looking at
    unsigned  argCnt     = 0;    // The number of args that have been pushed

    ptrArgTP  ptrArgs(0);        // The mask of stack values that contain pointers.
    ptrArgTP iptrArgs(0);        // The subset of ptrArgs that are interior pointers.
    ptrArgTP  argHigh(0);        // The current mask position that corresponds to the top of the stack.

    bool      isThis     = false;
    bool      iptr       = false;

    // The comment before the call to scanArgRegTableI in EnumGCRefs
    // describes why curOffsRegs can be smaller than curOffsArgs.
    _ASSERTE(curOffsRegs <= curOffsArgs);

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBABE);
#endif

    bool      hasPartialArgInfo;

#ifndef UNIX_X86_ABI
    hasPartialArgInfo = info->ebpFrame;
#else
    // For x86/Linux, interruptible code always has full arg info
    //
    // This should be aligned with emitFullArgInfo setting at
    // emitter::emitEndCodeGen (in JIT)
    hasPartialArgInfo = false;
#endif

  /*
      Encoding table for methods that are fully interruptible

      The encoding used is as follows:

          ptr reg dead        00RRRDDD    [RRR != 100]
          ptr reg live        01RRRDDD    [RRR != 100]

      non-ptr arg push        10110DDD                    [SSS == 110]
          ptr arg push        10SSSDDD                    [SSS != 110] && [SSS != 111]
          ptr arg pop         11CCCDDD    [CCC != 000] && [CCC != 110] && [CCC != 111]
      little delta skip       11000DDD    [CCC == 000]
      bigger delta skip       11110BBB                    [CCC == 110]

      The values used in the encodings are as follows:

        DDD                 code offset delta from previous entry (0-7)
        BBB                 bigger delta 000=8,001=16,010=24,...,111=64
        RRR                 register number (EAX=000,ECX=001,EDX=010,EBX=011,
                              EBP=101,ESI=110,EDI=111), ESP=100 is reserved
        SSS                 argument offset from base of stack. This is
                              redundant for frameless methods as we can
                              infer it from the previous pushes+pops. However,
                              for EBP-methods, we only report GC pushes, and
                              so we need SSS
        CCC                 argument count being popped (includes only ptrs for EBP methods)

      The following are the 'large' versions:

        large delta skip        10111000 [0xB8] , encodeUnsigned(delta)

        large     ptr arg push  11111000 [0xF8] , encodeUnsigned(pushCount)
        large non-ptr arg push  11111001 [0xF9] , encodeUnsigned(pushCount)
        large     ptr arg pop   11111100 [0xFC] , encodeUnsigned(popCount)
        large         arg dead  11111101 [0xFD] , encodeUnsigned(popCount) for caller-pop args.
                                                    Any GC args go dead after the call,
                                                    but are still sitting on the stack

        this pointer prefix     10111100 [0xBC]   the next encoding is a ptr live
                                                    or a ptr arg push
                                                    and contains the this pointer

        interior or by-ref      10111111 [0xBF]   the next encoding is a ptr live
             pointer prefix                         or a ptr arg push
                                                    and contains an interior
                                                    or by-ref pointer


        The value 11111111 [0xFF] indicates the end of the table.
  */

#if defined(DACCESS_COMPILE)
    bool fLastByteIsZero = false;
#endif // DACCESS_COMPILE

    /* Have we reached the instruction we're looking for? */

    while (ptrOffs <= curOffsArgs)
    {
        unsigned    val;

        int         isPop;
        unsigned    argOfs;

        unsigned    regMask;

        // iptrRegs & iptrArgs are subsets of ptrRegs & ptrArgs respectively

        _ASSERTE((iptrRegs & ptrRegs) == iptrRegs);
        _ASSERTE((iptrArgs & ptrArgs) == iptrArgs);

        /* Now find the next 'life' transition */

        val = *table++;
#if defined(DACCESS_COMPILE)
        // In this scenario, a zero byte means that EAX is going dead at the current offset.  Since EAX
        // can't go dead more than once at any given offset, it's invalid to have two consecutive bytes
        // of zero.  If this were to happen, then it means that there is most likely some sort of DAC
        // error, and it may lead to problems such as infinite loops.  So we bail out early instead.
        if ((val == 0) && fLastByteIsZero)
        {
            DacError(CORDBG_E_TARGET_INCONSISTENT);
            UNREACHABLE();
        }
        fLastByteIsZero = (val == 0);
#endif // DACCESS_COMPILE

        if  (!(val & 0x80))
        {
            /* A small 'regPtr' encoding */

            regNum       reg;

            ptrOffs += (val     ) & 0x7;
            if (ptrOffs > curOffsArgs) {
                iptr = isThis = false;
                goto REPORT_REFS;
            }
            else if (ptrOffs > curOffsRegs) {
                iptr = isThis = false;
                continue;
            }

            reg     = (regNum)((val >> 3) & 0x7);
            regMask = 1 << reg;         // EAX,ECX,EDX,EBX,---,EBP,ESI,EDI

#if 0
            printf("regMask = %04X -> %04X\n", ptrRegs,
                       (val & 0x40) ? (ptrRegs |  regMask)
                                    : (ptrRegs & ~regMask));
#endif

            /* The register is becoming live/dead here */

            if  (val & 0x40)
            {
                /* Becomes Live */
                _ASSERTE((ptrRegs  &  regMask) == 0);

                ptrRegs |=  regMask;

                if  (isThis)
                {
                    thisPtrReg = reg;
                }
                if  (iptr)
                {
                    iptrRegs |= regMask;
                }
            }
            else
            {
                /* Becomes Dead */
                _ASSERTE((ptrRegs  &  regMask) != 0);

                ptrRegs &= ~regMask;

                if  (reg == thisPtrReg)
                {
                    thisPtrReg = REGI_NA;
                }
                if  (iptrRegs & regMask)
                {
                    iptrRegs &= ~regMask;
                }
            }
            iptr = isThis = false;
            continue;
        }

        /* This is probably an argument push/pop */

        argOfs = (val & 0x38) >> 3;

        /* 6 [110] and 7 [111] are reserved for other encodings */
        if  (argOfs < 6)
        {

            /* A small argument encoding */

            ptrOffs += (val & 0x07);
            if (ptrOffs > curOffsArgs) {
                iptr = isThis = false;
                goto REPORT_REFS;
            }
            isPop    = (val & 0x40);

        ARG:

            if  (isPop)
            {
                if (argOfs == 0)
                    continue;           // little skip encoding

                /* We remove (pop) the top 'argOfs' entries */

                _ASSERTE(argOfs || argOfs <= argCnt);

                /* adjust # of arguments */

                argCnt -= argOfs;
                _ASSERTE(argCnt < MAX_PTRARG_OFS);

//              printf("[%04X] popping %u args: mask = %04X\n", ptrOffs, argOfs, (int)ptrArgs);

                do
                {
                    _ASSERTE(!isZero(argHigh));

                    /* Do we have an argument bit that's on? */

                    if  (intersect(ptrArgs, argHigh))
                    {
                        /* Turn off the bit */

                        setDiff(ptrArgs, argHigh);
                        setDiff(iptrArgs, argHigh);

                        /* We've removed one more argument bit */

                        argOfs--;
                    }
                    else if (hasPartialArgInfo)
                        argCnt--;
                    else /* full arg info && not a ref */
                        argOfs--;

                    /* Continue with the next lower bit */

                    argHigh >>= 1;
                }
                while (argOfs);

                _ASSERTE(!hasPartialArgInfo    ||
                         isZero(argHigh)       ||
                        (argHigh == CONSTRUCT_ptrArgTP(1, (argCnt-1))));

                if (hasPartialArgInfo)
                {
                    // We always leave argHigh pointing to the next ptr arg.
                    // So, while argHigh is non-zero, and not a ptrArg, we shift right (and subtract
                    // one arg from our argCnt) until it is a ptrArg.
                    while (!intersect(argHigh, ptrArgs) && (!isZero(argHigh)))
                    {
                        argHigh >>= 1;
                        argCnt--;
                    }
                }

            }
            else
            {
                /* Add a new ptr arg entry at stack offset 'argOfs' */

                if  (argOfs >= MAX_PTRARG_OFS)
                {
                    _ASSERTE_ALL_BUILDS(!"scanArgRegTableI: args pushed 'too deep'");
                }
                else
                {
                    /* Full arg info reports all pushes, and thus
                       argOffs has to be consistent with argCnt */

                    _ASSERTE(hasPartialArgInfo || argCnt == argOfs);

                    /* store arg count */

                    argCnt  = argOfs + 1;
                    _ASSERTE((argCnt < MAX_PTRARG_OFS));

                    /* Compute the appropriate argument offset bit */

                    ptrArgTP argMask = CONSTRUCT_ptrArgTP(1, argOfs);

//                  printf("push arg at offset %02u --> mask = %04X\n", argOfs, (int)argMask);

                    /* We should never push twice at the same offset */

                    _ASSERTE(!intersect( ptrArgs, argMask));
                    _ASSERTE(!intersect(iptrArgs, argMask));

                    /* We should never push within the current highest offset */

                    // _ASSERTE(argHigh < argMask);

                    /* This is now the highest bit we've set */

                    argHigh = argMask;

                    /* Set the appropriate bit in the argument mask */

                    ptrArgs |= argMask;

                    if (iptr)
                        iptrArgs |= argMask;
                }

                iptr = isThis = false;
            }
            continue;
        }
        else if (argOfs == 6)
        {
            if (val & 0x40) {
                /* Bigger delta  000=8,001=16,010=24,...,111=64 */
                ptrOffs += (((val & 0x07) + 1) << 3);
            }
            else {
                /* non-ptr arg push */
                _ASSERTE(!hasPartialArgInfo);
                ptrOffs += (val & 0x07);
                if (ptrOffs > curOffsArgs) {
                    iptr = isThis = false;
                    goto REPORT_REFS;
                }
                argHigh = CONSTRUCT_ptrArgTP(1, argCnt);
                argCnt++;
                _ASSERTE(argCnt < MAX_PTRARG_OFS);
            }
            continue;
        }

        /* argOfs was 7 [111] which is reserved for the larger encodings */

        _ASSERTE(argOfs==7);

        switch (val)
        {
        case 0xFF:
            iptr = isThis = false;
            goto REPORT_REFS;   // the method might loop !!!

        case 0xB8:
            val = fastDecodeUnsigned(table);
            ptrOffs += val;
            continue;

        case 0xBC:
            isThis = true;
            break;

        case 0xBF:
            iptr = true;
            break;

        case 0xF8:
        case 0xFC:
            isPop  = val & 0x04;
            argOfs = fastDecodeUnsigned(table);
            goto ARG;

        case 0xFD: {
            argOfs  = fastDecodeUnsigned(table);
            _ASSERTE(argOfs && argOfs <= argCnt);

            // Kill the top "argOfs" pointers.

            ptrArgTP argMask;
            for(argMask = CONSTRUCT_ptrArgTP(1, argCnt); (argOfs != 0); argMask >>= 1)
            {
                _ASSERTE(!isZero(argMask) && !isZero(ptrArgs)); // there should be remaining pointers

                if (intersect(ptrArgs, argMask))
                {
                    setDiff(ptrArgs, argMask);
                    setDiff(iptrArgs, argMask);
                    argOfs--;
                }
            }

            // For partial arg info, need to find the next highest pointer for argHigh

            if (hasPartialArgInfo)
            {
                for(argHigh = ptrArgTP(0); !isZero(argMask); argMask >>= 1)
                {
                    if (intersect(ptrArgs, argMask)) {
                        argHigh = argMask;
                        break;
                    }
                }
            }
            } break;

        case 0xF9:
            argOfs = fastDecodeUnsigned(table);
            argCnt  += argOfs;
            break;

        default:
            _ASSERTE(!"Unexpected special code %04X");
        }
    }

    /* Report all live pointer registers */
REPORT_REFS:

    _ASSERTE((iptrRegs & ptrRegs) == iptrRegs); // iptrRegs is a subset of ptrRegs
    _ASSERTE((iptrArgs & ptrArgs) == iptrArgs); // iptrArgs is a subset of ptrArgs

    /* Save the current live register, argument set, and argCnt */

    info->regMaskResult  = convertAllRegsMask(ptrRegs);
    info->argMaskResult  = ptrArgs;
    info->argHnumResult  = 0;
    info->iregMaskResult = convertAllRegsMask(iptrRegs);
    info->iargMaskResult = iptrArgs;

    info->thisPtrResult  = thisPtrReg;
    _ASSERTE(thisPtrReg == REGI_NA || (regNumToMask(thisPtrReg) & info->regMaskResult));

    if (hasPartialArgInfo)
    {
        return 0;
    }
    else
    {
        _ASSERTE(int(argCnt) < INT_MAX); // check that it did not underflow
        return (argCnt * sizeof(unsigned));
    }
}

/*****************************************************************************/

unsigned GetPushedArgSize(hdrInfo * info, PTR_CBYTE table, DWORD curOffs)
{
    SUPPORTS_DAC;

    unsigned sz;

    if  (info->interruptible)
    {
        sz = scanArgRegTableI(skipToArgReg(*info, table),
                              curOffs,
                              curOffs,
                              info);
    }
    else
    {
        sz = scanArgRegTable(skipToArgReg(*info, table),
                             curOffs,
                             info);
    }

    return sz;
}

/*****************************************************************************/

inline
void    TRASH_CALLEE_UNSAVED_REGS(PREGDISPLAY pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef _DEBUG
    /* This is not completely correct as we lose the current value, but
       it should not really be useful to anyone. */
    static DWORD s_badData = 0xDEADBEEF;
    pContext->SetEaxLocation(&s_badData);
    pContext->SetEcxLocation(&s_badData);
    pContext->SetEdxLocation(&s_badData);
#endif //_DEBUG
}

/*****************************************************************************
 *  Sizes of certain i386 instructions which are used in the prolog/epilog
 */

// Can we use sign-extended byte to encode the imm value, or do we need a dword
#define CAN_COMPRESS(val)       ((INT8)(val) == (INT32)(val))

#define SZ_ADD_REG(val)         ( 2 +  (CAN_COMPRESS(val) ? 1 : 4))
#define SZ_AND_REG(val)         SZ_ADD_REG(val)
#define SZ_POP_REG              1
#define SZ_LEA(offset)          SZ_ADD_REG(offset)
#define SZ_MOV_REG_REG          2

bool IsMarkerInstr(BYTE val)
{
    SUPPORTS_DAC;

#ifdef _DEBUG
    if (val == X86_INSTR_INT3)
    {
        return true;
    }
#ifdef HAVE_GCCOVER
    else // GcCover might have stomped on the instruction
    {
        if (GCStress<cfg_any>::IsEnabled())
        {
            if (IsGcCoverageInterruptInstructionVal(val))
            {
                return true;
            }
        }
    }
#endif // HAVE_GCCOVER
#endif // _DEBUG

    return false;
}

/* Check if the given instruction opcode is the one we expect.
   This is a "necessary" but not "sufficient" check as it ignores the check
   if the instruction is one of our special markers (for debugging and GcStress) */

bool CheckInstrByte(BYTE val, BYTE expectedValue)
{
    SUPPORTS_DAC;
    return ((val == expectedValue) || IsMarkerInstr(val));
}

/* Similar to CheckInstrByte(). Use this to check a masked opcode (ignoring
   optional bits in the opcode encoding).
   valPattern is the masked out value.
   expectedPattern is the mask value we expect.
   val is the actual instruction opcode
 */
bool CheckInstrBytePattern(BYTE valPattern, BYTE expectedPattern, BYTE val)
{
    SUPPORTS_DAC;

    _ASSERTE((valPattern & val) == valPattern);

    return ((valPattern == expectedPattern) || IsMarkerInstr(val));
}

/* Similar to CheckInstrByte() */

bool CheckInstrWord(WORD val, WORD expectedValue)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return ((val == expectedValue) || IsMarkerInstr(val & 0xFF));
}

// Use this to check if the instruction at offset "walkOffset" has already
// been executed
// "actualHaltOffset" is the offset when the code was suspended
// It is assumed that there is linear control flow from offset 0 to "actualHaltOffset".
//
// This has been factored out just so that the intent of the comparison
// is clear (compared to the opposite intent)

bool InstructionAlreadyExecuted(unsigned walkOffset, unsigned actualHaltOffset)
{
    SUPPORTS_DAC;
    return (walkOffset < actualHaltOffset);
}

// skips past a "arith REG, IMM"
inline unsigned SKIP_ARITH_REG(int val, PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

    unsigned delta = 0;
    if (val != 0)
    {
#ifdef _DEBUG
        // Confirm that arith instruction is at the correct place
        _ASSERTE(CheckInstrBytePattern(base[offset  ] & 0xFD, 0x81, base[offset]) &&
                 CheckInstrBytePattern(base[offset+1] & 0xC0, 0xC0, base[offset+1]));
        // only use DWORD form if needed
        _ASSERTE(((base[offset] & 2) != 0) == CAN_COMPRESS(val) ||
                 IsMarkerInstr(base[offset]));
#endif
        delta = 2 + (CAN_COMPRESS(val) ? 1 : 4);
    }
    return(offset + delta);
}

inline unsigned SKIP_PUSH_REG(PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Confirm it is a push instruction
    _ASSERTE(CheckInstrBytePattern(base[offset] & 0xF8, 0x50, base[offset]));
    return(offset + 1);
}

inline unsigned SKIP_POP_REG(PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Confirm it is a pop instruction
    _ASSERTE(CheckInstrBytePattern(base[offset] & 0xF8, 0x58, base[offset]));
    return(offset + 1);
}

inline unsigned SKIP_MOV_REG_REG(PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Confirm it is a move instruction
    // Note that only the first byte may have been stomped on by IsMarkerInstr()
    // So we can check the second byte directly
    _ASSERTE(CheckInstrBytePattern(base[offset] & 0xFD, 0x89, base[offset]) &&
             (base[offset+1] & 0xC0) == 0xC0);
    return(offset + 2);
}

inline unsigned SKIP_LEA_ESP_EBP(int val, PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef _DEBUG
    // Confirm it is the right instruction
    // Note that only the first byte may have been stomped on by IsMarkerInstr()
    // So we can check the second byte directly
    WORD wOpcode = *(PTR_WORD)base;
    _ASSERTE((CheckInstrWord(wOpcode, X86_INSTR_w_LEA_ESP_EBP_BYTE_OFFSET) &&
              (val == *(PTR_SBYTE)(base+2)) &&
              CAN_COMPRESS(val)) ||
             (CheckInstrWord(wOpcode, X86_INSTR_w_LEA_ESP_EBP_DWORD_OFFSET) &&
              (val == *(PTR_INT32)(base+2)) &&
              !CAN_COMPRESS(val)));
#endif

    unsigned delta = 2 + (CAN_COMPRESS(val) ? 1 : 4);
    return(offset + delta);
}

inline unsigned SKIP_LEA_EAX_ESP(int val, PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef _DEBUG
    WORD wOpcode = *(PTR_WORD)(base + offset);
    if (CheckInstrWord(wOpcode, X86_INSTR_w_LEA_EAX_ESP_BYTE_OFFSET))
    {
        _ASSERTE(val == *(PTR_SBYTE)(base + offset + 3));
        _ASSERTE(CAN_COMPRESS(val));
    }
    else
    {
        _ASSERTE(CheckInstrWord(wOpcode, X86_INSTR_w_LEA_EAX_ESP_DWORD_OFFSET));
        _ASSERTE(val == *(PTR_INT32)(base + offset + 3));
        _ASSERTE(!CAN_COMPRESS(val));
    }
#endif

    unsigned delta = 3 + (CAN_COMPRESS(-val) ? 1 : 4);
    return(offset + delta);
}

inline unsigned SKIP_HELPER_CALL(PTR_CBYTE base, unsigned offset)
{
    LIMITED_METHOD_DAC_CONTRACT;

    unsigned delta;

    if (CheckInstrByte(base[offset], X86_INSTR_CALL_REL32))
    {
        delta = 5;
    }
    else
    {
#ifdef _DEBUG
        WORD wOpcode = *(PTR_WORD)(base+offset);
        _ASSERTE(CheckInstrWord(wOpcode, X86_INSTR_W_CALL_IND_IMM));
#endif
        delta = 6;
    }

    return(offset+delta);
}

unsigned SKIP_ALLOC_FRAME(int size, PTR_CBYTE base, unsigned offset)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    _ASSERTE(size != 0);

    if (size == sizeof(void*))
    {
        // JIT emits "push eax" instead of "sub esp,4"
        return SKIP_PUSH_REG(base, offset);
    }

    const int STACK_PROBE_PAGE_SIZE_BYTES = 4096;
    const int STACK_PROBE_BOUNDARY_THRESHOLD_BYTES = 1024;

    int lastProbedLocToFinalSp = size;

    if (size < STACK_PROBE_PAGE_SIZE_BYTES)
    {
        // sub esp, size
        offset = SKIP_ARITH_REG(size, base, offset);
    }
    else
    {
        WORD wOpcode = *(PTR_WORD)(base + offset);

        if (CheckInstrWord(wOpcode, X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX))
        {
            // In .NET 5.0 and earlier for frames that have size smaller than 0x3000 bytes
            // JIT emits one or two 'test eax, [esp-dwOffset]' instructions before adjusting the stack pointer.
            _ASSERTE(size < 0x3000);

            // test eax, [esp-0x1000]
            offset += 7;
            lastProbedLocToFinalSp -= 0x1000;

            if (size >= 0x2000)
            {
#ifdef _DEBUG
                wOpcode = *(PTR_WORD)(base + offset);
                _ASSERTE(CheckInstrWord(wOpcode, X86_INSTR_w_TEST_ESP_DWORD_OFFSET_EAX));
#endif
                //test eax, [esp-0x2000]
                offset += 7;
                lastProbedLocToFinalSp -= 0x1000;
            }

            // sub esp, size
            offset = SKIP_ARITH_REG(size, base, offset);
        }
        else
        {
            bool pushedStubParam = false;

            if (CheckInstrByte(base[offset], X86_INSTR_PUSH_EAX))
            {
                // push eax
                offset = SKIP_PUSH_REG(base, offset);
                pushedStubParam = true;
            }

            if (CheckInstrByte(base[offset], X86_INSTR_XOR))
            {
                // In .NET Core 3.1 and earlier for frames that have size greater than or equal to 0x3000 bytes
                // JIT emits the following loop.
                _ASSERTE(size >= 0x3000);

                offset += 2;
                //      xor eax, eax                2
                //      [nop]                       0-3
                // loop:
                //      test [esp + eax], eax       3
                //      sub eax, 0x1000             5
                //      cmp eax, -size              5
                //      jge loop                    2

                // R2R images that support ReJIT may have extra nops we need to skip over.
                while (offset < 5)
                {
                    if (CheckInstrByte(base[offset], X86_INSTR_NOP))
                    {
                        offset++;
                    }
                    else
                    {
                        break;
                    }
                }

                offset += 15;

                if (pushedStubParam)
                {
                    // pop eax
                    offset = SKIP_POP_REG(base, offset);
                }

                // sub esp, size
                return SKIP_ARITH_REG(size, base, offset);
            }
            else
            {
                // In .NET 5.0 and later JIT emits a call to JIT_StackProbe helper.

                if (pushedStubParam)
                {
                    // lea eax, [esp-size+4]
                    offset = SKIP_LEA_EAX_ESP(-size + 4, base, offset);
                    // call JIT_StackProbe
                    offset = SKIP_HELPER_CALL(base, offset);
                    // pop eax
                    offset = SKIP_POP_REG(base, offset);
                    // sub esp, size
                    return SKIP_ARITH_REG(size, base, offset);
                }
                else
                {
                    // lea eax, [esp-size]
                    offset = SKIP_LEA_EAX_ESP(-size, base, offset);
                    // call JIT_StackProbe
                    offset = SKIP_HELPER_CALL(base, offset);
                    // mov esp, eax
                    return SKIP_MOV_REG_REG(base, offset);
                }
            }
        }
    }

    if (lastProbedLocToFinalSp + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > STACK_PROBE_PAGE_SIZE_BYTES)
    {
#ifdef _DEBUG
        WORD wOpcode = *(PTR_WORD)(base + offset);
        _ASSERTE(CheckInstrWord(wOpcode, X86_INSTR_w_TEST_ESP_EAX));
#endif
        // test [esp], eax
        offset += 3;
    }

    return offset;
}

/*****************************************************************************/

const RegMask CALLEE_SAVED_REGISTERS_MASK[] =
{
    RM_EDI, // first register to be pushed
    RM_ESI,
    RM_EBX,
    RM_EBP  // last register to be pushed
};

static void SetLocation(PREGDISPLAY pRD, int ind, PDWORD loc)
{
#if defined(FEATURE_NATIVEAOT)
    static const SIZE_T OFFSET_OF_CALLEE_SAVED_REGISTERS[] =
    {
        offsetof(REGDISPLAY, pRdi), // first register to be pushed
        offsetof(REGDISPLAY, pRsi),
        offsetof(REGDISPLAY, pRbx),
        offsetof(REGDISPLAY, pRbp), // last register to be pushed
    };

    SIZE_T offsetOfRegPtr = OFFSET_OF_CALLEE_SAVED_REGISTERS[ind];
    *(LPVOID*)(PBYTE(pRD) + offsetOfRegPtr) = loc;
#elif defined(FEATURE_EH_FUNCLETS)
    static const SIZE_T OFFSET_OF_CALLEE_SAVED_REGISTERS[] =
    {
        offsetof(T_KNONVOLATILE_CONTEXT_POINTERS, Edi), // first register to be pushed
        offsetof(T_KNONVOLATILE_CONTEXT_POINTERS, Esi),
        offsetof(T_KNONVOLATILE_CONTEXT_POINTERS, Ebx),
        offsetof(T_KNONVOLATILE_CONTEXT_POINTERS, Ebp), // last register to be pushed
    };

    SIZE_T offsetOfRegPtr = OFFSET_OF_CALLEE_SAVED_REGISTERS[ind];
    *(LPVOID*)(PBYTE(pRD->pCurrentContextPointers) + offsetOfRegPtr) = loc;
#else
    static const SIZE_T OFFSET_OF_CALLEE_SAVED_REGISTERS[] =
    {
        offsetof(REGDISPLAY, pEdi), // first register to be pushed
        offsetof(REGDISPLAY, pEsi),
        offsetof(REGDISPLAY, pEbx),
        offsetof(REGDISPLAY, pEbp), // last register to be pushed
    };

    SIZE_T offsetOfRegPtr = OFFSET_OF_CALLEE_SAVED_REGISTERS[ind];
    *(LPVOID*)(PBYTE(pRD) + offsetOfRegPtr) = loc;
#endif
}

/*****************************************************************************/

void UnwindEspFrameEpilog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE epilogBase,
        bool updateAllRegs)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(info->epilogOffs != hdrInfo::NOT_IN_EPILOG);
    _ASSERTE(!info->ebpFrame && !info->doubleAlign);
    _ASSERTE(info->epilogOffs > 0);

    int offset = 0;
    unsigned ESP = pContext->SP;

    if (info->rawStkSize)
    {
        if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
        {
            /* We have NOT executed the "ADD ESP, FrameSize",
               so manually adjust stack pointer */
            ESP += info->rawStkSize;
        }

        // We have already popped off the frame (excluding the callee-saved registers)

        if (epilogBase[0] == X86_INSTR_POP_ECX)
        {
            // We may use "POP ecx" for doing "ADD ESP, 4",
            // or we may not (in the case of JMP epilogs)
            _ASSERTE(info->rawStkSize == sizeof(void*));
            offset = SKIP_POP_REG(epilogBase, offset);
        }
        else
        {
            // "add esp, rawStkSize"
            offset = SKIP_ARITH_REG(info->rawStkSize, epilogBase, offset);
        }
    }

    /* Remaining callee-saved regs are at ESP. Need to update
       regsMask as well to exclude registers which have already been popped. */

    const RegMask regsMask = info->savedRegMask;

    /* Increment "offset" in steps to see which callee-saved
       registers have already been popped */

    for (unsigned i = ARRAY_SIZE(CALLEE_SAVED_REGISTERS_MASK); i > 0; i--)
    {
        RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i - 1];

        if (!(regMask & regsMask))
            continue;

        if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
        {
            /* We have NOT yet popped off the register.
               Get the value from the stack if needed */
            if (updateAllRegs || (regMask == RM_EBP))
            {
                SetLocation(pContext, i - 1, PTR_DWORD((TADDR)ESP));
            }

            /* Adjust ESP */
            ESP += sizeof(void*);
        }

        offset = SKIP_POP_REG(epilogBase, offset);
    }

    //CEE_JMP generates an epilog similar to a normal CEE_RET epilog except for the last instruction
    _ASSERTE(CheckInstrBytePattern(epilogBase[offset] & X86_INSTR_RET, X86_INSTR_RET, epilogBase[offset]) //ret
        || CheckInstrBytePattern(epilogBase[offset], X86_INSTR_JMP_NEAR_REL32, epilogBase[offset]) //jmp ret32
        || CheckInstrWord(*PTR_WORD(epilogBase + offset), X86_INSTR_w_JMP_FAR_IND_IMM)); //jmp [addr32]

    /* Finally we can set pPC */
    SetRegdisplayPCTAddr(pContext, (TADDR)ESP);

    pContext->SP = ESP;
}

/*****************************************************************************/

void UnwindEbpDoubleAlignFrameEpilog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE epilogBase,
        bool updateAllRegs)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(info->epilogOffs != hdrInfo::NOT_IN_EPILOG);
    _ASSERTE(info->ebpFrame || info->doubleAlign);

    _ASSERTE(info->argSize < 0x10000); // "ret" only has a 2 byte operand

   /* See how many instructions we have executed in the
      epilog to determine which callee-saved registers
      have already been popped */
    int offset = 0;

    unsigned ESP = pContext->SP;

    bool needMovEspEbp = false;

    if (info->doubleAlign)
    {
        // add esp, rawStkSize

        if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
            ESP += info->rawStkSize;
        _ASSERTE(info->rawStkSize != 0);
        offset = SKIP_ARITH_REG(info->rawStkSize, epilogBase, offset);

        // We also need "mov esp, ebp" after popping the callee-saved registers
        needMovEspEbp = true;
    }
    else
    {
        bool needLea = false;

        if (info->localloc)
        {
            // ESP may be variable if a localloc was actually executed. We will reset it.
            //    lea esp, [ebp-calleeSavedRegs]

            needLea = true;
        }
        else if (info->savedRegsCountExclFP == 0)
        {
            // We will just generate "mov esp, ebp" and be done with it.

            if (info->rawStkSize != 0)
            {
                needMovEspEbp = true;
            }
        }
        else if  (info->rawStkSize == 0)
        {
            // do nothing before popping the callee-saved registers
        }
        else if (info->rawStkSize == sizeof(void*))
        {
            // "pop ecx" will make ESP point to the callee-saved registers
            if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
                ESP += sizeof(void*);
            offset = SKIP_POP_REG(epilogBase, offset);
        }
        else
        {
            // We need to make ESP point to the callee-saved registers
            //    lea esp, [ebp-calleeSavedRegs]

            needLea = true;
        }

        if (needLea)
        {
            // lea esp, [ebp-calleeSavedRegs]

            unsigned calleeSavedRegsSize = info->savedRegsCountExclFP * sizeof(void*);

            if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
                ESP = GetRegdisplayFP(pContext) - calleeSavedRegsSize;

            offset = SKIP_LEA_ESP_EBP(-int(calleeSavedRegsSize), epilogBase, offset);
        }
    }

    for (unsigned i = STRING_LENGTH(CALLEE_SAVED_REGISTERS_MASK); i > 0; i--)
    {
        RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i - 1];
        _ASSERTE(regMask != RM_EBP);

        if ((info->savedRegMask & regMask) == 0)
            continue;

        if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
        {
            if (updateAllRegs)
            {
                SetLocation(pContext, i - 1, PTR_DWORD((TADDR)ESP));
            }
            ESP += sizeof(void*);
        }

        offset = SKIP_POP_REG(epilogBase, offset);
    }

    if (needMovEspEbp)
    {
        if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
            ESP = GetRegdisplayFP(pContext);

        offset = SKIP_MOV_REG_REG(epilogBase, offset);
    }

    // Have we executed the pop EBP?
    if (!InstructionAlreadyExecuted(offset, info->epilogOffs))
    {
        pContext->SetEbpLocation(PTR_DWORD(TADDR(ESP)));
        ESP += sizeof(void*);
    }
    offset = SKIP_POP_REG(epilogBase, offset);

    SetRegdisplayPCTAddr(pContext, (TADDR)ESP);

    pContext->SP = ESP;
}

inline SIZE_T GetStackParameterSize(hdrInfo * info)
{
    SUPPORTS_DAC;
    return (info->varargs ? 0 : info->argSize); // Note varargs is caller-popped
}

//****************************************************************************
// This is the value ESP is incremented by on doing a "return"

inline SIZE_T ESPIncrOnReturn(hdrInfo * info)
{
    SUPPORTS_DAC;
    return sizeof(void *) + // pop off the return address
           GetStackParameterSize(info);
}

/*****************************************************************************/

void UnwindEpilog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE epilogBase,
        bool updateAllRegs)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(info->epilogOffs != hdrInfo::NOT_IN_EPILOG);
    // _ASSERTE(flags & ActiveStackFrame); // <TODO> Wont work for thread death</TODO>
    _ASSERTE(info->epilogOffs > 0);

    if  (info->ebpFrame || info->doubleAlign)
    {
        UnwindEbpDoubleAlignFrameEpilog(pContext, info, epilogBase, updateAllRegs);
    }
    else
    {
        UnwindEspFrameEpilog(pContext, info, epilogBase, updateAllRegs);
    }

#ifdef _DEBUG
    if (updateAllRegs)
        TRASH_CALLEE_UNSAVED_REGS(pContext);
#endif

    /* Now adjust stack pointer */

    pContext->SP += ESPIncrOnReturn(info);
}

/*****************************************************************************/

void UnwindEspFrameProlog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE methodStart,
        bool updateAllRegs)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    /* we are in the middle of the prolog */
    _ASSERTE(info->prologOffs != hdrInfo::NOT_IN_PROLOG);
    _ASSERTE(!info->ebpFrame && !info->doubleAlign);

    unsigned offset = 0;

#ifdef _DEBUG
    // If the first two instructions are 'nop, int3', then  we will
    // assume that is from a JitHalt operation and skip past it
    if (methodStart[0] == X86_INSTR_NOP && methodStart[1] == X86_INSTR_INT3)
    {
        offset += 2;
    }
#endif

    const DWORD curOffs = info->prologOffs;
    unsigned ESP = pContext->SP;

    // Find out how many callee-saved regs have already been pushed

    unsigned regsMask = RM_NONE;
    PTR_DWORD savedRegPtr = PTR_DWORD((TADDR)ESP);

    for (unsigned i = 0; i < ARRAY_SIZE(CALLEE_SAVED_REGISTERS_MASK); i++)
    {
        RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i];

        if (!(info->savedRegMask & regMask))
            continue;

        if (InstructionAlreadyExecuted(offset, curOffs))
        {
            ESP += sizeof(void*);
            regsMask    |= regMask;
        }

        offset = SKIP_PUSH_REG(methodStart, offset);
    }

    if (info->rawStkSize)
    {
        offset = SKIP_ALLOC_FRAME(info->rawStkSize, methodStart, offset);

        // Note that this assumes that only the last instruction in SKIP_ALLOC_FRAME
        // actually updates ESP
        if (InstructionAlreadyExecuted(offset, curOffs + 1))
        {
            savedRegPtr += (info->rawStkSize / sizeof(DWORD));
            ESP += info->rawStkSize;
        }
    }

    //
    // Stack probe checks here
    //

    // Poison the value, we don't set it properly at the end of the prolog
#ifdef _DEBUG
    offset = 0xCCCCCCCC;
#endif

    // Always restore EBP
    if (regsMask & RM_EBP)
        pContext->SetEbpLocation(savedRegPtr++);

    if (updateAllRegs)
    {
        if (regsMask & RM_EBX)
            pContext->SetEbxLocation(savedRegPtr++);
        if (regsMask & RM_ESI)
            pContext->SetEsiLocation(savedRegPtr++);
        if (regsMask & RM_EDI)
            pContext->SetEdiLocation(savedRegPtr++);

        TRASH_CALLEE_UNSAVED_REGS(pContext);
    }

#if 0
// NOTE:
// THIS IS ONLY TRUE IF PROLOGSIZE DOES NOT INCLUDE REG-VAR INITIALIZATION !!!!
//
    /* there is (potentially) only one additional
       instruction in the prolog, (push ebp)
       but if we would have been passed that instruction,
       info->prologOffs would be hdrInfo::NOT_IN_PROLOG!
    */
    _ASSERTE(offset == info->prologOffs);
#endif

    pContext->SP = ESP;
}

/*****************************************************************************/

void UnwindEspFrame(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE table,
        PTR_CBYTE methodStart,
        DWORD curOffs,
        unsigned flags)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(!info->ebpFrame && !info->doubleAlign);
    _ASSERTE(info->epilogOffs == hdrInfo::NOT_IN_EPILOG);

    unsigned ESP = pContext->SP;


    if (info->prologOffs != hdrInfo::NOT_IN_PROLOG)
    {
        if (info->prologOffs != 0) // Do nothing for the very start of the method
        {
            UnwindEspFrameProlog(pContext, info, methodStart, flags);
            ESP = pContext->SP;
        }
    }
    else
    {
        /* we are past the prolog, ESP has been set above */

        // Are there any arguments pushed on the stack?

        ESP += GetPushedArgSize(info, table, curOffs);

        ESP += info->rawStkSize;

        const RegMask regsMask = info->savedRegMask;

        for (unsigned i = ARRAY_SIZE(CALLEE_SAVED_REGISTERS_MASK); i > 0; i--)
        {
            RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i - 1];

            if ((regMask & regsMask) == 0)
                continue;

            SetLocation(pContext, i - 1, PTR_DWORD((TADDR)ESP));

            ESP += sizeof(unsigned);
        }
    }

    /* we can now set the (address of the) return address */

    SetRegdisplayPCTAddr(pContext, (TADDR)ESP);

    /* Now adjust stack pointer */

    pContext->SP = ESP + ESPIncrOnReturn(info);
}


/*****************************************************************************/

void UnwindEbpDoubleAlignFrameProlog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE methodStart,
        bool updateAllRegs)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(info->prologOffs != hdrInfo::NOT_IN_PROLOG);
    _ASSERTE(info->ebpFrame || info->doubleAlign);

    DWORD offset = 0;

#ifdef _DEBUG
    // If the first two instructions are 'nop, int3', then  we will
    // assume that is from a JitHalt operation and skip past it
    if (methodStart[0] == X86_INSTR_NOP && methodStart[1] == X86_INSTR_INT3)
    {
        offset += 2;
    }
#endif

    /* Check for the case where EBP has not been updated yet. */

    const DWORD curOffs = info->prologOffs;

    // If we have still not excecuted "push ebp; mov ebp, esp", then we need to
    // report the frame relative to ESP

    if (!InstructionAlreadyExecuted(offset + 1, curOffs))
    {
        _ASSERTE(CheckInstrByte(methodStart [offset], X86_INSTR_PUSH_EBP) ||
                 CheckInstrWord(*PTR_WORD(methodStart + offset), X86_INSTR_W_MOV_EBP_ESP) ||
                 CheckInstrByte(methodStart [offset], X86_INSTR_JMP_NEAR_REL32));   // a rejit jmp-stamp

        /* If we're past the "push ebp", adjust ESP to pop EBP off */

        if  (curOffs == (offset + 1))
            pContext->SP += sizeof(TADDR);

        /* Stack pointer points to return address */

        SetRegdisplayPCTAddr(pContext, (TADDR)pContext->SP);

        /* EBP and callee-saved registers still have the correct value */

        return;
    }

    // We are atleast after the "push ebp; mov ebp, esp"

    offset = SKIP_MOV_REG_REG(methodStart,
                SKIP_PUSH_REG(methodStart, offset));

    /* At this point, EBP has been set up. The caller's ESP and the return value
       can be determined using EBP. Since we are still in the prolog,
       we need to know our exact location to determine the callee-saved registers */

    const unsigned curEBP = GetRegdisplayFP(pContext);

    if (updateAllRegs)
    {
        PTR_DWORD pSavedRegs = PTR_DWORD((TADDR)curEBP);

        /* make sure that we align ESP just like the method's prolog did */
        if  (info->doubleAlign)
        {
            // "and esp,-8"
            offset = SKIP_ARITH_REG(-8, methodStart, offset);
            if (curEBP & 0x04)
            {
                pSavedRegs--;
#ifdef _DEBUG
                if (dspPtr) printf("EnumRef: dblalign ebp: %08X\n", curEBP);
#endif
            }
        }

        /* Increment "offset" in steps to see which callee-saved
           registers have been pushed already */

        for (unsigned i = 0; i < STRING_LENGTH(CALLEE_SAVED_REGISTERS_MASK); i++)
        {
            RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i];
            _ASSERTE(regMask != RM_EBP);

            if ((info->savedRegMask & regMask) == 0)
                continue;

            if (InstructionAlreadyExecuted(offset, curOffs))
            {
                SetLocation(pContext, i, PTR_DWORD(--pSavedRegs));
            }

            // "push reg"
            offset = SKIP_PUSH_REG(methodStart, offset) ;
        }

        TRASH_CALLEE_UNSAVED_REGS(pContext);
    }

    /* The caller's saved EBP is pointed to by our EBP */

    pContext->SetEbpLocation(PTR_DWORD((TADDR)curEBP));
    pContext->SP = DWORD((TADDR)(curEBP + sizeof(void *)));

    /* Stack pointer points to return address */

    SetRegdisplayPCTAddr(pContext, (TADDR)pContext->SP);
}

/*****************************************************************************/

#ifdef FEATURE_NATIVEAOT
bool UnwindEbpDoubleAlignFrame(
        PREGDISPLAY     pContext,
        hdrInfo        *info,
        PTR_CBYTE       table,
        PTR_CBYTE       methodStart,
        DWORD           curOffs,
        bool            isFunclet,
        bool            updateAllRegs)
#else
bool UnwindEbpDoubleAlignFrame(
        PREGDISPLAY     pContext,
        hdrInfo        *info,
        PTR_CBYTE       table,
        PTR_CBYTE       methodStart,
        DWORD           curOffs,
        bool            isFunclet,
        bool            updateAllRegs,
        StackwalkCacheUnwindInfo  *pUnwindInfo) // out-only, perf improvement
#endif
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(info->ebpFrame || info->doubleAlign);

    const unsigned curESP = pContext->SP;
    const unsigned curEBP = GetRegdisplayFP(pContext);

    /* First check if we are in a filter (which is obviously after the prolog) */

    if (info->handlers && info->prologOffs == hdrInfo::NOT_IN_PROLOG)
    {
        TADDR baseSP;

#ifdef FEATURE_EH_FUNCLETS
        // Funclets' frame pointers(EBP) are always restored so they can access to main function's local variables.
        // Therefore the value of EBP is invalid for unwinder so we should use ESP instead.
        // TODO If funclet frame layout is changed from CodeGen::genFuncletProlog() and genFuncletEpilog(),
        //      we need to change here accordingly. It is likely to have changes when introducing PSPSym.
        // TODO Currently we assume that ESP of funclet frames is always fixed but actually it could change.
        if (isFunclet)
        {
            baseSP = curESP;
            // Set baseSP as initial SP
            baseSP += GetPushedArgSize(info, table, curOffs);

            // 16-byte stack alignment padding (allocated in genFuncletProlog)
            // Current funclet frame layout (see CodeGen::genFuncletProlog() and genFuncletEpilog()):
            //   prolog: sub esp, 12
            //   epilog: add esp, 12
            //           ret
            // SP alignment padding should be added for all instructions except the first one and the last one.
            // Epilog may not exist (unreachable), so we need to check the instruction code.
            if (curOffs != 0 && methodStart[curOffs] != X86_INSTR_RETN)
                baseSP += 12;

            SetRegdisplayPCTAddr(pContext, (TADDR)baseSP);

            pContext->SP = (DWORD)(baseSP + sizeof(TADDR));

            return true;
        }
#else // FEATURE_EH_FUNCLETS

        FrameType frameType = GetHandlerFrameInfo(info, curEBP,
                                                  curESP, (DWORD) IGNORE_VAL,
                                                  &baseSP);

        /* If we are in a filter, we only need to unwind the funclet stack.
           For catches/finallies, the normal handling will
           cause the frame to be unwound all the way up to ebp skipping
           other frames above it. This is OK, as those frames will be
           dead. Also, the EE will detect that this has happened and it
           will handle any EE frames correctly.
         */

        if (frameType == FR_INVALID)
        {
            return false;
        }

        if (frameType == FR_FILTER)
        {
            SetRegdisplayPCTAddr(pContext, (TADDR)baseSP);

            pContext->SP = (DWORD)(baseSP + sizeof(TADDR));

         // pContext->pEbp = same as before;

#ifdef _DEBUG
            /* The filter has to be called by the VM. So we dont need to
               update callee-saved registers.
             */

            if (updateAllRegs)
            {
                static DWORD s_badData = 0xDEADBEEF;

                pContext->SetEaxLocation(&s_badData);
                pContext->SetEcxLocation(&s_badData);
                pContext->SetEdxLocation(&s_badData);

                pContext->SetEbxLocation(&s_badData);
                pContext->SetEsiLocation(&s_badData);
                pContext->SetEdiLocation(&s_badData);
            }
#endif

#ifndef FEATURE_NATIVEAOT
            if (pUnwindInfo)
            {
                // The filter funclet is like an ESP-framed-method.
                pUnwindInfo->fUseEbp = FALSE;
                pUnwindInfo->fUseEbpAsFrameReg = FALSE;
            }
#endif

            return true;
        }
#endif // !FEATURE_EH_FUNCLETS
    }

    //
    // Prolog of an EBP method
    //

    if (info->prologOffs != hdrInfo::NOT_IN_PROLOG)
    {
        UnwindEbpDoubleAlignFrameProlog(pContext, info, methodStart, updateAllRegs);

        /* Now adjust stack pointer. */

        pContext->SP += ESPIncrOnReturn(info);
        return true;
    }

    if (updateAllRegs)
    {
        // Get to the first callee-saved register
        PTR_DWORD pSavedRegs = PTR_DWORD((TADDR)curEBP);

        if (info->doubleAlign && (curEBP & 0x04))
            pSavedRegs--;

        for (unsigned i = 0; i < STRING_LENGTH(CALLEE_SAVED_REGISTERS_MASK); i++)
        {
            RegMask regMask = CALLEE_SAVED_REGISTERS_MASK[i];
            if ((info->savedRegMask & regMask) == 0)
                continue;

            SetLocation(pContext, i, --pSavedRegs);
        }
    }

    /* The caller's ESP will be equal to EBP + retAddrSize + argSize. */

    pContext->SP = (DWORD)(curEBP + sizeof(curEBP) + ESPIncrOnReturn(info));

    /* The caller's saved EIP is right after our EBP */

    SetRegdisplayPCTAddr(pContext, (TADDR)curEBP + RETURN_ADDR_OFFS * sizeof(TADDR));

    /* The caller's saved EBP is pointed to by our EBP */

    pContext->SetEbpLocation(PTR_DWORD((TADDR)curEBP));
    return true;
}

#ifdef FEATURE_NATIVEAOT
bool UnwindStackFrame(PREGDISPLAY     pContext,
                      PTR_CBYTE       methodStart,
                      DWORD           curOffs,
                      GCInfoToken     gcInfoToken,
                      bool            isFunclet,
                      bool            updateAllRegs)
#else
bool UnwindStackFrame(PREGDISPLAY     pContext,
                      EECodeInfo     *pCodeInfo,
                      unsigned        flags,
                      CodeManState   *pState,
                      StackwalkCacheUnwindInfo  *pUnwindInfo /* out-only, perf improvement */)
#endif
{
#ifdef FEATURE_NATIVEAOT
    hdrInfo infoBuf;
    hdrInfo *info = &infoBuf;
    size_t infoSize = DecodeGCHdrInfo(gcInfoToken, curOffs, &infoBuf);
    PTR_CBYTE table = dac_cast<PTR_CBYTE>(gcInfoToken.Info) + infoSize;
#else
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    bool updateAllRegs = flags & UpdateAllRegs;

    // Address where the method has been interrupted
    PCODE       breakPC = pContext->ControlPC;
    _ASSERTE(PCODEToPINSTR(breakPC) == pCodeInfo->GetCodeAddress());

    PTR_CBYTE methodStart = PTR_CBYTE(pCodeInfo->GetSavedMethodCode());

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    PTR_VOID    methodInfoPtr = gcInfoToken.Info;
    DWORD       curOffs = pCodeInfo->GetRelOffset();

    _ASSERTE(sizeof(CodeManStateBuf) <= sizeof(pState->stateBuf));
    CodeManStateBuf * stateBuf = (CodeManStateBuf*)pState->stateBuf;

    if (pState->dwIsSet == 0)
    {
        /* Extract the necessary information from the info block header */

        stateBuf->hdrInfoSize = (DWORD)DecodeGCHdrInfo(gcInfoToken,
                                                          curOffs,
                                                          &stateBuf->hdrInfoBody);
    }

    PTR_CBYTE table = dac_cast<PTR_CBYTE>(methodInfoPtr) + stateBuf->hdrInfoSize;

    hdrInfo * info = &stateBuf->hdrInfoBody;

    info->isSpeculativeStackWalk = ((flags & SpeculativeStackwalk) != 0);

    if (pUnwindInfo != NULL)
    {
        pUnwindInfo->fUseEbpAsFrameReg = info->ebpFrame;
        pUnwindInfo->fUseEbp = ((info->savedRegMask & RM_EBP) != 0);
    }
#endif

    if  (info->epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {
        /*---------------------------------------------------------------------
         *  First, handle the epilog
         */

        PTR_CBYTE epilogBase = methodStart + (curOffs - info->epilogOffs);
        UnwindEpilog(pContext, info, epilogBase, updateAllRegs);
    }
    else if (!info->ebpFrame && !info->doubleAlign)
    {
        /*---------------------------------------------------------------------
         *  Now handle ESP frames
         */

        UnwindEspFrame(pContext, info, table, methodStart, curOffs, updateAllRegs);
        return true;
    }
    else
    {
        /*---------------------------------------------------------------------
         *  Now we know that have an EBP frame
         */

#ifdef FEATURE_NATIVEAOT
        if (!UnwindEbpDoubleAlignFrame(pContext, info, table, methodStart, curOffs, isFunclet, updateAllRegs))
            return false;
#else
        if (!UnwindEbpDoubleAlignFrame(pContext, info, table, methodStart, curOffs, pCodeInfo->IsFunclet(), updateAllRegs, pUnwindInfo))
            return false;
#endif
    }

    // TODO [DAVBR]: For the full fix for VsWhidbey 450273, all the below
    // may be uncommented once isLegalManagedCodeCaller works properly
    // with non-return address inputs, and with non-DEBUG builds
    /*
    // Ensure isLegalManagedCodeCaller succeeds for speculative stackwalks.
    // (We just assert this below for non-speculative stackwalks.)
    //
    FAIL_IF_SPECULATIVE_WALK(isLegalManagedCodeCaller(GetControlPC(pContext)));
    */

    return true;
}

bool EnumGcRefs(PREGDISPLAY     pContext,
                PTR_CBYTE       methodStart,
                DWORD           curOffs,
                GCInfoToken     gcInfoToken,
                bool            isFunclet,
                bool            isFilterFunclet,
                unsigned        flags,
                GCEnumCallback  pCallBack,
                LPVOID          hCallBack)
{
#ifdef FEATURE_EH_FUNCLETS
    if (flags & ParentOfFuncletStackFrame)
    {
        LOG((LF_GCROOTS, LL_INFO100000, "Not reporting this frame because it was already reported via another funclet.\n"));
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    unsigned  EBP     = GetRegdisplayFP(pContext);
    unsigned  ESP     = pContext->SP;

    unsigned  ptrOffs;

    unsigned  count;

    hdrInfo   info;
    PTR_CBYTE table = PTR_CBYTE(gcInfoToken.Info);
#if 0
    printf("EECodeManager::EnumGcRefs - EIP = %08x ESP = %08x  offset = %x  GC Info is at %08x\n", *pContext->pPC, ESP, curOffs, table);
#endif


    /* Extract the necessary information from the info block header */

    table += DecodeGCHdrInfo(gcInfoToken,
                             curOffs,
                             &info);

    _ASSERTE( curOffs <= info.methodSize);

#ifdef  _DEBUG
//    if ((gcInfoToken.Info == (void*)0x37760d0) && (curOffs == 0x264))
//        __asm int 3;

    if (trEnumGCRefs) {
        static unsigned lastESP = 0;
        unsigned        diffESP = ESP - lastESP;
        if (diffESP > 0xFFFF) {
            printf("------------------------------------------------------\n");
        }
        lastESP = ESP;
        printf("EnumGCRefs [%s][%s] at %s.%s + 0x%03X:\n",
               info.ebpFrame?"ebp":"   ",
               info.interruptible?"int":"   ",
               "UnknownClass","UnknownMethod", curOffs);
        fflush(stdout);
    }
#endif

    /* Are we in the prolog or epilog of the method? */

    if (info.prologOffs != hdrInfo::NOT_IN_PROLOG ||
        info.epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {

#if !DUMP_PTR_REFS
        // Under normal circumstances the system will not suspend a thread
        // if it is in the prolog or epilog of the function.   However ThreadAbort
        // exception or stack overflows can cause EH to happen in a prolog.
        // Once in the handler, a GC can happen, so we can get to this code path.
        // However since we are tearing down this frame, we don't need to report
        // anything and we can simply return.

        _ASSERTE(flags & ExecutionAborted);
#endif
        return true;
    }

#ifdef _DEBUG
#define CHK_AND_REPORT_REG(reg, doIt, iptr, regName)                    \
        if  (doIt)                                                      \
        {                                                               \
            if (dspPtr)                                                 \
                printf("    Live pointer register %s: ", #regName);     \
                pCallBack(hCallBack,                                    \
                          (OBJECTREF*)(pContext->Get##regName##Location()), \
                          (iptr ? GC_CALL_INTERIOR : 0)                 \
                          | CHECK_APP_DOMAIN                            \
                          DAC_ARG(DacSlotLocation(reg, 0, false)));     \
        }
#else // !_DEBUG
#define CHK_AND_REPORT_REG(reg, doIt, iptr, regName)                    \
        if  (doIt)                                                      \
                pCallBack(hCallBack,                                    \
                          (OBJECTREF*)(pContext->Get##regName##Location()), \
                          (iptr ? GC_CALL_INTERIOR : 0)                 \
                          | CHECK_APP_DOMAIN                            \
                          DAC_ARG(DacSlotLocation(reg, 0, false)));

#endif // _DEBUG

    /* What kind of a frame is this ? */

#ifndef FEATURE_EH_FUNCLETS
    FrameType   frameType = FR_NORMAL;
    TADDR       baseSP = 0;

    if (info.handlers)
    {
        _ASSERTE(info.ebpFrame);

        bool    hasInnerFilter, hadInnerFilter;
        frameType = GetHandlerFrameInfo(&info, EBP,
                                        ESP, (DWORD) IGNORE_VAL,
                                        &baseSP, NULL,
                                        &hasInnerFilter, &hadInnerFilter);
        _ASSERTE(frameType != FR_INVALID);

        /* If this is the parent frame of a filter which is currently
           executing, then the filter would have enumerated the frame using
           the filter PC.
         */

        if (hasInnerFilter)
            return true;

        /* If are in a try and we had a filter execute, we may have reported
           GC refs from the filter (and not using the try's offset). So
           we had better use the filter's end offset, as the try is
           effectively dead and its GC ref's would be stale */

        if (hadInnerFilter)
        {
            PTR_TADDR pFirstBaseSPslot = GetFirstBaseSPslotPtr(EBP, &info);
            curOffs = (unsigned)pFirstBaseSPslot[1] - 1;
            _ASSERTE(curOffs < info.methodSize);

            /* Extract the necessary information from the info block header */

            table = PTR_CBYTE(gcInfoToken.Info);

            table += DecodeGCHdrInfo(gcInfoToken,
                                     curOffs,
                                     &info);
        }
    }
#endif

    bool        willContinueExecution = !(flags & ExecutionAborted);
    unsigned    pushedSize = 0;

    /* if we have been interrupted we don't have to report registers/arguments
     * because we are about to lose this context anyway.
     * Alas, if we are in a ebp-less method we have to parse the table
     * in order to adjust ESP.
     *
     * Note that we report "this" for all methods, even if
     * noncontinuable, because of the off chance they may be
     * synchronized and we have to release the monitor on unwind. This
     * could conceivably be optimized, but it turns out to be more
     * expensive to check whether we're synchronized (which involves
     * consulting metadata) than to just report "this" all the time in
     * our most important scenarios.
     */

    if  (info.interruptible)
    {
        unsigned curOffsRegs = curOffs;

        // Don't decrement curOffsRegs when it is 0, as it is an unsigned and will wrap to MAX_UINT
        //
        if (curOffsRegs > 0)
        {
            // If we are not on the active stack frame, we need to report gc registers
            // that are live before the call. The reason is that the liveness of gc registers
            // may change across a call to a method that does not return. In this case the instruction
            // after the call may be a jump target and a register that didn't have a live gc pointer
            // before the call may have a live gc pointer after the jump. To make sure we report the
            // registers that have live gc pointers before the call we subtract 1 from curOffs.
            if ((flags & ActiveStackFrame) == 0)
            {
                // We are not the top most stack frame (i.e. the ActiveStackFrame)
                curOffsRegs--;   // decrement curOffsRegs
            }
        }

        pushedSize = scanArgRegTableI(skipToArgReg(info, table), curOffsRegs, curOffs, &info);

        RegMask   regs  = info.regMaskResult;
        RegMask  iregs  = info.iregMaskResult;
        ptrArgTP  args  = info.argMaskResult;
        ptrArgTP iargs  = info.iargMaskResult;

        _ASSERTE((isZero(args) || pushedSize != 0) || info.ebpFrame);
        _ASSERTE((args & iargs) == iargs);
        // Only synchronized methods and generic code that accesses
        // the type context via "this" need to report "this".
        // If its reported for other methods, its probably
        // done incorrectly. So flag such cases.
        _ASSERTE(info.thisPtrResult == REGI_NA /*||
                 pCodeInfo->GetMethodDesc()->IsSynchronized() ||
                 pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis()*/);

            /* now report registers and arguments if we are not interrupted */

        if  (willContinueExecution)
        {

            /* Propagate unsafed registers only in "current" method */
            /* If this is not the active method, then the callee wil
             * trash these registers, and so we wont need to report them */

            if (flags & ActiveStackFrame)
            {
                CHK_AND_REPORT_REG(REGI_EAX, regs & RM_EAX, iregs & RM_EAX, Eax);
                CHK_AND_REPORT_REG(REGI_ECX, regs & RM_ECX, iregs & RM_ECX, Ecx);
                CHK_AND_REPORT_REG(REGI_EDX, regs & RM_EDX, iregs & RM_EDX, Edx);
            }

            CHK_AND_REPORT_REG(REGI_EBX, regs & RM_EBX, iregs & RM_EBX, Ebx);
            CHK_AND_REPORT_REG(REGI_EBP, regs & RM_EBP, iregs & RM_EBP, Ebp);
            CHK_AND_REPORT_REG(REGI_ESI, regs & RM_ESI, iregs & RM_ESI, Esi);
            CHK_AND_REPORT_REG(REGI_EDI, regs & RM_EDI, iregs & RM_EDI, Edi);
            _ASSERTE(!(regs & RM_ESP));

            /* Report any pending pointer arguments */

            DWORD * pPendingArgFirst;       // points **AT** first parameter
            if (!info.ebpFrame)
            {
                // -sizeof(void*) because we want to point *AT* first parameter
                pPendingArgFirst = (DWORD *)(size_t)(ESP + pushedSize - sizeof(void*));
            }
            else
            {
                _ASSERTE(willContinueExecution);

#ifdef FEATURE_EH_FUNCLETS
                // Funclets' frame pointers(EBP) are always restored so they can access to main function's local variables.
                // Therefore the value of EBP is invalid for unwinder so we should use ESP instead.
                // See UnwindStackFrame for details.
                if (isFunclet)
                {
                    TADDR baseSP = ESP;
                    // Set baseSP as initial SP
                    baseSP += GetPushedArgSize(&info, table, curOffs);
                    // 16-byte stack alignment padding (allocated in genFuncletProlog)
                    // Current funclet frame layout (see CodeGen::genFuncletProlog() and genFuncletEpilog()):
                    //   prolog: sub esp, 12
                    //   epilog: add esp, 12
                    //           ret
                    // SP alignment padding should be added for all instructions except the first one and the last one.
                    // Epilog may not exist (unreachable), so we need to check the instruction code.
                    if (curOffs != 0 && methodStart[curOffs] != X86_INSTR_RETN)
                        baseSP += 12;

                    // -sizeof(void*) because we want to point *AT* first parameter
                    pPendingArgFirst = (DWORD *)(size_t)(baseSP - sizeof(void*));
                }
#else // FEATURE_EH_FUNCLETS
                if (info.handlers)
                {
                    // -sizeof(void*) because we want to point *AT* first parameter
                    pPendingArgFirst = (DWORD *)(size_t)(baseSP - sizeof(void*));
                }
#endif
                else if (info.localloc)
                {
                    TADDR baseSP = *(DWORD *)(size_t)(EBP - GetLocallocSPOffset(&info));
                    // -sizeof(void*) because we want to point *AT* first parameter
                    pPendingArgFirst = (DWORD *)(size_t) (baseSP - sizeof(void*));
                }
                else
                {
                    // Note that 'info.stackSize includes the size for pushing EBP, but EBP is pushed
                    // BEFORE EBP is set from ESP, thus (EBP - info.stackSize) actually points past
                    // the frame by one DWORD, and thus points *AT* the first parameter

                    pPendingArgFirst = (DWORD *)(size_t)(EBP - info.stackSize);
                }
            }

            if  (!isZero(args))
            {
                unsigned   i = 0;
                ptrArgTP   b(1);
                for (; !isZero(args) && (i < MAX_PTRARG_OFS); i += 1, b <<= 1)
                {
                    if  (intersect(args,b))
                    {
                        unsigned    argAddr = (unsigned)(size_t)(pPendingArgFirst - i);
                        bool        iptr    = false;

                        setDiff(args, b);
                        if (intersect(iargs,b))
                        {
                            setDiff(iargs, b);
                            iptr   = true;
                        }

#ifdef _DEBUG
                        if (dspPtr)
                        {
                            printf("    Pushed ptr arg  [E");
                            if  (info.ebpFrame)
                                printf("BP-%02XH]: ", EBP - argAddr);
                            else
                                printf("SP+%02XH]: ", argAddr - ESP);
                        }
#endif
                        _ASSERTE(true == GC_CALL_INTERIOR);
                        pCallBack(hCallBack, (OBJECTREF *)(size_t)argAddr, (int)iptr | CHECK_APP_DOMAIN
                                  DAC_ARG(DacSlotLocation(info.ebpFrame ? REGI_EBP : REGI_ESP,
                                                          info.ebpFrame ? EBP - argAddr : argAddr - ESP,
                                                          true)));
                    }
                }
            }
        }
        else
        {
            // Is "this" enregistered. If so, report it as we might need to
            // release the monitor for synchronized methods.
            // Else, it is on the stack and will be reported below.

            if (info.thisPtrResult != REGI_NA)
            {
                // Synchronized methods and methods satisfying
                // MethodDesc::AcquiresInstMethodTableFromThis (i.e. those
                // where "this" is reported in thisPtrResult) are
                // not supported on value types.
                _ASSERTE((regNumToMask(info.thisPtrResult) & info.iregMaskResult)== 0);

                void * thisReg = getCalleeSavedReg(pContext, info.thisPtrResult);
                pCallBack(hCallBack, (OBJECTREF *)thisReg, CHECK_APP_DOMAIN
                          DAC_ARG(DacSlotLocation(info.thisPtrResult, 0, false)));
            }
        }
    }
    else /* not interruptible */
    {
        pushedSize = scanArgRegTable(skipToArgReg(info, table), curOffs, &info);

        RegMask    regMask = info.regMaskResult;
        RegMask   iregMask = info.iregMaskResult;
        ptrArgTP   argMask = info.argMaskResult;
        ptrArgTP  iargMask = info.iargMaskResult;
        unsigned   argHnum = info.argHnumResult;
        PTR_CBYTE   argTab = info.argTabResult;

        // Only synchronized methods and generic code that accesses
        // the type context via "this" need to report "this".
        // If its reported for other methods, its probably
        // done incorrectly. So flag such cases.
        _ASSERTE(info.thisPtrResult == REGI_NA /*||
                 pCodeInfo->GetMethodDesc()->IsSynchronized()   ||
                 pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis()*/);


        /* now report registers and arguments if we are not interrupted */

        if  (willContinueExecution)
        {

            /* Report all live pointer registers */

            CHK_AND_REPORT_REG(REGI_EDI, regMask & RM_EDI, iregMask & RM_EDI, Edi);
            CHK_AND_REPORT_REG(REGI_ESI, regMask & RM_ESI, iregMask & RM_ESI, Esi);
            CHK_AND_REPORT_REG(REGI_EBX, regMask & RM_EBX, iregMask & RM_EBX, Ebx);
            CHK_AND_REPORT_REG(REGI_EBP, regMask & RM_EBP, iregMask & RM_EBP, Ebp);

            /* Esp cant be reported */
            _ASSERTE(!(regMask & RM_ESP));
            /* No callee-trashed registers */
            _ASSERTE(!(regMask & RM_CALLEE_TRASHED));
            /* EBP can't be reported unless we have an EBP-less frame */
            _ASSERTE(!(regMask & RM_EBP) || !(info.ebpFrame));

            /* Report any pending pointer arguments */

            if (argTab != 0)
            {
                unsigned    lowBits, stkOffs, argAddr, val;

                // argMask does not fit in 32-bits
                // thus arguments are reported via a table
                // Both of these are very rare cases

                do
                {
                    val = fastDecodeUnsigned(argTab);

                    lowBits = val &  OFFSET_MASK;
                    stkOffs = val & ~OFFSET_MASK;
                    _ASSERTE((lowBits == 0) || (lowBits == byref_OFFSET_FLAG));

                    argAddr = ESP + stkOffs;
#ifdef _DEBUG
                    if (dspPtr)
                        printf("    Pushed %sptr arg at [ESP+%02XH]",
                               lowBits ? "iptr " : "", stkOffs);
#endif
                    _ASSERTE(byref_OFFSET_FLAG == GC_CALL_INTERIOR);
                    pCallBack(hCallBack, (OBJECTREF *)(size_t)argAddr, lowBits | CHECK_APP_DOMAIN
                              DAC_ARG(DacSlotLocation(REGI_ESP, stkOffs, true)));
                }
                while(--argHnum);

                _ASSERTE(info.argTabResult + info.argTabBytes == argTab);
            }
            else
            {
                unsigned argAddr = ESP;

                while (!isZero(argMask))
                {
                    _ASSERTE(argHnum-- > 0);

                    if  (toUnsigned(argMask) & 1)
                    {
                        bool     iptr    = false;

                        if (toUnsigned(iargMask) & 1)
                            iptr = true;
#ifdef _DEBUG
                        if (dspPtr)
                            printf("    Pushed ptr arg at [ESP+%02XH]",
                                   argAddr - ESP);
#endif
                        _ASSERTE(true == GC_CALL_INTERIOR);
                        pCallBack(hCallBack, (OBJECTREF *)(size_t)argAddr, (int)iptr | CHECK_APP_DOMAIN
                                  DAC_ARG(DacSlotLocation(REGI_ESP, argAddr - ESP, true)));
                    }

                    argMask >>= 1;
                    iargMask >>= 1;
                    argAddr  += 4;
                }

            }

        }
        else
        {
            // Is "this" enregistered. If so, report it as we will need to
            // release the monitor. Else, it is on the stack and will be
            // reported below.

            // For partially interruptible code, info.thisPtrResult will be
            // the last known location of "this". So the compiler needs to
            // generate information which is correct at every point in the code,
            // not just at call sites.

            if (info.thisPtrResult != REGI_NA)
            {
                // Synchronized methods on value types are not supported
                _ASSERTE((regNumToMask(info.thisPtrResult) & info.iregMaskResult)== 0);

                void * thisReg = getCalleeSavedReg(pContext, info.thisPtrResult);
                pCallBack(hCallBack, (OBJECTREF *)thisReg, CHECK_APP_DOMAIN
                          DAC_ARG(DacSlotLocation(info.thisPtrResult, 0, false)));
            }
        }

    } //info.interruptible

    /* compute the argument base (reference point) */

    unsigned    argBase;

    if (info.ebpFrame)
        argBase = EBP;
    else
        argBase = ESP + pushedSize;

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBEEF);
#endif

    unsigned ptrAddr;
    unsigned lowBits;


    /* Process the untracked frame variable table */

#if defined(FEATURE_EH_FUNCLETS)   // funclets
    // Filters are the only funclet that run during the 1st pass, and must have
    // both the leaf and the parent frame reported.  In order to avoid double
    // reporting of the untracked variables, do not report them for the filter.
    //if (!pCodeInfo->GetJitManager()->IsFilterFunclet(pCodeInfo))
    if (!isFilterFunclet)
#endif // FEATURE_EH_FUNCLETS
    {
        count = info.untrackedCnt;
        int lastStkOffs = 0;
        while (count-- > 0)
        {
            int stkOffs = fastDecodeSigned(table);
            stkOffs = lastStkOffs - stkOffs;
            lastStkOffs = stkOffs;

            _ASSERTE(0 == ~OFFSET_MASK % sizeof(void*));

            lowBits  =   OFFSET_MASK & stkOffs;
            stkOffs &=  ~OFFSET_MASK;

            ptrAddr = argBase + stkOffs;
            if (info.doubleAlign && stkOffs >= int(info.stackSize - sizeof(void*))) {
                // We encode the arguments as if they were ESP based variables even though they aren't
                // If this frame would have ben an ESP based frame,   This fake frame is one DWORD
                // smaller than the real frame because it did not push EBP but the real frame did.
                // Thus to get the correct EBP relative offset we have to adjust by info.stackSize-sizeof(void*)
                ptrAddr = EBP + (stkOffs-(info.stackSize - sizeof(void*)));
            }

#ifdef  _DEBUG
            if (dspPtr)
            {
                printf("    Untracked %s%s local at [E",
                            (lowBits & pinned_OFFSET_FLAG) ? "pinned " : "",
                            (lowBits & byref_OFFSET_FLAG)  ? "byref"   : "");

                int   dspOffs = ptrAddr;
                char  frameType;

                if (info.ebpFrame) {
                    dspOffs   -= EBP;
                    frameType  = 'B';
                }
                else {
                    dspOffs   -= ESP;
                    frameType  = 'S';
                }

                if (dspOffs < 0)
                    printf("%cP-%02XH]: ", frameType, -dspOffs);
                else
                    printf("%cP+%02XH]: ", frameType, +dspOffs);
            }
#endif

            _ASSERTE((pinned_OFFSET_FLAG == GC_CALL_PINNED) &&
                   (byref_OFFSET_FLAG  == GC_CALL_INTERIOR));
            pCallBack(hCallBack, (OBJECTREF*)(size_t)ptrAddr, lowBits | CHECK_APP_DOMAIN
                      DAC_ARG(DacSlotLocation(info.ebpFrame ? REGI_EBP : REGI_ESP,
                                              info.ebpFrame ? EBP - ptrAddr : ptrAddr - ESP,
                                              true)));
        }

    }

#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xCAFE);
#endif

    /* Process the frame variable lifetime table */
    count = info.varPtrTableSize;

    /* If we are not in the active method, we are currently pointing
     * to the return address; at the return address stack variables
     * can become dead if the call the last instruction of a try block
     * and the return address is the jump around the catch block. Therefore
     * we simply assume an offset inside of call instruction.
     */

    unsigned newCurOffs;

    if (willContinueExecution)
    {
        newCurOffs = (flags & ActiveStackFrame) ?  curOffs    // after "call"
                                                :  curOffs-1; // inside "call"
    }
    else
    {
        /* However if ExecutionAborted, then this must be one of the
         * ExceptionFrames. Handle accordingly
         */
        _ASSERTE(!(flags & AbortingCall) || !(flags & ActiveStackFrame));

        newCurOffs = (flags & AbortingCall) ? curOffs-1 // inside "call"
                                            : curOffs;  // at faulting instr, or start of "try"
    }

    ptrOffs    = 0;

    while (count-- > 0)
    {
        int       stkOffs;
        unsigned  begOffs;
        unsigned  endOffs;

        stkOffs = fastDecodeUnsigned(table);
        begOffs  = ptrOffs + fastDecodeUnsigned(table);
        endOffs  = begOffs + fastDecodeUnsigned(table);

        _ASSERTE(0 == ~OFFSET_MASK % sizeof(void*));

        lowBits  =   OFFSET_MASK & stkOffs;
        stkOffs &=  ~OFFSET_MASK;

        if (info.ebpFrame) {
            stkOffs = -stkOffs;
            _ASSERTE(stkOffs < 0);
        }
        else {
            _ASSERTE(stkOffs >= 0);
        }

        ptrAddr = argBase + stkOffs;

        /* Is this variable live right now? */

        if (newCurOffs >= begOffs)
        {
            if (newCurOffs <  endOffs)
            {
#ifdef  _DEBUG
                if (dspPtr) {
                    printf("    Frame %s%s local at [E",
                           (lowBits & byref_OFFSET_FLAG) ? "byref "   : "",
#ifndef FEATURE_EH_FUNCLETS
                           (lowBits & this_OFFSET_FLAG)  ? "this-ptr" : "");
#else
                           (lowBits & pinned_OFFSET_FLAG)  ? "pinned" : "");
#endif


                    int  dspOffs = ptrAddr;
                    char frameType;

                    if (info.ebpFrame) {
                        dspOffs   -= EBP;
                        frameType  = 'B';
                    }
                    else {
                        dspOffs   -= ESP;
                        frameType  = 'S';
                    }

                    if (dspOffs < 0)
                        printf("%cP-%02XH]: ", frameType, -dspOffs);
                    else
                        printf("%cP+%02XH]: ", frameType, +dspOffs);
                }
#endif

                unsigned flags = CHECK_APP_DOMAIN;
#ifndef FEATURE_EH_FUNCLETS
                // First  Bit : byref
                // Second Bit : this
                // The second bit means `this` not `pinned`. So we ignore it.
                flags |= lowBits & byref_OFFSET_FLAG;
#else
                // First  Bit : byref
                // Second Bit : pinned
                // Both bits are valid
                flags |= lowBits;
#endif

                _ASSERTE(byref_OFFSET_FLAG == GC_CALL_INTERIOR);
                pCallBack(hCallBack, (OBJECTREF*)(size_t)ptrAddr, flags
                          DAC_ARG(DacSlotLocation(info.ebpFrame ? REGI_EBP : REGI_ESP,
                                          info.ebpFrame ? EBP - ptrAddr : ptrAddr - ESP,
                                          true)));
            }
        }
        // exit loop early if start of live range is beyond PC, as ranges are sorted by lower bound
        else break;

        ptrOffs  = begOffs;
    }


#if VERIFY_GC_TABLES
    _ASSERTE(*castto(table, unsigned short *)++ == 0xBABE);
#endif

#ifdef FEATURE_EH_FUNCLETS   // funclets
    //
    // If we're in a funclet, we do not want to report the incoming varargs.  This is
    // taken care of by the parent method and the funclet should access those arguments
    // by way of the parent method's stack frame.
    //
    //if(pCodeInfo->IsFunclet())
    if (isFunclet)
    {
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    /* Are we a varargs function, if so we have to report all args
       except 'this' (note that the GC tables created by the x86 jit
       do not contain ANY arguments except 'this' (even if they
       were statically declared */

    if (info.varargs) {
#ifdef FEATURE_NATIVEAOT
        PORTABILITY_ASSERT("EnumGCRefs: VarArgs");
#else
        LOG((LF_GCINFO, LL_INFO100, "Reporting incoming vararg GC refs\n"));

        PTR_BYTE argsStart;

        if (info.ebpFrame || info.doubleAlign)
            argsStart = PTR_BYTE((size_t)EBP) + 2* sizeof(void*);                 // pushed EBP and retAddr
        else
            argsStart = PTR_BYTE((size_t)argBase) + info.stackSize + sizeof(void*);   // ESP + locals + retAddr

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        // Note that I really want to say hCallBack is a GCCONTEXT, but this is pretty close
        extern void GcEnumObject(LPVOID pData, OBJECTREF *pObj, uint32_t flags);
        _ASSERTE((void*) GcEnumObject == pCallBack);
#endif
        GCCONTEXT   *pCtx = (GCCONTEXT *) hCallBack;

        // For varargs, look up the signature using the varArgSig token passed on the stack
        PTR_VASigCookie varArgSig = *PTR_PTR_VASigCookie(argsStart);

        promoteVarArgs(argsStart, varArgSig, pCtx);
#endif
    }

    return true;
}
