/**
 * \file
 * Functions to decompose complex IR instructions into simpler ones.
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "jit-icalls.h"

#include <mono/metadata/gc-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/mono-compiler.h>
#define MONO_MATH_DECLARE_ALL 1
#include <mono/utils/mono-math.h>

#ifndef DISABLE_JIT

/*
 * Decompose complex long opcodes on 64 bit machines.
 * This is also used on 32 bit machines when using LLVM, so it needs to handle I/U correctly.
 */
static gboolean
decompose_long_opcode (MonoCompile *cfg, MonoInst *ins, MonoInst **repl_ins)
{
	MonoInst *repl = NULL;

	*repl_ins = NULL;

	switch (ins->opcode) {
	case OP_LCONV_TO_I4:
		ins->opcode = OP_SEXT_I4;
		break;
	case OP_LCONV_TO_I8:
	case OP_LCONV_TO_U8:
		if (TARGET_SIZEOF_VOID_P == 4)
			ins->opcode = OP_LMOVE;
		else
			ins->opcode = OP_MOVE;
		break;
	case OP_LCONV_TO_I:
		if (TARGET_SIZEOF_VOID_P == 4)
			/* OP_LCONV_TO_I4 */
			ins->opcode = OP_SEXT_I4;
		else
			ins->opcode = OP_MOVE;
		break;
	case OP_LCONV_TO_U:
		if (TARGET_SIZEOF_VOID_P == 4) {
			/* OP_LCONV_TO_U4 */
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, ins->dreg, ins->sreg1, 0);
			NULLIFY_INS (ins);
		} else {
			ins->opcode = OP_MOVE;
		}
		break;
	case OP_ICONV_TO_I8:
		ins->opcode = OP_SEXT_I4;
		break;
	case OP_ICONV_TO_U8:
		ins->opcode = OP_ZEXT_I4;
		break;
	case OP_LCONV_TO_U4:
		/* Clean out the upper word */
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISHR_UN_IMM, ins->dreg, ins->sreg1, 0);
		NULLIFY_INS (ins);
		break;
	case OP_LADD_OVF: {
		int opcode;

		if (COMPILE_LLVM (cfg))
			break;
		if (cfg->backend->ilp32 && SIZEOF_REGISTER == 8)
			opcode = OP_LADDCC;
		else
			opcode = OP_ADDCC;
		EMIT_NEW_BIALU (cfg, repl, opcode, ins->dreg, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, OV, "OverflowException");
		NULLIFY_INS (ins);
		break;
	}
	case OP_LADD_OVF_UN: {
		int opcode;

		if (COMPILE_LLVM (cfg))
			break;
		if (cfg->backend->ilp32 && SIZEOF_REGISTER == 8)
			opcode = OP_LADDCC;
		else
			opcode = OP_ADDCC;
		EMIT_NEW_BIALU (cfg, repl, opcode, ins->dreg, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, C, "OverflowException");
		NULLIFY_INS (ins);
		break;
	}
#ifndef __mono_ppc64__
	case OP_LSUB_OVF: {
		int opcode;

		if (COMPILE_LLVM (cfg))
			break;
		if (cfg->backend->ilp32 && SIZEOF_REGISTER == 8)
			opcode = OP_LSUBCC;
		else
			opcode = OP_SUBCC;
		EMIT_NEW_BIALU (cfg, repl, opcode, ins->dreg, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, OV, "OverflowException");
		NULLIFY_INS (ins);
		break;
	}
	case OP_LSUB_OVF_UN: {
		int opcode;

		if (COMPILE_LLVM (cfg))
			break;
		if (cfg->backend->ilp32 && SIZEOF_REGISTER == 8)
			opcode = OP_LSUBCC;
		else
			opcode = OP_SUBCC;
		EMIT_NEW_BIALU (cfg, repl, opcode, ins->dreg, ins->sreg1, ins->sreg2);
		MONO_EMIT_NEW_COND_EXC (cfg, C, "OverflowException");
		NULLIFY_INS (ins);
		break;
	}
#endif
		
	case OP_ICONV_TO_OVF_I8:
	case OP_ICONV_TO_OVF_I:
		ins->opcode = OP_SEXT_I4;
		break;
	case OP_ICONV_TO_OVF_U8:
	case OP_ICONV_TO_OVF_U:
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg,ins->sreg1, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_ZEXT_I4, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_I8_UN:
	case OP_ICONV_TO_OVF_U8_UN:
	case OP_ICONV_TO_OVF_I_UN:
	case OP_ICONV_TO_OVF_U_UN:
		/* an unsigned 32 bit num always fits in an (un)signed 64 bit one */
		/* Clean out the upper word */
		MONO_EMIT_NEW_UNALU (cfg, OP_ZEXT_I4, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I1:
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 127);
		MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, -128);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_LCONV_TO_I1, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I1_UN:
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 127);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_LCONV_TO_I1, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U1:
		/* probe value to be within 0 to 255 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 255);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, ins->dreg, ins->sreg1, 0xff);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U1_UN:
		/* probe value to be within 0 to 255 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 255);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, ins->dreg, ins->sreg1, 0xff);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I2:
		/* Probe value to be within -32768 and 32767 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 32767);
		MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, -32768);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_LCONV_TO_I2, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I2_UN:
		/* Probe value to be within 0 and 32767 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 32767);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_LCONV_TO_I2, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U2:
		/* Probe value to be within 0 and 65535 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0xffff);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, ins->dreg, ins->sreg1, 0xffff);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U2_UN:
		/* Probe value to be within 0 and 65535 */
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0xffff);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, ins->dreg, ins->sreg1, 0xffff);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I4:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_LCONV_TO_OVF_I:
#endif
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0x7fffffff);
		MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
		/* The int cast is needed for the VS compiler.  See Compiler Warning (level 2) C4146. */
#if SIZEOF_REGISTER == 8
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, ((int)-2147483648));
#else
		g_assert (COMPILE_LLVM (cfg));
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, -2147483648LL);
#endif
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_I4_UN:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_LCONV_TO_OVF_I_UN:
#endif
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0x7fffffff);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U4:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_LCONV_TO_OVF_U:
#endif
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0xffffffffUL);
		MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U4_UN:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_LCONV_TO_OVF_U_UN:
#endif
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0xffffffff);
		MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
#if TARGET_SIZEOF_VOID_P == 8
	case OP_LCONV_TO_OVF_I:
	case OP_LCONV_TO_OVF_U_UN:
#endif
	case OP_LCONV_TO_OVF_U8_UN:
	case OP_LCONV_TO_OVF_I8:
		ins->opcode = OP_MOVE;
		break;
#if TARGET_SIZEOF_VOID_P == 8
	case OP_LCONV_TO_OVF_I_UN:
#endif
	case OP_LCONV_TO_OVF_I8_UN:
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_LCONV_TO_OVF_U8:
#if TARGET_SIZEOF_VOID_P == 8
	case OP_LCONV_TO_OVF_U:
#endif
		MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg1, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	default:
		return FALSE;
	}

	*repl_ins = repl;
	return TRUE;
}

/*
 * mono_decompose_opcode:
 *
 *   Decompose complex opcodes into ones closer to opcodes supported by
 * the given architecture.
 * Returns a MonoInst which represents the result of the decomposition, and can
 * be pushed on the IL stack. This is needed because the original instruction is
 * nullified.
 * Sets the cfg exception if an opcode is not supported.
 */
