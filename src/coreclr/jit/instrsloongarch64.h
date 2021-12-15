// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

/*****************************************************************************
 *  Loongarch64 instructions for JIT compiler
 *
 *          id      -- the enum name for the instruction
 *          nm      -- textual name (for assembly dipslay)
 *          fp      -- floating point instruction
 *          ld/st/cmp   -- load/store/compare instruction
 *          fmt     -- encoding format used by this instruction
 *          e1      -- encoding 1
 *          e2      -- encoding 2
 *          e3      -- encoding 3
 *          e4      -- encoding 4
 *          e5      -- encoding 5
 *
******************************************************************************/

#if !defined(TARGET_LOONGARCH64)
#error Unexpected target type
#endif

#ifndef INSTS
#error INSTS must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is LOONGARCH64-specific                               */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitLoongarch64.cpp.

// clang-format off
INSTS(invalid, "INVALID", 0, 0, IF_NONE,  BAD_CODE)


INSTS(nop ,	"nop",	0,	0,	IF_LA,	0x03400000)

////INS_bceqz/INS_beq/INS_blt/INS_bltu must be even number.
INSTS(bceqz,	"bceqz",	0,	0,	IF_LA,	0x48000000)
INSTS(bcnez,	"bcnez",	0,	0,	IF_LA,	0x48000100)

INSTS(beq,	"beq",	0,	0,	IF_LA,	0x58000000)
INSTS(bne,	"bne",	0,	0,	IF_LA,	0x5c000000)

INSTS(blt,	"blt",	0,	0,	IF_LA,	0x60000000)
INSTS(bge,	"bge",	0,	0,	IF_LA,	0x64000000)
INSTS(bltu,	"bltu",	0,	0,	IF_LA,	0x68000000)
INSTS(bgeu,	"bgeu",	0,	0,	IF_LA,	0x6c000000)

////R_I.
INSTS(beqz,	"beqz",	0,	0,	IF_LA,	0x40000000)
INSTS(bnez,	"bnez",	0,	0,	IF_LA,	0x44000000)

////I.
INSTS(b,	"b",	0,	0,	IF_LA,	0x50000000)
INSTS(bl,	"bl",	0,	0,	IF_LA,	0x54000000)

////////////////////////////////////////////////
////NOTE:  Begin
////     the fllowing instructions will be used by emitter::emitInsMayWriteToGCReg().
////////////////////////////////////////////////
//    enum     name     FP LD/ST   FMT   ENCODE

////NOTE: mov must be the first one !!! more info to see emitter::emitInsMayWriteToGCReg().
INSTS(mov,     "mov",    0, 0, IF_LA, 0x03800000)
      //  mov     rd,rj
      //NOTE: On loongarch, usually it's name is move, but here for compatible using mov.
      //      In fact, mov is an alias commond, "ori rd,rj,0"
INSTS(dneg,            "dneg",         0, 0, IF_LA,  0x00118000)
        //dneg is a alias instruction.
        //sub_d rd, zero, rk
INSTS(neg,             "neg",          0, 0, IF_LA,  0x00110000)
        //neg is a alias instruction.
        //sub_w rd, zero, rk
INSTS(not,             "not",          0, 0, IF_LA,  0x00140000)
        //not is a alias instruction.
        //nor rd, rj, zero

//    enum:id          name     FP LD/ST   Formate     ENCODE
////R_R_R.
INSTS(add_w,	"add.w",	0,	0,	IF_LA,	0x00100000)
INSTS(add_d,	"add.d",	0,	0,	IF_LA,	0x00108000)
INSTS(sub_w,	"sub.w",	0,	0,	IF_LA,	0x00110000)
INSTS(sub_d,	"sub.d",	0,	0,	IF_LA,	0x00118000)

INSTS(and,	"and",	0,	0,	IF_LA,	0x00148000)
INSTS(or,	"or",	0,	0,	IF_LA,	0x00150000)
INSTS(nor,	"nor",	0,	0,	IF_LA,	0x00140000)
INSTS(xor,	"xor",	0,	0,	IF_LA,	0x00158000)
INSTS(andn,	"andn",	0,	0,	IF_LA,	0x00168000)
INSTS(orn,	"orn",	0,	0,	IF_LA,	0x00160000)

