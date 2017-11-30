/**
 * \file
 * JIT trampoline code for amd64
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *   Johan Lorensson (lateralusx.github@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
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
#include "mini-runtime.h"
#include "debugger-agent.h"

#ifndef DISABLE_INTERPRETER
#include "interp/interp.h"
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

#ifndef DISABLE_JIT
/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	GSList *unwind_ops;
	int this_reg, size = 20;

	MonoDomain *domain = mono_domain_get ();

	this_reg = mono_arch_get_this_arg_reg (NULL);

	start = code = (guint8 *)mono_domain_code_reserve (domain, size + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	amd64_alu_reg_imm (code, X86_ADD, this_reg, sizeof (MonoObject));
	/* FIXME: Optimize this */
	amd64_mov_reg_imm (code, AMD64_RAX, addr);
	amd64_jump_reg (code, AMD64_RAX);
	g_assert ((code - start) < size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), domain);

	return start;
}

/*
 * mono_arch_get_static_rgctx_trampoline:
 *
 *   Create a trampoline which sets RGCTX_REG to ARG, then jumps to ADDR.
 */
gpointer
mono_arch_get_static_rgctx_trampoline (gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	GSList *unwind_ops;
	int buf_len;

	MonoDomain *domain = mono_domain_get ();

#ifdef MONO_ARCH_NOMAP32BIT
	buf_len = 32;
#else
	/* AOTed code could still have a non-32 bit address */
	if ((((guint64)addr) >> 32) == 0)
		buf_len = 16;
	else
		buf_len = 30;
#endif

	start = code = (guint8 *)mono_domain_code_reserve (domain, buf_len + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	amd64_mov_reg_imm (code, MONO_ARCH_RGCTX_REG, arg);
	amd64_jump_code (code, addr);
	g_assert ((code - start) < buf_len);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), domain);

	return start;
}
#endif /* !DISABLE_JIT */

#ifdef _WIN64
// Workaround lack of Valgrind support for 64-bit Windows
#define VALGRIND_DISCARD_TRANSLATIONS(...)
#endif

/*
 * mono_arch_patch_callsite:
 *
 *   Patch the callsite whose address is given by ORIG_CODE so it calls ADDR. ORIG_CODE
 * points to the pc right after the call.
 */
void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	guint8 *code;
	guint8 buf [16];
	gboolean can_write = mono_breakpoint_clean_code (method_start, orig_code, 14, buf, sizeof (buf));

	code = buf + 14;

	/* mov 64-bit imm into r11 (followed by call reg?)  or direct call*/
	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8) {
			if (can_write) {
				g_assert ((guint64)(orig_code - 11) % 8 == 0);
				mono_atomic_xchg_ptr ((gpointer*)(orig_code - 11), addr);
				VALGRIND_DISCARD_TRANSLATIONS (orig_code - 11, sizeof (gpointer));
			}
		} else {
			gboolean disp_32bit = ((((gint64)addr - (gint64)orig_code)) < (1 << 30)) && ((((gint64)addr - (gint64)orig_code)) > -(1 << 30));

			if ((((guint64)(addr)) >> 32) != 0 && !disp_32bit) {
				/* 
				 * This might happen with LLVM or when calling AOTed code. Create a thunk.
				 */
				guint8 *thunk_start, *thunk_code;

				thunk_start = thunk_code = (guint8 *)mono_domain_code_reserve (mono_domain_get (), 32);
				amd64_jump_membase (thunk_code, AMD64_RIP, 0);
				*(guint64*)thunk_code = (guint64)addr;
				addr = thunk_start;
				g_assert ((((guint64)(addr)) >> 32) == 0);
				mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
				MONO_PROFILER_RAISE (jit_code_buffer, (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
			}
			if (can_write) {
				mono_atomic_xchg_i32 ((gint32*)(orig_code - 4), ((gint64)addr - (gint64)orig_code));
				VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, 4);
			}
		}
	}
	else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		gpointer *got_entry = (gpointer*)((guint8*)orig_code + (*(guint32*)(orig_code - 4)));
		if (can_write) {
			mono_atomic_xchg_ptr (got_entry, addr);
			VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, sizeof (gpointer));
		}
	}
}

