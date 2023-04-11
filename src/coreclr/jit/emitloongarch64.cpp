// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                             emitloongarch64.cpp                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_LOONGARCH64)

/*****************************************************************************/
/*****************************************************************************/

#include "instr.h"
#include "emit.h"
#include "codegen.h"

////These are used for loongarch64 instrs's dump.
////LA_OP_2R  opcode: bit31 ~ bit10
#define LA_2R_CLO_W 0x4
#define LA_2R_CLZ_W 0x5
#define LA_2R_CTO_W 0x6
#define LA_2R_CTZ_W 0x7
#define LA_2R_CLO_D 0x8
#define LA_2R_CLZ_D 0x9
#define LA_2R_CTO_D 0xa
#define LA_2R_CTZ_D 0xb
#define LA_2R_REVB_2H 0xc
#define LA_2R_REVB_4H 0xd
#define LA_2R_REVB_2W 0xe
#define LA_2R_REVB_D 0xf
#define LA_2R_REVH_2W 0x10
#define LA_2R_REVH_D 0x11
#define LA_2R_BITREV_4B 0x12
#define LA_2R_BITREV_8B 0x13
#define LA_2R_BITREV_W 0x14
#define LA_2R_BITREV_D 0x15
#define LA_2R_EXT_W_H 0x16
#define LA_2R_EXT_W_B 0x17
#define LA_2R_RDTIMEL_W 0x18
#define LA_2R_RDTIMEH_W 0x19
#define LA_2R_RDTIME_D 0x1a
#define LA_2R_CPUCFG 0x1b
#define LA_2R_ASRTLE_D 0x2
#define LA_2R_ASRTGT_D 0x3
#define LA_2R_FABS_S 0x4501
#define LA_2R_FABS_D 0x4502
#define LA_2R_FNEG_S 0x4505
#define LA_2R_FNEG_D 0x4506
#define LA_2R_FLOGB_S 0x4509
#define LA_2R_FLOGB_D 0x450a
#define LA_2R_FCLASS_S 0x450d
#define LA_2R_FCLASS_D 0x450e
#define LA_2R_FSQRT_S 0x4511
#define LA_2R_FSQRT_D 0x4512
#define LA_2R_FRECIP_S 0x4515
#define LA_2R_FRECIP_D 0x4516
#define LA_2R_FRSQRT_S 0x4519
#define LA_2R_FRSQRT_D 0x451a
#define LA_2R_FMOV_S 0x4525
#define LA_2R_FMOV_D 0x4526
#define LA_2R_MOVGR2FR_W 0x4529
#define LA_2R_MOVGR2FR_D 0x452a
#define LA_2R_MOVGR2FRH_W 0x452b
#define LA_2R_MOVFR2GR_S 0x452d
#define LA_2R_MOVFR2GR_D 0x452e
#define LA_2R_MOVFRH2GR_S 0x452f
#define LA_2R_MOVGR2FCSR 0x4530
#define LA_2R_MOVFCSR2GR 0x4532
#define LA_2R_MOVFR2CF 0x4534
#define LA_2R_MOVCF2FR 0x4535
#define LA_2R_MOVGR2CF 0x4536
#define LA_2R_MOVCF2GR 0x4537
#define LA_2R_FCVT_S_D 0x4646
#define LA_2R_FCVT_D_S 0x4649
#define LA_2R_FTINTRM_W_S 0x4681
#define LA_2R_FTINTRM_W_D 0x4682
#define LA_2R_FTINTRM_L_S 0x4689
#define LA_2R_FTINTRM_L_D 0x468a
#define LA_2R_FTINTRP_W_S 0x4691
#define LA_2R_FTINTRP_W_D 0x4692
#define LA_2R_FTINTRP_L_S 0x4699
#define LA_2R_FTINTRP_L_D 0x469a
#define LA_2R_FTINTRZ_W_S 0x46a1
#define LA_2R_FTINTRZ_W_D 0x46a2
#define LA_2R_FTINTRZ_L_S 0x46a9
#define LA_2R_FTINTRZ_L_D 0x46aa
#define LA_2R_FTINTRNE_W_S 0x46b1
#define LA_2R_FTINTRNE_W_D 0x46b2
#define LA_2R_FTINTRNE_L_S 0x46b9
#define LA_2R_FTINTRNE_L_D 0x46ba
#define LA_2R_FTINT_W_S 0x46c1
#define LA_2R_FTINT_W_D 0x46c2
#define LA_2R_FTINT_L_S 0x46c9
#define LA_2R_FTINT_L_D 0x46ca
#define LA_2R_FFINT_S_W 0x4744
#define LA_2R_FFINT_S_L 0x4746
#define LA_2R_FFINT_D_W 0x4748
#define LA_2R_FFINT_D_L 0x474a
#define LA_2R_FRINT_S 0x4791
#define LA_2R_FRINT_D 0x4792
#define LA_2R_IOCSRRD_B 0x19200
#define LA_2R_IOCSRRD_H 0x19201
#define LA_2R_IOCSRRD_W 0x19202
#define LA_2R_IOCSRRD_D 0x19203
#define LA_2R_IOCSRWR_B 0x19204
#define LA_2R_IOCSRWR_H 0x19205
#define LA_2R_IOCSRWR_W 0x19206
#define LA_2R_IOCSRWR_D 0x19207

////LA_OP_3R  opcode: bit31 ~ bit15
#define LA_3R_ADD_W 0x20
#define LA_3R_ADD_D 0x21
#define LA_3R_SUB_W 0x22
#define LA_3R_SUB_D 0x23
#define LA_3R_SLT 0x24
#define LA_3R_SLTU 0x25
#define LA_3R_MASKEQZ 0x26
#define LA_3R_MASKNEZ 0x27
#define LA_3R_NOR 0x28
#define LA_3R_AND 0x29
#define LA_3R_OR 0x2a
#define LA_3R_XOR 0x2b
#define LA_3R_ORN 0x2c
#define LA_3R_ANDN 0x2d
#define LA_3R_SLL_W 0x2e
#define LA_3R_SRL_W 0x2f
#define LA_3R_SRA_W 0x30
#define LA_3R_SLL_D 0x31
#define LA_3R_SRL_D 0x32
#define LA_3R_SRA_D 0x33
#define LA_3R_ROTR_W 0x36
#define LA_3R_ROTR_D 0x37
#define LA_3R_MUL_W 0x38
#define LA_3R_MULH_W 0x39
#define LA_3R_MULH_WU 0x3a
#define LA_3R_MUL_D 0x3b
#define LA_3R_MULH_D 0x3c
#define LA_3R_MULH_DU 0x3d
#define LA_3R_MULW_D_W 0x3e
#define LA_3R_MULW_D_WU 0x3f
#define LA_3R_DIV_W 0x40
#define LA_3R_MOD_W 0x41
#define LA_3R_DIV_WU 0x42
#define LA_3R_MOD_WU 0x43
#define LA_3R_DIV_D 0x44
#define LA_3R_MOD_D 0x45
#define LA_3R_DIV_DU 0x46
#define LA_3R_MOD_DU 0x47
#define LA_3R_CRC_W_B_W 0x48
#define LA_3R_CRC_W_H_W 0x49
#define LA_3R_CRC_W_W_W 0x4a
#define LA_3R_CRC_W_D_W 0x4b
#define LA_3R_CRCC_W_B_W 0x4c
#define LA_3R_CRCC_W_H_W 0x4d
#define LA_3R_CRCC_W_W_W 0x4e
#define LA_3R_CRCC_W_D_W 0x4f
#define LA_3R_FADD_S 0x201
#define LA_3R_FADD_D 0x202
#define LA_3R_FSUB_S 0x205
#define LA_3R_FSUB_D 0x206
#define LA_3R_FMUL_S 0x209
#define LA_3R_FMUL_D 0x20a
#define LA_3R_FDIV_S 0x20d
#define LA_3R_FDIV_D 0x20e
#define LA_3R_FMAX_S 0x211
#define LA_3R_FMAX_D 0x212
#define LA_3R_FMIN_S 0x215
#define LA_3R_FMIN_D 0x216
#define LA_3R_FMAXA_S 0x219
#define LA_3R_FMAXA_D 0x21a
#define LA_3R_FMINA_S 0x21d
#define LA_3R_FMINA_D 0x21e
#define LA_3R_FSCALEB_S 0x221
#define LA_3R_FSCALEB_D 0x222
#define LA_3R_FCOPYSIGN_S 0x225
#define LA_3R_FCOPYSIGN_D 0x226
#define LA_3R_INVTLB 0xc91
#define LA_3R_LDX_B 0x7000
#define LA_3R_LDX_H 0x7008
#define LA_3R_LDX_W 0x7010
#define LA_3R_LDX_D 0x7018
#define LA_3R_STX_B 0x7020
#define LA_3R_STX_H 0x7028
#define LA_3R_STX_W 0x7030
#define LA_3R_STX_D 0x7038
#define LA_3R_LDX_BU 0x7040
#define LA_3R_LDX_HU 0x7048
#define LA_3R_LDX_WU 0x7050
#define LA_3R_PRELDX 0x7058
#define LA_3R_FLDX_S 0x7060
#define LA_3R_FLDX_D 0x7068
#define LA_3R_FSTX_S 0x7070
#define LA_3R_FSTX_D 0x7078
#define LA_3R_AMSWAP_W 0x70c0
#define LA_3R_AMSWAP_D 0x70c1
#define LA_3R_AMADD_W 0x70c2
#define LA_3R_AMADD_D 0x70c3
#define LA_3R_AMAND_W 0x70c4
#define LA_3R_AMAND_D 0x70c5
#define LA_3R_AMOR_W 0x70c6
#define LA_3R_AMOR_D 0x70c7
#define LA_3R_AMXOR_W 0x70c8
#define LA_3R_AMXOR_D 0x70c9
#define LA_3R_AMMAX_W 0x70ca
#define LA_3R_AMMAX_D 0x70cb
#define LA_3R_AMMIN_W 0x70cc
#define LA_3R_AMMIN_D 0x70cd
#define LA_3R_AMMAX_WU 0x70ce
#define LA_3R_AMMAX_DU 0x70cf
#define LA_3R_AMMIN_WU 0x70d0
#define LA_3R_AMMIN_DU 0x70d1
#define LA_3R_AMSWAP_DB_W 0x70d2
#define LA_3R_AMSWAP_DB_D 0x70d3
#define LA_3R_AMADD_DB_W 0x70d4
#define LA_3R_AMADD_DB_D 0x70d5
#define LA_3R_AMAND_DB_W 0x70d6
#define LA_3R_AMAND_DB_D 0x70d7
#define LA_3R_AMOR_DB_W 0x70d8
#define LA_3R_AMOR_DB_D 0x70d9
#define LA_3R_AMXOR_DB_W 0x70da
#define LA_3R_AMXOR_DB_D 0x70db
#define LA_3R_AMMAX_DB_W 0x70dc
#define LA_3R_AMMAX_DB_D 0x70dd
#define LA_3R_AMMIN_DB_W 0x70de
#define LA_3R_AMMIN_DB_D 0x70df
#define LA_3R_AMMAX_DB_WU 0x70e0
#define LA_3R_AMMAX_DB_DU 0x70e1
#define LA_3R_AMMIN_DB_WU 0x70e2
#define LA_3R_AMMIN_DB_DU 0x70e3
#define LA_3R_FLDGT_S 0x70e8
#define LA_3R_FLDGT_D 0x70e9
#define LA_3R_FLDLE_S 0x70ea
#define LA_3R_FLDLE_D 0x70eb
#define LA_3R_FSTGT_S 0x70ec
#define LA_3R_FSTGT_D 0x70ed
#define LA_3R_FSTLE_S 0x70ee
#define LA_3R_FSTLE_D 0x70ef
#define LA_3R_LDGT_B 0x70f0
#define LA_3R_LDGT_H 0x70f1
#define LA_3R_LDGT_W 0x70f2
#define LA_3R_LDGT_D 0x70f3
#define LA_3R_LDLE_B 0x70f4
#define LA_3R_LDLE_H 0x70f5
#define LA_3R_LDLE_W 0x70f6
#define LA_3R_LDLE_D 0x70f7
#define LA_3R_STGT_B 0x70f8
#define LA_3R_STGT_H 0x70f9
#define LA_3R_STGT_W 0x70fa
#define LA_3R_STGT_D 0x70fb
#define LA_3R_STLE_B 0x70fc
#define LA_3R_STLE_H 0x70fd
#define LA_3R_STLE_W 0x70fe
#define LA_3R_STLE_D 0x70ff

////LA_OP_4R opcode: bit31 ~ bit20
#define LA_4R_FMADD_S 0x81
#define LA_4R_FMADD_D 0x82
#define LA_4R_FMSUB_S 0x85
#define LA_4R_FMSUB_D 0x86
#define LA_4R_FNMADD_S 0x89
#define LA_4R_FNMADD_D 0x8a
#define LA_4R_FNMSUB_S 0x8d
#define LA_4R_FNMSUB_D 0x8e
#define LA_4R_FSEL 0xd0

////LA_OP_2RI8

////LA_OP_2RI12 opcode: bit31 ~ bit22
#define LA_2RI12_SLTI 0x8
#define LA_2RI12_SLTUI 0x9
#define LA_2RI12_ADDI_W 0xa
#define LA_2RI12_ADDI_D 0xb
#define LA_2RI12_LU52I_D 0xc
#define LA_2RI12_ANDI 0xd
#define LA_2RI12_ORI 0xe
#define LA_2RI12_XORI 0xf
#define LA_2RI12_CACHE 0x18
#define LA_2RI12_LD_B 0xa0
#define LA_2RI12_LD_H 0xa1
#define LA_2RI12_LD_W 0xa2
#define LA_2RI12_LD_D 0xa3
#define LA_2RI12_ST_B 0xa4
#define LA_2RI12_ST_H 0xa5
#define LA_2RI12_ST_W 0xa6
#define LA_2RI12_ST_D 0xa7
#define LA_2RI12_LD_BU 0xa8
#define LA_2RI12_LD_HU 0xa9
#define LA_2RI12_LD_WU 0xaa
#define LA_2RI12_PRELD 0xab
#define LA_2RI12_FLD_S 0xac
#define LA_2RI12_FST_S 0xad
#define LA_2RI12_FLD_D 0xae
#define LA_2RI12_FST_D 0xaf

////LA_OP_2RI14i opcode: bit31 ~ bit24
#define LA_2RI14_LL_W 0x20
#define LA_2RI14_SC_W 0x21
#define LA_2RI14_LL_D 0x22
#define LA_2RI14_SC_D 0x23
#define LA_2RI14_LDPTR_W 0x24
#define LA_2RI14_STPTR_W 0x25
#define LA_2RI14_LDPTR_D 0x26
#define LA_2RI14_STPTR_D 0x27

////LA_OP_2RI16 opcode: bit31 ~ bit26
#define LA_2RI16_ADDU16I_D 0x4
#define LA_2RI16_JIRL 0x13
#define LA_2RI16_BEQ 0x16
#define LA_2RI16_BNE 0x17
#define LA_2RI16_BLT 0x18
#define LA_2RI16_BGE 0x19
#define LA_2RI16_BLTU 0x1a
#define LA_2RI16_BGEU 0x1b

////LA_OP_1RI20 opcode: bit31 ~ bit25
#define LA_1RI20_LU12I_W 0xa
#define LA_1RI20_LU32I_D 0xb
#define LA_1RI20_PCADDI 0xc
#define LA_1RI20_PCALAU12I 0xd
#define LA_1RI20_PCADDU12I 0xe
#define LA_1RI20_PCADDU18I 0xf

////LA_OP_I26
#define LA_I26_B 0x14
#define LA_I26_BL 0x15

////LA_OP_1RI21
#define LA_1RI21_BEQZ 0x10
#define LA_1RI21_BNEZ 0x11
#define LA_1RI21_BCEQZ 0x12
#define LA_1RI21_BCNEZ 0x12

////other
#define LA_OP_ALSL_W 0x1
#define LA_OP_ALSL_WU 0x1
#define LA_OP_ALSL_D 0xb
#define LA_OP_BYTEPICK_W 0x2
#define LA_OP_BYTEPICK_D 0x3
#define LA_OP_BREAK 0x54
#define LA_OP_DBGCALL 0x55
#define LA_OP_SYSCALL 0x56
#define LA_OP_SLLI_W 0x10
#define LA_OP_SLLI_D 0x10
#define LA_OP_SRLI_W 0x11
#define LA_OP_SRLI_D 0x11
#define LA_OP_SRAI_W 0x12
#define LA_OP_SRAI_D 0x12
#define LA_OP_ROTRI_W 0x13
#define LA_OP_ROTRI_D 0x13
#define LA_OP_FCMP_cond_S 0xc1
#define LA_OP_FCMP_cond_D 0xc2
#define LA_OP_BSTRINS_W 0x1
#define LA_OP_BSTRPICK_W 0x1
#define LA_OP_BSTRINS_D 0x2
#define LA_OP_BSTRPICK_D 0x3
#define LA_OP_DBAR 0x70e4
#define LA_OP_IBAR 0x70e5

