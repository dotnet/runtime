// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

#ifndef _TARGET_ARM64_
#define _TARGET_ARM64_
#endif

#ifdef _TARGET_AMD64_
#undef _TARGET_AMD64_
#endif

#include "strike.h"
#include "util.h"
#include <dbghelp.h>


#include "disasm.h"

#include "../../../inc/corhdr.h"
#include "../../../inc/cor.h"
#include "../../../inc/dacprivate.h"

namespace ARM64GCDump
{
#undef _TARGET_X86_
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

#ifdef FEATURE_PAL
void SwitchToFiber(void*)
{
    // TODO: Fix for linux
    assert(false);
}
#endif

#if !defined(_TARGET_WIN64_)
#error This file only supports SOS targeting ARM64 from a 64-bit debugger
#endif

#if !defined(SOS_TARGET_ARM64)
#error This file should be used to support SOS targeting ARM64 debuggees
#endif


void ARM64Machine::IsReturnAddress(TADDR retAddr, TADDR* whereCalled) const
{
    *whereCalled = 0;

    DWORD previousInstr;
    move_xp(previousInstr, retAddr - sizeof(previousInstr));

    // ARM64TODO: needs to be implemented for jump stubs for ngen case

    if ((previousInstr & 0xfffffc1f) == 0xd63f0000)
    {
        // BLR <reg>
        *whereCalled = 0xffffffff;
    }
    else if ((previousInstr & 0xfc000000) == 0x94000000)
    {
        // BL <label>
        DWORD imm26 = previousInstr & 0x03ffffff;
        // offset = SignExtend(imm26:'00', 64);
        INT64 offset = ((INT64)imm26 << 38) >> 36;
        *whereCalled = retAddr - 4 + offset;
    }
}

// Determine if a value is MT/MD/Obj
static void HandleValue(TADDR value)
{
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
    // ARM64TODO: not (yet) implemented. perhaps we don't need it at all.
    
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
void ARM64Machine::Unassembly (
    TADDR PCBegin, 
    TADDR PCEnd, 
    TADDR PCAskedFor, 
    TADDR GCStressCodeCopy, 
    GCEncodingInfo *pGCEncodingInfo, 
    SOSEHInfo *pEHInfo,
    BOOL bSuppressLines,
    BOOL bDisplayOffsets) const
{
    TADDR PC = PCBegin;
    char line[1024];
    ULONG lineNum;
    ULONG curLine = -1;
    WCHAR fileName[MAX_LONGPATH];
    char *ptr;
    INT_PTR accumulatedConstant = 0;
    BOOL loBitsSet = FALSE;
    BOOL hiBitsSet = FALSE;
    char *szConstant = NULL;


    while(PC < PCEnd)
    {
        ULONG_PTR currentPC = PC;
        DisasmAndClean (PC, line, _countof(line));

        // This is the closing of the previous run. 
        // Check the next instruction. if it's not a the last movk, handle the accumulated value
        // else simply print a new line.
        if (loBitsSet && hiBitsSet)
        {
            ptr = line;
            // Advance to the instruction encoding
            NextTerm(ptr);
            // Advance to the opcode
            NextTerm(ptr);
            // if it's not movk, handle the accumulated value
            // otherwise simply print the new line. The constant in this expression will be 
            // accumulated below.
            if (strncmp(ptr, "movk ", 5))
            {
                HandleValue(accumulatedConstant);
                accumulatedConstant = 0;
            }
            ExtOut ("\n");
        }
        else if (currentPC != PCBegin)
        {
            ExtOut ("\n");
        }
        
        // This is the new instruction

        if (IsInterrupt())
            return;
        //
        // Print out line numbers if needed
        //
        if (!bSuppressLines && 
            SUCCEEDED(GetLineByOffset(TO_CDADDR(currentPC), &lineNum, fileName, MAX_LONGPATH)))
        {
            if (lineNum != curLine)
            {
                curLine = lineNum;
                ExtOut("\n%S @ %d:\n", fileName, lineNum);
            }
        }

        //
        // Print out any GC information corresponding to the current instruction offset.
        //
        if (pGCEncodingInfo)
        {
            SIZE_T curOffset = (currentPC - PCBegin) + pGCEncodingInfo->hotSizeToAdd;
            while (   !pGCEncodingInfo->fDoneDecoding
                   && pGCEncodingInfo->ofs <= curOffset)
            {
                ExtOut(pGCEncodingInfo->buf);
                ExtOut("\n");
                SwitchToFiber(pGCEncodingInfo->pvGCTableFiber);
            }
        }

        //
        // Print out any EH info corresponding to the current offset
        //
        if (pEHInfo)
        {
            pEHInfo->FormatForDisassembly(currentPC - PCBegin);
        }
        
        if (currentPC == PCAskedFor)
        {
            ExtOut (">>> ");
        }

        //
        // Print offsets, in addition to actual address.
        //
        if (bDisplayOffsets)
        {
            ExtOut("%04x ", currentPC - PCBegin);
        }

        // look at the disassembled bytes
        ptr = line;
        NextTerm (ptr);

        //
        // If there is gcstress info for this method, and this is a 'hlt'
        // instruction, then gcstress probably put the 'hlt' there.  Look
        // up the original instruction and print it instead.
        //        
        

        if (   GCStressCodeCopy
            && (   !strncmp (ptr, "badc0de0", 8)
                || !strncmp (ptr, "badc0de1", 8)
                || !strncmp (ptr, "badc0de2", 8)
                ))
        {
            ULONG_PTR InstrAddr = currentPC;

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

			ExtOut("%08x`%08x ", (ULONG)(InstrAddr >> 32), (ULONG)InstrAddr);

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

        if (!strncmp(ptr, "mov ", 4))
        {
            if ((szConstant = strchr(ptr, '#')) != NULL)
            {
                GetValueFromExpr(szConstant, accumulatedConstant);
                loBitsSet = TRUE;
            }
        }
        else if (!strncmp(ptr, "movk ", 5))
        {
            char *szShiftAmount = NULL;
            INT_PTR shiftAmount = 0;
            INT_PTR constant = 0;
            if (((szShiftAmount = strrchr(ptr, '#')) != NULL) &&
                ((szConstant = strchr(ptr, '#')) != NULL) &&
                (szShiftAmount != szConstant) &&
                (accumulatedConstant > 0)) // Misses when movk is succeeding mov reg, #0x0, which I don't think makes any sense 
            {
                GetValueFromExpr(szShiftAmount, shiftAmount);
                GetValueFromExpr(szConstant, constant);
                accumulatedConstant += (constant<<shiftAmount);
                hiBitsSet = TRUE;
            }
        }
        else 
        {
            accumulatedConstant = 0;
            loBitsSet = hiBitsSet = FALSE;
            if ((szConstant = strchr(ptr, '=')) != NULL)
            {
                // Some instruction fetched a PC-relative constant which the disassembler nicely decoded for
                // us using the ARM convention =<constant>. Retrieve this value and see if it's interesting.
                INT_PTR value;
                GetValueFromExpr(szConstant, value);
                HandleValue(value);
            }


            // ARM64TODO: we could possibly handle adr(p)/ldr pair too.
        }
                
    }
    ExtOut ("\n");
}


// @ARMTODO: Figure out how to extract this information under CoreARM
BOOL ARM64Machine::GetExceptionContext (TADDR stack, TADDR PC, TADDR *cxrAddr, CROSS_PLATFORM_CONTEXT * cxr,
                          TADDR * exrAddr, PEXCEPTION_RECORD exr) const
{
    _ASSERTE("ARM64:NYI");
    return FALSE;
}

///
/// Dump ARM GCInfo table
///
void ARM64Machine::DumpGCInfo(GCInfoToken gcInfoToken, unsigned methodSize, printfFtn gcPrintf, bool encBytes, bool bPrintHeader) const
{
    if (bPrintHeader)
    {
        ExtOut("Pointer table:\n");
    }

    ARM64GCDump::GCDump gcDump(gcInfoToken.Version, encBytes, 5, true);
    gcDump.gcPrintf = gcPrintf;

    gcDump.DumpGCTable(dac_cast<PTR_BYTE>(gcInfoToken.Info), methodSize, 0);
}

