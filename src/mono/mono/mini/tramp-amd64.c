/*
 * tramp-amd64.c: JIT trampoline code for amd64
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/arch/amd64/amd64-codegen.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include "mini.h"
#include "mini-amd64.h"

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

static guint8* nullified_class_init_trampoline;

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
	int this_reg;

	MonoDomain *domain = mono_domain_get ();

	this_reg = mono_arch_get_this_arg_reg (mono_method_signature (m), NULL);

	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 20);
	mono_domain_unlock (domain);

	amd64_alu_reg_imm (code, X86_ADD, this_reg, sizeof (MonoObject));
	/* FIXME: Optimize this */
	amd64_mov_reg_imm (code, AMD64_RAX, addr);
	amd64_jump_reg (code, AMD64_RAX);
	g_assert ((code - start) < 20);

	mono_arch_flush_icache (start, code - start);

	return start;
}

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

	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8) {
			if (can_write)
				InterlockedExchangePointer ((gpointer*)(orig_code - 11), addr);
		} else {
			if ((((guint64)(addr)) >> 32) != 0) {
				/* Print some diagnostics */
				MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), (char*)orig_code);
				if (ji)
					fprintf (stderr, "At %s, offset 0x%zx\n", mono_method_full_name (ji->method, TRUE), (guint8*)orig_code - (guint8*)ji->code_start);
				fprintf (stderr, "Addr: %p\n", addr);
				ji = mono_jit_info_table_find (mono_domain_get (), (char*)addr);
				if (ji)
					fprintf (stderr, "Callee: %s\n", mono_method_full_name (ji->method, TRUE));
				g_assert_not_reached ();
			}
			g_assert ((((guint64)(orig_code)) >> 32) == 0);
			if (can_write)
				InterlockedExchange ((gint32*)(orig_code - 4), ((gint64)addr - (gint64)orig_code));
		}
	}
	else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		gpointer *got_entry = (gpointer*)((guint8*)orig_code + (*(guint32*)(orig_code - 4)));
		if (can_write)
			InterlockedExchangePointer (got_entry, addr);
	}
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	gint32 disp;
	gpointer *plt_jump_table_entry;

	/* A PLT entry: jmp *<DISP>(%rip) */
	g_assert (code [0] == 0xff);
	g_assert (code [1] == 0x25);

	disp = *(gint32*)(code + 2);

	plt_jump_table_entry = (gpointer*)(code + 6 + disp);

	InterlockedExchangePointer (plt_jump_table_entry, addr);
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	guint8 buf [16];
	gboolean can_write = mono_breakpoint_clean_code (NULL, code, 7, buf, sizeof (buf));

	if (!can_write)
		return;

	code -= 3;

	/* 
	 * A given byte sequence can match more than case here, so we have to be
	 * really careful about the ordering of the cases. Longer sequences
	 * come first.
	 */
	if ((code [-4] == 0x41) && (code [-3] == 0xff) && (code [-2] == 0x15)) {
		gpointer *vtable_slot;

		/* call *<OFFSET>(%rip) */
		vtable_slot = mono_arch_get_vcall_slot_addr (code + 3, (gpointer*)regs);
		g_assert (vtable_slot);

		*vtable_slot = nullified_class_init_trampoline;
	} else if (code [-2] == 0xe8) {
		/* call <TARGET> */
		guint8 *buf = code - 2;

		buf [0] = 0x66;
		buf [1] = 0x66;
		buf [2] = 0x90;
		buf [3] = 0x66;
		buf [4] = 0x90;
	} else if ((code [0] == 0x41) && (code [1] == 0xff)) {
		/* call <REG> */
		/* happens on machines without MAP_32BIT like freebsd */
		/* amd64_set_reg_template is 10 bytes long */
		guint8* buf = code - 10;

		/* FIXME: Make this thread safe */
		/* Padding code suggested by the AMD64 Opt Manual */
		buf [0] = 0x66;
		buf [1] = 0x66;
		buf [2] = 0x66;
		buf [3] = 0x90;
		buf [4] = 0x66;
		buf [5] = 0x66;
		buf [6] = 0x66;
		buf [7] = 0x90;
		buf [8] = 0x66;
		buf [9] = 0x66;
		buf [10] = 0x90;
		buf [11] = 0x66;
		buf [12] = 0x90;
	} else if (code [0] == 0x90 || code [0] == 0xeb || code [0] == 0x66) {
		/* Already changed by another thread */
		;
	} else {
		printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
			code [4], code [5], code [6]);
		g_assert_not_reached ();
	}
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	mono_arch_patch_plt_entry (code, nullified_class_init_trampoline);
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp, *br [2], *r11_save_code, *after_r11_save_code;
	int i, lmf_offset, offset, res_offset, arg_offset, tramp_offset, saved_regs_offset;
	int saved_fpregs_offset, rbp_offset, framesize, orig_rsp_to_rbp_offset;
	gboolean has_caller;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = mono_global_codeman_reserve (524);

	framesize = 524 + sizeof (MonoLMF);
	framesize = (framesize + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

	if (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		static int byte_offset = -1;
		static guint8 bitmask;

		guint8 *jump;

		if (byte_offset < 0)
			mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

		amd64_test_membase_imm_size (code, MONO_ARCH_VTABLE_REG, byte_offset, bitmask, 1);
		jump = code;
		amd64_branch8 (code, X86_CC_Z, -1, 1);

		amd64_ret (code);

		x86_patch (jump, code);
	}

	orig_rsp_to_rbp_offset = 0;
	r11_save_code = code;
	/* Reserve 5 bytes for the mov_membase_reg to save R11 */
	code += 5;
	after_r11_save_code = code;

	/*
	 * The generic class init trampoline is called directly by
	 * JITted code, there is no specific trampoline.  The lazy
	 * fetch trampolines behave like generic class init
	 * trampolines.
	 */
	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT &&
			tramp_type != MONO_TRAMPOLINE_RGCTX_LAZY_FETCH) {
		/* Pop the return address off the stack */
		amd64_pop_reg (code, AMD64_R11);
		orig_rsp_to_rbp_offset += 8;
	}

	/* 
	 * Allocate a new stack frame
	 */
	amd64_push_reg (code, AMD64_RBP);
	orig_rsp_to_rbp_offset -= 8;
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	offset = 0;
	rbp_offset = - offset;

	offset += 8;
	tramp_offset = - offset;

	offset += 8;
	arg_offset = - offset;

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT &&
			tramp_type != MONO_TRAMPOLINE_RGCTX_LAZY_FETCH) {
		/* Compute the trampoline address from the return address */
		/* 5 = length of amd64_call_membase () */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 5);
		amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, 8);
	} else {
		amd64_mov_membase_imm (code, AMD64_RBP, tramp_offset, 0, 8);
	}

	offset += 8;
	res_offset = - offset;

	/* Save all registers */

	offset += AMD64_NREG * 8;
	saved_regs_offset = - offset;
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i == AMD64_RBP) {
			/* RAX is already saved */
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, rbp_offset, 8);
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * 8), AMD64_RAX, 8);
		} else if (i != AMD64_R11) {
			amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * 8), i, 8);
		} else {
			
			/* We have to save R11 right at the start of
			   the trampoline code because it's used as a
			   scratch register */
			amd64_mov_membase_reg (r11_save_code, AMD64_RSP, saved_regs_offset + orig_rsp_to_rbp_offset + (i * 8), i, 8);
			g_assert (r11_save_code == after_r11_save_code);
		}
	}
	offset += 8 * 8;
	saved_fpregs_offset = - offset;
	for (i = 0; i < 8; ++i)
		amd64_movsd_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * 8), i);

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT &&
			tramp_type != MONO_TRAMPOLINE_RGCTX_LAZY_FETCH) {
		/* Obtain the trampoline argument which is encoded in the instruction stream */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, tramp_offset, 8);
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
		amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, AMD64_R11, 8);
	} else {
		amd64_mov_membase_reg (code, AMD64_RBP, arg_offset, MONO_ARCH_VTABLE_REG, 8);
	}

	/* Save LMF begin */

	offset += sizeof (MonoLMF);
	lmf_offset = - offset;

	/* Save ip */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, 8);
	else
		amd64_mov_reg_imm (code, AMD64_R11, 0);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rip), AMD64_R11, 8);
	/* Save fp */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, framesize, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), AMD64_R11, 8);
	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, 8);
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, 8);
	/* Save method */
	if (tramp_type == MONO_TRAMPOLINE_GENERIC || tramp_type == MONO_TRAMPOLINE_JUMP) {
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, arg_offset, 8);
		amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), AMD64_R11, 8);
	} else {
		amd64_mov_membase_imm (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), 0, 8);
	}
	/* Save callee saved regs */
