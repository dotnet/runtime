// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if defined(TARGET_XARCH)
        #define INST0(id, nm, um, mr,                 flags) nm,
        #define INST1(id, nm, um, mr,                 flags) nm,
        #define INST2(id, nm, um, mr, mi,             flags) nm,
        #define INST3(id, nm, um, mr, mi, rm,         flags) nm,
        #define INST4(id, nm, um, mr, mi, rm, a4,     flags) nm,
        #define INST5(id, nm, um, mr, mi, rm, a4, rr, flags) nm,
        #include "instrs.h"

#elif defined(TARGET_ARM)
        #define INST1(id, nm, fp, ldst, fmt, e1                                 ) nm,
        #define INST2(id, nm, fp, ldst, fmt, e1, e2                             ) nm,
        #define INST3(id, nm, fp, ldst, fmt, e1, e2, e3                         ) nm,
        #define INST4(id, nm, fp, ldst, fmt, e1, e2, e3, e4                     ) nm,
        #define INST5(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5                 ) nm,
        #define INST6(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6             ) nm,
        #define INST8(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8     ) nm,
        #define INST9(id, nm, fp, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9 ) nm,
        #include "instrs.h"

#elif defined(TARGET_ARM64)
        #define INST1(id, nm, ldst, fmt, e1                                 ) nm,
        #define INST2(id, nm, ldst, fmt, e1, e2                             ) nm,
        #define INST3(id, nm, ldst, fmt, e1, e2, e3                         ) nm,
        #define INST4(id, nm, ldst, fmt, e1, e2, e3, e4                     ) nm,
        #define INST5(id, nm, ldst, fmt, e1, e2, e3, e4, e5                 ) nm,
        #define INST6(id, nm, ldst, fmt, e1, e2, e3, e4, e5, e6             ) nm,
        #define INST9(id, nm, ldst, fmt, e1, e2, e3, e4, e5, e6, e7, e8, e9 ) nm,
        #include "instrs.h"

#else
#error "Unknown TARGET"
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

        //      printf("[%08X:%04X]", GetEmitter().emitCodeCurBlock(), GetEmitter().emitCodeOffsInBlock());

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

    GetEmitter()->emitIns(ins);

#ifdef TARGET_XARCH
#ifdef PSEUDORANDOM_NOP_INSERTION
    // A workaround necessitated by limitations of emitter
    // if we are scheduled to insert a nop here, we have to delay it
    // hopefully we have not missed any other prefix instructions or places
    // they could be inserted
    if (ins == INS_lock && GetEmitter()->emitNextNop == 0)
    {
        GetEmitter()->emitNextNop = 1;
    }
#endif // PSEUDORANDOM_NOP_INSERTION
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

#ifdef TARGET_XARCH
    return (instInfo[ins] & INS_FLAGS_x87Instr) != 0;
#else
    return (instInfo[ins] & INST_FP) != 0;
#endif
}

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

    GetEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmp), tgtBlock);
}

/*****************************************************************************
 *
 *  Generate a set instruction.
 */

void CodeGen::inst_SET(emitJumpKind condition, regNumber reg)
{
#ifdef TARGET_XARCH
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
    GetEmitter()->emitIns_R(ins, EA_1BYTE, reg);
#elif defined(TARGET_ARM64)
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
    GetEmitter()->emitIns_R_COND(INS_cset, EA_8BYTE, reg, cond);
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

    GetEmitter()->emitIns_R(ins, size, reg);
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

#ifdef TARGET_ARM
    GetEmitter()->emitIns_R_R(ins, size, reg1, reg2, flags);
#else
    GetEmitter()->emitIns_R_R(ins, size, reg1, reg2);
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
#ifdef TARGET_ARM
    GetEmitter()->emitIns_R_R_R(ins, size, reg1, reg2, reg3, flags);
#elif defined(TARGET_XARCH)
    GetEmitter()->emitIns_R_R_R(ins, size, reg1, reg2, reg3);
#else
    NYI("inst_RV_RV_RV");
#endif
}
/*****************************************************************************
 *
 *  Generate a "op icon" instruction.
 */

