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

#if defined(TARGET_RISCV64)

#if defined(FEATURE_CFI_SUPPORT)
short Compiler::mapRegNumToDwarfReg(regNumber reg)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}
#endif // FEATURE_CFI_SUPPORT

void Compiler::unwindPush(regNumber reg)
{
    unreached(); // use one of the unwindSaveReg* functions instead.
}

void Compiler::unwindAllocStack(unsigned size)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            unwindAllocStackCFI(size);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    assert(size % 16 == 0);
    unsigned x = size / 16;

    if (x <= 0x1F)
    {
        // alloc_s: 000xxxxx: allocate small stack with size < 128 (2^5 * 16)
        // TODO-Review: should say size < 512

        pu->AddCode((BYTE)x);
    }
    else if (x <= 0x7F)
    {
        // alloc_m: 11000xxx | xxxxxxxx: allocate large stack with size < 2k (2^7 * 16)

        pu->AddCode(0xC0 | (BYTE)(x >> 8), (BYTE)x);
    }
    else
    {
        // alloc_l: 11100000 | xxxxxxxx | xxxxxxxx | xxxxxxxx : allocate large stack with size < 256M (2^24 * 16)
        //
        // For large stack size, the most significant bits
        // are stored first (and next to the opCode) per the unwind spec.

        pu->AddCode(0xE0, (BYTE)(x >> 16), (BYTE)(x >> 8), (BYTE)x);
    }
}

void Compiler::unwindSetFrameReg(regNumber reg, unsigned offset)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            unwindSetFrameRegCFI(reg, offset);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (offset == 0)
    {
        assert(reg == REG_FP);

        // set_fp: 11100001 : set up fp : with : move fp, sp
        pu->AddCode(0xE1);
    }
    else
    {
        // add_fp: 11100010 | 000xxxxx | xxxxxxxx : set up fp with : addi.d fp, sp, #x * 8

        assert(reg == REG_FP);
        assert((offset % 8) == 0);

        unsigned x = offset / 8;
        assert(x <= 0x1FF);

        pu->AddCode(0xE2, (BYTE)(x >> 8), (BYTE)x);
    }
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    unwindSaveReg(reg, (int)offset);
}

void Compiler::unwindNop()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindSaveReg(regNumber reg, int offset)
{

    // sd reg, sp, offset

    // offset for store in prolog must be positive and a multiple of 8.
    assert(0 <= offset && offset <= 2047);
    assert((offset % 8) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            FuncInfoDsc*   func     = funCurrentFunc();
            UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg), offset);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT
    int z = offset / 8;
    // assert(0 <= z && z <= 0xFF);

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (emitter::isGeneralRegister(reg))
    {
        // save_reg: 11010000 | 000xxxxx | zzzzzzzz: save reg r(1 + #X) at [sp + #Z * 8], offset <= 2047

        assert(reg == REG_RA || (REG_FP <= reg && reg <= REG_S11));  // first legal register: RA, last legal register: S11

        BYTE x = (BYTE)(reg - REG_RA);
        assert(0 <= x && x <= 0x1B);

        pu->AddCode(0xD0, (BYTE)x, (BYTE)z);
    }
    else
    {
        // save_freg: 1101110x | xxxxzzzz | zzzzzzzz : save reg f(8 + #X) at [sp + #Z * 8], offset <= 2047
        assert(REG_F8 == reg || REG_F9 == reg || // first legal register: F8
               (REG_F18 <= reg && reg <= REG_F27));  // last legal register: F27

        BYTE x = (BYTE)(reg - REG_F8);
        assert(0 <= x && x <= 0x13);

        pu->AddCode(0xDC | (BYTE)(x >> 3), (BYTE)(x << 4) | (BYTE)(z >> 8), (BYTE)z); // TODO NEED TO CHECK LATER
    }
}

void Compiler::unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset)
{
    assert(!"unused on RISCV64 yet");
}

void Compiler::unwindReturn(regNumber reg)
{
    // Nothing to do; we will always have at least one trailing "end" opcode in our padding.
}

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Debug helpers                                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

// Return the size of the unwind code (from 1 to 4 bytes), given the first byte of the unwind bytes

unsigned GetUnwindSizeFromUnwindHeader(BYTE b1)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
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

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindCodesBase
//
///////////////////////////////////////////////////////////////////////////////

#ifdef DEBUG

// Walk the prolog codes and calculate the size of the prolog or epilog, in bytes.
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

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Debug dumpers                                                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

// start is 0-based index from LSB, length is number of bits
DWORD ExtractBits(DWORD dw, DWORD start, DWORD length)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

// Dump the unwind data.
// Arguments:
//      isHotCode:          true if this unwind data is for the hot section
//      startOffset:        byte offset of the code start that this unwind data represents
//      endOffset:          byte offset of the code end   that this unwind data represents
//      pHeader:            pointer to the unwind data blob
//      unwindBlockSize:    size in bytes of the unwind data blob

