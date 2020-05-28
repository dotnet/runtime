/* Macros for DPI ops, auto-generated from template */


/* mov/mvn */

/* Rd := imm8 ROR rot */
#define ARM_MOV_REG_IMM_COND(p, reg, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_MOV, reg, 0, imm8, rot, cond)
#define ARM_MOV_REG_IMM(p, reg, imm8, rot) \
	ARM_MOV_REG_IMM_COND(p, reg, imm8, rot, ARMCOND_AL)
/* S */
#define ARM_MOVS_REG_IMM_COND(p, reg, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_MOV, reg, 0, imm8, rot, cond)
#define ARM_MOVS_REG_IMM(p, reg, imm8, rot) \
	ARM_MOVS_REG_IMM_COND(p, reg, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MOV_REG_IMM_COND(reg, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_MOV, reg, 0, imm8, rot, cond)
#define _MOV_REG_IMM(reg, imm8, rot) \
	_MOV_REG_IMM_COND(reg, imm8, rot, ARMCOND_AL)
/* S */
#define _MOVS_REG_IMM_COND(reg, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_MOV, reg, 0, imm8, rot, cond)
#define _MOVS_REG_IMM(reg, imm8, rot) \
	_MOVS_REG_IMM_COND(reg, imm8, rot, ARMCOND_AL)
#endif


/* Rd := imm8 */
#define ARM_MOV_REG_IMM8_COND(p, reg, imm8, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_MOV, reg, 0, imm8, 0, cond)
#define ARM_MOV_REG_IMM8(p, reg, imm8) \
	ARM_MOV_REG_IMM8_COND(p, reg, imm8, ARMCOND_AL)
/* S */
#define ARM_MOVS_REG_IMM8_COND(p, reg, imm8, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_MOV, reg, 0, imm8, 0, cond)
#define ARM_MOVS_REG_IMM8(p, reg, imm8) \
	ARM_MOVS_REG_IMM8_COND(p, reg, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MOV_REG_IMM8_COND(reg, imm8, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_MOV, reg, 0, imm8, 0, cond)
#define _MOV_REG_IMM8(reg, imm8) \
	_MOV_REG_IMM8_COND(reg, imm8, ARMCOND_AL)
/* S */
#define _MOVS_REG_IMM8_COND(reg, imm8, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_MOV, reg, 0, imm8, 0, cond)
#define _MOVS_REG_IMM8(reg, imm8) \
	_MOVS_REG_IMM8_COND(reg, imm8, ARMCOND_AL)
#endif


/* Rd := Rm */
#define ARM_MOV_REG_REG_COND(p, rd, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_MOV, rd, 0, rm, cond)
#define ARM_MOV_REG_REG(p, rd, rm) \
	ARM_MOV_REG_REG_COND(p, rd, rm, ARMCOND_AL)
/* S */
#define ARM_MOVS_REG_REG_COND(p, rd, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_MOV, rd, 0, rm, cond)
#define ARM_MOVS_REG_REG(p, rd, rm) \
	ARM_MOVS_REG_REG_COND(p, rd, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MOV_REG_REG_COND(rd, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_MOV, rd, 0, rm, cond)
#define _MOV_REG_REG(rd, rm) \
	_MOV_REG_REG_COND(rd, rm, ARMCOND_AL)
/* S */
#define _MOVS_REG_REG_COND(rd, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_MOV, rd, 0, rm, cond)
#define _MOVS_REG_REG(rd, rm) \
	_MOVS_REG_REG_COND(rd, rm, ARMCOND_AL)
#endif


/* Rd := Rm <shift_type> imm_shift */
#define ARM_MOV_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_MOV, rd, 0, rm, shift_type, imm_shift, cond)
#define ARM_MOV_REG_IMMSHIFT(p, rd, rm, shift_type, imm_shift) \
	ARM_MOV_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, ARMCOND_AL)
/* S */
#define ARM_MOVS_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_MOV, rd, 0, rm, shift_type, imm_shift, cond)
#define ARM_MOVS_REG_IMMSHIFT(p, rd, rm, shift_type, imm_shift) \
	ARM_MOVS_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MOV_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_MOV, rd, 0, rm, shift_type, imm_shift, cond)
#define _MOV_REG_IMMSHIFT(rd, rm, shift_type, imm_shift) \
	_MOV_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, ARMCOND_AL)
/* S */
#define _MOVS_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_MOV, rd, 0, rm, shift_type, imm_shift, cond)
#define _MOVS_REG_IMMSHIFT(rd, rm, shift_type, imm_shift) \
	_MOVS_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, ARMCOND_AL)
#endif



/* Rd := (Rm <shift_type> Rs) */
#define ARM_MOV_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_MOV, rd, 0, rm, shift_type, rs, cond)
#define ARM_MOV_REG_REGSHIFT(p, rd, rm, shift_type, rs) \
	ARM_MOV_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, ARMCOND_AL)
/* S */
#define ARM_MOVS_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_MOV, rd, 0, rm, shift_type, rs, cond)
#define ARM_MOVS_REG_REGSHIFT(p, rd, rm, shift_type, rs) \
	ARM_MOVS_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MOV_REG_REGSHIFT_COND(rd, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_MOV, rd, 0, rm, shift_type, rs, cond)
#define _MOV_REG_REGSHIFT(rd, rm, shift_type, rs) \
	_MOV_REG_REGSHIFT_COND(rd, rm, shift_type, rs, ARMCOND_AL)
/* S */
#define _MOVS_REG_REGSHIFT_COND(rd, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_MOV, rd, 0, rm, shift_type, rs, cond)
#define _MOVS_REG_REGSHIFT(rd, rm, shift_type, rs) \
	_MOVS_REG_REGSHIFT_COND(rd, rm, shift_type, rs, ARMCOND_AL)
#endif


/* Rd := imm8 ROR rot */
#define ARM_MVN_REG_IMM_COND(p, reg, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_MVN, reg, 0, imm8, rot, cond)
#define ARM_MVN_REG_IMM(p, reg, imm8, rot) \
	ARM_MVN_REG_IMM_COND(p, reg, imm8, rot, ARMCOND_AL)
