// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  RISCV64 instructions for JIT compiler
 *
 *          id          -- the enum name for the instruction
 *          nm          -- textual name (for assembly dipslay)
 *          ld/st/cmp   -- load/store/compare instruction
 *          encode      -- encoding 1
 *
 ******************************************************************************/

#if !defined(TARGET_RISCV64)
#error Unexpected target type
#endif

#ifndef INST
#error INST must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is RISCV64-specific                               */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitriscv64.cpp.

// clang-format off

// RV32I & RV64I
INST(invalid,       "INVALID",        0,    BAD_CODE)
INST(nop,           "nop",            0,    0x00000013)

//// R_R
INST(mov,           "mov",            0,    0x00000013)

////R_I
INST(lui,           "lui",            0,    0x00000037)
INST(auipc,         "auipc",          0,    0x00000017)

//// R_R_I
INST(addi,          "addi",           0,    0x00000013)
INST(slti,          "slti",           0,    0x00002013)
INST(sltiu,         "sltiu",          0,    0x00003013)
INST(xori,          "xori",           0,    0x00004013)
INST(ori,           "ori",            0,    0x00006013)
INST(andi,          "andi",           0,    0x00007013)
INST(slli,          "slli",           0,    0x00001013)
INST(srli,          "srli",           0,    0x00005013)
INST(srai,          "srai",           0,    0x40005013)

//// R_R_R
INST(add,           "add",            0,    0x00000033)
INST(sub,           "sub",            0,    0x40000033)
INST(sll,           "sll",            0,    0x00001033)
INST(slt,           "slt",            0,    0x00002033)
INST(sltu,          "sltu",           0,    0x00003033)
INST(xor,           "xor",            0,    0x00004033)
INST(srl,           "srl",            0,    0x00005033)
INST(sra,           "sra",            0,    0x40005033)
INST(or,            "or",             0,    0x00006033)
INST(and,           "and",            0,    0x00007033)

INST(fence,         "fence",          0,    0x0000000f)
INST(fence_i,       "fence.i",        0,    0x0000100f)

//// R_I_R
INST(csrrw,         "csrrw",          0,    0x00001073)
INST(csrrs,         "csrrs",          0,    0x00002073)
INST(csrrc,         "csrrc",          0,    0x00003073)

//// R_I_I
INST(csrrwi,        "csrrwi",         0,    0x00005073)
INST(csrrsi,        "csrrsi",         0,    0x00006073)
INST(csrrci,        "csrrci",         0,    0x00007073)

INST(ecall,         "ecall",          0,    0x00000073)
INST(ebreak,        "ebreak",         0,    0x00100073)
INST(uret,          "uret",           0,    0x00200073)
INST(sret,          "sret",           0,    0x10200073)
INST(mret,          "mret",           0,    0x30200073)
INST(wfi,           "wfi",            0,    0x10500073)
INST(sfence_vma,    "sfence.vma",     0,    0x12000073)

//// R_R_I
INST(lb,            "lb",             LD,   0x00000003)
INST(lh,            "lh",             LD,   0x00001003)
INST(lw,            "lw",             LD,   0x00002003)
INST(lbu,           "lbu",            LD,   0x00004003)
INST(lhu,           "lhu",            LD,   0x00005003)

INST(sb,            "sb",             ST,   0x00000023)
INST(sh,            "sh",             ST,   0x00001023)
INST(sw,            "sw",             ST,   0x00002023)

//// R_I
INST(jal,           "jal",            0,    0x0000006f)
INST(j,             "j",              0,    0x0000006f)
INST(beqz,          "beqz",           0,    0x00000063)
INST(bnez,          "bnez",           0,    0x00001063)

//// R_R_I
INST(jalr,          "jalr",           0,    0x00000067)
INST(beq,           "beq",            0,    0x00000063)
INST(bne,           "bne",            0,    0x00001063)
INST(blt,           "blt",            0,    0x00004063)
INST(bge,           "bge",            0,    0x00005063)
INST(bltu,          "bltu",           0,    0x00006063)
INST(bgeu,          "bgeu",           0,    0x00007063)

// RV64I
//// R_R_I
INST(addiw,         "addiw",          0,    0x0000001b)
INST(slliw,         "slliw",          0,    0x0000101b)
INST(srliw,         "srliw",          0,    0x0000501b)
INST(sraiw,         "sraiw",          0,    0x4000501b)

