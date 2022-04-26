/**
 * \file
 * gsharedvt support code for arm
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/arch/arm/arm-codegen.h>
#include <mono/arch/arm/arm-vfp-codegen.h>

#include "mini.h"
#include "mini-arm.h"
#include "mini-runtime.h"

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

static guint8*
emit_bx (guint8* code, int reg)
{
	if (mono_arm_thumb_supported ())
		ARM_BX (code, reg);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, reg);
	return code;
}

gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg,
							   double *caller_fregs, double *callee_fregs)
{
	int i;

	/*
	 * The caller/callee regs are mapped to slot 0..3, stack slot 0 is mapped to slot 4, etc.
	 */

	/* Set vtype ret arg */
	if (info->vret_slot != -1) {
		callee [info->vret_arg_reg] = &callee [info->vret_slot];
	}

	for (i = 0; i < info->map_count; ++i) {
		int src = info->map [i * 2];
		int dst = info->map [(i * 2) + 1];
		int arg_marshal = (src >> 24) & 0xff;

		switch (arg_marshal) {
		case GSHAREDVT_ARG_NONE:
			callee [dst] = caller [src];
			break;
		case GSHAREDVT_ARG_BYVAL_TO_BYREF:
			/* gsharedvt argument passed by addr in reg/stack slot */
			src = src & 0xffff;
			callee [dst] = caller + src;
			break;
		case GSHAREDVT_ARG_BYREF_TO_BYVAL: {
			/* gsharedvt argument passed by value */
			int nslots = (src >> 8) & 0xff;
			int src_slot = src & 0xff;
			int j;
			gpointer *addr = (gpointer*)caller [src_slot];

			for (j = 0; j < nslots; ++j)
				callee [dst + j] = addr [j];
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_I1: {
			int src_slot = src & 0xff;
			gpointer *addr = (gpointer*)caller [src_slot];

			callee [dst] = GINT_TO_POINTER ((int)*(gint8*)addr);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_I2: {
			int src_slot = src & 0xff;
			gpointer *addr = (gpointer*)caller [src_slot];

			callee [dst] = GINT_TO_POINTER ((int)*(gint16*)addr);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_U1: {
			int src_slot = src & 0xff;
			gpointer *addr = (gpointer*)caller [src_slot];

			callee [dst] = GUINT_TO_POINTER ((guint)*(guint8*)addr);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_U2: {
			int src_slot = src & 0xff;
			gpointer *addr = (gpointer*)caller [src_slot];

			callee [dst] = GUINT_TO_POINTER ((guint)*(guint16*)addr);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/* The slot based approach above is very complicated, use a nested switch instead for fp regs */
	// FIXME: Use this for the other cases as well
	if (info->have_fregs) {
		CallInfo *caller_cinfo = info->caller_cinfo;
		CallInfo *callee_cinfo = info->callee_cinfo;
		int aindex;

		for (aindex = 0; aindex < caller_cinfo->nargs; ++aindex) {
			ArgInfo *ainfo = &caller_cinfo->args [aindex];
			ArgInfo *ainfo2 = &callee_cinfo->args [aindex];

			switch (ainfo->storage) {
			case RegTypeFP: {
				switch (ainfo2->storage) {
				case RegTypeFP:
					callee_fregs [ainfo2->reg / 2] = caller_fregs [ainfo->reg / 2];
					break;
				case RegTypeGSharedVtInReg:
					callee [ainfo2->reg] = &caller_fregs [ainfo->reg / 2];
					break;
				case RegTypeGSharedVtOnStack: {
					int sslot = ainfo2->offset / 4;
					callee [sslot + 4] = &caller_fregs [ainfo->reg / 2];
					break;
				}
				default:
					g_assert_not_reached ();
					break;
				}
				break;
			}
			case RegTypeGSharedVtInReg: {
				switch (ainfo2->storage) {
				case RegTypeFP: {
					callee_fregs [ainfo2->reg / 2] = *(double*)caller [ainfo->reg];
					break;
				}
				default:
					break;
				}
				break;
			}
			case RegTypeGSharedVtOnStack: {
				switch (ainfo2->storage) {
				case RegTypeFP: {
					int sslot = ainfo->offset / 4;
					callee_fregs [ainfo2->reg / 2] = *(double*)caller [sslot + 4];
					break;
				}
				default:
					break;
				}
				break;
			}
			default:
				break;
			}
		}
	}

	if (info->vcall_offset != -1) {
		MonoObject *this_obj = (MonoObject*)caller [0];

		if (G_UNLIKELY (!this_obj))
			return NULL;
		if (info->vcall_offset == MONO_GSHAREDVT_DEL_INVOKE_VT_OFFSET)
			/* delegate invoke */
			return ((MonoDelegate*)this_obj)->invoke_impl;
		else
			return *(gpointer*)((char*)this_obj->vtable + info->vcall_offset);
	} else if (info->calli) {
		/* The address to call is passed in the mrgctx reg */
		return mrgctx_reg;
	} else {
		return info->addr;
	}
}

#ifndef DISABLE_JIT

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int buf_len, cfa_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	guint8 *br_out, *br [16], *br_ret [16];
	int i, offset, arg_reg, npushed, info_offset, mrgctx_offset;
	int caller_reg_area_offset, caller_freg_area_offset, callee_reg_area_offset, callee_freg_area_offset;
	int lr_offset, fp, br_ret_index, args_size;

	buf_len = 784;
	buf = code = mono_global_codeman_reserve (buf_len);

	arg_reg = ARMREG_R0;
	/* Registers pushed by the arg trampoline */
	npushed = 4;

	// ios abi compatible frame
	fp = ARMREG_R7;
	cfa_offset = npushed * TARGET_SIZEOF_VOID_P;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, cfa_offset);
	ARM_PUSH (code, (1 << fp) | (1 << ARMREG_LR));
	cfa_offset += 2 * TARGET_SIZEOF_VOID_P;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, fp, (- cfa_offset));
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, ((- cfa_offset) + 4));
	ARM_MOV_REG_REG (code, fp, ARMREG_SP);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, fp);
	/* Allocate stack frame */
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, (guint32)(32 + (16 * sizeof (double))));
MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	if (MONO_ARCH_FRAME_ALIGNMENT > 8)
		ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, (MONO_ARCH_FRAME_ALIGNMENT - 8));
