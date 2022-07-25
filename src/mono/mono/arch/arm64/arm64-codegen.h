/*
 * arm64-codegen.h: ARM64 code generation macros
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __ARM64_CODEGEN_H__
#define __ARM64_CODEGEN_H__

#include <glib.h>

enum {
	ARMREG_R0 = 0,
	ARMREG_R1 = 1,
	ARMREG_R2 = 2,
	ARMREG_R3 = 3,
	ARMREG_R4 = 4,
	ARMREG_R5 = 5,
	ARMREG_R6 = 6,
	ARMREG_R7 = 7,
	ARMREG_R8 = 8,
	ARMREG_R9 = 9,
	ARMREG_R10 = 10,
	ARMREG_R11 = 11,
	ARMREG_R12 = 12,
	ARMREG_R13 = 13,
	ARMREG_R14 = 14,
	ARMREG_R15 = 15,
	ARMREG_R16 = 16,
	ARMREG_R17 = 17,
	ARMREG_R18 = 18,
	ARMREG_R19 = 19,
	ARMREG_R20 = 20,
	ARMREG_R21 = 21,
	ARMREG_R22 = 22,
	ARMREG_R23 = 23,
	ARMREG_R24 = 24,
	ARMREG_R25 = 25,
	ARMREG_R26 = 26,
	ARMREG_R27 = 27,
	ARMREG_R28 = 28,
	ARMREG_R29 = 29,
	ARMREG_R30 = 30,
	ARMREG_SP = 31,
	ARMREG_RZR = 31,

	ARMREG_IP0 = ARMREG_R16,
	ARMREG_IP1 = ARMREG_R17,
	ARMREG_FP = ARMREG_R29,
	ARMREG_LR = ARMREG_R30
};

enum {
	ARMREG_D0 = 0,
	ARMREG_D1 = 1,
	ARMREG_D2 = 2,
	ARMREG_D3 = 3,
	ARMREG_D4 = 4,
	ARMREG_D5 = 5,
	ARMREG_D6 = 6,
	ARMREG_D7 = 7,
	ARMREG_D8 = 8,
	ARMREG_D9 = 9,
	ARMREG_D10 = 10,
	ARMREG_D11 = 11,
	ARMREG_D12 = 12,
	ARMREG_D13 = 13,
	ARMREG_D14 = 14,
	ARMREG_D15 = 15,
	ARMREG_D16 = 16,
	ARMREG_D17 = 17,
	ARMREG_D18 = 18,
	ARMREG_D19 = 19,
	ARMREG_D20 = 20,
	ARMREG_D21 = 21,
	ARMREG_D22 = 22,
	ARMREG_D23 = 23,
	ARMREG_D24 = 24,
	ARMREG_D25 = 25,
	ARMREG_D26 = 26,
	ARMREG_D27 = 27,
	ARMREG_D28 = 28,
	ARMREG_D29 = 29,
	ARMREG_D30 = 30,
	ARMREG_D31 = 31
};

typedef enum {
	ARMCOND_EQ = 0x0,          /* Equal; Z = 1 */
	ARMCOND_NE = 0x1,          /* Not equal, or unordered; Z = 0 */
	ARMCOND_CS = 0x2,          /* Carry set; C = 1 */
	ARMCOND_HS = ARMCOND_CS,   /* Unsigned higher or same; */
	ARMCOND_CC = 0x3,          /* Carry clear; C = 0 */
	ARMCOND_LO = ARMCOND_CC,   /* Unsigned lower */
	ARMCOND_MI = 0x4,          /* Negative; N = 1 */
	ARMCOND_PL = 0x5,          /* Positive or zero; N = 0 */
	ARMCOND_VS = 0x6,          /* Overflow; V = 1 */
	ARMCOND_VC = 0x7,          /* No overflow; V = 0 */
	ARMCOND_HI = 0x8,          /* Unsigned higher; C = 1 && Z = 0 */
	ARMCOND_LS = 0x9,          /* Unsigned lower or same; C = 0 || Z = 1 */
	ARMCOND_GE = 0xA,          /* Signed greater than or equal; N = V */
	ARMCOND_LT = 0xB,          /* Signed less than; N != V */
	ARMCOND_GT = 0xC,          /* Signed greater than; Z = 0 && N = V */
	ARMCOND_LE = 0xD,          /* Signed less than or equal; Z = 1 || N != V */
	ARMCOND_AL = 0xE,          /* Always */
	ARMCOND_NV = 0xF,          /* Never */
} ARMCond;

typedef enum {
	ARMSHIFT_LSL = 0x0,
	ARMSHIFT_LSR = 0x1,
	ARMSHIFT_ASR = 0x2
} ARMShift;

typedef enum {
	ARMSIZE_B = 0x0,
	ARMSIZE_H = 0x1,
	ARMSIZE_W = 0x2,
	ARMSIZE_X = 0x3
} ARMSize;

#define arm_emit(p, ins) do { *(guint32*)(p) = (ins); (p) += 4; } while (0)

/* Overwrite bits [offset,offset+nbits] with value */
static G_GNUC_UNUSED inline void
arm_set_ins_bits (void *p, int offset, int nbits, guint32 value)
{
	*(guint32*)p = (*(guint32*)p & ~(((1 << nbits) - 1) << offset)) | (value << offset);
}

/*
 * Naming conventions for codegen macros:
 * - 64 bit opcodes have an 'X' suffix
 * - 32 bit opcodes have a 'W' suffix
 * - the order of operands is the same as in assembly
 */

/*
 * http://infocenter.arm.com/help/index.jsp?topic=/com.arm.doc.ddi0487a/index.html
 */

/* Uncoditional branch (register) */

// 0b1101011 == 0x6b
#define arm_format_breg(p, opc, op2, op3, op4, rn) arm_emit ((p), (0x6b << 25) | ((opc) << 21) | ((op2) << 16) | ((op3) << 10) | ((rn) << 5) | ((op4) << 0))

// 0b0000 == 0x0, 0b11111 == 0x1f
#define arm_brx(p, reg) arm_format_breg ((p), 0x0, 0x1f, 0x0, 0x0, (reg))

// 0b0001 == 0x1
#define arm_blrx(p, reg) arm_format_breg ((p), 0x1, 0x1f, 0x0, 0x0, (reg))

//0b0010 == 0x2
#define arm_retx(p, reg) arm_format_breg ((p), 0x2, 0x1f, 0x0, 0x0, (reg))

/* Unconditional branch (immeditate) */

static G_GNUC_UNUSED inline gboolean
arm_is_bl_disp (void *code, void *target)
{
	gint64 disp = ((char*)(target) - (char*)(code)) / 4;

	return (disp > -(1 << 25)) && (disp < (1 << 25));
}

static G_GNUC_UNUSED inline unsigned int
arm_get_disp (void *p, void *target)
{
	unsigned int disp = ((char*)target - (char*)p) / 4;

	if (target)
		g_assert (arm_is_bl_disp (p, target));

	return (disp & 0x3ffffff);
}

// 0b00101 == 0x5
#define arm_b(p, target) do { if ((target)) g_assert (arm_is_bl_disp ((p), (target))); arm_emit (p, (0x0 << 31) | (0x5 << 26) | ((arm_get_disp ((p), (target)) << 0))); } while (0)

#define arm_bl(p, target) do { if ((target)) g_assert (arm_is_bl_disp ((p), (target))); arm_emit (p, (0x1 << 31) | (0x5 << 26) | ((arm_get_disp ((p), (target)) << 0))); } while (0)

/* Conditional branch */

static G_GNUC_UNUSED inline gboolean
arm_is_disp19 (void *code, void *target)
{
	gint64 disp = ((char*)(target) - (char*)(code)) / 4;

	return (disp > -(1 << 18)) && (disp < (1 << 18));
}

static G_GNUC_UNUSED inline unsigned int
arm_get_disp19 (void *p, void *target)
{
	unsigned int disp = ((char*)target - (char*)p) / 4;

	if (target)
		g_assert (arm_is_disp19 (p, target));

	return (disp & 0x7ffff);
}