//// R_R_R
INST(addw,          "addw",           0,    0x0000003b)
INST(subw,          "subw",           0,    0x4000003b)
INST(sllw,          "sllw",           0,    0x0000103b)
INST(srlw,          "srlw",           0,    0x0000503b)
INST(sraw,          "sraw",           0,    0x4000503b)

//// R_R_I
INST(lwu,           "lwu",            LD,   0x00006003)
INST(ld,            "ld",             LD,   0x00003003)
INST(sd,            "sd",             ST,   0x00003023)


// RV32M & RV64M
//// R_R_R
INST(mul,           "mul",            0,   0x02000033)
INST(mulh,          "mulh",           0,   0x02001033)
INST(mulhsu,        "mulhsu",         0,   0x02002033)
INST(mulhu,         "mulhu",          0,   0x02003033)
INST(div,           "div",            0,   0x02004033)
INST(divu,          "divu",           0,   0x02005033)
INST(rem,           "rem",            0,   0x02006033)
INST(remu,          "remu",           0,   0x02007033)


// RV64M
//// R_R_R
INST(mulw,          "mulw",           0,   0x0200003b)
INST(divw,          "divw",           0,   0x0200403b)
INST(divuw,         "divuw",          0,   0x0200503b)
INST(remw,          "remw",           0,   0x0200603b)
INST(remuw,         "remuw",          0,   0x0200703b)

// RV32F & RV64D
//// R_R_R_R
INST(fmadd_s,       "fmadd.s",        0,   0x00000043)
INST(fmsub_s,       "fmsub.s",        0,   0x00000047)
INST(fnmsub_s,      "fnmsub.s",       0,   0x0000004b)
INST(fnmadd_s,      "fnmadd.s",       0,   0x0000004f)

//// R_R_R
INST(fadd_s,        "fadd.s",         0,   0x00000053)
INST(fsub_s,        "fsub.s",         0,   0x08000053)
INST(fmul_s,        "fmul.s",         0,   0x10000053)
INST(fdiv_s,        "fdiv.s",         0,   0x18000053)
INST(fsqrt_s,       "fsqrt.s",        0,   0x58000053)
INST(fsgnj_s,       "fsgnj.s",        0,   0x20000053)
INST(fsgnjn_s,      "fsgnjn.s",       0,   0x20001053)
INST(fsgnjx_s,      "fsgnjx.s",       0,   0x20002053)
INST(fmin_s,        "fmin.s",         0,   0x28000053)
INST(fmax_s,        "fmax.s",         0,   0x28001053)

//// R_R
INST(fcvt_w_s,      "fcvt.w.s",       0,   0xc0000053)
INST(fcvt_wu_s,     "fcvt.wu.s",      0,   0xc0100053)
INST(fmv_x_w,       "fmv.x.w",        0,   0xe0000053)

//// R_R_R
INST(feq_s,         "feq.s",          0,   0xa0002053)
INST(flt_s,         "flt.s",          0,   0xa0001053)
INST(fle_s,         "fle.s",          0,   0xa0000053)

//// R_R
INST(fclass_s,      "fclass.s",       0,   0xe0001053)
INST(fcvt_s_w,      "fcvt.s.w",       0,   0xd0000053)
INST(fcvt_s_wu,     "fcvt.s.wu",      0,   0xd0100053)
INST(fmv_w_x,       "fmv.w.x",        0,   0xf0000053)

//// R_R_R_R
INST(fmadd_d,       "fmadd.d",        0,   0x02000043)
INST(fmsub_d,       "fmsub.d",        0,   0x02000047)
INST(fnmsub_d,      "fnmsub.d",       0,   0x0200004b)
INST(fnmadd_d,      "fnmadd.d",       0,   0x0200004f)

//// R_R_R
INST(fadd_d,        "fadd.d",         0,   0x02000053)
INST(fsub_d,        "fsub.d",         0,   0x0a000053)
INST(fmul_d,        "fmul.d",         0,   0x12000053)
INST(fdiv_d,        "fdiv.d",         0,   0x1a000053)
INST(fsqrt_d,       "fsqrt.d",        0,   0x5a000053)
INST(fsgnj_d,       "fsgnj.d",        0,   0x22000053)
INST(fsgnjn_d,      "fsgnjn.d",       0,   0x22001053)
INST(fsgnjx_d,      "fsgnjx.d",       0,   0x22002053)
INST(fmin_d,        "fmin.d",         0,   0x2a000053)
INST(fmax_d,        "fmax.d",         0,   0x2a001053)

