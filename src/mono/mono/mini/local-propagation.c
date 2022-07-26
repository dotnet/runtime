/**
 * \file
 * Local constant, copy and tree propagation.
 *
 * To make some sense of the tree mover, read mono/docs/tree-mover.txt
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2006 Novell, Inc.  http://www.novell.com
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include <string.h>
#include <stdio.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>
#include <mono/utils/unlocked.h>
#include "mini.h"
#include "ir-emit.h"

#ifndef MONO_ARCH_IS_OP_MEMBASE
#define MONO_ARCH_IS_OP_MEMBASE(opcode) FALSE
#endif

static MonoBitSet*
mono_bitset_mp_new_noinit (MonoMemPool *mp,  guint32 max_size)
{
	int size = mono_bitset_alloc_size (max_size, 0);
	gpointer mem;

	mem = mono_mempool_alloc (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

struct magic_unsigned {
	guint32 magic_number;
	gboolean addition;
	int shift;
};

struct magic_signed {
	gint32 magic_number;
	int shift;
};

/* http://www.hackersdelight.org/hdcodetxt/magicu.c.txt */
static struct magic_unsigned
compute_magic_unsigned (guint32 divisor) {
	guint32 nc, delta, q1, r1, q2, r2;
	struct magic_unsigned magu;
	gboolean gt = FALSE;
	int p;

	magu.addition = 0;
	nc = -1 - (-(gint32)divisor) % divisor;
	p = 31;
	q1 = 0x80000000 / nc;
	r1 = 0x80000000 - q1 * nc;
	q2 = 0x7FFFFFFF / divisor;
	r2 = 0x7FFFFFFF - q2 * divisor;
	do {
		p = p + 1;
		if (q1 >= 0x80000000)
			gt = TRUE;
		if (r1 >= nc - r1) {
			q1 = 2 * q1 + 1;
			r1 = 2 * r1 - nc;
		} else {
			q1 = 2 * q1;
			r1 = 2 * r1;
		}
		if (r2 + 1 >= divisor - r2) {
			if (q2 >= 0x7FFFFFFF)
				magu.addition = 1;
			q2 = 2 * q2 + 1;
			r2 = 2 * r2 + 1 - divisor;
		} else {
			if (q2 >= 0x80000000)
				magu.addition = 1;
			q2 = 2 * q2;
			r2 = 2 * r2 + 1;
		}
		delta = divisor - 1 - r2;
	} while (!gt && (q1 < delta || (q1 == delta && r1 == 0)));

	magu.magic_number = q2 + 1;
	magu.shift = p - 32;
	return magu;
}

/* http://www.hackersdelight.org/hdcodetxt/magic.c.txt */
static struct magic_signed
compute_magic_signed (gint32 divisor) {
	int p;
	guint32 ad, anc, delta, q1, r1, q2, r2, t;
	const guint32 two31 = 0x80000000;
	struct magic_signed mag;

	ad = abs (divisor);
	t = two31 + ((unsigned)divisor >> 31);
	anc = t - 1 - t % ad;
	p = 31;
	q1 = two31 / anc;
	r1 = two31 - q1 * anc;
	q2 = two31 / ad;
	r2 = two31 - q2 * ad;
	do {
		p++;
		q1 *= 2;
		r1 *= 2;
		if (r1 >= anc) {
			q1++;
			r1 -= anc;
		}

		q2 *= 2;
		r2 *= 2;

		if (r2 >= ad) {
			q2++;
			r2 -= ad;
		}

		delta = ad - r2;
	} while (q1 < delta || (q1 == delta && r1 == 0));

	mag.magic_number = q2 + 1;
	if (divisor < 0)
		mag.magic_number = -mag.magic_number;
	mag.shift = p - 32;
	return mag;
}

static gboolean
mono_strength_reduction_division (MonoCompile *cfg, MonoInst *ins)
{
	gboolean allocated_vregs = FALSE;
	/*
	 * We don't use it on 32bit systems because on those
	 * platforms we emulate long multiplication, driving the
	 * performance back down.
	 */
	switch (ins->opcode) {
		case OP_IDIV_UN_IMM: {
			guint32 tmp_regl;
#if SIZEOF_REGISTER == 8
			guint32 dividend_reg;
#else
			guint32 tmp_regi;
#endif
			struct magic_unsigned mag;
			int power2 = mono_is_power_of_two (GTMREG_TO_UINT32 (ins->inst_imm));

			/* The decomposition doesn't handle exception throwing */
			if (ins->inst_imm == 0)
				break;

			if (power2 >= 0) {
				ins->opcode = OP_ISHR_UN_IMM;
				ins->sreg2 = -1;
				ins->inst_imm = power2;
				break;
			}
			if (cfg->backend->disable_div_with_mul)
				break;
			allocated_vregs = TRUE;
			/*
			 * Replacement of unsigned division with multiplication,
			 * shifts and additions Hacker's Delight, chapter 10-10.
			 */
			mag = compute_magic_unsigned (GTMREG_TO_UINT32 (ins->inst_imm));
			tmp_regl = alloc_lreg (cfg);
#if SIZEOF_REGISTER == 8
			dividend_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_I8CONST (cfg, tmp_regl, mag.magic_number);
			MONO_EMIT_NEW_UNALU (cfg, OP_ZEXT_I4, dividend_reg, ins->sreg1);
			MONO_EMIT_NEW_BIALU (cfg, OP_LMUL, tmp_regl, dividend_reg, tmp_regl);
			if (mag.addition) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, tmp_regl, tmp_regl, 32);
				MONO_EMIT_NEW_BIALU (cfg, OP_LADD, tmp_regl, tmp_regl, dividend_reg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, ins->dreg, tmp_regl, mag.shift);
			} else {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, ins->dreg, tmp_regl, 32 + mag.shift);
			}
