/**
 * \file
 * gsharedvt support code for arm64
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <mono/metadata/abi-details.h>

#include "mini.h"
#include "mini-arm64.h"
#include "mini-arm64-gsharedvt.h"
#include "mini-runtime.h"

/*
 * GSHAREDVT
 */
#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   See tramp-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	guint8 *code, *buf;
	int buf_len = 40;

	/*
	 * Return a trampoline which calls ADDR passing in ARG.
	 * Pass the argument in ip1, clobbering ip0.
	 */
	buf = code = mono_global_codeman_reserve (buf_len);

	MINI_BEGIN_CODEGEN ();

	code = mono_arm_emit_imm64 (code, ARMREG_IP1, (guint64)arg);
	code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)addr);

	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert ((code - buf) < buf_len);

	MINI_END_CODEGEN (buf, code - buf, -1, NULL);

	return buf;
}

MONO_PRAGMA_WARNING_PUSH()
MONO_PRAGMA_WARNING_DISABLE(4701) /* potentially uninitialized local variable 'dst_ptr' used */
MONO_PRAGMA_WARNING_DISABLE(4703) /* potentially uninitialized local pointer variable 'dst_ptr' used */

gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	int i;

	/* Set vtype ret arg */
	if (info->vret_slot != -1) {
		g_assert (info->vret_slot);
		callee [info->vret_arg_reg] = &callee [info->vret_slot];
	}

	for (i = 0; i < info->map_count; ++i) {
		int src = info->map [i * 2];
		int dst = info->map [(i * 2) + 1];
		int arg_marshal = (src >> 18) & 0xf;
		int arg_size = (src >> 22) & 0xf;

		if (G_UNLIKELY (arg_size)) {
			int src_offset = (src >> 26) & 0xf;
			int dst_offset = (dst >> 26) & 0xf;
			int src_slot, dst_slot;
			guint8 *src_ptr, *dst_ptr;

			/*
			 * Argument passed in part of a stack slot on ios.
			 * src_offset/dst_offset is the offset within the stack slot.
			 */
			switch (arg_marshal) {
			case GSHAREDVT_ARG_NONE:
				src_slot = src & 0xffff;
				dst_slot = dst & 0xffff;
				src_ptr = (guint8*)(caller + src_slot) + src_offset;
				dst_ptr = (guint8*)(callee + dst_slot) + dst_offset;
				break;
			case GSHAREDVT_ARG_BYREF_TO_BYVAL:
				src_slot = src & 0x3f;
				dst_slot = dst & 0xffff;
				src_ptr = (guint8*)caller [src_slot];
				dst_ptr = (guint8*)(callee + dst_slot) + dst_offset;
				break;
			case GSHAREDVT_ARG_BYVAL_TO_BYREF_HFAR4:
			case GSHAREDVT_ARG_BYREF_TO_BYVAL_HFAR4:
			case GSHAREDVT_ARG_BYREF_TO_BYREF:
				g_assert_not_reached ();
				break;
			case GSHAREDVT_ARG_BYVAL_TO_BYREF:
				src_slot = src & 0x3f;
				src_ptr = (guint8*)(caller + src_slot) + src_offset;
				callee [dst] = src_ptr;
				break;
			default:
				NOT_IMPLEMENTED;
				break;
			}

			if (arg_marshal == GSHAREDVT_ARG_BYVAL_TO_BYREF)
				continue;

			switch (arg_size) {
			case GSHAREDVT_ARG_SIZE_I1:
				*(gint8*)dst_ptr = *(gint8*)src_ptr;
				break;
			case GSHAREDVT_ARG_SIZE_U1:
				*(guint8*)dst_ptr = *(guint8*)src_ptr;
				break;
			case GSHAREDVT_ARG_SIZE_I2:
				*(gint16*)dst_ptr = *(gint16*)src_ptr;
				break;
			case GSHAREDVT_ARG_SIZE_U2:
				*(guint16*)dst_ptr = *(guint16*)src_ptr;
				break;
			case GSHAREDVT_ARG_SIZE_I4:
				*(gint32*)dst_ptr = *(gint32*)src_ptr;
				break;
			case GSHAREDVT_ARG_SIZE_U4:
				*(guint32*)dst_ptr = *(guint32*)src_ptr;
				break;
			default:
				g_assert_not_reached ();
			}
			continue;
		}

		switch (arg_marshal) {
		case GSHAREDVT_ARG_NONE:
			callee [dst] = caller [src];
			break;
		case GSHAREDVT_ARG_BYVAL_TO_BYREF:
			/* gsharedvt argument passed by addr in reg/stack slot */
			src = src & 0x3f;
			callee [dst] = caller + src;
			break;
		case GSHAREDVT_ARG_BYVAL_TO_BYREF_HFAR4: {
			int nslots = (src >> 6) & 0xff;
			int src_slot = src & 0x3f;
			int j;
			float *dst_arr = (float*)(caller + src_slot);

			/* The r4 hfa is in separate slots, need to compress them together in place */
			for (j = 0; j < nslots; ++j)
				dst_arr [j] = *(float*)(caller + src_slot + j);

			callee [dst] = caller + src_slot;
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL: {
			int nslots = (src >> 6) & 0xff;
			int src_slot = src & 0x3f;
			int j;
			gpointer *addr = (gpointer*)caller [src_slot];

			for (j = 0; j < nslots; ++j)
				callee [dst + j] = addr [j];
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_HFAR4: {
			int nslots = (src >> 6) & 0xff;
			int src_slot = src & 0x3f;
			int j;
			guint32 *addr = (guint32*)(caller [src_slot]);

			/* addr points to an array of floats, need to load them to registers */
			for (j = 0; j < nslots; ++j)
				callee [dst + j] = GUINT_TO_POINTER (addr [j]);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYREF: {
			int src_slot = src & 0x3f;

			callee [dst] = caller [src_slot];
			break;
		}
		default:
			g_assert_not_reached ();
			break;
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

MONO_PRAGMA_WARNING_POP()

#ifndef DISABLE_JIT

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int buf_len, cfa_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	guint8 *br_out, *br [64], *br_ret [64], *bcc_ret [64];
	int i, n_arg_regs, n_arg_fregs, offset, arg_reg, info_offset, rgctx_arg_reg_offset;
	int caller_reg_area_offset, callee_reg_area_offset;
	int br_ret_index, bcc_ret_index;

	buf_len = 2048;
	buf = code = mono_global_codeman_reserve (buf_len);

	/*
	 * We are being called by an gsharedvt arg trampoline, the info argument is in IP1.
	 */
	arg_reg = ARMREG_IP1;
	n_arg_regs = NUM_GSHAREDVT_ARG_GREGS;
	n_arg_fregs = NUM_GSHAREDVT_ARG_FREGS;

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* info argument */
	info_offset = offset;
	offset += 8;
	/* saved rgctx */
	rgctx_arg_reg_offset = offset;
	offset += 8;
	/* alignment */
	offset += 8;
	/* argument regs */
	caller_reg_area_offset = offset;
	offset += (n_arg_regs + n_arg_fregs) * 8;

	/* We need the argument regs to be saved at the top of the frame */
	g_assert (offset % MONO_ARCH_FRAME_ALIGNMENT == 0);

	cfa_offset = offset;

	MINI_BEGIN_CODEGEN ();

	/* Setup frame */
	arm_stpx_pre (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, -cfa_offset);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_FP, -cfa_offset + 0);
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, -cfa_offset + 8);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, ARMREG_FP);

	/* Save info argument */
	arm_strx (code, arg_reg, ARMREG_FP, info_offset);

	/* Save rgxctx */
	arm_strx (code, MONO_ARCH_RGCTX_REG, ARMREG_FP, rgctx_arg_reg_offset);

	/* Save argument regs below the stack arguments */
	for (i = 0; i < n_arg_regs; ++i)
		arm_strx (code, i, ARMREG_SP, caller_reg_area_offset + (i * 8));
	// FIXME: Only do this if fp regs are used
	for (i = 0; i < n_arg_fregs; ++i)
		arm_strfpx (code, i, ARMREG_SP, caller_reg_area_offset + ((n_arg_regs + i) * 8));

	/* Allocate callee area */
	arm_ldrw (code, ARMREG_IP0, arg_reg, MONO_STRUCT_OFFSET (GSharedVtCallInfo, stack_usage));
	arm_movspx (code, ARMREG_LR, ARMREG_SP);
	arm_subx (code, ARMREG_LR, ARMREG_LR, ARMREG_IP0);
	arm_movspx (code, ARMREG_SP, ARMREG_LR);
	/* Allocate callee register area just below the callee area so it can be accessed from start_gsharedvt_call using negative offsets */
	/* The + 8 is for alignment */
	callee_reg_area_offset = 8;
	arm_subx_imm (code, ARMREG_SP, ARMREG_SP, ((n_arg_regs + n_arg_fregs) * sizeof (target_mgreg_t)) + 8);

	/*
	 * The stack now looks like this:
	 * <caller frame>
	 * <saved r0-r8>
	 * <our frame>
	 * <saved fp, lr> <- fp
	 * <callee area> <- sp
	 */

	/* Call start_gsharedvt_call () */
	/* arg1 == info */
	arm_ldrx (code, ARMREG_R0, ARMREG_FP, info_offset);
	/* arg2 = caller stack area */
	arm_addx_imm (code, ARMREG_R1, ARMREG_FP, caller_reg_area_offset);
	/* arg3 == callee stack area */
	arm_addx_imm (code, ARMREG_R2, ARMREG_SP, callee_reg_area_offset);
	/* arg4 = mrgctx reg */
	arm_ldrx (code, ARMREG_R3, ARMREG_FP, rgctx_arg_reg_offset);

	if (aot)
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arm_start_gsharedvt_call));
	else
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)mono_arm_start_gsharedvt_call);
	code = mono_arm_emit_blrx (code, ARMREG_IP0);

	/* Make the real method call */
	/* R0 contains the addr to call */
	arm_movx (code, ARMREG_IP1, ARMREG_R0);
	/* Load rgxctx */
	arm_ldrx (code, MONO_ARCH_RGCTX_REG, ARMREG_FP, rgctx_arg_reg_offset);
	/* Load argument registers */
	// FIXME:
	for (i = 0; i < n_arg_regs; ++i)
		arm_ldrx (code, i, ARMREG_SP, callee_reg_area_offset + (i * 8));
	// FIXME: Only do this if needed
	for (i = 0; i < n_arg_fregs; ++i)
		arm_ldrfpx (code, i, ARMREG_SP, callee_reg_area_offset + ((n_arg_regs + i) * 8));
	/* Clear callee reg area */
	arm_addx_imm (code, ARMREG_SP, ARMREG_SP, ((n_arg_regs + n_arg_fregs) * sizeof (target_mgreg_t)) + 8);
	/* Make the call */
	code = mono_arm_emit_blrx (code, ARMREG_IP1);

	br_ret_index = 0;
	bcc_ret_index = 0;

	// FIXME: Use a switch
	/* Branch between IN/OUT cases */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, info_offset);
	arm_ldrw (code, ARMREG_IP1, ARMREG_IP1, MONO_STRUCT_OFFSET (GSharedVtCallInfo, gsharedvt_in));
	br_out = code;
	arm_cbzx (code, ARMREG_IP1, 0);

	/* IN CASE */

	/* IP1 == return marshalling type */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, info_offset);
	arm_ldrw (code, ARMREG_IP1, ARMREG_IP1, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal));

	/* Continue if no marshalling required */
	// FIXME: Use cmpx_imm
	code = mono_arm_emit_imm64 (code, ARMREG_IP0, GSHAREDVT_RET_NONE);
	arm_cmpx (code, ARMREG_IP0, ARMREG_IP1);
	bcc_ret [bcc_ret_index ++] = code;
	arm_bcc (code, ARMCOND_EQ, 0);

	/* Compute vret area address in LR */
	arm_ldrx (code, ARMREG_LR, ARMREG_FP, info_offset);
	arm_ldrw (code, ARMREG_LR, ARMREG_LR, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_slot));
	arm_subx_imm (code, ARMREG_LR, ARMREG_LR, n_arg_regs + n_arg_fregs);
	arm_lslx (code, ARMREG_LR, ARMREG_LR, 3);
	arm_movspx (code, ARMREG_IP0, ARMREG_SP);
	arm_addx (code, ARMREG_LR, ARMREG_IP0, ARMREG_LR);

	/* Branch to specific marshalling code */
	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, i);
		arm_cmpx (code, ARMREG_IP0, ARMREG_IP1);
		br [i] = code;
		arm_bcc (code, ARMCOND_EQ, 0);
	}

	arm_brk (code, 0);

	/*
	 * The address of the return value area is in LR, have to load it into
	 * registers.
	 */
	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		mono_arm_patch (br [i], code, MONO_R_ARM64_BCC);
		switch (i) {
		case GSHAREDVT_RET_NONE:
			break;
		case GSHAREDVT_RET_I8:
			arm_ldrx (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I1:
			arm_ldrsbx (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_U1:
			arm_ldrb (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I2:
			arm_ldrshx (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_U2:
			arm_ldrh (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I4:
			arm_ldrswx (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_U4:
			arm_ldrw (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_R8:
			arm_ldrfpx (code, ARMREG_D0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_R4:
			arm_ldrfpw (code, ARMREG_D0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_IREGS_1:
		case GSHAREDVT_RET_IREGS_2:
		case GSHAREDVT_RET_IREGS_3:
		case GSHAREDVT_RET_IREGS_4:
		case GSHAREDVT_RET_IREGS_5:
		case GSHAREDVT_RET_IREGS_6:
		case GSHAREDVT_RET_IREGS_7:
		case GSHAREDVT_RET_IREGS_8: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_IREGS_1 + 1; ++j)
				arm_ldrx (code, j, ARMREG_LR, j * 8);
			break;
		}
		case GSHAREDVT_RET_HFAR8_1:
		case GSHAREDVT_RET_HFAR8_2:
		case GSHAREDVT_RET_HFAR8_3:
		case GSHAREDVT_RET_HFAR8_4: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_HFAR8_1 + 1; ++j)
				arm_ldrfpx (code, j, ARMREG_LR, j * 8);
			break;
		}
		case GSHAREDVT_RET_HFAR4_1:
		case GSHAREDVT_RET_HFAR4_2:
		case GSHAREDVT_RET_HFAR4_3:
		case GSHAREDVT_RET_HFAR4_4: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_HFAR4_1 + 1; ++j)
				arm_ldrfpw (code, j, ARMREG_LR, j * 4);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
		br_ret [br_ret_index ++] = code;
		arm_b (code, 0);
	}

	/* OUT CASE */
	mono_arm_patch (br_out, code, MONO_R_ARM64_CBZ);

	/* Compute vret area address in LR */
	arm_ldrx (code, ARMREG_LR, ARMREG_FP, caller_reg_area_offset + (ARMREG_R8 * 8));

	/* IP1 == return marshalling type */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, info_offset);
	arm_ldrw (code, ARMREG_IP1, ARMREG_IP1, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal));

	/* Branch to specific marshalling code */
	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, i);
		arm_cmpx (code, ARMREG_IP0, ARMREG_IP1);
		br [i] = code;
		arm_bcc (code, ARMCOND_EQ, 0);
	}

	/*
	 * The return value is in registers, need to save to the return area passed by the caller in
	 * R8.
	 */
	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		mono_arm_patch (br [i], code, MONO_R_ARM64_BCC);
		switch (i) {
		case GSHAREDVT_RET_NONE:
			break;
		case GSHAREDVT_RET_I8:
			arm_strx (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I1:
		case GSHAREDVT_RET_U1:
			arm_strb (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I2:
		case GSHAREDVT_RET_U2:
			arm_strh (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_I4:
		case GSHAREDVT_RET_U4:
			arm_strw (code, ARMREG_R0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_R8:
			arm_strfpx (code, ARMREG_D0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_R4:
			arm_strfpw (code, ARMREG_D0, ARMREG_LR, 0);
			break;
		case GSHAREDVT_RET_IREGS_1:
		case GSHAREDVT_RET_IREGS_2:
		case GSHAREDVT_RET_IREGS_3:
		case GSHAREDVT_RET_IREGS_4:
		case GSHAREDVT_RET_IREGS_5:
		case GSHAREDVT_RET_IREGS_6:
		case GSHAREDVT_RET_IREGS_7:
		case GSHAREDVT_RET_IREGS_8: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_IREGS_1 + 1; ++j)
				arm_strx (code, j, ARMREG_LR, j * 8);
			break;
		}
		case GSHAREDVT_RET_HFAR8_1:
		case GSHAREDVT_RET_HFAR8_2:
		case GSHAREDVT_RET_HFAR8_3:
		case GSHAREDVT_RET_HFAR8_4: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_HFAR8_1 + 1; ++j)
				arm_strfpx (code, j, ARMREG_LR, j * 8);
			break;
		}
		case GSHAREDVT_RET_HFAR4_1:
		case GSHAREDVT_RET_HFAR4_2:
		case GSHAREDVT_RET_HFAR4_3:
		case GSHAREDVT_RET_HFAR4_4: {
			int j;

			for (j = 0; j < i - GSHAREDVT_RET_HFAR4_1 + 1; ++j)
				arm_strfpw (code, j, ARMREG_LR, j * 4);
			break;
		}
		default:
			arm_brk (code, i);
			break;
		}
		br_ret [br_ret_index ++] = code;
		arm_b (code, 0);
	}

	arm_brk (code, 0);

	for (i = 0; i < br_ret_index; ++i)
		mono_arm_patch (br_ret [i], code, MONO_R_ARM64_B);
	for (i = 0; i < bcc_ret_index; ++i)
		mono_arm_patch (bcc_ret [i], code, MONO_R_ARM64_BCC);

	/* Normal return */
	arm_movspx (code, ARMREG_SP, ARMREG_FP);
	arm_ldpx_post (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, offset);
	arm_retx (code, ARMREG_LR);

	g_assert ((code - buf) < buf_len);

	if (info)
		*info = mono_tramp_info_create ("gsharedvt_trampoline", buf, code - buf, ji, unwind_ops);

	MINI_END_CODEGEN (buf, code - buf, -1, NULL);

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

#endif /* MONO_ARCH_GSHAREDVT_SUPPORTED */