MonoInst*
mono_decompose_opcode (MonoCompile *cfg, MonoInst *ins)
{
	MonoInst *repl = NULL;
	int type = ins->type;
	int dreg = ins->dreg;
	gboolean emulate = FALSE;

	/* FIXME: Instead of = NOP, don't emit the original ins at all */
	mono_arch_decompose_opts (cfg, ins);

	/*
	 * The code below assumes that we are called immediately after emitting 
	 * ins. This means we can emit code using the normal code generation
	 * macros.
	 */
	switch (ins->opcode) {
	/* this doesn't make sense on ppc and other architectures */
#if !defined(MONO_ARCH_NO_IOV_CHECK)
	case OP_IADD_OVF:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_IADDCC;
		MONO_EMIT_NEW_COND_EXC (cfg, IOV, "OverflowException");
		break;
	case OP_IADD_OVF_UN:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_IADDCC;
		MONO_EMIT_NEW_COND_EXC (cfg, IC, "OverflowException");
		break;
	case OP_ISUB_OVF:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_ISUBCC;
		MONO_EMIT_NEW_COND_EXC (cfg, IOV, "OverflowException");
		break;
	case OP_ISUB_OVF_UN:
		if (COMPILE_LLVM (cfg))
			break;
		ins->opcode = OP_ISUBCC;
		MONO_EMIT_NEW_COND_EXC (cfg, IC, "OverflowException");
		break;
#endif
	case OP_ICONV_TO_OVF_I1:
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 127);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT, "OverflowException");
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, -128);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I1, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_I1_UN:
		/* probe values between 0 to 127 */
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 127);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I1, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_U1:
	case OP_ICONV_TO_OVF_U1_UN:
		/* probe value to be within 0 to 255 */
		MONO_EMIT_NEW_COMPARE_IMM (cfg, ins->sreg1, 255);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, ins->dreg, ins->sreg1, 0xff);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_I2:
		/* Probe value to be within -32768 and 32767 */
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 32767);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT, "OverflowException");
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, -32768);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I2, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_I2_UN:
		/* Convert uint value into short, value within 0 and 32767 */
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 32767);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT_UN, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I2, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_U2:
	case OP_ICONV_TO_OVF_U2_UN:
		/* Probe value to be within 0 and 65535 */
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 0xffff);
		MONO_EMIT_NEW_COND_EXC (cfg, IGT_UN, "OverflowException");
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, ins->dreg, ins->sreg1, 0xffff);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_OVF_U4:
	case OP_ICONV_TO_OVF_I4_UN:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_ICONV_TO_OVF_U:
	case OP_ICONV_TO_OVF_I_UN:
#endif
		MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, ILT, "OverflowException");
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, ins->sreg1);
		NULLIFY_INS (ins);
		break;
	case OP_ICONV_TO_I4:
	case OP_ICONV_TO_U4:
	case OP_ICONV_TO_OVF_I4:
	case OP_ICONV_TO_OVF_U4_UN:
#if TARGET_SIZEOF_VOID_P == 4
	case OP_ICONV_TO_OVF_I:
	case OP_ICONV_TO_OVF_U_UN:
#endif
		ins->opcode = OP_MOVE;
		break;
	case OP_ICONV_TO_I:
#if TARGET_SIZEOF_VOID_P == 8
		ins->opcode = OP_SEXT_I4;
#else
		ins->opcode = OP_MOVE;
#endif
		break;
	case OP_ICONV_TO_U:
#if TARGET_SIZEOF_VOID_P == 8
		ins->opcode = OP_ZEXT_I4;
#else
		ins->opcode = OP_MOVE;
#endif
		break;

	case OP_FCONV_TO_R8:
		ins->opcode = OP_FMOVE;
		break;

	case OP_IDIV:
	case OP_IREM:
	case OP_IDIV_UN:
	case OP_IREM_UN:
		if (cfg->backend->emulate_div && mono_arch_opcode_needs_emulation (cfg, ins->opcode))
			emulate = TRUE;
		if (!emulate) {
			if (cfg->backend->need_div_check) {
				int reg1 = alloc_ireg (cfg);
				int reg2 = alloc_ireg (cfg);
				/* b == 0 */
				MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg2, 0);
				MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "DivideByZeroException");
				if (ins->opcode == OP_IDIV || ins->opcode == OP_IREM) {
					/* b == -1 && a == 0x80000000 */
					MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg2, -1);
					MONO_EMIT_NEW_UNALU (cfg, OP_ICEQ, reg1, -1);
					MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 0x80000000);
					MONO_EMIT_NEW_UNALU (cfg, OP_ICEQ, reg2, -1);
					MONO_EMIT_NEW_BIALU (cfg, OP_IAND, reg1, reg1, reg2);
					MONO_EMIT_NEW_ICOMPARE_IMM (cfg, reg1, 1);
					MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "OverflowException");
				}
			}
			MONO_EMIT_NEW_BIALU (cfg, ins->opcode, ins->dreg, ins->sreg1, ins->sreg2);
			NULLIFY_INS (ins);
		}
		break;

#if TARGET_SIZEOF_VOID_P == 8
	case OP_LDIV:
	case OP_LREM:
	case OP_LDIV_UN:
	case OP_LREM_UN:
		if (cfg->backend->emulate_div && mono_arch_opcode_needs_emulation (cfg, ins->opcode))
			emulate = TRUE;
		if (!emulate) {
			if (cfg->backend->need_div_check) {
				int reg1 = alloc_ireg (cfg);
				int reg2 = alloc_ireg (cfg);
				int reg3 = alloc_ireg (cfg);
				/* b == 0 */
				MONO_EMIT_NEW_LCOMPARE_IMM (cfg, ins->sreg2, 0);
				MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "DivideByZeroException");
				if (ins->opcode == OP_LDIV || ins->opcode == OP_LREM) {
					/* b == -1 && a == 0x80000000 */
					MONO_EMIT_NEW_I8CONST (cfg, reg3, -1);
					MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg2, reg3);
					MONO_EMIT_NEW_UNALU (cfg, OP_LCEQ, reg1, -1);
					MONO_EMIT_NEW_I8CONST (cfg, reg3, 0x8000000000000000L);
					MONO_EMIT_NEW_BIALU (cfg, OP_LCOMPARE, -1, ins->sreg1, reg3);
					MONO_EMIT_NEW_UNALU (cfg, OP_LCEQ, reg2, -1);
					MONO_EMIT_NEW_BIALU (cfg, OP_IAND, reg1, reg1, reg2);
					MONO_EMIT_NEW_ICOMPARE_IMM (cfg, reg1, 1);
					MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "OverflowException");
				}
			}
			MONO_EMIT_NEW_BIALU (cfg, ins->opcode, ins->dreg, ins->sreg1, ins->sreg2);
			NULLIFY_INS (ins);
		}
		break;
#endif

	case OP_DIV_IMM:
	case OP_REM_IMM:
	case OP_IDIV_IMM:
	case OP_IREM_IMM:
	case OP_IDIV_UN_IMM:
	case OP_IREM_UN_IMM:
		if (cfg->backend->need_div_check) {
			int reg1 = alloc_ireg (cfg);
			/* b == 0 */
			if (ins->inst_imm == 0) {
				// FIXME: Optimize this
				MONO_EMIT_NEW_ICONST (cfg, reg1, 0);
				MONO_EMIT_NEW_ICOMPARE_IMM (cfg, reg1, 0);
				MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "DivideByZeroException");
			}
			if ((ins->opcode == OP_DIV_IMM || ins->opcode == OP_IDIV_IMM || ins->opcode == OP_REM_IMM || ins->opcode == OP_IREM_IMM) &&
				(ins->inst_imm == -1)) {
					/* b == -1 && a == 0x80000000 */
					MONO_EMIT_NEW_ICOMPARE_IMM (cfg, ins->sreg1, 0x80000000);
					MONO_EMIT_NEW_COND_EXC (cfg, IEQ, "OverflowException");
			}
			MONO_EMIT_NEW_BIALU_IMM (cfg, ins->opcode, ins->dreg, ins->sreg1, ins->inst_imm);
			NULLIFY_INS (ins);
		} else {
			emulate = TRUE;
		}
		break;
	case OP_ICONV_TO_R_UN:
#ifdef MONO_ARCH_EMULATE_CONV_R8_UN
		if (!COMPILE_LLVM (cfg))
			emulate = TRUE;
#endif
		break;
	default:
		emulate = TRUE;
		break;
	}

	if (emulate) {
#if SIZEOF_REGISTER == 8
		if (decompose_long_opcode (cfg, ins, &repl))
			emulate = FALSE;
#else
		if (COMPILE_LLVM (cfg) && decompose_long_opcode (cfg, ins, &repl))
			emulate = FALSE;
#endif

		if (emulate && mono_find_jit_opcode_emulation (ins->opcode))
			cfg->has_emulated_ops = TRUE;
	}

	if (ins->opcode == OP_NOP) {
		if (repl) {
			repl->type = type;
			return repl;
		} else {
			/* Use the last emitted instruction */
			ins = cfg->cbb->last_ins;
			g_assert (ins);
			ins->type = type;
			g_assert (ins->dreg == dreg);
			return ins;
		}
	} else {
		return ins;
	}
}