void DumpUnwindInfo(Compiler*         comp,
                    bool              isHotCode,
                    UNATIVE_OFFSET    startOffset,
                    UNATIVE_OFFSET    endOffset,
                    const BYTE* const pHeader,
                    ULONG             unwindBlockSize)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

#endif // DEBUG

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

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        unwindBegPrologCFI();
        return;
    }
#endif // FEATURE_CFI_SUPPORT

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
}

void Compiler::unwindBegEpilog()
{
    assert(compGeneratingEpilog);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    funCurrentFunc()->uwi.AddEpilog();
}

void Compiler::unwindEndEpilog()
{
    assert(compGeneratingEpilog);
}

// The instructions between the last captured "current state" and the current instruction
// are in the prolog but have no effect for unwinding. Emit the appropriate NOP unwind codes
// for them.
void Compiler::unwindPadding()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

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
    bool funcHasColdSection = false;

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        DWORD unwindCodeBytes = 0;
        if (fgFirstColdBlock != nullptr)
        {
            eeReserveUnwindInfo(isFunclet, true /*isColdCode*/, unwindCodeBytes);
        }
        unwindCodeBytes = (DWORD)(func->cfiCodes->size() * sizeof(CFI_CODE));
        eeReserveUnwindInfo(isFunclet, false /*isColdCode*/, unwindCodeBytes);

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // If there is cold code, split the unwind data between the hot section and the
    // cold section. This needs to be done before we split into fragments, as each
    // of the hot and cold sections can have multiple fragments.

    if (fgFirstColdBlock != NULL)
    {
        assert(!isFunclet); // TODO-CQ: support hot/cold splitting with EH

        emitLocation* startLoc;
        emitLocation* endLoc;
        unwindGetFuncLocations(func, false, &startLoc, &endLoc);

        func->uwiCold = new (this, CMK_UnwindInfo) UnwindInfo();
        func->uwiCold->InitUnwindInfo(this, startLoc, endLoc);
        func->uwiCold->HotColdSplitCodes(&func->uwi);

        funcHasColdSection = true;
    }

    // First we need to split the function or funclet into fragments that are no larger
    // than 512K, so the fragment size will fit in the unwind data "Function Length" field.
    // The LOONGARCH Exception Data specification "Function Fragments" section describes this.
    func->uwi.Split();

    func->uwi.Reserve(isFunclet, true);

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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindPrologCodes
//
///////////////////////////////////////////////////////////////////////////////

// We're going to use the prolog codes memory to store the final unwind data.
// Ensure we have enough memory to store everything. If 'epilogBytes' > 0, then
// move the prolog codes so there are 'epilogBytes' bytes after the prolog codes.
// Set the header pointer for future use, adding the header bytes (this pointer
// is updated when a header byte is added), and remember the index that points
// to the beginning of the header.

void UnwindPrologCodes::SetFinalSize(int headerBytes, int epilogBytes)
{
#if 0 // TODO COMMENTED OUT BECAUSE s_UnwindSize is not set
#ifdef DEBUG
    // We're done adding codes. Check that we didn't accidentally create a bigger prolog.
    unsigned codeSize = GetCodeSizeFromUnwindCodes(true);
    assert(codeSize <= MAX_PROLOG_SIZE_BYTES);
#endif // DEBUG
#endif

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
        CLANG_FORMAT_COMMENT_ANCHOR;

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
    _ASSERTE(!"TODO RISCV64 NYI");
}

