// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

#ifndef _TARGET_ARM_
#define _TARGET_ARM_
#endif


#include "strike.h"
#include "util.h"
#include <dbghelp.h>


#include "disasm.h"

#include "../../../inc/corhdr.h"
#include "../../../inc/cor.h"
#include "../../../inc/dacprivate.h"

#ifndef FEATURE_PAL
namespace ARMGCDump
{
#undef _TARGET_X86_
#define WIN64EXCEPTIONS
#undef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_DAC_CONTRACT
#define SUPPORTS_DAC
#define LF_GCROOTS
#define LL_INFO1000
#define LOG(x)
#define LOG_PIPTR(pObjRef, gcFlags, hCallBack)
#define DAC_ARG(x)
#include "gcdumpnonx86.cpp"
}
#endif // !FEATURE_PAL

#if defined(_TARGET_WIN64_)
#error This file does not support SOS targeting ARM from a 64-bit debugger
#endif

#if !defined(SOS_TARGET_ARM)
#error This file should be used to support SOS targeting ARM debuggees
#endif

#ifdef SOS_TARGET_ARM
ARMMachine ARMMachine::s_ARMMachineInstance;

// Decodes the target label of the immediate form of bl and blx instructions. The PC given is that of the
// start of the instruction.
static TADDR DecodeCallTarget(TADDR PC, WORD rgInstr[2])
{
    // Displacement is spread across several bitfields in the two words of the instruction. Using the same
    // bitfield names as the ARM Architecture Reference Manual.
    DWORD S = (rgInstr[0] & 0x0400) >> 10;
    DWORD imm10 = rgInstr[0] & 0x03ff;
    DWORD J1 = (rgInstr[1] & 0x2000) >> 13;
    DWORD J2 = (rgInstr[1] & 0x0800) >> 11;
    DWORD imm11 = rgInstr[1] & 0x07ff;

    // For reasons that escape me the I1 and I2 fields are computed by XOR'ing J1 and J2 with S.
    DWORD I1 = (~J1 ^ S) & 0x1;
    DWORD I2 = (~J2 ^ S) & 0x1;

    // The final displacement is put together as: SignExtend(S:I1:I2:imm10:imm11:0)
    DWORD highByte = S ? 0xff000000 : 0x00000000;
    DWORD disp = highByte | (I1 << 23) | (I2 << 22) | (imm10 << 12) | (imm11 << 1);

    // The displacement is relative to the PC but the PC for a given instruction reads as the PC for the
    // beginning of the instruction plus 4.
    return PC + 4 + disp;
}

