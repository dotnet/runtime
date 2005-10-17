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
#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/metadata/mono-debug-debugger.h>

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
	int this_reg = AMD64_RDI;
	MonoDomain *domain = mono_domain_get ();

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_reg = AMD64_RSI;

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
mono_arch_patch_callsite (guint8 *code, guint8 *addr)
{
	if (((code [-13] == 0x49) && (code [-12] == 0xbb)) || (code [-5] == 0xe8)) {
		if (code [-5] != 0xe8)
			InterlockedExchangePointer ((gpointer*)(code - 11), addr);
		else {
			g_assert ((((guint64)(addr)) >> 32) == 0);
			g_assert ((((guint64)(code)) >> 32) == 0);
			InterlockedExchange ((guint32*)(code - 4), ((gint64)addr - (gint64)code));
		}
	}
	else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		gpointer *got_entry = (gpointer*)((guint8*)code + (*(guint32*)(code - 4)));
		InterlockedExchangePointer (got_entry, addr);
	}
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
	} else if ((code [0] == 0x49) && (code [1] == 0xff)) {
		/* call <REG> */
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
mono_arch_patch_delegate_trampoline (guint8 *code, guint8 *tramp, gssize *regs, guint8 *addr)
{
	guint8 rex = 0;
	guint32 reg;
	guint32 disp;

	if ((code [-3] == 0xff) && (amd64_modrm_reg (code [-2]) == 0x2) && (amd64_modrm_mod (code [-2]) == 0x1)) {
		/* call *[reg+disp8] */
		if (IS_REX (code [-4]))
			rex = code [-4];
		reg = amd64_modrm_rm (code [-2]);
		disp = *(guint8*)(code - 1);
		//printf ("B: [%%r%d+0x%x]\n", reg, disp);
	}
	else {
		int i;

		for (i = -16; i < 0; ++i)
			printf ("%d ", code [i]);
		printf ("\n");
		g_assert_not_reached ();
	}

	reg += amd64_rex_b (rex);

	/* R11 is clobbered by the trampoline code */
	g_assert (reg != AMD64_R11);

	*(gpointer*)(((guint64)(regs [reg])) + disp) = addr;
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp;
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

	/* 
	 * Allocate a new stack frame and transfer the two arguments received on 
	 * the stack to our frame.
	 */
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
	amd64_pop_reg (code, AMD64_R11);

	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, framesize);

	/* 
	 * The method is at offset -8 from the new RBP, so no need to
	 * copy it.
	 */
	offset += 8;
	method_offset = - offset;

	offset += 8;
	tramp_offset = - offset;
	amd64_mov_membase_reg (code, AMD64_RBP, tramp_offset, AMD64_R11, 8);

	/* Save all registers */

	offset += AMD64_NREG * 8;
	saved_regs_offset = - offset;
	for (i = 0; i < AMD64_NREG; ++i)
		amd64_mov_membase_reg (code, AMD64_RBP, saved_regs_offset + (i * 8), i, 8);
	offset += 8 * 8;
	saved_fpregs_offset = - offset;
	for (i = 0; i < 8; ++i)
		amd64_movsd_membase_reg (code, AMD64_RBP, saved_fpregs_offset + (i * 8), i);

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
	/* Save method */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, method_offset, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), AMD64_R11, 8);
	/* Save callee saved regs */
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
	amd64_lea_membase (code, AMD64_RDI, AMD64_RBP, saved_regs_offset);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		amd64_mov_reg_membase (code, AMD64_RSI, AMD64_RBP, 8, 8);
	else
		amd64_mov_reg_imm (code, AMD64_RSI, 0);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_RDX, AMD64_RBP, method_offset, 8);

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, tramp_offset, 8);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp = (guint8*)mono_class_init_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_AOT)
		tramp = (guint8*)mono_aot_trampoline;
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

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
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

#define TRAMPOLINE_SIZE 34

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp, *real_code;
	int size, jump_offset;

	tramp = mono_get_trampoline_code (tramp_type);

	code = buf = g_alloca (TRAMPOLINE_SIZE);

	/* push trampoline address */
	amd64_lea_membase (code, AMD64_R11, AMD64_RIP, -7);
	amd64_push_reg (code, AMD64_R11);

	/* push argument */
	if (amd64_is_imm32 ((gint64)arg1))
		amd64_push_imm (code, (gint64)arg1);
	else {
		amd64_mov_reg_imm (code, AMD64_R11, arg1);
		amd64_push_reg (code, AMD64_R11);
	}

	jump_offset = code - buf;
	amd64_jump_disp (code, 0xffffffff);

	g_assert ((code - buf) <= TRAMPOLINE_SIZE);

	mono_domain_lock (domain);
	/* 
	 * FIXME: Changing the size to code - buf causes strange crashes during
	 * mcs bootstrap.
	 */
	real_code = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE);
	size = code - buf;
	mono_domain_unlock (domain);

	memcpy (real_code, buf, size);

	/* Fix up jump */
	g_assert ((((gint64)tramp) >> 32) == 0);
	code = (guint8*)real_code + jump_offset;
	amd64_jump_disp (code, tramp - code);

	mono_jit_stats.method_trampolines++;

	if (code_len)
		*code_len = size;

	mono_arch_flush_icache (real_code, size);

	return real_code;
}	

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	amd64_mov_reg_imm (code, AMD64_RDI, func_arg);
	amd64_mov_reg_imm (code, AMD64_R11, func);

	x86_push_imm (code, (guint64)func_arg);
	amd64_call_reg (code, AMD64_R11);
}

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = mono_global_codeman_reserve (16);

	x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	x86_ret (buf);

	return ptr;
}