#if SIZEOF_REGISTER == 4
static int lbr_decomp [][2] = {
	{0, 0}, /* BEQ */
	{OP_IBGT, OP_IBGE_UN}, /* BGE */
	{OP_IBGT, OP_IBGT_UN}, /* BGT */
	{OP_IBLT, OP_IBLE_UN}, /* BLE */
	{OP_IBLT, OP_IBLT_UN}, /* BLT */
	{0, 0}, /* BNE_UN */
	{OP_IBGT_UN, OP_IBGE_UN}, /* BGE_UN */
	{OP_IBGT_UN, OP_IBGT_UN}, /* BGT_UN */
	{OP_IBLT_UN, OP_IBLE_UN}, /* BLE_UN */
	{OP_IBLT_UN, OP_IBLT_UN}, /* BLT_UN */
};

static int lcset_decomp [][2] = {
	{0, 0}, /* CEQ */
	{OP_IBLT, OP_IBLE_UN}, /* CGT */
	{OP_IBLT_UN, OP_IBLE_UN}, /* CGT_UN */
	{OP_IBGT, OP_IBGE_UN}, /* CLT */
	{OP_IBGT_UN, OP_IBGE_UN}, /* CLT_UN */
};
#endif

/**
 * mono_decompose_long_opts:
 *
 *  Decompose 64bit opcodes into 32bit opcodes on 32 bit platforms.
 */
void
mono_decompose_long_opts (MonoCompile *cfg)
{
#if SIZEOF_REGISTER == 4
	MonoBasicBlock *bb, *first_bb;

	/*
	 * Some opcodes, like lcall can't be decomposed so the rest of the JIT
	 * needs to be able to handle long vregs.
	 */

	/**
	 * Create a dummy bblock and emit code into it so we can use the normal 
	 * code generation macros.
	 */
	cfg->cbb = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
	first_bb = cfg->cbb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree = mono_bb_first_inst(bb, FILTER_IL_SEQ_POINT);
		MonoInst *prev = NULL;

		   /*
		mono_print_bb (bb, "BEFORE LOWER_LONG_OPTS");
		*/

		cfg->cbb->code = cfg->cbb->last_ins = NULL;

		while (tree) {
			mono_arch_decompose_long_opts (cfg, tree);

			switch (tree->opcode) {
			case OP_I8CONST:
				MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_LS (tree->dreg), ins_get_l_low (tree));
				MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (tree->dreg), ins_get_l_high (tree));
				break;
			case OP_DUMMY_I8CONST:
				MONO_EMIT_NEW_DUMMY_INIT (cfg, MONO_LVREG_LS (tree->dreg), OP_DUMMY_ICONST);
				MONO_EMIT_NEW_DUMMY_INIT (cfg, MONO_LVREG_MS (tree->dreg), OP_DUMMY_ICONST);
				break;
			case OP_LMOVE:
			case OP_LCONV_TO_U8:
			case OP_LCONV_TO_I8:
			case OP_LCONV_TO_OVF_U8_UN:
			case OP_LCONV_TO_OVF_I8:
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1));
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1));
				break;
			case OP_STOREI8_MEMBASE_REG:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, tree->inst_destbasereg, tree->inst_offset + MINI_MS_WORD_OFFSET, MONO_LVREG_MS (tree->sreg1));
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, tree->inst_destbasereg, tree->inst_offset + MINI_LS_WORD_OFFSET, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LOADI8_MEMBASE:
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, MONO_LVREG_MS (tree->dreg), tree->inst_basereg, tree->inst_offset + MINI_MS_WORD_OFFSET);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, MONO_LVREG_LS (tree->dreg), tree->inst_basereg, tree->inst_offset + MINI_LS_WORD_OFFSET);
				break;

			case OP_ICONV_TO_I8: {
				guint32 tmpreg = alloc_ireg (cfg);

				/* branchless code:
				 * low = reg;
				 * tmp = low > -1 ? 1: 0;
				 * high = tmp - 1; if low is zero or pos high becomes 0, else -1
				 */
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), tree->sreg1);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, MONO_LVREG_LS (tree->dreg), -1);
				MONO_EMIT_NEW_BIALU (cfg, OP_ICGT, tmpreg, -1, -1);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ISUB_IMM, MONO_LVREG_MS (tree->dreg), tmpreg, 1);
				break;
			}
			case OP_ICONV_TO_U8:
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), tree->sreg1);
				MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (tree->dreg), 0);
				break;
			case OP_ICONV_TO_OVF_I8:
				/* a signed 32 bit num always fits in a signed 64 bit one */
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_IMM, MONO_LVREG_MS (tree->dreg), tree->sreg1, 31);
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), tree->sreg1);
				break;
			case OP_ICONV_TO_OVF_U8:
				MONO_EMIT_NEW_COMPARE_IMM (cfg, tree->sreg1, 0);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
				MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (tree->dreg), 0);
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), tree->sreg1);
				break;
			case OP_ICONV_TO_OVF_I8_UN:
			case OP_ICONV_TO_OVF_U8_UN:
				/* an unsigned 32 bit num always fits in an (un)signed 64 bit one */
				MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (tree->dreg), 0);
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), tree->sreg1);
				break;
			case OP_LCONV_TO_I1:
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I1, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_U1:
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_U1, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_I2:
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I2, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_U2:
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_U2, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_I4:
			case OP_LCONV_TO_U4:
			case OP_LCONV_TO_I:
			case OP_LCONV_TO_U:
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
#ifndef MONO_ARCH_EMULATE_LCONV_TO_R8
			case OP_LCONV_TO_R8:
				MONO_EMIT_NEW_BIALU (cfg, OP_LCONV_TO_R8_2, tree->dreg, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_MS (tree->sreg1));
				break;
#endif
#ifndef MONO_ARCH_EMULATE_LCONV_TO_R4
			case OP_LCONV_TO_R4:
				MONO_EMIT_NEW_BIALU (cfg, OP_LCONV_TO_R4_2, tree->dreg, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_MS (tree->sreg1));
				break;
#endif
#ifndef MONO_ARCH_EMULATE_LCONV_TO_R8_UN
			case OP_LCONV_TO_R_UN:
				MONO_EMIT_NEW_BIALU (cfg, OP_LCONV_TO_R_UN_2, tree->dreg, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_MS (tree->sreg1));
				break;
