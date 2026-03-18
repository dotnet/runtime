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
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
}

void Compiler::unwindEndProlog()
{
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
}

void Compiler::unwindBegEpilog()
{
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
}

void Compiler::unwindEndEpilog()
{
    //_ASSERTE(!"NYI");
    //TODO: JK, no-op for minimal frameless bring-up
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

#endif // defined(TARGET_POWERPC64)

// Ask the VM to reserve space for the unwind information for the function and
// all its funclets.

void Compiler::unwindReserve()
{
    //_ASSERTE(!"NYI");
}

void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
    _ASSERTE(!"NYI");
}

// unwindEmit: Report all the unwind information to the VM.
// Arguments:
//      pHotCode:  Pointer to the beginning of the memory with the function and funclet hot  code
//      pColdCode: Pointer to the beginning of the memory with the function and funclet cold code.

void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
    //_ASSERTE(!"NYI");
}

void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
   // _ASSERTE(!"NYI");
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

unsigned GetOpcodeSizeFromUnwindHeader(BYTE b1)
{
    _ASSERTE(!"NYI");
}

// Return the size of the unwind code (from 1 to 4 bytes), given the first byte of the unwind bytes

unsigned GetUnwindSizeFromUnwindHeader(BYTE b1)
{
    _ASSERTE(!"NYI");
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
// The 0xFD and 0xFE "end + NOP" codes need to be handled differently between
// the prolog and epilog. They count as pure "end" codes in a prolog, but they
// count as 16 and 32 bit NOPs (respectively), as well as an "end", in an epilog.

unsigned UnwindCodesBase::GetCodeSizeFromUnwindCodes(bool isProlog)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

// Add a header word. Header words are added starting at the beginning, in order: first to last.
// This is in contrast to the prolog unwind codes, which are added in reverse order.

void UnwindPrologCodes::AddHeaderWord(DWORD d)
{
    _ASSERTE(!"NYI");
}

// AppendEpilog: copy the epilog bytes to the next epilog bytes slot

void UnwindPrologCodes::AppendEpilog(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"NYI");
}

// GetFinalInfo: return a pointer to the final unwind info to hand to the VM, and the size of this info in bytes

void UnwindPrologCodes::GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize)
{
    _ASSERTE(!"NYI");
}

// Do the argument unwind codes match our unwind codes?
// If they don't match, return -1. If they do, return the offset into
// our codes at which they match. Note that this means that the
// argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.
//
// This is similar to UnwindEpilogInfo::Match().
//

int UnwindPrologCodes::Match(UnwindEpilogInfo* pEpi)
{
    _ASSERTE(!"NYI");
}

// Copy the prolog codes from another prolog. The only time this is legal is
// if we are at the initial state and no prolog codes have been added.
// This is used to create the 'phantom' prolog for non-first fragments.

void UnwindPrologCodes::CopyFrom(UnwindPrologCodes* pCopyFrom)
{
    _ASSERTE(!"NYI");
}

void UnwindPrologCodes::EnsureSize(int requiredSize)
{
    _ASSERTE(!"NYI");
}

#ifdef DEBUG
void UnwindPrologCodes::Dump(int indent)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

void UnwindFragmentInfo::FinalizeOffset()
{
    _ASSERTE(!"NYI");
}

void UnwindFragmentInfo::AddEpilog()
{
    _ASSERTE(!"NYI");
}

// Copy the prolog codes from the 'pCopyFrom' fragment. These prolog codes will
// become 'phantom' prolog codes in this fragment. Note that this fragment should
// not have any prolog codes currently; it is at the initial state.

void UnwindFragmentInfo::CopyPrologCodes(UnwindFragmentInfo* pCopyFrom)
{
    _ASSERTE(!"NYI");
}

// Split the epilog codes that currently exist in 'pSplitFrom'. The ones that represent
// epilogs that start at or after the location represented by 'emitLoc' are removed
// from 'pSplitFrom' and moved to this fragment. Note that this fragment should not have
// any epilog codes currently; it is at the initial state.

void UnwindFragmentInfo::SplitEpilogCodes(emitLocation* emitLoc, UnwindFragmentInfo* pSplitFrom)
{
	    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

// Finalize: Prepare the unwind information for the VM. Compute and prepend the unwind header.

void UnwindFragmentInfo::Finalize(UNATIVE_OFFSET functionLength)
{
    _ASSERTE(!"NYI");
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

#ifdef DEBUG
void UnwindFragmentInfo::Dump(int indent)
{
    _ASSERTE(!"NYI");
}
#endif // DEBUG

/////////////////////////////////////////////////////////////////////////////
//
//  UnwindInfo
//
/////////////////////////////////////////////////////////////////////////////

void UnwindInfo::InitUnwindInfo(Compiler* comp, emitLocation* startLoc, emitLocation* endLoc)
{
    _ASSERTE(!"NYI");
}

// Split the unwind codes in 'puwi' into those that are in the hot section (leave them in 'puwi')
// and those that are in the cold section (move them to 'this'). There is exactly one fragment
// in each UnwindInfo; the fragments haven't been split for size, yet.

void UnwindInfo::HotColdSplitCodes(UnwindInfo* puwi)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

/*static*/ void UnwindInfo::EmitSplitCallback(void* context, emitLocation* emitLoc)
{
    _ASSERTE(!"NYI");
}

// Reserve space for the unwind info for all fragments

void UnwindInfo::Reserve(bool isFunclet, bool isHotCode)
{
    _ASSERTE(!"NYI");
}

// Allocate and populate VM unwind info for all fragments

void UnwindInfo::Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode)
{
    _ASSERTE(!"NYI");
}

void UnwindInfo::AddEpilog()
{
    _ASSERTE(!"NYI");
}

void UnwindInfo::CaptureLocation()
{
    _ASSERTE(!"NYI");
}

void UnwindInfo::AddFragment(emitLocation* emitLoc)
{
    _ASSERTE(!"NYI");
}

#ifdef DEBUG
void UnwindInfo::Dump(bool isHotCode, int indent)
{
    _ASSERTE(!"NYI");
}
#endif // DEBUG











