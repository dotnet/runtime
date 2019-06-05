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
        #define INST0(id, nm, um, mr,                 flags) nm,
        #define INST1(id, nm, um, mr,                 flags) nm,
        #define INST2(id, nm, um, mr, mi,             flags) nm,
        #define INST3(id, nm, um, mr, mi, rm,         flags) nm,
        #define INST4(id, nm, um, mr, mi, rm, a4,     flags) nm,
        #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) nm,
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

    assert((unsigned)ins < _countof(insNames));
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
    assert((unsigned)ins < _countof(instInfo));

#ifdef _TARGET_XARCH_
    return (instInfo[ins] & INS_FLAGS_x87Instr) != 0;
#else
    return (instInfo[ins] & INST_FP) != 0;
#endif
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
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef UNIX_X86_ABI
    // bbTgtStkDepth is a (pure) argument count (stack alignment padding should be excluded).
    assert((tgtBlock->bbTgtStkDepth * sizeof(int) == (genStackLevel - curNestedAlignment)) || isFramePointerUsed());
#else
    assert((tgtBlock->bbTgtStkDepth * sizeof(int) == genStackLevel) || isFramePointerUsed());
#endif
#endif // !FEATURE_FIXED_OUT_ARGS

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

        case EJ_jp:
            ins = INS_setp;
            break;
        case EJ_jnp:
            ins = INS_setnp;
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
#elif defined(_TARGET_XARCH_)
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

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void CodeGen::inst_set_SV_var(GenTree* tree)
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
    instruction ins, regNumber reg, target_ssize_t val, emitAttr size, insFlags flags /* = INS_FLAGS_DONT_CARE */)
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
        // TODO-Cleanup: Add a comment about why this is unreached() for RyuJIT backend.
        unreached();
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
        assert(!"Invalid immediate for inst_RV_IV");
    }
    else
#endif // _TARGET_AMD64_
    {
        getEmitter()->emitIns_R_I(ins, size, reg, val);
    }
#endif // !_TARGET_ARM_
}

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a tree (which has
 *  been made addressable).
 */

void CodeGen::inst_TT(instruction ins, GenTree* tree, unsigned offs, int shfv, emitAttr size)
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

    /* Is this a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        assert(!"ISSUE: If this can happen, we need to generate 'ins [ebp+spill]'");
    }

    switch (tree->gtOper)
    {
        unsigned varNum;

        case GT_LCL_VAR:

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
            assert(!"inst_TT not supported for GT_IND, GT_NULLCHECK or GT_ARR_ELEM");
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

void CodeGen::inst_TT_RV(instruction ins, GenTree* tree, regNumber reg, unsigned offs, emitAttr size, insFlags flags)
{
    assert(reg != REG_STK);

AGAIN:

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
                // TODO-LdStArch-Bug: Should regTmp be a dst on the node or an internal reg?
                // Either way, it is not currently being handled by Lowering.
                regNumber regTmp = tree->gtRegNum;
                assert(regTmp != REG_NA);
                getEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                getEmitter()->emitIns_R_R(ins, size, regTmp, reg, flags);
                getEmitter()->emitIns_S_R(ins_Store(tree->TypeGet()), size, regTmp, varNum, offs);

                regSet.verifyRegUsed(regTmp);
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
                NYI("Store of GT_CLS_VAR not supported for ARM");
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
            assert(!"inst_TT_RV not supported for GT_IND, GT_NULLCHECK or GT_ARR_ELEM");
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

/*****************************************************************************
 *
 *  Generate an instruction that has one operand given by a register and the
 *  other one by a tree (which has been made addressable).
 */

void CodeGen::inst_RV_TT(instruction ins,
                         regNumber   reg,
                         GenTree*    tree,
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
#if defined(_TARGET_ARM64_) || defined(_TARGET_ARM64_)
        ins = ins_Move_Extend(tree->TypeGet(), false);
#else
        NYI("CodeGen::inst_RV_TT with INS_mov");
#endif
    }
#endif // CPU_LOAD_STORE_ARCH

AGAIN:

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
                    regTmp = tree->gtRegNum;

                    getEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                    getEmitter()->emitIns_R_R(ins, size, reg, regTmp, flags);

                    regSet.verifyRegUsed(regTmp);
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
            assert(!"GT_CLS_VAR not supported in ARM backend");
#else  // CPU_LOAD_STORE_ARCH
            getEmitter()->emitIns_R_C(ins, size, reg, tree->gtClsVar.gtClsVarHnd, offs);