INSTS(mul_w,	"mul.w",	0,	0,	IF_LA,	0x001c0000)
INSTS(mul_d,	"mul.d",	0,	0,	IF_LA,	0x001d8000)
INSTS(mulh_w,	"mulh.w",	0,	0,	IF_LA,	0x001c8000)
INSTS(mulh_wu,	"mulh.wu",	0,	0,	IF_LA,	0x001d0000)
INSTS(mulh_d,	"mulh.d",	0,	0,	IF_LA,	0x001e0000)
INSTS(mulh_du,	"mulh.du",	0,	0,	IF_LA,	0x001e8000)
INSTS(mulw_d_w,	"mulw.d.w",	0,	0,	IF_LA,	0x001f0000)
INSTS(mulw_d_wu,	"mulw.d.wu",	0,	0,	IF_LA,	0x001f8000)
INSTS(div_w,	"div.w",	0,	0,	IF_LA,	0x00200000)
INSTS(div_wu,	"div.wu",	0,	0,	IF_LA,	0x00210000)
INSTS(div_d,	"div.d",	0,	0,	IF_LA,	0x00220000)
INSTS(div_du,	"div.du",	0,	0,	IF_LA,	0x00230000)
INSTS(mod_w,	"mod.w",	0,	0,	IF_LA,	0x00208000)
INSTS(mod_wu,	"mod.wu",	0,	0,	IF_LA,	0x00218000)
INSTS(mod_d,	"mod.d",	0,	0,	IF_LA,	0x00228000)
INSTS(mod_du,	"mod.du",	0,	0,	IF_LA,	0x00238000)

INSTS(sll_w,	"sll.w",	0,	0,	IF_LA,	0x00170000)
INSTS(srl_w,	"srl.w",	0,	0,	IF_LA,	0x00178000)
INSTS(sra_w,	"sra.w",	0,	0,	IF_LA,	0x00180000)
INSTS(rotr_w,	"rotr_w",	0,	0,	IF_LA,	0x001b0000)
INSTS(sll_d,	"sll.d",	0,	0,	IF_LA,	0x00188000)
INSTS(srl_d,	"srl.d",	0,	0,	IF_LA,	0x00190000)
INSTS(sra_d,	"sra.d",	0,	0,	IF_LA,	0x00198000)
INSTS(rotr_d,	"rotr.d",	0,	0,	IF_LA,	0x001b8000)

INSTS(maskeqz,	"maskeqz",	0,	0,	IF_LA,	0x00130000)
INSTS(masknez,	"masknez",	0,	0,	IF_LA,	0x00138000)

INSTS(slt,	"slt",	0,	0,	IF_LA,	0x00120000)
INSTS(sltu,	"sltu",	0,	0,	IF_LA,	0x00128000)

INSTS(amswap_w,	"amswap.w",	0,	0,	IF_LA,	0x38600000)
INSTS(amswap_d,	"amswap.d",	0,	0,	IF_LA,	0x38608000)
INSTS(amswap_db_w,	"amswap_db.w",	0,	0,	IF_LA,	0x38690000)
INSTS(amswap_db_d,	"amswap_db.d",	0,	0,	IF_LA,	0x38698000)
INSTS(amadd_w,	"amadd.w",	0,	0,	IF_LA,	0x38610000)
INSTS(amadd_d,	"amadd.d",	0,	0,	IF_LA,	0x38618000)
INSTS(amadd_db_w,	"amadd_db.w",	0,	0,	IF_LA,	0x386a0000)
INSTS(amadd_db_d,	"amadd_db.d",	0,	0,	IF_LA,	0x386a8000)
INSTS(amand_w,	"amand.w",	0,	0,	IF_LA,	0x38620000)
INSTS(amand_d,	"amand.d",	0,	0,	IF_LA,	0x38628000)
INSTS(amand_db_w,	"amand_db.w",	0,	0,	IF_LA,	0x386b0000)
INSTS(amand_db_d,	"amand_db.d",	0,	0,	IF_LA,	0x386b8000)
INSTS(amor_w,	"amor.w",	0,	0,	IF_LA,	0x38630000)
INSTS(amor_d,	"amor.d",	0,	0,	IF_LA,	0x38638000)
INSTS(amor_db_w,	"amor_db.w",	0,	0,	IF_LA,	0x386c0000)
INSTS(amor_db_d,	"amor_db.d",	0,	0,	IF_LA,	0x386c8000)
INSTS(amxor_w,	"amxor.w",	0,	0,	IF_LA,	0x38640000)
INSTS(amxor_d,	"amxor.d",	0,	0,	IF_LA,	0x38648000)
INSTS(amxor_db_w,	"amxor_db.w",	0,	0,	IF_LA,	0x386d0000)
INSTS(amxor_db_d,	"amxor_db.d",	0,	0,	IF_LA,	0x386d8000)
INSTS(ammax_w,	"ammax.w",	0,	0,	IF_LA,	0x38650000)
INSTS(ammax_d,	"ammax.d",	0,	0,	IF_LA,	0x38658000)
INSTS(ammax_db_w,	"ammax_db.w",	0,	0,	IF_LA,	0x386e0000)
INSTS(ammax_db_d,	"ammax_db.d",	0,	0,	IF_LA,	0x386e8000)
INSTS(ammin_w,	"ammin.w",	0,	0,	IF_LA,	0x38660000)
INSTS(ammin_d,	"ammin.d",	0,	0,	IF_LA,	0x38668000)
INSTS(ammin_db_w,	"ammin_db.w",	0,	0,	IF_LA,	0x386f0000)
INSTS(ammin_db_d,	"ammin_db.d",	0,	0,	IF_LA,	0x386f8000)
INSTS(ammax_wu,	"ammax.wu",	0,	0,	IF_LA,	0x38670000)
INSTS(ammax_du,	"ammax.du",	0,	0,	IF_LA,	0x38678000)
INSTS(ammax_db_wu,	"ammax_db.wu",	0,	0,	IF_LA,	0x38700000)
INSTS(ammax_db_du,	"ammax_db.du",	0,	0,	IF_LA,	0x38708000)
INSTS(ammin_wu,	"ammin.wu",	0,	0,	IF_LA,	0x38680000)
INSTS(ammin_du,	"ammin.du",	0,	0,	IF_LA,	0x38688000)
INSTS(ammin_db_wu,	"ammin_db.wu",	0,	0,	IF_LA,	0x38710000)
INSTS(ammin_db_du,	"ammin_db.du",	0,	0,	IF_LA,	0x38718000)