#ifndef DISABLE_JIT
guint8*
mono_arch_create_llvm_native_thunk (MonoDomain *domain, guint8 *addr)
{
	/*
	 * The caller is LLVM code and the call displacement might exceed 32 bits. We can't determine the caller address, so
	 * we add a thunk every time.
	 * Since the caller is also allocated using the domain code manager, hopefully the displacement will fit into 32 bits.
	 * FIXME: Avoid this if possible if !MONO_ARCH_NOMAP32BIT and ADDR is 32 bits.
	 */
	guint8 *thunk_start, *thunk_code;

	thunk_start = thunk_code = (guint8 *)mono_domain_code_reserve (mono_domain_get (), 32);
	amd64_jump_membase (thunk_code, AMD64_RIP, 0);
	*(guint64*)thunk_code = (guint64)addr;
	addr = thunk_start;
	mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
	MONO_PROFILER_RAISE (jit_code_buffer, (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	return addr;
}
#endif /* !DISABLE_JIT */

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	gint32 disp;
	gpointer *plt_jump_table_entry;

	/* A PLT entry: jmp *<DISP>(%rip) */
	g_assert (code [0] == 0xff);
	g_assert (code [1] == 0x25);

	disp = *(gint32*)(code + 2);

	plt_jump_table_entry = (gpointer*)(code + 6 + disp);

	mono_atomic_xchg_ptr (plt_jump_table_entry, addr);
}

#ifndef DISABLE_JIT
static void
stack_unaligned (MonoTrampolineType tramp_type)
{
	printf ("%d\n", tramp_type);
	g_assert_not_reached ();
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	char *tramp_name;
	guint8 *buf, *code, *tramp, *br [2], *r11_save_code, *after_r11_save_code, *br_ex_check;
	int i, lmf_offset, offset, res_offset, arg_offset, rax_offset, ex_offset, tramp_offset, ctx_offset, saved_regs_offset;
	int r11_save_offset, saved_fpregs_offset, rbp_offset, framesize, orig_rsp_to_rbp_offset, cfa_offset;
	gboolean has_caller;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	const guint kMaxCodeSize = 630;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	/* Compute stack frame size and offsets */
	offset = 0;
	rbp_offset = -offset;

	offset += sizeof(mgreg_t);
	rax_offset = -offset;

	offset += sizeof(mgreg_t);
	ex_offset = -offset;

	offset += sizeof(mgreg_t);
	r11_save_offset = -offset;

	offset += sizeof(mgreg_t);
	tramp_offset = -offset;

	offset += sizeof(gpointer);
	arg_offset = -offset;

	offset += sizeof(mgreg_t);
	res_offset = -offset;

	offset += sizeof (MonoContext);
	ctx_offset = -offset;
	saved_regs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);
	saved_fpregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, fregs);

	offset += sizeof (MonoLMFTramp);
	lmf_offset = -offset;

#ifdef TARGET_WIN32
	/* Reserve space where the callee can save the argument registers */
	offset += 4 * sizeof (mgreg_t);