// 0b0101010 == 0x2a
#define arm_format_condbr(p, o1, o0, cond, disp) arm_emit ((p), (0x2a << 25) | ((o1) << 24) | ((disp) << 5) | ((o0) << 4) | ((cond) << 0))
#define arm_get_bcc_cond(p) ((*(guint32*)p) & 0xf)

#define arm_bcc(p, cond, target) arm_format_condbr ((p), 0x0, 0x0, (cond), arm_get_disp19 ((p), (target)))

// 0b011010 == 0x1a
#define arm_format_cmpbr(p, sf, op, rt, target) arm_emit ((p), ((sf) << 31) | (0x1a << 25) | ((op) << 24) | (arm_get_disp19 ((p), (target)) << 5) | ((rt) << 0))

#define arm_set_cbz_target(p, target) arm_set_ins_bits (p, 5, 19, arm_get_disp19 ((p), (target)))

#define arm_cbzx(p, rt, target) arm_format_cmpbr ((p), 0x1, 0x0, (rt), (target))
#define arm_cbzw(p, rt, target) arm_format_cmpbr ((p), 0x0, 0x0, (rt), (target))

#define arm_cbnzx(p, rt, target) arm_format_cmpbr ((p), 0x1, 0x1, (rt), (target))
#define arm_cbnzw(p, rt, target) arm_format_cmpbr ((p), 0x0, 0x1, (rt), (target))

static G_GNUC_UNUSED inline unsigned int
arm_get_disp15 (void *p, void *target)
{
	unsigned int disp = ((char*)target - (char*)p) / 4;
	return (disp & 0x7fff);
}

// 0b011011 == 0x1b
#define arm_format_tbimm(p, op, rt, bit, target) arm_emit ((p), ((((bit) >> 5) & 1) << 31) | (0x1b << 25) | ((op) << 24) | (((bit) & 0x1f) << 19) | (arm_get_disp15 ((p), (target)) << 5) | ((rt) << 0))

#define arm_tbz(p, rt, bit, target) arm_format_tbimm ((p), 0x0, (rt), (bit), (target))
#define arm_tbnz(p, rt, bit, target) arm_format_tbimm ((p), 0x1, (rt), (bit), (target))

/* Memory access */

#define arm_is_pimm12_scaled(pimm,size) ((pimm) >= 0 && (pimm) / (size) <= 0xfff && ((pimm) % (size)) == 0)

static G_GNUC_UNUSED unsigned int
arm_encode_pimm12 (int pimm, int size)
{
	g_assert (arm_is_pimm12_scaled (pimm, size));
	return ((unsigned int)(pimm / size)) & 0xfff;
}

#define arm_is_strb_imm(pimm) arm_is_pimm12_scaled((pimm), 1)
#define arm_is_strh_imm(pimm) arm_is_pimm12_scaled((pimm), 2)
#define arm_is_strw_imm(pimm) arm_is_pimm12_scaled((pimm), 4)
#define arm_is_strx_imm(pimm) arm_is_pimm12_scaled((pimm), 8)

/* Load/Store register + scaled immediate */
/* No pre-index/post-index yet */
#define arm_format_mem_imm(p, size, opc, rt, rn, pimm, scale) arm_emit ((p), ((size) << 30) | (0x39 << 24) | ((opc) << 22) | (arm_encode_pimm12 ((pimm), (scale)) << 10) | ((rn) << 5) | ((rt) << 0))

/* C5.6.83 LDR (immediate) */
#define arm_ldrx(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_X, 0x1, (rt), (rn), (pimm), 8)
#define arm_ldrw(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_W, 0x1, (rt), (rn), (pimm), 4)
/* C5.6.86 LDRB (immediate) */
#define arm_ldrb(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_B, 0x1, (rt), (rn), (pimm), 1)
/* C5.6.88 LDRH (immediate) */
#define arm_ldrh(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_H, 0x1, (rt), (rn), (pimm), 2)
/* C5.6.90 LDRSB (immediate) */
#define arm_ldrsbx(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_B, 0x2, (rt), (rn), (pimm), 1)
#define arm_ldrsbw(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_B, 0x3, (rt), (rn), (pimm), 1)
/* C5.6.92 LDRSH (immediate) */
#define arm_ldrshx(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_H, 0x2, (rt), (rn), (pimm), 2)
#define arm_ldrshw(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_H, 0x3, (rt), (rn), (pimm), 2)
/* C5.6.94 LDRSW (immediate) */
#define arm_ldrswx(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_W, 0x2, (rt), (rn), (pimm), 4)

/* C5.6.178 STR (immediate) */
#define arm_strx(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_X, 0x0, (rt), (rn), (pimm), 8)
#define arm_strw(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_W, 0x0, (rt), (rn), (pimm), 4)
/* C5.6.182 STR (immediate) */
#define arm_strh(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_H, 0x0, (rt), (rn), (pimm), 2)
#define arm_strb(p, rt, rn, pimm) arm_format_mem_imm (p, ARMSIZE_B, 0x0, (rt), (rn), (pimm), 1)

/* C3.3.9 Load/store register (immediate post-indexed) */
static G_GNUC_UNUSED unsigned int
arm_encode_simm9 (int simm)
{
	g_assert (simm >= -256 && simm <= 255);
	return ((unsigned int)simm) & 0x1ff;
}

#define arm_format_mem_imm_post(p, size, V, opc, rt, rn, simm) arm_emit ((p), ((size) << 30) | (0x7 << 27) | ((V) << 26) | (0x0 << 24) | ((opc) << 22) | (arm_encode_simm9 ((simm)) << 12) | (0x1 << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_ldrx_post(p, rt, rn, simm) arm_format_mem_imm_post (p, ARMSIZE_X, 0x0, 0x1, (rt), (rn), (simm))
#define arm_ldrw_post(p, rt, rn, simm) arm_format_mem_imm_post (p, ARMSIZE_W, 0x0, 0x1, (rt), (rn), (simm))

#define arm_strx_post(p, rt, rn, simm) arm_format_mem_imm_post (p, ARMSIZE_X, 0x0, 0x0, (rt), (rn), (simm))
#define arm_strw_post(p, rt, rn, simm) arm_format_mem_imm_post (p, ARMSIZE_W, 0x0, 0x0, (rt), (rn), (simm))

/* C3.3.9 Load/store register (immediate pre-indexed) */
#define arm_format_mem_imm_pre(p, size, V, opc, rt, rn, simm) arm_emit ((p), ((size) << 30) | (0x7 << 27) | ((V) << 26) | (0x0 << 24) | ((opc) << 22) | (arm_encode_simm9 ((simm)) << 12) | (0x3 << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_ldrx_pre(p, rt, rn, simm) arm_format_mem_imm_pre (p, ARMSIZE_X, 0x0, 0x1, (rt), (rn), (simm))
#define arm_ldrw_pre(p, rt, rn, simm) arm_format_mem_imm_pre (p, ARMSIZE_W, 0x0, 0x1, (rt), (rn), (simm))

#define arm_strx_pre(p, rt, rn, simm) arm_format_mem_imm_pre (p, ARMSIZE_X, 0x0, 0x0, (rt), (rn), (simm))
#define arm_strw_pre(p, rt, rn, simm) arm_format_mem_imm_pre (p, ARMSIZE_W, 0x0, 0x0, (rt), (rn), (simm))

/* Load/Store register + register */
/* No extend/scale yet */
#define arm_format_mem_reg(p, size, opc, rt, rn, rm) arm_emit ((p), ((size) << 30) | (0x38 << 24) | ((opc) << 22) | (0x1 << 21) | ((rm) << 16) | (0x3 << 13) | (0 << 12) | (0x2 << 10) | ((rn) << 5) | ((rt) << 0))

/* C5.6.85 LDR (register) */
#define arm_ldrx_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_X, 0x1, (rt), (rn), (rm))
#define arm_ldrw_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_W, 0x1, (rt), (rn), (rm))
/* C5.6.87 LDRB (register) */
#define arm_ldrb_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_B, 0x1, (rt), (rn), (rm))
/* C5.6.88 LDRH (register) */
#define arm_ldrh_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_H, 0x1, (rt), (rn), (rm))
/* C5.6.91 LDRSB (register) */
#define arm_ldrsbx_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_B, 0x2, (rt), (rn), (rm))
#define arm_ldrsbw_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_B, 0x3, (rt), (rn), (rm))
/* C5.6.93 LDRSH (register) */
#define arm_ldrshx_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_H, 0x2, (rt), (rn), (rm))
#define arm_ldrshw_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_H, 0x3, (rt), (rn), (rm))
/* C5.6.96 LDRSW (register) */
#define arm_ldrswx_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_W, 0x2, (rt), (rn), (rm))