INSTS(crc_w_b_w,	"crc.w.b.w",	0,	0,	IF_LA,	0x00240000)
INSTS(crc_w_h_w,	"crc.w.h.w",	0,	0,	IF_LA,	0x00248000)
INSTS(crc_w_w_w,	"crc.w.w.w",	0,	0,	IF_LA,	0x00250000)
INSTS(crc_w_d_w,	"crc.w.d.w",	0,	0,	IF_LA,	0x00258000)
INSTS(crcc_w_b_w,	"crcc.w.b.w",	0,	0,	IF_LA,	0x00260000)
INSTS(crcc_w_h_w,	"crcc.w.h.w",	0,	0,	IF_LA,	0x00268000)
INSTS(crcc_w_w_w,	"crcc.w.w.w",	0,	0,	IF_LA,	0x00270000)
INSTS(crcc_w_d_w,	"crcc.w.d.w",	0,	0,	IF_LA,	0x00278000)

////R_R_R_I.
INSTS(alsl_w,	"alsl.w",	0,	0,	IF_LA,	0x00040000)
INSTS(alsl_wu,	"alsl.wu",	0,	0,	IF_LA,	0x00060000)
INSTS(alsl_d,	"alsl.d",	0,	0,	IF_LA,	0x002c0000)

INSTS(bytepick_w,	"bytepick.w",	0,	0,	IF_LA,	0x00080000)
INSTS(bytepick_d,	"bytepick.d",	0,	0,	IF_LA,	0x000c0000)

INSTS(fsel,	"fsel",	0,	0,	IF_LA,	0x0d000000)

////R_I.
INSTS(lu12i_w,	"lu12i.w",	0,	0,	IF_LA,	0x14000000)
INSTS(lu32i_d,	"lu32i.d",	0,	0,	IF_LA,	0x16000000)

INSTS(pcaddi,	"pcaddi",	0,	0,	IF_LA,	0x18000000)
INSTS(pcaddu12i,	"pcaddu12i",	0,	0,	IF_LA,	0x1c000000)
INSTS(pcalau12i,	"pcalau12i",	0,	0,	IF_LA,	0x1a000000)
INSTS(pcaddu18i,	"pcaddu18i",	0,	0,	IF_LA,	0x1e000000)

////R_R.
INSTS(ext_w_b,	"ext.w.b",	0,	0,	IF_LA,	0x00005c00)
INSTS(ext_w_h,	"ext.w.h",	0,	0,	IF_LA,	0x00005800)
INSTS(clo_w,	"clo.w",	0,	0,	IF_LA,	0x00001000)
INSTS(clz_w,	"clz.w",	0,	0,	IF_LA,	0x00001400)
INSTS(cto_w,	"cto.w",	0,	0,	IF_LA,	0x00001800)
INSTS(ctz_w,	"ctz.w",	0,	0,	IF_LA,	0x00001c00)
INSTS(clo_d,	"clo.d",	0,	0,	IF_LA,	0x00002000)
INSTS(clz_d,	"clz.d",	0,	0,	IF_LA,	0x00002400)
INSTS(cto_d,	"cto.d",	0,	0,	IF_LA,	0x00002800)
INSTS(ctz_d,	"ctz.d",	0,	0,	IF_LA,	0x00002c00)
INSTS(revb_2h,	"revb.2h",	0,	0,	IF_LA,	0x00003000)
INSTS(revb_4h,	"revb.4h",	0,	0,	IF_LA,	0x00003400)
INSTS(revb_2w,	"revb.2w",	0,	0,	IF_LA,	0x00003800)
INSTS(revb_d,	"revb.d",	0,	0,	IF_LA,	0x00003c00)
INSTS(revh_2w,	"revh.2w",	0,	0,	IF_LA,	0x00004000)
INSTS(revh_d,	"revh.d",	0,	0,	IF_LA,	0x00004400)
INSTS(bitrev_4b,	"bitrev.4b",	0,	0,	IF_LA,	0x00004800)
INSTS(bitrev_8b,	"bitrev.8b",	0,	0,	IF_LA,	0x00004c00)
INSTS(bitrev_w,	"bitrev.w",	0,	0,	IF_LA,	0x00005000)
INSTS(bitrev_d,	"bitrev.d",	0,	0,	IF_LA,	0x00005400)
INSTS(rdtimel_w,	"rdtimel.w",	0,	0,	IF_LA,	0x00006000)
INSTS(rdtimeh_w,	"rdtimeh.w",	0,	0,	IF_LA,	0x00006400)
INSTS(rdtime_d,	"rdtime.d",	0,	0,	IF_LA,	0x00006800)
INSTS(cpucfg,	"cpucfg",	0,	0,	IF_LA,	0x00006c00)