#endif

	framesize = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	// CFA = sp + 16 (the trampoline address is on the stack)
	cfa_offset = 16;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, 16);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RIP, -8);

	orig_rsp_to_rbp_offset = 0;
	r11_save_code = code;
	/* Reserve space for the mov_membase_reg to save R11 */
	code += 5;
	after_r11_save_code = code;

	/* Pop the return address off the stack */
	amd64_pop_reg (code, AMD64_R11);
	orig_rsp_to_rbp_offset += sizeof(mgreg_t);

	cfa_offset -= sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);

	/* 
	 * Allocate a new stack frame
	 */
	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	orig_rsp_to_rbp_offset -= sizeof(mgreg_t);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, buf, AMD64_RBP, 0);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	/* Compute the trampoline address from the return address */
	if (aot) {
		/* 7 = length of call *<offset>(rip) */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 7);
	} else {
		/* 5 = length of amd64_call_membase () */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 5);
	}
	amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, sizeof(gpointer));

	/* Save all registers */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i == AMD64_RBP) {
			/* RAX is already saved */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, rbp_offset, sizeof(mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_RAX, sizeof(mgreg_t));
		} else if (i == AMD64_RIP) {
			if (has_caller)
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, sizeof(gpointer));
			else
				amd64_mov_reg_imm (code, AMD64_R11, 0);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_R11, sizeof(mgreg_t));
		} else if (i == AMD64_RSP) {
			amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
			amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), AMD64_R11, sizeof(mgreg_t));
		} else if (i != AMD64_R11) {
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), i, sizeof(mgreg_t));
		} else {
			/* We have to save R11 right at the start of
			   the trampoline code because it's used as a
			   scratch register */
			/* This happens before the frame is set up, so it goes into the redzone */
			amd64_mov_membase_reg (r11_save_code, AMD64_RSP, r11_save_offset + orig_rsp_to_rbp_offset, i, sizeof(mgreg_t));
			g_assert (r11_save_code == after_r11_save_code);

			/* Copy from the save slot into the register array slot */
			amd64_mov_reg_membase (code, i, AMD64_RSP, r11_save_offset + orig_rsp_to_rbp_offset, sizeof(mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), i, sizeof(mgreg_t));
		}
		/* cfa = rbp + cfa_offset */
		mono_add_unwind_op_offset (unwind_ops, code, buf, i, - cfa_offset + saved_regs_offset + (i * sizeof (mgreg_t)));
	}
	for (i = 0; i < AMD64_XMM_NREG; ++i)
		if (AMD64_IS_ARGUMENT_XREG (i))
			amd64_movdqu_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * sizeof(MonoContextSimdReg)), i);

	/* Check that the stack is aligned */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof (mgreg_t));
	amd64_alu_reg_imm (code, X86_AND, AMD64_R11, 15);
	amd64_alu_reg_imm (code, X86_CMP, AMD64_R11, 0);
	br [0] = code;
	amd64_branch_disp (code, X86_CC_Z, 0, FALSE);
	if (aot) {
		amd64_mov_reg_imm (code, AMD64_R11, 0);
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, 8);
	} else {
		amd64_mov_reg_imm (code, MONO_AMD64_ARG_REG1, tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, stack_unaligned);
		amd64_call_reg (code, AMD64_R11);
	}
	mono_amd64_patch (br [0], code);
	//amd64_breakpoint (code);

	/* Obtain the trampoline argument which is encoded in the instruction stream */
	if (aot) {
		/* Load the GOT offset */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof(gpointer));
		/*
		 * r11 points to a call *<offset>(%rip) instruction, load the
		 * pc-relative offset from the instruction itself.
		 */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 3, 4);
		/* 7 is the length of the call, 8 is the offset to the next got slot */
		amd64_alu_reg_imm_size (code, X86_ADD, AMD64_RAX, 7 + sizeof (gpointer), sizeof(gpointer));
		/* Compute the address of the GOT slot */
		amd64_alu_reg_reg_size (code, X86_ADD, AMD64_R11, AMD64_RAX, sizeof(gpointer));
		/* Load the value */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, sizeof(gpointer));
	} else {
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof(gpointer));
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, 5, 1);
		amd64_widen_reg (code, AMD64_RAX, AMD64_RAX, TRUE, FALSE);
		amd64_alu_reg_imm_size (code, X86_CMP, AMD64_RAX, 4, 1);
		br [0] = code;
		x86_branch8 (code, X86_CC_NE, 6, FALSE);
		/* 32 bit immediate */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 6, 4);
		br [1] = code;
		x86_jump8 (code, 10);
		/* 64 bit immediate */
		mono_amd64_patch (br [0], code);
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 6, 8);
		mono_amd64_patch (br [1], code);
	}
	amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, AMD64_R11, sizeof(gpointer));

	/* Save LMF begin */

	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof(mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, sizeof(mgreg_t));
	/* Save pointer to context */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, ctx_offset);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, ctx), AMD64_R11, sizeof(mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_get_lmf_addr);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Save lmf_addr */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), AMD64_RAX, sizeof(gpointer));
	/* Save previous_lmf */
	/* Set the third lowest bit to signal that this is a MonoLMFTramp structure */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_ADD, AMD64_R11, 0x5, sizeof(gpointer));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, sizeof(gpointer));
	/* Set new lmf */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, lmf_offset);
	amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, sizeof(gpointer));

	/* Save LMF end */

	/* Arg1 is the pointer to the saved registers */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RBP, saved_regs_offset);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_ARG_REG2, AMD64_RBP, 8, sizeof(gpointer));
	else
		amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_RBP, arg_offset, sizeof(gpointer));

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_ARG_REG4, AMD64_RBP, tramp_offset, sizeof(gpointer));

	if (aot) {
		char *icall_name = g_strdup_printf ("trampoline_func_%d", tramp_type);
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, tramp);
	}
	amd64_call_reg (code, AMD64_R11);
	amd64_mov_membase_reg (code, AMD64_RBP, res_offset, AMD64_RAX, sizeof(mgreg_t));

	/* Restore LMF */
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof(gpointer));
	amd64_alu_reg_imm_size (code, X86_SUB, AMD64_RCX, 0x5, sizeof(gpointer));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), sizeof(gpointer));
	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, sizeof(gpointer));

	/* 
	 * Save rax to the stack, after the leave instruction, this will become part of
	 * the red zone.
	 */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, res_offset, sizeof(mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, rax_offset, AMD64_RAX, sizeof(mgreg_t));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint_noraise");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, (guint8*)mono_thread_force_interruption_checkpoint_noraise);
	}
	amd64_call_reg (code, AMD64_R11);

	amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
	br_ex_check = code;
	amd64_branch8 (code, X86_CC_Z, -1, 1);

	/*
	 * Exception case:
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	/* We are in the parent frame, the exception is in rax */
	/*
	 * EH is initialized after trampolines, so get the address of the variable
	 * which contains throw_exception, and load it from there.
	 */
	if (aot) {
		/* Not really a jit icall */
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "throw_exception_addr");
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, (guint8*)mono_get_throw_exception_addr ());
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, sizeof(gpointer));
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RAX, sizeof(mgreg_t));
	/*
	 * We still have the original return value on the top of the stack, so the
	 * throw trampoline will use that as the throw site.
	 */
	amd64_jump_reg (code, AMD64_R11);

	/* Normal case */
	mono_amd64_patch (br_ex_check, code);

	/* Restore argument registers, r10 (imt method/rgxtx)
	   and rax (needed for direct calls to C vararg functions). */
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_ARGUMENT_REG (i) || i == AMD64_R10 || i == AMD64_RAX)
			amd64_mov_reg_membase (code, i, AMD64_RBP, saved_regs_offset + (i * sizeof(mgreg_t)), sizeof(mgreg_t));
	for (i = 0; i < AMD64_XMM_NREG; ++i)
		if (AMD64_IS_ARGUMENT_XREG (i))
			amd64_movdqu_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * sizeof(MonoContextSimdReg)));

	/* Restore stack */