/* C5.6.179 STR (register) */
#define arm_strx_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_X, 0x0, (rt), (rn), (rm))
#define arm_strw_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_W, 0x0, (rt), (rn), (rm))
/* C5.6.181 STRB (register) */
#define arm_strb_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_B, 0x0, (rt), (rn), (rm))
/* C5.6.183 STRH (register) */
#define arm_strh_reg(p, rt, rn, rm) arm_format_mem_reg ((p), ARMSIZE_H, 0x0, (rt), (rn), (rm))

/* PC relative */

/* C5.6.84 LDR (literal) */

#define arm_get_ldr_lit_reg(p) (*(guint32*)(p) & 0x1f)

#define arm_ldrx_lit(p, rt, target) arm_emit ((p), (0x01 << 30) | (0x18 << 24) | (arm_get_disp19 ((p), (target)) << 5) | ((rt) << 0))
#define arm_ldrw_lit(p, rt, target) arm_emit ((p), (0x00 << 30) | (0x18 << 24) | (arm_get_disp19 ((p), (target)) << 5) | ((rt) << 0))
#define arm_ldrswx_lit(p, rt, target) arm_emit ((p), (0x2 << 30) | (0x18 << 24) | (arm_get_disp19 ((p), (target)) << 5) | ((rt) << 0))

/* Unscaled offset */
/* FIXME: Not yet */

/* Load/Store Pair */

static G_GNUC_UNUSED unsigned int
arm_encode_imm7 (int imm, int size)
{
	g_assert (imm / size >= -64 && imm / size <= 63 && (imm % size) == 0);
	return ((unsigned int)(imm / size)) & 0x7f;
}

#define arm_is_imm7_scaled(imm, size) ((imm) / (size) >= -64 && (imm) / (size) <= 63 && ((imm) % (size)) == 0)

#define arm_is_ldpx_imm(imm) arm_is_imm7_scaled ((imm), 8)

/* C3.3.14 */
#define arm_format_mem_p(p, size, opc, L, rt1, rt2, rn, imm) arm_emit ((p), (opc << 30) | (0x52 << 23) | ((L) << 22) | (arm_encode_imm7 (imm, size) << 15) | ((rt2) << 10) | ((rn) << 5) | ((rt1) << 0))

#define arm_ldpx(p, rt1, rt2, rn, imm) arm_format_mem_p ((p), 8, 0x2, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpw(p, rt1, rt2, rn, imm) arm_format_mem_p ((p), 4, 0x0, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpsw(p, rt1, rt2, rn, imm) arm_format_mem_p ((p), 4, 0x1, 1, (rt1), (rt2), (rn), (imm))
#define arm_stpx(p, rt1, rt2, rn, imm) arm_format_mem_p ((p), 8, 0x2, 0, (rt1), (rt2), (rn), (imm))
#define arm_stpw(p, rt1, rt2, rn, imm) arm_format_mem_p ((p), 4, 0x0, 0, (rt1), (rt2), (rn), (imm))

/* Load/Store Pair (Pre-indexed) */
/* C3.3.16 */
#define arm_format_mem_p_pre(p, size, opc, L, rt1, rt2, rn, imm) arm_emit ((p), (opc << 30) | (0x53 << 23) | ((L) << 22) | (arm_encode_imm7 (imm, size) << 15) | ((rt2) << 10) | ((rn) << 5) | ((rt1) << 0))

#define arm_ldpx_pre(p, rt1, rt2, rn, imm) arm_format_mem_p_pre ((p), 8, 0x2, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpw_pre(p, rt1, rt2, rn, imm) arm_format_mem_p_pre ((p), 4, 0x0, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpsw_pre(p, rt1, rt2, rn, imm) arm_format_mem_p_pre ((p), 4, 0x1, 1, (rt1), (rt2), (rn), (imm))
#define arm_stpx_pre(p, rt1, rt2, rn, imm) arm_format_mem_p_pre ((p), 8, 0x2, 0, (rt1), (rt2), (rn), (imm))
#define arm_stpw_pre(p, rt1, rt2, rn, imm) arm_format_mem_p_pre ((p), 4, 0x0, 0, (rt1), (rt2), (rn), (imm))

/* Not an official alias */
#define arm_pushpx (p, rt1, rt2) arm_LDPX_pre (p, rt1, rt2, ARMREG_RSP, -8)

/* Load/Store Pair (Post-indexed) */
/* C3.3.15 */
#define arm_format_mem_p_post(p, size, opc, L, rt1, rt2, rn, imm) arm_emit ((p), (opc << 30) | (0x51 << 23) | ((L) << 22) | (arm_encode_imm7 (imm, size) << 15) | ((rt2) << 10) | ((rn) << 5) | ((rt1) << 0))

#define arm_ldpx_post(p, rt1, rt2, rn, imm) arm_format_mem_p_post ((p), 8, 0x2, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpw_post(p, rt1, rt2, rn, imm) arm_format_mem_p_post ((p), 4, 0x0, 1, (rt1), (rt2), (rn), (imm))
#define arm_ldpsw_post(p, rt1, rt2, rn, imm) arm_format_mem_p_post ((p), 4, 0x1, 1, (rt1), (rt2), (rn), (imm))
#define arm_stpx_post(p, rt1, rt2, rn, imm) arm_format_mem_p_post ((p), 8, 0x2, 0, (rt1), (rt2), (rn), (imm))
#define arm_stpw_post(p, rt1, rt2, rn, imm) arm_format_mem_p_post ((p), 4, 0x0, 0, (rt1), (rt2), (rn), (imm))

/* Not an official alias */
#define arm_poppx (p, rt1, rt2) arm_ldpx_post (p, rt1, rt2, ARMREG_RSP, 8)

/* Load/Store Exclusive */
#define arm_format_ldxr(p, size, rt, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x1 << 22) | (0x0 << 21) | (0x1f << 16) | (0x0 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))
#define arm_format_ldxp(p, size, rt1, rt2, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x1 << 22) | (0x1 << 21) | (0x1f << 16) | (0x0 << 15) | ((rt2) << 10)| ((rn) << 5) | ((rt1) << 0))
#define arm_format_stxr(p, size, rs, rt, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x0 << 22) | (0x0 << 21) | ((rs) << 16) | (0x0 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))
#define arm_format_stxp(p, size, rs, rt1, rt2, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x0 << 22) | (0x1 << 21) | ((rs) << 16) | (0x0 << 15) | ((rt2) << 10)| ((rn) << 5) | ((rt1) << 0))

#define arm_ldxrx(p, rt, rn) arm_format_ldxr ((p), ARMSIZE_X, (rt), (rn))
#define arm_ldxrw(p, rt, rn) arm_format_ldxr ((p), ARMSIZE_W, (rt), (rn))
#define arm_ldxrh(p, rt, rn) arm_format_ldxr ((p), ARMSIZE_H, (rt), (rn))
#define arm_ldxrb(p, rt, rn) arm_format_ldxr ((p), ARMSIZE_B, (rt), (rn))
#define arm_ldxpx(p, rt1, rt2, rn) arm_format_ldxp ((p), ARMSIZE_X, (rt1), (rt2), (rn))
#define arm_ldxpw(p, rt1, rt2, rn) arm_format_ldxp ((p), ARMSIZE_W, (rt1), (rt2), (rn))
#define arm_stxrx(p, rs, rt, rn) arm_format_stxr ((p), ARMSIZE_X, (rs), (rt), (rn))
#define arm_stxrw(p, rs, rt, rn) arm_format_stxr ((p), ARMSIZE_W, (rs), (rt), (rn))
#define arm_stxrh(p, rs, rt, rn) arm_format_stxr ((p), ARMSIZE_H, (rs), (rt), (rn))
#define arm_stxrb(p, rs, rt, rn) arm_format_stxr ((p), ARMSIZE_B, (rs), (rt), (rn))
#define arm_stxpx(p, rs, rt1, rt2, rn) arm_format_stxp ((p), ARMSIZE_X, (rs), (rt1), (rt2), (rn))
#define arm_stxpw(p, rs, rt1, rt2, rn) arm_format_stxp ((p), ARMSIZE_W, (rs), (rt1), (rt2), (rn))

