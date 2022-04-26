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
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internals.h>
#include <mono/arch/amd64/amd64-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-amd64.h"
#include "mini-runtime.h"

#ifndef DISABLE_INTERPRETER
#include "interp/interp.h"
#endif
#include "mono/utils/mono-tls-inline.h"
#include <mono/metadata/components.h>

#ifdef MONO_ARCH_CODE_EXEC_ONLY
#include "aot-runtime.h"
guint8* mono_aot_arch_get_plt_entry_exec_only (gpointer amodule_info, host_mgreg_t *regs, guint8 *code, guint8 *plt);
guint32 mono_arch_get_plt_info_offset_exec_only (gpointer amodule_info, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code, MonoAotResolvePltInfoOffset resolver, gpointer amodule);
void mono_arch_patch_plt_entry_exec_only (gpointer amodule_info, guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr);
#endif

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

MONO_PRAGMA_WARNING_DISABLE(4127) /* conditional expression is constant */

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
	const int size = 20;
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (m);

	const int this_reg = mono_arch_get_this_arg_reg (NULL);

	start = code = (guint8 *)mono_mem_manager_code_reserve (mem_manager, size + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	amd64_alu_reg_imm (code, X86_ADD, this_reg, MONO_ABI_SIZEOF (MonoObject));
	/* FIXME: Optimize this */
	amd64_mov_reg_imm (code, AMD64_RAX, addr);
	amd64_jump_reg (code, AMD64_RAX);
	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), mem_manager);

	return start;
}

/*
 * mono_arch_get_static_rgctx_trampoline:
 *
 *   Create a trampoline which sets RGCTX_REG to ARG, then jumps to ADDR.
 */
gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	GSList *unwind_ops;
	int buf_len;

#ifdef MONO_ARCH_NOMAP32BIT
	buf_len = 32;
#else
	/* AOTed code could still have a non-32 bit address */
	if ((((guint64)addr) >> 32) == 0)
		buf_len = 16;
	else
		buf_len = 30;
#endif

	start = code = (guint8 *)mono_mem_manager_code_reserve (mem_manager, buf_len + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	amd64_mov_reg_imm (code, MONO_ARCH_RGCTX_REG, arg);
	amd64_jump_code (code, addr);
	g_assertf ((code - start) <= buf_len, "%d %d", (int)(code - start), buf_len);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_TRAMPOLINE_UNWINDINFO_SIZE(0)));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), mem_manager);

	return start;
}
#endif /* !DISABLE_JIT */

#ifdef _WIN64
// Workaround lack of Valgrind support for 64-bit Windows
#undef VALGRIND_DISCARD_TRANSLATIONS
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

	// Since method_start is retrieved from function return address (below current call/jmp to patch) there is a case when
	// last instruction of a function is the call (due to OP_NOT_REACHED) instruction and then directly followed by a
	// different method. In that case current orig_code points into next method and method_start will also point into
	// next method, not the method including the call to patch. For this specific case, fallback to using a method_start of NULL.
	mono_breakpoint_clean_code (method_start != orig_code ? method_start : NULL, orig_code, 14, buf, sizeof (buf));

	code = buf + 14;

	/* mov 64-bit imm into r11 (followed by call reg?)  or direct call*/
	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8) {
			g_assert ((guint64)(orig_code - 11) % 8 == 0);
			mono_atomic_xchg_ptr ((gpointer*)(orig_code - 11), addr);
			VALGRIND_DISCARD_TRANSLATIONS (orig_code - 11, sizeof (gpointer));
		} else {
			gboolean disp_32bit = ((((gint64)addr - (gint64)orig_code)) < (1 << 30)) && ((((gint64)addr - (gint64)orig_code)) > -(1 << 30));

			if ((((guint64)(addr)) >> 32) != 0 && !disp_32bit) {
				/*
				 * This might happen with LLVM or when calling AOTed code. Create a thunk.
				 */
				guint8 *thunk_start, *thunk_code;
				MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();

				thunk_start = thunk_code = (guint8 *)mono_mem_manager_code_reserve (mem_manager, 32);
				amd64_jump_membase (thunk_code, AMD64_RIP, 0);
				*(guint64*)thunk_code = (guint64)addr;
				addr = thunk_start;
				g_assert ((((guint64)(addr)) >> 32) == 0);
				mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
				MONO_PROFILER_RAISE (jit_code_buffer, (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
			}
			mono_atomic_xchg_i32 ((gint32*)(orig_code - 4), ((gint64)addr - (gint64)orig_code));
			VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, 4);
		}
	}
	else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		gpointer *got_entry = (gpointer*)((guint8*)orig_code + (*(guint32*)(orig_code - 4)));
		mono_atomic_xchg_ptr (got_entry, addr);
		VALGRIND_DISCARD_TRANSLATIONS (orig_code - 5, sizeof (gpointer));
	}
}

