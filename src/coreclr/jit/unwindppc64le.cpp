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

#if defined(TARGET_POWERPC64) && defined(FEATURE_CFI_SUPPORT)
short Compiler::mapRegNumToDwarfReg(regNumber reg)
{
    _ASSERTE(!"NYI");
}
#endif // TARGET_POWERPC64 && FEATURE_CFI_SUPPORT

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind APIs                                                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void Compiler::unwindBegProlog()
{
    assert(compGeneratingProlog);
    assert(!compGeneratingUnwindProlog);
    compGeneratingUnwindProlog = true;

    FuncInfoDsc* func = funCurrentFunc();

    // There is only one prolog for a function/funclet, and it comes first. So now is
    // a good time to initialize all the unwind data structures.

    emitLocation* startLoc;
    emitLocation* endLoc;
    unwindGetFuncLocations(func, true, &startLoc, &endLoc);

    func->uwi.InitUnwindInfo(this, startLoc, endLoc);
    func->uwi.CaptureLocation();

    func->uwiCold = NULL; // No cold data yet
}

void Compiler::unwindEndProlog()
{
    assert(compGeneratingProlog);
    assert(compGeneratingUnwindProlog);
    compGeneratingUnwindProlog = false;
}

void Compiler::unwindBegEpilog()
{
    assert(compGeneratingEpilog);
    assert(!compGeneratingUnwindEpilog);
    compGeneratingUnwindEpilog = true;

    funCurrentFunc()->uwi.AddEpilog();
}

void Compiler::unwindEndEpilog()
{
    assert(compGeneratingEpilog);
    assert(compGeneratingUnwindEpilog);
    compGeneratingUnwindEpilog = false;
}

#if defined(TARGET_POWERPC64)

void Compiler::unwindAllocStack(unsigned size)
{
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
}

void Compiler::unwindSetFrameReg(regNumber reg, unsigned offset)
{
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    //_ASSERTE(!"NYI");
    //unreached();
    //TODO: JK, no-op for minimal frameless bring-up
}

// The instructions between the last captured "current state" and the current instruction
// are in the prolog but have no effect for unwinding. Emit the appropriate NOP unwind codes
// for them.
void Compiler::unwindPadding()
{
    // TODO-PPC64: Implement unwind padding for PPC64LE
    // For now, this is a no-op similar to other unwind functions
}

#endif // defined(TARGET_POWERPC64)

// Ask the VM to reserve space for the unwind information for the function and
// all its funclets.

void Compiler::unwindReserve()
{
    assert(!compGeneratingProlog);
    assert(!compGeneratingEpilog);

    assert(compFuncInfoCount > 0);
    for (unsigned funcIdx = 0; funcIdx < compFuncInfoCount; funcIdx++)
    {
        unwindReserveFunc(funGetFunc(funcIdx));
    }
}

void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
    BOOL isFunclet          = (func->funKind == FUNC_ROOT) ? FALSE : TRUE;
    bool funcHasColdSection = (fgFirstColdBlock != nullptr);

#ifdef DEBUG
    if (JitConfig.JitFakeProcedureSplitting() && funcHasColdSection)
    {
        funcHasColdSection = false; // "Trick" the VM into thinking we don't have a cold section.
    }
#endif // DEBUG

    // If hot/cold splitting occurred at fgFirstFuncletBB, then the main body is not split.
    const bool splitAtFirstFunclet = (funcHasColdSection && (fgFirstColdBlock == fgFirstFuncletBB));

    if (!isFunclet && splitAtFirstFunclet)
    {
        funcHasColdSection = false;
    }

    // If there is cold code, split the unwind data between the hot section and the
    // cold section. This needs to be done before we split into fragments, as each
    // of the hot and cold sections can have multiple fragments.

    if (funcHasColdSection)
    {
        emitLocation* startLoc;
        emitLocation* endLoc;
        unwindGetFuncLocations(func, false, &startLoc, &endLoc);

        func->uwiCold = new (this, CMK_UnwindInfo) UnwindInfo();
        func->uwiCold->InitUnwindInfo(this, startLoc, endLoc);
        func->uwiCold->HotColdSplitCodes(&func->uwi);
    }

    // First we need to split the function or funclet into fragments that are no larger
    // than 512K, so the fragment size will fit in the unwind data "Function Length" field.
    // The PPC64LE Exception Data specification "Function Fragments" section describes this.
    func->uwi.Split();

    // If the function is split, EH funclets are always cold; skip this call for cold funclets.
    if (!isFunclet || !funcHasColdSection)
    {
        func->uwi.Reserve(isFunclet, true);
    }

    // After the hot section, split and reserve the cold section

    if (funcHasColdSection)
    {
        assert(func->uwiCold != NULL);

        func->uwiCold->Split();
        func->uwiCold->Reserve(isFunclet, false);
    }
}

// unwindEmit: Report all the unwind information to the VM.
// Arguments:
//      pHotCode:  Pointer to the beginning of the memory with the function and funclet hot  code
//      pColdCode: Pointer to the beginning of the memory with the function and funclet cold code.

void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
    assert(compFuncInfoCount > 0);
    for (unsigned funcIdx = 0; funcIdx < compFuncInfoCount; funcIdx++)
    {
        unwindEmitFunc(funGetFunc(funcIdx), pHotCode, pColdCode);
    }
}