#endif
			case OP_LCONV_TO_OVF_I1: {
				MonoBasicBlock *is_negative, *end_label;

				NEW_BBLOCK (cfg, is_negative);
				NEW_BBLOCK (cfg, end_label);

				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), -1);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBLT, is_negative);

				/* Positive */
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 127);
				MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_label);

				/* Negative */
				MONO_START_BB (cfg, is_negative);
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), -128);
				MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "OverflowException");

				MONO_START_BB (cfg, end_label);

				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I1, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			}
			case OP_LCONV_TO_OVF_I1_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");

				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 127);
				MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), -128);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I1, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_U1:
			case OP_LCONV_TO_OVF_U1_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");

				/* probe value to be within 0 to 255 */
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 255);
				MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, tree->dreg, MONO_LVREG_LS (tree->sreg1), 0xff);
				break;
			case OP_LCONV_TO_OVF_I2: {
				MonoBasicBlock *is_negative, *end_label;

				NEW_BBLOCK (cfg, is_negative);
				NEW_BBLOCK (cfg, end_label);

				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), -1);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBLT, is_negative);

				/* Positive */
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 32767);
				MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_label);

				/* Negative */
				MONO_START_BB (cfg, is_negative);
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), -32768);
				MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "OverflowException");
				MONO_START_BB (cfg, end_label);

				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I2, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			}
			case OP_LCONV_TO_OVF_I2_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");

				/* Probe value to be within -32768 and 32767 */
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 32767);
				MONO_EMIT_NEW_COND_EXC (cfg, GT, "OverflowException");
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), -32768);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
				MONO_EMIT_NEW_UNALU (cfg, OP_ICONV_TO_I2, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_U2:
			case OP_LCONV_TO_OVF_U2_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");

				/* Probe value to be within 0 and 65535 */
				MONO_EMIT_NEW_COMPARE_IMM (cfg, MONO_LVREG_LS (tree->sreg1), 0xffff);
				MONO_EMIT_NEW_COND_EXC (cfg, GT_UN, "OverflowException");
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, tree->dreg, MONO_LVREG_LS (tree->sreg1), 0xffff);
				break;
			case OP_LCONV_TO_OVF_I4:
			case OP_LCONV_TO_OVF_I:
				MONO_EMIT_NEW_BIALU (cfg, OP_LCONV_TO_OVF_I4_2, tree->dreg, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_MS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_U4:
			case OP_LCONV_TO_OVF_U:
			case OP_LCONV_TO_OVF_U4_UN:
			case OP_LCONV_TO_OVF_U_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_I_UN:
			case OP_LCONV_TO_OVF_I4_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "OverflowException");
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_LS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, tree->dreg, MONO_LVREG_LS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_U8:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1));
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1));
				break;
			case OP_LCONV_TO_OVF_I8_UN:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, MONO_LVREG_MS (tree->sreg1), 0);
				MONO_EMIT_NEW_COND_EXC (cfg, LT, "OverflowException");

				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1));
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1));
				break;

			case OP_LADD:
				MONO_EMIT_NEW_BIALU (cfg, OP_IADDCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IADC, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				break;
			case OP_LSUB:
				MONO_EMIT_NEW_BIALU (cfg, OP_ISUBCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_ISBB, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				break;

			case OP_LADD_OVF:
				/* ADC sets the condition code */
				MONO_EMIT_NEW_BIALU (cfg, OP_IADDCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IADC, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				MONO_EMIT_NEW_COND_EXC (cfg, OV, "OverflowException");
				break;
			case OP_LADD_OVF_UN:
				/* ADC sets the condition code */
				MONO_EMIT_NEW_BIALU (cfg, OP_IADDCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IADC, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				MONO_EMIT_NEW_COND_EXC (cfg, C, "OverflowException");
				break;
			case OP_LSUB_OVF:
				/* SBB sets the condition code */
				MONO_EMIT_NEW_BIALU (cfg, OP_ISUBCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_ISBB, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				MONO_EMIT_NEW_COND_EXC (cfg, OV, "OverflowException");
				break;
			case OP_LSUB_OVF_UN:
				/* SBB sets the condition code */
				MONO_EMIT_NEW_BIALU (cfg, OP_ISUBCC, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_ISBB, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				MONO_EMIT_NEW_COND_EXC (cfg, C, "OverflowException");
				break;
			case OP_LAND:
				MONO_EMIT_NEW_BIALU (cfg, OP_IAND, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IAND, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				break;
			case OP_LOR:
				MONO_EMIT_NEW_BIALU (cfg, OP_IOR, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IOR, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				break;
			case OP_LXOR:
				MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
				MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
				break;
			case OP_LNOT:
				MONO_EMIT_NEW_UNALU (cfg, OP_INOT, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1));
				MONO_EMIT_NEW_UNALU (cfg, OP_INOT, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1));
				break;
			case OP_LNEG:
				/* Handled in mono_arch_decompose_long_opts () */
				g_assert_not_reached ();
				break;
			case OP_LMUL:
				/* Emulated */
				/* FIXME: Add OP_BIGMUL optimization */
				break;

			case OP_LADD_IMM:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADDCC_IMM, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), ins_get_l_low (tree));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ADC_IMM, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), ins_get_l_high (tree));
				break;
			case OP_LSUB_IMM:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUBCC_IMM, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), ins_get_l_low (tree));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SBB_IMM, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), ins_get_l_high (tree));
				break;
			case OP_LAND_IMM:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), ins_get_l_low (tree));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), ins_get_l_high (tree));
				break;
			case OP_LOR_IMM:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_OR_IMM, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), ins_get_l_low (tree));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_OR_IMM, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), ins_get_l_high (tree));
				break;
			case OP_LXOR_IMM:
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_XOR_IMM, MONO_LVREG_LS (tree->dreg), MONO_LVREG_LS (tree->sreg1), ins_get_l_low (tree));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_XOR_IMM, MONO_LVREG_MS (tree->dreg), MONO_LVREG_MS (tree->sreg1), ins_get_l_high (tree));
				break;
#ifdef TARGET_POWERPC
/* FIXME This is normally handled in cprop. Proper fix or remove if no longer needed. */
			case OP_LSHR_UN_IMM:
				if (tree->inst_c1 == 32) {

					/* The original code had this comment: */
					/* special case that gives a nice speedup and happens to workaorund a ppc jit but (for the release)
					 * later apply the speedup to the left shift as well
					 * See BUG# 57957.
					 */
					/* just move the upper half to the lower and zero the high word */
					MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (tree->dreg), MONO_LVREG_MS (tree->sreg1));
					MONO_EMIT_NEW_ICONST (cfg, MONO_LVREG_MS (tree->dreg), 0);
				}
				break;
#endif
			case OP_LCOMPARE: {
				MonoInst *next = mono_inst_next (tree, FILTER_IL_SEQ_POINT);

				g_assert (next);

				switch (next->opcode) {
				case OP_LBEQ:
				case OP_LBNE_UN: {
					int d1, d2;

					/* Branchless version based on gcc code */
					d1 = alloc_ireg (cfg);
					d2 = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, d1, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
					MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, d2, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BIALU (cfg, OP_IOR, d1, d1, d2);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, d1, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK2 (cfg, next->opcode == OP_LBEQ ? OP_IBEQ : OP_IBNE_UN, next->inst_true_bb, next->inst_false_bb);
					NULLIFY_INS (next);
					break;
				}
				case OP_LBGE:
				case OP_LBGT:
				case OP_LBLE:
				case OP_LBLT:
				case OP_LBGE_UN:
				case OP_LBGT_UN:
				case OP_LBLE_UN:
				case OP_LBLT_UN:
					/* Convert into three comparisons + branches */
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lbr_decomp [next->opcode - OP_LBEQ][0], next->inst_true_bb);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, next->inst_false_bb);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK2 (cfg, lbr_decomp [next->opcode - OP_LBEQ][1], next->inst_true_bb, next->inst_false_bb);
					NULLIFY_INS (next);
					break;
				case OP_LCEQ: {
					int d1, d2;
	
					/* Branchless version based on gcc code */
					d1 = alloc_ireg (cfg);
					d2 = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, d1, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
					MONO_EMIT_NEW_BIALU (cfg, OP_IXOR, d2, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BIALU (cfg, OP_IOR, d1, d1, d2);

					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, d1, 0);
					MONO_EMIT_NEW_UNALU (cfg, OP_ICEQ, next->dreg, -1);
					NULLIFY_INS (next);
					break;
				}
				case OP_LCLT:
				case OP_LCLT_UN:
				case OP_LCGT:
				case OP_LCGT_UN: {
					MonoBasicBlock *set_to_0, *set_to_1;
	
					NEW_BBLOCK (cfg, set_to_0);
					NEW_BBLOCK (cfg, set_to_1);

					MONO_EMIT_NEW_ICONST (cfg, next->dreg, 0);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lcset_decomp [next->opcode - OP_LCEQ][0], set_to_0);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_MS (tree->sreg1), MONO_LVREG_MS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, set_to_1);
					MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, MONO_LVREG_LS (tree->sreg1), MONO_LVREG_LS (tree->sreg2));
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lcset_decomp [next->opcode - OP_LCEQ][1], set_to_0);
					MONO_START_BB (cfg, set_to_1);
					MONO_EMIT_NEW_ICONST (cfg, next->dreg, 1);
					MONO_START_BB (cfg, set_to_0);
					NULLIFY_INS (next);
					break;	
				}
				default:
					g_assert_not_reached ();
				}
				break;
			}

			/* Not yet used, since lcompare is decomposed before local cprop */
			case OP_LCOMPARE_IMM: {
				MonoInst *next = mono_inst_next (tree, FILTER_IL_SEQ_POINT);
				guint32 low_imm = ins_get_l_low (tree);
				guint32 high_imm = ins_get_l_high (tree);
				int low_reg = MONO_LVREG_LS (tree->sreg1);
				int high_reg = MONO_LVREG_MS (tree->sreg1);

				g_assert (next);

				switch (next->opcode) {
				case OP_LBEQ:
				case OP_LBNE_UN: {
					int d1, d2;

					/* Branchless version based on gcc code */
					d1 = alloc_ireg (cfg);
					d2 = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IXOR_IMM, d1, low_reg, low_imm);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IXOR_IMM, d2, high_reg, high_imm);
					MONO_EMIT_NEW_BIALU (cfg, OP_IOR, d1, d1, d2);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, d1, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK2 (cfg, next->opcode == OP_LBEQ ? OP_IBEQ : OP_IBNE_UN, next->inst_true_bb, next->inst_false_bb);
					NULLIFY_INS (next);
					break;
				}

				case OP_LBGE:
				case OP_LBGT:
				case OP_LBLE:
				case OP_LBLT:
				case OP_LBGE_UN:
				case OP_LBGT_UN:
				case OP_LBLE_UN:
				case OP_LBLT_UN:
					/* Convert into three comparisons + branches */
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, high_reg, high_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lbr_decomp [next->opcode - OP_LBEQ][0], next->inst_true_bb);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, high_reg, high_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, next->inst_false_bb);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, low_reg, low_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK2 (cfg, lbr_decomp [next->opcode - OP_LBEQ][1], next->inst_true_bb, next->inst_false_bb);
					NULLIFY_INS (next);
					break;
				case OP_LCEQ: {
					int d1, d2;
	
					/* Branchless version based on gcc code */
					d1 = alloc_ireg (cfg);
					d2 = alloc_ireg (cfg);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IXOR_IMM, d1, low_reg, low_imm);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IXOR_IMM, d2, high_reg, high_imm);
					MONO_EMIT_NEW_BIALU (cfg, OP_IOR, d1, d1, d2);

					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, d1, 0);
					MONO_EMIT_NEW_UNALU (cfg, OP_ICEQ, next->dreg, -1);
					NULLIFY_INS (next);
					break;
				}
				case OP_LCLT:
				case OP_LCLT_UN:
				case OP_LCGT:
				case OP_LCGT_UN: {
					MonoBasicBlock *set_to_0, *set_to_1;
	
					NEW_BBLOCK (cfg, set_to_0);
					NEW_BBLOCK (cfg, set_to_1);

					MONO_EMIT_NEW_ICONST (cfg, next->dreg, 0);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, high_reg, high_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lcset_decomp [next->opcode - OP_LCEQ][0], set_to_0);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, high_reg, high_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, set_to_1);
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, low_reg, low_imm);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, lcset_decomp [next->opcode - OP_LCEQ][1], set_to_0);
					MONO_START_BB (cfg, set_to_1);
					MONO_EMIT_NEW_ICONST (cfg, next->dreg, 1);
					MONO_START_BB (cfg, set_to_0);
					NULLIFY_INS (next);
					break;	
				}
				default:
					g_assert_not_reached ();
				}
				break;
			}

			default:
				break;
			}

			if (cfg->cbb->code || (cfg->cbb != first_bb)) {
				MonoInst *new_prev;

				/* Replace the original instruction with the new code sequence */

				/* Ignore the new value of prev */
				new_prev = prev;
				mono_replace_ins (cfg, bb, tree, &new_prev, first_bb, cfg->cbb);

				/* Process the newly added ops again since they can be long ops too */
				if (prev)
					tree = mono_inst_next (prev, FILTER_IL_SEQ_POINT);
				else
					tree = mono_bb_first_inst (bb, FILTER_IL_SEQ_POINT);

				first_bb->code = first_bb->last_ins = NULL;
				first_bb->in_count = first_bb->out_count = 0;
				cfg->cbb = first_bb;
			}
			else {
				prev = tree;
				tree = mono_inst_next (tree, FILTER_IL_SEQ_POINT);
			}
		}
	}