/* S */
#define ARM_MVNS_REG_IMM_COND(p, reg, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_MVN, reg, 0, imm8, rot, cond)
#define ARM_MVNS_REG_IMM(p, reg, imm8, rot) \
	ARM_MVNS_REG_IMM_COND(p, reg, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MVN_REG_IMM_COND(reg, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_MVN, reg, 0, imm8, rot, cond)
#define _MVN_REG_IMM(reg, imm8, rot) \
	_MVN_REG_IMM_COND(reg, imm8, rot, ARMCOND_AL)
/* S */
#define _MVNS_REG_IMM_COND(reg, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_MVN, reg, 0, imm8, rot, cond)
#define _MVNS_REG_IMM(reg, imm8, rot) \
	_MVNS_REG_IMM_COND(reg, imm8, rot, ARMCOND_AL)
#endif


/* Rd := imm8 */
#define ARM_MVN_REG_IMM8_COND(p, reg, imm8, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_MVN, reg, 0, imm8, 0, cond)
#define ARM_MVN_REG_IMM8(p, reg, imm8) \
	ARM_MVN_REG_IMM8_COND(p, reg, imm8, ARMCOND_AL)
/* S */
#define ARM_MVNS_REG_IMM8_COND(p, reg, imm8, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_MVN, reg, 0, imm8, 0, cond)
#define ARM_MVNS_REG_IMM8(p, reg, imm8) \
	ARM_MVNS_REG_IMM8_COND(p, reg, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MVN_REG_IMM8_COND(reg, imm8, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_MVN, reg, 0, imm8, 0, cond)
#define _MVN_REG_IMM8(reg, imm8) \
	_MVN_REG_IMM8_COND(reg, imm8, ARMCOND_AL)
/* S */
#define _MVNS_REG_IMM8_COND(reg, imm8, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_MVN, reg, 0, imm8, 0, cond)
#define _MVNS_REG_IMM8(reg, imm8) \
	_MVNS_REG_IMM8_COND(reg, imm8, ARMCOND_AL)
#endif


/* Rd := Rm */
#define ARM_MVN_REG_REG_COND(p, rd, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_MVN, rd, 0, rm, cond)
#define ARM_MVN_REG_REG(p, rd, rm) \
	ARM_MVN_REG_REG_COND(p, rd, rm, ARMCOND_AL)
/* S */
#define ARM_MVNS_REG_REG_COND(p, rd, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_MVN, rd, 0, rm, cond)
#define ARM_MVNS_REG_REG(p, rd, rm) \
	ARM_MVNS_REG_REG_COND(p, rd, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MVN_REG_REG_COND(rd, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_MVN, rd, 0, rm, cond)
#define _MVN_REG_REG(rd, rm) \
	_MVN_REG_REG_COND(rd, rm, ARMCOND_AL)
/* S */
#define _MVNS_REG_REG_COND(rd, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_MVN, rd, 0, rm, cond)
#define _MVNS_REG_REG(rd, rm) \
	_MVNS_REG_REG_COND(rd, rm, ARMCOND_AL)
#endif


/* Rd := Rm <shift_type> imm_shift */
#define ARM_MVN_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_MVN, rd, 0, rm, shift_type, imm_shift, cond)
#define ARM_MVN_REG_IMMSHIFT(p, rd, rm, shift_type, imm_shift) \
	ARM_MVN_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, ARMCOND_AL)
/* S */
#define ARM_MVNS_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_MVN, rd, 0, rm, shift_type, imm_shift, cond)
#define ARM_MVNS_REG_IMMSHIFT(p, rd, rm, shift_type, imm_shift) \
	ARM_MVNS_REG_IMMSHIFT_COND(p, rd, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MVN_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_MVN, rd, 0, rm, shift_type, imm_shift, cond)
#define _MVN_REG_IMMSHIFT(rd, rm, shift_type, imm_shift) \
	_MVN_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, ARMCOND_AL)
/* S */
#define _MVNS_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_MVN, rd, 0, rm, shift_type, imm_shift, cond)
#define _MVNS_REG_IMMSHIFT(rd, rm, shift_type, imm_shift) \
	_MVNS_REG_IMMSHIFT_COND(rd, rm, shift_type, imm_shift, ARMCOND_AL)
#endif



/* Rd := (Rm <shift_type> Rs) */
#define ARM_MVN_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_MVN, rd, 0, rm, shift_type, rs, cond)
#define ARM_MVN_REG_REGSHIFT(p, rd, rm, shift_type, rs) \
	ARM_MVN_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, ARMCOND_AL)
/* S */
#define ARM_MVNS_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_MVN, rd, 0, rm, shift_type, rs, cond)
#define ARM_MVNS_REG_REGSHIFT(p, rd, rm, shift_type, rs) \
	ARM_MVNS_REG_REGSHIFT_COND(p, rd, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _MVN_REG_REGSHIFT_COND(rd, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_MVN, rd, 0, rm, shift_type, rs, cond)
#define _MVN_REG_REGSHIFT(rd, rm, shift_type, rs) \
	_MVN_REG_REGSHIFT_COND(rd, rm, shift_type, rs, ARMCOND_AL)
/* S */
#define _MVNS_REG_REGSHIFT_COND(rd, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_MVN, rd, 0, rm, shift_type, rs, cond)
#define _MVNS_REG_REGSHIFT(rd, rm, shift_type, rs) \
	_MVNS_REG_REGSHIFT_COND(rd, rm, shift_type, rs, ARMCOND_AL)
#endif



/* DPIs, arithmetic and logical */

/* -- AND -- */

/* Rd := Rn AND (imm8 ROR rot) ; rot is power of 2 */
#define ARM_AND_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_AND, rd, rn, imm8, rot, cond)
#define ARM_AND_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_AND_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_ANDS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_AND, rd, rn, imm8, rot, cond)
#define ARM_ANDS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ANDS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _AND_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_AND, rd, rn, imm8, rot, cond)
#define _AND_REG_IMM(rd, rn, imm8, rot) \
	_AND_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _ANDS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_AND, rd, rn, imm8, rot, cond)
#define _ANDS_REG_IMM(rd, rn, imm8, rot) \
	_ANDS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn AND imm8 */
#define ARM_AND_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_AND_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_AND_REG_IMM8(p, rd, rn, imm8) \
	ARM_AND_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_ANDS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ANDS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ANDS_REG_IMM8(p, rd, rn, imm8) \
	ARM_ANDS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _AND_REG_IMM8_COND(rd, rn, imm8, cond) \
	_AND_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _AND_REG_IMM8(rd, rn, imm8) \
	_AND_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _ANDS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ANDS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ANDS_REG_IMM8(rd, rn, imm8) \
	_ANDS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn AND Rm */