void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    // Verify that the JIT enum is in sync with the JIT-EE interface enum
    static_assert_no_msg(FUNC_ROOT == (FuncKind)CORJIT_FUNC_ROOT);
    static_assert_no_msg(FUNC_HANDLER == (FuncKind)CORJIT_FUNC_HANDLER);
    static_assert_no_msg(FUNC_FILTER == (FuncKind)CORJIT_FUNC_FILTER);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        // TODO: Support cold EH funclets.
        unwindEmitFuncCFI(func, pHotCode, pColdCode);
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // If the function is split, EH funclets are always cold; skip this call for cold funclets.
    if ((func->funKind == FUNC_ROOT) || (func->uwiCold == NULL))
    {
        func->uwi.Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, true);
    }

    if (func->uwiCold != NULL)
    {
        func->uwiCold->Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, false);
    }
}

#if defined(TARGET_POWERPC64)

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Debug helpers                                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

// Return the opcode size of an instruction, in bytes, given the first byte of
// its corresponding unwind code.

// Return the size of the unwind code (from 1 to 4 bytes), given the first byte of the unwind bytes
// PPC64LE uses ARM64-style unwind codes with similar encoding

unsigned GetUnwindSizeFromUnwindHeader(BYTE b1)
{
    static const BYTE s_UnwindSize[256] = {
        // array of unwind sizes, in bytes (based on ARM64/RISC-V64 encoding)
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 00-0F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 10-1F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 20-2F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 30-3F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 40-4F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 50-5F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 60-6F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 70-7F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 80-8F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 90-9F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // A0-AF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // B0-BF
        2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 3, 2, 2, 2, // C0-CF
        3, 2, 2, 2, 2, 2, 3, 2, 3, 2, 3, 2, 3, 3, 2, 1, // D0-DF
        4, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // E0-EF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  // F0-FF
    };

    unsigned size = s_UnwindSize[b1];
    assert(1 <= size && size <= 4);
    return size;
}

#endif // DEBUG

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Support Classes                                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindCodesBase
//
///////////////////////////////////////////////////////////////////////////////

#ifdef DEBUG

// Walk the prolog codes and calculate the size of the prolog or epilog, in bytes.
// All PPC64LE unwind codes represent 4-byte instructions.

unsigned UnwindCodesBase::GetCodeSizeFromUnwindCodes(bool isProlog)
{
    BYTE*    pCodesStart = GetCodes();
    BYTE*    pCodes      = pCodesStart;
    unsigned size        = 0;
    for (;;)
    {
        BYTE b1 = *pCodes;
        if (IsEndCode(b1))
        {
            break; // We hit an "end" code; we're done
        }
        size += 4; // All codes represent 4 byte instructions.
        pCodes += GetUnwindSizeFromUnwindHeader(b1);
        assert(pCodes - pCodesStart < 256); // 255 is the absolute maximum number of code bytes allowed
    }
    return size;
}

#endif // DEBUG

#endif // defined(TARGET_POWERPC64)

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindPrologCodes
//
/////////////////////////////////////////////////////////////////////////////

// We're going to use the prolog codes memory to store the final unwind data.
// Ensure we have enough memory to store everything. If 'epilogBytes' > 0, then
// move the prolog codes so there are 'epilogBytes' bytes after the prolog codes.
// Set the header pointer for future use, adding the header bytes (this pointer
// is updated when a header byte is added), and remember the index that points
// to the beginning of the header.

void UnwindPrologCodes::SetFinalSize(int headerBytes, int epilogBytes)
{
#ifdef DEBUG
    // We're done adding codes. Check that we didn't accidentally create a bigger prolog.
    unsigned codeSize = GetCodeSizeFromUnwindCodes(true);
    assert(codeSize <= MAX_PROLOG_SIZE_BYTES);
#endif // DEBUG

    int prologBytes = Size();

    EnsureSize(headerBytes + prologBytes + epilogBytes + 3); // 3 = padding bytes for alignment

    upcUnwindBlockSlot = upcCodeSlot - headerBytes - epilogBytes; // Index of the first byte of the unwind header

    assert(upcMemSize == upcUnwindBlockSlot + headerBytes + prologBytes + epilogBytes + 3);

    upcHeaderSlot = upcUnwindBlockSlot - 1; // upcHeaderSlot is always incremented before storing
    assert(upcHeaderSlot >= -1);

    if (epilogBytes > 0)
    {
        // The prolog codes that are already at the end of the array need to get moved to the middle,
        // with space for the non-matching epilog codes to follow.

        memmove_s(&upcMem[upcUnwindBlockSlot + headerBytes], upcMemSize - (upcUnwindBlockSlot + headerBytes),
                  &upcMem[upcCodeSlot], prologBytes);

        // Note that the three UWC_END padding bytes still exist at the end of the array.

#ifdef DEBUG
        // Zero out the epilog codes memory, to ensure we've copied the right bytes. Don't zero the padding bytes.
        memset(&upcMem[upcUnwindBlockSlot + headerBytes + prologBytes], 0, epilogBytes);
#endif // DEBUG

        upcEpilogSlot =
            upcUnwindBlockSlot + headerBytes + prologBytes; // upcEpilogSlot points to the next epilog location to fill

        // Update upcCodeSlot to point at the new beginning of the prolog codes
        upcCodeSlot = upcUnwindBlockSlot + headerBytes;
    }
}

// Add a header word. Header words are added starting at the beginning, in order: first to last.
// This is in contrast to the prolog unwind codes, which are added in reverse order.

void UnwindPrologCodes::AddHeaderWord(DWORD d)
{
    assert(-1 <= upcHeaderSlot);
    assert(upcHeaderSlot + 4 < upcCodeSlot); // Don't collide with the unwind codes that are already there!

    // Store it byte-by-byte in little-endian format. We've already ensured there is enough space
    // in SetFinalSize().
    upcMem[++upcHeaderSlot] = (BYTE)d;
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 8);
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 16);
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 24);
}