//// add other define-macro here.

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
    NYI_LOONGARCH64("emitInsToJumpKind-----unimplemented on LOONGARCH64 yet----");
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
    if (emitIsScnsInsDsc(id))
        return SMALL_IDSC_SIZE;

    insOpts insOp = id->idInsOpt();

    switch (insOp)
    {
        case INS_OPTS_JIRL:
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

inline bool emitter::emitInsMayWriteToGCReg(instruction ins)
{
    assert(ins != INS_invalid);
    // NOTE: please reference the file "instrsloongarch64.h" for details !!!
    return (INS_mov <= ins) && (ins <= INS_jirl) ? true : false;
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
        case INS_st_d:
        case INS_st_w:
        case INS_st_b:
        case INS_st_h:
        case INS_stptr_d:
        case INS_stx_d:
        case INS_stx_w:
        case INS_stx_b:
        case INS_stx_h:
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

#undef LD
#undef ST

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
    // instrDesc* id  = emitNewInstrSmall(EA_8BYTE);
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
 *  Special notes for LoongArch64:
 *    The parameter `offs` has special info.
 *
 *    (1) The real value of `offs` is positive. `offs` = `offs`.
 *
 *    (2) If the `offs` is negtive, `offs` = -(offs),
 *        the negtive `offs` is special for optimizing the large offset which >2047.
 *        when offs >2047 we can't encode one instruction to load/store the data,
 *        if there are several load/store at this case, you have to repeat the similar
 *        large offs with reduntant instructions and maybe eat up the `emitIGbuffSize`.
 *
 *    Before optimizing the following instructions:
 *      lu12i.w  x0, 0x0
 *      ori  x0, x0, 0x9ac
 *      add.d  x0, x0, fp
 *      fst.s  fa0, x0, 0
 *
 *    After optimized the instructions:
 *      For the offs within range [0,0x7ff], using one instruction:
 *        ori  x0, x0, offs
 *      For the offs within range [0x1000,0xffffffff], using two instruction
 *        lu12i.w  x0, offs-hi-20bits
 *        ori  x0, x0, offs-low-12bits
 *
 *      Then Store/Load the data:
 *        fstx.s  fa0, x0, fp
 *
 *    If storing/loading the second field of a struct,
 *      addi_d  x0,x0,sizeof(type)
 *      fstx.s  fa0, x0, fp
 *
 */
void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs)
{
    ssize_t imm;

    emitAttr size = EA_SIZE(attr);

#ifdef DEBUG
    switch (ins)
    {
        case INS_st_d:
        case INS_stx_d:
        case INS_st_w:
        case INS_stx_w:
        case INS_fst_s:
        case INS_fst_d:
        case INS_fstx_s:
        case INS_fstx_d:
        case INS_st_b:
        case INS_st_h:
        case INS_stx_b:
        case INS_stx_h:
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
    regNumber reg2 = offs < 0 ? REG_R21 : reg3;
    offs           = offs < 0 ? -offs - 8 : offs;

    if ((-2048 <= imm) && (imm < 2048))
    {
        // regs[1] = reg2;
    }
    else
    {
        ssize_t imm3 = imm & 0x800;
        ssize_t imm2 = imm + imm3;
        assert(isValidSimm20(imm2 >> 12));
        emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_RA, imm2 >> 12);

        emitIns_R_R_R(INS_add_d, EA_PTRSIZE, REG_RA, REG_RA, reg2);

        imm2 = imm2 & 0x7ff;
        imm  = imm3 ? imm2 - imm3 : imm2;

        reg2 = REG_RA;
    }

    instrDesc* id = emitNewInstr(attr);

    id->idReg1(reg1);

    id->idReg2(reg2);

    id->idIns(ins);

    code_t code = emitInsCode(ins);
    code |= (code_t)(reg1 & 0x1f);
    code |= (code_t)reg2 << 5;
    if ((ins == INS_stx_d) || (ins == INS_stx_w) || (ins == INS_stx_h) || (ins == INS_stx_b) || (ins == INS_fstx_d) ||
        (ins == INS_fstx_s))
    {
        code |= (code_t)reg3 << 10;
    }
    else
    {
        code |= (code_t)(imm & 0xfff) << 10;
    }

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
        case INS_ld_b:
        case INS_ld_bu:

        case INS_ld_h:
        case INS_ld_hu:

        case INS_ld_w:
        case INS_ld_wu:
        case INS_fld_s:

        case INS_ld_d:
        case INS_fld_d:

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
    reg2           = offs < 0 ? REG_R21 : reg2;
    offs           = offs < 0 ? -offs - 8 : offs;

    reg1 = (regNumber)((char)reg1 & 0x1f);
    code_t code;
    if ((-2048 <= imm) && (imm < 2048))
    {
        if (ins == INS_lea)
        {
            ins = INS_addi_d;
        }
        code = emitInsCode(ins);
        code |= (code_t)(reg1 & 0x1f);
        code |= (code_t)reg2 << 5;
        code |= (imm & 0xfff) << 10;
    }
    else
    {
        if (ins == INS_lea)
        {
            assert(isValidSimm20(imm >> 12));
            emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_RA, imm >> 12);
            ssize_t imm2 = imm & 0xfff;
            emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_RA, REG_RA, imm2);

            ins  = INS_add_d;
            code = emitInsCode(ins);
            code |= (code_t)reg1;
            code |= (code_t)reg2 << 5;
            code |= (code_t)REG_RA << 10;
        }
        else
        {
            ssize_t imm3 = imm & 0x800;
            ssize_t imm2 = imm + imm3;
            assert(isValidSimm20(imm2 >> 12));
            emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_RA, imm2 >> 12);

            emitIns_R_R_R(INS_add_d, EA_PTRSIZE, REG_RA, REG_RA, reg2);

            imm2 = imm2 & 0x7ff;
            imm3 = imm3 ? imm2 - imm3 : imm2;
            code = emitInsCode(ins);
            code |= (code_t)reg1;
            code |= (code_t)REG_RA << 5;
            code |= (code_t)(imm3 & 0xfff) << 10;
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
        case INS_b:
        case INS_bl:
            assert(!(imm & 0x3));
            code |= ((imm >> 18) & 0x3ff);       // offs[25:16]
            code |= ((imm >> 2) & 0xffff) << 10; // offs[15:0]
            break;
        case INS_dbar:
        case INS_ibar:
            assert((0 <= imm) && (imm <= 0x7fff));
            code |= (imm & 0x7fff); // hint
            break;
        default:
            unreached();
    }

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
}

void emitter::emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs)
{
#ifdef DEBUG
    switch (ins)
    {
        case INS_bceqz:
        case INS_bcnez:
            break;

        default:
            unreached();
    }
#endif

    code_t code = emitInsCode(ins);

    assert(!(offs & 0x3));
    assert(!(cc >> 3));
    code |= ((cc & 0x7) << 5);            // cj
    code |= ((offs >> 18) & 0x1f);        // offs[20:16]
    code |= ((offs >> 2) & 0xffff) << 10; // offs[15:0]

    instrDesc* id = emitNewInstr(attr);

    id->idIns(ins);
    id->idAddr()->iiaSetInstrEncode(code);
    id->idCodeSize(4);

    appendToCurIG(id);
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
        case INS_lu12i_w:
        case INS_lu32i_d:
        case INS_pcaddi:
        case INS_pcalau12i:
        case INS_pcaddu12i:
        case INS_pcaddu18i:
            assert(isGeneralRegister(reg));
            assert((-524288 <= imm) && (imm < 524288));

            code |= reg;                  // rd
            code |= (imm & 0xfffff) << 5; // si20
            break;
        case INS_beqz:
        case INS_bnez:
            assert(isGeneralRegisterOrR0(reg));
            assert(!(imm & 0x3));
            assert((-1048576 <= (imm >> 2)) && ((imm >> 2) <= 1048575));

            code |= ((imm >> 18) & 0x1f);        // offs[20:16]
            code |= reg << 5;                    // rj
            code |= ((imm >> 2) & 0xffff) << 10; // offs[15:0]
            break;
        case INS_movfr2cf:
            assert(isFloatReg(reg));
            assert((0 <= imm) && (imm <= 7));

            code |= (reg & 0x1f) << 5; // fj
            code |= imm;               // cc
            break;
        case INS_movcf2fr:
            assert(isFloatReg(reg));
            assert((0 <= imm) && (imm <= 7));

            code |= (reg & 0x1f); // fd
            code |= imm << 5;     // cc
            break;
        case INS_movgr2cf:
            assert(isGeneralRegister(reg));
            assert((0 <= imm) && (imm <= 7));

            code |= reg << 5; // rj
            code |= imm;      // cc
            break;
        case INS_movcf2gr:
            assert(isGeneralRegister(reg));
            assert((0 <= imm) && (imm <= 7));

            code |= reg;      // rd
            code |= imm << 5; // cc
            break;
        default:
            unreached();
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
    assert(IsMovInstruction(ins));

    if (!canSkip || (dstReg != srcReg))
    {
        if ((EA_4BYTE == attr) && (INS_mov == ins))
            emitIns_R_R_I(INS_slli_w, attr, dstReg, srcReg, 0);
        else
            emitIns_R_R(ins, attr, dstReg, srcReg);
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
        code |= reg1;      // rd
        code |= reg2 << 5; // rj
    }
    else if ((INS_ext_w_b <= ins) && (ins <= INS_cpucfg))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_ext_w_b:
            case INS_ext_w_h:
            case INS_clo_w:
            case INS_clz_w:
            case INS_cto_w:
            case INS_ctz_w:
            case INS_clo_d:
            case INS_clz_d:
            case INS_cto_d:
            case INS_ctz_d:
            case INS_revb_2h:
            case INS_revb_4h:
            case INS_revb_2w:
            case INS_revb_d:
            case INS_revh_2w:
            case INS_revh_d:
            case INS_bitrev_4b:
            case INS_bitrev_8b:
            case INS_bitrev_w:
            case INS_bitrev_d:
            case INS_rdtimel_w:
            case INS_rdtimeh_w:
            case INS_rdtime_d:
            case INS_cpucfg:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R --1!");
        }
#endif
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= reg1;      // rd
        code |= reg2 << 5; // rj
    }
    else if ((INS_asrtle_d == ins) || (INS_asrtgt_d == ins))
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= reg1 << 5;  // rj
        code |= reg2 << 10; // rk
    }
    else if ((INS_fabs_s <= ins) && (ins <= INS_fmov_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_fabs_s:
            case INS_fabs_d:
            case INS_fneg_s:
            case INS_fneg_d:
            case INS_fsqrt_s:
            case INS_fsqrt_d:
            case INS_frsqrt_s:
            case INS_frsqrt_d:
            case INS_frecip_s:
            case INS_frecip_d:
            case INS_flogb_s:
            case INS_flogb_d:
            case INS_fclass_s:
            case INS_fclass_d:
            case INS_fcvt_s_d:
            case INS_fcvt_d_s:
            case INS_ffint_s_w:
            case INS_ffint_s_l:
            case INS_ffint_d_w:
            case INS_ffint_d_l:
            case INS_ftint_w_s:
            case INS_ftint_w_d:
            case INS_ftint_l_s:
            case INS_ftint_l_d:
            case INS_ftintrm_w_s:
            case INS_ftintrm_w_d:
            case INS_ftintrm_l_s:
            case INS_ftintrm_l_d:
            case INS_ftintrp_w_s:
            case INS_ftintrp_w_d:
            case INS_ftintrp_l_s:
            case INS_ftintrp_l_d:
            case INS_ftintrz_w_s:
            case INS_ftintrz_w_d:
            case INS_ftintrz_l_s:
            case INS_ftintrz_l_d:
            case INS_ftintrne_w_s:
            case INS_ftintrne_w_d:
            case INS_ftintrne_l_s:
            case INS_ftintrne_l_d:
            case INS_frint_s:
            case INS_frint_d:
            case INS_fmov_s:
            case INS_fmov_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R --2!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isFloatReg(reg2));
        code |= (reg1 & 0x1f);      // fd
        code |= (reg2 & 0x1f) << 5; // fj
    }
    else if ((INS_movgr2fr_w <= ins) && (ins <= INS_movgr2frh_w))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_movgr2fr_w:
            case INS_movgr2fr_d:
            case INS_movgr2frh_w:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R --3!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        code |= (reg1 & 0x1f); // fd
        code |= reg2 << 5;     // rj
    }
    else if ((INS_movfr2gr_s <= ins) && (ins <= INS_movfrh2gr_s))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_movfr2gr_s:
            case INS_movfr2gr_d:
            case INS_movfrh2gr_s:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R --4!");
        }
#endif
        assert(isGeneralRegisterOrR0(reg1));
        assert(isFloatReg(reg2));
        code |= reg1;               // rd
        code |= (reg2 & 0x1f) << 5; // fj
    }
    else if ((INS_dneg == ins) || (INS_neg == ins))
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        // sub_d rd, zero, rk
        // sub_w rd, zero, rk
        code |= reg1;       // rd
        code |= reg2 << 10; // rk
    }
    else if (INS_not == ins)
    {
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        // nor rd, rj, zero
        code |= reg1;      // rd
        code |= reg2 << 5; // rj
    }
    else
    {
        unreached();
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
    code_t code = emitInsCode(ins);

    if ((INS_slli_w <= ins) && (ins <= INS_rotri_w))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_slli_w:
            case INS_srli_w:
            case INS_srai_w:
            case INS_rotri_w:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --1!");
        }
#endif

        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((0 <= imm) && (imm <= 0x1f));

        code |= reg1;               // rd
        code |= reg2 << 5;          // rj
        code |= (imm & 0x1f) << 10; // ui5
    }
    else if ((INS_slli_d <= ins) && (ins <= INS_rotri_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_slli_d:
            case INS_srli_d:
            case INS_srai_d:
            case INS_rotri_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --2!");
        }
#endif
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((0 <= imm) && (imm <= 0x3f));

        code |= reg1;               // rd
        code |= reg2 << 5;          // rj
        code |= (imm & 0x3f) << 10; // ui6
    }
    else if (((INS_addi_w <= ins) && (ins <= INS_xori)) || ((INS_ld_b <= ins) && (ins <= INS_ld_wu)) ||
             ((INS_st_b <= ins) && (ins <= INS_st_d)))
    {
#ifdef DEBUG
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        if (((INS_addi_w <= ins) && (ins <= INS_slti)) || ((INS_ld_b <= ins) && (ins <= INS_ld_wu)) ||
            ((INS_st_b <= ins) && (ins <= INS_st_d)))
        {
            switch (ins)
            {
                case INS_addi_w:
                case INS_addi_d:
                case INS_lu52i_d:
                case INS_slti:
                case INS_ld_b:
                case INS_ld_h:
                case INS_ld_w:
                case INS_ld_d:
                case INS_ld_bu:
                case INS_ld_hu:
                case INS_ld_wu:
                case INS_st_b:
                case INS_st_h:
                case INS_st_w:
                case INS_st_d:
                    break;
                default:
                    NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --3!");
            }

            assert((-2048 <= imm) && (imm <= 2047));
        }
        else if (ins == INS_sltui)
        {
            assert((0 <= imm) && (imm <= 0x7ff));
        }
        else
        {
            switch (ins)
            {
                case INS_andi:
                case INS_ori:
                case INS_xori:
                    break;
                default:
                    NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --4!");
            }
            assert((0 <= imm) && (imm <= 0xfff));
        }
#endif
        code |= reg1;                // rd
        code |= reg2 << 5;           // rj
        code |= (imm & 0xfff) << 10; // si12 or ui12
    }
    else if ((INS_fld_s <= ins) && (ins <= INS_fst_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_fld_s:
            case INS_fld_d:
            case INS_fst_s:
            case INS_fst_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --5!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-2048 <= imm) && (imm <= 2047));

        code |= reg1 & 0x1f;         // fd
        code |= reg2 << 5;           // rj
        code |= (imm & 0xfff) << 10; // si12
    }
    else if (((INS_ll_d >= ins) && (ins >= INS_ldptr_w)) || ((INS_sc_d >= ins) && (ins >= INS_stptr_w)))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_ldptr_w:
            case INS_ldptr_d:
            case INS_ll_w:
            case INS_ll_d:
            case INS_stptr_w:
            case INS_stptr_d:
            case INS_sc_w:
            case INS_sc_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --6!");
        }
#endif
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-8192 <= imm) && (imm <= 8191));

        code |= reg1;                 // rd
        code |= reg2 << 5;            // rj
        code |= (imm & 0x3fff) << 10; // si14
    }
    else if ((INS_beq <= ins) && (ins <= INS_bgeu))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_beq:
            case INS_bne:
            case INS_blt:
            case INS_bltu:
            case INS_bge:
            case INS_bgeu:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --7!");
        }
