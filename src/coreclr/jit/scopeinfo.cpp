// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  ScopeInfo                                XX
XX                                                                           XX
XX   Classes to gather the Scope information from the local variable info.   XX
XX   Translates the given LocalVarTab from IL instruction offsets into       XX
XX   native code offsets.                                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/******************************************************************************
 *                                  Debuggable code
 *
 *  We break up blocks at the start and end IL ranges of the local variables.
 *  This is because IL offsets do not correspond exactly to native offsets
 *  except at block boundaries. No basic-blocks are deleted (not even
 *  unreachable), so there will not be any missing address-ranges, though the
 *  blocks themselves may not be ordered. (Also, internal blocks may be added).
 *  o At the start of each basic block, siBeginBlock() checks if any variables
 *    are coming in scope, and adds an open scope to siOpenScopeList if needed.
 *  o At the end of each basic block, siEndBlock() checks if any variables
 *    are going out of scope and moves the open scope from siOpenScopeLast
 *    to siScopeList.
 *
 *                                  Optimized code
 *
 *  We cannot break up the blocks as this will produce different code under
 *  the debugger. Instead we try to do a best effort.
 *  o At the start of each basic block, siBeginBlock() adds open scopes
 *    corresponding to block->bbLiveIn to siOpenScopeList. Also siUpdate()
 *    is called to close scopes for variables which are not live anymore.
 *  o siEndBlock() closes scopes for any variables which go out of range
 *    before bbCodeOffsEnd.
 *  o siCloseAllOpenScopes() closes any open scopes after all the blocks.
 *    This should only be needed if some basic block are deleted/out of order,
 *    etc.
 *  Also,
 *  o At every assignment to a variable, siCheckVarScope() adds an open scope
 *    for the variable being assigned to.
 *  o UpdateLifeVar() calls siUpdate() which closes scopes for variables which
 *    are not live anymore.
 *
 ******************************************************************************
 */

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "emit.h"
#include "codegen.h"

//============================================================================
//           siVarLoc functions
//============================================================================

bool CodeGenInterface::siVarLoc::vlIsInReg(regNumber reg) const
{
    switch (vlType)
    {
        case CodeGenInterface::VLT_REG:
            return (vlReg.vlrReg == reg);
        case CodeGenInterface::VLT_REG_REG:
            return ((vlRegReg.vlrrReg1 == reg) || (vlRegReg.vlrrReg2 == reg));
        case CodeGenInterface::VLT_REG_STK:
            return (vlRegStk.vlrsReg == reg);
        case CodeGenInterface::VLT_STK_REG:
            return (vlStkReg.vlsrReg == reg);

        case CodeGenInterface::VLT_STK:
        case CodeGenInterface::VLT_STK2:
        case CodeGenInterface::VLT_FPSTK:
            return false;

        default:
            assert(!"Bad locType");
            return false;
    }
}

bool CodeGenInterface::siVarLoc::vlIsOnStack(regNumber reg, signed offset) const
{
    regNumber actualReg;

    switch (vlType)
    {

        case CodeGenInterface::VLT_REG_STK:
            actualReg = vlRegStk.vlrsStk.vlrssBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlRegStk.vlrsStk.vlrssOffset == offset));
        case CodeGenInterface::VLT_STK_REG:
            actualReg = vlStkReg.vlsrStk.vlsrsBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlStkReg.vlsrStk.vlsrsOffset == offset));
        case CodeGenInterface::VLT_STK:
            actualReg = vlStk.vlsBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlStk.vlsOffset == offset));
        case CodeGenInterface::VLT_STK2:
            actualReg = vlStk2.vls2BaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && ((vlStk2.vls2Offset == offset) || (vlStk2.vls2Offset == (offset - 4))));

        case CodeGenInterface::VLT_REG:
        case CodeGenInterface::VLT_REG_FP:
        case CodeGenInterface::VLT_REG_REG:
        case CodeGenInterface::VLT_FPSTK:
            return false;

        default:
            assert(!"Bad locType");
            return false;
    }
}

bool CodeGenInterface::siVarLoc::vlIsOnStack() const
{
    switch (vlType)
    {
        case CodeGenInterface::VLT_STK:
        case CodeGenInterface::VLT_STK2:
        case CodeGenInterface::VLT_FPSTK:
            return true;

        default:
            return false;
    }
}

//------------------------------------------------------------------------
// storeVariableInRegisters: Convert the siVarLoc instance in a register
//  location using the given registers.
//
// Arguments:
//    reg       - the first register where the variable is placed.
//    otherReg  - the second register where the variable is placed
//      or REG_NA if does not apply.
//
void CodeGenInterface::siVarLoc::storeVariableInRegisters(regNumber reg, regNumber otherReg)
{
    if (otherReg == REG_NA)
    {
        // Only one register is used
        vlType       = VLT_REG;
        vlReg.vlrReg = reg;
    }
    else
    {
        // Two register are used
        vlType            = VLT_REG_REG;
        vlRegReg.vlrrReg1 = reg;
        vlRegReg.vlrrReg2 = otherReg;
    }
}

//------------------------------------------------------------------------
// storeVariableOnStack: Convert the siVarLoc instance in a stack location
//  with the given base register and stack offset.
//
// Arguments:
//    stackBaseReg      - the base of the stack.
//    varStackOffset    - the offset from the base where the variable is placed.
//
void CodeGenInterface::siVarLoc::storeVariableOnStack(regNumber stackBaseReg, NATIVE_OFFSET varStackOffset)
{
    vlType           = VLT_STK;
    vlStk.vlsBaseReg = stackBaseReg;
    vlStk.vlsOffset  = varStackOffset;
}

