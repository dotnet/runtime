// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              UnwindInfo                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(FEATURE_EH_FUNCLETS)

//------------------------------------------------------------------------
// Compiler::unwindGetFuncLocations: Get the start/end emitter locations for this
// function or funclet. If 'getHotSectionData' is true, get the start/end locations
// for the hot section. Otherwise, get the data for the cold section.
//
// Note that we grab these locations before the prolog and epilogs are generated, so the
// locations must remain correct after the prolog and epilogs are generated.
//
// For the prolog, instructions are put in the special, preallocated, prolog instruction group.
// We don't want to expose the emitPrologIG unnecessarily (locations are actually pointers to
// emitter instruction groups). Since we know the offset of the start of the function/funclet,
// where the prolog is, will be zero, we use a nullptr start location to indicate that.
//
// There is no instruction group beyond the end of the end of the function, so there is no
// location to indicate that. Once again, use nullptr for that.
//
// Intermediate locations point at the first instruction group of a funclet, which is a
// placeholder IG. These are converted to real IGs, not deleted and replaced, so the location
// remains valid.
//
// Arguments:
//    func              - main function or funclet to get locations for.
//    getHotSectionData - 'true' to get the hot section data, 'false' to get the cold section data.
//    ppStartLoc        - OUT parameter. Set to the start emitter location.
//    ppEndLoc          - OUT parameter. Set to the end   emitter location (the location immediately
//                        the range; the 'end' location is not inclusive).
//
// Notes:
//    A start location of nullptr means the beginning of the code.
//    An end location of nullptr means the end of the code.
//
void Compiler::unwindGetFuncLocations(FuncInfoDsc*             func,
                                      bool                     getHotSectionData,
                                      /* OUT */ emitLocation** ppStartLoc,
                                      /* OUT */ emitLocation** ppEndLoc)
{
    if (func->funKind == FUNC_ROOT)
    {
        // Since all funclets are pulled out of line, the main code size is everything
        // up to the first handler. If the function is hot/cold split, we need to get the
        // appropriate sub-range.

        if (getHotSectionData)
        {
            *ppStartLoc = nullptr; // nullptr emit location means the beginning of the code. This is to handle the first
                                   // fragment prolog.

            if (fgFirstColdBlock != nullptr)
            {
                // The hot section only goes up to the cold section
                assert(fgFirstFuncletBB == nullptr);

                *ppEndLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstColdBlock));
            }
            else
            {
                if (fgFirstFuncletBB != nullptr)
                {
                    *ppEndLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstFuncletBB));
                }
                else
                {
                    *ppEndLoc = nullptr; // nullptr end location means the end of the code
                }
            }
        }
        else
        {
            assert(fgFirstFuncletBB == nullptr); // TODO-CQ: support hot/cold splitting in functions with EH
            assert(fgFirstColdBlock != nullptr); // There better be a cold section!

            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(fgFirstColdBlock));
            *ppEndLoc   = nullptr; // nullptr end location means the end of the code
        }
    }
    else
    {
        assert(getHotSectionData); // TODO-CQ: support funclets in cold section

        EHblkDsc* HBtab = ehGetDsc(func->funEHIndex);

        if (func->funKind == FUNC_FILTER)
        {
            assert(HBtab->HasFilter());
            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdFilter));
            *ppEndLoc   = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndBeg));
        }
        else
        {
            assert(func->funKind == FUNC_HANDLER);
            *ppStartLoc = new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndBeg));
            *ppEndLoc   = (HBtab->ebdHndLast->bbNext == nullptr)
                            ? nullptr
                            : new (this, CMK_UnwindInfo) emitLocation(ehEmitCookie(HBtab->ebdHndLast->bbNext));
        }
    }
}

#endif // FEATURE_EH_FUNCLETS

#if defined(TARGET_UNIX)

void Compiler::createCfiCode(FuncInfoDsc* func, UNATIVE_OFFSET codeOffset, UCHAR cfiOpcode, short dwarfReg, INT offset)
{
    noway_assert(static_cast<UCHAR>(codeOffset) == codeOffset);
    CFI_CODE cfiEntry(static_cast<UCHAR>(codeOffset), cfiOpcode, dwarfReg, offset);
    func->cfiCodes->push_back(cfiEntry);
}

void Compiler::unwindPushPopCFI(regNumber reg)
{
    assert(compGeneratingProlog);

    FuncInfoDsc*   func     = funCurrentFunc();
    UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

    regMaskTP relOffsetMask = RBM_CALLEE_SAVED
#if defined(UNIX_AMD64_ABI) && ETW_EBP_FRAMED
                              // In case of ETW_EBP_FRAMED defined the REG_FPBASE (RBP)
                              // is excluded from the callee-save register list.
                              // Make sure the register gets PUSH unwind info in this case,
                              // since it is pushed as a frame register.
                              | RBM_FPBASE
#endif
#if defined(TARGET_ARM)
                              | RBM_R11 | RBM_LR | RBM_PC
#endif
        ;

    if (relOffsetMask & genRegMask(reg))
    {
#ifndef TARGET_ARM
        createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, REGSIZE_BYTES);
#endif
        createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg));
    }
    else
    {
        createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, REGSIZE_BYTES);
    }
}