#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	cfa_offset -= sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, cfa_offset);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Load result */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, rax_offset - sizeof(mgreg_t), sizeof(mgreg_t));
		amd64_ret (code);
	} else {
		/* call the compiled method using the saved rax */
		amd64_jump_membase (code, AMD64_RSP, rax_offset - sizeof(mgreg_t));
	}

	g_assert ((code - buf) <= kMaxCodeSize);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);
	g_free (tramp_name);

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	int size;
	gboolean far_addr = FALSE;

	tramp = mono_get_trampoline_code (tramp_type);

	if ((((guint64)arg1) >> 32) == 0)
		size = 5 + 1 + 4;
	else
		size = 5 + 1 + 8;

	code = buf = (guint8 *)mono_domain_code_reserve_align (domain, size, 1);

	if (((gint64)tramp - (gint64)code) >> 31 != 0 && ((gint64)tramp - (gint64)code) >> 31 != -1) {
#ifndef MONO_ARCH_NOMAP32BIT
		g_assert_not_reached ();
#endif
		far_addr = TRUE;
		size += 16;
		code = buf = (guint8 *)mono_domain_code_reserve_align (domain, size, 1);
	}

	if (far_addr) {
		amd64_mov_reg_imm (code, AMD64_R11, tramp);
		amd64_call_reg (code, AMD64_R11);
	} else {
		amd64_call_code (code, tramp);
	}
	/* The trampoline code will obtain the argument from the instruction stream */
	if ((((guint64)arg1) >> 32) == 0) {
		*code = 0x4;
		*(guint32*)(code + 1) = (gint64)arg1;
		code += 5;
	} else {
		*code = 0x8;
		*(guint64*)(code + 1) = (gint64)arg1;
		code += 9;
	}

	g_assert ((code - buf) <= size);

	if (code_len)
		*code_len = size;

	mono_arch_flush_icache (buf, size);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type)));

	return buf;
}	

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	int tramp_size;
	int depth, index;
	int i;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 64 + 8 * depth;

	code = buf = (guint8 *)mono_global_codeman_reserve (tramp_size + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = (guint8 **)g_malloc (sizeof (guint8*) * (depth + 2));

	if (mrgctx) {
		/* get mrgctx ptr */
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
	} else {
		/* load rgctx ptr from vtable */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_ARG_REG1, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context), sizeof(gpointer));
		/* is the rgctx ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [0] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT, sizeof(gpointer));
		else
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, 0, sizeof(gpointer));
		/* is the ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, sizeof (gpointer) * (index + 1), sizeof(gpointer));
	/* is the slot null? */
	amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [depth + 1] = code;
	amd64_branch8 (code, X86_CC_Z, -1, 1);
	/* otherwise return */
	amd64_ret (code);

	for (i = mrgctx ? 1 : 0; i <= depth + 1; ++i)
		mono_amd64_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	if (MONO_ARCH_VTABLE_REG != AMD64_ARG_REG1) {
		/* move the rgctx pointer to the VTABLE register */
		amd64_mov_reg_reg (code, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, sizeof(gpointer));
	}

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, g_strdup_printf ("specific_trampoline_lazy_fetch_%u", slot));
		amd64_jump_reg (code, AMD64_R11);
	} else {
		tramp = (guint8 *)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return buf;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int tramp_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops;

	g_assert (aot);
	tramp_size = 64;

	code = buf = (guint8 *)mono_global_codeman_reserve (tramp_size + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	// FIXME: Currently, we always go to the slow path.
	/* This receives a <slot, trampoline> in the rgctx arg reg. */
	/* Load trampoline addr */
	amd64_mov_reg_membase (code, AMD64_R11, MONO_ARCH_RGCTX_REG, 8, 8);
	/* move the rgctx pointer to the VTABLE register */
	amd64_mov_reg_reg (code, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, sizeof(gpointer));
	/* Jump to the trampoline */
	amd64_jump_reg (code, AMD64_R11);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

	return buf;
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = (guint8 *)ji->code_start;

	amd64_mov_reg_imm (code, AMD64_ARG_REG1, func_arg);
	amd64_mov_reg_imm (code, AMD64_R11, func);

	x86_push_imm (code, (guint64)func_arg);
	amd64_call_reg (code, AMD64_R11);
}
#endif /* !DISABLE_JIT */

/*
 * mono_arch_get_call_target:
 *
 *   Return the address called by the code before CODE if exists.
 */
guint8*
mono_arch_get_call_target (guint8 *code)
{
	if (code [-5] == 0xe8) {
		gint32 disp = *(gint32*)(code - 4);
		guint8 *target = code + disp;

		return target;
	} else {
		return NULL;
	}
}

/*
 * mono_arch_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry PLT_ENTRY.
 */
guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, mgreg_t *regs, guint8 *code)
{
	return *(guint32*)(plt_entry + 6);
}