//------------------------------------------------------------------------
// Equals: Compares first reference and then values of the structures.
//
// Arguments:
//    lhs   - a "siVarLoc *" to compare.
//    rhs   - a "siVarLoc *" to compare.
//
// Notes:
//    Return true if both are nullptr.
//
// static
bool CodeGenInterface::siVarLoc::Equals(const siVarLoc* lhs, const siVarLoc* rhs)
{
    if (lhs == rhs)
    {
        // Are both nullptr or the same reference
        return true;
    }
    if ((lhs == nullptr) || (rhs == nullptr))
    {
        // Just one of them is a nullptr
        return false;
    }
    if (lhs->vlType != rhs->vlType)
    {
        return false;
    }
    assert(lhs->vlType == rhs->vlType);
    // If neither is nullptr, and are not the same reference, compare values
    switch (lhs->vlType)
    {
        case VLT_STK:
        case VLT_STK_BYREF:
            return (lhs->vlStk.vlsBaseReg == rhs->vlStk.vlsBaseReg) && (lhs->vlStk.vlsOffset == rhs->vlStk.vlsOffset);

        case VLT_STK2:
            return (lhs->vlStk2.vls2BaseReg == rhs->vlStk2.vls2BaseReg) &&
                   (lhs->vlStk2.vls2Offset == rhs->vlStk2.vls2Offset);

        case VLT_REG:
        case VLT_REG_FP:
        case VLT_REG_BYREF:
            return (lhs->vlReg.vlrReg == rhs->vlReg.vlrReg);

        case VLT_REG_REG:
            return (lhs->vlRegReg.vlrrReg1 == rhs->vlRegReg.vlrrReg1) &&
                   (lhs->vlRegReg.vlrrReg2 == rhs->vlRegReg.vlrrReg2);

        case VLT_REG_STK:
            return (lhs->vlRegStk.vlrsReg == rhs->vlRegStk.vlrsReg) &&
                   (lhs->vlRegStk.vlrsStk.vlrssBaseReg == rhs->vlRegStk.vlrsStk.vlrssBaseReg) &&
                   (lhs->vlRegStk.vlrsStk.vlrssOffset == rhs->vlRegStk.vlrsStk.vlrssOffset);

        case VLT_STK_REG:
            return (lhs->vlStkReg.vlsrReg == rhs->vlStkReg.vlsrReg) &&
                   (lhs->vlStkReg.vlsrStk.vlsrsBaseReg == rhs->vlStkReg.vlsrStk.vlsrsBaseReg) &&
                   (lhs->vlStkReg.vlsrStk.vlsrsOffset == rhs->vlStkReg.vlsrStk.vlsrsOffset);

        case VLT_FPSTK:
            return (lhs->vlFPstk.vlfReg == rhs->vlFPstk.vlfReg);

        case VLT_FIXED_VA:
            return (lhs->vlFixedVarArg.vlfvOffset == rhs->vlFixedVarArg.vlfvOffset);

        case VLT_COUNT:
        case VLT_INVALID:
            return true;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// siFillStackVarLoc: Fill "siVarLoc" struct indicating the stack position of the variable
// using "LclVarDsc" and "baseReg"/"offset".
//
// Arguments:
//    varDsc    - a "LclVarDsc *" to the variable it is desired to build the "siVarLoc".
//    varLoc    - a "siVarLoc &" to fill with the data of the "varDsc".
//    type      - a "var_types" which indicate the type of the variable.
//    baseReg   - a "regNumber" use as a base for the offset.
//    offset    - a signed amount of bytes distance from "baseReg" for the position of the variable.
//    isFramePointerUsed - a boolean variable
//
// Notes:
//    The "varLoc" argument is filled depending of the "type" argument but as a VLT_STK... variation.
//    "baseReg" and "offset" are used to indicate the position of the variable in the stack.
void CodeGenInterface::siVarLoc::siFillStackVarLoc(
    const LclVarDsc* varDsc, var_types type, regNumber baseReg, int offset, bool isFramePointerUsed)
{
    assert(offset != BAD_STK_OFFS);

    switch (type)
    {
        case TYP_INT:
        case TYP_REF:
        case TYP_BYREF:
        case TYP_FLOAT:
        case TYP_STRUCT:
#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        case TYP_SIMD64:
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
#ifdef TARGET_64BIT
        case TYP_LONG:
        case TYP_DOUBLE:
#endif // TARGET_64BIT
#if FEATURE_IMPLICIT_BYREFS
            // In the AMD64 ABI we are supposed to pass a struct by reference when its
            // size is not 1, 2, 4 or 8 bytes in size. During fgMorph, the compiler modifies
            // the IR to comply with the ABI and therefore changes the type of the lclVar
            // that holds the struct from TYP_STRUCT to TYP_BYREF but it gives us a hint that
            // this is still a struct by setting the lvIsImplicitByref flag.
            // The same is true for ARM64 and structs > 16 bytes.
            //
            // See lvaSetStruct for further detail.
            //
            // Now, the VM expects a special enum for these type of local vars: VLT_STK_BYREF
            // to accommodate for this situation.
            if (varDsc->lvIsImplicitByRef)
            {
                assert(varDsc->lvIsParam);
                assert(varDsc->lvType == TYP_BYREF);
                this->vlType = VLT_STK_BYREF;
            }
            else
#endif // FEATURE_IMPLICIT_BYREFS
            {
                this->vlType = VLT_STK;
            }
            this->vlStk.vlsBaseReg = baseReg;
            this->vlStk.vlsOffset  = offset;
            if (!isFramePointerUsed && this->vlStk.vlsBaseReg == REG_SPBASE)
            {
                this->vlStk.vlsBaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
            }
            break;

#ifndef TARGET_64BIT
        case TYP_LONG:
        case TYP_DOUBLE:
            this->vlType             = VLT_STK2;
            this->vlStk2.vls2BaseReg = baseReg;
            this->vlStk2.vls2Offset  = offset;
            if (!isFramePointerUsed && this->vlStk2.vls2BaseReg == REG_SPBASE)
            {
                this->vlStk2.vls2BaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
            }
            break;
#endif // !TARGET_64BIT

        default:
            noway_assert(!"Invalid type");
    }
}

//------------------------------------------------------------------------
// siFillRegisterVarLoc: Fill "siVarLoc" struct indicating the register position of the variable
// using "LclVarDsc" and "baseReg"/"offset" if it has a part in the stack (x64 bit float or long).
//
// Arguments:
//    varDsc    - a "LclVarDsc *" to the variable it is desired to build the "siVarLoc".
//    varLoc    - a "siVarLoc &" to fill with the data of the "varDsc".
//    type      - a "var_types" which indicate the type of the variable.
//    baseReg   - a "regNumber" use as a base for the offset.
//    offset    - a signed amount of bytes distance from "baseReg" for the position of the variable.
//    isFramePointerUsed    - a boolean indicating whether the current method sets up an
//    explicit stack frame or not.
//
// Notes:
//    The "varLoc" argument is filled depending of the "type" argument but as a VLT_REG... variation.
//    "baseReg" and "offset" are used .for not 64 bit and values that are split in two parts.
void CodeGenInterface::siVarLoc::siFillRegisterVarLoc(
    const LclVarDsc* varDsc, var_types type, regNumber baseReg, int offset, bool isFramePointerUsed)
{
    switch (type)
    {
        case TYP_INT:
        case TYP_REF:
        case TYP_BYREF:
#ifdef TARGET_64BIT
        case TYP_LONG:
#endif // TARGET_64BIT
            this->vlType       = VLT_REG;
            this->vlReg.vlrReg = varDsc->GetRegNum();
            break;

#ifndef TARGET_64BIT
        case TYP_LONG:
            if (varDsc->GetOtherReg() != REG_STK)
            {
                this->vlType            = VLT_REG_REG;
                this->vlRegReg.vlrrReg1 = varDsc->GetRegNum();
                this->vlRegReg.vlrrReg2 = varDsc->GetOtherReg();
            }
            else
            {
                this->vlType                        = VLT_REG_STK;
                this->vlRegStk.vlrsReg              = varDsc->GetRegNum();
                this->vlRegStk.vlrsStk.vlrssBaseReg = baseReg;
                if (isFramePointerUsed && this->vlRegStk.vlrsStk.vlrssBaseReg == REG_SPBASE)
                {
                    this->vlRegStk.vlrsStk.vlrssBaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
                }
                this->vlRegStk.vlrsStk.vlrssOffset = offset + sizeof(int);
            }
            break;
#endif // !TARGET_64BIT

        case TYP_FLOAT:
        case TYP_DOUBLE:
            this->vlType       = VLT_REG_FP;
            this->vlReg.vlrReg = varDsc->GetRegNum();
            break;

#endif // !TARGET_64BIT

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        case TYP_SIMD64:
#endif // TARGET_XARCH
        {
            this->vlType = VLT_REG_FP;

            // Note: Need to initialize vlrReg field, otherwise during jit dump hitting an assert
            // in eeDispVar() --> getRegName() that regNumber is valid.
            this->vlReg.vlrReg = varDsc->GetRegNum();
            break;
        }
#endif // FEATURE_SIMD

        default:
            noway_assert(!"Invalid type");
    }
}

//------------------------------------------------------------------------
// siVarLoc: Non-empty constructor of siVarLoc struct
// Arguments:
//    varDsc    - a "LclVarDsc *" to the variable it is desired to build the "siVarLoc".
//    baseReg   - a "regNumber" use as a base for the offset.
//    offset    - a signed amount of bytes distance from "baseReg" for the position of the variable.
//    isFramePointerUsed - a boolean variable
//
// Notes:
//    Called for every psiScope in "psiScopeList" codegen.h
CodeGenInterface::siVarLoc::siVarLoc(const LclVarDsc* varDsc, regNumber baseReg, int offset, bool isFramePointerUsed)
{
    if (varDsc->lvIsInReg())
    {
        var_types regType = genActualType(varDsc->GetRegisterType());
        siFillRegisterVarLoc(varDsc, regType, baseReg, offset, isFramePointerUsed);
    }
    else
    {
        var_types stackType = genActualType(varDsc->TypeGet());
        siFillStackVarLoc(varDsc, stackType, baseReg, offset, isFramePointerUsed);
    }
}

//------------------------------------------------------------------------
// getSiVarLoc: Returns a "siVarLoc" instance representing the variable location.
//
// Arguments:
//    varDsc       - the variable it is desired to build the "siVarLoc".
//    stackLevel   - the current stack level. If the stack pointer changes in
//                   the function, we must adjust stack pointer-based local
//                   variable offsets to compensate.
//
// Return Value:
//    A "siVarLoc" representing the variable location, which could live
//    in a register, an stack position, or a combination of both.
//
CodeGenInterface::siVarLoc CodeGenInterface::getSiVarLoc(const LclVarDsc* varDsc, unsigned int stackLevel) const
{
    // For stack vars, find the base register, and offset

    regNumber baseReg;
    signed    offset = varDsc->GetStackOffset();

    if (!varDsc->lvFramePointerBased)
    {
        baseReg = REG_SPBASE;
        offset += stackLevel;
    }
    else
    {
        baseReg = REG_FPBASE;
    }

    return CodeGenInterface::siVarLoc(varDsc, baseReg, offset, isFramePointerUsed());
}

#ifdef DEBUG
void CodeGenInterface::dumpSiVarLoc(const siVarLoc* varLoc) const
{
    // "varLoc" cannot be null
    noway_assert(varLoc != nullptr);

    switch (varLoc->vlType)
    {
        case VLT_REG:
        case VLT_REG_BYREF:
        case VLT_REG_FP:
            printf("%s", getRegName(varLoc->vlReg.vlrReg));
            if (varLoc->vlType == VLT_REG_BYREF)
            {
                printf(" byref");
            }
            break;

        case VLT_STK:
        case VLT_STK_BYREF:
            if ((int)varLoc->vlStk.vlsBaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s[%d] (1 slot)", getRegName(varLoc->vlStk.vlsBaseReg), varLoc->vlStk.vlsOffset);
            }
            else
            {
                printf(STR_SPBASE "'[%d] (1 slot)", varLoc->vlStk.vlsOffset);
            }
            if (varLoc->vlType == VLT_REG_BYREF)
            {
                printf(" byref");
            }
            break;

#ifndef TARGET_AMD64
        case VLT_REG_REG:
            printf("%s-%s", getRegName(varLoc->vlRegReg.vlrrReg1), getRegName(varLoc->vlRegReg.vlrrReg2));
            break;

        case VLT_REG_STK:
            if ((int)varLoc->vlRegStk.vlrsStk.vlrssBaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s-%s[%d]", getRegName(varLoc->vlRegStk.vlrsReg),
                       getRegName(varLoc->vlRegStk.vlrsStk.vlrssBaseReg), varLoc->vlRegStk.vlrsStk.vlrssOffset);
            }
            else
            {
                printf("%s-" STR_SPBASE "'[%d]", getRegName(varLoc->vlRegStk.vlrsReg),
                       varLoc->vlRegStk.vlrsStk.vlrssOffset);
            }
            break;

        case VLT_STK_REG:
            unreached();

        case VLT_STK2:
            if ((int)varLoc->vlStk2.vls2BaseReg != (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                printf("%s[%d] (2 slots)", getRegName(varLoc->vlStk2.vls2BaseReg), varLoc->vlStk2.vls2Offset);
            }
            else
            {
                printf(STR_SPBASE "'[%d] (2 slots)", varLoc->vlStk2.vls2Offset);
            }
            break;

        case VLT_FPSTK:
            printf("ST(L-%d)", varLoc->vlFPstk.vlfReg);
            break;

        case VLT_FIXED_VA:
            printf("fxd_va[%d]", varLoc->vlFixedVarArg.vlfvOffset);
            break;
#endif // !TARGET_AMD64

        default:
            unreached();
    }
}
#endif // DEBUG

#ifdef DEBUG

//------------------------------------------------------------------------
//                      VariableLiveRanges dumpers
//------------------------------------------------------------------------

// Dump "VariableLiveRange" when code has not been generated and we don't have so the assembly native offset
// but at least "emitLocation"s and "siVarLoc"
void CodeGenInterface::VariableLiveKeeper::VariableLiveRange::dumpVariableLiveRange(
    const CodeGenInterface* codeGen) const
{
    codeGen->dumpSiVarLoc(&m_VarLocation);

    printf(" [");
    m_StartEmitLocation.Print(codeGen->GetCompiler()->compMethodID);
    printf(", ");
    if (m_EndEmitLocation.Valid())
    {
        m_EndEmitLocation.Print(codeGen->GetCompiler()->compMethodID);
    }
    else
    {
        printf("...");
    }
    printf("]");
}

// Dump "VariableLiveRange" when code has been generated and we have the assembly native offset of each "emitLocation"
void CodeGenInterface::VariableLiveKeeper::VariableLiveRange::dumpVariableLiveRange(
    emitter* emit, const CodeGenInterface* codeGen) const
{
    assert(emit != nullptr);

    // "VariableLiveRanges" are created setting its location ("m_VarLocation") and the initial native offset
    // ("m_StartEmitLocation")
    codeGen->dumpSiVarLoc(&m_VarLocation);

    // If this is an open "VariableLiveRange", "m_EndEmitLocation" is non-valid and print -1
    UNATIVE_OFFSET endAssemblyOffset = m_EndEmitLocation.Valid() ? m_EndEmitLocation.CodeOffset(emit) : -1;

    printf(" [%X, %X)", m_StartEmitLocation.CodeOffset(emit), m_EndEmitLocation.CodeOffset(emit));
}

//------------------------------------------------------------------------
//                      LiveRangeDumper
//------------------------------------------------------------------------

//------------------------------------------------------------------------
// resetDumper: If the "liveRange" has its last "VariableLiveRange" closed, it points
//  the "LiveRangeDumper" to end of "liveRange" (nullptr). Otherwise,
//  it points the "LiveRangeDumper" to the last "VariableLiveRange" of
//  "liveRange", which is opened.
//
// Arguments:
//  liveRanges - the "LiveRangeList" of the "VariableLiveDescriptor" we want to
//      update its "LiveRangeDumper".
//
// Notes:
//  This method is expected to be called once the code for a BasicBlock has been
//  generated and all the new "VariableLiveRange"s of the variable during this block
//  has been dumped.
//
void CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::resetDumper(const LiveRangeList* liveRanges)
{
    // There must have reported something in order to reset
    assert(m_hasLiveRangesToDump);

    if (liveRanges->back().m_EndEmitLocation.Valid())
    {
        // the last "VariableLiveRange" is closed and the variable
        // is no longer alive
        m_hasLiveRangesToDump = false;
    }
    else
    {
        // the last "VariableLiveRange" remains opened because it is
        // live at "BasicBlock"s "bbLiveOut".
        m_startingLiveRange = liveRanges->backPosition();
    }
}

//------------------------------------------------------------------------
// setDumperStartAt: Make "LiveRangeDumper" instance point at the last "VariableLiveRange"
// added so we can start dumping from there after the "BasicBlock"s code is generated.
//
// Arguments:
//  liveRangeIt - an iterator to a position in "VariableLiveDescriptor::m_VariableLiveRanges"
//
void CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::setDumperStartAt(const LiveRangeListIterator liveRangeIt)
{
    m_hasLiveRangesToDump = true;
    m_startingLiveRange   = liveRangeIt;
}

//------------------------------------------------------------------------
// getStartForDump: Return an iterator to the first "VariableLiveRange" edited/added
//  during the current "BasicBlock"
//
// Return Value:
//  A LiveRangeListIterator to the first "VariableLiveRange" in "LiveRangeList" which
//  was used during last "BasicBlock".
//
CodeGenInterface::VariableLiveKeeper::LiveRangeListIterator CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::
    getStartForDump() const
{
    return m_startingLiveRange;
}

//------------------------------------------------------------------------
// hasLiveRangesToDump: Return whether at least a "VariableLiveRange" was alive during
//  the current "BasicBlock"'s code generation
//
// Return Value:
//  A boolean indicating indicating if there is at least a "VariableLiveRange"
//  that has been used for the variable during last "BasicBlock".
//
bool CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::hasLiveRangesToDump() const
{
    return m_hasLiveRangesToDump;
}

#endif // DEBUG

//------------------------------------------------------------------------
//                      VariableLiveDescriptor
//------------------------------------------------------------------------

CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::VariableLiveDescriptor(
    CompAllocator allocator DEBUG_ARG(unsigned varNum))
{
    // Initialize an empty list
    m_VariableLiveRanges = new (allocator) LiveRangeList(allocator);

    INDEBUG(m_VariableLifeBarrier = new (allocator) LiveRangeDumper(m_VariableLiveRanges));
    INDEBUG(m_varNum = varNum);
}

//------------------------------------------------------------------------
// hasVariableLiveRangeOpen: Return true if the variable is still alive,
//  false in other case.
//
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVariableLiveRangeOpen() const
{
    return !m_VariableLiveRanges->empty() && !m_VariableLiveRanges->back().m_EndEmitLocation.Valid();
}

//------------------------------------------------------------------------
// getLiveRanges: Return the list of variable locations for this variable.
//
// Return Value:
//  A const LiveRangeList* pointing to the first variable location if it has
//  any or the end of the list in other case.
//
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::
    getLiveRanges() const
{
    return m_VariableLiveRanges;
}

//------------------------------------------------------------------------
// startLiveRangeFromEmitter: Report this variable as being born in "varLocation"
//  at the instruction where "emit" is located.
//
// Arguments:
//  varLocation  - the home of the variable.
//  emit - an emitter* instance located at the first instruction where "varLocation" becomes valid.
//
// Assumptions:
//  This variable is being born so it should currently be dead.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::startLiveRangeFromEmitter(
    CodeGenInterface::siVarLoc varLocation, emitter* emit) const
{
    noway_assert(emit != nullptr);

    // Is the first "VariableLiveRange" or the previous one has been closed so its "m_EndEmitLocation" is valid
    noway_assert(m_VariableLiveRanges->empty() || m_VariableLiveRanges->back().m_EndEmitLocation.Valid());

    if (!m_VariableLiveRanges->empty() &&
        siVarLoc::Equals(&varLocation, &(m_VariableLiveRanges->back().m_VarLocation)) &&
        m_VariableLiveRanges->back().m_EndEmitLocation.IsPreviousInsNum(emit))
    {
        JITDUMP("Debug: Extending V%02u debug range...\n", m_varNum);

        // The variable is being born just after the instruction at which it died.
        // In this case, i.e. an update of the variable's value, we coalesce the live ranges.
        m_VariableLiveRanges->back().m_EndEmitLocation.Init();
    }
    else
    {
        JITDUMP("Debug: New V%02u debug range: %s\n", m_varNum,
                m_VariableLiveRanges->empty()
                    ? "first"
                    : siVarLoc::Equals(&varLocation, &(m_VariableLiveRanges->back().m_VarLocation))
                          ? "new var or location"
                          : "not adjacent");
        // Creates new live range with invalid end
        m_VariableLiveRanges->emplace_back(varLocation, emitLocation(), emitLocation());
        m_VariableLiveRanges->back().m_StartEmitLocation.CaptureLocation(emit);
    }

#ifdef DEBUG
    if (!m_VariableLifeBarrier->hasLiveRangesToDump())
    {
        m_VariableLifeBarrier->setDumperStartAt(m_VariableLiveRanges->backPosition());
    }
#endif // DEBUG

    // m_startEmitLocation must be Valid. m_EndEmitLocation must not be valid.
    noway_assert(m_VariableLiveRanges->back().m_StartEmitLocation.Valid());
    noway_assert(!m_VariableLiveRanges->back().m_EndEmitLocation.Valid());
}

//------------------------------------------------------------------------
// endLiveRangeAtEmitter: Report this variable as becoming dead starting at the
//  instruction where "emit" is located.
//
// Arguments:
//  emit - an emitter* instance located at the first instruction where
//   this variable becomes dead.
//
// Assumptions:
//  This variable is becoming dead so it should currently be alive.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::endLiveRangeAtEmitter(emitter* emit) const
{
    noway_assert(emit != nullptr);
    noway_assert(hasVariableLiveRangeOpen());

    // Using [close, open) ranges so as to not compute the size of the last instruction
    m_VariableLiveRanges->back().m_EndEmitLocation.CaptureLocation(emit);

    JITDUMP("Debug: Closing V%02u debug range.\n", m_varNum);

    // m_EndEmitLocation must be Valid
    noway_assert(m_VariableLiveRanges->back().m_EndEmitLocation.Valid());
}

//------------------------------------------------------------------------
// updateLiveRangeAtEmitter: Report this variable as changing its variable
//  home to "varLocation" at the instruction where "emit" is located.
//
// Arguments:
//  varLocation  - the new variable location.
//  emit - an emitter* instance located at the first instruction where "varLocation" becomes valid.
//
// Assumptions:
//  This variable should already be alive.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::updateLiveRangeAtEmitter(
    CodeGenInterface::siVarLoc varLocation, emitter* emit) const
{
    // This variable is changing home so it has been started before during this block
    noway_assert(m_VariableLiveRanges != nullptr && !m_VariableLiveRanges->empty());

    // And its last m_EndEmitLocation has to be invalid
    noway_assert(!m_VariableLiveRanges->back().m_EndEmitLocation.Valid());

    // If we are reporting again the same home, that means we are doing something twice?
    // noway_assert(! CodeGenInterface::siVarLoc::Equals(&m_VariableLiveRanges->back().m_VarLocation, varLocation));

    // Close previous live range
    endLiveRangeAtEmitter(emit);

    startLiveRangeFromEmitter(varLocation, emit);
}