// AppendEpilog: copy the epilog bytes to the next epilog bytes slot
void UnwindPrologCodes::AppendEpilog(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// GetFinalInfo: return a pointer to the final unwind info to hand to the VM, and the size of this info in bytes
void UnwindPrologCodes::GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

int UnwindPrologCodes::Match(UnwindEpilogInfo* pEpi)
{
    if (Size() < pEpi->Size())
    {
        return -1;
    }

    int matchIndex = 0; // Size() - pEpi->Size();

    BYTE* pProlog = GetCodes();
    BYTE* pEpilog = pEpi->GetCodes();

    // First check set_fp.
    if (0 < pEpi->Size())
    {
        if (*pProlog == 0xE1)
        {
            pProlog++;
            if (*pEpilog == 0xE1)
            {
                pEpilog++;
            }
            else
            {
                matchIndex = 1;
            }
        }
        else if (*pProlog == 0xE2)
        {
            pProlog += 3;
            if (*pEpilog == 0xE1)
            {
                pEpilog += 3;
            }
            else
            {
                matchIndex = 3;
            }
        }
    }

    if (0 == memcmp(pProlog, pEpilog, pEpi->Size()))
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogCodes
//
///////////////////////////////////////////////////////////////////////////////

void UnwindEpilogCodes::EnsureSize(int requiredSize)
{
    if (requiredSize > uecMemSize)
    {
        // Reallocate, and copy everything to a new array.

        // Choose the next power of two size. This may or may not be the best choice.
        noway_assert((requiredSize & 0xC0000000) == 0); // too big!
        int newSize;
        for (newSize = uecMemSize << 1; newSize < requiredSize; newSize <<= 1)
        {
            // do nothing
        }

        BYTE* newUnwindCodes = new (uwiComp, CMK_UnwindInfo) BYTE[newSize];
        memcpy_s(newUnwindCodes, newSize, uecMem, uecMemSize);
#ifdef DEBUG
        // Clear the old unwind codes; nobody should be looking at them
        memset(uecMem, 0xFF, uecMemSize);
#endif                           // DEBUG
        uecMem = newUnwindCodes; // we don't free anything that used to be there since we have a no-release allocator
        // uecCodeSlot stays the same
        uecMemSize = newSize;
    }
}

#ifdef DEBUG
void UnwindEpilogCodes::Dump(int indent)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogInfo
//
///////////////////////////////////////////////////////////////////////////////

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
    _ASSERTE(!"TODO RISCV64 NYI");
    return -1;
}

void UnwindEpilogInfo::CaptureEmitLocation()
{
    noway_assert(epiEmitLocation == NULL); // This function is only called once per epilog
    epiEmitLocation = new (uwiComp, CMK_UnwindInfo) emitLocation();
    epiEmitLocation->CaptureLocation(uwiComp->GetEmitter());
}

void UnwindEpilogInfo::FinalizeOffset()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

#ifdef DEBUG
void UnwindEpilogInfo::Dump(int indent)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindFragmentInfo
//
///////////////////////////////////////////////////////////////////////////////

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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Split the epilog codes that currently exist in 'pSplitFrom'. The ones that represent
// epilogs that start at or after the location represented by 'emitLoc' are removed
// from 'pSplitFrom' and moved to this fragment. Note that this fragment should not have
// any epilog codes currently; it is at the initial state.

void UnwindFragmentInfo::SplitEpilogCodes(emitLocation* emitLoc, UnwindFragmentInfo* pSplitFrom)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Is this epilog at the end of an unwind fragment? Ask the emitter.
// Note that we need to know this before all code offsets are finalized,
// so we can determine whether we can omit an epilog scope word for a
// single matching epilog.

bool UnwindFragmentInfo::IsAtFragmentEnd(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
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
    unsigned epilogCodeBytes = 0; // The total number of unwind code bytes used by epilogs that don't match the
                                  // prolog codes
    unsigned epilogIndex = ufiPrologCodes.Size(); // The "Epilog Start Index" for the next non-matching epilog codes
    UnwindEpilogInfo* pEpi;

    for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        ++epilogCount;

        pEpi->FinalizeCodes();

        // Does this epilog match the prolog?
        // NOTE: for the purpose of matching, we don't handle the 0xFD and 0xFE end codes that allow slightly unequal
        // prolog and epilog codes.

        int matchIndex;

        matchIndex = ufiPrologCodes.Match(pEpi);
        if (matchIndex != -1)
        {
            pEpi->SetMatches();
            pEpi->SetStartIndex(matchIndex); // Prolog codes start at zero, so matchIndex is exactly the start index
        }
        else
        {
            // The epilog codes don't match the prolog codes. Do they match any of the epilogs
            // we've seen so far?

            bool matched = false;
            for (UnwindEpilogInfo* pEpi2 = ufiEpilogList; pEpi2 != pEpi; pEpi2 = pEpi2->epiNext)
            {
                matchIndex = pEpi2->Match(pEpi);
                if (matchIndex != -1)
                {
                    // Use the same epilog index as the one we matched, as it has already been set.
                    pEpi->SetMatches();
                    pEpi->SetStartIndex(pEpi2->GetStartIndex() + matchIndex); // We might match somewhere inside pEpi2's
                                                                              // codes, in which case matchIndex > 0
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                pEpi->SetStartIndex(epilogIndex); // We'll copy these codes to the next available location
                epilogCodeBytes += pEpi->Size();
                epilogIndex += pEpi->Size();
            }
        }
    }

    DWORD codeBytes = ufiPrologCodes.Size() + epilogCodeBytes;
    codeBytes       = AlignUp(codeBytes, sizeof(DWORD));

    DWORD codeWords =
        codeBytes / sizeof(DWORD); // This is how many words we need to store all the unwind codes in the unwind block

    // Do we need the 2nd header word for "Extended Code Words" or "Extended Epilog Count"?

    bool needExtendedCodeWordsEpilogCount =
        (codeWords > UW_MAX_CODE_WORDS_COUNT) || (epilogCount > UW_MAX_EPILOG_COUNT);

    // How many epilog scope words do we need?

    bool     setEBit      = false;       // do we need to set the E bit?
    unsigned epilogScopes = epilogCount; // Note that this could be zero if we have no epilogs!

    if (epilogCount == 1)
    {
        assert(ufiEpilogList != NULL);
        assert(ufiEpilogList->epiNext == NULL);

        if (ufiEpilogList->Matches() && (ufiEpilogList->GetStartIndex() == 0) && // The match is with the prolog
            !needExtendedCodeWordsEpilogCount && IsAtFragmentEnd(ufiEpilogList))
        {
            epilogScopes = 0; // Don't need any epilog scope words
            setEBit      = true;
        }
    }

    DWORD headerBytes = (1                                            // Always need first header DWORD
                         + (needExtendedCodeWordsEpilogCount ? 1 : 0) // Do we need the 2nd DWORD for Extended Code
                                                                      // Words or Extended Epilog Count?
                         + epilogScopes                               // One DWORD per epilog scope, for EBit = 0
                         ) *
                        sizeof(DWORD); // convert it to bytes

    DWORD finalSize = headerBytes + codeBytes; // Size of actual unwind codes, aligned up to 4-byte words,
                                               // including end padding if necessary

    // Construct the final unwind information.

    // We re-use the memory for the prolog unwind codes to construct the full unwind data. If all the epilogs
    // match the prolog, this is easy: we just prepend the header. If there are epilog codes that don't match
    // the prolog, we still use the prolog codes memory, but it's a little more complicated, since the
    // unwind info is ordered as: (a) header, (b) prolog codes, (c) non-matching epilog codes. And, the prolog
    // codes array is filled in from end-to-beginning. So, we compute the size of memory we need, ensure we
    // have that much memory, and then copy the prolog codes to the right place, appending the non-matching
    // epilog codes and prepending the header.

    ufiPrologCodes.SetFinalSize(headerBytes, epilogCodeBytes);

    if (epilogCodeBytes != 0)
    {
        // We need to copy the epilog code bytes to their final memory location

        for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
            if (!pEpi->Matches())
            {
                ufiPrologCodes.AppendEpilog(pEpi);
            }
        }
    }

    // Save some data for later
    ufiSize                             = finalSize;
    ufiSetEBit                          = setEBit;
    ufiNeedExtendedCodeWordsEpilogCount = needExtendedCodeWordsEpilogCount;
    ufiCodeWords                        = codeWords;
    ufiEpilogScopes                     = epilogScopes;
}

