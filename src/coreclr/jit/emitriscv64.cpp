// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitriscv64.cpp                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_RISCV64)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

/*****************************************************************************/

const instruction emitJumpKindInstructions[] = {
    INS_nop,

#define JMP_SMALL(en, rev, ins) INS_##ins,
#include "emitjmps.h"
};

const emitJumpKind emitReverseJumpKinds[] = {
    EJ_NONE,

#define JMP_SMALL(en, rev, ins) EJ_##rev,
#include "emitjmps.h"
};

/*****************************************************************************
 * Look up the instruction for a jump kind
 */

/*static*/ instruction emitter::emitJumpKindToIns(emitJumpKind jumpKind)
{
    assert((unsigned)jumpKind < ArrLen(emitJumpKindInstructions));
    return emitJumpKindInstructions[jumpKind];
}

/*****************************************************************************
 * Reverse the conditional jump
 */

/*static*/ emitJumpKind emitter::emitReverseJumpKind(emitJumpKind jumpKind)
{
    assert(jumpKind < EJ_COUNT);
    return emitReverseJumpKinds[jumpKind];
}

/*****************************************************************************
 *
 *  Return the allocated size (in bytes) of the given instruction descriptor.
 */

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    if (emitIsSmallInsDsc(id))
        return SMALL_IDSC_SIZE;

    insOpts insOp = id->idInsOpt();

    switch (insOp)
    {
        case INS_OPTS_JALR:
        case INS_OPTS_J_cond:
        case INS_OPTS_J:
            return sizeof(instrDescJmp);

        case INS_OPTS_C:
            if (id->idIsLargeCall())
            {
                /* Must be a "fat" call descriptor */
                return sizeof(instrDescCGCA);
            }
            else
            {
                assert(!id->idIsLargeDsp());
                assert(!id->idIsLargeCns());
                return sizeof(instrDesc);
            }

        case INS_OPTS_I:
        case INS_OPTS_RC:
        case INS_OPTS_RL:
        case INS_OPTS_RELOC:
        case INS_OPTS_NONE:
            return sizeof(instrDesc);
        default:
            NO_WAY("unexpected instruction descriptor format");
            break;
    }
}

bool emitter::emitInsWritesToLclVarStackLoc(instrDesc* id)
{
    if (!id->idIsLclVar())
        return false;

    instruction ins = id->idIns();

    // This list is related to the list of instructions used to store local vars in emitIns_S_R().
    // We don't accept writing to float local vars.

    switch (ins)
    {
        case INS_sd:
        case INS_sw:
        case INS_sb:
        case INS_sh:
            return true;

        default:
            return false;
    }
}

#define LD 1
#define ST 2

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST(id, nm, info, e1) info,
    #include "instrs.h"
};
// clang-format on

inline bool emitter::emitInsMayWriteToGCReg(instruction ins)
{
    assert(ins != INS_invalid);
    return (ins <= INS_remuw) && (ins >= INS_mov) && !(ins >= INS_jal && ins <= INS_bgeu && ins != INS_jalr) &&
                   (CodeGenInterface::instInfo[ins] & ST) == 0
               ? true
               : false;
}

//------------------------------------------------------------------------
// emitInsLoad: Returns true if the instruction is some kind of load instruction.
//
bool emitter::emitInsIsLoad(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & LD) != 0;
    else
        return false;
}

//------------------------------------------------------------------------
// emitInsIsStore: Returns true if the instruction is some kind of store instruction.
//
bool emitter::emitInsIsStore(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & ST) != 0;
    else
        return false;
}

//-------------------------------------------------------------------------
// emitInsIsLoadOrStore: Returns true if the instruction is some kind of load/store instruction.
//
bool emitter::emitInsIsLoadOrStore(instruction ins)
{
    // We have pseudo ins like lea which are not included in emitInsLdStTab.
    if (ins < ArrLen(CodeGenInterface::instInfo))
        return (CodeGenInterface::instInfo[ins] & (LD | ST)) != 0;
    else
        return false;
}

/*****************************************************************************
 *
 *  Returns the specific encoding of the given CPU instruction.
 */

inline emitter::code_t emitter::emitInsCode(instruction ins /*, insFormat fmt*/) const
{
    code_t code = BAD_CODE;

    // clang-format off
    const static code_t insCode[] =
    {
        #define INST(id, nm, info, e1) e1,
        #include "instrs.h"
    };
    // clang-format on

    code = insCode[ins];

    assert((code != BAD_CODE));

    return code;
}

/****************************************************************************
 *
 *  Add an instruction with no operands.
 */

void emitter::emitIns(instruction ins)
{
    instrDesc* id = emitNewInstr(EA_8BYTE);

    id->idIns(ins);
    id->idAddr()->iiaSetInstrEncode(emitInsCode(ins));
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *  emitter::emitIns_S_R(), emitter::emitIns_S_R_R() and emitter::emitIns_R_S():
 *
 *  Add an Load/Store instruction(s): base+offset and base-addr-computing if needed.
 *  For referencing a stack-based local variable and a register
 *
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    emitIns_S_R_R(ins, attr, reg1, REG_NA, varx, offs);
}

void emitter::emitIns_S_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber tmpReg, int varx, int offs)
{
    ssize_t imm;

    assert(tmpReg != codeGen->rsGetRsvdReg());
    assert(reg1 != codeGen->rsGetRsvdReg());

    emitAttr size = EA_SIZE(attr);

#ifdef DEBUG
    switch (ins)
    {
        case INS_sd:
        case INS_sw:
        case INS_sh:
        case INS_sb:
        case INS_fsd:
        case INS_fsw:
            break;

        default:
            NYI_RISCV64("illegal ins within emitIns_S_R_R!");
            return;

    } // end switch (ins)
#endif

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);

    regNumber regBase = FPbased ? REG_FPBASE : REG_SPBASE;
    regNumber reg2;

    if (tmpReg == REG_NA)
    {
        reg2 = regBase;
        imm  = base + offs;
    }
    else
    {
        reg2 = tmpReg;
        imm  = offs;
    }

    assert(reg2 != REG_NA && reg2 != codeGen->rsGetRsvdReg());

    if (!isValidSimm12(imm))
    {
        // If immediate does not fit to store immediate 12 bits, construct necessary value in rsRsvdReg()
        // and keep tmpReg hint value unchanged.
        assert(isValidSimm20((imm + 0x800) >> 12));

        emitIns_R_I(INS_lui, EA_PTRSIZE, codeGen->rsGetRsvdReg(), (imm + 0x800) >> 12);
        emitIns_R_R_R(INS_add, EA_PTRSIZE, codeGen->rsGetRsvdReg(), codeGen->rsGetRsvdReg(), reg2);

        imm  = imm & 0xfff;
        reg2 = codeGen->rsGetRsvdReg();
    }

    instrDesc* id = emitNewInstr(attr);

    id->idReg1(reg1);

    id->idReg2(reg2);

    id->idIns(ins);

    assert(isGeneralRegister(reg2));
    code_t code = emitInsCode(ins);
    code |= (code_t)(reg1 & 0x1f) << 20;
    code |= (code_t)reg2 << 15;
    code |= (((imm >> 5) & 0x7f) << 25) | ((imm & 0x1f) << 7);

    id->idAddr()->iiaSetInstrEncode(code);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*
 *  Special notes for `offs`, please see the comment for `emitter::emitIns_S_R`.
 */
