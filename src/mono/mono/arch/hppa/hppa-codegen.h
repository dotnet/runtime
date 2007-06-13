#ifndef _HPPA_CODEGEN_H_
#define _HPPA_CODEGEN_H_

typedef enum {
	hppa_r0 = 0,
	hppa_r1,
	hppa_r2,
	hppa_rp = hppa_r2,
	hppa_r3,
	hppa_r4,
	hppa_r5,
	hppa_r6,
	hppa_r7,
	hppa_r8,
	hppa_r9,
	hppa_r10,
	hppa_r11,
	hppa_r12,
	hppa_r13,
	hppa_r14,
	hppa_r15,
	hppa_r16,
	hppa_r17,
	hppa_r18,
	hppa_r19,
	hppa_r20,
	hppa_r21,
	hppa_r22,
	hppa_r23,
	hppa_r24,
	hppa_r25,
	hppa_r26,
	hppa_r27,
	hppa_r28,
	hppa_r29,
	hppa_ap = hppa_r29,
	hppa_r30,
	hppa_sp = hppa_r30,
	hppa_r31
} HPPAIntRegister;

typedef enum {
	hppa_fr0,
	hppa_fr1,
	hppa_fr2,
	hppa_fr3,
	hppa_fr4,
	hppa_fr5,
	hppa_fr6,
	hppa_fr7,
	hppa_fr8,
	hppa_fr9,
	hppa_fr10,
	hppa_fr11,
	hppa_fr12,
	hppa_fr13,
	hppa_fr14,
	hppa_fr15,
	hppa_fr16,
	hppa_fr17,
	hppa_fr18,
	hppa_fr19,
	hppa_fr20,
	hppa_fr21,
	hppa_fr22,
	hppa_fr23,
	hppa_fr24,
	hppa_fr25,
	hppa_fr26,
	hppa_fr27,
	hppa_fr28,
	hppa_fr29,
	hppa_fr30,
	hppa_fr31
} HPPAFloatRegister;

#define hppa_opcode(op)	((op) << 26)
#define hppa_opcode_alu(op1, op2) (((op1) << 26) | ((op2) << 6))
#define hppa_op_r1(r) ((r) << 21)
#define hppa_op_r2(r) ((r) << 16)
#define hppa_op_r3(r) (r)

/* imm5, imm11 and imm14 are encoded by putting the sign bit in the LSB */
#define hppa_op_imm5(im5) ((((im5) & 0xf) << 1) | (((int)(im5)) < 0))
#define hppa_op_imm11(im11) ((((im11) & 0x3ff) << 1) | (((int)(im11)) < 0))
#define hppa_op_imm14(im14) ((((im14) & 0x1fff) << 1) | (((int)(im14)) < 0))

/* HPPA uses "selectors" for some operations. The two we need are L% and R% */
/* lsel: select left 21 bits */
#define hppa_lsel(v)		(((int)(v))>>11)
/* rsel: select right 11 bits */
#define hppa_rsel(v)		(((int)(v))&0x7ff)

/* imm12 is used by the conditional branch insns
 * w1 (bits [2..12])
 * w (bit 0)
 * value = assemble_12(w1,w) = cat(w,w1{10},w1{0..9})
 * (note PA bit numbering)
 *
 * if the original number is:
 * abcdefghijkl
 *
 *  3         2         1         0
 * 10987654321098765432109876543210
 *                    cdefghijklb a
 */
static inline int hppa_op_imm12(int im12)
{
	unsigned int a = im12 < 0;
	unsigned int b = (im12 >> 10) & 0x1;
	unsigned int cdefghijkl = im12 & 0x3ff;

	return (cdefghijkl << 3) | (b << 2) | a;
}

/*
 * imm17 is used by the BL insn, which has 
 * w1 (bits [16..20])
 * w2 (bits [2..12])
 * w (bit 0)
 * value = assemble_17(w1,w2,w) = cat(w,w1,w2{10},w2{0..9})
 * (note PA bit numbering)
 *
 * if the original number is:
 * abcdefghijklmnopq
 *
 *  3         2         1         0
 * 10987654321098765432109876543210
 *            bcdef   hijklmnopqg a
 */
static inline int hppa_op_imm17(int im17)
{
	unsigned int a = im17 < 0;
	unsigned int bcdef = (im17 >> 11) & 0x1f;
	unsigned int g = (im17 >> 10) & 0x1;
	unsigned int hijklmnopq = im17 & 0x3ff;

	return (bcdef << 16) | (hijklmnopq << 3) | (g << 2) | a;
}

/* imm21 is used by addil and ldil
 *
 * value = assemble_21(x) = cat(x{20},x{9..19},x{5..6},x{0..4},x{7..8})
 * (note PA bit numbering)
 *
 * if the original number is:
 * abcdefghijklmnopqrstu
 *
 *  3         2         1         0
 * 10987654321098765432109876543210
 *            opqrsmntubcdefghijkla
 */