void CodeGen::inst_IV(instruction ins, cnsval_ssize_t val)
{
    GetEmitter()->emitIns_I(ins, EA_PTRSIZE, val);
}

/*****************************************************************************
 *
 *  Generate a "op icon" instruction where icon is a handle of type specified
 *  by 'flags'
 */

void CodeGen::inst_IV_handle(instruction ins, cnsval_ssize_t val)
{
    GetEmitter()->emitIns_I(ins, EA_HANDLE_CNS_RELOC, val);
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void CodeGen::inst_set_SV_var(GenTree* tree)
{
#ifdef DEBUG
    assert((tree != nullptr) && tree->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_STORE_LCL_VAR));
    assert(tree->AsLclVarCommon()->GetLclNum() < compiler->lvaCount);

    GetEmitter()->emitVarRefOffs = tree->AsLclVar()->gtLclILoffs;

#endif // DEBUG
}

/*****************************************************************************
 *
 *  Generate a "op reg, icon" instruction.
 */

void CodeGen::inst_RV_IV(
    instruction ins, regNumber reg, target_ssize_t val, emitAttr size, insFlags flags /* = INS_FLAGS_DONT_CARE */)
{
#if !defined(TARGET_64BIT)
    assert(size != EA_8BYTE);
#endif

#ifdef TARGET_ARM
    if (arm_Valid_Imm_For_Instr(ins, val, flags))
    {
        GetEmitter()->emitIns_R_I(ins, size, reg, val, flags);
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
#elif defined(TARGET_ARM64)
    // TODO-Arm64-Bug: handle large constants!
    // Probably need something like the ARM case above: if (arm_Valid_Imm_For_Instr(ins, val)) ...
    assert(ins != INS_cmp);
    assert(ins != INS_tst);
    assert(ins != INS_mov);
    GetEmitter()->emitIns_R_R_I(ins, size, reg, reg, val);
#else // !TARGET_ARM
#ifdef TARGET_AMD64
    // Instead of an 8-byte immediate load, a 4-byte immediate will do fine
    // as the high 4 bytes will be zero anyway.
    if (size == EA_8BYTE && ins == INS_mov && ((val & 0xFFFFFFFF00000000LL) == 0))
    {
        size = EA_4BYTE;
        GetEmitter()->emitIns_R_I(ins, size, reg, val);
    }
    else if (EA_SIZE(size) == EA_8BYTE && ins != INS_mov && (((int)val != val) || EA_IS_CNS_RELOC(size)))
    {
        assert(!"Invalid immediate for inst_RV_IV");
    }
    else
#endif // TARGET_AMD64
    {
        GetEmitter()->emitIns_R_I(ins, size, reg, val);
    }
#endif // !TARGET_ARM
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
            offs += tree->AsLclFld()->GetLclOffs();
            goto LCL;

        LCL:
            varNum = tree->AsLclVarCommon()->GetLclNum();
            assert(varNum < compiler->lvaCount);

            if (shfv)
            {
                GetEmitter()->emitIns_S_I(ins, size, varNum, offs, shfv);
            }
            else
            {
                GetEmitter()->emitIns_S(ins, size, varNum, offs);
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
                GetEmitter()->emitIns_C_I(ins, size, tree->AsClsVar()->gtClsVarHnd, offs, shfv);
            }
            else
            {
                GetEmitter()->emitIns_C(ins, size, tree->AsClsVar()->gtClsVarHnd, offs);
            }
            return;

        case GT_IND:
        case GT_NULLCHECK:
        case GT_ARR_ELEM:
        {
            assert(!"inst_TT not supported for GT_IND, GT_NULLCHECK or GT_ARR_ELEM");
        }
        break;

#ifdef TARGET_X86
        case GT_CNS_INT:
            // We will get here for GT_MKREFANY from CodeGen::genPushArgList
            assert(offs == 0);
            assert(!shfv);
            if (tree->IsIconHandle())
                inst_IV_handle(ins, tree->AsIntCon()->gtIconVal);
            else
                inst_IV(ins, tree->AsIntCon()->gtIconVal);
            break;
#endif

        case GT_COMMA:
            //     tree->AsOp()->gtOp1 - already processed by genCreateAddrMode()
            tree = tree->AsOp()->gtOp2;
            goto AGAIN;

        default:
            assert(!"invalid address");
    }
}

