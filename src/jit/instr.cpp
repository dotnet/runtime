// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Instruction                                     XX
XX                                                                           XX
XX          The interface to generate a machine-instruction.                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"
#include "instr.h"
#include "emit.h"

/*****************************************************************************/
#ifdef DEBUG

/*****************************************************************************
 *
 *  Returns the string representation of the given CPU instruction.
 */

const char* CodeGen::genInsName(instruction ins)
{
    // clang-format off
    static
    const char * const insNames[] =
    {
#if defined(_TARGET_XARCH_)
        #define INST0(id, nm, fp, um, rf, wf, mr                 ) nm,
        #define INST1(id, nm, fp, um, rf, wf, mr                 ) nm,
        #define INST2(id, nm, fp, um, rf, wf, mr, mi             ) nm,
        #define INST3(id, nm, fp, um, rf, wf, mr, mi, rm         ) nm,
        #define INST4(id, nm, fp, um, rf, wf, mr, mi, rm, a4     ) nm,
        #define INST5(id, nm, fp, um, rf, wf, mr, mi, rm, a4, rr ) nm,
        #include "instrs.h"

#elif defined(_TARGET_ARM_)
        #define INST1(id, nm, fp, ldst, fmt, e1                                 ) nm,
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                             ) nm,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                         ) nm,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                     ) nm,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                 ) nm,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6             ) nm,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8     ) nm,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9 ) nm,
        #include "instrs.h"

#elif defined(_TARGET_ARM64_)
        #define INST1(id, nm, fp, ldst, fmt, e1                                 ) nm,
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                             ) nm,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                         ) nm,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                     ) nm,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                 ) nm,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6             ) nm,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9 ) nm,
        #include "instrs.h"

#else
#error "Unknown _TARGET_"
#endif
    };
    // clang-format on

    assert((unsigned)ins < sizeof(insNames) / sizeof(insNames[0]));
    assert(insNames[ins] != nullptr);

    return insNames[ins];
}

void __cdecl CodeGen::instDisp(instruction ins, bool noNL, const char* fmt, ...)
{
    if (compiler->opts.dspCode)
    {
        /* Display the instruction offset within the emit block */

        //      printf("[%08X:%04X]", getEmitter().emitCodeCurBlock(), getEmitter().emitCodeOffsInBlock());

        /* Display the FP stack depth (before the instruction is executed) */

        //      printf("[FP=%02u] ", genGetFPstkLevel());

        /* Display the instruction mnemonic */
        printf("        ");

        printf("            %-8s", genInsName(ins));

        if (fmt)
        {
            va_list args;
            va_start(args, fmt);
            vprintf(fmt, args);
            va_end(args);
        }

        if (!noNL)
        {
            printf("\n");
        }
    }
}

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/

void CodeGen::instInit()
{
}

/*****************************************************************************
 *
 *  Return the size string (e.g. "word ptr") appropriate for the given size.
 */

#ifdef DEBUG

const char* CodeGen::genSizeStr(emitAttr attr)
{
    // clang-format off
    static
    const char * const sizes[] =
    {
        "",
        "byte  ptr ",
        "word  ptr ",
        nullptr,
        "dword ptr ",
        nullptr,
        nullptr,
        nullptr,
        "qword ptr ",
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        "xmmword ptr ",
        nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr,
        nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr,
        nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr,
        nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr,
        "ymmword ptr"
    };
    // clang-format on

    unsigned size = EA_SIZE(attr);

    assert(size == 0 || size == 1 || size == 2 || size == 4 || size == 8 || size == 16 || size == 32);

    if (EA_ATTR(size) == attr)
    {
        return sizes[size];
    }
    else if (attr == EA_GCREF)
    {
        return "gword ptr ";
    }
    else if (attr == EA_BYREF)
    {
        return "bword ptr ";
    }
    else if (EA_IS_DSP_RELOC(attr))
    {
        return "rword ptr ";
    }
    else
    {
        assert(!"Unexpected");
        return "unknw ptr ";
    }
}

#endif

/*****************************************************************************
 *
 *  Generate an instruction.
 */

void CodeGen::instGen(instruction ins)
{

    getEmitter()->emitIns(ins);

#ifdef _TARGET_XARCH_
    // A workaround necessitated by limitations of emitter
    // if we are scheduled to insert a nop here, we have to delay it
    // hopefully we have not missed any other prefix instructions or places
    // they could be inserted
    if (ins == INS_lock && getEmitter()->emitNextNop == 0)
    {
        getEmitter()->emitNextNop = 1;
    }
#endif
}

/*****************************************************************************
 *
 *  Returns non-zero if the given CPU instruction is a floating-point ins.
 */

// static inline
bool CodeGenInterface::instIsFP(instruction ins)
{
    assert((unsigned)ins < sizeof(instInfo) / sizeof(instInfo[0]));

    return (instInfo[ins] & INST_FP) != 0;
}

#ifdef _TARGET_XARCH_
/*****************************************************************************
 *
 *  Generate a multi-byte NOP instruction.
 */

void CodeGen::instNop(unsigned size)
{
    assert(size <= 15);
    getEmitter()->emitIns_Nop(size);
}
#endif

/*****************************************************************************
 *
 *  Generate a jump instruction.
 */

void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
#if !FEATURE_FIXED_OUT_ARGS
    // On the x86 we are pushing (and changing the stack level), but on x64 and other archs we have
    // a fixed outgoing args area that we store into and we never change the stack level when calling methods.
    //
    // Thus only on x86 do we need to assert that the stack level at the target block matches the current stack level.
    //
    assert(tgtBlock->bbTgtStkDepth * sizeof(int) == genStackLevel || compiler->rpFrameType != FT_ESP_FRAME);
#endif

    getEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmp), tgtBlock);
}

/*****************************************************************************
 *
 *  Generate a set instruction.
 */

void CodeGen::inst_SET(emitJumpKind condition, regNumber reg)
{
#ifdef _TARGET_XARCH_
    instruction ins;

    /* Convert the condition to an instruction opcode */

    switch (condition)
    {
        case EJ_js:
            ins = INS_sets;
            break;
        case EJ_jns:
            ins = INS_setns;
            break;
        case EJ_je:
            ins = INS_sete;
            break;
        case EJ_jne:
            ins = INS_setne;
            break;

        case EJ_jl:
            ins = INS_setl;
            break;
        case EJ_jle:
            ins = INS_setle;
            break;
        case EJ_jge:
            ins = INS_setge;
            break;
        case EJ_jg:
            ins = INS_setg;
            break;

        case EJ_jb:
            ins = INS_setb;
            break;
        case EJ_jbe:
            ins = INS_setbe;
            break;
        case EJ_jae:
            ins = INS_setae;
            break;
        case EJ_ja:
            ins = INS_seta;
            break;

        case EJ_jpe:
            ins = INS_setpe;
            break;
        case EJ_jpo:
            ins = INS_setpo;
            break;

        default:
            NO_WAY("unexpected condition type");
            return;
    }

    assert(genRegMask(reg) & RBM_BYTE_REGS);

    // These instructions only write the low byte of 'reg'
    getEmitter()->emitIns_R(ins, EA_1BYTE, reg);
#elif defined(_TARGET_ARM64_)
    insCond cond;
    /* Convert the condition to an insCond value */
    switch (condition)
    {
        case EJ_eq:
            cond = INS_COND_EQ;
            break;
        case EJ_ne:
            cond = INS_COND_NE;
            break;
        case EJ_hs:
            cond = INS_COND_HS;
            break;
        case EJ_lo:
            cond = INS_COND_LO;
            break;

        case EJ_mi:
            cond = INS_COND_MI;
            break;
        case EJ_pl:
            cond = INS_COND_PL;
            break;
        case EJ_vs:
            cond = INS_COND_VS;
            break;
        case EJ_vc:
            cond = INS_COND_VC;
            break;

        case EJ_hi:
            cond = INS_COND_HI;
            break;
        case EJ_ls:
            cond = INS_COND_LS;
            break;
        case EJ_ge:
            cond = INS_COND_GE;
            break;
        case EJ_lt:
            cond = INS_COND_LT;
            break;

        case EJ_gt:
            cond = INS_COND_GT;
            break;
        case EJ_le:
            cond = INS_COND_LE;
            break;

        default:
            NO_WAY("unexpected condition type");
            return;
    }
    getEmitter()->emitIns_R_COND(INS_cset, EA_8BYTE, reg, cond);
#else
    NYI("inst_SET");
#endif
}

/*****************************************************************************
 *
 *  Generate a "op reg" instruction.
 */

void CodeGen::inst_RV(instruction ins, regNumber reg, var_types type, emitAttr size)
{
    if (size == EA_UNKNOWN)
    {
        size = emitActualTypeSize(type);
    }

    getEmitter()->emitIns_R(ins, size, reg);
}

/*****************************************************************************
 *
 *  Generate a "op reg1, reg2" instruction.
 */

void CodeGen::inst_RV_RV(instruction ins,
                         regNumber   reg1,
                         regNumber   reg2,
                         var_types   type,
                         emitAttr    size,
                         insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
    if (size == EA_UNKNOWN)
    {
        size = emitActualTypeSize(type);
    }

#ifdef _TARGET_ARM_
    getEmitter()->emitIns_R_R(ins, size, reg1, reg2, flags);
#else
    getEmitter()->emitIns_R_R(ins, size, reg1, reg2);
#endif
}

/*****************************************************************************
 *
 *  Generate a "op reg1, reg2, reg3" instruction.
 */

void CodeGen::inst_RV_RV_RV(instruction ins,
                            regNumber   reg1,
                            regNumber   reg2,
                            regNumber   reg3,
                            emitAttr    size,
                            insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
#ifdef _TARGET_ARM_
    getEmitter()->emitIns_R_R_R(ins, size, reg1, reg2, reg3, flags);
#elif defined(_TARGET_XARCH_) && defined(FEATURE_AVX_SUPPORT)
    getEmitter()->emitIns_R_R_R(ins, size, reg1, reg2, reg3);
#else
    NYI("inst_RV_RV_RV");
#endif
}
/*****************************************************************************
 *
 *  Generate a "op icon" instruction.
 */

void CodeGen::inst_IV(instruction ins, int val)
{
    getEmitter()->emitIns_I(ins, EA_PTRSIZE, val);
}

/*****************************************************************************
 *
 *  Generate a "op icon" instruction where icon is a handle of type specified
 *  by 'flags'
 */

void CodeGen::inst_IV_handle(instruction ins, int val)
{
    getEmitter()->emitIns_I(ins, EA_HANDLE_CNS_RELOC, val);
}

#if FEATURE_STACK_FP_X87
/*****************************************************************************
 *
 *  Generate a "op ST(n), ST(0)" instruction.
 */

void CodeGen::inst_FS(instruction ins, unsigned stk)
{
    assert(stk < 8);

#ifdef DEBUG

    switch (ins)
    {
        case INS_fcompp:
            assert(stk == 1);
            break; // Implicit operand of compp is ST(1)
        case INS_fld:
        case INS_fxch:
            assert(!"don't do this. Do you want to use inst_FN() instead?");
            break;
        default:
            break;
    }

#endif

    getEmitter()->emitIns_F_F0(ins, stk);
}

/*****************************************************************************
 *
 *  Generate a "op ST(0), ST(n)" instruction
 */

void CodeGenInterface::inst_FN(instruction ins, unsigned stk)
{
    assert(stk < 8);

#ifdef DEBUG

    switch (ins)
    {
        case INS_fst:
        case INS_fstp:
        case INS_faddp:
        case INS_fsubp:
        case INS_fsubrp:
        case INS_fmulp:
        case INS_fdivp:
        case INS_fdivrp:
        case INS_fcompp:
            assert(!"don't do this. Do you want to use inst_FS() instead?");
            break;
        default:
            break;
    }

#endif // DEBUG

    getEmitter()->emitIns_F0_F(ins, stk);
}
#endif // FEATURE_STACK_FP_X87

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void CodeGen::inst_set_SV_var(GenTreePtr tree)
{
#ifdef DEBUG
    assert(tree && (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_VAR_ADDR || tree->gtOper == GT_STORE_LCL_VAR));
    assert(tree->gtLclVarCommon.gtLclNum < compiler->lvaCount);

    getEmitter()->emitVarRefOffs = tree->gtLclVar.gtLclILoffs;

#endif // DEBUG
}

/*****************************************************************************
 *
 *  Generate a "op reg, icon" instruction.
 */

void CodeGen::inst_RV_IV(
    instruction ins, regNumber reg, ssize_t val, emitAttr size, insFlags flags /* = INS_FLAGS_DONT_CARE */)
{
#if !defined(_TARGET_64BIT_)
    assert(size != EA_8BYTE);
#endif

#ifdef _TARGET_ARM_
    if (arm_Valid_Imm_For_Instr(ins, val, flags))
    {
        getEmitter()->emitIns_R_I(ins, size, reg, val, flags);
    }
    else if (ins == INS_mov)
    {
        instGen_Set_Reg_To_Imm(size, reg, val);
    }
    else
    {
#ifndef LEGACY_BACKEND
        // TODO-Cleanup: Add a comment about why this is unreached() for RyuJIT backend.
        unreached();
#else  // LEGACY_BACKEND
        regNumber tmpReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
        instGen_Set_Reg_To_Imm(size, tmpReg, val);
        getEmitter()->emitIns_R_R(ins, size, reg, tmpReg, flags);
#endif // LEGACY_BACKEND
    }
#elif defined(_TARGET_ARM64_)
    // TODO-Arm64-Bug: handle large constants!
    // Probably need something like the ARM case above: if (arm_Valid_Imm_For_Instr(ins, val)) ...
    assert(ins != INS_cmp);
    assert(ins != INS_tst);
    assert(ins != INS_mov);
    getEmitter()->emitIns_R_R_I(ins, size, reg, reg, val);
#else // !_TARGET_ARM_
#ifdef _TARGET_AMD64_
    // Instead of an 8-byte immediate load, a 4-byte immediate will do fine
    // as the high 4 bytes will be zero anyway.
    if (size == EA_8BYTE && ins == INS_mov && ((val & 0xFFFFFFFF00000000LL) == 0))
    {
        size = EA_4BYTE;
        getEmitter()->emitIns_R_I(ins, size, reg, val);
    }
    else if (EA_SIZE(size) == EA_8BYTE && ins != INS_mov && (((int)val != val) || EA_IS_CNS_RELOC(size)))
    {
#ifndef LEGACY_BACKEND
        assert(!"Invalid immediate for inst_RV_IV");
#else  // LEGACY_BACKEND
        // We can't fit the immediate into this instruction, so move it into
        // a register first
        regNumber tmpReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
        instGen_Set_Reg_To_Imm(size, tmpReg, val);

        // We might have to switch back from 3-operand imul to two operand form
        if (instrIs3opImul(ins))
        {
            assert(getEmitter()->inst3opImulReg(ins) == reg);
            ins = INS_imul;
        }
        getEmitter()->emitIns_R_R(ins, EA_TYPE(size), reg, tmpReg);
#endif // LEGACY_BACKEND
    }
    else
#endif // _TARGET_AMD64_
    {
        getEmitter()->emitIns_R_I(ins, size, reg, val);
    }
#endif // !_TARGET_ARM_
}