/* C5.6.73 LDAR: Load-Acquire Register */

#define arm_format_ldar(p, size, rt, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x1 << 23) | (0x1 << 22) | (0x0 << 21) | (0x1f << 16) | (0x1 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_ldarx(p, rt, rn) arm_format_ldar ((p), ARMSIZE_X, (rt), (rn))
#define arm_ldarw(p, rt, rn) arm_format_ldar ((p), ARMSIZE_W, (rt), (rn))
#define arm_ldarh(p, rt, rn) arm_format_ldar ((p), ARMSIZE_H, (rt), (rn))
#define arm_ldarb(p, rt, rn) arm_format_ldar ((p), ARMSIZE_B, (rt), (rn))

/* C5.6.169 STLR: Store-Release Register */

#define arm_format_stlr(p, size, rt, rn) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x1 << 23) | (0x0 << 22) | (0x0 << 21) | (0x1f << 16) | (0x1 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_stlrx(p, rn, rt) arm_format_stlr ((p), ARMSIZE_X, (rt), (rn))
#define arm_stlrw(p, rn, rt) arm_format_stlr ((p), ARMSIZE_W, (rt), (rn))
#define arm_stlrh(p, rn, rt) arm_format_stlr ((p), ARMSIZE_H, (rt), (rn))
#define arm_stlrb(p, rn, rt) arm_format_stlr ((p), ARMSIZE_B, (rt), (rn))

/* C5.6.77 LDAXR */
#define arm_format_ldaxr(p, size, rn, rt) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x1 << 22) | (0x0 << 21) | (0x1f << 16) | (0x1 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_ldaxrx(p, rt, rn) arm_format_ldaxr ((p), 0x3, (rn), (rt))
#define arm_ldaxrw(p, rt, rn) arm_format_ldaxr ((p), 0x2, (rn), (rt))

/* C5.6.173 STLXR */
#define arm_format_stlxr(p, size, rs, rn, rt) arm_emit ((p), ((size) << 30) | (0x8 << 24) | (0x0 << 23) | (0x0 << 22) | (0x0 << 21) | ((rs) << 16) | (0x1 << 15) | (0x1f << 10) | ((rn) << 5) | ((rt) << 0))

#define arm_stlxrx(p, rs, rt, rn) arm_format_stlxr ((p), 0x3, (rs), (rn), (rt))
#define arm_stlxrw(p, rs, rt, rn) arm_format_stlxr ((p), 0x2, (rs), (rn), (rt))

/* Load/Store SIMD&FP */

/* C6.3.285 STR (immediate, SIMD&FP) */
#define arm_format_strfp_imm(p, size, opc, rt, rn, pimm, scale) arm_emit ((p), ((size) << 30) | (0xf << 26) | (0x1 << 24) | ((opc) << 22) | (arm_encode_pimm12 ((pimm), (scale)) << 10) | ((rn) << 5) | ((rt) << 0))

/* Store double */
#define arm_strfpx(p, dt, xn, simm) arm_format_strfp_imm ((p), ARMSIZE_X, 0x0, (dt), (xn), (simm), 8)
/* Store single */
#define arm_strfpw(p, st, xn, simm) arm_format_strfp_imm ((p), ARMSIZE_W, 0x0, (st), (xn), (simm), 4)
/* Store 128 bit */
#define arm_strfpq(p, qt, xn, simm) arm_format_strfp_imm ((p), 0x0, 0x2, (qt), (xn), (simm), 16)

/* C6.3.166 LDR (immediate, SIMD&FP) */
#define arm_format_ldrfp_imm(p, size, opc, rt, rn, pimm, scale) arm_emit ((p), ((size) << 30) | (0xf << 26) | (0x1 << 24) | ((opc) << 22) | (arm_encode_pimm12 ((pimm), (scale)) << 10) | ((rn) << 5) | ((rt) << 0))

/* Load double */
#define arm_ldrfpx(p, dt, xn, simm) arm_format_ldrfp_imm ((p), ARMSIZE_X, 0x1, dt, xn, simm, 8)
/* Load single */
#define arm_ldrfpw(p, dt, xn, simm) arm_format_ldrfp_imm ((p), ARMSIZE_W, 0x1, dt, xn, simm, 4)
/* Load 128 bit */
#define arm_ldrfpq(p, qt, xn, simm) arm_format_ldrfp_imm ((p), 0, 0x3, qt, xn, simm, 16)

/* Arithmetic (immediate) */
static G_GNUC_UNUSED inline guint32
arm_encode_arith_imm (int imm, guint32 *shift)
{
	// FIXME:
	g_assert ((imm >= 0) && (imm < 0xfff));
	*shift = 0;
	return (guint32)imm;
}

// FIXME:
#define arm_is_arith_imm(imm)  (((imm) >= 0) && ((imm) < 0xfff))

#define arm_format_alu_imm(p, sf, op, S, rd, rn, imm) do { \
	guint32 _imm12, _shift; \
	_imm12 = arm_encode_arith_imm ((imm), &_shift); arm_emit ((p), ((sf) << 31) | ((op) << 30) | ((S) << 29) | (0x11 << 24) | ((_shift) << 22) | ((_imm12) << 10) | ((rn) << 5) | ((rd) << 0)); \
} while (0)

/* rd/rn can be SP for addx/subx */
#define arm_addx_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x1, 0x0, 0x0, (rd), (rn), (imm))
#define arm_addw_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x0, 0x0, 0x0, (rd), (rn), (imm))
#define arm_addsx_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x1, 0x0, 0x1, (rd), (rn), (imm))
#define arm_addsw_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x0, 0x0, 0x1, (rd), (rn), (imm))
#define arm_subx_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x1, 0x1, 0x0, (rd), (rn), (imm))
#define arm_subw_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x0, 0x1, 0x0, (rd), (rn), (imm))
#define arm_subsx_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x1, 0x1, 0x1, (rd), (rn), (imm))
#define arm_subsw_imm(p, rd, rn, imm) arm_format_alu_imm ((p), 0x0, 0x1, 0x1, (rd), (rn), (imm))

#define arm_cmpx_imm(p, rn, imm) arm_subsx_imm ((p), ARMREG_RZR, (rn), (imm))
#define arm_cmpw_imm(p, rn, imm) arm_subsw_imm ((p), ARMREG_RZR, (rn), (imm))
#define arm_cmnx_imm(p, rn, imm) arm_addsx_imm ((p), ARMREG_RZR, (rn), (imm))
#define arm_cmnw_imm(p, rn, imm) arm_addsw_imm ((p), ARMREG_RZR, (rn), (imm))

/* Logical (immediate) */

// FIXME: imm
#if 0
#define arm_format_and(p, sf, opc, rd, rn, imm) arm_emit ((p), ((sf) << 31) | ((opc) << 29) | (0x24 << 23) | ((0) << 22) | ((imm) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_andx_imm(p, rd, rn, imm) arm_format_and ((p), 0x1, 0x0, (rd), (rn), (imm))
#define arm_andw_imm(p, rd, rn, imm) arm_format_and ((p), 0x0, 0x0, (rd), (rn), (imm))
#define arm_andsx_imm(p, rd, rn, imm) arm_format_and ((p), 0x1, 0x3, (rd), (rn), (imm))
#define arm_andsw_imm(p, rd, rn, imm) arm_format_and ((p), 0x0, 0x3, (rd), (rn), (imm))
#define arm_eorx_imm(p, rd, rn, imm) arm_format_and ((p), 0x1, 0x2, (rd), (rn), (imm))
#define arm_eorw_imm(p, rd, rn, imm) arm_format_and ((p), 0x0, 0x2, (rd), (rn), (imm))
#define arm_orrx_imm(p, rd, rn, imm) arm_format_and ((p), 0x1, 0x1, (rd), (rn), (imm))
#define arm_orrw_imm(p, rd, rn, imm) arm_format_and ((p), 0x0, 0x1, (rd), (rn), (imm))