#ifdef DEBUG
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::dumpAllRegisterLiveRangesForBlock(
    emitter* emit, const CodeGenInterface* codeGen) const
{
    bool first = true;
    for (LiveRangeListIterator it = m_VariableLiveRanges->begin(); it != m_VariableLiveRanges->end(); it++)
    {
        if (!first)
        {
            printf("; ");
        }
        it->dumpVariableLiveRange(emit, codeGen);
        first = false;
    }
}

void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::dumpRegisterLiveRangesForBlockBeforeCodeGenerated(
    const CodeGenInterface* codeGen) const
{
    bool first = true;
    for (LiveRangeListIterator it = m_VariableLifeBarrier->getStartForDump(); it != m_VariableLiveRanges->end(); it++)
    {
        if (!first)
        {
            printf("; ");
        }
        it->dumpVariableLiveRange(codeGen);
        first = false;
    }
}

// Returns true if a live range for this variable has been recorded
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVarLiveRangesToDump() const
{
    return !m_VariableLiveRanges->empty();
}

// Returns true if a live range for this variable has been recorded from last call to EndBlock
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVarLiveRangesFromLastBlockToDump() const
{
    return m_VariableLifeBarrier->hasLiveRangesToDump();
}

// Reset the barrier so as to dump only next block changes on next block
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::endBlockLiveRanges()
{
    // make "m_VariableLifeBarrier->m_startingLiveRange" now points to nullptr for printing purposes
    m_VariableLifeBarrier->resetDumper(m_VariableLiveRanges);
}
#endif // DEBUG