#if defined(LEGACY_BACKEND)
/*****************************************************************************
 *  Figure out the operands to address the tree.
 *  'addr' can be one of (1) a pointer to be indirected
 *                       (2) a calculation to be done with LEA_AVAILABLE
 *                       (3) GT_ARR_ELEM
 *
 *  On return, *baseReg, *indScale, *indReg, and *cns are set.
 */

void CodeGen::instGetAddrMode(GenTreePtr addr, regNumber* baseReg, unsigned* indScale, regNumber* indReg, unsigned* cns)
{
    if (addr->gtOper == GT_ARR_ELEM)
    {
        /* For GT_ARR_ELEM, the addressibility registers are marked on
           gtArrObj and gtArrInds[0] */

        assert(addr->gtArrElem.gtArrObj->gtFlags & GTF_REG_VAL);
        *baseReg = addr->gtArrElem.gtArrObj->gtRegNum;

        assert(addr->gtArrElem.gtArrInds[0]->gtFlags & GTF_REG_VAL);
        *indReg = addr->gtArrElem.gtArrInds[0]->gtRegNum;

        if (jitIsScaleIndexMul(addr->gtArrElem.gtArrElemSize))
            *indScale = addr->gtArrElem.gtArrElemSize;
        else
            *indScale = 0;

        *cns = compiler->eeGetMDArrayDataOffset(addr->gtArrElem.gtArrElemType, addr->gtArrElem.gtArrRank);
    }
    else if (addr->gtOper == GT_LEA)
    {
        GenTreeAddrMode* lea  = addr->AsAddrMode();
        GenTreePtr       base = lea->Base();
        assert(!base || (base->gtFlags & GTF_REG_VAL));
        GenTreePtr index = lea->Index();
        assert(!index || (index->gtFlags & GTF_REG_VAL));

        *baseReg  = base ? base->gtRegNum : REG_NA;
        *indReg   = index ? index->gtRegNum : REG_NA;
        *indScale = lea->gtScale;
        *cns      = lea->gtOffset;
        return;
    }
    else
    {
        /* Figure out what complex address mode to use */

        GenTreePtr rv1 = NULL;
        GenTreePtr rv2 = NULL;
        bool       rev = false;

        INDEBUG(bool yes =)
        genCreateAddrMode(addr, -1, true, RBM_NONE, &rev, &rv1, &rv2,
#if SCALED_ADDR_MODES
                          indScale,
#endif
                          cns);

        assert(yes); // // since we have called genMakeAddressable() on addr
        // Ensure that the base and index, if used, are in registers.
        if (rv1 && ((rv1->gtFlags & GTF_REG_VAL) == 0))
        {
            if (rv1->gtFlags & GTF_SPILLED)
            {
                genRecoverReg(rv1, RBM_ALLINT, RegSet::KEEP_REG);
            }
            else
            {
                genCodeForTree(rv1, RBM_NONE);
                regSet.rsMarkRegUsed(rv1, addr);
            }
            assert(rv1->gtFlags & GTF_REG_VAL);
        }
        if (rv2 && ((rv2->gtFlags & GTF_REG_VAL) == 0))
        {
            if (rv2->gtFlags & GTF_SPILLED)
            {
                genRecoverReg(rv2, ~genRegMask(rv1->gtRegNum), RegSet::KEEP_REG);
            }
            else
            {
                genCodeForTree(rv2, RBM_NONE);
                regSet.rsMarkRegUsed(rv2, addr);
            }
            assert(rv2->gtFlags & GTF_REG_VAL);
        }
        // If we did both, we might have spilled rv1.
        if (rv1 && ((rv1->gtFlags & GTF_SPILLED) != 0))
        {
            regSet.rsLockUsedReg(genRegMask(rv2->gtRegNum));
            genRecoverReg(rv1, ~genRegMask(rv2->gtRegNum), RegSet::KEEP_REG);
            regSet.rsUnlockReg(genRegMask(rv2->gtRegNum));
        }

        *baseReg = rv1 ? rv1->gtRegNum : REG_NA;
        *indReg  = rv2 ? rv2->gtRegNum : REG_NA;
    }
}

#if CPU_LOAD_STORE_ARCH
/*****************************************************************************
 *
 *  Originally this was somewhat specific to the x86 instrution format.
 *  For a Load/Store arch we generate the 1-8 instructions necessary to
 *  implement the single addressing mode instruction used on x86.
 *  We currently don't have an instruction scheduler enabled on any target.
 *
 *  [Schedule] an "ins reg, [r/m]" (rdst=true), or "ins [r/m], reg" (rdst=false)
 *  instruction (the r/m operand given by a tree). We also allow instructions
 *  of the form "ins [r/m], icon", these are signaled by setting 'cons' to
 *  true.
 *
 *   The longest instruction sequence emitted on the ARM is as follows:
 *
 *       - the "addr" represents an array addressing mode,
 *          with a baseReg, indReg with a shift and a large offset
 *          (Note that typically array addressing modes do NOT have a large offset)
 *       - "ins" is an ALU instruction,
 *       - cons=true, and imm is a large constant that can not be directly encoded with "ins"
 *       - We may need to grab upto four additional registers: regT, rtegVal, regOffs and regImm
 *
 *       add    regT, baseReg, indReg<<shift
 *       movw   regOffs, offsLo
 *       movt   regOffs, offsHi
 *       ldr    regVal, [regT + regOffs]
 *       movw   regImm, consLo
 *       movt   regImm, consHi
 *       "ins"  regVal, regImm
 *       str    regVal, [regT + regOffs]
 *
 */

void CodeGen::sched_AM(instruction ins,
                       emitAttr    size,
                       regNumber   ireg,
                       bool        rdst,
                       GenTreePtr  addr,
                       unsigned    offs,
                       bool        cons,
                       int         imm,
                       insFlags    flags)
{
    assert(addr);
    assert(size != EA_UNKNOWN);

    enum INS_TYPE
    {
        eIT_Lea,
        eIT_Load,
        eIT_Store,
        eIT_Other
    };
    INS_TYPE insType = eIT_Other;

    if (ins == INS_lea)
    {
        insType = eIT_Lea;
        ins     = INS_add;
    }
    else if (getEmitter()->emitInsIsLoad(ins))
    {
        insType = eIT_Load;
    }
    else if (getEmitter()->emitInsIsStore(ins))
    {
        insType = eIT_Store;
    }

    regNumber baseReg  = REG_NA;
    regNumber indReg   = REG_NA;
    unsigned  indScale = 0;

    regMaskTP avoidMask = RBM_NONE;

    if (addr->gtFlags & GTF_REG_VAL)
    {
        /* The address is "[reg+offs]" */
        baseReg = addr->gtRegNum;
    }
    else if (addr->IsCnsIntOrI())
    {
#ifdef RELOC_SUPPORT
        // Do we need relocations?
        if (compiler->opts.compReloc && addr->IsIconHandle())
        {
            size = EA_SET_FLG(size, EA_DSP_RELOC_FLG);
            // offs should be smaller than ZapperModule::FixupPlaceHolder
            // so that we can uniquely identify the handle
            assert(offs <= 4);
        }
#endif
        ssize_t disp = addr->gtIntCon.gtIconVal + offs;
        if ((insType == eIT_Store) && (ireg != REG_NA))
        {
            // Can't use the ireg as the baseReg when we have a store instruction
            avoidMask |= genRegMask(ireg);
        }
        baseReg = regSet.rsPickFreeReg(RBM_ALLINT & ~avoidMask);

        avoidMask |= genRegMask(baseReg);
        instGen_Set_Reg_To_Imm(size, baseReg, disp);
        offs = 0;
    }
    else
    {
        unsigned cns = 0;

        instGetAddrMode(addr, &baseReg, &indScale, &indReg, &cns);

        /* Add the constant offset value, if present */

        offs += cns;

#if SCALED_ADDR_MODES
        noway_assert((baseReg != REG_NA) || (indReg != REG_NA));
        if (baseReg != REG_NA)
#endif
        {
            avoidMask |= genRegMask(baseReg);
        }

        // I don't think this is necessary even in the non-proto-jit case, but better to be
        // conservative here.  It is only necessary to avoid using ireg if it is used as regT,
        // in which case it will be added to avoidMask below.

        if (ireg != REG_NA)
        {
            avoidMask |= genRegMask(ireg);
        }

        if (indReg != REG_NA)
        {
            avoidMask |= genRegMask(indReg);
        }
    }

    unsigned shift = (indScale > 0) ? genLog2((unsigned)indScale) : 0;

    regNumber regT    = REG_NA; // the register where the address is computed into
    regNumber regOffs = REG_NA; // a temporary register to use for the offs when it can't be directly encoded
    regNumber regImm  = REG_NA; // a temporary register to use for the imm when it can't be directly encoded
    regNumber regVal  = REG_NA; // a temporary register to use when we have to do a load/modify/store operation

    // Setup regT
    if (indReg == REG_NA)
    {
        regT = baseReg; // We can use the baseReg, regT is read-only
    }
    else // We have an index register (indReg != REG_NA)
    {
        // Check for special case that we can encode using one instruction
        if ((offs == 0) && (insType != eIT_Other) && !instIsFP(ins) && baseReg != REG_NA)
        {
            //  ins    ireg, [baseReg + indReg << shift]
            getEmitter()->emitIns_R_R_R_I(ins, size, ireg, baseReg, indReg, shift, flags, INS_OPTS_LSL);
            return;
        }

        // Otherwise setup regT, regT is written once here
        //
        if (insType == eIT_Lea || (insType == eIT_Load && !instIsFP(ins)))
        {
            assert(ireg != REG_NA);
            // ireg will be written, so we can take it as our temporary register
            regT = ireg;
        }
        else
        {
            // need a new temporary reg
            regT = regSet.rsPickFreeReg(RBM_ALLINT & ~avoidMask);
            regTracker.rsTrackRegTrash(regT);
        }

#if SCALED_ADDR_MODES
        if (baseReg == REG_NA)
        {
            assert(shift > 0);
            //  LSL    regT, indReg, shift.
            getEmitter()->emitIns_R_R_I(INS_lsl, EA_PTRSIZE, regT, indReg, shift & ((TARGET_POINTER_SIZE * 8) - 1));
        }
        else
#endif // SCALED_ADDR_MODES
        {
            assert(baseReg != REG_NA);

            //  add    regT, baseReg, indReg<<shift.
            getEmitter()->emitIns_R_R_R_I(INS_add,
                                          // The "add" operation will yield either a pointer or byref, depending on the
                                          // type of "addr."
                                          varTypeIsGC(addr->TypeGet()) ? EA_BYREF : EA_PTRSIZE, regT, baseReg, indReg,
                                          shift, INS_FLAGS_NOT_SET, INS_OPTS_LSL);
        }
    }

    // regT is the base register for a load/store or an operand for add when insType is eIT_Lea
    //
    assert(regT != REG_NA);
    avoidMask |= genRegMask(regT);

    if (insType != eIT_Other)
    {
        assert((flags != INS_FLAGS_SET) || (insType == eIT_Lea));
        if ((insType == eIT_Lea) && (offs == 0))
        {
            // If we have the same register as src and dst and we do not need to set the flags
            //   then we can skip emitting the instruction
            if ((ireg != regT) || (flags == INS_FLAGS_SET))
            {
                //  mov    ireg, regT
                getEmitter()->emitIns_R_R(INS_mov, size, ireg, regT, flags);
            }
        }
        else if (arm_Valid_Imm_For_Instr(ins, offs, flags))
        {
            //  ins    ireg, [regT + offs]
            getEmitter()->emitIns_R_R_I(ins, size, ireg, regT, offs, flags);
        }
        else
        {
            regOffs = regSet.rsPickFreeReg(RBM_ALLINT & ~avoidMask);

            // We cannot use [regT + regOffs] to load/store a floating register
            if (emitter::isFloatReg(ireg))
            {
                if (arm_Valid_Imm_For_Instr(INS_add, offs, flags))
                {
                    //  add    regOffs, regT, offs
                    getEmitter()->emitIns_R_R_I(INS_add, EA_4BYTE, regOffs, regT, offs, flags);
                }
                else
                {
                    //  movw   regOffs, offs_lo16
                    //  movt   regOffs, offs_hi16
                    //  add    regOffs, regOffs, regT
                    instGen_Set_Reg_To_Imm(EA_4BYTE, regOffs, offs);
                    getEmitter()->emitIns_R_R_R(INS_add, EA_4BYTE, regOffs, regOffs, regT, flags);
                }
                //  ins    ireg, [regOffs]
                getEmitter()->emitIns_R_R_I(ins, size, ireg, regOffs, 0, flags);

                regTracker.rsTrackRegTrash(regOffs);
            }
            else
            {
                //  mov    regOffs, offs
                //  ins    ireg, [regT + regOffs]
                instGen_Set_Reg_To_Imm(EA_4BYTE, regOffs, offs);
                getEmitter()->emitIns_R_R_R(ins, size, ireg, regT, regOffs, flags);
            }
        }
    }
    else // (insType == eIT_Other);
    {
        // Setup regVal
        //

        regVal = regSet.rsPickReg(RBM_ALLINT & ~avoidMask);
        regTracker.rsTrackRegTrash(regVal);
        avoidMask |= genRegMask(regVal);
        var_types load_store_type;
        switch (size)
        {
            case EA_4BYTE:
                load_store_type = TYP_INT;
                break;

            case EA_2BYTE:
                load_store_type = TYP_SHORT;
                break;

            case EA_1BYTE:
                load_store_type = TYP_BYTE;
                break;

            default:
                assert(!"Unexpected size in sched_AM, eIT_Other");
                load_store_type = TYP_INT;
                break;
        }

        // Load the content at addr into regVal using regT + offs
        if (arm_Valid_Disp_For_LdSt(offs, load_store_type))
        {
            //  ldrX   regVal, [regT + offs]
            getEmitter()->emitIns_R_R_I(ins_Load(load_store_type), size, regVal, regT, offs);
        }
        else
        {
            //  mov    regOffs, offs
            //  ldrX   regVal, [regT + regOffs]
            regOffs = regSet.rsPickFreeReg(RBM_ALLINT & ~avoidMask);
            avoidMask |= genRegMask(regOffs);
            instGen_Set_Reg_To_Imm(EA_4BYTE, regOffs, offs);
            getEmitter()->emitIns_R_R_R(ins_Load(load_store_type), size, regVal, regT, regOffs);
        }

        if (cons)
        {
            if (arm_Valid_Imm_For_Instr(ins, imm, flags))
            {
                getEmitter()->emitIns_R_I(ins, size, regVal, imm, flags);
            }
            else
            {
                assert(regOffs == REG_NA);
                regImm = regSet.rsPickFreeReg(RBM_ALLINT & ~avoidMask);
                avoidMask |= genRegMask(regImm);
                instGen_Set_Reg_To_Imm(size, regImm, imm);
                getEmitter()->emitIns_R_R(ins, size, regVal, regImm, flags);
            }
        }
        else if (rdst)
        {
            getEmitter()->emitIns_R_R(ins, size, ireg, regVal, flags);
        }
        else
        {
            getEmitter()->emitIns_R_R(ins, size, regVal, ireg, flags);
        }

        //  If we do not have a register destination we must perform the write-back store instruction
        //  (unless we have an instruction like INS_cmp that does not write a destination)
        //
        if (!rdst && ins_Writes_Dest(ins))
        {
            // Store regVal into [addr]
            if (regOffs == REG_NA)
            {
                //  strX   regVal, [regT + offs]
                getEmitter()->emitIns_R_R_I(ins_Store(load_store_type), size, regVal, regT, offs);
            }
            else
            {
                //  strX   regVal, [regT + regOffs]
                getEmitter()->emitIns_R_R_R(ins_Store(load_store_type), size, regVal, regT, regOffs);
            }
        }
    }
}