//------------------------------------------------------------------------
// inst_TT_RV: Generate a store of a lclVar
//
// Arguments:
//    ins  - the instruction to generate
//    size - the size attributes for the store
//    tree - the lclVar node
//    reg  - the register currently holding the value of the local
//
void CodeGen::inst_TT_RV(instruction ins, emitAttr size, GenTree* tree, regNumber reg)
{
#ifdef DEBUG
    // The tree must have a valid register value.
    assert(reg != REG_STK);

    bool isValidInReg = ((tree->gtFlags & GTF_SPILLED) == 0);
    if (!isValidInReg)
    {
        // Is this the special case of a write-thru lclVar?
        // We mark it as SPILLED to denote that its value is valid in memory.
        if (((tree->gtFlags & GTF_SPILL) != 0) && tree->gtOper == GT_STORE_LCL_VAR)
        {
            isValidInReg = true;
        }
    }
    assert(isValidInReg);
    assert(size != EA_UNKNOWN);
    assert(tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR));
#endif // DEBUG

    unsigned varNum = tree->AsLclVarCommon()->GetLclNum();
    assert(varNum < compiler->lvaCount);
#if CPU_LOAD_STORE_ARCH
    assert(GetEmitter()->emitInsIsStore(ins));
#endif
    GetEmitter()->emitIns_S_R(ins, size, reg, varNum, 0);
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

#ifdef TARGET_XARCH
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
#if defined(TARGET_ARM64) || defined(TARGET_ARM64)
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
            offs += tree->AsLclFld()->GetLclOffs();
            goto LCL;

        LCL:
            varNum = tree->AsLclVarCommon()->GetLclNum();
            assert(varNum < compiler->lvaCount);

#ifdef TARGET_ARM
            switch (ins)
            {
                case INS_mov:
                    ins = ins_Load(tree->TypeGet());
                    FALLTHROUGH;

                case INS_lea:
                case INS_ldr:
                case INS_ldrh:
                case INS_ldrb:
                case INS_ldrsh:
                case INS_ldrsb:
                case INS_vldr:
                    assert(flags != INS_FLAGS_SET);
                    GetEmitter()->emitIns_R_S(ins, size, reg, varNum, offs);
                    return;

                default:
                    regNumber regTmp;
                    regTmp = tree->GetRegNum();

                    GetEmitter()->emitIns_R_S(ins_Load(tree->TypeGet()), size, regTmp, varNum, offs);
                    GetEmitter()->emitIns_R_R(ins, size, reg, regTmp, flags);

                    regSet.verifyRegUsed(regTmp);
                    return;
            }
#else  // !TARGET_ARM
            GetEmitter()->emitIns_R_S(ins, size, reg, varNum, offs);
            return;
#endif // !TARGET_ARM

        case GT_CLS_VAR:
            // Make sure FP instruction size matches the operand size
            // (We optimized constant doubles to floats when we can, just want to
            // make sure that we don't mistakenly use 8 bytes when the
            // constant.
            assert(!isFloatRegType(tree->gtType) || genTypeSize(tree->gtType) == EA_SIZE_IN_BYTES(size));

#if CPU_LOAD_STORE_ARCH
            assert(!"GT_CLS_VAR not supported in ARM backend");
#else // CPU_LOAD_STORE_ARCH
            GetEmitter()->emitIns_R_C(ins, size, reg, tree->AsClsVar()->gtClsVarHnd, offs);
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
            inst_RV_IV(ins, reg, (target_ssize_t)tree->AsIntCon()->gtIconVal, emitActualTypeSize(tree->TypeGet()),
                       flags);
            break;

        case GT_CNS_LNG:

            assert(size == EA_4BYTE || size == EA_8BYTE);

#ifdef TARGET_AMD64
            assert(offs == 0);
