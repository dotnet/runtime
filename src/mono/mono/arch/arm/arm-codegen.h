/*
 * arm-codegen.h
 * Copyright (c) 2002 Sergey Chaban <serge@wildwestsoftware.com>
 */


#ifndef ARM_H
#define ARM_H

#ifdef __cplusplus
extern "C" {
#endif

typedef unsigned int arminstr_t;
typedef unsigned int armword_t;

/* Helper functions */
arminstr_t* arm_emit_std_prologue(arminstr_t* p, unsigned int local_size);
arminstr_t* arm_emit_std_epilogue(arminstr_t* p, unsigned int local_size, int pop_regs);
arminstr_t* arm_emit_lean_prologue(arminstr_t* p, unsigned int local_size, int push_regs);
int arm_is_power_of_2(armword_t val);
int calc_arm_mov_const_shift(armword_t val);
int is_arm_const(armword_t val);
int arm_bsf(armword_t val);
arminstr_t* arm_mov_reg_imm32_cond(arminstr_t* p, int reg, armword_t imm32, int cond);
arminstr_t* arm_mov_reg_imm32(arminstr_t* p, int reg, armword_t imm32);



#if defined(_MSC_VER) || defined(__CC_NORCROFT)
	void __inline _arm_emit(arminstr_t** p, arminstr_t i) {**p = i; (*p)++;}
#	define ARM_EMIT(p, i) _arm_emit((arminstr_t**)&p, (arminstr_t)(i))
#else
#	define ARM_EMIT(p, i) *(arminstr_t*)p = (arminstr_t)i; ((arminstr_t*)p)++
#endif

/* even_scale = rot << 1 */
#define ARM_SCALE(imm8, even_scale) ( ((imm8) >> (even_scale)) | ((imm8) << (32 - even_scale)) )



typedef enum {
	ARMREG_R0 = 0,
	ARMREG_R1,
	ARMREG_R2,
	ARMREG_R3,
	ARMREG_R4,
	ARMREG_R5,
	ARMREG_R6,
	ARMREG_R7,
	ARMREG_R8,
	ARMREG_R9,
	ARMREG_R10,
	ARMREG_R11,
	ARMREG_R12,
	ARMREG_R13,
	ARMREG_R14,
	ARMREG_R15,


	/* aliases */
	/* args */
	ARMREG_A1 = ARMREG_R0,
	ARMREG_A2 = ARMREG_R1,
	ARMREG_A3 = ARMREG_R2,
	ARMREG_A4 = ARMREG_R3,

	/* local vars */
	ARMREG_V1 = ARMREG_R4,
	ARMREG_V2 = ARMREG_R5,
	ARMREG_V3 = ARMREG_R6,
	ARMREG_V4 = ARMREG_R7,
	ARMREG_V5 = ARMREG_R8,
	ARMREG_V6 = ARMREG_R9,
	ARMREG_V7 = ARMREG_R10,

	ARMREG_FP = ARMREG_R11,
	ARMREG_IP = ARMREG_R12,
	ARMREG_SP = ARMREG_R13,
	ARMREG_LR = ARMREG_R14,
	ARMREG_PC = ARMREG_R15,

	/* co-processor */
	ARMREG_CR0 = 0,
	ARMREG_CR1,
	ARMREG_CR2,
	ARMREG_CR3,
	ARMREG_CR4,
	ARMREG_CR5,
	ARMREG_CR6,
	ARMREG_CR7,
	ARMREG_CR8,
	ARMREG_CR9,
	ARMREG_CR10,
	ARMREG_CR11,
	ARMREG_CR12,
	ARMREG_CR13,
	ARMREG_CR14,
	ARMREG_CR15,

	ARMREG_MAX = ARMREG_R15
} ARMReg;

/* number of argument registers */
#define ARM_NUM_ARG_REGS 4

/* bitvector for all argument regs (A1-A4) */
#define ARM_ALL_ARG_REGS \
	(1 << ARMREG_A1) | (1 << ARMREG_A2) | (1 << ARMREG_A3) | (1 << ARMREG_A4)


typedef enum {
	ARMCOND_EQ = 0x0,          /* Equal */
	ARMCOND_NE = 0x1,          /* Not equal, or unordered */
	ARMCOND_CS = 0x2,          /* Carry set */
	ARMCOND_HS = ARMCOND_CS,   /* Unsigned higher or same */
	ARMCOND_CC = 0x3,          /* Carry clear */
	ARMCOND_LO = ARMCOND_CC,   /* Unsigned lower */
	ARMCOND_MI = 0x4,          /* Negative */
	ARMCOND_PL = 0x5,          /* Positive or zero */
	ARMCOND_VS = 0x6,          /* Overflow */
	ARMCOND_VC = 0x7,          /* No overflow */
	ARMCOND_HI = 0x8,          /* Unsigned higher */
	ARMCOND_LS = 0x9,          /* Unsigned lower or same */
	ARMCOND_GE = 0xA,          /* Signed greater than or equal */
	ARMCOND_LT = 0xB,          /* Signed less than */
	ARMCOND_GT = 0xC,          /* Signed greater than */
	ARMCOND_LE = 0xD,          /* Signed less than or equal */
	ARMCOND_AL = 0xE,          /* Always */
	ARMCOND_NV = 0xF,          /* Never */

	ARMCOND_SHIFT = 28
} ARMCond;

#define ARMCOND_MASK (ARMCOND_NV << ARMCOND_SHIFT)

#define ARM_DEF_COND(cond) (((cond) & 0xF) << ARMCOND_SHIFT)



typedef enum {
	ARMSHIFT_LSL = 0,
	ARMSHIFT_LSR = 1,
	ARMSHIFT_ASR = 2,
	ARMSHIFT_ROR = 3,

	ARMSHIFT_ASL = ARMSHIFT_LSL
	/* rrx = (ror, 1) */
} ARMShiftType;


typedef struct {
	armword_t PSR_c : 8;
	armword_t PSR_x : 8;
	armword_t PSR_s : 8;
	armword_t PSR_f : 8;
} ARMPSR;

typedef enum {
	ARMOP_AND = 0x0,
	ARMOP_EOR = 0x1,
	ARMOP_SUB = 0x2,
	ARMOP_RSB = 0x3,
	ARMOP_ADD = 0x4,
	ARMOP_ADC = 0x5,
	ARMOP_SBC = 0x6,
	ARMOP_RSC = 0x7,
	ARMOP_TST = 0x8,
	ARMOP_TEQ = 0x9,
	ARMOP_CMP = 0xa,
	ARMOP_CMN = 0xb,
	ARMOP_ORR = 0xc,
	ARMOP_MOV = 0xd,
	ARMOP_BIC = 0xe,
	ARMOP_MVN = 0xf,


	/* not really opcodes */

	ARMOP_STR = 0x0,
	ARMOP_LDR = 0x1,

	/* ARM2+ */
	ARMOP_MUL   = 0x0, /* Rd := Rm*Rs */
	ARMOP_MLA   = 0x1, /* Rd := (Rm*Rs)+Rn */

	/* ARM3M+ */
	ARMOP_UMUL  = 0x4,
	ARMOP_UMLAL = 0x5,
	ARMOP_SMULL = 0x6,
	ARMOP_SMLAL = 0x7
} ARMOpcode;


/* Generic form - all ARM instructions are conditional. */
typedef struct {
	arminstr_t icode : 28;
	arminstr_t cond  :  4;
} ARMInstrGeneric;



/* Branch or Branch with Link instructions. */
typedef struct {
	arminstr_t offset : 24;
	arminstr_t link   :  1;
	arminstr_t tag    :  3; /* 1 0 1 */
	arminstr_t cond   :  4;
} ARMInstrBR;

#define ARM_BR_ID 5
#define ARM_BR_MASK 7 << 25
#define ARM_BR_TAG ARM_BR_ID << 25

#define ARM_DEF_BR(offs, l, cond) ((offs) | ((l) << 24) | (ARM_BR_TAG) | (cond << ARMCOND_SHIFT))

/* branch */
#define ARM_B_COND(p, cond, offset) ARM_EMIT(p, ARM_DEF_BR(offset, 0, cond))
#define ARM_B(p, offs) ARM_B_COND((p), ARMCOND_AL, (offs))
/* branch with link */
#define ARM_BL_COND(p, cond, offset) ARM_EMIT(p, ARM_DEF_BR(offset, 1, cond))
#define ARM_BL(p, offs) ARM_BL_COND((p), ARMCOND_AL, (offs))



/* Data Processing Instructions - there are 3 types. */

typedef struct {
	arminstr_t imm : 8;
	arminstr_t rot : 4;
} ARMDPI_op2_imm;

typedef struct {
	arminstr_t rm   : 4;
	arminstr_t tag  : 1; /* 0 - immediate shift, 1 - reg shift */
	arminstr_t type : 2; /* shift type - logical, arithmetic, rotate */
} ARMDPI_op2_reg_shift;


/* op2 is reg shift by imm */
typedef union {
	ARMDPI_op2_reg_shift r2;
	struct {
		arminstr_t _dummy_r2 : 7;
		arminstr_t shift : 5;
	} imm;
} ARMDPI_op2_reg_imm;

/* op2 is reg shift by reg */
typedef union {
	ARMDPI_op2_reg_shift r2;
	struct {
		arminstr_t _dummy_r2 : 7;
		arminstr_t pad       : 1; /* always 0, to differentiate from HXFER etc. */
		arminstr_t rs        : 4;
	} reg;
} ARMDPI_op2_reg_reg;

/* Data processing instrs */
typedef union {
	ARMDPI_op2_imm op2_imm;

	ARMDPI_op2_reg_shift op2_reg;
	ARMDPI_op2_reg_imm op2_reg_imm;
	ARMDPI_op2_reg_reg op2_reg_reg;

	struct {
		arminstr_t op2    : 12; /* raw operand 2 */
		arminstr_t rd     :  4; /* destination reg */
		arminstr_t rn     :  4; /* first operand reg */
		arminstr_t s      :  1; /* S-bit controls PSR update */
		arminstr_t opcode :  4; /* arithmetic/logic operation */
		arminstr_t type   :  1; /* type of op2, 0 = register, 1 = immediate */
		arminstr_t tag    :  2; /* 0 0 */
		arminstr_t cond   :  4;
	} all;
} ARMInstrDPI;

#define ARM_DPI_ID 0
#define ARM_DPI_MASK 3 << 26
#define ARM_DPI_TAG ARM_DPI_ID << 26

#define ARM_DEF_DPI_IMM_COND(imm8, rot, rd, rn, s, op, cond) \
	((imm8) & 0xFF)      | \
	(((rot) & 0xF) << 8) | \
	((rd) << 12)         | \
	((rn) << 16)         | \
	((s) << 20)          | \
	((op) << 21)         | \
	(1 << 25)            | \
	(ARM_DPI_TAG)        | \
	ARM_DEF_COND(cond)


#define ARM_DEF_DPI_IMM(imm8, rot, rd, rn, s, op) \
	ARM_DEF_DPI_IMM_COND(imm8, rot, rd, rn, s, op, ARMCOND_AL)


#define ARM_DPIOP_REG_IMM8ROT_COND(p, op, rd, rn, imm8, rot, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_IMM_COND((imm8), ((rot) >> 1), (rd), (rn), 0, (op), cond))
#define ARM_DPIOP_S_REG_IMM8ROT_COND(p, op, rd, rn, imm8, rot, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_IMM_COND((imm8), ((rot) >> 1), (rd), (rn), 1, (op), cond))



#define ARM_DEF_DPI_REG_IMMSHIFT_COND(rm, shift_type, imm_shift, rd, rn, s, op, cond) \
	(rm)                        | \
	((shift_type & 3) << 5)     | \
	(((imm_shift) & 0x1F) << 7) | \
	((rd) << 12)                | \
	((rn) << 16)                | \
	((s) << 20)                 | \
	((op) << 21)                | \
	(ARM_DPI_TAG)               | \
	ARM_DEF_COND(cond)

#define ARM_DPIOP_REG_IMMSHIFT_COND(p, op, rd, rn, rm, shift_t, imm_shift, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_REG_IMMSHIFT_COND((rm), shift_t, imm_shift, (rd), (rn), 0, (op), cond))

#define ARM_DPIOP_S_REG_IMMSHIFT_COND(p, op, rd, rn, rm, shift_t, imm_shift, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_REG_IMMSHIFT_COND((rm), shift_t, imm_shift, (rd), (rn), 1, (op), cond))

#define ARM_DPIOP_REG_REG_COND(p, op, rd, rn, rm, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_REG_IMMSHIFT_COND((rm), ARMSHIFT_LSL, 0, (rd), (rn), 0, (op), cond))

#define ARM_DPIOP_S_REG_REG_COND(p, op, rd, rn, rm, cond) \
	ARM_EMIT(p, ARM_DEF_DPI_REG_IMMSHIFT_COND((rm), ARMSHIFT_LSL, 0, (rd), (rn), 1, (op), cond))





/* Multiple register transfer. */
typedef struct {
	arminstr_t reg_list : 16; /* bitfield */
	arminstr_t rn       :  4; /* base reg */
	arminstr_t ls       :  1; /* load(1)/store(0) */
	arminstr_t wb       :  1; /* write-back "!" */
	arminstr_t s        :  1; /* restore PSR, force user bit */
	arminstr_t u        :  1; /* up/down */
	arminstr_t p        :  1; /* pre(1)/post(0) index */
	arminstr_t tag      :  3; /* 1 0 0 */
	arminstr_t cond     :  4;
} ARMInstrMRT;

#define ARM_MRT_ID 4
#define ARM_MRT_MASK 7 << 25
#define ARM_MRT_TAG ARM_MRT_ID << 25

#define ARM_DEF_MRT(regs, rn, l, w, s, u, p, cond) \
		(regs)        | \
		(rn << 16)    | \
		(l << 20)     | \
		(w << 21)     | \
		(s << 22)     | \
		(u << 23)     | \
		(p << 24)     | \
		(ARM_MRT_TAG) | \
		ARM_DEF_COND(cond)



/* stmdb sp!, {regs} */
#define ARM_PUSH(p, regs) ARM_EMIT(p, ARM_DEF_MRT(regs, ARMREG_SP, 0, 1, 0, 0, 1, ARMCOND_AL))

/* ldmia sp!, {regs} */
#define ARM_POP(p, regs) ARM_EMIT(p, ARM_DEF_MRT(regs, ARMREG_SP, 1, 1, 0, 1, 0, ARMCOND_AL))

/* ldmia sp, {regs} ; (no write-back) */
#define ARM_POP_NWB(p, regs) ARM_EMIT(p, ARM_DEF_MRT(regs, ARMREG_SP, 1, 0, 0, 1, 0, ARMCOND_AL))



/* Multiply instructions */
typedef struct {
	arminstr_t rm     : 4;
	arminstr_t tag2   : 4;   /* 9 */
	arminstr_t rs     : 4;
	arminstr_t rn     : 4;
	arminstr_t rd     : 4;
	arminstr_t s      : 1;
	arminstr_t opcode : 3;
	arminstr_t tag    : 4;
	arminstr_t cond   : 4;
} ARMInstrMul;

#define ARM_MUL_ID 0
#define ARM_MUL_ID2 9
#define ARM_MUL_MASK ((0xF << 24) | (0xF << 4))
#define ARM_MUL_TAG ((ARM_MUL_ID << 24) | (ARM_MUL_ID2 << 4))


/*  Word/byte transfer */
typedef union {
	ARMDPI_op2_reg_imm op2_reg_imm;
	struct {
		arminstr_t op2_imm : 12;
		arminstr_t rd      :  4;
		arminstr_t rn      :  4;
		arminstr_t ls      :  1;
		arminstr_t wb      :  1;
		arminstr_t b       :  1;
		arminstr_t u       :  1;
		arminstr_t p       :  1; /* post-index(0) / pre-index(1) */
		arminstr_t type    :  1; /* imm(0) / register(1) */
		arminstr_t tag     :  2; /* 0 1 */
		arminstr_t cond    :  4;
	} all;
} ARMInstrWXfer;

#define ARM_WXFER_ID 1
#define ARM_WXFER_MASK 3 << 26
#define ARM_WXFER_TAG ARM_WXFER_ID << 26


#define ARM_DEF_WXFER_IMM(imm12, rd, rn, ls, wb, b, p, cond) \
	((((int)imm12) < 0) ? -(int)(imm12) : (imm12)) | \
	((rd) << 12)                                   | \
	((rn) << 16)                                   | \
	((ls) << 20)                                   | \
	((wb) << 21)                                   | \
	((b)  << 22)                                   | \
	(((int)(imm12) >= 0) << 23)                    | \
	((p) << 24)                                    | \
	ARM_WXFER_TAG                                  | \
	ARM_DEF_COND(cond)

#define ARM_WXFER_MAX_OFFS 0xFFF

/* this macro checks imm12 bounds */
#define ARM_EMIT_WXFER_IMM(ptr, imm12, rd, rn, ls, wb, b, p, cond) \
	do { \
		int _imm12 = (int)(imm12) < -ARM_WXFER_MAX_OFFS  \
		             ? -ARM_WXFER_MAX_OFFS               \
		             : (int)(imm12) > ARM_WXFER_MAX_OFFS \
		             ? ARM_WXFER_MAX_OFFS                \
		             : (int)(imm12);                     \
		ARM_EMIT((ptr), \
		ARM_DEF_WXFER_IMM(_imm12, (rd), (rn), (ls), (wb), (b), (p), (cond))); \
	} while (0)


/* LDRx */
/* immediate offset, post-index */
#define ARM_LDR_IMM_POST_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_LDR, 0, 0, 0, cond))

#define ARM_LDR_IMM_POST(p, rd, rn, imm) ARM_LDR_IMM_POST_COND(p, rd, rn, imm, ARMCOND_AL)

#define ARM_LDRB_IMM_POST_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_LDR, 0, 1, 0, cond))

#define ARM_LDRB_IMM_POST(p, rd, rn, imm) ARM_LDRB_IMM_POST_COND(p, rd, rn, imm, ARMCOND_AL)

/* immediate offset, pre-index */
#define ARM_LDR_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_LDR, 0, 0, 1, cond))

#define ARM_LDR_IMM(p, rd, rn, imm) ARM_LDR_IMM_COND(p, rd, rn, imm, ARMCOND_AL)

#define ARM_LDRB_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_LDR, 0, 1, 1, cond))

#define ARM_LDRB_IMM(p, rd, rn, imm) ARM_LDRB_IMM_COND(p, rd, rn, imm, ARMCOND_AL)

/* STRx */
/* immediate offset, post-index */
#define ARM_STR_IMM_POST_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_STR, 0, 0, 0, cond))

#define ARM_STR_IMM_POST(p, rd, rn, imm) ARM_STR_IMM_POST_COND(p, rd, rn, imm, ARMCOND_AL)

#define ARM_STRB_IMM_POST_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_STR, 0, 1, 0, cond))

#define ARM_STRB_IMM_POST(p, rd, rn, imm) ARM_STRB_IMM_POST_COND(p, rd, rn, imm, ARMCOND_AL)

/* immediate offset, pre-index */
#define ARM_STR_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT_WXFER_IMM(p, imm, rd, rn, ARMOP_STR, 0, 0, 1, cond)
/*	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_STR, 0, 0, 1, cond)) */

#define ARM_STR_IMM(p, rd, rn, imm) ARM_STR_IMM_COND(p, rd, rn, imm, ARMCOND_AL)

#define ARM_STRB_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_IMM(imm, rd, rn, ARMOP_STR, 0, 1, 1, cond))

#define ARM_STRB_IMM(p, rd, rn, imm) ARM_STRB_IMM_COND(p, rd, rn, imm, ARMCOND_AL)



#define ARM_DEF_WXFER_REG_REG(rm, shift_type, shift, rd, rn, ls, wb, b, p, cond) \
	(rm)                | \
	((shift_type) << 5) | \
	((shift) << 7)      | \
	((rd) << 12)        | \
	((rn) << 16)        | \
	((ls) << 20)        | \
	((wb) << 21)        | \
	((b)  << 22)        | \
	((p) << 24)         | \
	(1 << 25)           | \
	ARM_WXFER_TAG       | \
	ARM_DEF_COND(cond)


#define ARM_LDR_REG_REG_SHIFT_COND(p, rd, rn, rm, shift_type, shift, cond) \
	ARM_EMIT(p, ARM_DEF_WXFER_REG_REG(rm, shift_type, shift, rd, rn, ARMOP_LDR, 0, 0, 1, cond))
#define ARM_LDR_REG_REG_SHIFT(p, rd, rn, rm, shift_type, shift) \
	ARM_LDR_REG_REG_SHIFT_COND(p, rd, rn, rm, shift_type, shift, ARMCOND_AL)
#define ARM_LDR_REG_REG(p, rd, rn, rm) \
	ARM_LDR_REG_REG_SHIFT(p, rd, rn, rm, ARMSHIFT_LSL, 0)



/* Half-word or byte (signed) transfer. */
typedef struct {
	arminstr_t rm     : 4; /* imm_lo */
	arminstr_t tag3   : 1; /* 1 */
	arminstr_t h      : 1; /* half-word or byte */
	arminstr_t s      : 1; /* sign-extend or zero-extend */
	arminstr_t tag2   : 1; /* 1 */
	arminstr_t imm_hi : 4;
	arminstr_t rd     : 4;
	arminstr_t rn     : 4;
	arminstr_t ls     : 1;
	arminstr_t wb     : 1;
	arminstr_t type   : 1; /* imm(1) / reg(0) */
	arminstr_t u      : 1; /* +- */
	arminstr_t p      : 1; /* pre/post-index */
	arminstr_t tag    : 3;
	arminstr_t cond   : 4;
} ARMInstrHXfer;

#define ARM_HXFER_ID 0
#define ARM_HXFER_ID2 1
#define ARM_HXFER_ID3 1
#define ARM_HXFER_MASK ((0x7 << 25) | (0x9 << 4))
#define ARM_HXFER_TAG ((ARM_HXFER_ID << 25) | (ARM_HXFER_ID2 << 7) | (ARM_HXFER_ID3 << 4))

#define ARM_DEF_HXFER_IMM_COND(imm, h, s, rd, rn, ls, wb, p, cond) \
	((imm) & 0xF)               | \
	((h) << 5)                  | \
	((s) << 6)                  | \
	(((imm) << 4) & (0xF << 8)) | \
	((rd) << 12)                | \
	((rn) << 16)                | \
	((ls) << 20)                | \
	((wb) << 21)                | \
	(1 << 22)                   | \
	(((int)(imm) >= 0) << 23)   | \
	((p) << 24)                 | \
	ARM_HXFER_TAG               | \
	ARM_DEF_COND(cond)

#define ARM_LDRH_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_HXFER_IMM_COND(imm, 1, 0, rd, rn, ARMOP_LDR, 0, 1, cond))
#define ARM_LDRH_IMM(p, rd, rn, imm) \
	ARM_LDRH_IMM_COND(p, rd, rn, imm, ARMCOND_AL)
#define ARM_LDRSH_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_HXFER_IMM_COND(imm, 1, 1, rd, rn, ARMOP_LDR, 0, 1, cond))
#define ARM_LDRSH_IMM(p, rd, rn, imm) \
	ARM_LDRSH_IMM_COND(p, rd, rn, imm, ARMCOND_AL)
#define ARM_LDRSB_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_HXFER_IMM_COND(imm, 0, 1, rd, rn, ARMOP_LDR, 0, 1, cond))
#define ARM_LDRSB_IMM(p, rd, rn, imm) \
	ARM_LDRSB_IMM_COND(p, rd, rn, imm, ARMCOND_AL)


#define ARM_STRH_IMM_COND(p, rd, rn, imm, cond) \
	ARM_EMIT(p, ARM_DEF_HXFER_IMM_COND(imm, 1, 0, rd, rn, ARMOP_STR, 0, 1, cond))
#define ARM_STRH_IMM(p, rd, rn, imm) \
	ARM_STRH_IMM_COND(p, rd, rn, imm, ARMCOND_AL)



/* Swap */
typedef struct {
	arminstr_t rm   : 4;
	arminstr_t tag3 : 8; /* 0x9 */
	arminstr_t rd   : 4;
	arminstr_t rn   : 4;
	arminstr_t tag2 : 2;
	arminstr_t b    : 1;
	arminstr_t tag  : 5; /* 0x2 */
	arminstr_t cond : 4;
} ARMInstrSwap;

#define ARM_SWP_ID 2
#define ARM_SWP_ID2 9
#define ARM_SWP_MASK ((0x1F << 23) | (3 << 20) | (0xFF << 4))
#define ARM_SWP_TAG ((ARM_SWP_ID << 23) | (ARM_SWP_ID2 << 4))



/* Software interrupt */
typedef struct {
	arminstr_t num  : 24;
	arminstr_t tag  :  4;
	arminstr_t cond :  4;
} ARMInstrSWI;

#define ARM_SWI_ID 0xF
#define ARM_SWI_MASK (0xF << 24)
#define ARM_SWI_TAG (ARM_SWI_ID << 24)



/* Co-processor Data Processing */
typedef struct {
	arminstr_t crm  : 4;
	arminstr_t tag2 : 1; /* 0 */
	arminstr_t op2  : 3;
	arminstr_t cpn  : 4; /* CP number */
	arminstr_t crd  : 4;
	arminstr_t crn  : 4;
	arminstr_t op   : 4;
	arminstr_t tag  : 4; /* 0xE */
	arminstr_t cond : 4;
} ARMInstrCDP;

#define ARM_CDP_ID 0xE
#define ARM_CDP_ID2 0
#define ARM_CDP_MASK ((0xF << 24) | (1 << 4))
#define ARM_CDP_TAG ((ARM_CDP_ID << 24) | (ARM_CDP_ID2 << 4))


/* Co-processor Data Transfer (ldc/stc) */
typedef struct {
	arminstr_t offs : 8;
	arminstr_t cpn  : 4;
	arminstr_t crd  : 4;
	arminstr_t rn   : 4;
	arminstr_t ls   : 1;
	arminstr_t wb   : 1;
	arminstr_t n    : 1;
	arminstr_t u    : 1;
	arminstr_t p    : 1;
	arminstr_t tag  : 3;
	arminstr_t cond : 4;
} ARMInstrCDT;

#define ARM_CDT_ID 6
#define ARM_CDT_MASK (7 << 25)
#define ARM_CDT_TAG (ARM_CDT_ID << 25)


/* Co-processor Register Transfer (mcr/mrc) */
typedef struct {
	arminstr_t crm  : 4;
	arminstr_t tag2 : 1;
	arminstr_t op2  : 3;
	arminstr_t cpn  : 4;
	arminstr_t rd   : 4;
	arminstr_t crn  : 4;
	arminstr_t ls   : 1;
	arminstr_t op1  : 3;
	arminstr_t tag  : 4;
	arminstr_t cond : 4;
} ARMInstrCRT;

#define ARM_CRT_ID 0xE
#define ARM_CRT_ID2 0x1
#define ARM_CRT_MASK ((0xF << 24) | (1 << 4))
#define ARM_CRT_TAG ((ARM_CRT_ID << 24) | (ARM_CRT_ID2 << 4))

/* Move register to PSR. */
typedef union {
	ARMDPI_op2_imm op2_imm;
	struct {
		arminstr_t rm   : 4;
		arminstr_t pad  : 8; /* 0 */
		arminstr_t tag4 : 4; /* 0xF */
		arminstr_t fld  : 4;
		arminstr_t tag3 : 2; /* 0x2 */
		arminstr_t sel  : 1;
		arminstr_t tag2 : 2; /* 0x2 */
		arminstr_t type : 1;
		arminstr_t tag  : 2; /* 0 */
		arminstr_t cond : 4;
	} all;
} ARMInstrMSR;

#define ARM_MSR_ID 0
#define ARM_MSR_ID2 2
#define ARM_MSR_ID3 2
#define ARM_MSR_ID4 0xF
#define ARM_MSR_MASK ((3 << 26) | \
                      (3 << 23) | \
                      (3 << 20) | \
                      (0xF << 12))