#else // !CPU_LOAD_STORE_ARCH

/*****************************************************************************
 *
 *  This is somewhat specific to the x86 instrution format.
 *  We currently don't have an instruction scheduler enabled on any target.
 *
 *  [Schedule] an "ins reg, [r/m]" (rdst=true), or "ins [r/m], reg" (rdst=false)
 *  instruction (the r/m operand given by a tree). We also allow instructions
 *  of the form "ins [r/m], icon", these are signalled by setting 'cons' to
 *  true.
 */

void CodeGen::sched_AM(instruction ins,
                       emitAttr    size,
                       regNumber   ireg,
                       bool        rdst,
                       GenTreePtr  addr,
                       unsigned    offs,
                       bool        cons,
                       int         imm,
                       insFlags    flags)
{
#ifdef _TARGET_XARCH_
    /* Don't use this method for issuing calls. Use instEmit_xxxCall() */
    assert(ins != INS_call);
#endif

    assert(addr);
    assert(size != EA_UNKNOWN);

    regNumber reg;

    /* Has the address been conveniently loaded into a register,
       or is it an absolute value ? */

    if ((addr->gtFlags & GTF_REG_VAL) || (addr->IsCnsIntOrI()))
    {
        if (addr->gtFlags & GTF_REG_VAL)
        {
            /* The address is "[reg+offs]" */

            reg = addr->gtRegNum;

            if (cons)
                getEmitter()->emitIns_I_AR(ins, size, imm, reg, offs);
            else if (rdst)
                getEmitter()->emitIns_R_AR(ins, size, ireg, reg, offs);
            else
                getEmitter()->emitIns_AR_R(ins, size, ireg, reg, offs);
        }
        else
        {
            /* The address is an absolute value */

            assert(addr->IsCnsIntOrI());

#ifdef RELOC_SUPPORT
            // Do we need relocations?
            if (compiler->opts.compReloc && addr->IsIconHandle())
            {
                size = EA_SET_FLG(size, EA_DSP_RELOC_FLG);
                // offs should be smaller than ZapperModule::FixupPlaceHolder
                // so that we can uniquely identify the handle
                assert(offs <= 4);
            }
#endif
            reg          = REG_NA;
            ssize_t disp = addr->gtIntCon.gtIconVal + offs;

            // Cross our fingers and hope the codegenerator did the right
            // thing and the constant address can be RIP-relative

            if (cons)
                getEmitter()->emitIns_I_AI(ins, size, imm, disp);
            else if (rdst)
                getEmitter()->emitIns_R_AI(ins, size, ireg, disp);
            else
                getEmitter()->emitIns_AI_R(ins, size, ireg, disp);
        }

        return;
    }

    /* Figure out what complex address mode to use */

    regNumber baseReg, indReg;
    unsigned  indScale = 0, cns = 0;

    instGetAddrMode(addr, &baseReg, &indScale, &indReg, &cns);

    /* Add the constant offset value, if present */

    offs += cns;

    /* Is there an index reg operand? */

    if (indReg != REG_NA)
    {
        /* Is the index reg operand scaled? */

        if (indScale)
        {
            /* Is there a base address operand? */

            if (baseReg != REG_NA)
            {
                reg = baseReg;

                /* The address is "[reg + {2/4/8} * indReg + offs]" */

                if (cons)
                    getEmitter()->emitIns_I_ARX(ins, size, imm, reg, indReg, indScale, offs);
                else if (rdst)
                    getEmitter()->emitIns_R_ARX(ins, size, ireg, reg, indReg, indScale, offs);
                else
                    getEmitter()->emitIns_ARX_R(ins, size, ireg, reg, indReg, indScale, offs);
            }
            else
            {
                /* The address is "[{2/4/8} * indReg + offs]" */

                if (cons)
                    getEmitter()->emitIns_I_AX(ins, size, imm, indReg, indScale, offs);
                else if (rdst)
                    getEmitter()->emitIns_R_AX(ins, size, ireg, indReg, indScale, offs);
                else
                    getEmitter()->emitIns_AX_R(ins, size, ireg, indReg, indScale, offs);
            }
        }
        else
        {
            assert(baseReg != REG_NA);
            reg = baseReg;

            /* The address is "[reg + indReg + offs]" */
            if (cons)
                getEmitter()->emitIns_I_ARR(ins, size, imm, reg, indReg, offs);
            else if (rdst)
                getEmitter()->emitIns_R_ARR(ins, size, ireg, reg, indReg, offs);
            else
                getEmitter()->emitIns_ARR_R(ins, size, ireg, reg, indReg, offs);
        }
    }
    else
    {
        unsigned             cpx = 0;
        CORINFO_CLASS_HANDLE cls = 0;

        /* No second operand: the address is "[reg  + icon]" */

        assert(baseReg != REG_NA);
        reg = baseReg;

#ifdef LATE_DISASM
        /*
            Keep in mind that non-static data members (GT_FIELD nodes) were
            transformed into GT_IND nodes - we keep the CLS/CPX information
            in the GT_CNS_INT node representing the field offset of the
            class member
         */

        if (addr->gtOper != GT_LEA && (addr->gtOp.gtOp2->gtOper == GT_CNS_INT) &&
            addr->gtOp.gtOp2->IsIconHandle(GTF_ICON_FIELD_HDL))
        {
            /* This is a field offset - set the CPX/CLS values to emit a fixup */

            cpx = addr->gtOp.gtOp2->gtIntCon.gtIconFld.gtIconCPX;
            cls = addr->gtOp.gtOp2->gtIntCon.gtIconFld.gtIconCls;
        }
#endif

        if (cons)
        {
            getEmitter()->emitIns_I_AR(ins, size, imm, reg, offs, cpx, cls);
        }
        else if (rdst)
        {
            getEmitter()->emitIns_R_AR(ins, size, ireg, reg, offs, cpx, cls);
        }
        else
        {
            getEmitter()->emitIns_AR_R(ins, size, ireg, reg, offs, cpx, cls);
        }
    }
}

#endif // !CPU_LOAD_STORE_ARCH
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Emit a "call [r/m]" instruction (the r/m operand given by a tree).
 */

void CodeGen::instEmit_indCall(GenTreePtr call,
                               size_t     argSize,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize))
{
    GenTreePtr addr;

    emitter::EmitCallType emitCallType;

    regNumber brg = REG_NA;
    regNumber xrg = REG_NA;
    unsigned  mul = 0;
    unsigned  cns = 0;

    CORINFO_SIG_INFO* sigInfo = nullptr;

    assert(call->gtOper == GT_CALL);

    /* Get hold of the function address */

    assert(call->gtCall.gtCallType == CT_INDIRECT);
    addr = call->gtCall.gtCallAddr;
    assert(addr);

#ifdef DEBUG
    // Pass the call signature information from the GenTree node so the emitter can associate
    // native call sites with the signatures they were generated from.
    sigInfo = call->gtCall.callSig;
#endif // DEBUG

#if CPU_LOAD_STORE_ARCH

    emitCallType = emitter::EC_INDIR_R;

    if (!addr->OperIsIndir())
    {
        if (!(addr->gtFlags & GTF_REG_VAL) && (addr->OperGet() == GT_CNS_INT))
        {
            ssize_t funcPtr = addr->gtIntCon.gtIconVal;

            getEmitter()->emitIns_Call(emitter::EC_FUNC_ADDR,
                                       NULL, // methHnd
                                       INDEBUG_LDISASM_COMMA(sigInfo)(void*) funcPtr, argSize,
                                       retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                       gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
            return;
        }
    }
    else
    {
        /* Get hold of the address of the function pointer */

        addr = addr->gtOp.gtOp1;
    }

    if (addr->gtFlags & GTF_REG_VAL)
    {
        /* The address is "reg" */

        brg = addr->gtRegNum;
    }
    else
    {
        // Force the address into a register
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef LEGACY_BACKEND
        genCodeForTree(addr, RBM_NONE);
#endif // LEGACY_BACKEND
        assert(addr->gtFlags & GTF_REG_VAL);
        brg = addr->gtRegNum;
    }

#else // CPU_LOAD_STORE_ARCH

    /* Is there an indirection? */

    if (!addr->OperIsIndir())
    {
        if (addr->gtFlags & GTF_REG_VAL)
        {
            emitCallType = emitter::EC_INDIR_R;
            brg          = addr->gtRegNum;
        }
        else
        {
            if (addr->OperGet() != GT_CNS_INT)
            {
                assert(addr->OperGet() == GT_LCL_VAR);

                emitCallType = emitter::EC_INDIR_SR;
                cns          = addr->gtLclVarCommon.gtLclNum;
            }
            else
            {
                ssize_t funcPtr = addr->gtIntCon.gtIconVal;

                getEmitter()->emitIns_Call(emitter::EC_FUNC_ADDR,
                                           nullptr, // methHnd
                                           INDEBUG_LDISASM_COMMA(sigInfo)(void*) funcPtr, argSize,
                                           retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                           gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
                return;
            }
        }
    }
    else
    {
        /* This is an indirect call */

        emitCallType = emitter::EC_INDIR_ARD;

        /* Get hold of the address of the function pointer */

        addr = addr->gtOp.gtOp1;

        /* Has the address been conveniently loaded into a register? */

        if (addr->gtFlags & GTF_REG_VAL)
        {
            /* The address is "reg" */

            brg = addr->gtRegNum;
        }
        else
        {
            bool rev = false;

            GenTreePtr rv1 = nullptr;
            GenTreePtr rv2 = nullptr;

            /* Figure out what complex address mode to use */

            INDEBUG(bool yes =)
            genCreateAddrMode(addr, -1, true, RBM_NONE, &rev, &rv1, &rv2, &mul, &cns);

            INDEBUG(PREFIX_ASSUME(yes)); // since we have called genMakeAddressable() on call->gtCall.gtCallAddr

            /* Get the additional operands if any */

            if (rv1)
            {
                assert(rv1->gtFlags & GTF_REG_VAL);
                brg = rv1->gtRegNum;
            }

            if (rv2)
            {
                assert(rv2->gtFlags & GTF_REG_VAL);
                xrg = rv2->gtRegNum;
            }
        }
    }

    assert(emitCallType == emitter::EC_INDIR_R || emitCallType == emitter::EC_INDIR_SR ||
           emitCallType == emitter::EC_INDIR_C || emitCallType == emitter::EC_INDIR_ARD);

#endif // CPU_LOAD_STORE_ARCH

    getEmitter()->emitIns_Call(emitCallType,
                               nullptr,                                // methHnd
                               INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                               argSize, retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                               gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                               BAD_IL_OFFSET, // ilOffset
                               brg, xrg, mul,
                               cns); // addressing mode values
}

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Emit an "op [r/m]" instruction (the r/m operand given by a tree).
 */

void CodeGen::instEmit_RM(instruction ins, GenTreePtr tree, GenTreePtr addr, unsigned offs)
{
    emitAttr size;

    if (!instIsFP(ins))
        size = emitTypeSize(tree->TypeGet());
    else
        size = EA_ATTR(genTypeSize(tree->TypeGet()));

    sched_AM(ins, size, REG_NA, false, addr, offs);
}

/*****************************************************************************
 *
 *  Emit an "op [r/m], reg" instruction (the r/m operand given by a tree).
 */

void CodeGen::instEmit_RM_RV(instruction ins, emitAttr size, GenTreePtr tree, regNumber reg, unsigned offs)
{
#ifdef _TARGET_XARCH_
    assert(instIsFP(ins) == 0);
#endif
    sched_AM(ins, size, reg, false, tree, offs);
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a tree (which has
 *  been made addressable).
 */

void CodeGen::inst_TT(instruction ins, GenTreePtr tree, unsigned offs, int shfv, emitAttr size)
{
    bool sizeInferred = false;

    if (size == EA_UNKNOWN)
    {
        sizeInferred = true;
        if (instIsFP(ins))
        {
            size = EA_ATTR(genTypeSize(tree->TypeGet()));
        }
        else
        {
            size = emitTypeSize(tree->TypeGet());
        }
    }

AGAIN:

    /* Is the value sitting in a register? */

    if (tree->gtFlags & GTF_REG_VAL)
    {
        regNumber reg;

#ifndef _TARGET_64BIT_
#ifdef LEGACY_BACKEND
    LONGREG_TT:
#endif // LEGACY_BACKEND
#endif

#if FEATURE_STACK_FP_X87

        /* Is this a floating-point instruction? */

        if (isFloatRegType(tree->gtType))
        {
            reg = tree->gtRegNum;

            assert(instIsFP(ins) && ins != INS_fst && ins != INS_fstp);
            assert(shfv == 0);

            inst_FS(ins, reg + genGetFPstkLevel());
            return;
        }
#endif // FEATURE_STACK_FP_X87

        assert(!instIsFP(ins));

#if CPU_LONG_USES_REGPAIR
        if (tree->gtType == TYP_LONG)
        {
            if (offs)
            {
                assert(offs == sizeof(int));
                reg = genRegPairHi(tree->gtRegPair);
            }
            else
            {
                reg = genRegPairLo(tree->gtRegPair);
            }
        }
        else
#endif // CPU_LONG_USES_REGPAIR
        {
            reg = tree->gtRegNum;
        }

        /* Make sure it is not the "stack-half" of an enregistered long */

        if (reg != REG_STK)
        {
            // For short types, indicate that the value is promoted to 4 bytes.
            // For longs, we are only emitting half of it so again set it to 4 bytes.
            // but leave the GC tracking information alone
            if (sizeInferred && EA_SIZE(size) < EA_4BYTE)
            {
                size = EA_SET_SIZE(size, 4);
            }

            if (shfv)
            {
                getEmitter()->emitIns_R_I(ins, size, reg, shfv);
            }
            else
            {
                inst_RV(ins, reg, tree->TypeGet(), size);
            }

            return;
        }
    }

    /* Is this a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        assert(!"ISSUE: If this can happen, we need to generate 'ins [ebp+spill]'");
    }

    switch (tree->gtOper)
    {
        unsigned varNum;

        case GT_LCL_VAR:

#ifdef LEGACY_BACKEND
            /* Is this an enregistered long ? */

            if (tree->gtType == TYP_LONG && !(tree->gtFlags & GTF_REG_VAL))
            {
                /* Avoid infinite loop */

                if (genMarkLclVar(tree))
                    goto LONGREG_TT;
            }
#endif // LEGACY_BACKEND

            inst_set_SV_var(tree);
            goto LCL;

        case GT_LCL_FLD:

            offs += tree->gtLclFld.gtLclOffs;
            goto LCL;

        LCL:
            varNum = tree->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);

            if (shfv)
            {
                getEmitter()->emitIns_S_I(ins, size, varNum, offs, shfv);
            }
            else
            {
                getEmitter()->emitIns_S(ins, size, varNum, offs);
            }

            return;

        case GT_CLS_VAR:
            // Make sure FP instruction size matches the operand size
            // (We optimized constant doubles to floats when we can, just want to
            // make sure that we don't mistakenly use 8 bytes when the
            // constant.
            assert(!isFloatRegType(tree->gtType) || genTypeSize(tree->gtType) == EA_SIZE_IN_BYTES(size));

            if (shfv)
            {
                getEmitter()->emitIns_C_I(ins, size, tree->gtClsVar.gtClsVarHnd, offs, shfv);
            }
            else
            {
                getEmitter()->emitIns_C(ins, size, tree->gtClsVar.gtClsVarHnd, offs);
            }
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        {
#ifndef LEGACY_BACKEND
            assert(!"inst_TT not supported for GT_IND, GT_NULLCHECK or GT_ARR_ELEM in !LEGACY_BACKEND");
#else  // LEGACY_BACKEND
            GenTreePtr addr = tree->OperIsIndir() ? tree->gtOp.gtOp1 : tree;
            if (shfv)
                sched_AM(ins, size, REG_NA, false, addr, offs, true, shfv);
            else
                instEmit_RM(ins, tree, addr, offs);
#endif // LEGACY_BACKEND
        }
        break;

#ifdef _TARGET_X86_
        case GT_CNS_INT:
            // We will get here for GT_MKREFANY from CodeGen::genPushArgList
            assert(offs == 0);
            assert(!shfv);
            if (tree->IsIconHandle())
                inst_IV_handle(ins, tree->gtIntCon.gtIconVal);
            else
                inst_IV(ins, tree->gtIntCon.gtIconVal);
            break;
#endif

        case GT_COMMA:
            //     tree->gtOp.gtOp1 - already processed by genCreateAddrMode()
            tree = tree->gtOp.gtOp2;
            goto AGAIN;

        default:
            assert(!"invalid address");
    }
}

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a tree (which has
 *  been made addressable) and another that is a register.
 */

void CodeGen::inst_TT_RV(instruction ins, GenTreePtr tree, regNumber reg, unsigned offs, emitAttr size, insFlags flags)
{
    assert(reg != REG_STK);

AGAIN:

    /* Is the value sitting in a register? */

    if (tree->gtFlags & GTF_REG_VAL)
    {
        regNumber rg2;

#ifdef _TARGET_64BIT_
        assert(!instIsFP(ins));

        rg2 = tree->gtRegNum;

        assert(offs == 0);
        assert(rg2 != REG_STK);

        if (ins != INS_mov || rg2 != reg)
        {
            inst_RV_RV(ins, rg2, reg, tree->TypeGet());
        }
        return;

#else // !_TARGET_64BIT_

#ifdef LEGACY_BACKEND
    LONGREG_TT_RV:
#endif // LEGACY_BACKEND

#ifdef _TARGET_XARCH_
        assert(!instIsFP(ins));
#endif

#if CPU_LONG_USES_REGPAIR
        if (tree->gtType == TYP_LONG)
        {
            if (offs)
            {
                assert(offs == sizeof(int));
                rg2 = genRegPairHi(tree->gtRegPair);
            }
            else
            {
                rg2 = genRegPairLo(tree->gtRegPair);
            }
        }
        else
#endif // CPU_LONG_USES_REGPAIR
        {
            rg2 = tree->gtRegNum;
        }

        if (rg2 != REG_STK)
        {
            if (ins != INS_mov || rg2 != reg)
                inst_RV_RV(ins, rg2, reg, tree->TypeGet(), size, flags);
            return;
        }

#endif // _TARGET_64BIT_
    }

    /* Is this a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        assert(!"ISSUE: If this can happen, we need to generate 'ins [ebp+spill]'");
    }

    if (size == EA_UNKNOWN)
    {
        if (instIsFP(ins))
        {
            size = EA_ATTR(genTypeSize(tree->TypeGet()));
        }
        else
        {
            size = emitTypeSize(tree->TypeGet());
        }
    }

    switch (tree->gtOper)
    {
        unsigned varNum;

        case GT_LCL_VAR:

#ifdef LEGACY_BACKEND
            if (tree->gtType == TYP_LONG && !(tree->gtFlags & GTF_REG_VAL))
            {
                /* Avoid infinite loop */

                if (genMarkLclVar(tree))
                    goto LONGREG_TT_RV;
            }
#endif // LEGACY_BACKEND

            inst_set_SV_var(tree);
            goto LCL;

        case GT_LCL_FLD:
        case GT_STORE_LCL_FLD:
            offs += tree->gtLclFld.gtLclOffs;
            goto LCL;

        LCL:

            varNum = tree->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);