#endif
        assert(isGeneralRegisterOrR0(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(!(imm & 0x3));
        assert((-32768 <= (imm >> 2)) && ((imm >> 2) <= 32767));

        code |= reg1 << 5;                   // rj
        code |= reg2;                        // rd
        code |= ((imm >> 2) & 0xffff) << 10; // offs16
    }
    else if ((INS_fcmp_caf_s <= ins) && (ins <= INS_fcmp_sune_s))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_fcmp_caf_s:
            case INS_fcmp_cun_s:
            case INS_fcmp_ceq_s:
            case INS_fcmp_cueq_s:
            case INS_fcmp_clt_s:
            case INS_fcmp_cult_s:
            case INS_fcmp_cle_s:
            case INS_fcmp_cule_s:
            case INS_fcmp_cne_s:
            case INS_fcmp_cor_s:
            case INS_fcmp_cune_s:
            case INS_fcmp_saf_d:
            case INS_fcmp_sun_d:
            case INS_fcmp_seq_d:
            case INS_fcmp_sueq_d:
            case INS_fcmp_slt_d:
            case INS_fcmp_sult_d:
            case INS_fcmp_sle_d:
            case INS_fcmp_sule_d:
            case INS_fcmp_sne_d:
            case INS_fcmp_sor_d:
            case INS_fcmp_sune_d:
            case INS_fcmp_caf_d:
            case INS_fcmp_cun_d:
            case INS_fcmp_ceq_d:
            case INS_fcmp_cueq_d:
            case INS_fcmp_clt_d:
            case INS_fcmp_cult_d:
            case INS_fcmp_cle_d:
            case INS_fcmp_cule_d:
            case INS_fcmp_cne_d:
            case INS_fcmp_cor_d:
            case INS_fcmp_cune_d:
            case INS_fcmp_saf_s:
            case INS_fcmp_sun_s:
            case INS_fcmp_seq_s:
            case INS_fcmp_sueq_s:
            case INS_fcmp_slt_s:
            case INS_fcmp_sult_s:
            case INS_fcmp_sle_s:
            case INS_fcmp_sule_s:
            case INS_fcmp_sne_s:
            case INS_fcmp_sor_s:
            case INS_fcmp_sune_s:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_I --8!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isFloatReg(reg2));
        assert((0 <= imm) && (imm <= 7));

        code |= (reg1 & 0x1f) << 5;  // fj
        code |= (reg2 & 0x1f) << 10; // fk
        code |= imm & 0x7;           // cc
    }
    else if (INS_addu16i_d == ins)
    {
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-32768 <= imm) && (imm < 32768));

        code |= reg1;                 // rd
        code |= reg2 << 5;            // rj
        code |= (imm & 0xffff) << 10; // si16
    }
    else if (INS_jirl == ins)
    {
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-32768 <= imm) && (imm < 32768));

        code |= reg1;                 // rd
        code |= reg2 << 5;            // rj
        code |= (imm & 0xffff) << 10; // offs16
    }
    else
    {
        unreached();
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
 *  Add an instruction referencing three registers.
 */

void emitter::emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt) /* = INS_OPTS_NONE */
{
    code_t code = emitInsCode(ins);

    if (((INS_add_w <= ins) && (ins <= INS_crcc_w_d_w)) || ((INS_ldx_b <= ins) && (ins <= INS_ldle_d)) ||
        ((INS_stx_b <= ins) && (ins <= INS_stle_d)))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_add_w:
            case INS_add_d:
            case INS_sub_w:
            case INS_sub_d:
            case INS_and:
            case INS_or:
            case INS_nor:
            case INS_xor:
            case INS_andn:
            case INS_orn:

            case INS_mul_w:
            case INS_mul_d:
            case INS_mulh_w:
            case INS_mulh_wu:
            case INS_mulh_d:
            case INS_mulh_du:
            case INS_mulw_d_w:
            case INS_mulw_d_wu:
            case INS_div_w:
            case INS_div_wu:
            case INS_div_d:
            case INS_div_du:
            case INS_mod_w:
            case INS_mod_wu:
            case INS_mod_d:
            case INS_mod_du:

            case INS_sll_w:
            case INS_srl_w:
            case INS_sra_w:
            case INS_rotr_w:
            case INS_sll_d:
            case INS_srl_d:
            case INS_sra_d:
            case INS_rotr_d:

            case INS_maskeqz:
            case INS_masknez:

            case INS_slt:
            case INS_sltu:

            case INS_ldx_b:
            case INS_ldx_h:
            case INS_ldx_w:
            case INS_ldx_d:
            case INS_ldx_bu:
            case INS_ldx_hu:
            case INS_ldx_wu:
            case INS_stx_b:
            case INS_stx_h:
            case INS_stx_w:
            case INS_stx_d:

            case INS_ldgt_b:
            case INS_ldgt_h:
            case INS_ldgt_w:
            case INS_ldgt_d:
            case INS_ldle_b:
            case INS_ldle_h:
            case INS_ldle_w:
            case INS_ldle_d:
            case INS_stgt_b:
            case INS_stgt_h:
            case INS_stgt_w:
            case INS_stgt_d:
            case INS_stle_b:
            case INS_stle_h:
            case INS_stle_w:
            case INS_stle_d:

            case INS_amswap_w:
            case INS_amswap_d:
            case INS_amswap_db_w:
            case INS_amswap_db_d:
            case INS_amadd_w:
            case INS_amadd_d:
            case INS_amadd_db_w:
            case INS_amadd_db_d:
            case INS_amand_w:
            case INS_amand_d:
            case INS_amand_db_w:
            case INS_amand_db_d:
            case INS_amor_w:
            case INS_amor_d:
            case INS_amor_db_w:
            case INS_amor_db_d:
            case INS_amxor_w:
            case INS_amxor_d:
            case INS_amxor_db_w:
            case INS_amxor_db_d:
            case INS_ammax_w:
            case INS_ammax_d:
            case INS_ammax_db_w:
            case INS_ammax_db_d:
            case INS_ammin_w:
            case INS_ammin_d:
            case INS_ammin_db_w:
            case INS_ammin_db_d:
            case INS_ammax_wu:
            case INS_ammax_du:
            case INS_ammax_db_wu:
            case INS_ammax_db_du:
            case INS_ammin_wu:
            case INS_ammin_du:
            case INS_ammin_db_wu:
            case INS_ammin_db_du:

            case INS_crc_w_b_w:
            case INS_crc_w_h_w:
            case INS_crc_w_w_w:
            case INS_crc_w_d_w:
            case INS_crcc_w_b_w:
            case INS_crcc_w_h_w:
            case INS_crcc_w_w_w:
            case INS_crcc_w_d_w:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_R --1!");
        }
#endif
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= (reg1 /*& 0x1f*/);       // rd
        code |= (reg2 /*& 0x1f*/) << 5;  // rj
        code |= (reg3 /*& 0x1f*/) << 10; // rk
    }
    else if ((INS_fadd_s <= ins) && (ins <= INS_fcopysign_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_fadd_s:
            case INS_fadd_d:
            case INS_fsub_s:
            case INS_fsub_d:
            case INS_fmul_s:
            case INS_fmul_d:
            case INS_fdiv_s:
            case INS_fdiv_d:
            case INS_fmax_s:
            case INS_fmax_d:
            case INS_fmin_s:
            case INS_fmin_d:
            case INS_fmaxa_s:
            case INS_fmaxa_d:
            case INS_fmina_s:
            case INS_fmina_d:
            case INS_fscaleb_s:
            case INS_fscaleb_d:
            case INS_fcopysign_s:
            case INS_fcopysign_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_R --2!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isFloatReg(reg2));
        assert(isFloatReg(reg3));

        code |= (reg1 & 0x1f);       // fd
        code |= (reg2 & 0x1f) << 5;  // fj
        code |= (reg3 & 0x1f) << 10; // fk
    }
    else if ((INS_fldx_s <= ins) && (ins <= INS_fstle_d))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_fldx_s:
            case INS_fldx_d:
            case INS_fstx_s:
            case INS_fstx_d:

            case INS_fldgt_s:
            case INS_fldgt_d:
            case INS_fldle_s:
            case INS_fldle_d:
            case INS_fstgt_s:
            case INS_fstgt_d:
            case INS_fstle_s:
            case INS_fstle_d:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_R --3!");
        }
#endif
        assert(isFloatReg(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= reg1 & 0x1f; // fd
        code |= reg2 << 5;   // rj
        code |= reg3 << 10;  // rk
    }
    else
    {
        NYI_LOONGARCH64("Unsupported instruction in emitIns_R_R_R");
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
    code_t code = emitInsCode(ins);

    if ((INS_alsl_w <= ins) && (ins <= INS_bytepick_w))
    {
#ifdef DEBUG
        switch (ins)
        {
            case INS_alsl_w:
            case INS_alsl_wu:
            case INS_alsl_d:
            case INS_bytepick_w:
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R --4!");
        }
#endif
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));
        assert((0 <= imm) && (imm <= 3));

        code |= reg1;       // rd
        code |= reg2 << 5;  // rj
        code |= reg3 << 10; // rk
        code |= imm << 15;  // sa2
    }
    else if (INS_bytepick_d == ins)
    {
        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));
        assert((0 <= imm) && (imm <= 7));

        code |= reg1;       // rd
        code |= reg2 << 5;  // rj
        code |= reg3 << 10; // rk
        code |= imm << 15;  // sa3
    }
    else if (INS_fsel == ins)
    {
        assert(isFloatReg(reg1));
        assert(isFloatReg(reg2));
        assert(isFloatReg(reg3));
        assert((0 <= imm) && (imm <= 7));

        code |= (reg1 & 0x1f);       // fd
        code |= (reg2 & 0x1f) << 5;  // fj
        code |= (reg3 & 0x1f) << 10; // fk
        code |= imm << 15;           // ca
    }
    else
    {
        unreached();
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
 *  Add an instruction referencing two registers and two constants.
 */

void emitter::emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt)
{
    code_t code = emitInsCode(ins);

    assert(isGeneralRegisterOrR0(reg1));
    assert(isGeneralRegisterOrR0(reg2));
    switch (ins)
    {
        case INS_bstrins_w:
        case INS_bstrpick_w:
            code |= (reg1 /*& 0x1f*/);      // rd
            code |= (reg2 /*& 0x1f*/) << 5; // rj
            assert((0 <= imm2) && (imm2 <= imm1) && (imm1 < 32));
            code |= (imm1 & 0x1f) << 16; // msbw
            code |= (imm2 & 0x1f) << 10; // lsbw
            break;
        case INS_bstrins_d:
        case INS_bstrpick_d:
            code |= (reg1 /*& 0x1f*/);      // rd
            code |= (reg2 /*& 0x1f*/) << 5; // rj
            assert((0 <= imm2) && (imm2 <= imm1) && (imm1 < 64));
            code |= (imm1 & 0x3f) << 16; // msbd
            code |= (imm2 & 0x3f) << 10; // lsbd
            break;
        default:
            unreached();
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
 *  Add an instruction referencing four registers.
 */

void emitter::emitIns_R_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4)
{
    code_t code = emitInsCode(ins);

    switch (ins)
    {
        case INS_fmadd_s:
        case INS_fmadd_d:
        case INS_fmsub_s:
        case INS_fmsub_d:
        case INS_fnmadd_s:
        case INS_fnmadd_d:
        case INS_fnmsub_s:
        case INS_fnmsub_d:
            assert(isFloatReg(reg1));
            assert(isFloatReg(reg2));
            assert(isFloatReg(reg3));
            assert(isFloatReg(reg4));

            code |= (reg1 & 0x1f);       // fd
            code |= (reg2 & 0x1f) << 5;  // fj
            code |= (reg3 & 0x1f) << 10; // fk
            code |= (reg4 & 0x1f) << 15; // fa
            break;
        default:
            unreached();
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
 *  Add an instruction with a register + static member operands.
 *  Constant is stored into JIT data which is adjacent to code.
 *  For LOONGARCH64, maybe not the best, here just supports the func-interface.
 *
 */
void emitter::emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber addrReg, CORINFO_FIELD_HANDLE fldHnd, int offs)
{
    assert(offs >= 0);
    assert(instrDesc::fitsInSmallCns(offs)); // can optimize.

    // when id->idIns == bl, for reloc! 4-ins.
    //   pcaddu12i reg, off-hi-20bits
    //   addi_d  reg, reg, off-lo-12bits
    // when id->idIns == load-ins, for reloc! 4-ins.
    //   pcaddu12i reg, off-hi-20bits
    //   load  reg, offs_lo-12bits(reg)
    //
    // INS_OPTS_RC: ins == bl placeholders.  3-ins:
    //   lu12i_w r21, addr_bits[31:12]
    //   ori     reg, r21, addr_bits[11:0]
    //   lu32i_d reg, addr_bits[50:32]
    //
    // INS_OPTS_RC: ins == load.  3-ins:
    //   lu12i_w r21, addr_bits[31:12]
    //   lu32i_d r21, addr_bits[50:32]
    //   load  reg, r21 + addr_bits[11:0]

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
    {
        id->idCodeSize(12);
    }

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

    // TODO-LoongArch64: this maybe deleted.
    id->idSetIsBound(); // We won't patch address since we will know the exact distance
                        // once JIT code and data are allocated together.

    assert(addrReg == REG_NA); // NOTE: for LOONGARCH64, not support addrReg != REG_NA.

    id->idAddr()->iiaFieldHnd = fldHnd;

    appendToCurIG(id);
}

void emitter::emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs)
{
    NYI_LOONGARCH64("emitIns_R_AR-----unimplemented/unused on LOONGARCH64 yet----");
}

// This computes address from the immediate which is relocatable.
void emitter::emitIns_R_AI(instruction ins,
                           emitAttr    attr,
                           regNumber   reg,
                           ssize_t addr DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    assert(EA_IS_RELOC(attr)); // EA_PTR_DSP_RELOC
    assert(ins == INS_bl);     // for special.
    assert(isGeneralRegister(reg));

    // INS_OPTS_RELOC: placeholders.  2-ins:
    //  case:EA_HANDLE_CNS_RELOC
    //   pcaddu12i  reg, off-hi-20bits
    //   addi_d  reg, reg, off-lo-12bits
    //  case:EA_PTR_DSP_RELOC
    //   pcaddu12i  reg, off-hi-20bits
    //   ld_d  reg, reg, off-lo-12bits

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
    // TODO-LoongArch64: maybe delete it on future.
    NYI_LOONGARCH64("emitSetShortJump-----unimplemented/unused on LOONGARCH64 yet----");
}

/*****************************************************************************
 *
 *  Add a label instruction.
 */

void emitter::emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    assert(dst->bbFlags & BBF_HAS_LABEL);

    // if for reloc!  4-ins:
    //   pcaddu12i reg, offset-hi20
    //   addi_d  reg, reg, offset-lo12
    //
    // else:  3-ins:
    //   lu12i_w r21, addr_bits[31:12]
    //   ori     reg, r21, addr_bits[11:0]
    //   lu32i_d reg, addr_bits[50:32]

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
    {
        id->idCodeSize(12);
    }

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
    if (emitComp->compCurBB->bbJumpKind == BBJ_EHCATCHRET)
    {
        id->idDebugOnlyInfo()->idCatchRet = true;
    }
#endif // DEBUG

    appendToCurIG(id);
}

void emitter::emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg)
{
    NYI_LOONGARCH64("emitIns_J_R-----unimplemented/unused on LOONGARCH64 yet----");
}