//------------------------------------------------------------------------
//                      VariableLiveKeeper
//------------------------------------------------------------------------

// Initialize structures for VariableLiveRanges
void CodeGenInterface::initializeVariableLiveKeeper()
{
    CompAllocator allocator = compiler->getAllocator(CMK_VariableLiveRanges);

    int amountTrackedVariables = compiler->opts.compDbgInfo ? compiler->info.compLocalsCount : 0;
    int amountTrackedArgs      = compiler->opts.compDbgInfo ? compiler->info.compArgsCount : 0;

    varLiveKeeper = new (allocator) VariableLiveKeeper(amountTrackedVariables, amountTrackedArgs, compiler, allocator);
}

CodeGenInterface::VariableLiveKeeper* CodeGenInterface::getVariableLiveKeeper() const
{
    return varLiveKeeper;
};

//------------------------------------------------------------------------
// VariableLiveKeeper: Create an instance of the object in charge of managing
//  VariableLiveRanges and initialize the array "m_vlrLiveDsc".
//
// Arguments:
//    totalLocalCount   - the count of args, special args and IL Local
//      variables in the method.
//    argsCount         - the count of args and special args in the method.
//    compiler          - a compiler instance
//
CodeGenInterface::VariableLiveKeeper::VariableLiveKeeper(unsigned int  totalLocalCount,
                                                         unsigned int  argsCount,
                                                         Compiler*     comp,
                                                         CompAllocator allocator)
    : m_LiveDscCount(totalLocalCount)
    , m_LiveArgsCount(argsCount)
    , m_Compiler(comp)
    , m_LastBasicBlockHasBeenEmitted(false)
{
    if (m_LiveDscCount > 0)
    {
        // Allocate memory for "m_vlrLiveDsc" and initialize each "VariableLiveDescriptor"
        m_vlrLiveDsc          = allocator.allocate<VariableLiveDescriptor>(m_LiveDscCount);
        m_vlrLiveDscForProlog = allocator.allocate<VariableLiveDescriptor>(m_LiveDscCount);

        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            new (m_vlrLiveDsc + varNum, jitstd::placement_t()) VariableLiveDescriptor(allocator DEBUG_ARG(varNum));
            new (m_vlrLiveDscForProlog + varNum, jitstd::placement_t())
                VariableLiveDescriptor(allocator DEBUG_ARG(varNum));
        }
    }
}

//------------------------------------------------------------------------
// siStartOrCloseVariableLiveRange: Reports the given variable as being born or becoming dead.
//
// Arguments:
//    varDsc    - the variable for which a location changed will be reported
//    varNum    - the index of the variable in "lvaTable"
//    isBorn    - true if the variable is being born where the emitter is located.
//    isDying   - true if the variable is dying where the emitter is located.
//
// Assumptions:
//    The emitter should be located on the first instruction where
//    the variable is becoming valid (when isBorn is true) or invalid (when isDying is true).
//
// Notes:
//    This method is being called from treeLifeUpdater when the variable is being born,
//    becoming dead, or both.
//
void CodeGenInterface::VariableLiveKeeper::siStartOrCloseVariableLiveRange(const LclVarDsc* varDsc,
                                                                           unsigned int     varNum,
                                                                           bool             isBorn,
                                                                           bool             isDying)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exists in the IL, "this", and special arguments
    // are reported.
    if (m_Compiler->opts.compDbgInfo && varNum < m_LiveDscCount)
    {
        if (isBorn && !isDying)
        {
            // "varDsc" is valid from this point
            siStartVariableLiveRange(varDsc, varNum);
        }
        if (isDying && !isBorn)
        {
            // this variable live range is no longer valid from this point
            siEndVariableLiveRange(varNum);
        }
    }
}

//------------------------------------------------------------------------
// siStartOrCloseVariableLiveRanges: Iterates the given set of variables
//  calling "siStartOrCloseVariableLiveRange" with each one.
//
// Arguments:
//    varsIndexSet    - the set of variables to report start/end "VariableLiveRange"
//    isBorn    - whether the set is being born from where the emitter is located.
//    isDying   - whether the set is dying from where the emitter is located.
//
// Assumptions:
//    The emitter should be located on the first instruction from where is true that
//    the variable becoming valid (when isBorn is true) or invalid (when isDying is true).
//
// Notes:
//    This method is being called from treeLifeUpdater when a set of variables
//    is being born, becoming dead, or both.
//
void CodeGenInterface::VariableLiveKeeper::siStartOrCloseVariableLiveRanges(VARSET_VALARG_TP varsIndexSet,
                                                                            bool             isBorn,
                                                                            bool             isDying)
{
    if (m_Compiler->opts.compDbgInfo)
    {
        VarSetOps::Iter iter(m_Compiler, varsIndexSet);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            unsigned int     varNum = m_Compiler->lvaTrackedIndexToLclNum(varIndex);
            const LclVarDsc* varDsc = m_Compiler->lvaGetDesc(varNum);
            siStartOrCloseVariableLiveRange(varDsc, varNum, isBorn, isDying);
        }
    }
}