#if CPU_LOAD_STORE_ARCH
            if (!getEmitter()->emitInsIsStore(ins))
            {
#ifndef LEGACY_BACKEND
                // TODO-LdStArch-Bug: Should regTmp be a dst on the node or an internal reg?
                // Either way, it is not currently being handled by Lowering.
                regNumber regTmp = tree->gtRegNum;
                assert(regTmp != REG_NA);
#else  // LEGACY_BACKEND
                regNumber regTmp      = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(reg));
#endif // LEGACY_BACKEND
                getEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                getEmitter()->emitIns_R_R(ins, size, regTmp, reg, flags);
                getEmitter()->emitIns_S_R(ins_Store(tree->TypeGet()), size, regTmp, varNum, offs);

                regTracker.rsTrackRegTrash(regTmp);
            }
            else
#endif
            {
                // ins is a Store instruction
                //
                getEmitter()->emitIns_S_R(ins, size, reg, varNum, offs);
#ifdef _TARGET_ARM_
                // If we need to set the flags then add an extra movs reg,reg instruction
                if (flags == INS_FLAGS_SET)
                    getEmitter()->emitIns_R_R(INS_mov, size, reg, reg, INS_FLAGS_SET);
#endif
            }
            return;

        case GT_CLS_VAR:
            // Make sure FP instruction size matches the operand size
            // (We optimized constant doubles to floats when we can, just want to
            // make sure that we don't mistakenly use 8 bytes when the
            // constant).
            assert(!isFloatRegType(tree->gtType) || genTypeSize(tree->gtType) == EA_SIZE_IN_BYTES(size));

#if CPU_LOAD_STORE_ARCH
            if (!getEmitter()->emitInsIsStore(ins))
            {
#ifndef LEGACY_BACKEND
                NYI("Store of GT_CLS_VAR not supported for ARM RyuJIT Backend");
#else  // LEGACY_BACKEND
                regNumber regTmpAddr  = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(reg));
                regNumber regTmpArith = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(reg) & ~genRegMask(regTmpAddr));

                getEmitter()->emitIns_R_C(INS_lea, EA_PTRSIZE, regTmpAddr, tree->gtClsVar.gtClsVarHnd, offs);
                getEmitter()->emitIns_R_R(ins_Load(tree->TypeGet()), size, regTmpArith, regTmpAddr);
                getEmitter()->emitIns_R_R(ins, size, regTmpArith, reg, flags);
                getEmitter()->emitIns_R_R(ins_Store(tree->TypeGet()), size, regTmpArith, regTmpAddr);

                regTracker.rsTrackRegTrash(regTmpAddr);
                regTracker.rsTrackRegTrash(regTmpArith);
#endif // LEGACY_BACKEND
            }
            else
#endif // CPU_LOAD_STORE_ARCH
            {
                getEmitter()->emitIns_C_R(ins, size, tree->gtClsVar.gtClsVarHnd, reg, offs);
            }
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        {
#ifndef LEGACY_BACKEND
            assert(!"inst_TT_RV not supported for GT_IND, GT_NULLCHECK or GT_ARR_ELEM in RyuJIT Backend");
#else  // LEGACY_BACKEND
            GenTreePtr addr = tree->OperIsIndir() ? tree->gtOp.gtOp1 : tree;
            sched_AM(ins, size, reg, false, addr, offs, false, 0, flags);
#endif // LEGACY_BACKEND
        }
        break;

        case GT_COMMA:
            //     tree->gtOp.gtOp1 - already processed by genCreateAddrMode()
            tree = tree->gtOp.gtOp2;
            goto AGAIN;

        default:
            assert(!"invalid address");
    }
}

regNumber CodeGen::genGetZeroRegister()
{
    regNumber zeroReg = REG_NA;

#if REDUNDANT_LOAD

    // Is the constant already in some register?

    zeroReg = regTracker.rsIconIsInReg(0);
#endif

#ifdef LEGACY_BACKEND
    if (zeroReg == REG_NA)
    {
        regMaskTP freeMask = regSet.rsRegMaskFree();

        if ((freeMask != 0) && (compiler->compCodeOpt() != Compiler::FAST_CODE))
        {
            // For SMALL_CODE and BLENDED_CODE,
            // we try to generate:
            //
            //  xor   reg,  reg
            //  mov   dest, reg
            //
            // When selecting a register to xor we try to avoid REG_TMP_0
            // when we have another CALLEE_TRASH register available.
            // This will often let us reuse the zeroed register in
            // several back-to-back assignments
            //
            if ((freeMask & RBM_CALLEE_TRASH) != RBM_TMP_0)
                freeMask &= ~RBM_TMP_0;
            zeroReg = regSet.rsGrabReg(freeMask); // PickReg in stress will pick 'random' registers
                                                  // We want one in the freeMask set, so just use GrabReg
            genSetRegToIcon(zeroReg, 0, TYP_INT);
        }
    }
#endif // !LEGACY_BACKEND

    return zeroReg;
}

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a tree (which has
 *  been made addressable) and another that is an integer constant.
 */
#ifdef LEGACY_BACKEND
void CodeGen::inst_TT_IV(instruction ins, GenTreePtr tree, ssize_t val, unsigned offs, emitAttr size, insFlags flags)
{
    bool sizeInferred = false;

    if (size == EA_UNKNOWN)
    {
        sizeInferred = true;
        if (instIsFP(ins))
            size = EA_ATTR(genTypeSize(tree->TypeGet()));
        else
            size = emitTypeSize(tree->TypeGet());
    }

AGAIN:

    /* Is the value sitting in a register? */

    if (tree->gtFlags & GTF_REG_VAL)
    {
#ifndef _TARGET_64BIT_
    LONGREG_TT_IV:
#endif
        regNumber reg;

        assert(instIsFP(ins) == 0);

#if CPU_LONG_USES_REGPAIR
        if (tree->gtType == TYP_LONG)
        {
            if (offs == 0)
            {
                reg = genRegPairLo(tree->gtRegPair);
            }
            else // offs == 4
            {
                assert(offs == sizeof(int));
                reg = genRegPairHi(tree->gtRegPair);
            }
#if CPU_LOAD_STORE_ARCH
            if (reg == REG_STK && !getEmitter()->emitInsIsLoadOrStore(ins))
            {
                reg = regSet.rsPickFreeReg();
                inst_RV_TT(INS_mov, reg, tree, offs, EA_4BYTE, flags);
                regTracker.rsTrackRegTrash(reg);
            }
#endif
        }
        else
#endif // CPU_LONG_USES_REGPAIR
        {
            reg = tree->gtRegNum;
        }

        if (reg != REG_STK)
        {
            // We always widen as part of enregistering,
            // so a smaller tree in a register can be
            // treated as 4 bytes
            if (sizeInferred && (size < EA_4BYTE))
            {
                size = EA_SET_SIZE(size, EA_4BYTE);
            }

            if ((ins == INS_mov) && !EA_IS_CNS_RELOC(size))
            {
                genSetRegToIcon(reg, val, tree->TypeGet(), flags);
            }
            else
            {
#if defined(_TARGET_XARCH_)
                inst_RV_IV(ins, reg, val, size);
#elif defined(_TARGET_ARM_)
                if (!EA_IS_CNS_RELOC(size) && arm_Valid_Imm_For_Instr(ins, val, flags))
                {
                    getEmitter()->emitIns_R_I(ins, size, reg, val, flags);
                }
                else // We need a scratch register
                {
                    // Load imm into a register
                    regMaskTP usedMask;
                    if (tree->gtType == TYP_LONG)
                    {
                        usedMask = genRegPairMask(tree->gtRegPair);
#if CPU_LOAD_STORE_ARCH
                        // In gtRegPair, this part of the long may have been on the stack
                        // in which case, the code above would have loaded it into 'reg'
                        // and so we need to also include 'reg' in the set of registers
                        // that are already in use.
                        usedMask |= genRegMask(reg);
#endif // CPU_LOAD_STORE_ARCH
                    }
                    else
                    {
                        usedMask = genRegMask(tree->gtRegNum);
                    }
                    regNumber immReg = regSet.rsGrabReg(RBM_ALLINT & ~usedMask);
                    noway_assert(reg != immReg);
                    instGen_Set_Reg_To_Imm(size, immReg, val);
                    if (getEmitter()->emitInsIsStore(ins))
                        ins = INS_mov;
                    getEmitter()->emitIns_R_R(ins, size, reg, immReg, flags);
                }
#else
                NYI("inst_TT_IV - unknown target");
#endif
            }
            return;
        }
    }

#ifdef _TARGET_XARCH_
    /* Are we storing a zero? */

    if ((ins == INS_mov) && (val == 0) &&
        ((genTypeSize(tree->gtType) == sizeof(int)) || (genTypeSize(tree->gtType) == REGSIZE_BYTES)))
    {
        regNumber zeroReg;

        zeroReg = genGetZeroRegister();

        if (zeroReg != REG_NA)
        {
            inst_TT_RV(INS_mov, tree, zeroReg, offs);
            return;
        }
    }
#endif

#if CPU_LOAD_STORE_ARCH
    /* Are we storing/comparing with a constant? */

    if (getEmitter()->emitInsIsStore(ins) || getEmitter()->emitInsIsCompare(ins))
    {
        // Load val into a register

        regNumber valReg;
        valReg = regSet.rsGrabReg(RBM_ALLINT);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, valReg, val);
        inst_TT_RV(ins, tree, valReg, offs, size, flags);
        return;
    }
    else if (ins == INS_mov)
    {
        assert(!"Please call ins_Store(type) to get the store instruction");
    }
    assert(!getEmitter()->emitInsIsLoad(ins));