#endif // TARGET_AMD64

            target_ssize_t constVal;
            emitAttr       size;
            if (offs == 0)
            {
                constVal = (target_ssize_t)(tree->AsLngCon()->gtLconVal);
                size     = EA_PTRSIZE;
            }
            else
            {
                constVal = (target_ssize_t)(tree->AsLngCon()->gtLconVal >> 32);
                size     = EA_4BYTE;
            }

            inst_RV_IV(ins, reg, constVal, size, flags);
            break;

        case GT_COMMA:
            tree = tree->AsOp()->gtOp2;
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
#if defined(TARGET_ARM)

    if (val >= 32)
        val &= 0x1f;

    GetEmitter()->emitIns_R_I(ins, size, reg, val, flags);

#elif defined(TARGET_XARCH)

#ifdef TARGET_AMD64
    // X64 JB BE insures only encodable values make it here.
    // x86 can encode 8 bits, though it masks down to 5 or 6
    // depending on 32-bit or 64-bit registers are used.
    // Here we will allow anything that is encodable.
    assert(val < 256);
#endif

    ins = genMapShiftInsToShiftByConstantIns(ins, val);

    if (val == 1)
    {
        GetEmitter()->emitIns_R(ins, size, reg);
    }
    else
    {
        GetEmitter()->emitIns_R_I(ins, size, reg, val);
    }

#else
    NYI("inst_RV_SH - unknown target");
#endif // TARGET*
}

/*****************************************************************************
 *
 *  Generate a "shift [r/m], icon" instruction.
 */

void CodeGen::inst_TT_SH(instruction ins, GenTree* tree, unsigned val, unsigned offs)
{
#ifdef TARGET_XARCH
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
#endif // TARGET_XARCH

#ifdef TARGET_ARM
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

#if defined(TARGET_XARCH)
void CodeGen::inst_RV_RV_IV(instruction ins, emitAttr size, regNumber reg1, regNumber reg2, unsigned ival)
{
    assert(ins == INS_shld || ins == INS_shrd || ins == INS_shufps || ins == INS_shufpd || ins == INS_pshufd ||
           ins == INS_cmpps || ins == INS_cmppd || ins == INS_dppd || ins == INS_dpps || ins == INS_insertps ||
           ins == INS_roundps || ins == INS_roundss || ins == INS_roundpd || ins == INS_roundsd);

    GetEmitter()->emitIns_R_R_I(ins, size, reg1, reg2, ival);
}

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
void CodeGen::inst_RV_TT_IV(instruction ins, emitAttr attr, regNumber reg1, GenTree* rmOp, int ival)
{
    noway_assert(GetEmitter()->emitVerifyEncodable(ins, EA_SIZE(attr), reg1));

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
#if defined(FEATURE_HW_INTRINSICS)
                assert(rmOp->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(rmOp->AsHWIntrinsic()) == 1);
                addr = rmOp->gtGetOp1();
#else
                unreached();
#endif // FEATURE_HW_INTRINSICS
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    GetEmitter()->emitIns_R_C_I(ins, attr, reg1, addr->AsClsVar()->gtClsVarHnd, 0, ival);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(rmOp->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
                    GetEmitter()->emitIns_R_A_I(ins, attr, reg1, memIndir, ival);
                    return;
                }
            }
        }
        else
        {
            switch (rmOp->OperGet())
            {
                case GT_LCL_FLD:
                    varNum = rmOp->AsLclFld()->GetLclNum();
                    offset = rmOp->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(rmOp->IsRegOptional() ||
                           !compiler->lvaGetDesc(rmOp->AsLclVar()->GetLclNum())->lvIsRegCandidate());
                    varNum = rmOp->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                default:
                    unreached();
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        GetEmitter()->emitIns_R_S_I(ins, attr, reg1, varNum, offset, ival);
    }
    else
    {
        regNumber rmOpReg = rmOp->GetRegNum();
        GetEmitter()->emitIns_SIMD_R_R_I(ins, attr, reg1, rmOpReg, ival);
    }
}