// AppendEpilog: copy the epilog bytes to the next epilog bytes slot

void UnwindPrologCodes::AppendEpilog(UnwindEpilogInfo* pEpi)
{
    assert(upcEpilogSlot != -1);

    int epiSize = pEpi->Size();
    memcpy_s(&upcMem[upcEpilogSlot], upcMemSize - upcEpilogSlot - 3, pEpi->GetCodes(),
             epiSize);                                            // -3 to avoid writing to the alignment padding
    assert(pEpi->GetStartIndex() == upcEpilogSlot - upcCodeSlot); // Make sure we copied it where we expected to copy
                                                                  // it.

    upcEpilogSlot += epiSize;
    assert(upcEpilogSlot <= upcMemSize - 3);
}

// GetFinalInfo: return a pointer to the final unwind info to hand to the VM, and the size of this info in bytes

void UnwindPrologCodes::GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize)
{
    assert(upcHeaderSlot + 1 == upcCodeSlot); // We better have filled in the header before asking for the final data!

    *ppUnwindBlock = &upcMem[upcUnwindBlockSlot];

    // We put 4 'end' codes at the end for padding, so we can ensure we have an
    // unwind block that is a multiple of 4 bytes in size. Subtract off three 'end'
    // codes (leave one), and then align the size up to a multiple of 4.
    *pUnwindBlockSize = AlignUp((UINT)(upcMemSize - upcUnwindBlockSlot - 3), sizeof(DWORD));
}

// Do the argument unwind codes match our unwind codes?
// If they don't match, return -1. If they do, return the offset into
// our codes at which they match. Note that this means that the
// argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.
//
// This is similar to UnwindEpilogInfo::Match().
//

// Do the argument unwind codes match our unwind codes?
// If they don't match, return -1. If they do, return the offset into
// our codes at which they match. Note that this means that the
// argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.

int UnwindPrologCodes::Match(UnwindEpilogInfo* pEpi)
{
    if (Size() < pEpi->Size())
    {
        return -1;
    }

    int matchIndex = Size() - pEpi->Size();

    if (0 == memcmp(GetCodes() + matchIndex, pEpi->GetCodes(), pEpi->Size()))
    {
        return matchIndex;
    }

    return -1;
}

// Copy the prolog codes from another prolog. The only time this is legal is
// if we are at the initial state and no prolog codes have been added.
// This is used to create the 'phantom' prolog for non-first fragments.

void UnwindPrologCodes::CopyFrom(UnwindPrologCodes* pCopyFrom)
{
    assert(uwiComp == pCopyFrom->uwiComp);
    assert(upcMem == upcMemLocal);
    assert(upcMemSize == UPC_LOCAL_COUNT);
    assert(upcHeaderSlot == -1);
    assert(upcEpilogSlot == -1);

    // Copy the codes
    EnsureSize(pCopyFrom->upcMemSize);
    assert(upcMemSize == pCopyFrom->upcMemSize);
    memcpy_s(upcMem, upcMemSize, pCopyFrom->upcMem, pCopyFrom->upcMemSize);

    // Copy the other data
    upcCodeSlot        = pCopyFrom->upcCodeSlot;
    upcHeaderSlot      = pCopyFrom->upcHeaderSlot;
    upcEpilogSlot      = pCopyFrom->upcEpilogSlot;
    upcUnwindBlockSlot = pCopyFrom->upcUnwindBlockSlot;
}

void UnwindPrologCodes::EnsureSize(int requiredSize)
{
    if (requiredSize > upcMemSize)
    {
        // Reallocate, and copy everything to a new array.

        // Choose the next power of two size. This may or may not be the best choice.
        noway_assert((requiredSize & 0xC0000000) == 0); // too big!
        int newSize;
        for (newSize = upcMemSize << 1; newSize < requiredSize; newSize <<= 1)
        {
            // do nothing
        }

        BYTE* newUnwindCodes = new (uwiComp, CMK_UnwindInfo) BYTE[newSize];
        memcpy_s(newUnwindCodes + newSize - upcMemSize, upcMemSize, upcMem,
                 upcMemSize); // copy the existing data to the end
#ifdef DEBUG
        // Clear the old unwind codes; nobody should be looking at them
        memset(upcMem, 0xFF, upcMemSize);
#endif                           // DEBUG
        upcMem = newUnwindCodes; // we don't free anything that used to be there since we have a no-release allocator
        upcCodeSlot += newSize - upcMemSize;
        upcMemSize = newSize;
    }
}

