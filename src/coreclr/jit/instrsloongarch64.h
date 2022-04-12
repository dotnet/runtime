// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  LoongArch64 instructions for JIT compiler
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly dipslay)
 *          ld/st/cmp   -- load/store/compare instruction
 *          encode      -- encoding 1
 *
******************************************************************************/

#if !defined(TARGET_LOONGARCH64)
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is LOONGARCH64-specific                               */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitLoongarch64.cpp.

// clang-format off
INST(invalid,       "INVALID",        0,    BAD_CODE)
INST(nop ,          "nop",            0,    0x03400000)

                    // INS_bceqz/INS_beq/INS_blt/INS_bltu must be even number.
INST(bceqz,         "bceqz",          0,    0x48000000)
INST(bcnez,         "bcnez",          0,    0x48000100)

INST(beq,           "beq",            0,    0x58000000)
INST(bne,           "bne",            0,    0x5c000000)

INST(blt,           "blt",            0,    0x60000000)
INST(bge,           "bge",            0,    0x64000000)
INST(bltu,          "bltu",           0,    0x68000000)
INST(bgeu,          "bgeu",           0,    0x6c000000)

////R_I.
INST(beqz,          "beqz",           0,    0x40000000)
INST(bnez,          "bnez",           0,    0x44000000)

////I.
INST(b,             "b",              0,    0x50000000)
INST(bl,            "bl",             0,    0x54000000)

///////////////////////////////////////////////////////////////////////////////////////////
////NOTE:  Begin
////     the following instructions will be used by emitter::emitInsMayWriteToGCReg().
////////////////////////////////////////////////
//    enum     name     FP LD/ST   FMT   ENCODE
//
////NOTE: mov must be the first one !!! more info to see emitter::emitInsMayWriteToGCReg().
///////////////////////////////////////////////////////////////////////////////////////////
//  mov     rd,rj
//  In fact, mov is an alias instruction, "ori rd,rj,0"
INST(mov,           "mov",            0,    0x03800000)
                    //dneg is a alias instruction.
                    //sub_d rd, zero, rk
INST(dneg,          "dneg",           0,    0x00118000)
                    //neg is a alias instruction.
                    //sub_w rd, zero, rk
INST(neg,           "neg",            0,    0x00110000)
                    //not is a alias instruction.
                    //nor rd, rj, zero
INST(not,           "not",            0,    0x00140000)

//   enum:id        name             FP   LD/ST   Formate   ENCODE
////R_R_R.
INST(add_w,         "add.w",          0,    0x00100000)
INST(add_d,         "add.d",          0,    0x00108000)
INST(sub_w,         "sub.w",          0,    0x00110000)
INST(sub_d,         "sub.d",          0,    0x00118000)

INST(and,           "and",            0,    0x00148000)
INST(or,            "or",             0,    0x00150000)
INST(nor,           "nor",            0,    0x00140000)
INST(xor,           "xor",            0,    0x00158000)
INST(andn,          "andn",           0,    0x00168000)
INST(orn,           "orn",            0,    0x00160000)

INST(mul_w,         "mul.w",          0,    0x001c0000)
INST(mul_d,         "mul.d",          0,    0x001d8000)
INST(mulh_w,        "mulh.w",         0,    0x001c8000)
INST(mulh_wu,       "mulh.wu",        0,    0x001d0000)
INST(mulh_d,        "mulh.d",         0,    0x001e0000)
INST(mulh_du,       "mulh.du",        0,    0x001e8000)
INST(mulw_d_w,      "mulw.d.w",       0,    0x001f0000)
INST(mulw_d_wu,     "mulw.d.wu",      0,    0x001f8000)
INST(div_w,         "div.w",          0,    0x00200000)
INST(div_wu,        "div.wu",         0,    0x00210000)
INST(div_d,         "div.d",          0,    0x00220000)
INST(div_du,        "div.du",         0,    0x00230000)
INST(mod_w,         "mod.w",          0,    0x00208000)
INST(mod_wu,        "mod.wu",         0,    0x00218000)
INST(mod_d,         "mod.d",          0,    0x00228000)
INST(mod_du,        "mod.du",         0,    0x00238000)