#define ARM_AND_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_AND, rd, rn, rm, cond)
#define ARM_AND_REG_REG(p, rd, rn, rm) \
	ARM_AND_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_ANDS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_AND, rd, rn, rm, cond)
#define ARM_ANDS_REG_REG(p, rd, rn, rm) \
	ARM_ANDS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _AND_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_AND, rd, rn, rm, cond)
#define _AND_REG_REG(rd, rn, rm) \
	_AND_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _ANDS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_AND, rd, rn, rm, cond)
#define _ANDS_REG_REG(rd, rn, rm) \
	_ANDS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn AND (Rm <shift_type> imm_shift) */
#define ARM_AND_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_AND, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_AND_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_AND_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_ANDS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_AND, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ANDS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ANDS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _AND_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_AND, rd, rn, rm, shift_type, imm_shift, cond)
#define _AND_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_AND_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _ANDS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_AND, rd, rn, rm, shift_type, imm_shift, cond)
#define _ANDS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ANDS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn AND (Rm <shift_type> Rs) */
#define ARM_AND_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_AND, rd, rn, rm, shift_t, rs, cond)
#define ARM_AND_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_AND_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_ANDS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_AND, rd, rn, rm, shift_t, rs, cond)
#define ARM_ANDS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ANDS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _AND_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_AND, rd, rn, rm, shift_t, rs, cond)
#define _AND_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_AND_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _ANDS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_AND, rd, rn, rm, shift_t, rs, cond)
#define _ANDS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ANDS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- EOR -- */

/* Rd := Rn EOR (imm8 ROR rot) ; rot is power of 2 */
#define ARM_EOR_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_EOR, rd, rn, imm8, rot, cond)
#define ARM_EOR_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_EOR_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_EORS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_EOR, rd, rn, imm8, rot, cond)
#define ARM_EORS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_EORS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _EOR_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_EOR, rd, rn, imm8, rot, cond)
#define _EOR_REG_IMM(rd, rn, imm8, rot) \
	_EOR_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _EORS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_EOR, rd, rn, imm8, rot, cond)
#define _EORS_REG_IMM(rd, rn, imm8, rot) \
	_EORS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn EOR imm8 */
#define ARM_EOR_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_EOR_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_EOR_REG_IMM8(p, rd, rn, imm8) \
	ARM_EOR_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_EORS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_EORS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_EORS_REG_IMM8(p, rd, rn, imm8) \
	ARM_EORS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _EOR_REG_IMM8_COND(rd, rn, imm8, cond) \
	_EOR_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _EOR_REG_IMM8(rd, rn, imm8) \
	_EOR_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _EORS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_EORS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _EORS_REG_IMM8(rd, rn, imm8) \
	_EORS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn EOR Rm */
#define ARM_EOR_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_EOR, rd, rn, rm, cond)
#define ARM_EOR_REG_REG(p, rd, rn, rm) \
	ARM_EOR_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_EORS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_EOR, rd, rn, rm, cond)
#define ARM_EORS_REG_REG(p, rd, rn, rm) \
	ARM_EORS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _EOR_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_EOR, rd, rn, rm, cond)
#define _EOR_REG_REG(rd, rn, rm) \
	_EOR_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _EORS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_EOR, rd, rn, rm, cond)
#define _EORS_REG_REG(rd, rn, rm) \
	_EORS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn EOR (Rm <shift_type> imm_shift) */
#define ARM_EOR_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_EOR, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_EOR_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_EOR_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_EORS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_EOR, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_EORS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_EORS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _EOR_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_EOR, rd, rn, rm, shift_type, imm_shift, cond)
#define _EOR_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_EOR_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _EORS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_EOR, rd, rn, rm, shift_type, imm_shift, cond)
#define _EORS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_EORS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn EOR (Rm <shift_type> Rs) */
#define ARM_EOR_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_EOR, rd, rn, rm, shift_t, rs, cond)
#define ARM_EOR_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_EOR_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_EORS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_EOR, rd, rn, rm, shift_t, rs, cond)
#define ARM_EORS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_EORS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _EOR_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_EOR, rd, rn, rm, shift_t, rs, cond)
#define _EOR_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_EOR_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _EORS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_EOR, rd, rn, rm, shift_t, rs, cond)
#define _EORS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_EORS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- SUB -- */

/* Rd := Rn SUB (imm8 ROR rot) ; rot is power of 2 */
#define ARM_SUB_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_SUB, rd, rn, imm8, rot, cond)
#define ARM_SUB_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_SUB_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_SUBS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_SUB, rd, rn, imm8, rot, cond)
#define ARM_SUBS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_SUBS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SUB_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_SUB, rd, rn, imm8, rot, cond)
#define _SUB_REG_IMM(rd, rn, imm8, rot) \
	_SUB_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _SUBS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_SUB, rd, rn, imm8, rot, cond)
#define _SUBS_REG_IMM(rd, rn, imm8, rot) \
	_SUBS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn SUB imm8 */
#define ARM_SUB_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_SUB_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_SUB_REG_IMM8(p, rd, rn, imm8) \
	ARM_SUB_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_SUBS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_SUBS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_SUBS_REG_IMM8(p, rd, rn, imm8) \
	ARM_SUBS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SUB_REG_IMM8_COND(rd, rn, imm8, cond) \
	_SUB_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _SUB_REG_IMM8(rd, rn, imm8) \
	_SUB_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _SUBS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_SUBS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _SUBS_REG_IMM8(rd, rn, imm8) \
	_SUBS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn SUB Rm */
#define ARM_SUB_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_SUB, rd, rn, rm, cond)
#define ARM_SUB_REG_REG(p, rd, rn, rm) \
	ARM_SUB_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_SUBS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_SUB, rd, rn, rm, cond)
#define ARM_SUBS_REG_REG(p, rd, rn, rm) \
	ARM_SUBS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SUB_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_SUB, rd, rn, rm, cond)
#define _SUB_REG_REG(rd, rn, rm) \
	_SUB_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _SUBS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_SUB, rd, rn, rm, cond)
#define _SUBS_REG_REG(rd, rn, rm) \
	_SUBS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn SUB (Rm <shift_type> imm_shift) */