#ifndef DISABLE_JIT
guint8*
mono_arch_create_llvm_native_thunk (guint8 *addr)
{
	/*
	 * The caller is LLVM code and the call displacement might exceed 32 bits. We can't determine the caller address, so
	 * we add a thunk every time.
	 * Since the caller is also allocated using the domain code manager, hopefully the displacement will fit into 32 bits.
	 * FIXME: Avoid this if possible if !MONO_ARCH_NOMAP32BIT and ADDR is 32 bits.
	 */
	guint8 *thunk_start, *thunk_code;
	// FIXME: Has to be an argument
	MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();

	thunk_start = thunk_code = (guint8 *)mono_mem_manager_code_reserve (mem_manager, 32);
	amd64_jump_membase (thunk_code, AMD64_RIP, 0);
	*(guint64*)thunk_code = (guint64)addr;
	addr = thunk_start;
	mono_arch_flush_icache (thunk_start, thunk_code - thunk_start);
	MONO_PROFILER_RAISE (jit_code_buffer, (thunk_start, thunk_code - thunk_start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	return addr;
}

static void
stack_unaligned (MonoTrampolineType tramp_type)
{
	printf ("%d\n", tramp_type);
	g_assert_not_reached ();
}

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	const char *tramp_name;
	guint8 *buf, *code, *tramp, *br [2], *r11_save_code, *after_r11_save_code, *br_ex_check;
	int i, lmf_offset, offset, res_offset, arg_offset, rax_offset, tramp_offset, ctx_offset, saved_regs_offset;
	int r11_save_offset, saved_fpregs_offset, rbp_offset, framesize, orig_rsp_to_rbp_offset, cfa_offset;
	gboolean has_caller;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	const int kMaxCodeSize = 630;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	/* Compute stack frame size and offsets */
	offset = 0;
	rbp_offset = -offset;

	offset += sizeof (target_mgreg_t);
	rax_offset = -offset;

	/* ex_offset */
	offset += sizeof (target_mgreg_t);

	offset += sizeof (target_mgreg_t);
	r11_save_offset = -offset;

	offset += sizeof (target_mgreg_t);
	tramp_offset = -offset;

	offset += sizeof (target_mgreg_t);
	arg_offset = -offset;

	offset += sizeof (target_mgreg_t);
	res_offset = -offset;

	offset += sizeof (MonoContext);
	ctx_offset = -offset;
	saved_regs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);
	saved_fpregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, fregs);

	offset += sizeof (MonoLMFTramp);
	lmf_offset = -offset;

#ifdef TARGET_WIN32
	/* Reserve space where the callee can save the argument registers */
	offset += 4 * sizeof (target_mgreg_t);
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
	orig_rsp_to_rbp_offset += sizeof (target_mgreg_t);

	cfa_offset -= sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);

	/*
	 * Allocate a new stack frame
	 */
	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	orig_rsp_to_rbp_offset -= sizeof (target_mgreg_t);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (target_mgreg_t));
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
	amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, sizeof (target_mgreg_t));

	/* Save all registers */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i == AMD64_RBP) {
			/* RAX is already saved */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, rbp_offset, sizeof (target_mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), AMD64_RAX, sizeof (target_mgreg_t));
		} else if (i == AMD64_RIP) {
			if (has_caller)
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, sizeof (target_mgreg_t));
			else
				amd64_mov_reg_imm (code, AMD64_R11, 0);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), AMD64_R11, sizeof (target_mgreg_t));
		} else if (i == AMD64_RSP) {
			amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof (target_mgreg_t));
			amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), AMD64_R11, sizeof (target_mgreg_t));
		} else if (i != AMD64_R11) {
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), i, sizeof (target_mgreg_t));
		} else {
			/* We have to save R11 right at the start of
			   the trampoline code because it's used as a
			   scratch register */
			/* This happens before the frame is set up, so it goes into the redzone */
			amd64_mov_membase_reg (r11_save_code, AMD64_RSP, r11_save_offset + orig_rsp_to_rbp_offset, i, sizeof (target_mgreg_t));
			g_assert (r11_save_code == after_r11_save_code);

			/* Copy from the save slot into the register array slot */
			amd64_mov_reg_membase (code, i, AMD64_RSP, r11_save_offset + orig_rsp_to_rbp_offset + framesize, sizeof (target_mgreg_t));
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), i, sizeof (target_mgreg_t));
		}
		/* cfa = rbp + cfa_offset */
		mono_add_unwind_op_offset (unwind_ops, code, buf, i, - cfa_offset + saved_regs_offset + (i * sizeof (target_mgreg_t)));
	}
	for (i = 0; i < AMD64_XMM_NREG; ++i)
		if (AMD64_IS_ARGUMENT_XREG (i))