#define ARM_MSR_TAG ((ARM_MSR_ID << 26)  | \
                     (ARM_MSR_ID2 << 23) | \
                     (ARM_MSR_ID3 << 20) | \
                     (ARM_MSR_ID4 << 12))


/* Move PSR to register. */
typedef struct {
	arminstr_t tag3 : 12;
	arminstr_t rd   :  4;
	arminstr_t tag2 :  6;
	arminstr_t sel  :  1; /* CPSR | SPSR */
	arminstr_t tag  :  5;
	arminstr_t cond :  4;
} ARMInstrMRS;

#define ARM_MRS_ID 2
#define ARM_MRS_ID2 0xF
#define ARM_MRS_ID3 0
#define ARM_MRS_MASK ((0x1F << 23) | (0x3F << 16) | 0xFFF)
#define ARM_MRS_TAG ((ARM_MRS_ID << 23) | (ARM_MRS_ID2 << 16) | ARM_MRS_ID3)



typedef union {
	ARMInstrBR    br;
	ARMInstrDPI   dpi;
	ARMInstrMRT   mrt;
	ARMInstrMul   mul;
	ARMInstrWXfer wxfer;
	ARMInstrHXfer hxfer;
	ARMInstrSwap  swp;
	ARMInstrCDP   cdp;
	ARMInstrCDT   cdt;
	ARMInstrCRT   crt;
	ARMInstrSWI   swi;
	ARMInstrMSR   msr;
	ARMInstrMRS   mrs;

	ARMInstrGeneric generic;
	arminstr_t      raw;
} ARMInstr;


#include "arm_dpimacros.h"

#define ARM_NOP(p) ARM_MOV_REG_REG(p, ARMREG_R0, ARMREG_R0)


#ifdef __cplusplus
}
#endif

#endif /* ARM_H */