INST(sll_w,         "sll.w",          0,    0x00170000)
INST(srl_w,         "srl.w",          0,    0x00178000)
INST(sra_w,         "sra.w",          0,    0x00180000)
INST(rotr_w,        "rotr_w",         0,    0x001b0000)
INST(sll_d,         "sll.d",          0,    0x00188000)
INST(srl_d,         "srl.d",          0,    0x00190000)
INST(sra_d,         "sra.d",          0,    0x00198000)
INST(rotr_d,        "rotr.d",         0,    0x001b8000)

INST(maskeqz,       "maskeqz",        0,    0x00130000)
INST(masknez,       "masknez",        0,    0x00138000)

INST(slt,           "slt",            0,    0x00120000)
INST(sltu,          "sltu",           0,    0x00128000)

INST(amswap_w,      "amswap.w",       0,    0x38600000)
INST(amswap_d,      "amswap.d",       0,    0x38608000)
INST(amswap_db_w,   "amswap_db.w",    0,    0x38690000)
INST(amswap_db_d,   "amswap_db.d",    0,    0x38698000)
INST(amadd_w,       "amadd.w",        0,    0x38610000)
INST(amadd_d,       "amadd.d",        0,    0x38618000)
INST(amadd_db_w,    "amadd_db.w",     0,    0x386a0000)
INST(amadd_db_d,    "amadd_db.d",     0,    0x386a8000)
INST(amand_w,       "amand.w",        0,    0x38620000)
INST(amand_d,       "amand.d",        0,    0x38628000)
INST(amand_db_w,    "amand_db.w",     0,    0x386b0000)
INST(amand_db_d,    "amand_db.d",     0,    0x386b8000)
INST(amor_w,        "amor.w",         0,    0x38630000)
INST(amor_d,        "amor.d",         0,    0x38638000)
INST(amor_db_w,     "amor_db.w",      0,    0x386c0000)
INST(amor_db_d,     "amor_db.d",      0,    0x386c8000)
INST(amxor_w,       "amxor.w",        0,    0x38640000)
INST(amxor_d,       "amxor.d",        0,    0x38648000)
INST(amxor_db_w,    "amxor_db.w",     0,    0x386d0000)
INST(amxor_db_d,    "amxor_db.d",     0,    0x386d8000)
INST(ammax_w,       "ammax.w",        0,    0x38650000)
INST(ammax_d,       "ammax.d",        0,    0x38658000)
INST(ammax_db_w,    "ammax_db.w",     0,    0x386e0000)
INST(ammax_db_d,    "ammax_db.d",     0,    0x386e8000)
INST(ammin_w,       "ammin.w",        0,    0x38660000)
INST(ammin_d,       "ammin.d",        0,    0x38668000)
INST(ammin_db_w,    "ammin_db.w",     0,    0x386f0000)
INST(ammin_db_d,    "ammin_db.d",     0,    0x386f8000)
INST(ammax_wu,      "ammax.wu",       0,    0x38670000)
INST(ammax_du,      "ammax.du",       0,    0x38678000)
INST(ammax_db_wu,   "ammax_db.wu",    0,    0x38700000)
INST(ammax_db_du,   "ammax_db.du",    0,    0x38708000)
INST(ammin_wu,      "ammin.wu",       0,    0x38680000)
INST(ammin_du,      "ammin.du",       0,    0x38688000)
INST(ammin_db_wu,   "ammin_db.wu",    0,    0x38710000)
INST(ammin_db_du,   "ammin_db.du",    0,    0x38718000)

INST(crc_w_b_w,     "crc.w.b.w",      0,    0x00240000)
INST(crc_w_h_w,     "crc.w.h.w",      0,    0x00248000)
INST(crc_w_w_w,     "crc.w.w.w",      0,    0x00250000)
INST(crc_w_d_w,     "crc.w.d.w",      0,    0x00258000)
INST(crcc_w_b_w,    "crcc.w.b.w",     0,    0x00260000)
INST(crcc_w_h_w,    "crcc.w.h.w",     0,    0x00268000)
INST(crcc_w_w_w,    "crcc.w.w.w",     0,    0x00270000)
INST(crcc_w_d_w,    "crcc.w.d.w",     0,    0x00278000)