#if defined(MONO_HAVE_SIMD_REG)
			amd64_movdqu_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * sizeof (MonoContextSimdReg)), i);
#else
			amd64_movsd_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * sizeof (double)), i);
#endif

	/* Check that the stack is aligned */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof (target_mgreg_t));
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
		/*
		 * tramp_index = (tramp_addr - specific_trampolines) / tramp_size
		 * arg = mscorlib_amodule->got [specific_trampolines_got_offsets_base + (tramp_index * 2) + 1]
		 */
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES, NULL);
		/* Trampoline addr */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, tramp_offset, sizeof (target_mgreg_t));
		/* Trampoline offset */
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RAX, AMD64_R11);
		/* Trampoline index */
		amd64_shift_reg_imm (code, X86_SHR, AMD64_RAX, 3);
		/* Every trampoline uses 2 got slots */
		amd64_shift_reg_imm (code, X86_SHL, AMD64_RAX, 1);
		/* pointer size */
		amd64_shift_reg_imm (code, X86_SHL, AMD64_RAX, 3);
		/* Address of block of got slots */
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES_GOT_SLOTS_BASE, NULL);
		/* Address of got slots belonging to this trampoline */
		amd64_alu_reg_reg (code, X86_ADD, AMD64_RAX, AMD64_R11);
		/* The second slot contains the argument */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, sizeof (target_mgreg_t), sizeof (target_mgreg_t));
	} else {
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, sizeof (target_mgreg_t));
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
	amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, AMD64_R11, sizeof (target_mgreg_t));

	/* Save LMF begin */

	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, sizeof (target_mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, sizeof (target_mgreg_t));
	/* Save pointer to context */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, ctx_offset);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, ctx), AMD64_R11, sizeof (target_mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_get_lmf_addr));
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_get_lmf_addr);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Save lmf_addr */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), AMD64_RAX, sizeof (target_mgreg_t));
	/* Save previous_lmf */
	/* Set the third lowest bit to signal that this is a MonoLMFTramp structure */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, sizeof (target_mgreg_t));
	amd64_alu_reg_imm_size (code, X86_ADD, AMD64_R11, 0x5, sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, sizeof (target_mgreg_t));
	/* Set new lmf */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, lmf_offset);
	amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, sizeof (target_mgreg_t));

	/* Save LMF end */

	/* Arg1 is the pointer to the saved registers */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RBP, saved_regs_offset);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_ARG_REG2, AMD64_RBP, 8, sizeof (target_mgreg_t));
	else
		amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_RBP, arg_offset, sizeof (target_mgreg_t));

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_ARG_REG4, AMD64_RBP, tramp_offset, sizeof (target_mgreg_t));

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GINT_TO_POINTER (mono_trampoline_type_to_jit_icall_id (tramp_type)));
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		amd64_mov_reg_imm (code, AMD64_R11, tramp);
	}
	amd64_call_reg (code, AMD64_R11);
	amd64_mov_membase_reg (code, AMD64_RBP, res_offset, AMD64_RAX, sizeof (target_mgreg_t));

	/* Restore LMF */
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof (target_mgreg_t));
	amd64_alu_reg_imm_size (code, X86_SUB, AMD64_RCX, 0x5, sizeof (target_mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMFTramp, lmf_addr), sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, sizeof (target_mgreg_t));

	/*
	 * Save rax to the stack, after the leave instruction, this will become part of
	 * the red zone.
	 */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, res_offset, sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, rax_offset, AMD64_RAX, sizeof (target_mgreg_t));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/*
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_thread_force_interruption_checkpoint_noraise));
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
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_rethrow_preserve_exception));
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, (guint8*)mono_get_rethrow_preserve_exception_addr ());
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, sizeof (target_mgreg_t));
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RAX, sizeof (target_mgreg_t));
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
		if (AMD64_IS_ARGUMENT_REG (i) || i == AMD64_R10 || i == AMD64_RAX || i == AMD64_R11)
			amd64_mov_reg_membase (code, i, AMD64_RBP, saved_regs_offset + (i * sizeof (target_mgreg_t)), sizeof (target_mgreg_t));
	for (i = 0; i < AMD64_XMM_NREG; ++i)
		if (AMD64_IS_ARGUMENT_XREG (i))
#if defined(MONO_HAVE_SIMD_REG)
			amd64_movdqu_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * sizeof (MonoContextSimdReg)));
#else
			amd64_movsd_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * sizeof (double)));
#endif

	/* Restore stack */