static inline int hppa_op_imm21(int im21)
{
	unsigned int a = im21 < 0;
	unsigned int bcdefghijkl = (im21 >> 9) & 0x7ff;
	unsigned int mn = (im21 >> 7) & 0x3;
	unsigned int opqrs = (im21 >> 2) & 0x1f;
	unsigned int tu = im21 & 0x3;

	return (opqrs << 16) | (mn << 14) | (tu << 12) | (bcdefghijkl << 1) | a;
}

/* returns 1 if VAL can fit in BITS */
static inline int hppa_check_bits(int val, int bits)
{
	/* positive offset */
	if (!(val & (1 << (bits - 1))) && (val >> bits) != 0)
		return 0;
	/* negative offset */
	if ((val & (1 << (bits - 1))) && ((val >> bits) != (-1 >>(bits+2))))
		return 0;
	return 1;
}

static inline void *hppa_emit(void *inp, unsigned int insn)
{
	unsigned int *code = inp;
	*code = insn;
	return ((char *)code) + 4;
}

/* Table 5-3: Compare conditons */
#define HPPA_CMP_COND_NEVER	(0)
#define HPPA_CMP_COND_EQ	(1)
#define HPPA_CMP_COND_SLT	(2)
#define HPPA_CMP_COND_SLE	(3)
#define HPPA_CMP_COND_ULT	(4)
#define HPPA_CMP_COND_ULE	(5)
#define HPPA_CMP_COND_OV	(6)
#define HPPA_CMP_COND_ODD	(7)

/* Table 5-3: Subtaction conditions */
#define HPPA_SUB_COND_NEVER	((0 << 1) | 0)
#define HPPA_SUB_COND_EQ	((1 << 1) | 0)
#define HPPA_SUB_COND_SLT	((2 << 1) | 0)
#define HPPA_SUB_COND_SLE	((3 << 1) | 0)
#define HPPA_SUB_COND_ULT	((4 << 1) | 0)
#define HPPA_SUB_COND_ULE	((5 << 1) | 0)
#define HPPA_SUB_COND_SV	((6 << 1) | 0)
#define HPPA_SUB_COND_OD	((7 << 1) | 0)
#define HPPA_SUB_COND_ALWAYS	((0 << 1) | 1)
#define HPPA_SUB_COND_NE	((1 << 1) | 1)
#define HPPA_SUB_COND_SGE	((2 << 1) | 1)
#define HPPA_SUB_COND_SGT	((3 << 1) | 1)
#define HPPA_SUB_COND_UGE	((4 << 1) | 1)
#define HPPA_SUB_COND_UGT	((5 << 1) | 1)
#define HPPA_SUB_COND_NSV	((6 << 1) | 1)
#define HPPA_SUB_COND_EV	((7 << 1) | 1)

/* Table 5-4: Addition conditions */
#define HPPA_ADD_COND_NEVER	((0 << 1) | 0)
#define HPPA_ADD_COND_EQ	((1 << 1) | 0)
#define HPPA_ADD_COND_LT	((2 << 1) | 0)
#define HPPA_ADD_COND_LE	((3 << 1) | 0)
#define HPPA_ADD_COND_NUV	((4 << 1) | 0)
#define HPPA_ADD_COND_ZUV	((5 << 1) | 0)
#define HPPA_ADD_COND_SV	((6 << 1) | 0)
#define HPPA_ADD_COND_OD	((7 << 1) | 0)
#define HPPA_ADD_COND_ALWAYS	((0 << 1) | 1)
#define HPPA_ADD_COND_NE	((1 << 1) | 1)
#define HPPA_ADD_COND_GE	((2 << 1) | 1)
#define HPPA_ADD_COND_GT	((3 << 1) | 1)
#define HPPA_ADD_COND_UV	((4 << 1) | 1)
#define HPPA_ADD_COND_VNZ	((5 << 1) | 1)
#define HPPA_ADD_COND_NSV	((6 << 1) | 1)
#define HPPA_ADD_COND_EV	((7 << 1) | 1)

/* Table 5-5: Logical instruction conditions */
#define HPPA_LOGICAL_COND_NEVER			((0 << 1) | 0)
#define HPPA_LOGICAL_COND_ZERO			((1 << 1) | 0)
#define HPPA_LOGICAL_COND_MSB_SET		((2 << 1) | 0)
#define HPPA_LOGICAL_COND_MSB_SET_OR_ZERO	((3 << 1) | 0)
#define HPPA_LOGICAL_COND_LSB_SET		((7 << 1) | 0)
#define HPPA_LOGICAL_COND_ALWAYS		((0 << 1) | 1)
#define HPPA_LOGICAL_COND_NZ			((1 << 1) | 1)
#define HPPA_LOGICAL_COND_MSB_CLR		((2 << 1) | 1)
#define HPPA_LOGICAL_COND_MSB_CLR_AND_NZ	((3 << 1) | 1)
#define HPPA_LOGICAL_COND_LSB_CLR		((7 << 1) | 1)