//------------------------------------------------------------------------
// siStartVariableLiveRange: Reports the given variable as being born.
//
// Arguments:
//    varDsc    - the variable descriptor for which a location change will be reported
//    varNum    - the variable number
//
// Assumptions:
//    The emitter should be pointing to the first instruction where the VariableLiveRange is
//    becoming valid.
//    The given "varDsc" should have its VariableRangeLists initialized.
//
// Notes:
//    This method should be called at every location where a variable is becoming live.
//
void CodeGenInterface::VariableLiveKeeper::siStartVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exists in the IL, "this", and special arguments are reported, as long as they were
    // allocated.
    if (m_Compiler->opts.compDbgInfo && (varNum < m_LiveDscCount) && (varDsc->lvIsInReg() || varDsc->lvOnFrame))
    {
        // Build siVarLoc for this born "varDsc"
        CodeGenInterface::siVarLoc varLocation =
            m_Compiler->codeGen->getSiVarLoc(varDsc, m_Compiler->codeGen->getCurrentStackLevel());

        VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDsc[varNum];
        // this variable live range is valid from this point
        varLiveDsc->startLiveRangeFromEmitter(varLocation, m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siEndVariableLiveRange: Reports the variable as becoming dead.
//
// Arguments:
//    varNum    - the index of the variable at m_vlrLiveDsc or lvaTable in that
//       is becoming dead.
//
// Assumptions:
//    The given variable should be alive.
//    The emitter should be pointing to the first instruction where the VariableLiveRange is
//    becoming invalid.
//
// Notes:
//    This method should be called at every location where a variable is becoming dead.
//
void CodeGenInterface::VariableLiveKeeper::siEndVariableLiveRange(unsigned int varNum)
{
    // Only the variables that exists in the IL, "this", and special arguments
    // will be reported.

    // This method is being called from genUpdateLife, which is called after
    // code for BasicBlock has been generated, but the emitter no longer has
    // a valid IG so we don't report the close of a "VariableLiveRange" after code is
    // emitted.

    if (m_Compiler->opts.compDbgInfo && (varNum < m_LiveDscCount) && !m_LastBasicBlockHasBeenEmitted &&
        m_vlrLiveDsc[varNum].hasVariableLiveRangeOpen())
    {
        // this variable live range is no longer valid from this point
        m_vlrLiveDsc[varNum].endLiveRangeAtEmitter(m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siUpdateVariableLiveRange: Reports the change of variable location for the
//  given variable.
//
// Arguments:
//    varDsc    - the variable descriptor for which the home has changed.
//    varNum    - the variable number
//
// Assumptions:
//    The given variable should be alive.
//    The emitter should be pointing to the first instruction where
//    the new variable location is becoming valid.
//
void CodeGenInterface::VariableLiveKeeper::siUpdateVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exist in the IL, "this", and special arguments
    // will be reported. These are locals and arguments, and are counted in
    // "info.compLocalsCount".

    // This method is being called when the prolog is being generated, and
    // the emitter no longer has a valid IG so we don't report the close of
    // a "VariableLiveRange" after code is emitted.
    if (m_Compiler->opts.compDbgInfo && (varNum < m_LiveDscCount) && !m_LastBasicBlockHasBeenEmitted)
    {
        // Build the location of the variable
        CodeGenInterface::siVarLoc siVarLoc =
            m_Compiler->codeGen->getSiVarLoc(varDsc, m_Compiler->codeGen->getCurrentStackLevel());

        // Report the home change for this variable
        VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDsc[varNum];
        varLiveDsc->updateLiveRangeAtEmitter(siVarLoc, m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siEndAllVariableLiveRange: Reports the set of variables as becoming dead.
//
// Arguments:
//    newLife    - the set of variables that are becoming dead.
//
// Assumptions:
//    All the variables in the set are alive.
//
// Notes:
//    This method is called when the last block being generated to killed all
//    the live variables and set a flag to avoid reporting variable locations for
//    on next calls to method that update variable liveness.
//
void CodeGenInterface::VariableLiveKeeper::siEndAllVariableLiveRange(VARSET_VALARG_TP varsToClose)
{
    if (m_Compiler->opts.compDbgInfo)
    {
        if (m_Compiler->lvaTrackedCount > 0 || !m_Compiler->opts.OptimizationDisabled())
        {
            VarSetOps::Iter iter(m_Compiler, varsToClose);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                unsigned int varNum = m_Compiler->lvaTrackedIndexToLclNum(varIndex);
                siEndVariableLiveRange(varNum);
            }
        }
        else
        {
            // It seems we are compiling debug code, so we don't have variable
            //  liveness info
            siEndAllVariableLiveRange();
        }
    }

    m_LastBasicBlockHasBeenEmitted = true;
}

//------------------------------------------------------------------------
// siEndAllVariableLiveRange: Reports all live variables as dead.
//
// Notes:
//    This overload exists for the case we are compiling code compiled in
//    debug mode. When that happen we don't have variable liveness info
//    as "BasicBlock::bbLiveIn" or "BasicBlock::bbLiveOut" and there is no
//    tracked variable.
//
void CodeGenInterface::VariableLiveKeeper::siEndAllVariableLiveRange()
{
    // TODO: we can improve this keeping a set for the variables with
    // open VariableLiveRanges

    for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
    {
        const VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;
        if (varLiveDsc->hasVariableLiveRangeOpen())
        {
            siEndVariableLiveRange(varNum);
        }
    }
}

//------------------------------------------------------------------------
// getLiveRangesForVarForBody: Return the "VariableLiveRange" that correspond to
//  the given "varNum".
//
// Arguments:
//  varNum  - the index of the variable in m_vlrLiveDsc, which is the same as
//      in lvaTable.
//
// Return Value:
//  A const pointer to the list of variable locations reported for the variable.
//
// Assumptions:
//  This variable should be an argument, a special argument or an IL local
//  variable.
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::getLiveRangesForVarForBody(
    unsigned int varNum) const
{
    // There should be at least one variable for which its liveness is tracked
    noway_assert(varNum < m_LiveDscCount);

    return m_vlrLiveDsc[varNum].getLiveRanges();
}

//------------------------------------------------------------------------
// getLiveRangesForVarForProlog: Return the "VariableLiveRange" that correspond to
//  the given "varNum".
//
// Arguments:
//  varNum  - the index of the variable in m_vlrLiveDsc, which is the same as
//      in lvaTable.
//
// Return Value:
//  A const pointer to the list of variable locations reported for the variable.
//
// Assumptions:
//  This variable should be an argument, a special argument or an IL local
//  variable.
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::getLiveRangesForVarForProlog(
    unsigned int varNum) const
{
    // There should be at least one variable for which its liveness is tracked
    noway_assert(varNum < m_LiveDscCount);

    return m_vlrLiveDscForProlog[varNum].getLiveRanges();
}

//------------------------------------------------------------------------
// getLiveRangesCount: Returns the count of variable locations reported for the tracked
//  variables, which are arguments, special arguments, and local IL variables.
//
// Return Value:
//    size_t - the count of variable locations
//
// Notes:
//    This method is being called from "genSetScopeInfo" to know the count of
//    "varResultInfo" that should be created on eeSetLVcount.
//
size_t CodeGenInterface::VariableLiveKeeper::getLiveRangesCount() const
{
    size_t liveRangesCount = 0;

    if (m_Compiler->opts.compDbgInfo)
    {
        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            for (int i = 0; i < 2; i++)
            {
                VariableLiveDescriptor* varLiveDsc = (i == 0 ? m_vlrLiveDscForProlog : m_vlrLiveDsc) + varNum;

                if (m_Compiler->compMap2ILvarNum(varNum) != (unsigned int)ICorDebugInfo::UNKNOWN_ILNUM)
                {
                    liveRangesCount += varLiveDsc->getLiveRanges()->size();
                }
            }
        }
    }
    return liveRangesCount;
}

//------------------------------------------------------------------------
// psiStartVariableLiveRange: Reports the given variable as being born.
//
// Arguments:
//  varLocation - the variable location
//  varNum      - the index of the variable in "compiler->lvaTable" or
//      "VariableLiveKeeper->m_vlrLiveDsc"
//
// Notes:
//  This function is expected to be called from "psiBegProlog" during
//  prolog code generation.
//
void CodeGenInterface::VariableLiveKeeper::psiStartVariableLiveRange(CodeGenInterface::siVarLoc varLocation,
                                                                     unsigned int               varNum)
{
    // This descriptor has to correspond to a parameter. The first slots in lvaTable
    // are arguments and special arguments.
    noway_assert(varNum < m_LiveArgsCount);

    VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDscForProlog[varNum];
    varLiveDsc->startLiveRangeFromEmitter(varLocation, m_Compiler->GetEmitter());
}

//------------------------------------------------------------------------
// psiClosePrologVariableRanges: Report all the parameters as becoming dead.
//
// Notes:
//  This function is expected to be called from "psiEndProlog" after
//  code for prolog has been generated.
//
void CodeGenInterface::VariableLiveKeeper::psiClosePrologVariableRanges()
{
    noway_assert(m_LiveArgsCount <= m_LiveDscCount);

    for (unsigned int varNum = 0; varNum < m_LiveArgsCount; varNum++)
    {
        VariableLiveDescriptor* varLiveDsc = m_vlrLiveDscForProlog + varNum;

        if (varLiveDsc->hasVariableLiveRangeOpen())
        {
            varLiveDsc->endLiveRangeAtEmitter(m_Compiler->GetEmitter());
        }
    }
}

#ifdef DEBUG
void CodeGenInterface::VariableLiveKeeper::dumpBlockVariableLiveRanges(const BasicBlock* block)
{
    assert(block != nullptr);

    bool hasDumpedHistory = false;

    printf("\nVariable Live Range History Dump for " FMT_BB "\n", block->bbNum);

    if (m_Compiler->opts.compDbgInfo)
    {
        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;

            if (varLiveDsc->hasVarLiveRangesFromLastBlockToDump())
            {
                hasDumpedHistory = true;
                m_Compiler->gtDispLclVar(varNum, false);
                printf(": ");
                varLiveDsc->dumpRegisterLiveRangesForBlockBeforeCodeGenerated(m_Compiler->codeGen);
                varLiveDsc->endBlockLiveRanges();
                printf("\n");
            }
        }
    }

    if (!hasDumpedHistory)
    {
        printf("..None..\n");
    }
}

void CodeGenInterface::VariableLiveKeeper::dumpLvaVariableLiveRanges() const
{
    bool hasDumpedHistory = false;

    printf("VARIABLE LIVE RANGES:\n");

    if (m_Compiler->opts.compDbgInfo)
    {
        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;

            if (varLiveDsc->hasVarLiveRangesToDump())
            {
                hasDumpedHistory = true;
                m_Compiler->gtDispLclVar(varNum, false);
                printf(": ");
                varLiveDsc->dumpAllRegisterLiveRangesForBlock(m_Compiler->GetEmitter(), m_Compiler->codeGen);
                printf("\n");
            }
        }
    }

    if (!hasDumpedHistory)
    {
        printf("..None..\n");
    }
}
#endif // DEBUG

/*============================================================================
 *           INTERFACE (public) Functions for ScopeInfo
 *============================================================================
 */

// Check every CodeGenInterface::siVarLocType and CodeGenInterface::siVarLoc
// are what ICodeDebugInfo is expecting.
void CodeGen::checkICodeDebugInfo()
{
#ifdef TARGET_X86
    assert((unsigned)ICorDebugInfo::REGNUM_EAX == REG_EAX);
    assert((unsigned)ICorDebugInfo::REGNUM_ECX == REG_ECX);
    assert((unsigned)ICorDebugInfo::REGNUM_EDX == REG_EDX);
    assert((unsigned)ICorDebugInfo::REGNUM_EBX == REG_EBX);
    assert((unsigned)ICorDebugInfo::REGNUM_ESP == REG_ESP);
    assert((unsigned)ICorDebugInfo::REGNUM_EBP == REG_EBP);
    assert((unsigned)ICorDebugInfo::REGNUM_ESI == REG_ESI);
    assert((unsigned)ICorDebugInfo::REGNUM_EDI == REG_EDI);
#endif

    assert((unsigned)ICorDebugInfo::VLT_REG == CodeGenInterface::VLT_REG);
    assert((unsigned)ICorDebugInfo::VLT_STK == CodeGenInterface::VLT_STK);
    assert((unsigned)ICorDebugInfo::VLT_REG_REG == CodeGenInterface::VLT_REG_REG);
    assert((unsigned)ICorDebugInfo::VLT_REG_STK == CodeGenInterface::VLT_REG_STK);
    assert((unsigned)ICorDebugInfo::VLT_STK_REG == CodeGenInterface::VLT_STK_REG);
    assert((unsigned)ICorDebugInfo::VLT_STK2 == CodeGenInterface::VLT_STK2);
    assert((unsigned)ICorDebugInfo::VLT_FPSTK == CodeGenInterface::VLT_FPSTK);
    assert((unsigned)ICorDebugInfo::VLT_FIXED_VA == CodeGenInterface::VLT_FIXED_VA);
    assert((unsigned)ICorDebugInfo::VLT_COUNT == CodeGenInterface::VLT_COUNT);
    assert((unsigned)ICorDebugInfo::VLT_INVALID == CodeGenInterface::VLT_INVALID);

    /* ICorDebugInfo::VarLoc and siVarLoc should overlap exactly as we cast
     * one to the other in eeSetLVinfo()
     * Below is a "required but not sufficient" condition
     */

    assert(sizeof(ICorDebugInfo::VarLoc) == sizeof(CodeGenInterface::siVarLoc));
}

void CodeGen::siInit()
{
    checkICodeDebugInfo();

    assert(compiler->opts.compScopeInfo);

#if defined(FEATURE_EH_FUNCLETS)
    if (compiler->info.compVarScopesCount > 0)
    {
        siInFuncletRegion = false;
    }
#endif // FEATURE_EH_FUNCLETS

    siLastEndOffs = 0;

    compiler->compResetScopeLists();
}

/*****************************************************************************
 *                          siBeginBlock
 *
 * Called at the beginning of code-gen for a block. Checks if any scopes
 * need to be opened.
 */

void CodeGen::siBeginBlock(BasicBlock* block)
{
    assert(block != nullptr);

    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

    if (compiler->info.compVarScopesCount == 0)
    {
        return;
    }

#if defined(FEATURE_EH_FUNCLETS)
    if (siInFuncletRegion)
    {
        return;
    }

    if (block->HasFlag(BBF_FUNCLET_BEG))
    {
        // For now, don't report any scopes in funclets. JIT64 doesn't.
        siInFuncletRegion = true;

        JITDUMP("Scope info: found beginning of funclet region at block " FMT_BB "; ignoring following blocks\n",
                block->bbNum);

        return;
    }
#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG
    if (verbose)
    {
        printf("\nScope info: begin block " FMT_BB ", IL range ", block->bbNum);
        block->dspBlockILRange();
        printf("\n");
    }
#endif // DEBUG

    unsigned beginOffs = block->bbCodeOffs;

    if (beginOffs == BAD_IL_OFFSET)
    {
        JITDUMP("Scope info: ignoring block beginning\n");
        return;
    }

    // If we have tracked locals, use liveness to update the debug state.
    //
    // Note: we can improve on this some day -- if there are any tracked
    // locals, untracked locals will fail to be reported.
    if (compiler->lvaTrackedCount <= 0)
    {
        siOpenScopesForNonTrackedVars(block, siLastEndOffs);
    }
}

//------------------------------------------------------------------------
// siOpenScopesForNonTrackedVars: If optimizations are disable, it will open
//  a "siScope" for each variable which has a "VarScopeDsc" (input of the JIT)
//  and is referenced at least once. If optimizations are applied, nothing is done.
//
// Arguments:
//    block   - the block whose code is going to be generated.
//    lastBlockILEndOffset         - the IL offset at the ending of the last generated basic block.
//
// Notes:
//    When there we are jitting methods compiled in debug mode, no variable is
//    tracked and there is no info that shows variable liveness like block->bbLiveIn.
//    On debug code variables are not enregistered the whole method so we can just
//    report them as beign born from here on the stack until the whole method is
//    generated.
//
void CodeGen::siOpenScopesForNonTrackedVars(const BasicBlock* block, unsigned int lastBlockILEndOffset)
{
    unsigned int beginOffs = block->bbCodeOffs;

    // There aren't any tracked locals.
    //
    // For debuggable or minopts code, scopes can begin only on block boundaries.
    // For other codegen modes (eg minopts/tier0) we currently won't report any
    // untracked locals.
    if (compiler->opts.OptimizationDisabled())
    {
        // Check if there are any scopes on the current block's start boundary.
        VarScopeDsc* varScope = nullptr;

#if defined(FEATURE_EH_FUNCLETS)

        // If we find a spot where the code offset isn't what we expect, because
        // there is a gap, it might be because we've moved the funclets out of
        // line. Catch up with the enter and exit scopes of the current block.
        // Ignore the enter/exit scope changes of the missing scopes, which for
        // funclets must be matched.
        if (lastBlockILEndOffset != beginOffs)
        {
            assert(beginOffs > 0);
            assert(lastBlockILEndOffset < beginOffs);

            JITDUMP("Scope info: found offset hole. lastOffs=%u, currOffs=%u\n", lastBlockILEndOffset, beginOffs);

            // Skip enter scopes
            while ((varScope = compiler->compGetNextEnterScope(beginOffs - 1, true)) != nullptr)
            {
                /* do nothing */
                JITDUMP("Scope info: skipping enter scope, LVnum=%u\n", varScope->vsdLVnum);
            }

            // Skip exit scopes
            while ((varScope = compiler->compGetNextExitScope(beginOffs - 1, true)) != nullptr)
            {
                /* do nothing */
                JITDUMP("Scope info: skipping exit scope, LVnum=%u\n", varScope->vsdLVnum);
            }
        }

#else // !FEATURE_EH_FUNCLETS

        if (lastBlockILEndOffset != beginOffs)
        {
            assert(lastBlockILEndOffset < beginOffs);
            return;
        }

#endif // !FEATURE_EH_FUNCLETS

        while ((varScope = compiler->compGetNextEnterScope(beginOffs)) != nullptr)
        {
            LclVarDsc* lclVarDsc = compiler->lvaGetDesc(varScope->vsdVarNum);

            // Only report locals that were referenced, if we're not doing debug codegen
            if (compiler->opts.compDbgCode || (lclVarDsc->lvRefCnt() > 0))
            {
                // brace-matching editor workaround for following line: (
                JITDUMP("Scope info: opening scope, LVnum=%u [%03X..%03X)\n", varScope->vsdLVnum, varScope->vsdLifeBeg,
                        varScope->vsdLifeEnd);

                varLiveKeeper->siStartVariableLiveRange(lclVarDsc, varScope->vsdVarNum);

                INDEBUG(assert(!lclVarDsc->lvTracked ||
                               VarSetOps::IsMember(compiler, block->bbLiveIn, lclVarDsc->lvVarIndex)));
            }
            else
            {
                JITDUMP("Skipping open scope for V%02u, unreferenced\n", varScope->vsdVarNum);
            }
        }
    }
}

/*****************************************************************************
 *                          siEndBlock
 *
 * Called at the end of code-gen for a block. Any closing scopes are marked
 * as such. Note that if we are collecting LocalVar info, scopes can
 * only begin or end at block boundaries for debuggable code.
 */

void CodeGen::siEndBlock(BasicBlock* block)
{
    assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));

#if defined(FEATURE_EH_FUNCLETS)
    if (siInFuncletRegion)
    {
        return;
    }
#endif // FEATURE_EH_FUNCLETS

    unsigned endOffs = block->bbCodeOffsEnd;

    if (endOffs == BAD_IL_OFFSET)
    {
        JITDUMP("Scope info: ignoring block end\n");
        return;
    }

    siLastEndOffs = endOffs;
}

//------------------------------------------------------------------------
// psiGetVarStackOffset: Return the offset of the lclVarDsc on the stack.
//
// Arguments:
//    lclVarDsc - the LclVarDsc from whom the offset is asked.
//
NATIVE_OFFSET CodeGen::psiGetVarStackOffset(const LclVarDsc* lclVarDsc) const
{
    noway_assert(lclVarDsc != nullptr);

    NATIVE_OFFSET stackOffset = 0;

#ifdef TARGET_AMD64
    // scOffset = offset from caller SP - REGSIZE_BYTES
    // TODO-Cleanup - scOffset needs to be understood.  For now just matching with the existing definition.
    stackOffset = compiler->lvaToCallerSPRelativeOffset(lclVarDsc->GetStackOffset(), lclVarDsc->lvFramePointerBased) +
                  REGSIZE_BYTES;
#else  // !TARGET_AMD64
    if (doubleAlignOrFramePointerUsed())
    {
        // REGSIZE_BYTES - for the pushed value of EBP
        stackOffset = lclVarDsc->GetStackOffset() - REGSIZE_BYTES;
    }
    else
    {
        stackOffset = lclVarDsc->GetStackOffset() - genTotalFrameSize();
    }
#endif // !TARGET_AMD64

    return stackOffset;
}

/*============================================================================
*           INTERFACE (public) Functions for PrologScopeInfo
*============================================================================
*/

//------------------------------------------------------------------------
// psiBegProlog: Initializes the PrologScopeInfo creating open psiScopes or
//  VariableLiveRanges for all the parameters of the method depending on which
//  flag is being used.
//
void CodeGen::psiBegProlog()
{
    assert(compiler->compGeneratingProlog);

    compiler->compResetScopeLists();

    VarScopeDsc* varScope;
    while ((varScope = compiler->compGetNextEnterScope(0)) != nullptr)
    {
        LclVarDsc* lclVarDsc = compiler->lvaGetDesc(varScope->vsdVarNum);

        if (!lclVarDsc->lvIsParam)
        {
            continue;
        }
        siVarLoc varLocation;

        if (lclVarDsc->lvIsRegArg)
        {
            bool isStructHandled = false;
#if defined(UNIX_AMD64_ABI)
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            if (varTypeIsStruct(lclVarDsc))
            {
                CORINFO_CLASS_HANDLE typeHnd = lclVarDsc->GetLayout()->GetClassHandle();
                assert(typeHnd != nullptr);
                compiler->eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
                if (structDesc.passedInRegisters)
                {
                    regNumber regNum      = REG_NA;
                    regNumber otherRegNum = REG_NA;
                    for (unsigned nCnt = 0; nCnt < structDesc.eightByteCount; nCnt++)
                    {
                        var_types regType = TYP_UNDEF;

                        if (nCnt == 0)
                        {
                            regNum = lclVarDsc->GetArgReg();
                        }
                        else if (nCnt == 1)
                        {
                            otherRegNum = lclVarDsc->GetOtherArgReg();
                        }
                        else
                        {
                            assert(false && "Invalid eightbyte number.");
                        }

                        regType = compiler->GetEightByteType(structDesc, nCnt);
#ifdef DEBUG
                        regType = compiler->mangleVarArgsType(regType);
                        assert(genMapRegNumToRegArgNum((nCnt == 0 ? regNum : otherRegNum), regType) != (unsigned)-1);
#endif // DEBUG
                    }

                    varLocation.storeVariableInRegisters(regNum, otherRegNum);
                }
                else
                {
                    // Stack passed argument. Get the offset from the  caller's frame.
                    varLocation.storeVariableOnStack(REG_SPBASE, psiGetVarStackOffset(lclVarDsc));
                }

                isStructHandled = true;
            }
#endif // !defined(UNIX_AMD64_ABI)
            if (!isStructHandled)
            {
#ifdef DEBUG
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
                var_types regType;
                if (varTypeIsStruct(lclVarDsc))
                {
                    // Must be <= 16 bytes or else it wouldn't be passed in registers,
                    // which can be bigger (and is handled above).
                    noway_assert(EA_SIZE_IN_BYTES(lclVarDsc->lvSize()) <= 16);
                    if (emitter::isFloatReg(lclVarDsc->GetArgReg()))
                    {
                        regType = TYP_DOUBLE;
                    }
                    else
                    {
                        regType = lclVarDsc->GetLayout()->GetGCPtrType(0);
                    }
                }
                else
                {
                    regType = compiler->mangleVarArgsType(lclVarDsc->TypeGet());
                    if (emitter::isGeneralRegisterOrR0(lclVarDsc->GetArgReg()) && isFloatRegType(regType))
                    {
                        // For LoongArch64 and RISCV64's ABI, the float args may be passed by integer register.
                        regType = TYP_LONG;
                    }
                }
#else
                var_types regType = compiler->mangleVarArgsType(lclVarDsc->TypeGet());
                if (lclVarDsc->lvIsHfaRegArg())
                {
                    regType = lclVarDsc->GetHfaType();
                }
#endif // defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
                assert(genMapRegNumToRegArgNum(lclVarDsc->GetArgReg(), regType) != (unsigned)-1);
#endif // DEBUG
                varLocation.storeVariableInRegisters(lclVarDsc->GetArgReg(), REG_NA);
            }
        }
        else
        {
            varLocation.storeVariableOnStack(REG_SPBASE, psiGetVarStackOffset(lclVarDsc));
        }

        // Start a VariableLiveRange for this LclVarDsc on the built location
        varLiveKeeper->psiStartVariableLiveRange(varLocation, varScope->vsdVarNum);
    }
}

//------------------------------------------------------------------------
// psiEndProlog: Close all the open "psiScope" or "VariableLiveRanges"
//  after prolog has been generated.
//
// Notes:
//  This function is expected to be called after prolog code has been generated.
//
void CodeGen::psiEndProlog()
{
    assert(compiler->compGeneratingProlog);
    varLiveKeeper->psiClosePrologVariableRanges();
}

/*****************************************************************************
 *                          genSetScopeInfo
 *
 * This function should be called only after the sizes of the emitter blocks
 * have been finalized.
 */

void CodeGen::genSetScopeInfo()
{
    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genSetScopeInfo()\n");
    }
#endif

    unsigned varsLocationsCount = 0;

    varsLocationsCount = (unsigned int)varLiveKeeper->getLiveRangesCount();

    if (varsLocationsCount == 0)
    {
        // No variable home to report
        compiler->eeSetLVcount(0);
        compiler->eeSetLVdone();
        return;
    }

    noway_assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));

    // Initialize the table where the reported variables' home will be placed.
    compiler->eeSetLVcount(varsLocationsCount);