////R_R_I_I.
INSTS(bstrins_w,	"bstrins.w",	0,	0,	IF_LA,	0x00600000)
INSTS(bstrins_d,	"bstrins.d",	0,	0,	IF_LA,	0x00800000)
INSTS(bstrpick_w,	"bstrpick.w",	0,	0,	IF_LA,	0x00608000)
INSTS(bstrpick_d,	"bstrpick.d",	0,	0,	IF_LA,	0x00c00000)

////Load.
INSTS(ld_b,	"ld.b",	0,	LD,	IF_LA,	0x28000000)
INSTS(ld_h,	"ld.h",	0,	LD,	IF_LA,	0x28400000)
INSTS(ld_w,	"ld.w",	0,	LD,	IF_LA,	0x28800000)
INSTS(ld_d,	"ld.d",	0,	LD,	IF_LA,	0x28c00000)
INSTS(ld_bu,	"ld.bu",	0,	LD,	IF_LA,	0x2a000000)
INSTS(ld_hu,	"ld.hu",	0,	LD,	IF_LA,	0x2a400000)
INSTS(ld_wu,	"ld.wu",	0,	LD,	IF_LA,	0x2a800000)

INSTS(ldptr_w,	"ldptr.w",	0,	LD,	IF_LA,	0x24000000)
INSTS(ldptr_d,	"ldptr.d",	0,	LD,	IF_LA,	0x26000000)
INSTS(ll_w,	"ll.w",	0,	0,	IF_LA,	0x20000000)
INSTS(ll_d,	"ll.d",	0,	0,	IF_LA,	0x22000000)

INSTS(ldx_b,	"ldx.b",	0,	LD,	IF_LA,	0x38000000)
INSTS(ldx_h,	"ldx.h",	0,	LD,	IF_LA,	0x38040000)
INSTS(ldx_w,	"ldx.w",	0,	LD,	IF_LA,	0x38080000)
INSTS(ldx_d,	"ldx.d",	0,	LD,	IF_LA,	0x380c0000)
INSTS(ldx_bu,	"ldx.bu",	0,	LD,	IF_LA,	0x38200000)
INSTS(ldx_hu,	"ldx.hu",	0,	LD,	IF_LA,	0x38240000)
INSTS(ldx_wu,	"ldx.wu",	0,	LD,	IF_LA,	0x38280000)

INSTS(ldgt_b,	"ldgt.b",	0,	0,	IF_LA,	0x38780000)
INSTS(ldgt_h,	"ldgt.h",	0,	0,	IF_LA,	0x38788000)
INSTS(ldgt_w,	"ldgt.w",	0,	0,	IF_LA,	0x38790000)
INSTS(ldgt_d,	"ldgt.d",	0,	0,	IF_LA,	0x38798000)
INSTS(ldle_b,	"ldle.b",	0,	0,	IF_LA,	0x387a0000)
INSTS(ldle_h,	"ldle.h",	0,	0,	IF_LA,	0x387a8000)
INSTS(ldle_w,	"ldle.w",	0,	0,	IF_LA,	0x387b0000)
INSTS(ldle_d,	"ldle.d",	0,	0,	IF_LA,	0x387b8000)

////R_R_I.
INSTS(addi_w,	"addi.w",	0,	0,	IF_LA,	0x02800000)
INSTS(addi_d,	"addi.d",	0,	0,	IF_LA,	0x02c00000)
INSTS(lu52i_d,	"lu52i.d",	0,	0,	IF_LA,	0x03000000)
INSTS(slti,	"slti",	0,	0,	IF_LA,	0x02000000)

INSTS(sltui,	"sltui",	0,	0,	IF_LA,	0x02400000)
INSTS(andi,	"andi",	0,	0,	IF_LA,	0x03400000)
INSTS(ori,	"ori",	0,	0,	IF_LA,	0x03800000)
INSTS(xori,	"xori",	0,	0,	IF_LA,	0x03c00000)

INSTS(slli_w,	"slli.w",	0,	0,	IF_LA,	0x00408000)
INSTS(srli_w,	"srli.w",	0,	0,	IF_LA,	0x00448000)
INSTS(srai_w,	"srai.w",	0,	0,	IF_LA,	0x00488000)
INSTS(rotri_w,	"rotri.w",	0,	0,	IF_LA,	0x004c8000)
INSTS(slli_d,	"slli.d",	0,	0,	IF_LA,	0x00410000)
INSTS(srli_d,	"srli.d",	0,	0,	IF_LA,	0x00450000)
INSTS(srai_d,	"srai.d",	0,	0,	IF_LA,	0x00490000)
INSTS(rotri_d,	"rotri.d",	0,	0,	IF_LA,	0x004d0000)

