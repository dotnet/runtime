//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
/*****************************************************************************
 *                               GCDumpNonX86.cpp
 */

#include "gcdump.h"

#define LIMITED_METHOD_CONTRACT ((void)0)
#define WRAPPER_NO_CONTRACT ((void)0)

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.h"
#include "gcinfodumper.h"


PCSTR GetRegName (UINT32 regnum)
{
#ifdef _TARGET_AMD64_

    switch (regnum)
    {
    case 0: return "rax";
    case 1: return "rcx";
    case 2: return "rdx";
    case 3: return "rbx";
    case 4: return "rsp";
    case 5: return "rbp";
    case 6: return "rsi";
    case 7: return "rdi";
    case 8: return "r8";
    case 9: return "r9";
    case 10: return "r10";
    case 11: return "r11";
    case 12: return "r12";
    case 13: return "r13";
    case 14: return "r14";
    case 15: return "r15";
    }
    
    
    return "???";
#elif defined(_TARGET_ARM64_)
    if (regnum > 28)
        return "???";

    static CHAR szRegName[16];
    _snprintf_s(szRegName, _countof(szRegName), sizeof(szRegName), "X%u", regnum);
    return szRegName;
#elif defined(_TARGET_ARM_)
    if (regnum > 128)
        return "???";

    static CHAR szRegName[16];
    _snprintf_s(szRegName, _countof(szRegName), sizeof(szRegName), "r%u", regnum);
    return szRegName;

#endif
}


/*****************************************************************************/


GCDump::GCDump(bool encBytes, unsigned maxEncBytes, bool dumpCodeOffs)
  : fDumpEncBytes   (encBytes    ), 
    cMaxEncBytes    (maxEncBytes ), 
    fDumpCodeOffsets(dumpCodeOffs)
{
}


/*****************************************************************************/


struct GcInfoDumpState
{
    UINT32 LastCodeOffset;
    BOOL fAnythingPrinted;
    BOOL fSafePoint;
    UINT32 FrameRegister;
    GCDump::printfFtn pfnPrintf;
};


BOOL InterruptibleStateChangeCallback (
            UINT32 CodeOffset,
            BOOL fInterruptible,
            PVOID pvData)
{
    GcInfoDumpState *pState = (GcInfoDumpState*)pvData;

    if (pState->fAnythingPrinted)
    {
        pState->pfnPrintf("\n");
        pState->fAnythingPrinted = FALSE;
        pState->fSafePoint = FALSE;
    }
    
    pState->pfnPrintf("%08x%s interruptible\n", CodeOffset, fInterruptible ? "" : " not");

    pState->LastCodeOffset = -1;

    return FALSE;
}

BOOL SafePointCallback (
            UINT32 CodeOffset,
            PVOID pvData)
{
    GcInfoDumpState *pState = (GcInfoDumpState*)pvData;

    if (pState->fAnythingPrinted)
    {
        pState->pfnPrintf("\n");
    }
    
    pState->pfnPrintf("%08x is a safepoint: ", CodeOffset);

    pState->LastCodeOffset = CodeOffset;
    pState->fAnythingPrinted = TRUE;
    pState->fSafePoint = TRUE;

    return FALSE;
}


VOID PrintFlags (GCDump::printfFtn pfnPrintf, GcSlotFlags Flags)
{
    if (Flags & GC_SLOT_PINNED)
        pfnPrintf("(pinned)");

    if (Flags & GC_SLOT_INTERIOR)
        pfnPrintf("(interior)");

    if (Flags & GC_SLOT_UNTRACKED)
        pfnPrintf("(untracked)");
}