#define ARM_SUB_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_SUB, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_SUB_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_SUB_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_SUBS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_SUB, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_SUBS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_SUBS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SUB_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_SUB, rd, rn, rm, shift_type, imm_shift, cond)
#define _SUB_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_SUB_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _SUBS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_SUB, rd, rn, rm, shift_type, imm_shift, cond)
#define _SUBS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_SUBS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn SUB (Rm <shift_type> Rs) */
#define ARM_SUB_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_SUB, rd, rn, rm, shift_t, rs, cond)
#define ARM_SUB_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_SUB_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_SUBS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_SUB, rd, rn, rm, shift_t, rs, cond)
#define ARM_SUBS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_SUBS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SUB_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_SUB, rd, rn, rm, shift_t, rs, cond)
#define _SUB_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_SUB_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _SUBS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_SUB, rd, rn, rm, shift_t, rs, cond)
#define _SUBS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_SUBS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- RSB -- */

/* Rd := Rn RSB (imm8 ROR rot) ; rot is power of 2 */
#define ARM_RSB_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_RSB, rd, rn, imm8, rot, cond)
#define ARM_RSB_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_RSB_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_RSBS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_RSB, rd, rn, imm8, rot, cond)
#define ARM_RSBS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_RSBS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSB_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_RSB, rd, rn, imm8, rot, cond)
#define _RSB_REG_IMM(rd, rn, imm8, rot) \
	_RSB_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _RSBS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_RSB, rd, rn, imm8, rot, cond)
#define _RSBS_REG_IMM(rd, rn, imm8, rot) \
	_RSBS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn RSB imm8 */
#define ARM_RSB_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_RSB_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_RSB_REG_IMM8(p, rd, rn, imm8) \
	ARM_RSB_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_RSBS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_RSBS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_RSBS_REG_IMM8(p, rd, rn, imm8) \
	ARM_RSBS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSB_REG_IMM8_COND(rd, rn, imm8, cond) \
	_RSB_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _RSB_REG_IMM8(rd, rn, imm8) \
	_RSB_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _RSBS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_RSBS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _RSBS_REG_IMM8(rd, rn, imm8) \
	_RSBS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn RSB Rm */
#define ARM_RSB_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_RSB, rd, rn, rm, cond)
#define ARM_RSB_REG_REG(p, rd, rn, rm) \
	ARM_RSB_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_RSBS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_RSB, rd, rn, rm, cond)
#define ARM_RSBS_REG_REG(p, rd, rn, rm) \
	ARM_RSBS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSB_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_RSB, rd, rn, rm, cond)
#define _RSB_REG_REG(rd, rn, rm) \
	_RSB_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _RSBS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_RSB, rd, rn, rm, cond)
#define _RSBS_REG_REG(rd, rn, rm) \
	_RSBS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn RSB (Rm <shift_type> imm_shift) */
#define ARM_RSB_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_RSB, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_RSB_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_RSB_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_RSBS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_RSB, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_RSBS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_RSBS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSB_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_RSB, rd, rn, rm, shift_type, imm_shift, cond)
#define _RSB_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_RSB_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _RSBS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_RSB, rd, rn, rm, shift_type, imm_shift, cond)
#define _RSBS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_RSBS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn RSB (Rm <shift_type> Rs) */
#define ARM_RSB_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_RSB, rd, rn, rm, shift_t, rs, cond)
#define ARM_RSB_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_RSB_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_RSBS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_RSB, rd, rn, rm, shift_t, rs, cond)
#define ARM_RSBS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_RSBS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSB_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_RSB, rd, rn, rm, shift_t, rs, cond)
#define _RSB_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_RSB_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _RSBS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_RSB, rd, rn, rm, shift_t, rs, cond)
#define _RSBS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_RSBS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- ADD -- */

/* Rd := Rn ADD (imm8 ROR rot) ; rot is power of 2 */
#define ARM_ADD_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_ADD, rd, rn, imm8, rot, cond)
#define ARM_ADD_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ADD_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_ADDS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_ADD, rd, rn, imm8, rot, cond)
#define ARM_ADDS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ADDS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADD_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_ADD, rd, rn, imm8, rot, cond)
#define _ADD_REG_IMM(rd, rn, imm8, rot) \
	_ADD_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _ADDS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_ADD, rd, rn, imm8, rot, cond)
#define _ADDS_REG_IMM(rd, rn, imm8, rot) \
	_ADDS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn ADD imm8 */
#define ARM_ADD_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ADD_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ADD_REG_IMM8(p, rd, rn, imm8) \
	ARM_ADD_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_ADDS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ADDS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ADDS_REG_IMM8(p, rd, rn, imm8) \
	ARM_ADDS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADD_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ADD_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ADD_REG_IMM8(rd, rn, imm8) \
	_ADD_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _ADDS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ADDS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ADDS_REG_IMM8(rd, rn, imm8) \
	_ADDS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn ADD Rm */
#define ARM_ADD_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_ADD, rd, rn, rm, cond)
#define ARM_ADD_REG_REG(p, rd, rn, rm) \
	ARM_ADD_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_ADDS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_ADD, rd, rn, rm, cond)
#define ARM_ADDS_REG_REG(p, rd, rn, rm) \
	ARM_ADDS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADD_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_ADD, rd, rn, rm, cond)
#define _ADD_REG_REG(rd, rn, rm) \
	_ADD_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _ADDS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_ADD, rd, rn, rm, cond)
#define _ADDS_REG_REG(rd, rn, rm) \
	_ADDS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn ADD (Rm <shift_type> imm_shift) */
#define ARM_ADD_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_ADD, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ADD_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ADD_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_ADDS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_ADD, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ADDS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ADDS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADD_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_ADD, rd, rn, rm, shift_type, imm_shift, cond)
#define _ADD_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ADD_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _ADDS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_ADD, rd, rn, rm, shift_type, imm_shift, cond)
#define _ADDS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ADDS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn ADD (Rm <shift_type> Rs) */
#define ARM_ADD_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_ADD, rd, rn, rm, shift_t, rs, cond)
#define ARM_ADD_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ADD_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_ADDS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_ADD, rd, rn, rm, shift_t, rs, cond)
#define ARM_ADDS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ADDS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADD_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_ADD, rd, rn, rm, shift_t, rs, cond)
#define _ADD_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ADD_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _ADDS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_ADD, rd, rn, rm, shift_t, rs, cond)
#define _ADDS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ADDS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- ADC -- */