#else
			tmp_regi = alloc_ireg (cfg);
			MONO_EMIT_NEW_ICONST (cfg, tmp_regi, mag.magic_number);
			MONO_EMIT_NEW_BIALU (cfg, OP_BIGMUL_UN, tmp_regl, ins->sreg1, tmp_regi);
			/* Long shifts below will be decomposed during cprop */
			if (mag.addition) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, tmp_regl, tmp_regl, 32);
				MONO_EMIT_NEW_BIALU (cfg, OP_IADDCC, MONO_LVREG_LS (tmp_regl), MONO_LVREG_LS (tmp_regl), ins->sreg1);
				/* MONO_LVREG_MS (tmp_reg) is 0, save in it the carry */
				MONO_EMIT_NEW_BIALU (cfg, OP_IADC, MONO_LVREG_MS (tmp_regl), MONO_LVREG_MS (tmp_regl), MONO_LVREG_MS (tmp_regl));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, tmp_regl, tmp_regl, mag.shift);
			} else {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, tmp_regl, tmp_regl, 32 + mag.shift);
			}
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, MONO_LVREG_LS (tmp_regl));
#endif
			UnlockedIncrement (&mono_jit_stats.optimized_divisions);
			break;
		}
		case OP_IDIV_IMM: {
			guint32 tmp_regl;
#if SIZEOF_REGISTER == 8
			guint32 dividend_reg;
#else
			guint32 tmp_regi;
#endif
			struct magic_signed mag;
			int power2 = (ins->inst_imm > 0) ? mono_is_power_of_two (GTMREG_TO_UINT32 (ins->inst_imm)) : -1;
			/* The decomposition doesn't handle exception throwing */
			/* Optimization with MUL does not apply for -1, 0 and 1 divisors */
			if (ins->inst_imm == 0 || ins->inst_imm == -1) {
				break;
			} else if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				ins->inst_imm = 0;
				break;
			}
			allocated_vregs = TRUE;
			if (power2 == 1) {
				guint32 r1 = alloc_ireg (cfg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, r1, ins->sreg1, 31);
				MONO_EMIT_NEW_BIALU (cfg, OP_IADD, r1, r1, ins->sreg1);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, ins->dreg, r1, 1);
				break;
			} else if (power2 > 0 && power2 < 31) {
				guint32 r1 = alloc_ireg (cfg);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, r1, ins->sreg1, 31);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, r1, r1, (32 - power2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IADD, r1, r1, ins->sreg1);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, ins->dreg, r1, power2);
				break;
			}

			if (cfg->backend->disable_div_with_mul)
				break;
			/*
			 * Replacement of signed division with multiplication,
			 * shifts and additions Hacker's Delight, chapter 10-6.
			 */
			mag = compute_magic_signed (GTMREG_TO_UINT32 (ins->inst_imm));
			tmp_regl = alloc_lreg (cfg);
#if SIZEOF_REGISTER == 8
			dividend_reg = alloc_lreg (cfg);
			MONO_EMIT_NEW_I8CONST (cfg, tmp_regl, mag.magic_number);
			MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, dividend_reg, ins->sreg1);
			MONO_EMIT_NEW_BIALU (cfg, OP_LMUL, tmp_regl, dividend_reg, tmp_regl);
			if ((ins->inst_imm > 0 && mag.magic_number < 0) || (ins->inst_imm < 0 && mag.magic_number > 0)) {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_IMM, tmp_regl, tmp_regl, 32);
				if (ins->inst_imm > 0 && mag.magic_number < 0) {
					MONO_EMIT_NEW_BIALU (cfg, OP_LADD, tmp_regl, tmp_regl, dividend_reg);
				} else if (ins->inst_imm < 0 && mag.magic_number > 0) {
					MONO_EMIT_NEW_BIALU (cfg, OP_LSUB, tmp_regl, tmp_regl, dividend_reg);
				}
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_IMM, tmp_regl, tmp_regl, mag.shift);
			} else {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_IMM, tmp_regl, tmp_regl, 32 + mag.shift);
			}
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_LSHR_UN_IMM, ins->dreg, tmp_regl, SIZEOF_REGISTER * 8 - 1);
			MONO_EMIT_NEW_BIALU (cfg, OP_LADD, ins->dreg, ins->dreg, tmp_regl);