INSTS(addu16i_d,	"addu16i.d",	0,	0,	IF_LA,	0x10000000)

INSTS(jirl,	"jirl",	0,	0,	IF_LA,	0x4c000000)

////NOTE: jirl must be the last one !!! more info to see emitter::emitInsMayWriteToGCReg().
////////////////////////////////////////////////
////NOTE:  End
////     the above instructions will be used by emitter::emitInsMayWriteToGCReg().
////////////////////////////////////////////////
////Store.
INSTS(st_b,	"st.b",	0,	ST,	IF_LA,	0x29000000)
INSTS(st_h,	"st.h",	0,	ST,	IF_LA,	0x29400000)
INSTS(st_w,	"st.w",	0,	ST,	IF_LA,	0x29800000)
INSTS(st_d,	"st.d",	0,	ST,	IF_LA,	0x29c00000)

INSTS(stptr_w,	"stptr.w",	0,	ST,	IF_LA,	0x25000000)
INSTS(stptr_d,	"stptr.d",	0,	ST,	IF_LA,	0x27000000)
INSTS(sc_w,	"sc.w",	0,	0,	IF_LA,	0x21000000)
INSTS(sc_d,	"sc.d",	0,	0,	IF_LA,	0x23000000)

INSTS(stx_b,	"stx.b",	0,	ST,	IF_LA,	0x38100000)
INSTS(stx_h,	"stx.h",	0,	ST,	IF_LA,	0x38140000)
INSTS(stx_w,	"stx.w",	0,	ST,	IF_LA,	0x38180000)
INSTS(stx_d,	"stx.d",	0,	ST,	IF_LA,	0x381c0000)
INSTS(stgt_b,	"stgt.b",	0,	0,	IF_LA,	0x387c0000)
INSTS(stgt_h,	"stgt.h",	0,	0,	IF_LA,	0x387c8000)
INSTS(stgt_w,	"stgt.w",	0,	0,	IF_LA,	0x387d0000)
INSTS(stgt_d,	"stgt.d",	0,	0,	IF_LA,	0x387d8000)
INSTS(stle_b,	"stle.b",	0,	0,	IF_LA,	0x387e0000)
INSTS(stle_h,	"stle.h",	0,	0,	IF_LA,	0x387e8000)
INSTS(stle_w,	"stle.w",	0,	0,	IF_LA,	0x387f0000)
INSTS(stle_d,	"stle.d",	0,	0,	IF_LA,	0x387f8000)

INSTS(dbar,    "dbar", 0,      0,      IF_LA,  0x38720000)
INSTS(ibar,    "ibar", 0,      0,      IF_LA,  0x38728000)

INSTS(syscall, "syscall",      0,      0,      IF_LA,  0x002b0000)
INSTS(break,   "break",        0,      0,      IF_LA,  0x002a0005)

INSTS(asrtle_d,        "asrtle.d",     0,      0,      IF_LA,  0x00010000)
INSTS(asrtgt_d,        "asrtgt.d",     0,      0,      IF_LA,  0x00018000)

INSTS(preld,   "preld",        0,      LD,     IF_LA,  0x2ac00000)
INSTS(preldx,  "preldx",       0,      LD,     IF_LA,  0x382c0000)

////Float instructions.
////R_R_R.
INSTS(fadd_s,	"fadd.s",	0,	0,	IF_LA,	0x01008000)
INSTS(fadd_d,	"fadd.d",	0,	0,	IF_LA,	0x01010000)
INSTS(fsub_s,	"fsub.s",	0,	0,	IF_LA,	0x01028000)
INSTS(fsub_d,	"fsub.d",	0,	0,	IF_LA,	0x01030000)
INSTS(fmul_s,	"fmul.s",	0,	0,	IF_LA,	0x01048000)
INSTS(fmul_d,	"fmul.d",	0,	0,	IF_LA,	0x01050000)
INSTS(fdiv_s,	"fdiv.s",	0,	0,	IF_LA,	0x01068000)
INSTS(fdiv_d,	"fdiv.d",	0,	0,	IF_LA,	0x01070000)

INSTS(fmax_s,	"fmax.s",	0,	0,	IF_LA,	0x01088000)
INSTS(fmax_d,	"fmax.d",	0,	0,	IF_LA,	0x01090000)
INSTS(fmin_s,	"fmin.s",	0,	0,	IF_LA,	0x010a8000)
INSTS(fmin_d,	"fmin.d",	0,	0,	IF_LA,	0x010b0000)
INSTS(fmaxa_s,	"fmaxa.s",	0,	0,	IF_LA,	0x010c8000)
INSTS(fmaxa_d,	"fmaxa.d",	0,	0,	IF_LA,	0x010d0000)
INSTS(fmina_s,	"fmina.s",	0,	0,	IF_LA,	0x010e8000)
INSTS(fmina_d,	"fmina.d",	0,	0,	IF_LA,	0x010f0000)