//------------------------------------------------------------------------
// inst_RV_RV_TT: Generates an instruction that takes 2 operands:
//                a register operand and an operand that may be in memory or register
//                the result is returned in register
//
// Arguments:
//    ins       -- The instruction being emitted
//    size      -- The emit size attribute
//    targetReg -- The target register
//    op1Reg    -- The first operand register
//    op2       -- The second operand, which may be a memory node or a node producing a register
//    isRMW     -- true if the instruction is RMW; otherwise, false
//
void CodeGen::inst_RV_RV_TT(
    instruction ins, emitAttr size, regNumber targetReg, regNumber op1Reg, GenTree* op2, bool isRMW)
{
    noway_assert(GetEmitter()->emitVerifyEncodable(ins, EA_SIZE(size), targetReg));

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isUsedFromSpillTemp())
        {
            assert(op2->IsRegOptional());

            tmpDsc = getSpillTempDsc(op2);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op2->isIndir() || op2->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;

            if (op2->isIndir())
            {
                memIndir = op2->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
#if defined(FEATURE_HW_INTRINSICS)
                assert(op2->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(op2->AsHWIntrinsic()) == 1);
                addr = op2->gtGetOp1();
#else
                unreached();
#endif // FEATURE_HW_INTRINSICS
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    GetEmitter()->emitIns_SIMD_R_R_C(ins, size, targetReg, op1Reg, addr->AsClsVar()->gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(op2->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
                    GetEmitter()->emitIns_SIMD_R_R_A(ins, size, targetReg, op1Reg, memIndir);
                    return;
                }
            }
        }
        else
        {
            switch (op2->OperGet())
            {
                case GT_LCL_FLD:
                {
                    varNum = op2->AsLclFld()->GetLclNum();
                    offset = op2->AsLclFld()->GetLclOffs();
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(op2->IsRegOptional() ||
                           !compiler->lvaGetDesc(op2->AsLclVar()->GetLclNum())->lvIsRegCandidate());
                    varNum = op2->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                case GT_CNS_DBL:
                {
                    GenTreeDblCon*       dblCns = op2->AsDblCon();
                    CORINFO_FIELD_HANDLE cnsDblHnd =
                        GetEmitter()->emitFltOrDblConst(dblCns->gtDconVal, emitTypeSize(dblCns));
                    GetEmitter()->emitIns_SIMD_R_R_C(ins, size, targetReg, op1Reg, cnsDblHnd, 0);
                    return;
                }

                default:
                {
                    unreached();
                }
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        GetEmitter()->emitIns_SIMD_R_R_S(ins, size, targetReg, op1Reg, varNum, offset);
    }
    else
    {
        regNumber op2Reg = op2->GetRegNum();

        if ((op1Reg != targetReg) && (op2Reg == targetReg) && isRMW)
        {
            // We have "reg2 = reg1 op reg2" where "reg1 != reg2" on a RMW instruction.
            //
            // For non-commutative instructions, we should have ensured that op2 was marked
            // delay free in order to prevent it from getting assigned the same register
            // as target. However, for commutative instructions, we can just swap the operands
            // in order to have "reg2 = reg2 op reg1" which will end up producing the right code.

            op2Reg = op1Reg;
            op1Reg = targetReg;
        }

        GetEmitter()->emitIns_SIMD_R_R_R(ins, size, targetReg, op1Reg, op2Reg);
    }
}
#endif // TARGET_XARCH

/*****************************************************************************
 *
 *  Generate an instruction with two registers, the second one being a byte
 *  or word register (i.e. this is something like "movzx eax, cl").
 */

void CodeGen::inst_RV_RR(instruction ins, emitAttr size, regNumber reg1, regNumber reg2)
{
    assert(size == EA_1BYTE || size == EA_2BYTE);
#ifdef TARGET_XARCH
    assert(ins == INS_movsx || ins == INS_movzx);
    assert(size != EA_1BYTE || (genRegMask(reg2) & RBM_BYTE_REGS));
#endif

    GetEmitter()->emitIns_R_R(ins, size, reg1, reg2);
}

/*****************************************************************************
 *
 *  The following should all end up inline in compiler.hpp at some point.
 */

void CodeGen::inst_ST_RV(instruction ins, TempDsc* tmp, unsigned ofs, regNumber reg, var_types type)
{
    GetEmitter()->emitIns_S_R(ins, emitActualTypeSize(type), reg, tmp->tdTempNum(), ofs);
}

void CodeGen::inst_ST_IV(instruction ins, TempDsc* tmp, unsigned ofs, int val, var_types type)
{
    GetEmitter()->emitIns_S_I(ins, emitActualTypeSize(type), tmp->tdTempNum(), ofs, val);
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

    GetEmitter()->emitIns_S_R(ins, emitActualTypeSize(type), reg, compiler->lvaOutgoingArgSpaceVar, ofs);
}

void CodeGen::inst_SA_IV(instruction ins, unsigned ofs, int val, var_types type)
{
    assert(ofs < compiler->lvaOutgoingArgSpaceSize);

    GetEmitter()->emitIns_S_I(ins, emitActualTypeSize(type), compiler->lvaOutgoingArgSpaceVar, ofs, val);
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
#ifdef TARGET_XARCH
void CodeGen::inst_FS_ST(instruction ins, emitAttr size, TempDsc* tmp, unsigned ofs)
{
    GetEmitter()->emitIns_S(ins, size, tmp->tdTempNum(), ofs);
}
#endif

#ifdef TARGET_ARM
bool CodeGenInterface::validImmForInstr(instruction ins, target_ssize_t imm, insFlags flags)
{
    if (GetEmitter()->emitInsIsLoadOrStore(ins) && !instIsFP(ins))
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
#endif // TARGET_ARM

#if defined(TARGET_ARM64)
bool CodeGenInterface::validImmForBL(ssize_t addr)
{
    // On arm64, we always assume a call target is in range and generate a 28-bit relative
    // 'bl' instruction. If this isn't sufficient range, the VM will generate a jump stub when
    // we call recordRelocation(). See the IMAGE_REL_ARM64_BRANCH26 case in jitinterface.cpp
    // (for JIT) or zapinfo.cpp (for NGEN). If we cannot allocate a jump stub, it is fatal.
    return true;
}
#endif // TARGET_ARM64

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
#if defined(TARGET_XARCH)
        // SSE2/AVX requires destination to be a reg always.
        // If src is in reg means, it is a reg-reg move.
        //
        // SSE2 Note: always prefer movaps/movups over movapd/movupd since the
        // former doesn't require 66h prefix and one byte smaller than the
        // latter.
        //
        // TODO-CQ: based on whether src type is aligned use movaps instead

        return (srcInReg) ? INS_movaps : INS_movups;
#elif defined(TARGET_ARM64)
        return (srcInReg) ? INS_mov : ins_Load(srcType);
#else  // !defined(TARGET_ARM64) && !defined(TARGET_XARCH)
        assert(!"unhandled SIMD type");
#endif // !defined(TARGET_ARM64) && !defined(TARGET_XARCH)
    }

#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM)
    if (varTypeIsFloating(srcType))
        return INS_vmov;
#else
    assert(!varTypeIsFloating(srcType));
#endif

#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM)
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
#elif defined(TARGET_ARM64)
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
#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM64)
        return INS_ldr;
#else
        assert(!"ins_Load with SIMD type");
#endif
    }

    if (varTypeIsFloating(srcType))
    {
#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM64)
        return INS_ldr;
#elif defined(TARGET_ARM)
        return INS_vldr;
#else
        assert(!varTypeIsFloating(srcType));
#endif
    }

#if defined(TARGET_XARCH)
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

#elif defined(TARGET_ARMARCH)
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
    assert(emitTypeActSz[dstType] != 0);
#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM64)
    if (varTypeIsFloating(dstType))
    {
        return INS_fmov;
    }
    else
    {
        return INS_mov;
    }
#elif defined(TARGET_ARM)
    assert(!varTypeIsSIMD(dstType));
    if (varTypeIsFloating(dstType))
    {
        return INS_vmov;
    }
    else
    {
        return INS_mov;
    }
#else // TARGET_*
#error "Unknown TARGET_"
#endif
}

//------------------------------------------------------------------------
//  ins_Copy: Get the machine dependent instruction for performing a reg-reg copy
//            from srcReg to a register of dstType.
//
// Arguments:
//      srcReg  - source register
//      dstType - destination type
//
// Notes:
//    This assumes the size of the value in 'srcReg' is the same as the size of
//    'dstType'.
//
instruction CodeGen::ins_Copy(regNumber srcReg, var_types dstType)
{
    bool dstIsFloatReg = isFloatRegType(dstType);
    bool srcIsFloatReg = genIsValidFloatReg(srcReg);
    if (srcIsFloatReg == dstIsFloatReg)
    {
        return ins_Copy(dstType);
    }
#if defined(TARGET_XARCH)
    return INS_movd;
#elif defined(TARGET_ARM64)
    if (dstIsFloatReg)
    {
        return INS_fmov;
    }
    else
    {
        return INS_mov;
    }
#elif defined(TARGET_ARM)
    // No SIMD support yet
    assert(!varTypeIsSIMD(dstType));
    if (dstIsFloatReg)
    {
        return (dstType == TYP_DOUBLE) ? INS_vmov_i2d : INS_vmov_i2f;
    }
    else
    {
        return (dstType == TYP_LONG) ? INS_vmov_d2i : INS_vmov_f2i;
    }
#else // TARGET*
#error "Unknown TARGET"
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

#if defined(TARGET_XARCH)
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
#elif defined(TARGET_ARM64)
    if (varTypeIsSIMD(dstType) || varTypeIsFloating(dstType))
    {
        // All sizes of SIMD and FP instructions use INS_str
        return INS_str;
    }
#elif defined(TARGET_ARM)
    assert(!varTypeIsSIMD(dstType));
    if (varTypeIsFloating(dstType))
    {
        return INS_vstr;
    }
#else
    assert(!varTypeIsSIMD(dstType));
    assert(!varTypeIsFloating(dstType));
#endif

#if defined(TARGET_XARCH)
    ins = INS_mov;
#elif defined(TARGET_ARMARCH)
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

//------------------------------------------------------------------------
// ins_StoreFromSrc: Get the machine dependent instruction for performing a store to dstType on the stack from a srcReg.
//
// Arguments:
//   srcReg  - the source register for the store
//   dstType - the destination type
//   aligned - whether the destination is properly aligned if dstType is a SIMD type
//
// Return Value:
//   the instruction to use
//
// Notes:
//   The function currently does not expect float srcReg with integral dstType and will assert on such cases.
//
instruction CodeGenInterface::ins_StoreFromSrc(regNumber srcReg, var_types dstType, bool aligned /*=false*/)
{
    bool dstIsFloatType = isFloatRegType(dstType);
    bool srcIsFloatReg  = genIsValidFloatReg(srcReg);
    if (srcIsFloatReg == dstIsFloatType)
    {
        return ins_Store(dstType, aligned);
    }

    assert(!srcIsFloatReg && dstIsFloatType && "not expecting an integer type passed in a float reg");
    assert(!varTypeIsSmall(dstType) && "not expecting small float types");

    instruction ins = INS_invalid;
#if defined(TARGET_XARCH)
    ins = INS_mov;
#elif defined(TARGET_ARMARCH)
    ins     = INS_str;
#else
    NYI("ins_Store");
#endif
    assert(ins != INS_invalid);
    return ins;
}

#if defined(TARGET_XARCH)

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

#elif defined(TARGET_ARM)

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
                    break;
                case TYP_DOUBLE:
                    NYI("long to double");
                    break;
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
                    break;
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
                    break;
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
    unreached();
}

#endif // TARGET_ARM

/*****************************************************************************
 *
 *  Machine independent way to return
 */
void CodeGen::instGen_Return(unsigned stkArgSize)
{
#if defined(TARGET_XARCH)
    if (stkArgSize == 0)
    {
        instGen(INS_ret);
    }
    else
    {
        inst_IV(INS_ret, stkArgSize);
    }
#elif defined(TARGET_ARM)
//
// The return on ARM is folded into the pop multiple instruction
// and as we do not know the exact set of registers that we will
// need to restore (pop) when we first call instGen_Return we will
// instead just not emit anything for this method on the ARM
// The return will be part of the pop multiple and that will be
// part of the epilog that is generated by genFnEpilog()
#elif defined(TARGET_ARM64)
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
void CodeGen::instGen_MemoryBarrier(BarrierKind barrierKind)
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

#if defined(TARGET_XARCH)
    // only full barrier needs to be emitted on Xarch
    if (barrierKind != BARRIER_FULL)
    {
        return;
    }

    instGen(INS_lock);
    GetEmitter()->emitIns_I_AR(INS_or, EA_4BYTE, 0, REG_SPBASE, 0);
#elif defined(TARGET_ARM)
    // ARM has only full barriers, so all barriers need to be emitted as full.
    GetEmitter()->emitIns_I(INS_dmb, EA_4BYTE, 0xf);
#elif defined(TARGET_ARM64)
    GetEmitter()->emitIns_BARR(INS_dmb, barrierKind == BARRIER_LOAD_ONLY ? INS_BARRIER_ISHLD : INS_BARRIER_ISH);
#else
#error "Unknown TARGET"
#endif
}

/*****************************************************************************
 *
 *  Machine independent way to move a Zero value into a register
 */
void CodeGen::instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags)
{
#if defined(TARGET_XARCH)
    GetEmitter()->emitIns_R_R(INS_xor, size, reg, reg);
#elif defined(TARGET_ARMARCH)
    GetEmitter()->emitIns_R_I(INS_mov, size, reg, 0 ARM_ARG(flags));
#else
#error "Unknown TARGET"
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
#if defined(TARGET_XARCH)
    GetEmitter()->emitIns_R_R(INS_test, size, reg, reg);
#elif defined(TARGET_ARMARCH)
    GetEmitter()->emitIns_R_I(INS_cmp, size, reg, 0);
#else
#error "Unknown TARGET"
#endif
}