////R_R_R_I.
INST(alsl_w,        "alsl.w",         0,    0x00040000)
INST(alsl_wu,       "alsl.wu",        0,    0x00060000)
INST(alsl_d,        "alsl.d",         0,    0x002c0000)

INST(bytepick_w,    "bytepick.w",     0,    0x00080000)
INST(bytepick_d,    "bytepick.d",     0,    0x000c0000)

INST(fsel,          "fsel",           0,    0x0d000000)

////R_I.
INST(lu12i_w,       "lu12i.w",        0,    0x14000000)
INST(lu32i_d,       "lu32i.d",        0,    0x16000000)

INST(pcaddi,        "pcaddi",         0,    0x18000000)
INST(pcaddu12i,     "pcaddu12i",      0,    0x1c000000)
INST(pcalau12i,     "pcalau12i",      0,    0x1a000000)
INST(pcaddu18i,     "pcaddu18i",      0,    0x1e000000)

////R_R.
INST(ext_w_b,       "ext.w.b",        0,    0x00005c00)
INST(ext_w_h,       "ext.w.h",        0,    0x00005800)
INST(clo_w,         "clo.w",          0,    0x00001000)
INST(clz_w,         "clz.w",          0,    0x00001400)
INST(cto_w,         "cto.w",          0,    0x00001800)
INST(ctz_w,         "ctz.w",          0,    0x00001c00)
INST(clo_d,         "clo.d",          0,    0x00002000)
INST(clz_d,         "clz.d",          0,    0x00002400)
INST(cto_d,         "cto.d",          0,    0x00002800)
INST(ctz_d,         "ctz.d",          0,    0x00002c00)
INST(revb_2h,       "revb.2h",        0,    0x00003000)
INST(revb_4h,       "revb.4h",        0,    0x00003400)
INST(revb_2w,       "revb.2w",        0,    0x00003800)
INST(revb_d,        "revb.d",         0,    0x00003c00)
INST(revh_2w,       "revh.2w",        0,    0x00004000)
INST(revh_d,        "revh.d",         0,    0x00004400)
INST(bitrev_4b,     "bitrev.4b",      0,    0x00004800)
INST(bitrev_8b,     "bitrev.8b",      0,    0x00004c00)
INST(bitrev_w,      "bitrev.w",       0,    0x00005000)
INST(bitrev_d,      "bitrev.d",       0,    0x00005400)
INST(rdtimel_w,     "rdtimel.w",      0,    0x00006000)
INST(rdtimeh_w,     "rdtimeh.w",      0,    0x00006400)
INST(rdtime_d,      "rdtime.d",       0,    0x00006800)
INST(cpucfg,        "cpucfg",         0,    0x00006c00)

////R_R_I_I.
INST(bstrins_w,     "bstrins.w",      0,    0x00600000)
INST(bstrins_d,     "bstrins.d",      0,    0x00800000)
INST(bstrpick_w,    "bstrpick.w",     0,    0x00608000)
INST(bstrpick_d,    "bstrpick.d",     0,    0x00c00000)

////Load.
INST(ld_b,          "ld.b",           LD,   0x28000000)
INST(ld_h,          "ld.h",           LD,   0x28400000)
INST(ld_w,          "ld.w",           LD,   0x28800000)
INST(ld_d,          "ld.d",           LD,   0x28c00000)
INST(ld_bu,         "ld.bu",          LD,   0x2a000000)
INST(ld_hu,         "ld.hu",          LD,   0x2a400000)
INST(ld_wu,         "ld.wu",          LD,   0x2a800000)

INST(ldptr_w,       "ldptr.w",        LD,   0x24000000)
INST(ldptr_d,       "ldptr.d",        LD,   0x26000000)
INST(ll_w,          "ll.w",           0,    0x20000000)
INST(ll_d,          "ll.d",           0,    0x22000000)