INSTS(fscaleb_s,	"fscaleb.s",	0,	0,	IF_LA,	0x01108000)
INSTS(fscaleb_d,	"fscaleb.d",	0,	0,	IF_LA,	0x01110000)

INSTS(fcopysign_s,	"fcopysign.s",	0,	0,	IF_LA,	0x01128000)
INSTS(fcopysign_d,	"fcopysign.d",	0,	0,	IF_LA,	0x01130000)

INSTS(fldx_s,	"fldx.s",	0,	LD,	IF_LA,	0x38300000)
INSTS(fldx_d,	"fldx.d",	0,	LD,	IF_LA,	0x38340000)
INSTS(fstx_s,	"fstx.s",	0,	ST,	IF_LA,	0x38380000)
INSTS(fstx_d,	"fstx.d",	0,	ST,	IF_LA,	0x383c0000)

INSTS(fldgt_s,	"fldgt.s",	0,	0,	IF_LA,	0x38740000)
INSTS(fldgt_d,	"fldgt.d",	0,	0,	IF_LA,	0x38748000)
INSTS(fldle_s,	"fldle.s",	0,	0,	IF_LA,	0x38750000)
INSTS(fldle_d,	"fldle.d",	0,	0,	IF_LA,	0x38758000)
INSTS(fstgt_s,	"fstgt.s",	0,	0,	IF_LA,	0x38760000)
INSTS(fstgt_d,	"fstgt.d",	0,	0,	IF_LA,	0x38768000)
INSTS(fstle_s,	"fstle.s",	0,	0,	IF_LA,	0x38770000)
INSTS(fstle_d,	"fstle.d",	0,	0,	IF_LA,	0x38778000)

////R_R_R_R.
INSTS(fmadd_s,	"fmadd.s",	0,	0,	IF_LA,	0x08100000)
INSTS(fmadd_d,	"fmadd.d",	0,	0,	IF_LA,	0x08200000)
INSTS(fmsub_s,	"fmsub.s",	0,	0,	IF_LA,	0x08500000)
INSTS(fmsub_d,	"fmsub.d",	0,	0,	IF_LA,	0x08600000)
INSTS(fnmadd_s,	"fnmadd.s",	0,	0,	IF_LA,	0x08900000)
INSTS(fnmadd_d,	"fnmadd.d",	0,	0,	IF_LA,	0x08a00000)
INSTS(fnmsub_s,	"fnmsub.s",	0,	0,	IF_LA,	0x08d00000)
INSTS(fnmsub_d,	"fnmsub.d",	0,	0,	IF_LA,	0x08e00000)

////R_R.
INSTS(fabs_s,	"fabs.s",	0,	0,	IF_LA,	0x01140400)
INSTS(fabs_d,	"fabs.d",	0,	0,	IF_LA,	0x01140800)
INSTS(fneg_s,	"fneg.s",	0,	0,	IF_LA,	0x01141400)
INSTS(fneg_d,	"fneg.d",	0,	0,	IF_LA,	0x01141800)

INSTS(fsqrt_s,	"fsqrt.s",	0,	0,	IF_LA,	0x01144400)
INSTS(fsqrt_d,	"fsqrt.d",	0,	0,	IF_LA,	0x01144800)
INSTS(frsqrt_s,	"frsqrt.s",	0,	0,	IF_LA,	0x01146400)
INSTS(frsqrt_d,	"frsqrt.d",	0,	0,	IF_LA,	0x01146800)
INSTS(frecip_s,	"frecip.s",	0,	0,	IF_LA,	0x01145400)
INSTS(frecip_d,	"frecip.d",	0,	0,	IF_LA,	0x01145800)
INSTS(flogb_s,	"flogb.s",	0,	0,	IF_LA,	0x01142400)
INSTS(flogb_d,	"flogb.d",	0,	0,	IF_LA,	0x01142800)
INSTS(fclass_s,	"fclass.s",	0,	0,	IF_LA,	0x01143400)
INSTS(fclass_d,	"fclass.d",	0,	0,	IF_LA,	0x01143800)

