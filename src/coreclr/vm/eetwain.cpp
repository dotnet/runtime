// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#include "ecall.h"
#include "eetwain.h"
#include "dbginterface.h"
#include "gcenv.h"

#define RETURN_ADDR_OFFS        1       // in DWORDS

#ifdef USE_GC_INFO_DECODER
#include "gcinfodecoder.h"
#endif

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "argdestination.h"

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

#ifndef USE_GC_INFO_DECODER


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
#endif // !USE_GC_INFO_DECODER

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

    if (gcInfoDecoder.IsInterruptible())
        return true;

    if (gcInfoDecoder.IsInterruptibleSafePoint())
        return true;

    return false;
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

#ifdef _DEBUG
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

/*****************************************************************************/
#ifdef TARGET_X86 // UnwindStackFrame
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
#ifdef FEATURE_EH_FUNCLETS
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
        unsigned flags)
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
            if ((flags & UpdateAllRegs) || (regMask == RM_EBP))
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
    pContext->PCTAddr = (TADDR)ESP;
    pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

    pContext->SP = ESP;
}

/*****************************************************************************/

void UnwindEbpDoubleAlignFrameEpilog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE epilogBase,
        unsigned flags)
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
            if (flags & UpdateAllRegs)
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

    pContext->PCTAddr = (TADDR)ESP;
    pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

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
        unsigned flags)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(info->epilogOffs != hdrInfo::NOT_IN_EPILOG);
    // _ASSERTE(flags & ActiveStackFrame); // <TODO> Wont work for thread death</TODO>
    _ASSERTE(info->epilogOffs > 0);

    if  (info->ebpFrame || info->doubleAlign)
    {
        UnwindEbpDoubleAlignFrameEpilog(pContext, info, epilogBase, flags);
    }
    else
    {
        UnwindEspFrameEpilog(pContext, info, epilogBase, flags);
    }

#ifdef _DEBUG
    if (flags & UpdateAllRegs)
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
        unsigned flags)
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
    INDEBUG(offset = 0xCCCCCCCC);


    // Always restore EBP
    if (regsMask & RM_EBP)
        pContext->SetEbpLocation(savedRegPtr++);

    if (flags & UpdateAllRegs)
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

    pContext->PCTAddr = (TADDR)ESP;
    pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

    /* Now adjust stack pointer */

    pContext->SP = ESP + ESPIncrOnReturn(info);
}


/*****************************************************************************/

void UnwindEbpDoubleAlignFrameProlog(
        PREGDISPLAY pContext,
        hdrInfo * info,
        PTR_CBYTE methodStart,
        unsigned flags)
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

        pContext->PCTAddr = (TADDR)pContext->SP;
        pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

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

    if (flags & UpdateAllRegs)
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

    pContext->PCTAddr = (TADDR)pContext->SP;
    pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);
}

/*****************************************************************************/

bool UnwindEbpDoubleAlignFrame(
        PREGDISPLAY     pContext,
        EECodeInfo     *pCodeInfo,
        hdrInfo        *info,
        PTR_CBYTE       table,
        PTR_CBYTE       methodStart,
        DWORD           curOffs,
        unsigned        flags,
        StackwalkCacheUnwindInfo  *pUnwindInfo) // out-only, perf improvement
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
        if (pCodeInfo->IsFunclet())
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
            const TADDR funcletStart = pCodeInfo->GetJitManager()->GetFuncletStartAddress(pCodeInfo);
            if (funcletStart != pCodeInfo->GetCodeAddress() && methodStart[pCodeInfo->GetRelOffset()] != X86_INSTR_RETN)
                baseSP += 12;

            pContext->PCTAddr = baseSP;
            pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

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
            pContext->PCTAddr = baseSP;
            pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

            pContext->SP = (DWORD)(baseSP + sizeof(TADDR));

         // pContext->pEbp = same as before;

#ifdef _DEBUG
            /* The filter has to be called by the VM. So we dont need to
               update callee-saved registers.
             */

            if (flags & UpdateAllRegs)
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

            if (pUnwindInfo)
            {
                // The filter funclet is like an ESP-framed-method.
                pUnwindInfo->fUseEbp = FALSE;
                pUnwindInfo->fUseEbpAsFrameReg = FALSE;
            }

            return true;
        }