INST(ldx_b,         "ldx.b",          LD,   0x38000000)
INST(ldx_h,         "ldx.h",          LD,   0x38040000)
INST(ldx_w,         "ldx.w",          LD,   0x38080000)
INST(ldx_d,         "ldx.d",          LD,   0x380c0000)
INST(ldx_bu,        "ldx.bu",         LD,   0x38200000)
INST(ldx_hu,        "ldx.hu",         LD,   0x38240000)
INST(ldx_wu,        "ldx.wu",         LD,   0x38280000)

INST(ldgt_b,        "ldgt.b",         0,    0x38780000)
INST(ldgt_h,        "ldgt.h",         0,    0x38788000)
INST(ldgt_w,        "ldgt.w",         0,    0x38790000)
INST(ldgt_d,        "ldgt.d",         0,    0x38798000)
INST(ldle_b,        "ldle.b",         0,    0x387a0000)
INST(ldle_h,        "ldle.h",         0,    0x387a8000)
INST(ldle_w,        "ldle.w",         0,    0x387b0000)
INST(ldle_d,        "ldle.d",         0,    0x387b8000)

////R_R_I.
INST(addi_w,        "addi.w",         0,    0x02800000)
INST(addi_d,        "addi.d",         0,    0x02c00000)
INST(lu52i_d,       "lu52i.d",        0,    0x03000000)
INST(slti,          "slti",           0,    0x02000000)

INST(sltui,         "sltui",          0,    0x02400000)
INST(andi,          "andi",           0,    0x03400000)
INST(ori,           "ori",            0,    0x03800000)
INST(xori,          "xori",           0,    0x03c00000)

INST(slli_w,        "slli.w",         0,    0x00408000)
INST(srli_w,        "srli.w",         0,    0x00448000)
INST(srai_w,        "srai.w",         0,    0x00488000)
INST(rotri_w,       "rotri.w",        0,    0x004c8000)
INST(slli_d,        "slli.d",         0,    0x00410000)
INST(srli_d,        "srli.d",         0,    0x00450000)
INST(srai_d,        "srai.d",         0,    0x00490000)
INST(rotri_d,       "rotri.d",        0,    0x004d0000)

INST(addu16i_d,     "addu16i.d",      0,    0x10000000)

INST(jirl,          "jirl",           0,    0x4c000000)
////////////////////////////////////////////////////////////////////////////////////////////
////NOTE: jirl must be the last one !!! more info to see emitter::emitInsMayWriteToGCReg().
//
////NOTE:  End
////     the above instructions will be used by emitter::emitInsMayWriteToGCReg().
////////////////////////////////////////////////////////////////////////////////////////////

////Store.
INST(st_b,          "st.b",           ST,   0x29000000)
INST(st_h,          "st.h",           ST,   0x29400000)
INST(st_w,          "st.w",           ST,   0x29800000)
INST(st_d,          "st.d",           ST,   0x29c00000)

INST(stptr_w,       "stptr.w",        ST,   0x25000000)
INST(stptr_d,       "stptr.d",        ST,   0x27000000)
INST(sc_w,          "sc.w",           0,    0x21000000)
INST(sc_d,          "sc.d",           0,    0x23000000)

INST(stx_b,         "stx.b",          ST,   0x38100000)
INST(stx_h,         "stx.h",          ST,   0x38140000)
INST(stx_w,         "stx.w",          ST,   0x38180000)
INST(stx_d,         "stx.d",          ST,   0x381c0000)
INST(stgt_b,        "stgt.b",         0,    0x387c0000)
INST(stgt_h,        "stgt.h",         0,    0x387c8000)
INST(stgt_w,        "stgt.w",         0,    0x387d0000)
INST(stgt_d,        "stgt.d",         0,    0x387d8000)
INST(stle_b,        "stle.b",         0,    0x387e0000)
INST(stle_h,        "stle.h",         0,    0x387e8000)
INST(stle_w,        "stle.w",         0,    0x387f0000)
INST(stle_d,        "stle.d",         0,    0x387f8000)

INST(dbar,          "dbar",           0,    0x38720000)
INST(ibar,          "ibar",           0,    0x38728000)

INST(syscall,       "syscall",        0,    0x002b0000)
INST(break,         "break",          0,    0x002a0005)

INST(asrtle_d,      "asrtle.d",       0,    0x00010000)
INST(asrtgt_d,      "asrtgt.d",       0,    0x00018000)