#endif // CPU_LOAD_STORE_ARCH

    /* Is this a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        assert(!"ISSUE: If this can happen, we need to generate 'ins [ebp+spill], icon'");
    }

#ifdef _TARGET_AMD64_
    if ((EA_SIZE(size) == EA_8BYTE) && (((int)val != (ssize_t)val) || EA_IS_CNS_RELOC(size)))
    {
        // Load imm into a register
        regNumber immReg = regSet.rsGrabReg(RBM_ALLINT);
        instGen_Set_Reg_To_Imm(size, immReg, val);
        inst_TT_RV(ins, tree, immReg, offs);
        return;
    }
#endif // _TARGET_AMD64_

    int ival = (int)val;

    switch (tree->gtOper)
    {
        unsigned   varNum;
        LclVarDsc* varDsc;

        case GT_LCL_FLD:

            varNum = tree->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);
            offs += tree->gtLclFld.gtLclOffs;

            goto LCL;

        case GT_LCL_VAR:

#ifndef _TARGET_64BIT_
            /* Is this an enregistered long ? */

            if (tree->gtType == TYP_LONG && !(tree->gtFlags & GTF_REG_VAL))
            {
                /* Avoid infinite loop */

                if (genMarkLclVar(tree))
                    goto LONGREG_TT_IV;
            }
#endif // !_TARGET_64BIT_

            inst_set_SV_var(tree);

            varNum = tree->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);
            varDsc = &compiler->lvaTable[varNum];

            // Fix the immediate by sign extending if needed
            if (size < EA_4BYTE && !varTypeIsUnsigned(varDsc->TypeGet()))
            {
                if (size == EA_1BYTE)
                {
                    if ((ival & 0x7f) != ival)
                        ival = ival | 0xffffff00;
                }
                else
                {
                    assert(size == EA_2BYTE);
                    if ((ival & 0x7fff) != ival)
                        ival = ival | 0xffff0000;
                }
            }

            // A local stack slot is at least 4 bytes in size, regardles of
            // what the local var is typed as, so auto-promote it here
            // unless the codegenerator told us a size, or it is a field
            // of a promoted struct
            if (sizeInferred && (size < EA_4BYTE) && !varDsc->lvIsStructField)
            {
                size = EA_SET_SIZE(size, EA_4BYTE);
            }

        LCL:

            /* Integer instructions never operate on more than EA_PTRSIZE */

            assert(instIsFP(ins) == false);

#if CPU_LOAD_STORE_ARCH
            if (!getEmitter()->emitInsIsStore(ins))
            {
                regNumber regTmp = regSet.rsPickFreeReg(RBM_ALLINT);
                getEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                regTracker.rsTrackRegTrash(regTmp);

                if (arm_Valid_Imm_For_Instr(ins, val, flags))
                {
                    getEmitter()->emitIns_R_I(ins, size, regTmp, ival, flags);
                }
                else // We need a scratch register
                {
                    // Load imm into a register
                    regNumber regImm = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regTmp));

                    instGen_Set_Reg_To_Imm(size, regImm, val);
                    getEmitter()->emitIns_R_R(ins, size, regTmp, regImm, flags);
                }
                getEmitter()->emitIns_S_R(ins_Store(tree->TypeGet()), size, regTmp, varNum, offs);
            }
            else
#endif
            {
                getEmitter()->emitIns_S_I(ins, size, varNum, offs, ival);
            }
            return;

        case GT_CLS_VAR:
            // Make sure FP instruction size matches the operand size
            // (We optimize constant doubles to floats when we can)
            // We just want to make sure that we don't mistakenly
            // use 8 bytes when the constant is smaller.
            //
            assert(!isFloatRegType(tree->gtType) || genTypeSize(tree->gtType) == EA_SIZE_IN_BYTES(size));

#if CPU_LOAD_STORE_ARCH
            regNumber regTmpAddr;
            regTmpAddr = regSet.rsPickFreeReg(RBM_ALLINT);

            getEmitter()->emitIns_R_C(INS_lea, EA_PTRSIZE, regTmpAddr, tree->gtClsVar.gtClsVarHnd, offs);
            regTracker.rsTrackRegTrash(regTmpAddr);

            if (!getEmitter()->emitInsIsStore(ins))
            {
                regNumber regTmpArith = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(regTmpAddr));

                getEmitter()->emitIns_R_R(ins_Load(tree->TypeGet()), size, regTmpArith, regTmpAddr);

                if (arm_Valid_Imm_For_Instr(ins, ival, flags))
                {
                    getEmitter()->emitIns_R_R_I(ins, size, regTmpArith, regTmpArith, ival, flags);
                }
                else
                {
                    regNumber regTmpImm =
                        regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(regTmpAddr) & ~genRegMask(regTmpArith));
                    instGen_Set_Reg_To_Imm(EA_4BYTE, regTmpImm, (ssize_t)ival);
                    getEmitter()->emitIns_R_R(ins, size, regTmpArith, regTmpImm, flags);
                }
                regTracker.rsTrackRegTrash(regTmpArith);

                getEmitter()->emitIns_R_R(ins_Store(tree->TypeGet()), size, regTmpArith, regTmpAddr);
            }
            else
            {
                regNumber regTmpImm = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(regTmpAddr));

                instGen_Set_Reg_To_Imm(EA_4BYTE, regTmpImm, (ssize_t)ival, flags);
                getEmitter()->emitIns_R_R(ins_Store(tree->TypeGet()), size, regTmpImm, regTmpAddr);
            }
#else // !CPU_LOAD_STORE_ARCH
            getEmitter()->emitIns_C_I(ins, size, tree->gtClsVar.gtClsVarHnd, offs, ival);
#endif
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        {
            GenTreePtr addr = tree->OperIsIndir() ? tree->gtOp.gtOp1 : tree;
            sched_AM(ins, size, REG_NA, false, addr, offs, true, ival, flags);
        }
            return;

        case GT_COMMA:
            //     tree->gtOp.gtOp1 - already processed by genCreateAddrMode()
            tree = tree->gtOp.gtOp2;
            goto AGAIN;

        default:
            assert(!"invalid address");
    }
}
#endif // LEGACY_BACKEND

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a register and the
 *  other one by an indirection tree (which has been made addressable).
 */

void CodeGen::inst_RV_AT(
    instruction ins, emitAttr size, var_types type, regNumber reg, GenTreePtr tree, unsigned offs, insFlags flags)
{
#ifdef _TARGET_XARCH_
#ifdef DEBUG
    // If it is a GC type and the result is not, then either
    // 1) it is an LEA
    // 2) optOptimizeBools() optimized if (ref != 0 && ref != 0) to if (ref & ref)
    // 3) optOptimizeBools() optimized if (ref == 0 || ref == 0) to if (ref | ref)
    // 4) byref - byref = int
    if (type == TYP_REF && !EA_IS_GCREF(size))
        assert((EA_IS_BYREF(size) && ins == INS_add) || (ins == INS_lea || ins == INS_and || ins == INS_or));
    if (type == TYP_BYREF && !EA_IS_BYREF(size))
        assert(ins == INS_lea || ins == INS_and || ins == INS_or || ins == INS_sub);
    assert(!instIsFP(ins));
#endif
#endif

    // Integer instructions never operate on more than EA_PTRSIZE.
    if (EA_SIZE(size) > EA_PTRSIZE && !instIsFP(ins))
        EA_SET_SIZE(size, EA_PTRSIZE);

    GenTreePtr addr = tree;
    sched_AM(ins, size, reg, true, addr, offs, false, 0, flags);
}

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by an indirection tree
 *  (which has been made addressable) and an integer constant.
 */

void CodeGen::inst_AT_IV(instruction ins, emitAttr size, GenTreePtr baseTree, int icon, unsigned offs)
{
    sched_AM(ins, size, REG_NA, false, baseTree, offs, true, icon);
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a register and the
 *  other one by a tree (which has been made addressable).
 */

void CodeGen::inst_RV_TT(instruction ins,
                         regNumber   reg,
                         GenTreePtr  tree,
                         unsigned    offs,
                         emitAttr    size,
                         insFlags    flags /* = INS_FLAGS_DONT_CARE */)
{
    assert(reg != REG_STK);

    if (size == EA_UNKNOWN)
    {
        if (!instIsFP(ins))
        {
            size = emitTypeSize(tree->TypeGet());
        }
        else
        {
            size = EA_ATTR(genTypeSize(tree->TypeGet()));
        }
    }

#ifdef _TARGET_XARCH_
#ifdef DEBUG
    // If it is a GC type and the result is not, then either
    // 1) it is an LEA
    // 2) optOptimizeBools() optimized if (ref != 0 && ref != 0) to if (ref & ref)
    // 3) optOptimizeBools() optimized if (ref == 0 || ref == 0) to if (ref | ref)
    // 4) byref - byref = int
    if (tree->gtType == TYP_REF && !EA_IS_GCREF(size))
    {
        assert((EA_IS_BYREF(size) && ins == INS_add) || (ins == INS_lea || ins == INS_and || ins == INS_or));
    }
    if (tree->gtType == TYP_BYREF && !EA_IS_BYREF(size))
    {
        assert(ins == INS_lea || ins == INS_and || ins == INS_or || ins == INS_sub);
    }
#endif
#endif

#if CPU_LOAD_STORE_ARCH
    if (ins == INS_mov)
    {
#if defined(_TARGET_ARM_)
        if (tree->TypeGet() != TYP_LONG)
        {
            ins = ins_Move_Extend(tree->TypeGet(), (tree->gtFlags & GTF_REG_VAL) != 0);
        }
        else if (offs == 0)
        {
            ins = ins_Move_Extend(TYP_INT,
                                  (tree->gtFlags & GTF_REG_VAL) != 0 && genRegPairLo(tree->gtRegPair) != REG_STK);
        }
        else
        {
            ins = ins_Move_Extend(TYP_INT,
                                  (tree->gtFlags & GTF_REG_VAL) != 0 && genRegPairHi(tree->gtRegPair) != REG_STK);
        }
#elif defined(_TARGET_ARM64_)
        ins = ins_Move_Extend(tree->TypeGet(), (tree->gtFlags & GTF_REG_VAL) != 0);
#else
        NYI("CodeGen::inst_RV_TT with INS_mov");
#endif
    }
#endif // CPU_LOAD_STORE_ARCH

AGAIN:

    /* Is the value sitting in a register? */

    if (tree->gtFlags & GTF_REG_VAL)
    {
#ifdef _TARGET_64BIT_
        assert(instIsFP(ins) == 0);

        regNumber rg2 = tree->gtRegNum;

        assert(offs == 0);
        assert(rg2 != REG_STK);

        if ((ins != INS_mov) || (rg2 != reg))
        {
            inst_RV_RV(ins, reg, rg2, tree->TypeGet(), size);
        }
        return;

#else // !_TARGET_64BIT_

#ifdef LEGACY_BACKEND
    LONGREG_RVTT:
#endif // LEGACY_BACKEND

#ifdef _TARGET_XARCH_
        assert(instIsFP(ins) == 0);
#endif

        regNumber rg2;

#if CPU_LONG_USES_REGPAIR
        if (tree->gtType == TYP_LONG)
        {
            if (offs)
            {
                assert(offs == sizeof(int));

                rg2 = genRegPairHi(tree->gtRegPair);
            }
            else
            {
                rg2 = genRegPairLo(tree->gtRegPair);
            }
        }
        else
#endif // LEGACY_BACKEND
        {
            rg2 = tree->gtRegNum;
        }

        if (rg2 != REG_STK)
        {
#ifdef _TARGET_ARM_
            if (getEmitter()->emitInsIsLoad(ins) || (ins == INS_lea))
            {
                ins = ins_Copy(tree->TypeGet());
            }
#endif

            bool isMoveIns = (ins == INS_mov);
#ifdef _TARGET_ARM_
            if (ins == INS_vmov)
                isMoveIns = true;
#endif
            if (!isMoveIns || (rg2 != reg))
            {
                inst_RV_RV(ins, reg, rg2, tree->TypeGet(), size, flags);
            }
            return;
        }

#endif // _TARGET_64BIT_
    }

    /* Is this a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        assert(!"ISSUE: If this can happen, we need to generate 'ins [ebp+spill]'");
    }

    switch (tree->gtOper)
    {
        unsigned varNum;

        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:

#ifdef LEGACY_BACKEND
            /* Is this an enregistered long ? */

            if (tree->gtType == TYP_LONG && !(tree->gtFlags & GTF_REG_VAL))
            {

                /* Avoid infinite loop */

                if (genMarkLclVar(tree))
                    goto LONGREG_RVTT;
            }
#endif // LEGACY_BACKEND

            inst_set_SV_var(tree);
            goto LCL;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_FLD:
            offs += tree->gtLclFld.gtLclOffs;
            goto LCL;

        LCL:
            varNum = tree->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);

#ifdef _TARGET_ARM_
            switch (ins)
            {
                case INS_mov:
                    ins = ins_Load(tree->TypeGet());
                    __fallthrough;

                case INS_lea:
                case INS_ldr:
                case INS_ldrh:
                case INS_ldrb:
                case INS_ldrsh:
                case INS_ldrsb:
                case INS_vldr:
                    assert(flags != INS_FLAGS_SET);
                    getEmitter()->emitIns_R_S(ins, size, reg, varNum, offs);
                    return;

                default:
                    regNumber regTmp;
#ifndef LEGACY_BACKEND
                    if (tree->TypeGet() == TYP_LONG)
                        regTmp = (offs == 0) ? genRegPairLo(tree->gtRegPair) : genRegPairHi(tree->gtRegPair);
                    else
                        regTmp = tree->gtRegNum;
#else  // LEGACY_BACKEND
                    if (varTypeIsFloating(tree))
                    {
                        regTmp = regSet.PickRegFloat(tree->TypeGet());
                    }
                    else
                    {
                        regTmp = regSet.rsPickReg(RBM_ALLINT & ~genRegMask(reg));
                    }
#endif // LEGACY_BACKEND

                    getEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                    getEmitter()->emitIns_R_R(ins, size, reg, regTmp, flags);

                    regTracker.rsTrackRegTrash(regTmp);
                    return;
            }
#else  // !_TARGET_ARM_
            getEmitter()->emitIns_R_S(ins, size, reg, varNum, offs);
            return;
#endif // !_TARGET_ARM_

        case GT_CLS_VAR:
            // Make sure FP instruction size matches the operand size
            // (We optimized constant doubles to floats when we can, just want to
            // make sure that we don't mistakenly use 8 bytes when the
            // constant.
            assert(!isFloatRegType(tree->gtType) || genTypeSize(tree->gtType) == EA_SIZE_IN_BYTES(size));

#if CPU_LOAD_STORE_ARCH
#ifndef LEGACY_BACKEND
            assert(!"GT_CLS_VAR not supported in ARM RyuJIT backend");
#else  // LEGACY_BACKEND
            switch (ins)
            {
                case INS_mov:
                    ins = ins_Load(tree->TypeGet());

                    __fallthrough;

                case INS_lea:
                case INS_ldr:
                case INS_ldrh:
                case INS_ldrb:
                case INS_ldrsh:
                case INS_ldrsb:
                case INS_vldr:
                    assert(flags != INS_FLAGS_SET);
                    getEmitter()->emitIns_R_C(ins, size, reg, tree->gtClsVar.gtClsVarHnd, offs);
                    return;

                default:
                    regNumber regTmp = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(reg));
                    getEmitter()->emitIns_R_C(ins_Load(tree->TypeGet()), size, regTmp, tree->gtClsVar.gtClsVarHnd,
                                              offs);
                    getEmitter()->emitIns_R_R(ins, size, reg, regTmp, flags);
                    regTracker.rsTrackRegTrash(regTmp);
                    return;
            }