#define arm_tstx_imm(p, rn, imm) arm_andsx_imm ((p), ARMREG_RZR, (rn), (imm))
#define arm_tstw_imm(p, rn, imm) arm_andsw_imm ((p), ARMREG_RZR, (rn), (imm))
#endif

/* Move (wide immediate) */
#define arm_format_mov(p, sf, opc, hw, rd, imm16) arm_emit ((p), ((sf) << 31) | ((opc) << 29) | (0x25 << 23) | ((hw) << 21) | (((guint32)(imm16) & 0xffff) << 5) | ((rd) << 0))

#define arm_get_movzx_rd(p) ((*(guint32*)p) & 0x1f)

#define arm_movzx(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x1, 0x2, (shift) / 16, (rd), (imm)); } while (0)
#define arm_movzw(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x0, 0x2, (shift) / 16, (rd), (imm)); } while (0)
#define arm_movnx(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x1, 0x0, (shift) / 16, (rd), (imm)); } while (0)
#define arm_movnw(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x0, 0x0, (shift) / 16, (rd), (imm)); } while (0)
#define arm_movkx(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x1, 0x3, (shift) / 16, (rd), (imm)); } while (0)
#define arm_movkw(p, rd, imm, shift) do { g_assert ((shift) % 16 == 0); arm_format_mov ((p), 0x0, 0x3, (shift) / 16, (rd), (imm)); } while (0)

/* PC-relative address calculation */
#define arm_format_adrp(p, op, rd, target) do { guint64 imm1 = (guint64)(target); guint64 imm2 = (guint64)(p); int _imm = imm1 - imm2; arm_emit ((p), ((op) << 31) | (((_imm) & 0x3) << 29) | (0x10 << 24) | (((_imm >> 2) & 0x7ffff) << 5) | ((rd) << 0)); } while (0)

#define arm_adrpx(p, rd, target) arm_format_adrp ((p), 0x1, (rd), (target))
#define arm_adrx(p, rd, target) arm_format_adrp ((p), 0x0, (rd), (target))

/* Bitfield move */
#define arm_format_bfm(p, sf, opc, N, immr, imms, rn, rd) arm_emit ((p), ((sf) << 31) | ((opc) << 29) | (0x26 << 23) | ((N) << 22) | ((N) << 22) | ((immr) << 16) | ((imms) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_bfmx(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x1, 0x1, 0x1, (immr), (imms), (rn), (rd))
#define arm_bfmw(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x0, 0x1, 0x0, (immr), (imms), (rn), (rd))
#define arm_sbfmx(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x1, 0x0, 0x1, (immr), (imms), (rn), (rd))
#define arm_sbfmw(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x0, 0x0, 0x0, (immr), (imms), (rn), (rd))
#define arm_ubfmx(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x1, 0x2, 0x1, (immr), (imms), (rn), (rd))
#define arm_ubfmw(p, rd, rn, immr, imms) arm_format_bfm ((p), 0x0, 0x2, 0x0, (immr), (imms), (rn), (rd))

/* Sign extend and Zero-extend */
#define arm_sxtbx(p, rd, rn) arm_sbfmx ((p), (rd), (rn), 0, 7)
#define arm_sxtbw(p, rd, rn) arm_sbfmw ((p), (rd), (rn), 0, 7)
#define arm_sxthx(p, rd, rn) arm_sbfmx ((p), (rd), (rn), 0, 15)
#define arm_sxthw(p, rd, rn) arm_sbfmw ((p), (rd), (rn), 0, 15)
#define arm_sxtwx(p, rd, rn) arm_sbfmx ((p), (rd), (rn), 0, 31)
#define arm_uxtbx(p, rd, rn) arm_ubfmx ((p), (rd), (rn), 0, 7)
#define arm_uxtbw(p, rd, rn) arm_ubfmw ((p), (rd), (rn), 0, 7)
#define arm_uxthx(p, rd, rn) arm_ubfmx ((p), (rd), (rn), 0, 15)
#define arm_uxthw(p, rd, rn) arm_ubfmw ((p), (rd), (rn), 0, 15)

/* Extract register */
#define arm_format_extr(p, sf, N, rd, rn, rm, imms) arm_emit ((p), ((sf) << 31) | (0x27 << 23) | ((N) << 22) | (0x0 << 21) | ((rm) << 16) | ((imms) << 10) | ((rn) << 5) | ((rd) << 0))
#define arm_extrx(p, rd, rn, rm, lsb) arm_format_extr ((p), 0x1, 0x1, (rd), (rn), (rm), (lsb))
#define arm_extrw(p, rd, rn, rm, lsb) arm_format_extr ((p), 0x0, 0x0, (rd), (rn), (rm), (lsb))

/* Shift (immediate) */
#define arm_asrx(p, rd, rn, shift) arm_sbfmx ((p), (rd), (rn), (shift), 63)
#define arm_asrw(p, rd, rn, shift) arm_sbfmw ((p), (rd), (rn), (shift), 31)
#define arm_lslx(p, rd, rn, shift) arm_ubfmx ((p), (rd), (rn), 64 - ((shift) % 64), 63 - ((shift) % 64))
#define arm_lslw(p, rd, rn, shift) arm_ubfmw ((p), (rd), (rn), 32 - ((shift) % 32), 31 - ((shift) % 32))
#define arm_lsrx(p, rd, rn, shift) arm_ubfmx ((p), (rd), (rn), shift, 63)
#define arm_lsrw(p, rd, rn, shift) arm_ubfmw ((p), (rd), (rn), shift, 31)
#define arm_rorx(p, rd, rs, shift) arm_extrx ((p), (rd), (rs), (rs), (shift))
#define arm_rorw(p, rd, rs, shift) arm_extrw ((p), (rd), (rs), (rs), (shift))

/* Arithmetic (shifted register) */
#define arm_format_alu_shift(p, sf, op, S, rd, rn, rm, shift, imm6) arm_emit ((p), ((sf) << 31) | ((op) << 30) | ((S) << 29) | (0xb << 24) | ((shift) << 22) | (0x0 << 21) | ((rm) << 16) | ((imm6) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_addx_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x1, 0x0, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_addw_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x0, 0x0, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_addsx_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x1, 0x0, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_addsw_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x0, 0x0, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_subx_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x1, 0x1, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_subw_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x0, 0x1, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_subsx_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x1, 0x1, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_subsw_shift(p, rd, rn, rm, shift_type, amount) arm_format_alu_shift ((p), 0x0, 0x1, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_cmnx_shift(p, rn, rm, shift_type, amount) arm_addsx_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
#define arm_cmnw_shift(p, rn, rm, shift_type, amount) arm_addsw_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
#define arm_cmpx_shift(p, rn, rm, shift_type, amount) arm_subsx_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
#define arm_cmpw_shift(p, rn, rm, shift_type, amount) arm_subsw_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
#define arm_negx_shift(p, rd, rm, shift_type, amount) arm_subx_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))
#define arm_negw_shift(p, rd, rm, shift_type, amount) arm_subw_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))
#define arm_negsx_shift(p, rd, rm, shift_type, amount) arm_subsx_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))
#define arm_negsw_shift(p, rd, rm, shift_type, amount) arm_subsw_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))

#define arm_addx(p, rd, rn, rm) arm_addx_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_addw(p, rd, rn, rm) arm_addw_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_subx(p, rd, rn, rm) arm_subx_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_subw(p, rd, rn, rm) arm_subw_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_addsx(p, rd, rn, rm) arm_addsx_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_addsw(p, rd, rn, rm) arm_addsw_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_subsx(p, rd, rn, rm) arm_subsx_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_subsw(p, rd, rn, rm) arm_subsw_shift ((p), (rd), (rn), (rm), 0, 0)
#define arm_cmpx(p, rd, rn) arm_cmpx_shift ((p), (rd), (rn), 0, 0)
#define arm_cmpw(p, rd, rn) arm_cmpw_shift ((p), (rd), (rn), 0, 0)
#define arm_negx(p, rd, rn) arm_negx_shift ((p), (rd), (rn), 0, 0)
#define arm_negw(p, rd, rn) arm_negw_shift ((p), (rd), (rn), 0, 0)