#ifdef DEBUG
void UnwindPrologCodes::Dump(int indent)
{
    printf("%*sUnwindPrologCodes @0x%08p, size:%d:\n", indent, "", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  &upcMemLocal[0]: 0x%08p\n", indent, "", dspPtr(&upcMemLocal[0]));
    printf("%*s  upcMem: 0x%08p\n", indent, "", dspPtr(upcMem));
    printf("%*s  upcMemSize: %d\n", indent, "", upcMemSize);
    printf("%*s  upcCodeSlot: %d\n", indent, "", upcCodeSlot);
    printf("%*s  upcHeaderSlot: %d\n", indent, "", upcHeaderSlot);
    printf("%*s  upcEpilogSlot: %d\n", indent, "", upcEpilogSlot);
    printf("%*s  upcUnwindBlockSlot: %d\n", indent, "", upcUnwindBlockSlot);

    if (upcMemSize > 0)
    {
        printf("%*s  codes:", indent, "");
        for (int i = 0; i < upcMemSize; i++)
        {
            printf(" %02x", upcMem[i]);
            if (i == upcCodeSlot)
            {
                printf(" <-C");
            }
            else if (i == upcHeaderSlot)
            {
                printf(" <-H");
            }
            else if (i == upcEpilogSlot)
            {
                printf(" <-E");
            }
            else if (i == upcUnwindBlockSlot)
            {
                printf(" <-U");
            }
        }
        printf("\n");
    }
}
#endif // DEBUG

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogCodes
//
/////////////////////////////////////////////////////////////////////////////

void UnwindEpilogCodes::EnsureSize(int requiredSize)
{
    _ASSERTE(!"NYI");
}

#ifdef DEBUG
void UnwindEpilogCodes::Dump(int indent)
{
    _ASSERTE(!"NYI");
}
#endif // DEBUG

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogInfo
//
/////////////////////////////////////////////////////////////////////////////

// Do the current unwind codes match those of the argument epilog?
// If they don't match, return -1. If they do, return the offset into
// our codes at which the argument codes match. Note that this means that
// the argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.
//
// Note that if we wanted to handle 0xFD and 0xFE codes, by converting
// an existing 0xFF code to one of those, we might do that here.

int UnwindEpilogInfo::Match(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"NYI");
}

void UnwindEpilogInfo::CaptureEmitLocation()
{
    _ASSERTE(!"NYI");
}

void UnwindEpilogInfo::FinalizeOffset()
{
    _ASSERTE(!"NYI");
    //epiStartOffset = epiEmitLocation->CodeOffset(uwiComp->GetEmitter());
}

#ifdef DEBUG
void UnwindEpilogInfo::Dump(int indent)
{
    _ASSERTE(!"NYI");
}
#endif // DEBUG

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindFragmentInfo
//
/////////////////////////////////////////////////////////////////////////////

UnwindFragmentInfo::UnwindFragmentInfo(Compiler* comp, emitLocation* emitLoc, bool hasPhantomProlog)
	    : UnwindBase(comp)
          , ufiNext(NULL)
          , ufiEmitLoc(emitLoc)
          , ufiHasPhantomProlog(hasPhantomProlog)
          , ufiPrologCodes(comp)
          , ufiEpilogFirst(comp)
          , ufiEpilogList(NULL)
          , ufiEpilogLast(NULL)
          , ufiCurCodes(&ufiPrologCodes)
          , ufiSize(0)
          , ufiStartOffset(UFI_ILLEGAL_OFFSET)
{
#ifdef DEBUG
    ufiNum         = 1;
    ufiInProlog    = true;
    ufiInitialized = UFI_INITIALIZED_PATTERN;
#endif // DEBUG
}

void UnwindFragmentInfo::FinalizeOffset()
{
    if (ufiEmitLoc == NULL)
    {
        // NULL emit location means the beginning of the code. This is to handle the first fragment prolog.
        ufiStartOffset = 0;
    }
    else
    {
        ufiStartOffset = ufiEmitLoc->CodeOffset(uwiComp->GetEmitter());
    }

    for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        pEpi->FinalizeOffset();
    }
}

void UnwindFragmentInfo::AddEpilog()
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

#ifdef DEBUG
    if (ufiInProlog)
    {
        assert(ufiEpilogList == NULL);
        ufiInProlog = false;
    }
    else
    {
        assert(ufiEpilogList != NULL);
    }
#endif // DEBUG

    // Either allocate a new epilog object, or, for the first one, use the
    // preallocated one that is a member of the UnwindFragmentInfo class.

    UnwindEpilogInfo* newepi;

    if (ufiEpilogList == NULL)
    {
        // Use the epilog that's in the class already. Be sure to initialize it!
        newepi = ufiEpilogList = &ufiEpilogFirst;
    }
    else
    {
        newepi = new (uwiComp, CMK_UnwindInfo) UnwindEpilogInfo(uwiComp);
    }

    // Put the new epilog at the end of the epilog list

    if (ufiEpilogLast != NULL)
    {
        ufiEpilogLast->epiNext = newepi;
    }

    ufiEpilogLast = newepi;

    // What is the starting code offset of the epilog? Store an emitter location
    // so we can ask the emitter later, after codegen.

    newepi->CaptureEmitLocation();

    // Put subsequent unwind codes in this new epilog

    ufiCurCodes = &newepi->epiCodes;
}

// Copy the prolog codes from the 'pCopyFrom' fragment. These prolog codes will
// become 'phantom' prolog codes in this fragment. Note that this fragment should
// not have any prolog codes currently; it is at the initial state.

void UnwindFragmentInfo::CopyPrologCodes(UnwindFragmentInfo* pCopyFrom)
{
    ufiPrologCodes.CopyFrom(&pCopyFrom->ufiPrologCodes);
    // PPC64LE doesn't need UWC_END_C like ARM64
}

// Split the epilog codes that currently exist in 'pSplitFrom'. The ones that represent
// epilogs that start at or after the location represented by 'emitLoc' are removed
// from 'pSplitFrom' and moved to this fragment. Note that this fragment should not have
// any epilog codes currently; it is at the initial state.