/*****************************************************************************
 *
 *  Machine independent way to set the flags based upon
 *   comparing a register with another register
 */
void CodeGen::instGen_Compare_Reg_To_Reg(emitAttr size, regNumber reg1, regNumber reg2)
{
#if defined(TARGET_XARCH) || defined(TARGET_ARMARCH)
    GetEmitter()->emitIns_R_R(INS_cmp, size, reg1, reg2);
#else
#error "Unknown TARGET"
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
#if defined(TARGET_XARCH)
#if defined(TARGET_AMD64)
        if ((EA_SIZE(size) == EA_8BYTE) && (((int)imm != (ssize_t)imm) || EA_IS_CNS_RELOC(size)))
        {
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
        }
        else
#endif // TARGET_AMD64
        {
            GetEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
#elif defined(TARGET_ARM)
        if (arm_Valid_Imm_For_Alu(imm) || arm_Valid_Imm_For_Alu(-imm))
        {
            GetEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
        else // We need a scratch register
        {
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
        }
#elif defined(TARGET_ARM64)
        if (true) // TODO-ARM64-NYI: arm_Valid_Imm_For_Alu(imm) || arm_Valid_Imm_For_Alu(-imm))
        {
            GetEmitter()->emitIns_R_I(INS_cmp, size, reg, imm);
        }
        else // We need a scratch register
        {
            assert(!"Invalid immediate for instGen_Compare_Reg_To_Imm");
        }
#else
#error "Unknown TARGET"
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

    GetEmitter()->emitIns_R_S(ins_Load(srcType), size, dstReg, varNum, offs);
}

/*****************************************************************************
 *
 *  Machine independent way to move a register into a stack based local variable
 */
void CodeGen::instGen_Store_Reg_Into_Lcl(var_types dstType, regNumber srcReg, int varNum, int offs)
{
    emitAttr size = emitTypeSize(dstType);

    GetEmitter()->emitIns_S_R(ins_Store(dstType), size, srcReg, varNum, offs);
}

/*****************************************************************************/
/*****************************************************************************/
/*****************************************************************************/