#ifndef DISABLE_JIT
/*
 * mono_arch_create_sdb_trampoline:
 *
 *   Return a trampoline which captures the current context, passes it to
 * debugger_agent_single_step_from_context ()/debugger_agent_breakpoint_from_context (),
 * then restores the (potentially changed) context.
 */
guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	int tramp_size = 512;
	int i, framesize, ctx_offset, cfa_offset, gregs_offset;
	guint8 *code, *buf;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	code = buf = (guint8 *)mono_global_codeman_reserve (tramp_size + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	framesize = 0;
#ifdef TARGET_WIN32
	/* Reserve space where the callee can save the argument registers */
	framesize += 4 * sizeof (mgreg_t);
#endif

	ctx_offset = framesize;
	framesize += sizeof (MonoContext);

	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	// CFA = sp + 8
	cfa_offset = 8;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, 8);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RIP, -cfa_offset);

	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof(mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, buf, AMD64_RBP, 0);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	gregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);

	/* Initialize a MonoContext structure on the stack */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (i * sizeof (mgreg_t)), i, sizeof (mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 0, sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, 2 * sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RSP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, sizeof (mgreg_t), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (mgreg_t)), AMD64_R11, sizeof (mgreg_t));

	/* Call the single step/breakpoint function in sdb */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RSP, ctx_offset);

	if (aot) {
		if (single_step)
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "debugger_agent_single_step_from_context");
		else
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, "debugger_agent_breakpoint_from_context");
	} else {
		if (single_step)
			amd64_mov_reg_imm (code, AMD64_R11, debugger_agent_single_step_from_context);
		else
			amd64_mov_reg_imm (code, AMD64_R11, debugger_agent_breakpoint_from_context);
	}	
	amd64_call_reg (code, AMD64_R11);

	/* Restore registers from ctx */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_reg_membase (code, i, AMD64_RSP, gregs_offset + (i * sizeof (mgreg_t)), sizeof (mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (mgreg_t)), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, 0, AMD64_R11, sizeof (mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (mgreg_t)), sizeof (mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, sizeof (mgreg_t), AMD64_R11, sizeof (mgreg_t));

#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	cfa_offset -= sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, cfa_offset);
	amd64_ret (code);

	mono_arch_flush_icache (code, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	g_assert (code - buf <= tramp_size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

/*
 * mono_arch_get_enter_icall_trampoline:
 *
 *   A trampoline that handles the transition from interpreter into native
 *   world. It requiers to set up a descriptor (InterpMethodArguments), so the
 *   trampoline can translate the arguments into the native calling convention.
 *
 *   See also `build_args_from_sig ()` in interp.c.
 */
gpointer
mono_arch_get_enter_icall_trampoline (MonoTrampInfo **info)
{
#ifndef DISABLE_INTERPRETER
	guint8 *start = NULL, *code, *label_gexits [INTERP_ICALL_TRAMP_IARGS], *label_fexits [INTERP_ICALL_TRAMP_FARGS], *label_leave_tramp [3], *label_is_float_ret;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	static int farg_regs[] = {AMD64_XMM0, AMD64_XMM1, AMD64_XMM2, AMD64_XMM3};
	int buf_len, i, framesize = 0, off_rbp, off_methodargs, off_targetaddr;

	g_assert ((sizeof (farg_regs) / sizeof (farg_regs [0])) >= INTERP_ICALL_TRAMP_FARGS);
	buf_len = 512 + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0);
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len);

	off_rbp = -framesize;

	framesize += sizeof (mgreg_t);
	off_methodargs = -framesize;

	framesize += sizeof (mgreg_t);
	off_targetaddr = -framesize;

	framesize += (INTERP_ICALL_TRAMP_IARGS - PARAM_REGS) * sizeof (mgreg_t);

	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (mgreg_t));
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT));

	/* save InterpMethodArguments* onto stack */
	amd64_mov_membase_reg (code, AMD64_RBP, off_methodargs, AMD64_ARG_REG2, sizeof (mgreg_t));

	/* save target address on stack */
	amd64_mov_membase_reg (code, AMD64_RBP, off_targetaddr, AMD64_ARG_REG1, sizeof (mgreg_t));

	/* load pointer to InterpMethodArguments* into R11 */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_ARG_REG2, 8);
	
	/* move flen into RAX */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, flen), sizeof (mgreg_t));
	/* load pointer to fargs into R11 */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, fargs), sizeof (mgreg_t));

	for (i = 0; i < INTERP_ICALL_TRAMP_FARGS; ++i) {
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		label_fexits [i] = code;
		x86_branch8 (code, X86_CC_Z, 0, FALSE);

		amd64_sse_movsd_reg_membase (code, farg_regs [i], AMD64_R11, i * sizeof (double));
		amd64_dec_reg_size (code, AMD64_RAX, 1);
	}

	for (i = 0; i < INTERP_ICALL_TRAMP_FARGS; i++)
		x86_patch (label_fexits [i], code);

	/* load pointer to InterpMethodArguments* into R11 */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_ARG_REG2, sizeof (mgreg_t));
	/* move ilen into RAX */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, ilen), sizeof (mgreg_t));

	int stack_offset = 0;
	for (i = 0; i < INTERP_ICALL_TRAMP_IARGS; i++) {
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		label_gexits [i] = code;
		x86_branch32 (code, X86_CC_Z, 0, FALSE);

		/* load pointer to InterpMethodArguments* into R11 */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_methodargs, sizeof (mgreg_t));
		/* load pointer to iargs into R11 */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, iargs), sizeof (mgreg_t));

		if (i < PARAM_REGS) {
			amd64_mov_reg_membase (code, param_regs [i], AMD64_R11, i * sizeof (mgreg_t), sizeof (mgreg_t));
		} else {
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, i * sizeof (mgreg_t), sizeof (mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RSP, stack_offset, AMD64_R11, sizeof (mgreg_t));
			stack_offset += sizeof (mgreg_t);
		}
		amd64_dec_reg_size (code, AMD64_RAX, 1);
	}

	for (i = 0; i < INTERP_ICALL_TRAMP_IARGS; i++)
		x86_patch (label_gexits [i], code);

	/* load target addr */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_targetaddr, sizeof (mgreg_t));

	/* call into native function */
	amd64_call_reg (code, AMD64_R11);

	/* load InterpMethodArguments */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_methodargs, sizeof (mgreg_t));

	/* load is_float_ret */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, is_float_ret), sizeof (mgreg_t));

	/* check if a float return value is expected */
	amd64_test_reg_reg (code, AMD64_R11, AMD64_R11);

	label_is_float_ret = code;
	x86_branch8 (code, X86_CC_NZ, 0, FALSE);

	/* greg return */
	/* load InterpMethodArguments */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_methodargs, sizeof (mgreg_t));
	/* load retval */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, retval), sizeof (mgreg_t));

	amd64_test_reg_reg (code, AMD64_R11, AMD64_R11);
	label_leave_tramp [0] = code;
	x86_branch8 (code, X86_CC_Z, 0, FALSE);

	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RAX, sizeof (mgreg_t));

	label_leave_tramp [1] = code;
	x86_jump8 (code, 0);

	/* freg return */
	x86_patch (label_is_float_ret, code);
	/* load InterpMethodArguments */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_methodargs, sizeof (mgreg_t));
	/* load retval */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, MONO_STRUCT_OFFSET (InterpMethodArguments, retval), sizeof (mgreg_t));

	amd64_test_reg_reg (code, AMD64_R11, AMD64_R11);
	label_leave_tramp [2] = code;
	x86_branch8 (code, X86_CC_Z, 0, FALSE);

	amd64_sse_movsd_membase_reg (code, AMD64_R11, 0, AMD64_XMM0);

	for (i = 0; i < 3; i++)
		x86_patch (label_leave_tramp [i], code);

	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT));
	amd64_pop_reg (code, AMD64_RBP);
	amd64_ret (code);

	g_assert (code - start < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("enter_icall_trampoline", start, code - start, ji, unwind_ops);

	return start;
#else
	g_assert_not_reached ();
	return NULL;
#endif /* DISABLE_INTERPRETER */
}
#endif /* !DISABLE_JIT */

#ifdef DISABLE_JIT
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	g_assert_not_reached ();
	return;
}

guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_enter_icall_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}
#endif /* DISABLE_JIT */