#else
			tmp_regi = alloc_ireg (cfg);
			MONO_EMIT_NEW_ICONST (cfg, tmp_regi, mag.magic_number);
			MONO_EMIT_NEW_BIALU (cfg, OP_BIGMUL, tmp_regl, ins->sreg1, tmp_regi);
			if ((ins->inst_imm > 0 && mag.magic_number < 0) || (ins->inst_imm < 0 && mag.magic_number > 0)) {
				if (ins->inst_imm > 0 && mag.magic_number < 0) {
					/* Opposite sign, cannot overflow */
					MONO_EMIT_NEW_BIALU (cfg, OP_IADD, tmp_regi, MONO_LVREG_MS (tmp_regl), ins->sreg1);
				} else if (ins->inst_imm < 0 && mag.magic_number > 0) {
					/* Same sign, cannot overflow */
					MONO_EMIT_NEW_BIALU (cfg, OP_ISUB, tmp_regi, MONO_LVREG_MS (tmp_regl), ins->sreg1);
				}
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, tmp_regi, tmp_regi, mag.shift);
			} else {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, tmp_regi, MONO_LVREG_MS (tmp_regl), mag.shift);
			}
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, ins->dreg, tmp_regi, SIZEOF_REGISTER * 8 - 1);
			MONO_EMIT_NEW_BIALU (cfg, OP_IADD, ins->dreg, ins->dreg, tmp_regi);
#endif
			UnlockedIncrement (&mono_jit_stats.optimized_divisions);
			break;
		}
	}
	return allocated_vregs;
}

/*
 * Replaces ins with optimized opcodes.
 *
 * We can emit to cbb the equivalent instructions which will be used as
 * replacement for ins, or simply change the fields of ins. Spec needs to
 * be updated if we silently change the opcode of ins.
 *
 * Returns TRUE if additional vregs were allocated.
 */
static gboolean
mono_strength_reduction_ins (MonoCompile *cfg, MonoInst *ins, const char **spec)
{
	gboolean allocated_vregs = FALSE;

	/* FIXME: Add long/float */
	switch (ins->opcode) {
	case OP_MOVE:
	case OP_XMOVE:
		if (ins->dreg == ins->sreg1) {
			NULLIFY_INS (ins);
		}
		break;
	case OP_ADD_IMM:
	case OP_IADD_IMM:
	case OP_SUB_IMM:
	case OP_ISUB_IMM:
#if SIZEOF_REGISTER == 8
	case OP_LADD_IMM:
	case OP_LSUB_IMM:
#endif
		if (ins->inst_imm == 0) {
			ins->opcode = OP_MOVE;
		}
		break;
	case OP_MUL_IMM:
	case OP_IMUL_IMM:
#if SIZEOF_REGISTER == 8
	case OP_LMUL_IMM:
#endif
		if (ins->inst_imm == 0) {
			ins->opcode = (ins->opcode == OP_LMUL_IMM) ? OP_I8CONST : OP_ICONST;
			ins->inst_c0 = 0;
			ins->sreg1 = -1;
		} else if (ins->inst_imm == 1) {
			ins->opcode = OP_MOVE;
		} else if ((ins->opcode == OP_IMUL_IMM) && (ins->inst_imm == -1)) {
			ins->opcode = OP_INEG;
		} else if ((ins->opcode == OP_LMUL_IMM) && (ins->inst_imm == -1)) {
			ins->opcode = OP_LNEG;
		} else if (ins->inst_imm > 0 && ins->inst_imm <= UINT32_MAX) {
			int power2 = mono_is_power_of_two (GTMREG_TO_UINT32 (ins->inst_imm));
			if (power2 >= 0) {
				ins->opcode = (ins->opcode == OP_MUL_IMM) ? OP_SHL_IMM : ((ins->opcode == OP_LMUL_IMM) ? OP_LSHL_IMM : OP_ISHL_IMM);
				ins->inst_imm = power2;
			}
		}
		break;
	case OP_IREM_UN_IMM: {
		int power2 = mono_is_power_of_two (GTMREG_TO_UINT32 (ins->inst_imm));

		if (power2 >= 0) {
			ins->opcode = OP_IAND_IMM;
			ins->sreg2 = -1;
			ins->inst_imm = (1 << power2) - 1;
		}
		break;
	}
	case OP_IDIV_UN_IMM:
	case OP_IDIV_IMM: {
		if ((!COMPILE_LLVM (cfg)) && (!cfg->backend->optimized_div))
			allocated_vregs = mono_strength_reduction_division (cfg, ins);
		break;
	}
#if SIZEOF_REGISTER == 8
	case OP_LREM_IMM:
#endif
	case OP_IREM_IMM: {
		int power = mono_is_power_of_two (GTMREG_TO_UINT32 (ins->inst_imm));
		if (ins->inst_imm == 1) {
			ins->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (ins);
			ins->inst_c0 = 0;
		} else if ((ins->inst_imm > 0) && (ins->inst_imm < (1LL << 32)) &&
			   (power != -1) && (!cfg->backend->optimized_div)) {
			gboolean is_long = ins->opcode == OP_LREM_IMM;
			int compensator_reg = alloc_ireg (cfg);
			int intermediate_reg;

			/* Based on gcc code */

			/* Add compensation for negative numerators */

			if (power > 1) {
				intermediate_reg = compensator_reg;
				MONO_EMIT_NEW_BIALU_IMM (cfg, is_long ? OP_LSHR_IMM : OP_ISHR_IMM, intermediate_reg, ins->sreg1, is_long ? 63 : 31);
			} else {
				intermediate_reg = ins->sreg1;
			}

			MONO_EMIT_NEW_BIALU_IMM (cfg, is_long ? OP_LSHR_UN_IMM : OP_ISHR_UN_IMM, compensator_reg, intermediate_reg, (is_long ? 64 : 32) - power);
			MONO_EMIT_NEW_BIALU (cfg, is_long ? OP_LADD : OP_IADD, ins->dreg, ins->sreg1, compensator_reg);
			/* Compute remainder */
			MONO_EMIT_NEW_BIALU_IMM (cfg, is_long ? OP_LAND_IMM : OP_AND_IMM, ins->dreg, ins->dreg, (1 << power) - 1);
			/* Remove compensation */
			MONO_EMIT_NEW_BIALU (cfg, is_long ? OP_LSUB : OP_ISUB, ins->dreg, ins->dreg, compensator_reg);

			allocated_vregs = TRUE;
		}
		break;
	}
#if SIZEOF_REGISTER == 4
	case OP_LSHR_IMM: {
		if (COMPILE_LLVM (cfg))
			break;
		if (ins->inst_c1 == 32) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), 31);
		} else if (ins->inst_c1 == 0) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1));
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
		} else if (ins->inst_c1 > 32) {
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_MS (ins->sreg1), ins->inst_c1 - 32);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), 31);
		} else {
			guint32 tmpreg = alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, tmpreg, MONO_LVREG_MS (ins->sreg1), 32 - ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU (cfg, OP_IOR, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->dreg), tmpreg);
			allocated_vregs = TRUE;
		}
		break;
	}
	case OP_LSHR_UN_IMM: {
		if (COMPILE_LLVM (cfg))
			break;
		if (ins->inst_c1 == 32) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
			MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (ins->dreg), 0);
		} else if (ins->inst_c1 == 0) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1));
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
		} else if (ins->inst_c1 > 32) {
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_MS (ins->sreg1), ins->inst_c1 - 32);
			MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (ins->dreg), 0);
		} else {
			guint32 tmpreg = alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, tmpreg, MONO_LVREG_MS (ins->sreg1), 32 - ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU (cfg, OP_IOR, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->dreg), tmpreg);
			allocated_vregs = TRUE;
		}
		break;
	}
	case OP_LSHL_IMM: {
		if (COMPILE_LLVM (cfg))
			break;
		if (ins->inst_c1 == 32) {
			/* just move the lower half to the upper and zero the lower word */
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (ins->dreg), MONO_LVREG_LS (ins->sreg1));
			MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_LS (ins->dreg), 0);
		} else if (ins->inst_c1 == 0) {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1));
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1));
		} else if (ins->inst_c1 > 32) {
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_LS (ins->sreg1), ins->inst_c1 - 32);
			MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_LS (ins->dreg), 0);
		} else {
			guint32 tmpreg = alloc_ireg (cfg);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, tmpreg, MONO_LVREG_LS (ins->sreg1), 32 - ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHL_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), ins->inst_c1);
			MONO_EMIT_NEW_BIALU (cfg, OP_IOR, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->dreg), tmpreg);
			allocated_vregs = TRUE;
		}
		break;
	}