#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	cfa_offset -= sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, cfa_offset);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type)) {
		/* Load result */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, rax_offset - sizeof (target_mgreg_t), sizeof (target_mgreg_t));
		amd64_ret (code);
	} else {
		/* call the compiled method using the saved rax */
		amd64_jump_membase (code, AMD64_RSP, rax_offset - sizeof (target_mgreg_t));
	}

	g_assertf ((code - buf) <= kMaxCodeSize, "%d %d", code, buf, (int)(code - buf), kMaxCodeSize);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	int size;
	gboolean far_addr = FALSE;

	tramp = mono_get_trampoline_code (tramp_type);

	if ((((guint64)arg1) >> 32) == 0)
		size = 5 + 1 + 4;
	else
		size = 5 + 1 + 8;

	code = buf = (guint8 *)mono_mem_manager_code_reserve_align (mem_manager, size, 1);

	if (((gint64)tramp - (gint64)code) >> 31 != 0 && ((gint64)tramp - (gint64)code) >> 31 != -1) {
#ifndef MONO_ARCH_NOMAP32BIT
		g_assert_not_reached ();
#endif
		far_addr = TRUE;
		size += 16;
		code = buf = (guint8 *)mono_mem_manager_code_reserve_align (mem_manager, size, 1);
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
	int depth, index;
	int i;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (target_mgreg_t);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	const int tramp_size = 64 + 8 * depth;

	code = buf = (guint8 *)mono_global_codeman_reserve (tramp_size + MONO_TRAMPOLINE_UNWINDINFO_SIZE(0));

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = (guint8 **)g_malloc (sizeof (guint8*) * (depth + 2));

	if (mrgctx) {
		/* get mrgctx ptr */
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
	} else {
		/* load rgctx ptr from vtable */
		amd64_mov_reg_membase (code, AMD64_RAX, AMD64_ARG_REG1, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context), sizeof (target_mgreg_t));
		/* is the rgctx ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [0] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT, sizeof (target_mgreg_t));
		else
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, 0, sizeof (target_mgreg_t));
		/* is the ptr null? */
		amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RAX, sizeof (target_mgreg_t) * (index + 1), sizeof (target_mgreg_t));
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
		amd64_mov_reg_reg (code, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, sizeof (target_mgreg_t));
	}

	if (aot) {
		code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR, GUINT_TO_POINTER (slot));
		amd64_jump_reg (code, AMD64_R11);
	} else {
		MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();
		tramp = (guint8 *)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, NULL);

		/* jump to the actual trampoline */
		amd64_jump_code (code, tramp);
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assertf ((code - buf) <= tramp_size, "%d %d", (int)(code - buf), tramp_size);

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
	amd64_mov_reg_reg (code, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, sizeof (target_mgreg_t));
	/* Jump to the trampoline */
	amd64_jump_reg (code, AMD64_R11);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assertf ((code - buf) <= tramp_size, "%d %d", (int)(code - buf), tramp_size);

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

#ifdef MONO_ARCH_CODE_EXEC_ONLY
/* Keep in sync with aot-compiler.c, arch_emit_plt_entry. */
#define PLT_ENTRY_OFFSET_REG AMD64_RAX

/* If PLT_ENTRY_OFFSET_REG is R8 - R15, increase mov instruction size by 1 due to use of REX. */
#define PLT_MOV_REG_IMM8_SIZE (1 + sizeof (guint8))
#define PLT_MOV_REG_IMM16_SIZE (2 + sizeof (guint16))
#define PLT_MOV_REG_IMM32_SIZE (1 + sizeof (guint32))
#define PLT_JMP_INST_SIZE 6