#ifdef DEBUG
    genTrnslLocalVarCount = varsLocationsCount;
    if (varsLocationsCount)
    {
        genTrnslLocalVarInfo = new (compiler, CMK_DebugOnly) TrnslLocalVarInfo[varsLocationsCount];
    }
#endif

    // We can have one of both flags defined, both, or none. Specially if we need to compare both
    // both results. But we cannot report both to the debugger, since there would be overlapping
    // intervals, and may not indicate the same variable location.

    genSetScopeInfoUsingVariableRanges();

    compiler->eeSetLVdone();
}

//------------------------------------------------------------------------
// genSetScopeInfoUsingVariableRanges: Call "genSetScopeInfo" with the
//  "VariableLiveRanges" created for the arguments, special arguments and
//  IL local variables.
//
// Notes:
//  This function is called from "genSetScopeInfo" once the code is generated
//  and we want to send debug info to the debugger.
//
void CodeGen::genSetScopeInfoUsingVariableRanges()
{
    unsigned int liveRangeIndex = 0;

    for (unsigned int varNum = 0; varNum < compiler->info.compLocalsCount; varNum++)
    {
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

        if (compiler->compMap2ILvarNum(varNum) == (unsigned int)ICorDebugInfo::UNKNOWN_ILNUM)
        {
            continue;
        }

        auto reportRange = [this, varDsc, varNum, &liveRangeIndex](siVarLoc* loc, UNATIVE_OFFSET start,
                                                                   UNATIVE_OFFSET end) {
            if (varDsc->lvIsParam && (start == end))
            {
                // If the length is zero, it means that the prolog is empty. In that case,
                // CodeGen::genSetScopeInfo will report the liveness of all arguments
                // as spanning the first instruction in the method, so that they can
                // at least be inspected on entry to the method.
                end++;
            }

            if (start < end)
            {
                genSetScopeInfo(liveRangeIndex, start, end - start, varNum, varNum, true, loc);
                liveRangeIndex++;
            }
        };

        siVarLoc*      curLoc   = nullptr;
        UNATIVE_OFFSET curStart = 0;
        UNATIVE_OFFSET curEnd   = 0;

        for (int rangeIndex = 0; rangeIndex < 2; rangeIndex++)
        {
            VariableLiveKeeper::LiveRangeList* liveRanges;
            if (rangeIndex == 0)
            {
                liveRanges = varLiveKeeper->getLiveRangesForVarForProlog(varNum);
            }
            else
            {
                liveRanges = varLiveKeeper->getLiveRangesForVarForBody(varNum);
            }

            for (VariableLiveKeeper::VariableLiveRange& liveRange : *liveRanges)
            {
                UNATIVE_OFFSET startOffs = liveRange.m_StartEmitLocation.CodeOffset(GetEmitter());
                UNATIVE_OFFSET endOffs   = liveRange.m_EndEmitLocation.CodeOffset(GetEmitter());

                assert(startOffs <= endOffs);
                assert(startOffs >= curEnd);
                if ((curLoc != nullptr) && (startOffs == curEnd) && siVarLoc::Equals(curLoc, &liveRange.m_VarLocation))
                {
                    // Extend current range.
                    curEnd = endOffs;
                    continue;
                }

                // Report old range if any.
                if (curLoc != nullptr)
                {
                    reportRange(curLoc, curStart, curEnd);
                }

                // Start a new range.
                curLoc   = &liveRange.m_VarLocation;
                curStart = startOffs;
                curEnd   = endOffs;
            }
        }

        // Report last range
        if (curLoc != nullptr)
        {
            reportRange(curLoc, curStart, curEnd);
        }
    }

    compiler->eeVarsCount = liveRangeIndex;
}

