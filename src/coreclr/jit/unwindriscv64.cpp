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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindSetFrameReg(regNumber reg, unsigned offset)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindNop()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

void Compiler::unwindSaveReg(regNumber reg, int offset)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
