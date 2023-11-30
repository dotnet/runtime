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

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    if (emitIsSmallInsDsc(id))
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
#ifdef FEATURE_SIMD
        case INS_vst:
        case INS_vstx:
        case INS_xvst:
        case INS_xvstx:
#endif
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
    if ((ins == INS_stx_d) || (ins == INS_stx_w) || (ins == INS_stx_h) || (ins == INS_stx_b) ||
#ifdef FEATURE_SIMD
        (ins == INS_vstx) || (ins == INS_xvstx) ||
#endif
        (ins == INS_fstx_s) || (ins == INS_fstx_d))
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

#ifdef FEATURE_SIMD
        case INS_vld:
        case INS_xvld:
#endif

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

#ifdef FEATURE_SIMD
        case INS_vldi:
        case INS_xvldi:
            assert(isVectorRegister(reg));
            assert((imm >> 13) == 0);
            code |= (reg & 0x1f); // vd/xd
            code |= imm << 5;     // si13
            break;
        case INS_vseteqz_v:
        case INS_vsetnez_v:
        case INS_vsetanyeqz_b:
        case INS_vsetanyeqz_h:
        case INS_vsetanyeqz_w:
        case INS_vsetanyeqz_d:
        case INS_vsetallnez_b:
        case INS_vsetallnez_h:
        case INS_vsetallnez_w:
        case INS_vsetallnez_d:
        case INS_xvseteqz_v:
        case INS_xvsetnez_v:
        case INS_xvsetanyeqz_b:
        case INS_xvsetanyeqz_h:
        case INS_xvsetanyeqz_w:
        case INS_xvsetanyeqz_d:
        case INS_xvsetallnez_b:
        case INS_xvsetallnez_h:
        case INS_xvsetallnez_w:
        case INS_xvsetallnez_d:
            assert(isVectorRegister(reg));
            assert((imm >> 3) == 0);

            code |= imm;               // cc
            code |= (reg & 0x1f) << 5; // vj/xj
            break;
#endif

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
#ifdef FEATURE_SIMD
        // This include dstReg is Float/SIMD type.
        if (isVectorRegister(dstReg))
        {
            assert(size <= EA_32BYTE);
            if (isVectorRegister(srcReg))
            {
                ins = size == EA_32BYTE ? INS_xvbsll_v : INS_vbsll_v;
                emitIns_R_R_I(ins, size, dstReg, srcReg, 0);
            }
            else
            {
                assert((INS_vreplgr2vr_b <= ins && ins <= INS_vreplgr2vr_d) ||
                       (INS_xvreplgr2vr_b <= ins && ins <= INS_xvreplgr2vr_d) ||
                       (INS_movgr2fr_w <= ins && ins <= INS_movgr2fr_d));
                emitIns_R_R(ins, attr, dstReg, srcReg);
            }
        }
        else