/* Table 5-6: Unit Conditions */
#define HPPA_UNIT_COND_NEVER	((0 << 1) | 0)
#define HPPA_UNIT_COND_SBZ	((2 << 1) | 0)
#define HPPA_UNIT_COND_SHZ	((3 << 1) | 0)
#define HPPA_UNIT_COND_SDC	((4 << 1) | 0)
#define HPPA_UNIT_COND_SBC	((6 << 1) | 0)
#define HPPA_UNIT_COND_SHC	((7 << 1) | 0)
#define HPPA_UNIT_COND_ALWAYS	((0 << 1) | 1)
#define HPPA_UNIT_COND_NBZ	((2 << 1) | 1)
#define HPPA_UNIT_COND_NHZ	((3 << 1) | 1)
#define HPPA_UNIT_COND_NDC	((4 << 1) | 1)
#define HPPA_UNIT_COND_NBC	((6 << 1) | 1)
#define HPPA_UNIT_COND_NHC	((7 << 1) | 1)

/* Table 5-7: Shift/Extract/Deposit Conditions */
#define HPPA_BIT_COND_NEVER	(0)
#define HPPA_BIT_COND_ZERO	(1)
#define HPPA_BIT_COND_MSB_SET	(2)
#define HPPA_BIT_COND_LSB_SET	(3)
#define HPPA_BIT_COND_ALWAYS	(4)
#define HPPA_BIT_COND_SOME_SET	(5)
#define HPPA_BIT_COND_MSB_CLR	(6)
#define HPPA_BIT_COND_LSB_CLR	(7)

#define hppa_mtsar(p, r)				\
	p = hppa_emit (p, hppa_opcode(0x00) | hppa_op_r1(11) | hppa_op_r2(r) | (0xC2 << 5))

#define hppa_bl_full(p, n, target, t) do { 		\
	g_assert (hppa_check_bits (target, 17)); 	\
	p = hppa_emit (p, hppa_opcode(0x3A) | hppa_op_r1(t) | hppa_op_imm17(((int)(((target) - 8)>>2))) | ((n) << 1)); \
} while (0)

#define hppa_bl(p, target, t) hppa_bl_full(p, 0, target, t)
#define hppa_bl_n(p, target, t) hppa_bl_full(p, 1, target, t)

#define hppa_bv(p, x, b)				\
	p = hppa_emit (p, hppa_opcode(0x3A) | hppa_op_r1(b) | hppa_op_r2(x) | (6 << 13))

#define hppa_blr(p, x, t)				\
	p = hppa_emit (p, hppa_opcode(0x3A) | hppa_op_r1(t) | hppa_op_r2(x) | (2 << 13))

/* hardcoded sr = sr4 */
#define hppa_ble_full(p, n, d, b)			\
	p = hppa_emit (p, hppa_opcode(0x39) | hppa_op_r1(b) | hppa_op_imm17(((int)(d)) >> 2) | (1 << 13) | ((n) << 1))

#define hppa_ble(p, d, b) hppa_ble_full(p, 0, d, b)
#define hppa_ble_n(p, d, b) hppa_ble_full(p, 1, d, b)

#define hppa_be_full(p, n, d, b)			\
	p = hppa_emit (p, hppa_opcode(0x38) | hppa_op_r1(b) | hppa_op_imm17(((int)(d)) >> 2) | (1 << 13) | ((n) << 1))

#define hppa_be(p, d, b) hppa_be_full(p, 0, d, b)
#define hppa_be_n(p, d, b) hppa_be_full(p, 1, d, b)

#define hppa_bb_full(p, cond, n, r, b, t)		\
	p = hppa_emit (p, hppa_opcode(0x31) | hppa_op_r1(b) | hppa_op_r2(r) | ((cond) << 13) | ((n) << 1) | hppa_op_imm12((int)(t)))

#define hppa_bb(p, cond, r, b, t) hppa_bb_full(p, cond, 0, r, b, t)
#define hppa_bb_n(p, cond, r, b, t) hppa_bb_full(p, cond, 1, r, b, t)


#define hppa_movb(p, r1, r2, cond, target) do {		\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x32) | hppa_op_r1(r2) | hppa_op_r2(r1) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

#define hppa_movib(p, i, r, cond, target) do {		\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x33) | hppa_op_r1(r) | (hppa_op_imm5(((int)(i))) << 16) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

#define hppa_combt(p, r1, r2, cond, target) do { 	\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x20) | hppa_op_r1(r2) | hppa_op_r2(r1) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

#define hppa_combf(p, r1, r2, cond, target) do {	\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x22) | hppa_op_r1(r2) | hppa_op_r2(r1) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