void UnwindFragmentInfo::SplitEpilogCodes(emitLocation* emitLoc, UnwindFragmentInfo* pSplitFrom)
{
    UnwindEpilogInfo* pEpiPrev;
    UnwindEpilogInfo* pEpi;

    UNATIVE_OFFSET splitOffset = emitLoc->CodeOffset(uwiComp->GetEmitter());

    for (pEpiPrev = NULL, pEpi = pSplitFrom->ufiEpilogList; pEpi != NULL; pEpiPrev = pEpi, pEpi = pEpi->epiNext)
    {
        pEpi->FinalizeOffset(); // Get the offset of the epilog from the emitter so we can compare it
        if (pEpi->GetStartOffset() >= splitOffset)
        {
            // This epilog and all following epilogs, which must be in order of increasing offsets,
            // get moved to this fragment.

            // Splice in the epilogs to this fragment. Set the head of the epilog
            // list to this epilog.
            ufiEpilogList = pEpi; // In this case, don't use 'ufiEpilogFirst'
            ufiEpilogLast = pSplitFrom->ufiEpilogLast;

            // Splice out the tail of the list from the 'pSplitFrom' epilog list
            pSplitFrom->ufiEpilogLast = pEpiPrev;
            if (pSplitFrom->ufiEpilogLast == NULL)
            {
                pSplitFrom->ufiEpilogList = NULL;
            }
            else
            {
                pSplitFrom->ufiEpilogLast->epiNext = NULL;
            }

            // No more codes should be added once we start splitting
            pSplitFrom->ufiCurCodes = NULL;
            ufiCurCodes             = NULL;

            break;
        }
    }
}

// Is this epilog at the end of an unwind fragment? Ask the emitter.
// Note that we need to know this before all code offsets are finalized,
// so we can determine whether we can omit an epilog scope word for a
// single matching epilog.

bool UnwindFragmentInfo::IsAtFragmentEnd(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"NYI");
    //return uwiComp->GetEmitter()->emitIsFuncEnd(pEpi->epiEmitLocation, (ufiNext == NULL) ? NULL : ufiNext->ufiEmitLoc);
}

// Merge the unwind codes as much as possible.
// This function is called before all offsets are final.
// Also, compute the size of the final unwind block. Store this
// and some other data for later, when we actually emit the
// unwind block.

void UnwindFragmentInfo::MergeCodes()
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

    unsigned epilogCount     = 0;
    unsigned epilogCodeBytes = 0;
    unsigned epilogIndex = ufiPrologCodes.Size();
    UnwindEpilogInfo* pEpi;

    for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        ++epilogCount;

        pEpi->FinalizeCodes();

        int matchIndex;

        matchIndex = ufiPrologCodes.Match(pEpi);
        if (matchIndex != -1)
        {
            pEpi->SetMatches();
            pEpi->SetStartIndex(matchIndex);
        }
        else
        {
            bool matched = false;
            for (UnwindEpilogInfo* pEpi2 = ufiEpilogList; pEpi2 != pEpi; pEpi2 = pEpi2->epiNext)
            {
                matchIndex = pEpi2->Match(pEpi);
                if (matchIndex != -1)
                {
                    pEpi->SetMatches();
                    pEpi->SetStartIndex(pEpi2->GetStartIndex() + matchIndex);
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                pEpi->SetStartIndex(epilogIndex);
                epilogCodeBytes += pEpi->Size();
                epilogIndex += pEpi->Size();
            }
        }
    }

    DWORD codeBytes = ufiPrologCodes.Size() + epilogCodeBytes;
    codeBytes       = AlignUp(codeBytes, sizeof(DWORD));

    DWORD codeWords = codeBytes / sizeof(DWORD);

    bool needExtendedCodeWordsEpilogCount =
        (codeWords > UW_MAX_CODE_WORDS_COUNT) || (epilogCount > UW_MAX_EPILOG_COUNT);

    bool     setEBit      = false;
    unsigned epilogScopes = epilogCount;

    if (epilogCount == 1)
    {
        assert(ufiEpilogList != NULL);
        assert(ufiEpilogList->epiNext == NULL);

        if (ufiEpilogList->Matches() && (ufiEpilogList->GetStartIndex() == 0) &&
            !needExtendedCodeWordsEpilogCount && IsAtFragmentEnd(ufiEpilogList))
        {
            epilogScopes = 0;
            setEBit      = true;
        }
    }

    DWORD headerBytes = (1 + (needExtendedCodeWordsEpilogCount ? 1 : 0) + epilogScopes) * sizeof(DWORD);

    DWORD finalSize = headerBytes + codeBytes;

    ufiPrologCodes.SetFinalSize(headerBytes, epilogCodeBytes);

    if (epilogCodeBytes != 0)
    {
        for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
            if (!pEpi->Matches())
            {
                ufiPrologCodes.AppendEpilog(pEpi);
            }
        }
    }

    ufiSize                             = finalSize;
    ufiSetEBit                          = setEBit;
    ufiNeedExtendedCodeWordsEpilogCount = needExtendedCodeWordsEpilogCount;
    ufiCodeWords                        = codeWords;
    ufiEpilogScopes                     = epilogScopes;
}

// Finalize: Prepare the unwind information for the VM. Compute and prepend the unwind header.

void UnwindFragmentInfo::Finalize(UNATIVE_OFFSET functionLength)
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

#ifdef DEBUG
    if (0 && uwiComp->verbose)
    {
        printf("*************** Before fragment #%d finalize\n", ufiNum);
        Dump();
    }
