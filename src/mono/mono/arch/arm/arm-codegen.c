/*
 * arm-codegen.c
 * Copyright (c) 2002 Sergey Chaban <serge@wildwestsoftware.com>
 */

#include "arm-codegen.h"


arminstr_t* arm_emit_std_prologue(arminstr_t* p, unsigned int local_size) {
	ARM_MOV_REG_REG(p, ARMREG_IP, ARMREG_SP);

	/* save args */
	ARM_PUSH(p,   (1 << ARMREG_A1)
	            | (1 << ARMREG_A2)
	            | (1 << ARMREG_A3)
	            | (1 << ARMREG_A4));

	ARM_PUSH(p, (1U << ARMREG_IP) | (1U << ARMREG_LR));

	if (local_size != 0) {
		if ((local_size & (~0xFF)) == 0) {
			ARM_SUB_REG_IMM8(p, ARMREG_SP, ARMREG_SP, local_size);
		} else {
			/* TODO: optimize */
			p = arm_mov_reg_imm32(p, ARMREG_IP, local_size);
			ARM_SUB_REG_REG(p, ARMREG_SP, ARMREG_SP, ARMREG_IP);
			ARM_ADD_REG_IMM8(p, ARMREG_IP, ARMREG_IP, sizeof(armword_t));
			ARM_LDR_REG_REG(p, ARMREG_IP, ARMREG_SP, ARMREG_IP);
		}
	}

	return p;
}

arminstr_t* arm_emit_std_epilogue(arminstr_t* p, unsigned int local_size, int pop_regs) {
	if (local_size != 0) {
		if ((local_size & (~0xFF)) == 0) {
			ARM_ADD_REG_IMM8(p, ARMREG_SP, ARMREG_SP, local_size);
		} else {
			/* TODO: optimize */
			p = arm_mov_reg_imm32(p, ARMREG_IP, local_size);
			ARM_ADD_REG_REG(p, ARMREG_SP, ARMREG_SP, ARMREG_IP);
		}
	}

	ARM_POP_NWB(p, (1 << ARMREG_SP) | (1 << ARMREG_PC) | (pop_regs & 0x3FF));

	return p;
}


/* do not push A1-A4 */
arminstr_t* arm_emit_lean_prologue(arminstr_t* p, unsigned int local_size, int push_regs) {
	ARM_MOV_REG_REG(p, ARMREG_IP, ARMREG_SP);
	/* push_regs upto R10 will be saved */
	ARM_PUSH(p, (1U << ARMREG_IP) | (1U << ARMREG_LR) | (push_regs & 0x3FF));

	if (local_size != 0) {
		if ((local_size & (~0xFF)) == 0) {
			ARM_SUB_REG_IMM8(p, ARMREG_SP, ARMREG_SP, local_size);
		} else {
			/* TODO: optimize */
			p = arm_mov_reg_imm32(p, ARMREG_IP, local_size);
			ARM_SUB_REG_REG(p, ARMREG_SP, ARMREG_SP, ARMREG_IP);
			/* restore IP from stack */
			ARM_ADD_REG_IMM8(p, ARMREG_IP, ARMREG_IP, sizeof(armword_t));
			ARM_LDR_REG_REG(p, ARMREG_IP, ARMREG_SP, ARMREG_IP);
		}
	}

	return p;
}

/* Bit scan forward. */
int arm_bsf(armword_t val) {
	int i;
	armword_t mask;

	if (val == 0) return 0;
	for (i=1, mask=1; (i <= 8 * sizeof(armword_t)) && ((val & mask) == 0); ++i, mask<<=1);

	return i;
}


int arm_is_power_of_2(armword_t val) {
	return ((val & (val-1)) == 0);
}


/*
 * returns:
 *   1 - unable to represent
 *   positive even number - MOV-representable
 *   negative even number - MVN-representable
 */
int calc_arm_mov_const_shift(armword_t val) {
	armword_t mask;
	int res = 1, shift;

	for (shift=0; shift < 32; shift+=2) {
		mask = ARM_SCALE(0xFF, shift);
		if ((val & (~mask)) == 0) {
			res = shift;
			break;
		}
		if (((~val) & (~mask)) == 0) {
			res = -shift - 2;
			break;
		}
	}

	return res;
}


int is_arm_const(armword_t val) {
	int res;
	res = arm_is_power_of_2(val);
	if (!res) {
		res = calc_arm_mov_const_shift(val);
		res = !(res < 0 || res == 1);
	}
	return res;
}


int arm_const_steps(armword_t val) {
	int shift, steps = 0;

	while (val != 0) {
		shift = (arm_bsf(val) - 1) & (~1);
		val &= ~(0xFF << shift);
		++steps;
	}
	return steps;
}


/*
 * ARM cannot load arbitrary 32-bit constants directly into registers;
 * widely used work-around for this is to store constants into a
 * PC-addressable pool and use LDR instruction with PC-relative address
 * to load constant into register. Easiest way to implement this is to
 * embed constant inside a function with unconditional branch around it.
 * The above method is not used at the moment.
 * This routine always emits sequence of instructions to generate
 * requested constant. In the worst case it takes 4 instructions to
 * synthesize a constant - 1 MOV and 3 subsequent ORRs.
 */
arminstr_t* arm_mov_reg_imm32_cond(arminstr_t* p, int reg, armword_t imm32, int cond) {
	int mov_op;
	int step_op;
	int snip;
	int shift = calc_arm_mov_const_shift(imm32);

	if ((shift & 0x80000001) != 1) {
		if (shift >= 0) {
			ARM_MOV_REG_IMM_COND(p, reg, imm32 >> ((32 - shift) & 31), shift, cond);
		} else {
			ARM_MVN_REG_IMM_COND(p, reg, (imm32 ^ (~0)) >> ((32 + 2 + shift) & 31), (-shift - 2), cond);
		}
	} else {
		mov_op = ARMOP_MOV;
		step_op = ARMOP_ORR;

		if (arm_const_steps(imm32) > arm_const_steps(~imm32)) {
			mov_op = ARMOP_MVN;
			step_op = ARMOP_SUB;
			imm32 = ~imm32;
		}

		shift = (arm_bsf(imm32) - 1) & (~1);
		snip = imm32 & (0xFF << shift);
		ARM_EMIT(p, ARM_DEF_DPI_IMM_COND((unsigned)snip >> shift, (32 - shift) >> 1, reg, 0, 0, mov_op, cond));

		while ((imm32 ^= snip) != 0) {
			shift = (arm_bsf(imm32) - 1) & (~1);
			snip = imm32 & (0xFF << shift);
			ARM_EMIT(p, ARM_DEF_DPI_IMM_COND((unsigned)snip >> shift, (32 - shift) >> 1, reg, reg, 0, step_op, cond));
		}
	}

	return p;
}


arminstr_t* arm_mov_reg_imm32(arminstr_t* p, int reg, armword_t imm32) {
	return arm_mov_reg_imm32_cond(p, reg, imm32, ARMCOND_AL);
}