#endif // LEGACY_BACKEND
#else  // CPU_LOAD_STORE_ARCH
            getEmitter()->emitIns_R_C(ins, size, reg, tree->gtClsVar.gtClsVarHnd, offs);
#endif // CPU_LOAD_STORE_ARCH
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        case GT_LEA:
        {
#ifndef LEGACY_BACKEND
            assert(!"inst_RV_TT not supported for GT_IND, GT_NULLCHECK, GT_ARR_ELEM or GT_LEA in !LEGACY_BACKEND");
#else  // LEGACY_BACKEND
            GenTreePtr addr = tree->OperIsIndir() ? tree->gtOp.gtOp1 : tree;
            inst_RV_AT(ins, size, tree->TypeGet(), reg, addr, offs, flags);
#endif // LEGACY_BACKEND
        }
        break;

        case GT_CNS_INT:

            assert(offs == 0);

            inst_RV_IV(ins, reg, tree->gtIntCon.gtIconVal, emitActualTypeSize(tree->TypeGet()), flags);
            break;

        case GT_CNS_LNG:

            assert(size == EA_4BYTE || size == EA_8BYTE);

#ifdef _TARGET_AMD64_
            assert(offs == 0);
#endif // _TARGET_AMD64_

            ssize_t  constVal;
            emitAttr size;
            if (offs == 0)
            {
                constVal = (ssize_t)(tree->gtLngCon.gtLconVal);
                size     = EA_PTRSIZE;
            }
            else
            {
                constVal = (ssize_t)(tree->gtLngCon.gtLconVal >> 32);
                size     = EA_4BYTE;
            }
#ifndef LEGACY_BACKEND
#ifdef _TARGET_ARM_
            if ((ins != INS_mov) && !arm_Valid_Imm_For_Instr(ins, constVal, flags))
            {
                regNumber constReg = (offs == 0) ? genRegPairLo(tree->gtRegPair) : genRegPairHi(tree->gtRegPair);
                instGen_Set_Reg_To_Imm(size, constReg, constVal);
                getEmitter()->emitIns_R_R(ins, size, reg, constReg, flags);
                break;
            }
#endif // _TARGET_ARM_
#endif // !LEGACY_BACKEND

            inst_RV_IV(ins, reg, constVal, size, flags);
            break;

        case GT_COMMA:
            tree = tree->gtOp.gtOp2;
            goto AGAIN;

        default:
            assert(!"invalid address");
    }
}

/*****************************************************************************
 *
 *  Generate the 3-operand imul instruction "imul reg, [tree], icon"
 *  which is reg=[tree]*icon
 */
#ifdef LEGACY_BACKEND
void CodeGen::inst_RV_TT_IV(instruction ins, regNumber reg, GenTreePtr tree, int val)
{
    assert(tree->gtType <= TYP_I_IMPL);

#ifdef _TARGET_XARCH_
    /* Only 'imul' uses this instruction format. Since we don't represent
       three operands for an instruction, we encode the target register as
       an implicit operand */

    assert(ins == INS_imul);
    ins = getEmitter()->inst3opImulForReg(reg);

    genUpdateLife(tree);
    inst_TT_IV(ins, tree, val);
#else
    NYI("inst_RV_TT_IV - unknown target");
#endif
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Generate a "shift reg, icon" instruction.
 */

void CodeGen::inst_RV_SH(
    instruction ins, emitAttr size, regNumber reg, unsigned val, insFlags flags /* = INS_FLAGS_DONT_CARE */)
{
#if defined(_TARGET_ARM_)

    if (val >= 32)
        val &= 0x1f;

    getEmitter()->emitIns_R_I(ins, size, reg, val, flags);

#elif defined(_TARGET_XARCH_)

#ifdef _TARGET_AMD64_
    // X64 JB BE insures only encodable values make it here.
    // x86 can encode 8 bits, though it masks down to 5 or 6
    // depending on 32-bit or 64-bit registers are used.
    // Here we will allow anything that is encodable.
    assert(val < 256);
#endif

    ins = genMapShiftInsToShiftByConstantIns(ins, val);

    if (val == 1)
    {
        getEmitter()->emitIns_R(ins, size, reg);
    }
    else
    {
        getEmitter()->emitIns_R_I(ins, size, reg, val);
    }

#else
    NYI("inst_RV_SH - unknown target");
#endif // _TARGET_*
}

/*****************************************************************************
 *
 *  Generate a "shift [r/m], icon" instruction.
 */

void CodeGen::inst_TT_SH(instruction ins, GenTreePtr tree, unsigned val, unsigned offs)
{
#ifdef _TARGET_XARCH_
    if (val == 0)
    {
        // Shift by 0 - why are you wasting our precious time????
        return;
    }

    ins = genMapShiftInsToShiftByConstantIns(ins, val);
    if (val == 1)
    {
        inst_TT(ins, tree, offs, 0, emitTypeSize(tree->TypeGet()));
    }
    else
    {
        inst_TT(ins, tree, offs, val, emitTypeSize(tree->TypeGet()));
    }
#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM_
    inst_TT(ins, tree, offs, val, emitTypeSize(tree->TypeGet()));
#endif
}

/*****************************************************************************
 *
 *  Generate a "shift [addr], cl" instruction.
 */

void CodeGen::inst_TT_CL(instruction ins, GenTreePtr tree, unsigned offs)
{
    inst_TT(ins, tree, offs, 0, emitTypeSize(tree->TypeGet()));
}

/*****************************************************************************
 *
 *  Generate an instruction of the form "op reg1, reg2, icon".
 */

#if defined(_TARGET_XARCH_)
void CodeGen::inst_RV_RV_IV(instruction ins, emitAttr size, regNumber reg1, regNumber reg2, unsigned ival)
{
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    assert(ins == INS_shld || ins == INS_shrd || ins == INS_shufps || ins == INS_shufpd || ins == INS_pshufd ||
           ins == INS_cmpps || ins == INS_cmppd || ins == INS_dppd || ins == INS_dpps || ins == INS_insertps);
#else  // !_TARGET_XARCH_
    assert(ins == INS_shld || ins == INS_shrd);
#endif // !_TARGET_XARCH_

    getEmitter()->emitIns_R_R_I(ins, size, reg1, reg2, ival);
}
#endif

/*****************************************************************************
 *
 *  Generate an instruction with two registers, the second one being a byte
 *  or word register (i.e. this is something like "movzx eax, cl").
 */

void CodeGen::inst_RV_RR(instruction ins, emitAttr size, regNumber reg1, regNumber reg2)
{
    assert(size == EA_1BYTE || size == EA_2BYTE);
#ifdef _TARGET_XARCH_
    assert(ins == INS_movsx || ins == INS_movzx);
    assert(size != EA_1BYTE || (genRegMask(reg2) & RBM_BYTE_REGS));
#endif

    getEmitter()->emitIns_R_R(ins, size, reg1, reg2);
}

/*****************************************************************************
 *
 *  The following should all end up inline in compiler.hpp at some point.
 */

void CodeGen::inst_ST_RV(instruction ins, TempDsc* tmp, unsigned ofs, regNumber reg, var_types type)
{
    getEmitter()->emitIns_S_R(ins, emitActualTypeSize(type), reg, tmp->tdTempNum(), ofs);
}

void CodeGen::inst_ST_IV(instruction ins, TempDsc* tmp, unsigned ofs, int val, var_types type)
{
    getEmitter()->emitIns_S_I(ins, emitActualTypeSize(type), tmp->tdTempNum(), ofs, val);
}

#if FEATURE_FIXED_OUT_ARGS
/*****************************************************************************
 *
 *  Generate an instruction that references the outgoing argument space
 *  like "str r3, [sp+0x04]"
 */

void CodeGen::inst_SA_RV(instruction ins, unsigned ofs, regNumber reg, var_types type)
{
    assert(ofs < compiler->lvaOutgoingArgSpaceSize);

    getEmitter()->emitIns_S_R(ins, emitActualTypeSize(type), reg, compiler->lvaOutgoingArgSpaceVar, ofs);
}

void CodeGen::inst_SA_IV(instruction ins, unsigned ofs, int val, var_types type)
{
    assert(ofs < compiler->lvaOutgoingArgSpaceSize);

    getEmitter()->emitIns_S_I(ins, emitActualTypeSize(type), compiler->lvaOutgoingArgSpaceVar, ofs, val);
}
#endif // FEATURE_FIXED_OUT_ARGS

/*****************************************************************************
 *
 *  Generate an instruction with one register and one operand that is byte
 *  or short (e.g. something like "movzx eax, byte ptr [edx]").
 */

void CodeGen::inst_RV_ST(instruction ins, emitAttr size, regNumber reg, GenTreePtr tree)
{
    assert(size == EA_1BYTE || size == EA_2BYTE);

    /* "movsx erx, rl" must be handled as a special case */

    if (tree->gtFlags & GTF_REG_VAL)
    {
        inst_RV_RR(ins, size, reg, tree->gtRegNum);
    }
    else
    {
        inst_RV_TT(ins, reg, tree, 0, size);
    }
}

void CodeGen::inst_RV_ST(instruction ins, regNumber reg, TempDsc* tmp, unsigned ofs, var_types type, emitAttr size)
{
    if (size == EA_UNKNOWN)
    {
        size = emitActualTypeSize(type);
    }

#ifdef _TARGET_ARM_
    switch (ins)
    {
        case INS_mov:
            assert(!"Please call ins_Load(type) to get the load instruction");
            break;

        case INS_add:
        case INS_ldr:
        case INS_ldrh:
        case INS_ldrb:
        case INS_ldrsh:
        case INS_ldrsb:
        case INS_lea:
        case INS_vldr:
            getEmitter()->emitIns_R_S(ins, size, reg, tmp->tdTempNum(), ofs);
            break;

        default:
#ifndef LEGACY_BACKEND
            assert(!"Default inst_RV_ST case not supported for Arm !LEGACY_BACKEND");
#else  // LEGACY_BACKEND
            regNumber regTmp;
            if (varTypeIsFloating(type))
            {
                regTmp = regSet.PickRegFloat(type);
            }
            else
            {
                regTmp = regSet.rsPickFreeReg(RBM_ALLINT & ~genRegMask(reg));
            }
            getEmitter()->emitIns_R_S(ins_Load(type), size, regTmp, tmp->tdTempNum(), ofs);
            regTracker.rsTrackRegTrash(regTmp);
            getEmitter()->emitIns_R_R(ins, size, reg, regTmp);
#endif // LEGACY_BACKEND
            break;
    }
#else  // !_TARGET_ARM_
    getEmitter()->emitIns_R_S(ins, size, reg, tmp->tdTempNum(), ofs);
#endif // !_TARGET_ARM_
}

void CodeGen::inst_mov_RV_ST(regNumber reg, GenTreePtr tree)
{
    /* Figure out the size of the value being loaded */

    emitAttr    size    = EA_ATTR(genTypeSize(tree->gtType));
    instruction loadIns = ins_Move_Extend(tree->TypeGet(), (tree->gtFlags & GTF_REG_VAL) != 0);

    if (size < EA_4BYTE)
    {
        if ((tree->gtFlags & GTF_SMALL_OK) && (size == EA_1BYTE)
#if CPU_HAS_BYTE_REGS
            && (genRegMask(reg) & RBM_BYTE_REGS)
#endif
                )
        {
            /* We only need to load the actual size */

            inst_RV_TT(INS_mov, reg, tree, 0, EA_1BYTE);
        }
        else
        {
            /* Generate the "movsx/movzx" opcode */

            inst_RV_ST(loadIns, size, reg, tree);
        }
    }
    else
    {
        /* Compute op1 into the target register */

        inst_RV_TT(loadIns, reg, tree);
    }
}
#ifdef _TARGET_XARCH_
void CodeGen::inst_FS_ST(instruction ins, emitAttr size, TempDsc* tmp, unsigned ofs)
{
    getEmitter()->emitIns_S(ins, size, tmp->tdTempNum(), ofs);
}
#endif

#ifdef _TARGET_ARM_
bool CodeGenInterface::validImmForInstr(instruction ins, ssize_t imm, insFlags flags)
{
    if (getEmitter()->emitInsIsLoadOrStore(ins) && !instIsFP(ins))
    {
        return validDispForLdSt(imm, TYP_INT);
    }

    bool result = false;
    switch (ins)
    {
        case INS_cmp:
        case INS_cmn:
            if (validImmForAlu(imm) || validImmForAlu(-imm))
                result = true;
            break;

        case INS_and:
        case INS_bic:
        case INS_orr:
        case INS_orn:
        case INS_mvn:
            if (validImmForAlu(imm) || validImmForAlu(~imm))
                result = true;
            break;

        case INS_mov:
            if (validImmForMov(imm))
                result = true;
            break;

        case INS_addw:
        case INS_subw:
            if ((unsigned_abs(imm) <= 0x00000fff) && (flags != INS_FLAGS_SET)) // 12-bit immediate
                result = true;
            break;

        case INS_add:
        case INS_sub:
            if (validImmForAdd(imm, flags))
                result = true;
            break;

        case INS_tst:
        case INS_eor:
        case INS_teq:
        case INS_adc:
        case INS_sbc:
        case INS_rsb:
            if (validImmForAlu(imm))
                result = true;
            break;

        case INS_asr:
        case INS_lsl:
        case INS_lsr:
        case INS_ror:
            if (imm > 0 && imm <= 32)
                result = true;
            break;

        case INS_vstr:
        case INS_vldr:
            if ((imm & 0x3FC) == imm)
                result = true;
            break;

        default:
            break;
    }
    return result;
}
bool CodeGen::arm_Valid_Imm_For_Instr(instruction ins, ssize_t imm, insFlags flags)
{
    return validImmForInstr(ins, imm, flags);
}

bool CodeGenInterface::validDispForLdSt(ssize_t disp, var_types type)
{
    if (varTypeIsFloating(type))
    {
        if ((disp & 0x3FC) == disp)
            return true;
        else
            return false;
    }
    else
    {
        if ((disp >= -0x00ff) && (disp <= 0x0fff))
            return true;
        else
            return false;
    }
}
bool CodeGen::arm_Valid_Disp_For_LdSt(ssize_t disp, var_types type)
{
    return validDispForLdSt(disp, type);
}

bool CodeGenInterface::validImmForAlu(ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_alu(imm);
}
bool CodeGen::arm_Valid_Imm_For_Alu(ssize_t imm)
{
    return validImmForAlu(imm);
}

bool CodeGenInterface::validImmForMov(ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_mov(imm);
}
bool CodeGen::arm_Valid_Imm_For_Mov(ssize_t imm)
{
    return validImmForMov(imm);
}

bool CodeGen::arm_Valid_Imm_For_Small_Mov(regNumber reg, ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_small_mov(reg, imm, flags);
}

bool CodeGenInterface::validImmForAdd(ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_add(imm, flags);
}
bool CodeGen::arm_Valid_Imm_For_Add(ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_add(imm, flags);
}

// Check "add Rd,SP,i10"
bool CodeGen::arm_Valid_Imm_For_Add_SP(ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_add_sp(imm);
}

bool CodeGenInterface::validImmForBL(ssize_t addr)
{
    return
        // If we are running the altjit for NGEN, then assume we can use the "BL" instruction.
        // This matches the usual behavior for NGEN, since we normally do generate "BL".
        (!compiler->info.compMatchedVM && (compiler->opts.eeFlags & CORJIT_FLG_PREJIT)) ||
        (compiler->eeGetRelocTypeHint((void*)addr) == IMAGE_REL_BASED_THUMB_BRANCH24);
}
bool CodeGen::arm_Valid_Imm_For_BL(ssize_t addr)
{
    return validImmForBL(addr);
}

// Returns true if this instruction writes to a destination register
//
bool CodeGen::ins_Writes_Dest(instruction ins)
{
    switch (ins)
    {

        case INS_cmp:
        case INS_cmn:
        case INS_tst:
        case INS_teq:
            return false;

        default:
            return true;
    }
}
#endif // _TARGET_ARM_

/*****************************************************************************
 *
 *  Get the machine dependent instruction for performing sign/zero extension.
 *
 *  Parameters
 *      srcType   - source type
 *      srcInReg  - whether source is in a register
 */
instruction CodeGen::ins_Move_Extend(var_types srcType, bool srcInReg)
{
    instruction ins = INS_invalid;

    if (varTypeIsSIMD(srcType))
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        // SSE2/AVX requires destination to be a reg always.
        // If src is in reg means, it is a reg-reg move.
        //
        // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
        // former doesn't require 66h prefix and one byte smaller than the
        // latter.
        //
        // TODO-CQ: based on whether src type is aligned use movaps instead

        return (srcInReg) ? INS_movaps : INS_movups;
#else  // !defined(_TARGET_XARCH_) || defined(LEGACY_BACKEND)
        assert(!"unhandled SIMD type");
#endif // !defined(_TARGET_XARCH_) || defined(LEGACY_BACKEND)
    }

#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    if (varTypeIsFloating(srcType))
    {
        if (srcType == TYP_DOUBLE)
        {
            return (srcInReg) ? INS_movaps : INS_movsdsse2;
        }
        else if (srcType == TYP_FLOAT)
        {
            return (srcInReg) ? INS_movaps : INS_movss;
        }
        else
        {
            assert(!"unhandled floating type");
        }
    }
#elif defined(_TARGET_ARM_)
    if (varTypeIsFloating(srcType))
        return INS_vmov;
#else
    assert(!varTypeIsFloating(srcType));
#endif

#if defined(_TARGET_XARCH_)
    if (!varTypeIsSmall(srcType))
    {
        ins = INS_mov;
    }
    else if (varTypeIsUnsigned(srcType))
    {
        ins = INS_movzx;
    }
    else
    {
        ins = INS_movsx;
    }
#elif defined(_TARGET_ARM_)
    //
    // Register to Register zero/sign extend operation
    //
    if (srcInReg)
    {
        if (!varTypeIsSmall(srcType))
        {
            ins = INS_mov;
        }
        else if (varTypeIsUnsigned(srcType))
        {
            if (varTypeIsByte(srcType))
                ins = INS_uxtb;
            else
                ins = INS_uxth;
        }
        else
        {
            if (varTypeIsByte(srcType))
                ins = INS_sxtb;
            else
                ins = INS_sxth;
        }
    }
    else
    {
        ins = ins_Load(srcType);
    }
#elif defined(_TARGET_ARM64_)
    //
    // Register to Register zero/sign extend operation
    //
    if (srcInReg)
    {
        if (varTypeIsUnsigned(srcType))
        {
            if (varTypeIsByte(srcType))
            {
                ins = INS_uxtb;
            }
            else if (varTypeIsShort(srcType))
            {
                ins = INS_uxth;
            }
            else
            {
                // A mov Rd, Rm instruction performs the zero extend
                // for the upper 32 bits when the size is EA_4BYTE

                ins = INS_mov;
            }
        }
        else
        {
            if (varTypeIsByte(srcType))
            {
                ins = INS_sxtb;
            }
            else if (varTypeIsShort(srcType))
            {
                ins = INS_sxth;
            }
            else
            {
                if (srcType == TYP_INT)
                {
                    ins = INS_sxtw;
                }
                else
                {
                    ins = INS_mov;
                }
            }
        }
    }
    else
    {
        ins = ins_Load(srcType);
    }
#else
    NYI("ins_Move_Extend");
#endif
    assert(ins != INS_invalid);
    return ins;
}