#define hppa_combit(p, i, r, cond, target) do { 	\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x21) | hppa_op_r1(r) | (hppa_op_imm5(((int)(i))) << 16) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

#define hppa_combif(p, i, r, cond, target) do { 	\
	g_assert (hppa_check_bits (target, 12)); 	\
	p = hppa_emit (p, hppa_opcode(0x23) | hppa_op_r1(r) | (hppa_op_imm5(((int)(i))) << 16) | ((cond) << 13) | hppa_op_imm12(((int)(target)))); \
} while (0)

/* TODO: addbt, addbf, addbit, addbif */

/* Load/store insns */
#define hppa_ld_disp(p, op, d, b, t) do { 		\
	g_assert (hppa_check_bits (d, 14));		\
	p = hppa_emit (p, hppa_opcode(op) | hppa_op_r1(b) | hppa_op_r2(t) | hppa_op_imm14(((int)(d)))); \
} while (0)

#define hppa_ldb(p, d, b, t) hppa_ld_disp(p, 0x10, d, b, t)
#define hppa_ldh(p, d, b, t) hppa_ld_disp(p, 0x11, d, b, t)
#define hppa_ldw(p, d, b, t) hppa_ld_disp(p, 0x12, d, b, t)

#define hppa_ldwm(p, d, b, t) \
	p = hppa_emit (p, hppa_opcode(0x13) | hppa_op_r1(b) | hppa_op_r2(t) | hppa_op_imm14(d)); \

#define hppa_ldbx(p, x, b, t) hppa_ld_indexed(p, 0, x, b, t)

#define hppa_st_disp(p, op, r, d, b) do { 		\
	g_assert (hppa_check_bits (d, 14)); 		\
	p = hppa_emit (p, hppa_opcode(op) | hppa_op_r1(b) | hppa_op_r2(r) | hppa_op_imm14(((int)(d)))); \
} while (0)

#define hppa_stb(p, r, d, b) hppa_st_disp(p, 0x18, r, d, b)
#define hppa_sth(p, r, d, b) hppa_st_disp(p, 0x19, r, d, b)
#define hppa_stw(p, r, d, b) hppa_st_disp(p, 0x1A, r, d, b)

#define hppa_stwm(p, r, d, b) \
	p = hppa_emit (p, hppa_opcode(0x1B) | hppa_op_r1(b) | hppa_op_r2(r) | hppa_op_imm14(d))

#define hppa_ldbx(p, x, b, t) hppa_ld_indexed(p, 0, x, b, t)

/* s = 0, u = 0, cc = 0, m = 0 */
#define hppa_ld_indexed(p, op, x, b, t)			\
	p = hppa_emit (p, hppa_opcode(0x03) | hppa_op_r1(b) | hppa_op_r2(x) | hppa_op_r3(t) | (op << 6)) 

#define hppa_ldbx(p, x, b, t) hppa_ld_indexed(p, 0, x, b, t)
#define hppa_ldhx(p, x, b, t) hppa_ld_indexed(p, 1, x, b, t)
#define hppa_ldwx(p, x, b, t) hppa_ld_indexed(p, 2, x, b, t)

#define hppa_ldil(p, i, t)				\
	p = hppa_emit (p, hppa_opcode(0x08) | hppa_op_r1(t) | hppa_op_imm21(((int)(i))))

#define hppa_ldo(p, d, b, t)				\
	p = hppa_emit (p, hppa_opcode(0x0D) | hppa_op_r1(b) | hppa_op_r2(t) | hppa_op_imm14((int)(d)))

#define hppa_set(p, imm, t) do {			\
	if (hppa_check_bits ((int)(imm), 14))		\
		hppa_ldo (p, (int)(imm), hppa_r0, t); \
	else {						\
		hppa_ldil (p, hppa_lsel (imm), t); \
		hppa_ldo (p, hppa_rsel (imm), t, t); \
	}						\
} while (0)

/* addil's destination is always r1 */
#define hppa_addil(p, i, r)				\
	p = hppa_emit (p, hppa_opcode(0x0A) | hppa_op_r1(r) | hppa_op_imm21(i))

#define hppa_alu_op(p, op, cond, r1, r2, t)	\
	p = hppa_emit (p, hppa_opcode_alu(0x02, op) | hppa_op_r1(r2) | hppa_op_r2(r1) | hppa_op_r3(t) | ((cond) << 12))

