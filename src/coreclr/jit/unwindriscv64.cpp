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
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Finalize: Prepare the unwind information for the VM. Compute and prepend the unwind header.

void UnwindFragmentInfo::Finalize(UNATIVE_OFFSET functionLength)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*static*/ void UnwindInfo::EmitSplitCallback(void* context, emitLocation* emitLoc)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Reserve space for the unwind info for all fragments

void UnwindInfo::Reserve(bool isFunclet, bool isHotCode)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

// Allocate and populate VM unwind info for all fragments

void UnwindInfo::Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void UnwindInfo::AddEpilog()
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
