/**
 * \file
 * libcorkscrew-based native unwinder
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *   Rodrigo Kumpera <kumpera@gmail.com>
 *   Andi McClure <andi.mcclure@xamarin.com>
 *   Johan Lorensson <johan.lorensson@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internals.h>
#include <mono/arch/amd64/amd64-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-amd64.h"
#include "mini-amd64-gsharedvt.h"
#include "debugger-agent.h"

#if defined (MONO_ARCH_GSHAREDVT_SUPPORTED)

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define SRC_REG_SHIFT 0
#define SRC_REG_MASK 0xFFFF

#define SRC_DESCRIPTOR_MARSHAL_SHIFT 16
#define SRC_DESCRIPTOR_MARSHAL_MASK 0x0FF

#define SLOT_COUNT_SHIFT 24
#define SLOT_COUNT_MASK 0xFF

gpointer
mono_amd64_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	int i;

#ifdef DEBUG_AMD64_GSHAREDVT
	printf ("mono_amd64_start_gsharedvt_call info %p caller %p callee %p ctx %p\n", info, caller, callee, mrgctx_reg);

	for (i = 0; i < PARAM_REGS; ++i)
		printf ("\treg [%d] -> %p\n", i, caller [i]);
#endif

	/* Set vtype ret arg */
	if (info->vret_slot != -1) {
		DEBUG_AMD64_GSHAREDVT_PRINT ("vret handling\n[%d] < &%d (%p)\n", info->vret_arg_reg, info->vret_slot, &callee [info->vret_slot]);
		g_assert (info->vret_slot);
		callee [info->vret_arg_reg] = &callee [info->vret_slot];
	}

	for (i = 0; i < info->map_count; ++i) {
		int src = info->map [i * 2];
		int dst = info->map [(i * 2) + 1];
		int arg_marshal = (src >> SRC_DESCRIPTOR_MARSHAL_SHIFT) & SRC_DESCRIPTOR_MARSHAL_MASK;

		int source_reg = src & SRC_REG_MASK;
		int dest_reg = dst & SRC_REG_MASK;

		DEBUG_AMD64_GSHAREDVT_PRINT ("source %x dest %x marshal %d: ", src, dst, arg_marshal);
		switch (arg_marshal) {
		case GSHAREDVT_ARG_NONE:
			callee [dest_reg] = caller [source_reg];
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- %d (%p) <- (%p)\n", dest_reg, source_reg, &callee [dest_reg], caller [source_reg]);
			break;
		case GSHAREDVT_ARG_BYVAL_TO_BYREF:
			/* gsharedvt argument passed by addr in reg/stack slot */
			callee [dest_reg] = &caller [source_reg];
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- &%d (%p) <- (%p)\n", dest_reg, source_reg, &callee [dest_reg], &caller [source_reg]);
			break;
		case GSHAREDVT_ARG_BYREF_TO_BYVAL: {
			int slot_count = (src >> SLOT_COUNT_SHIFT) & SLOT_COUNT_MASK;
			int j;
			gpointer *addr = caller [source_reg];

			for (j = 0; j < slot_count; ++j)
				callee [dest_reg + j] = addr [j];
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- [%d] (%d words) (%p) <- (%p)\n", dest_reg, source_reg, slot_count, &callee [dest_reg], &caller [source_reg]);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_U1: {
			guint8 *addr = caller [source_reg];

			callee [dest_reg] = (gpointer)(mgreg_t)*addr;
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- (u1) [%d] (%p) <- (%p)\n", dest_reg, source_reg, &callee [dest_reg], &caller [source_reg]);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_U2: {
			guint16 *addr = caller [source_reg];

			callee [dest_reg] = (gpointer)(mgreg_t)*addr;
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- (u2) [%d] (%p) <- (%p)\n", dest_reg, source_reg, &callee [dest_reg], &caller [source_reg]);
			break;
		}
		case GSHAREDVT_ARG_BYREF_TO_BYVAL_U4: {
			guint32 *addr = caller [source_reg];

			callee [dest_reg] = (gpointer)(mgreg_t)*addr;
			DEBUG_AMD64_GSHAREDVT_PRINT ("[%d] <- (u4) [%d] (%p) <- (%p)\n", dest_reg, source_reg, &callee [dest_reg], &caller [source_reg]);
			break;
		}

		default:
			g_error ("cant handle arg marshal %d\n", arg_marshal);
		}
	}

	//Can't handle for now
	if (info->vcall_offset != -1){
		MonoObject *this_obj = caller [0];

		DEBUG_AMD64_GSHAREDVT_PRINT ("target is a vcall at offset %d\n", info->vcall_offset / 8);
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
		DEBUG_AMD64_GSHAREDVT_PRINT ("target is %p\n", info->addr);
		return info->addr;
	}
}