INST(preld,         "preld",          LD,   0x2ac00000)
INST(preldx,        "preldx",         LD,   0x382c0000)

////Float instructions.
////R_R_R.
INST(fadd_s,        "fadd.s",         0,    0x01008000)
INST(fadd_d,        "fadd.d",         0,    0x01010000)
INST(fsub_s,        "fsub.s",         0,    0x01028000)
INST(fsub_d,        "fsub.d",         0,    0x01030000)
INST(fmul_s,        "fmul.s",         0,    0x01048000)
INST(fmul_d,        "fmul.d",         0,    0x01050000)
INST(fdiv_s,        "fdiv.s",         0,    0x01068000)
INST(fdiv_d,        "fdiv.d",         0,    0x01070000)

INST(fmax_s,        "fmax.s",         0,    0x01088000)
INST(fmax_d,        "fmax.d",         0,    0x01090000)
INST(fmin_s,        "fmin.s",         0,    0x010a8000)
INST(fmin_d,        "fmin.d",         0,    0x010b0000)
INST(fmaxa_s,       "fmaxa.s",        0,    0x010c8000)
INST(fmaxa_d,       "fmaxa.d",        0,    0x010d0000)
INST(fmina_s,       "fmina.s",        0,    0x010e8000)
INST(fmina_d,       "fmina.d",        0,    0x010f0000)

INST(fscaleb_s,     "fscaleb.s",      0,    0x01108000)
INST(fscaleb_d,     "fscaleb.d",      0,    0x01110000)

INST(fcopysign_s,   "fcopysign.s",    0,    0x01128000)
INST(fcopysign_d,   "fcopysign.d",    0,    0x01130000)

INST(fldx_s,        "fldx.s",         LD,   0x38300000)
INST(fldx_d,        "fldx.d",         LD,   0x38340000)
INST(fstx_s,        "fstx.s",         ST,   0x38380000)
INST(fstx_d,        "fstx.d",         ST,   0x383c0000)

INST(fldgt_s,       "fldgt.s",        0,    0x38740000)
INST(fldgt_d,       "fldgt.d",        0,    0x38748000)
INST(fldle_s,       "fldle.s",        0,    0x38750000)
INST(fldle_d,       "fldle.d",        0,    0x38758000)
INST(fstgt_s,       "fstgt.s",        0,    0x38760000)
INST(fstgt_d,       "fstgt.d",        0,    0x38768000)
INST(fstle_s,       "fstle.s",        0,    0x38770000)
INST(fstle_d,       "fstle.d",        0,    0x38778000)

////R_R_R_R.
INST(fmadd_s,       "fmadd.s",        0,    0x08100000)
INST(fmadd_d,       "fmadd.d",        0,    0x08200000)
INST(fmsub_s,       "fmsub.s",        0,    0x08500000)
INST(fmsub_d,       "fmsub.d",        0,    0x08600000)
INST(fnmadd_s,      "fnmadd.s",       0,    0x08900000)
INST(fnmadd_d,      "fnmadd.d",       0,    0x08a00000)
INST(fnmsub_s,      "fnmsub.s",       0,    0x08d00000)
INST(fnmsub_d,      "fnmsub.d",       0,    0x08e00000)

////R_R.
INST(fabs_s,        "fabs.s",         0,    0x01140400)
INST(fabs_d,        "fabs.d",         0,    0x01140800)
INST(fneg_s,        "fneg.s",         0,    0x01141400)
INST(fneg_d,        "fneg.d",         0,    0x01141800)

INST(fsqrt_s,       "fsqrt.s",        0,    0x01144400)
INST(fsqrt_d,       "fsqrt.d",        0,    0x01144800)
INST(frsqrt_s,      "frsqrt.s",       0,    0x01146400)
INST(frsqrt_d,      "frsqrt.d",       0,    0x01146800)
INST(frecip_s,      "frecip.s",       0,    0x01145400)
INST(frecip_d,      "frecip.d",       0,    0x01145800)
INST(flogb_s,       "flogb.s",        0,    0x01142400)
INST(flogb_d,       "flogb.d",        0,    0x01142800)
INST(fclass_s,      "fclass.s",       0,    0x01143400)
INST(fclass_d,      "fclass.d",       0,    0x01143800)