#define hppa_add_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x18, cond, r1, r2, t)
#define hppa_add(p, r1, r2, t) hppa_add_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_addl_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x28, cond, r1, r2, t)
#define hppa_addl(p, r1, r2, t) hppa_addl_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_addo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x38, cond, r1, r2, t)
#define hppa_addo(p, r1, r2, t) hppa_addo_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_addc_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x1C, cond, r1, r2, t)
#define hppa_addc(p, r1, r2, t) hppa_addc_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_addco_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x3C, cond, r1, r2, t)
#define hppa_addco(p, r1, r2, t) hppa_addco_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh1add_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x19, cond, r1, r2, t)
#define hppa_sh1add(p, r1, r2, t) hppa_sh1add_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh1addl_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x29, cond, r1, r2, t)
#define hppa_sh1addl(p, r1, r2, t) hppa_sh1addl_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh1addo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x39, cond, r1, r2, t)
#define hppa_sh1addo(p, r1, r2, t) hppa_sh1addo_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh2add_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x1A, cond, r1, r2, t)
#define hppa_sh2add(p, r1, r2, t) hppa_sh2add_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh2addl_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x2A, cond, r1, r2, t)
#define hppa_sh2addl(p, r1, r2, t) hppa_sh2addl_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh2addo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x3A, cond, r1, r2, t)
#define hppa_sh2addo(p, r1, r2, t) hppa_sh2addo_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh3add_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x1B, cond, r1, r2, t)
#define hppa_sh3add(p, r1, r2, t) hppa_sh3add_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh3addl_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x2B, cond, r1, r2, t)
#define hppa_sh3addl(p, r1, r2, t) hppa_add_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)
#define hppa_sh3addo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x3B, cond, r1, r2, t)
#define hppa_sh3addo(p, r1, r2, t) hppa_sh3addo_cond(p, HPPA_ADD_COND_NEVER, r1, r2, t)

#define hppa_sub_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x10, cond, r1, r2, t)
#define hppa_sub(p, r1, r2, t) hppa_sub_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_subo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x30, cond, r1, r2, t)
#define hppa_subo(p, r1, r2, t) hppa_subo_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_subb_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x14, cond, r1, r2, t)
#define hppa_subb(p, r1, r2, t) hppa_subb_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_subbo_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x34, cond, r1, r2, t)
#define hppa_subbo(p, r1, r2, t) hppa_subbo_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_subt_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x13, cond, r1, r2, t)
#define hppa_subt(p, r1, r2, t) hppa_subt_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_subto_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x33, cond, r1, r2, t)
#define hppa_subto(p, r1, r2, t) hppa_subto_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_ds_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x11, cond, r1, r2, t)
#define hppa_ds(p, r1, r2, t) hppa_ds_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)
#define hppa_comclr_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x22, cond, r1, r2, t)
#define hppa_comclr(p, r1, r2, t) hppa_comclr_cond(p, HPPA_SUB_COND_NEVER, r1, r2, t)

#define hppa_or_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x09, cond, r1, r2, t)
#define hppa_or(p, r1, r2, t) hppa_or_cond(p, HPPA_LOGICAL_COND_NEVER, r1, r2, t)
#define hppa_copy(p, r1, r2) hppa_or(p, r1, hppa_r0, r2)
#define hppa_nop(p) hppa_or(p, hppa_r0, hppa_r0, hppa_r0)
#define hppa_xor_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x0A, cond, r1, r2, t)
#define hppa_xor(p, r1, r2, t) hppa_xor_cond(p, HPPA_LOGICAL_COND_NEVER, r1, r2, t)
#define hppa_and_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x08, cond, r1, r2, t)
#define hppa_and(p, r1, r2, t) hppa_and_cond(p, HPPA_LOGICAL_COND_NEVER, r1, r2, t)
#define hppa_andcm_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x00, cond, r1, r2, t)
#define hppa_andcm(p, r1, r2, t) hppa_andcm_cond(p, HPPA_LOGICAL_COND_NEVER, r1, r2, t)

#define hppa_uxor_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x0E, cond, r1, r2, t)
#define hppa_uxor(p, r1, r2, t) hppa_uxor_cond(p, HPPA_UNIT_COND_NEVER, r1, r2, t)
#define hppa_uaddcm_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x26, cond, r1, r2, t)
#define hppa_uaddcm(p, r1, r2, t) hppa_uaddcm_cond(p, HPPA_UNIT_COND_NEVER, r1, r2, t)
#define hppa_uaddcmt_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x27, cond, r1, r2, t)
#define hppa_uaddcmt(p, r1, r2, t) hppa_uaddcmt_cond(p, HPPA_UNIT_COND_NEVER, r1, r2, t)
#define hppa_dcor_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x2E, cond, r1, r2, t)
#define hppa_dcor(p, r1, r2, t) hppa_dcor_cond(p, HPPA_UNIT_COND_NEVER, r1, r2, t)
#define hppa_idcor_cond(p, cond, r1, r2, t) hppa_alu_op(p, 0x2F, cond, r1, r2, t)
#define hppa_idcor(p, r1, r2, t) hppa_idcor_cond(p, HPPA_UNIT_COND_NEVER, r1, r2, t)

#define hppa_addi(p, i, r, t)				\
	p = hppa_emit (p, hppa_opcode(0x2D) | hppa_op_r1(r) | hppa_op_r2(t) | hppa_op_imm11(((int)(i))))