BOOL RegisterStateChangeCallback (
            UINT32 CodeOffset,
            UINT32 RegisterNumber,
            GcSlotFlags Flags,
            GcSlotState NewState,
            PVOID pvData)
{
    GcInfoDumpState *pState = (GcInfoDumpState*)pvData;

    if (pState->fSafePoint && (GC_SLOT_LIVE != NewState))
    {
        // Don't print deaths for safepoints
        return FALSE;
    }

    if (pState->LastCodeOffset != CodeOffset)
    {
        if (pState->fAnythingPrinted)
            pState->pfnPrintf("\n");

        pState->pfnPrintf("%08x", CodeOffset);

        pState->LastCodeOffset = CodeOffset;
    }

    char delta = ((GC_SLOT_LIVE == NewState) ? '+' : '-');

    pState->pfnPrintf(" %c%s", delta, GetRegName(RegisterNumber));

    PrintFlags(pState->pfnPrintf, Flags);

    pState->fAnythingPrinted = TRUE;

    return FALSE;
}

    
BOOL StackSlotStateChangeCallback (
            UINT32 CodeOffset,
            GcSlotFlags flags,
            GcStackSlotBase BaseRegister,
            SSIZE_T StackOffset,
            GcSlotState NewState,
            PVOID pvData)
{
    GcInfoDumpState *pState = (GcInfoDumpState*)pvData;

    if (pState->fSafePoint && (GC_SLOT_LIVE != NewState))
    {
        // Don't print deaths for safepoints
        return FALSE;
    }

    if (pState->LastCodeOffset != CodeOffset)
    {
        if (pState->fAnythingPrinted)
            pState->pfnPrintf("\n");

        if ((CodeOffset == -2) && !pState->fAnythingPrinted)
            pState->pfnPrintf("Untracked:");
        else
            pState->pfnPrintf("%08x", CodeOffset);

        pState->LastCodeOffset = CodeOffset;
    }

    char delta = ((GC_SLOT_LIVE == NewState) ? '+' : '-');

    CHAR sign = '+';

    // the dumper's call back (in GcInfoDumper.cpp) has to "guess" the base register
    // for stack slots it usually guesses it wrong ......
    // We try to filter out at least the non-sensical combinations
    //     - negative offset relative to SP
    //     - positive offset relative to CALLER_SP

    if (StackOffset < 0)
    {
        StackOffset = -StackOffset;
        sign = '-';
#ifndef GCINFODUMPER_IS_FIXED
        if (BaseRegister == GC_SP_REL)
        {                                    // negative offset to SP????
            BaseRegister = GC_CALLER_SP_REL;
        }
#endif // !GCINFODUMPER_IS_FIXED
    }
#ifndef GCINFODUMPER_IS_FIXED
    else if (BaseRegister == GC_CALLER_SP_REL)
    {                                       // positive offset to Caller_SP????
        BaseRegister = GC_SP_REL;
    }
#endif // !GCINFODUMPER_IS_FIXED

    

    PCSTR pszBaseReg;

    switch (BaseRegister)
    {
    case GC_CALLER_SP_REL: pszBaseReg = "caller.sp";                       break;
    case GC_SP_REL:        pszBaseReg = "sp";                              break;
    case GC_FRAMEREG_REL:  pszBaseReg = GetRegName(pState->FrameRegister); break;
    default:               pszBaseReg = "???";                             break;
    }
    
    pState->pfnPrintf(" %c%s%c%x", delta, pszBaseReg, sign, StackOffset);

    PrintFlags(pState->pfnPrintf, flags);

    pState->fAnythingPrinted = TRUE;

    return FALSE;
}

    
size_t      GCDump::DumpGCTable(PTR_CBYTE      table,
                                unsigned       methodSize,
                                bool           verifyGCTables)
{
    GcInfoDecoder hdrdecoder(table,
                             (GcInfoDecoderFlags)(  DECODE_SECURITY_OBJECT
                                                  | DECODE_GS_COOKIE
                                                  | DECODE_CODE_LENGTH
                                                  | DECODE_PSP_SYM
                                                  | DECODE_VARARG
                                                  | DECODE_GENERICS_INST_CONTEXT
                                                  | DECODE_GC_LIFETIMES
                                                  | DECODE_PROLOG_LENGTH),
                             0);

    if (NO_SECURITY_OBJECT != hdrdecoder.GetSecurityObjectStackSlot() ||
        NO_GENERICS_INST_CONTEXT != hdrdecoder.GetGenericsInstContextStackSlot() ||
        NO_GS_COOKIE == hdrdecoder.GetGSCookieStackSlot())
    {
        gcPrintf("Prolog size: ");
        UINT32 prologSize = hdrdecoder.GetPrologSize();
        gcPrintf("%d\n", prologSize);
    }
    
    gcPrintf("Security object: ");
    if (NO_SECURITY_OBJECT == hdrdecoder.GetSecurityObjectStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetSecurityObjectStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

        gcPrintf("caller.sp%c%x\n", sign, ofs);
    }

    gcPrintf("GS cookie: ");
    if (NO_GS_COOKIE == hdrdecoder.GetGSCookieStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetGSCookieStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

        gcPrintf("caller.sp%c%x\n", sign, ofs);

        UINT32 validRangeStart = hdrdecoder.GetGSCookieValidRangeStart();
        UINT32 validRangeEnd = hdrdecoder.GetGSCookieValidRangeEnd();
        gcPrintf("GS cookie valid range: [%x;%x)\n", validRangeStart, validRangeEnd);
    }

    gcPrintf("PSPSym: ");
    if (NO_PSP_SYM == hdrdecoder.GetPSPSymStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetPSPSymStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

#ifdef _TARGET_AMD64_
        // The PSPSym is relative to InitialSP on X64 and CallerSP on other platforms.
        gcPrintf("initial.sp%c%x\n", sign, ofs);
#else
        gcPrintf("caller.sp%c%x\n", sign, ofs);
#endif
    }

    gcPrintf("Generics inst context: ");
    if (NO_GENERICS_INST_CONTEXT == hdrdecoder.GetGenericsInstContextStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetGenericsInstContextStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

        gcPrintf("caller.sp%c%x\n", sign, ofs);
    }

    gcPrintf("PSP slot: ");
    if (NO_PSP_SYM == hdrdecoder.GetPSPSymStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetPSPSymStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

        gcPrintf("caller.sp%c%x\n", sign, ofs);

    }
    
    gcPrintf("GenericInst slot: ");
    if (NO_GENERICS_INST_CONTEXT == hdrdecoder.GetGenericsInstContextStackSlot())
    {
        gcPrintf("<none>\n");
    }
    else
    {
        INT32 ofs = hdrdecoder.GetGenericsInstContextStackSlot();
        char sign = '+';

        if (ofs < 0)
        {
            sign = '-';
            ofs = -ofs;
        }

        gcPrintf("caller.sp%c%x ", sign, ofs);

        if (hdrdecoder.HasMethodDescGenericsInstContext())
             gcPrintf("(GENERIC_PARAM_CONTEXT_METHODDESC)\n");
        else if (hdrdecoder.HasMethodTableGenericsInstContext())
             gcPrintf("(GENERIC_PARAM_CONTEXT_METHODHANDLE)\n");
        else
             gcPrintf("(GENERIC_PARAM_CONTEXT_THIS)\n");
    }
    
    gcPrintf("Varargs: %u\n", hdrdecoder.GetIsVarArg());
    gcPrintf("Frame pointer: %s\n", NO_STACK_BASE_REGISTER == hdrdecoder.GetStackBaseRegister()
                                    ? "<none>"
                                    : GetRegName(hdrdecoder.GetStackBaseRegister()));

    gcPrintf("Wants Report Only Leaf: %u\n", hdrdecoder.WantsReportOnlyLeaf());
    
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    gcPrintf("Size of parameter area: %x\n", hdrdecoder.GetSizeOfStackParameterArea());
#endif

    UINT32 cbEncodedMethodSize = hdrdecoder.GetCodeLength();
    gcPrintf("Code size: %x\n", cbEncodedMethodSize);

    GcInfoDumper dumper(table);

    GcInfoDumpState state;
    state.LastCodeOffset = -1;
    state.fAnythingPrinted = FALSE;
    state.fSafePoint = FALSE;
    state.FrameRegister = hdrdecoder.GetStackBaseRegister();
    state.pfnPrintf = gcPrintf;

    GcInfoDumper::EnumerateStateChangesResults result = dumper.EnumerateStateChanges(
            &InterruptibleStateChangeCallback,
            &RegisterStateChangeCallback,
            &StackSlotStateChangeCallback,
            &SafePointCallback,
            &state);

    if (state.fAnythingPrinted)
        gcPrintf("\n");

    switch (result)
    {
    case GcInfoDumper::SUCCESS:
        // do nothing
        break;

    case GcInfoDumper::OUT_OF_MEMORY:
        gcPrintf("out of memory\n");
        break;

    case GcInfoDumper::REPORTED_REGISTER_IN_CALLERS_FRAME:
        gcPrintf("reported register in caller's frame\n");
        break;

    case GcInfoDumper::REPORTED_FRAME_POINTER:
        gcPrintf("reported frame register\n");
        break;

    case GcInfoDumper::REPORTED_INVALID_BASE_REGISTER:
        gcPrintf("reported pointer relative to wrong base register\n");
        break;

    case GcInfoDumper::REPORTED_INVALID_POINTER:
        gcPrintf("reported invalid pointer\n");
        break;

    case GcInfoDumper::DECODER_FAILED:
        gcPrintf("decoder failed\n");
        break;

    default:
        gcPrintf("invalid GC info\n");
        break;
    }

    return (result == GcInfoDumper::SUCCESS) ? dumper.GetGCInfoSize() : 0;
}


/*****************************************************************************/

void    GCDump::DumpPtrsInFrame(PTR_CBYTE   infoBlock,
                                PTR_CBYTE   codeBlock,
                                unsigned    offs,
                                bool        verifyGCTables)
{
    _ASSERTE(!"NYI");
}


#define _common_h_

#ifndef LOG
#define LOG(x) ((void)0)
#endif

#define GCINFODECODER_CONTRACT(contract)
#define GET_CALLER_SP(pREGDISPLAY) ((size_t)GetSP(pREGDISPLAY->pCallerContext))
#define VALIDATE_OBJECTREF(objref, fDeep) ((void)0)
#define VALIDATE_ROOT(isInterior, hCallBack, pObjRef) ((void)0)
#include "../vm/gcinfodecoder.cpp"
#include "../gcinfo/gcinfodumper.cpp"
#ifdef VERIFY_GCINFO
#include "../vm/dbggcinfodecoder.cpp"
#endif