#endif

	default:
		break;
	}

	*spec = INS_INFO (ins->opcode);
	return allocated_vregs;
}

/*
 * mono_local_cprop:
 *
 *  A combined local copy and constant propagation pass.
 */
void
mono_local_cprop (MonoCompile *cfg)
{
	MonoBasicBlock *bb, *bb_opt;
	MonoInst **defs;
	gint32 *def_index;
	guint32 max;
	int filter = FILTER_IL_SEQ_POINT;
	int initial_max_vregs = cfg->next_vreg;

	max = cfg->next_vreg;
	defs = (MonoInst **)mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * cfg->next_vreg);
	def_index = (gint32 *)mono_mempool_alloc (cfg->mempool, sizeof (guint32) * cfg->next_vreg);
	cfg->cbb = bb_opt = (MonoBasicBlock *)mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		int ins_index;
		int last_call_index;

		/* Manually init the defs entries used by the bblock */
		MONO_BB_FOR_EACH_INS (bb, ins) {
			int sregs [MONO_MAX_SRC_REGS];
			int num_sregs, i;

			if (ins->dreg != -1) {
#if SIZEOF_REGISTER == 4
				const char *spec = INS_INFO (ins->opcode);
				if (spec [MONO_INST_DEST] == 'l') {
					defs [ins->dreg + 1] = NULL;
					defs [ins->dreg + 2] = NULL;
				}
#endif
				defs [ins->dreg] = NULL;
			}

			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i) {
				int sreg = sregs [i];
#if SIZEOF_REGISTER == 4
				const char *spec = INS_INFO (ins->opcode);
				if (spec [MONO_INST_SRC1 + i] == 'l') {
					defs [sreg + 1] = NULL;
					defs [sreg + 2] = NULL;
				}
#endif
				defs [sreg] = NULL;
			}
		}

		ins_index = 0;
		last_call_index = -1;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);
			int regtype, srcindex, sreg;
			int num_sregs;
			int sregs [MONO_MAX_SRC_REGS];

			if (ins->opcode == OP_NOP) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}

			g_assert (ins->opcode > MONO_CEE_LAST);

			/* FIXME: Optimize this */
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var = (MonoInst *)ins->inst_p0;

				defs [var->dreg] = NULL;
				/*
				if (!MONO_TYPE_ISSTRUCT (var->inst_vtype))
					break;
				*/
			}

			if (MONO_IS_STORE_MEMBASE (ins)) {
				sreg = ins->dreg;
				regtype = 'i';

				if ((regtype == 'i') && (sreg != -1) && defs [sreg]) {
					MonoInst *def = defs [sreg];

					if ((def->opcode == OP_MOVE) && (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg])) && !vreg_is_volatile (cfg, def->sreg1)) {
						int vreg = def->sreg1;
						if (cfg->verbose_level > 2) printf ("CCOPY: R%d -> R%d\n", sreg, vreg);
						ins->dreg = vreg;
					}
				}
			}

			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (srcindex = 0; srcindex < num_sregs; ++srcindex) {
				MonoInst *def;

				mono_inst_get_src_registers (ins, sregs);

				regtype = spec [MONO_INST_SRC1 + srcindex];
				sreg = sregs [srcindex];

				if ((regtype == ' ') || (sreg == -1) || (!defs [sreg]))
					continue;

				def = defs [sreg];

				/* Copy propagation */
				/*
				 * The first check makes sure the source of the copy did not change since
				 * the copy was made.
				 * The second check avoids volatile variables.
				 * The third check avoids copy propagating local vregs through a call,
				 * since the lvreg will be spilled
				 * The fourth check avoids copy propagating a vreg in cases where
				 * it would be eliminated anyway by reverse copy propagation later,
				 * because propagating it would create another use for it, thus making
				 * it impossible to use reverse copy propagation.
				 */
				/* Enabling this for floats trips up the fp stack */
				/*
				 * Enabling this for floats on amd64 seems to cause a failure in
				 * basic-math.cs, most likely because it gets rid of some r8->r4
				 * conversions.
				 */
				if (MONO_IS_MOVE (def) &&
					(!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg])) &&
					!vreg_is_volatile (cfg, def->sreg1) &&
					/* This avoids propagating local vregs across calls */
					((get_vreg_to_inst (cfg, def->sreg1) || !defs [def->sreg1] || (def_index [def->sreg1] >= last_call_index) || (def->opcode == OP_VMOVE))) &&
					!(defs [def->sreg1] && mono_inst_next (defs [def->sreg1], filter) == def) &&
					(!MONO_ARCH_USE_FPSTACK || (def->opcode != OP_FMOVE)) &&
					(def->opcode != OP_FMOVE)) {
					int vreg = def->sreg1;

					if (cfg->verbose_level > 2) printf ("CCOPY/2: R%d -> R%d\n", sreg, vreg);
					sregs [srcindex] = vreg;
					mono_inst_set_src_registers (ins, sregs);

					/* Allow further iterations */
					srcindex = -1;
					continue;
				}

				/* Constant propagation */
				/* is_inst_imm is only needed for binops */
				if ((((def->opcode == OP_ICONST) || ((sizeof (gpointer) == 8) && (def->opcode == OP_I8CONST)) || (def->opcode == OP_PCONST)))
					||
					(!MONO_ARCH_USE_FPSTACK && (def->opcode == OP_R8CONST))) {
					guint32 opcode2;

					/* srcindex == 1 -> binop, ins->sreg2 == -1 -> unop */
					if ((srcindex == 1) && (ins->sreg1 != -1) && defs [ins->sreg1] &&
						((defs [ins->sreg1]->opcode == OP_ICONST) || defs [ins->sreg1]->opcode == OP_PCONST) &&
						defs [ins->sreg2]) {
						/* Both arguments are constants, perform cfold */
						mono_constant_fold_ins (cfg, ins, defs [ins->sreg1], defs [ins->sreg2], TRUE);
					} else if ((srcindex == 0) && (ins->sreg2 != -1) && defs [ins->sreg2]) {
						/* Arg 1 is constant, swap arguments if possible */
						int opcode = ins->opcode;
						mono_constant_fold_ins (cfg, ins, defs [ins->sreg1], defs [ins->sreg2], TRUE);
						if (ins->opcode != opcode) {
							/* Allow further iterations */
							srcindex = -1;
							continue;
						}
					} else if ((srcindex == 0) && (ins->sreg2 == -1)) {
						/* Constant unop, perform cfold */
						mono_constant_fold_ins (cfg, ins, defs [ins->sreg1], NULL, TRUE);
					}

					opcode2 = mono_op_to_op_imm (ins->opcode);
					if ((opcode2 != -1) && mono_arch_is_inst_imm (ins->opcode, opcode2, def->inst_c0) && ((srcindex == 1) || (ins->sreg2 == -1))) {
						ins->opcode = GUINT32_TO_OPCODE (opcode2);
						if ((def->opcode == OP_I8CONST) && TARGET_SIZEOF_VOID_P == 4)
							ins->inst_l = def->inst_l;
						else if (regtype == 'l' && TARGET_SIZEOF_VOID_P == 4)
							/* This can happen if the def was a result of an iconst+conv.i8, which is transformed into just an iconst */
							ins->inst_l = def->inst_c0;
						else
							ins->inst_imm = def->inst_c0;
						sregs [srcindex] = -1;
						mono_inst_set_src_registers (ins, sregs);

						if ((opcode2 == OP_VOIDCALL) || (opcode2 == OP_CALL) || (opcode2 == OP_LCALL) || (opcode2 == OP_FCALL))
							((MonoCallInst*)ins)->fptr = (gpointer)(uintptr_t)ins->inst_imm;

						/* Allow further iterations */
						srcindex = -1;
						continue;
					}
					else {
						/* Special cases */
#if defined(TARGET_X86) || defined(TARGET_AMD64)
						if ((ins->opcode == OP_X86_LEA) && (srcindex == 1)) {
#if SIZEOF_REGISTER == 8
							/* FIXME: Use OP_PADD_IMM when the new JIT is done */
							ins->opcode = OP_LADD_IMM;
#else
							ins->opcode = OP_ADD_IMM;
#endif
							ins->inst_imm += def->inst_c0 << ins->backend.shift_amount;
							ins->sreg2 = -1;
						}
#endif
						opcode2 = mono_load_membase_to_load_mem (ins->opcode);
						if ((srcindex == 0) && (opcode2 != -1) && mono_arch_is_inst_imm (ins->opcode, opcode2, def->inst_c0)) {
							ins->opcode = GUINT32_TO_OPCODE (opcode2);
							ins->inst_imm = def->inst_c0 + ins->inst_offset;
							ins->sreg1 = -1;
						}
					}
				}
				else if (((def->opcode == OP_ADD_IMM) || (def->opcode == OP_LADD_IMM)) && (MONO_IS_LOAD_MEMBASE (ins) || MONO_ARCH_IS_OP_MEMBASE (ins->opcode))) {
					/* ADD_IMM is created by spill_global_vars */
					/*
					 * We have to guarantee that def->sreg1 haven't changed since def->dreg
					 * was defined. cfg->frame_reg is assumed to remain constant.
					 */
					if ((def->sreg1 == cfg->frame_reg) || ((mono_inst_next (def, filter) == ins) && (def->dreg != def->sreg1))) {
						ins->inst_basereg = def->sreg1;
						ins->inst_offset += def->inst_imm;
					}
				} else if ((ins->opcode == OP_ISUB_IMM) && (def->opcode == OP_IADD_IMM) && (mono_inst_next (def, filter) == ins) && (def->dreg != def->sreg1)) {
					ins->sreg1 = def->sreg1;
					ins->inst_imm -= def->inst_imm;
				} else if ((ins->opcode == OP_IADD_IMM) && (def->opcode == OP_ISUB_IMM) && (mono_inst_next (def, filter) == ins) && (def->dreg != def->sreg1)) {
					ins->sreg1 = def->sreg1;
					ins->inst_imm -= def->inst_imm;
				} else if (ins->opcode == OP_STOREI1_MEMBASE_REG &&
						   (def->opcode == OP_ICONV_TO_U1 || def->opcode == OP_ICONV_TO_I1 || def->opcode == OP_SEXT_I4 || (SIZEOF_REGISTER == 8 && def->opcode == OP_LCONV_TO_U1)) &&
						   (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg]))) {
					/* Avoid needless sign extension */
					ins->sreg1 = def->sreg1;
				} else if (ins->opcode == OP_STOREI2_MEMBASE_REG &&
						   (def->opcode == OP_ICONV_TO_U2 || def->opcode == OP_ICONV_TO_I2 || def->opcode == OP_SEXT_I4 || (SIZEOF_REGISTER == 8 && def->opcode == OP_LCONV_TO_I2)) &&
						   (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg]))) {
					/* Avoid needless sign extension */
					ins->sreg1 = def->sreg1;
				} else if (ins->opcode == OP_COMPARE_IMM && def->opcode == OP_LDADDR && ins->inst_imm == 0) {
					MonoInst dummy_arg1;

					memset (&dummy_arg1, 0, sizeof (MonoInst));
					dummy_arg1.opcode = OP_ICONST;
					dummy_arg1.inst_c0 = 1;

					mono_constant_fold_ins (cfg, ins, &dummy_arg1, NULL, TRUE);
				} else if (srcindex == 0 && ins->opcode == OP_COMPARE && defs [ins->sreg1]->opcode == OP_PCONST && defs [ins->sreg2] && defs [ins->sreg2]->opcode == OP_PCONST) {
					/* typeof(T) == typeof(..) */
					mono_constant_fold_ins (cfg, ins, defs [ins->sreg1], defs [ins->sreg2], TRUE);
				} else if (ins->opcode == OP_MOVE && def->opcode == OP_LDADDR) {
					ins->opcode = OP_LDADDR;
					ins->sreg1 = -1;
					ins->inst_p0 = def->inst_p0;
					ins->klass = def->klass;
				}
			}

			g_assert (cfg->cbb == bb_opt);
			g_assert (!bb_opt->code);
			/* Do strength reduction here */
			if (mono_strength_reduction_ins (cfg, ins, &spec) && max < cfg->next_vreg) {
				MonoInst **defs_prev = defs;
				gint32 *def_index_prev = def_index;
				guint32 prev_max = max;
				guint32 additional_vregs = cfg->next_vreg - initial_max_vregs;

				/* We have more vregs so we need to reallocate defs and def_index arrays */
				max  = initial_max_vregs + additional_vregs * 2;
				defs = (MonoInst **)mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * max);
				def_index = (gint32 *)mono_mempool_alloc (cfg->mempool, sizeof (guint32) * max);

				/* Keep the entries for the previous vregs, zero the rest */
				memcpy (defs, defs_prev, sizeof (MonoInst*) * prev_max);
				memset (defs + prev_max, 0, sizeof (MonoInst*) * (max - prev_max));
				memcpy (def_index, def_index_prev, sizeof (guint32) * prev_max);
				memset (def_index + prev_max, 0, sizeof (guint32) * (max - prev_max));
			}

			if (cfg->cbb->code || (cfg->cbb != bb_opt)) {
				MonoInst *saved_prev = ins->prev;

				/* If we have code in cbb, we need to replace ins with the decomposition */
				mono_replace_ins (cfg, bb, ins, &ins->prev, bb_opt, cfg->cbb);
				bb_opt->code = bb_opt->last_ins = NULL;
				bb_opt->in_count = bb_opt->out_count = 0;
				cfg->cbb = bb_opt;

				if (!saved_prev) {
					/* first instruction of basic block got replaced, so create
					 * dummy inst that points to start of basic block */
					MONO_INST_NEW (cfg, saved_prev, OP_NOP);
					saved_prev = bb->code;
				}
				/* ins is hanging, continue scanning the emitted code */
				ins = saved_prev;
				continue;
			}

			if (spec [MONO_INST_DEST] != ' ') {
				MonoInst *def = defs [ins->dreg];

				if (def && (def->opcode == OP_ADD_IMM) && (def->sreg1 == cfg->frame_reg) && (MONO_IS_STORE_MEMBASE (ins))) {
					/* ADD_IMM is created by spill_global_vars */
					/* cfg->frame_reg is assumed to remain constant */
					ins->inst_destbasereg = def->sreg1;
					ins->inst_offset += def->inst_imm;
				}

				if (!MONO_IS_STORE_MEMBASE (ins) && !vreg_is_volatile (cfg, ins->dreg)) {
					defs [ins->dreg] = ins;
					def_index [ins->dreg] = ins_index;
				}
			}

			if (MONO_IS_CALL (ins))
				last_call_index = ins_index;

			ins_index ++;
		}
	}
}