static guchar
aot_arch_get_plt_entry_size (MonoAotFileInfo *info, host_mgreg_t *regs, guint8 *code, guint8 *plt)
{
	if (info->plt_size <= 0xFF)
		return PLT_MOV_REG_IMM8_SIZE + PLT_JMP_INST_SIZE;
	else if (info->plt_size <= 0xFFFF)
		return PLT_MOV_REG_IMM16_SIZE + PLT_JMP_INST_SIZE;
	else
		return PLT_MOV_REG_IMM32_SIZE + PLT_JMP_INST_SIZE;
}

static guint32
aot_arch_get_plt_entry_index (MonoAotFileInfo *info, host_mgreg_t *regs, guint8 *code, guint8 *plt)
{
	if (info->plt_size <= 0xFF)
		return regs[PLT_ENTRY_OFFSET_REG] & 0xFF;
	else if (info->plt_size <= 0xFFFF)
		return regs[PLT_ENTRY_OFFSET_REG] & 0xFFFF;
	else
		return regs[PLT_ENTRY_OFFSET_REG] & 0xFFFFFFFF;
}

guint8*
mono_aot_arch_get_plt_entry_exec_only (gpointer amodule_info, host_mgreg_t *regs, guint8 *code, guint8 *plt)
{
	guint32 plt_entry_index = aot_arch_get_plt_entry_index ((MonoAotFileInfo *)amodule_info, regs, code, plt);
	guchar plt_entry_size = aot_arch_get_plt_entry_size ((MonoAotFileInfo *)amodule_info, regs, code, plt);

	/* First PLT slot is never emitted into table, take that into account */
	/* when calculating corresponding PLT entry. */
	plt_entry_index--;
	return plt + ((gsize)plt_entry_index * (gsize)plt_entry_size);
}

guint32
mono_arch_get_plt_info_offset_exec_only (gpointer amodule_info, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code, MonoAotResolvePltInfoOffset resolver, gpointer amodule)
{
	guint32 plt_entry_index = aot_arch_get_plt_entry_index ((MonoAotFileInfo *)amodule_info, regs, code, NULL);

	/* First PLT slot is never emitted into table, take that into account */
	/* when calculating offset. */
	plt_entry_index--;
	return resolver (amodule, plt_entry_index);
}

void
mono_arch_patch_plt_entry_exec_only (gpointer amodule_info, guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	/* Same calculation of GOT offset as done in aot-compiler.c, emit_plt and used as jmp DISP. */
	guint32 plt_entry_index = aot_arch_get_plt_entry_index ((MonoAotFileInfo *)amodule_info, regs, code, NULL);
	gpointer *plt_jump_table_entry = ((gpointer *)(got + ((MonoAotFileInfo *)amodule_info)->plt_got_offset_base) + plt_entry_index);
	mono_atomic_xchg_ptr (plt_jump_table_entry, addr);
}
#else
/*
 * mono_arch_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry PLT_ENTRY.
 */
guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	return *(guint32*)(plt_entry + 6);
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
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
#endif

#ifndef DISABLE_JIT
/*
 * mono_arch_create_sdb_trampoline:
 *
 *   Return a trampoline which captures the current context, passes it to
 * mono_debugger_agent_single_step_from_context ()/mono_debugger_agent_breakpoint_from_context (),
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
	framesize += 4 * sizeof (target_mgreg_t);
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
	cfa_offset += sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, buf, AMD64_RBP, - cfa_offset);

	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (target_mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, buf, AMD64_RBP, 0);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	gregs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);

	/* Initialize a MonoContext structure on the stack */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (i * sizeof (target_mgreg_t)), i, sizeof (target_mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 0, sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (target_mgreg_t)), AMD64_R11, sizeof (target_mgreg_t));
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, 2 * sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RSP * sizeof (target_mgreg_t)), AMD64_R11, sizeof (target_mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, sizeof (target_mgreg_t), sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (target_mgreg_t)), AMD64_R11, sizeof (target_mgreg_t));

	/* Call the single step/breakpoint function in sdb */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RSP, ctx_offset);

	if (aot) {
		if (single_step)
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_single_step_from_context));
		else
			code = mono_arch_emit_load_aotconst (buf, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_breakpoint_from_context));
	} else {
		if (single_step)
			amd64_mov_reg_imm (code, AMD64_R11, mono_component_debugger ()->single_step_from_context);
		else
			amd64_mov_reg_imm (code, AMD64_R11, mono_component_debugger ()->breakpoint_from_context);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Restore registers from ctx */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_RBP)
			amd64_mov_reg_membase (code, i, AMD64_RSP, gregs_offset + (i * sizeof (target_mgreg_t)), sizeof (target_mgreg_t));
	}
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RBP * sizeof (target_mgreg_t)), sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, 0, AMD64_R11, sizeof (target_mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, gregs_offset + (AMD64_RIP * sizeof (target_mgreg_t)), sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RBP, sizeof (target_mgreg_t), AMD64_R11, sizeof (target_mgreg_t));