void emitter::emitIns_R_S(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    ssize_t imm;

    emitAttr size = EA_SIZE(attr);

#ifdef DEBUG
    switch (ins)
    {
        case INS_lb:
        case INS_lbu:

        case INS_lh:
        case INS_lhu:

        case INS_lw:
        case INS_lwu:
        case INS_flw:

        case INS_ld:
        case INS_fld:

            break;

        case INS_lea:
            assert(size == EA_8BYTE);
            break;

        default:
            NYI_RISCV64("illegal ins within emitIns_R_S!");
            return;

    } // end switch (ins)
#endif

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    imm  = offs < 0 ? -offs - 8 : base + offs;

    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;
    assert(offs >= 0);
    offs = offs < 0 ? -offs - 8 : offs;

    reg1 = (regNumber)((char)reg1 & 0x1f);
    code_t code;
    if ((-2048 <= imm) && (imm < 2048))
    {
        if (ins == INS_lea)
        {
            ins = INS_addi;
        }
        code = emitInsCode(ins);
        code |= (code_t)reg1 << 7;
        code |= (code_t)reg2 << 15;
        code |= (imm & 0xfff) << 20;
    }
    else
    {
        if (ins == INS_lea)
        {
            assert(isValidSimm20((imm + 0x800) >> 12));
            emitIns_R_I(INS_lui, EA_PTRSIZE, codeGen->rsGetRsvdReg(), (imm + 0x800) >> 12);
            ssize_t imm2 = imm & 0xfff;
            emitIns_R_R_I(INS_addi, EA_PTRSIZE, codeGen->rsGetRsvdReg(), codeGen->rsGetRsvdReg(), imm2);

            ins  = INS_add;
            code = emitInsCode(ins);
            code |= (code_t)reg1 << 7;
            code |= (code_t)reg2 << 15;
            code |= (code_t)codeGen->rsGetRsvdReg() << 20;
        }
        else
        {
            assert(isValidSimm20((imm + 0x800) >> 12));
            emitIns_R_I(INS_lui, EA_PTRSIZE, codeGen->rsGetRsvdReg(), (imm + 0x800) >> 12);

            emitIns_R_R_R(INS_add, EA_PTRSIZE, codeGen->rsGetRsvdReg(), codeGen->rsGetRsvdReg(), reg2);

            ssize_t imm2 = imm & 0xfff;
            code         = emitInsCode(ins);
            code |= (code_t)reg1 << 7;
            code |= (code_t)codeGen->rsGetRsvdReg() << 15;
            code |= (code_t)(imm2 & 0xfff) << 20;
        }
    }

    instrDesc* id = emitNewInstr(attr);

    id->idReg1(reg1);

    id->idIns(ins);

    id->idAddr()->iiaSetInstrEncode(code);
    id->idAddr()->iiaLclVar.initLclVarAddr(varx, offs);
    id->idSetIsLclVar();
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction with a single immediate value.
 */

void emitter::emitIns_I(instruction ins, emitAttr attr, ssize_t imm)
{
    code_t code = emitInsCode(ins);

    switch (ins)
    {
        case INS_fence:
            code |= ((imm & 0xff) << 20);
            break;
        case INS_j:
            assert(imm >= -1048576 && imm < 1048576);
            code |= ((imm >> 12) & 0xff) << 12;
            code |= ((imm >> 11) & 0x1) << 20;
            code |= ((imm >> 1) & 0x3ff) << 21;
            code |= ((imm >> 20) & 0x1) << 31;
            break;
        default:
            NO_WAY("illegal ins within emitIns_I!");
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

void emitter::emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs)
{
    NYI_RISCV64("emitIns_I_I-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    code_t code = emitInsCode(ins);

    switch (ins)
    {
        case INS_lui:
        case INS_auipc:
            assert(reg != REG_R0);
            assert(isGeneralRegister(reg));
            assert((((size_t)imm) >> 20) == 0);

            code |= reg << 7;
            code |= (imm & 0xfffff) << 12;
            break;
        case INS_jal:
            assert(isGeneralRegisterOrR0(reg));
            assert(isValidSimm21(imm));

            code |= reg << 7;
            code |= ((imm >> 12) & 0xff) << 12;
            code |= ((imm >> 11) & 0x1) << 20;
            code |= ((imm >> 1) & 0x3ff) << 21;
            code |= ((imm >> 20) & 0x1) << 31;
            break;
        default:
            NO_WAY("illegal ins within emitIns_R_I!");
            break;
    } // end switch (ins)

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

//------------------------------------------------------------------------
// emitIns_Mov: Emits a move instruction
//
// Arguments:
//    ins       -- The instruction being emitted
//    attr      -- The emit attribute
//    dstReg    -- The destination register
//    srcReg    -- The source register
//    canSkip   -- true if the move can be elided when dstReg == srcReg, otherwise false
//    insOpts   -- The instruction options
//
void emitter::emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt /* = INS_OPTS_NONE */)
{
    if (!canSkip || (dstReg != srcReg))
    {
        if ((EA_4BYTE == attr) && (INS_mov == ins))
        {
            assert(isGeneralRegisterOrR0(srcReg));
            assert(isGeneralRegisterOrR0(dstReg));
            emitIns_R_R_I(INS_addiw, attr, dstReg, srcReg, 0);
        }
        else if (INS_fsgnj_s == ins || INS_fsgnj_d == ins)
        {
            assert(isFloatReg(srcReg));
            assert(isFloatReg(dstReg));
            emitIns_R_R_R(ins, attr, dstReg, srcReg, srcReg);
        }
        else if (genIsValidFloatReg(srcReg) || genIsValidFloatReg(dstReg))
        {
            emitIns_R_R(ins, attr, dstReg, srcReg);
        }
        else
        {
            assert(isGeneralRegisterOrR0(srcReg));
            assert(isGeneralRegisterOrR0(dstReg));
            emitIns_R_R_I(INS_addi, attr, dstReg, srcReg, 0);
        }
    }
}

void emitter::emitIns_Mov(emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip)
{
    if (!canSkip || dstReg != srcReg)
    {
        assert(attr == EA_4BYTE || attr == EA_PTRSIZE);
        if (isGeneralRegisterOrR0(dstReg) && isGeneralRegisterOrR0(srcReg))
        {
            emitIns_R_R_I(attr == EA_4BYTE ? INS_addiw : INS_addi, attr, dstReg, srcReg, 0);
        }
        else if (isGeneralRegisterOrR0(dstReg) && genIsValidFloatReg(srcReg))
        {
            emitIns_R_R(attr == EA_4BYTE ? INS_fmv_x_w : INS_fmv_x_d, attr, dstReg, srcReg);
        }
        else if (genIsValidFloatReg(dstReg) && isGeneralRegisterOrR0(srcReg))
        {
            emitIns_R_R(attr == EA_4BYTE ? INS_fmv_w_x : INS_fmv_d_x, attr, dstReg, srcReg);
        }
        else if (genIsValidFloatReg(dstReg) && genIsValidFloatReg(srcReg))
        {
            emitIns_R_R_R(attr == EA_4BYTE ? INS_fsgnj_s : INS_fsgnj_d, attr, dstReg, srcReg, srcReg);
        }
        else
        {
            assert(!"Invalid registers in emitIns_Mov()\n");
        }
    }
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt /* = INS_OPTS_NONE */)
{
    code_t code = emitInsCode(ins);

    if (INS_mov == ins)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= reg1 << 7;
        code |= reg2 << 15;
    }
    else if (INS_fmv_x_d == ins || INS_fmv_x_w == ins || INS_fclass_s == ins || INS_fclass_d == ins)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isFloatReg(reg2));
        code |= reg1 << 7;
        code |= (reg2 & 0x1f) << 15;
    }
    else if (INS_fcvt_w_s == ins || INS_fcvt_wu_s == ins || INS_fcvt_w_d == ins || INS_fcvt_wu_d == ins ||
             INS_fcvt_l_s == ins || INS_fcvt_lu_s == ins || INS_fcvt_l_d == ins || INS_fcvt_lu_d == ins)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isFloatReg(reg2));
        code |= reg1 << 7;
        code |= (reg2 & 0x1f) << 15;
        code |= 0x1 << 12;
    }
    else if (INS_fmv_w_x == ins || INS_fmv_d_x == ins)
    {
        assert(isFloatReg(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= (reg1 & 0x1f) << 7;
        code |= reg2 << 15;
    }
    else if (INS_fcvt_s_w == ins || INS_fcvt_s_wu == ins || INS_fcvt_d_w == ins || INS_fcvt_d_wu == ins ||
             INS_fcvt_s_l == ins || INS_fcvt_s_lu == ins || INS_fcvt_d_l == ins || INS_fcvt_d_lu == ins)
    {
        assert(isFloatReg(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= (reg1 & 0x1f) << 7;
        code |= reg2 << 15;
        code |= 0x7 << 12;
    }
    else if (INS_fcvt_s_d == ins || INS_fcvt_d_s == ins)
    {
        assert(isFloatReg(reg1));
        assert(isFloatReg(reg2));
        code |= (reg1 & 0x1f) << 7;
        code |= (reg2 & 0x1f) << 15;
        code |= 0x7 << 12;
    }
    else
    {
        NYI_RISCV64("illegal ins within emitIns_R_R!");
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    code_t     code = emitInsCode(ins);
    instrDesc* id   = emitNewInstr(attr);

    if ((INS_addi <= ins && INS_srai >= ins) || (INS_addiw <= ins && INS_sraiw >= ins) ||
        (INS_lb <= ins && INS_lhu >= ins) || INS_ld == ins || INS_lw == ins || INS_jalr == ins || INS_fld == ins ||
        INS_flw == ins)
    {
        assert(isGeneralRegister(reg2));
        code |= (reg1 & 0x1f) << 7; // rd
        code |= reg2 << 15;         // rs1
        code |= imm << 20;          // imm
    }
    else if (INS_sd == ins || INS_sw == ins || INS_sh == ins || INS_sb == ins || INS_fsw == ins || INS_fsd == ins)
    {
        assert(isGeneralRegister(reg2));
        code |= (reg1 & 0x1f) << 20;                               // rs2
        code |= reg2 << 15;                                        // rs1
        code |= (((imm >> 5) & 0x7f) << 25) | ((imm & 0x1f) << 7); // imm
    }
    else if (INS_beq <= ins && INS_bgeu >= ins)
    {
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegister(reg2));
        assert(isValidSimm13(imm));
        assert(!(imm & 3));
        code |= reg1 << 15;
        code |= reg2 << 20;
        code |= ((imm >> 11) & 0x1) << 7;
        code |= ((imm >> 1) & 0xf) << 8;
        code |= ((imm >> 5) & 0x3f) << 25;
        code |= ((imm >> 12) & 0x1) << 31;
        // TODO-RISCV64: Move jump logic to emitIns_J
        id->idAddr()->iiaSetInstrCount(imm / sizeof(code_t));
    }
    else if (ins == INS_csrrs || ins == INS_csrrw || ins == INS_csrrc)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isValidUimm12(imm));
        code |= reg1 << 7;
        code |= reg2 << 15;
        code |= imm << 20;
    }
    else
    {
        NYI_RISCV64("illegal ins within emitIns_R_R_I!");
    }

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing register and two constants.
 */

void emitter::emitIns_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, ssize_t imm1, ssize_t imm2, insOpts opt) /* = INS_OPTS_NONE */
{
    code_t code = emitInsCode(ins);

    if (INS_csrrwi <= ins && ins <= INS_csrrci)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isValidUimm5(imm1));
        assert(isValidUimm12(imm2));
        code |= reg1 << 7;
        code |= imm1 << 15;
        code |= imm2 << 20;
    }
    else
    {
        NYI_RISCV64("illegal ins within emitIns_R_I_I!");
    }
    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt) /* = INS_OPTS_NONE */
{
    code_t code = emitInsCode(ins);

    if ((INS_add <= ins && ins <= INS_and) || (INS_mul <= ins && ins <= INS_remuw) ||
        (INS_addw <= ins && ins <= INS_sraw) || (INS_fadd_s <= ins && ins <= INS_fmax_s) ||
        (INS_fadd_d <= ins && ins <= INS_fmax_d) || (INS_feq_s <= ins && ins <= INS_fle_s) ||
        (INS_feq_d <= ins && ins <= INS_fle_d) || (INS_lr_w <= ins && ins <= INS_amomaxu_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_add:
            case INS_sub:
            case INS_sll:
            case INS_slt:
            case INS_sltu:
            case INS_xor:
            case INS_srl:
            case INS_sra:
            case INS_or:
            case INS_and:

            case INS_addw:
            case INS_subw:
            case INS_sllw:
            case INS_srlw:
            case INS_sraw:

            case INS_mul:
            case INS_mulh:
            case INS_mulhsu:
            case INS_mulhu:
            case INS_div:
            case INS_divu:
            case INS_rem:
            case INS_remu:

            case INS_mulw:
            case INS_divw:
            case INS_divuw:
            case INS_remw:
            case INS_remuw:

            case INS_fadd_s:
            case INS_fsub_s:
            case INS_fmul_s:
            case INS_fdiv_s:
            case INS_fsqrt_s:
            case INS_fsgnj_s:
            case INS_fsgnjn_s:
            case INS_fsgnjx_s:
            case INS_fmin_s:
            case INS_fmax_s:

            case INS_feq_s:
            case INS_flt_s:
            case INS_fle_s:

            case INS_fadd_d:
            case INS_fsub_d:
            case INS_fmul_d:
            case INS_fdiv_d:
            case INS_fsqrt_d:
            case INS_fsgnj_d:
            case INS_fsgnjn_d:
            case INS_fsgnjx_d:
            case INS_fmin_d:
            case INS_fmax_d:

            case INS_feq_d:
            case INS_flt_d:
            case INS_fle_d:

            case INS_lr_w:
            case INS_lr_d:
            case INS_sc_w:
            case INS_sc_d:
            case INS_amoswap_w:
            case INS_amoswap_d:
            case INS_amoadd_w:
            case INS_amoadd_d:
            case INS_amoxor_w:
            case INS_amoxor_d:
            case INS_amoand_w:
            case INS_amoand_d:
            case INS_amoor_w:
            case INS_amoor_d:
            case INS_amomin_w:
            case INS_amomin_d:
            case INS_amomax_w:
            case INS_amomax_d:
            case INS_amominu_w:
            case INS_amominu_d:
            case INS_amomaxu_w:
            case INS_amomaxu_d:
                break;
            default:
                NYI_RISCV64("illegal ins within emitIns_R_R_R!");
        }

#endif
        // Src/data register for load reserved should be empty
        assert((ins != INS_lr_w && ins != INS_lr_d) || reg3 == REG_R0);

        code |= ((reg1 & 0x1f) << 7);
        code |= ((reg2 & 0x1f) << 15);
        code |= ((reg3 & 0x1f) << 20);
        if ((INS_fadd_s <= ins && INS_fsqrt_s >= ins) || (INS_fadd_d <= ins && INS_fsqrt_d >= ins))
        {
            code |= 0x7 << 12;
        }
        else if (INS_lr_w <= ins && ins <= INS_amomaxu_d)
        {
            // For now all atomics are seq. consistent as Interlocked.* APIs don't expose acquire/release ordering
            code |= 0b11 << 25;
        }
    }
    else
    {
        NYI_RISCV64("illegal ins within emitIns_R_R_R!");
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idReg3(reg3);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers and a constant.
 */

void emitter::emitIns_R_R_R_I(instruction ins,
                              emitAttr    attr,
                              regNumber   reg1,
                              regNumber   reg2,
                              regNumber   reg3,
                              ssize_t     imm,
                              insOpts     opt /* = INS_OPTS_NONE */,
                              emitAttr    attrReg2 /* = EA_UNKNOWN */)
{
    NYI_RISCV64("emitIns_R_R_R_I-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt)
{
    NYI_RISCV64("emitIns_R_R_I_I-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add an instruction referencing four registers.
 */

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4)
{
    NYI_RISCV64("emitIns_R_R_R_R-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add an instruction with a register + static member operands.
 *  Constant is stored into JIT data which is adjacent to code.
 *
 */
void emitter::emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber addrReg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    assert(offs >= 0);
    assert(instrDesc::fitsInSmallCns(offs)); // can optimize.
    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    assert(reg != REG_R0); // for special. reg Must not be R0.
    id->idReg1(reg);       // destination register that will get the constant value.

    id->idSmallCns(offs); // usually is 0.
    id->idInsOpt(INS_OPTS_RC);
    if (emitComp->opts.compReloc)
    {
        id->idSetIsDspReloc();
        id->idCodeSize(8);
    }
    else
        id->idCodeSize(16);

    if (EA_IS_GCREF(attr))
    {
        /* A special value indicates a GCref pointer value */
        id->idGCref(GCT_GCREF);
        id->idOpSize(EA_PTRSIZE);
    }
    else if (EA_IS_BYREF(attr))
    {
        /* A special value indicates a Byref pointer value */
        id->idGCref(GCT_BYREF);
        id->idOpSize(EA_PTRSIZE);
    }

    // TODO-RISCV64: this maybe deleted.
    id->idSetIsBound(); // We won't patch address since we will know the exact distance
                        // once JIT code and data are allocated together.

    assert(addrReg == REG_NA); // NOTE: for RISV64, not support addrReg != REG_NA.

    id->idAddr()->iiaFieldHnd = fldHnd;

    appendToCurIG(id);
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    NYI_RISCV64("emitIns_R_AR-----unimplemented/unused on RISCV64 yet----");
}

// This computes address from the immediate which is relocatable.
void emitter::emitIns_R_AI(instruction ins,
                           emitAttr    attr,
                           regNumber   reg,
                           ssize_t addr DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    assert(EA_IS_RELOC(attr)); // EA_PTR_DSP_RELOC
    assert(ins == INS_jal);    // for special.
    assert(isGeneralRegister(reg));

    // INS_OPTS_RELOC: placeholders.  2-ins:
    //  case:EA_HANDLE_CNS_RELOC
    //   auipc  reg, off-hi-20bits
    //   addi   reg, reg, off-lo-12bits
    //  case:EA_PTR_DSP_RELOC
    //   auipc  reg, off-hi-20bits
    //   ld     reg, reg, off-lo-12bits

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    assert(reg != REG_R0); // for special. reg Must not be R0.
    id->idReg1(reg);       // destination register that will get the constant value.

    id->idInsOpt(INS_OPTS_RELOC);

    if (EA_IS_GCREF(attr))
    {
        /* A special value indicates a GCref pointer value */
        id->idGCref(GCT_GCREF);
        id->idOpSize(EA_PTRSIZE);
    }
    else if (EA_IS_BYREF(attr))
    {
        /* A special value indicates a Byref pointer value */
        id->idGCref(GCT_BYREF);
        id->idOpSize(EA_PTRSIZE);
    }

    id->idAddr()->iiaAddr = (BYTE*)addr;
    id->idCodeSize(8);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Record that a jump instruction uses the short encoding
 *
 */
void emitter::emitSetShortJump(instrDescJmp* id)
{
    // TODO-RISCV64: maybe delete it on future.
    NYI_RISCV64("emitSetShortJump-----unimplemented/unused on RISCV64 yet----");
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */

void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(dst->HasFlag(BBF_HAS_LABEL));

    // if for reloc!  4-ins:
    //   auipc reg, offset-hi20
    //   addi  reg, reg, offset-lo12
    //
    // else:  3-ins:
    //   lui  tmp, dst-hi-20bits
    //   addi tmp, tmp, dst-lo-12bits
    //   lui  reg, 0xff << 12
    //   slli reg, reg, 32
    //   add  reg, tmp, reg

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idInsOpt(INS_OPTS_RL);
    id->idAddr()->iiaBBlabel = dst;

    if (emitComp->opts.compReloc)
    {
        id->idSetIsDspReloc();
        id->idCodeSize(8);
    }
    else
        id->idCodeSize(20);

    id->idReg1(reg);

    if (EA_IS_GCREF(attr))
    {
        /* A special value indicates a GCref pointer value */
        id->idGCref(GCT_GCREF);
        id->idOpSize(EA_PTRSIZE);
    }
    else if (EA_IS_BYREF(attr))
    {
        /* A special value indicates a Byref pointer value */
        id->idGCref(GCT_BYREF);
        id->idOpSize(EA_PTRSIZE);
    }

#ifdef DEBUG
    // Mark the catch return
    if (emitComp->compCurBB->KindIs(BBJ_EHCATCHRET))
    {
        id->idDebugOnlyInfo()->idCatchRet = true;
    }
#endif // DEBUG

    appendToCurIG(id);
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    NYI_RISCV64("emitIns_J_R-----unimplemented/unused on RISCV64 yet----");
}

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    assert(dst != nullptr);
    //
    // INS_OPTS_J: placeholders.  1-ins: if the dst outof-range will be replaced by INS_OPTS_JALR.
    // jal/j/jalr/bnez/beqz/beq/bne/blt/bge/bltu/bgeu dst

    assert(dst->HasFlag(BBF_HAS_LABEL));

    instrDescJmp* id = emitNewInstrJmp();
    assert((INS_jal <= ins) && (ins <= INS_bgeu));
    id->idIns(ins);
    id->idReg1((regNumber)(instrCount & 0x1f));
    id->idReg2((regNumber)((instrCount >> 5) & 0x1f));

    id->idInsOpt(INS_OPTS_J);
    emitCounts_INS_OPTS_J++;
    id->idAddr()->iiaBBlabel = dst;

    if (emitComp->opts.compReloc)
    {
        id->idSetIsDspReloc();
    }

    id->idjShort = false;

    // TODO-RISCV64: maybe deleted this.
    id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);
#ifdef DEBUG
    if (emitComp->opts.compLongAddress) // Force long branches
        id->idjKeepLong = 1;
#endif // DEBUG

    /* Record the jump's IG and offset within it */
    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */
    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    id->idCodeSize(4);

    appendToCurIG(id);
}

void emitter::emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1, regNumber reg2)
{
    // TODO-RISCV64:
    //   Now the emitIns_J_cond_la() is only the short condition branch.
    //   There is no long condition branch for RISCV64 so far.
    //   For RISCV64 , the long condition branch is like this:
    //     --->  branch_condition  condition_target;     //here is the condition branch, short branch is enough.
    //     --->  jump jump_target; (this supporting the long jump.)
    //     condition_target:
    //     ...
    //     ...
    //     jump_target:
    //
    //
    // INS_OPTS_J_cond: placeholders.  1-ins.
    //   ins  reg1, reg2, dst

    assert(dst != nullptr);
    assert(dst->HasFlag(BBF_HAS_LABEL));

    instrDescJmp* id = emitNewInstrJmp();

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idjShort = false;

    id->idInsOpt(INS_OPTS_J_cond);
    id->idAddr()->iiaBBlabel = dst;

    id->idjKeepLong = emitComp->fgInDifferentRegions(emitComp->compCurBB, dst);
#ifdef DEBUG
    if (emitComp->opts.compLongAddress) // Force long branches
        id->idjKeepLong = 1;
#endif // DEBUG

    /* Record the jump's IG and offset within it */
    id->idjIG   = emitCurIG;
    id->idjOffs = emitCurIGsize;

    /* Append this jump to this IG's jump list */
    id->idjNext      = emitCurIGjmpList;
    emitCurIGjmpList = id;

#if EMITTER_STATS
    emitTotalIGjmps++;
#endif

    id->idCodeSize(4);

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Emits load of 64-bit constant to register.
 *
 */
void emitter::emitLoadImmediate(emitAttr size, regNumber reg, ssize_t imm)
{
    // In the worst case a sequence of 8 instructions will be used:
    //   LUI + ADDIW + SLLI + ADDI + SLLI + ADDI + SLLI + ADDI
    //
    // First 2 instructions (LUI + ADDIW) load up to 31 bit. And followed
    // sequence of (SLLI + ADDI) is used for loading remaining part by batches
    // of up to 11 bits.
    //
    // Note that LUI, ADDI/W use sign extension so that's why we have to work
    // with 31 and 11 bit batches there.

    assert(!EA_IS_RELOC(size));
    assert(isGeneralRegister(reg));

    if (isValidSimm12(imm))
    {
        emitIns_R_R_I(INS_addi, size, reg, REG_R0, imm & 0xFFF);
        return;
    }

    // TODO-RISCV64: maybe optimized via emitDataConst(), check #86790

    UINT32 msb = BitOperations::BitScanReverse((uint64_t)imm);
    UINT32 high31;
    if (msb > 30)
    {
        high31 = (imm >> (msb - 30)) & 0x7FffFFff;
    }
    else
    {
        high31 = imm & 0x7FffFFff;
    }

    // Since ADDIW use sign extension fo immediate
    // we have to adjust higher 19 bit loaded by LUI
    // for case when low part is bigger than 0x800.
    UINT32 high19 = (high31 + 0x800) >> 12;

    emitIns_R_I(INS_lui, size, reg, high19);
    emitIns_R_R_I(INS_addiw, size, reg, reg, high31 & 0xFFF);

    // And load remaining part part by batches of 11 bits size.
    INT32 remainingShift = msb - 30;
    while (remainingShift > 0)
    {
        UINT32 shift = remainingShift >= 11 ? 11 : remainingShift % 11;
        emitIns_R_R_I(INS_slli, size, reg, reg, shift);

        UINT32  mask  = 0x7ff >> (11 - shift);
        ssize_t low11 = (imm >> (remainingShift - shift)) & mask;
        emitIns_R_R_I(INS_addi, size, reg, reg, low11);
        remainingShift = remainingShift - shift;
    }
}

/*****************************************************************************
 *
 *  Add a call instruction (direct or indirect).
 *      argSize<0 means that the caller will pop the arguments
 *
 * The other arguments are interpreted depending on callType as shown:
 * Unless otherwise specified, ireg,xreg,xmul,disp should have default values.
 *
 * EC_FUNC_TOKEN       : addr is the method address
 *
 * If callType is one of these emitCallTypes, addr has to be NULL.
 * EC_INDIR_R          : "call ireg".
 *
 */

void emitter::emitIns_Call(EmitCallType          callType,
                           CORINFO_METHOD_HANDLE methHnd,
                           INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                           void*    addr,
                           ssize_t  argSize,
                           emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                           VARSET_VALARG_TP ptrVars,
                           regMaskTP        gcrefRegs,
                           regMaskTP        byrefRegs,
                           const DebugInfo& di /* = DebugInfo() */,
                           regNumber        ireg /* = REG_NA */,
                           regNumber        xreg /* = REG_NA */,
                           unsigned         xmul /* = 0     */,
                           ssize_t          disp /* = 0     */,
                           bool             isJump /* = false */)
{
    /* Sanity check the arguments depending on callType */

    assert(callType < EC_COUNT);
    assert((callType != EC_FUNC_TOKEN) || (ireg == REG_NA && xreg == REG_NA && xmul == 0 && disp == 0));
    assert(callType < EC_INDIR_R || addr == NULL);
    assert(callType != EC_INDIR_R || (ireg < REG_COUNT && xreg == REG_NA && xmul == 0 && disp == 0));

    // RISCV64 never uses these
    assert(xreg == REG_NA && xmul == 0 && disp == 0);

    // Our stack level should be always greater than the bytes of arguments we push. Just
    // a sanity test.
    assert((unsigned)abs(argSize) <= codeGen->genStackLevel);

    // Trim out any callee-trashed registers from the live set.
    regMaskTP savedSet = emitGetGCRegsSavedOrModified(methHnd);
    gcrefRegs &= savedSet;
    byrefRegs &= savedSet;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("Call: GCvars=%s ", VarSetOps::ToString(emitComp, ptrVars));
        dumpConvertedVarSet(emitComp, ptrVars);
        printf(", gcrefRegs=");
        printRegMaskInt(gcrefRegs);
        emitDispRegSet(gcrefRegs);
        printf(", byrefRegs=");
        printRegMaskInt(byrefRegs);
        emitDispRegSet(byrefRegs);
        printf("\n");
    }
#endif

    /* Managed RetVal: emit sequence point for the call */
    if (emitComp->opts.compDbgInfo && di.GetLocation().IsValid())
    {
        codeGen->genIPmappingAdd(IPmappingDscKind::Normal, di, false);
    }

    /*
        We need to allocate the appropriate instruction descriptor based
        on whether this is a direct/indirect call, and whether we need to
        record an updated set of live GC variables.
     */
    instrDesc* id;

    assert(argSize % REGSIZE_BYTES == 0);
    int argCnt = (int)(argSize / (int)REGSIZE_BYTES);

    if (callType >= EC_INDIR_R)
    {
        /* Indirect call, virtual calls */

        assert(callType == EC_INDIR_R);

        id = emitNewInstrCallInd(argCnt, disp, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
    }
    else
    {
        /* Helper/static/nonvirtual/function calls (direct or through handle),
           and calls to an absolute addr. */

        assert(callType == EC_FUNC_TOKEN);

        id = emitNewInstrCallDir(argCnt, ptrVars, gcrefRegs, byrefRegs, retSize, secondRetSize);
    }

    /* Update the emitter's live GC ref sets */

    VarSetOps::Assign(emitComp, emitThisGCrefVars, ptrVars);
    emitThisGCrefRegs = gcrefRegs;
    emitThisByrefRegs = byrefRegs;

    id->idSetIsNoGC(emitNoGChelper(methHnd));

    /* Set the instruction - special case jumping a function */
    instruction ins;

    ins = INS_jalr; // jalr
    id->idIns(ins);

    id->idInsOpt(INS_OPTS_C);
    // TODO-RISCV64: maybe optimize.

    // INS_OPTS_C: placeholders.  1/2/4-ins:
    //   if (callType == EC_INDIR_R)
    //      jalr REG_R0/REG_RA, ireg, 0   <---- 1-ins
    //   else if (callType == EC_FUNC_TOKEN || callType == EC_FUNC_ADDR)
    //     if reloc:
    //             //pc + offset_38bits       # only when reloc.
    //      auipc t2, addr-hi20
    //      jalr r0/1, t2, addr-lo12
    //
    //     else:
    //      lui  t2, dst_offset_lo32-hi
    //      ori  t2, t2, dst_offset_lo32-lo
    //      lui  t2, dst_offset_hi32-lo
    //      jalr REG_R0/REG_RA, t2, 0

    /* Record the address: method, indirection, or funcptr */
    if (callType == EC_INDIR_R)
    {
        /* This is an indirect call (either a virtual call or func ptr call) */
        // assert(callType == EC_INDIR_R);

        id->idSetIsCallRegPtr();

        regNumber reg_jalr = isJump ? REG_R0 : REG_RA;
        id->idReg4(reg_jalr);
        id->idReg3(ireg); // NOTE: for EC_INDIR_R, using idReg3.
        assert(xreg == REG_NA);

        id->idCodeSize(4);
    }
    else
    {
        /* This is a simple direct call: "call helper/method/addr" */

        assert(callType == EC_FUNC_TOKEN);
        assert(addr != NULL);

        addr = (void*)(((size_t)addr) + (isJump ? 0 : 1)); // NOTE: low-bit0 is used for jalr ra/r0,rd,0
        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (emitComp->opts.compReloc)
        {
            id->idSetIsDspReloc();
            id->idCodeSize(8);
        }
        else
        {
            id->idCodeSize(32);
        }
    }

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        if (id->idIsLargeCall())
        {
            printf("[%02u] Rec call GC vars = %s\n", id->idDebugOnlyInfo()->idNum,
                   VarSetOps::ToString(emitComp, ((instrDescCGCA*)id)->idcGCvars));
        }
    }

    id->idDebugOnlyInfo()->idMemCookie = (size_t)methHnd; // method token
    id->idDebugOnlyInfo()->idCallSig   = sigInfo;
#endif // DEBUG

#ifdef LATE_DISASM
    if (addr != nullptr)
    {
        codeGen->getDisAssembler().disSetMethod((size_t)addr, methHnd);
    }
#endif // LATE_DISASM

    appendToCurIG(id);
}

/*****************************************************************************
 *
 *  Output a call instruction.
 */

unsigned emitter::emitOutputCall(const insGroup* ig, BYTE* dst, instrDesc* id, code_t code)
{
    unsigned char callInstrSize = sizeof(code_t); // 4 bytes
    regMaskTP     gcrefRegs;
    regMaskTP     byrefRegs;

    VARSET_TP GCvars(VarSetOps::UninitVal());

    // Is this a "fat" call descriptor?
    if (id->idIsLargeCall())
    {
        instrDescCGCA* idCall = (instrDescCGCA*)id;
        gcrefRegs             = idCall->idcGcrefRegs;
        byrefRegs             = idCall->idcByrefRegs;
        VarSetOps::Assign(emitComp, GCvars, idCall->idcGCvars);
    }
    else
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCns());

        gcrefRegs = emitDecodeCallGCregs(id);
        byrefRegs = 0;
        VarSetOps::AssignNoCopy(emitComp, GCvars, VarSetOps::MakeEmpty(emitComp));
    }

    /* We update the GC info before the call as the variables cannot be
        used by the call. Killing variables before the call helps with
        boundary conditions if the call is CORINFO_HELP_THROW - see bug 50029.
        If we ever track aliased variables (which could be used by the
        call), we would have to keep them alive past the call. */

    emitUpdateLiveGCvars(GCvars, dst);
#ifdef DEBUG
    // NOTEADD:
    // Output any delta in GC variable info, corresponding to the before-call GC var updates done above.
    if (EMIT_GC_VERBOSE || emitComp->opts.disasmWithGC)
    {
        emitDispGCVarDelta(); // define in emit.cpp
    }
#endif // DEBUG

    assert(id->idIns() == INS_jalr);
    if (id->idIsCallRegPtr())
    { // EC_INDIR_R
        code = emitInsCode(id->idIns());
        code |= (code_t)id->idReg4() << 7;
        code |= (code_t)id->idReg3() << 15;
        // the offset default is 0;
        emitOutput_Instr(dst, code);
    }
    else if (id->idIsReloc())
    {
        // pc + offset_32bits
        //
        //   auipc t2, addr-hi20
        //   jalr r0/1,t2,addr-lo12

        emitOutput_Instr(dst, 0x00000397);

        size_t addr = (size_t)(id->idAddr()->iiaAddr); // get addr.

        int reg2 = ((int)addr & 1) + 10;
        addr     = addr ^ 1;

        assert(isValidSimm32(addr - (ssize_t)dst));
        assert((addr & 1) == 0);

        dst += 4;
        emitGCregDeadUpd(REG_DEFAULT_HELPER_CALL_TARGET, dst);

#ifdef DEBUG
        code = emitInsCode(INS_auipc);
        assert((code | (REG_DEFAULT_HELPER_CALL_TARGET << 7)) == 0x00000397);
        assert((int)REG_DEFAULT_HELPER_CALL_TARGET == 7);
        code = emitInsCode(INS_jalr);
        assert(code == 0x00000067);
#endif
        emitOutput_Instr(dst, 0x00000067 | (REG_DEFAULT_HELPER_CALL_TARGET << 15) | reg2 << 7);

        emitRecordRelocation(dst - 4, (BYTE*)addr, IMAGE_REL_RISCV64_PC);
    }
    else
    {
        // lui  t2, dst_offset_hi32-hi
        // addi t2, t2, dst_offset_hi32-lo
        // slli t2, t2, 11
        // addi t2, t2, dst_offset_low32-hi
        // slli t2, t2, 11
        // addi t2, t2, dst_offset_low32-md
        // slli t2, t2, 10
        // jalr t2

        ssize_t imm = (ssize_t)(id->idAddr()->iiaAddr);
        assert((imm >> 32) <= 0xff);

        int reg2 = (int)(imm & 1);
        imm -= reg2;

        UINT32 high = imm >> 32;
        code        = emitInsCode(INS_lui);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= ((code_t)((high + 0x800) >> 12) & 0xfffff) << 12;
        emitOutput_Instr(dst, code);
        dst += 4;

        emitGCregDeadUpd(REG_DEFAULT_HELPER_CALL_TARGET, dst);

        code = emitInsCode(INS_addi);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= (code_t)(high & 0xfff) << 20;
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_slli);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= (code_t)(11 << 20);
        emitOutput_Instr(dst, code);
        dst += 4;

        UINT32 low = imm & 0xffffffff;

        code = emitInsCode(INS_addi);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= ((low >> 21) & 0x7ff) << 20;
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_slli);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= (code_t)(11 << 20);
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_addi);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= ((low >> 10) & 0x7ff) << 20;
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_slli);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= (code_t)(10 << 20);
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_jalr);
        code |= (code_t)reg2 << 7;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 15;
        code |= (low & 0x3ff) << 20;
        // the offset default is 0;
        emitOutput_Instr(dst, code);
    }

    dst += 4;

    // If the method returns a GC ref, mark INTRET (A0) appropriately.
    if (id->idGCref() == GCT_GCREF)
    {
        gcrefRegs |= RBM_INTRET;
    }
    else if (id->idGCref() == GCT_BYREF)
    {
        byrefRegs |= RBM_INTRET;
    }

    // If is a multi-register return method is called, mark INTRET_1 (A1) appropriately
    if (id->idIsLargeCall())
    {
        instrDescCGCA* idCall = (instrDescCGCA*)id;
        if (idCall->idSecondGCref() == GCT_GCREF)
        {
            gcrefRegs |= RBM_INTRET_1;
        }
        else if (idCall->idSecondGCref() == GCT_BYREF)
        {
            byrefRegs |= RBM_INTRET_1;
        }
    }

    // If the GC register set has changed, report the new set.
    if (gcrefRegs != emitThisGCrefRegs)
    {
        emitUpdateLiveGCregs(GCT_GCREF, gcrefRegs, dst);
    }
    // If the Byref register set has changed, report the new set.
    if (byrefRegs != emitThisByrefRegs)
    {
        emitUpdateLiveGCregs(GCT_BYREF, byrefRegs, dst);
    }

    // Some helper calls may be marked as not requiring GC info to be recorded.
    if (!id->idIsNoGC())
    {
        // On RISCV64, as on AMD64 and LOONGARCH64, we don't change the stack pointer to push/pop args.
        // So we're not really doing a "stack pop" here (note that "args" is 0), but we use this mechanism
        // to record the call for GC info purposes.  (It might be best to use an alternate call,
        // and protect "emitStackPop" under the EMIT_TRACK_STACK_DEPTH preprocessor variable.)
        emitStackPop(dst, /*isCall*/ true, callInstrSize, /*args*/ 0);

        // Do we need to record a call location for GC purposes?
        //
        if (!emitFullGCinfo)
        {
            emitRecordGCcall(dst, callInstrSize);
        }
    }
    if (id->idIsCallRegPtr())
    {
        callInstrSize = 1 << 2;
    }
    else
    {
        callInstrSize = id->idIsReloc() ? (2 << 2) : (8 << 2); // INS_OPTS_C: 2/9-ins.
    }

    return callInstrSize;
}