#ifdef PLATFORM_WIN32
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rdi), AMD64_RDI, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsi), AMD64_RSI, 8);
#endif
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), AMD64_RBX, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), AMD64_R12, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), AMD64_R13, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), AMD64_R14, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), AMD64_R15, 8);

	amd64_mov_reg_imm (code, AMD64_R11, mono_get_lmf_addr);
	amd64_call_reg (code, AMD64_R11);

	/* Save lmf_addr */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), AMD64_RAX, 8);
	/* Save previous_lmf */
	/* Set the lowest bit to 1 to signal that this LMF has the ip field set */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, 8);
	amd64_alu_reg_imm_size (code, X86_ADD, AMD64_R11, 1, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, 8);
	/* Set new lmf */
	amd64_lea_membase (code, AMD64_R11, AMD64_RBP, lmf_offset);
	amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, 8);

	/* Save LMF end */

	/* Arg1 is the pointer to the saved registers */
	amd64_lea_membase (code, AMD64_ARG_REG1, AMD64_RBP, saved_regs_offset);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_ARG_REG2, AMD64_RBP, 8, 8);
	else
		amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_RBP, arg_offset, 8);

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_ARG_REG4, AMD64_RBP, tramp_offset, 8);

	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	amd64_mov_reg_imm (code, AMD64_RAX, tramp);
	amd64_call_reg (code, AMD64_RAX);

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	amd64_mov_membase_reg (code, AMD64_RBP, res_offset, AMD64_RAX, 8);
	amd64_mov_reg_imm (code, AMD64_RAX, (guint8*)mono_thread_force_interruption_checkpoint);
	amd64_call_reg (code, AMD64_RAX);
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RBP, res_offset, 8);	

	/* Restore LMF */

	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), 8);
	amd64_alu_reg_imm_size (code, X86_SUB, AMD64_RCX, 1, 8);
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), 8);
	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, 8);

	/* Restore argument registers, r10 (needed to pass rgctx to
	   static shared generic methods) and r11 (imt register for
	   interface calls). */
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_ARGUMENT_REG (i) || i == AMD64_R10 || i == AMD64_R11)
			amd64_mov_reg_membase (code, i, AMD64_RBP, saved_regs_offset + (i * 8), 8);

	for (i = 0; i < 8; ++i)
		amd64_movsd_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * 8));

	/* Restore stack */
	amd64_leave (code);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT ||
			tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT ||
			tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		amd64_ret (code);
	else
		/* call the compiled method */
		amd64_jump_reg (code, X86_EAX);

	g_assert ((code - buf) <= 524);

	mono_arch_flush_icache (buf, code - buf);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = code = mono_global_codeman_reserve (16);
		x86_ret (code);
	}

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	int size;

	tramp = mono_get_trampoline_code (tramp_type);

	if ((((guint64)arg1) >> 32) == 0)
		size = 5 + 1 + 4;
	else
		size = 5 + 1 + 8;

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve_align (domain->code_mp, size, 1);
	mono_domain_unlock (domain);

	amd64_call_code (code, tramp);
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

	return buf;
}	

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot)
{
	guint8 *tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_RGCTX_LAZY_FETCH);
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	int tramp_size;
	int depth, index;
	int i;

	g_assert (tramp);

	index = slot;
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 32 + 8 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	/* load rgctx ptr from vtable */
	amd64_mov_reg_membase (buf, AMD64_RAX, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoVTable, runtime_generic_context), 8);
	/* is the rgctx ptr null? */
	amd64_test_reg_reg (buf, AMD64_RAX, AMD64_RAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [0] = buf;
	amd64_branch8 (buf, X86_CC_Z, -1, 1);

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		amd64_mov_reg_membase (buf, AMD64_RAX, AMD64_RAX, 0, 8);
		/* is the ptr null? */
		amd64_test_reg_reg (buf, AMD64_RAX, AMD64_RAX);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [i + 1] = buf;
		amd64_branch8 (buf, X86_CC_Z, -1, 1);
	}

	/* fetch slot */
	amd64_mov_reg_membase (buf, AMD64_RAX, AMD64_RAX, sizeof (gpointer) * (index + 1), 8);
	/* is the slot null? */
	amd64_test_reg_reg (buf, AMD64_RAX, AMD64_RAX);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [depth + 1] = buf;
	amd64_branch8 (buf, X86_CC_Z, -1, 1);
	/* otherwise return */
	amd64_ret (buf);

	for (i = 0; i <= depth + 1; ++i)
		x86_patch (rgctx_null_jumps [i], buf);

	g_free (rgctx_null_jumps);

	/* move the rgctx pointer to the VTABLE register */
	amd64_mov_reg_reg (buf, MONO_ARCH_VTABLE_REG, AMD64_ARG_REG1, 8);
	/* store the slot in RAX */
	amd64_mov_reg_imm (buf, AMD64_RAX, slot);
	/* jump to the actual trampoline */
	amd64_jump_code (buf, tramp);

	mono_arch_flush_icache (code, buf - code);

	g_assert (buf - code <= tramp_size);

	return code;
}

guint32
mono_arch_get_rgctx_lazy_fetch_offset (gpointer *regs)
{
	return (guint32)(gulong)(regs [AMD64_RAX]);
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	amd64_mov_reg_imm (code, AMD64_ARG_REG1, func_arg);
	amd64_mov_reg_imm (code, AMD64_R11, func);

	x86_push_imm (code, (guint64)func_arg);
	amd64_call_reg (code, AMD64_R11);
}