// NOTE:
//  For loongarch64, emitIns_J is just only jump, not include the condition branch!
//  The condition branch is the emitIns_J_cond_la().
//  If using "BasicBlock* dst" label as target, the INS_OPTS_J is a short jump while long jump will be replace by
//  INS_OPTS_JIRL.
//
//  The arg "instrCount" is two regs's encoding when ins is beq/bne/blt/bltu/bge/bgeu/beqz/bnez.
void emitter::emitIns_J(instruction ins, BasicBlock* dst, int instrCount)
{
    if (dst == nullptr)
    { // Now this case not used for loongarch64.
        assert(instrCount != 0);
        assert(ins == INS_b); // when dst==nullptr, ins is INS_b by now.

        assert((-33554432 <= instrCount) && (instrCount < 33554432)); // 0x2000000.
        emitIns_I(ins, EA_PTRSIZE, instrCount << 2); // NOTE: instrCount is the number of the instructions.

        return;
    }

    //
    // INS_OPTS_J: placeholders.  1-ins: if the dst outof-range will be replaced by INS_OPTS_JIRL.
    //   bceqz/bcnez/beq/bne/blt/bltu/bge/bgeu/beqz/bnez/b/bl  dst

    assert(dst->bbFlags & BBF_HAS_LABEL);

    instrDescJmp* id = emitNewInstrJmp();
    assert((INS_bceqz <= ins) && (ins <= INS_bl));
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

    // TODO-LoongArch64: maybe deleted this.
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

// NOTE:
//  For loongarch64, emitIns_J_cond_la() is the condition branch.
//  NOTE: Only supported short branch so far !!!
//
void emitter::emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1, regNumber reg2)
{
    // TODO-LoongArch64:
    //   Now the emitIns_J_cond_la() is only the short condition branch.
    //   There is no long condition branch for loongarch64 so far.
    //   For loongarch64, the long condition branch is like this:
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
    assert(dst->bbFlags & BBF_HAS_LABEL);

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

void emitter::emitIns_I_la(emitAttr size, regNumber reg, ssize_t imm)
{
    assert(!EA_IS_RELOC(size));
    assert(isGeneralRegister(reg));
    // size = EA_SIZE(size);

    if (-1 == (imm >> 11) || 0 == (imm >> 11))
    {
        emitIns_R_R_I(INS_addi_w, size, reg, REG_R0, imm);
        return;
    }

    if (0 == (imm >> 12))
    {
        emitIns_R_R_I(INS_ori, size, reg, REG_R0, imm);
        return;
    }

    instrDesc* id = emitNewInstr(size);

    if ((imm == INT64_MAX) || (imm == 0xffffffff))
    {
        // emitIns_R_R_I(INS_addi_d, size, reg, REG_R0, -1);
        // emitIns_R_R_I(INS_srli_d, size, reg, reg, ui6);
        id->idReg2((regNumber)1); // special for INT64_MAX(ui6=1) or UINT32_MAX(ui6=32);
        id->idCodeSize(8);
    }
    else if (-1 == (imm >> 31) || 0 == (imm >> 31))
    {
        // emitIns_R_I(INS_lu12i_w, size, reg, (imm >> 12));
        // emitIns_R_R_I(INS_ori, size, reg, reg, imm);

        id->idCodeSize(8);
    }
    else if (-1 == (imm >> 51) || 0 == (imm >> 51))
    {
        // low-32bits.
        // emitIns_R_I(INS_lu12i_w, size, reg, (imm >> 12);
        // emitIns_R_R_I(INS_ori, size, reg, reg, imm);
        //
        // high-20bits.
        // emitIns_R_I(INS_lu32i_d, size, reg, (imm>>32));

        id->idCodeSize(12);
    }
    else
    { // 0xffff ffff ffff ffff.
        // low-32bits.
        // emitIns_R_I(INS_lu12i_w, size, reg, (imm >> 12));
        // emitIns_R_R_I(INS_ori, size, reg, reg, imm);
        //
        // high-32bits.
        // emitIns_R_I(INS_lu32i_d, size, reg, (imm>>32));
        // emitIns_R_R_I(INS_lu52i_d, size, reg, reg, (imm>>52));

        id->idCodeSize(16);
    }

    id->idIns(INS_lu12i_w);
    id->idReg1(reg); // destination register that will get the constant value.
    assert(reg != REG_R0);

    id->idInsOpt(INS_OPTS_I);

    id->idAddr()->iiaAddr = (BYTE*)imm;

    appendToCurIG(id);
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
 * For LOONGARCH xreg, xmul and disp are never used and should always be 0/REG_NA.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
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

    // LoongArch64 never uses these
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

    ins = INS_jirl; // jirl t2
    id->idIns(ins);

    id->idInsOpt(INS_OPTS_C);
    // TODO-LoongArch64: maybe optimize.

    // INS_OPTS_C: placeholders.  1/2/4-ins:
    //   if (callType == EC_INDIR_R)
    //      jirl REG_R0/REG_RA, ireg, 0   <---- 1-ins
    //   else if (callType == EC_FUNC_TOKEN || callType == EC_FUNC_ADDR)
    //     if reloc:
    //             //pc + offset_38bits       # only when reloc.
    //      pcaddu18i  t2, addr-hi20
    //      jilr r0/1,t2,addr-lo18
    //
    //     else:
    //      lu12i_w  t2, dst_offset_lo32-hi
    //      ori  t2, t2, dst_offset_lo32-lo
    //      lu32i_d  t2, dst_offset_hi32-lo
    //      jirl REG_R0/REG_RA, t2, 0

    /* Record the address: method, indirection, or funcptr */
    if (callType == EC_INDIR_R)
    {
        /* This is an indirect call (either a virtual call or func ptr call) */
        // assert(callType == EC_INDIR_R);

        id->idSetIsCallRegPtr();

        regNumber reg_jirl = isJump ? REG_R0 : REG_RA;
        id->idReg4(reg_jirl);
        id->idReg3(ireg); // NOTE: for EC_INDIR_R, using idReg3.
        assert(xreg == REG_NA);

        id->idCodeSize(4);
    }
    else
    {
        /* This is a simple direct call: "call helper/method/addr" */

        assert(callType == EC_FUNC_TOKEN);
        assert(addr != NULL);
        assert((((size_t)addr) & 3) == 0);

        addr = (void*)(((size_t)addr) + (isJump ? 0 : 1)); // NOTE: low-bit0 is used for jirl ra/r0,rd,0
        id->idAddr()->iiaAddr = (BYTE*)addr;

        if (emitComp->opts.compReloc)
        {
            id->idSetIsDspReloc();
            id->idCodeSize(8);
        }
        else
        {
            id->idCodeSize(16);
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

    assert(id->idIns() == INS_jirl);
    if (id->idIsCallRegPtr())
    { // EC_INDIR_R
        code = emitInsCode(id->idIns());
        code |= (code_t)id->idReg4();
        code |= (code_t)id->idReg3() << 5;
        // the offset default is 0;
        emitOutput_Instr(dst, code);
    }
    else if (id->idIsReloc())
    {
        // pc + offset_38bits
        //
        //   pcaddu18i  t2, addr-hi20
        //   jilr r0/1,t2,addr-lo18

        emitOutput_Instr(dst, 0x1e00000e);

        size_t addr = (size_t)(id->idAddr()->iiaAddr); // get addr.

        int reg2 = (int)addr & 1;
        addr     = addr ^ 1;

        assert(isValidSimm38(addr - (ssize_t)dst));
        assert((addr & 3) == 0);

        dst += 4;
        emitGCregDeadUpd(REG_T2, dst);

#ifdef DEBUG
        code = emitInsCode(INS_pcaddu18i);
        assert((code | (14)) == 0x1e00000e);
        assert((int)REG_T2 == 14);
        code = emitInsCode(INS_jirl);
        assert(code == 0x4c000000);
#endif
        emitOutput_Instr(dst, 0x4c000000 | (14 << 5) | reg2);

        emitRecordRelocation(dst - 4, (BYTE*)addr, IMAGE_REL_LOONGARCH64_JIR);
    }
    else
    {
        // lu12i_w  t2, addr_bits[31:12]   // TODO-LoongArch64: maybe optimize.
        // ori  t2, t2, addr_bits[11:0]
        // lu32i_d  t2, addr_bits[50:32]
        // jirl  t2

        ssize_t imm = (ssize_t)(id->idAddr()->iiaAddr);
        assert((uint64_t)(imm >> 32) <= 0x7ffff); // In fact max is <= 0xffff.

        int reg2 = (int)(imm & 1);
        imm -= reg2;

        code = emitInsCode(INS_lu12i_w);
        code |= (code_t)REG_T2;
        code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

        emitOutput_Instr(dst, code);
        dst += 4;
        emitGCregDeadUpd(REG_T2, dst);

        code = emitInsCode(INS_ori);
        code |= (code_t)REG_T2;
        code |= (code_t)REG_T2 << 5;
        code |= (code_t)(imm & 0xfff) << 10;
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_lu32i_d);
        code |= (code_t)REG_T2;
        code |= ((imm >> 32) & 0x7ffff) << 5;

        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_jirl);
        code |= (code_t)reg2;
        code |= (code_t)REG_T2 << 5;
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
        // On LOONGARCH64, as on AMD64, we don't change the stack pointer to push/pop args.
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
        callInstrSize = id->idIsReloc() ? (2 << 2) : (4 << 2); // INS_OPTS_C: 2/4-ins.
    }

    return callInstrSize;
}

//----------------------------------------------------------------------------------
//  LoongArch64 has an individual implementation for emitJumpDistBind().
//
//  Bind targets of relative jumps/branch to choose the smallest possible encoding.
//  LoongArch64 has a small medium, and large encoding.
//
//  Even though the small encoding is offset-18bits which lowest 2bits is always 0.
//  The small encoding as the default is fit for most cases.
//

void emitter::emitJumpDistBind()
{
#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** In emitJumpDistBind()\n");
    }
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nInstruction list before jump distance binding:\n\n");
        emitDispIGlist(true);
    }
#endif

    instrDescJmp* jmp;

    UNATIVE_OFFSET adjIG;
    UNATIVE_OFFSET adjSJ;
    insGroup*      lstIG;
#ifdef DEBUG
    insGroup* prologIG = emitPrologIG;
#endif // DEBUG

    // NOTE:
    //  bit0 of isLinkingEnd_LA: indicating whether updating the instrDescJmp's size with the type INS_OPTS_J;
    //  bit1 of isLinkingEnd_LA: indicating not needed updating the size while emitTotalCodeSize <= (0x7fff << 2) or had
    //  updated;
    unsigned int isLinkingEnd_LA = emitTotalCodeSize <= (0x7fff << 2) ? 2 : 0;

    UNATIVE_OFFSET ssz = 0; // relative small jump's delay-slot.
    // small  jump max. neg distance
    NATIVE_OFFSET nsd = B_DIST_SMALL_MAX_NEG;
    // small  jump max. pos distance
    NATIVE_OFFSET psd =
        B_DIST_SMALL_MAX_POS -
        emitCounts_INS_OPTS_J * (3 << 2); // the max placeholder sizeof(INS_OPTS_JIRL) - sizeof(INS_OPTS_J).

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
            assert(jmp->idDebugOnlyInfo() != nullptr);
            if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[1] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                }
                printf("[1] Jump  block is at %08X\n", jmpIG->igOffs);
                printf("[1] Jump reloffset is %04X\n", jmp->idjOffs);
                printf("[1] Jump source is at %08X\n", srcEncodingOffs);
                printf("[1] Label block is at %08X\n", dstOffs);
                printf("[1] Jump  dist. is    %04X\n", jmpDist);
                if (extra > 0)
                {
                    printf("[1] Dist excess [S] = %d  \n", extra);
                }
            }
            if (EMITVERBOSE)
            {
                printf("Estimate of fwd jump [%08X/%03u]: %04X -> %04X = %04X\n", dspPtr(jmp),
                       jmp->idDebugOnlyInfo()->idNum, srcInstrOffs, dstOffs, jmpDist);
            }
#endif // DEBUG_EMIT

            assert(jmpDist >= 0); // Forward jump
            assert(!(jmpDist & 0x3));

            if (isLinkingEnd_LA & 0x2)
            {
                jmp->idAddr()->iiaSetJmpOffset(jmpDist);
            }
            else if ((extra > 0) && (jmp->idInsOpt() == INS_OPTS_J))
            {
                instruction ins = jmp->idIns();
                assert((INS_bceqz <= ins) && (ins <= INS_bl));

                if (ins <
                    INS_beqz) //   bceqz/bcnez/beq/bne/blt/bltu/bge/bgeu < beqz < bnez  // See instrsloongarch64.h.
                {
                    if ((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000)
                    {
                        extra = 4;
                    }
                    else
                    {
                        assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                        extra = 8;
                    }
                }
                else if (ins < INS_b) //   beqz/bnez < b < bl    // See instrsloongarch64.h.
                {
                    if (jmpDist + emitCounts_INS_OPTS_J * 4 < 0x200000)
                        continue;

                    extra = 4;
                    assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                }
                else
                {
                    assert(ins == INS_b || ins == INS_bl);
                    assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                    continue;
                }

                jmp->idInsOpt(INS_OPTS_JIRL);
                jmp->idCodeSize(jmp->idCodeSize() + extra);
                jmpIG->igSize += (unsigned short)extra; // the placeholder sizeof(INS_OPTS_JIRL) - sizeof(INS_OPTS_J).
                adjSJ += (UNATIVE_OFFSET)extra;
                adjIG += (UNATIVE_OFFSET)extra;
                emitTotalCodeSize += (UNATIVE_OFFSET)extra;
                jmpIG->igFlags |= IGF_UPD_ISZ;
                isLinkingEnd_LA |= 0x1;
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
            assert(jmp->idDebugOnlyInfo() != nullptr);
            if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[2] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                }
                printf("[2] Jump  block is at %08X\n", jmpIG->igOffs);
                printf("[2] Jump reloffset is %04X\n", jmp->idjOffs);
                printf("[2] Jump source is at %08X\n", srcEncodingOffs);
                printf("[2] Label block is at %08X\n", dstOffs);
                printf("[2] Jump  dist. is    %04X\n", jmpDist);
                if (extra > 0)
                {
                    printf("[2] Dist excess [S] = %d  \n", extra);
                }
            }
            if (EMITVERBOSE)
            {
                printf("Estimate of bwd jump [%08X/%03u]: %04X -> %04X = %04X\n", dspPtr(jmp),
                       jmp->idDebugOnlyInfo()->idNum, srcInstrOffs, dstOffs, jmpDist);
            }
#endif // DEBUG_EMIT

            assert(jmpDist >= 0); // Backward jump
            assert(!(jmpDist & 0x3));

            if (isLinkingEnd_LA & 0x2)
            {
                jmp->idAddr()->iiaSetJmpOffset(-jmpDist); // Backward jump is negative!
            }
            else if ((extra > 0) && (jmp->idInsOpt() == INS_OPTS_J))
            {
                instruction ins = jmp->idIns();
                assert((INS_bceqz <= ins) && (ins <= INS_bl));

                if (ins <
                    INS_beqz) //   bceqz/bcnez/beq/bne/blt/bltu/bge/bgeu < beqz < bnez  // See instrsloongarch64.h.
                {
                    if ((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000)
                    {
                        extra = 4;
                    }
                    else
                    {
                        assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                        extra = 8;
                    }
                }
                else if (ins < INS_b) //   beqz/bnez < b < bl    // See instrsloongarch64.h.
                {
                    if (jmpDist + emitCounts_INS_OPTS_J * 4 < 0x200000)
                        continue;

                    extra = 4;
                    assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                }
                else
                {
                    assert(ins == INS_b || ins == INS_bl);
                    assert((jmpDist + emitCounts_INS_OPTS_J * 4) < 0x8000000);
                    continue;
                }

                jmp->idInsOpt(INS_OPTS_JIRL);
                jmp->idCodeSize(jmp->idCodeSize() + extra);
                jmpIG->igSize += (unsigned short)extra; // the placeholder sizeof(INS_OPTS_JIRL) - sizeof(INS_OPTS_J).
                adjSJ += (UNATIVE_OFFSET)extra;
                adjIG += (UNATIVE_OFFSET)extra;
                emitTotalCodeSize += (UNATIVE_OFFSET)extra;
                jmpIG->igFlags |= IGF_UPD_ISZ;
                isLinkingEnd_LA |= 0x1;
            }
            continue;
        }
    } // end for each jump

    if ((isLinkingEnd_LA & 0x3) < 0x2)
    {
        // indicating the instrDescJmp's size of the type INS_OPTS_J had updated
        // after the first round and should iterate again to update.
        isLinkingEnd_LA = 0x2;

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
        printf("\nLabels list after the jump dist binding:\n\n");
        emitDispIGlist(false);
    }

    emitCheckIGList();
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Emit a 32-bit LOONGARCH64 instruction
 */