INSTS(fcvt_s_d,	"fcvt.s.d",	0,	0,	IF_LA,	0x01191800)
INSTS(fcvt_d_s,	"fcvt.d.s",	0,	0,	IF_LA,	0x01192400)
INSTS(ffint_s_w,	"ffint.s.w",	0,	0,	IF_LA,	0x011d1000)
INSTS(ffint_s_l,	"ffint.s.l",	0,	0,	IF_LA,	0x011d1800)
INSTS(ffint_d_w,	"ffint.d.w",	0,	0,	IF_LA,	0x011d2000)
INSTS(ffint_d_l,	"ffint.d.l",	0,	0,	IF_LA,	0x011d2800)
INSTS(ftint_w_s,	"ftint.w.s",	0,	0,	IF_LA,	0x011b0400)
INSTS(ftint_w_d,	"ftint.w.d",	0,	0,	IF_LA,	0x011b0800)
INSTS(ftint_l_s,	"ftint.l.s",	0,	0,	IF_LA,	0x011b2400)
INSTS(ftint_l_d,	"ftint.l.d",	0,	0,	IF_LA,	0x011b2800)
INSTS(ftintrm_w_s,	"ftintrm.w.s",	0,	0,	IF_LA,	0x011a0400)
INSTS(ftintrm_w_d,	"ftintrm.w.d",	0,	0,	IF_LA,	0x011a0800)
INSTS(ftintrm_l_s,	"ftintrm.l.s",	0,	0,	IF_LA,	0x011a2400)
INSTS(ftintrm_l_d,	"ftintrm.l.d",	0,	0,	IF_LA,	0x011a2800)
INSTS(ftintrp_w_s,	"ftintrp.w.s",	0,	0,	IF_LA,	0x011a4400)
INSTS(ftintrp_w_d,	"ftintrp.w.d",	0,	0,	IF_LA,	0x011a4800)
INSTS(ftintrp_l_s,	"ftintrp.l.s",	0,	0,	IF_LA,	0x011a6400)
INSTS(ftintrp_l_d,	"ftintrp.l.d",	0,	0,	IF_LA,	0x011a6800)
INSTS(ftintrz_w_s,	"ftintrz.w.s",	0,	0,	IF_LA,	0x011a8400)
INSTS(ftintrz_w_d,	"ftintrz.w.d",	0,	0,	IF_LA,	0x011a8800)
INSTS(ftintrz_l_s,	"ftintrz.l.s",	0,	0,	IF_LA,	0x011aa400)
INSTS(ftintrz_l_d,	"ftintrz.l.d",	0,	0,	IF_LA,	0x011aa800)
INSTS(ftintrne_w_s,	"ftintrne.w.s",	0,	0,	IF_LA,	0x011ac400)
INSTS(ftintrne_w_d,	"ftintrne.w.d",	0,	0,	IF_LA,	0x011ac800)
INSTS(ftintrne_l_s,	"ftintrne.l.s",	0,	0,	IF_LA,	0x011ae400)
INSTS(ftintrne_l_d,	"ftintrne.l.d",	0,	0,	IF_LA,	0x011ae800)
INSTS(frint_s,	"frint.s",	0,	0,	IF_LA,	0x011e4400)
INSTS(frint_d,	"frint.d",	0,	0,	IF_LA,	0x011e4800)

INSTS(fmov_s,	"fmov.s",	0,	0,	IF_LA,	0x01149400)
INSTS(fmov_d,	"fmov.d",	0,	0,	IF_LA,	0x01149800)

INSTS(movgr2fr_w,	"movgr2fr.w",	0,	0,	IF_LA,	0x0114a400)
INSTS(movgr2fr_d,	"movgr2fr.d",	0,	0,	IF_LA,	0x0114a800)
INSTS(movgr2frh_w,	"movgr2frh.w",	0,	0,	IF_LA,	0x0114ac00)
INSTS(movfr2gr_s,	"movfr2gr.s",	0,	0,	IF_LA,	0x0114b400)
INSTS(movfr2gr_d,	"movfr2gr.d",	0,	0,	IF_LA,	0x0114b800)
INSTS(movfrh2gr_s,	"movfrh2gr.s",	0,	0,	IF_LA,	0x0114bc00)

////
INSTS(movgr2fcsr,	"movgr2fcsr",	0,	0,	IF_LA,	0x0114c000)
INSTS(movfcsr2gr,	"movfcsr2gr",	0,	0,	IF_LA,	0x0114c800)
INSTS(movfr2cf,	"movfr2cf",	0,	0,	IF_LA,	0x0114d000)
INSTS(movcf2fr,	"movcf2fr",	0,	0,	IF_LA,	0x0114d400)
INSTS(movgr2cf,	"movgr2cf",	0,	0,	IF_LA,	0x0114d800)
INSTS(movcf2gr,	"movcf2gr",	0,	0,	IF_LA,	0x0114dc00)

////R_R_I.
INSTS(fcmp_caf_s,	"fcmp.caf.s",	0,	0,	IF_LA,	0x0c100000)
INSTS(fcmp_cun_s,	"fcmp.cun.s",	0,	0,	IF_LA,	0x0c140000)
INSTS(fcmp_ceq_s,	"fcmp.ceq.s",	0,	0,	IF_LA,	0x0c120000)
INSTS(fcmp_cueq_s,	"fcmp.cueq.s",	0,	0,	IF_LA,	0x0c160000)
INSTS(fcmp_clt_s,	"fcmp.clt.s",	0,	0,	IF_LA,	0x0c110000)
INSTS(fcmp_cult_s,	"fcmp.cult.s",	0,	0,	IF_LA,	0x0c150000)
INSTS(fcmp_cle_s,	"fcmp.cle.s",	0,	0,	IF_LA,	0x0c130000)
INSTS(fcmp_cule_s,	"fcmp.cule.s",	0,	0,	IF_LA,	0x0c170000)
INSTS(fcmp_cne_s,	"fcmp.cne.s",	0,	0,	IF_LA,	0x0c180000)
INSTS(fcmp_cor_s,	"fcmp.cor.s",	0,	0,	IF_LA,	0x0c1a0000)
INSTS(fcmp_cune_s,	"fcmp.cune.s",	0,	0,	IF_LA,	0x0c1c0000)