#endif // CPU_LOAD_STORE_ARCH
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        case GT_LEA:
        {
            assert(!"inst_RV_TT not supported for GT_IND, GT_NULLCHECK, GT_ARR_ELEM or GT_LEA");
        }
        break;

        case GT_CNS_INT:

            assert(offs == 0);

            // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t type.
            inst_RV_IV(ins, reg, (target_ssize_t)tree->gtIntCon.gtIconVal, emitActualTypeSize(tree->TypeGet()), flags);
            break;

        case GT_CNS_LNG:

            assert(size == EA_4BYTE || size == EA_8BYTE);

#ifdef _TARGET_AMD64_
            assert(offs == 0);
#endif // _TARGET_AMD64_

            target_ssize_t constVal;
            emitAttr       size;
            if (offs == 0)
            {
                constVal = (target_ssize_t)(tree->gtLngCon.gtLconVal);
                size     = EA_PTRSIZE;
            }
            else
            {
                constVal = (target_ssize_t)(tree->gtLngCon.gtLconVal >> 32);
                size     = EA_4BYTE;
            }

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

void CodeGen::inst_TT_SH(instruction ins, GenTree* tree, unsigned val, unsigned offs)
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

void CodeGen::inst_TT_CL(instruction ins, GenTree* tree, unsigned offs)
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
    assert(ins == INS_shld || ins == INS_shrd || ins == INS_shufps || ins == INS_shufpd || ins == INS_pshufd ||
           ins == INS_cmpps || ins == INS_cmppd || ins == INS_dppd || ins == INS_dpps || ins == INS_insertps ||
           ins == INS_roundps || ins == INS_roundss || ins == INS_roundpd || ins == INS_roundsd);

    getEmitter()->emitIns_R_R_I(ins, size, reg1, reg2, ival);
}