/* Rd := Rn ADC (imm8 ROR rot) ; rot is power of 2 */
#define ARM_ADC_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_ADC, rd, rn, imm8, rot, cond)
#define ARM_ADC_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ADC_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_ADCS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_ADC, rd, rn, imm8, rot, cond)
#define ARM_ADCS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ADCS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADC_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_ADC, rd, rn, imm8, rot, cond)
#define _ADC_REG_IMM(rd, rn, imm8, rot) \
	_ADC_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _ADCS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_ADC, rd, rn, imm8, rot, cond)
#define _ADCS_REG_IMM(rd, rn, imm8, rot) \
	_ADCS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn ADC imm8 */
#define ARM_ADC_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ADC_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ADC_REG_IMM8(p, rd, rn, imm8) \
	ARM_ADC_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_ADCS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ADCS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ADCS_REG_IMM8(p, rd, rn, imm8) \
	ARM_ADCS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADC_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ADC_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ADC_REG_IMM8(rd, rn, imm8) \
	_ADC_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _ADCS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ADCS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ADCS_REG_IMM8(rd, rn, imm8) \
	_ADCS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn ADC Rm */
#define ARM_ADC_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_ADC, rd, rn, rm, cond)
#define ARM_ADC_REG_REG(p, rd, rn, rm) \
	ARM_ADC_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_ADCS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_ADC, rd, rn, rm, cond)
#define ARM_ADCS_REG_REG(p, rd, rn, rm) \
	ARM_ADCS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADC_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_ADC, rd, rn, rm, cond)
#define _ADC_REG_REG(rd, rn, rm) \
	_ADC_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _ADCS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_ADC, rd, rn, rm, cond)
#define _ADCS_REG_REG(rd, rn, rm) \
	_ADCS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn ADC (Rm <shift_type> imm_shift) */
#define ARM_ADC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_ADC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ADC_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ADC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_ADCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_ADC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ADCS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ADCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_ADC, rd, rn, rm, shift_type, imm_shift, cond)
#define _ADC_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ADC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _ADCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_ADC, rd, rn, rm, shift_type, imm_shift, cond)
#define _ADCS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ADCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn ADC (Rm <shift_type> Rs) */
#define ARM_ADC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_ADC, rd, rn, rm, shift_t, rs, cond)
#define ARM_ADC_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ADC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_ADCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_ADC, rd, rn, rm, shift_t, rs, cond)
#define ARM_ADCS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ADCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ADC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_ADC, rd, rn, rm, shift_t, rs, cond)
#define _ADC_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ADC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _ADCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_ADC, rd, rn, rm, shift_t, rs, cond)
#define _ADCS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ADCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- SBC -- */

/* Rd := Rn SBC (imm8 ROR rot) ; rot is power of 2 */
#define ARM_SBC_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_SBC, rd, rn, imm8, rot, cond)
#define ARM_SBC_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_SBC_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_SBCS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_SBC, rd, rn, imm8, rot, cond)
#define ARM_SBCS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_SBCS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SBC_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_SBC, rd, rn, imm8, rot, cond)
#define _SBC_REG_IMM(rd, rn, imm8, rot) \
	_SBC_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _SBCS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_SBC, rd, rn, imm8, rot, cond)
#define _SBCS_REG_IMM(rd, rn, imm8, rot) \
	_SBCS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn SBC imm8 */
#define ARM_SBC_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_SBC_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_SBC_REG_IMM8(p, rd, rn, imm8) \
	ARM_SBC_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_SBCS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_SBCS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_SBCS_REG_IMM8(p, rd, rn, imm8) \
	ARM_SBCS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SBC_REG_IMM8_COND(rd, rn, imm8, cond) \
	_SBC_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _SBC_REG_IMM8(rd, rn, imm8) \
	_SBC_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _SBCS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_SBCS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _SBCS_REG_IMM8(rd, rn, imm8) \
	_SBCS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn SBC Rm */
#define ARM_SBC_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_SBC, rd, rn, rm, cond)
#define ARM_SBC_REG_REG(p, rd, rn, rm) \
	ARM_SBC_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_SBCS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_SBC, rd, rn, rm, cond)
#define ARM_SBCS_REG_REG(p, rd, rn, rm) \
	ARM_SBCS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SBC_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_SBC, rd, rn, rm, cond)
#define _SBC_REG_REG(rd, rn, rm) \
	_SBC_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _SBCS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_SBC, rd, rn, rm, cond)
#define _SBCS_REG_REG(rd, rn, rm) \
	_SBCS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn SBC (Rm <shift_type> imm_shift) */
#define ARM_SBC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_SBC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_SBC_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_SBC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_SBCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_SBC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_SBCS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_SBCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SBC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_SBC, rd, rn, rm, shift_type, imm_shift, cond)
#define _SBC_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_SBC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _SBCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_SBC, rd, rn, rm, shift_type, imm_shift, cond)
#define _SBCS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_SBCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn SBC (Rm <shift_type> Rs) */
#define ARM_SBC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_SBC, rd, rn, rm, shift_t, rs, cond)
#define ARM_SBC_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_SBC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_SBCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_SBC, rd, rn, rm, shift_t, rs, cond)
#define ARM_SBCS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_SBCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _SBC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_SBC, rd, rn, rm, shift_t, rs, cond)
#define _SBC_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_SBC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _SBCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_SBC, rd, rn, rm, shift_t, rs, cond)
#define _SBCS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_SBCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- RSC -- */

/* Rd := Rn RSC (imm8 ROR rot) ; rot is power of 2 */
#define ARM_RSC_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_RSC, rd, rn, imm8, rot, cond)
#define ARM_RSC_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_RSC_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_RSCS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_RSC, rd, rn, imm8, rot, cond)
#define ARM_RSCS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_RSCS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSC_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_RSC, rd, rn, imm8, rot, cond)
#define _RSC_REG_IMM(rd, rn, imm8, rot) \
	_RSC_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _RSCS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_RSC, rd, rn, imm8, rot, cond)
#define _RSCS_REG_IMM(rd, rn, imm8, rot) \
	_RSCS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn RSC imm8 */
#define ARM_RSC_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_RSC_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_RSC_REG_IMM8(p, rd, rn, imm8) \
	ARM_RSC_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_RSCS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_RSCS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_RSCS_REG_IMM8(p, rd, rn, imm8) \
	ARM_RSCS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSC_REG_IMM8_COND(rd, rn, imm8, cond) \
	_RSC_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _RSC_REG_IMM8(rd, rn, imm8) \
	_RSC_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _RSCS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_RSCS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _RSCS_REG_IMM8(rd, rn, imm8) \
	_RSCS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn RSC Rm */