#endif

    // Compute the header (PPC64LE uses ARM64-style format)
    noway_assert((functionLength & 3) == 0);
    DWORD headerFunctionLength = functionLength / 4;

    DWORD headerVers = 0;
    DWORD headerXBit = 0;
    DWORD headerEBit;
    DWORD headerEpilogCount;
    DWORD headerCodeWords;
    DWORD headerExtendedEpilogCount = 0;
    DWORD headerExtendedCodeWords   = 0;

    if (ufiSetEBit)
    {
        headerEBit        = 1;
        headerEpilogCount = ufiEpilogList->GetStartIndex();
        headerCodeWords   = ufiCodeWords;
    }
    else
    {
        headerEBit = 0;

        if (ufiNeedExtendedCodeWordsEpilogCount)
        {
            headerEpilogCount         = 0;
            headerCodeWords           = 0;
            headerExtendedEpilogCount = ufiEpilogScopes;
            headerExtendedCodeWords   = ufiCodeWords;
        }
        else
        {
            headerEpilogCount = ufiEpilogScopes;
            headerCodeWords   = ufiCodeWords;
        }
    }

    noway_assert(headerFunctionLength <= 0x3FFFFU);

    if ((headerEpilogCount > UW_MAX_EPILOG_COUNT) || (headerCodeWords > UW_MAX_CODE_WORDS_COUNT))
    {
        IMPL_LIMITATION("unwind data too large");
    }

    // PPC64LE uses ARM64-style header format
    DWORD header = headerFunctionLength | (headerVers << 18) | (headerXBit << 20) | (headerEBit << 21) |
                   (headerEpilogCount << 22) | (headerCodeWords << 27);

    ufiPrologCodes.AddHeaderWord(header);

    if (ufiNeedExtendedCodeWordsEpilogCount)
    {
        noway_assert(headerEBit == 0);
        noway_assert(headerEpilogCount == 0);
        noway_assert(headerCodeWords == 0);
        noway_assert((headerExtendedEpilogCount > UW_MAX_EPILOG_COUNT) ||
                     (headerExtendedCodeWords > UW_MAX_CODE_WORDS_COUNT));

        if ((headerExtendedEpilogCount > UW_MAX_EXTENDED_EPILOG_COUNT) ||
            (headerExtendedCodeWords > UW_MAX_EXTENDED_CODE_WORDS_COUNT))
        {
            IMPL_LIMITATION("unwind data too large");
        }

        DWORD header2 = headerExtendedEpilogCount | (headerExtendedCodeWords << 16);

        ufiPrologCodes.AddHeaderWord(header2);
    }

    if (!ufiSetEBit)
    {
        for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
            assert(pEpi->GetStartOffset() >= GetStartOffset());

            DWORD headerEpilogStartOffset = pEpi->GetStartOffset() - GetStartOffset();

            noway_assert((headerEpilogStartOffset & 3) == 0);
            headerEpilogStartOffset /= 4;

            DWORD headerEpilogStartIndex = pEpi->GetStartIndex();

            if ((headerEpilogStartOffset > UW_MAX_EPILOG_START_OFFSET) ||
                (headerEpilogStartIndex > UW_MAX_EPILOG_START_INDEX))
            {
                IMPL_LIMITATION("unwind data too large");
            }

            DWORD epilogScopeWord = headerEpilogStartOffset | (headerEpilogStartIndex << 22);

            ufiPrologCodes.AddHeaderWord(epilogScopeWord);
        }
    }
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    MergeCodes();

    bool isColdCode = !isHotCode;

    ULONG unwindSize = Size();

#ifdef DEBUG
    if (uwiComp->verbose)
    {
        if (ufiNum != 1)
            printf("reserveUnwindInfo: fragment #%d:\n", ufiNum);
    }
#endif

    uwiComp->eeReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
}

// Allocate the unwind info for a fragment with the VM.
// Arguments:
//      funKind:       funclet kind
//      pHotCode:      hot section code buffer
//      pColdCode:     cold section code buffer
//      funcEndOffset: offset of the end of this function/funclet. Used if this fragment is the last one for a
//                     function/funclet.
//      isHotCode:     are we allocating the unwind info for the hot code section?

void UnwindFragmentInfo::Allocate(
		    CorJitFuncKind funKind, void* pHotCode, void* pColdCode, UNATIVE_OFFSET funcEndOffset, bool isHotCode)
{
    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;
    UNATIVE_OFFSET codeSize;

    startOffset = GetStartOffset();

    if (ufiNext == NULL)
    {
        assert(funcEndOffset != 0);
        endOffset = funcEndOffset;
    }
    else
    {
        endOffset = ufiNext->GetStartOffset();
    }

    assert(endOffset > startOffset);
    codeSize = endOffset - startOffset;

    Finalize(codeSize);

    BYTE* pUnwindBlock;
    ULONG unwindSize;
    ufiPrologCodes.GetFinalInfo(&pUnwindBlock, &unwindSize);

    uwiComp->eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock,
                               funKind);
}

#ifdef DEBUG
void UnwindFragmentInfo::Dump(int indent)
{
    printf("%*sUnwindFragmentInfo @0x%08p, size:%d:\n", indent, "", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  ufiNext: 0x%08p\n", indent, "", dspPtr(ufiNext));
    printf("%*s  ufiEmitLoc: 0x%08p\n", indent, "", dspPtr(ufiEmitLoc));
    printf("%*s  ufiHasPhantomProlog: %s\n", indent, "", dspBool(ufiHasPhantomProlog));
    printf("%*s  ufiSize: %u\n", indent, "", ufiSize);
    printf("%*s  ufiStartOffset: 0x%x\n", indent, "", ufiStartOffset);

    ufiPrologCodes.Dump(indent + 2);

    for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        pEpi->Dump(indent + 2);
    }
}
#endif // DEBUG

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindInfo
//
/////////////////////////////////////////////////////////////////////////////