#define hppa_subi(p, i, r, t)				\
	p = hppa_emit (p, hppa_opcode(0x25) | hppa_op_r1(r) | hppa_op_r2(t) | hppa_op_imm11(((int)(i))))

#define hppa_not(p, r, t) hppa_subi(p, -1, r, t)

#define hppa_comiclr(p, i, r, t)			\
	p = hppa_emit (p, hppa_opcode(0x24) | hppa_op_r1(r) | hppa_op_r2(t) | hppa_op_imm11(((int)(i))))

#define hppa_vshd(p, r1, r2, t)				\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r2) | hppa_op_r2(r1) | hppa_op_r3(t))

/* shift is a register */
#define hppa_lshr(p, r, shift, t)			\
	do {						\
		hppa_mtsar(p, shift);			\
		hppa_vshd(p, hppa_r0, r, t);		\
	} while (0)

/* shift is a constant */
#define hppa_shd(p, r1, r2, shift, t)			\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r2) | hppa_op_r2(r1) | hppa_op_r3(t) | (2 << 10) | ((31 - (shift)) << 5))

#define hppa_vextru(p, r, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r) | hppa_op_r2(t) | (4 << 10) | (32 - (len)))

#define hppa_vextrs(p, r, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r) | hppa_op_r2(t) | (5 << 10) | (32 - (len)))

/* shift is a register */
#define hppa_shr(p, r, shift, t)			\
	do {						\
		hppa_subi(p, 31, shift, t);		\
		hppa_mtsar(p, t);			\
		hppa_vextrs(p, r, 32, t);		\
	} while (0)

/* shift is a constant */
#define hppa_extru(p, r, shift, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r) | hppa_op_r2(t) | (6 << 10) | ((shift) << 5) | (32 - (len)))

#define hppa_extrs(p, r, shift, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x34) | hppa_op_r1(r) | hppa_op_r2(t) | (7 << 10) | ((shift) << 5) | (32 - (len)))

#define hppa_vdep(p, r, len, t)				\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(r) | hppa_op_r2(t) | (1 << 10) | (32 - (len)))

#define hppa_dep(p, r, pos, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | hppa_op_r2(r) | (3 << 10) | ((31 - (pos)) << 5) | (32 - (len)))

#define hppa_vdepi(p, i, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | (hppa_op_imm5(((int)(i))) << 16) | (5 << 10) | (32 - (len)))

#define hppa_depi(p, i, pos, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | (hppa_op_imm5(((int)(i))) << 16) | (7 << 10) | ((31 - (pos)) << 5) | (32 - (len)))

#define hppa_zvdep(p, r, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | hppa_op_r2(r) | (0 << 10) | (32 - (len)))

/* shift is a register */
#define hppa_shl(p, r, shift, t)			\
	do {						\
		hppa_subi(p, 31, shift, t);		\
		hppa_mtsar(p, t);			\
		hppa_zvdep(p, r, 32, t);		\
	} while (0)

#define hppa_zdep(p, r, pos, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | hppa_op_r2(r) | (2 << 10) | ((31 - (pos)) << 5) | (32 - (len)))

#define hppa_zvdepi(p, i, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | (hppa_op_imm5(((int)(i))) << 16) | (4 << 10) | (32 - (len)))

#define hppa_zdepi(p, i, pos, len, t)			\
	p = hppa_emit (p, hppa_opcode(0x35) | hppa_op_r1(t) | (hppa_op_imm5(((int)(i))) << 16) | (6 << 10) | ((31 - (pos)) << 5) | (32 - (len)))

/* FPU insns */
/* These are valid for op == 0x0C only, for op == 0x0E there is an extra bit for
 * r and t */
#define hppa_fpu_class0(p, r, sub, fmt, t)		\
	p = hppa_emit (p, hppa_opcode(0x0C) | hppa_op_r1(r) | hppa_op_r3(t) | ((sub) << 13) | ((fmt) << 11))

#define hppa_fpu_class1(p, r, sub, df, sf, t)		\
	p = hppa_emit (p, hppa_opcode(0x0C) | hppa_op_r1(r) | hppa_op_r3(t) | ((sub) << 15) | ((df) << 13) | ((sf) << 11) | (1 << 9))

#define hppa_fpu_class2(p, r1, r2, sub, fmt, n, cond)	\
	p = hppa_emit (p, hppa_opcode(0x0C) | hppa_op_r1(r1) | hppa_op_r2(r2) | hppa_op_r3(cond) | ((sub) << 13) | ((fmt) << 11) | (2 << 9) | ((n) << 5))

#define hppa_fpu_class3(p, r1, r2, sub, fmt, t)		\
	p = hppa_emit (p, hppa_opcode(0x0C) | hppa_op_r1(r1) | hppa_op_r2(r2) | hppa_op_r3(t) | ((sub) << 13) | ((fmt) << 11) | (3 << 9))