#define ARM_RSC_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_RSC, rd, rn, rm, cond)
#define ARM_RSC_REG_REG(p, rd, rn, rm) \
	ARM_RSC_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_RSCS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_RSC, rd, rn, rm, cond)
#define ARM_RSCS_REG_REG(p, rd, rn, rm) \
	ARM_RSCS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSC_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_RSC, rd, rn, rm, cond)
#define _RSC_REG_REG(rd, rn, rm) \
	_RSC_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _RSCS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_RSC, rd, rn, rm, cond)
#define _RSCS_REG_REG(rd, rn, rm) \
	_RSCS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn RSC (Rm <shift_type> imm_shift) */
#define ARM_RSC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_RSC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_RSC_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_RSC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_RSCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_RSC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_RSCS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_RSCS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_RSC, rd, rn, rm, shift_type, imm_shift, cond)
#define _RSC_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_RSC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _RSCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_RSC, rd, rn, rm, shift_type, imm_shift, cond)
#define _RSCS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_RSCS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn RSC (Rm <shift_type> Rs) */
#define ARM_RSC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_RSC, rd, rn, rm, shift_t, rs, cond)
#define ARM_RSC_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_RSC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_RSCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_RSC, rd, rn, rm, shift_t, rs, cond)
#define ARM_RSCS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_RSCS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _RSC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_RSC, rd, rn, rm, shift_t, rs, cond)
#define _RSC_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_RSC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _RSCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_RSC, rd, rn, rm, shift_t, rs, cond)
#define _RSCS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_RSCS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- ORR -- */

/* Rd := Rn ORR (imm8 ROR rot) ; rot is power of 2 */
#define ARM_ORR_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_ORR, rd, rn, imm8, rot, cond)
#define ARM_ORR_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ORR_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_ORRS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_ORR, rd, rn, imm8, rot, cond)
#define ARM_ORRS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_ORRS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ORR_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_ORR, rd, rn, imm8, rot, cond)
#define _ORR_REG_IMM(rd, rn, imm8, rot) \
	_ORR_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _ORRS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_ORR, rd, rn, imm8, rot, cond)
#define _ORRS_REG_IMM(rd, rn, imm8, rot) \
	_ORRS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn ORR imm8 */
#define ARM_ORR_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ORR_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ORR_REG_IMM8(p, rd, rn, imm8) \
	ARM_ORR_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_ORRS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_ORRS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_ORRS_REG_IMM8(p, rd, rn, imm8) \
	ARM_ORRS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ORR_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ORR_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ORR_REG_IMM8(rd, rn, imm8) \
	_ORR_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _ORRS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_ORRS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _ORRS_REG_IMM8(rd, rn, imm8) \
	_ORRS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn ORR Rm */
#define ARM_ORR_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_ORR, rd, rn, rm, cond)
#define ARM_ORR_REG_REG(p, rd, rn, rm) \
	ARM_ORR_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_ORRS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_ORR, rd, rn, rm, cond)
#define ARM_ORRS_REG_REG(p, rd, rn, rm) \
	ARM_ORRS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ORR_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_ORR, rd, rn, rm, cond)
#define _ORR_REG_REG(rd, rn, rm) \
	_ORR_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _ORRS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_ORR, rd, rn, rm, cond)
#define _ORRS_REG_REG(rd, rn, rm) \
	_ORRS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn ORR (Rm <shift_type> imm_shift) */
#define ARM_ORR_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_ORR, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ORR_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ORR_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_ORRS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_ORR, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_ORRS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_ORRS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ORR_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_ORR, rd, rn, rm, shift_type, imm_shift, cond)
#define _ORR_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ORR_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _ORRS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_ORR, rd, rn, rm, shift_type, imm_shift, cond)
#define _ORRS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_ORRS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn ORR (Rm <shift_type> Rs) */
#define ARM_ORR_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_ORR, rd, rn, rm, shift_t, rs, cond)
#define ARM_ORR_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ORR_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_ORRS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_ORR, rd, rn, rm, shift_t, rs, cond)
#define ARM_ORRS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_ORRS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _ORR_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_ORR, rd, rn, rm, shift_t, rs, cond)
#define _ORR_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ORR_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _ORRS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_ORR, rd, rn, rm, shift_t, rs, cond)
#define _ORRS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_ORRS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif


/* -- BIC -- */

/* Rd := Rn BIC (imm8 ROR rot) ; rot is power of 2 */
#define ARM_BIC_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_REG_IMM8ROT_COND(p, ARMOP_BIC, rd, rn, imm8, rot, cond)
#define ARM_BIC_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_BIC_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)
#define ARM_BICS_REG_IMM_COND(p, rd, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_BIC, rd, rn, imm8, rot, cond)
#define ARM_BICS_REG_IMM(p, rd, rn, imm8, rot) \
	ARM_BICS_REG_IMM_COND(p, rd, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _BIC_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_REG_IMM8ROT_COND(ARMOP_BIC, rd, rn, imm8, rot, cond)
#define _BIC_REG_IMM(rd, rn, imm8, rot) \
	_BIC_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#define _BICS_REG_IMM_COND(rd, rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_BIC, rd, rn, imm8, rot, cond)
#define _BICS_REG_IMM(rd, rn, imm8, rot) \
	_BICS_REG_IMM_COND(rd, rn, imm8, rot, ARMCOND_AL)
#endif


/* Rd := Rn BIC imm8 */
#define ARM_BIC_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_BIC_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_BIC_REG_IMM8(p, rd, rn, imm8) \
	ARM_BIC_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)
#define ARM_BICS_REG_IMM8_COND(p, rd, rn, imm8, cond) \
	ARM_BICS_REG_IMM_COND(p, rd, rn, imm8, 0, cond)
#define ARM_BICS_REG_IMM8(p, rd, rn, imm8) \
	ARM_BICS_REG_IMM8_COND(p, rd, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _BIC_REG_IMM8_COND(rd, rn, imm8, cond) \
	_BIC_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _BIC_REG_IMM8(rd, rn, imm8) \
	_BIC_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#define _BICS_REG_IMM8_COND(rd, rn, imm8, cond) \
	_BICS_REG_IMM_COND(rd, rn, imm8, 0, cond)
#define _BICS_REG_IMM8(rd, rn, imm8) \
	_BICS_REG_IMM8_COND(rd, rn, imm8, ARMCOND_AL)
#endif


/* Rd := Rn BIC Rm */
#define ARM_BIC_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_REG_REG_COND(p, ARMOP_BIC, rd, rn, rm, cond)
#define ARM_BIC_REG_REG(p, rd, rn, rm) \
	ARM_BIC_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)