#ifndef DISABLE_JIT

// Compiler support

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   See tramp-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;

	buf_len = 32;

	start = code = mono_domain_code_reserve (domain, buf_len);

	amd64_mov_reg_imm (code, AMD64_RAX, arg);
	amd64_jump_code (code, addr);
	g_assert ((code - start) < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), domain);

	return start;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int buf_len, cfa_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int n_arg_regs, n_arg_fregs, framesize, i;
	int info_offset, offset, rgctx_arg_reg_offset;
	int caller_reg_area_offset, callee_reg_area_offset, callee_stack_area_offset;
	guint8 *br_out, *br [64], *br_ret [64];
	int b_ret_index;
	int reg_area_size;

	buf_len = 2048;
	buf = code = mono_global_codeman_reserve (buf_len + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	/*
	 * We are being called by an gsharedvt arg trampoline, the info argument is in AMD64_RAX.
	 */
	n_arg_regs = PARAM_REGS;
	n_arg_fregs = FLOAT_PARAM_REGS;

	/* Compute stack frame size and offsets */
	offset = 0;
	/* info reg */
	info_offset = offset;
	offset += 8;

	/* rgctx reg */
	rgctx_arg_reg_offset = offset;
	offset += 8;

	/*callconv in regs */
	caller_reg_area_offset = offset;
	reg_area_size = ALIGN_TO ((n_arg_regs + n_arg_fregs) * 8, MONO_ARCH_FRAME_ALIGNMENT);
	offset += reg_area_size;

	framesize = offset;

	g_assert (framesize % MONO_ARCH_FRAME_ALIGNMENT == 0);
	g_assert (reg_area_size % MONO_ARCH_FRAME_ALIGNMENT == 0);

	/* unwind markers 1/3 */
	cfa_offset = sizeof (gpointer);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RIP, -cfa_offset);

	/* save the old frame pointer */
	amd64_push_reg (code, AMD64_RBP);

	/* unwind markers 2/3 */
	cfa_offset += sizeof (gpointer);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	/* set it as the new frame pointer */
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));

	/* unwind markers 3/3 */
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, buf, AMD64_RBP, 0);

	/* setup the frame */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);
	
	/* save stuff */

	/* save info */
	amd64_mov_membase_reg (code, AMD64_RSP, info_offset, AMD64_RAX, sizeof (mgreg_t));
	/* save rgctx */
	amd64_mov_membase_reg (code, AMD64_RSP, rgctx_arg_reg_offset, MONO_ARCH_RGCTX_REG, sizeof (mgreg_t));

	for (i = 0; i < n_arg_regs; ++i)
		amd64_mov_membase_reg (code, AMD64_RSP, caller_reg_area_offset + i * 8, param_regs [i], sizeof (mgreg_t));

	for (i = 0; i < n_arg_fregs; ++i)
		amd64_sse_movsd_membase_reg (code, AMD64_RSP, caller_reg_area_offset + (i + n_arg_regs) * 8, i);

	/* TODO Allocate stack area used to pass arguments to the method */


	/* Allocate callee register area just below the caller area so it can be accessed from start_gsharedvt_call using negative offsets */
	/* XXX figure out alignment */
	callee_reg_area_offset = reg_area_size - ((n_arg_regs + n_arg_fregs) * 8); /* Ensure alignment */
	callee_stack_area_offset = callee_reg_area_offset + reg_area_size;
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, reg_area_size);

	/* Allocate stack area used to pass arguments to the method */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, MONO_STRUCT_OFFSET (GSharedVtCallInfo, stack_usage), 4);
	amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, AMD64_R11);

	/* The stack now looks like this:

	<caller stack params area>
	<return address>
	<old frame pointer>
	<caller registers area>
	<rgctx>
	<gsharedvt info>
	<callee stack area>
	<callee reg area>
	 */

	/* Call start_gsharedvt_call () */
	/* arg1 == info */
	amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG1, AMD64_RAX, sizeof(mgreg_t));
	/* arg2 = caller stack area */
	amd64_lea_membase (code, MONO_AMD64_ARG_REG2, AMD64_RBP, -(framesize - caller_reg_area_offset)); 

	/* arg3 == callee stack area */
	amd64_lea_membase (code, MONO_AMD64_ARG_REG3, AMD64_RSP, callee_reg_area_offset);

	/* arg4 = mrgctx reg */
	amd64_mov_reg_reg (code, MONO_AMD64_ARG_REG4, MONO_ARCH_RGCTX_REG, sizeof(mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_amd64_start_gsharedvt_call");
		#ifdef TARGET_WIN32
			/* Since we are doing a call as part of setting up stackframe, the reserved shadow stack used by Windows platform is allocated up in
			the callee stack area but currently the callee reg area is in between. Windows calling convention dictates that room is made on stack where
			callee can save any parameters passed in registers. Since Windows x64 calling convention
			uses 4 registers for the first 4 parameters, stack needs to be adjusted before making the call.
			NOTE, Windows calling convention assumes that space for all registers have been reserved, regardless
			of the number of function parameters actually used.
			*/
			int shadow_reg_size = 0;

			shadow_reg_size = ALIGN_TO (PARAM_REGS * sizeof(gpointer), MONO_ARCH_FRAME_ALIGNMENT);
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, shadow_reg_size);
			amd64_call_reg (code, AMD64_R11);
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, shadow_reg_size);
		#else
			amd64_call_reg (code, AMD64_R11);
		#endif
	} else {
		amd64_call_code (code, mono_amd64_start_gsharedvt_call);
	}

	/* Method to call is now on RAX. Restore regs and jump */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, sizeof(mgreg_t));

	for (i = 0; i < n_arg_regs; ++i)
		amd64_mov_reg_membase (code, param_regs [i], AMD64_RSP, callee_reg_area_offset + i * 8, sizeof (mgreg_t));

	for (i = 0; i < n_arg_fregs; ++i)
		amd64_sse_movsd_reg_membase (code, i, AMD64_RSP, callee_reg_area_offset + (i + n_arg_regs) * 8);

	//load rgctx
	amd64_mov_reg_membase (code, MONO_ARCH_RGCTX_REG, AMD64_RBP, -(framesize - rgctx_arg_reg_offset), sizeof (mgreg_t));

	/* Clear callee reg area */
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, reg_area_size);

	/* Call the thing */
	amd64_call_reg (code, AMD64_R11);

	/* Marshal return value. Available registers: R10 and R11 */
	/* Load info struct */
	amd64_mov_reg_membase (code, AMD64_R10, AMD64_RBP, -(framesize - info_offset), sizeof (mgreg_t));

	/* Branch to the in/out handling code */
	amd64_alu_membase_imm_size (code, X86_CMP, AMD64_R10, MONO_STRUCT_OFFSET (GSharedVtCallInfo, gsharedvt_in), 1, 4);

	b_ret_index = 0;
	br_out = code;
	x86_branch32 (code, X86_CC_NE, 0, TRUE);

	/*
	 * IN CASE
	 */

	/* Load vret_slot */
	/* Use first input parameter register as scratch since it is volatile on all platforms */
	amd64_mov_reg_membase (code, MONO_AMD64_ARG_REG1, AMD64_R10, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_slot), 4);
	amd64_alu_reg_imm (code, X86_SUB, MONO_AMD64_ARG_REG1, n_arg_regs + n_arg_fregs);
	amd64_shift_reg_imm (code, X86_SHL, MONO_AMD64_ARG_REG1, 3);

	/* vret address is RBP - (framesize - caller_reg_area_offset) */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
	amd64_alu_reg_reg (code, X86_ADD, AMD64_R11, MONO_AMD64_ARG_REG1);

	/* Load ret marshal type */
	/* Load vret address in R11 */
	amd64_mov_reg_membase (code, AMD64_R10, AMD64_R10, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal), 4);

	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		amd64_alu_reg_imm (code, X86_CMP, AMD64_R10, i);
		br [i] = code;
		amd64_branch8 (code, X86_CC_EQ, 0, TRUE);
	}
	x86_breakpoint (code); /* unhandled case */

	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		mono_amd64_patch (br [i], code);
		switch (i) {
		case GSHAREDVT_RET_NONE:
			break;
		case GSHAREDVT_RET_I1:
			amd64_widen_membase (code, AMD64_RAX, AMD64_R11, 0, TRUE, FALSE);
			break;
		case GSHAREDVT_RET_U1:
			amd64_widen_membase (code, AMD64_RAX, AMD64_R11, 0, FALSE, FALSE);
			break;
		case GSHAREDVT_RET_I2:
			amd64_widen_membase (code, AMD64_RAX, AMD64_R11, 0, TRUE, TRUE);
			break;
		case GSHAREDVT_RET_U2:
			amd64_widen_membase (code, AMD64_RAX, AMD64_R11, 0, FALSE, TRUE);
			break;
		case GSHAREDVT_RET_I4: // CORRECT
		case GSHAREDVT_RET_U4: // THIS IS INCORRECT. WHY IS IT NOT FAILING?
			amd64_movsxd_reg_membase (code, AMD64_RAX, AMD64_R11, 0);
			break;
		case GSHAREDVT_RET_I8:
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 0, 8);
			break;
		case GSHAREDVT_RET_IREGS_1:
			amd64_mov_reg_membase (code, return_regs [i - GSHAREDVT_RET_IREGS_1], AMD64_R11, 0, 8);
			break;
		case GSHAREDVT_RET_R8:
			amd64_sse_movsd_reg_membase (code, AMD64_XMM0, AMD64_R11, 0);
			break;
		default:
			x86_breakpoint (code); /* can't handle specific case */
		}

		br_ret [b_ret_index ++] = code;
		x86_jump32 (code, 0);
	}

	/*
	 * OUT CASE
	 */
	mono_amd64_patch (br_out, code);

	/*
		Address to write return to is in the original value of the register specified by vret_arg_reg.
		This will be either RSI, RDI (System V) or RCX, RDX (Windows) depending on whether this is a static call.
		Its location:
		We alloc 'framesize' bytes below RBP to save regs, info and rgctx. RSP = RBP - framesize
		We store RDI (System V), RCX (Windows) at RSP + caller_reg_area_offset + slot_index_of (register) * 8.

		address: RBP - framesize + caller_reg_area_offset + 8*slot
	*/

	int caller_vret_offset = caller_reg_area_offset - framesize;

	/* Load vret address in R11 */
	/* Position to return to is passed as a hidden argument. Load 'vret_arg_slot' to find it */
	amd64_movsxd_reg_membase (code, AMD64_R11, AMD64_R10, MONO_STRUCT_OFFSET (GSharedVtCallInfo, vret_arg_reg));

	// In the GSHAREDVT_RET_NONE case, vret_arg_slot is -1. In this case, skip marshalling.
	amd64_alu_reg_imm (code, X86_CMP, AMD64_R11, 0);
	br_ret [b_ret_index ++] = code;
	amd64_branch32 (code, X86_CC_LT, 0, TRUE);

	/* Compute ret area address in the caller frame, *( ((gpointer *)RBP) [R11+2] ) */
	amd64_shift_reg_imm (code, X86_SHL, AMD64_R11, 3);
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, caller_vret_offset);
	amd64_alu_reg_reg (code, X86_ADD, AMD64_R11, AMD64_RBP);
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, sizeof (gpointer));

	/* Load ret marshal type in R10 */
	amd64_mov_reg_membase (code, AMD64_R10, AMD64_R10, MONO_STRUCT_OFFSET (GSharedVtCallInfo, ret_marshal), 4);

	// Switch table for ret_marshal value
	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		amd64_alu_reg_imm (code, X86_CMP, AMD64_R10, i);
		br [i] = code;
		amd64_branch8 (code, X86_CC_EQ, 0, TRUE);
	}
	x86_breakpoint (code); /* unhandled case */

	for (i = GSHAREDVT_RET_NONE; i < GSHAREDVT_RET_NUM; ++i) {
		mono_amd64_patch (br [i], code);
		switch (i) {
		case GSHAREDVT_RET_NONE:
			break;
		case GSHAREDVT_RET_IREGS_1:
			amd64_mov_membase_reg (code, AMD64_R11, 0, return_regs [i - GSHAREDVT_RET_IREGS_1], 8);
			break;
		case GSHAREDVT_RET_R8:
			amd64_sse_movsd_membase_reg (code, AMD64_R11, 0, AMD64_XMM0);
			break;
		default:
			x86_breakpoint (code); /* can't handle specific case */
		}

		br_ret [b_ret_index ++] = code;
		x86_jump32 (code, 0);
	}

	/* exit path */
	for (i = 0; i < b_ret_index; ++i)
		mono_amd64_patch (br_ret [i], code);

	/* Exit code path */
#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	amd64_ret (code);

	g_assert ((code - buf) < buf_len);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	if (info)
		*info = mono_tramp_info_create ("gsharedvt_trampoline", buf, code - buf, ji, unwind_ops);

	mono_arch_flush_icache (buf, code - buf);
	return buf;
}

#else

gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

#else

gpointer
mono_amd64_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg)
{
	g_assert_not_reached ();
	return NULL;
}

#endif
