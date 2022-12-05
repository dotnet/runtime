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
* Look up the jump kind for an instruction. It better be a conditional
* branch instruction with a jump kind!
*/

/*static*/ emitJumpKind emitter::emitInsToJumpKind(instruction ins)
{
    NYI_RISCV64("emitInsToJumpKind-----unimplemented on RISCV64 yet----");
    return EJ_NONE;
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

size_t emitter::emitSizeOfInsDsc(instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

inline bool emitter::emitInsMayWriteToGCReg(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

bool emitter::emitInsWritesToLclVarStackLoc(instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
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
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

//-------------------------------------------------------------------------
// emitInsIsLoadOrStore: Returns true if the instruction is some kind of load/store instruction.
//
bool emitter::emitInsIsLoadOrStore(instruction ins)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

/*****************************************************************************
 *
 *  Returns the specific encoding of the given CPU instruction.
 */

inline emitter::code_t emitter::emitInsCode(instruction ins /*, insFormat fmt*/)
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
 *  emitter::emitIns_S_R() and emitter::emitIns_R_S():
 *
 *  Add an Load/Store instruction(s): base+offset and base-addr-computing if needed.
 *  For referencing a stack-based local variable and a register
 *
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    ssize_t imm;

    emitAttr size = EA_SIZE(attr);

#ifdef DEBUG
    switch (ins)
    {
        case INS_sd:
        case INS_sw:
        case INS_fsw:
        case INS_fsd:
        case INS_sb:
        case INS_sh:
            break;

        default:
            NYI("emitIns_S_R");
            return;

    } // end switch (ins)
#endif

    /* Figure out the variable's frame position */
    int  base;
    bool FPbased;

    base = emitComp->lvaFrameAddress(varx, &FPbased);
    imm  = offs < 0 ? -offs - 8 : base + offs;

    regNumber reg3 = FPbased ? REG_FPBASE : REG_SPBASE;
    assert(offs >= 0);
    // regNumber reg2 = offs < 0 ? REG_R21 : reg3;
    regNumber reg2 = reg3;
    offs           = offs < 0 ? -offs - 8 : offs;

    if ((-2048 <= imm) && (imm < 2048))
    {
        // regs[1] = reg2;
    }
    else
    {
        assert(isValidSimm20((imm + 0x800) >> 12));
        emitIns_R_I(INS_lui, EA_PTRSIZE, REG_RA, (imm + 0x800) >> 12);

        emitIns_R_R_R(INS_add, EA_PTRSIZE, REG_RA, REG_RA, reg2);

        reg2 = REG_RA;
    }

    instrDesc* id = emitNewInstr(attr);

    id->idReg1(reg1);

    id->idReg2(reg2);

    id->idIns(ins);

    code_t code = emitInsCode(ins);
    code |= (code_t)reg1 << 7;
    code |= (code_t)reg2 << 15;
    code |= (code_t)(imm & 0xfff) << 20;

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
            NYI("emitIns_R_S");
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
    //reg2           = offs < 0 ? REG_R21 : reg2; // TODO
    offs           = offs < 0 ? -offs - 8 : offs;

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
            emitIns_R_I(INS_lui, EA_PTRSIZE, REG_RA, (imm  + 0x800) >> 12);
            ssize_t imm2 = imm & 0xfff;
            emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_RA, REG_RA, imm2);

            ins  = INS_add;
            code = emitInsCode(ins);
            code |= (code_t)reg1 << 7;
            code |= (code_t)reg2 << 15;
            code |= (code_t)REG_RA << 20;
        }
        else
        {
            assert(isValidSimm20((imm + 0x800) >> 12));
            emitIns_R_I(INS_lui, EA_PTRSIZE, REG_RA, (imm + 0x800) >> 12);

            emitIns_R_R_R(INS_add, EA_PTRSIZE, REG_RA, REG_RA, reg2);

            ssize_t imm2 = imm & 0xfff;
            code = emitInsCode(ins);
            code |= (code_t)reg1 << 7;
            code |= (code_t)REG_RA << 15;
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing a register and a constant.
 */

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    // assert(IsMovInstruction(ins));

    if (!canSkip || (dstReg != srcReg))
    {
        if ((EA_4BYTE == attr) && (INS_mov == ins))
            emitIns_R_R_I(INS_addiw, attr, dstReg, srcReg, 0);
        else
            emitIns_R_R_I(INS_addi, attr, dstReg, srcReg, 0);
    }
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers
 */

void emitter::emitIns_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt /* = INS_OPTS_NONE */)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and a constant.
 */

void emitter::emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt /* = INS_OPTS_NONE */)
{
    code_t code = emitInsCode(ins);
    if ((INS_addi <= ins && INS_srai >= ins) || INS_ld == ins || INS_lw == ins)
    {
        code |= reg1 << 7;           // rd
        code |= reg2 << 15;          // rs1
        code |= imm << 20;           // imm
    }
    else if (INS_sd == ins)
    {
        code |= reg1 << 20;          // rs2
        code |= reg2 << 15;          // rs1
        code |= imm << 25;           // imm
    }
    else if (INS_beq <= ins && INS_bgeu >= ins)
    {
        code |= reg1 << 15;
        code |= reg2 << 20;
        code |= ((imm >> 11) & 0x1)  << 7;
        code |= ((imm >> 1)  & 0xf)  << 8;
        code |= ((imm >> 5)  & 0x3f) << 25;
        code |= ((imm >> 12) & 0x1)  << 31;
    }
    else
    {
        fprintf(stderr, "[RISCV64] CODE %x\n", code);
        _ASSERTE(!"TODO RISCV64 NYI");
    }
    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idReg1(reg1);
    id->idReg2(reg2);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);
}