/* Arithmetic with carry */
#define arm_format_adc(p, sf, op, S, rd, rn, rm) arm_emit ((p), ((sf) << 31) | ((op) << 30) | ((S) << 29) | (0xd0 << 21) | ((rm) << 16) | (0x0 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_adcx(p, rd, rn, rm) arm_format_adc ((p), 0x1, 0x0, 0x0, (rd), (rn), (rm))
#define arm_adcw(p, rd, rn, rm) arm_format_adc ((p), 0x0, 0x0, 0x0, (rd), (rn), (rm))
#define arm_adcsx(p, rd, rn, rm) arm_format_adc ((p), 0x1, 0x0, 0x1, (rd), (rn), (rm))
#define arm_adcsw(p, rd, rn, rm) arm_format_adc ((p), 0x0, 0x0, 0x1, (rd), (rn), (rm))
#define arm_sbcx(p, rd, rn, rm) arm_format_adc ((p), 0x1, 0x1, 0x0, (rd), (rn), (rm))
#define arm_sbcw(p, rd, rn, rm) arm_format_adc ((p), 0x0, 0x1, 0x0, (rd), (rn), (rm))
#define arm_sbcsx(p, rd, rn, rm) arm_format_adc ((p), 0x1, 0x1, 0x1, (rd), (rn), (rm))
#define arm_sbcsw(p, rd, rn, rm) arm_format_adc ((p), 0x0, 0x1, 0x1, (rd), (rn), (rm))
#define arm_ngcx(p, rd, rm) arm_sbcx ((p), (rd), ARMREG_RZR, (rm))
#define arm_ngcw(p, rd, rm) arm_sbcw ((p), (rd), ARMREG_RZR, (rm))
#define arm_ngcsx(p, rd, rm) arm_sbcsx ((p), (rd), ARMREG_RZR, (rm))
#define arm_ngcsw(p, rd, rm) arm_sbcsw ((p), (rd), ARMREG_RZR, (rm))

/* Logical (shifted register) */
#define arm_format_logical_shift(p, sf, op, N, rd, rn, rm, shift, imm6) arm_emit ((p), ((sf) << 31) | ((op) << 29) | (0xa << 24) | ((shift) << 22) | ((N) << 21) | ((rm) << 16) | ((imm6) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_andx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x0, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_andw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x0, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_andsx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x3, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_andsw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x3, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_bicx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x0, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_bicw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x0, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_bicsx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x3, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_bicsw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x3, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_eonx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x2, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_eonw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x2, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_eorx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x2, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_eorw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x2, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_orrx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x1, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_orrw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x1, 0x0, (rd), (rn), (rm), (shift_type), (amount))
#define arm_ornx_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x1, 0x1, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_ornw_shift(p, rd, rn, rm, shift_type, amount) arm_format_logical_shift ((p), 0x0, 0x1, 0x1, (rd), (rn), (rm), (shift_type), (amount))
#define arm_mvnx_shift(p, rd, rm, shift_type, amount) arm_ornx_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))
#define arm_mvnw_shift(p, rd, rm, shift_type, amount) arm_ornw_shift ((p), (rd), ARMREG_RZR, (rm), (shift_type), (amount))
#define arm_tstx_shift(p, rn, rm, shift_type, amount) arm_andsx_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
#define arm_tstw_shift(p, rn, rm, shift_type, amount) arm_andsw_shift ((p), ARMREG_RZR, (rn), (rm), (shift_type), (amount))
/* Aliases */
#define arm_andx(p, rd, rn, rm) arm_andx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_andw(p, rd, rn, rm) arm_andw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_andsx(p, rd, rn, rm) arm_andsx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_andsw(p, rd, rn, rm) arm_andsw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_bixx(p, rd, rn, rm) arm_bixx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_bixw(p, rd, rn, rm) arm_bixw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_bixsx(p, rd, rn, rm) arm_bixsx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_bixsw(p, rd, rn, rm) arm_bixsw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_eonx(p, rd, rn, rm) arm_eonx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_eonw(p, rd, rn, rm) arm_eonw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_eorx(p, rd, rn, rm) arm_eorx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_eorw(p, rd, rn, rm) arm_eorw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_orrx(p, rd, rn, rm) arm_orrx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_orrw(p, rd, rn, rm) arm_orrw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_ornx(p, rd, rn, rm) arm_ornx_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_ornw(p, rd, rn, rm) arm_ornw_shift(p, rd, rn, rm, ARMSHIFT_LSL, 0)
#define arm_mvnx(p, rd, rm) arm_mvnx_shift(p, rd, rm, ARMSHIFT_LSL, 0)
#define arm_mvnw(p, rd, rm) arm_mvnw_shift(p, rd, rm, ARMSHIFT_LSL, 0)
#define arm_tstx(p, rn, rm) arm_tstx_shift(p, rn, rm, ARMSHIFT_LSL, 0)
#define arm_tstw(p, rn, rm) arm_tstw_shift(p, rn, rm, ARMSHIFT_LSL, 0)

/* Move (register) */
#define arm_movx(p, rn, rm) arm_orrx_shift ((p), (rn), ARMREG_RZR, (rm), ARMSHIFT_LSL, 0)
#define arm_movw(p, rn, rm) arm_orrw_shift ((p), (rn), ARMREG_RZR, (rm), ARMSHIFT_LSL, 0)

/* Not an official alias */
#define arm_movspx(p, rn, rm) arm_addx_imm ((p), (rn), (rm), 0)

/* Shift (register) */
#define arm_format_shift_reg(p, sf, op2, rd, rn, rm) arm_emit ((p), ((sf) << 31) | (0xd6 << 21) | ((rm) << 16) | (0x2 << 12) | ((op2) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_asrvx(p, rd, rn, rm) arm_format_shift_reg ((p), 0x1, 0x2, (rd), (rn), (rm))
#define arm_asrvw(p, rd, rn, rm) arm_format_shift_reg ((p), 0x0, 0x2, (rd), (rn), (rm))
#define arm_lslvx(p, rd, rn, rm) arm_format_shift_reg ((p), 0x1, 0x0, (rd), (rn), (rm))
#define arm_lslvw(p, rd, rn, rm) arm_format_shift_reg ((p), 0x0, 0x0, (rd), (rn), (rm))
#define arm_lsrvx(p, rd, rn, rm) arm_format_shift_reg ((p), 0x1, 0x1, (rd), (rn), (rm))
#define arm_lsrvw(p, rd, rn, rm) arm_format_shift_reg ((p), 0x0, 0x1, (rd), (rn), (rm))
#define arm_rorvx(p, rd, rn, rm) arm_format_shift_reg ((p), 0x1, 0x3, (rd), (rn), (rm))
#define arm_rorvw(p, rd, rn, rm) arm_format_shift_reg ((p), 0x0, 0x3, (rd), (rn), (rm))

/* Multiply */
#define arm_format_mul(p, sf, o0, rd, rn, rm, ra) arm_emit ((p), ((sf) << 31) | (0x0 << 29) | (0x1b << 24) | (0x0 << 21) | ((rm) << 16) | ((o0) << 15) | ((ra) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_maddx(p, rd, rn, rm, ra) arm_format_mul((p), 0x1, 0x0, (rd), (rn), (rm), (ra))
#define arm_maddw(p, rd, rn, rm, ra) arm_format_mul((p), 0x0, 0x0, (rd), (rn), (rm), (ra))
#define arm_msubx(p, rd, rn, rm, ra) arm_format_mul((p), 0x1, 0x1, (rd), (rn), (rm), (ra))
#define arm_msubw(p, rd, rn, rm, ra) arm_format_mul((p), 0x0, 0x1, (rd), (rn), (rm), (ra))
#define arm_mnegx(p, rd, rn, rm) arm_msubx ((p), (rd), (rn), (rm), ARMREG_RZR)
#define arm_mnegw(p, rd, rn, rm) arm_msubw ((p), (rd), (rn), (rm), ARMREG_RZR)
#define arm_mulx(p, rd, rn, rm) arm_maddx ((p), (rd), (rn), (rm), ARMREG_RZR)
#define arm_mulw(p, rd, rn, rm) arm_maddw ((p), (rd), (rn), (rm), ARMREG_RZR)

/* FIXME: Missing multiple opcodes */

