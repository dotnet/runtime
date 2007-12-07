/*
 * tramp-x86.c: JIT trampoline code for x86
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
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
	int this_reg = AMD64_ARG_REG1;

	MonoDomain *domain = mono_domain_get ();

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_reg = AMD64_ARG_REG2;

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

void
mono_arch_patch_callsite (guint8 *orig_code, guint8 *addr)
{
	guint8 *code;
	guint8 buf [16];
	gboolean can_write = mono_breakpoint_clean_code (orig_code - 14, buf, sizeof (buf));

	code = buf + 14;

	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8) {
			if (can_write)
				InterlockedExchangePointer ((gpointer*)(orig_code - 11), addr);
		} else {
			g_assert ((((guint64)(addr)) >> 32) == 0);
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
	guint8 *buf, *code, *tramp, *br [2];
	int i, lmf_offset, offset, method_offset, tramp_offset, saved_regs_offset, saved_fpregs_offset, framesize;
	gboolean has_caller;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = mono_global_codeman_reserve (512);

	framesize = 512 + sizeof (MonoLMF);
	framesize = (framesize + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

	offset = 0;

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		/* Pop the return address off the stack */
		amd64_pop_reg (code, AMD64_R11);
	}

	/* 
	 * Allocate a new stack frame
	 */
	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		offset += 8;
		tramp_offset = - offset;

		offset += 8;
		method_offset = - offset;

		/* Compute the trampoline address from the return address */
		/* 5 = length of amd64_call_membase () */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_R11, 5);
		amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, 8);
	}

	/* Save all registers */

	offset += AMD64_NREG * 8;
	saved_regs_offset = - offset;
	for (i = 0; i < AMD64_NREG; ++i)
		amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * 8), i, 8);
	offset += 8 * 8;
	saved_fpregs_offset = - offset;
	for (i = 0; i < 8; ++i)
		amd64_movsd_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * 8), i);

	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
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
		amd64_mov_membase_reg (code, AMD64_RBP, method_offset, AMD64_R11, 8);
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
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp), AMD64_R11, 8);
	/* Save sp */
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, 8);
	amd64_alu_reg_imm (code, X86_ADD, AMD64_R11, framesize + 16);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_R11, 8);
	if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		/* Save method */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, method_offset, 8);
		amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), AMD64_R11, 8);
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
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, 8);
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

	/* Arg3 is the method ptr / dummy */
	if (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
		amd64_mov_reg_imm (code, AMD64_ARG_REG3, 0);
	else
		amd64_mov_reg_membase (code, AMD64_ARG_REG3, AMD64_RBP, method_offset, 8);

	/* Arg4 is the trampoline address */
	if (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
		amd64_mov_reg_imm (code, AMD64_ARG_REG4, 0);
	else
		amd64_mov_reg_membase (code, AMD64_ARG_REG4, AMD64_RBP, tramp_offset, 8);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp = (guint8*)mono_class_init_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
		tramp = (guint8*)mono_generic_class_init_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_AOT)
		tramp = (guint8*)mono_aot_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_AOT_PLT)
		tramp = (guint8*)mono_aot_plt_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_DELEGATE)
		tramp = (guint8*)mono_delegate_trampoline;
	else
		tramp = (guint8*)mono_magic_trampoline;

	amd64_mov_reg_imm (code, AMD64_RAX, tramp);
	amd64_call_reg (code, AMD64_RAX);

	/* Restore LMF */

	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), 8);
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), 8);
	amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, 8);

	/* Restore argument registers */
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_ARGUMENT_REG (i))
			amd64_mov_reg_membase (code, i, AMD64_RBP, saved_regs_offset + (i * 8), 8);

	for (i = 0; i < 8; ++i)
		amd64_movsd_reg_membase (code, i, AMD64_RBP, saved_fpregs_offset + (i * 8));

	/* Restore stack */
	amd64_leave (code);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT || tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
		amd64_ret (code);
	else
		/* call the compiled method */
		amd64_jump_reg (code, X86_EAX);

	g_assert ((code - buf) <= 512);

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

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (void)
{
	guint8 *buf, *code;

	code = buf = mono_global_codeman_reserve (2);
	x86_breakpoint (buf);
	x86_ret (buf);
	return code;
}