INST(fcvt_s_d,      "fcvt.s.d",       0,    0x01191800)
INST(fcvt_d_s,      "fcvt.d.s",       0,    0x01192400)
INST(ffint_s_w,     "ffint.s.w",      0,    0x011d1000)
INST(ffint_s_l,     "ffint.s.l",      0,    0x011d1800)
INST(ffint_d_w,     "ffint.d.w",      0,    0x011d2000)
INST(ffint_d_l,     "ffint.d.l",      0,    0x011d2800)
INST(ftint_w_s,     "ftint.w.s",      0,    0x011b0400)
INST(ftint_w_d,     "ftint.w.d",      0,    0x011b0800)
INST(ftint_l_s,     "ftint.l.s",      0,    0x011b2400)
INST(ftint_l_d,     "ftint.l.d",      0,    0x011b2800)
INST(ftintrm_w_s,   "ftintrm.w.s",    0,    0x011a0400)
INST(ftintrm_w_d,   "ftintrm.w.d",    0,    0x011a0800)
INST(ftintrm_l_s,   "ftintrm.l.s",    0,    0x011a2400)
INST(ftintrm_l_d,   "ftintrm.l.d",    0,    0x011a2800)
INST(ftintrp_w_s,   "ftintrp.w.s",    0,    0x011a4400)
INST(ftintrp_w_d,   "ftintrp.w.d",    0,    0x011a4800)
INST(ftintrp_l_s,   "ftintrp.l.s",    0,    0x011a6400)
INST(ftintrp_l_d,   "ftintrp.l.d",    0,    0x011a6800)
INST(ftintrz_w_s,   "ftintrz.w.s",    0,    0x011a8400)
INST(ftintrz_w_d,   "ftintrz.w.d",    0,    0x011a8800)
INST(ftintrz_l_s,   "ftintrz.l.s",    0,    0x011aa400)
INST(ftintrz_l_d,   "ftintrz.l.d",    0,    0x011aa800)
INST(ftintrne_w_s,  "ftintrne.w.s",   0,    0x011ac400)
INST(ftintrne_w_d,  "ftintrne.w.d",   0,    0x011ac800)
INST(ftintrne_l_s,  "ftintrne.l.s",   0,    0x011ae400)
INST(ftintrne_l_d,  "ftintrne.l.d",   0,    0x011ae800)
INST(frint_s,       "frint.s",        0,    0x011e4400)
INST(frint_d,       "frint.d",        0,    0x011e4800)

INST(fmov_s,        "fmov.s",         0,    0x01149400)
INST(fmov_d,        "fmov.d",         0,    0x01149800)

INST(movgr2fr_w,    "movgr2fr.w",     0,    0x0114a400)
INST(movgr2fr_d,    "movgr2fr.d",     0,    0x0114a800)
INST(movgr2frh_w,   "movgr2frh.w",    0,    0x0114ac00)
INST(movfr2gr_s,    "movfr2gr.s",     0,    0x0114b400)
INST(movfr2gr_d,    "movfr2gr.d",     0,    0x0114b800)
INST(movfrh2gr_s,   "movfrh2gr.s",    0,    0x0114bc00)

////
INST(movgr2fcsr,    "movgr2fcsr",     0,    0x0114c000)
INST(movfcsr2gr,    "movfcsr2gr",     0,    0x0114c800)
INST(movfr2cf,      "movfr2cf",       0,    0x0114d000)
INST(movcf2fr,      "movcf2fr",       0,    0x0114d400)
INST(movgr2cf,      "movgr2cf",       0,    0x0114d800)
INST(movcf2gr,      "movcf2gr",       0,    0x0114dc00)