/*****************************************************************************
 *
 *  Get the machine dependent instruction for performing a load for srcType
 *
 *  Parameters
 *      srcType   - source type
 *      aligned   - whether source is 16-byte aligned if srcType is a SIMD type
 */
instruction CodeGenInterface::ins_Load(var_types srcType, bool aligned /*=false*/)
{
    instruction ins = INS_invalid;

    if (varTypeIsSIMD(srcType))
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
#ifdef FEATURE_SIMD
        if (srcType == TYP_SIMD8)
        {
            return INS_movsdsse2;
        }
        else
#endif // FEATURE_SIMD
            if (compiler->canUseAVX())
        {
            // TODO-CQ: consider alignment of AVX vectors.
            return INS_movupd;
        }
        else
        {
            // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
            // former doesn't require 66h prefix and one byte smaller than the
            // latter.
            return (aligned) ? INS_movaps : INS_movups;
        }
#else
        assert(!"ins_Load with SIMD type");
#endif
    }

    if (varTypeIsFloating(srcType))
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        if (srcType == TYP_DOUBLE)
        {
            return INS_movsdsse2;
        }
        else if (srcType == TYP_FLOAT)
        {
            return INS_movss;
        }
        else
        {
            assert(!"unhandled floating type");
        }
#elif defined(_TARGET_ARM64_)
        return INS_ldr;
#elif defined(_TARGET_ARM_)
        return INS_vldr;
#else
        assert(!varTypeIsFloating(srcType));
#endif
    }

#if defined(_TARGET_XARCH_)
    if (!varTypeIsSmall(srcType))
    {
        ins = INS_mov;
    }
    else if (varTypeIsUnsigned(srcType))
    {
        ins = INS_movzx;
    }
    else
    {
        ins = INS_movsx;
    }

#elif defined(_TARGET_ARMARCH_)
    if (!varTypeIsSmall(srcType))
    {
#if defined(_TARGET_ARM64_)
        if (!varTypeIsI(srcType) && !varTypeIsUnsigned(srcType))
        {
            ins = INS_ldrsw;
        }
        else
#endif // defined(_TARGET_ARM64_)
        {
            ins = INS_ldr;
        }
    }
    else if (varTypeIsByte(srcType))
    {
        if (varTypeIsUnsigned(srcType))
            ins = INS_ldrb;
        else
            ins = INS_ldrsb;
    }
    else if (varTypeIsShort(srcType))
    {
        if (varTypeIsUnsigned(srcType))
            ins = INS_ldrh;
        else
            ins = INS_ldrsh;
    }
#else
    NYI("ins_Load");
#endif

    assert(ins != INS_invalid);
    return ins;
}

/*****************************************************************************
 *
 *  Get the machine dependent instruction for performing a reg-reg copy for dstType
 *
 *  Parameters
 *      dstType   - destination type
 */
instruction CodeGen::ins_Copy(var_types dstType)
{
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    if (varTypeIsSIMD(dstType))
    {
        return INS_movaps;
    }
    else if (varTypeIsFloating(dstType))
    {
        // Both float and double copy can use movaps
        return INS_movaps;
    }
    else
    {
        return INS_mov;
    }
#elif defined(_TARGET_ARM64_)
    if (varTypeIsFloating(dstType))
    {
        return INS_fmov;
    }
    else
    {
        return INS_mov;
    }
#elif defined(_TARGET_ARM_)
    assert(!varTypeIsSIMD(dstType));
    if (varTypeIsFloating(dstType))
    {
        return INS_vmov;
    }
    else
    {
        return INS_mov;
    }
#elif defined(_TARGET_X86_)
    assert(!varTypeIsSIMD(dstType));
    assert(!varTypeIsFloating(dstType));
    return INS_mov;
#else // _TARGET_*
#error "Unknown _TARGET_"
#endif
}

/*****************************************************************************
 *
 *  Get the machine dependent instruction for performing a store for dstType
 *
 *  Parameters
 *      dstType   - destination type
 *      aligned   - whether destination is 16-byte aligned if dstType is a SIMD type
 */
instruction CodeGenInterface::ins_Store(var_types dstType, bool aligned /*=false*/)
{
    instruction ins = INS_invalid;

#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    if (varTypeIsSIMD(dstType))
    {
#ifdef FEATURE_SIMD
        if (dstType == TYP_SIMD8)
        {
            return INS_movsdsse2;
        }
        else
#endif // FEATURE_SIMD
            if (compiler->canUseAVX())
        {
            // TODO-CQ: consider alignment of AVX vectors.
            return INS_movupd;
        }
        else
        {
            // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
            // former doesn't require 66h prefix and one byte smaller than the
            // latter.
            return (aligned) ? INS_movaps : INS_movups;
        }
    }
    else if (varTypeIsFloating(dstType))
    {
        if (dstType == TYP_DOUBLE)
        {
            return INS_movsdsse2;
        }
        else if (dstType == TYP_FLOAT)
        {
            return INS_movss;
        }
        else
        {
            assert(!"unhandled floating type");
        }
    }
#elif defined(_TARGET_ARM64_)
    if (varTypeIsSIMD(dstType) || varTypeIsFloating(dstType))
    {
        // All sizes of SIMD and FP instructions use INS_str
        return INS_str;
    }
#elif defined(_TARGET_ARM_)
    assert(!varTypeIsSIMD(dstType));
    if (varTypeIsFloating(dstType))
    {
        return INS_vstr;
    }
#else
    assert(!varTypeIsSIMD(dstType));
    assert(!varTypeIsFloating(dstType));
#endif

#if defined(_TARGET_XARCH_)
    ins = INS_mov;
#elif defined(_TARGET_ARMARCH_)
    if (!varTypeIsSmall(dstType))
        ins = INS_str;
    else if (varTypeIsByte(dstType))
        ins = INS_strb;
    else if (varTypeIsShort(dstType))
        ins = INS_strh;
#else
    NYI("ins_Store");
#endif

    assert(ins != INS_invalid);
    return ins;
}

#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)

bool CodeGen::isMoveIns(instruction ins)
{
    return (ins == INS_mov);
}

instruction CodeGenInterface::ins_FloatLoad(var_types type)
{
    // Do Not use this routine in RyuJIT backend. Instead use ins_Load()/ins_Store()
    unreached();
}

// everything is just an addressing mode variation on x64
instruction CodeGen::ins_FloatStore(var_types type)
{
    // Do Not use this routine in RyuJIT backend. Instead use ins_Store()
    unreached();
}

instruction CodeGen::ins_FloatCopy(var_types type)
{
    // Do Not use this routine in RyuJIT backend. Instead use ins_Load().
    unreached();
}

instruction CodeGen::ins_FloatCompare(var_types type)
{
    return (type == TYP_FLOAT) ? INS_ucomiss : INS_ucomisd;
}

instruction CodeGen::ins_CopyIntToFloat(var_types srcType, var_types dstType)
{
    // On SSE2/AVX - the same instruction is used for moving double/quad word to XMM/YMM register.
    assert((srcType == TYP_INT) || (srcType == TYP_UINT) || (srcType == TYP_LONG) || (srcType == TYP_ULONG));
    return INS_mov_i2xmm;
}

instruction CodeGen::ins_CopyFloatToInt(var_types srcType, var_types dstType)
{
    // On SSE2/AVX - the same instruction is used for moving double/quad word of XMM/YMM to an integer register.
    assert((dstType == TYP_INT) || (dstType == TYP_UINT) || (dstType == TYP_LONG) || (dstType == TYP_ULONG));
    return INS_mov_xmm2i;
}

instruction CodeGen::ins_MathOp(genTreeOps oper, var_types type)
{
    switch (oper)
    {
        case GT_ADD:
        case GT_ASG_ADD:
            return type == TYP_DOUBLE ? INS_addsd : INS_addss;
            break;
        case GT_SUB:
        case GT_ASG_SUB:
            return type == TYP_DOUBLE ? INS_subsd : INS_subss;
            break;
        case GT_MUL:
        case GT_ASG_MUL:
            return type == TYP_DOUBLE ? INS_mulsd : INS_mulss;
            break;
        case GT_DIV:
        case GT_ASG_DIV:
            return type == TYP_DOUBLE ? INS_divsd : INS_divss;
        case GT_AND:
            return type == TYP_DOUBLE ? INS_andpd : INS_andps;
        case GT_OR:
            return type == TYP_DOUBLE ? INS_orpd : INS_orps;
        case GT_XOR:
            return type == TYP_DOUBLE ? INS_xorpd : INS_xorps;
        default:
            unreached();
    }
}