// Validate that a potential call target points to readable memory. If so, and the code appears to be one of
// our standard jump thunks we'll deference through that and return the real target. Returns 0 if any checks
// fail.
static TADDR GetRealCallTarget(TADDR PC)
{
    WORD instr[2];

    // Read the minimum (a WORD) first in case we're calling to a single WORD method at the end of a page
    // (e.g. BLX <reg>).
    if (g_ExtData->ReadVirtual(TO_CDADDR(PC), &instr[0], sizeof(WORD), NULL) != S_OK)
        return 0;

    // All the jump thunks we handle start with the literal form of LDR (i.e. LDR <reg>, [PC +/- <imm>]). It's
    // always the two word form since we're either loading R12 or PC. We never use the decrement version of
    // the instruction (U == 0).
    // If it's not an instruction of that form we can return immediately.
    if (instr[0] != 0xf8df)
        return PC;

    // The first instruction is definitely a LDR of the form we expect so it's OK to read the second half of
    // the encoding.
    if (g_ExtData->ReadVirtual(TO_CDADDR(PC + 2), &instr[1], sizeof(WORD), NULL) != S_OK)
        return 0;

    // Determine which register we're loading. There are three cases:
    //  1) PC: we're jumping, perform final calculation of the jump target
    //  2) R12: we're possibly setting up a special argument to the jump target. Ignore this instruction and
    //          check for a LDR PC in the next instruction
    //  3) Any other register: we don't recognize this instruction sequence, just return the PC we have
    WORD reg = (instr[1] & 0xf000) >> 12;
    if (reg == 12)
    {
        // Possibly a LDR R12, [...]; LDR PC, [...] thunk. Overwrite the current instruction with the next and
        // then fall through into the common LDR PC, [...] handling below. If we fail to read the next word
        // we're not really looking at valid code. But we need to be more careful reading the second word of
        // the potential instruction since there are valid sequences that would terminate with a single word
        // at the end of page.
        if (g_ExtData->ReadVirtual(TO_CDADDR(PC + 4), &instr[0], sizeof(WORD), NULL) != S_OK)
            return 0;

        // Following instruction is not a LDR <literal>. Return this PC as the real target.
        if (instr[0] != 0xf8df)
            return PC;

        // Read second half of the LDR instruction.
        if (g_ExtData->ReadVirtual(TO_CDADDR(PC + 6), &instr[1], sizeof(WORD), NULL) != S_OK)
            return 0;

        // Determine the target register. If it's not the PC then return this PC as the real target.
        reg = (instr[1] & 0xf000) >> 12;
        if (reg != 12)
            return PC;

        // Fall through to process this LDR PC, [...] instruction. Update the input PC because it figures into
        // the calculation below.
        PC += 4;
    }
    else if (reg == 15)
    {
        // First instruction was a LDR PC, [...] Just fall through to common handling below.
    }
    else
    {
        // Any other target register is unrecognized. Just return what we have as the final target.
        return PC;
    }

    // Decode the LDR PC, [PC + <imm>] to find the jump target.
    // The displacement is in the low order 12 bits of the second instruction word.
    DWORD disp = instr[1] & 0x0fff;

    // The PC used for the effective address calculation is the PC from the start of the instruction rounded
    // down to 4-byte alignment then incremented by 4.
    TADDR targetAddress = (PC & ~3) + 4 + disp;

    // Read the target address from this routine.
    TADDR target;
    if (g_ExtData->ReadVirtual(TO_CDADDR(targetAddress), &target, sizeof(target), NULL) != S_OK)
        return 0;

    // Clear the low-bit in the target used to indicate a Thumb mode destination. If this is not set we can't
    // be looking at one of our jump thunks (in fact ARM mode code is illegal under CoreARM so this would
    // indicate an issue).
    _ASSERTE((target & 1) == 1);
    target &= ~1;

    // Recursively call ourselves on this target in case we have any double jump thunks.
    return GetRealCallTarget(target);
}

// Determine (heuristically, basically a best effort guess) whether an address on the stack represents a
// return address. This is achieved by looking at the memory prior to the potential return address and
// disassembling it to see whether it looks like a potential call. If possible the target of the callsite is
// also returned.
//
// Result is returned in whereCalled:
//  0           : retAddr doesn't look like a return address
//  0xffffffff  : retAddr looks like a return address but we couldn't tell where the call site was targeted
//  <other>     : retAddr looks like a return address, *whereCalled set to target address
void ARMMachine::IsReturnAddress(TADDR retAddr, TADDR* whereCalled) const
{
    *whereCalled = 0;

    // If retAddr doesn't have the low-order bit set (indicating a return to Thumb code) then it can't be a
    // legal return address.
    if ((retAddr & 1) == 0)
        return;
    retAddr &= ~1;

    // Potential calling instructions may have been one or two WORDs in length.
    WORD rgPrevious[2];
    move_xp(rgPrevious, retAddr - sizeof(rgPrevious));

    // Check two-word variants first.
    if (((rgPrevious[0] & 0xf800) == 0xf000) &&
        ((rgPrevious[1] & 0xd000) == 0xd000))
    {
        // BL <label>

        // Decode and validate PC-relative call target. Dereference through any jump thunks and return the
        // call target.
        TADDR target = GetRealCallTarget(DecodeCallTarget(retAddr - 4, rgPrevious));
        if (target)
        {
            *whereCalled = target;
            return;
        }
    }
    else if (((rgPrevious[0] & 0xf800) == 0xf000) &&
             ((rgPrevious[1] & 0xd001) == 0xc000))
    {
        // BLX <label>

        // Decode and validate PC-relative call target. Dereference through any jump thunks and return the
        // call target.
        TADDR target = GetRealCallTarget(DecodeCallTarget(retAddr - 4, rgPrevious));
        if (target)
        {
            *whereCalled = target;
            return;
        }
    }
    else if (((rgPrevious[0] & 0xfff0) == 0xf8d0) &&
             ((rgPrevious[1] & 0xf000) == 0xf000))
    {
        // LDR PC, [<reg> + #<imm>]
        *whereCalled = 0xffffffff;
        return;
    }
    else if (((rgPrevious[0] & 0xff7f) == 0xf85f) &&
             ((rgPrevious[1] & 0xf000) == 0xf000))
    {
        // LDR PC, [PC + #<imm>]
        *whereCalled = 0xffffffff;
        return;
    }
    else if (((rgPrevious[0] & 0xfff0) == 0xf850) &&
             ((rgPrevious[1] & 0xffc0) == 0xf000))
    {
        // LDR PC, [<reg> + <reg>, LSL #<imm>]
        *whereCalled = 0xffffffff;
        return;
    }

    // Fall through any failures to decode as a two-word instruction to the one word cases below...

    // BLX <register>
    if ((rgPrevious[1] & 0xff87) == 0x4780)
    {
        *whereCalled = 0xffffffff;
        return;
    }
}