#endif

	/*
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		mono_print_bb (bb, "AFTER LOWER-LONG-OPTS");
	*/
}

/**
 * mono_decompose_vtype_opts:
 *
 *  Decompose valuetype opcodes.
 */
void
mono_decompose_vtype_opts (MonoCompile *cfg)
{
	MonoBasicBlock *bb, *first_bb;

	/**
	 * Using OP_V opcodes and decomposing them later have two main benefits:
	 * - it simplifies method_to_ir () since there is no need to special-case vtypes
	 *   everywhere.
	 * - it gets rid of the LDADDR opcodes generated when vtype operations are decomposed,
	 *   enabling optimizations to work on vtypes too.
	 * Unlike decompose_long_opts, this pass does not alter the CFG of the method so it 
	 * can be executed anytime. It should be executed as late as possible so vtype
	 * opcodes can be optimized by the other passes.
	 * The pinvoke wrappers need to manipulate vtypes in their unmanaged representation.
	 * This is indicated by setting the 'backend.is_pinvoke' field of the MonoInst for the 
	 * var to 1.
	 * This is done on demand, ie. by the LDNATIVEOBJ opcode, and propagated by this pass 
	 * when OP_VMOVE opcodes are decomposed.
	 */

	/* 
	 * Vregs have no associated type information, so we store the type of the vregs
	 * in ins->klass.
	 */

	/**
	 * Create a dummy bblock and emit code into it so we can use the normal 
	 * code generation macros.
	 */
	cfg->cbb = (MonoBasicBlock *)mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
	first_bb = cfg->cbb;

	/* For LLVM, decompose only the OP_STOREV_MEMBASE opcodes, which need write barriers and the gsharedvt opcodes */

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MonoInst *prev = NULL;
		MonoInst *src_var, *dest_var, *src, *dest;
		gboolean restart;
		int dreg;

		if (cfg->verbose_level > 2) mono_print_bb (bb, "BEFORE LOWER-VTYPE-OPTS ");

		cfg->cbb->code = cfg->cbb->last_ins = NULL;
		cfg->cbb->out_of_line = bb->out_of_line;
		restart = TRUE;

		while (restart) {
			restart = FALSE;

			for (ins = bb->code; ins; ins = ins->next) {
#ifdef MONO_ARCH_SIMD_INTRINSICS
				mono_simd_decompose_intrinsic (cfg, bb, ins);
#endif
				switch (ins->opcode) {
				case OP_VMOVE: {
					g_assert (ins->klass);
					if (COMPILE_LLVM (cfg) && !mini_is_gsharedvt_klass (ins->klass))
						break;
					src_var = get_vreg_to_inst (cfg, ins->sreg1);
					dest_var = get_vreg_to_inst (cfg, ins->dreg);

					if (!src_var)
						src_var = mono_compile_create_var_for_vreg (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL, ins->dreg);

					if (!dest_var)
						dest_var = mono_compile_create_var_for_vreg (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL, ins->dreg);

					// FIXME:
					if (src_var->backend.is_pinvoke)
						dest_var->backend.is_pinvoke = 1;

					EMIT_NEW_VARLOADA ((cfg), (src), src_var, src_var->inst_vtype);
					EMIT_NEW_VARLOADA ((cfg), (dest), dest_var, dest_var->inst_vtype);
					mini_emit_memory_copy (cfg, dest, src, src_var->klass, src_var->backend.is_pinvoke, 0);

					break;
				}
				case OP_VZERO:
					if (COMPILE_LLVM (cfg) && !mini_is_gsharedvt_klass (ins->klass))
						break;

					g_assert (ins->klass);

					EMIT_NEW_VARLOADA_VREG (cfg, dest, ins->dreg, m_class_get_byval_arg (ins->klass));

					mini_emit_initobj (cfg, dest, NULL, ins->klass);
					
					if (cfg->compute_gc_maps) {
						MonoInst *tmp;

						/* 
						 * Tell the GC map code that the vtype is considered live after
						 * the initialization.
						 */
						MONO_INST_NEW (cfg, tmp, OP_GC_LIVENESS_DEF);
						tmp->inst_c1 = ins->dreg;
						MONO_ADD_INS (cfg->cbb, tmp);
					}
					break;
				case OP_DUMMY_VZERO:
					if (COMPILE_LLVM (cfg))
						break;

					NULLIFY_INS (ins);
					break;
				case OP_STOREV_MEMBASE: {
					src_var = get_vreg_to_inst (cfg, ins->sreg1);

					mono_class_init_sizes (ins->klass);

					if (COMPILE_LLVM (cfg) && !mini_is_gsharedvt_klass (ins->klass) && !(cfg->gen_write_barriers && m_class_has_references (ins->klass)))
						break;

					if (!src_var) {
						g_assert (ins->klass);
						src_var = mono_compile_create_var_for_vreg (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL, ins->sreg1);
					}

					EMIT_NEW_VARLOADA_VREG ((cfg), (src), ins->sreg1, m_class_get_byval_arg (ins->klass));

					dreg = alloc_preg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, dest, OP_ADD_IMM, dreg, ins->inst_destbasereg, ins->inst_offset);
					mini_emit_memory_copy (cfg, dest, src, src_var->klass, src_var->backend.is_pinvoke, ins->flags);
					break;
				}
				case OP_LOADV_MEMBASE: {
					g_assert (ins->klass);
					if (COMPILE_LLVM (cfg) && !mini_is_gsharedvt_klass (ins->klass))
						break;

					dest_var = get_vreg_to_inst (cfg, ins->dreg);
					// FIXME-VT:
					// FIXME:
					if (!dest_var)
						dest_var = mono_compile_create_var_for_vreg (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL, ins->dreg);

					dreg = alloc_preg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, src, OP_ADD_IMM, dreg, ins->inst_basereg, ins->inst_offset);
					EMIT_NEW_VARLOADA (cfg, dest, dest_var, dest_var->inst_vtype);
					mini_emit_memory_copy (cfg, dest, src, dest_var->klass, dest_var->backend.is_pinvoke, 0);
					break;
				}
				case OP_OUTARG_VT: {
					if (COMPILE_LLVM (cfg))
						break;

					g_assert (ins->klass);

					src_var = get_vreg_to_inst (cfg, ins->sreg1);
					if (!src_var)
						src_var = mono_compile_create_var_for_vreg (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL, ins->sreg1);
					EMIT_NEW_VARLOADA (cfg, src, src_var, src_var->inst_vtype);

					mono_arch_emit_outarg_vt (cfg, ins, src);

					/* This might be decomposed into other vtype opcodes */
					restart = TRUE;
					break;
				}
				case OP_OUTARG_VTRETADDR: {
					MonoCallInst *call = (MonoCallInst*)ins->inst_p1;

					src_var = get_vreg_to_inst (cfg, call->inst.dreg);
					if (!src_var)
						src_var = mono_compile_create_var_for_vreg (cfg, call->signature->ret, OP_LOCAL, call->inst.dreg);
					// FIXME: src_var->backend.is_pinvoke ?

					EMIT_NEW_VARLOADA (cfg, src, src_var, src_var->inst_vtype);
					src->dreg = ins->dreg;
					break;
				}
				case OP_VCALL:
				case OP_VCALL_REG:
				case OP_VCALL_MEMBASE: {
					MonoCallInst *call = (MonoCallInst*)ins;
					int size;

					if (COMPILE_LLVM (cfg))
						break;

					if (call->vret_in_reg) {
						MonoCallInst *call2;
						int align;

						/* Replace the vcall with a scalar call */
						MONO_INST_NEW_CALL (cfg, call2, OP_NOP);
						memcpy (call2, call, sizeof (MonoCallInst));
						switch (ins->opcode) {
						case OP_VCALL:
							call2->inst.opcode = call->vret_in_reg_fp ? OP_FCALL : OP_CALL;
							break;
						case OP_VCALL_REG:
							call2->inst.opcode = call->vret_in_reg_fp ? OP_FCALL_REG : OP_CALL_REG;
							break;
						case OP_VCALL_MEMBASE:
							call2->inst.opcode = call->vret_in_reg_fp ? OP_FCALL_MEMBASE : OP_CALL_MEMBASE;
							break;
						}
						call2->inst.dreg = alloc_preg (cfg);
						MONO_ADD_INS (cfg->cbb, ((MonoInst*)call2));

						/* Compute the vtype location */
						dest_var = get_vreg_to_inst (cfg, call->inst.dreg);
						if (!dest_var)
							dest_var = mono_compile_create_var_for_vreg (cfg, call->signature->ret, OP_LOCAL, call->inst.dreg);
						EMIT_NEW_VARLOADA (cfg, dest, dest_var, dest_var->inst_vtype);

						/* Save the result */
						if (dest_var->backend.is_pinvoke)
							size = mono_class_native_size (mono_class_from_mono_type_internal (dest_var->inst_vtype), NULL);
						else
							size = mono_type_size (dest_var->inst_vtype, &align);
						switch (size) {
						case 1:
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
							break;
						case 2:
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
							break;
						case 3:
						case 4:
							if (call->vret_in_reg_fp)
								MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
							else
								MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
							break;
						case 5:
						case 6:
						case 7:
						case 8:
							if (call->vret_in_reg_fp) {
								MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
								break;
							}
#if SIZEOF_REGISTER == 4
							/*
							FIXME Other ABIs might return in different regs than the ones used for LCALL.
							FIXME It would be even nicer to be able to leverage the long decompose stuff.
							*/
							switch (call2->inst.opcode) {
							case OP_CALL:
								call2->inst.opcode = OP_LCALL;
								break;
							case OP_CALL_REG:
								call2->inst.opcode = OP_LCALL_REG;
								break;
							case OP_CALL_MEMBASE:
								call2->inst.opcode = OP_LCALL_MEMBASE;
								break;
							}
							call2->inst.dreg = alloc_lreg (cfg);
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, dest->dreg, MINI_MS_WORD_OFFSET, MONO_LVREG_MS (call2->inst.dreg));
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, dest->dreg, MINI_LS_WORD_OFFSET, MONO_LVREG_LS (call2->inst.dreg));
#else
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
#endif
							break;
						default:
							/* This assumes the vtype is sizeof (gpointer) long */
							MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, dest->dreg, 0, call2->inst.dreg);
							break;
						}
					} else {
						switch (ins->opcode) {
						case OP_VCALL:
							ins->opcode = OP_VCALL2;
							break;
						case OP_VCALL_REG:
							ins->opcode = OP_VCALL2_REG;
							break;
						case OP_VCALL_MEMBASE:
							ins->opcode = OP_VCALL2_MEMBASE;
							break;
						}
						ins->dreg = -1;
					}
					break;
				}
				case OP_BOX:
				case OP_BOX_ICONST: {
					MonoInst *src;

					/* Temporary value required by emit_box () */
					if (ins->opcode == OP_BOX_ICONST) {
						NEW_ICONST (cfg, src, ins->inst_c0);
						src->klass = ins->klass;
						MONO_ADD_INS (cfg->cbb, src);
					} else {
						MONO_INST_NEW (cfg, src, OP_LOCAL);
						src->type = STACK_MP;
						src->klass = ins->klass;
						src->dreg = ins->sreg1;
					}
					MonoInst *tmp = mini_emit_box (cfg, src, ins->klass, mini_class_check_context_used (cfg, ins->klass));
					g_assert (tmp);

					MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ins->dreg, tmp->dreg);

					/* This might be decomposed into other vtype opcodes */
					restart = TRUE;
					break;
				}
				default:
					break;
				}

				g_assert (cfg->cbb == first_bb);

				if (cfg->cbb->code || (cfg->cbb != first_bb)) {
					/* Replace the original instruction with the new code sequence */

					mono_replace_ins (cfg, bb, ins, &prev, first_bb, cfg->cbb);
					first_bb->code = first_bb->last_ins = NULL;
					first_bb->in_count = first_bb->out_count = 0;
					cfg->cbb = first_bb;
				}
				else
					prev = ins;
			}
		}

		if (cfg->verbose_level > 2) mono_print_bb (bb, "AFTER LOWER-VTYPE-OPTS ");
	}
}