typedef jitstd::vector<CFI_CODE> CFICodeVector;

void Compiler::unwindBegPrologCFI()
{
    assert(compGeneratingProlog);

#if defined(FEATURE_EH_FUNCLETS)
    FuncInfoDsc* func = funCurrentFunc();

    // There is only one prolog for a function/funclet, and it comes first. So now is
    // a good time to initialize all the unwind data structures.

    unwindGetFuncLocations(func, true, &func->startLoc, &func->endLoc);

    if (fgFirstColdBlock != nullptr)
    {
        unwindGetFuncLocations(func, false, &func->coldStartLoc, &func->coldEndLoc);
    }

    func->cfiCodes = new (getAllocator(CMK_UnwindInfo)) CFICodeVector(getAllocator());
#endif // FEATURE_EH_FUNCLETS
}

void Compiler::unwindPushPopMaskCFI(regMaskTP regMask, bool isFloat)
{
    regMaskTP regBit = isFloat ? genRegMask(REG_FP_FIRST) : 1;

    regNumber regNum = isFloat ? REG_FP_FIRST : REG_FIRST;
    for (; regNum < REG_COUNT;)
    {
        if (regBit > regMask)
        {
            break;
        }

        if (regBit & regMask)
        {
            unwindPushPopCFI(regNum);
        }

#if TARGET_ARM
        // JIT for ARM emit local variables in S0-S31 registers,
        // which cannot be emitted to DWARF when using LLVM,
        // because LLVM only know about D0-D31.
        // As such pairs Sx,Sx+1 are referenced as D0-D15 registers in DWARF
        // For that we process registers in pairs.
        regNum = isFloat ? REG_NEXT(REG_NEXT(regNum)) : REG_NEXT(regNum);
        regBit <<= isFloat ? 2 : 1;
#else
        regNum = REG_NEXT(regNum);
        regBit <<= 1;
#endif
    }
}

void Compiler::unwindAllocStackCFI(unsigned size)
{
    assert(compGeneratingProlog);
    FuncInfoDsc*   func     = funCurrentFunc();
    UNATIVE_OFFSET cbProlog = 0;
    if (compGeneratingProlog)
    {
        cbProlog = unwindGetCurrentOffset(func);
    }
    createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, size);
}

//------------------------------------------------------------------------
// Compiler::unwindSetFrameRegCFI: Record a cfi info for a frame register set.
//
// Arguments:
//    reg    - The register being set as the frame register.
//    offset - The offset from the current stack pointer that the frame pointer will point at.
//
void Compiler::unwindSetFrameRegCFI(regNumber reg, unsigned offset)
{
    assert(compGeneratingProlog);
    FuncInfoDsc*   func     = funCurrentFunc();
    UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

    createCfiCode(func, cbProlog, CFI_DEF_CFA_REGISTER, mapRegNumToDwarfReg(reg));
    if (offset != 0)
    {
        // before: cfa = rsp + old_cfa_offset;
        //         rbp = rsp + offset;
        // after: cfa should be based on rbp, but points to the old address:
        //         rsp + old_cfa_offset == rbp + old_cfa_offset + adjust;
        // adjust = -offset;
        int adjust = -(int)offset;
        createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, adjust);
    }
}

void Compiler::unwindEmitFuncCFI(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;
    DWORD          unwindCodeBytes = 0;
    BYTE*          pUnwindBlock    = nullptr;

    if (func->startLoc == nullptr)
    {
        startOffset = 0;
    }
    else
    {
        startOffset = func->startLoc->CodeOffset(GetEmitter());
    }

    if (func->endLoc == nullptr)
    {
        endOffset = info.compNativeCodeSize;
    }
    else
    {
        endOffset = func->endLoc->CodeOffset(GetEmitter());
    }

    DWORD size = (DWORD)func->cfiCodes->size();
    if (size > 0)
    {
        unwindCodeBytes = size * sizeof(CFI_CODE);
        pUnwindBlock    = (BYTE*)&(*func->cfiCodes)[0];
    }

#ifdef DEBUG
    if (opts.dspUnwind)
    {
        DumpCfiInfo(true /*isHotCode*/, startOffset, endOffset, unwindCodeBytes, (const CFI_CODE* const)pUnwindBlock);
    }
#endif // DEBUG

    assert(endOffset <= info.compTotalHotCodeSize);

    eeAllocUnwindInfo((BYTE*)pHotCode, nullptr /* pColdCode */, startOffset, endOffset, unwindCodeBytes, pUnwindBlock,
                      (CorJitFuncKind)func->funKind);

    if (pColdCode != nullptr)
    {
        assert(fgFirstColdBlock != nullptr);
        assert(func->funKind == FUNC_ROOT); // No splitting of funclets.

        unwindCodeBytes = 0;
        pUnwindBlock    = nullptr;

        if (func->coldStartLoc == nullptr)
        {
            startOffset = 0;
        }
        else
        {
            startOffset = func->coldStartLoc->CodeOffset(GetEmitter());
        }

        if (func->coldEndLoc == nullptr)
        {
            endOffset = info.compNativeCodeSize;
        }
        else
        {
            endOffset = func->coldEndLoc->CodeOffset(GetEmitter());
        }

#ifdef DEBUG
        if (opts.dspUnwind)
        {
            DumpCfiInfo(false /*isHotCode*/, startOffset, endOffset, unwindCodeBytes,
                        (const CFI_CODE* const)pUnwindBlock);
        }
#endif // DEBUG

        assert(startOffset >= info.compTotalHotCodeSize);
        startOffset -= info.compTotalHotCodeSize;
        endOffset -= info.compTotalHotCodeSize;

        eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, unwindCodeBytes, pUnwindBlock,
                          (CorJitFuncKind)func->funKind);
    }
}