/* Division */
#define arm_format_div(p, sf, o1, rd, rn, rm) arm_emit ((p), ((sf) << 31) | (0xd6 << 21) | ((rm) << 16) | (0x1 << 11) | ((o1) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_sdivx(p, rd, rn, rm) arm_format_div ((p), 0x1, 0x1, (rd), (rn), (rm))
#define arm_sdivw(p, rd, rn, rm) arm_format_div ((p), 0x0, 0x1, (rd), (rn), (rm))
#define arm_udivx(p, rd, rn, rm) arm_format_div ((p), 0x1, 0x0, (rd), (rn), (rm))
#define arm_udivw(p, rd, rn, rm) arm_format_div ((p), 0x0, 0x0, (rd), (rn), (rm))

/* Conditional select */
#define arm_format_csel(p, sf, op, op2, cond, rd, rn, rm) arm_emit ((p), ((sf) << 31) | ((op) << 30) | (0xd4 << 21) | ((rm) << 16) | ((cond) << 12) | ((op2) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_cselx(p, cond, rd, rn, rm) arm_format_csel ((p), 0x1, 0x0, 0x0, (cond), (rd), (rn), (rm))
#define arm_cselw(p, cond, rd, rn, rm) arm_format_csel ((p), 0x0, 0x0, 0x0, (cond), (rd), (rn), (rm))
#define arm_csincx(p, cond, rd, rn, rm) arm_format_csel ((p), 0x1, 0x0, 0x1, (cond), (rd), (rn), (rm))
#define arm_csincw(p, cond, rd, rn, rm) arm_format_csel ((p), 0x0, 0x0, 0x1, (cond), (rd), (rn), (rm))
#define arm_csinvx(p, cond, rd, rn, rm) arm_format_csel ((p), 0x1, 0x1, 0x0, (cond), (rd), (rn), (rm))
#define arm_csinvw(p, cond, rd, rn, rm) arm_format_csel ((p), 0x0, 0x1, 0x0, (cond), (rd), (rn), (rm))
#define arm_csnegx(p, cond, rd, rn, rm) arm_format_csel ((p), 0x1, 0x1, 0x1, (cond), (rd), (rn), (rm))
#define arm_csnegw(p, cond, rd, rn, rm) arm_format_csel ((p), 0x0, 0x1, 0x1, (cond), (rd), (rn), (rm))

#define arm_cset(p, cond, rd) arm_csincx ((p), ((cond) ^ 0x1), (rd), ARMREG_RZR, ARMREG_RZR)

/* C5.6.68 (HINT) */
#define arm_hint(p, imm) arm_emit ((p), (0xd5032 << 12) | ((imm) << 5) | (0x1f << 0))
#define arm_nop(p) arm_hint ((p), 0x0)

/* C5.6.29 BRK */
#define arm_brk(p, imm) arm_emit ((p), (0xd4 << 24) | (0x1 << 21) | ((imm) << 5))

/* C6.3.114 FMOV (General) */
#define arm_format_fmov_gr(p, sf, type, rmode, opcode, rn, rd) arm_emit ((p), ((sf) << 31) | (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rmode) << 19) | ((opcode) << 16) | ((rn) << 5) | ((rd) << 0))

/* Move gr->vfp */
#define arm_fmov_rx_to_double(p, dd, xn) arm_format_fmov_gr ((p), 0x1, 0x1, 0x0, 0x7, (xn), (dd))

/* Move vfp->gr */
#define arm_fmov_double_to_rx(p, xd, dn) arm_format_fmov_gr ((p), 0x1, 0x1, 0x0, 0x6, (dn), (xd))

/* C6.3.113 FMOV (register) */
#define arm_format_fmov(p, type, rn, rd) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | (0x10 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fmovd(p, dd, dn) arm_format_fmov ((p), 0x1, (dn), (dd))
#define arm_fmovs(p, dd, dn) arm_format_fmov ((p), 0x0, (dn), (dd))

/* C6.3.54 FCMP */
#define arm_format_fcmp(p, type, opc, rn, rm) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rm) << 16) | (0x8 << 10) | ((rn) << 5) | ((opc) << 3))

#define arm_fcmpd(p, dn, dm) arm_format_fcmp (p, 0x1, 0x0, (dn), (dm))
#define arm_fcmps(p, dn, dm) arm_format_fcmp (p, 0x0, 0x0, (dn), (dm))

/* Float precision */
#define arm_format_fcvt(p, type, opc, rn, rd) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | (0x1 << 17) | ((opc) << 15) | (0x10 << 10) | ((rn) << 5) | ((rd) << 0))

/* C6.3.57 FCVT */
/* single->double */
#define arm_fcvt_sd(p, dd, sn) arm_format_fcvt ((p), 0x0, 0x1, (sn), (dd))
/* double->single */
#define arm_fcvt_ds(p, sd, dn) arm_format_fcvt ((p), 0x1, 0x0, (dn), (sd))

/* Float conversion to integer conversion */
#define arm_format_fcvtz(p, sf, type, rmode, opcode, rn, rd) arm_emit ((p), ((sf) << 31) | (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rmode) << 19) | ((opcode) << 16) | ((rn) << 5) | ((rd) << 0))

/* C6.3.80 FCVTZS (scalar, integer) */
#define arm_fcvtzs_dw(p, rd, rn) arm_format_fcvtz ((p), 0x0, 0x1, 0x3, 0x0, (rn), (rd))
#define arm_fcvtzs_dx(p, rd, rn) arm_format_fcvtz ((p), 0x1, 0x1, 0x3, 0x0, (rn), (rd))
#define arm_fcvtzs_sw(p, rd, rn) arm_format_fcvtz ((p), 0x0, 0x0, 0x3, 0x0, (rn), (rd))
#define arm_fcvtzs_sx(p, rd, rn) arm_format_fcvtz ((p), 0x1, 0x0, 0x3, 0x0, (rn), (rd))

/* C6.3.84 FCVTZU (scalar, integer) */
#define arm_fcvtzu_dw(p, rd, rn) arm_format_fcvtz ((p), 0x0, 0x1, 0x3, 0x1, (rn), (rd))
#define arm_fcvtzu_dx(p, rd, rn) arm_format_fcvtz ((p), 0x1, 0x1, 0x3, 0x1, (rn), (rd))
#define arm_fcvtzu_sw(p, rd, rn) arm_format_fcvtz ((p), 0x0, 0x0, 0x3, 0x1, (rn), (rd))
#define arm_fcvtzu_sx(p, rd, rn) arm_format_fcvtz ((p), 0x1, 0x0, 0x3, 0x1, (rn), (rd))

/* C6.3.208 SCVTF (vector, integer) */
#define arm_format_scvtf_vector(p, sz, rn, rd) arm_emit ((p), (0x1 << 30) | (0x0 << 29) | (0x1e << 24) | ((sz) << 22) | (0x10 << 17) | (0x1d << 12) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_scvtf_d(p, dd, dn) arm_format_scvtf_vector ((p), 0x1, (dn), (dd))
#define arm_scvtf_s(p, sd, sn) arm_format_scvtf_vector ((p), 0x0, (sn), (sd))

/* C6.3.210 SCVTF (scalar, integer) */
#define arm_format_scvtf_scalar(p, sf, type, rn, rd) arm_emit ((p), ((sf) << 31) | (0x1e << 24) | ((type) << 22) | (0x1 << 21) | (0x2 << 16) | (0x0 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_scvtf_rx_to_d(p, dd, rn) arm_format_scvtf_scalar ((p), 0x1, 0x1, rn, dd)
#define arm_scvtf_rw_to_d(p, dd, rn) arm_format_scvtf_scalar ((p), 0x0, 0x1, rn, dd)
#define arm_scvtf_rx_to_s(p, dd, rn) arm_format_scvtf_scalar ((p), 0x1, 0x0, rn, dd)
#define arm_scvtf_rw_to_s(p, dd, rn) arm_format_scvtf_scalar ((p), 0x0, 0x0, rn, dd)