MONO_RESTORE_WARNING
	offset = 4;
	info_offset = -offset;
	offset += 4;
	mrgctx_offset = -offset;
	offset += 4 * 4;
	callee_reg_area_offset = -offset;
	offset += 8 * 8;
	caller_freg_area_offset = -offset;
	offset += 8 * 8;
	callee_freg_area_offset = -offset;

	caller_reg_area_offset = cfa_offset - (npushed * TARGET_SIZEOF_VOID_P);
	lr_offset = 4;
	/* Save info struct which is in r0 */
	ARM_STR_IMM (code, arg_reg, fp, info_offset);
	/* Save rgctx reg */
	ARM_STR_IMM (code, MONO_ARCH_RGCTX_REG, fp, mrgctx_offset);
	/* Allocate callee area */
	ARM_LDR_IMM (code, ARMREG_IP, arg_reg, MONO_STRUCT_OFFSET (GSharedVtCallInfo, stack_usage));
	ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
	/* Allocate callee register area just below the callee area so the slots are correct */
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 4 * TARGET_SIZEOF_VOID_P);
	if (mono_arm_is_hard_float ()) {
		/* Save caller fregs */
		ARM_SUB_REG_IMM8 (code, ARMREG_IP, fp, -caller_freg_area_offset);
		for (i = 0; i < 8; ++i)
			ARM_FSTD (code, i * 2, ARMREG_IP, ((int)(i * sizeof (double))));
	}

	/*
	 * The stack now looks like this:
	 * <caller frame>
	 * <saved r0-r3, lr>
	 * <saved fp> <- fp
	 * <our frame>
	 * <callee area> <- sp
	 */
	g_assert (mono_arm_thumb_supported ());

	/* Call start_gsharedvt_call () */
	/* 6 arguments, needs 2 stack slot, need to clean it up after the call */
	args_size = 2 * TARGET_SIZEOF_VOID_P;
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, args_size);
	/* arg1 == info */
	ARM_LDR_IMM (code, ARMREG_R0, fp, info_offset);
	/* arg2 == caller stack area */
	ARM_ADD_REG_IMM8 (code, ARMREG_R1, fp, cfa_offset - 4 * TARGET_SIZEOF_VOID_P);
	/* arg3 == callee stack area */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, args_size);
	/* arg4 == mrgctx reg */
	ARM_LDR_IMM (code, ARMREG_R3, fp, mrgctx_offset);
	/* arg5 == caller freg area */
	ARM_SUB_REG_IMM8 (code, ARMREG_IP, fp, -caller_freg_area_offset);
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_SP, 0);
	/* arg6 == callee freg area */
	ARM_SUB_REG_IMM8 (code, ARMREG_IP, fp, -callee_freg_area_offset);
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_SP, 4);
	/* Make the call */
	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arm_start_gsharedvt_call));
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = (gpointer)mono_arm_start_gsharedvt_call;
		code += 4;
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_IP);
	/* Clean up stack */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, args_size);

	/* Make the real method call */
	/* R0 contains the addr to call */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_R0);
	/* Load argument registers */
	ARM_LDM (code, ARMREG_SP, (1 << ARMREG_R0) | (1 << ARMREG_R1) | (1 << ARMREG_R2) | (1 << ARMREG_R3));
	if (mono_arm_is_hard_float ()) {
		/* Load argument fregs */
		ARM_SUB_REG_IMM8 (code, ARMREG_LR, fp, -callee_freg_area_offset);
		for (i = 0; i < 8; ++i)
			ARM_FLDD (code, i * 2, ARMREG_LR, ((int)(i * sizeof (double))));
	}
	/* Pop callee register area */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 4 * TARGET_SIZEOF_VOID_P);
	/* Load rgctx */
	ARM_LDR_IMM (code, MONO_ARCH_RGCTX_REG, fp, mrgctx_offset);
	/* Make the call */