instruction CodeGen::ins_FloatSqrt(var_types type)
{
    instruction ins = INS_invalid;

    if (type == TYP_DOUBLE)
    {
        ins = INS_sqrtsd;
    }
    else
    {
        // Right now sqrt of scalar single is not needed.
        unreached();
    }

    return ins;
}

// Conversions to or from floating point values
instruction CodeGen::ins_FloatConv(var_types to, var_types from)
{
    // AVX: For now we support only conversion from Int/Long -> float

    switch (from)
    {
        // int/long -> float/double use the same instruction but type size would be different.
        case TYP_INT:
        case TYP_LONG:
            switch (to)
            {
                case TYP_FLOAT:
                    return INS_cvtsi2ss;
                case TYP_DOUBLE:
                    return INS_cvtsi2sd;
                default:
                    unreached();
            }
            break;

        case TYP_FLOAT:
            switch (to)
            {
                case TYP_INT:
                    return INS_cvttss2si;
                case TYP_LONG:
                    return INS_cvttss2si;
                case TYP_FLOAT:
                    return ins_Move_Extend(TYP_FLOAT, false);
                case TYP_DOUBLE:
                    return INS_cvtss2sd;
                default:
                    unreached();
            }
            break;

        case TYP_DOUBLE:
            switch (to)
            {
                case TYP_INT:
                    return INS_cvttsd2si;
                case TYP_LONG:
                    return INS_cvttsd2si;
                case TYP_FLOAT:
                    return INS_cvtsd2ss;
                case TYP_DOUBLE:
                    return ins_Move_Extend(TYP_DOUBLE, false);
                default:
                    unreached();
            }
            break;

        default:
            unreached();
    }
}

#elif defined(_TARGET_ARM_)

bool CodeGen::isMoveIns(instruction ins)
{
    return (ins == INS_vmov) || (ins == INS_mov);
}

instruction CodeGenInterface::ins_FloatLoad(var_types type)
{
    assert(type == TYP_DOUBLE || type == TYP_FLOAT);
    return INS_vldr;
}
instruction CodeGen::ins_FloatStore(var_types type)
{
    assert(type == TYP_DOUBLE || type == TYP_FLOAT);
    return INS_vstr;
}
instruction CodeGen::ins_FloatCopy(var_types type)
{
    assert(type == TYP_DOUBLE || type == TYP_FLOAT);
    return INS_vmov;
}

instruction CodeGen::ins_CopyIntToFloat(var_types srcType, var_types dstType)
{
    // Not used and not implemented
    unreached();
}

instruction CodeGen::ins_CopyFloatToInt(var_types srcType, var_types dstType)
{
    // Not used and not implemented
    unreached();
}

instruction CodeGen::ins_FloatCompare(var_types type)
{
    // Not used and not implemented
    unreached();
}

instruction CodeGen::ins_FloatSqrt(var_types type)
{
    // Not used and not implemented
    unreached();
}

instruction CodeGen::ins_MathOp(genTreeOps oper, var_types type)
{
    switch (oper)
    {
        case GT_ADD:
        case GT_ASG_ADD:
            return INS_vadd;
            break;
        case GT_SUB:
        case GT_ASG_SUB:
            return INS_vsub;
            break;
        case GT_MUL:
        case GT_ASG_MUL:
            return INS_vmul;
            break;
        case GT_DIV:
        case GT_ASG_DIV:
            return INS_vdiv;
        case GT_NEG:
            return INS_vneg;
        default:
            unreached();
    }
}

instruction CodeGen::ins_FloatConv(var_types to, var_types from)
{
    switch (from)
    {
        case TYP_INT:
            switch (to)
            {
                case TYP_FLOAT:
                    return INS_vcvt_i2f;
                case TYP_DOUBLE:
                    return INS_vcvt_i2d;
                default:
                    unreached();
            }
            break;
        case TYP_UINT:
            switch (to)
            {
                case TYP_FLOAT:
                    return INS_vcvt_u2f;
                case TYP_DOUBLE:
                    return INS_vcvt_u2d;
                default:
                    unreached();
            }
            break;
        case TYP_LONG:
            switch (to)
            {
                case TYP_FLOAT:
                    NYI("long to float");
                case TYP_DOUBLE:
                    NYI("long to double");
                default:
                    unreached();
            }
            break;
        case TYP_FLOAT:
            switch (to)
            {
                case TYP_INT:
                    return INS_vcvt_f2i;
                case TYP_UINT:
                    return INS_vcvt_f2u;
                case TYP_LONG:
                    NYI("float to long");
                case TYP_DOUBLE:
                    return INS_vcvt_f2d;
                case TYP_FLOAT:
                    return INS_vmov;
                default:
                    unreached();
            }
            break;
        case TYP_DOUBLE:
            switch (to)
            {
                case TYP_INT:
                    return INS_vcvt_d2i;
                case TYP_UINT:
                    return INS_vcvt_d2u;
                case TYP_LONG:
                    NYI("double to long");
                case TYP_FLOAT:
                    return INS_vcvt_d2f;
                case TYP_DOUBLE:
                    return INS_vmov;
                default:
                    unreached();
            }
            break;
        default:
            unreached();
    }
}

#endif // #elif defined(_TARGET_ARM_)

/*****************************************************************************
 *
 *  Machine independent way to return
 */
void CodeGen::instGen_Return(unsigned stkArgSize)
{
#if defined(_TARGET_XARCH_)
    if (stkArgSize == 0)
    {
        instGen(INS_ret);
    }
    else
    {
        inst_IV(INS_ret, stkArgSize);
    }
#elif defined(_TARGET_ARM_)
//
// The return on ARM is folded into the pop multiple instruction
// and as we do not know the exact set of registers that we will
// need to restore (pop) when we first call instGen_Return we will
// instead just not emit anything for this method on the ARM
// The return will be part of the pop multiple and that will be
// part of the epilog that is generated by genFnEpilog()
#elif defined(_TARGET_ARM64_)
    // This function shouldn't be used on ARM64.
    unreached();
#else
    NYI("instGen_Return");
#endif
}

/*****************************************************************************
 *
 *  Emit a MemoryBarrier instruction
 *
 *     Note: all MemoryBarriers instructions can be removed by
 *           SET COMPlus_JitNoMemoryBarriers=1
 */
void CodeGen::instGen_MemoryBarrier()
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

#if defined(_TARGET_XARCH_)
    instGen(INS_lock);
    getEmitter()->emitIns_I_AR(INS_or, EA_4BYTE, 0, REG_SPBASE, 0);
#elif defined(_TARGET_ARM_)
    getEmitter()->emitIns_I(INS_dmb, EA_4BYTE, 0xf);
#elif defined(_TARGET_ARM64_)
    getEmitter()->emitIns_BARR(INS_dmb, INS_BARRIER_SY);
#else
#error "Unknown _TARGET_"
#endif
}

/*****************************************************************************
 *
 *  Machine independent way to move a Zero value into a register
 */
void CodeGen::instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags)
{
#if defined(_TARGET_XARCH_)
    getEmitter()->emitIns_R_R(INS_xor, size, reg, reg);
#elif defined(_TARGET_ARMARCH_)
    getEmitter()->emitIns_R_I(INS_mov, size, reg, 0 ARM_ARG(flags));
#else
#error "Unknown _TARGET_"
#endif
    regTracker.rsTrackRegIntCns(reg, 0);
}

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Machine independent way to move an immediate value into a register
 */
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags)
{
#if RELOC_SUPPORT
    if (!compiler->opts.compReloc)
#endif // RELOC_SUPPORT
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs
    }

    if ((imm == 0) && !EA_IS_RELOC(size))
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
#if defined(_TARGET_XARCH_)
        getEmitter()->emitIns_R_I(INS_mov, size, reg, imm);
#elif defined(_TARGET_ARM_)

        if (EA_IS_RELOC(size))
        {
            getEmitter()->emitIns_R_I(INS_movw, size, reg, imm);
            getEmitter()->emitIns_R_I(INS_movt, size, reg, imm);
        }
        else if (arm_Valid_Imm_For_Mov(imm))
        {
            getEmitter()->emitIns_R_I(INS_mov, size, reg, imm, flags);
        }
        else // We have to use a movw/movt pair of instructions
        {
            ssize_t imm_lo16 = (imm & 0xffff);
            ssize_t imm_hi16 = (imm >> 16) & 0xffff;

            assert(arm_Valid_Imm_For_Mov(imm_lo16));
            assert(imm_hi16 != 0);

            getEmitter()->emitIns_R_I(INS_movw, size, reg, imm_lo16);

            // If we've got a low register, the high word is all bits set,
            // and the high bit of the low word is set, we can sign extend
            // halfword and save two bytes of encoding. This can happen for
            // small magnitude negative numbers 'n' for -32768 <= n <= -1.

            if (getEmitter()->isLowRegister(reg) && (imm_hi16 == 0xffff) && ((imm_lo16 & 0x8000) == 0x8000))
            {
                getEmitter()->emitIns_R_R(INS_sxth, EA_2BYTE, reg, reg);
            }
            else
            {
                getEmitter()->emitIns_R_I(INS_movt, size, reg, imm_hi16);
            }

            if (flags == INS_FLAGS_SET)
                getEmitter()->emitIns_R_R(INS_mov, size, reg, reg, INS_FLAGS_SET);
        }
#elif defined(_TARGET_ARM64_)
        NYI_ARM64("instGen_Set_Reg_To_Imm");
#else
#error "Unknown _TARGET_"
#endif
    }
    regTracker.rsTrackRegIntCns(reg, imm);
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Machine independent way to set the flags based on
 *   comparing a register with zero
 */
void CodeGen::instGen_Compare_Reg_To_Zero(emitAttr size, regNumber reg)
{
#if defined(_TARGET_XARCH_)
    getEmitter()->emitIns_R_R(INS_test, size, reg, reg);
#elif defined(_TARGET_ARMARCH_)
    getEmitter()->emitIns_R_I(INS_cmp, size, reg, 0);
#else
#error "Unknown _TARGET_"
#endif
}

/*****************************************************************************
 *
 *  Machine independent way to set the flags based upon
 *   comparing a register with another register
 */
void CodeGen::instGen_Compare_Reg_To_Reg(emitAttr size, regNumber reg1, regNumber reg2)
{
#if defined(_TARGET_XARCH_) || defined(_TARGET_ARMARCH_)
    getEmitter()->emitIns_R_R(INS_cmp, size, reg1, reg2);
#else
#error "Unknown _TARGET_"
#endif
}

/*****************************************************************************
 *
 *  Machine independent way to set the flags based upon
 *   comparing a register with an immediate
 */
void CodeGen::instGen_Compare_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm)
{
    if (imm == 0)
    {
        instGen_Compare_Reg_To_Zero(size, reg);
    }
    else
    {
#if defined(_TARGET_XARCH_)
#if defined(_TARGET_AMD64_)
        if ((EA_SIZE(size) == EA_8BYTE) && (((int)imm != (ssize_t)imm) || EA_IS_CNS_RELOC(size)))
        {
#ifndef LEGACY_BACKEND
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
#else  // LEGACY_BACKEND
            // Load imm into a register
            regNumber immReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
            instGen_Set_Reg_To_Imm(size, immReg, (ssize_t)imm);
            getEmitter()->emitIns_R_R(INS_cmp, EA_TYPE(size), reg, immReg);
#endif // LEGACY_BACKEND
        }
        else
#endif // _TARGET_AMD64_
        {
            getEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
#elif defined(_TARGET_ARM_)
        if (arm_Valid_Imm_For_Alu(imm) || arm_Valid_Imm_For_Alu(-imm))
        {
            getEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
        else // We need a scratch register
        {
#ifndef LEGACY_BACKEND
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
#else  // LEGACY_BACKEND
            // Load imm into a register
            regNumber immReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
            instGen_Set_Reg_To_Imm(size, immReg, (ssize_t)imm);
            getEmitter()->emitIns_R_R(INS_cmp, size, reg, immReg);
#endif // !LEGACY_BACKEND
        }
#elif defined(_TARGET_ARM64_)
        if (true) // TODO-ARM64-NYI: arm_Valid_Imm_For_Alu(imm) || arm_Valid_Imm_For_Alu(-imm))
        {
            getEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
        else // We need a scratch register
        {
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
        }
#else
#error "Unknown _TARGET_"
#endif
    }
}

/*****************************************************************************
 *
 *  Machine independent way to move a stack based local variable into a register
 */
void CodeGen::instGen_Load_Reg_From_Lcl(var_types srcType, regNumber dstReg, int varNum, int offs)
{
    emitAttr size = emitTypeSize(srcType);

    getEmitter()->emitIns_R_S(ins_Load(srcType), size, dstReg, varNum, offs);
}

/*****************************************************************************
 *
 *  Machine independent way to move a register into a stack based local variable
 */
void CodeGen::instGen_Store_Reg_Into_Lcl(var_types dstType, regNumber srcReg, int varNum, int offs)
{
    emitAttr size = emitTypeSize(dstType);

    getEmitter()->emitIns_S_R(ins_Store(dstType), size, srcReg, varNum, offs);
}

/*****************************************************************************
 *
 *  Machine independent way to move an immediate into a stack based local variable
 */
void CodeGen::instGen_Store_Imm_Into_Lcl(
    var_types dstType, emitAttr sizeAttr, ssize_t imm, int varNum, int offs, regNumber regToUse)
{
#ifdef _TARGET_XARCH_
#ifdef _TARGET_AMD64_
    if ((EA_SIZE(sizeAttr) == EA_8BYTE) && (((int)imm != (ssize_t)imm) || EA_IS_CNS_RELOC(sizeAttr)))
    {
        assert(!"Invalid immediate for instGen_Store_Imm_Into_Lcl");
    }
    else
#endif // _TARGET_AMD64_
    {
        getEmitter()->emitIns_S_I(ins_Store(dstType), sizeAttr, varNum, offs, (int)imm);
    }
#elif defined(_TARGET_ARMARCH_)
    // Load imm into a register
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef LEGACY_BACKEND
    regNumber immReg = regToUse;
    assert(regToUse != REG_NA);
#else  // LEGACY_BACKEND
    regNumber immReg = (regToUse == REG_NA) ? regSet.rsGrabReg(RBM_ALLINT) : regToUse;
#endif // LEGACY_BACKEND
    instGen_Set_Reg_To_Imm(sizeAttr, immReg, (ssize_t)imm);
    instGen_Store_Reg_Into_Lcl(dstType, immReg, varNum, offs);
    if (EA_IS_RELOC(sizeAttr))
    {
        regTracker.rsTrackRegTrash(immReg);
    }
#else  // _TARGET_*
#error "Unknown _TARGET_"
#endif // _TARGET_*
}

/*****************************************************************************/
/*****************************************************************************/
/*****************************************************************************/