#endif
        {
            if ((EA_4BYTE == attr) && (INS_mov == ins))
            {
                emitIns_R_R_I(INS_slli_w, attr, dstReg, srcReg, 0);
            }
            else
            {
                emitIns_R_R(ins, attr, dstReg, srcReg);
            }
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
#ifdef FEATURE_SIMD
    else if (((INS_vreplgr2vr_b <= ins) && (ins <= INS_vreplgr2vr_d)) ||
             ((INS_xvreplgr2vr_b <= ins) && (ins <= INS_xvreplgr2vr_d)))
    {
        // [x]vreplgr2vr.{b/h/w/d} xd, rj
        assert(isVectorRegister(reg1));      // vd(xd)
        assert(isGeneralRegisterOrR0(reg2)); // rj
        code |= (reg1 & 0x1f);               // xd , the bit field in the instruction is between 0 and 31.
        code |= reg2 << 5;                   // rj
    }
    else if (((INS_vclo_b <= ins) && (ins <= INS_vextl_qu_du)) || ((INS_xvclo_b <= ins) && (ins <= INS_xvextl_qu_du)))
    {
        assert(isVectorRegister(reg1));
        assert(isVectorRegister(reg2));

        code |= (reg1 & 0x1f);      // vd(xd)
        code |= (reg2 & 0x1f) << 5; // vj(xj)
    }
#endif // FEATURE_SIMD
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
#ifdef FEATURE_SIMD
    else if (((INS_vseqi_b <= ins) && (ins <= INS_vmini_d)) || ((INS_xvseqi_b <= ins) && (ins <= INS_xvmin_d)))
    {
        assert(isVectorRegister(reg1));
        assert(isVectorRegister(reg2));
        assert((-16 <= imm) && (imm <= 15));

        code |= reg1 & 0x1f;        // vd/xd
        code |= (reg2 & 0x1f) << 5; // vj/xj
        code |= (imm & 0x1f) << 10; // si5
    }
    else if ((INS_vldrepl_d == ins) || (INS_xvldrepl_d == ins))
    {
        assert(isVectorRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-256 <= imm) && (imm <= 255));
        code |= reg1 & 0x1f;         // vd/xd
        code |= (reg2 & 0x1f) << 5;  // rj
        code |= (imm & 0x1ff) << 10; // si9
    }
    else if ((INS_vldrepl_w == ins) || (INS_xvldrepl_w == ins))
    {
        assert(isVectorRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-512 <= imm) && (imm <= 511));
        code |= reg1 & 0x1f;         // vd/xd
        code |= (reg2 & 0x1f) << 5;  // rj
        code |= (imm & 0x3ff) << 10; // si10
    }
    else if ((INS_vldrepl_h == ins) || (INS_xvldrepl_h == ins))
    {
        assert(isVectorRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-1024 <= imm) && (imm <= 1023));
        code |= reg1 & 0x1f;         // vd/xd
        code |= (reg2 & 0x1f) << 5;  // rj
        code |= (imm & 0x7ff) << 10; // si11
    }
    else if (((INS_vld <= ins) && (ins <= INS_vst)) || ((INS_xvld <= ins) && (ins <= INS_xvst)))
    {
        assert(isVectorRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert((-2048 <= imm) && (imm <= 2047));

        code |= reg1 & 0x1f;         // vd(xd)
        code |= (reg2 & 0x1f) << 5;  // rj
        code |= (imm & 0xfff) << 10; // si12
    }
    else if (((INS_vinsgr2vr_d <= ins) && (ins <= INS_vreplvei_d)) || (INS_xvrepl128vei_d == ins))
    {
#ifdef DEBUG
        if (INS_vinsgr2vr_d == ins)
        {
            assert(isVectorRegister(reg1));      // xd
            assert(isGeneralRegisterOrR0(reg2)); // rj
        }
        else if ((INS_vreplvei_d == ins) || (INS_xvrepl128vei_d == ins))
        {
            assert(isVectorRegister(reg1)); // vd/xd
            assert(isVectorRegister(reg2)); // vj/xj
        }
        else
        {
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // vj/xj
        }
        assert((0 <= imm) && (imm <= 1));
#endif

        code |= reg1 & 0x1f;
        code |= (reg2 & 0x1f) << 5;
        code |= (imm & 0x1) << 10; // ui1
    }
    else if (((INS_vinsgr2vr_w <= ins) && (ins <= INS_vreplvei_w)) ||
             ((INS_xvinsve0_d <= ins) && (ins <= INS_xvpickve2gr_du)))
    {
#ifdef DEBUG
        if (INS_vinsgr2vr_w == ins)
        {
            assert(isVectorRegister(reg1));      // vd
            assert(isGeneralRegisterOrR0(reg2)); // rj
        }
        else if ((INS_vpickve2gr_w <= ins) && (ins <= INS_vpickve2gr_wu))
        {
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // vj
        }
        else if (INS_vreplvei_w == ins)
        {
            assert(isVectorRegister(reg1)); // vd
            assert(isVectorRegister(reg2)); // vj
        }
        else if ((INS_xvinsve0_d <= ins) && (ins <= INS_xvpickve_d))
        {
            assert(isVectorRegister(reg1)); // xd
            assert(isVectorRegister(reg2)); // xj
        }
        else if (INS_xvinsgr2vr_d == ins)
        {
            assert(isVectorRegister(reg1));      // xd
            assert(isGeneralRegisterOrR0(reg2)); // rj
        }
        else if ((INS_xvpickve2gr_d <= ins) && (ins <= INS_xvpickve2gr_du))
        {
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // xj
        }
        else
        {
            assert(isVectorRegister(reg1)); // xd/vd
            assert(isVectorRegister(reg2)); // xj/vj
        }
#endif
        assert((0 <= imm) && (imm <= 3));

        code |= reg1 & 0x1f;        // xd/vd/rd
        code |= (reg2 & 0x1f) << 5; // xj/vj/rj
        code |= (imm & 0x3) << 10;  // ui2
    }
    else if (((INS_vslli_b <= ins) && (ins <= INS_vreplvei_h)) || ((INS_xvslli_b <= ins) && (ins <= INS_xvsat_bu)))
    {
#ifdef DEBUG
        if ((INS_vinsgr2vr_h == ins) || (INS_xvinsgr2vr_w == ins))
        {
            // vd/xd, rj, ui3
            assert(isVectorRegister(reg1));      // vd/xd
            assert(isGeneralRegisterOrR0(reg2)); // rj
        }
        else if ((INS_vpickve2gr_h == ins) || (INS_vpickve2gr_hu == ins) || (INS_xvpickve2gr_w == ins) ||
                 (INS_xvpickve2gr_wu == ins))
        {
            // rd, vj/xj, ui3
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // vj/xj
        }
        else
        {
            assert(isVectorRegister(reg1)); // vd/xd
            assert(isVectorRegister(reg2)); // vj/xj
        }
#endif
        assert((0 <= imm) && (imm <= 7));

        code |= reg1 & 0x1f;        // vd(xd)
        code |= (reg2 & 0x1f) << 5; // vj(xj)
        code |= (imm & 0x7) << 10;  // ui3
    }
    else if (((INS_vslli_h <= ins) && (ins <= INS_vreplvei_b)) || ((INS_xvslli_h <= ins) && (ins <= INS_xvsat_hu)))
    {
#ifdef DEBUG
        if (INS_vinsgr2vr_b == ins)
        {
            assert(isVectorRegister(reg1));      // vd/xd
            assert(isGeneralRegisterOrR0(reg2)); // rj
        }
        else if (INS_vpickve2gr_b == ins)
        {
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // vj/xj
        }
        else if (INS_vpickve2gr_bu == ins)
        {
            assert(isGeneralRegisterOrR0(reg1)); // rd
            assert(isVectorRegister(reg2));      // vj/xj
        }
        else
        {
            assert(isVectorRegister(reg1)); // vd/xd
            assert(isVectorRegister(reg2)); // vj/xj
        }
#endif
        assert((0 <= imm) && (imm <= 15));

        code |= reg1 & 0x1f;        // vd
        code |= (reg2 & 0x1f) << 5; // vj
        code |= (imm & 0xf) << 10;  // ui4
    }
    else if (((INS_vslei_bu <= ins) && (ins <= INS_vsat_wu)) || ((INS_xvslei_bu <= ins) && (ins <= INS_xvsat_wu)))
    {
        assert(isVectorRegister(reg1)); // vd/xd
        assert(isVectorRegister(reg2)); // vj/xj

        assert((0 <= imm) && (imm <= 31));

        code |= reg1 & 0x1f;        // vd
        code |= (reg2 & 0x1f) << 5; // vj
        code |= (imm & 0x1f) << 10; // ui5
    }
    else if (((INS_vslli_d <= ins) && (ins <= INS_vsat_du)) || ((INS_xvslli_d <= ins) && (ins <= INS_xvsat_du)))
    {
        assert(isVectorRegister(reg1)); // vd/xd
        assert(isVectorRegister(reg2)); // vj/xj

        assert((0 <= imm) && (imm <= 63));

        code |= reg1 & 0x1f;        // vd
        code |= (reg2 & 0x1f) << 5; // vj
        code |= (imm & 0x3f) << 10; // ui6
    }
    else if (((INS_vsrlni_d_q <= ins) && (ins <= INS_vssrarni_du_q)) ||
             ((INS_xvsrlni_d_q <= ins) && (ins <= INS_xvssrarni_du_q)))
    {
        assert(isVectorRegister(reg1)); // vd/xd
        assert(isVectorRegister(reg2)); // vj/xj

        assert((0 <= imm) && (imm <= 127));

        code |= reg1 & 0x1f;        // vd
        code |= (reg2 & 0x1f) << 5; // vj
        code |= (imm & 0x7f) << 10; // ui7
    }
    else if (((INS_vextrins_d <= ins) && (ins <= INS_vpermi_w)) || ((INS_xvextrins_d <= ins) && (ins <= INS_xvpermi_q)))
    {
        assert(isVectorRegister(reg1)); // vd/xd
        assert(isVectorRegister(reg2)); // vj/xj

        assert((0 <= imm) && (imm <= 255));

        code |= reg1 & 0x1f;        // vd
        code |= (reg2 & 0x1f) << 5; // vj
        code |= (imm & 0xff) << 10; // ui8
    }
#endif
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
    else if ((INS_amswap_w <= ins) && (ins <= INS_ammin_db_du))
    {
#ifdef DEBUG
        switch (ins)
        {
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
                break;
            default:
                NYI_LOONGARCH64("illegal ins within emitIns_R_R_R --am!");
        }
#endif

        assert(isGeneralRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= (reg1 /*& 0x1f*/);       // rd
        code |= (reg2 /*& 0x1f*/) << 10; // rk
        code |= (reg3 /*& 0x1f*/) << 5;  // rj
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
#ifdef FEATURE_SIMD
    else if ((INS_vldx == ins) || (INS_vstx == ins) || (INS_xvldx == ins) || (INS_xvstx == ins))
    {
        assert(isVectorRegister(reg1));
        assert(isGeneralRegisterOrR0(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= reg1 & 0x1f; // vd(xd)
        code |= reg2 << 5;   // rj
        code |= reg3 << 10;  // rk
    }
    else if (((INS_vreplve_b <= ins) && (ins <= INS_vreplve_d)) || ((INS_xvreplve_b <= ins) && (ins <= INS_xvreplve_d)))
    {
        assert(isVectorRegister(reg1));
        assert(isVectorRegister(reg2));
        assert(isGeneralRegisterOrR0(reg3));

        code |= (reg1 & 0x1f);      // vd(xd)
        code |= (reg2 & 0x1f) << 5; // vj(xj)
        code |= reg3 << 10;         // rk
    }
    else if (((INS_vfcmp_caf_s <= ins) && (ins <= INS_vshuf_d)) || ((INS_xvfcmp_caf_s <= ins) && (ins <= INS_xvperm_w)))
    {
        assert(isVectorRegister(reg1));
        assert(isVectorRegister(reg2));
        assert(isVectorRegister(reg3));

        code |= (reg1 & 0x1f);       // vd(xd)
        code |= (reg2 & 0x1f) << 5;  // vj(xj)
        code |= (reg3 & 0x1f) << 10; // vk(xk)
    }
#endif
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
#ifdef FEATURE_SIMD
        case INS_vstelm_d:
        case INS_vstelm_w:
        case INS_vstelm_h:
        case INS_vstelm_b:
        case INS_xvstelm_d:
        case INS_xvstelm_w:
        case INS_xvstelm_h:
        case INS_xvstelm_b:
            assert(isVectorRegister(reg1));
            assert(isGeneralRegisterOrR0(reg2));
            assert((-128 <= imm1) && (imm1 <= 127)); // si8, without left shift
            code |= reg1 & 0x1f;                     // vd/xd
            code |= reg2 << 5;                       // rj
            code |= (imm1 & 0xff) << 10;             // si8
            if (INS_vstelm_d == ins)
            {
                assert((0 <= imm2) && (imm2 <= 1)); // 1 bit
                code |= (imm2 & 0x1) << 18;         // idx(1 bit)
            }
            else if ((INS_vstelm_w == ins) || (INS_xvstelm_d == ins))
            {
                assert((0 <= imm2) && (imm2 <= 3)); // 2 bit
                code |= (imm2 & 0x3) << 18;         // idx(2 bit)
            }
            else if ((INS_vstelm_h == ins) || (INS_xvstelm_w == ins))
            {
                assert((0 <= imm2) && (imm2 <= 7)); // 3 bit
                code |= (imm2 & 0x7) << 18;         // idx(3 bit)
            }
            else if ((INS_vstelm_b == ins) || (INS_xvstelm_h == ins))
            {
                assert((0 <= imm2) && (imm2 <= 15)); // 4 bit
                code |= (imm2 & 0xf) << 18;          // idx(4 bit)
            }
            else if (INS_xvstelm_b == ins)
            {
                assert((0 <= imm2) && (imm2 <= 31)); // 5 bit
                code |= (imm2 & 0x1f) << 18;         // idx(5 bit)
            }
            else
            {
                unreached();
            }
            break;
#endif
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
#ifdef FEATURE_SIMD
        case INS_vfmadd_s:
        case INS_vfmadd_d:
        case INS_vfmsub_s:
        case INS_vfmsub_d:
        case INS_vfnmadd_s:
        case INS_vfnmadd_d:
        case INS_vfnmsub_s:
        case INS_vfnmsub_d:
        case INS_vbitsel_v:
        case INS_vshuf_b:

        case INS_xvfmadd_s:
        case INS_xvfmadd_d:
        case INS_xvfmsub_s:
        case INS_xvfmsub_d:
        case INS_xvfnmadd_s:
        case INS_xvfnmadd_d:
        case INS_xvfnmsub_s:
        case INS_xvfnmsub_d:
        case INS_xvbitsel_v:
        case INS_xvshuf_b:
#endif
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
    if (emitComp->compCurBB->KindIs(BBJ_EHCATCHRET))
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
    regMaskTP gcrefRegs;
    regMaskTP byrefRegs;

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
        //   pcaddu18i  t4, addr-hi20
        //   jilr r0/1,t4,addr-lo18

        emitOutput_Instr(dst, 0x1e000000 | (int)REG_DEFAULT_HELPER_CALL_TARGET);

        size_t addr = (size_t)(id->idAddr()->iiaAddr); // get addr.

        int reg2 = (int)addr & 1;
        addr     = addr ^ 1;

        assert(isValidSimm38(addr - (ssize_t)dst));
        assert((addr & 3) == 0);

        dst += 4;
        emitGCregDeadUpd(REG_DEFAULT_HELPER_CALL_TARGET, dst);

#ifdef DEBUG
        code = emitInsCode(INS_pcaddu18i);
        assert(code == 0x1e000000);
        code = emitInsCode(INS_jirl);
        assert(code == 0x4c000000);
#endif
        emitOutput_Instr(dst, 0x4c000000 | ((int)REG_DEFAULT_HELPER_CALL_TARGET << 5) | reg2);

        emitRecordRelocation(dst - 4, (BYTE*)addr, IMAGE_REL_LOONGARCH64_JIR);
    }
    else
    {
        // lu12i_w  t4, addr_bits[31:12]   // TODO-LoongArch64: maybe optimize.
        // ori  t4, t4, addr_bits[11:0]
        // lu32i_d  t4, addr_bits[50:32]
        // jirl  t4

        ssize_t imm = (ssize_t)(id->idAddr()->iiaAddr);
        assert((uint64_t)(imm >> 32) <= 0x7ffff); // In fact max is <= 0xffff.

        int reg2 = (int)(imm & 1);
        imm -= reg2;

        code = emitInsCode(INS_lu12i_w);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET;
        code |= ((code_t)(imm >> 12) & 0xfffff) << 5;

        emitOutput_Instr(dst, code);
        dst += 4;
        emitGCregDeadUpd(REG_DEFAULT_HELPER_CALL_TARGET, dst);

        code = emitInsCode(INS_ori);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 5;
        code |= (code_t)(imm & 0xfff) << 10;
        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_lu32i_d);
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET;
        code |= ((imm >> 32) & 0x7ffff) << 5;

        emitOutput_Instr(dst, code);
        dst += 4;

        code = emitInsCode(INS_jirl);
        code |= (code_t)reg2;
        code |= (code_t)REG_DEFAULT_HELPER_CALL_TARGET << 5;
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
        emitStackPop(dst, /*isCall*/ true, sizeof(code_t), /*args*/ 0);

        // Do we need to record a call location for GC purposes?
        //
        if (!emitFullGCinfo)
        {
            emitRecordGCcall(dst, sizeof(code_t));
        }
    }

    return id->idCodeSize();
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
    size_t      sz;

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

#ifdef FEATURE_SIMD
static const char * const  vRegNames[] =
{
    "vr0",  "vr1",  "vr2",  "vr3",  "vr4",
    "vr5",  "vr6",  "vr7",  "vr8",  "vr9",
    "vr10", "vr11", "vr12", "vr13", "vr14",
    "vr15", "vr16", "vr17", "vr18", "vr19",
    "vr20", "vr21", "vr22", "vr23", "vr24",
    "vr25", "vr26", "vr27", "vr28", "vr29",
    "vr30", "vr31"
};

static const char * const  xRegNames[] =
{
    "xr0",  "xr1",  "xr2",  "xr3",  "xr4",
    "xr5",  "xr6",  "xr7",  "xr8",  "xr9",
    "xr10", "xr11", "xr12", "xr13", "xr14",
    "xr15", "xr16", "xr17", "xr18", "xr19",
    "xr20", "xr21", "xr22", "xr23", "xr24",
    "xr25", "xr26", "xr27", "xr28", "xr29",
    "xr30", "xr31"
};
#endif
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
    const char* const FcsrName[]  = {"fcsr0", "fcsr1", "fcsr2", "fcsr3"};

    instruction ins = id->idIns();

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

    // General registers
    const char* rd = RegNames[code & 0x1f];
    const char* rj = RegNames[(code >> 5) & 0x1f];
    const char* rk = RegNames[(code >> 10) & 0x1f];
    // Float registers
    const char* fd = RegNames[(code & 0x1f) + 32];
    const char* fj = RegNames[((code >> 5) & 0x1f) + 32];
    const char* fk = RegNames[((code >> 10) & 0x1f) + 32];
    const char* fa = RegNames[((code >> 15) & 0x1f) + 32];

#ifdef FEATURE_SIMD
    // LSX registers
    const char* vd = vRegNames[code & 0x1f];
    const char* vj = vRegNames[(code >> 5) & 0x1f];
    const char* vk = vRegNames[(code >> 10) & 0x1f];
    const char* va = vRegNames[(code >> 15) & 0x1f];
    // LASX registers
    const char* xd = xRegNames[code & 0x1f];
    const char* xj = xRegNames[(code >> 5) & 0x1f];
    const char* xk = xRegNames[(code >> 10) & 0x1f];
    const char* xa = xRegNames[(code >> 15) & 0x1f];
#endif

    unsigned int opcode = (code >> 26) & 0x3f;
    // bits: 31-26,MSB6
    switch (opcode)
    {
        case 0x0:
            goto Label_OPCODE_0;
        case 0x2:
            goto Label_OPCODE_2;
        case 0x3:
            goto Label_OPCODE_3;
        case 0xe:
            goto Label_OPCODE_E;
#ifdef FEATURE_SIMD
        case 0xb:
            goto Label_OPCODE_B;
        case 0xc:
            goto Label_OPCODE_C;
        case 0x1c:
            goto Label_OPCODE_1C;
        case 0x1d:
            goto Label_OPCODE_1D;
#endif
        case LA_2RI16_ADDU16I_D: // 0x4
        {
            short si16 = (code >> 10) & 0xffff;
            printf("addu16i.d    %s, %s, %d\n", rd, rj, si16);
            return;
        }
        case 0x5:
        case 0x6:
        case 0x7:
        {
            // bits: 31-25,MSB7
            unsigned int inscode = (code >> 25) & 0x7f;
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
                    printf("pcaddu18i    %s, 0x%x\n", rd, si20);
                    return;
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
            int offs21 = (((code >> 10) & 0xffff) | ((code & 0x1f) << 16)) << 11;
            offs21 >>= 9;
            printf("beqz         %s, 0x%llx\n", rj, (int64_t)insAdr + offs21);
            return;
        }
        case LA_1RI21_BNEZ: // 0x11
        {
            int offs21 = (((code >> 10) & 0xffff) | ((code & 0x1f) << 16)) << 11;
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
            }
            else if (1 == ((code >> 8) & 0x3))
            {
                printf("bcnez        %s, 0x%llx\n", cj, (int64_t)insAdr + offs21);
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
            }
            return;
        }
        case LA_2RI16_JIRL: // 0x13
        {
            int offs16 = (short)((code >> 10) & 0xffff);
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
            printf("bl           0x%llx", (int64_t)insAdr + offs26);
            if (id->idDebugOnlyInfo()->idMemCookie)
            {
                assert(0 < id->idDebugOnlyInfo()->idMemCookie);
                const char* methodName;
                methodName = emitComp->eeGetMethodFullName((CORINFO_METHOD_HANDLE)id->idDebugOnlyInfo()->idMemCookie);
                printf("  # %s\n", methodName);
            }
            else
            {
                printf("\n");
            }
            return;
        }
        case LA_2RI16_BEQ: // 0x16
        {
            int offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("beq          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BNE: // 0x17
        {
            int offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bne          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BLT: // 0x18
        {
            int offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("blt          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BGE: // 0x19
        {
            int offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bge          %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BLTU: // 0x1a
        {
            int offs16 = (short)((code >> 10) & 0xffff);
            offs16 <<= 2;
            printf("bltu         %s, %s, 0x%llx\n", rj, rd, (int64_t)insAdr + offs16);
            return;
        }
        case LA_2RI16_BGEU: // 0x1b
        {
            int offs16 = (short)((code >> 10) & 0xffff);
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
                            printf("asrtle.d     %s, %s\n", rj, rk);
                            return;
                        }
                        case LA_2R_ASRTGT_D:
                        {
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
                    unsigned int sa2 = (code >> 15) & 0x3;
                    if (0 == ((code >> 17) & 0x1))
                    {
                        printf("alsl.w       %s, %s, %s, %d\n", rd, rj, rk, (sa2 + 1));
                    }
                    else if (1 == ((code >> 17) & 0x1))
                    {
                        printf("alsl.wu      %s, %s, %s, %d\n", rd, rj, rk, (sa2 + 1));
                    }
                    else
                    {
                        printf("LOONGARCH illegal instruction: %08X\n", code);
                    }
                    return;
                }
                case LA_OP_BYTEPICK_W: // 0x2
                {
                    unsigned int sa2 = (code >> 15) & 0x3;
                    printf("bytepick.w   %s, %s, %s, %d\n", rd, rj, rk, sa2);
                    return;
                }
                case LA_OP_BYTEPICK_D: // 0x3
                {
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
                unsigned int lsbw = (code >> 10) & 0x1f;
                unsigned int msbw = (code >> 16) & 0x1f;
                if (!(code & 0x8000))
                {
                    printf("bstrins.w    %s, %s, %d, %d\n", rd, rj, msbw, lsbw);
                }
                else if (code & 0x8000)
                {
                    printf("bstrpick.w   %s, %s, %d, %d\n", rd, rj, msbw, lsbw);
                }
                else
                {
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                }
                return;
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
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("slli.w       %s, %s, %d\n", rd, rj, ui5);
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("slli.d       %s, %s, %d\n", rd, rj, ui6);
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                        }
                        return;
                    }
                    case 0x11:
                    {
                        // LA_OP_SRLI_W:
                        // LA_OP_SRLI_D:
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("srli.w       %s, %s, %d\n", rd, rj, ui5);
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("srli.d      %s, %s, %d\n", rd, rj, ui6);
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                        }
                        return;
                    }
                    case 0x12:
                    {
                        // LA_OP_SRAI_W:
                        // LA_OP_SRAI_D:
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("srai.w       %s, %s, %d\n", rd, rj, ui5);
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("srai.d       %s, %s, %d\n", rd, rj, ui6);
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
                        }
                        return;
                    }
                    case 0x13:
                    {
                        // LA_OP_ROTRI_W:
                        // LA_OP_ROTRI_D:
                        if (1 == ((code >> 15) & 0x7))
                        {
                            unsigned int ui5 = (code >> 10) & 0x1f;
                            printf("rotri.w      %s, %s, %d\n", rd, rj, ui5);
                        }
                        else if (1 == ((code >> 16) & 0x3))
                        {
                            unsigned int ui6 = (code >> 10) & 0x3f;
                            printf("rotri.d      %s, %s, %d\n", rd, rj, ui6);
                        }
                        else
                        {
                            printf("LOONGARCH illegal instruction: %08X\n", code);
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
            unsigned int lsbd = (code >> 10) & 0x3f;
            unsigned int msbd = (code >> 16) & 0x3f;
            printf("bstrins.d    %s, %s, %d, %d\n", rd, rj, msbd, lsbd);
            return;
        }
        case LA_OP_BSTRPICK_D:
        {
            unsigned int lsbd = (code >> 10) & 0x3f;
            unsigned int msbd = (code >> 16) & 0x3f;
            printf("bstrpick.d   %s, %s, %d, %d\n", rd, rj, msbd, lsbd);
            return;
        }
        case 0x4:
        {
            // bits: 31-15,MSB17
            unsigned int inscode1 = (code >> 15) & 0x1ffff;

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
                        {
                            const char* fcsr = FcsrName[code & 0x1f];
                            printf("movgr2fcsr   %s, %s\n", fcsr, rj);
                            return;
                        }
                        case LA_2R_MOVFCSR2GR:
                        {
                            const char* fcsr = FcsrName[(code >> 5) & 0x1f];
                            printf("movfcsr2gr   %s, %s\n", rd, fcsr);
                            return;
                        }
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
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("slti         %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_SLTUI: // 0x9
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("sltui        %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_ADDI_W: // 0xa
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("addi.w       %s, %s, %d\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_ADDI_D: // 0xb
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("addi.d       %s, %s, %ld\n", rd, rj, si12);
            return;
        }
        case LA_2RI12_LU52I_D: // 0xc
        {
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
                unsigned int ui12 = ((code >> 10) & 0xfff);
                printf("andi         %s, %s, 0x%x\n", rd, rj, ui12);
            }
            return;
        }
        case LA_2RI12_ORI: // 0xe
        {
            unsigned int ui12 = ((code >> 10) & 0xfff);
            printf("ori          %s, %s, 0x%x\n", rd, rj, ui12);
            return;
        }
        case LA_2RI12_XORI: // 0xf
        {
            unsigned int ui12 = ((code >> 10) & 0xfff);
            printf("xori         %s, %s, 0x%x\n", rd, rj, ui12);
            return;
        }

        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_2:
    opcode = (code >> 20) & 0xfff;

    // bits: 31-20,MSB12
    switch (opcode)
    {
        case LA_4R_FMADD_S:
            printf("fmadd.s      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FMADD_D:
            printf("fmadd.d      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FMSUB_S:
            printf("fmsub.s      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FMSUB_D:
            printf("fmsub.d      %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FNMADD_S:
            printf("fnmadd.s     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FNMADD_D:
            printf("fnmadd.d     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FNMSUB_S:
            printf("fnmsub.s     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;
        case LA_4R_FNMSUB_D:
            printf("fnmsub.d     %s, %s, %s, %s\n", fd, fj, fk, fa);
            return;

#ifdef FEATURE_SIMD
        case 0x91:
            printf("vfmadd.s     %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x92:
            printf("vfmadd.d     %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x95:
            printf("vfmsub.s     %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x96:
            printf("vfmsub.d     %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x99:
            printf("vfnmadd.s    %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x9a:
            printf("vfnmadd.d    %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x9d:
            printf("vfnmsub.s    %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0x9e:
            printf("vfnmsub.d    %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0xa1:
            printf("xvfmadd.s    %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xa2:
            printf("xcfmadd.d    %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xa5:
            printf("xvfmsub.s    %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xa6:
            printf("xvfmsub.d    %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xa9:
            printf("xvfnmadd.s   %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xaa:
            printf("xvfnmadd.d   %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xad:
            printf("xvfnmsub.s   %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xae:
            printf("xvfnmsub.d   %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
#endif
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
            const char* ca = CFregName[(code >> 15) & 0x7];
            printf("fsel         %s, %s, %s, %s\n", fd, fj, fk, ca);
            return;
        }
#ifdef FEATURE_SIMD
        case 0xd1:
            printf("vbitsel.v    %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0xd5:
            printf("vshuf.b      %s, %s, %s, %s\n", vd, vj, vk, va);
            return;
        case 0xc5:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            switch (cond)
            {
                case 0x0:
                    printf("vfcmp.caf.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x1:
                    printf("vfcmp.saf.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x2:
                    printf("vfcmp.clt.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x3:
                    printf("vfcmp.slt.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x4:
                    printf("vfcmp.ceq.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x5:
                    printf("vfcmp.seq.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x6:
                    printf("vfcmp.cle.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x7:
                    printf("vfcmp.sle.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x8:
                    printf("vfcmp.cun.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x9:
                    printf("vfcmp.sun.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xA:
                    printf("vfcmp.cult.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xB:
                    printf("vfcmp.sult.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xC:
                    printf("vfcmp.cueq.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xD:
                    printf("vfcmp.sueq.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xE:
                    printf("vfcmp.cule.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xF:
                    printf("vfcmp.sule.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x10:
                    printf("vfcmp.cne.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x11:
                    printf("vfcmp.sne.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x14:
                    printf("vfcmp.cor.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x15:
                    printf("vfcmp.sor.s  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x18:
                    printf("vfcmp.cune.s %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x19:
                    printf("vfcmp.sune.s %s, %s, %s\n", vd, vj, vk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
        case 0xc6:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            switch (cond)
            {
                case 0x0:
                    printf("vfcmp.caf.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x1:
                    printf("vfcmp.saf.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x2:
                    printf("vfcmp.clt.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x3:
                    printf("vfcmp.slt.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x4:
                    printf("vfcmp.ceq.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x5:
                    printf("vfcmp.seq.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x6:
                    printf("vfcmp.cle.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x7:
                    printf("vfcmp.sle.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x8:
                    printf("vfcmp.cun.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x9:
                    printf("vfcmp.sun.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xA:
                    printf("vfcmp.cult.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xB:
                    printf("vfcmp.sult.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xC:
                    printf("vfcmp.cueq.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xD:
                    printf("vfcmp.sueq.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xE:
                    printf("vfcmp.cule.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0xF:
                    printf("vfcmp.sule.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x10:
                    printf("vfcmp.cne.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x11:
                    printf("vfcmp.sne.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x14:
                    printf("vfcmp.cor.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x15:
                    printf("vfcmp.sor.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x18:
                    printf("vfcmp.cune.d %s, %s, %s\n", vd, vj, vk);
                    return;
                case 0x19:
                    printf("vfcmp.sune.d %s, %s, %s\n", vd, vj, vk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }

        case 0xd2:
            printf("xvbitsel.v   %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xd6:
            printf("xvshuf.b     %s, %s, %s, %s\n", xd, xj, xk, xa);
            return;
        case 0xc9:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            switch (cond)
            {
                case 0x0:
                    printf("xvfcmp.caf.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x1:
                    printf("xvfcmp.saf.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x2:
                    printf("xvfcmp.clt.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x3:
                    printf("xvfcmp.slt.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x4:
                    printf("xvfcmp.ceq.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x5:
                    printf("xvfcmp.seq.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x6:
                    printf("xvfcmp.cle.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x7:
                    printf("xvfcmp.sle.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x8:
                    printf("xvfcmp.cun.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x9:
                    printf("xvfcmp.sun.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xA:
                    printf("xvfcmp.cult.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xB:
                    printf("xvfcmp.sult.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xC:
                    printf("xvfcmp.cueq.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xD:
                    printf("xvfcmp.sueq.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xE:
                    printf("xvfcmp.cule.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xF:
                    printf("xvfcmp.sule.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x10:
                    printf("xvfcmp.cne.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x11:
                    printf("xvfcmp.sne.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x14:
                    printf("xvfcmp.cor.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x15:
                    printf("xvfcmp.sor.s %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x18:
                    printf("xvfcmp.cune.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x19:
                    printf("xvfcmp.sune.s  %s, %s, %s\n", xd, xj, xk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
        case 0xca:
        {
            // bits:19-15,cond
            unsigned int cond = (code >> 15) & 0x1f;
            switch (cond)
            {
                case 0x0:
                    printf("xvfcmp.caf.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x1:
                    printf("xvfcmp.saf.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x2:
                    printf("xvfcmp.clt.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x3:
                    printf("xvfcmp.slt.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x4:
                    printf("xvfcmp.ceq.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x5:
                    printf("xvfcmp.seq.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x6:
                    printf("xvfcmp.cle.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x7:
                    printf("xvfcmp.sle.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x8:
                    printf("xvfcmp.cun.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x9:
                    printf("xvfcmp.sun.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xA:
                    printf("xvfcmp.cult.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xB:
                    printf("xvfcmp.sult.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xC:
                    printf("xvfcmp.cueq.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xD:
                    printf("xvfcmp.sueq.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xE:
                    printf("xvfcmp.cule.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0xF:
                    printf("xvfcmp.sule.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x10:
                    printf("xvfcmp.cne.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x11:
                    printf("xvfcmp.sne.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x14:
                    printf("xvfcmp.cor.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x15:
                    printf("xvfcmp.sor.d %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x18:
                    printf("xvfcmp.cune.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                case 0x19:
                    printf("xvfcmp.sune.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
#endif
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
            printf("ldx.b        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_H:
            printf("ldx.h        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_W:
            printf("ldx.w        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_D:
            printf("ldx.d        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STX_B:
            printf("stx.b        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STX_H:
            printf("stx.h        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STX_W:
            printf("stx.w        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STX_D:
            printf("stx.d        %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_BU:
            printf("ldx.bu       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_HU:
            printf("ldx.hu       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDX_WU:
            printf("ldx.wu       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_PRELDX:
            NYI_LOONGARCH64("unused instr LA_3R_PRELDX");
            return;
        case LA_3R_FLDX_S:
            printf("fldx.s       %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FLDX_D:
            printf("fldx.d       %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTX_S:
            printf("fstx.s       %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTX_D:
            printf("fstx.d       %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_AMSWAP_W:
            printf("amswap.w     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMSWAP_D:
            printf("amswap.d     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMADD_W:
            printf("amadd.w      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMADD_D:
            printf("amadd.d      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMAND_W:
            printf("amand.w      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMAND_D:
            printf("amand.d      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMOR_W:
            printf("amor.w       %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMOR_D:
            printf("amor.d       %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMXOR_W:
            printf("amxor.w      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMXOR_D:
            printf("amxor.d      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_W:
            printf("ammax.w      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_D:
            printf("ammax.d      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_W:
            printf("ammin.w      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_D:
            printf("ammin.d      %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_WU:
            printf("ammax.wu     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_DU:
            printf("ammax.du     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_WU:
            printf("ammin.wu     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_DU:
            printf("ammin.du     %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMSWAP_DB_W:
            printf("amswap_db.w  %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMSWAP_DB_D:
            printf("amswap_db.d  %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMADD_DB_W:
            printf("amadd_db.w   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMADD_DB_D:
            printf("amadd_db.d   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMAND_DB_W:
            printf("amand_db.w   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMAND_DB_D:
            printf("amand_db.d   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMOR_DB_W:
            printf("amor_db.w    %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMOR_DB_D:
            printf("amor_db.d    %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMXOR_DB_W:
            printf("amxor_db.w   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMXOR_DB_D:
            printf("amxor_db.d   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_DB_W:
            printf("ammax_db.w   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_DB_D:
            printf("ammax_db.d   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_DB_W:
            printf("ammin_db.w   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_DB_D:
            printf("ammin_db.d   %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_DB_WU:
            printf("ammax_db.wu  %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMAX_DB_DU:
            printf("ammax_db.du  %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_DB_WU:
            printf("ammin_db.wu  %s, %s, %s\n", rd, rk, rj);
            return;
        case LA_3R_AMMIN_DB_DU:
            printf("ammin_db.du  %s, %s, %s\n", rd, rk, rj);
            return;
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
            printf("fldgt.s      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FLDGT_D:
            printf("fldgt.d      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FLDLE_S:
            printf("fldle.s      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FLDLE_D:
            printf("fldle.d      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTGT_S:
            printf("fstgt.s      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTGT_D:
            printf("fstgt.d      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTLE_S:
            printf("fstle.s      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_FSTLE_D:
            printf("fstle.d      %s, %s, %s\n", fd, rj, rk);
            return;
        case LA_3R_LDGT_B:
            printf("ldgt.b       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDGT_H:
            printf("ldgt.h       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDGT_W:
            printf("ldgt.w       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDGT_D:
            printf("ldgt.d       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDLE_B:
            printf("ldle.b       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDLE_H:
            printf("ldle.h       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDLE_W:
            printf("ldle.w       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_LDLE_D:
            printf("ldle.d       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STGT_B:
            printf("stgt.b       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STGT_H:
            printf("stgt.h       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STGT_W:
            printf("stgt.w       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STGT_D:
            printf("stgt.d       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STLE_B:
            printf("stle.b       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STLE_H:
            printf("stle.h       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STLE_W:
            printf("stle.w       %s, %s, %s\n", rd, rj, rk);
            return;
        case LA_3R_STLE_D:
            printf("stle.d       %s, %s, %s\n", rd, rj, rk);
            return;

#ifdef FEATURE_SIMD
        case 0x7080:
            printf("vldx         %s, %s, %s\n", vd, rj, rk);
            return;
        case 0x7088:
            printf("vstx         %s, %s, %s\n", vd, rj, rk);
            return;
        case 0x7090:
            printf("xvldx        %s, %s, %s\n", xd, rj, rk);
            return;
        case 0x7098:
            printf("xvstx        %s, %s, %s\n", xd, rj, rk);
            return;
#endif

        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

#ifdef FEATURE_SIMD
Label_OPCODE_B:
    opcode = (code >> 22) & 0x3ff;

    // bits: 31-22,MSB10
    switch (opcode)
    {
        case 0xb0:
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("vld          %s, %s, %d\n", vd, rj, si12);
            return;
        }
        case 0xb1:
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("vst          %s, %s, %d\n", vd, rj, si12);
            return;
        }
        case 0xb2:
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("xvld         %s, %s, %d\n", xd, rj, si12);
            return;
        }
        case 0xb3:
        {
            short si12 = ((code >> 10) & 0xfff) << 4;
            si12 >>= 4;
            printf("xvst         %s, %s, %d\n", xd, rj, si12);
            return;
        }

        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_C:
    opcode = code >> 23;
    switch (opcode)
    {
        case 0x60:
        {
            opcode = code >> 19;
            if ((opcode & 0xf) == 0x2)
            {
                short si9 = ((code >> 10) & 0x1ff) << 7;
                si9 >>= 7;
                assert((-256 <= si9) && (si9 <= 255));
                printf("vldrepl.d    %s, %s, %d\n", vd, rj, si9);
                return;
            }
            else if ((opcode & 0xe) == 0x4)
            {
                short si10 = ((code >> 10) & 0x3ff) << 6;
                si10 >>= 6;
                assert((-512 <= si10) && (si10 <= 511));
                printf("vldrepl.w    %s, %s, %d\n", vd, rj, si10);
                return;
            }
            else if ((opcode & 0xc) == 0x8)
            {
                short si11 = ((code >> 10) & 0x7ff) << 5;
                si11 >>= 5;
                assert((-1024 <= si11) && (si11 <= 1023));
                printf("vldrepl.h    %s, %s, %d\n", vd, rj, si11);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x61:
        {
            opcode = code >> 22;
            if ((opcode & 0x1) == 0x0)
            {
                short si12 = ((code >> 10) & 0xfff) << 4;
                si12 >>= 4;
                assert((-2048 <= si12) && (si12 <= 2047));
                printf("vldrepl.b    %s, %s, %d\n", vd, rj, si12);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x62:
        {
            opcode = code >> 19;
            if ((opcode & 0xf) == 0x2)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0x1;
                printf("vstelm.d     %s, %s, %d, %d\n", vd, rj, si8 << 3, idx);
                return;
            }
            else if ((opcode & 0xe) == 0x4)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0x3;
                printf("vstelm.w     %s, %s, %d, %d\n", vd, rj, si8 << 2, idx);
                return;
            }
            else if ((opcode & 0xc) == 0x8)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0x7;
                printf("vstelm.h     %s, %s, %d, %d\n", vd, rj, si8 << 1, idx);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x63:
        {
            opcode = code >> 22;
            if ((opcode & 0x1) == 0x0)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0xf;
                printf("vstelm.b     %s, %s, %d, %d\n", vd, rj, si8, idx);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x64:
        {
            opcode = code >> 19;
            if ((opcode & 0xf) == 0x2)
            {
                short si9 = ((code >> 10) & 0x1ff) << 7;
                si9 >>= 7;
                assert((-256 <= si9) && (si9 <= 255));
                printf("xvldrepl.d   %s, %s, %d\n", xd, rj, si9);
                return;
            }
            else if ((opcode & 0xe) == 0x4)
            {
                short si10 = ((code >> 10) & 0x3ff) << 6;
                si10 >>= 6;
                assert((-512 <= si10) && (si10 <= 511));
                printf("xvldrepl.w   %s, %s, %d\n", xd, rj, si10);
                return;
            }
            else if ((opcode & 0xc) == 0x8)
            {
                short si11 = ((code >> 10) & 0x7ff) << 5;
                si11 >>= 5;
                assert((-1024 <= si11) && (si11 <= 1023));
                printf("xvldrepl.h   %s, %s, %d\n", xd, rj, si11);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x65:
        {
            opcode = code >> 22;
            if ((opcode & 0x1) == 0x0)
            {
                short si12 = ((code >> 10) & 0xfff) << 4;
                si12 >>= 4;
                assert((-2048 <= si12) && (si12 <= 2047));
                printf("xvldrepl.b   %s, %s, %d\n", xd, rj, si12);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x66:
        {
            opcode = code >> 20;
            if ((opcode & 0x7) == 0x1)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0x3;
                printf("xvstelm.d    %s, %s, %d, %d\n", xd, rj, si8 << 3, idx);
                return;
            }
            else if ((opcode & 0x6) == 0x2)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0x7;
                printf("xvstelm.w    %s, %s, %d, %d\n", xd, rj, si8 << 2, idx);
                return;
            }
            else if ((opcode & 0x4) == 0x4)
            {
                char           si8 = (code >> 10) & 0xff;
                unsigned short idx = (code >> 18) & 0xf;
                printf("xvstelm.h    %s, %s, %d, %d\n", xd, rj, si8 << 1, idx);
                return;
            }
            else
            {
                printf("LOONGARCH illegal instruction: %08X\n", code);
                return;
            }
        }
        case 0x67:
        {
            char           si8 = (code >> 10) & 0xff;
            unsigned short idx = (code >> 18) & 0x1f;
            printf("xvstelm.b    %s, %s, %d, %d\n", xd, rj, si8, idx);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }

Label_OPCODE_1C:
    opcode = (code >> 18) & 0xff; // MSB14
    switch (opcode)
    {
        case 0 ... 0xa6:
            opcode = (code >> 15) & 0x7ff;
            switch (opcode)
            {
                case 0x0:
                {
                    printf("vseq.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1:
                {
                    printf("vseq.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x2:
                {
                    printf("vseq.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x3:
                {
                    printf("vseq.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x4:
                {
                    printf("vsle.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x5:
                {
                    printf("vsle.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x6:
                {
                    printf("vsle.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x7:
                {
                    printf("vsle.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x8:
                {
                    printf("vsle.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x9:
                {
                    printf("vsle.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xa:
                {
                    printf("vsle.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb:
                {
                    printf("vsle.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc:
                {
                    printf("vslt.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd:
                {
                    printf("vslt.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe:
                {
                    printf("vslt.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xf:
                {
                    printf("vslt.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10:
                {
                    printf("vslt.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x11:
                {
                    printf("vslt.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x12:
                {
                    printf("vslt.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x13:
                {
                    printf("vslt.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x14:
                {
                    printf("vadd.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15:
                {
                    printf("vadd.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16:
                {
                    printf("vadd.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17:
                {
                    printf("vadd.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x18:
                {
                    printf("vsub.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x19:
                {
                    printf("vsub.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1a:
                {
                    printf("vsub.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1b:
                {
                    printf("vsub.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x3c:
                {
                    printf("vaddwev.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x3d:
                {
                    printf("vaddwev.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x3e:
                {
                    printf("vaddwev.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x3f:
                {
                    printf("vaddwev.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x40:
                {
                    printf("vsubwev.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x41:
                {
                    printf("vsubwev.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x42:
                {
                    printf("vsubwev.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x43:
                {
                    printf("vsubwev.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x44:
                {
                    printf("vaddwod.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x45:
                {
                    printf("vaddwod.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x46:
                {
                    printf("vaddwod.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x47:
                {
                    printf("vaddwod.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x48:
                {
                    printf("vsubwod.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x49:
                {
                    printf("vsubwod.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x4a:
                {
                    printf("vsubwod.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x4b:
                {
                    printf("vsubwod.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x5c:
                {
                    printf("vaddwev.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x5d:
                {
                    printf("vaddwev.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x5e:
                {
                    printf("vaddwev.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x5f:
                {
                    printf("vaddwev.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x60:
                {
                    printf("vsubwev.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x61:
                {
                    printf("vsubwev.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x62:
                {
                    printf("vsubwev.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x63:
                {
                    printf("vsubwev.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x64:
                {
                    printf("vaddwod.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x65:
                {
                    printf("vaddwod.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x66:
                {
                    printf("vaddwod.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x67:
                {
                    printf("vaddwod.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x68:
                {
                    printf("vsubwod.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x69:
                {
                    printf("vsubwod.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x6a:
                {
                    printf("vsubwod.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x6b:
                {
                    printf("vsubwod.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x7c:
                {
                    printf("vaddwev.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x7d:
                {
                    printf("vaddwev.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x7e:
                {
                    printf("vaddwev.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x7f:
                {
                    printf("vaddwev.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x80:
                {
                    printf("vaddwod.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x81:
                {
                    printf("vaddwod.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x82:
                {
                    printf("vaddwod.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x83:
                {
                    printf("vaddwod.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x8c:
                {
                    printf("vsadd.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x8d:
                {
                    printf("vsadd.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x8e:
                {
                    printf("vsadd.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x8f:
                {
                    printf("vsadd.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x90:
                {
                    printf("vssub.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x91:
                {
                    printf("vssub.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x92:
                {
                    printf("vssub.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x93:
                {
                    printf("vssub.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x94:
                {
                    printf("vsadd.bu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x95:
                {
                    printf("vsadd.hu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x96:
                {
                    printf("vsadd.wu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x97:
                {
                    printf("vsadd.du     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x98:
                {
                    printf("vssub.bu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x99:
                {
                    printf("vssub.hu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x9a:
                {
                    printf("vssub.wu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x9b:
                {
                    printf("vssub.du     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xa8:
                {
                    printf("vhaddw.h.b   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xa9:
                {
                    printf("vhaddw.w.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xaa:
                {
                    printf("vhaddw.d.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xab:
                {
                    printf("vhaddw.q.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xac:
                {
                    printf("vhsubw.h.b   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xad:
                {
                    printf("vhsubw.w.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xae:
                {
                    printf("vhsubw.d.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xaf:
                {
                    printf("vhsubw.q.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb0:
                {
                    printf("vhaddw.hu.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb1:
                {
                    printf("vhaddw.wu.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb2:
                {
                    printf("vhaddw.du.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb3:
                {
                    printf("vhaddw.qu.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb4:
                {
                    printf("vhsubw.hu.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb5:
                {
                    printf("vhsubw.wu.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb6:
                {
                    printf("vhsubw.du.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb7:
                {
                    printf("vhsubw.qu.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb8:
                {
                    printf("vadda.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xb9:
                {
                    printf("vadda.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xba:
                {
                    printf("vadda.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xbb:
                {
                    printf("vadda.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc0:
                {
                    printf("vabsd.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc1:
                {
                    printf("vabsd.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc2:
                {
                    printf("vabsd.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc3:
                {
                    printf("vabsd.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc4:
                {
                    printf("vabsd.bu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc5:
                {
                    printf("vabsd.hu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc6:
                {
                    printf("vabsd.wu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc7:
                {
                    printf("vabsd.du     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc8:
                {
                    printf("vavg.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xc9:
                {
                    printf("vavg.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xca:
                {
                    printf("vavg.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xcb:
                {
                    printf("vavg.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xcc:
                {
                    printf("vavg.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xcd:
                {
                    printf("vavg.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xce:
                {
                    printf("vavg.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xcf:
                {
                    printf("vavg.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd0:
                {
                    printf("vavgr.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd1:
                {
                    printf("vavgr.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd2:
                {
                    printf("vavgr.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd3:
                {
                    printf("vavgr.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd4:
                {
                    printf("vavgr.bu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd5:
                {
                    printf("vavgr.hu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd6:
                {
                    printf("vavgr.wu     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xd7:
                {
                    printf("vavgr.du     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe0:
                {
                    printf("vmax.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe1:
                {
                    printf("vmax.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe2:
                {
                    printf("vmax.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe3:
                {
                    printf("vmax.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe4:
                {
                    printf("vmin.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe5:
                {
                    printf("vmin.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe6:
                {
                    printf("vmin.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe7:
                {
                    printf("vmin.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe8:
                {
                    printf("vmax.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xe9:
                {
                    printf("vmax.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xea:
                {
                    printf("vmax.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xeb:
                {
                    printf("vmax.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xec:
                {
                    printf("vmin.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xed:
                {
                    printf("vmin.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xee:
                {
                    printf("vmin.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0xef:
                {
                    printf("vmin.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x108:
                {
                    printf("vmul.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x109:
                {
                    printf("vmul.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10a:
                {
                    printf("vmul.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10b:
                {
                    printf("vmul.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10c:
                {
                    printf("vmuh.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10d:
                {
                    printf("vmuh.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10e:
                {
                    printf("vmuh.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x10f:
                {
                    printf("vmuh.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x110:
                {
                    printf("vmuh.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x111:
                {
                    printf("vmuh.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x112:
                {
                    printf("vmuh.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x113:
                {
                    printf("vmuh.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x120:
                {
                    printf("vmulwev.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x121:
                {
                    printf("vmulwev.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x122:
                {
                    printf("vmulwev.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x123:
                {
                    printf("vmulwev.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x124:
                {
                    printf("vmulwod.h.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x125:
                {
                    printf("vmulwod.w.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x126:
                {
                    printf("vmulwod.d.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x127:
                {
                    printf("vmulwod.q.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x130:
                {
                    printf("vmulwev.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x131:
                {
                    printf("vmulwev.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x132:
                {
                    printf("vmulwev.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x133:
                {
                    printf("vmulwev.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x134:
                {
                    printf("vmulwod.h.bu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x135:
                {
                    printf("vmulwod.w.hu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x136:
                {
                    printf("vmulwod.d.wu %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x137:
                {
                    printf("vmulwod.q.du %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x140:
                {
                    printf("vmulwev.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x141:
                {
                    printf("vmulwev.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x142:
                {
                    printf("vmulwev.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x143:
                {
                    printf("vmulwev.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x144:
                {
                    printf("vmulwod.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x145:
                {
                    printf("vmulwod.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x146:
                {
                    printf("vmulwod.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x147:
                {
                    printf("vmulwod.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x150:
                {
                    printf("vmadd.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x151:
                {
                    printf("vmadd.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x152:
                {
                    printf("vmadd.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x153:
                {
                    printf("vmadd.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x154:
                {
                    printf("vmsub.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x155:
                {
                    printf("vmsub.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x156:
                {
                    printf("vmsub.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x157:
                {
                    printf("vmsub.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x158:
                {
                    printf("vmaddwev.h.b %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x159:
                {
                    printf("vmaddwev.w.h %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15a:
                {
                    printf("vmaddwev.d.w %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15b:
                {
                    printf("vmaddwev.q.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15c:
                {
                    printf("vmaddwod.h.b %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15d:
                {
                    printf("vmaddwod.w.h %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15e:
                {
                    printf("vmaddwod.d.w %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x15f:
                {
                    printf("vmaddwod.q.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x168:
                {
                    printf("vmaddwev.h.bu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x169:
                {
                    printf("vmaddwev.w.hu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16a:
                {
                    printf("vmaddwev.d.wu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16b:
                {
                    printf("vmaddwev.q.du  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16c:
                {
                    printf("vmaddwod.h.bu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16d:
                {
                    printf("vmaddwod.w.hu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16e:
                {
                    printf("vmaddwod.d.wu  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x16f:
                {
                    printf("vmaddwod.q.du  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x178:
                {
                    printf("vmaddwev.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x179:
                {
                    printf("vmaddwev.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17a:
                {
                    printf("vmaddwev.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17b:
                {
                    printf("vmaddwev.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17c:
                {
                    printf("vmaddwod.h.bu.b  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17d:
                {
                    printf("vmaddwod.w.hu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17e:
                {
                    printf("vmaddwod.d.wu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x17f:
                {
                    printf("vmaddwod.q.du.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c0:
                {
                    printf("vdiv.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c1:
                {
                    printf("vdiv.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c2:
                {
                    printf("vdiv.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c3:
                {
                    printf("vdiv.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c4:
                {
                    printf("vmod.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c5:
                {
                    printf("vmod.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c6:
                {
                    printf("vmod.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c7:
                {
                    printf("vmod.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c8:
                {
                    printf("vdiv.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1c9:
                {
                    printf("vdiv.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ca:
                {
                    printf("vdiv.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1cb:
                {
                    printf("vdiv.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1cc:
                {
                    printf("vmod.bu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1cd:
                {
                    printf("vmod.hu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ce:
                {
                    printf("vmod.wu      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1cf:
                {
                    printf("vmod.du      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d0:
                {
                    printf("vsll.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d1:
                {
                    printf("vsll.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d2:
                {
                    printf("vsll.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d3:
                {
                    printf("vsll.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d4:
                {
                    printf("vsrl.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d5:
                {
                    printf("vsrl.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d6:
                {
                    printf("vsrl.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d7:
                {
                    printf("vsrl.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d8:
                {
                    printf("vsra.b       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1d9:
                {
                    printf("vsra.h       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1da:
                {
                    printf("vsra.w       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1db:
                {
                    printf("vsra.d       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1dc:
                {
                    printf("vrotr.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1dd:
                {
                    printf("vrotr.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1de:
                {
                    printf("vrotr.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1df:
                {
                    printf("vrotr.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e0:
                {
                    printf("vsrlr.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e1:
                {
                    printf("vsrlr.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e2:
                {
                    printf("vsrlr.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e3:
                {
                    printf("vsrlr.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e4:
                {
                    printf("vsrar.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e5:
                {
                    printf("vsrar.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e6:
                {
                    printf("vsrar.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e7:
                {
                    printf("vsrar.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1e9:
                {
                    printf("vsrln.b.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ea:
                {
                    printf("vsrln.h.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1eb:
                {
                    printf("vsrln.w.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ed:
                {
                    printf("vsran.b.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ee:
                {
                    printf("vsran.h.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ef:
                {
                    printf("vsran.w.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f1:
                {
                    printf("vsrlrn.b.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f2:
                {
                    printf("vsrlrn.h.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f3:
                {
                    printf("vsrlrn.w.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f5:
                {
                    printf("vsrarn.b.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f6:
                {
                    printf("vsrarn.h.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f7:
                {
                    printf("vsrarn.w.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1f9:
                {
                    printf("vssrln.b.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1fa:
                {
                    printf("vssrln.h.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1fb:
                {
                    printf("vssrln.w.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1fd:
                {
                    printf("vssran.b.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1fe:
                {
                    printf("vssran.h.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x1ff:
                {
                    printf("vssran.w.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x201:
                {
                    printf("vssrlrn.b.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x202:
                {
                    printf("vssrlrn.h.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x203:
                {
                    printf("vssrlrn.w.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x205:
                {
                    printf("vssrarn.b.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x206:
                {
                    printf("vssrarn.h.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x207:
                {
                    printf("vssrarn.w.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x209:
                {
                    printf("vssrln.bu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x20a:
                {
                    printf("vssrln.hu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x20b:
                {
                    printf("vssrln.wu.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x20d:
                {
                    printf("vssran.bu.h  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x20e:
                {
                    printf("vssran.hu.w  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x20f:
                {
                    printf("vssran.wu.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x211:
                {
                    printf("vssrlrn.bu.h %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x212:
                {
                    printf("vssrlrn.hu.w %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x213:
                {
                    printf("vssrlrn.wu.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x215:
                {
                    printf("vssrarn.bu.h %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x216:
                {
                    printf("vssrarn.hu.w %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x217:
                {
                    printf("vssrarn.wu.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x218:
                {
                    printf("vbitclr.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x219:
                {
                    printf("vbitclr.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21a:
                {
                    printf("vbitclr.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21b:
                {
                    printf("vbitclr.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21c:
                {
                    printf("vbitset.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21d:
                {
                    printf("vbitset.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21e:
                {
                    printf("vbitset.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x21f:
                {
                    printf("vbitset.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x220:
                {
                    printf("vbitrev.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x221:
                {
                    printf("vbitrev.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x222:
                {
                    printf("vbitrev.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x223:
                {
                    printf("vbitrev.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x22c:
                {
                    printf("vpackev.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x22d:
                {
                    printf("vpackev.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x22e:
                {
                    printf("vpackev.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x22f:
                {
                    printf("vpackev.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x230:
                {
                    printf("vpackod.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x231:
                {
                    printf("vpackod.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x232:
                {
                    printf("vpackod.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x233:
                {
                    printf("vpackod.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x234:
                {
                    printf("vilvl.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x235:
                {
                    printf("vilvl.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x236:
                {
                    printf("vilvl.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x237:
                {
                    printf("vilvl.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x238:
                {
                    printf("vilvh.b      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x239:
                {
                    printf("vilvh.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23a:
                {
                    printf("vilvh.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23b:
                {
                    printf("vilvh.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23c:
                {
                    printf("vpickev.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23d:
                {
                    printf("vpickev.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23e:
                {
                    printf("vpickev.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x23f:
                {
                    printf("vpickev.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x240:
                {
                    printf("vpickod.b    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x241:
                {
                    printf("vpickod.h    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x242:
                {
                    printf("vpickod.w    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x243:
                {
                    printf("vpickod.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x244:
                {
                    printf("vreplve.b    %s, %s, %s\n", vd, vj, rk);
                    return;
                }
                case 0x245:
                {
                    printf("vreplve.h    %s, %s, %s\n", vd, vj, rk);
                    return;
                }
                case 0x246:
                {
                    printf("vreplve.w    %s, %s, %s\n", vd, vj, rk);
                    return;
                }
                case 0x247:
                {
                    printf("vreplve.d    %s, %s, %s\n", vd, vj, rk);
                    return;
                }
                case 0x24c:
                {
                    printf("vand.v       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x24d:
                {
                    printf("vor.v        %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x24e:
                {
                    printf("vxor.v       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x24f:
                {
                    printf("vnor.v       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x250:
                {
                    printf("vandn.v      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x251:
                {
                    printf("vorn.v       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x256:
                {
                    printf("vfrstp.b     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x257:
                {
                    printf("vfrstp.h     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25a:
                {
                    printf("vadd.q       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25b:
                {
                    printf("vsub.q       %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25c:
                {
                    printf("vsigncov.b   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25d:
                {
                    printf("vsigncov.h   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25e:
                {
                    printf("vsigncov.w   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x25f:
                {
                    printf("vsigncov.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x261:
                {
                    printf("vfadd.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x262:
                {
                    printf("vfadd.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x265:
                {
                    printf("vfsub.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x266:
                {
                    printf("vfsub.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x271:
                {
                    printf("vfmul.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x272:
                {
                    printf("vfmul.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x275:
                {
                    printf("vfdiv.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x276:
                {
                    printf("vfdiv.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x279:
                {
                    printf("vfmax.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x27a:
                {
                    printf("vfmax.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x27d:
                {
                    printf("vfmin.s      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x27e:
                {
                    printf("vfmin.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x281:
                {
                    printf("vfmaxa.s     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x282:
                {
                    printf("vfmaxa.d     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x285:
                {
                    printf("vfmina.s     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x286:
                {
                    printf("vfmina.d     %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x28c:
                {
                    printf("vfcvt.h.s    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x28d:
                {
                    printf("vfcvt.s.d    %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x290:
                {
                    printf("vffint.s.l   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x293:
                {
                    printf("vftint.w.d   %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x294:
                {
                    printf("vftintrm.w.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x295:
                {
                    printf("vftintrp.w.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x296:
                {
                    printf("vftintrz.w.d %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x297:
                {
                    printf("vftintrne.w.d  %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x2f5:
                {
                    printf("vshuf.h      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x2f6:
                {
                    printf("vshuf.w      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x2f7:
                {
                    printf("vshuf.d      %s, %s, %s\n", vd, vj, vk);
                    return;
                }
                case 0x500:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vseqi.b      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x501:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vseqi.h      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x502:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vseqi.w      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x503:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vseqi.d      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x504:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslei.b      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x505:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslei.h      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x506:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslei.w      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x507:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslei.d      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x508:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslei.bu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x509:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslei.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x50a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslei.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x50b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslei.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x50c:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslti.b      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x50d:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslti.h      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x50e:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslti.w      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x50f:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslti.d      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x510:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vslti.bu     %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x511:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslti.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x512:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslti.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x513:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vslti.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x514:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vaddi.bu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x515:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vaddi.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x516:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vaddi.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x517:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vaddi.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x518:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vsubi.bu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x519:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vsubi.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x51a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vsubi.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x51b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vsubi.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x51c:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vbsll.v      %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x51d:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vbsrl.v      %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x520:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmaxi.b      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x521:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmaxi.h      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x522:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmaxi.w      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x523:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmaxi.d      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x524:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmini.b      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x525:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmini.h      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x526:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmini.w      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x527:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("vmini.d      %s, %s, %d\n", vd, vj, si5);
                    return;
                }
                case 0x528:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmaxi.bu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x529:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmaxi.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmaxi.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmaxi.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52c:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmini.bu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52d:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmini.hu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52e:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmini.wu     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x52f:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vmini.du     %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x534:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vfrstpi.b    %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                case 0x535:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("vfrstpi.h    %s, %s, %d\n", vd, vj, ui5);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            break;
        case 0xa7:
        {
            opcode = (code >> 10) & 0xff;
            switch (opcode)
            {
                case 0x0:
                {
                    printf("vclo.b       %s, %s\n", vd, vj);
                    return;
                }
                case 0x1:
                {
                    printf("vclo.h       %s, %s\n", vd, vj);
                    return;
                }
                case 0x2:
                {
                    printf("vclo.w       %s, %s\n", vd, vj);
                    return;
                }
                case 0x3:
                {
                    printf("vclo.d       %s, %s\n", vd, vj);
                    return;
                }
                case 0x4:
                {
                    printf("vclz.b       %s, %s\n", vd, vj);
                    return;
                }
                case 0x5:
                {
                    printf("vclz.h       %s, %s\n", vd, vj);
                    return;
                }
                case 0x6:
                {
                    printf("vclz.w       %s, %s\n", vd, vj);
                    return;
                }
                case 0x7:
                {
                    printf("vclz.d       %s, %s\n", vd, vj);
                    return;
                }
                case 0x8:
                {
                    printf("vpcnt.b      %s, %s\n", vd, vj);
                    return;
                }
                case 0x9:
                {
                    printf("vpcnt.h      %s, %s\n", vd, vj);
                    return;
                }
                case 0xa:
                {
                    printf("vpcnt.w      %s, %s\n", vd, vj);
                    return;
                }
                case 0xb:
                {
                    printf("vpcnt.d      %s, %s\n", vd, vj);
                    return;
                }
                case 0xc:
                {
                    printf("vneg.b       %s, %s\n", vd, vj);
                    return;
                }
                case 0xd:
                {
                    printf("vneg.h       %s, %s\n", vd, vj);
                    return;
                }
                case 0xe:
                {
                    printf("vneg.w       %s, %s\n", vd, vj);
                    return;
                }
                case 0xf:
                {
                    printf("vneg.d       %s, %s\n", vd, vj);
                    return;
                }
                case 0x10:
                {
                    printf("vmskltz.b    %s, %s\n", vd, vj);
                    return;
                }
                case 0x11:
                {
                    printf("vmskltz.h    %s, %s\n", vd, vj);
                    return;
                }
                case 0x12:
                {
                    printf("vmskltz.w    %s, %s\n", vd, vj);
                    return;
                }
                case 0x13:
                {
                    printf("vmskltz.d    %s, %s\n", vd, vj);
                    return;
                }
                case 0x14:
                {
                    printf("vmskgez.b    %s, %s\n", vd, vj);
                    return;
                }
                case 0x18:
                {
                    printf("vmsknz.b     %s, %s\n", vd, vj);
                    return;
                }
                case 0x26:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vseteqz.v    %s, %s\n", cd, vj);
                    return;
                }
                case 0x27:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetnez.v    %s, %s\n", cd, vj);
                    return;
                }
                case 0x28:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetanyeqz.b %s, %s\n", cd, vj);
                    return;
                }
                case 0x29:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetanyeqz.h %s, %s\n", cd, vj);
                    return;
                }
                case 0x2a:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetanyeqz.w %s, %s\n", cd, vj);
                    return;
                }
                case 0x2b:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetanyeqz.d %s, %s\n", cd, vj);
                    return;
                }
                case 0x2c:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetallnez.b %s, %s\n", cd, vj);
                    return;
                }
                case 0x2d:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetallnez.h %s, %s\n", cd, vj);
                    return;
                }
                case 0x2e:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetallnez.w %s, %s\n", cd, vj);
                    return;
                }
                case 0x2f:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("vsetallnez.d %s, %s\n", cd, vj);
                    return;
                }
                case 0x31:
                {
                    printf("vflogb.s     %s, %s\n", vd, vj);
                    return;
                }
                case 0x32:
                {
                    printf("vflogb.d     %s, %s\n", vd, vj);
                    return;
                }
                case 0x35:
                {
                    printf("vfclass.s    %s, %s\n", vd, vj);
                    return;
                }
                case 0x36:
                {
                    printf("vfclass.d    %s, %s\n", vd, vj);
                    return;
                }
                case 0x39:
                {
                    printf("vfsqrt.s     %s, %s\n", vd, vj);
                    return;
                }
                case 0x3a:
                {
                    printf("vfsqrt.d     %s, %s\n", vd, vj);
                    return;
                }
                case 0x3d:
                {
                    printf("vfrecip.s    %s, %s\n", vd, vj);
                    return;
                }
                case 0x3e:
                {
                    printf("vfrecip.d    %s, %s\n", vd, vj);
                    return;
                }
                case 0x41:
                {
                    printf("vfrsqrt.s    %s, %s\n", vd, vj);
                    return;
                }
                case 0x42:
                {
                    printf("vfrsqrt.d    %s, %s\n", vd, vj);
                    return;
                }
                case 0x4d:
                {
                    printf("vfrint.s     %s, %s\n", vd, vj);
                    return;
                }
                case 0x4e:
                {
                    printf("vfrint.d     %s, %s\n", vd, vj);
                    return;
                }
                case 0x51:
                {
                    printf("vfrintrm.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x52:
                {
                    printf("vfrintrm.d   %s, %s\n", vd, vj);
                    return;
                }
                case 0x55:
                {
                    printf("vfrintrp.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x56:
                {
                    printf("vfrintrp.d   %s, %s\n", vd, vj);
                    return;
                }
                case 0x59:
                {
                    printf("vfrintrz.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x5a:
                {
                    printf("vfrintrz.d   %s, %s\n", vd, vj);
                    return;
                }
                case 0x5d:
                {
                    printf("vfrintrne.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0x5e:
                {
                    printf("vfrintrne.d  %s, %s\n", vd, vj);
                    return;
                }
                case 0x7a:
                {
                    printf("vfcvtl.s.h   %s, %s\n", vd, vj);
                    return;
                }
                case 0x7b:
                {
                    printf("vfcvth.s.h   %s, %s\n", vd, vj);
                    return;
                }
                case 0x7c:
                {
                    printf("vfcvtl.d.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x7d:
                {
                    printf("vfcvth.d.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x80:
                {
                    printf("vffint.s.w   %s, %s\n", vd, vj);
                    return;
                }
                case 0x81:
                {
                    printf("vffint.s.wu  %s, %s\n", vd, vj);
                    return;
                }
                case 0x82:
                {
                    printf("vffint.d.l   %s, %s\n", vd, vj);
                    return;
                }
                case 0x83:
                {
                    printf("vffint.d.lu  %s, %s\n", vd, vj);
                    return;
                }
                case 0x84:
                {
                    printf("vffintl.d.w  %s, %s\n", vd, vj);
                    return;
                }
                case 0x85:
                {
                    printf("vffinth.d.w  %s, %s\n", vd, vj);
                    return;
                }
                case 0x8c:
                {
                    printf("vftint.w.s   %s, %s\n", vd, vj);
                    return;
                }
                case 0x8d:
                {
                    printf("vftint.l.d   %s, %s\n", vd, vj);
                    return;
                }
                case 0x8e:
                {
                    printf("vftintrm.w.s %s, %s\n", vd, vj);
                    return;
                }
                case 0x8f:
                {
                    printf("vftintrm.l.d %s, %s\n", vd, vj);
                    return;
                }
                case 0x90:
                {
                    printf("vftintrp.w.s %s, %s\n", vd, vj);
                    return;
                }
                case 0x91:
                {
                    printf("vftintrp.l.d %s, %s\n", vd, vj);
                    return;
                }
                case 0x92:
                {
                    printf("vftintrz.w.s %s, %s\n", vd, vj);
                    return;
                }
                case 0x93:
                {
                    printf("vftintrz.l.d %s, %s\n", vd, vj);
                    return;
                }
                case 0x94:
                {
                    printf("vftintrne.w.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0x95:
                {
                    printf("vftintrne.l.d  %s, %s\n", vd, vj);
                    return;
                }
                case 0x96:
                {
                    printf("vftint.wu.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0x97:
                {
                    printf("vftint.lu.d  %s, %s\n", vd, vj);
                    return;
                }
                case 0x9c:
                {
                    printf("vftintrz.wu.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0x9d:
                {
                    printf("vftintrz.lu.d  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa0:
                {
                    printf("vftintl.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa1:
                {
                    printf("vftinth.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa2:
                {
                    printf("vftintrml.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa3:
                {
                    printf("vftintrmh.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa4:
                {
                    printf("vftintrpl.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa5:
                {
                    printf("vftintrph.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa6:
                {
                    printf("vftintrzl.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa7:
                {
                    printf("vftintrzh.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa8:
                {
                    printf("vftintrnel.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xa9:
                {
                    printf("vftintrneh.l.s  %s, %s\n", vd, vj);
                    return;
                }
                case 0xb8:
                {
                    printf("vexth.h.b    %s, %s\n", vd, vj);
                    return;
                }
                case 0xb9:
                {
                    printf("vexth.w.h    %s, %s\n", vd, vj);
                    return;
                }
                case 0xba:
                {
                    printf("vexth.d.w    %s, %s\n", vd, vj);
                    return;
                }
                case 0xbb:
                {
                    printf("vexth.q.d    %s, %s\n", vd, vj);
                    return;
                }
                case 0xbc:
                {
                    printf("vexth.hu.bu  %s, %s\n", vd, vj);
                    return;
                }
                case 0xbd:
                {
                    printf("vexth.wu.hu  %s, %s\n", vd, vj);
                    return;
                }
                case 0xbe:
                {
                    printf("vexth.du.wu  %s, %s\n", vd, vj);
                    return;
                }
                case 0xbf:
                {
                    printf("vexth.qu.du  %s, %s\n", vd, vj);
                    return;
                }
                case 0xc0:
                {
                    printf("vreplgr2vr.b %s, %s\n", vd, rj);
                    return;
                }
                case 0xc1:
                {
                    printf("vreplgr2vr.h %s, %s\n", vd, rj);
                    return;
                }
                case 0xc2:
                {
                    printf("vreplgr2vr.w %s, %s\n", vd, rj);
                    return;
                }
                case 0xc3:
                {
                    printf("vreplgr2vr.d %s, %s\n", vd, rj);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            break;
        }
        case 0xa8:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vrotri.b     %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vrotri.h     %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vrotri.w     %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vrotri.d     %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xa9:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsrlri.b     %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrlri.h     %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrlri.w     %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrlri.d     %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xaa:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsrari.b     %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrari.h     %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrari.w     %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrari.d     %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xba:
        {
            opcode = code >> 11;
            if ((opcode & 0x78) == 0x70)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vinsgr2vr.b  %s, %s, %d\n", vd, rj, ui4);
                return;
            }
            else if ((opcode & 0x7c) == 0x78)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vinsgr2vr.h  %s, %s, %d\n", vd, rj, ui3);
                return;
            }
            else if ((opcode & 0x7e) == 0x7c)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                assert((0 <= ui2) && (ui2 <= 3));
                printf("vinsgr2vr.w  %s, %s, %d\n", vd, rj, ui2);
                return;
            }
            else if ((opcode & 0x7f) == 0x7e)
            {
                unsigned short ui1 = (code >> 10) & 0x1;
                printf("vinsgr2vr.d  %s, %s, %d\n", vd, rj, ui1);
                return;
            }
            break;
        }
        case 0xbb:
        {
            opcode = code >> 11;
            if ((opcode & 0x78) == 0x70)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vpickve2gr.b %s, %s, %d\n", rd, vj, ui4);
                return;
            }
            else if ((opcode & 0x7c) == 0x78)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vpickve2gr.h %s, %s, %d\n", rd, vj, ui3);
                return;
            }
            else if ((opcode & 0x7e) == 0x7c)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                printf("vpickve2gr.w %s, %s, %d\n", rd, vj, ui2);
                return;
            }
            else if ((opcode & 0x7f) == 0x7e)
            {
                unsigned short ui1 = (code >> 10) & 0x1;
                printf("vpickve2gr.d %s, %s, %d\n", rd, vj, ui1);
                return;
            }
            break;
        }
        case 0xbc:
        {
            opcode = code >> 11;
            if ((opcode & 0x78) == 0x70)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vpickve2gr.bu  %s, %s, %d\n", rd, vj, ui4);
                return;
            }
            else if ((opcode & 0x7c) == 0x78)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vpickve2gr.hu  %s, %s, %d\n", rd, vj, ui3);
                return;
            }
            else if ((opcode & 0x7e) == 0x7c)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                printf("vpickve2gr.wu  %s, %s, %d\n", rd, vj, ui2);
                return;
            }
            else if ((opcode & 0x7f) == 0x7e)
            {
                unsigned short ui1 = (code >> 10) & 0x1;
                printf("vpickve2gr.du  %s, %s, %d\n", rd, vj, ui1);
                return;
            }
            break;
        }
        case 0xbd:
        {
            opcode = code >> 11;
            if ((opcode & 0x78) == 0x70)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vreplvei.b   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x7c) == 0x78)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vreplvei.h   %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x7e) == 0x7c)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                assert((0 <= ui2) && (ui2 <= 3));
                printf("vreplvei.w   %s, %s, %d\n", vd, vj, ui2);
                return;
            }
            else if ((opcode & 0x7f) == 0x7e)
            {
                unsigned short ui1 = (code >> 10) & 0x1;
                printf("vreplvei.d   %s, %s, %d\n", rd, vj, ui1);
                return;
            }
            break;
        }
        case 0xc2:
        {
            opcode = code >> 10;
            if ((opcode & 0xf8) == 0x8)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsllwil.h.b  %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0xf0) == 0x10)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsllwil.w.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe0) == 0x20)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsllwil.d.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xff) == 0x40)
            {
                printf("vextl.q.d    %s, %s\n", vd, vj);
                return;
            }
            break;
        }
        case 0xc3:
        {
            opcode = code >> 10;
            if ((opcode & 0xf8) == 0x8)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsllwil.hu.bu  %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0xf0) == 0x10)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsllwil.wu.hu  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe0) == 0x20)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsllwil.du.wu  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xff) == 0x40)
            {
                printf("vextl.qu.du  %s, %s\n", vd, vj);
                return;
            }
            break;
        }
        case 0xc4:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vbitclri.b   %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vbitclri.h   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vbitclri.w   %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vbitclri.d   %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xc5:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vbitseti.b   %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vbitseti.h   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vbitseti.w   %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vbitseti.d   %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xc6:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vbitrevi.b   %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vbitrevi.h   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vbitrevi.w   %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vbitrevi.d   %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xc9:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsat.b       %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsat.h       %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsat.w       %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsat.d       %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xca:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsat.bu      %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsat.hu      %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsat.wu      %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsat.du      %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xcb:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vslli.b      %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vslli.h      %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vslli.w      %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vslli.d      %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xcc:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsrli.b      %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrli.h      %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrli.w      %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrli.d      %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xcd:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("vsrai.b      %s, %s, %d\n", vd, vj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrai.h      %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrai.w      %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrai.d      %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            break;
        }
        case 0xd0:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrlni.b.h   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrlni.h.w   %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrlni.w.d   %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vsrlni.d.q   %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd1:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrlrni.b.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrlrni.h.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrlrni.w.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vsrlrni.d.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd2:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrlni.b.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrlni.h.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrlni.w.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrlni.d.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd3:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrlni.bu.h %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrlni.hu.w %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrlni.wu.d %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrlni.du.q %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd4:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrlrni.b.h %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrlrni.h.w %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrlrni.w.d %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrlrni.d.q %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd5:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrlrni.bu.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrlrni.hu.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrlrni.wu.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrlrni.du.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd6:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrani.b.h   %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrani.h.w   %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrani.w.d   %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vsrani.d.q   %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd7:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vsrarni.b.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vsrarni.h.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vsrarni.w.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vsrarni.d.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd8:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrani.b.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrani.h.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrani.w.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrani.d.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xd9:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrani.bu.h %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrani.hu.w %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrani.wu.d %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrani.du.q %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xda:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrarni.b.h %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrarni.h.w %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrarni.w.d %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrarni.d.q %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xdb:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("vssrarni.bu.h  %s, %s, %d\n", vd, vj, ui4);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("vssrarni.hu.w  %s, %s, %d\n", vd, vj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("vssrarni.wu.d  %s, %s, %d\n", vd, vj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("vssrarni.du.q  %s, %s, %d\n", vd, vj, ui7);
                return;
            }
            break;
        }
        case 0xe0:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vextrins.d   %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe1:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vextrins.w   %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe2:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vextrins.h   %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe3:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vextrins.b   %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe4:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vshuf4i.b    %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe5:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vshuf4i.h    %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe6:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vshuf4i.w    %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xe7:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vshuf4i.d    %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf1:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vbitseli.b   %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf4:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vandi.b      %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf5:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vori.b       %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf6:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vxori.b      %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf7:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("vnori.b      %s, %s, %d\n", vd, vj, ui8);
            return;
        }
        case 0xf8:
        {
            short i13 = (code >> 5) & 0x1fff;
            printf("vldi         %s, 0x%x\n", vd, i13);
            return;
        }
        case 0xf9:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvpermi.w    %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }
    printf("LOONGARCH illegal instruction: %08X\n", code);
    return;

Label_OPCODE_1D:
    opcode = (code >> 18) & 0xff; // MSB14
    switch (opcode)
    {
        case 0 ... 0xa6:
            opcode = (code >> 15) & 0x7ff;
            switch (opcode)
            {
                case 0x0:
                {
                    printf("xvseq.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1:
                {
                    printf("xvseq.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x2:
                {
                    printf("xvseq.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x3:
                {
                    printf("xvseq.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x4:
                {
                    printf("xvsle.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x5:
                {
                    printf("xvsle.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x6:
                {
                    printf("xvsle.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x7:
                {
                    printf("xvsle.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x8:
                {
                    printf("xvsle.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x9:
                {
                    printf("xvsle.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xa:
                {
                    printf("xvsle.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb:
                {
                    printf("xvsle.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc:
                {
                    printf("xvslt.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd:
                {
                    printf("xvslt.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe:
                {
                    printf("xvslt.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xf:
                {
                    printf("xvslt.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10:
                {
                    printf("xvslt.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x11:
                {
                    printf("xvslt.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x12:
                {
                    printf("xvslt.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x13:
                {
                    printf("xvslt.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x14:
                {
                    printf("xvadd.b        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15:
                {
                    printf("xvadd.h        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16:
                {
                    printf("xvadd.w        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17:
                {
                    printf("xvadd.d        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x18:
                {
                    printf("xvsub.b        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x19:
                {
                    printf("xvsub.h        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1a:
                {
                    printf("xvsub.w        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1b:
                {
                    printf("xvsub.d        %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x3c:
                {
                    printf("xvaddwev.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x3d:
                {
                    printf("xvaddwev.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x3e:
                {
                    printf("xvaddwev.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x3f:
                {
                    printf("xvaddwev.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x40:
                {
                    printf("xvsubwev.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x41:
                {
                    printf("xvsubwev.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x42:
                {
                    printf("xvsubwev.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x43:
                {
                    printf("xvsubwev.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x44:
                {
                    printf("xvaddwod.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x45:
                {
                    printf("xvaddwod.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x46:
                {
                    printf("xvaddwod.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x47:
                {
                    printf("xvaddwod.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x48:
                {
                    printf("xvsubwod.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x49:
                {
                    printf("xvsubwod.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x4a:
                {
                    printf("xvsubwod.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x4b:
                {
                    printf("xvsubwod.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x5c:
                {
                    printf("xvaddwev.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x5d:
                {
                    printf("xvaddwev.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x5e:
                {
                    printf("xvaddwev.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x5f:
                {
                    printf("xvaddwev.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x60:
                {
                    printf("xvsubwev.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x61:
                {
                    printf("xvsubwev.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x62:
                {
                    printf("xvsubwev.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x63:
                {
                    printf("xvsubwev.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x64:
                {
                    printf("xvaddwod.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x65:
                {
                    printf("xvaddwod.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x66:
                {
                    printf("xvaddwod.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x67:
                {
                    printf("xvaddwod.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x68:
                {
                    printf("xvsubwod.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x69:
                {
                    printf("xvsubwod.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x6a:
                {
                    printf("xvsubwod.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x6b:
                {
                    printf("xvsubwod.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x7c:
                {
                    printf("xvaddwev.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x7d:
                {
                    printf("xvaddwev.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x7e:
                {
                    printf("xvaddwev.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x7f:
                {
                    printf("xvaddwev.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x80:
                {
                    printf("xvaddwod.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x81:
                {
                    printf("xvaddwod.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x82:
                {
                    printf("xvaddwod.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x83:
                {
                    printf("xvaddwod.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x8c:
                {
                    printf("xvsadd.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x8d:
                {
                    printf("xvsadd.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x8e:
                {
                    printf("xvsadd.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x8f:
                {
                    printf("xvsadd.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x90:
                {
                    printf("xvssub.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x91:
                {
                    printf("xvssub.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x92:
                {
                    printf("xvssub.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x93:
                {
                    printf("xvssub.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x94:
                {
                    printf("xvsadd.bu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x95:
                {
                    printf("xvsadd.hu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x96:
                {
                    printf("xvsadd.wu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x97:
                {
                    printf("xvsadd.du    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x98:
                {
                    printf("xvssub.bu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x99:
                {
                    printf("xvssub.hu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x9a:
                {
                    printf("xvssub.wu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x9b:
                {
                    printf("xvssub.du    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xa8:
                {
                    printf("xvhaddw.h.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xa9:
                {
                    printf("xvhaddw.w.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xaa:
                {
                    printf("xvhaddw.d.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xab:
                {
                    printf("xvhaddw.q.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xac:
                {
                    printf("xvhsubw.h.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xad:
                {
                    printf("xvhsubw.w.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xae:
                {
                    printf("xvhsubw.d.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xaf:
                {
                    printf("xvhsubw.q.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb0:
                {
                    printf("xvhaddw.hu.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb1:
                {
                    printf("xvhaddw.wu.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb2:
                {
                    printf("xvhaddw.du.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb3:
                {
                    printf("xvhaddw.qu.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb4:
                {
                    printf("xvhsubw.hu.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb5:
                {
                    printf("xvhsubw.wu.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb6:
                {
                    printf("xvhsubw.du.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb7:
                {
                    printf("xvhsubw.qu.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb8:
                {
                    printf("xvadda.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xb9:
                {
                    printf("xvadda.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xba:
                {
                    printf("xvadda.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xbb:
                {
                    printf("xvadda.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc0:
                {
                    printf("xvabsd.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc1:
                {
                    printf("xvabsd.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc2:
                {
                    printf("xvabsd.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc3:
                {
                    printf("xvabsd.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc4:
                {
                    printf("xvabsd.bu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc5:
                {
                    printf("xvabsd.hu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc6:
                {
                    printf("xvabsd.wu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc7:
                {
                    printf("xvabsd.du    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc8:
                {
                    printf("xvavg.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xc9:
                {
                    printf("xvavg.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xca:
                {
                    printf("xvavg.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xcb:
                {
                    printf("xvavg.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xcc:
                {
                    printf("xvavg.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xcd:
                {
                    printf("xvavg.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xce:
                {
                    printf("xvavg.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xcf:
                {
                    printf("xvavg.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd0:
                {
                    printf("xvavgr.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd1:
                {
                    printf("xvavgr.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd2:
                {
                    printf("xvavgr.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd3:
                {
                    printf("xvavgr.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd4:
                {
                    printf("xvavgr.bu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd5:
                {
                    printf("xvavgr.hu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd6:
                {
                    printf("xvavgr.wu    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xd7:
                {
                    printf("xvavgr.du    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe0:
                {
                    printf("xvmax.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe1:
                {
                    printf("xvmax.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe2:
                {
                    printf("xvmax.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe3:
                {
                    printf("xvmax.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe4:
                {
                    printf("xvmin.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe5:
                {
                    printf("xvmin.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe6:
                {
                    printf("xvmin.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe7:
                {
                    printf("xvmin.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe8:
                {
                    printf("xvmax.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xe9:
                {
                    printf("xvmax.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xea:
                {
                    printf("xvmax.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xeb:
                {
                    printf("xvmax.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xec:
                {
                    printf("xvmin.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xed:
                {
                    printf("xvmin.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xee:
                {
                    printf("xvmin.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0xef:
                {
                    printf("xvmin.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x108:
                {
                    printf("xvmul.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x109:
                {
                    printf("xvmul.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10a:
                {
                    printf("xvmul.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10b:
                {
                    printf("xvmul.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10c:
                {
                    printf("xvmuh.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10d:
                {
                    printf("xvmuh.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10e:
                {
                    printf("xvmuh.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x10f:
                {
                    printf("xvmuh.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x110:
                {
                    printf("xvmuh.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x111:
                {
                    printf("xvmuh.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x112:
                {
                    printf("xvmuh.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x113:
                {
                    printf("xvmuh.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x120:
                {
                    printf("xvmulwev.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x121:
                {
                    printf("xvmulwev.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x122:
                {
                    printf("xvmulwev.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x123:
                {
                    printf("xvmulwev.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x124:
                {
                    printf("xvmulwod.h.b %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x125:
                {
                    printf("xvmulwod.w.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x126:
                {
                    printf("xvmulwod.d.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x127:
                {
                    printf("xvmulwod.q.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x130:
                {
                    printf("xvmulwev.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x131:
                {
                    printf("xvmulwev.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x132:
                {
                    printf("xvmulwev.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x133:
                {
                    printf("xvmulwev.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x134:
                {
                    printf("xvmulwod.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x135:
                {
                    printf("xvmulwod.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x136:
                {
                    printf("xvmulwod.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x137:
                {
                    printf("xvmulwod.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x140:
                {
                    printf("xvmulwev.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x141:
                {
                    printf("xvmulwev.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x142:
                {
                    printf("xvmulwev.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x143:
                {
                    printf("xvmulwev.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x144:
                {
                    printf("xvmulwod.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x145:
                {
                    printf("xvmulwod.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x146:
                {
                    printf("xvmulwod.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x147:
                {
                    printf("xvmulwod.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x150:
                {
                    printf("xvmadd.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x151:
                {
                    printf("xvmadd.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x152:
                {
                    printf("xvmadd.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x153:
                {
                    printf("xvmadd.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x154:
                {
                    printf("xvmsub.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x155:
                {
                    printf("xvmsub.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x156:
                {
                    printf("xvmsub.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x157:
                {
                    printf("xvmsub.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x158:
                {
                    printf("xvmaddwev.h.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x159:
                {
                    printf("xvmaddwev.w.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15a:
                {
                    printf("xvmaddwev.d.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15b:
                {
                    printf("xvmaddwev.q.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15c:
                {
                    printf("xvmaddwod.h.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15d:
                {
                    printf("xvmaddwod.w.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15e:
                {
                    printf("xvmaddwod.d.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x15f:
                {
                    printf("xvmaddwod.q.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x168:
                {
                    printf("xvmaddwev.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x169:
                {
                    printf("xvmaddwev.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16a:
                {
                    printf("xvmaddwev.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16b:
                {
                    printf("xvmaddwev.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16c:
                {
                    printf("xvmaddwod.h.bu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16d:
                {
                    printf("xvmaddwod.w.hu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16e:
                {
                    printf("xvmaddwod.d.wu  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x16f:
                {
                    printf("xvmaddwod.q.du  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x178:
                {
                    printf("xvmaddwev.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x179:
                {
                    printf("xvmaddwev.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17a:
                {
                    printf("xvmaddwev.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17b:
                {
                    printf("xvmaddwev.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17c:
                {
                    printf("xvmaddwod.h.bu.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17d:
                {
                    printf("xvmaddwod.w.hu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17e:
                {
                    printf("xvmaddwod.d.wu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x17f:
                {
                    printf("xvmaddwod.q.du.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c0:
                {
                    printf("xvdiv.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c1:
                {
                    printf("xvdiv.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c2:
                {
                    printf("xvdiv.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c3:
                {
                    printf("xvdiv.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c4:
                {
                    printf("xvmod.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c5:
                {
                    printf("xvmod.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c6:
                {
                    printf("xvmod.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c7:
                {
                    printf("xvmod.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c8:
                {
                    printf("xvdiv.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1c9:
                {
                    printf("xvdiv.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ca:
                {
                    printf("xvdiv.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1cb:
                {
                    printf("xvdiv.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1cc:
                {
                    printf("xvmod.bu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1cd:
                {
                    printf("xvmod.hu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ce:
                {
                    printf("xvmod.wu     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1cf:
                {
                    printf("xvmod.du     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d0:
                {
                    printf("xvsll.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d1:
                {
                    printf("xvsll.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d2:
                {
                    printf("xvsll.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d3:
                {
                    printf("xvsll.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d4:
                {
                    printf("xvsrl.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d5:
                {
                    printf("xvsrl.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d6:
                {
                    printf("xvsrl.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d7:
                {
                    printf("xvsrl.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d8:
                {
                    printf("xvsra.b      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1d9:
                {
                    printf("xvsra.h      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1da:
                {
                    printf("xvsra.w      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1db:
                {
                    printf("xvsra.d      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1dc:
                {
                    printf("xvrotr.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1dd:
                {
                    printf("xvrotr.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1de:
                {
                    printf("xvrotr.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1df:
                {
                    printf("xvrotr.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e0:
                {
                    printf("xvsrlr.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e1:
                {
                    printf("xvsrlr.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e2:
                {
                    printf("xvsrlr.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e3:
                {
                    printf("xvsrlr.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e4:
                {
                    printf("xvsrar.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e5:
                {
                    printf("xvsrar.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e6:
                {
                    printf("xvsrar.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e7:
                {
                    printf("xvsrar.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1e9:
                {
                    printf("xvsrln.b.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ea:
                {
                    printf("xvsrln.h.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1eb:
                {
                    printf("xvsrln.w.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ed:
                {
                    printf("xvsran.b.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ee:
                {
                    printf("xvsran.h.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ef:
                {
                    printf("xvsran.w.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f1:
                {
                    printf("xvsrlrn.b.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f2:
                {
                    printf("xvsrlrn.h.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f3:
                {
                    printf("xvsrlrn.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f5:
                {
                    printf("xvsrarn.b.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f6:
                {
                    printf("xvsrarn.h.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f7:
                {
                    printf("xvsrarn.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1f9:
                {
                    printf("xvssrln.b.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1fa:
                {
                    printf("xvssrln.h.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1fb:
                {
                    printf("xvssrln.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1fd:
                {
                    printf("xvssran.b.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1fe:
                {
                    printf("xvssran.h.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x1ff:
                {
                    printf("xvssran.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x201:
                {
                    printf("xvssrlrn.b.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x202:
                {
                    printf("xvssrlrn.h.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x203:
                {
                    printf("xvssrlrn.w.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x205:
                {
                    printf("xvssrarn.b.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x206:
                {
                    printf("xvssrarn.h.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x207:
                {
                    printf("xvssrarn.w.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x209:
                {
                    printf("xvssrln.bu.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x20a:
                {
                    printf("xvssrln.hu.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x20b:
                {
                    printf("xvssrln.wu.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x20d:
                {
                    printf("xvssran.bu.h %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x20e:
                {
                    printf("xvssran.hu.w %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x20f:
                {
                    printf("xvssran.wu.d %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x211:
                {
                    printf("xvssrlrn.bu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x212:
                {
                    printf("xvssrlrn.hu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x213:
                {
                    printf("xvssrlrn.wu.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x215:
                {
                    printf("xvssrarn.bu.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x216:
                {
                    printf("xvssrarn.hu.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x217:
                {
                    printf("xvssrarn.wu.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x218:
                {
                    printf("xvbitclr.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x219:
                {
                    printf("xvbitclr.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21a:
                {
                    printf("xvbitclr.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21b:
                {
                    printf("xvbitclr.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21c:
                {
                    printf("xvbitset.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21d:
                {
                    printf("xvbitset.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21e:
                {
                    printf("xvbitset.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x21f:
                {
                    printf("xvbitset.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x220:
                {
                    printf("xvbitrev.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x221:
                {
                    printf("xvbitrev.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x222:
                {
                    printf("xvbitrev.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x223:
                {
                    printf("xvbitrev.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x22c:
                {
                    printf("xvpackev.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x22d:
                {
                    printf("xvpackev.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x22e:
                {
                    printf("xvpackev.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x22f:
                {
                    printf("xvpackev.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x230:
                {
                    printf("xvpackod.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x231:
                {
                    printf("xvpackod.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x232:
                {
                    printf("xvpackod.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x233:
                {
                    printf("xvpackod.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x234:
                {
                    printf("xvilvl.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x235:
                {
                    printf("xvilvl.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x236:
                {
                    printf("xvilvl.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x237:
                {
                    printf("xvilvl.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x238:
                {
                    printf("xvilvh.b     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x239:
                {
                    printf("xvilvh.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23a:
                {
                    printf("xvilvh.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23b:
                {
                    printf("xvilvh.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23c:
                {
                    printf("xvpickev.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23d:
                {
                    printf("xvpickev.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23e:
                {
                    printf("xvpickev.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x23f:
                {
                    printf("xvpickev.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x240:
                {
                    printf("xvpickod.b   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x241:
                {
                    printf("xvpickod.h   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x242:
                {
                    printf("xvpickod.w   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x243:
                {
                    printf("xvpickod.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x244:
                {
                    printf("xvreplve.b   %s, %s, %s\n", xd, xj, rk);
                    return;
                }
                case 0x245:
                {
                    printf("xvreplve.h   %s, %s, %s\n", xd, xj, rk);
                    return;
                }
                case 0x246:
                {
                    printf("xvreplve.w   %s, %s, %s\n", xd, xj, rk);
                    return;
                }
                case 0x247:
                {
                    printf("xvreplve.d   %s, %s, %s\n", xd, xj, rk);
                    return;
                }
                case 0x24c:
                {
                    printf("xvand.v      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x24d:
                {
                    printf("xvor.v       %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x24e:
                {
                    printf("xvxor.v      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x24f:
                {
                    printf("xvnor.v      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x250:
                {
                    printf("xvandn.v     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x251:
                {
                    printf("xvorn.v      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x256:
                {
                    printf("xvfrstp.b    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x257:
                {
                    printf("xvfrstp.h    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25a:
                {
                    printf("xvadd.q      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25b:
                {
                    printf("xvsub.q      %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25c:
                {
                    printf("xvsigncov.b  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25d:
                {
                    printf("xvsigncov.h  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25e:
                {
                    printf("xvsigncov.w  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x25f:
                {
                    printf("xvsigncov.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x261:
                {
                    printf("xvfadd.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x262:
                {
                    printf("xvfadd.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x265:
                {
                    printf("xvfsub.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x266:
                {
                    printf("xvfsub.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x271:
                {
                    printf("xvfmul.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x272:
                {
                    printf("xvfmul.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x275:
                {
                    printf("xvfdiv.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x276:
                {
                    printf("xvfdiv.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x279:
                {
                    printf("xvfmax.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x27a:
                {
                    printf("xvfmax.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x27d:
                {
                    printf("xvfmin.s     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x27e:
                {
                    printf("xvfmin.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x281:
                {
                    printf("xvfmaxa.s    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x282:
                {
                    printf("xvfmaxa.d    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x285:
                {
                    printf("xvfmina.s    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x286:
                {
                    printf("xvfmina.d    %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x28c:
                {
                    printf("xvfcvt.h.s   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x28d:
                {
                    printf("xvfcvt.s.d   %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x290:
                {
                    printf("xvffint.s.l  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x293:
                {
                    printf("xvftint.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x294:
                {
                    printf("xvftintrm.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x295:
                {
                    printf("xvftintrp.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x296:
                {
                    printf("xvftintrz.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x297:
                {
                    printf("xvftintrne.w.d  %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x2f5:
                {
                    printf("xvshuf.h     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x2f6:
                {
                    printf("xvshuf.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x2f7:
                {
                    printf("xvshuf.d     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x2fa:
                {
                    printf("xvperm.w     %s, %s, %s\n", xd, xj, xk);
                    return;
                }
                case 0x500:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvseqi.b     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x501:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvseqi.h     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x502:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvseqi.w     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x503:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvseqi.d     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x504:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslei.b     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x505:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslei.h     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x506:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslei.w     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x507:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslei.d     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x508:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslei.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x509:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslei.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x50a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslei.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x50b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslei.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x50c:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslti.b     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x50d:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslti.h     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x50e:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslti.w     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x50f:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvslti.d     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x510:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslti.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x511:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslti.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x512:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslti.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x513:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvslti.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x514:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvaddi.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x515:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvaddi.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x516:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvaddi.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x517:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvaddi.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x518:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvsubi.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x519:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvsubi.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x51a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvsubi.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x51b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvsubi.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x51c:
                {
                    unsigned int ui5 = (code >> 10) & 0x1f;
                    printf("xvbsll.v     %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x51d:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvbsrl.v     %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x520:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmaxi.b     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x521:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmaxi.h     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x522:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmaxi.w     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x523:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmaxi.d     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x524:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmini.b     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x525:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmini.h     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x526:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmini.w     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x527:
                {
                    short si5 = ((code >> 10) & 0x1f) << 11;
                    si5 >>= 11;
                    assert((-16 <= si5) && (si5 <= 15));
                    printf("xvmini.d     %s, %s, %d\n", xd, xj, si5);
                    return;
                }
                case 0x528:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmaxi.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x529:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmaxi.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52a:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmaxi.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52b:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmaxi.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52c:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmini.bu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52d:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmini.hu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52e:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmini.wu    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x52f:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvmini.du    %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x534:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvfrstpi.b   %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                case 0x535:
                {
                    unsigned short ui5 = (code >> 10) & 0x1f;
                    printf("xvfrstpi.h   %s, %s, %d\n", xd, xj, ui5);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            break;
        case 0xa7:
        {
            opcode = (code >> 10) & 0xff;
            switch (opcode)
            {
                case 0x0:
                {
                    printf("xvclo.b      %s, %s\n", xd, xj);
                    return;
                }
                case 0x1:
                {
                    printf("xvclo.h      %s, %s\n", xd, xj);
                    return;
                }
                case 0x2:
                {
                    printf("xvclo.w      %s, %s\n", xd, xj);
                    return;
                }
                case 0x3:
                {
                    printf("xvclo.d      %s, %s\n", xd, xj);
                    return;
                }
                case 0x4:
                {
                    printf("xvclz.b      %s, %s\n", xd, xj);
                    return;
                }
                case 0x5:
                {
                    printf("xvclz.h      %s, %s\n", xd, xj);
                    return;
                }
                case 0x6:
                {
                    printf("xvclz.w      %s, %s\n", xd, xj);
                    return;
                }
                case 0x7:
                {
                    printf("xvclz.d      %s, %s\n", xd, xj);
                    return;
                }
                case 0x8:
                {
                    printf("xvpcnt.b     %s, %s\n", xd, xj);
                    return;
                }
                case 0x9:
                {
                    printf("xvpcnt.h     %s, %s\n", xd, xj);
                    return;
                }
                case 0xa:
                {
                    printf("xvpcnt.w     %s, %s\n", xd, xj);
                    return;
                }
                case 0xb:
                {
                    printf("xvpcnt.d     %s, %s\n", xd, xj);
                    return;
                }
                case 0xc:
                {
                    printf("xvneg.b      %s, %s\n", xd, xj);
                    return;
                }
                case 0xd:
                {
                    printf("xvneg.h      %s, %s\n", xd, xj);
                    return;
                }
                case 0xe:
                {
                    printf("xvneg.w      %s, %s\n", xd, xj);
                    return;
                }
                case 0xf:
                {
                    printf("xvneg.d      %s, %s\n", xd, xj);
                    return;
                }
                case 0x10:
                {
                    printf("xvmskltz.b   %s, %s\n", xd, xj);
                    return;
                }
                case 0x11:
                {
                    printf("xvmskltz.h   %s, %s\n", xd, xj);
                    return;
                }
                case 0x12:
                {
                    printf("xvmskltz.w   %s, %s\n", xd, xj);
                    return;
                }
                case 0x13:
                {
                    printf("xvmskltz.d   %s, %s\n", xd, xj);
                    return;
                }
                case 0x14:
                {
                    printf("xvmskgez.b   %s, %s\n", xd, xj);
                    return;
                }
                case 0x18:
                {
                    printf("xvmsknz.b    %s, %s\n", xd, xj);
                    return;
                }
                case 0x26:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvseteqz.v   %s, %s\n", cd, xj);
                    return;
                }
                case 0x27:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetnez.v   %s, %s\n", cd, xj);
                    return;
                }
                case 0x28:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetanyeqz.b  %s, %s\n", cd, xj);
                    return;
                }
                case 0x29:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetanyeqz.h  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2a:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetanyeqz.w  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2b:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetanyeqz.d  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2c:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetallnez.b  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2d:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetallnez.h  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2e:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetallnez.w  %s, %s\n", cd, xj);
                    return;
                }
                case 0x2f:
                {
                    const char* cd = CFregName[code & 0x7];
                    printf("xvsetallnez.d  %s, %s\n", cd, xj);
                    return;
                }
                case 0x31:
                {
                    printf("xvflogb.s    %s, %s\n", xd, xj);
                    return;
                }
                case 0x32:
                {
                    printf("xvflogb.d    %s, %s\n", xd, xj);
                    return;
                }
                case 0x35:
                {
                    printf("xvfclass.s   %s, %s\n", xd, xj);
                    return;
                }
                case 0x36:
                {
                    printf("xvfclass.d   %s, %s\n", xd, xj);
                    return;
                }
                case 0x39:
                {
                    printf("xvfsqrt.s    %s, %s\n", xd, xj);
                    return;
                }
                case 0x3a:
                {
                    printf("xvfsqrt.d    %s, %s\n", xd, xj);
                    return;
                }
                case 0x3d:
                {
                    printf("xvfrecip.s   %s, %s\n", xd, xj);
                    return;
                }
                case 0x3e:
                {
                    printf("xvfrecip.d   %s, %s\n", xd, xj);
                    return;
                }
                case 0x41:
                {
                    printf("xvfrsqrt.s   %s, %s\n", xd, xj);
                    return;
                }
                case 0x42:
                {
                    printf("xvfrsqrt.d   %s, %s\n", xd, xj);
                    return;
                }
                case 0x4d:
                {
                    printf("xvfrint.s    %s, %s\n", xd, xj);
                    return;
                }
                case 0x4e:
                {
                    printf("xvfrint.d    %s, %s\n", xd, xj);
                    return;
                }
                case 0x51:
                {
                    printf("xvfrintrm.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x52:
                {
                    printf("xvfrintrm.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x55:
                {
                    printf("xvfrintrp.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x56:
                {
                    printf("xvfrintrp.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x59:
                {
                    printf("xvfrintrz.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x5a:
                {
                    printf("xvfrintrz.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x5d:
                {
                    printf("xvfrintrne.s %s, %s\n", xd, xj);
                    return;
                }
                case 0x5e:
                {
                    printf("xvfrintrne.d %s, %s\n", xd, xj);
                    return;
                }
                case 0x7a:
                {
                    printf("xvfcvtl.s.h  %s, %s\n", xd, xj);
                    return;
                }
                case 0x7b:
                {
                    printf("xvfcvth.s.h  %s, %s\n", xd, xj);
                    return;
                }
                case 0x7c:
                {
                    printf("xvfcvtl.d.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x7d:
                {
                    printf("xvfcvth.d.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x80:
                {
                    printf("xvffint.s.w  %s, %s\n", xd, xj);
                    return;
                }
                case 0x81:
                {
                    printf("xvffint.s.wu %s, %s\n", xd, xj);
                    return;
                }
                case 0x82:
                {
                    printf("xvffint.d.l  %s, %s\n", xd, xj);
                    return;
                }
                case 0x83:
                {
                    printf("xvffint.d.lu %s, %s\n", xd, xj);
                    return;
                }
                case 0x84:
                {
                    printf("xvffintl.d.w %s, %s\n", xd, xj);
                    return;
                }
                case 0x85:
                {
                    printf("xvffinth.d.w %s, %s\n", xd, xj);
                    return;
                }
                case 0x8c:
                {
                    printf("xvftint.w.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x8d:
                {
                    printf("xvftint.l.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x8e:
                {
                    printf("xvftintrm.w.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x8f:
                {
                    printf("xvftintrm.l.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x90:
                {
                    printf("xvftintrp.w.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x91:
                {
                    printf("xvftintrp.l.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x92:
                {
                    printf("xvftintrz.w.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x93:
                {
                    printf("xvftintrz.l.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x94:
                {
                    printf("xvftintrne.w.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x95:
                {
                    printf("xvftintrne.l.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0x96:
                {
                    printf("xvftint.wu.s %s, %s\n", xd, xj);
                    return;
                }
                case 0x97:
                {
                    printf("xvftint.lu.d %s, %s\n", xd, xj);
                    return;
                }
                case 0x9c:
                {
                    printf("xvftintrz.wu.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0x9d:
                {
                    printf("xvftintrz.lu.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa0:
                {
                    printf("xvftintl.l.s %s, %s\n", xd, xj);
                    return;
                }
                case 0xa1:
                {
                    printf("xvftinth.l.s %s, %s\n", xd, xj);
                    return;
                }
                case 0xa2:
                {
                    printf("xvftintrml.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa3:
                {
                    printf("xvftintrmh.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa4:
                {
                    printf("xvftintrpl.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa5:
                {
                    printf("xvftintrph.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa6:
                {
                    printf("xvftintrzl.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa7:
                {
                    printf("xvftintrzh.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa8:
                {
                    printf("xvftintrnel.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xa9:
                {
                    printf("xvftintrneh.l.s  %s, %s\n", xd, xj);
                    return;
                }
                case 0xb8:
                {
                    printf("xvexth.h.b   %s, %s\n", xd, xj);
                    return;
                }
                case 0xb9:
                {
                    printf("xvexth.w.h   %s, %s\n", xd, xj);
                    return;
                }
                case 0xba:
                {
                    printf("xvexth.d.w   %s, %s\n", xd, xj);
                    return;
                }
                case 0xbb:
                {
                    printf("xvexth.q.d   %s, %s\n", xd, xj);
                    return;
                }
                case 0xbc:
                {
                    printf("xvexth.hu.bu %s, %s\n", xd, xj);
                    return;
                }
                case 0xbd:
                {
                    printf("xvexth.wu.hu %s, %s\n", xd, xj);
                    return;
                }
                case 0xbe:
                {
                    printf("xvexth.du.wu %s, %s\n", xd, xj);
                    return;
                }
                case 0xbf:
                {
                    printf("xvexth.qu.du %s, %s\n", xd, xj);
                    return;
                }
                case 0xc0:
                {
                    printf("xvreplgr2vr.b  %s, %s\n", xd, rj);
                    return;
                }
                case 0xc1:
                {
                    printf("xvreplgr2vr.h  %s, %s\n", xd, rj);
                    return;
                }
                case 0xc2:
                {
                    printf("xvreplgr2vr.w  %s, %s\n", xd, rj);
                    return;
                }
                case 0xc3:
                {
                    printf("xvreplgr2vr.d  %s, %s\n", xd, rj);
                    return;
                }
                case 0xc4:
                {
                    printf("vext2xv.h.b  %s, %s\n", xd, xj);
                    return;
                }
                case 0xc5:
                {
                    printf("vext2xv.w.b  %s, %s\n", xd, xj);
                    return;
                }
                case 0xc6:
                {
                    printf("vext2xv.d.b  %s, %s\n", xd, xj);
                    return;
                }
                case 0xc7:
                {
                    printf("vext2xv.w.h  %s, %s\n", xd, xj);
                    return;
                }
                case 0xc8:
                {
                    printf("vext2xv.d.h  %s, %s\n", xd, xj);
                    return;
                }
                case 0xc9:
                {
                    printf("vext2xv.d.w  %s, %s\n", xd, xj);
                    return;
                }
                case 0xca:
                {
                    printf("vext2xv.hu.bu  %s, %s\n", xd, xj);
                    return;
                }
                case 0xcb:
                {
                    printf("vext2xv.wu.bu  %s, %s\n", xd, xj);
                    return;
                }
                case 0xcc:
                {
                    printf("vext2xv.du.bu  %s, %s\n", xd, xj);
                    return;
                }
                case 0xcd:
                {
                    printf("vext2xv.wu.hu  %s, %s\n", xd, xj);
                    return;
                }
                case 0xce:
                {
                    printf("vext2xv.du.hu  %s, %s\n", xd, xj);
                    return;
                }
                case 0xcf:
                {
                    printf("vext2xv.du.wu  %s, %s\n", xd, xj);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
            break;
        }
        case 0xa8:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvrotri.b    %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvrotri.h    %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvrotri.w    %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvrotri.d    %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xa9:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrlri.b    %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsrlri.h    %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrlri.w    %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrlri.d    %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xaa:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrari.b    %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsrari.h    %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrari.w    %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrari.d    %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xba:
        {
            opcode = code >> 12;
            if ((opcode & 0x3e) == 0x3c)
            {
                unsigned int ui3 = (code >> 10) & 0x7;
                printf("xvinsgr2vr.w %s, %s, %d\n", xd, rj, ui3);
                return;
            }
            else if ((opcode & 0x3f) == 0x3e)
            {
                unsigned int ui2 = (code >> 10) & 0x3;
                printf("xvinsgr2vr.d %s, %s, %d\n", xd, rj, ui2);
                return;
            }
            break;
        }
        case 0xbb:
        {
            opcode = code >> 12;
            if ((opcode & 0x3e) == 0x3c)
            {
                unsigned int ui3 = (code >> 10) & 0x7;
                printf("xvpickve2gr.w  %s, %s, %d\n", rd, xj, ui3);
                return;
            }
            else if ((opcode & 0x3f) == 0x3e)
            {
                unsigned int ui2 = (code >> 10) & 0x3;
                printf("xvpickve2gr.d  %s, %s, %d\n", rd, xj, ui2);
                return;
            }
            break;
        }
        case 0xbc:
        {
            opcode = code >> 12;
            if ((opcode & 0x3e) == 0x3c)
            {
                unsigned int ui3 = (code >> 10) & 0x7;
                printf("xvpickve2gr.wu  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x3f) == 0x3e)
            {
                unsigned int ui2 = (code >> 10) & 0x3;
                printf("xvpickve2gr.du  %s, %s, %d\n", rd, xj, ui2);
                return;
            }
            break;
        }
        case 0xbd:
        {
            opcode = code >> 11;
            if ((opcode & 0x78) == 0x70)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvrepl128vei.b  %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x7c) == 0x78)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvrepl128vei.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x7e) == 0x7c)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                assert((0 <= ui2) && (ui2 <= 3));
                printf("xvrepl128vei.w  %s, %s, %d\n", xd, xj, ui2);
                return;
            }
            else if ((opcode & 0x7f) == 0x7e)
            {
                unsigned short ui1 = (code >> 10) & 0x1;
                printf("xvrepl128vei.d  %s, %s, %d\n", xd, xj, ui1);
                return;
            }
            break;
        }
        case 0xbf:
        {
            opcode = code >> 12;
            if ((opcode & 0x3e) == 0x3c)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvinsve0.w   %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x3f) == 0x3e)
            {
                unsigned short ui2 = (code >> 10) & 0x3;
                assert((0 <= ui2) && (ui2 <= 3));
                printf("xvinsve0.d   %s, %s, %d\n", xd, xj, ui2);
                return;
            }
            break;
        }
        case 0xc0:
        {
            opcode = code >> 12;
            if ((opcode & 0x3e) == 0x3c)
            {
                unsigned int ui3 = (code >> 10) & 0x7;
                printf("xvpickve.w   %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x3f) == 0x3e)
            {
                unsigned int ui2 = (code >> 10) & 0x3;
                printf("xvpickve.d   %s, %s, %d\n", xd, xj, ui2);
                return;
            }
            break;
        }
        case 0xc1:
        {
            opcode = (code >> 10) & 0xff;
            switch (opcode)
            {
                case 0xc0:
                {
                    printf("xvreplve0.b  %s, %s\n", xd, xj);
                    return;
                }
                case 0xe0:
                {
                    printf("xvreplve0.h  %s, %s\n", xd, xj);
                    return;
                }
                case 0xf0:
                {
                    printf("xvreplve0.w  %s, %s\n", xd, xj);
                    return;
                }
                case 0xf8:
                {
                    printf("xvreplve0.d  %s, %s\n", xd, xj);
                    return;
                }
                case 0xfc:
                {
                    printf("xvreplve0.q  %s, %s\n", xd, xj);
                    return;
                }
                default:
                    printf("LOONGARCH illegal instruction: %08X\n", code);
                    return;
            }
        }
        case 0xc2:
        {
            opcode = code >> 10;
            if ((opcode & 0xf8) == 0x8)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsllwil.h.b %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xf0) == 0x10)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsllwil.w.h %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0xe0) == 0x20)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsllwil.d.w %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xff) == 0x40)
            {
                printf("xvextl.q.d   %s, %s\n", xd, xj);
                return;
            }
            break;
        }
        case 0xc3:
        {
            opcode = code >> 10;
            if ((opcode & 0xf8) == 0x8)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsllwil.hu.bu  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xf0) == 0x10)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsllwil.wu.hu  %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0xe0) == 0x20)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsllwil.du.wu  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xff) == 0x40)
            {
                printf("xvextl.qu.du %s, %s\n", xd, xj);
                return;
            }
            break;
        }
        case 0xc4:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvbitclri.b  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvbitclri.h  %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvbitclri.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvbitclri.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xc5:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvbitseti.b  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvbitseti.h  %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvbitseti.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvbitseti.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xc6:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvbitrevi.b  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvbitrevi.h  %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvbitrevi.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvbitrevi.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xc9:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsat.b      %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsat.h      %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsat.w      %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsat.d      %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xca:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsat.bu     %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsat.hu     %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsat.wu     %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsat.du     %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xcb:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvslli.b     %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvslli.h     %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvslli.w     %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvslli.d     %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xcc:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrli.b     %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsrli.h     %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrli.w     %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrli.d     %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xcd:
        {
            opcode = code >> 13;
            if ((opcode & 0x1f) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrai.b     %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0x1e) == 0x2)
            {
                unsigned short ui4 = (code >> 10) & 0xf;
                printf("xvsrai.h     %s, %s, %d\n", xd, xj, ui4);
                return;
            }
            else if ((opcode & 0x1c) == 0x4)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrai.w     %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0x18) == 0x8)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrai.d     %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            break;
        }
        case 0xd0:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrlni.b.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrlni.h.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrlni.w.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvsrlni.d.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd1:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrlrni.b.h %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrlrni.h.w %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrlrni.w.d %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvsrlrni.d.q %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd2:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrlni.b.h %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrlni.h.w %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrlni.w.d %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrlni.d.q %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd3:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrlni.bu.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrlni.hu.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrlni.wu.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrlni.du.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd4:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrlrni.b.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrlrni.h.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrlrni.w.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrlrni.d.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd5:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrlrni.bu.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrlrni.hu.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrlrni.wu.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrlrni.du.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd6:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrani.b.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrani.h.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrani.w.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvsrani.d.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd7:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvsrarni.b.h %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvsrarni.h.w %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvsrarni.w.d %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvsrarni.d.q %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd8:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrani.b.h %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrani.h.w %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrani.w.d %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrani.d.q %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xd9:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrani.bu.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrani.hu.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrani.wu.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrani.du.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xda:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrarni.b.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrarni.h.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrarni.w.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrarni.d.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xdb:
        {
            opcode = code >> 14;
            if ((opcode & 0xf) == 0x1)
            {
                unsigned short ui3 = (code >> 10) & 0x7;
                printf("xvssrarni.bu.h  %s, %s, %d\n", xd, xj, ui3);
                return;
            }
            else if ((opcode & 0xe) == 0x2)
            {
                unsigned short ui5 = (code >> 10) & 0x1f;
                printf("xvssrarni.hu.w  %s, %s, %d\n", xd, xj, ui5);
                return;
            }
            else if ((opcode & 0xc) == 0x4)
            {
                unsigned short ui6 = (code >> 10) & 0x3f;
                printf("xvssrarni.wu.d  %s, %s, %d\n", xd, xj, ui6);
                return;
            }
            else if ((opcode & 0x8) == 0x8)
            {
                unsigned short ui7 = (code >> 10) & 0x7f;
                printf("xvssrarni.du.q  %s, %s, %d\n", xd, xj, ui7);
                return;
            }
            break;
        }
        case 0xe0:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvextrins.d  %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe1:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvextrins.w  %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe2:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvextrins.h  %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe3:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvextrins.b  %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe4:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvshuf4i.b   %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe5:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvshuf4i.h   %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe6:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvshuf4i.w   %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xe7:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvshuf4i.d   %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf1:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvbitseli.b  %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf4:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvandi.b     %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf5:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvori.b      %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf6:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvxori.b     %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf7:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvnori.b     %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xf8:
        {
            short i13 = (code >> 5) & 0x1fff;
            printf("xvldi        %s, 0x%x\n", xd, i13);
            return;
        }
        case 0xf9:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvpermi.w    %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xfa:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvpermi.d    %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        case 0xfb:
        {
            unsigned short ui8 = (code >> 10) & 0xff;
            printf("xvpermi.q    %s, %s, %d\n", xd, xj, ui8);
            return;
        }
        default:
            printf("LOONGARCH illegal instruction: %08X\n", code);
            return;
    }
    printf("LOONGARCH illegal instruction: %08X\n", code);
    return;
#endif
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

/*****************************************************************************
 *
 * For LoongArch64, the `emitter::emitDispIns` only supports
 * the `DOTNET_JitDump`.
 */
void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    if (ig)
    {
        BYTE* addr = emitCodeBlock + offset + writeableOffset;

        int size = id->idCodeSize();
        while (size > 0)
        {
            emitDisInsName(*(code_t*)addr, addr, id);
            addr += 4;
            size -= 4;
        }
    }
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

#ifdef DEBUG
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
            printf("LOONGARCH64-Invalid ins for overflow check: %s\n", codeGen->genInsName(ins));
            assert(!"Invalid ins for overflow check");
        }
    }
#endif

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
        if (!needCheckOv)
        {
            emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());
        }
        else
        {
            assert(REG_R21 != dst->GetRegNum());
            assert(REG_R21 != src1->GetRegNum());
            assert(REG_R21 != src2->GetRegNum());
            assert(REG_RA != dst->GetRegNum());
            assert(REG_RA != src1->GetRegNum());
            assert(REG_RA != src2->GetRegNum());

            regNumber dstReg  = dst->GetRegNum();
            regNumber tmpReg1 = src1->GetRegNum();
            regNumber tmpReg2 = src2->GetRegNum();

            bool        isUnsignd = (dst->gtFlags & GTF_UNSIGNED) != 0;
            instruction ins2;
            if (attr == EA_8BYTE)
            {
                if (isUnsignd)
                {
                    ins2 = INS_mulh_du;
                }
                else
                {
                    ins2 = INS_mulh_d;
                }
            }
            else
            {
                if (isUnsignd)
                {
                    ins2 = INS_mulh_wu;
                }
                else
                {
                    ins2 = INS_mulh_w;
                }
            }
            emitIns_R_R_R(ins2, EA_8BYTE, REG_R21, tmpReg1, tmpReg2);

            // n * n bytes will store n bytes result
            emitIns_R_R_R(ins, attr, dstReg, tmpReg1, tmpReg2);

            if (isUnsignd)
            {
                tmpReg2 = REG_R0;
            }
            else
            {
                size_t imm = (EA_SIZE(attr) == EA_8BYTE) ? 63 : 31;
                emitIns_R_R_I(EA_SIZE(attr) == EA_8BYTE ? INS_srai_d : INS_srai_w, attr, REG_RA, dstReg, imm);
                tmpReg2 = REG_RA;
            }

            codeGen->genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21, nullptr, tmpReg2);
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
                    tempReg2 = dst->GetSingleTempReg();
                    assert(tempReg1 != tempReg2);
                    assert(tempReg1 != saveOperReg1);
                    assert(tempReg2 != saveOperReg2);

                    ssize_t ui6 = (attr == EA_4BYTE) ? 31 : 63;
                    if (dst->OperGet() == GT_ADD)
                    {
                        emitIns_R_R_I(INS_srli_d, attr, tempReg1, saveOperReg1, ui6);
                    }
                    else
                    {
                        emitIns_R_R_I(INS_srli_d, attr, tempReg1, dst->GetRegNum(), ui6);
                    }
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

#ifdef FEATURE_SIMD
// For the given 'size' and 'index' returns true if it specifies a valid index for a vector register of 'size'
bool emitter::isValidVectorIndex(emitAttr datasize, emitAttr elemsize, ssize_t index)
{
    assert(isValidVectorDatasize(datasize));
    assert(isValidVectorElemsize(elemsize));

    bool result = false;
    if (index >= 0)
    {
        if (datasize == EA_8BYTE)
        {
            switch (elemsize)
            {
                case EA_1BYTE:
                    result = (index < 8);
                    break;
                case EA_2BYTE:
                    result = (index < 4);
                    break;
                case EA_4BYTE:
                    result = (index < 2);
                    break;
                default:
                    unreached();
                    break;
            }
        }
        else if (datasize == EA_16BYTE)
        {
            switch (elemsize)
            {
                case EA_1BYTE:
                    result = (index < 16);
                    break;
                case EA_2BYTE:
                    result = (index < 8);
                    break;
                case EA_4BYTE:
                    result = (index < 4);
                    break;
                case EA_8BYTE:
                    result = (index < 2);
                    break;
                default:
                    unreached();
                    break;
            }
        }
        else if (datasize == EA_32BYTE)
        {
            switch (elemsize)
            {
                case EA_1BYTE:
                    result = (index < 32);
                    break;
                case EA_2BYTE:
                    result = (index < 16);
                    break;
                case EA_4BYTE:
                    result = (index < 8);
                    break;
                case EA_8BYTE:
                    result = (index < 4);
                    break;
                case EA_16BYTE:
                    result = (index < 2);
                    break;
                default:
                    unreached();
                    break;
            }
        }
    }
    return result;
}

void emitter::emitIns_S_R_SIMD12(regNumber reg, int varx, int offs)
{
    bool FPbased;
    int  base = emitComp->lvaFrameAddress(varx, &FPbased);
    int  imm  = base + offs + 8;

    emitIns_S_R(INS_fst_d, EA_8BYTE, reg, varx, offs);
    if (imm < 512)
    {
        assert(imm >= 0);
        regNumber reg1 = FPbased ? REG_FPBASE : REG_SPBASE;
        emitIns_R_R_I_I(INS_vstelm_w, EA_4BYTE, reg, reg1, (imm >> 2), 2);
    }
    else
    {
        emitIns_R_R_I(INS_xvpickve_w, EA_4BYTE, REG_SCRATCH_FLT, reg, 2);
        emitIns_S_R(INS_fst_s, EA_4BYTE, REG_SCRATCH_FLT, varx, offs + 8);
    }
}
#endif
#endif // defined(TARGET_LOONGARCH64)