#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	cfa_offset -= sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, AMD64_RSP, cfa_offset);
	amd64_ret (code);

	g_assertf ((code - buf) <= tramp_size, "%d %d", (int)(code - buf), tramp_size);

	mono_arch_flush_icache (code, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	g_assert (code - buf <= tramp_size);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

/*
 * mono_arch_get_interp_to_native_trampoline:
 *
 *   A trampoline that handles the transition from interpreter into native
 *   world. It requires to set up a descriptor (CallContext), so the
 *   trampoline can translate the arguments into the native calling convention.
 */
gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
#ifndef DISABLE_INTERPRETER
	guint8 *start = NULL, *code;
	guint8 *label_start_copy, *label_exit_copy;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int buf_len, i, cfa_offset, off_methodargs, off_targetaddr;

	buf_len = 512;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	// CFA = sp + 8
	cfa_offset = 8;
	mono_add_unwind_op_def_cfa (unwind_ops, code, start, AMD64_RSP, cfa_offset);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, start, AMD64_RIP, -cfa_offset);

	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, start, AMD64_RBP, -cfa_offset);

	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (target_mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, start, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, start, AMD64_RBP, 0);

	/* allocate space for saving the target addr and the call context */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 2 * sizeof (target_mgreg_t));

	/* save CallContext* onto stack */
	off_methodargs = - 8;
	amd64_mov_membase_reg (code, AMD64_RBP, off_methodargs, AMD64_ARG_REG2, sizeof (target_mgreg_t));

	/* save target address on stack */
	off_targetaddr = - 2 * 8;
	amd64_mov_membase_reg (code, AMD64_RBP, off_targetaddr, AMD64_ARG_REG1, sizeof (target_mgreg_t));

	/* load pointer to CallContext* into R11 */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_ARG_REG2, sizeof (target_mgreg_t));

	/* allocate the stack space necessary for the call */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11, MONO_STRUCT_OFFSET (CallContext, stack_size), sizeof (target_mgreg_t));
	amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, AMD64_RAX);

	/* copy stack from the CallContext, reg1 = dest, reg2 = source */
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RSP, sizeof (target_mgreg_t));
	amd64_mov_reg_membase (code, AMD64_ARG_REG2, AMD64_R11, MONO_STRUCT_OFFSET (CallContext, stack), sizeof (target_mgreg_t));

	label_start_copy = code;
	amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
	label_exit_copy = code;
	amd64_branch8 (code, X86_CC_Z, 0, FALSE);
	amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_ARG_REG2, 0, sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_ARG_REG1, 0, AMD64_ARG_REG3, sizeof (target_mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_ARG_REG1, sizeof (target_mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_ARG_REG2, sizeof (target_mgreg_t));
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RAX, sizeof (target_mgreg_t));
	amd64_jump_code (code, label_start_copy);
	x86_patch (label_exit_copy, code);

	/* set all general purpose registers from CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		amd64_mov_reg_membase (code, param_regs [i], AMD64_R11, MONO_STRUCT_OFFSET (CallContext, gregs) + param_regs [i] * sizeof (target_mgreg_t), sizeof (target_mgreg_t));

	/* set all floating registers from CallContext  */
	for (i = 0; i < FLOAT_PARAM_REGS; ++i)
		amd64_sse_movsd_reg_membase (code, i, AMD64_R11, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* load target addr */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_targetaddr, sizeof (target_mgreg_t));

	/* call into native function */
	amd64_call_reg (code, AMD64_R11);

	/* save all return general purpose registers in the CallContext */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, off_methodargs, sizeof (target_mgreg_t));
	for (i = 0; i < RETURN_REGS; i++)
		amd64_mov_membase_reg (code, AMD64_R11, MONO_STRUCT_OFFSET (CallContext, gregs) + return_regs [i] * sizeof (target_mgreg_t), return_regs [i], sizeof (target_mgreg_t));

	/* save all return floating registers in the CallContext */
	for (i = 0; i < FLOAT_RETURN_REGS; i++)
		amd64_sse_movsd_membase_reg (code, AMD64_R11, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double), i);