// Finalize: Prepare the unwind information for the VM. Compute and prepend the unwind header.

void UnwindFragmentInfo::Finalize(UNATIVE_OFFSET functionLength)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    assert(isHotCode || !isFunclet); // TODO-CQ: support hot/cold splitting in functions with EH

    MergeCodes();

    BOOL isColdCode = isHotCode ? FALSE : TRUE;

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
    _ASSERTE(!"TODO RISCV64 NYI");
}

#ifdef DEBUG
void UnwindFragmentInfo::Dump(int indent)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindInfo
//
///////////////////////////////////////////////////////////////////////////////

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
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Split the function or funclet into fragments that are no larger than 512K,
// so the fragment size will fit in the unwind data "Function Length" field.

void UnwindInfo::Split()
{
    UNATIVE_OFFSET maxFragmentSize; // The maximum size of a code fragment in bytes

    maxFragmentSize = UW_MAX_FRAGMENT_SIZE_BYTES;

#ifdef DEBUG
    // Consider COMPlus_JitSplitFunctionSize
    unsigned splitFunctionSize = (unsigned)JitConfig.JitSplitFunctionSize();

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
    CLANG_FORMAT_COMMENT_ANCHOR;

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
    // It might be fewer if the COMPlus_JitSplitFunctionSize was used, but it better not
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
        // this fragment into the unwind data! If you set COMPlus_JitSplitFunctionSize to something
        // small, we might not be able to split into as many fragments as asked for, because we
        // can't split prologs or epilogs.
        assert(maxFragmentSize != UW_MAX_FRAGMENT_SIZE_BYTES);
    }
#endif // DEBUG
}

/*static*/ void UnwindInfo::EmitSplitCallback(void* context, emitLocation* emitLoc)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Reserve space for the unwind info for all fragments

void UnwindInfo::Reserve(bool isFunclet, bool isHotCode)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(isHotCode || !isFunclet);

    for (UnwindFragmentInfo* pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Reserve(isFunclet, isHotCode);
    }
}

// Allocate and populate VM unwind info for all fragments

void UnwindInfo::Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

#ifdef DEBUG

void UnwindInfo::Dump(bool isHotCode, int indent)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

#endif // DEBUG

#endif // TARGET_RISCV64