static gboolean
reg_is_softreg_no_fpstack (int reg, const char spec)
{
	return (spec == 'i' && reg >= MONO_MAX_IREGS)
		|| ((spec == 'f' && reg >= MONO_MAX_FREGS) && !MONO_ARCH_USE_FPSTACK)
#ifdef MONO_ARCH_SIMD_INTRINSICS
		|| (spec == 'x' && reg >= MONO_MAX_XREGS)
#endif
		|| (spec == 'v');
}

static gboolean
reg_is_softreg (int reg, const char spec)
{
	return (spec == 'i' && reg >= MONO_MAX_IREGS)
		|| (spec == 'f' && reg >= MONO_MAX_FREGS)
#ifdef MONO_ARCH_SIMD_INTRINSICS
		|| (spec == 'x' && reg >= MONO_MAX_XREGS)
#endif
		|| (spec == 'v');
}

static gboolean
mono_is_simd_accessor (MonoInst *ins)
{
#ifdef MONO_ARCH_SIMD_INTRINSICS
	switch (ins->opcode) {
	case OP_INSERT_I1:
	case OP_INSERT_I2:
	case OP_INSERT_I4:
	case OP_INSERT_I8:
	case OP_INSERT_R4:
	case OP_INSERT_R8:

	case OP_INSERTX_U1_SLOW:
	case OP_INSERTX_I4_SLOW:
	case OP_INSERTX_R4_SLOW:
	case OP_INSERTX_R8_SLOW:
	case OP_INSERTX_I8_SLOW:
		return TRUE;
	default:
		return FALSE;
	}
#else
	return FALSE;
#endif
}