#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
#else
	amd64_mov_reg_reg (code, AMD64_RSP, AMD64_RBP, sizeof (target_mgreg_t));
#endif
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, start, AMD64_RBP);

	cfa_offset -= sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, start, AMD64_RSP, cfa_offset);
	amd64_ret (code);

	g_assertf ((code - start) <= buf_len, "%d %d", (int)(code - start), buf_len);

	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", start, code - start, ji, unwind_ops);

	return start;
#else
	g_assert_not_reached ();
	return NULL;
#endif /* DISABLE_INTERPRETER */
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
#ifndef DISABLE_INTERPRETER
	guint8 *start = NULL, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int buf_len, i, framesize, cfa_offset, ctx_offset;

	buf_len = 512;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);

	framesize = 0;
#ifdef TARGET_WIN32
	/* Reserve space where the callee can save the argument registers */
	framesize += 4 * sizeof (target_mgreg_t);
#endif

	ctx_offset = framesize;
	framesize += MONO_ABI_SIZEOF (CallContext);
	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	// CFA = sp + 8
	cfa_offset = 8;
	mono_add_unwind_op_def_cfa (unwind_ops, code, start, AMD64_RSP, cfa_offset);
	// IP saved at CFA - 8
	mono_add_unwind_op_offset (unwind_ops, code, start, AMD64_RIP, -cfa_offset);

	amd64_push_reg (code, AMD64_RBP);
	cfa_offset += sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, start, AMD64_RBP, -cfa_offset);

	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (target_mgreg_t));
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, start, AMD64_RBP);
	mono_add_unwind_op_fp_alloc (unwind_ops, code, start, AMD64_RBP, 0);

	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	/* save all general purpose registers into the CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		amd64_mov_membase_reg (code, AMD64_RSP, ctx_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + param_regs [i] * sizeof (target_mgreg_t), param_regs [i], sizeof (target_mgreg_t));

	/* save all floating registers into the CallContext  */
	for (i = 0; i < FLOAT_PARAM_REGS; i++)
		amd64_sse_movsd_membase_reg (code, AMD64_RSP, ctx_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double), i);

	/* set the stack pointer to the value at call site */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RBP, sizeof (target_mgreg_t));
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, 2 * sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, ctx_offset + MONO_STRUCT_OFFSET (CallContext, stack), AMD64_R11, sizeof (target_mgreg_t));

	/* call interp_entry with the ccontext and rmethod as arguments */
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RSP, sizeof (target_mgreg_t));
	if (ctx_offset != 0)
		amd64_alu_reg_imm (code, X86_ADD, AMD64_ARG_REG1, ctx_offset);
	amd64_mov_reg_membase (code, AMD64_ARG_REG2, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, arg), sizeof (target_mgreg_t));
	amd64_mov_reg_membase (code, AMD64_R11, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, addr), sizeof (target_mgreg_t));
	amd64_call_reg (code, AMD64_R11);

	/* load the return values from the context */
	for (i = 0; i < RETURN_REGS; i++)
		amd64_mov_reg_membase (code, return_regs [i], AMD64_RSP, ctx_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + return_regs [i] * sizeof (target_mgreg_t), sizeof (target_mgreg_t));

	for (i = 0; i < FLOAT_RETURN_REGS; i++)
		amd64_sse_movsd_reg_membase (code, i, AMD64_RSP, ctx_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* reset stack and return */
#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
#else
	amd64_mov_reg_reg (code, AMD64_RSP, AMD64_RBP, sizeof (target_mgreg_t));
#endif
	amd64_pop_reg (code, AMD64_RBP);
	mono_add_unwind_op_same_value (unwind_ops, code, start, AMD64_RBP);

	cfa_offset -= sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, start, AMD64_RSP, cfa_offset);
	amd64_ret (code);

	g_assertf ((code - start) <= buf_len, "%d %d", (int)(code - start), buf_len);

	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("native_to_interp_trampoline", start, code - start, ji, unwind_ops);

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
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
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
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
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
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}
#endif /* DISABLE_JIT */