void emitter::emitJumpDistBind()
{
#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** In emitJumpDistBind()\n");
    }
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nInstruction list before the jump distance binding:\n\n");
        emitDispIGlist(true);
    }
#endif

#if DEBUG_EMIT
    auto printJmpInfo = [this](const instrDescJmp* jmp, const insGroup* jmpIG, NATIVE_OFFSET extra,
                               UNATIVE_OFFSET srcInstrOffs, UNATIVE_OFFSET srcEncodingOffs, UNATIVE_OFFSET dstOffs,
                               NATIVE_OFFSET jmpDist, const char* direction) {
        assert(jmp->idDebugOnlyInfo() != nullptr);
        if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
        {
            const char* dirId = (strcmp(direction, "fwd") == 0) ? "[1]" : "[2]";
            if (INTERESTING_JUMP_NUM == 0)
            {
                printf("%s Jump %u:\n", dirId, jmp->idDebugOnlyInfo()->idNum);
            }
            printf("%s Jump  block is at %08X\n", dirId, jmpIG->igOffs);
            printf("%s Jump reloffset is %04X\n", dirId, jmp->idjOffs);
            printf("%s Jump source is at %08X\n", dirId, srcEncodingOffs);
            printf("%s Label block is at %08X\n", dirId, dstOffs);
            printf("%s Jump  dist. is    %04X\n", dirId, jmpDist);
            if (extra > 0)
            {
                printf("%s Dist excess [S] = %d  \n", dirId, extra);
            }
        }
        if (EMITVERBOSE)
        {
            printf("Estimate of %s jump [%08X/%03u]: %04X -> %04X = %04X\n", direction, dspPtr(jmp),
                   jmp->idDebugOnlyInfo()->idNum, srcInstrOffs, dstOffs, jmpDist);
        }
    };
#endif

    instrDescJmp* jmp;

    UNATIVE_OFFSET adjIG;
    UNATIVE_OFFSET adjSJ;
    insGroup*      lstIG;
#ifdef DEBUG
    insGroup* prologIG = emitPrologIG;
#endif // DEBUG

    // NOTE:
    //  bit0 of isLinkingEnd: indicating whether updating the instrDescJmp's size with the type INS_OPTS_J;
    //  bit1 of isLinkingEnd: indicating not needed updating the size while emitTotalCodeSize <= 0xfff or had
    //  updated;
    unsigned int isLinkingEnd = emitTotalCodeSize <= 0xfff ? 2 : 0;

    UNATIVE_OFFSET ssz = 0; // relative small jump's delay-slot.
    // small  jump max. neg distance
    NATIVE_OFFSET nsd = B_DIST_SMALL_MAX_NEG;
    // small  jump max. pos distance
    NATIVE_OFFSET maxPlaceholderSize =
        emitCounts_INS_OPTS_J * (6 << 2); // the max placeholder sizeof(INS_OPTS_JALR) - sizeof(INS_OPTS_J)
    NATIVE_OFFSET psd = B_DIST_SMALL_MAX_POS - maxPlaceholderSize;

/*****************************************************************************/
/* If the default small encoding is not enough, we start again here.     */
/*****************************************************************************/

AGAIN:

#ifdef DEBUG
    emitCheckIGList();
#endif

#ifdef DEBUG
    insGroup*     lastIG = nullptr;
    instrDescJmp* lastSJ = nullptr;
#endif

    lstIG = nullptr;
    adjSJ = 0;
    adjIG = 0;

    for (jmp = emitJumpList; jmp; jmp = jmp->idjNext)
    {
        insGroup* jmpIG;
        insGroup* tgtIG;

        UNATIVE_OFFSET jsz; // size of the jump instruction in bytes

        NATIVE_OFFSET  extra;           // How far beyond the short jump range is this jump offset?
        UNATIVE_OFFSET srcInstrOffs;    // offset of the source instruction of the jump
        UNATIVE_OFFSET srcEncodingOffs; // offset of the source used by the instruction set to calculate the relative
                                        // offset of the jump
        UNATIVE_OFFSET dstOffs;
        NATIVE_OFFSET  jmpDist; // the relative jump distance, as it will be encoded

/* Make sure the jumps are properly ordered */

#ifdef DEBUG
        assert(lastSJ == nullptr || lastIG != jmp->idjIG || lastSJ->idjOffs < (jmp->idjOffs + adjSJ));
        lastSJ = (lastIG == jmp->idjIG) ? jmp : nullptr;

        assert(lastIG == nullptr || lastIG->igNum <= jmp->idjIG->igNum || jmp->idjIG == prologIG ||
               emitNxtIGnum > unsigned(0xFFFF)); // igNum might overflow
        lastIG = jmp->idjIG;
#endif // DEBUG

        /* Get hold of the current jump size */

        jsz = jmp->idCodeSize();

        /* Get the group the jump is in */

        jmpIG = jmp->idjIG;

        /* Are we in a group different from the previous jump? */

        if (lstIG != jmpIG)
        {
            /* Were there any jumps before this one? */

            if (lstIG)
            {
                /* Adjust the offsets of the intervening blocks */

                do
                {
                    lstIG = lstIG->igNext;
                    assert(lstIG);
#ifdef DEBUG
                    if (EMITVERBOSE)
                    {
                        printf("Adjusted offset of " FMT_BB " from %04X to %04X\n", lstIG->igNum, lstIG->igOffs,
                               lstIG->igOffs + adjIG);
                    }
#endif // DEBUG
                    lstIG->igOffs += adjIG;
                    assert(IsCodeAligned(lstIG->igOffs));
                } while (lstIG != jmpIG);
            }

            /* We've got the first jump in a new group */
            adjSJ = 0;
            lstIG = jmpIG;
        }

        /* Apply any local size adjustment to the jump's relative offset */
        jmp->idjOffs += adjSJ;

        // If this is a jump via register, the instruction size does not change, so we are done.
        CLANG_FORMAT_COMMENT_ANCHOR;

        /* Have we bound this jump's target already? */

        if (jmp->idIsBound())
        {
            /* Does the jump already have the smallest size? */

            if (jmp->idjShort)
            {
                // We should not be jumping/branching across funclets/functions
                emitCheckFuncletBranch(jmp, jmpIG);

                continue;
            }

            tgtIG = jmp->idAddr()->iiaIGlabel;
        }
        else
        {
            /* First time we've seen this label, convert its target */
            CLANG_FORMAT_COMMENT_ANCHOR;

            tgtIG = (insGroup*)emitCodeGetCookie(jmp->idAddr()->iiaBBlabel);

#ifdef DEBUG
            if (EMITVERBOSE)
            {
                if (tgtIG)
                {
                    printf(" to %s\n", emitLabelString(tgtIG));
                }
                else
                {
                    printf("-- ERROR, no emitter cookie for " FMT_BB "; it is probably missing BBF_HAS_LABEL.\n",
                           jmp->idAddr()->iiaBBlabel->bbNum);
                }
            }
            assert(tgtIG);
#endif // DEBUG

            /* Record the bound target */

            jmp->idAddr()->iiaIGlabel = tgtIG;
            jmp->idSetIsBound();
        }

        // We should not be jumping/branching across funclets/functions
        emitCheckFuncletBranch(jmp, jmpIG);

        /*
            In the following distance calculations, if we're not actually
            scheduling the code (i.e. reordering instructions), we can
            use the actual offset of the jump (rather than the beg/end of
            the instruction group) since the jump will not be moved around
            and thus its offset is accurate.

            First we need to figure out whether this jump is a forward or
            backward one; to do this we simply look at the ordinals of the
            group that contains the jump and the target.
         */

        srcInstrOffs = jmpIG->igOffs + jmp->idjOffs;

        /* Note that the destination is always the beginning of an IG, so no need for an offset inside it */
        dstOffs = tgtIG->igOffs;

        srcEncodingOffs = srcInstrOffs + ssz; // Encoding offset of relative offset for small branch

        if (jmpIG->igNum < tgtIG->igNum)
        {
            /* Forward jump */

            /* Adjust the target offset by the current delta. This is a worst-case estimate, as jumps between
               here and the target could be shortened, causing the actual distance to shrink.
             */

            dstOffs += adjIG;

            /* Compute the distance estimate */

            jmpDist = dstOffs - srcEncodingOffs;

            /* How much beyond the max. short distance does the jump go? */

            extra = jmpDist - psd;

#if DEBUG_EMIT
            printJmpInfo(jmp, jmpIG, extra, srcInstrOffs, srcEncodingOffs, dstOffs, jmpDist, "fwd");
#endif // DEBUG_EMIT

            assert(jmpDist >= 0); // Forward jump
            assert(!(jmpDist & 0x3));

            if (!(isLinkingEnd & 0x2) && (extra > 0) &&
                (jmp->idInsOpt() == INS_OPTS_J || jmp->idInsOpt() == INS_OPTS_J_cond))
            {
                // transform forward INS_OPTS_J/INS_OPTS_J_cond jump when jmpDist exceed the maximum short distance
                instruction ins = jmp->idIns();
                assert((INS_jal <= ins) && (ins <= INS_bgeu));

                if (ins > INS_jalr ||
                    (ins < INS_jalr && ins > INS_j)) // jal < beqz < bnez < jalr < beq/bne/blt/bltu/bge/bgeu
                {
                    if (isValidSimm13(jmpDist + maxPlaceholderSize))
                    {
                        continue;
                    }
                    else if (isValidSimm21(jmpDist + maxPlaceholderSize))
                    {
                        // convert to opposite branch and jal
                        extra = 4;
                    }
                    else
                    {
                        // convert to opposite branch and jalr
                        extra = 4 * 6;
                    }
                }
                else if (ins == INS_jal || ins == INS_j)
                {
                    if (isValidSimm21(jmpDist + maxPlaceholderSize))
                    {
                        continue;
                    }
                    else
                    {
                        // convert to jalr
                        extra = 4 * 5;
                    }
                }
                else
                {
                    assert(ins == INS_jalr);
                    assert((jmpDist + maxPlaceholderSize) < 0x800);
                    continue;
                }

                jmp->idInsOpt(INS_OPTS_JALR);
                jmp->idCodeSize(jmp->idCodeSize() + extra);
                jmpIG->igSize += (unsigned short)extra; // the placeholder sizeof(INS_OPTS_JALR) - sizeof(INS_OPTS_J).
                adjSJ += (UNATIVE_OFFSET)extra;
                adjIG += (UNATIVE_OFFSET)extra;
                emitTotalCodeSize += (UNATIVE_OFFSET)extra;
                jmpIG->igFlags |= IGF_UPD_ISZ;
                isLinkingEnd |= 0x1;
            }
            continue;
        }
        else
        {
            /* Backward jump */

            /* Compute the distance estimate */

            jmpDist = srcEncodingOffs - dstOffs;

            /* How much beyond the max. short distance does the jump go? */

            extra = jmpDist + nsd;

#if DEBUG_EMIT
            printJmpInfo(jmp, jmpIG, extra, srcInstrOffs, srcEncodingOffs, dstOffs, jmpDist, "bwd");
#endif // DEBUG_EMIT

            assert(jmpDist >= 0); // Backward jump
            assert(!(jmpDist & 0x3));

            if (!(isLinkingEnd & 0x2) && (extra > 0) &&
                (jmp->idInsOpt() == INS_OPTS_J || jmp->idInsOpt() == INS_OPTS_J_cond))
            {
                // transform backward INS_OPTS_J/INS_OPTS_J_cond jump when jmpDist exceed the maximum short distance
                instruction ins = jmp->idIns();
                assert((INS_jal <= ins) && (ins <= INS_bgeu));

                if (ins > INS_jalr ||
                    (ins < INS_jalr && ins > INS_j)) // jal < beqz < bnez < jalr < beq/bne/blt/bltu/bge/bgeu
                {
                    if (isValidSimm13(jmpDist + maxPlaceholderSize))
                    {
                        continue;
                    }
                    else if (isValidSimm21(jmpDist + maxPlaceholderSize))
                    {
                        // convert to opposite branch and jal
                        extra = 4;
                    }
                    else
                    {
                        // convert to opposite branch and jalr
                        extra = 4 * 6;
                    }
                }
                else if (ins == INS_jal || ins == INS_j)
                {
                    if (isValidSimm21(jmpDist + maxPlaceholderSize))
                    {
                        continue;
                    }
                    else
                    {
                        // convert to jalr
                        extra = 4 * 5;
                    }
                }
                else
                {
                    assert(ins == INS_jalr);
                    assert((jmpDist + maxPlaceholderSize) < 0x800);
                    continue;
                }

                jmp->idInsOpt(INS_OPTS_JALR);
                jmp->idCodeSize(jmp->idCodeSize() + extra);
                jmpIG->igSize += (unsigned short)extra; // the placeholder sizeof(INS_OPTS_JALR) - sizeof(INS_OPTS_J).
                adjSJ += (UNATIVE_OFFSET)extra;
                adjIG += (UNATIVE_OFFSET)extra;
                emitTotalCodeSize += (UNATIVE_OFFSET)extra;
                jmpIG->igFlags |= IGF_UPD_ISZ;
                isLinkingEnd |= 0x1;
            }
            continue;
        }
    } // end for each jump

    if ((isLinkingEnd & 0x3) < 0x2)
    {
        // indicating the instrDescJmp's size of the type INS_OPTS_J had updated
        // after the first round and should iterate again to update.
        isLinkingEnd = 0x2;

        // Adjust offsets of any remaining blocks.
        for (; lstIG;)
        {
            lstIG = lstIG->igNext;
            if (!lstIG)
            {
                break;
            }
#ifdef DEBUG
            if (EMITVERBOSE)
            {
                printf("Adjusted offset of " FMT_BB " from %04X to %04X\n", lstIG->igNum, lstIG->igOffs,
                       lstIG->igOffs + adjIG);
            }
#endif // DEBUG

            lstIG->igOffs += adjIG;

            assert(IsCodeAligned(lstIG->igOffs));
        }
        goto AGAIN;
    }

