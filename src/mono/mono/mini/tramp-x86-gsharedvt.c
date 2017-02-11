/**
 * \file
 * gsharedvt support code for x86
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include <mono/metadata/abi-details.h>

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

gpointer
mono_x86_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	int i;
	int *map = info->map;

	/* Set vtype ret arg */
	if (info->vret_arg_slot != -1) {
		callee [info->vret_arg_slot] = &callee [info->vret_slot];
	}
	/* Copy data from the caller argument area to the callee */
	for (i = 0; i < info->map_count; ++i) {
		int src = map [i * 2];
		int dst = map [i * 2 + 1];

		switch ((src >> 16) & 0x3) {
		case 0:
			callee [dst] = caller [src];
			break;
		case 1: {
			int j, nslots;
			gpointer *arg;

			/* gsharedvt->normal */
			nslots = src >> 18;
			arg = (gpointer*)caller [src & 0xffff];
			for (j = 0; j < nslots; ++j)
				callee [dst + j] = arg [j];
			break;
		}
		case 2:
			/* gsharedvt arg, have to take its address */
			callee [dst] = caller + (src & 0xffff);
			break;
#if 0
		int dst = map [i * 2 + 1];
		if (dst >= 0xffff) {
			/* gsharedvt arg, have to take its address */
			callee [dst - 0xffff] = caller + map [i * 2];
		} else {
			callee [dst] = caller [map [i * 2]];
		}
#endif
		}
	}

	if (info->vcall_offset != -1) {
		MonoObject *this_obj = caller [0];

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

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int buf_len, cfa_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	guint8 *br_out, *br [16];
	int info_offset, mrgctx_offset;

	buf_len = 320;
	buf = code = mono_global_codeman_reserve (buf_len);

	/*
	 * This trampoline is responsible for marshalling calls between normal code and gsharedvt code. The
	 * caller is a normal or gshared method which uses the signature of the inflated method to make the call, while
	 * the callee is a gsharedvt method which has a signature which uses valuetypes in place of type parameters, i.e.
	 * caller:
	 * foo<bool> (bool b)
	 * callee:
	 * T=<type used to represent vtype type arguments, currently TypedByRef>
	 * foo<T> (T b)
	 * The trampoline is responsible for marshalling the arguments and marshalling the result back. To simplify
	 * things, we create our own stack frame, and do most of the work in a C function, which receives a
	 * GSharedVtCallInfo structure as an argument. The structure should contain information to execute the C function to
	 * be as fast as possible. The argument is received in EAX from a gsharedvt trampoline. So the real
	 * call sequence looks like this:
	 * caller -> gsharedvt trampoline -> gsharevt in trampoline -> start_gsharedvt_call
	 * FIXME: Optimize this.
	 */

	cfa_offset = sizeof (gpointer);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, X86_ESP, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_NREG, -cfa_offset);
	x86_push_reg (code, X86_EBP);
	cfa_offset += sizeof (gpointer);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, X86_EBP, - cfa_offset);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, sizeof (gpointer));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, X86_EBP);
	/* Alloc stack frame/align stack */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
	info_offset = -4;
	mrgctx_offset = - 8;
	/* The info struct is put into EAX by the gsharedvt trampoline */
	/* Save info struct addr */
	x86_mov_membase_reg (code, X86_EBP, info_offset, X86_EAX, 4);
	/* Save rgctx */
	x86_mov_membase_reg (code, X86_EBP, mrgctx_offset, MONO_ARCH_RGCTX_REG, 4);

	/* Allocate stack area used to pass arguments to the method */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, stack_usage), sizeof (gpointer));
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, X86_EAX);

#if 0
	/* Stack alignment check */
	x86_mov_reg_reg (code, X86_ECX, X86_ESP, 4);
	x86_alu_reg_imm (code, X86_AND, X86_ECX, MONO_ARCH_FRAME_ALIGNMENT - 1);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, 0);
	x86_branch_disp (code, X86_CC_EQ, 3, FALSE);
	x86_breakpoint (code);