void UnwindInfo::InitUnwindInfo(Compiler* comp, emitLocation* startLoc, emitLocation* endLoc)
{
    uwiComp = comp;

    // The first fragment is a member of UnwindInfo, so it doesn't need to be allocated.
    // However, its constructor needs to be explicitly called, since the constructor for
    // UnwindInfo is not called.

    new (&uwiFragmentFirst, jitstd::placement_t()) UnwindFragmentInfo(comp, startLoc, false);

    uwiFragmentLast = &uwiFragmentFirst;

    uwiEndLoc = endLoc;

    // Allocate an emitter location object. It is initialized to something
    // invalid: it has a null 'ig' that needs to get set before it can be used.
    // Note that when we create an UnwindInfo for the cold section, this never
    // gets initialized with anything useful, since we never add unwind codes
    // to the cold section; we simply distribute the existing (previously added) codes.
    uwiCurLoc = new (uwiComp, CMK_UnwindInfo) emitLocation();

#ifdef DEBUG
    uwiInitialized = UWI_INITIALIZED_PATTERN;
    uwiAddingNOP   = false;
#endif // DEBUG
}

// Split the unwind codes in 'puwi' into those that are in the hot section (leave them in 'puwi')
// and those that are in the cold section (move them to 'this'). There is exactly one fragment
// in each UnwindInfo; the fragments haven't been split for size, yet.

void UnwindInfo::HotColdSplitCodes(UnwindInfo* puwi)
{
    // Ensure that there is exactly a single fragment in both the hot and the cold sections
    assert(&uwiFragmentFirst == uwiFragmentLast);
    assert(&puwi->uwiFragmentFirst == puwi->uwiFragmentLast);
    assert(uwiFragmentLast->ufiNext == NULL);
    assert(puwi->uwiFragmentLast->ufiNext == NULL);

    // The real prolog is in the hot section, so this, cold, section has a phantom prolog
    uwiFragmentLast->ufiHasPhantomProlog = true;
    uwiFragmentLast->CopyPrologCodes(puwi->uwiFragmentLast);

    // Now split the epilog codes
    uwiFragmentLast->SplitEpilogCodes(uwiFragmentLast->ufiEmitLoc, puwi->uwiFragmentLast);
}

// Split the function or funclet into fragments that are no larger than 512K,
// so the fragment size will fit in the unwind data "Function Length" field.
// The ARM Exception Data specification "Function Fragments" section describes this.
// We split the function so that it is no larger than 512K bytes, or the value of
// the DOTNET_JitSplitFunctionSize value, if defined (and smaller). We must determine
// how to split the function/funclet before we issue the instructions, so we can
// reserve the unwind space with the VM. The instructions issued may shrink (but not
// expand!) during issuing (although this is extremely rare in any case, and may not
// actually occur on ARM), so we don't finalize actual sizes or offsets.
//
// ARM64 has very similar limitations, except functions can be up to 1MB. TODO-ARM64-Bug?: make sure this works!
//
// We don't split any prolog or epilog. Ideally, we might not split an instruction,
// although that doesn't matter because the unwind at any point would still be
// well-defined.

void UnwindInfo::Split()
{
    UNATIVE_OFFSET maxFragmentSize; // The maximum size of a code fragment in bytes

    maxFragmentSize = UW_MAX_FRAGMENT_SIZE_BYTES;

#ifdef DEBUG
    // Consider DOTNET_JitSplitFunctionSize
    unsigned splitFunctionSize = (unsigned)JitConfig.JitSplitFunctionSize();
    if (splitFunctionSize == 0)
    {
        // If the split configuration is not set, then sometimes set it during stress.
        // Use two stress modes: a split size of 4 (extreme) and a split size of 200 (reasonable).
        if (uwiComp->compStressCompile(Compiler::STRESS_UNWIND, 10))
        {
            if (uwiComp->compStressCompile(Compiler::STRESS_UNWIND, 5))
            {
                splitFunctionSize = 4;
            }
            else
            {
                splitFunctionSize = 200;
            }
        }
    }

    if (splitFunctionSize != 0)
        if (splitFunctionSize < maxFragmentSize)
            maxFragmentSize = splitFunctionSize;
#endif // DEBUG

    // Now, there should be exactly one fragment.

    assert(uwiFragmentLast != NULL);
    assert(uwiFragmentLast == &uwiFragmentFirst);
    assert(uwiFragmentLast->ufiNext == NULL);

    // Find the code size of this function/funclet.

    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;
    UNATIVE_OFFSET codeSize;

    if (uwiFragmentLast->ufiEmitLoc == NULL)
    {
        // NULL emit location means the beginning of the code. This is to handle the first fragment prolog.
        startOffset = 0;
    }
    else
    {
        startOffset = uwiFragmentLast->ufiEmitLoc->CodeOffset(uwiComp->GetEmitter());
    }

    if (uwiEndLoc == NULL)
    {
        // Note that compTotalHotCodeSize and compTotalColdCodeSize are computed before issuing instructions
        // from the emitter instruction group offsets, and will be accurate unless the issued code shrinks.
        // compNativeCodeSize is precise, but is only set after instructions are issued, which is too late
        // for us, since we need to decide how many fragments we need before the code memory is allocated
        // (which is before instruction issuing).
        UNATIVE_OFFSET estimatedTotalCodeSize =
            uwiComp->info.compTotalHotCodeSize + uwiComp->info.compTotalColdCodeSize;
        assert(estimatedTotalCodeSize != 0);
        endOffset = estimatedTotalCodeSize;
    }
    else
    {
        endOffset = uwiEndLoc->CodeOffset(uwiComp->GetEmitter());
    }

    assert(endOffset > startOffset); // there better be at least 1 byte of code
    codeSize = endOffset - startOffset;

    // Now that we know the code size for this section (main function hot or cold, or funclet),
    // figure out how many fragments we're going to need.

    UNATIVE_OFFSET numberOfFragments = (codeSize + maxFragmentSize - 1) / maxFragmentSize; // round up
    assert(numberOfFragments > 0);

    if (numberOfFragments == 1)
    {
        // No need to split; we're done
        return;
    }

    // Now, we're going to commit to splitting the function into "numberOfFragments" fragments,
    // for the purpose of unwind information. We need to do the actual splits so we can figure out
    // the size of each piece of unwind data for the call to reserveUnwindInfo(). We won't know
    // the actual offsets of the splits since we haven't issued the instructions yet, so store
    // an emitter location instead of an offset, and "finalize" the offset in the unwindEmit() phase,
    // like we do for the function length and epilog offsets.

#ifdef DEBUG
    if (uwiComp->verbose)
    {
        printf("Split unwind info into %d fragments (function/funclet size: %d, maximum fragment size: %d)\n",
               numberOfFragments, codeSize, maxFragmentSize);
    }
#endif // DEBUG

    // Call the emitter to do the split, and call us back for every split point it chooses.
    uwiComp->GetEmitter()->emitSplit(uwiFragmentLast->ufiEmitLoc, uwiEndLoc, maxFragmentSize, (void*)this,
                                     EmitSplitCallback);

#ifdef DEBUG
    // Did the emitter split the function/funclet into as many fragments as we asked for?
    // It might be fewer if the DOTNET_JitSplitFunctionSize was used, but it better not
    // be fewer if we're splitting into 512K blocks!

    unsigned fragCount = 0;
    for (UnwindFragmentInfo* pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        ++fragCount;
    }
    if (fragCount < numberOfFragments)
    {
        if (uwiComp->verbose)
        {
            printf("WARNING: asked the emitter for %d fragments, but only got %d\n", numberOfFragments, fragCount);
        }

        // If this fires, then we split into fewer fragments than we asked for, and we are using
        // the default, unwind-data-defined 512K maximum fragment size. We won't be able to fit
        // this fragment into the unwind data! If you set DOTNET_JitSplitFunctionSize to something
        // small, we might not be able to split into as many fragments as asked for, because we
        // can't split prologs or epilogs.
        assert(maxFragmentSize != UW_MAX_FRAGMENT_SIZE_BYTES);
    }
#endif // DEBUG
}