#ifdef DEBUG
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nLabels list after the jump distance binding:\n\n");
        emitDispIGlist(false);
    }

    emitCheckIGList();
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 instruction
 */

unsigned emitter::emitOutput_Instr(BYTE* dst, code_t code) const
{
    assert(sizeof(code_t) == 4);
    memcpy(dst + writeableOffset, &code, sizeof(code_t));
    return sizeof(code_t);
}

static inline void assertCodeLength(unsigned code, uint8_t size)
{
    assert((code >> size) == 0);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 R-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  R-Type layout:
 *  31-------25-24---20-19--15-14------12-11-----------7-6------------0
 *  | funct7   |  rs2  | rs1  |  funct3  |      rd      |   opcode    |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeRTypeInstr(
    unsigned opcode, unsigned rd, unsigned funct3, unsigned rs1, unsigned rs2, unsigned funct7)
{
    assertCodeLength(opcode, 7);
    assertCodeLength(rd, 5);
    assertCodeLength(funct3, 3);
    assertCodeLength(rs1, 5);
    assertCodeLength(rs2, 5);
    assertCodeLength(funct7, 7);

    return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 I-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  I-Type layout:
 *  31------------20-19-----15-14------12-11-----------7-6------------0
 *  |   imm[11:0]   |   rs1   |  funct3  |      rd      |   opcode    |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeITypeInstr(
    unsigned opcode, unsigned rd, unsigned funct3, unsigned rs1, unsigned imm12)
{
    assertCodeLength(opcode, 7);
    assertCodeLength(rd, 5);
    assertCodeLength(funct3, 3);
    assertCodeLength(rs1, 5);
    // This assert may be triggered by the untrimmed signed integers. Please refer to the TrimSigned helpers
    assertCodeLength(imm12, 12);

    return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | (imm12 << 20);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 S-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  S-Type layout:
 *  31-------25-24---20-19--15-14------12-11-----------7-6------------0
 *  |imm[11:5] |  rs2  | rs1  |  funct3  |   imm[4:0]   |   opcode    |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeSTypeInstr(
    unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, unsigned imm12)
{
    static constexpr unsigned kLoMask = 0x1f; // 0b00011111
    static constexpr unsigned kHiMask = 0x7f; // 0b01111111

    assertCodeLength(opcode, 7);
    assertCodeLength(funct3, 3);
    assertCodeLength(rs1, 5);
    assertCodeLength(rs2, 5);
    // This assert may be triggered by the untrimmed signed integers. Please refer to the TrimSigned helpers
    assertCodeLength(imm12, 12);

    unsigned imm12Lo = imm12 & kLoMask;
    unsigned imm12Hi = (imm12 >> 5) & kHiMask;

    return opcode | (imm12Lo << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (imm12Hi << 25);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 U-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  U-Type layout:
 *  31---------------------------------12-11-----------7-6------------0
 *  |             imm[31:12]             |      rd      |   opcode    |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeUTypeInstr(unsigned opcode, unsigned rd, unsigned imm20)
{
    assertCodeLength(opcode, 7);
    assertCodeLength(rd, 5);
    // This assert may be triggered by the untrimmed signed integers. Please refer to the TrimSigned helpers
    assertCodeLength(imm20, 20);

    return opcode | (rd << 7) | (imm20 << 12);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 B-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  B-Type layout:
 *  31-------30-----25-24-20-19-15-14--12-11-------8----7----6--------0
 *  |imm[12]|imm[10:5]| rs2 | rs1 |funct3|  imm[4:1]|imm[11]| opcode  |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeBTypeInstr(
    unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, unsigned imm13)
{
    static constexpr unsigned kLoSectionMask = 0x0f; // 0b00001111
    static constexpr unsigned kHiSectionMask = 0x3f; // 0b00111111
    static constexpr unsigned kBitMask       = 0x01;

    assertCodeLength(opcode, 7);
    assertCodeLength(funct3, 3);
    assertCodeLength(rs1, 5);
    assertCodeLength(rs2, 5);
    // This assert may be triggered by the untrimmed signed integers. Please refer to the TrimSigned helpers
    assertCodeLength(imm13, 13);
    assert((imm13 & 0x01) == 0);

    unsigned imm12          = imm13 >> 1;
    unsigned imm12LoSection = imm12 & kLoSectionMask;
    unsigned imm12LoBit     = (imm12 >> 10) & kBitMask;
    unsigned imm12HiSection = (imm12 >> 4) & kHiSectionMask;
    unsigned imm12HiBit     = (imm12 >> 11) & kBitMask;

    return opcode | (imm12LoBit << 7) | (imm12LoSection << 8) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) |
           (imm12HiSection << 25) | (imm12HiBit << 31);
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 J-Type instruction
 *
 *  Note: Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
 *  J-Type layout:
 *  31-------30--------21----20---19----------12-11----7-6------------0
 *  |imm[20]| imm[10:1]  |imm[11]|  imm[19:12]  |  rd   |   opcode    |
 *  -------------------------------------------------------------------
 */

/*static*/ emitter::code_t emitter::insEncodeJTypeInstr(unsigned opcode, unsigned rd, unsigned imm21)
{
    static constexpr unsigned kHiSectionMask = 0x3ff; // 0b1111111111
    static constexpr unsigned kLoSectionMask = 0xff;  // 0b11111111
    static constexpr unsigned kBitMask       = 0x01;

    assertCodeLength(opcode, 7);
    assertCodeLength(rd, 5);
    // This assert may be triggered by the untrimmed signed integers. Please refer to the TrimSigned helpers
    assertCodeLength(imm21, 21);
    assert((imm21 & 0x01) == 0);

    unsigned imm20          = imm21 >> 1;
    unsigned imm20HiSection = imm20 & kHiSectionMask;
    unsigned imm20HiBit     = (imm20 >> 19) & kBitMask;
    unsigned imm20LoSection = (imm20 >> 11) & kLoSectionMask;
    unsigned imm20LoBit     = (imm20 >> 10) & kBitMask;

    return opcode | (rd << 7) | (imm20LoSection << 12) | (imm20LoBit << 20) | (imm20HiSection << 21) |
           (imm20HiBit << 31);
}

static constexpr unsigned kInstructionOpcodeMask = 0x7f;
static constexpr unsigned kInstructionFunct3Mask = 0x7000;
static constexpr unsigned kInstructionFunct5Mask = 0xf8000000;
static constexpr unsigned kInstructionFunct7Mask = 0xfe000000;
static constexpr unsigned kInstructionFunct2Mask = 0x06000000;

#ifdef DEBUG

/*static*/ void emitter::emitOutput_RTypeInstr_SanityCheck(instruction ins, regNumber rd, regNumber rs1, regNumber rs2)
{
    switch (ins)
    {
        case INS_add:
        case INS_sub:
        case INS_sll:
        case INS_slt:
        case INS_sltu:
        case INS_xor:
        case INS_srl:
        case INS_sra:
        case INS_or:
        case INS_and:
        case INS_addw:
        case INS_subw:
        case INS_sllw:
        case INS_srlw:
        case INS_sraw:
        case INS_mul:
        case INS_mulh:
        case INS_mulhsu:
        case INS_mulhu:
        case INS_div:
        case INS_divu:
        case INS_rem:
        case INS_remu:
        case INS_mulw:
        case INS_divw:
        case INS_divuw:
        case INS_remw:
        case INS_remuw:
            assert(isGeneralRegisterOrR0(rd));
            assert(isGeneralRegisterOrR0(rs1));
            assert(isGeneralRegisterOrR0(rs2));
            break;
        case INS_fsgnj_s:
        case INS_fsgnjn_s:
        case INS_fsgnjx_s:
        case INS_fmin_s:
        case INS_fmax_s:
        case INS_fsgnj_d:
        case INS_fsgnjn_d:
        case INS_fsgnjx_d:
        case INS_fmin_d:
        case INS_fmax_d:
            assert(isFloatReg(rd));
            assert(isFloatReg(rs1));
            assert(isFloatReg(rs2));
            break;
        case INS_feq_s:
        case INS_feq_d:
        case INS_flt_d:
        case INS_flt_s:
        case INS_fle_s:
        case INS_fle_d:
            assert(isGeneralRegisterOrR0(rd));
            assert(isFloatReg(rs1));
            assert(isFloatReg(rs2));
            break;
        case INS_fmv_w_x:
        case INS_fmv_d_x:
            assert(isFloatReg(rd));
            assert(isGeneralRegisterOrR0(rs1));
            assert(rs2 == 0);
            break;
        case INS_fmv_x_d:
        case INS_fmv_x_w:
        case INS_fclass_s:
        case INS_fclass_d:
            assert(isGeneralRegisterOrR0(rd));
            assert(isFloatReg(rs1));
            assert(rs2 == 0);
            break;
        default:
            NO_WAY("Illegal ins within emitOutput_RTypeInstr!");
            break;
    }
}

/*static*/ void emitter::emitOutput_ITypeInstr_SanityCheck(
    instruction ins, regNumber rd, regNumber rs1, unsigned immediate, unsigned opcode)
{
    switch (ins)
    {
        case INS_mov:
        case INS_jalr:
        case INS_lb:
        case INS_lh:
        case INS_lw:
        case INS_lbu:
        case INS_lhu:
        case INS_addi:
        case INS_slti:
        case INS_sltiu:
        case INS_xori:
        case INS_ori:
        case INS_andi:
        case INS_lwu:
        case INS_ld:
        case INS_addiw:
        case INS_fence_i:
        case INS_csrrw:
        case INS_csrrs:
        case INS_csrrc:
            assert(isGeneralRegisterOrR0(rd));
            assert(isGeneralRegisterOrR0(rs1));
            assert((opcode & kInstructionFunct7Mask) == 0);
            break;
        case INS_flw:
        case INS_fld:
            assert(isFloatReg(rd));
            assert(isGeneralRegisterOrR0(rs1));
            assert((opcode & kInstructionFunct7Mask) == 0);
            break;
        case INS_slli:
        case INS_srli:
        case INS_srai:
            assert(immediate < 64);
            assert(isGeneralRegisterOrR0(rd));
            assert(isGeneralRegisterOrR0(rs1));
            break;
        case INS_slliw:
        case INS_srliw:
        case INS_sraiw:
            assert(immediate < 32);
            assert(isGeneralRegisterOrR0(rd));
            assert(isGeneralRegisterOrR0(rs1));
            break;
        case INS_csrrwi:
        case INS_csrrsi:
        case INS_csrrci:
            assert(isGeneralRegisterOrR0(rd));
            assert(rs1 < 32);
            assert((opcode & kInstructionFunct7Mask) == 0);
            break;
        case INS_fence:
        {
            assert(rd == REG_ZERO);
            assert(rs1 == REG_ZERO);
            ssize_t format = immediate >> 8;
            assertCodeLength(format, 3);
        }
        break;
        default:
            NO_WAY("Illegal ins within emitOutput_ITypeInstr!");
            break;
    }
}

/*static*/ void emitter::emitOutput_STypeInstr_SanityCheck(instruction ins, regNumber rs1, regNumber rs2)
{
    switch (ins)
    {
        case INS_sb:
        case INS_sh:
        case INS_sw:
        case INS_sd:
            assert(isGeneralRegister(rs1));
            assert(isGeneralRegisterOrR0(rs2));
            break;
        case INS_fsw:
        case INS_fsd:
            assert(isGeneralRegister(rs1));
            assert(isFloatReg(rs2));
            break;
        default:
            NO_WAY("Illegal ins within emitOutput_STypeInstr!");
            break;
    }
}

/*static*/ void emitter::emitOutput_UTypeInstr_SanityCheck(instruction ins, regNumber rd)
{
    switch (ins)
    {
        case INS_lui:
        case INS_auipc:
            assert(isGeneralRegisterOrR0(rd));
            break;
        default:
            NO_WAY("Illegal ins within emitOutput_UTypeInstr!");
            break;
    }
}

/*static*/ void emitter::emitOutput_BTypeInstr_SanityCheck(instruction ins, regNumber rs1, regNumber rs2)
{
    switch (ins)
    {
        case INS_beqz:
        case INS_bnez:
            assert((rs1 == REG_ZERO) || (rs2 == REG_ZERO));
            FALLTHROUGH;
        case INS_beq:
        case INS_bne:
        case INS_blt:
        case INS_bge:
        case INS_bltu:
        case INS_bgeu:
            assert(isGeneralRegisterOrR0(rs1));
            assert(isGeneralRegisterOrR0(rs2));
            break;
        default:
            NO_WAY("Illegal ins within emitOutput_BTypeInstr!");
            break;
    }
}

/*static*/ void emitter::emitOutput_JTypeInstr_SanityCheck(instruction ins, regNumber rd)
{
    switch (ins)
    {
        case INS_j:
            assert(rd == REG_ZERO);
            break;
        case INS_jal:
            assert(isGeneralRegisterOrR0(rd));
            break;
        default:
            NO_WAY("Illegal ins within emitOutput_JTypeInstr!");
            break;
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Casts an integral or float register from their identification number to
 *  theirs binary format. In case of the integral registers the encoded number
 *  is the register id. In case of the floating point registers the encoded
 *  number is shifted back by the floating point register base (32) (The
 *  instruction itself specifies whether the register contains floating
 *  point or integer, in their encoding they are indistinguishable)
 *
 */

/*static*/ unsigned emitter::castFloatOrIntegralReg(regNumber reg)
{
    static constexpr unsigned kRegisterMask = 0x1f;

    assert(isGeneralRegisterOrR0(reg) || isFloatReg(reg));

    return reg & kRegisterMask;
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 R-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_RTypeInstr(BYTE* dst, instruction ins, regNumber rd, regNumber rs1, regNumber rs2) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_RTypeInstr_SanityCheck(ins, rd, rs1, rs2);
#endif // DEBUG
    unsigned opcode = insCode & kInstructionOpcodeMask;
    unsigned funct3 = (insCode & kInstructionFunct3Mask) >> 12;
    unsigned funct7 = (insCode & kInstructionFunct7Mask) >> 25;
    return emitOutput_Instr(dst, insEncodeRTypeInstr(opcode, castFloatOrIntegralReg(rd), funct3,
                                                     castFloatOrIntegralReg(rs1), castFloatOrIntegralReg(rs2), funct7));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 I-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_ITypeInstr(BYTE* dst, instruction ins, regNumber rd, regNumber rs1, unsigned imm12) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_ITypeInstr_SanityCheck(ins, rd, rs1, imm12, insCode);
#endif // DEBUG
    unsigned opcode = insCode & kInstructionOpcodeMask;
    unsigned funct3 = (insCode & kInstructionFunct3Mask) >> 12;
    unsigned funct7 = (insCode & kInstructionFunct7Mask) >> 20; // only used by some of the immediate shifts
    return emitOutput_Instr(dst, insEncodeITypeInstr(opcode, castFloatOrIntegralReg(rd), funct3, rs1, imm12 | funct7));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 S-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_STypeInstr(BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm12) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_STypeInstr_SanityCheck(ins, rs1, rs2);
#endif // DEBUG
    unsigned opcode = insCode & kInstructionOpcodeMask;
    unsigned funct3 = (insCode & kInstructionFunct3Mask) >> 12;
    return emitOutput_Instr(dst, insEncodeSTypeInstr(opcode, funct3, rs1, castFloatOrIntegralReg(rs2), imm12));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 U-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_UTypeInstr(BYTE* dst, instruction ins, regNumber rd, unsigned imm20) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_UTypeInstr_SanityCheck(ins, rd);
#endif // DEBUG
    return emitOutput_Instr(dst, insEncodeUTypeInstr(insCode, rd, imm20));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 B-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_BTypeInstr(BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm13) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_BTypeInstr_SanityCheck(ins, rs1, rs2);
#endif // DEBUG
    unsigned opcode = insCode & kInstructionOpcodeMask;
    unsigned funct3 = (insCode & kInstructionFunct3Mask) >> 12;
    return emitOutput_Instr(dst, insEncodeBTypeInstr(opcode, funct3, rs1, rs2, imm13));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 B-Type instruction with inverted comparation to
 *  the given buffer. Returns a length of an encoded instruction opcode
 *
 *  Note: Replaces:
 *      - beqz with bnez and vice versa
 *      - beq with bne and vice versa
 *      - blt with bge and vice versa
 *      - bltu with bgeu and vice versa
 */

unsigned emitter::emitOutput_BTypeInstr_InvertComparation(
    BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm13) const
{
    unsigned insCode = emitInsCode(ins) ^ 0x1000;
#ifdef DEBUG
    emitOutput_BTypeInstr_SanityCheck(ins, rs1, rs2);
#endif // DEBUG
    unsigned opcode = insCode & kInstructionOpcodeMask;
    unsigned funct3 = (insCode & kInstructionFunct3Mask) >> 12;
    return emitOutput_Instr(dst, insEncodeBTypeInstr(opcode, funct3, rs1, rs2, imm13));
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 J-Type instruction to the given buffer. Returns a
 *  length of an encoded instruction opcode
 *
 */

unsigned emitter::emitOutput_JTypeInstr(BYTE* dst, instruction ins, regNumber rd, unsigned imm21) const
{
    unsigned insCode = emitInsCode(ins);
#ifdef DEBUG
    emitOutput_JTypeInstr_SanityCheck(ins, rd);
#endif // JTypeInstructionSanityCheck
    return emitOutput_Instr(dst, insEncodeJTypeInstr(insCode, rd, imm21));
}

void emitter::emitOutputInstrJumpDistanceHelper(const insGroup* ig,
                                                instrDescJmp*   jmp,
                                                UNATIVE_OFFSET& dstOffs,
                                                const BYTE*&    dstAddr) const
{
    if (jmp->idAddr()->iiaHasInstrCount())
    {
        assert(ig != nullptr);
        int      instrCount = jmp->idAddr()->iiaGetInstrCount();
        unsigned insNum     = emitFindInsNum(ig, jmp);
        if (instrCount < 0)
        {
            // Backward branches using instruction count must be within the same instruction group.
            assert(insNum + 1 >= static_cast<unsigned>(-instrCount));
        }
        dstOffs = ig->igOffs + emitFindOffset(ig, insNum + 1 + instrCount);
        dstAddr = emitOffsetToPtr(dstOffs);
        return;
    }
    dstOffs = jmp->idAddr()->iiaIGlabel->igOffs;
    dstAddr = emitOffsetToPtr(dstOffs);
}

/*****************************************************************************
 *
 *  Calculates a current jump instruction distance
 *
 */

ssize_t emitter::emitOutputInstrJumpDistance(const BYTE* src, const insGroup* ig, instrDescJmp* jmp)
{
    UNATIVE_OFFSET srcOffs = emitCurCodeOffs(src);
    const BYTE*    srcAddr = emitOffsetToPtr(srcOffs);

    assert(!jmp->idAddr()->iiaIsJitDataOffset()); // not used by riscv64 impl

    UNATIVE_OFFSET dstOffs{};
    const BYTE*    dstAddr = nullptr;
    emitOutputInstrJumpDistanceHelper(ig, jmp, dstOffs, dstAddr);

    ssize_t distVal = static_cast<ssize_t>(dstAddr - srcAddr);

    if (dstOffs > srcOffs)
    {
        // This is a forward jump

        emitFwdJumps = true;

        // The target offset will be closer by at least 'emitOffsAdj', but only if this
        // jump doesn't cross the hot-cold boundary.
        if (!emitJumpCrossHotColdBoundary(srcOffs, dstOffs))
        {
            distVal -= emitOffsAdj;
            dstOffs -= emitOffsAdj;
        }
        jmp->idjOffs = dstOffs;
        if (jmp->idjOffs != dstOffs)
        {
            IMPL_LIMITATION("Method is too large");
        }
    }
    return distVal;
}

static constexpr size_t NBitMask(uint8_t bits)
{
    return (static_cast<size_t>(1) << bits) - 1;
}

template <uint8_t MaskSize>
static ssize_t LowerNBitsOfWord(ssize_t word)
{
    static_assert(MaskSize < 32, "Given mask size is bigger than the word itself");
    static_assert(MaskSize > 0, "Given mask size cannot be zero");

    static constexpr size_t kMask = NBitMask(MaskSize);

    return word & kMask;
}

template <uint8_t MaskSize>
static ssize_t UpperNBitsOfWord(ssize_t word)
{
    static constexpr size_t kShift = 32 - MaskSize;

    return LowerNBitsOfWord<MaskSize>(word >> kShift);
}

template <uint8_t MaskSize>
static ssize_t UpperNBitsOfWordSignExtend(ssize_t word)
{
    static constexpr unsigned kSignExtend = 1 << (31 - MaskSize);

    return UpperNBitsOfWord<MaskSize>(word + kSignExtend);
}

static ssize_t UpperWordOfDoubleWord(ssize_t immediate)
{
    return immediate >> 32;
}

static ssize_t LowerWordOfDoubleWord(ssize_t immediate)
{
    static constexpr size_t kWordMask = NBitMask(32);

    return immediate & kWordMask;
}

template <uint8_t UpperMaskSize, uint8_t LowerMaskSize>
static ssize_t DoubleWordSignExtend(ssize_t doubleWord)
{
    static constexpr size_t kLowerSignExtend = static_cast<size_t>(1) << (63 - LowerMaskSize);
    static constexpr size_t kUpperSignExtend = static_cast<size_t>(1) << (63 - UpperMaskSize);

    return doubleWord + (kLowerSignExtend | kUpperSignExtend);
}

template <uint8_t UpperMaskSize>
static ssize_t UpperWordOfDoubleWordSingleSignExtend(ssize_t doubleWord)
{
    static constexpr size_t kUpperSignExtend = static_cast<size_t>(1) << (31 - UpperMaskSize);

    return UpperWordOfDoubleWord(doubleWord + kUpperSignExtend);
}

template <uint8_t UpperMaskSize, uint8_t LowerMaskSize>
static ssize_t UpperWordOfDoubleWordDoubleSignExtend(ssize_t doubleWord)
{
    return UpperWordOfDoubleWord(DoubleWordSignExtend<UpperMaskSize, LowerMaskSize>(doubleWord));
}

/*static*/ unsigned emitter::TrimSignedToImm12(int imm12)
{
    assert(isValidSimm12(imm12));

    return static_cast<unsigned>(LowerNBitsOfWord<12>(imm12));
}

/*static*/ unsigned emitter::TrimSignedToImm13(int imm13)
{
    assert(isValidSimm13(imm13));

    return static_cast<unsigned>(LowerNBitsOfWord<13>(imm13));
}

/*static*/ unsigned emitter::TrimSignedToImm20(int imm20)
{
    assert(isValidSimm20(imm20));

    return static_cast<unsigned>(LowerNBitsOfWord<20>(imm20));
}

/*static*/ unsigned emitter::TrimSignedToImm21(int imm21)
{
    assert(isValidSimm21(imm21));

    return static_cast<unsigned>(LowerNBitsOfWord<21>(imm21));
}

BYTE* emitter::emitOutputInstr_OptsReloc(BYTE* dst, const instrDesc* id, instruction* ins)
{
    BYTE* const     dstBase = dst;
    const regNumber reg1    = id->idReg1();

    dst += emitOutput_UTypeInstr(dst, INS_auipc, reg1, 0);

    if (id->idIsCnsReloc())
    {
        *ins = INS_addi;
    }
    else
    {
        assert(id->idIsDspReloc());
        *ins = INS_ld;
    }

    dst += emitOutput_ITypeInstr(dst, *ins, reg1, reg1, 0);

    emitRecordRelocation(dstBase, id->idAddr()->iiaAddr, IMAGE_REL_RISCV64_PC);

    return dst;
}

BYTE* emitter::emitOutputInstr_OptsI(BYTE* dst, const instrDesc* id)
{
    ssize_t         immediate = reinterpret_cast<ssize_t>(id->idAddr()->iiaAddr);
    const regNumber reg1      = id->idReg1();

    switch (id->idCodeSize())
    {
        case 8:
            return emitOutputInstr_OptsI8(dst, id, immediate, reg1);
        case 32:
            return emitOutputInstr_OptsI32(dst, immediate, reg1);
        default:
            break;
    }
    unreached();
    return nullptr;
}

BYTE* emitter::emitOutputInstr_OptsI8(BYTE* dst, const instrDesc* id, ssize_t immediate, regNumber reg1)
{
    if (id->idReg2())
    {
        // special for INT64_MAX or UINT32_MAX
        dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, REG_R0, NBitMask<12>());
        const ssize_t shiftValue = (immediate == INT64_MAX) ? 1 : 32;
        dst += emitOutput_ITypeInstr(dst, INS_srli, reg1, reg1, shiftValue);
    }
    else
    {
        dst += emitOutput_UTypeInstr(dst, INS_lui, reg1, UpperNBitsOfWordSignExtend<20>(immediate));
        dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<12>(immediate));
    }
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsI32(BYTE* dst, ssize_t immediate, regNumber reg1)
{
    const ssize_t upperWord = UpperWordOfDoubleWord(immediate);
    dst += emitOutput_UTypeInstr(dst, INS_lui, reg1, UpperNBitsOfWordSignExtend<20>(upperWord));
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<12>(upperWord));
    const ssize_t lowerWord = LowerWordOfDoubleWord(immediate);
    dst += emitOutput_ITypeInstr(dst, INS_slli, reg1, reg1, 11);
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<11>(lowerWord >> 21));
    dst += emitOutput_ITypeInstr(dst, INS_slli, reg1, reg1, 11);
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<11>(lowerWord >> 10));
    dst += emitOutput_ITypeInstr(dst, INS_slli, reg1, reg1, 10);
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<10>(lowerWord));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsRc(BYTE* dst, const instrDesc* id, instruction* ins)
{
    assert(id->idAddr()->iiaIsJitDataOffset());
    assert(id->idGCref() == GCT_NONE);

    const int dataOffs = id->idAddr()->iiaGetJitDataOffset();
    assert(dataOffs >= 0);

    const ssize_t immediate = emitGetInsSC(id);
    assert((immediate >= 0) && (immediate < 0x4000)); // 0x4000 is arbitrary, currently 'imm' is always 0.

    const unsigned offset = static_cast<unsigned>(dataOffs + immediate);
    assert(offset < emitDataSize());

    *ins                 = id->idIns();
    const regNumber reg1 = id->idReg1();

    if (id->idIsReloc())
    {
        return emitOutputInstr_OptsRcReloc(dst, ins, offset, reg1);
    }
    return emitOutputInstr_OptsRcNoReloc(dst, ins, offset, reg1);
}

BYTE* emitter::emitOutputInstr_OptsRcReloc(BYTE* dst, instruction* ins, unsigned offset, regNumber reg1)
{
    const ssize_t immediate = (emitConsBlock - dst) + offset;
    assert((immediate > 0) && ((immediate & 0x03) == 0));

    const regNumber rsvdReg = codeGen->rsGetRsvdReg();
    dst += emitOutput_UTypeInstr(dst, INS_auipc, rsvdReg, UpperNBitsOfWordSignExtend<20>(immediate));

    const instruction lastIns = *ins;

    if (*ins == INS_jal)
    {
        assert(isGeneralRegister(reg1));
        *ins = lastIns = INS_addi;
    }
    dst += emitOutput_ITypeInstr(dst, lastIns, reg1, rsvdReg, LowerNBitsOfWord<12>(immediate));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsRcNoReloc(BYTE* dst, instruction* ins, unsigned offset, regNumber reg1)
{
    const ssize_t immediate = reinterpret_cast<ssize_t>(emitConsBlock) + offset;
    assertCodeLength(immediate, 40);
    const regNumber rsvdReg = codeGen->rsGetRsvdReg();

    const instruction lastIns = (*ins == INS_jal) ? (*ins = INS_addi) : *ins;
    const UINT32      high = immediate >> 11;

    dst += emitOutput_UTypeInstr(dst, INS_lui, rsvdReg, UpperNBitsOfWordSignExtend<20>(high));
    dst += emitOutput_ITypeInstr(dst, INS_addi, rsvdReg, rsvdReg, LowerNBitsOfWord<12>(high));
    dst += emitOutput_ITypeInstr(dst, INS_slli, rsvdReg, rsvdReg, 11);
    dst += emitOutput_ITypeInstr(dst, lastIns, reg1, rsvdReg, LowerNBitsOfWord<11>(immediate));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsRl(BYTE* dst, instrDesc* id, instruction* ins)
{
    insGroup* targetInsGroup = static_cast<insGroup*>(emitCodeGetCookie(id->idAddr()->iiaBBlabel));
    id->idAddr()->iiaIGlabel = targetInsGroup;

    const regNumber reg1   = id->idReg1();
    const ssize_t   igOffs = targetInsGroup->igOffs;

    if (id->idIsReloc())
    {
        *ins = INS_addi;
        return emitOutputInstr_OptsRlReloc(dst, igOffs, reg1);
    }
    *ins = INS_add;
    return emitOutputInstr_OptsRlNoReloc(dst, igOffs, reg1);
}

BYTE* emitter::emitOutputInstr_OptsRlReloc(BYTE* dst, ssize_t igOffs, regNumber reg1)
{
    const ssize_t immediate = (emitCodeBlock - dst) + igOffs;
    assert((immediate & 0x03) == 0);

    dst += emitOutput_UTypeInstr(dst, INS_auipc, reg1, UpperNBitsOfWordSignExtend<20>(immediate));
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, reg1, LowerNBitsOfWord<12>(immediate));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsRlNoReloc(BYTE* dst, ssize_t igOffs, regNumber reg1)
{
    const ssize_t immediate = reinterpret_cast<ssize_t>(emitCodeBlock) + igOffs;
    assertCodeLength(immediate, 32 + 20);

    const regNumber rsvdReg      = codeGen->rsGetRsvdReg();
    const ssize_t   upperSignExt = UpperWordOfDoubleWordDoubleSignExtend<32, 52>(immediate);

    dst += emitOutput_UTypeInstr(dst, INS_lui, rsvdReg, UpperNBitsOfWordSignExtend<20>(immediate));
    dst += emitOutput_ITypeInstr(dst, INS_addi, rsvdReg, rsvdReg, LowerNBitsOfWord<12>(immediate));
    dst += emitOutput_ITypeInstr(dst, INS_addi, reg1, REG_ZERO, LowerNBitsOfWord<12>(upperSignExt));
    dst += emitOutput_ITypeInstr(dst, INS_slli, reg1, reg1, 32);
    dst += emitOutput_RTypeInstr(dst, INS_add, reg1, reg1, rsvdReg);
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsJalr(BYTE* dst, instrDescJmp* jmp, const insGroup* ig, instruction* ins)
{
    const ssize_t immediate = emitOutputInstrJumpDistance(dst, ig, jmp) - 4;
    assert((immediate & 0x03) == 0);

    *ins = jmp->idIns();
    switch (jmp->idCodeSize())
    {
        case 8:
            return emitOutputInstr_OptsJalr8(dst, jmp, immediate);
        case 24:
            assert(jmp->idInsIs(INS_jal, INS_j));
            return emitOutputInstr_OptsJalr24(dst, immediate);
        case 28:
            return emitOutputInstr_OptsJalr28(dst, jmp, immediate);
        default:
            // case 0 - 4: The original INS_OPTS_JALR: not used by now!!!
            break;
    }
    unreached();
    return nullptr;
}

BYTE* emitter::emitOutputInstr_OptsJalr8(BYTE* dst, const instrDescJmp* jmp, ssize_t immediate)
{
    const regNumber reg2 = jmp->idInsIs(INS_beqz, INS_bnez) ? REG_R0 : jmp->idReg2();

    dst += emitOutput_BTypeInstr_InvertComparation(dst, jmp->idIns(), jmp->idReg1(), reg2, 0x8);
    dst += emitOutput_JTypeInstr(dst, INS_jal, REG_ZERO, TrimSignedToImm21(immediate));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsJalr24(BYTE* dst, ssize_t immediate)
{
    // Make target address with offset, then jump (JALR) with the target address
    immediate -= 2 * 4;
    const ssize_t high = UpperWordOfDoubleWordSingleSignExtend<0>(immediate);

    dst += emitOutput_UTypeInstr(dst, INS_lui, REG_RA, UpperNBitsOfWordSignExtend<20>(high));
    dst += emitOutput_ITypeInstr(dst, INS_addi, REG_RA, REG_RA, LowerNBitsOfWord<12>(high));
    dst += emitOutput_ITypeInstr(dst, INS_slli, REG_RA, REG_RA, 32);

    const regNumber rsvdReg = codeGen->rsGetRsvdReg();
    const ssize_t   low     = LowerWordOfDoubleWord(immediate);

    dst += emitOutput_UTypeInstr(dst, INS_auipc, rsvdReg, UpperNBitsOfWordSignExtend<20>(low));
    dst += emitOutput_RTypeInstr(dst, INS_add, rsvdReg, REG_RA, rsvdReg);
    dst += emitOutput_ITypeInstr(dst, INS_jalr, REG_RA, rsvdReg, LowerNBitsOfWord<12>(low));

    return dst;
}

BYTE* emitter::emitOutputInstr_OptsJalr28(BYTE* dst, const instrDescJmp* jmp, ssize_t immediate)
{
    regNumber reg2 = jmp->idInsIs(INS_beqz, INS_bnez) ? REG_R0 : jmp->idReg2();

    dst += emitOutput_BTypeInstr_InvertComparation(dst, jmp->idIns(), jmp->idReg1(), reg2, 0x1c);

    return emitOutputInstr_OptsJalr24(dst, immediate);
}

BYTE* emitter::emitOutputInstr_OptsJCond(BYTE* dst, instrDesc* id, const insGroup* ig, instruction* ins)
{
    const ssize_t immediate = emitOutputInstrJumpDistance(dst, ig, static_cast<instrDescJmp*>(id));

    *ins = id->idIns();

    dst += emitOutput_BTypeInstr(dst, *ins, id->idReg1(), id->idReg2(), TrimSignedToImm13(immediate));
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsJ(BYTE* dst, instrDesc* id, const insGroup* ig, instruction* ins)
{
    const ssize_t immediate = emitOutputInstrJumpDistance(dst, ig, static_cast<instrDescJmp*>(id));
    assert((immediate & 0x03) == 0);

    *ins = id->idIns();

    switch (*ins)
    {
        case INS_jal:
            dst += emitOutput_JTypeInstr(dst, INS_jal, REG_RA, TrimSignedToImm21(immediate));
            break;
        case INS_j:
            dst += emitOutput_JTypeInstr(dst, INS_j, REG_ZERO, TrimSignedToImm21(immediate));
            break;
        case INS_jalr:
            dst += emitOutput_ITypeInstr(dst, INS_jalr, id->idReg1(), id->idReg2(), TrimSignedToImm12(immediate));
            break;
        case INS_bnez:
        case INS_beqz:
            dst += emitOutput_BTypeInstr(dst, *ins, id->idReg1(), REG_ZERO, TrimSignedToImm13(immediate));
            break;
        case INS_beq:
        case INS_bne:
        case INS_blt:
        case INS_bge:
        case INS_bltu:
        case INS_bgeu:
            dst += emitOutput_BTypeInstr(dst, *ins, id->idReg1(), id->idReg2(), TrimSignedToImm13(immediate));
            break;
        default:
            unreached();
            break;
    }
    return dst;
}

BYTE* emitter::emitOutputInstr_OptsC(BYTE* dst, instrDesc* id, const insGroup* ig, size_t* size)
{
    if (id->idIsLargeCall())
    {
        *size = sizeof(instrDescCGCA);
    }
    else
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCns());
        *size = sizeof(instrDesc);
    }
    dst += emitOutputCall(ig, dst, id, 0);
    return dst;
}

/*****************************************************************************
 *
 *  Append the machine code corresponding to the given instruction descriptor
 *  to the code block at '*dp'; the base of the code block is 'bp', and 'ig'
 *  is the instruction group that contains the instruction. Updates '*dp' to
 *  point past the generated code, and returns the size of the instruction
 *  descriptor in bytes.
 */

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    BYTE*             dst  = *dp;
    BYTE*             dst2 = dst + 4;
    const BYTE* const odst = *dp;
    instruction       ins;
    size_t            sz = 0;

    assert(REG_NA == static_cast<int>(REG_NA));
    assert(writeableOffset == 0);

    insOpts insOp = id->idInsOpt();

    switch (insOp)
    {
        case INS_OPTS_RELOC:
            dst = emitOutputInstr_OptsReloc(dst, id, &ins);
            sz  = sizeof(instrDesc);
            break;
        case INS_OPTS_I:
            dst = emitOutputInstr_OptsI(dst, id);
            ins = INS_addi;
            sz  = sizeof(instrDesc);
            break;
        case INS_OPTS_RC:
            dst = emitOutputInstr_OptsRc(dst, id, &ins);
            sz  = sizeof(instrDesc);
            break;
        case INS_OPTS_RL:
            dst = emitOutputInstr_OptsRl(dst, id, &ins);
            sz  = sizeof(instrDesc);
            break;
        case INS_OPTS_JALR:
            dst = emitOutputInstr_OptsJalr(dst, static_cast<instrDescJmp*>(id), ig, &ins);
            sz  = sizeof(instrDescJmp);
            break;
        case INS_OPTS_J_cond:
            dst = emitOutputInstr_OptsJCond(dst, id, ig, &ins);
            sz  = sizeof(instrDescJmp);
            break;
        case INS_OPTS_J:
            // jal/j/jalr/bnez/beqz/beq/bne/blt/bge/bltu/bgeu dstRW-relative.
            dst = emitOutputInstr_OptsJ(dst, id, ig, &ins);
            sz  = sizeof(instrDescJmp);
            break;
        case INS_OPTS_C:
            dst  = emitOutputInstr_OptsC(dst, id, ig, &sz);
            dst2 = dst;
            ins  = INS_nop;
            break;
        default: // case INS_OPTS_NONE:
            dst += emitOutput_Instr(dst, id->idAddr()->iiaGetInstrEncode());
            ins = id->idIns();
            sz  = sizeof(instrDesc);
            break;
    }

    // Determine if any registers now hold GC refs, or whether a register that was overwritten held a GC ref.
    // We assume here that "id->idGCref()" is not GC_NONE only if the instruction described by "id" writes a
    // GC ref to register "id->idReg1()".  (It may, apparently, also not be GC_NONE in other cases, such as
    // for stores, but we ignore those cases here.)
    if (emitInsMayWriteToGCReg(ins)) // True if "id->idIns()" writes to a register than can hold GC ref.
    {
        // We assume that "idReg1" is the primary destination register for all instructions
        if (id->idGCref() != GCT_NONE)
        {
            emitGCregLiveUpd(id->idGCref(), id->idReg1(), dst2);
        }
        else
        {
            emitGCregDeadUpd(id->idReg1(), dst2);
        }
    }

    // Now we determine if the instruction has written to a (local variable) stack location, and either written a GC
    // ref or overwritten one.
    if (emitInsWritesToLclVarStackLoc(id) /*|| emitInsWritesToLclVarStackLocPair(id)*/)
    {
        int      varNum = id->idAddr()->iiaLclVar.lvaVarNum();
        unsigned ofs    = AlignDown(id->idAddr()->iiaLclVar.lvaOffset(), TARGET_POINTER_SIZE);
        bool     FPbased;
        int      adr = emitComp->lvaFrameAddress(varNum, &FPbased);
        if (id->idGCref() != GCT_NONE)
        {
            emitGCvarLiveUpd(adr + ofs, varNum, id->idGCref(), dst2 DEBUG_ARG(varNum));
        }
        else
        {
            // If the type of the local is a gc ref type, update the liveness.
            var_types vt;
            if (varNum >= 0)
            {
                // "Regular" (non-spill-temp) local.
                vt = var_types(emitComp->lvaTable[varNum].lvType);
            }
            else
            {
                TempDsc* tmpDsc = codeGen->regSet.tmpFindNum(varNum);
                vt              = tmpDsc->tdTempType();
            }
            if (vt == TYP_REF || vt == TYP_BYREF)
                emitGCvarDeadUpd(adr + ofs, dst2 DEBUG_ARG(varNum));
        }
        // if (emitInsWritesToLclVarStackLocPair(id))
        //{
        //    unsigned ofs2 = ofs + TARGET_POINTER_SIZE;
        //    if (id->idGCrefReg2() != GCT_NONE)
        //    {
        //        emitGCvarLiveUpd(adr + ofs2, varNum, id->idGCrefReg2(), *dp);
        //    }
        //    else
        //    {
        //        // If the type of the local is a gc ref type, update the liveness.
        //        var_types vt;
        //        if (varNum >= 0)
        //        {
        //            // "Regular" (non-spill-temp) local.
        //            vt = var_types(emitComp->lvaTable[varNum].lvType);
        //        }
        //        else
        //        {
        //            TempDsc* tmpDsc = codeGen->regSet.tmpFindNum(varNum);
        //            vt              = tmpDsc->tdTempType();
        //        }
        //        if (vt == TYP_REF || vt == TYP_BYREF)
        //            emitGCvarDeadUpd(adr + ofs2, *dp);
        //    }
        //}
    }

#ifdef DEBUG
    /* Make sure we set the instruction descriptor size correctly */

    if (emitComp->opts.disAsm || emitComp->verbose)
    {
#if DUMP_GC_TABLES
        bool dspOffs = emitComp->opts.dspGCtbls;
#else  // !DUMP_GC_TABLES
        bool dspOffs = !emitComp->opts.disDiffable;
#endif // !DUMP_GC_TABLES
        emitDispIns(id, false, dspOffs, true, emitCurCodeOffs(odst), *dp, (dst - odst), ig);
    }

    if (emitComp->compDebugBreak)
    {
        // For example, set JitBreakEmitOutputInstr=a6 will break when this method is called for
        // emitting instruction a6, (i.e. IN00a6 in jitdump).
        if ((unsigned)JitConfig.JitBreakEmitOutputInstr() == id->idDebugOnlyInfo()->idNum)
        {
            assert(!"JitBreakEmitOutputInstr reached");
        }
    }
#else  // !DEBUG
    if (emitComp->opts.disAsm)
    {
        emitDispIns(id, false, false, true, emitCurCodeOffs(odst), *dp, (dst - odst), ig);
    }
#endif // !DEBUG

    /* All instructions are expected to generate code */

    assert(*dp != dst);

    *dp = dst;

    return sz;
}

bool emitter::emitDispBranchInstrType(unsigned opcode2) const
{
    switch (opcode2)
    {
        case 0:
            printf("beq ");
            break;
        case 1:
            printf("bne ");
            break;
        case 4:
            printf("blt ");
            break;
        case 5:
            printf("bge ");
            break;
        case 6:
            printf("bltu");
            break;
        case 7:
            printf("bgeu");
            break;
        default:
            return false;
    }
    return true;
}

void emitter::emitDispBranchOffset(const instrDesc* id, const insGroup* ig) const
{
    int instrCount = id->idAddr()->iiaGetInstrCount();
    if (ig == nullptr)
    {
        printf("pc%+d instructions", instrCount);
        return;
    }
    unsigned insNum = emitFindInsNum(ig, id);

    if (ig->igInsCnt < insNum + 1 + instrCount)
    {
        // TODO-RISCV64-BUG: This should be a labeled offset but does not contain an iiaIGlabel
        printf("pc%+d instructions", instrCount);
        return;
    }

    UNATIVE_OFFSET srcOffs = ig->igOffs + emitFindOffset(ig, insNum + 1);
    UNATIVE_OFFSET dstOffs = ig->igOffs + emitFindOffset(ig, insNum + 1 + instrCount);
    ssize_t        relOffs = static_cast<ssize_t>(emitOffsetToPtr(dstOffs) - emitOffsetToPtr(srcOffs));
    printf("pc%+d (%d instructions)", static_cast<int>(relOffs), instrCount);
}

void emitter::emitDispBranchLabel(const instrDesc* id) const
{
    if (id->idIsBound())
    {
        return emitPrintLabel(id->idAddr()->iiaIGlabel);
    }
    printf("L_M%03u_", FMT_BB, emitComp->compMethodID, id->idAddr()->iiaBBlabel->bbNum);
}

bool emitter::emitDispBranch(unsigned         opcode2,
                             const char*      register1Name,
                             const char*      register2Name,
                             const instrDesc* id,
                             const insGroup*  ig) const
{
    if (!emitDispBranchInstrType(opcode2))
    {
        return false;
    }
    printf("           %s, %s, ", register1Name, register2Name);
    assert(id != nullptr);
    if (id->idAddr()->iiaHasInstrCount())
    {
        // Branch is jumping to some non-labeled offset
        emitDispBranchOffset(id, ig);
    }
    else
    {
        // Branch is jumping to the labeled offset
        emitDispBranchLabel(id);
    }
    printf("\n");
    return true;
}

void emitter::emitDispIllegalInstruction(code_t instructionCode)
{
    printf("RISCV64 illegal instruction: 0x%08X\n", instructionCode);
}

/*****************************************************************************/
/*****************************************************************************/

// clang-format off
static const char* const RegNames[] =
{
    #define REGDEF(name, rnum, mask, sname) sname,
    #include "register.h"
};
// clang-format on

//----------------------------------------------------------------------------------------
// Disassemble the given instruction.
// The `emitter::emitDispInsName` is focused on the most important for debugging.
// So it implemented as far as simply and independently which is very useful for
// porting easily to the release mode.
//
// Arguments:
//    code - The instruction's encoding.
//    addr - The address of the code.
//    doffs - Flag informing whether the instruction's offset should be displayed.
//    insOffset - The instruction's offset.
//    id   - The instrDesc of the code if needed.
//    ig   - The insGroup of the code if needed
//
// Note:
//    The length of the instruction's name include aligned space is 15.
//

void emitter::emitDispInsName(
    code_t code, const BYTE* addr, bool doffs, unsigned insOffset, const instrDesc* id, const insGroup* ig)
{
    const BYTE* insAdr = addr - writeableOffset;

    unsigned int opcode = code & 0x7f;
    assert((opcode & 0x3) == 0x3);

    emitDispInsAddr(insAdr);
    emitDispInsOffs(insOffset, doffs);

    printf("      ");

    switch (opcode)
    {
        case 0x37: // LUI
        {
            const char* rd    = RegNames[(code >> 7) & 0x1f];
            int         imm20 = (code >> 12) & 0xfffff;
            if (imm20 & 0x80000)
            {
                imm20 |= 0xfff00000;
            }
            printf("lui            %s, %d\n", rd, imm20);
            return;
        }
        case 0x17: // AUIPC
        {
            const char* rd    = RegNames[(code >> 7) & 0x1f];
            int         imm20 = (code >> 12) & 0xfffff;
            if (imm20 & 0x80000)
            {
                imm20 |= 0xfff00000;
            }
            printf("auipc          %s, %d\n", rd, imm20);
            return;
        }
        case 0x13:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rd      = RegNames[(code >> 7) & 0x1f];
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            int          imm12   = (((int)code) >> 20); // & 0xfff;
            // if (imm12 & 0x800)
            //{
            //    imm12 |= 0xfffff000;
            //}
            switch (opcode2)
            {
                case 0x0: // ADDI
                    printf("addi           %s, %s, %d\n", rd, rs1, imm12);
                    return;
                case 0x1:                                                         // SLLI
                    printf("slli           %s, %s, %d\n", rd, rs1, imm12 & 0x3f); // 6 BITS for SHAMT in RISCV64
                    return;
                case 0x2: // SLTI
                    printf("slti           %s, %s, %d\n", rd, rs1, imm12);
                    return;
                case 0x3: // SLTIU
                    printf("sltiu          %s, %s, %d\n", rd, rs1, imm12);
                    return;
                case 0x4: // XORI
                    printf("xori           %s, %s, 0x%x\n", rd, rs1, imm12);
                    return;
                case 0x5: // SRLI & SRAI
                    if (((code >> 30) & 0x1) == 0)
                    {
                        printf("srli           %s, %s, %d\n", rd, rs1, imm12 & 0x3f); // 6BITS for SHAMT in RISCV64
                    }
                    else
                    {
                        printf("srai           %s, %s, %d\n", rd, rs1, imm12 & 0x3f); // 6BITS for SHAMT in RISCV64
                    }
                    return;
                case 0x6: // ORI
                    printf("ori            %s, %s, 0x%x\n", rd, rs1, imm12 & 0xfff);
                    return;
                case 0x7: // ANDI
                    printf("andi           %s, %s, 0x%x\n", rd, rs1, imm12 & 0xfff);
                    return;
                default:
                    printf("RISCV64 illegal instruction: 0x%08X\n", code);
                    return;
            }
        }
        case 0x1b:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rd      = RegNames[(code >> 7) & 0x1f];
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            int          imm12   = (((int)code) >> 20); // & 0xfff;
            // if (imm12 & 0x800)
            //{
            //    imm12 |= 0xfffff000;
            //}
            switch (opcode2)
            {
                case 0x0: // ADDIW
                    printf("addiw          %s, %s, %d\n", rd, rs1, imm12);
                    return;
                case 0x1:                                                         // SLLIW
                    printf("slliw          %s, %s, %d\n", rd, rs1, imm12 & 0x3f); // 6 BITS for SHAMT in RISCV64
                    return;
                case 0x5: // SRLIW & SRAIW
                    if (((code >> 30) & 0x1) == 0)
                    {
                        printf("srliw          %s, %s, %d\n", rd, rs1, imm12 & 0x1f); // 5BITS for SHAMT in RISCV64
                    }
                    else
                    {
                        printf("sraiw          %s, %s, %d\n", rd, rs1, imm12 & 0x1f); // 5BITS for SHAMT in RISCV64
                    }
                    return;
                default:
                    printf("RISCV64 illegal instruction: 0x%08X\n", code);
                    return;
            }
        }
        case 0x33:
        {
            unsigned int opcode2 = (code >> 25) & 0x3;
            unsigned int opcode3 = (code >> 12) & 0x7;
            const char*  rd      = RegNames[(code >> 7) & 0x1f];
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rs2     = RegNames[(code >> 20) & 0x1f];
            if (opcode2 == 0)
            {
                switch (opcode3)
                {
                    case 0x0: // ADD & SUB
                        if (((code >> 30) & 0x1) == 0)
                        {
                            printf("add            %s, %s, %s\n", rd, rs1, rs2);
                        }
                        else
                        {
                            printf("sub            %s, %s, %s\n", rd, rs1, rs2);
                        }
                        return;
                    case 0x1: // SLL
                        printf("sll            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x2: // SLT
                        printf("slt            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x3: // SLTU
                        printf("sltu           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x4: // XOR
                        printf("xor            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x5: // SRL & SRA
                        if (((code >> 30) & 0x1) == 0)
                        {
                            printf("srl            %s, %s, %s\n", rd, rs1, rs2);
                        }
                        else
                        {
                            printf("sra            %s, %s, %s\n", rd, rs1, rs2);
                        }
                        return;
                    case 0x6: // OR
                        printf("or             %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x7: // AND
                        printf("and            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    default:
                        printf("RISCV64 illegal instruction: 0x%08X\n", code);
                        return;
                }
            }
            else if (opcode2 == 0x1)
            {
                switch (opcode3)
                {
                    case 0x0: // MUL
                        printf("mul            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x1: // MULH
                        printf("mulh           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x2: // MULHSU
                        printf("mulhsu         %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x3: // MULHU
                        printf("mulhu          %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x4: // DIV
                        printf("div            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x5: // DIVU
                        printf("divu           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x6: // REM
                        printf("rem            %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x7: // REMU
                        printf("remu           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    default:
                        printf("RISCV64 illegal instruction: 0x%08X\n", code);
                        return;
                }
            }
            else
            {
                printf("RISCV64 illegal instruction: 0x%08X\n", code);
                return;
            }
        }
        case 0x3b:
        {
            unsigned int opcode2 = (code >> 25) & 0x3;
            unsigned int opcode3 = (code >> 12) & 0x7;
            const char*  rd      = RegNames[(code >> 7) & 0x1f];
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rs2     = RegNames[(code >> 20) & 0x1f];

            if (opcode2 == 0)
            {
                switch (opcode3)
                {
                    case 0x0: // ADDW & SUBW
                        if (((code >> 30) & 0x1) == 0)
                        {
                            printf("addw           %s, %s, %s\n", rd, rs1, rs2);
                        }
                        else
                        {
                            printf("subw           %s, %s, %s\n", rd, rs1, rs2);
                        }
                        return;
                    case 0x1: // SLLW
                        printf("sllw           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x5: // SRLW & SRAW
                        if (((code >> 30) & 0x1) == 0)
                        {
                            printf("srlw           %s, %s, %s\n", rd, rs1, rs2);
                        }
                        else
                        {
                            printf("sraw           %s, %s, %s\n", rd, rs1, rs2);
                        }
                        return;
                    default:
                        printf("RISCV64 illegal instruction: 0x%08X\n", code);
                        return;
                }
            }
            else if (opcode2 == 1)
            {
                switch (opcode3)
                {
                    case 0x0: // MULW
                        printf("mulw           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x4: // DIVW
                        printf("divw           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x5: // DIVUW
                        printf("divuw          %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x6: // REMW
                        printf("remw           %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    case 0x7: // REMUW
                        printf("remuw          %s, %s, %s\n", rd, rs1, rs2);
                        return;
                    default:
                        printf("RISCV64 illegal instruction: 0x%08X\n", code);
                        return;
                }
            }
            else
            {
                printf("RISCV64 illegal instruction: 0x%08X\n", code);
                return;
            }
        }
        case 0x23:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rs2     = RegNames[(code >> 20) & 0x1f];
            int          offset  = (((code >> 25) & 0x7f) << 5) | ((code >> 7) & 0x1f);
            if (offset & 0x800)
            {
                offset |= 0xfffff000;
            }

            switch (opcode2)
            {
                case 0: // SB
                    printf("sb             %s, %d(%s)\n", rs2, offset, rs1);
                    return;
                case 1: // SH
                    printf("sh             %s, %d(%s)\n", rs2, offset, rs1);
                    return;
                case 2: // SW
                    printf("sw             %s, %d(%s)\n", rs2, offset, rs1);
                    return;
                case 3: // SD
                    printf("sd             %s, %d(%s)\n", rs2, offset, rs1);
                    return;
                default:
                    printf("RISCV64 illegal instruction: 0x%08X\n", code);
                    return;
            }
        }
        case 0x63: // BRANCH
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rs2     = RegNames[(code >> 20) & 0x1f];
            // int offset = (((code >> 31) & 0x1) << 12) | (((code >> 7) & 0x1) << 11) | (((code >> 25) & 0x3f) << 5) |
            //              (((code >> 8) & 0xf) << 1);
            // if (offset & 0x800)
            // {
            //     offset |= 0xfffff000;
            // }
            if (!emitDispBranch(opcode2, rs1, rs2, id, ig))
            {
                emitDispIllegalInstruction(code);
            }
            return;
        }
        case 0x03:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rd      = RegNames[(code >> 7) & 0x1f];
            int          offset  = ((code >> 20) & 0xfff);
            if (offset & 0x800)
            {
                offset |= 0xfffff000;
            }

            switch (opcode2)
            {
                case 0: // LB
                    printf("lb             %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 1: // LH
                    printf("lh             %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 2: // LW
                    printf("lw             %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 3: // LD
                    printf("ld             %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 4: // LBU
                    printf("lbu            %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 5: // LHU
                    printf("lhu            %s, %d(%s)\n", rd, offset, rs1);
                    return;
                case 6: // LWU
                    printf("lwu            %s, %d(%s)\n", rd, offset, rs1);
                    return;
                default:
                    printf("RISCV64 illegal instruction: 0x%08X\n", code);
                    return;
            }
        }
        case 0x67:
        {
            const char* rs1    = RegNames[(code >> 15) & 0x1f];
            const char* rd     = RegNames[(code >> 7) & 0x1f];
            int         offset = ((code >> 20) & 0xfff);
            if (offset & 0x800)
            {
                offset |= 0xfffff000;
            }
            printf("jalr           %s, %d(%s)", rd, offset, rs1);
            CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie;
            // Target for ret call is unclear, e.g.:
            //   jalr zero, 0(ra)
            // So, skip it
            if (handle != 0)
            {
                const char* methodName = emitComp->eeGetMethodFullName(handle);
                printf("\t\t// %s", methodName);
            }

            printf("\n");
            return;
        }
        case 0x6f:
        {
            const char* rd = RegNames[(code >> 7) & 0x1f];
            int offset = (((code >> 31) & 0x1) << 20) | (((code >> 12) & 0xff) << 12) | (((code >> 20) & 0x1) << 11) |
                         (((code >> 21) & 0x3ff) << 1);
            if (offset & 0x80000)
            {
                offset |= 0xfff00000;
            }
            printf("jal            %s, %d", rd, offset);
            CORINFO_METHOD_HANDLE handle = (CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie;
            if (handle != 0)
            {
                const char* methodName = emitComp->eeGetMethodFullName(handle);
                printf("\t\t// %s", methodName);
            }

            printf("\n");
            return;
        }
        case 0x0f:
        {
            int pred = ((code) >> 24) & 0xf;
            int succ = ((code) >> 20) & 0xf;
            printf("fence          %d, %d\n", pred, succ);
            return;
        }
        case 0x73:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            if (opcode2 != 0)
            {
                const char* rd      = RegNames[(code >> 7) & 0x1f];
                int         csrtype = (code >> 20);
                if (opcode2 <= 0x3)
                {
                    const char* rs1 = RegNames[(code >> 15) & 0x1f];
                    switch (opcode2)
                    {
                        case 0x1: // CSRRW
                            printf("csrrw           %s, %d, %s\n", rd, csrtype, rs1);
                            return;
                        case 0x2: // CSRRS
                            printf("csrrs           %s, %d, %s\n", rd, csrtype, rs1);
                            return;
                        case 0x3: // CSRRC
                            printf("csrrc           %s, %d, %s\n", rd, csrtype, rs1);
                            return;
                        default:
                            printf("RISCV64 illegal instruction: 0x%08X\n", code);
                            break;
                    }
                }
                else
                {
                    unsigned imm5 = ((code >> 15) & 0x1f);
                    switch (opcode2)
                    {
                        case 0x5: // CSRRWI
                            printf("csrrwi           %s, %d, %d\n", rd, csrtype, imm5);
                            return;
                        case 0x6: // CSRRSI
                            printf("csrrsi           %s, %d, %d\n", rd, csrtype, imm5);
                            return;
                        case 0x7: // CSRRCI
                            printf("csrrci           %s, %d, %d\n", rd, csrtype, imm5);
                            return;
                        default:
                            printf("RISCV64 illegal instruction: 0x%08X\n", code);
                            break;
                    }
                }
            }

            if (code == emitInsCode(INS_ebreak))
            {
                printf("ebreak\n");
            }
            else
            {
                NYI_RISCV64("illegal ins within emitDisInsName!");
            }
            return;
        }
        case 0x53:
        {
            unsigned int opcode2 = (code >> 25) & 0x7f;
            unsigned int opcode3 = (code >> 20) & 0x1f;
            unsigned int opcode4 = (code >> 12) & 0x7;
            const char*  fd      = RegNames[((code >> 7) & 0x1f) | 0x20];
            const char*  fs1     = RegNames[((code >> 15) & 0x1f) | 0x20];
            const char*  fs2     = RegNames[((code >> 20) & 0x1f) | 0x20];

            const char* xd  = RegNames[(code >> 7) & 0x1f];
            const char* xs1 = RegNames[(code >> 15) & 0x1f];
            const char* xs2 = RegNames[(code >> 20) & 0x1f];

            switch (opcode2)
            {
                case 0x00: // FADD.S
                    printf("fadd.s         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x04: // FSUB.S
                    printf("fsub.s         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x08: // FMUL.S
                    printf("fmul.s         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x0C: // FDIV.S
                    printf("fdiv.s         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x2C: // FSQRT.S
                    printf("fsqrt.s        %s, %s\n", fd, fs1);
                    return;
                case 0x10:            // FSGNJ.S & FSGNJN.S & FSGNJX.S
                    if (opcode4 == 0) // FSGNJ.S
                    {
                        printf("fsgnj.s        %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FSGNJN.S
                    {
                        printf("fsgnjn.s       %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 2) // FSGNJX.S
                    {
                        printf("fsgnjx.s       %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x14:            // FMIN.S & FMAX.S
                    if (opcode4 == 0) // FMIN.S
                    {
                        printf("fmin.s         %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FMAX.S
                    {
                        printf("fmax.s         %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x60:            // FCVT.W.S & FCVT.WU.S & FCVT.L.S & FCVT.LU.S
                    if (opcode3 == 0) // FCVT.W.S
                    {
                        printf("fcvt.w.s       %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 1) // FCVT.WU.S
                    {
                        printf("fcvt.wu.s      %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 2) // FCVT.L.S
                    {
                        printf("fcvt.l.s       %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 3) // FCVT.LU.S
                    {
                        printf("fcvt.lu.s      %s, %s\n", xd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x70:            // FMV.X.W & FCLASS.S
                    if (opcode4 == 0) // FMV.X.W
                    {
                        printf("fmv.x.w        %s, %s\n", xd, fs1);
                    }
                    else if (opcode4 == 1) // FCLASS.S
                    {
                        printf("fclass.s       %s, %s\n", xd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x50:            // FLE.S & FLT.S & FEQ.S
                    if (opcode4 == 0) // FLE.S
                    {
                        printf("fle.s          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FLT.S
                    {
                        printf("flt.s          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else if (opcode4 == 2) // FEQ.S
                    {
                        printf("feq.s          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x68:            // FCVT.S.W & FCVT.S.WU & FCVT.S.L & FCVT.S.LU
                    if (opcode3 == 0) // FCVT.S.W
                    {
                        printf("fcvt.s.w       %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 1) // FCVT.S.WU
                    {
                        printf("fcvt.s.wu      %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 2) // FCVT.S.L
                    {
                        printf("fcvt.s.l       %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 3) // FCVT.S.LU
                    {
                        printf("fcvt.s.lu      %s, %s\n", fd, xs1);
                    }

                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x78: // FMV.W.X
                    printf("fmv.w.x        %s, %s\n", fd, xs1);
                    return;
                case 0x1: // FADD.D
                    printf("fadd.d         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x5: // FSUB.D
                    printf("fsub.d         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x9: // FMUL.D
                    printf("fmul.d         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0xd: // FDIV.D
                    printf("fdiv.d         %s, %s, %s\n", fd, fs1, fs2);
                    return;
                case 0x2d: // FSQRT.D
                    printf("fsqrt.d        %s, %s\n", fd, fs1);
                    return;
                case 0x11:            // FSGNJ.D & FSGNJN.D & FSGNJX.D
                    if (opcode4 == 0) // FSGNJ.D
                    {
                        printf("fsgnj.d        %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FSGNJN.D
                    {
                        printf("fsgnjn.d       %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 2) // FSGNJX.D
                    {
                        printf("fsgnjx.d       %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x15:            // FMIN.D & FMAX.D
                    if (opcode4 == 0) // FMIN.D
                    {
                        printf("fmin.d         %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FMAX.D
                    {
                        printf("fmax.d         %s, %s, %s\n", fd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x20:            // FCVT.S.D
                    if (opcode3 == 1) // FCVT.S.D
                    {
                        printf("fcvt.s.d       %s, %s\n", fd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x21:            // FCVT.D.S
                    if (opcode3 == 0) // FCVT.D.S
                    {
                        printf("fcvt.d.s       %s, %s\n", fd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x51:            // FLE.D & FLT.D & FEQ.D
                    if (opcode4 == 0) // FLE.D
                    {
                        printf("fle.d          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else if (opcode4 == 1) // FLT.D
                    {
                        printf("flt.d          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else if (opcode4 == 2) // FEQ.D
                    {
                        printf("feq.d          %s, %s, %s\n", xd, fs1, fs2);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x61: // FCVT.W.D & FCVT.WU.D & FCVT.L.D & FCVT.LU.D

                    if (opcode3 == 0) // FCVT.W.D
                    {
                        printf("fcvt.w.d       %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 1) // FCVT.WU.D
                    {
                        printf("fcvt.wu.d      %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 2) // FCVT.L.D
                    {
                        printf("fcvt.l.d       %s, %s\n", xd, fs1);
                    }
                    else if (opcode3 == 3) // FCVT.LU.D
                    {
                        printf("fcvt.lu.d      %s, %s\n", xd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x69:            // FCVT.D.W & FCVT.D.WU & FCVT.D.L & FCVT.D.LU
                    if (opcode3 == 0) // FCVT.D.W
                    {
                        printf("fcvt.d.w       %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 1) // FCVT.D.WU
                    {
                        printf("fcvt.d.wu      %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 2)
                    {
                        printf("fcvt.d.l       %s, %s\n", fd, xs1);
                    }
                    else if (opcode3 == 3)
                    {
                        printf("fcvt.d.lu      %s, %s\n", fd, xs1);
                    }

                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }

                    return;
                case 0x71:            // FMV.X.D & FCLASS.D
                    if (opcode4 == 0) // FMV.X.D
                    {
                        printf("fmv.x.d        %s, %s\n", xd, fs1);
                    }
                    else if (opcode4 == 1) // FCLASS.D
                    {
                        printf("fclass.d       %s, %s\n", xd, fs1);
                    }
                    else
                    {
                        NYI_RISCV64("illegal ins within emitDisInsName!");
                    }
                    return;
                case 0x79: // FMV.D.X
                    assert(opcode4 == 0);
                    printf("fmv.d.x        %s, %s\n", fd, xs1);
                    return;
                default:
                    NYI_RISCV64("illegal ins within emitDisInsName!");
                    return;
            }
            return;
        }
        case 0x27:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;

            const char* rs1    = RegNames[(code >> 15) & 0x1f];
            const char* rs2    = RegNames[((code >> 20) & 0x1f) | 0x20];
            int         offset = (((code >> 25) & 0x7f) << 5) | ((code >> 7) & 0x1f);
            if (offset & 0x800)
            {
                offset |= 0xfffff000;
            }
            if (opcode2 == 2) // FSW
            {
                printf("fsw            %s, %d(%s)\n", rs2, offset, rs1);
            }
            else if (opcode2 == 3) // FSD
            {
                printf("fsd            %s, %d(%s)\n", rs2, offset, rs1);
            }
            else
            {
                NYI_RISCV64("illegal ins within emitDisInsName!");
            }
            return;
        }
        case 0x7:
        {
            unsigned int opcode2 = (code >> 12) & 0x7;
            const char*  rs1     = RegNames[(code >> 15) & 0x1f];
            const char*  rd      = RegNames[((code >> 7) & 0x1f) | 0x20];
            int          offset  = ((code >> 20) & 0xfff);
            if (offset & 0x800)
            {
                offset |= 0xfffff000;
            }
            if (opcode2 == 2) // FLW
            {
                printf("flw            %s, %d(%s)\n", rd, offset, rs1);
            }
            else if (opcode2 == 3) // FLD
            {
                printf("fld            %s, %d(%s)\n", rd, offset, rs1);
            }
            else
            {
                NYI_RISCV64("illegal ins within emitDisInsName!");
            }
            return;
        }
        case 0x2f: // AMO - atomic memory operation
        {
            bool        hasDataReg = true;
            const char* name;
            switch (code >> 27) // funct5
            {
                case 0b00010:
                    name       = "lr";
                    hasDataReg = false;
                    break;
                case 0b00011:
                    name = "sc";
                    break;
                case 0b00001:
                    name = "amoswap";
                    break;
                case 0b00000:
                    name = "amoadd";
                    break;
                case 0b00100:
                    name = "amoxor";
                    break;
                case 0b01100:
                    name = "amoand";
                    break;
                case 0b01000:
                    name = "amoor";
                    break;
                case 0b10000:
                    name = "amomin";
                    break;
                case 0b10100:
                    name = "amomax";
                    break;
                case 0b11000:
                    name = "amominu";
                    break;
                case 0b11100:
                    name = "amomaxu";
                    break;
                default:
                    assert(!"Illegal funct5 within atomic memory operation, emitDisInsName");
                    name = "?";
            }

            char width;
            switch ((code >> 12) & 0x7) // funct3: width
            {
                case 0x2:
                    width = 'w';
                    break;
                case 0x3:
                    width = 'd';
                    break;
                default:
                    assert(!"Illegal width tag within atomic memory operation, emitDisInsName");
                    width = '?';
            }

            const char* aq = code & (1 << 25) ? "aq" : "";
            const char* rl = code & (1 << 26) ? "rl" : "";

            int len = printf("%s.%c.%s%s", name, width, aq, rl);
            if (len <= 0)
            {
                return;
            }
            static const int INS_LEN = 14;
            assert(len <= INS_LEN);

            const char* dest = RegNames[(code >> 7) & 0x1f];
            const char* addr = RegNames[(code >> 15) & 0x1f];

            int dataReg = (code >> 20) & 0x1f;
            if (hasDataReg)
            {
                const char* data = RegNames[dataReg];
                printf("%*s %s, %s, (%s)\n", INS_LEN - len, "", dest, data, addr);
            }
            else
            {
                assert(dataReg == REG_R0);
                printf("%*s %s, (%s)\n", INS_LEN - len, "", dest, addr);
            }
            return;
        }
        default:
            NO_WAY("illegal ins within emitDisInsName!");
    }

    NO_WAY("illegal ins within emitDisInsName!");
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    if (!emitComp->opts.disCodeBytes)
    {
        return;
    }

    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable)
    {
        if (sz == 4)
        {
            printf("  %08X    ", (*((code_t*)code)));
        }
        else
        {
            assert(sz == 0);
            printf("              ");
        }
    }
}

void emitter::emitDispInsInstrNum(const instrDesc* id) const
{
#ifdef DEBUG
    if (!emitComp->verbose)
        return;

    printf("IN%04x: ", id->idDebugOnlyInfo()->idNum);
#endif // DEBUG
}

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    if (pCode == nullptr)
        return;

    emitDispInsInstrNum(id);

    const BYTE* instr = pCode + writeableOffset;
    size_t      instrSize;
    for (size_t i = 0; i < sz; instr += instrSize, i += instrSize, offset += instrSize)
    {
        // TODO-RISCV64: support different size instructions
        instrSize = sizeof(code_t);
        code_t instruction;
        memcpy(&instruction, instr, instrSize);
#ifdef DEBUG
        if (emitComp->verbose && i != 0)
        {
            printf("        ");
        }
#endif
        emitDispInsName(instruction, instr, doffs, offset, id, ig);
    }
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    NYI_RISCV64("emitDispFrameRef-----unimplemented/unused on RISCV64 yet----");
}

#endif // DEBUG

// Generate code for a load or store operation with a potentially complex addressing mode
// This method handles the case of a GT_IND with contained GT_LEA op1 of the x86 form [base + index*sccale + offset]
//
void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    GenTree* addr = indir->Addr();

    if (addr->isContained())
    {
        assert(addr->OperIs(GT_LCL_ADDR, GT_LEA));

        int   offset = 0;
        DWORD lsl    = 0;

        if (addr->OperGet() == GT_LEA)
        {
            offset = addr->AsAddrMode()->Offset();
            if (addr->AsAddrMode()->gtScale > 0)
            {
                assert(isPow2(addr->AsAddrMode()->gtScale));
                BitScanForward(&lsl, addr->AsAddrMode()->gtScale);
            }
        }

        GenTree* memBase = indir->Base();
        emitAttr addType = varTypeIsGC(memBase) ? EA_BYREF : EA_PTRSIZE;

        if (indir->HasIndex())
        {
            GenTree* index = indir->Index();

            if (offset != 0)
            {
                regNumber tmpReg = indir->GetSingleTempReg();

                if (isValidSimm12(offset))
                {
                    if (lsl > 0)
                    {
                        // Generate code to set tmpReg = base + index*scale
                        emitIns_R_R_I(INS_slli, addType, tmpReg, index->GetRegNum(), lsl);
                        emitIns_R_R_R(INS_add, addType, tmpReg, memBase->GetRegNum(), tmpReg);
                    }
                    else // no scale
                    {
                        // Generate code to set tmpReg = base + index
                        emitIns_R_R_R(INS_add, addType, tmpReg, memBase->GetRegNum(), index->GetRegNum());
                    }

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));

                    // Then load/store dataReg from/to [tmpReg + offset]
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, offset);
                }
                else // large offset
                {
                    // First load/store tmpReg with the large offset constant
                    emitLoadImmediate(EA_PTRSIZE, tmpReg,
                                      offset); // codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);
                    // Then add the base register
                    //      rd = rd + base
                    emitIns_R_R_R(INS_add, addType, tmpReg, tmpReg, memBase->GetRegNum());

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));
                    noway_assert(tmpReg != index->GetRegNum());

                    regNumber scaleReg = indir->GetSingleTempReg();
                    // Then load/store dataReg from/to [tmpReg + index*scale]
                    emitIns_R_R_I(INS_slli, addType, scaleReg, index->GetRegNum(), lsl);
                    emitIns_R_R_R(INS_add, addType, tmpReg, tmpReg, scaleReg);
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, 0);
                }
            }
            else // (offset == 0)
            {
                // Then load/store dataReg from/to [memBase + index]
                switch (EA_SIZE(emitTypeSize(indir->TypeGet())))
                {
                    case EA_1BYTE:
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld ||
                               ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd);
                        if (ins <= INS_lhu || ins == INS_lwu || ins == INS_ld)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_lbu;
                            else
                                ins = INS_lb;
                        }
                        else
                            ins = INS_sb;
                        break;
                    case EA_2BYTE:
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld ||
                               ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd);
                        if (ins <= INS_lhu || ins == INS_lwu || ins == INS_ld)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_lhu;
                            else
                                ins = INS_lh;
                        }
                        else
                            ins = INS_sh;
                        break;
                    case EA_4BYTE:
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld ||
                               ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd || ins == INS_fsw ||
                               ins == INS_flw);
                        assert(INS_fsw > INS_sd);
                        if (ins <= INS_lhu || ins == INS_lwu || ins == INS_ld)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_lwu;
                            else
                                ins = INS_lw;
                        }
                        else if (ins != INS_flw && ins != INS_fsw)
                            ins = INS_sw;
                        break;
                    case EA_8BYTE:
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld ||
                               ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd || ins == INS_fld ||
                               ins == INS_fsd);
                        assert(INS_fsd > INS_sd);
                        if (ins <= INS_lhu || ins == INS_lwu || ins == INS_ld)
                        {
                            ins = INS_ld;
                        }
                        else if (ins != INS_fld && ins != INS_fsd)
                            ins = INS_sd;
                        break;
                    default:
                        NO_WAY("illegal ins within emitInsLoadStoreOp!");
                }

                if (lsl > 0)
                {
                    // Then load/store dataReg from/to [memBase + index*scale]
                    emitIns_R_R_I(INS_slli, emitActualTypeSize(index->TypeGet()), codeGen->rsGetRsvdReg(),
                                  index->GetRegNum(), lsl);
                    emitIns_R_R_R(INS_add, addType, codeGen->rsGetRsvdReg(), memBase->GetRegNum(),
                                  codeGen->rsGetRsvdReg());
                    emitIns_R_R_I(ins, attr, dataReg, codeGen->rsGetRsvdReg(), 0);
                }
                else // no scale
                {
                    emitIns_R_R_R(INS_add, addType, codeGen->rsGetRsvdReg(), memBase->GetRegNum(), index->GetRegNum());
                    emitIns_R_R_I(ins, attr, dataReg, codeGen->rsGetRsvdReg(), 0);
                }
            }
        }
        else // no Index register
        {
            if (addr->OperIs(GT_LCL_ADDR))
            {
                GenTreeLclVarCommon* varNode = addr->AsLclVarCommon();
                unsigned             lclNum  = varNode->GetLclNum();
                unsigned             offset  = varNode->GetLclOffs();
                if (emitInsIsStore(ins))
                {
                    emitIns_S_R(ins, attr, dataReg, lclNum, offset);
                }
                else
                {
                    emitIns_R_S(ins, attr, dataReg, lclNum, offset);
                }
            }
            else if (isValidSimm12(offset))
            {
                // Then load/store dataReg from/to [memBase + offset]
                emitIns_R_R_I(ins, attr, dataReg, memBase->GetRegNum(), offset);
            }
            else
            {
                // We require a tmpReg to hold the offset
                regNumber tmpReg = indir->GetSingleTempReg();

                // First load/store tmpReg with the large offset constant
                emitLoadImmediate(EA_PTRSIZE, tmpReg, offset);
                // codeGen->instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                // Then load/store dataReg from/to [memBase + tmpReg]
                emitIns_R_R_R(INS_add, addType, tmpReg, memBase->GetRegNum(), tmpReg);
                emitIns_R_R_I(ins, attr, dataReg, tmpReg, 0);
            }
        }
    }
    else // addr is not contained, so we evaluate it into a register
    {
#ifdef DEBUG
        if (addr->OperIs(GT_LCL_ADDR))
        {
            // If the local var is a gcref or byref, the local var better be untracked, because we have
            // no logic here to track local variable lifetime changes, like we do in the contained case
            // above. E.g., for a `st a0,[a1]` for byref `a1` to local `V01`, we won't store the local
            // `V01` and so the emitter can't update the GC lifetime for `V01` if this is a variable birth.
            LclVarDsc* varDsc = emitComp->lvaGetDesc(addr->AsLclVarCommon());
            assert(!varDsc->lvTracked);
        }
#endif // DEBUG

        // Then load/store dataReg from/to [addrReg]
        emitIns_R_R_I(ins, attr, dataReg, addr->GetRegNum(), 0);
    }
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.

regNumber emitter::emitInsBinary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src)
{
    NYI_RISCV64("emitInsBinary-----unimplemented/unused on RISCV64 yet----");
    return REG_R0;
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.
regNumber emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2)
{
    // dst can only be a reg
    assert(!dst->isContained());

    // find immed (if any) - it cannot be a dst
    // Only one src can be an int.
    GenTreeIntConCommon* intConst  = nullptr;
    GenTree*             nonIntReg = nullptr;

    const bool needCheckOv = dst->gtOverflowEx();

    if (varTypeIsFloating(dst))
    {
        // src1 can only be a reg
        assert(!src1->isContained());
        // src2 can only be a reg
        assert(!src2->isContained());
    }
    else // not floating point
    {
        // src2 can be immed or reg
        assert(!src2->isContained() || src2->isContainedIntOrIImmed());

        // Check src2 first as we can always allow it to be a contained immediate
        if (src2->isContainedIntOrIImmed())
        {
            intConst  = src2->AsIntConCommon();
            nonIntReg = src1;
        }
        // Only for commutative operations do we check src1 and allow it to be a contained immediate
        else if (dst->OperIsCommutative())
        {
            // src1 can be immed or reg
            assert(!src1->isContained() || src1->isContainedIntOrIImmed());

            // Check src1 and allow it to be a contained immediate
            if (src1->isContainedIntOrIImmed())
            {
                assert(!src2->isContainedIntOrIImmed());
                intConst  = src1->AsIntConCommon();
                nonIntReg = src2;
            }
        }
        else
        {
            // src1 can only be a reg
            assert(!src1->isContained());
        }
    }

#ifdef DEBUG
    if (needCheckOv)
    {
        if (ins == INS_add)
        {
            assert(attr == EA_8BYTE);
        }
        else if (ins == INS_addw) // || ins == INS_add
        {
            assert(attr == EA_4BYTE);
        }
        else if (ins == INS_addi)
        {
            assert(intConst != nullptr);
        }
        else if (ins == INS_addiw)
        {
            assert(intConst != nullptr);
        }
        else if (ins == INS_sub)
        {
            assert(attr == EA_8BYTE);
        }
        else if (ins == INS_subw)
        {
            assert(attr == EA_4BYTE);
        }
        else if ((ins == INS_mul) || (ins == INS_mulh) || (ins == INS_mulhu))
        {
            assert(attr == EA_8BYTE);
            // NOTE: overflow format doesn't support an int constant operand directly.
            assert(intConst == nullptr);
        }
        else if (ins == INS_mulw)
        {
            assert(attr == EA_4BYTE);
            // NOTE: overflow format doesn't support an int constant operand directly.
            assert(intConst == nullptr);
        }
        else
        {
            printf("RISCV64-Invalid ins for overflow check: %s\n", codeGen->genInsName(ins));
            assert(!"Invalid ins for overflow check");
        }
    }
#endif // DEBUG

    regNumber dstReg  = dst->GetRegNum();
    regNumber src1Reg = src1->GetRegNum();
    regNumber src2Reg = src2->GetRegNum();

    if (intConst != nullptr)
    {
        ssize_t imm = intConst->IconValue();
        assert(isValidSimm12(imm));

        if (ins == INS_sub)
        {
            assert(attr == EA_8BYTE);
            assert(imm != -2048);
            ins = INS_addi;
            imm = -imm;
        }
        else if (ins == INS_subw)
        {
            assert(attr == EA_4BYTE);
            assert(imm != -2048);
            ins = INS_addiw;
            imm = -imm;
        }

        assert(ins == INS_addi || ins == INS_addiw || ins == INS_andi || ins == INS_ori || ins == INS_xori);

        regNumber tempReg = needCheckOv ? dst->ExtractTempReg() : REG_NA;

        if (needCheckOv)
        {
            emitIns_R_R(INS_mov, attr, tempReg, nonIntReg->GetRegNum());
        }

        emitIns_R_R_I(ins, attr, dstReg, nonIntReg->GetRegNum(), imm);

        if (needCheckOv)
        {
            // At this point andi/ori/xori are excluded by previous checks
            assert(ins == INS_addi || ins == INS_addiw);

            // AS11 = B + C
            if ((dst->gtFlags & GTF_UNSIGNED) != 0)
            {
                codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bltu, dstReg, nullptr, tempReg);
            }
            else
            {
                if (imm > 0)
                {
                    // B > 0 and C > 0, if A < B, goto overflow
                    BasicBlock* tmpLabel = codeGen->genCreateTempLabel();
                    emitIns_J_cond_la(INS_bge, tmpLabel, REG_R0, tempReg);
                    emitIns_R_R_I(INS_slti, EA_PTRSIZE, tempReg, dstReg, imm);

                    codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg);

                    codeGen->genDefineTempLabel(tmpLabel);
                }
                else if (imm < 0)
                {
                    // B < 0 and C < 0, if A > B, goto overflow
                    BasicBlock* tmpLabel = codeGen->genCreateTempLabel();
                    emitIns_J_cond_la(INS_bge, tmpLabel, tempReg, REG_R0);
                    emitIns_R_R_I(INS_addi, attr, tempReg, REG_R0, imm);

                    codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, tempReg, nullptr, dstReg);

                    codeGen->genDefineTempLabel(tmpLabel);
                }
            }
        }
    }
    else if (varTypeIsFloating(dst))
    {
        emitIns_R_R_R(ins, attr, dstReg, src1Reg, src2Reg);
    }
    else
    {
        regNumber tempReg = needCheckOv ? dst->ExtractTempReg() : REG_NA;

        switch (dst->OperGet())
        {
            case GT_MUL:
            {
                if (!needCheckOv && !(dst->gtFlags & GTF_UNSIGNED))
                {
                    emitIns_R_R_R(ins, attr, dstReg, src1Reg, src2Reg);
                }
                else
                {
                    if (needCheckOv)
                    {
                        assert(tempReg != dstReg);
                        assert(tempReg != src1Reg);
                        assert(tempReg != src2Reg);

                        assert(REG_RA != dstReg);
                        assert(REG_RA != src1Reg);
                        assert(REG_RA != src2Reg);

                        if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                        {
                            if (attr == EA_4BYTE)
                            {
                                emitIns_R_R_I(INS_slli, EA_8BYTE, tempReg, src1Reg, 32);
                                emitIns_R_R_I(INS_slli, EA_8BYTE, REG_RA, src2Reg, 32);
                                emitIns_R_R_R(INS_mulhu, EA_8BYTE, tempReg, tempReg, REG_RA);
                                emitIns_R_R_I(INS_srai, attr, tempReg, tempReg, 32);
                            }
                            else
                            {
                                emitIns_R_R_R(INS_mulhu, attr, tempReg, src1Reg, src2Reg);
                            }
                        }
                        else
                        {
                            if (attr == EA_4BYTE)
                            {
                                emitIns_R_R_R(INS_mul, EA_8BYTE, tempReg, src1Reg, src2Reg);
                                emitIns_R_R_I(INS_srai, attr, tempReg, tempReg, 32);
                            }
                            else
                            {
                                emitIns_R_R_R(INS_mulh, attr, tempReg, src1Reg, src2Reg);
                            }
                        }
                    }

                    // n * n bytes will store n bytes result
                    emitIns_R_R_R(ins, attr, dstReg, src1Reg, src2Reg);

                    if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                    {
                        if (attr == EA_4BYTE)
                        {
                            emitIns_R_R_I(INS_slli, EA_8BYTE, dstReg, dstReg, 32);
                            emitIns_R_R_I(INS_srli, EA_8BYTE, dstReg, dstReg, 32);
                        }
                    }

                    if (needCheckOv)
                    {
                        assert(tempReg != dstReg);
                        assert(tempReg != src1Reg);
                        assert(tempReg != src2Reg);

                        if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                        {
                            codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg);
                        }
                        else
                        {
                            regNumber tempReg2 = dst->ExtractTempReg();
                            assert(tempReg2 != dstReg);
                            assert(tempReg2 != src1Reg);
                            assert(tempReg2 != src2Reg);
                            size_t imm = (EA_SIZE(attr) == EA_8BYTE) ? 63 : 31;
                            emitIns_R_R_I(EA_SIZE(attr) == EA_8BYTE ? INS_srai : INS_sraiw, attr, tempReg2, dstReg,
                                          imm);
                            codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg, nullptr, tempReg2);
                        }
                    }
                }
            }
            break;

            case GT_AND:
            case GT_AND_NOT:
            case GT_OR:
            case GT_XOR:
            {
                emitIns_R_R_R(ins, attr, dstReg, src1Reg, src2Reg);

                // TODO-RISCV64-CQ: here sign-extend dst when deal with 32bit data is too conservative.
                if (EA_SIZE(attr) == EA_4BYTE)
                    emitIns_R_R_I(INS_slliw, attr, dstReg, dstReg, 0);
            }
            break;

            case GT_ADD:
            case GT_SUB:
            {
                regNumber regOp1       = src1Reg;
                regNumber regOp2       = src2Reg;
                regNumber saveOperReg1 = REG_NA;
                regNumber saveOperReg2 = REG_NA;

                if ((dst->gtFlags & GTF_UNSIGNED) && (attr == EA_8BYTE))
                {
                    if (src1->gtType == TYP_INT)
                    {
                        emitIns_R_R_I(INS_slli, EA_8BYTE, regOp1, regOp1, 32);
                        emitIns_R_R_I(INS_srli, EA_8BYTE, regOp1, regOp1, 32);
                    }
                    if (src2->gtType == TYP_INT)
                    {
                        emitIns_R_R_I(INS_slli, EA_8BYTE, regOp2, regOp2, 32);
                        emitIns_R_R_I(INS_srli, EA_8BYTE, regOp2, regOp2, 32);
                    }
                }

                if (needCheckOv)
                {
                    assert(!varTypeIsFloating(dst));

                    assert(tempReg != dstReg);

                    if (dstReg == regOp1)
                    {
                        assert(tempReg != regOp1);
                        assert(REG_RA != regOp1);
                        saveOperReg1 = tempReg;
                        saveOperReg2 = regOp2;
                        emitIns_R_R_I(INS_addi, attr, tempReg, regOp1, 0);
                    }
                    else if (dstReg == regOp2)
                    {
                        assert(tempReg != regOp2);
                        assert(REG_RA != regOp2);
                        saveOperReg1 = regOp1;
                        saveOperReg2 = tempReg;
                        emitIns_R_R_I(INS_addi, attr, tempReg, regOp2, 0);
                    }
                    else
                    {
                        saveOperReg1 = regOp1;
                        saveOperReg2 = regOp2;
                    }
                }

                emitIns_R_R_R(ins, attr, dstReg, regOp1, regOp2);

                if (needCheckOv)
                {
                    ssize_t   imm;
                    regNumber tempReg1;
                    regNumber tempReg2;
                    // ADD : A = B + C
                    // SUB : C = A - B
                    bool isAdd = (dst->OperGet() == GT_ADD);
                    if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                    {
                        // if A < B, goto overflow
                        if (isAdd)
                        {
                            tempReg1 = dstReg;
                            tempReg2 = saveOperReg1;
                        }
                        else
                        {
                            tempReg1 = saveOperReg1;
                            tempReg2 = saveOperReg2;
                        }
                        codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bltu, tempReg1, nullptr, tempReg2);
                    }
                    else
                    {
                        tempReg1 = REG_RA;
                        tempReg2 = dst->ExtractTempReg();
                        assert(tempReg1 != tempReg2);
                        assert(tempReg1 != saveOperReg1);
                        assert(tempReg2 != saveOperReg2);

                        ssize_t ui6 = (attr == EA_4BYTE) ? 31 : 63;
                        emitIns_R_R_I(INS_srli, attr, tempReg1, isAdd ? saveOperReg1 : dstReg, ui6);
                        emitIns_R_R_I(INS_srli, attr, tempReg2, saveOperReg2, ui6);

                        emitIns_R_R_R(INS_xor, attr, tempReg1, tempReg1, tempReg2);
                        if (attr == EA_4BYTE)
                        {
                            imm = 1;
                            emitIns_R_R_I(INS_andi, attr, tempReg1, tempReg1, imm);
                            emitIns_R_R_I(INS_andi, attr, tempReg2, tempReg2, imm);
                        }
                        // if (B > 0 && C < 0) || (B < 0  && C > 0), skip overflow
                        BasicBlock* tmpLabel  = codeGen->genCreateTempLabel();
                        BasicBlock* tmpLabel2 = codeGen->genCreateTempLabel();
                        BasicBlock* tmpLabel3 = codeGen->genCreateTempLabel();

                        emitIns_J_cond_la(INS_bne, tmpLabel, tempReg1, REG_R0);

                        emitIns_J_cond_la(INS_bne, tmpLabel3, tempReg2, REG_R0);

                        // B > 0 and C > 0, if A < B, goto overflow
                        emitIns_J_cond_la(INS_bge, tmpLabel, isAdd ? dstReg : saveOperReg1,
                                          isAdd ? saveOperReg1 : saveOperReg2);

                        codeGen->genDefineTempLabel(tmpLabel2);

                        codeGen->genJumpToThrowHlpBlk(EJ_jmp, SCK_OVERFLOW);

                        codeGen->genDefineTempLabel(tmpLabel3);

                        // B < 0 and C < 0, if A > B, goto overflow
                        emitIns_J_cond_la(INS_blt, tmpLabel2, isAdd ? saveOperReg1 : saveOperReg2,
                                          isAdd ? dstReg : saveOperReg1);

                        codeGen->genDefineTempLabel(tmpLabel);
                    }
                }
            }
            break;

            default:
                NO_WAY("unexpected instruction within emitInsTernary!");
        }
    }

    return dstReg;
}

unsigned emitter::get_curTotalCodeSize()
{
    return emitTotalCodeSize;
}

#if defined(DEBUG) || defined(LATE_DISASM)

//----------------------------------------------------------------------------------------
// getInsExecutionCharacteristics:
//    Returns the current instruction execution characteristics
//
// Arguments:
//    id  - The current instruction descriptor to be evaluated
//
// Return Value:
//    A struct containing the current instruction execution characteristics
//
// Notes:
//    The instruction latencies and throughput values returned by this function
//    are NOT accurate and just a function feature.
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    insExecutionCharacteristics result;

    // TODO-RISCV64: support this function.
    result.insThroughput       = PERFSCORE_THROUGHPUT_ZERO;
    result.insLatency          = PERFSCORE_LATENCY_ZERO;
    result.insMemoryAccessKind = PERFSCORE_MEMORY_NONE;

    return result;
}

#endif // defined(DEBUG) || defined(LATE_DISASM)

#ifdef DEBUG
//------------------------------------------------------------------------
// emitRegName: Returns a general-purpose register name or SIMD and floating-point scalar register name.
//
// TODO-RISCV64: supporting SIMD.
// Arguments:
//    reg - A general-purpose register orfloating-point register.
//    size - unused parameter.
//    varName - unused parameter.
//
// Return value:
//    A string that represents a general-purpose register name or floating-point scalar register name.
//
const char* emitter::emitRegName(regNumber reg, emitAttr size, bool varName) const
{
    assert(reg < REG_COUNT);

    const char* rn = nullptr;

    rn = RegNames[reg];
    assert(rn != nullptr);

    return rn;
}
#endif

//------------------------------------------------------------------------
// IsMovInstruction: Determines whether a give instruction is a move instruction
//
// Arguments:
//    ins       -- The instruction being checked
//
bool emitter::IsMovInstruction(instruction ins)
{
    switch (ins)
    {
        case INS_mov:
        case INS_fsgnj_s:
        case INS_fsgnj_d:
        {
            return true;
        }

        default:
        {
            return false;
        }
    }
    return false;
}

#endif // defined(TARGET_RISCV64)
