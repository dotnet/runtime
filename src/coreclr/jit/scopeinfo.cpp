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

#ifdef TARGET_64BIT
        case TYP_FLOAT:
        case TYP_DOUBLE:
            // TODO-AMD64-Bug: ndp\clr\src\inc\corinfo.h has a definition of RegNum that only goes up to R15,
            // so no XMM registers can get debug information.
            this->vlType       = VLT_REG_FP;
            this->vlReg.vlrReg = varDsc->GetRegNum();
            break;

#else // !TARGET_64BIT

        case TYP_FLOAT:
        case TYP_DOUBLE:
            if (isFloatRegType(type))
            {
                this->vlType         = VLT_FPSTK;
                this->vlFPstk.vlfReg = varDsc->GetRegNum();
            }
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

            // TODO-AMD64-Bug: ndp\clr\src\inc\corinfo.h has a definition of RegNum that only goes up to R15,
            // so no XMM registers can get debug information.
            //
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
#endif

/*============================================================================
 *           INTERFACE (public) Functions for ScopeInfo
 *============================================================================
 */

// Check every CodeGenInterface::siVarLocType and CodeGenInterface::siVarLoc
// are what ICodeDebugInfo is expetecting.
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

    if (block->bbFlags & BBF_FUNCLET_BEG)
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