//------------------------------------------------------------------------
// genSetScopeInfo: Record scope information for debug info
//
// Arguments:
//    which
//    startOffs - the starting offset for this scope
//    length    - the length of this scope
//    varNum    - the lclVar for this scope info
//    LVnum
//    avail     - a bool indicating if it has a home
//    varLoc    - the position (reg or stack) of the variable
//
// Notes:
//    Called for every scope info piece to record by the main genSetScopeInfo()

void CodeGen::genSetScopeInfo(unsigned       which,
                              UNATIVE_OFFSET startOffs,
                              UNATIVE_OFFSET length,
                              unsigned       varNum,
                              unsigned       LVnum,
                              bool           avail,
                              siVarLoc*      varLoc)
{
    // We need to do some mapping while reporting back these variables.

    unsigned ilVarNum = compiler->compMap2ILvarNum(varNum);
    noway_assert((int)ilVarNum != ICorDebugInfo::UNKNOWN_ILNUM);

#ifdef TARGET_X86
    // Non-x86 platforms are allowed to access all arguments directly
    // so we don't need this code.

    // Is this a varargs function?
    if (compiler->info.compIsVarArgs && varNum != compiler->lvaVarargsHandleArg &&
        varNum < compiler->info.compArgsCount && !compiler->lvaGetDesc(varNum)->lvIsRegArg)
    {
        noway_assert(varLoc->vlType == VLT_STK || varLoc->vlType == VLT_STK2);

        // All stack arguments (except the varargs handle) have to be
        // accessed via the varargs cookie. Discard generated info,
        // and just find its position relative to the varargs handle

        PREFIX_ASSUME(compiler->lvaVarargsHandleArg < compiler->info.compArgsCount);
        if (!compiler->lvaGetDesc(compiler->lvaVarargsHandleArg)->lvOnFrame)
        {
            noway_assert(!compiler->opts.compDbgCode);
            return;
        }

        // Can't check compiler->lvaTable[varNum].lvOnFrame as we don't set it for
        // arguments of vararg functions to avoid reporting them to GC.
        noway_assert(!compiler->lvaGetDesc(varNum)->lvRegister);
        unsigned cookieOffset = compiler->lvaGetDesc(compiler->lvaVarargsHandleArg)->GetStackOffset();
        unsigned varOffset    = compiler->lvaGetDesc(varNum)->GetStackOffset();

        noway_assert(cookieOffset < varOffset);
        unsigned offset     = varOffset - cookieOffset;
        unsigned stkArgSize = compiler->compArgSize - intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;
        noway_assert(offset < stkArgSize);
        offset = stkArgSize - offset;

        varLoc->vlType                   = VLT_FIXED_VA;
        varLoc->vlFixedVarArg.vlfvOffset = offset;
    }

#endif // TARGET_X86

    VarName name = nullptr;

#ifdef DEBUG

    for (unsigned scopeNum = 0; scopeNum < compiler->info.compVarScopesCount; scopeNum++)
    {
        if (LVnum == compiler->info.compVarScopes[scopeNum].vsdLVnum)
        {
            name = compiler->info.compVarScopes[scopeNum].vsdName;
        }
    }

    // Hang on to this compiler->info.

    TrnslLocalVarInfo& tlvi = genTrnslLocalVarInfo[which];

    tlvi.tlviVarNum    = ilVarNum;
    tlvi.tlviLVnum     = LVnum;
    tlvi.tlviName      = name;
    tlvi.tlviStartPC   = startOffs;
    tlvi.tlviLength    = length;
    tlvi.tlviAvailable = avail;
    tlvi.tlviVarLoc    = *varLoc;

#endif // DEBUG

    compiler->eeSetLVinfo(which, startOffs, length, ilVarNum, *varLoc);
}