//// R_R
INST(fcvt_s_d,      "fcvt.s.d",       0,   0x40101053)
INST(fcvt_d_s,      "fcvt.d.s",       0,   0x42001053)

//// R_R_R
INST(feq_d,         "feq.d",          0,   0xa2002053)
INST(flt_d,         "flt.d",          0,   0xa2001053)
INST(fle_d,         "fle.d",          0,   0xa2000053)

//// R_R
INST(fclass_d,      "fclass.d",       0,   0xe2001053)
INST(fcvt_w_d,      "fcvt.w.d",       0,   0xc2001053)
INST(fcvt_wu_d,     "fcvt.wu.d",      0,   0xc2101053)
INST(fcvt_d_w,      "fcvt.d.w",       0,   0xd2001053)
INST(fcvt_d_wu,     "fcvt.d.wu",      0,   0xd2101053)

//// R_R_I
INST(flw,           "flw",            LD,  0x00002007)
INST(fsw,           "fsw",            ST,  0x00002027)
INST(fld,           "fld",            LD,  0x00003007)
INST(fsd,           "fsd",            ST,  0x00003027)

// RV64F
//// R_R
INST(fcvt_l_s,      "fcvt.l.s",       0,   0xc0200053)
INST(fcvt_lu_s,     "fcvt.lu.s",      0,   0xc0300053)
INST(fcvt_s_l,      "fcvt.s.l",       0,   0xd0200053)
INST(fcvt_s_lu,     "fcvt.s.lu",      0,   0xd0300053)

// RV64D
INST(fcvt_l_d,      "fcvt.l.d",       0,   0xc2200053)
INST(fcvt_lu_d,     "fcvt.lu.d",      0,   0xc2300053)
INST(fmv_x_d,       "fmv.x.d",        0,   0xe2000053)
INST(fcvt_d_l,      "fcvt.d.l",       0,   0xd2200053)
INST(fcvt_d_lu,     "fcvt.d.lu",      0,   0xd2300053)
INST(fmv_d_x,       "fmv.d.x",        0,   0xf2000053)

// RV32A + RV64A (R-type, R_R_R)
INST(lr_w,          "lr.w",           0,   0x1000202f) // funct5:00010
INST(lr_d,          "lr.d",           0,   0x1000302f) // funct5:00010
INST(sc_w,          "sc.w",           0,   0x1800202f) // funct5:00011
INST(sc_d,          "sc.d",           0,   0x1800302f) // funct5:00011
INST(amoswap_w,     "amoswap.w",      0,   0x0800202f) // funct5:00001
INST(amoswap_d,     "amoswap.d",      0,   0x0800302f) // funct5:00001
INST(amoadd_w,      "amoadd.w",       0,   0x0000202f) // funct5:00000
INST(amoadd_d,      "amoadd.d",       0,   0x0000302f) // funct5:00000
INST(amoxor_w,      "amoxor.w",       0,   0x2000202f) // funct5:00100
INST(amoxor_d,      "amoxor.d",       0,   0x2000302f) // funct5:00100
INST(amoand_w,      "amoand.w",       0,   0x6000202f) // funct5:01100
INST(amoand_d,      "amoand.d",       0,   0x6000302f) // funct5:01100
INST(amoor_w,       "amoor.w",        0,   0x4000202f) // funct5:01000
INST(amoor_d,       "amoor.d",        0,   0x4000302f) // funct5:01000
INST(amomin_w,      "amomin.w",       0,   0x8000202f) // funct5:10000
INST(amomin_d,      "amomin.d",       0,   0x8000302f) // funct5:10000
INST(amomax_w,      "amomax.w",       0,   0xa000202f) // funct5:10100
INST(amomax_d,      "amomax.d",       0,   0xa000302f) // funct5:10100
INST(amominu_w,     "amominu.w",      0,   0xc000202f) // funct5:11000
INST(amominu_d,     "amominu.d",      0,   0xc000302f) // funct5:11000
INST(amomaxu_w,     "amomaxu.w",      0,   0xe000202f) // funct5:11100
INST(amomaxu_d,     "amomaxu.d",      0,   0xe000302f) // funct5:11100
// clang-format on
/*****************************************************************************/
#undef INST
/*****************************************************************************/