#define HPPA_FP_FMT_SGL 0
#define HPPA_FP_FMT_DBL 1
#define HPPA_FP_FMT_QUAD 3

#define hppa_fcpy(p, fmt, r, t) hppa_fpu_class0(p, r, 2, fmt, t)
#define hppa_fabs(p, fmt, r, t) hppa_fpu_class0(p, r, 3, fmt, t)
#define hppa_fsqrt(p, fmt, r, t) hppa_fpu_class0(p, r, 4, fmt, t)
#define hppa_frnd(p, fmt, r, t) hppa_fpu_class0(p, r, 5, fmt, t)

#define hppa_fcnvff(p, sf, df, r, t) hppa_fpu_class1(p, r, 0, df, sf, t)
#define hppa_fcnvxf(p, sf, df, r, t) hppa_fpu_class1(p, r, 1, df, sf, t)
#define hppa_fcnvfx(p, sf, df, r, t) hppa_fpu_class1(p, r, 2, df, sf, t)
#define hppa_fcnvfxt(p, sf, df, r, t) hppa_fpu_class1(p, r, 3, df, sf, t)

#define hppa_fcmp(p, fmt, cond, r1, r2) hppa_fpu_class2(p, r1, r2, 0, fmt, 0, cond)
#define hppa_ftest(p, cond) hppa_fpu_class2(p, 0, 0, 1, 0, 1, cond)

#define hppa_fadd(p, fmt, r1, r2, t) hppa_fpu_class3(p, r1, r2, 0, fmt, t)
#define hppa_fsub(p, fmt, r1, r2, t) hppa_fpu_class3(p, r1, r2, 1, fmt, t)
#define hppa_fmul(p, fmt, r1, r2, t) hppa_fpu_class3(p, r1, r2, 2, fmt, t)
#define hppa_fdiv(p, fmt, r1, r2, t) hppa_fpu_class3(p, r1, r2, 3, fmt, t)

/* Note: fmpyadd and fmpysub have different fmt encodings as the other
 * FP ops
 */
#define hppa_fmpyadd(p, fmt, rm1, rm2, tm, ra, ta)	\
	p = hppa_emit (p, hppa_opcode(0x06) | hppa_op_r1(rm1) | hppa_op_r2(rm2) | hppa_op_r3(tm) | ((ta) << 11) | ((ra) << 6) | ((fmt) << 5))

#define hppa_fmpyadd_sgl(p, rm1, rm2, tm, ra, ta)	\
	hppa_fmpyadd(p, 1, rm1, rm2, tm, ra, ta)

#define hppa_fmpyadd_dbl(p, rm1, rm2, tm, ra, ta)	\
	hppa_fmpyadd(p, 0, rm1, rm2, tm, ra, ta)

#define hppa_fmpysub(p, fmt, rm1, rm2, tm, ra, ta)	\
	p = hppa_emit (p, hppa_opcode(0x06) | hppa_op_r1(rm1) | hppa_op_r2(rm2) | hppa_op_r3(tm) | ((ta) << 11) | ((ra) << 6) | ((fmt) << 5))

#define hppa_fmpysub_sgl(p, rm1, rm2, tm, ra, ta)	\
	hppa_fmpysub(p, 1, rm1, rm2, tm, ra, ta)

#define hppa_fmpysub_dbl(p, rm1, rm2, tm, ra, ta)	\
	hppa_fmpysub(p, 0, rm1, rm2, tm, ra, ta)

#define hppa_xmpyu(p, r1, r2, t)			\
	p = hppa_emit (p, hppa_opcode(0x0E) | hppa_op_r1(r1) | hppa_op_r2(r2) | hppa_op_r3(t) | (2 << 13) | (3 << 9) | (1 << 8))

#define hppa_fldwx(p, x, b, t, half)			\
	p = hppa_emit (p, hppa_opcode(0x09) | hppa_op_r1(b) | hppa_op_r2(x) | hppa_op_r3(t) | ((half) << 6))

#define hppa_flddx(p, x, b, t)				\
	p = hppa_emit (p, hppa_opcode(0x0B) | hppa_op_r1(b) | hppa_op_r2(x) | hppa_op_r3(t))

#define hppa_fstwx(p, r, half, x, b)			\
	p = hppa_emit (p, hppa_opcode(0x09) | hppa_op_r1(b) | hppa_op_r2(x) | hppa_op_r3(r) | ((half) << 6) | (1 << 9))

#define hppa_fstdx(p, r, x, b)				\
	p = hppa_emit (p, hppa_opcode(0x0B) | hppa_op_r1(b) | hppa_op_r2(x) | hppa_op_r3(r) | (1 << 9))