INSTS(fcmp_saf_d,	"fcmp.saf.d",	0,	0,	IF_LA,	0x0c208000)
INSTS(fcmp_sun_d,	"fcmp.sun.d",	0,	0,	IF_LA,	0x0c248000)
INSTS(fcmp_seq_d,	"fcmp.seq.d",	0,	0,	IF_LA,	0x0c228000)
INSTS(fcmp_sueq_d,	"fcmp.sueq.d",	0,	0,	IF_LA,	0x0c268000)
INSTS(fcmp_slt_d,	"fcmp.slt.d",	0,	0,	IF_LA,	0x0c218000)
INSTS(fcmp_sult_d,	"fcmp.sult.d",	0,	0,	IF_LA,	0x0c258000)
INSTS(fcmp_sle_d,	"fcmp.sle.d",	0,	0,	IF_LA,	0x0c238000)
INSTS(fcmp_sule_d,	"fcmp.sule.d",	0,	0,	IF_LA,	0x0c278000)
INSTS(fcmp_sne_d,	"fcmp.sne.d",	0,	0,	IF_LA,	0x0c288000)
INSTS(fcmp_sor_d,	"fcmp.sor.d",	0,	0,	IF_LA,	0x0c2a8000)
INSTS(fcmp_sune_d,	"fcmp.sune.d",	0,	0,	IF_LA,	0x0c2c8000)

INSTS(fcmp_caf_d,	"fcmp.caf.d",	0,	0,	IF_LA,	0x0c200000)
INSTS(fcmp_cun_d,	"fcmp.cun.d",	0,	0,	IF_LA,	0x0c240000)
INSTS(fcmp_ceq_d,	"fcmp.ceq.d",	0,	0,	IF_LA,	0x0c220000)
INSTS(fcmp_cueq_d,	"fcmp.cueq.d",	0,	0,	IF_LA,	0x0c260000)
INSTS(fcmp_clt_d,	"fcmp.clt.d",	0,	0,	IF_LA,	0x0c210000)
INSTS(fcmp_cult_d,	"fcmp.cult.d",	0,	0,	IF_LA,	0x0c250000)
INSTS(fcmp_cle_d,	"fcmp.cle.d",	0,	0,	IF_LA,	0x0c230000)
INSTS(fcmp_cule_d,	"fcmp.cule.d",	0,	0,	IF_LA,	0x0c270000)
INSTS(fcmp_cne_d,	"fcmp.cne.d",	0,	0,	IF_LA,	0x0c280000)
INSTS(fcmp_cor_d,	"fcmp.cor.d",	0,	0,	IF_LA,	0x0c2a0000)
INSTS(fcmp_cune_d,	"fcmp.cune.d",	0,	0,	IF_LA,	0x0c2c0000)

INSTS(fcmp_saf_s,	"fcmp.saf.s",	0,	0,	IF_LA,	0x0c108000)
INSTS(fcmp_sun_s,	"fcmp.sun.s",	0,	0,	IF_LA,	0x0c148000)
INSTS(fcmp_seq_s,	"fcmp.seq.s",	0,	0,	IF_LA,	0x0c128000)
INSTS(fcmp_sueq_s,	"fcmp.sueq.s",	0,	0,	IF_LA,	0x0c168000)
INSTS(fcmp_slt_s,	"fcmp.slt.s",	0,	0,	IF_LA,	0x0c118000)
INSTS(fcmp_sult_s,	"fcmp.sult.s",	0,	0,	IF_LA,	0x0c158000)
INSTS(fcmp_sle_s,	"fcmp.sle.s",	0,	0,	IF_LA,	0x0c138000)
INSTS(fcmp_sule_s,	"fcmp.sule.s",	0,	0,	IF_LA,	0x0c178000)
INSTS(fcmp_sne_s,	"fcmp.sne.s",	0,	0,	IF_LA,	0x0c188000)
INSTS(fcmp_sor_s,	"fcmp.sor.s",	0,	0,	IF_LA,	0x0c1a8000)
INSTS(fcmp_sune_s,	"fcmp.sune.s",	0,	0,	IF_LA,	0x0c1c8000)

////R_R_I.
INSTS(fld_s,	"fld.s",	0,	LD,	IF_LA,	0x2b000000)
INSTS(fld_d,	"fld.d",	0,	LD,	IF_LA,	0x2b800000)
INSTS(fst_s,	"fst.s",	0,	ST,	IF_LA,	0x2b400000)
INSTS(fst_d,	"fst.d",	0,	ST,	IF_LA,	0x2bc00000)


// clang-format on
/*****************************************************************************/
#undef INSTS
/*****************************************************************************/