#endif // !FEATURE_EH_FUNCLETS
    }

    //
    // Prolog of an EBP method
    //

    if (info->prologOffs != hdrInfo::NOT_IN_PROLOG)
    {
        UnwindEbpDoubleAlignFrameProlog(pContext, info, methodStart, flags);

        /* Now adjust stack pointer. */

        pContext->SP += ESPIncrOnReturn(info);
        return true;
    }

    if (flags & UpdateAllRegs)
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

    pContext->PCTAddr = (TADDR)curEBP + RETURN_ADDR_OFFS * sizeof(TADDR);
    pContext->ControlPC = *PTR_PCODE(pContext->PCTAddr);

    /* The caller's saved EBP is pointed to by our EBP */

    pContext->SetEbpLocation(PTR_DWORD((TADDR)curEBP));
    return true;
}

bool UnwindStackFrame(PREGDISPLAY     pContext,
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

    if  (info->epilogOffs != hdrInfo::NOT_IN_EPILOG)
    {
        /*---------------------------------------------------------------------
         *  First, handle the epilog
         */

        PTR_CBYTE epilogBase = methodStart + (curOffs - info->epilogOffs);
        UnwindEpilog(pContext, info, epilogBase, flags);
    }
    else if (!info->ebpFrame && !info->doubleAlign)
    {
        /*---------------------------------------------------------------------
         *  Now handle ESP frames
         */

        UnwindEspFrame(pContext, info, table, methodStart, curOffs, flags);
        return true;
    }
    else
    {
        /*---------------------------------------------------------------------
         *  Now we know that have an EBP frame
         */

        if (!UnwindEbpDoubleAlignFrame(pContext, pCodeInfo, info, table, methodStart, curOffs, flags, pUnwindInfo))
            return false;
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

#endif // TARGET_X86

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

#ifdef FEATURE_EH_FUNCLETS
    if (flags & ParentOfFuncletStackFrame)
    {
        LOG((LF_GCROOTS, LL_INFO100000, "Not reporting this frame because it was already reported via another funclet.\n"));
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    GCInfoToken gcInfoToken = pCodeInfo->GetGCInfoToken();
    unsigned  curOffs = pCodeInfo->GetRelOffset();

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
        _ASSERTE(info.thisPtrResult == REGI_NA ||
                 pCodeInfo->GetMethodDesc()->IsSynchronized() ||
                 pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis());

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

                if (info.handlers)
                {
                    // -sizeof(void*) because we want to point *AT* first parameter
                    pPendingArgFirst = (DWORD *)(size_t)(baseSP - sizeof(void*));
                }
                else if (info.localloc)
                {
                    baseSP = *(DWORD *)(size_t)(EBP - GetLocallocSPOffset(&info));
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
        _ASSERTE(info.thisPtrResult == REGI_NA ||
                 pCodeInfo->GetMethodDesc()->IsSynchronized()   ||
                 pCodeInfo->GetMethodDesc()->AcquiresInstMethodTableFromThis());


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
    if (!pCodeInfo->GetJitManager()->IsFilterFunclet(pCodeInfo))
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
    if(pCodeInfo->IsFunclet())
    {
        return true;
    }
#endif // FEATURE_EH_FUNCLETS

    /* Are we a varargs function, if so we have to report all args
       except 'this' (note that the GC tables created by the x86 jit
       do not contain ANY arguments except 'this' (even if they
       were statically declared */

    if (info.varargs) {
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
    }

    return true;
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
        if(!_gcInfoDecoder.IsInterruptible() && !_gcInfoDecoder.IsInterruptibleSafePoint())
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
        _ASSERTE(_gcInfoDecoder.IsInterruptible() || _gcInfoDecoder.IsInterruptibleSafePoint());
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

    if ((flags & ActiveStackFrame) != 0)
    {
        // CONSIDER: We can optimize this by remembering the need to adjust in IsSafePoint and propagating into here.
        //           Or, better yet, maybe we should change the decoder to not require this adjustment.
        //           The scenario that adjustment tries to handle (fallthrough into BB with random liveness)
        //           does not seem possible.
        if (!gcInfoDecoder.HasInterruptibleRanges())
        {
            gcInfoDecoder = GcInfoDecoder(
                gcInfoToken,
                GcInfoDecoderFlags(DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG),
                curOffs - 1
            );

            _ASSERTE(gcInfoDecoder.IsInterruptibleSafePoint());
        }
    }

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