/*static*/ unsigned emitter::emitOutput_Instr(BYTE* dst, code_t code)
{
    assert(sizeof(code_t) == 4);
    BYTE* dstRW       = dst + writeableOffset;
    *((code_t*)dstRW) = code;

    return sizeof(code_t);
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
    BYTE*       dstRW  = *dp + writeableOffset;
    BYTE*       dstRW2 = dstRW + 4; // addr for updating gc info if needed.
    code_t      code   = 0;
    instruction ins;
    size_t      sz; // = emitSizeOfInsDsc(id);

#ifdef DEBUG
#if DUMP_GC_TABLES
    bool dspOffs = emitComp->opts.dspGCtbls;
#else
    bool dspOffs = !emitComp->opts.disDiffable;
#endif
#endif // DEBUG

    assert(REG_NA == (int)REG_NA);

    insOpts insOp = id->idInsOpt();

    switch (insOp)
    {
        case INS_OPTS_RELOC:
        {
            //  case:EA_HANDLE_CNS_RELOC
            //   pcaddu12i  reg, off-hi-20bits
            //   addi_d  reg, reg, off-lo-12bits
            //  case:EA_PTR_DSP_RELOC
            //   pcaddu12i  reg, off-hi-20bits
            //   ld_d  reg, reg, off-lo-12bits

            regNumber reg1 = id->idReg1();

            *(code_t*)dstRW = 0x1c000000 | (code_t)reg1;

            dstRW += 4;

#ifdef DEBUG
            code = emitInsCode(INS_pcaddu12i);
            assert(code == 0x1c000000);
            code = emitInsCode(INS_addi_d);
            assert(code == 0x02c00000);
            code = emitInsCode(INS_ld_d);
            assert(code == 0x28c00000);
#endif

            if (id->idIsCnsReloc())
            {
                ins             = INS_addi_d;
                *(code_t*)dstRW = 0x02c00000 | (code_t)reg1 | (code_t)(reg1 << 5);
            }
            else
            {
                assert(id->idIsDspReloc());
                ins             = INS_ld_d;
                *(code_t*)dstRW = 0x28c00000 | (code_t)reg1 | (code_t)(reg1 << 5);
            }

            dstRW += 4;

            emitRecordRelocation(dstRW - 8 - writeableOffset, id->idAddr()->iiaAddr, IMAGE_REL_LOONGARCH64_PC);

            sz = sizeof(instrDesc);
        }
        break;
        case INS_OPTS_I:
        {
            ssize_t   imm  = (ssize_t)(id->idAddr()->iiaAddr);
            regNumber reg1 = id->idReg1();

            switch (id->idCodeSize())
            {
                case 8:
                {
                    if (id->idReg2())
                    { // special for INT64_MAX or UINT32_MAX;
                        code = emitInsCode(INS_addi_d);
                        code |= (code_t)reg1;
                        code |= (code_t)REG_R0;
                        code |= 0xfff << 10;

                        *(code_t*)dstRW = code;
                        dstRW += 4;

                        ssize_t ui6 = (imm == INT64_MAX) ? 1 : 32;
                        code        = emitInsCode(INS_srli_d);
                        code |= ((code_t)reg1 | ((code_t)reg1 << 5) | (ui6 << 10));
                        *(code_t*)dstRW = code;
                    }
                    else
                    {
                        code = emitInsCode(INS_lu12i_w);
                        code |= (code_t)reg1;
                        code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

                        *(code_t*)dstRW = code;
                        dstRW += 4;

                        code = emitInsCode(INS_ori);
                        code |= (code_t)reg1;
                        code |= (code_t)reg1 << 5;
                        code |= (code_t)(imm & 0xfff) << 10;
                        *(code_t*)dstRW = code;
                    }
                    break;
                }
                case 12:
                {
                    code = emitInsCode(INS_lu12i_w);
                    code |= (code_t)reg1;
                    code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_ori);
                    code |= (code_t)reg1;
                    code |= (code_t)reg1 << 5;
                    code |= (code_t)(imm & 0xfff) << 10;
                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_lu32i_d);
                    code |= (code_t)reg1;
                    code |= ((code_t)(imm >> 32) & 0xfffff) << 5;

                    *(code_t*)dstRW = code;

                    break;
                }
                case 16:
                {
                    code = emitInsCode(INS_lu12i_w);
                    code |= (code_t)reg1;
                    code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_ori);
                    code |= (code_t)reg1;
                    code |= (code_t)reg1 << 5;
                    code |= (code_t)(imm & 0xfff) << 10;
                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_lu32i_d);
                    code |= (code_t)reg1;
                    code |= (code_t)((imm >> 32) & 0xfffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_lu52i_d);
                    code |= (code_t)reg1;
                    code |= (code_t)(reg1) << 5;
                    code |= ((code_t)(imm >> 52) & 0xfff) << 10;

                    *(code_t*)dstRW = code;

                    break;
                }
                default:
                    unreached();
                    break;
            }

            ins = INS_ori;
            dstRW += 4;

            sz = sizeof(instrDesc);
        }
        break;
        case INS_OPTS_RC:
        {
            // Reference to JIT data

            // when id->idIns == bl, for reloc!
            //   pcaddu12i r21, off-hi-20bits
            //   addi_d  reg, r21, off-lo-12bits
            // when id->idIns == load-ins
            //   pcaddu12i r21, off-hi-20bits
            //   load  reg, offs_lo-12bits(r21)    #when ins is load ins.
            //
            // when id->idIns == bl
            //   lu12i_w r21, addr_bits[31:12]
            //   ori     reg, r21, addr_bits[11:0]
            //   lu32i_d reg, addr_bits[50:32]
            //
            // when id->idIns == load-ins
            //   lu12i_w r21, addr_bits[31:12]
            //   lu32i_d r21, addr_bits[50:32]
            //   load  reg, r21 + addr_bits[11:0]
            assert(id->idAddr()->iiaIsJitDataOffset());
            assert(id->idGCref() == GCT_NONE);

            int doff = id->idAddr()->iiaGetJitDataOffset();
            assert(doff >= 0);

            ssize_t imm = emitGetInsSC(id);
            assert((imm >= 0) && (imm < 0x4000)); // 0x4000 is arbitrary, currently 'imm' is always 0.

            unsigned dataOffs = (unsigned)(doff + imm);

            assert(dataOffs < emitDataSize());

            ins            = id->idIns();
            regNumber reg1 = id->idReg1();

            if (id->idIsReloc())
            {
                // get the addr-offset of the data.
                imm = (ssize_t)emitConsBlock - (ssize_t)(dstRW - writeableOffset) + dataOffs;
                assert(imm > 0);
                assert(!(imm & 3));

                doff = (int)(imm & 0x800);
                imm += doff;
                assert(isValidSimm20(imm >> 12));

                doff = (int)(imm & 0x7ff) - doff; // addr-lo-12bit.

#ifdef DEBUG
                code = emitInsCode(INS_pcaddu12i);
                assert(code == 0x1c000000);
#endif
                code            = 0x1c000000 | 21;
                *(code_t*)dstRW = code | (((code_t)imm & 0xfffff000) >> 7);
                dstRW += 4;

                if (ins == INS_bl)
                {
                    assert(isGeneralRegister(reg1));
                    ins = INS_addi_d;
#ifdef DEBUG
                    code = emitInsCode(INS_addi_d);
                    assert(code == 0x02c00000);
#endif
                    code            = 0x02c00000 | (21 << 5);
                    *(code_t*)dstRW = code | (code_t)reg1 | (((code_t)doff & 0xfff) << 10);
                }
                else
                {
                    code = emitInsCode(ins);
                    code |= (code_t)(reg1 & 0x1f);
                    code |= (code_t)REG_R21 << 5; // NOTE:here must be REG_R21 !!!
                    code |= (code_t)(doff & 0xfff) << 10;
                    *(code_t*)dstRW = code;
                }
                dstRW += 4;
            }
            else
            {
                // get the addr of the data.
                imm = (ssize_t)emitConsBlock + dataOffs;

                code = emitInsCode(INS_lu12i_w);
                if (ins == INS_bl)
                {
                    assert((uint64_t)(imm >> 32) <= 0x7ffff);

                    doff = (int)imm >> 12;
                    code |= (code_t)REG_R21;
                    code |= ((code_t)doff & 0xfffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_ori);
                    code |= (code_t)reg1;
                    code |= (code_t)REG_R21 << 5;
                    code |= (code_t)(imm & 0xfff) << 10;
                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    ins  = INS_lu32i_d;
                    code = emitInsCode(INS_lu32i_d);
                    code |= (code_t)reg1;
                    code |= ((imm >> 32) & 0x7ffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;
                }
                else
                {
                    doff = (int)(imm & 0x800);
                    imm += doff;
                    doff = (int)(imm & 0x7ff) - doff; // addr-lo-12bit.

                    assert((uint64_t)(imm >> 32) <= 0x7ffff);

                    dataOffs = (unsigned)(imm >> 12); // addr-hi-20bits.
                    code |= (code_t)REG_R21;
                    code |= ((code_t)dataOffs & 0xfffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(INS_lu32i_d);
                    code |= (code_t)REG_R21;
                    code |= ((imm >> 32) & 0x7ffff) << 5;

                    *(code_t*)dstRW = code;
                    dstRW += 4;

                    code = emitInsCode(ins);
                    code |= (code_t)(reg1 & 0x1f);
                    code |= (code_t)REG_R21 << 5;
                    code |= (code_t)(doff & 0xfff) << 10;

                    *(code_t*)dstRW = code;
                    dstRW += 4;
                }
            }

            sz = sizeof(instrDesc);
        }
        break;

        case INS_OPTS_RL:
        {
            // if for reloc!
            //   pcaddu12i reg, offset-hi20
            //   addi_d  reg, reg, offset-lo12
            //
            // else:       // TODO-LoongArch64:optimize.
            //   lu12i_w r21, addr_bits[31:12]
            //   ori     reg, r21, addr_bits[11:0]
            //   lu32i_d reg, addr_bits[50:32]

            insGroup* tgtIG          = (insGroup*)emitCodeGetCookie(id->idAddr()->iiaBBlabel);
            id->idAddr()->iiaIGlabel = tgtIG;

            regNumber reg1 = id->idReg1();
            assert(isGeneralRegister(reg1));

            if (id->idIsReloc())
            {
                ssize_t imm = (ssize_t)tgtIG->igOffs;
                imm         = (ssize_t)emitCodeBlock + imm - (ssize_t)(dstRW - writeableOffset);
                assert((imm & 3) == 0);

                int doff = (int)(imm & 0x800);
                imm += doff;
                assert(isValidSimm20(imm >> 12));

                doff = (int)(imm & 0x7ff) - doff; // addr-lo-12bit.

                code            = 0x1c000000;
                *(code_t*)dstRW = code | (code_t)reg1 | ((imm & 0xfffff000) >> 7);
                dstRW += 4;
#ifdef DEBUG
                code = emitInsCode(INS_pcaddu12i);
                assert(code == 0x1c000000);
                code = emitInsCode(INS_addi_d);
                assert(code == 0x02c00000);
#endif
                ins             = INS_addi_d;
                *(code_t*)dstRW = 0x02c00000 | (code_t)reg1 | ((code_t)reg1 << 5) | ((doff & 0xfff) << 10);
            }
            else
            {
                ssize_t imm = (ssize_t)tgtIG->igOffs + (ssize_t)emitCodeBlock;
                assert((uint64_t)(imm >> 32) <= 0x7ffff);

                code = emitInsCode(INS_lu12i_w);
                code |= (code_t)REG_R21;
                code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

                *(code_t*)dstRW = code;
                dstRW += 4;

                code = emitInsCode(INS_ori);
                code |= (code_t)reg1;
                code |= (code_t)REG_R21 << 5;
                code |= (code_t)(imm & 0xfff) << 10;
                *(code_t*)dstRW = code;
                dstRW += 4;

                ins  = INS_lu32i_d;
                code = emitInsCode(INS_lu32i_d);
                code |= (code_t)reg1;
                code |= ((imm >> 32) & 0x7ffff) << 5;

                *(code_t*)dstRW = code;
            }

            dstRW += 4;

            sz = sizeof(instrDesc);
        }
        break;
        case INS_OPTS_JIRL:
            //  case_1:           <----------from INS_OPTS_J:
            //   xor r21,reg1,reg2   |   bne/beq  _next   |    bcnez/bceqz  _next
            //   bnez/beqz  dstRW      |   b  dstRW           |    b  dstRW
            //_next:
            //
            //  case_2:           <---------- TODO-LoongArch64: from INS_OPTS_J:
            //   bnez/beqz  _next:
            //   pcaddi r21,off-hi
            //   jirl  r0,r21,off-lo
            //_next:
            //
            //  case_3:           <----------INS_OPTS_JIRL:   //not used by now !!!
            //   b dstRW
            //
            //  case_4:           <----------INS_OPTS_JIRL:   //not used by now !!!
            //   pcaddi r21,off-hi
            //   jirl  r0,r21,off-lo
            //
            {
                instrDescJmp* jmp = (instrDescJmp*)id;

                regNumber reg1 = id->idReg1();
                {
                    ssize_t imm = (ssize_t)id->idAddr()->iiaGetJmpOffset();
                    imm -= 4;

                    assert((imm & 0x3) == 0);

                    ins = jmp->idIns();
                    assert(jmp->idCodeSize() > 4); // The original INS_OPTS_JIRL: not used by now!!!
                    switch (jmp->idCodeSize())
                    {
                        case 8:
                        {
                            regNumber reg2 = id->idReg2();
                            assert((INS_bceqz <= ins) && (ins <= INS_bgeu));

                            if ((INS_beq == ins) || (INS_bne == ins))
                            {
                                if ((-0x400000 <= imm) && (imm < 0x400000))
                                {
                                    code = emitInsCode(INS_xor);
                                    code |= (code_t)REG_R21;
                                    code |= (code_t)reg1 << 5;
                                    code |= (code_t)reg2 << 10;

                                    *(code_t*)dstRW = code;
                                    dstRW += 4;

                                    code = emitInsCode(ins == INS_beq ? INS_beqz : INS_bnez);
                                    code |= (code_t)REG_R21 << 5;
                                    code |= (((code_t)imm << 8) & 0x3fffc00);
                                    code |= (((code_t)imm >> 18) & 0x1f);

                                    *(code_t*)dstRW = code;
                                    dstRW += 4;
                                }
                                else
                                {
                                    assert((-0x8000000 <= imm) && (imm < 0x8000000));
                                    assert((INS_bne & 0xfffe) == INS_beq);

                                    code = emitInsCode((instruction)((int)ins ^ 0x1));
                                    code |= ((code_t)(reg1) /*& 0x1f */) << 5; /* rj */
                                    code |= ((code_t)(reg2) /*& 0x1f */);      /* rd */
                                    code |= 0x800;
                                    *(code_t*)dstRW = code;
                                    dstRW += 4;

                                    code = emitInsCode(INS_b);
                                    code |= ((code_t)imm >> 18) & 0x3ff;
                                    code |= ((code_t)imm << 8) & 0x3fffc00;

                                    *(code_t*)dstRW = code;
                                    dstRW += 4;
                                }
                            }
                            else if ((INS_bceqz == ins) || (INS_bcnez == ins))
                            {
                                assert((-0x8000000 <= imm) && (imm < 0x8000000));
                                assert((INS_bcnez & 0xfffe) == INS_bceqz);

                                code = emitInsCode((instruction)((int)ins ^ 0x1));
                                code |= ((code_t)reg1) << 5;
                                code |= 0x800;
                                *(code_t*)dstRW = code;
                                dstRW += 4;

                                code = emitInsCode(INS_b);
                                code |= ((code_t)imm >> 18) & 0x3ff;
                                code |= ((code_t)imm << 8) & 0x3fffc00;

                                *(code_t*)dstRW = code;
                                dstRW += 4;
                            }
                            else if ((INS_blt <= ins) && (ins <= INS_bgeu))
                            {
                                assert((-0x8000000 <= imm) && (imm < 0x8000000));
                                assert((INS_bge & 0xfffe) == INS_blt);
                                assert((INS_bgeu & 0xfffe) == INS_bltu);

                                code = emitInsCode((instruction)((int)ins ^ 0x1));
                                code |= ((code_t)(reg1) /*& 0x1f */) << 5; /* rj */
                                code |= ((code_t)(reg2) /*& 0x1f */);      /* rd */
                                code |= 0x800;
                                *(code_t*)dstRW = code;
                                dstRW += 4;

                                code = emitInsCode(INS_b);
                                code |= ((code_t)imm >> 18) & 0x3ff;
                                code |= ((code_t)imm << 8) & 0x3fffc00;

                                *(code_t*)dstRW = code;
                                dstRW += 4;
                            }
                            break;
                        }

                        default:
                            unreached();
                            break;
                    }
                }
                sz = sizeof(instrDescJmp);
            }
            break;
        case INS_OPTS_J_cond:
            //   b_cond  dstRW-relative.
            //
            // NOTE:
            //  the case "imm > 0x7fff" not supported.
            //  More info within the emitter::emitIns_J_cond_la();
            {
                ssize_t imm = (ssize_t)id->idAddr()->iiaGetJmpOffset(); // get jmp's offset relative delay-slot.
                assert((OFFSET_DIST_SMALL_MAX_NEG << 2) <= imm && imm <= (OFFSET_DIST_SMALL_MAX_POS << 2));
                assert(!(imm & 3));

                ins  = id->idIns();
                code = emitInsCode(ins);
                code |= ((code_t)id->idReg1()) << 5;
                code |= ((code_t)id->idReg2());
                code |= (((code_t)imm << 8) & 0x3fffc00);

                *(code_t*)dstRW = code;
                dstRW += 4;

                sz = sizeof(instrDescJmp);
            }
            break;
        case INS_OPTS_J:
            //   bceqz/bcnez/beq/bne/blt/bltu/bge/bgeu/beqz/bnez/b/bl  dstRW-relative.
            {
                ssize_t imm = (ssize_t)id->idAddr()->iiaGetJmpOffset(); // get jmp's offset relative delay-slot.
                assert((imm & 3) == 0);

                ins  = id->idIns();
                code = emitInsCode(ins);
                if (ins == INS_b || ins == INS_bl)
                {
                    code |= ((code_t)imm >> 18) & 0x3ff;
                    code |= ((code_t)imm << 8) & 0x3fffc00;
                }
                else if (ins == INS_bnez || ins == INS_beqz)
                {
                    code |= (code_t)id->idReg1() << 5;
                    code |= (((code_t)imm << 8) & 0x3fffc00);
                    code |= (((code_t)imm >> 18) & 0x1f);
                }
                else if (ins == INS_bcnez || ins == INS_bceqz)
                {
                    assert((code_t)(id->idReg1()) < 8); // cc
                    code |= (code_t)id->idReg1() << 5;
                    code |= (((code_t)imm << 8) & 0x3fffc00);
                    code |= (((code_t)imm >> 18) & 0x1f);
                }
                else if ((INS_beq <= ins) && (ins <= INS_bgeu))
                {
                    code |= ((code_t)id->idReg1()) << 5;
                    code |= ((code_t)id->idReg2());
                    code |= (((code_t)imm << 8) & 0x3fffc00);
                }
                else
                {
                    assert(!"unimplemented on LOONGARCH yet");
                }

                *(code_t*)dstRW = code;
                dstRW += 4;

                sz = sizeof(instrDescJmp);
            }
            break;

        case INS_OPTS_C:
            if (id->idIsLargeCall())
            {
                /* Must be a "fat" call descriptor */
                sz = sizeof(instrDescCGCA);
            }
            else
            {
                assert(!id->idIsLargeDsp());
                assert(!id->idIsLargeCns());
                sz = sizeof(instrDesc);
            }
            dstRW += emitOutputCall(ig, *dp, id, 0);

            dstRW2 = dstRW;
            ins    = INS_nop;
            break;

        // case INS_OPTS_NONE:
        default:
            *(code_t*)dstRW = id->idAddr()->iiaGetInstrEncode();
            dstRW += 4;
            ins = id->idIns();
            sz  = emitSizeOfInsDsc(id);
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
            emitGCregLiveUpd(id->idGCref(), id->idReg1(), dstRW2 - writeableOffset);
        }
        else
        {
            emitGCregDeadUpd(id->idReg1(), dstRW2 - writeableOffset);
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
            emitGCvarLiveUpd(adr + ofs, varNum, id->idGCref(), dstRW2 - writeableOffset DEBUG_ARG(varNum));
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
                emitGCvarDeadUpd(adr + ofs, dstRW2 - writeableOffset DEBUG_ARG(varNum));
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

    // size_t expected = emitSizeOfInsDsc(id);
    // assert(sz == expected);

    if (emitComp->opts.disAsm || emitComp->verbose)
    {
        code_t* cp = (code_t*)(*dp + writeableOffset);
        while ((BYTE*)cp != dstRW)
        {
            emitDisInsName(*cp, (BYTE*)cp, id);
            cp++;
        }
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
#endif

    /* All instructions are expected to generate code */

    assert(*dp != (dstRW - writeableOffset));

    *dp = dstRW - writeableOffset;

    return sz;
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
    const BYTE*       insAdr      = addr - writeableOffset;
    const char* const CFregName[] = {"fcc0", "fcc1", "fcc2", "fcc3", "fcc4", "fcc5", "fcc6", "fcc7"};

    unsigned int opcode = (code >> 26) & 0x3f;

    bool disOpcode = !emitComp->opts.disDiffable;
    bool disAddr   = emitComp->opts.disAddr;
    if (disAddr)
    {
        printf("  0x%llx", insAdr);
    }

    printf("  ");

    if (disOpcode)
    {
        printf("%08X  ", code);
    }

    // bits: 31-26,MSB6
    switch (opcode)
    {
        case 0x0:
        {
            goto Label_OPCODE_0;
        }
        case 0x2:
        {
            goto Label_OPCODE_2;
        }
        case 0x3:
        {
            goto Label_OPCODE_3;
        }
        case 0xe:
        {
            goto Label_OPCODE_E;
        }
        case LA_2RI16_ADDU16I_D: // 0x4
        {
            const char* rd   = RegNames[code & 0x1f];
            const char* rj   = RegNames[(code >> 5) & 0x1f];
            short       si16 = (code >> 10) & 0xffff;
            printf("addu16i.d    %s, %s, %d\n", rd, rj, si16);
            return;
        }
        case 0x5:
        case 0x6:
        case 0x7:
        {
            // bits: 31-25,MSB7
            unsigned int inscode = (code >> 25) & 0x7f;
            const char*  rd      = RegNames[code & 0x1f];
            unsigned int si20    = (code >> 5) & 0xfffff;
            switch (inscode)
            {
                case LA_1RI20_LU12I_W:
                    printf("lu12i.w      %s, 0x%x\n", rd, si20);
                    return;
                case LA_1RI20_LU32I_D:
                    printf("lu32i.d      %s, 0x%x\n", rd, si20);
                    return;
                case LA_1RI20_PCADDI:
                    printf("pcaddi       %s, 0x%x\n", rd, si20);
                    return;
                case LA_1RI20_PCALAU12I:
                    printf("pcalau12i    %s, 0x%x\n", rd, si20);
                    return;
                case LA_1RI20_PCADDU12I:
                    printf("pcaddu12i    %s, 0x%x\n", rd, si20);
                    return;
                case LA_1RI20_PCADDU18I:
                {
                    printf("pcaddu18i    %s, 0x%x\n", rd, si20);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            return;
        }
        case 0x8:
        case 0x9:
        {
            // bits: 31-24,MSB8
            unsigned int inscode = (code >> 24) & 0xff;
            const char*  rd      = RegNames[code & 0x1f];
            const char*  rj      = RegNames[(code >> 5) & 0x1f];
            short        si14    = ((code >> 10) & 0x3fff) << 2;
            si14 >>= 2;
            switch (inscode)
            {
                case LA_2RI14_LL_W:
                    printf("ll.w         %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_SC_W:
                    printf("sc.w         %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_LL_D:
                    printf("ll.d         %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_SC_D:
                    printf("sc.d         %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_LDPTR_W:
                    printf("ldptr.w      %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_STPTR_W:
                    printf("stptr.w      %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_LDPTR_D:
                    printf("ldptr.d      %s, %s, %d\n", rd, rj, si14);
                    return;
                case LA_2RI14_STPTR_D:
                    printf("stptr.d      %s, %s, %d\n", rd, rj, si14);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            return;
        }
        case 0xa:
        {
            // bits: 31-24,MSB8
            unsigned int inscode = (code >> 22) & 0x3ff;
            const char*  rd      = RegNames[code & 0x1f];
            const char*  rj      = RegNames[(code >> 5) & 0x1f];
            const char*  fd      = RegNames[(code & 0x1f) + 32];
            short        si12    = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            switch (inscode)
            {
                case LA_2RI12_LD_B:
                    printf("ld.b         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_H:
                    printf("ld.h         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_W:
                    printf("ld.w         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_D:
                    printf("ld.d         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_ST_B:
                    printf("st.b         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_ST_H:
                    printf("st.h         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_ST_W:
                    printf("st.w         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_ST_D:
                    printf("st.d         %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_BU:
                    printf("ld.bu        %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_HU:
                    printf("ld.hu        %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_LD_WU:
                    printf("ld.wu        %s, %s, %d\n", rd, rj, si12);
                    return;
                case LA_2RI12_PRELD:
                    NYI_LOONGARCH64("unused instr LA_2RI12_PRELD");
                    return;
                case LA_2RI12_FLD_S:
                    printf("fld.s        %s, %s, %d\n", fd, rj, si12);
                    return;
                case LA_2RI12_FST_S:
                    printf("fst.s        %s, %s, %d\n", fd, rj, si12);
                    return;
                case LA_2RI12_FLD_D:
                    printf("fld.d        %s, %s, %d\n", fd, rj, si12);
                    return;
                case LA_2RI12_FST_D:
                    printf("fst.d        %s, %s, %d\n", fd, rj, si12);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            return;
        }
        case LA_1RI21_BEQZ: // 0x10
        {
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs21 = (((code >> 10) & 0xffff) | ((code & 0x1f) << 16)) << 11;
            offs21 >>= 9;
            printf("beqz         %s, 0x%llx\n", rj, (int64_t)insAdr + offs21);
            return;
        }
        case LA_1RI21_BNEZ: // 0x11
        {
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs21 = (((code >> 10) & 0xffff) | ((code & 0x1f) << 16)) << 11;
            offs21 >>= 9;
            printf("bnez         %s, 0x%llx\n", rj, (int64_t)insAdr + offs21);
            return;
        }
        case 0x12:
        {
            // LA_1RI21_BCEQZ
            // LA_1RI21_BCNEZ
            const char* cj     = CFregName[(code >> 5) & 0x7];
            int         offs21 = (((code >> 10) & 0xffff) | ((code & 0x1f) << 16)) << 11;
            offs21 >>= 9;
            if (0 == ((code >> 8) & 0x3))
            {
                printf("bceqz        %s, 0x%llx\n", cj, (int64_t)insAdr + offs21);
                return;
            }
            else if (1 == ((code >> 8) & 0x3))
            {
                printf("bcnez        %s, 0x%llx\n", cj, (int64_t)insAdr + offs21);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
            return;
        }
        case LA_2RI16_JIRL: // 0x13
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            if (id->idDebugOnlyInfo()->idMemCookie)
            {
                assert(0 < id->idDebugOnlyInfo()->idMemCookie);
                const char* methodName;
                methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
                printf("jirl         %s, %s, %d  #%s\n", rd, rj, offs16, methodName);
            }
            else
            {
                printf("jirl         %s, %s, %d\n", rd, rj, offs16);
            }
            return;
        }
        case LA_I26_B: // 0x14
        {
            int offs26 = (((code >> 10) & 0xffff) | ((code & 0x3ff) << 16)) << 6;
            offs26 >>= 4;
            printf("b            0x%llx\n", (int64_t)insAdr + offs26);
            return;
        }
        case LA_I26_BL: // 0x15
        {
            int offs26 = (((code >> 10) & 0xffff) | ((code & 0x3ff) << 16)) << 6;
            offs26 >>= 4;
            printf("bl           0x%llx\n", (int64_t)insAdr + offs26);
            return;
        }
        case LA_2RI16_BEQ: // 0x16
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("beq          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BNE: // 0x17
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bne          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BLT: // 0x18
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("blt          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BGE: // 0x19
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bge          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BLTU: // 0x1a
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bltu         %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BGEU: // 0x1b
        {
            const char* rd     = RegNames[code & 0x1f];
            const char* rj     = RegNames[(code >> 5) & 0x1f];
            int         offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bgeu         %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }

        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_0:
    opcode = (code >> 22) & 0x3ff;

    // bits: 31-22,MSB10
    switch (opcode)
    {
        case 0x0:
        {
            // bits: 31-18,MSB14
            unsigned int inscode1 = (code >> 18) & 0x3fff;
            switch (inscode1)
            {
                case 0x0:
                {
                    // bits: 31-15,MSB17
                    unsigned int inscode2 = (code >> 15) & 0x1ffff;
                    switch (inscode2)
                    {
                        case 0x0:
                        {
                            // bits:31-10,MSB22
                            unsigned int inscode3 = (code >> 10) & 0x3fffff;
                            const char*  rd       = RegNames[code & 0x1f];
                            const char*  rj       = RegNames[(code >> 5) & 0x1f];
                            switch (inscode3)
                            {
                                case LA_2R_CLO_W:
                                    printf("clo.w        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CLZ_W:
                                    printf("clz.w        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CTO_W:
                                    printf("cto.w        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CTZ_W:
                                    printf("ctz.w        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CLO_D:
                                    printf("clo.d        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CLZ_D:
                                    printf("clz.d        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CTO_D:
                                    printf("cto.d        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CTZ_D:
                                    printf("ctz.d        %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVB_2H:
                                    printf("revb.2h      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVB_4H:
                                    printf("revb.4h      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVB_2W:
                                    printf("revb.2w      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVB_D:
                                    printf("revb.d       %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVH_2W:
                                    printf("revh.2w      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_REVH_D:
                                    printf("revh.d       %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_BITREV_4B:
                                    printf("bitrev.4b    %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_BITREV_8B:
                                    printf("bitrev.8b    %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_BITREV_W:
                                    printf("bitrev.w     %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_BITREV_D:
                                    printf("bitrev.d     %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_EXT_W_H:
                                    printf("ext.w.h      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_EXT_W_B:
                                    printf("ext.w.b      %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_RDTIMEL_W:
                                    printf("rdtimel.w    %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_RDTIMEH_W:
                                    printf("rdtimeh.w    %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_RDTIME_D:
                                    printf("rdtime.d     %s, %s\n", rd, rj);
                                    return;
                                case LA_2R_CPUCFG:
                                    printf("cpucfg       %s, %s\n", rd, rj);
                                    return;

                                default:
                                    printf("LOONGARCH illegal instruction: %08X\n", code);
                                    return;
                            }
                            return;
                        }
                        case LA_2R_ASRTLE_D:
                        {
                            const char* rj = RegNames[(code >> 5) & 0x1f];
                            const char* rk = RegNames[(code >> 10) & 0x1f];
                            printf("asrtle.d     %s, %s\n", rj, rk);
                            return;
                        }
                        case LA_2R_ASRTGT_D:
                        {
                            const char* rj = RegNames[(code >> 5) & 0x1f];
                            const char* rk = RegNames[(code >> 10) & 0x1f];
                            printf("asrtgt.d     %s, %s\n", rj, rk);
                            return;
                        }
                        default:
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                    }
                    return;
                }
                case 0x1:
                {
                    // LA_OP_ALSL_W
                    // LA_OP_ALSL_WU
                    const char*  rd  = RegNames[code & 0x1f];
                    const char*  rj  = RegNames[(code >> 5) & 0x1f];
                    const char*  rk  = RegNames[(code >> 10) & 0x1f];
                    unsigned int sa2 = (code >> 15) & 0x3;
                    if (0 == ((code >> 17) & 0x1))
                    {
                        printf("alsl.w       %s, %s, %s, %d\n", rd, rj, rk, (sa2 + 1));
                        return;
                    }
                    else if (1 == ((code >> 17) & 0x1))
                    {
                        printf("alsl.wu      %s, %s, %s, %d\n", rd, rj, rk, (sa2 + 1));
                        return;
                    }
                    else
                    {
                        printf("LOONGARCH illegal instruction: %08X\n", code);
                        return;
                    }
                    return;
                }
                case LA_OP_BYTEPICK_W: // 0x2
                {
                    const char*  rd  = RegNames[code & 0x1f];
                    const char*  rj  = RegNames[(code >> 5) & 0x1f];
                    const char*  rk  = RegNames[(code >> 10) & 0x1f];
                    unsigned int sa2 = (code >> 15) & 0x3;
                    printf("bytepick.w   %s, %s, %s, %d\n", rd, rj, rk, sa2);
                    return;
                }
                case LA_OP_BYTEPICK_D: // 0x3
                {
                    const char*  rd  = RegNames[code & 0x1f];
                    const char*  rj  = RegNames[(code >> 5) & 0x1f];
                    const char*  rk  = RegNames[(code >> 10) & 0x1f];
                    unsigned int sa3 = (code >> 15) & 0x7;
                    printf("bytepick.d   %s, %s, %s, %d\n", rd, rj, rk, sa3);
                    return;
                }
                case 0x4:
                case 0x5:
                case 0x6:
                case 0x7:
                case 0x8:
                case 0x9:
                {
                    // bits: 31-15,MSB17
                    unsigned int inscode2 = (code >> 15) & 0x1ffff;
                    const char*  rd       = RegNames[code & 0x1f];
                    const char*  rj       = RegNames[(code >> 5) & 0x1f];
                    const char*  rk       = RegNames[(code >> 10) & 0x1f];

                    switch (inscode2)
                    {
                        case LA_3R_ADD_W:
                            printf("add.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_ADD_D:
                            printf("add.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SUB_W:
                            printf("sub.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SUB_D:
                            printf("sub.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SLT:
                            printf("slt          %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SLTU:
                            printf("sltu         %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MASKEQZ:
                            printf("maskeqz      %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MASKNEZ:
                            printf("masknez      %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_NOR:
                            printf("nor          %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_AND:
                            printf("and          %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_OR:
                            printf("or           %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_XOR:
                            printf("xor          %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_ORN:
                            printf("orn          %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_ANDN:
                            printf("andn         %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SLL_W:
                            printf("sll.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SRL_W:
                            printf("srl.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SRA_W:
                            printf("sra.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SLL_D:
                            printf("sll.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SRL_D:
                            printf("srl.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_SRA_D:
                            printf("sra.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_ROTR_W:
                            printf("rotr.w       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_ROTR_D:
                            printf("rotr.d       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MUL_W:
                            printf("mul.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULH_W:
                            printf("mulh.w       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULH_WU:
                            printf("mulh.wu      %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MUL_D:
                            printf("mul.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULH_D:
                            printf("mulh.d       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULH_DU:
                            printf("mulh.du      %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULW_D_W:
                            printf("mulw.d.w     %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MULW_D_WU:
                            printf("mulw.d.wu    %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_DIV_W:
                            printf("div.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MOD_W:
                            printf("mod.w        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_DIV_WU:
                            printf("div.wu       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MOD_WU:
                            printf("mod.wu       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_DIV_D:
                            printf("div.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MOD_D:
                            printf("mod.d        %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_DIV_DU:
                            printf("div.du       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_MOD_DU:
                            printf("mod.du       %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRC_W_B_W:
                            printf("crc.w.b.w    %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRC_W_H_W:
                            printf("crc.w.h.w    %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRC_W_W_W:
                            printf("crc.w.w.w    %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRC_W_D_W:
                            printf("crc.w.d.w    %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRCC_W_B_W:
                            printf("crcc.w.b.w   %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRCC_W_H_W:
                            printf("crcc.w.h.w   %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRCC_W_W_W:
                            printf("crcc.w.w.w   %s, %s, %s\n", rd, rj, rk);
                            return;
                        case LA_3R_CRCC_W_D_W:
                            printf("crcc.w.d.w   %s, %s, %s\n", rd, rj, rk);
                            return;
                        default:
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                    }
                }
                case 0xa:
                {
                    // bits: 31-15,MSB17
                    unsigned int inscode2  = (code >> 15) & 0x1ffff;
                    unsigned int codefield = code & 0x7fff;
                    switch (inscode2)
                    {
                        case LA_OP_BREAK:
                            printf("break        0x%x\n", codefield);
                            return;
                        case LA_OP_DBGCALL:
                            printf("dbgcall      0x%x\n", codefield);
                            return;
                        case LA_OP_SYSCALL:
                            printf("syscall      0x%x\n", codefield);
                            return;
                        default:
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                    }
                }
                case LA_OP_ALSL_D: // 0xb
                {
                    const char*  rd  = RegNames[code & 0x1f];
                    const char*  rj  = RegNames[(code >> 5) & 0x1f];
                    const char*  rk  = RegNames[(code >> 10) & 0x1f];
                    unsigned int sa2 = (code >> 15) & 0x3;
                    printf("alsl.d       %s, %s, %s, %d\n", rd, rj, rk, (sa2 + 1));
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            return;
        }
        case 0x1:
        {
            if (code & 0x200000)
            {
                // LA_OP_BSTRINS_W
                // LA_OP_BSTRPICK_W
                const char*  rd   = RegNames[code & 0x1f];
                const char*  rj   = RegNames[(code >> 5) & 0x1f];
                unsigned int lsbw = (code >> 10) & 0x1f;
                unsigned int msbw = (code >> 16) & 0x1f;
                if (!(code & 0x8000))
                {
                    printf("bstrins.w    %s, %s, %d, %d\n", rd, rj, msbw, lsbw);
                    return;
                }
                else if (code & 0x8000)
                {
                    printf("bstrpick.w   %s, %s, %d, %d\n", rd, rj, msbw, lsbw);
                    return;
                }
                else
                {
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
                }
            }
            else
            {
                // bits: 31-18,MSB14
                unsigned int inscode1 = (code >> 18) & 0x3fff;
                switch (inscode1)
                {
                    case 0x10:
                    {
                        // LA_OP_SLLI_W:
                        // LA_OP_SLLI_D:
                        const char* rd = RegNames[code & 0x1f];
                        const char* rj = RegNames[(code >> 5) & 0x1f];
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("slli.w       %s, %s, %d\n", rd, rj, ui5);
                            return;
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("slli.d       %s, %s, %d\n", rd, rj, ui6);
                            return;
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                        }
                        return;
                    }
                    case 0x11:
                    {
                        // LA_OP_SRLI_W:
                        // LA_OP_SRLI_D:
                        const char* rd = RegNames[code & 0x1f];
                        const char* rj = RegNames[(code >> 5) & 0x1f];
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("srli.w       %s, %s, %d\n", rd, rj, ui5);
                            return;
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("srli.d      %s, %s, %d\n", rd, rj, ui6);
                            return;
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                        }
                        return;
                    }
                    case 0x12:
                    {
                        // LA_OP_SRAI_W:
                        // LA_OP_SRAI_D:
                        const char* rd = RegNames[code & 0x1f];
                        const char* rj = RegNames[(code >> 5) & 0x1f];
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("srai.w       %s, %s, %d\n", rd, rj, ui5);
                            return;
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("srai.d       %s, %s, %d\n", rd, rj, ui6);
                            return;
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                        }
                        return;
                    }
                    case 0x13:
                    {
                        // LA_OP_ROTRI_W:
                        // LA_OP_ROTRI_D:
                        const char* rd = RegNames[code & 0x1f];
                        const char* rj = RegNames[(code >> 5) & 0x1f];
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("rotri.w      %s, %s, %d\n", rd, rj, ui5);
                            return;
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("rotri.d      %s, %s, %d\n", rd, rj, ui6);
                            return;
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                        }
                        return;
                    }
                    default:
                        printf("LOONGARCH illegal instruction: %08X\n", code);
                        return;
                }
                return;
            }
            return;
        }
        case LA_OP_BSTRINS_D:
        {
            const char*  rd   = RegNames[code & 0x1f];
            const char*  rj   = RegNames[(code >> 5) & 0x1f];
            unsigned int lsbd = (code >> 10) & 0x3f;
            unsigned int msbd = (code >> 16) & 0x3f;
            printf("bstrins.d    %s, %s, %d, %d\n", rd, rj, msbd, lsbd);
            return;
        }
        case LA_OP_BSTRPICK_D:
        {
            const char*  rd   = RegNames[code & 0x1f];
            const char*  rj   = RegNames[(code >> 5) & 0x1f];
            unsigned int lsbd = (code >> 10) & 0x3f;
            unsigned int msbd = (code >> 16) & 0x3f;
            printf("bstrpick.d   %s, %s, %d, %d\n", rd, rj, msbd, lsbd);
            return;
        }
        case 0x4:
        {
            // bits: 31-15,MSB17
            unsigned int inscode1 = (code >> 15) & 0x1ffff;
            const char*  fd       = RegNames[(code & 0x1f) + 32];
            const char*  fj       = RegNames[((code >> 5) & 0x1f) + 32];
            const char*  fk       = RegNames[((code >> 10) & 0x1f) + 32];
            const char*  rd       = RegNames[code & 0x1f];
            const char*  rj       = RegNames[(code >> 5) & 0x1f];

            switch (inscode1)
            {
                case LA_3R_FADD_S:
                    printf("fadd.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FADD_D:
                    printf("fadd.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FSUB_S:
                    printf("fsub.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FSUB_D:
                    printf("fsub.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMUL_S:
                    printf("fmul.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMUL_D:
                    printf("fmul.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FDIV_S:
                    printf("fdiv.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FDIV_D:
                    printf("fdiv.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMAX_S:
                    printf("fmax.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMAX_D:
                    printf("fmax.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMIN_S:
                    printf("fmin.s       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMIN_D:
                    printf("fmin.d       %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMAXA_S:
                    printf("fmaxa.s      %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMAXA_D:
                    printf("fmaxa.d      %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMINA_S:
                    printf("fmina.s      %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FMINA_D:
                    printf("fmina.d      %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FSCALEB_S:
                    printf("fscaleb.s    %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FSCALEB_D:
                    printf("fscaleb.d    %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FCOPYSIGN_S:
                    printf("fcopysign.s  %s, %s, %s\n", fd, fj, fk);
                    return;
                case LA_3R_FCOPYSIGN_D:
                    printf("fcopysign.d  %s, %s, %s\n", fd, fj, fk);
                    return;
                case 0x228:
                case 0x229:
                case 0x232:
                case 0x234:
                case 0x235:
                case 0x236:
                case 0x23a:
                case 0x23c:
                {
                    // bits:31-10,MSB22
                    unsigned int inscode2 = (code >> 10) & 0x3fffff;
                    switch (inscode2)
                    {
                        case LA_2R_FABS_S:
                            printf("fabs.s       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FABS_D:
                            printf("fabs.d       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FNEG_S:
                            printf("fneg.s       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FNEG_D:
                            printf("fneg.d       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FLOGB_S:
                            printf("flogb.s      %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FLOGB_D:
                            printf("flogb.d      %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FCLASS_S:
                            printf("fclass.s     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FCLASS_D:
                            printf("fclass.d     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FSQRT_S:
                            printf("fsqrt.s      %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FSQRT_D:
                            printf("fsqrt.d      %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRECIP_S:
                            printf("frecip.s     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRECIP_D:
                            printf("frecip.d     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRSQRT_S:
                            printf("frsqrt.s     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRSQRT_D:
                            printf("frsqrt.d     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FMOV_S:
                            printf("fmov.s       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FMOV_D:
                            printf("fmov.d       %s, %s\n", fd, fj);
                            return;
                        case LA_2R_MOVGR2FR_W:
                            printf("movgr2fr.w   %s, %s\n", fd, rj);
                            return;
                        case LA_2R_MOVGR2FR_D:
                            printf("movgr2fr.d   %s, %s\n", fd, rj);
                            return;
                        case LA_2R_MOVGR2FRH_W:
                            printf("movgr2frh.w  %s, %s\n", fd, rj);
                            return;
                        case LA_2R_MOVFR2GR_S:
                            printf("movfr2gr.s   %s, %s\n", rd, fj);
                            return;
                        case LA_2R_MOVFR2GR_D:
                            printf("movfr2gr.d   %s, %s\n", rd, fj);
                            return;
                        case LA_2R_MOVFRH2GR_S:
                            printf("movfrh2gr.s  %s, %s\n", rd, fj);
                            return;
                        case LA_2R_MOVGR2FCSR:
                            NYI_LOONGARCH64("unused instr LA_2R_MOVGR2FCSR");
                            return;
                        case LA_2R_MOVFCSR2GR:
                            NYI_LOONGARCH64("unused instr LA_2R_MOVFCSR2GR");
                            return;
                        case LA_2R_MOVFR2CF:
                        {
                            const char* cd = CFregName[code & 0x7];
                            printf("movfr2cf     %s, %s\n", cd, fj);
                            return;
                        }
                        case LA_2R_MOVCF2FR:
                        {
                            const char* cj = CFregName[(code >> 5) & 0x7];
                            printf("movcf2fr     %s, %s\n", fd, cj);
                            return;
                        }
                        case LA_2R_MOVGR2CF:
                        {
                            const char* cd = CFregName[code & 0x7];
                            printf("movgr2cf     %s, %s\n", cd, rj);
                            return;
                        }
                        case LA_2R_MOVCF2GR:
                        {
                            const char* cj = CFregName[(code >> 5) & 0x7];
                            printf("movcf2gr     %s, %s\n", rd, cj);
                            return;
                        }
                        case LA_2R_FCVT_S_D:
                            printf("fcvt.s.d     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FCVT_D_S:
                            printf("fcvt.d.s     %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRM_W_S:
                            printf("ftintrm.w.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRM_W_D:
                            printf("ftintrm.w.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRM_L_S:
                            printf("ftintrm.l.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRM_L_D:
                            printf("ftintrm.l.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRP_W_S:
                            printf("ftintrp.w.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRP_W_D:
                            printf("ftintrp.w.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRP_L_S:
                            printf("ftintrp.l.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRP_L_D:
                            printf("ftintrp.l.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRZ_W_S:
                            printf("ftintrz.w.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRZ_W_D:
                            printf("ftintrz.w.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRZ_L_S:
                            printf("ftintrz.l.s  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRZ_L_D:
                            printf("ftintrz.l.d  %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRNE_W_S:
                            printf("ftintrne.w.s %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRNE_W_D:
                            printf("ftintrne.w.d %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRNE_L_S:
                            printf("ftintrne.l.s %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINTRNE_L_D:
                            printf("ftintrne.l.d %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINT_W_S:
                            printf("ftint.w.s    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINT_W_D:
                            printf("ftint.w.d    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINT_L_S:
                            printf("ftint.l.s    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FTINT_L_D:
                            printf("ftint.l.d    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FFINT_S_W:
                            printf("ffint.s.w    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FFINT_S_L:
                            printf("ffint.s.l    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FFINT_D_W:
                            printf("ffint.d.w    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FFINT_D_L:
                            printf("ffint.d.l    %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRINT_S:
                            printf("frint.s      %s, %s\n", fd, fj);
                            return;
                        case LA_2R_FRINT_D:
                            printf("frint.d      %s, %s\n", fd, fj);
                            return;
                        default:
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                            return;
                    }
                    return;
                }

                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            return;
        }
        case LA_2RI12_SLTI: // 0x8
        {
            const char* rd   = RegNames[code & 0x1f];
            const char* rj   = RegNames[(code >> 5) & 0x1f];
            short       si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("slti         %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_SLTUI: // 0x9
        {
            const char* rd   = RegNames[code & 0x1f];
            const char* rj   = RegNames[(code >> 5) & 0x1f];
            short       si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("sltui        %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_ADDI_W: // 0xa
        {
            const char* rd   = RegNames[code & 0x1f];
            const char* rj   = RegNames[(code >> 5) & 0x1f];
            short       si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("addi.w       %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_ADDI_D: // 0xb
        {
            const char* rd   = RegNames[code & 0x1f];
            const char* rj   = RegNames[(code >> 5) & 0x1f];
            short       si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("addi.d       %s, %s, %ld\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_LU52I_D: // 0xc
        {
            const char*  rd   = RegNames[code & 0x1f];
            const char*  rj   = RegNames[(code >> 5) & 0x1f];
            unsigned int si12 = (code >> 10) & 0xfff;
            printf("lu52i.d      %s, %s, 0x%x\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_ANDI: // 0xd
        {
            if (code == 0x03400000)
            {
                printf("nop\n");
            }
            else
            {
                const char*  rd   = RegNames[code & 0x1f];
                const char*  rj   = RegNames[(code >> 5) & 0x1f];
                unsigned int ui12 = ((code >> 10) & 0xfff);
                printf("andi         %s, %s, 0x%x\n", rd, rj, ui12);
            }
            return;
        }
        case LA_2RI12_ORI: // 0xe
        {
            const char*  rd   = RegNames[code & 0x1f];
            const char*  rj   = RegNames[(code >> 5) & 0x1f];
            unsigned int ui12 = ((code >> 10) & 0xfff);
            printf("ori          %s, %s, 0x%x\n", rd, rj, ui12);
            return;
        }
        case LA_2RI12_XORI: // 0xf
        {
            const char*  rd   = RegNames[code & 0x1f];
            const char*  rj   = RegNames[(code >> 5) & 0x1f];
            unsigned int ui12 = ((code >> 10) & 0xfff);
            printf("xori         %s, %s, 0x%x\n", rd, rj, ui12);
            return;
        }

        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

// Label_OPCODE_1:
//    opcode = (code >> 24) & 0xff;
//    //bits: 31-24,MSB8

Label_OPCODE_2:
    opcode = (code >> 20) & 0xfff;

    // bits: 31-20,MSB12
    switch (opcode)
    {
        case LA_4R_FMADD_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fmadd.s      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FMADD_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fmadd.d      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FMSUB_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fmsub.s      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FMSUB_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fmsub.d      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FNMADD_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fnmadd.s     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FNMADD_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fnmadd.d     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FNMSUB_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fnmsub.s     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        case LA_4R_FNMSUB_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* fa = RegNames[((code >> 15) & 0x1f) + 32];
            printf("fnmsub.d     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_3:
    opcode = (code >> 20) & 0xfff;

    // bits: 31-20,MSB12
    switch (opcode)
    {
        case LA_OP_FCMP_cond_S:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            const char*  cd   = CFregName[code & 0x7];
            const char*  fj   = RegNames[((code >> 5) & 0x1f) + 32];
            const char*  fk   = RegNames[((code >> 10) & 0x1f) + 32];
            switch (cond)
            {
                case 0x0:
                    printf("fcmp.caf.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x1:
                    printf("fcmp.saf.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x2:
                    printf("fcmp.clt.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x3:
                    printf("fcmp.slt.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x4:
                    printf("fcmp.ceq.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x5:
                    printf("fcmp.seq.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x6:
                    printf("fcmp.cle.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x7:
                    printf("fcmp.sle.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x8:
                    printf("fcmp.cun.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x9:
                    printf("fcmp.sun.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xA:
                    printf("fcmp.cult.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xB:
                    printf("fcmp.sult.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xC:
                    printf("fcmp.cueq.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xD:
                    printf("fcmp.sueq.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xE:
                    printf("fcmp.cule.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xF:
                    printf("fcmp.sule.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x10:
                    printf("fcmp.cne.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x11:
                    printf("fcmp.sne.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x14:
                    printf("fcmp.cor.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x15:
                    printf("fcmp.sor.s   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x18:
                    printf("fcmp.cune.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x19:
                    printf("fcmp.sune.s  %s, %s, %s\n", cd, fj, fk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
        case LA_OP_FCMP_cond_D:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            const char*  cd   = CFregName[code & 0x7];
            const char*  fj   = RegNames[((code >> 5) & 0x1f) + 32];
            const char*  fk   = RegNames[((code >> 10) & 0x1f) + 32];
            switch (cond)
            {
                case 0x0:
                    printf("fcmp.caf.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x1:
                    printf("fcmp.saf.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x2:
                    printf("fcmp.clt.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x3:
                    printf("fcmp.slt.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x4:
                    printf("fcmp.ceq.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x5:
                    printf("fcmp.seq.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x6:
                    printf("fcmp.cle.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x7:
                    printf("fcmp.sle.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x8:
                    printf("fcmp.cun.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x9:
                    printf("fcmp.sun.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xA:
                    printf("fcmp.cult.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xB:
                    printf("fcmp.sult.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xC:
                    printf("fcmp.cueq.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xD:
                    printf("fcmp.sueq.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xE:
                    printf("fcmp.cule.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0xF:
                    printf("fcmp.sule.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x10:
                    printf("fcmp.cne.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x11:
                    printf("fcmp.sne.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x14:
                    printf("fcmp.cor.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x15:
                    printf("fcmp.sor.d   %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x18:
                    printf("fcmp.cune.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                case 0x19:
                    printf("fcmp.sune.d  %s, %s, %s\n", cd, fj, fk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
        case LA_4R_FSEL:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
            const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
            const char* ca = CFregName[(code >> 15) & 0x7];
            printf("fsel         %s, %s, %s, %s\n", fd, fj, fk, ca);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_E:
    opcode = (code >> 15) & 0x1ffff;

    // bits: 31-15,MSB17
    switch (opcode)
    {
        case LA_3R_LDX_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.b        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.h        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.w        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.d        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STX_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stx.b        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STX_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stx.h        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STX_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stx.w        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STX_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stx.d        %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_BU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.bu       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_HU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.hu       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDX_WU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldx.wu       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_PRELDX:
            NYI_LOONGARCH64("unused instr LA_3R_PRELDX");
            return;
        case LA_3R_FLDX_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldx.s       %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FLDX_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldx.d       %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTX_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstx.s       %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTX_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstx.d       %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_AMSWAP_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amswap.w     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMSWAP_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amswap.d     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMADD_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amadd.w      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMADD_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amadd.d      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMAND_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amand.w      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMAND_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amand.d      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMOR_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amor.w       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMOR_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amor.d       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMXOR_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amxor.w      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMXOR_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amxor.d      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax.w      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax.d      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin.w      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin.d      %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_WU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax.wu     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_DU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax.du     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_WU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin.wu     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_DU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin.du     %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMSWAP_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amswap_db.w  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMSWAP_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amswap_db.d  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMADD_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amadd_db.w   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMADD_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amadd_db.d   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMAND_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amand_db.w   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMAND_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amand_db.d   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMOR_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amor_db.w    %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMOR_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amor_db.d    %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMXOR_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amxor_db.w   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMXOR_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("amxor_db.d   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax_db.w   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax_db.d   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_DB_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin_db.w   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_DB_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin_db.d   %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_DB_WU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax_db.wu  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMAX_DB_DU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammax_db.du  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_DB_WU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin_db.wu  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_AMMIN_DB_DU:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ammin_db.du  %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_OP_DBAR:
        {
            unsigned int hint = code & 0x7fff;
            printf("dbar         0x%x\n", hint);
            return;
        }
        case LA_OP_IBAR:
        {
            unsigned int hint = code & 0x7fff;
            printf("ibar         0x%x\n", hint);
            return;
        }
        case LA_3R_FLDGT_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldgt.s      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FLDGT_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldgt.d      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FLDLE_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldle.s      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FLDLE_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fldle.d      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTGT_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstgt.s      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTGT_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstgt.d      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTLE_S:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstle.s      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_FSTLE_D:
        {
            const char* fd = RegNames[(code & 0x1f) + 32];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("fstle.d      %s, %s, %s\n", fd, rj, rk);
            return;
        }
        case LA_3R_LDGT_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldgt.b       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDGT_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldgt.h       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDGT_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldgt.w       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDGT_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldgt.d       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDLE_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldle.b       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDLE_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldle.h       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDLE_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldle.w       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_LDLE_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("ldle.d       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STGT_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stgt.b       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STGT_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stgt.h       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STGT_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stgt.w       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STGT_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stgt.d       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STLE_B:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stle.b       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STLE_H:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stle.h       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STLE_W:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stle.w       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        case LA_3R_STLE_D:
        {
            const char* rd = RegNames[code & 0x1f];
            const char* rj = RegNames[(code >> 5) & 0x1f];
            const char* rk = RegNames[(code >> 10) & 0x1f];
            printf("stle.d       %s, %s, %s\n", rd, rj, rk);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }
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
    NYI_LOONGARCH64("LA not used the emitter::emitDispIns");
}

/*****************************************************************************
 *
 *  Display a stack frame reference.
 */

void emitter::emitDispFrameRef(int varx, int disp, int offs, bool asmfm)
{
    printf("[");

    if (varx < 0)
        printf("TEMP_%02u", -varx);
    else
        emitComp->gtDispLclVar(+varx, false);

    if (disp < 0)
        printf("-0x%02x", -disp);
    else if (disp > 0)
        printf("+0x%02x", +disp);

    printf("]");

    if (varx >= 0 && emitComp->opts.varNames)
    {
        LclVarDsc*  varDsc;
        const char* varName;

        assert((unsigned)varx < emitComp->lvaCount);
        varDsc  = emitComp->lvaTable + varx;
        varName = emitComp->compLocalVarName(varx, offs);

        if (varName)
        {
            printf("'%s", varName);

            if (disp < 0)
                printf("-%d", -disp);
            else if (disp > 0)
                printf("+%d", +disp);

            printf("'");
        }
    }
}

#endif // DEBUG

// Generate code for a load or store operation with a potentially complex addressing mode
// This method handles the case of a GT_IND with contained GT_LEA op1 of the x86 form [base + index*sccale + offset]
// Since LOONGARCH64 does not directly support this complex of an addressing mode
// we may generates up to three instructions for this for LOONGARCH64
//
void emitter::emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir)
{
    GenTree* addr = indir->Addr();

    if (addr->isContained())
    {
        assert(addr->OperIs(GT_CLS_VAR_ADDR, GT_LCL_ADDR, GT_LEA));

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
                        emitIns_R_R_I(INS_slli_d, addType, REG_R21, index->GetRegNum(), lsl);
                        emitIns_R_R_R(INS_add_d, addType, tmpReg, memBase->GetRegNum(), REG_R21);
                    }
                    else // no scale
                    {
                        // Generate code to set tmpReg = base + index
                        emitIns_R_R_R(INS_add_d, addType, tmpReg, memBase->GetRegNum(), index->GetRegNum());
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
                    emitIns_R_R_R(INS_add_d, addType, tmpReg, tmpReg, memBase->GetRegNum());

                    noway_assert(emitInsIsLoad(ins) || (tmpReg != dataReg));
                    noway_assert(tmpReg != index->GetRegNum());

                    // Then load/store dataReg from/to [tmpReg + index*scale]
                    emitIns_R_R_I(INS_slli_d, addType, REG_R21, index->GetRegNum(), lsl);
                    emitIns_R_R_R(INS_add_d, addType, tmpReg, tmpReg, REG_R21);
                    emitIns_R_R_I(ins, attr, dataReg, tmpReg, 0);
                }
            }
            else // (offset == 0)
            {
                // Then load/store dataReg from/to [memBase + index]
                switch (EA_SIZE(emitTypeSize(indir->TypeGet())))
                {
                    case EA_1BYTE:
                        assert(((ins <= INS_ld_wu) && (ins >= INS_ld_b)) || ((ins <= INS_st_d) && (ins >= INS_st_b)));
                        if (ins <= INS_ld_wu)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_ldx_bu;
                            else
                                ins = INS_ldx_b;
                        }
                        else
                            ins = INS_stx_b;
                        break;
                    case EA_2BYTE:
                        assert(((ins <= INS_ld_wu) && (ins >= INS_ld_b)) || ((ins <= INS_st_d) && (ins >= INS_st_b)));
                        if (ins <= INS_ld_wu)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_ldx_hu;
                            else
                                ins = INS_ldx_h;
                        }
                        else
                            ins = INS_stx_h;
                        break;
                    case EA_4BYTE:
                        assert(((ins <= INS_ld_wu) && (ins >= INS_ld_b)) || ((ins <= INS_st_d) && (ins >= INS_st_b)) ||
                               (ins == INS_fst_s) || (ins == INS_fld_s));
                        assert(INS_fst_s > INS_st_d);
                        if (ins <= INS_ld_wu)
                        {
                            if (varTypeIsUnsigned(indir->TypeGet()))
                                ins = INS_ldx_wu;
                            else
                                ins = INS_ldx_w;
                        }
                        else if (ins == INS_fld_s)
                            ins = INS_fldx_s;
                        else if (ins == INS_fst_s)
                            ins = INS_fstx_s;
                        else
                            ins = INS_stx_w;
                        break;
                    case EA_8BYTE:
                        assert(((ins <= INS_ld_wu) && (ins >= INS_ld_b)) || ((ins <= INS_st_d) && (ins >= INS_st_b)) ||
                               (ins == INS_fst_d) || (ins == INS_fld_d));
                        assert(INS_fst_d > INS_st_d);
                        if (ins <= INS_ld_wu)
                        {
                            ins = INS_ldx_d;
                        }
                        else if (ins == INS_fld_d)
                            ins = INS_fldx_d;
                        else if (ins == INS_fst_d)
                            ins = INS_fstx_d;
                        else
                            ins = INS_stx_d;
                        break;
                    default:
                        assert(!"------------TODO for LOONGARCH64: unsupported ins.");
                }

                if (lsl > 0)
                {
                    // Then load/store dataReg from/to [memBase + index*scale]
                    emitIns_R_R_I(INS_slli_d, emitActualTypeSize(index->TypeGet()), REG_R21, index->GetRegNum(), lsl);
                    emitIns_R_R_R(ins, attr, dataReg, memBase->GetRegNum(), REG_R21);
                }
                else // no scale
                {
                    emitIns_R_R_R(ins, attr, dataReg, memBase->GetRegNum(), index->GetRegNum());
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
            else if (addr->OperIs(GT_LCL_ADDR))
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
                emitIns_R_R_R(INS_add_d, addType, tmpReg, memBase->GetRegNum(), tmpReg);
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
    NYI_LOONGARCH64("emitInsBinary-----unused");
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

    bool needCheckOv = dst->gtOverflowEx();

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

    if (needCheckOv)
    {
        if (ins == INS_add_d)
        {
            assert(attr == EA_8BYTE);
        }
        else if (ins == INS_add_w) // || ins == INS_add
        {
            assert(attr == EA_4BYTE);
        }
        else if (ins == INS_addi_d)
        {
            assert(intConst != nullptr);
        }
        else if (ins == INS_addi_w)
        {
            assert(intConst != nullptr);
        }
        else if (ins == INS_sub_d)
        {
            assert(attr == EA_8BYTE);
        }
        else if (ins == INS_sub_w)
        {
            assert(attr == EA_4BYTE);
        }
        else if ((ins == INS_mul_d) || (ins == INS_mulh_d) || (ins == INS_mulh_du))
        {
            assert(attr == EA_8BYTE);
            // NOTE: overflow format doesn't support an int constant operand directly.
            assert(intConst == nullptr);
        }
        else if ((ins == INS_mul_w) || (ins == INS_mulw_d_w) || (ins == INS_mulh_w) || (ins == INS_mulh_wu) ||
                 (ins == INS_mulw_d_wu))
        {
            assert(attr == EA_4BYTE);
            // NOTE: overflow format doesn't support an int constant operand directly.
            assert(intConst == nullptr);
        }
        else
        {
#ifdef DEBUG
            printf("LOONGARCH64-Invalid ins for overflow check: %s\n", codeGen->genInsName(ins));
#endif
            assert(!"Invalid ins for overflow check");
        }
    }

    if (intConst != nullptr)
    {
        ssize_t imm = intConst->IconValue();
        if (ins == INS_andi || ins == INS_ori || ins == INS_xori)
        {
            assert(isValidUimm12(imm));
        }
        else
        {
            assert(isValidSimm12(imm));
        }

        if (ins == INS_sub_d)
        {
            assert(attr == EA_8BYTE);
            assert(imm != -2048);
            ins = INS_addi_d;
            imm = -imm;
        }
        else if (ins == INS_sub_w)
        {
            assert(attr == EA_4BYTE);
            assert(imm != -2048);
            ins = INS_addi_w;
            imm = -imm;
        }

        assert(ins == INS_addi_d || ins == INS_addi_w || ins == INS_andi || ins == INS_ori || ins == INS_xori);

        if (needCheckOv)
        {
            emitIns_R_R_R(INS_or, attr, REG_R21, nonIntReg->GetRegNum(), REG_R0);
        }

        emitIns_R_R_I(ins, attr, dst->GetRegNum(), nonIntReg->GetRegNum(), imm);

        if (needCheckOv)
        {
            if (ins == INS_addi_d || ins == INS_addi_w)
            {
                // A = B + C
                if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                {
                    codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bltu, dst->GetRegNum(), nullptr, REG_R21);
                }
                else
                {
                    if (imm > 0)
                    {
                        // B > 0 and C > 0, if A < B, goto overflow
                        BasicBlock* tmpLabel = codeGen->genCreateTempLabel();
                        emitIns_J_cond_la(INS_bge, tmpLabel, REG_R0, REG_R21);
                        emitIns_R_R_I(INS_slti, EA_PTRSIZE, REG_R21, dst->GetRegNum(), imm);

                        codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21);

                        codeGen->genDefineTempLabel(tmpLabel);
                    }
                    else if (imm < 0)
                    {
                        // B < 0 and C < 0, if A > B, goto overflow
                        BasicBlock* tmpLabel = codeGen->genCreateTempLabel();
                        emitIns_J_cond_la(INS_bge, tmpLabel, REG_R21, REG_R0);
                        emitIns_R_R_I(INS_addi_d, attr, REG_R21, REG_R0, imm);

                        codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, REG_R21, nullptr, dst->GetRegNum());

                        codeGen->genDefineTempLabel(tmpLabel);
                    }
                }
            }
            else
            {
                assert(!"unimplemented on LOONGARCH yet");
            }
        }
    }
    else if (varTypeIsFloating(dst))
    {
        emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());
    }
    else if (dst->OperGet() == GT_MUL)
    {
        if (!needCheckOv && !(dst->gtFlags & GTF_UNSIGNED))
        {
            emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());
        }
        else
        {
            if (needCheckOv)
            {
                assert(REG_R21 != dst->GetRegNum());
                assert(REG_R21 != src1->GetRegNum());
                assert(REG_R21 != src2->GetRegNum());

                instruction ins2;

                if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                {
                    if (attr == EA_4BYTE)
                        ins2 = INS_mulh_wu;
                    else
                        ins2 = INS_mulh_du;
                }
                else
                {
                    if (attr == EA_8BYTE)
                        ins2 = INS_mulh_d;
                    else
                        ins2 = INS_mulh_w;
                }

                emitIns_R_R_R(ins2, attr, REG_R21, src1->GetRegNum(), src2->GetRegNum());
            }

            // n * n bytes will store n bytes result
            emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());

            if ((dst->gtFlags & GTF_UNSIGNED) != 0)
            {
                if (attr == EA_4BYTE)
                    emitIns_R_R_I_I(INS_bstrins_d, EA_8BYTE, dst->GetRegNum(), REG_R0, 63, 32);
            }

            if (needCheckOv)
            {
                assert(REG_R21 != dst->GetRegNum());
                assert(REG_R21 != src1->GetRegNum());
                assert(REG_R21 != src2->GetRegNum());

                if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                {
                    codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21);
                }
                else
                {
                    assert(REG_RA != dst->GetRegNum());
                    assert(REG_RA != src1->GetRegNum());
                    assert(REG_RA != src2->GetRegNum());
                    size_t imm = (EA_SIZE(attr) == EA_8BYTE) ? 63 : 31;
                    emitIns_R_R_I(EA_SIZE(attr) == EA_8BYTE ? INS_srai_d : INS_srai_w, attr, REG_RA, dst->GetRegNum(),
                                  imm);
                    codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21, nullptr, REG_RA);
                }
            }
        }
    }
    else if (dst->OperIs(GT_AND, GT_AND_NOT, GT_OR, GT_XOR))
    {
        emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());

        // TODO-LOONGARCH64-CQ: here sign-extend dst when deal with 32bit data is too conservative.
        if (EA_SIZE(attr) == EA_4BYTE)
            emitIns_R_R_I(INS_slli_w, attr, dst->GetRegNum(), dst->GetRegNum(), 0);
    }
    else
    {
        regNumber regOp1       = src1->GetRegNum();
        regNumber regOp2       = src2->GetRegNum();
        regNumber saveOperReg1 = REG_NA;
        regNumber saveOperReg2 = REG_NA;

        if ((dst->gtFlags & GTF_UNSIGNED) && (attr == EA_8BYTE))
        {
            if (src1->gtType == TYP_INT)
            {
                assert(REG_R21 != regOp1);
                assert(REG_RA != regOp1);
                emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, REG_RA, regOp1, /*src1->GetRegNum(),*/ 31, 0);
                regOp1 = REG_RA; // dst->ExtractTempReg();
            }
            if (src2->gtType == TYP_INT)
            {
                assert(REG_R21 != regOp2);
                assert(REG_RA != regOp2);
                emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, REG_R21, regOp2, /*src2->GetRegNum(),*/ 31, 0);
                regOp2 = REG_R21; // dst->ExtractTempReg();
            }
        }
        if (needCheckOv)
        {
            assert(!varTypeIsFloating(dst));

            assert(REG_R21 != dst->GetRegNum());
            assert(REG_RA != dst->GetRegNum());

            if (dst->GetRegNum() == regOp1)
            {
                assert(REG_R21 != regOp1);
                assert(REG_RA != regOp1);
                saveOperReg1 = REG_R21;
                saveOperReg2 = regOp2;
                emitIns_R_R_R(INS_or, attr, REG_R21, regOp1, REG_R0);
            }
            else if (dst->GetRegNum() == regOp2)
            {
                assert(REG_R21 != regOp2);
                assert(REG_RA != regOp2);
                saveOperReg1 = regOp1;
                saveOperReg2 = REG_R21;
                emitIns_R_R_R(INS_or, attr, REG_R21, regOp2, REG_R0);
            }
            else
            {
                saveOperReg1 = regOp1;
                saveOperReg2 = regOp2;
            }
        }

        emitIns_R_R_R(ins, attr, dst->GetRegNum(), regOp1, regOp2);

        if (needCheckOv)
        {
            if (dst->OperGet() == GT_ADD || dst->OperGet() == GT_SUB)
            {
                ssize_t   imm;
                regNumber tempReg1;
                regNumber tempReg2;
                // ADD : A = B + C
                // SUB : C = A - B
                if ((dst->gtFlags & GTF_UNSIGNED) != 0)
                {
                    // if A < B, goto overflow
                    if (dst->OperGet() == GT_ADD)
                    {
                        tempReg1 = dst->GetRegNum();
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
                    tempReg2 = codeGen->rsGetRsvdReg();
                    assert(tempReg1 != tempReg2);
                    assert(tempReg1 != saveOperReg1);
                    assert(tempReg2 != saveOperReg2);

                    ssize_t ui6 = (attr == EA_4BYTE) ? 31 : 63;
                    if (dst->OperGet() == GT_ADD)
                        emitIns_R_R_I(INS_srli_d, attr, tempReg1, saveOperReg1, ui6);
                    else
                        emitIns_R_R_I(INS_srli_d, attr, tempReg1, dst->GetRegNum(), ui6);
                    emitIns_R_R_I(INS_srli_d, attr, tempReg2, saveOperReg2, ui6);

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
                    emitIns_J_cond_la(INS_bge, tmpLabel, dst->OperGet() == GT_ADD ? dst->GetRegNum() : saveOperReg1,
                                      dst->OperGet() == GT_ADD ? saveOperReg1 : saveOperReg2);

                    codeGen->genDefineTempLabel(tmpLabel2);

                    codeGen->genJumpToThrowHlpBlk(EJ_jmp, SCK_OVERFLOW);

                    codeGen->genDefineTempLabel(tmpLabel3);

                    // B < 0 and C < 0, if A > B, goto overflow
                    emitIns_J_cond_la(INS_blt, tmpLabel2, dst->OperGet() == GT_ADD ? saveOperReg1 : saveOperReg2,
                                      dst->OperGet() == GT_ADD ? dst->GetRegNum() : saveOperReg1);

                    codeGen->genDefineTempLabel(tmpLabel);
                }
            }
            else
            {
#ifdef DEBUG
                printf("---------[LOONGARCH64]-NOTE: UnsignedOverflow instruction %d\n", ins);
#endif
                assert(!"unimplemented on LOONGARCH yet");
            }
        }
    }

    return dst->GetRegNum();
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

    // TODO-LoongArch64: support this function.
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
// TODO-LoongArch64: supporting SIMD.
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
    switch (ins)
    {
        case INS_mov:
        case INS_fmov_s:
        case INS_fmov_d:
        case INS_movgr2fr_w:
        case INS_movgr2fr_d:
        case INS_movfr2gr_s:
        case INS_movfr2gr_d:
        {
            return true;
        }

        default:
        {
            return false;
        }
    }
}
#endif // defined(TARGET_LOONGARCH64)