inline static MonoInst *
mono_get_domainvar (MonoCompile *cfg)
{
	if (!cfg->domainvar)
		cfg->domainvar = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
	return cfg->domainvar;
}

/**
 * mono_decompose_array_access_opts:
 *
 *  Decompose array access and other misc opcodes.
 */
void
mono_decompose_array_access_opts (MonoCompile *cfg)
{
	MonoBasicBlock *bb, *first_bb;

	/*
	 * Unlike decompose_long_opts, this pass does not alter the CFG of the method so it 
	 * can be executed anytime. It should be run before decompose_long
	 */

	/**
	 * Create a dummy bblock and emit code into it so we can use the normal 
	 * code generation macros.
	 */
	cfg->cbb = (MonoBasicBlock *)mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
	first_bb = cfg->cbb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MonoInst *prev = NULL;
		MonoInst *dest;
		MonoInst *iargs [3];
		gboolean restart;

		if (!bb->needs_decompose)
			continue;

		if (cfg->verbose_level > 3) mono_print_bb (bb, "BEFORE DECOMPOSE-ARRAY-ACCESS-OPTS ");

		cfg->cbb->code = cfg->cbb->last_ins = NULL;
		restart = TRUE;

		while (restart) {
			restart = FALSE;

			for (ins = bb->code; ins; ins = ins->next) {
				switch (ins->opcode) {
				case OP_TYPED_OBJREF:
					ins->opcode = OP_MOVE;
					break;
				case OP_LDLEN:
					NEW_LOAD_MEMBASE_FLAGS (cfg, dest, OP_LOADI4_MEMBASE, ins->dreg, ins->sreg1,
											ins->inst_imm, ins->flags);
					MONO_ADD_INS (cfg->cbb, dest);
					break;
				case OP_BOUNDS_CHECK:
					MONO_EMIT_NULL_CHECK (cfg, ins->sreg1, FALSE);
					if (COMPILE_LLVM (cfg)) {
						int index2_reg = alloc_preg (cfg);
						MONO_EMIT_NEW_UNALU (cfg, OP_SEXT_I4, index2_reg, ins->sreg2);
						MONO_EMIT_DEFAULT_BOUNDS_CHECK (cfg, ins->sreg1, ins->inst_imm, index2_reg, ins->flags & MONO_INST_FAULT, ins->inst_p0);
					} else {
						MONO_ARCH_EMIT_BOUNDS_CHECK (cfg, ins->sreg1, ins->inst_imm, ins->sreg2, ins->inst_p0);
					}
					break;
				case OP_NEWARR:
					if (cfg->opt & MONO_OPT_SHARED) {
						EMIT_NEW_DOMAINCONST (cfg, iargs [0]);
						EMIT_NEW_CLASSCONST (cfg, iargs [1], ins->inst_newa_class);
						MONO_INST_NEW (cfg, iargs [2], OP_MOVE);
						iargs [2]->dreg = ins->sreg1;

						dest = mono_emit_jit_icall (cfg, ves_icall_array_new, iargs);
						dest->dreg = ins->dreg;
					} else {
						MonoClass *array_class = mono_class_create_array (ins->inst_newa_class, 1);
						ERROR_DECL (vt_error);
						MonoVTable *vtable = mono_class_vtable_checked (cfg->domain, array_class, vt_error);
						MonoMethod *managed_alloc = mono_gc_get_managed_array_allocator (array_class);

						mono_error_assert_ok (vt_error); /*This shall not fail since we check for this condition on OP_NEWARR creation*/
						NEW_VTABLECONST (cfg, iargs [0], vtable);
						MONO_ADD_INS (cfg->cbb, iargs [0]);
						MONO_INST_NEW (cfg, iargs [1], OP_MOVE);
						iargs [1]->dreg = ins->sreg1;

						if (managed_alloc)
							dest = mono_emit_method_call (cfg, managed_alloc, iargs, NULL);
						else
							dest = mono_emit_jit_icall (cfg, ves_icall_array_new_specific, iargs);
						dest->dreg = ins->dreg;
					}
					break;
				case OP_STRLEN:
					MONO_EMIT_NEW_LOAD_MEMBASE_OP_FLAGS (cfg, OP_LOADI4_MEMBASE, ins->dreg,
														 ins->sreg1, MONO_STRUCT_OFFSET (MonoString, length), ins->flags | MONO_INST_INVARIANT_LOAD);
					break;
				default:
					break;
				}

				g_assert (cfg->cbb == first_bb);

				if (cfg->cbb->code || (cfg->cbb != first_bb)) {
					/* Replace the original instruction with the new code sequence */

					mono_replace_ins (cfg, bb, ins, &prev, first_bb, cfg->cbb);
					first_bb->code = first_bb->last_ins = NULL;
					first_bb->in_count = first_bb->out_count = 0;
					cfg->cbb = first_bb;
				}
				else
					prev = ins;
			}
		}

		if (cfg->verbose_level > 3) mono_print_bb (bb, "AFTER DECOMPOSE-ARRAY-ACCESS-OPTS ");
	}
}