#define hppa_fldws(p, d, b, t, half)			\
	p = hppa_emit (p, hppa_opcode(0x09) | hppa_op_r1(b) | (hppa_op_imm5(((int)(d))) << 16) | hppa_op_r3(t) | ((half) << 6) | (1 << 12))

#define hppa_fldds(p, d, b, t)				\
	p = hppa_emit (p, hppa_opcode(0x0B) | hppa_op_r1(b) | (hppa_op_imm5(((int)(d))) << 16) | hppa_op_r3(t) | (1 << 12))

#define hppa_fstws(p, r, half, d, b)			\
	p = hppa_emit (p, hppa_opcode(0x09) | hppa_op_r1(b) | (hppa_op_imm5(((int)(d))) << 16) | hppa_op_r3(r) | ((half) << 6) | (1 << 12) | (1 << 9))

#define hppa_fstds(p, r, d, b)				\
	p = hppa_emit (p, hppa_opcode(0x0B) | hppa_op_r1(b) | (hppa_op_imm5(((int)(d))) << 16) | hppa_op_r3(r) | (1 << 12) | (1 << 9))


/* Not yet converted old macros - used by interpreter */
#define hppa_ldd_with_flags(p, disp, base, dest, m, a)	\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
	        int neg = (disp) < 0;			\
		int im10a = (disp) >> 3;		\
		g_assert(((disp) & 7) == 0);		\
		*c++ = (0x50000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_ldd(p, disp, base, dest) \
	hppa_ldd_with_flags(p, disp, base, dest, 0, 0)

#define hppa_ldd_mb(p, disp, base, dest) \
	hppa_ldd_with_flags(p, disp, base, dest, 1, 1)

#define hppa_std_with_flags(p, src, disp, base, m, a)	\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
	        int neg = (disp) < 0;			\
		int im10a = (disp) >> 3;		\
		g_assert(((disp) & 7) == 0);		\
		*c++ = (0x70000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_std(p, disp, base, dest) \
	hppa_std_with_flags(p, disp, base, dest, 0, 0)

#define hppa_std_ma(p, disp, base, dest) \
	hppa_std_with_flags(p, disp, base, dest, 1, 0)

#define hppa_fldd_with_flags(p, disp, base, dest, m, a) \
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		int neg = (disp) < 0;			\
		int im10a = (disp) >> 3;		\
		*c++ = (0x50000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_fldd(p, disp, base, dest) \
	hppa_fldd_with_flags(p, disp, base, dest, 0, 0)

#define hppa_fstd_with_flags(p, src, disp, base, m, a)	\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		int neg = (disp) < 0;			\
		int im10a = (disp) >> 3;		\
		*c++ = (0x70000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_fstd(p, disp, base, dest) \
	hppa_fstd_with_flags(p, disp, base, dest, 0, 0)


#define hppa_fldw_with_flags(p, im11a, base, dest, r)	\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		int neg = (disp) < 0;			\
		int im11a = (disp) >> 2;		\
		*c++ = (0x5c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((dest) << 16) | neg | ((r) ? 0x2 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_fldw(p, disp, base, dest) \
	hppa_fldw_with_flags(p, disp, base, dest, 1)

#define hppa_fstw_with_flags(p, src, disp, base, r)	\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		int neg = (disp) < 0;			\
		int im11a = (disp) >> 2;		\
		*c++ = (0x7c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((src) << 16) | neg | ((r) ? 0x2 : 0)); \
		p = (void *)c;				\
	} while (0)

#define hppa_fstw(p, src, disp, base) \
	hppa_fstw_with_flags(p, src, disp, base, 1)

/* only works on right half SP registers */
#define hppa_fcnv(p, src, ssng, dest, dsng)		\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		*c++ = (0x38000200 | ((src) << 21) | ((ssng) ? 0x80 : 0x800) | (dest) | ((dsng) ? 0x40 : 0x2000)); \
		p = (void *)c;				\
	} while (0)

#define hppa_fcnv_sng_dbl(p, src, dest) \
	hppa_fcnv(p, src, 1, dest, 0)

#define hppa_fcnv_dbl_sng(p, src, dest) \
	hppa_fcnv(p, src, 0, dest, 1)

#define hppa_extrdu(p, src, pos, len, dest)		\
	do {						\
		unsigned int *c = (unsigned int *)(p);	\
		*c++ = (0xd8000000 | ((src) << 21) | ((dest) << 16) | ((pos) > 32 ? 0x800 : 0) | (((pos) & 31) << 5) | ((len) > 32 ? 0x1000 : 0) | (32 - (len & 31))); \
		p = (void *)c;				\
	} while (0)

#define hppa_bve(p, reg, link) \
	do { \
		*(p) = (0xE8001000 | ((link ? 7 : 6) << 13) | ((reg) << 21)); \
		p++; \
	} while (0)

#define hppa_blve(p, reg) \
	hppa_bve(p, reg, 1)

#endif