////R_R_I.
INST(fcmp_caf_s,    "fcmp.caf.s",     0,    0x0c100000)
INST(fcmp_cun_s,    "fcmp.cun.s",     0,    0x0c140000)
INST(fcmp_ceq_s,    "fcmp.ceq.s",     0,    0x0c120000)
INST(fcmp_cueq_s,   "fcmp.cueq.s",    0,    0x0c160000)
INST(fcmp_clt_s,    "fcmp.clt.s",     0,    0x0c110000)
INST(fcmp_cult_s,   "fcmp.cult.s",    0,    0x0c150000)
INST(fcmp_cle_s,    "fcmp.cle.s",     0,    0x0c130000)
INST(fcmp_cule_s,   "fcmp.cule.s",    0,    0x0c170000)
INST(fcmp_cne_s,    "fcmp.cne.s",     0,    0x0c180000)
INST(fcmp_cor_s,    "fcmp.cor.s",     0,    0x0c1a0000)
INST(fcmp_cune_s,    "fcmp.cune.s",   0,    0x0c1c0000)

INST(fcmp_saf_d,    "fcmp.saf.d",     0,    0x0c208000)
INST(fcmp_sun_d,    "fcmp.sun.d",     0,    0x0c248000)
INST(fcmp_seq_d,    "fcmp.seq.d",     0,    0x0c228000)
INST(fcmp_sueq_d,   "fcmp.sueq.d",    0,    0x0c268000)
INST(fcmp_slt_d,    "fcmp.slt.d",     0,    0x0c218000)
INST(fcmp_sult_d,   "fcmp.sult.d",    0,    0x0c258000)
INST(fcmp_sle_d,    "fcmp.sle.d",     0,    0x0c238000)
INST(fcmp_sule_d,   "fcmp.sule.d",    0,    0x0c278000)
INST(fcmp_sne_d,    "fcmp.sne.d",     0,    0x0c288000)
INST(fcmp_sor_d,    "fcmp.sor.d",     0,    0x0c2a8000)
INST(fcmp_sune_d,   "fcmp.sune.d",    0,    0x0c2c8000)

INST(fcmp_caf_d,    "fcmp.caf.d",     0,    0x0c200000)
INST(fcmp_cun_d,    "fcmp.cun.d",     0,    0x0c240000)
INST(fcmp_ceq_d,    "fcmp.ceq.d",     0,    0x0c220000)
INST(fcmp_cueq_d,   "fcmp.cueq.d",    0,    0x0c260000)
INST(fcmp_clt_d,    "fcmp.clt.d",     0,    0x0c210000)
INST(fcmp_cult_d,   "fcmp.cult.d",    0,    0x0c250000)
INST(fcmp_cle_d,    "fcmp.cle.d",     0,    0x0c230000)
INST(fcmp_cule_d,   "fcmp.cule.d",    0,    0x0c270000)
INST(fcmp_cne_d,    "fcmp.cne.d",     0,    0x0c280000)
INST(fcmp_cor_d,    "fcmp.cor.d",     0,    0x0c2a0000)
INST(fcmp_cune_d,   "fcmp.cune.d",    0,    0x0c2c0000)

INST(fcmp_saf_s,    "fcmp.saf.s",     0,    0x0c108000)
INST(fcmp_sun_s,    "fcmp.sun.s",     0,    0x0c148000)
INST(fcmp_seq_s,    "fcmp.seq.s",     0,    0x0c128000)
INST(fcmp_sueq_s,   "fcmp.sueq.s",    0,    0x0c168000)
INST(fcmp_slt_s,    "fcmp.slt.s",     0,    0x0c118000)
INST(fcmp_sult_s,   "fcmp.sult.s",    0,    0x0c158000)
INST(fcmp_sle_s,    "fcmp.sle.s",     0,    0x0c138000)
INST(fcmp_sule_s,   "fcmp.sule.s",    0,    0x0c178000)
INST(fcmp_sne_s,    "fcmp.sne.s",     0,    0x0c188000)
INST(fcmp_sor_s,    "fcmp.sor.s",     0,    0x0c1a8000)
INST(fcmp_sune_s,   "fcmp.sune.s",    0,    0x0c1c8000)

////R_R_I.
INST(fld_s,         "fld.s",          LD,   0x2b000000)
INST(fld_d,         "fld.d",          LD,   0x2b800000)
INST(fst_s,         "fst.s",          ST,   0x2b400000)
INST(fst_d,         "fst.d",          ST,   0x2bc00000)

// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