typedef union {
	guint32 vali [2];
	gint64 vall;
	double vald;
} DVal;

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK

/**
 * mono_decompose_soft_float:
 *
 *  Soft float support on ARM. We store each double value in a pair of integer vregs,
 * similar to long support on 32 bit platforms. 32 bit float values require special
 * handling when used as locals, arguments, and in calls.
 * One big problem with soft-float is that there are few r4 test cases in our test suite.
 */
void
mono_decompose_soft_float (MonoCompile *cfg)
{
	MonoBasicBlock *bb, *first_bb;

	/*
	 * This pass creates long opcodes, so it should be run before decompose_long_opts ().
	 */

	/**
	 * Create a dummy bblock and emit code into it so we can use the normal 
	 * code generation macros.
	 */
	cfg->cbb = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
	first_bb = cfg->cbb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MonoInst *prev = NULL;
		gboolean restart;

		if (cfg->verbose_level > 3) mono_print_bb (bb, "BEFORE HANDLE-SOFT-FLOAT ");

		cfg->cbb->code = cfg->cbb->last_ins = NULL;
		restart = TRUE;

		while (restart) {
			restart = FALSE;

			for (ins = bb->code; ins; ins = ins->next) {
				const char *spec = INS_INFO (ins->opcode);

				/* Most fp operations are handled automatically by opcode emulation */

				switch (ins->opcode) {
				case OP_R8CONST: {
					DVal d;
					d.vald = *(double*)ins->inst_p0;
					MONO_EMIT_NEW_I8CONST (cfg, ins->dreg, d.vall);
					break;
				}
				case OP_R4CONST: {
					DVal d;
					/* We load the r8 value */
					d.vald = *(float*)ins->inst_p0;
					MONO_EMIT_NEW_I8CONST (cfg, ins->dreg, d.vall);
					break;
				}
				case OP_FMOVE:
					ins->opcode = OP_LMOVE;
					break;
				case OP_FGETLOW32:
					ins->opcode = OP_MOVE;
					ins->sreg1 = MONO_LVREG_LS (ins->sreg1);
					break;
				case OP_FGETHIGH32:
					ins->opcode = OP_MOVE;
					ins->sreg1 = MONO_LVREG_MS (ins->sreg1);
					break;
				case OP_SETFRET: {
					int reg = ins->sreg1;

					ins->opcode = OP_SETLRET;
					ins->dreg = -1;
					ins->sreg1 = MONO_LVREG_LS (reg);
					ins->sreg2 = MONO_LVREG_MS (reg);
					break;
				}
				case OP_LOADR8_MEMBASE:
					ins->opcode = OP_LOADI8_MEMBASE;
					break;
				case OP_STORER8_MEMBASE_REG:
					ins->opcode = OP_STOREI8_MEMBASE_REG;
					break;
				case OP_STORER4_MEMBASE_REG: {
					MonoInst *iargs [2];
					int addr_reg;

					/* Arg 1 is the double value */
					MONO_INST_NEW (cfg, iargs [0], OP_ARG);
					iargs [0]->dreg = ins->sreg1;

					/* Arg 2 is the address to store to */
					addr_reg = mono_alloc_preg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, iargs [1], OP_PADD_IMM, addr_reg, ins->inst_destbasereg, ins->inst_offset);
					mono_emit_jit_icall (cfg, mono_fstore_r4, iargs);
					restart = TRUE;
					break;
				}
				case OP_LOADR4_MEMBASE: {
					MonoInst *iargs [1];
					MonoInst *conv;
					int addr_reg;

					addr_reg = mono_alloc_preg (cfg);
					EMIT_NEW_BIALU_IMM (cfg, iargs [0], OP_PADD_IMM, addr_reg, ins->inst_basereg, ins->inst_offset);
					conv = mono_emit_jit_icall (cfg, mono_fload_r4, iargs);
					conv->dreg = ins->dreg;
					break;
				}					
				case OP_FCALL:
				case OP_FCALL_REG:
				case OP_FCALL_MEMBASE: {
					MonoCallInst *call = (MonoCallInst*)ins;
					if (call->signature->ret->type == MONO_TYPE_R4) {
						MonoCallInst *call2;
						MonoInst *iargs [1];
						MonoInst *conv;
						GSList *l;

						/* Convert the call into a call returning an int */
						MONO_INST_NEW_CALL (cfg, call2, OP_CALL);
						memcpy (call2, call, sizeof (MonoCallInst));
						switch (ins->opcode) {
						case OP_FCALL:
							call2->inst.opcode = OP_CALL;
							break;
						case OP_FCALL_REG:
							call2->inst.opcode = OP_CALL_REG;
							break;
						case OP_FCALL_MEMBASE:
							call2->inst.opcode = OP_CALL_MEMBASE;
							break;
						default:
							g_assert_not_reached ();
						}
						call2->inst.dreg = mono_alloc_ireg (cfg);
						MONO_ADD_INS (cfg->cbb, (MonoInst*)call2);

						/* Remap OUTARG_VT instructions referencing this call */
						for (l = call->outarg_vts; l; l = l->next)
							((MonoInst*)(l->data))->inst_p0 = call2;

						/* FIXME: Optimize this */

						/* Emit an r4->r8 conversion */
						EMIT_NEW_VARLOADA_VREG (cfg, iargs [0], call2->inst.dreg, mono_get_int32_type ());
						conv = mono_emit_jit_icall (cfg, mono_fload_r4, iargs);
						conv->dreg = ins->dreg;

						/* The call sequence might include fp ins */
						restart = TRUE;
					} else {
						switch (ins->opcode) {
						case OP_FCALL:
							ins->opcode = OP_LCALL;
							break;
						case OP_FCALL_REG:
							ins->opcode = OP_LCALL_REG;
							break;
						case OP_FCALL_MEMBASE:
							ins->opcode = OP_LCALL_MEMBASE;
							break;
						default:
							g_assert_not_reached ();
						}
					}
					break;
				}
				case OP_FCOMPARE: {
					MonoJitICallInfo *info;
					MonoInst *iargs [2];
					MonoInst *call, *cmp, *br;

					/* Convert fcompare+fbcc to icall+icompare+beq */

					if (!ins->next) {
						/* The branch might be optimized away */
						NULLIFY_INS (ins);
						break;
					}

					info = mono_find_jit_opcode_emulation (ins->next->opcode);
					if (!info) {
						/* The branch might be optimized away */
						NULLIFY_INS (ins);
						break;
					}

					/* Create dummy MonoInst's for the arguments */
					MONO_INST_NEW (cfg, iargs [0], OP_ARG);
					iargs [0]->dreg = ins->sreg1;
					MONO_INST_NEW (cfg, iargs [1], OP_ARG);
					iargs [1]->dreg = ins->sreg2;

					call = mono_emit_jit_icall_id (cfg, mono_jit_icall_info_id (info), iargs);

					MONO_INST_NEW (cfg, cmp, OP_ICOMPARE_IMM);
					cmp->sreg1 = call->dreg;
					cmp->inst_imm = 0;
					MONO_ADD_INS (cfg->cbb, cmp);
					
					MONO_INST_NEW (cfg, br, OP_IBNE_UN);
					br->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * 2);
					br->inst_true_bb = ins->next->inst_true_bb;
					br->inst_false_bb = ins->next->inst_false_bb;
					MONO_ADD_INS (cfg->cbb, br);

					/* The call sequence might include fp ins */
					restart = TRUE;

					/* Skip fbcc or fccc */
					NULLIFY_INS (ins->next);
					break;
				}
				case OP_FCEQ:
				case OP_FCGT:
				case OP_FCGT_UN:
				case OP_FCLT:
				case OP_FCLT_UN: {
					MonoJitICallInfo *info;
					MonoInst *iargs [2];
					MonoInst *call;

					/* Convert fccc to icall+icompare+iceq */

					info = mono_find_jit_opcode_emulation (ins->opcode);
					g_assert (info);

					/* Create dummy MonoInst's for the arguments */
					MONO_INST_NEW (cfg, iargs [0], OP_ARG);
					iargs [0]->dreg = ins->sreg1;
					MONO_INST_NEW (cfg, iargs [1], OP_ARG);
					iargs [1]->dreg = ins->sreg2;

					call = mono_emit_jit_icall_id (cfg, mono_jit_icall_info_id (info), iargs);

					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ICOMPARE_IMM, -1, call->dreg, 1);
					MONO_EMIT_NEW_UNALU (cfg, OP_ICEQ, ins->dreg, -1);

					/* The call sequence might include fp ins */
					restart = TRUE;
					break;
				}
				case OP_CKFINITE: {
					MonoInst *iargs [2];
					MonoInst *call, *cmp;

					/* Convert to icall+icompare+cond_exc+move */

					/* Create dummy MonoInst's for the arguments */
					MONO_INST_NEW (cfg, iargs [0], OP_ARG);
					iargs [0]->dreg = ins->sreg1;

					call = mono_emit_jit_icall (cfg, mono_isfinite_double, iargs);

					MONO_INST_NEW (cfg, cmp, OP_ICOMPARE_IMM);
					cmp->sreg1 = call->dreg;
					cmp->inst_imm = 1;
					MONO_ADD_INS (cfg->cbb, cmp);

					MONO_EMIT_NEW_COND_EXC (cfg, INE_UN, "ArithmeticException");

					/* Do the assignment if the value is finite */
					MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, ins->dreg, ins->sreg1);

					restart = TRUE;
					break;
				}
				default:
					if (spec [MONO_INST_SRC1] == 'f' || spec [MONO_INST_SRC2] == 'f' || spec [MONO_INST_DEST] == 'f') {
						mono_print_ins (ins);
						g_assert_not_reached ();
					}
					break;
				}

				g_assert (cfg->cbb == first_bb);

				if (cfg->cbb->code || (cfg->cbb != first_bb)) {
					/* Replace the original instruction with the new code sequence */

					mono_replace_ins (cfg, bb, ins, &prev, first_bb, cfg->cbb);
					first_bb->code = first_bb->last_ins = NULL;
					first_bb->in_count = first_bb->out_count = 0;
					cfg->cbb = first_bb;
				}
				else
					prev = ins;
			}
		}

		if (cfg->verbose_level > 3) mono_print_bb (bb, "AFTER HANDLE-SOFT-FLOAT ");
	}

	mono_decompose_long_opts (cfg);
}