#ifdef FEATURE_HW_INTRINSICS
//------------------------------------------------------------------------
// inst_RV_TT_IV: Generates an instruction that takes 3 operands:
//                a register operand, an operand that may be memory or register and an immediate
//                and that returns a value in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    reg1      -- The first operand, a register
//    rmOp      -- The second operand, which may be a memory node or a node producing a register
//    ival      -- The immediate operand
//
// Notes:
//    This isn't really specific to HW intrinsics, but depends on other methods that are
//    only defined for FEATURE_HW_INTRINSICS, and is currently only used in that context.
//
void CodeGen::inst_RV_TT_IV(instruction ins, emitAttr attr, regNumber reg1, GenTree* rmOp, int ival)
{
    noway_assert(getEmitter()->emitVerifyEncodable(ins, EA_SIZE(attr), reg1));

    if (rmOp->isContained() || rmOp->isUsedFromSpillTemp())
    {
        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (rmOp->isUsedFromSpillTemp())
        {
            assert(rmOp->IsRegOptional());

            tmpDsc = getSpillTempDsc(rmOp);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (rmOp->isIndir() || rmOp->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;

            if (rmOp->isIndir())
            {
                memIndir = rmOp->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
                assert(rmOp->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(rmOp->AsHWIntrinsic()) == 1);
                addr = rmOp->gtGetOp1();
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                {
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = 0;
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    getEmitter()->emitIns_R_C_I(ins, attr, reg1, addr->gtClsVar.gtClsVarHnd, 0, ival);
                    return;
                }

                default:
                {
                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        GenTreeIndir load = indirForm(rmOp->TypeGet(), addr);
                        memIndir          = &load;
                    }
                    getEmitter()->emitIns_R_A_I(ins, attr, reg1, memIndir, ival);
                    return;
                }
            }
        }
        else
        {
            switch (rmOp->OperGet())
            {
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclField = rmOp->AsLclFld();

                    varNum = lclField->GetLclNum();
                    offset = lclField->gtLclFld.gtLclOffs;
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(rmOp->IsRegOptional() || !compiler->lvaGetDesc(rmOp->gtLclVar.gtLclNum)->lvIsRegCandidate());
                    varNum = rmOp->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                default:
                    unreached();
                    break;
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        getEmitter()->emitIns_R_S_I(ins, attr, reg1, varNum, offset, ival);
    }
    else
    {
        regNumber rmOpReg = rmOp->gtRegNum;
        getEmitter()->emitIns_SIMD_R_R_I(ins, attr, reg1, rmOpReg, ival);
    }
}
#endif // FEATURE_HW_INTRINSICS

#endif // _TARGET_XARCH_

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

void CodeGen::inst_RV_ST(instruction ins, emitAttr size, regNumber reg, GenTree* tree)
{
    assert(size == EA_1BYTE || size == EA_2BYTE);

    inst_RV_TT(ins, reg, tree, 0, size);
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
            assert(!"Default inst_RV_ST case not supported for Arm");
            break;
    }
#else  // !_TARGET_ARM_
    getEmitter()->emitIns_R_S(ins, size, reg, tmp->tdTempNum(), ofs);
#endif // !_TARGET_ARM_
}

void CodeGen::inst_mov_RV_ST(regNumber reg, GenTree* tree)
{
    /* Figure out the size of the value being loaded */

    emitAttr    size    = EA_ATTR(genTypeSize(tree->gtType));
    instruction loadIns = ins_Move_Extend(tree->TypeGet(), false);

    if (size < EA_4BYTE)
    {
        /* Generate the "movsx/movzx" opcode */

        inst_RV_ST(loadIns, size, reg, tree);
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
bool CodeGenInterface::validImmForInstr(instruction ins, target_ssize_t imm, insFlags flags)
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
bool CodeGen::arm_Valid_Imm_For_Instr(instruction ins, target_ssize_t imm, insFlags flags)
{
    return validImmForInstr(ins, imm, flags);
}

bool CodeGenInterface::validDispForLdSt(target_ssize_t disp, var_types type)
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
bool CodeGen::arm_Valid_Disp_For_LdSt(target_ssize_t disp, var_types type)
{
    return validDispForLdSt(disp, type);
}

bool CodeGenInterface::validImmForAlu(target_ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_alu(imm);
}
bool CodeGen::arm_Valid_Imm_For_Alu(target_ssize_t imm)
{
    return validImmForAlu(imm);
}

bool CodeGenInterface::validImmForMov(target_ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_mov(imm);
}
bool CodeGen::arm_Valid_Imm_For_Mov(target_ssize_t imm)
{
    return validImmForMov(imm);
}

bool CodeGen::arm_Valid_Imm_For_Small_Mov(regNumber reg, target_ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_small_mov(reg, imm, flags);
}

bool CodeGenInterface::validImmForAdd(target_ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_add(imm, flags);
}
bool CodeGen::arm_Valid_Imm_For_Add(target_ssize_t imm, insFlags flags)
{
    return emitter::emitIns_valid_imm_for_add(imm, flags);
}

// Check "add Rd,SP,i10"
bool CodeGen::arm_Valid_Imm_For_Add_SP(target_ssize_t imm)
{
    return emitter::emitIns_valid_imm_for_add_sp(imm);
}

bool CodeGenInterface::validImmForBL(ssize_t addr)
{
    return
        // If we are running the altjit for NGEN, then assume we can use the "BL" instruction.
        // This matches the usual behavior for NGEN, since we normally do generate "BL".
        (!compiler->info.compMatchedVM && compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT)) ||
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

#if defined(_TARGET_ARM64_)
bool CodeGenInterface::validImmForBL(ssize_t addr)
{
    // On arm64, we always assume a call target is in range and generate a 28-bit relative
    // 'bl' instruction. If this isn't sufficient range, the VM will generate a jump stub when
    // we call recordRelocation(). See the IMAGE_REL_ARM64_BRANCH26 case in jitinterface.cpp
    // (for JIT) or zapinfo.cpp (for NGEN). If we cannot allocate a jump stub, it is fatal.
    return true;
}
#endif // _TARGET_ARM64_

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
#if defined(_TARGET_XARCH_)
        // SSE2/AVX requires destination to be a reg always.
        // If src is in reg means, it is a reg-reg move.
        //
        // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
        // former doesn't require 66h prefix and one byte smaller than the
        // latter.
        //
        // TODO-CQ: based on whether src type is aligned use movaps instead

        return (srcInReg) ? INS_movaps : INS_movups;
#elif defined(_TARGET_ARM64_)
        return (srcInReg) ? INS_mov : ins_Load(srcType);
#else  // !defined(_TARGET_ARM64_) && !defined(_TARGET_XARCH_)
        assert(!"unhandled SIMD type");
#endif // !defined(_TARGET_ARM64_) && !defined(_TARGET_XARCH_)
    }

#if defined(_TARGET_XARCH_)
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
 *      aligned   - whether source is properly aligned if srcType is a SIMD type
 */
instruction CodeGenInterface::ins_Load(var_types srcType, bool aligned /*=false*/)
{
    instruction ins = INS_invalid;

    if (varTypeIsSIMD(srcType))
    {
#if defined(_TARGET_XARCH_)
#ifdef FEATURE_SIMD
        if (srcType == TYP_SIMD8)
        {
            return INS_movsdsse2;
        }
        else
#endif // FEATURE_SIMD
            if (compiler->canUseVexEncoding())
        {
            return (aligned) ? INS_movapd : INS_movupd;
        }
        else
        {
            // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
            // former doesn't require 66h prefix and one byte smaller than the
            // latter.
            return (aligned) ? INS_movaps : INS_movups;
        }
#elif defined(_TARGET_ARM64_)
        return INS_ldr;
#else
        assert(!"ins_Load with SIMD type");
#endif
    }

    if (varTypeIsFloating(srcType))
    {
#if defined(_TARGET_XARCH_)
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
        ins = INS_ldr;
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
#if defined(_TARGET_XARCH_)
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
 *      aligned   - whether destination is properly aligned if dstType is a SIMD type
 */
instruction CodeGenInterface::ins_Store(var_types dstType, bool aligned /*=false*/)
{
    instruction ins = INS_invalid;

#if defined(_TARGET_XARCH_)
    if (varTypeIsSIMD(dstType))
    {
#ifdef FEATURE_SIMD
        if (dstType == TYP_SIMD8)
        {
            return INS_movsdsse2;
        }
        else
#endif // FEATURE_SIMD
            if (compiler->canUseVexEncoding())
        {
            return (aligned) ? INS_movapd : INS_movupd;
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

#if defined(_TARGET_XARCH_)

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

#if !defined(_TARGET_64BIT_)
    // No 64-bit registers on x86.
    assert((srcType != TYP_LONG) && (srcType != TYP_ULONG));
#endif // !defined(_TARGET_64BIT_)

    return INS_mov_i2xmm;
}

instruction CodeGen::ins_CopyFloatToInt(var_types srcType, var_types dstType)
{
    // On SSE2/AVX - the same instruction is used for moving double/quad word of XMM/YMM to an integer register.
    assert((dstType == TYP_INT) || (dstType == TYP_UINT) || (dstType == TYP_LONG) || (dstType == TYP_ULONG));

#if !defined(_TARGET_64BIT_)
    // No 64-bit registers on x86.
    assert((dstType != TYP_LONG) && (dstType != TYP_ULONG));
#endif // !defined(_TARGET_64BIT_)

    return INS_mov_xmm2i;
}

instruction CodeGen::ins_MathOp(genTreeOps oper, var_types type)
{
    switch (oper)
    {
        case GT_ADD:
            return type == TYP_DOUBLE ? INS_addsd : INS_addss;
        case GT_SUB:
            return type == TYP_DOUBLE ? INS_subsd : INS_subss;
        case GT_MUL:
            return type == TYP_DOUBLE ? INS_mulsd : INS_mulss;
        case GT_DIV:
            return type == TYP_DOUBLE ? INS_divsd : INS_divss;
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
    else if (type == TYP_FLOAT)
    {
        ins = INS_sqrtss;
    }
    else
    {
        assert(!"ins_FloatSqrt: Unsupported type");
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
    assert((dstType == TYP_FLOAT) || (dstType == TYP_DOUBLE));
    assert((srcType == TYP_INT) || (srcType == TYP_UINT) || (srcType == TYP_LONG) || (srcType == TYP_ULONG));

    if ((srcType == TYP_LONG) || (srcType == TYP_ULONG))
    {
        return INS_vmov_i2d;
    }
    else
    {
        return INS_vmov_i2f;
    }
}

instruction CodeGen::ins_CopyFloatToInt(var_types srcType, var_types dstType)
{
    assert((srcType == TYP_FLOAT) || (srcType == TYP_DOUBLE));
    assert((dstType == TYP_INT) || (dstType == TYP_UINT) || (dstType == TYP_LONG) || (dstType == TYP_ULONG));

    if ((dstType == TYP_LONG) || (dstType == TYP_ULONG))
    {
        return INS_vmov_d2i;
    }
    else
    {
        return INS_vmov_f2i;
    }
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
            return INS_vadd;
        case GT_SUB:
            return INS_vsub;
        case GT_MUL:
            return INS_vmul;
        case GT_DIV:
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
#ifdef _TARGET_ARM64_
void CodeGen::instGen_MemoryBarrier(insBarrier barrierType)
#else
void CodeGen::instGen_MemoryBarrier()
#endif
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
    getEmitter()->emitIns_BARR(INS_dmb, barrierType);
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
    regSet.verifyRegUsed(reg);
}

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
void CodeGen::instGen_Compare_Reg_To_Imm(emitAttr size, regNumber reg, target_ssize_t imm)
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
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
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
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
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

/*****************************************************************************/
/*****************************************************************************/
/*****************************************************************************/
