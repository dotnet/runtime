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

typedef enum {
	MONO_TRAMPOLINE_GENERIC,
	MONO_TRAMPOLINE_JUMP,
	MONO_TRAMPOLINE_CLASS_INIT
} MonoTrampolineType;

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8 *mono_generic_trampoline_code = NULL;

/*
 * AMD64 processors maintain icache coherency only for pages which are marked
 * executable, so we have to alloc memory through a code manager.
 */
static CRITICAL_SECTION tramp_codeman_mutex;
static MonoCodeManager *tramp_codeman;

/*
 * get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
static gpointer
get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_reg = AMD64_RDI;
	MonoDomain *domain = mono_domain_get ();

	if (!m->signature->ret->byref && MONO_TYPE_ISSTRUCT (m->signature->ret))
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

/**
 * amd64_magic_trampoline:
 */
static gpointer
amd64_magic_trampoline (long *regs, guint8 *code, MonoMethod *m, guint8* tramp)
{
	gpointer addr;
	gpointer *vtable_slot;

	addr = mono_compile_method (m);
	g_assert (addr);

	//printf ("ENTER: %s\n", mono_method_full_name (m, TRUE));

	/* the method was jumped to */
	if (!code)
		return addr;

	vtable_slot = mono_amd64_get_vcall_slot_addr (code, regs);

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = get_unbox_trampoline (m, addr);

		g_assert (*vtable_slot);

		*vtable_slot = addr;
	}
	else {
		/* Patch calling code */

		if ((code [-13] == 0x49) && (code [-12] == 0xbb)) {
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), addr);

			/* The first part of the condition means an icall without a wrapper */
			if ((!target_ji && m->addr) || mono_method_same_domain (ji, target_ji)) {
				InterlockedExchangePointer ((gpointer*)(code - 11), addr);
			}
		}
		else {
			/* FIXME: handle more cases */

			/* Patch trampoline in case the calling code can't be patched */
			/* FIXME: Make this thread safe */
#if 0
			/* FIXME:  This causes nunit-console to fail */
			amd64_mov_reg_imm (tramp, AMD64_R11, addr);
			amd64_jump_reg (tramp, AMD64_R11);
#endif
		}
	}

	return addr;
}

/**
 * amd64_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type, then patches the caller code so it is not called again.
 */
static void
amd64_class_init_trampoline (long *regs, guint8 *code, MonoVTable *vtable)
{
	mono_runtime_class_init (vtable);

	code -= 3;
	if ((code [0] == 0x49) && (code [1] == 0xff)) {
		if (!mono_running_on_valgrind ()) {
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
		}
	}
	else
		if (code [0] == 0x90 || code [0] == 0xeb || code [0] == 0x66)
			/* Already changed by another thread */
			;
		else {
			printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
			g_assert_not_reached ();
		}
}

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp;
	int i, lmf_offset, offset, method_offset, tramp_offset, saved_regs_offset, saved_fpregs_offset, framesize;
	static guint8* generic_jump_trampoline = NULL;
	static guint8 *generic_class_init_trampoline = NULL;

	switch (tramp_type) {
	case MONO_TRAMPOLINE_GENERIC:
		if (mono_generic_trampoline_code)
			return mono_generic_trampoline_code;
		break;
	case MONO_TRAMPOLINE_JUMP:
		if (generic_jump_trampoline)
			return generic_jump_trampoline;
		break;
	case MONO_TRAMPOLINE_CLASS_INIT:
		if (generic_class_init_trampoline)
			return generic_class_init_trampoline;
		break;
	}

	EnterCriticalSection (&tramp_codeman_mutex);
	code = buf = mono_code_manager_reserve (tramp_codeman, 512);
	LeaveCriticalSection (&tramp_codeman_mutex);

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
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		amd64_mov_reg_imm (code, AMD64_R11, 0);
	else
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, 8, 8);
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rip), AMD64_R11, 8);
	/* Save fp */
	amd64_mov_membase_reg (code, AMD64_RBP, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp), AMD64_RBP, 8);
	/* Save method */
	if (tramp_type == MONO_TRAMPOLINE_GENERIC)
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RBP, method_offset, 8);
	else
		amd64_mov_reg_imm (code, AMD64_R11, 0);
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
	amd64_mov_reg_membase (code, AMD64_RSI, AMD64_RBP, 8, 8);

	/* Arg3 is the method/vtable ptr */
	amd64_mov_reg_membase (code, AMD64_RDX, AMD64_RBP, method_offset, 8);

	/* Arg4 is the trampoline address */
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RBP, tramp_offset, 8);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp = (guint8*)amd64_class_init_trampoline;
	else
		tramp = (guint8*)amd64_magic_trampoline;

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

	switch (tramp_type) {
	case MONO_TRAMPOLINE_GENERIC:
		mono_generic_trampoline_code = buf;
		break;
	case MONO_TRAMPOLINE_JUMP:
		generic_jump_trampoline = buf;
		break;
	case MONO_TRAMPOLINE_CLASS_INIT:
		generic_class_init_trampoline = buf;
		break;
	}

	return buf;
}

#define TRAMPOLINE_SIZE 34

static MonoJitInfo*
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain)
{
	MonoJitInfo *ji;
	guint8 *code, *buf, *tramp;

	tramp = create_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	/* push trampoline address */
	amd64_lea_membase (code, AMD64_R11, AMD64_RIP, -7);
	amd64_push_reg (code, AMD64_R11);

	/* push argument */
	amd64_mov_reg_imm (code, AMD64_R11, arg1);
	amd64_push_reg (code, AMD64_R11);

	/* FIXME: Optimize this */
	amd64_mov_reg_imm (code, AMD64_R11, tramp);
	amd64_jump_reg (code, AMD64_R11);

	g_assert ((code - buf) <= TRAMPOLINE_SIZE);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = buf;
	ji->code_size = code - buf;

	mono_jit_stats.method_trampolines++;

	mono_arch_flush_icache (ji->code_start, ji->code_size);

	return ji;
}	

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji = create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get ());

	ji->method = method;
	return ji;
}

/**
 * mono_arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * corresponding vtable entry (see amd64_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji;
	MonoDomain *domain = mono_domain_get ();

	/* Trampoline are domain specific, so cache only the one used in the root domain */
	if ((domain == mono_get_root_domain ()) && method->info)
		return method->info;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_arch_create_jit_trampoline (mono_marshal_get_synchronized_wrapper (method));

	ji = create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, domain);
	if (domain == mono_get_root_domain ())
		method->info = ji->code_start;
	g_free (ji);

	return ji->code_start;
}

/**
 * mono_arch_create_class_init_trampoline:
 *  @vtable: the type to initialize
 *
 * Creates a trampoline function to run a type initializer. 
 * If the trampoline is called, it calls mono_runtime_class_init with the
 * given vtable, then patches the caller code so it does not get called any
 * more.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_class_init_trampoline (MonoVTable *vtable)
{
	MonoJitInfo *ji;
	gpointer code;

	ji = create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, vtable->domain);
	code = ji->code_start;
	g_free (ji);

	return code;
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

	EnterCriticalSection (&tramp_codeman_mutex);
	ptr = buf = mono_code_manager_reserve (tramp_codeman, 16);
	LeaveCriticalSection (&tramp_codeman_mutex);

	x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	x86_ret (buf);

	return ptr;
}

void
mono_amd64_tramp_init (void)
{
	InitializeCriticalSection (&tramp_codeman_mutex);

	tramp_codeman = mono_code_manager_new ();

	create_trampoline_code (MONO_TRAMPOLINE_GENERIC);
	create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);
}