#endif

void
mono_local_emulate_ops (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	gboolean inlined_wrapper = FALSE;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;

		MONO_BB_FOR_EACH_INS (bb, ins) {
			int op_noimm = mono_op_imm_to_op (ins->opcode);
			MonoJitICallInfo *info;

			/*
			 * These opcodes don't have logical equivalence to the emulating native
			 * function. They are decomposed in specific fashion in mono_decompose_soft_float.
			 */
			if (MONO_HAS_CUSTOM_EMULATION (ins))
				continue;

			/*
			 * Emulation can't handle _IMM ops. If this is an imm opcode we need
			 * to check whether its non-imm counterpart is emulated and, if so,
			 * decompose it back to its non-imm counterpart.
			 */
			if (op_noimm != -1)
				info = mono_find_jit_opcode_emulation (op_noimm);
			else
				info = mono_find_jit_opcode_emulation (ins->opcode);

			if (info) {
				MonoInst **args;
				MonoInst *call;
				MonoBasicBlock *first_bb;

				/* Create dummy MonoInst's for the arguments */
				g_assert (!info->sig->hasthis);
				g_assert (info->sig->param_count <= MONO_MAX_SRC_REGS);

				if (op_noimm != -1)
					mono_decompose_op_imm (cfg, bb, ins);

				args = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * info->sig->param_count);
				if (info->sig->param_count > 0) {
					int sregs [MONO_MAX_SRC_REGS];
					int num_sregs, i;
					num_sregs = mono_inst_get_src_registers (ins, sregs);
					g_assert (num_sregs == info->sig->param_count);
					for (i = 0; i < num_sregs; ++i) {
						MONO_INST_NEW (cfg, args [i], OP_ARG);
						args [i]->dreg = sregs [i];
					}
				}

				/* We emit the call on a separate dummy basic block */
				cfg->cbb = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
				first_bb = cfg->cbb;

				call = mono_emit_jit_icall_by_info (cfg, bb->real_offset, info, args);
				call->dreg = ins->dreg;

				/* Replace ins with the emitted code and do the necessary bb linking */
				if (cfg->cbb->code || (cfg->cbb != first_bb)) {
					MonoInst *saved_prev = ins->prev;

					mono_replace_ins (cfg, bb, ins, &ins->prev, first_bb, cfg->cbb);
					first_bb->code = first_bb->last_ins = NULL;
					first_bb->in_count = first_bb->out_count = 0;
					cfg->cbb = first_bb;

					if (!saved_prev) {
						/* first instruction of basic block got replaced, so create
						 * dummy inst that points to start of basic block */
						MONO_INST_NEW (cfg, saved_prev, OP_NOP);
						saved_prev = bb->code;
					}

					/* ins is hanging, continue scanning the emitted code */
					ins = saved_prev;
				} else {
					g_error ("Failed to emit emulation code");
				}
				inlined_wrapper = TRUE;
			}
		}
	}

	/*
	 * Avoid rerunning these passes by emitting directly the exception checkpoint
	 * at IR level, instead of inlining the icall wrapper. FIXME
	 */
	if (inlined_wrapper) {
		if (!COMPILE_LLVM (cfg))
			mono_decompose_long_opts (cfg);
		if (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP))
			mono_local_cprop (cfg);
	}
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (decompose);

#endif /* !DISABLE_JIT */