#define ARM_BICS_REG_REG_COND(p, rd, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_BIC, rd, rn, rm, cond)
#define ARM_BICS_REG_REG(p, rd, rn, rm) \
	ARM_BICS_REG_REG_COND(p, rd, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _BIC_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_REG_REG_COND(ARMOP_BIC, rd, rn, rm, cond)
#define _BIC_REG_REG(rd, rn, rm) \
	_BIC_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#define _BICS_REG_REG_COND(rd, rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_BIC, rd, rn, rm, cond)
#define _BICS_REG_REG(rd, rn, rm) \
	_BICS_REG_REG_COND(rd, rn, rm, ARMCOND_AL)
#endif


/* Rd := Rn BIC (Rm <shift_type> imm_shift) */
#define ARM_BIC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_REG_IMMSHIFT_COND(p, ARMOP_BIC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_BIC_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_BIC_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define ARM_BICS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_BIC, rd, rn, rm, shift_type, imm_shift, cond)
#define ARM_BICS_REG_IMMSHIFT(p, rd, rn, rm, shift_type, imm_shift) \
	ARM_BICS_REG_IMMSHIFT_COND(p, rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _BIC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_REG_IMMSHIFT_COND(ARMOP_BIC, rd, rn, rm, shift_type, imm_shift, cond)
#define _BIC_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_BIC_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#define _BICS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_BIC, rd, rn, rm, shift_type, imm_shift, cond)
#define _BICS_REG_IMMSHIFT(rd, rn, rm, shift_type, imm_shift) \
	_BICS_REG_IMMSHIFT_COND(rd, rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* Rd := Rn BIC (Rm <shift_type> Rs) */
#define ARM_BIC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_REG_REGSHIFT_COND(p, ARMOP_BIC, rd, rn, rm, shift_t, rs, cond)
#define ARM_BIC_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_BIC_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define ARM_BICS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, cond) \
	ARM_DPIOP_S_REG_REGSHIFT_COND(p, ARMOP_BIC, rd, rn, rm, shift_t, rs, cond)
#define ARM_BICS_REG_REGSHIFT(p, rd, rn, rm, shift_type, rs) \
	ARM_BICS_REG_REGSHIFT_COND(p, rd, rn, rm, shift_type, rs, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _BIC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_REG_REGSHIFT_COND(ARMOP_BIC, rd, rn, rm, shift_t, rs, cond)
#define _BIC_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_BIC_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#define _BICS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, cond) \
	ARM_IASM_DPIOP_S_REG_REGSHIFT_COND(ARMOP_BIC, rd, rn, rm, shift_t, rs, cond)
#define _BICS_REG_REGSHIFT(rd, rn, rm, shift_type, rs) \
	_BICS_REG_REGSHIFT_COND(rd, rn, rm, shift_type, rs, ARMCOND_AL)
#endif






/* DPIs, comparison */

/* PSR := TST Rn, (imm8 ROR 2*rot) */
#define ARM_TST_REG_IMM_COND(p, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_TST, 0, rn, imm8, rot, cond)
#define ARM_TST_REG_IMM(p, rn, imm8, rot) \
	ARM_TST_REG_IMM_COND(p, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TST_REG_IMM_COND(rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_TST, 0, rn, imm8, rot, cond)
#define _TST_REG_IMM(rn, imm8, rot) \
	_TST_REG_IMM_COND(rn, imm8, rot, ARMCOND_AL)
#endif


/* PSR := TST Rn, imm8 */
#define ARM_TST_REG_IMM8_COND(p, rn, imm8, cond) \
	ARM_TST_REG_IMM_COND(p, rn, imm8, 0, cond)
#define ARM_TST_REG_IMM8(p, rn, imm8) \
	ARM_TST_REG_IMM8_COND(p, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TST_REG_IMM8_COND(rn, imm8, cond) \
	_TST_REG_IMM_COND(rn, imm8, 0, cond)
#define _TST_REG_IMM8(rn, imm8) \
	_TST_REG_IMM8_COND(rn, imm8, ARMCOND_AL)
#endif


/* PSR := TST Rn, Rm */
#define ARM_TST_REG_REG_COND(p, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_TST, 0, rn, rm, cond)
#define ARM_TST_REG_REG(p, rn, rm) \
	ARM_TST_REG_REG_COND(p, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TST_REG_REG_COND(rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_TST, 0, rn, rm, cond)
#define _TST_REG_REG(rn, rm) \
	_TST_REG_REG_COND(rn, rm, ARMCOND_AL)
#endif


/* PSR := TST Rn, (Rm <shift_type> imm8) */
#define ARM_TST_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_TST, 0, rn, rm, shift_type, imm_shift, cond)
#define ARM_TST_REG_IMMSHIFT(p, rn, rm, shift_type, imm_shift) \
	ARM_TST_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TST_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_TST, 0, rn, rm, shift_type, imm_shift, cond)
#define _TST_REG_IMMSHIFT(rn, rm, shift_type, imm_shift) \
	_TST_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* PSR := TEQ Rn, (imm8 ROR 2*rot) */
#define ARM_TEQ_REG_IMM_COND(p, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_TEQ, 0, rn, imm8, rot, cond)
#define ARM_TEQ_REG_IMM(p, rn, imm8, rot) \
	ARM_TEQ_REG_IMM_COND(p, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TEQ_REG_IMM_COND(rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_TEQ, 0, rn, imm8, rot, cond)
#define _TEQ_REG_IMM(rn, imm8, rot) \
	_TEQ_REG_IMM_COND(rn, imm8, rot, ARMCOND_AL)
#endif


/* PSR := TEQ Rn, imm8 */
#define ARM_TEQ_REG_IMM8_COND(p, rn, imm8, cond) \
	ARM_TEQ_REG_IMM_COND(p, rn, imm8, 0, cond)
#define ARM_TEQ_REG_IMM8(p, rn, imm8) \
	ARM_TEQ_REG_IMM8_COND(p, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TEQ_REG_IMM8_COND(rn, imm8, cond) \
	_TEQ_REG_IMM_COND(rn, imm8, 0, cond)
#define _TEQ_REG_IMM8(rn, imm8) \
	_TEQ_REG_IMM8_COND(rn, imm8, ARMCOND_AL)
#endif


/* PSR := TEQ Rn, Rm */
#define ARM_TEQ_REG_REG_COND(p, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_TEQ, 0, rn, rm, cond)
#define ARM_TEQ_REG_REG(p, rn, rm) \
	ARM_TEQ_REG_REG_COND(p, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TEQ_REG_REG_COND(rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_TEQ, 0, rn, rm, cond)
#define _TEQ_REG_REG(rn, rm) \
	_TEQ_REG_REG_COND(rn, rm, ARMCOND_AL)
#endif


/* PSR := TEQ Rn, (Rm <shift_type> imm8) */
#define ARM_TEQ_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_TEQ, 0, rn, rm, shift_type, imm_shift, cond)
#define ARM_TEQ_REG_IMMSHIFT(p, rn, rm, shift_type, imm_shift) \
	ARM_TEQ_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _TEQ_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_TEQ, 0, rn, rm, shift_type, imm_shift, cond)
#define _TEQ_REG_IMMSHIFT(rn, rm, shift_type, imm_shift) \
	_TEQ_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* PSR := CMP Rn, (imm8 ROR 2*rot) */
#define ARM_CMP_REG_IMM_COND(p, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_CMP, 0, rn, imm8, rot, cond)
#define ARM_CMP_REG_IMM(p, rn, imm8, rot) \
	ARM_CMP_REG_IMM_COND(p, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMP_REG_IMM_COND(rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_CMP, 0, rn, imm8, rot, cond)
#define _CMP_REG_IMM(rn, imm8, rot) \
	_CMP_REG_IMM_COND(rn, imm8, rot, ARMCOND_AL)
#endif


/* PSR := CMP Rn, imm8 */
#define ARM_CMP_REG_IMM8_COND(p, rn, imm8, cond) \
	ARM_CMP_REG_IMM_COND(p, rn, imm8, 0, cond)
#define ARM_CMP_REG_IMM8(p, rn, imm8) \
	ARM_CMP_REG_IMM8_COND(p, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMP_REG_IMM8_COND(rn, imm8, cond) \
	_CMP_REG_IMM_COND(rn, imm8, 0, cond)
#define _CMP_REG_IMM8(rn, imm8) \
	_CMP_REG_IMM8_COND(rn, imm8, ARMCOND_AL)
#endif


/* PSR := CMP Rn, Rm */
#define ARM_CMP_REG_REG_COND(p, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_CMP, 0, rn, rm, cond)
#define ARM_CMP_REG_REG(p, rn, rm) \
	ARM_CMP_REG_REG_COND(p, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMP_REG_REG_COND(rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_CMP, 0, rn, rm, cond)
#define _CMP_REG_REG(rn, rm) \
	_CMP_REG_REG_COND(rn, rm, ARMCOND_AL)
#endif


/* PSR := CMP Rn, (Rm <shift_type> imm8) */
#define ARM_CMP_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_CMP, 0, rn, rm, shift_type, imm_shift, cond)
#define ARM_CMP_REG_IMMSHIFT(p, rn, rm, shift_type, imm_shift) \
	ARM_CMP_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMP_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_CMP, 0, rn, rm, shift_type, imm_shift, cond)
#define _CMP_REG_IMMSHIFT(rn, rm, shift_type, imm_shift) \
	_CMP_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif


/* PSR := CMN Rn, (imm8 ROR 2*rot) */
#define ARM_CMN_REG_IMM_COND(p, rn, imm8, rot, cond) \
	ARM_DPIOP_S_REG_IMM8ROT_COND(p, ARMOP_CMN, 0, rn, imm8, rot, cond)
#define ARM_CMN_REG_IMM(p, rn, imm8, rot) \
	ARM_CMN_REG_IMM_COND(p, rn, imm8, rot, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMN_REG_IMM_COND(rn, imm8, rot, cond) \
	ARM_IASM_DPIOP_S_REG_IMM8ROT_COND(ARMOP_CMN, 0, rn, imm8, rot, cond)
#define _CMN_REG_IMM(rn, imm8, rot) \
	_CMN_REG_IMM_COND(rn, imm8, rot, ARMCOND_AL)
#endif


/* PSR := CMN Rn, imm8 */
#define ARM_CMN_REG_IMM8_COND(p, rn, imm8, cond) \
	ARM_CMN_REG_IMM_COND(p, rn, imm8, 0, cond)
#define ARM_CMN_REG_IMM8(p, rn, imm8) \
	ARM_CMN_REG_IMM8_COND(p, rn, imm8, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMN_REG_IMM8_COND(rn, imm8, cond) \
	_CMN_REG_IMM_COND(rn, imm8, 0, cond)
#define _CMN_REG_IMM8(rn, imm8) \
	_CMN_REG_IMM8_COND(rn, imm8, ARMCOND_AL)
#endif


/* PSR := CMN Rn, Rm */
#define ARM_CMN_REG_REG_COND(p, rn, rm, cond) \
	ARM_DPIOP_S_REG_REG_COND(p, ARMOP_CMN, 0, rn, rm, cond)
#define ARM_CMN_REG_REG(p, rn, rm) \
	ARM_CMN_REG_REG_COND(p, rn, rm, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMN_REG_REG_COND(rn, rm, cond) \
	ARM_IASM_DPIOP_S_REG_REG_COND(ARMOP_CMN, 0, rn, rm, cond)
#define _CMN_REG_REG(rn, rm) \
	_CMN_REG_REG_COND(rn, rm, ARMCOND_AL)
#endif


/* PSR := CMN Rn, (Rm <shift_type> imm8) */
#define ARM_CMN_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, cond) \
	ARM_DPIOP_S_REG_IMMSHIFT_COND(p, ARMOP_CMN, 0, rn, rm, shift_type, imm_shift, cond)
#define ARM_CMN_REG_IMMSHIFT(p, rn, rm, shift_type, imm_shift) \
	ARM_CMN_REG_IMMSHIFT_COND(p, rn, rm, shift_type, imm_shift, ARMCOND_AL)

#ifndef ARM_NOIASM
#define _CMN_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, cond) \
	ARM_IASM_DPIOP_S_REG_IMMSHIFT_COND(ARMOP_CMN, 0, rn, rm, shift_type, imm_shift, cond)
#define _CMN_REG_IMMSHIFT(rn, rm, shift_type, imm_shift) \
	_CMN_REG_IMMSHIFT_COND(rn, rm, shift_type, imm_shift, ARMCOND_AL)
#endif



/* end generated */