#ifdef DEBUG
//------------------------------------------------------------------------
// DumpCfiInfo: Dump the Cfi data.
//
// Arguments:
//    isHotCode   - true if this cfi data is for the hot section, false otherwise.
//    startOffset - byte offset of the code start that this cfi data represents.
//    endOffset   - byte offset of the code end   that this cfi data represents.
//    pcFiCode    - pointer to the cfi data blob.
//
void Compiler::DumpCfiInfo(bool                  isHotCode,
                           UNATIVE_OFFSET        startOffset,
                           UNATIVE_OFFSET        endOffset,
                           DWORD                 cfiCodeBytes,
                           const CFI_CODE* const pCfiCode)
{
    printf("Cfi Info%s:\n", isHotCode ? "" : " COLD");
    printf("  >> Start offset   : 0x%06x \n", dspOffset(startOffset));
    printf("  >>   End offset   : 0x%06x \n", dspOffset(endOffset));

    for (int i = 0; i < (int)(cfiCodeBytes / sizeof(CFI_CODE)); i++)
    {
        const CFI_CODE* const pCode = &(pCfiCode[i]);

        UCHAR codeOffset = pCode->CodeOffset;
        SHORT dwarfReg   = pCode->DwarfReg;
        INT   offset     = pCode->Offset;

        switch (pCode->CfiOpCode)
        {
            case CFI_REL_OFFSET:
                printf("    CodeOffset: 0x%02X Op: RelOffset DwarfReg:0x%x Offset:0x%X\n", codeOffset, dwarfReg,
                       offset);
                break;
            case CFI_DEF_CFA_REGISTER:
                assert(offset == 0);
                printf("    CodeOffset: 0x%02X Op: DefCfaRegister DwarfReg:0x%X\n", codeOffset, dwarfReg);
                break;
            case CFI_ADJUST_CFA_OFFSET:
                assert(dwarfReg == DWARF_REG_ILLEGAL);
                printf("    CodeOffset: 0x%02X Op: AdjustCfaOffset Offset:0x%X\n", codeOffset, offset);
                break;
            default:
                printf("    Unrecognized CFI_CODE: 0x%IX\n", *(UINT64*)pCode);
                break;
        }
    }
}
#endif // DEBUG

#endif // TARGET_UNIX

//------------------------------------------------------------------------
// Compiler::unwindGetCurrentOffset: Calculate the current byte offset of the
// prolog being generated.
//
// Arguments:
//    func - The main function or funclet of interest.
//
// Return Value:
//    The byte offset of the prolog currently being generated.
//
UNATIVE_OFFSET Compiler::unwindGetCurrentOffset(FuncInfoDsc* func)
{
    assert(compGeneratingProlog);
    UNATIVE_OFFSET offset;
    if (func->funKind == FUNC_ROOT)
    {
        offset = GetEmitter()->emitGetPrologOffsetEstimate();
    }
    else
    {
#if defined(TARGET_AMD64) || (defined(TARGET_UNIX) && (defined(TARGET_ARMARCH) || defined(TARGET_X86)))
        assert(func->startLoc != nullptr);
        offset = func->startLoc->GetFuncletPrologOffset(GetEmitter());
#else
        offset = 0; // TODO ???
#endif
    }

    return offset;
}

#if defined(TARGET_AMD64)

// See unwindAmd64.cpp

#elif defined(TARGET_ARM64)

// See unwindArm64.cpp

#elif defined(TARGET_ARM)

// See unwindArm.cpp

#elif defined(TARGET_X86)

// See unwindX86.cpp

#else // TARGET*

#error Unsupported or unset target architecture

#endif // TARGET*