/*****************************************************************************
 *
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt) /* = INS_OPTS_NONE */
{
    code_t code = emitInsCode(ins);

    if (((INS_add <= ins) && (ins <= INS_and)))
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
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_R --1!");
        }
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= (reg1 << 7);
        code |= (reg2 << 15);
        code |= (reg3 << 20);
#endif
    }
    else
    {
        fprintf(stderr, "[RISCV64] %x\n", code);
        _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Add an instruction referencing four registers.
 */

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    NYI_RISCV64("emitIns_J_R-----unimplemented/unused on RISCV64 yet----");
}

void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    assert(dst != nullptr);
    //
    // INS_OPTS_J: placeholders.  1-ins: if the dst outof-range will be replaced by INS_OPTS_JIRL.
    //   bceqz/bcnez/beq/bne/blt/bltu/bge/bgeu/beqz/bnez/b/bl  dst

    assert(dst->bbFlags & BBF_HAS_LABEL);

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
    _ASSERTE(!"TODO RISCV64 NYI");
}

void emitter::emitIns_I_la(emitAttr size, regNumber reg, ssize_t imm)
{
    assert(!EA_IS_RELOC(size));
    assert(isGeneralRegister(reg));
    if (0 == ((imm + 0x800) >> 32)) {
        if (((imm + 0x800) >> 12) != 0)
        {
            emitIns_R_I(INS_lui, size, reg, ((imm + 0x800) >> 12));
            if ((imm & 0xFFF) != 0)
            {
                emitIns_R_R_I(INS_addi, size, reg, reg, imm & 0xFFF);
            }
        }
        else
        {
            emitIns_R_R_I(INS_addi, size, reg, REG_R0, imm & 0xFFF);
        }
    }
    else
    {
        UINT32 upper = imm >> 32;
        if (((upper + 0x800) >> 12) != 0)
        {
            emitIns_R_I(INS_lui, size, reg, ((upper + 0x800) >> 12));
        }
        if ((upper & 0xFFF) != 0)
        {
            emitIns_R_R_I(INS_addi, size, reg, REG_R0, upper & 0xFFF);
        }
        UINT32 lower = (imm << 32) >> 32;
        UINT32 shift = 0;
        for (int i = 32; i >= 0; i -= 11)
        {
            shift += i > 11 ? 11 : i;
            UINT32 current = lower >> (i < 11 ? 0 : i - 11);
            if (current != 0)
            {
                emitIns_R_R_I(INS_slli, size, reg, reg, shift);
                emitIns_R_R_I(INS_addi, size, reg, reg, current & 0x7FF);
                shift = 0;
            }
        }
        if (shift)
        {
            emitIns_R_R_I(INS_slli, size, reg, reg, shift);
        }
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
        // assert((((size_t)addr) & 3) == 0); // TODO NEED TO CHECK ALIGNMENT (ex. Address of RNGCHKFAIL is 0x2033b32 in my test.)

        addr = (void*)(((size_t)addr) + (isJump ? 0 : 1)); // NOTE: low-bit0 is used for jirl ra/r0,rd,0
        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (emitComp->opts.compReloc)
        {
            id->idSetIsDspReloc();
            id->idCodeSize(8); // TODO NEED TO CHECK LATER
        }
        else
        {
            id->idCodeSize(16); // TODO NEED TO CHECK LATER
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

unsigned emitter::emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

void emitter::emitJumpDistBind()
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Emit a 32-bit RISCV64 instruction
 */

/*static*/ unsigned emitter::emitOutput_Instr(BYTE* dst, code_t code)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
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
    _ASSERTE(!"TODO RISCV64 NYI");
    return 0;
}

/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG

// clang-format off
static const char* const RegNames[] =
{
    #define REGDEF(name, rnum, mask, sname) sname,
    #include "register.h"
};
// clang-format on

//----------------------------------------------------------------------------------------
// Disassemble the given instruction.
// The `emitter::emitDisInsName` is focused on the most important for debugging.
// So it implemented as far as simply and independently which is very useful for
// porting easily to the release mode.
//
// Arguments:
//    code - The instruction's encoding.
//    addr - The address of the code.
//    id   - The instrDesc of the code if needed.
//
// Note:
//    The length of the instruction's name include aligned space is 13.
//

void emitter::emitDisInsName(code_t code, const BYTE* addr, instrDesc* id)
{
    _ASSERTE(!"TODO RISCV64 NYI");
}

/*****************************************************************************
 *
 *  Display (optionally) the instruction encoding in hex
 */

void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
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

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    // LA implements this similar by `emitter::emitDisInsName`.
    // For LA maybe the `emitDispIns` is over complicate.
    // The `emitter::emitDisInsName` is focused on the most important for debugging.
    NYI_RISCV64("LA not used the emitter::emitDispIns");
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    _ASSERTE(!"TODO RISCV64 NYI");
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
        assert(addr->OperIs(GT_CLS_VAR_ADDR, GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR, GT_LEA));

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
                    emitIns_I_la(EA_PTRSIZE, tmpReg,
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
                regNumber tmpReg = indir->GetSingleTempReg();
                // Then load/store dataReg from/to [memBase + index]
                switch (EA_SIZE(emitTypeSize(indir->TypeGet())))
                {
                    case EA_1BYTE:
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld || ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd);
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
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld || ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd);
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
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) || ins == INS_lwu || ins == INS_ld || ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd || ins == INS_fsw || ins == INS_flw);
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
                        assert(((ins <= INS_lhu) && (ins >= INS_lb)) ||
                            ins == INS_lwu || ins == INS_ld ||
                            ((ins <= INS_sw) && (ins >= INS_sb)) || ins == INS_sd || ins == INS_fld || ins == INS_fsd);
                        assert(INS_fsd > INS_sd);
                        if (ins <= INS_lhu || ins == INS_lwu || ins == INS_ld)
                        {
                            ins = INS_ld;
                        }
                        else if (ins != INS_fld && ins != INS_fsd)
                            ins = INS_sd;
                        break;
                    default:
                        assert(!"------------TODO for RISCV64: unsupported ins.");
                }

                if (lsl > 0)
                {
                    // Then load/store dataReg from/to [memBase + index*scale]
                    emitIns_R_R_I(INS_slli, emitActualTypeSize(index->TypeGet()), tmpReg, index->GetRegNum(), lsl);
                    emitIns_R_R_R(INS_add, addType, tmpReg, memBase->GetRegNum(), tmpReg);
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, 0);
                }
                else // no scale
                {
                    emitIns_R_R_R(INS_add, addType, tmpReg, memBase->GetRegNum(), index->GetRegNum());
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, 0);
                }
            }
        }
        else // no Index register
        {
            if (addr->OperGet() == GT_CLS_VAR_ADDR)
            {
                // Get a temp integer register to compute long address.
                regNumber addrReg = indir->GetSingleTempReg();
                emitIns_R_C(ins, attr, dataReg, addrReg, addr->AsClsVar()->gtClsVarHnd, 0);
            }
            else if (addr->OperIs(GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR))
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
                emitIns_I_la(EA_PTRSIZE, tmpReg, offset);
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
        if (addr->OperIs(GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR))
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
    NYI_RISCV64("emitInsBinary-----unused");
    return REG_R0;
}

// The callee must call genConsumeReg() for any non-contained srcs
// and genProduceReg() for any non-contained dsts.
regNumber emitter::emitInsTernary(instruction ins, emitAttr attr, GenTree* dst, GenTree* src1, GenTree* src2)
{
    _ASSERTE(!"TODO RISCV64 NYI");
    return REG_R0;
    // dst can only be a reg
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
const char* emitter::emitRegName(regNumber reg, emitAttr size, bool varName)
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
    _ASSERTE(!"TODO RISCV64 NYI");
    return false;
}

#endif // defined(TARGET_RISCV64)