/* C6.3.306 UCVTF (vector, integer) */
#define arm_format_ucvtf_vector(p, sz, rn, rd) arm_emit ((p), (0x1 << 30) | (0x1 << 29) | (0x1e << 24) | ((sz) << 22) | (0x10 << 17) | (0x1d << 12) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_ucvtf_d(p, dd, dn) arm_format_ucvtf_vector ((p), 0x1, (dn), (dd))
#define arm_ucvtf_s(p, sd, sn) arm_format_ucvtf_vector ((p), 0x0, (sn), (sd))

/* C6.3.308 UCVTF (scalar, integer) */
#define arm_format_ucvtf_scalar(p, sf, type, rn, rd) arm_emit ((p), ((sf) << 31) | (0x1e << 24) | ((type) << 22) | (0x1 << 21) | (0x3 << 16) | (0x0 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_ucvtf_rx_to_d(p, dd, rn) arm_format_ucvtf_scalar ((p), 0x1, 0x1, rn, dd)
#define arm_ucvtf_rw_to_d(p, dd, rn) arm_format_ucvtf_scalar ((p), 0x0, 0x1, rn, dd)

/* C6.3.41 FADD (scalar) */
#define arm_format_fadd_scalar(p, type, rd, rn, rm) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rm) << 16) | (0x1 << 13) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fadd_d(p, rd, rn, rm) arm_format_fadd_scalar ((p), 0x1, (rd), (rn), (rm))
#define arm_fadd_s(p, rd, rn, rm) arm_format_fadd_scalar ((p), 0x0, (rd), (rn), (rm))

/* C6.3.149 FSUB (scalar) */
#define arm_format_fsub_scalar(p, type, rd, rn, rm) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rm) << 16) | (0x1 << 13) | (0x1 << 12) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fsub_d(p, rd, rn, rm) arm_format_fsub_scalar ((p), 0x1, (rd), (rn), (rm))
#define arm_fsub_s(p, rd, rn, rm) arm_format_fsub_scalar ((p), 0x0, (rd), (rn), (rm))

/* C6.3.119 FMUL (scalar) */
#define arm_format_fmul_scalar(p, type, rd, rn, rm) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rm) << 16) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fmul_d(p, rd, rn, rm) arm_format_fmul_scalar ((p), 0x1, (rd), (rn), (rm))
#define arm_fmul_s(p, rd, rn, rm) arm_format_fmul_scalar ((p), 0x0, (rd), (rn), (rm))

/* C6.3.86 FDIV (scalar) */
#define arm_format_fdiv_scalar(p, type, rd, rn, rm) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((rm) << 16) | (0x1 << 12) | (0x2 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fdiv_d(p, rd, rn, rm) arm_format_fdiv_scalar ((p), 0x1, (rd), (rn), (rm))
#define arm_fdiv_s(p, rd, rn, rm) arm_format_fdiv_scalar ((p), 0x0, (rd), (rn), (rm))

/* C6.3.116 FMSUB */
#define arm_format_fmsub(p, type, rd, rn, rm, ra) arm_emit ((p), (0x1f << 24) | ((type) << 22) | (0x0 << 21) | ((rm) << 16) | (0x1 << 15) | ((ra) << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fmsub_d(p, rd, rn, rm, ra) arm_format_fmsub ((p), 0x1, (rd), (rn), (rm), (ra))

/* C6.3.123 FNEG */
#define arm_format_fneg(p, type, rd, rn) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | (0x2 << 15) | (0x10 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fneg_d(p, rd, rn) arm_format_fneg ((p), 0x1, (rd), (rn))
#define arm_fneg_s(p, rd, rn) arm_format_fneg ((p), 0x0, (rd), (rn))

/* C6.3.37 FABS (scalar) */
#define arm_format_fabs(p, type, opc, rd, rn) arm_emit ((p), (0x1e << 24) | ((type) << 22) | (0x1 << 21) | ((opc) << 15) | (0x10 << 10) | ((rn) << 5) | ((rd) << 0))

#define arm_fabs_d(p, rd, rn) arm_format_fabs ((p), 0x1, 0x1, (rd), (rn))

/* C5.6.60 DMB */
#define arm_format_dmb(p, opc, CRm) arm_emit ((p), (0x354 << 22) | (0x3 << 16) | (0x3 << 12) | ((CRm) << 8) | (0x1 << 7) | ((opc) << 5) | (0x1f << 0))

#define ARM_DMB_OSHLD 0x1
#define ARM_DMB_OSHST 0x2
#define ARM_DMB_OSH   0x3
#define ARM_DMB_NSHLD 0x5
#define ARM_DMB_NSHST 0x6
#define ARM_DMB_NSH   0x7
#define ARM_DMB_ISHLD 0x9
#define ARM_DMB_ISHST 0xa
#define ARM_DMB_ISH   0xb
#define ARM_DMB_LD    0xd
#define ARM_DMB_ST    0xe
#define ARM_DMB_SY    0xf

#define arm_dmb(p, imm) arm_format_dmb ((p), 0x1, (imm))

/* C5.6.129 MRS */

#define ARM_MRS_REG_TPIDR_EL0 0x5e82

#define arm_format_mrs(p, sysreg, rt) arm_emit ((p), (0x354 << 22) | (0x1 << 21) | (0x1 << 20) | ((sysreg) << 5) | ((rt) << 0))

#define arm_mrs(p, rt, sysreg) arm_format_mrs ((p), (sysreg), (rt))

#ifdef MONO_ARCH_ILP32
#define arm_strp arm_strw
#define arm_ldrp arm_ldrw
#define arm_cmpp arm_cmpw
#else
#define arm_strp arm_strx
#define arm_ldrp arm_ldrx
#define arm_cmpp arm_cmpx
#endif

/* ARM v8.3 */

/* PACIA */

#define arm_format_pacia(p, crm, op2) arm_emit ((p), (0b11010101000000110010000000011111 << 0) | ((crm) << 8) | ((op2) << 5))
#define arm_paciasp(p) arm_format_pacia ((p), 0b0011, 0b001)

/* PACIB */

#define arm_format_pacib(p, crm, op2) arm_emit ((p), (0b11010101000000110010000000011111 << 0) | ((crm) << 8) | ((op2) << 5))
#define arm_pacibsp(p) arm_format_pacib ((p), 0b0011, 0b011)

/* RETA */
#define arm_format_reta(p,key) arm_emit ((p), 0b11010110010111110000101111111111 + ((key) << 10))

#define arm_retaa(p) arm_format_reta ((p),0)
#define arm_retab(p) arm_format_reta ((p),1)

/* BRA */

#define arm_format_bra(p, z, m, rn, rm) arm_emit ((p), (0b1101011000011111000010 << 10) + ((z) << 24) + ((m) << 10) + ((rn) << 5) + ((rm) << 0))

#define arm_braaz(p, rn) arm_format_bra ((p), 0, 0, (rn), 0b11111)
#define arm_brabz(p, rn) arm_format_bra ((p), 0, 1, (rn), 0b11111)
#define arm_braa(p, rn, rm) arm_format_bra ((p), 1, 0, (rn), (rm))
#define arm_brab(p, rn, rm) arm_format_bra ((p), 1, 1, (rn), (rm))

/* BLRA */

#define arm_format_blra(p, z, m, rn, rm) arm_emit ((p), (0b1101011000111111000010 << 10) + ((z) << 24) + ((m) << 10) + ((rn) << 5) + ((rm) << 0))

#define arm_blraaz(p, rn) arm_format_blra ((p), 0, 0, (rn), 0b11111)
#define arm_blraa(p, rn, rm) arm_format_blra ((p), 1, 0, (rn), (rm))
#define arm_blrabz(p, rn) arm_format_blra ((p), 0, 1, (rn), 0b11111)
#define arm_blrab(p, rn, rm) arm_format_blra ((p), 1, 1, (rn), (rm))

/* AUTIA */

#define arm_format_autia(p, crm, op2) arm_emit ((p), (0b11010101000000110010000000011111 << 0) | ((crm) << 8) | ((op2) << 5))

#define arm_autiasp(p) arm_format_autia ((p), 0b0011, 0b101)

/* AUTIB */

#define arm_format_autib(p, crm, op2) arm_emit ((p), (0b11010101000000110010000000011111 << 0) | ((crm) << 8) | ((op2) << 5))

#define arm_autibsp(p) arm_format_autib ((p), 0b0011, 0b111)

#endif /* __arm_CODEGEN_H__ */