// Return 0 for non-managed call.  Otherwise return MD address.
static TADDR MDForCall (TADDR callee)
{
    // call managed code?
    JITTypes jitType;
    TADDR methodDesc;
    TADDR PC = callee;
    TADDR gcinfoAddr;

    PC = GetRealCallTarget(callee);
    if (!PC)
        return 0;

    IP2MethodDesc (PC, methodDesc, jitType, gcinfoAddr);
    return methodDesc;
}

// Determine if a value is MT/MD/Obj
static void HandleValue(TADDR value)
{
#ifndef FEATURE_PAL
    // remove the thumb bit (if set)
    value = value & ~1;
#else
    // set the thumb bit (if not set)
    value = value | 1;
#endif //!FEATURE_PAL

    // A MethodTable?
    if (IsMethodTable(value))
    {
        NameForMT_s (value, g_mdName,mdNameLen);
        ExtOut (" (MT: %S)", g_mdName);
        return;
    }
    
    // A Managed Object?
    TADDR dwMTAddr;
    move_xp (dwMTAddr, value);
    if (IsStringObject(value))
    {
        ExtOut (" (\"");
        StringObjectContent (value, TRUE);
        ExtOut ("\")");
        return;
    }
    else if (IsMethodTable(dwMTAddr))
    {
        NameForMT_s (dwMTAddr, g_mdName,mdNameLen);
        ExtOut (" (Object: %S)", g_mdName);
        return;
    }
    
    // A MethodDesc?
    if (IsMethodDesc(value))
    {        
        NameForMD_s (value, g_mdName,mdNameLen);
        ExtOut (" (MD: %S)", g_mdName);
        return;
    }

    // A JitHelper?
    const char* name = HelperFuncName(value);
    if (name) {
        ExtOut (" (JitHelp: %s)", name);
        return;
    }

    // A call to managed code?
    TADDR methodDesc = MDForCall(value);
    if (methodDesc)
    {  
        NameForMD_s (methodDesc, g_mdName,mdNameLen);
        ExtOut (" (code for MD: %S)", g_mdName);
        return;
    }
    
    // Random symbol.
    char Symbol[1024];
    if (SUCCEEDED(g_ExtSymbols->GetNameByOffset(TO_CDADDR(value), Symbol, 1024,
                                                NULL, NULL)))
    {
        if (Symbol[0] != '\0')
        {
            ExtOut (" (%s)", Symbol);
            return;
        }
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    Unassembly a managed code.  Translating managed object,           *  
*    call.                                                             *
*                                                                      *
\**********************************************************************/
void ARMMachine::Unassembly (
    TADDR PCBegin, 
    TADDR PCEnd, 
    TADDR PCAskedFor, 
    TADDR GCStressCodeCopy, 
    GCEncodingInfo *pGCEncodingInfo, 
    SOSEHInfo *pEHInfo,
    BOOL bSuppressLines,
    BOOL bDisplayOffsets) const
{
    ULONG_PTR PC = PCBegin;
    char line[1024];
    char *ptr;
    char *valueptr;
    bool fLastWasMovW = false;
    INT_PTR lowbits = 0;
    ULONG curLine = -1;
    WCHAR filename[MAX_LONGPATH];
    ULONG linenum;

    while (PC < PCEnd)
    {
        if (IsInterrupt())
            return;

        // Print out line numbers if needed
        if (!bSuppressLines
            && SUCCEEDED(GetLineByOffset(TO_CDADDR(PC), &linenum, filename, MAX_LONGPATH)))
        {
            if (linenum != curLine)
            {
                curLine = linenum;
                ExtOut("\n%S @ %d:\n", filename, linenum);
            }
        }

#ifndef FEATURE_PAL
        //
        // Print out any GC information corresponding to the current instruction offset.
        //
        if (pGCEncodingInfo)
        {
            SIZE_T curOffset = (PC - PCBegin) + pGCEncodingInfo->hotSizeToAdd;
            while (   !pGCEncodingInfo->fDoneDecoding
                   && pGCEncodingInfo->ofs <= curOffset)
            {
                ExtOut(pGCEncodingInfo->buf);
                ExtOut("\n");
                SwitchToFiber(pGCEncodingInfo->pvGCTableFiber);
            }
        }
#endif //!FEATURE_PAL
        //
        // Print out any EH info corresponding to the current offset
        //
        if (pEHInfo)
        {
            pEHInfo->FormatForDisassembly(PC - PCBegin);
        }
        
        if ((PC & ~1) == (PCAskedFor & ~1))
        {
            ExtOut (">>> ");
        }
        
        //
        // Print offsets, in addition to actual address.
        //
        if (bDisplayOffsets)
        {
            ExtOut("%04x ", PC - PCBegin);
        }

        ULONG_PTR prevPC = PC;
        DisasmAndClean (PC, line, _countof(line));

        // look at the disassembled bytes
        ptr = line;
        NextTerm (ptr);

        //
        // If there is gcstress info for this method, and this is a 'hlt'
        // instruction, then gcstress probably put the 'hlt' there.  Look
        // up the original instruction and print it instead.
        //        
        

        if (   GCStressCodeCopy
            && (   !strncmp (ptr, "de00 ", 5)
                || !strncmp (ptr, "de01 ", 5)
                || !strncmp (ptr, "de02 ", 5)
                || !strncmp (ptr, "f7f0a001", 8)
                || !strncmp (ptr, "f7f0a002", 8)
                || !strncmp (ptr, "f7f0a003", 8)
                ))
        {
            ULONG_PTR InstrAddr = prevPC;

            //
            // Compute address into saved copy of the code, and
            // disassemble the original instruction
            //
            
            ULONG_PTR OrigInstrAddr = GCStressCodeCopy + (InstrAddr - PCBegin);
            ULONG_PTR OrigPC = OrigInstrAddr;

            DisasmAndClean(OrigPC, line, _countof(line));

            //
            // Increment the real PC based on the size of the unmodifed
            // instruction
            //

            PC = InstrAddr + (OrigPC - OrigInstrAddr);

            //
            // Print out real code address in place of the copy address
            //

            ExtOut("%08x ", (ULONG)InstrAddr);

            ptr = line;
            NextTerm (ptr);

            //
            // Print out everything after the code address, and skip the
            // instruction bytes
            //

            ExtOut(ptr);

            //
            // Add an indicator that this address has not executed yet
            //

            ExtOut(" (gcstress)");
        }
        else
        {
            ExtOut (line);
        }

        // Now advance to the opcode
        NextTerm (ptr);
    
        if (!strncmp (ptr, "movw ", 5) || !strncmp (ptr, "mov ", 4))
        {
            // Possibly the loading the low-order 16-bits of a 32-bit constant. Cache the value in case the
            // next instruction is a movt with the high-order bits.
            if ((valueptr = strchr(ptr, '#')) != NULL)
            {
                GetValueFromExpr(valueptr, lowbits);
                fLastWasMovW = true;
            }
        }
        else
        {
            if (!strncmp (ptr, "movt ", 5) && fLastWasMovW)
            {
                // A movt following a movw (if we were being really careful we'd check that the destination
                // register was the same in both cases). Assemble the two 16-bit immediate values from both
                // instructions and see if the resultant constant is interesting.
                if ((valueptr = strchr(ptr, '#')) != NULL)
                {
                    INT_PTR highbits;
                    GetValueFromExpr(valueptr, highbits);
                    HandleValue((highbits << 16) | lowbits);
                }
            }
            else if ((valueptr = strchr(ptr, '=')) != NULL)
            {
                // Some instruction fetched a PC-relative constant which the disassembler nicely decoded for
                // us using the ARM convention =<constant>. Retrieve this value and see if it's interesting.
                INT_PTR value;
                GetValueFromExpr(valueptr, value);
                HandleValue(value);
            }

            fLastWasMovW = false;
        }

        ExtOut ("\n");
    }
}

#if 0 // @ARMTODO: Figure out how to extract this information under CoreARM
static void ExpFuncStateInit (TADDR *PCRetAddr)
{
    ULONG64 offset;
    if (FAILED(g_ExtSymbols->GetOffsetByName("ntdll!KiUserExceptionDispatcher", &offset))) {
        return;
    }
    char            line[256];
    int i = 0;
    while (i < 3) {
        g_ExtControl->Disassemble (offset, 0, line, 256, NULL, &offset);
        if (strstr (line, "call")) {
            PCRetAddr[i++] = (TADDR)offset;
        }
    }
}
#endif // 0


// @ARMTODO: Figure out how to extract this information under CoreARM
BOOL ARMMachine::GetExceptionContext (TADDR stack, TADDR PC, TADDR *cxrAddr, CROSS_PLATFORM_CONTEXT * cxr,
                          TADDR * exrAddr, PEXCEPTION_RECORD exr) const
{
    return FALSE;
#if 0 // @ARMTODO: Figure out how to extract this information under CoreARM
    static TADDR PCRetAddr[3] = {0,0,0};

    if (PCRetAddr[0] == 0) {
        ExpFuncStateInit (PCRetAddr);
    }
    *cxrAddr = 0;
    *exrAddr = 0;
    if (PC == PCRetAddr[0]) {
        *exrAddr = stack + sizeof(TADDR);
        *cxrAddr = stack + 2*sizeof(TADDR);
    }
    else if (PC == PCRetAddr[1]) {
        *cxrAddr = stack + sizeof(TADDR);
    }
    else if (PC == PCRetAddr[2]) {
        *exrAddr = stack + sizeof(TADDR);
        *cxrAddr = stack + 2*sizeof(TADDR);
    }
    else
        return FALSE;

    if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*cxrAddr), &stack, sizeof(stack), NULL)))
        return FALSE;
    *cxrAddr = stack;

    if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(stack), cxr, sizeof(DT_CONTEXT), NULL))) {
        return FALSE;
    }

    if (*exrAddr) {
        if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(*exrAddr), &stack, sizeof(stack), NULL)))
        {
            *exrAddr = 0;
            return TRUE;
        }
        *exrAddr = stack;
        size_t erSize = offsetof (EXCEPTION_RECORD, ExceptionInformation);
        if (FAILED (g_ExtData->ReadVirtual(TO_CDADDR(stack), exr, erSize, NULL))) {
            *exrAddr = 0;
            return TRUE;
        }
    }
    return TRUE;
#endif // 0
}


///
/// Dump ARM GCInfo table
///
void ARMMachine::DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const
{
#ifndef FEATURE_PAL
    if (bPrintHeader)
    {
        ExtOut("Pointer table:\n");
    }

    ARMGCDump::GCDump gcDump(gcInfoToken.Version, encBytes, 5, true);
    gcDump.gcPrintf = gcPrintf;

    gcDump.DumpGCTable(dac_cast<PTR_BYTE>(gcInfoToken.Info), methodSize, 0);
#endif // !FEATURE_PAL
}

#endif // SOS_TARGET_ARM