#endif

	/* ecx = caller argument area */
	x86_mov_reg_reg (code, X86_ECX, X86_EBP, 4);
	x86_alu_reg_imm (code, X86_ADD, X86_ECX, 8);
	/* eax = callee argument area */
	x86_mov_reg_reg (code, X86_EAX, X86_ESP, 4);

	/* Call start_gsharedvt_call */
	/* Arg 4 */
	x86_push_membase (code, X86_EBP, mrgctx_offset);
	/* Arg3 */
	x86_push_reg (code, X86_EAX);
	/* Arg2 */
	x86_push_reg (code, X86_ECX);
	/* Arg1 */
	x86_push_membase (code, X86_EBP, info_offset);
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_x86_start_gsharedvt_call");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, mono_x86_start_gsharedvt_call);
	}
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4 * 4);
	/* The address to call is in eax */
	/* The stack is now setup for the real call */
	/* Load info struct */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, info_offset, 4);
	/* Load rgctx */
	x86_mov_reg_membase (code, MONO_ARCH_RGCTX_REG, X86_EBP, mrgctx_offset, sizeof (gpointer));
	/* Make the call */
	x86_call_reg (code, X86_EAX);
	/* The return value is either in registers, or stored to an area beginning at sp [info->vret_slot] */
	/* EAX/EDX might contain the return value, only ECX is free */
	/* Load info struct */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, info_offset, 4);

	/* Branch to the in/out handling code */
	x86_alu_membase_imm (code, X86_CMP, X86_ECX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, gsharedvt_in), 1);	
	br_out = code;
	x86_branch32 (code, X86_CC_NE, 0, TRUE);

	/*
	 * IN CASE
	 */

	/* Load ret marshal type */
	x86_mov_reg_membase (code, X86_ECX, X86_ECX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal), 4);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_NONE);
	br [0] = code;
	x86_branch8 (code, X86_CC_NE, 0, TRUE);

	/* Normal return, no marshalling required */
	x86_leave (code);
	x86_ret (code);

	/* Return value marshalling */
	x86_patch (br [0], code);
	/* Load info struct */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, info_offset, 4);
	/* Load 'vret_slot' */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_slot), 4);
	/* Compute ret area address */
	x86_shift_reg_imm (code, X86_SHL, X86_EAX, 2);
	x86_alu_reg_reg (code, X86_ADD, X86_EAX, X86_ESP);
	/* The callee does a ret $4, so sp is off by 4 */
	x86_alu_reg_imm (code, X86_SUB, X86_EAX, sizeof (gpointer));

	/* Branch to specific marshalling code */
	// FIXME: Move the I4 case to the top */
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_DOUBLE_FPSTACK);
	br [1] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_FLOAT_FPSTACK);
	br [2] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_STACK_POP);
	br [3] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_I1);
	br [4] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_U1);
	br [5] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_I2);
	br [6] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_U2);
	br [7] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	/* IREGS case */
	/* Load both eax and edx for simplicity */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX, sizeof (gpointer), sizeof (gpointer));
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, 0, sizeof (gpointer));
	x86_leave (code);
	x86_ret (code);
	/* DOUBLE_FPSTACK case */
	x86_patch (br [1], code);
	x86_fld_membase (code, X86_EAX, 0, TRUE);
	x86_jump8 (code, 0);
	x86_leave (code);
	x86_ret (code);
	/* FLOAT_FPSTACK case */
	x86_patch (br [2], code);
	x86_fld_membase (code, X86_EAX, 0, FALSE);
	x86_leave (code);
	x86_ret (code);
	/* STACK_POP case */
	x86_patch (br [3], code);
	x86_leave (code);
	x86_ret_imm (code, 4);
	/* I1 case */
	x86_patch (br [4], code);
	x86_widen_membase (code, X86_EAX, X86_EAX, 0, TRUE, FALSE);
	x86_leave (code);
	x86_ret (code);
	/* U1 case */
	x86_patch (br [5], code);
	x86_widen_membase (code, X86_EAX, X86_EAX, 0, FALSE, FALSE);
	x86_leave (code);
	x86_ret (code);
	/* I2 case */
	x86_patch (br [6], code);
	x86_widen_membase (code, X86_EAX, X86_EAX, 0, TRUE, TRUE);
	x86_leave (code);
	x86_ret (code);
	/* U2 case */
	x86_patch (br [7], code);
	x86_widen_membase (code, X86_EAX, X86_EAX, 0, FALSE, TRUE);
	x86_leave (code);
	x86_ret (code);

	/*
	 * OUT CASE
	 */

	x86_patch (br_out, code);
	/* Load ret marshal type into ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_ECX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal), 4);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_NONE);
	br [0] = code;
	x86_branch8 (code, X86_CC_NE, 0, TRUE);

	/* Normal return, no marshalling required */
	x86_leave (code);
	x86_ret (code);

	/* Return value marshalling */
	x86_patch (br [0], code);

	/* EAX might contain the return value */
	// FIXME: Use moves
	x86_push_reg (code, X86_EAX);

	/* Load info struct */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, info_offset, 4);
	/* Load 'vret_arg_slot' */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_slot), 4);
	/* Compute ret area address in the caller frame in EAX */
	x86_shift_reg_imm (code, X86_SHL, X86_EAX, 2);
	x86_alu_reg_reg (code, X86_ADD, X86_EAX, X86_EBP);
	x86_alu_reg_imm (code, X86_ADD, X86_EAX, 8);
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, 0, sizeof (gpointer));

	/* Branch to specific marshalling code */
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_DOUBLE_FPSTACK);
	br [1] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_FLOAT_FPSTACK);
	br [2] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_STACK_POP);
	br [3] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	x86_alu_reg_imm (code, X86_CMP, X86_ECX, GSHAREDVT_RET_IREGS);
	br [4] = code;
	x86_branch8 (code, X86_CC_E, 0, TRUE);
	/* IREG case */
	x86_mov_reg_reg (code, X86_ECX, X86_EAX, sizeof (gpointer));
	x86_pop_reg (code, X86_EAX);
	x86_mov_membase_reg (code, X86_ECX, 0, X86_EAX, sizeof (gpointer));
	x86_leave (code);
	x86_ret_imm (code, 4);
	/* IREGS case */
	x86_patch (br [4], code);
	x86_mov_reg_reg (code, X86_ECX, X86_EAX, sizeof (gpointer));
	x86_pop_reg (code, X86_EAX);
	x86_mov_membase_reg (code, X86_ECX, sizeof (gpointer), X86_EDX, sizeof (gpointer));
	x86_mov_membase_reg (code, X86_ECX, 0, X86_EAX, sizeof (gpointer));
	x86_leave (code);
	x86_ret_imm (code, 4);
	/* DOUBLE_FPSTACK case */
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	x86_patch (br [1], code);
	x86_fst_membase (code, X86_EAX, 0, TRUE, TRUE);
	x86_jump8 (code, 0);
	x86_leave (code);
	x86_ret_imm (code, 4);
	/* FLOAT_FPSTACK case */
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	x86_patch (br [2], code);
	x86_fst_membase (code, X86_EAX, 0, FALSE, TRUE);
	x86_leave (code);
	x86_ret_imm (code, 4);
	/* STACK_POP case */
	x86_patch (br [3], code);
	x86_leave (code);
	x86_ret_imm (code, 4);

	g_assert ((code - buf) < buf_len);

	if (info)
		*info = mono_tramp_info_create ("gsharedvt_trampoline", buf, code - buf, ji, unwind_ops);

	mono_arch_flush_icache (buf, code - buf);
	return buf;
}

#endif /* MONO_ARCH_GSHAREDVT_SUPPORTED */