#if 0
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, addr));
#endif
	/* mono_arch_find_imt_method () depends on this */
	ARM_ADD_REG_IMM8 (code, ARMREG_LR, ARMREG_PC, 4);
	ARM_BX (code, ARMREG_IP);
	*((gpointer*)code) = NULL;
	code += 4;

	br_ret_index = 0;

	/* Branch between IN/OUT cases */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, gsharedvt_in));

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, 1);
	br_out = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* IN CASE */

	/* LR == return marshalling type */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal));

	/* Continue if no marshalling required */
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_NONE);
	br_ret [br_ret_index ++] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);

	/* Compute vret area address in LR */
	ARM_LDR_IMM (code, ARMREG_LR, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_LR, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_slot));
	/* The slot value is off by 4 */
	ARM_SUB_REG_IMM8 (code, ARMREG_LR, ARMREG_LR, 4);
	ARM_SHL_IMM (code, ARMREG_LR, ARMREG_LR, 2);
	ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ARMREG_SP);

	/* Branch to specific marshalling code */
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_IREG);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_IREGS);
	br [1] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_I1);
	br [2] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_U1);
	br [3] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_I2);
	br [4] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_U2);
	br [5] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_VFP_R4);
	br [6] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_VFP_R8);
	br [7] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);

	/* IN IREG case */
	arm_patch (br [0], code);
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* IN IREGS case */
	arm_patch (br [1], code);
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	ARM_LDR_IMM (code, ARMREG_R1, ARMREG_LR, 4);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* I1 case */
	arm_patch (br [2], code);
	ARM_LDRSB_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* U1 case */
	arm_patch (br [3], code);
	ARM_LDRB_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* I2 case */
	arm_patch (br [4], code);
	ARM_LDRSH_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* U2 case */
	arm_patch (br [5], code);
	ARM_LDRH_IMM (code, ARMREG_R0, ARMREG_LR, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* R4 case */
	arm_patch (br [6], code);
	ARM_FLDS (code, ARM_VFP_D0, ARMREG_LR, 0);
	code += 4;
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	/* R8 case */
	arm_patch (br [7], code);
	ARM_FLDD (code, ARM_VFP_D0, ARMREG_LR, 0);
	code += 4;
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);

	/* OUT CASE */
	arm_patch (br_out, code);

	/* Marshal return value */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal));

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_IREGS);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* OUT IREGS case */
	/* Load vtype ret addr from the caller arg regs */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));
	ARM_SHL_IMM (code, ARMREG_IP, ARMREG_IP, 2);
	ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, fp);
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_IP, caller_reg_area_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Save both registers for simplicity */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, 0);
	ARM_STR_IMM (code, ARMREG_R1, ARMREG_IP, 4);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	arm_patch (br [0], code);

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_IREG);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* OUT IREG case */
	/* Load vtype ret addr from the caller arg regs */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));
	ARM_SHL_IMM (code, ARMREG_IP, ARMREG_IP, 2);
	ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, fp);
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_IP, caller_reg_area_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Save the return value to the buffer pointed to by the vret addr */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	arm_patch (br [0], code);

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_U1);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* OUT U1 case */
	/* Load vtype ret addr from the caller arg regs */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));
	ARM_SHL_IMM (code, ARMREG_IP, ARMREG_IP, 2);
	ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, fp);
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_IP, caller_reg_area_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Save the return value to the buffer pointed to by the vret addr */
	ARM_STRB_IMM (code, ARMREG_R0, ARMREG_IP, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	arm_patch (br [0], code);

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_VFP_R4);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* OUT R4 case */
	/* Load vtype ret addr from the caller arg regs */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));
	ARM_SHL_IMM (code, ARMREG_IP, ARMREG_IP, 2);
	ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, fp);
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_IP, caller_reg_area_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Save the return value to the buffer pointed to by the vret addr */
	ARM_FSTS (code, ARM_VFP_D0, ARMREG_IP, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	arm_patch (br [0], code);

	ARM_CMP_REG_IMM8 (code, ARMREG_IP, GSHAREDVT_RET_VFP_R8);
	br [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	/* OUT R8 case */
	/* Load vtype ret addr from the caller arg regs */
	ARM_LDR_IMM (code, ARMREG_IP, fp, info_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));
	ARM_SHL_IMM (code, ARMREG_IP, ARMREG_IP, 2);
	ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, fp);
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_IP, caller_reg_area_offset);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Save the return value to the buffer pointed to by the vret addr */
	ARM_FSTD (code, ARM_VFP_D0, ARMREG_IP, 0);
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);
	arm_patch (br [0], code);

	/* OUT other cases */
	br_ret [br_ret_index ++] = code;
	ARM_B (code, 0);

	for (i = 0; i < br_ret_index; ++i)
		arm_patch (br_ret [i], code);

	/* Normal return */
	/* Restore registers + stack */
	ARM_MOV_REG_REG (code, ARMREG_SP, fp);
	ARM_LDM (code, fp, (1 << fp) | (1 << ARMREG_LR));
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, cfa_offset);
	/* Return */
	ARM_BX (code, ARMREG_LR);

	g_assert ((code - buf) < buf_len);

	if (info)
		*info = mono_tramp_info_create ("gsharedvt_trampoline", buf, code - buf, ji, unwind_ops);

	mono_arch_flush_icache (buf, code - buf);
	return buf;
}

#else

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif


#else


gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	*info = NULL;
	return NULL;
}


#endif