/*****************************************************************************/
#ifdef LATE_DISASM
#if defined(DEBUG)
/*****************************************************************************
 *                          CompilerRegName
 *
 * Can be called only after lviSetLocalVarInfo() has been called
 */

/* virtual */
const char* CodeGen::siRegVarName(size_t offs, size_t size, unsigned reg)
{
    if (!compiler->opts.compScopeInfo)
        return nullptr;

    if (compiler->info.compVarScopesCount == 0)
        return nullptr;

    noway_assert(genTrnslLocalVarCount == 0 || genTrnslLocalVarInfo);

    for (unsigned i = 0; i < genTrnslLocalVarCount; i++)
    {
        if ((genTrnslLocalVarInfo[i].tlviVarLoc.vlIsInReg((regNumber)reg)) &&
            (genTrnslLocalVarInfo[i].tlviAvailable == true) && (genTrnslLocalVarInfo[i].tlviStartPC <= offs + size) &&
            (genTrnslLocalVarInfo[i].tlviStartPC + genTrnslLocalVarInfo[i].tlviLength > offs))
        {
            return genTrnslLocalVarInfo[i].tlviName ? compiler->VarNameToStr(genTrnslLocalVarInfo[i].tlviName) : NULL;
        }
    }

    return NULL;
}

/*****************************************************************************
 *                          CompilerStkName
 *
 * Can be called only after lviSetLocalVarInfo() has been called
 */

/* virtual */
const char* CodeGen::siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs)
{
    if (!compiler->opts.compScopeInfo)
        return nullptr;

    if (compiler->info.compVarScopesCount == 0)
        return nullptr;

    noway_assert(genTrnslLocalVarCount == 0 || genTrnslLocalVarInfo);

    for (unsigned i = 0; i < genTrnslLocalVarCount; i++)
    {
        if ((genTrnslLocalVarInfo[i].tlviVarLoc.vlIsOnStack((regNumber)reg, stkOffs)) &&
            (genTrnslLocalVarInfo[i].tlviAvailable == true) && (genTrnslLocalVarInfo[i].tlviStartPC <= offs + size) &&
            (genTrnslLocalVarInfo[i].tlviStartPC + genTrnslLocalVarInfo[i].tlviLength > offs))
        {
            return genTrnslLocalVarInfo[i].tlviName ? compiler->VarNameToStr(genTrnslLocalVarInfo[i].tlviName) : NULL;
        }
    }

    return NULL;
}

/*****************************************************************************/
#endif // defined(DEBUG)
#endif // LATE_DISASM