/*static*/ void UnwindInfo::EmitSplitCallback(void* context, emitLocation* emitLoc)
{
    UnwindInfo* puwi = (UnwindInfo*)context;
    puwi->AddFragment(emitLoc);
}

// Reserve space for the unwind info for all fragments

void UnwindInfo::Reserve(bool isFunclet, bool isHotCode)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);

    for (UnwindFragmentInfo* pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Reserve(isFunclet, isHotCode);
    }
}

// Allocate and populate VM unwind info for all fragments

void UnwindInfo::Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);

    UnwindFragmentInfo* pFrag;

    // First, finalize all the offsets (the location of the beginning of fragments, and epilogs),
    // so a fragment can use the finalized offset of the subsequent fragment to determine its code size.

    UNATIVE_OFFSET endOffset;

    if (uwiEndLoc == NULL)
    {
        assert(uwiComp->info.compNativeCodeSize != 0);
        endOffset = uwiComp->info.compNativeCodeSize;
    }
    else
    {
        endOffset = uwiEndLoc->CodeOffset(uwiComp->GetEmitter());
    }

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->FinalizeOffset();
    }

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Allocate(funKind, pHotCode, pColdCode, endOffset, isHotCode);
    }
}

void UnwindInfo::AddEpilog()
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiFragmentLast != NULL);
    uwiFragmentLast->AddEpilog();
    CaptureLocation();
}

void UnwindInfo::CaptureLocation()
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiCurLoc != NULL);
    uwiCurLoc->CaptureLocation(uwiComp->GetEmitter());
}

void UnwindInfo::AddFragment(emitLocation* emitLoc)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiFragmentLast != NULL);

    UnwindFragmentInfo* newFrag = new (uwiComp, CMK_UnwindInfo) UnwindFragmentInfo(uwiComp, emitLoc, true);

#ifdef DEBUG
    newFrag->ufiNum = uwiFragmentLast->ufiNum + 1;
#endif // DEBUG

    newFrag->CopyPrologCodes(&uwiFragmentFirst);
    newFrag->SplitEpilogCodes(emitLoc, uwiFragmentLast);

    // Link the new fragment in at the end of the fragment list
    uwiFragmentLast->ufiNext = newFrag;
    uwiFragmentLast          = newFrag;
}

#ifdef DEBUG
void UnwindInfo::Dump(bool isHotCode, int indent)
{
    unsigned            count;
    UnwindFragmentInfo* pFrag;

    count = 0;
    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        ++count;
    }

    printf("%*sUnwindInfo %s@0x%08p, size:%d:\n", indent, "", isHotCode ? "" : "COLD ", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  %d fragment%s\n", indent, "", count, (count != 1) ? "s" : "");
    printf("%*s  uwiFragmentLast: 0x%08p\n", indent, "", dspPtr(uwiFragmentLast));
    printf("%*s  uwiEndLoc: 0x%08p\n", indent, "", dspPtr(uwiEndLoc));
    printf("%*s  uwiInitialized: 0x%08x\n", indent, "", uwiInitialized);

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Dump(indent + 2);
    }
}
#endif // DEBUG