/**
 * mono_local_deadce:
 *
 *   Get rid of the dead assignments to local vregs like the ones created by the
 * copyprop pass.
 */
void
mono_local_deadce (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst *ins, *prev;
	MonoBitSet *used, *defined;

	//mono_print_code (cfg, "BEFORE LOCAL-DEADCE");

	/*
	 * Assignments to global vregs can't be eliminated so this pass must come
	 * after the handle_global_vregs () pass.
	 */

	used = mono_bitset_mp_new_noinit (cfg->mempool, cfg->next_vreg + 1);
	defined = mono_bitset_mp_new_noinit (cfg->mempool, cfg->next_vreg + 1);

	/* First pass: collect liveness info */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		/* Manually init the defs entries used by the bblock */
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);
			int sregs [MONO_MAX_SRC_REGS];
			int num_sregs, i;

			if (spec [MONO_INST_DEST] != ' ') {
				mono_bitset_clear_fast (used, ins->dreg);
				mono_bitset_clear_fast (defined, ins->dreg);
#if SIZEOF_REGISTER == 4
				/* Regpairs */
				mono_bitset_clear_fast (used, ins->dreg + 1);
				mono_bitset_clear_fast (defined, ins->dreg + 1);
#endif
			}
			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i) {
				mono_bitset_clear_fast (used, sregs [i]);
#if SIZEOF_REGISTER == 4
				mono_bitset_clear_fast (used, sregs [i] + 1);
#endif
			}
		}

		/*
		 * Make a reverse pass over the instruction list
		 */
		MONO_BB_FOR_EACH_INS_REVERSE_SAFE (bb, prev, ins) {
			const char *spec = INS_INFO (ins->opcode);
			int sregs [MONO_MAX_SRC_REGS];
			int num_sregs, i;
			MonoInst *prev_f = mono_inst_prev (ins, FILTER_NOP | FILTER_IL_SEQ_POINT);

			if (ins->opcode == OP_NOP) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}

			g_assert (ins->opcode > MONO_CEE_LAST);

			if (MONO_IS_NON_FP_MOVE (ins) && prev_f) {
				MonoInst *def;
				const char *spec2;

				def = prev_f;
				spec2 = INS_INFO (def->opcode);

				/*
				 * Perform a limited kind of reverse copy propagation, i.e.
				 * transform B <- FOO; A <- B into A <- FOO
				 * This isn't copyprop, not deadce, but it can only be performed
				 * after handle_global_vregs () has run.
				 */
				if (!get_vreg_to_inst (cfg, ins->sreg1) && (spec2 [MONO_INST_DEST] != ' ') && (def->dreg == ins->sreg1) && !mono_bitset_test_fast (used, ins->sreg1) && !MONO_IS_STORE_MEMBASE (def) && reg_is_softreg (ins->sreg1, spec [MONO_INST_DEST]) && !mono_is_simd_accessor (def)) {
					if (cfg->verbose_level > 2) {
						printf ("\tReverse copyprop in BB%d on ", bb->block_num);
						mono_print_ins (ins);
					}

					def->dreg = ins->dreg;
					MONO_DELETE_INS (bb, ins);
					spec = INS_INFO (ins->opcode);
				}
			}

			/* Enabling this on x86 could screw up the fp stack */
			if (reg_is_softreg_no_fpstack (ins->dreg, spec [MONO_INST_DEST])) {
				/*
				 * Assignments to global vregs can only be eliminated if there is another
				 * assignment to the same vreg later in the same bblock.
				 */
				if (!mono_bitset_test_fast (used, ins->dreg) &&
					(!get_vreg_to_inst (cfg, ins->dreg) || (!bb->extended && !vreg_is_volatile (cfg, ins->dreg) && mono_bitset_test_fast (defined, ins->dreg))) &&
					MONO_INS_HAS_NO_SIDE_EFFECT (ins)) {
					/* Happens with CMOV instructions */
					if (prev_f && prev_f->opcode == OP_ICOMPARE_IMM) {
						MonoInst *prev_ins = prev_f;
						/*
						 * Can't use DELETE_INS since that would interfere with the
						 * FOR_EACH_INS loop.
						 */
						NULLIFY_INS (prev_ins);
					}
					//printf ("DEADCE: "); mono_print_ins (ins);
					MONO_DELETE_INS (bb, ins);
					spec = INS_INFO (ins->opcode);
				}

				if (spec [MONO_INST_DEST] != ' ')
					mono_bitset_clear_fast (used, ins->dreg);
			}

			if (spec [MONO_INST_DEST] != ' ')
				mono_bitset_set_fast (defined, ins->dreg);
			num_sregs = mono_inst_get_src_registers (ins, sregs);
			for (i = 0; i < num_sregs; ++i)
				mono_bitset_set_fast (used, sregs [i]);
			if (MONO_IS_STORE_MEMBASE (ins))
				mono_bitset_set_fast (used, ins->dreg);

			if (MONO_IS_CALL (ins)) {
				MonoCallInst *call = (MonoCallInst*)ins;
				GSList *l;

				if (call->out_ireg_args) {
					for (l = call->out_ireg_args; l; l = l->next) {
						guint32 regpair, reg;

						regpair = (guint32)(gssize)(l->data);
						reg = regpair & 0xffffff;

						mono_bitset_set_fast (used, reg);
					}
				}

				if (call->out_freg_args) {
					for (l = call->out_freg_args; l; l = l->next) {
						guint32 regpair, reg;

						regpair = (guint32)(gssize)(l->data);
						reg = regpair & 0xffffff;

						mono_bitset_set_fast (used, reg);
					}
				}
			}
		}
	}

	//mono_print_code (cfg, "AFTER LOCAL-DEADCE");
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (local_propagation);

#endif /* !DISABLE_JIT */
