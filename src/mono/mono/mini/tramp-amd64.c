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
	MONO_TRAMPOLINE_CLASS_INIT,
	MONO_TRAMPOLINE_AOT
} MonoTrampolineType;

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8 *mono_generic_trampoline_code = NULL;

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

/**
 * amd64_magic_trampoline:
 */
static gpointer
amd64_magic_trampoline (long *regs, guint8 *code, MonoMethod *m, guint8* tramp)
{
	gpointer addr;
	gpointer *vtable_slot, *method_ptr;

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

		if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
			*vtable_slot = addr;
	}
	else {
		/* Patch calling code */

		if (((code [-13] == 0x49) && (code [-12] == 0xbb)) ||
			(code [-5] == 0xe8)) {
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), addr);

			if (mono_method_same_domain (ji, target_ji)) {
				if (code [-5] != 0xe8)
					InterlockedExchangePointer ((gpointer*)(code - 11), addr);
				else {
					g_assert ((((guint64)(addr)) >> 32) == 0);
					g_assert ((((guint64)(code)) >> 32) == 0);
					InterlockedExchange ((guint32*)(code - 4), ((gint64)addr - (gint64)code));
				}
			}
		}
		else if ((code [-7] == 0x41) && (code [-6] == 0xff) && (code [-5] == 0x15)) {
			/* call *<OFFSET>(%rip) */
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), addr);

			if (mono_method_same_domain (ji, target_ji)) {
				gpointer *got_entry = (gpointer*)((guint8*)code + (*(guint32*)(code - 4)));
				InterlockedExchangePointer (got_entry, addr);
			}
		}
		else if ((method_ptr = mono_amd64_get_delegate_method_ptr_addr (code, regs))) {
			/* This is a call inside a delegate wrapper to the target method */
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), addr);

			if (ji && (ji->method->wrapper_type == MONO_WRAPPER_DELEGATE_INVOKE) && mono_method_same_domain (ji, target_ji)) {
				InterlockedExchangePointer (method_ptr, addr);
			}
		}
	}

	return addr;
}

/*
 * amd64_aot_trampoline:
 *
 *   This trampoline handles calls made from AOT code. We try to bypass the 
 * normal JIT compilation logic to avoid loading the metadata for the method.
 */
static gpointer
amd64_aot_trampoline (long *regs, guint8 *code, guint8 *token_info, 
					  guint8* tramp)
{
	MonoImage *image;
	guint32 token;
	MonoMethod *method;
	gpointer addr;
	gpointer *vtable_slot;

	image = *(gpointer*)token_info;
	token_info += sizeof (gpointer);
	token = *(guint32*)token_info;

	/* Later we could avoid allocating the MonoMethod */
	method = mono_get_method (image, token, NULL);
	g_assert (method);

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);

	addr = mono_compile_method (method);
	g_assert (addr);

	vtable_slot = mono_amd64_get_vcall_slot_addr (code, regs);
	g_assert (vtable_slot);

	if (method->klass->valuetype)
		addr = get_unbox_trampoline (method, addr);

	if (mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
		*vtable_slot = addr;

	return addr;
}

/**
 * amd64_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type, then patches the caller code so it is not called again.
 */
static void
amd64_class_init_trampoline (long *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
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
	} else if (code [-2] == 0xe8) {
		guint8 *buf = code - 2;

		buf [0] = 0x66;
		buf [1] = 0x66;
		buf [2] = 0x90;
		buf [3] = 0x66;
		buf [4] = 0x90;
	} else if (code [0] == 0x90 || code [0] == 0xeb || code [0] == 0x66)
		/* Already changed by another thread */
		;
	else if ((code [-4] == 0x41) && (code [-3] == 0xff) && (code [-2] == 0x15))
		/* call *<OFFSET>(%rip) */
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
	static guint8 *trampoline_code [16];

	if (trampoline_code [tramp_type])
		return trampoline_code [tramp_type];

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
	else if (tramp_type == MONO_TRAMPOLINE_AOT)
		tramp = (guint8*)amd64_aot_trampoline;
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

	if (tramp_type == MONO_TRAMPOLINE_GENERIC)
		mono_generic_trampoline_code = buf;
	trampoline_code [tramp_type] = buf;

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
	gpointer code_start;

	ji = create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, mono_domain_get ());
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
}

gpointer
mono_arch_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	gpointer code_start;
	guint8 *buf, *start;

	mono_domain_lock (domain);
	buf = start = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	*(gpointer*)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)buf = token;

	ji = create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain);
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
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

	ptr = buf = mono_global_codeman_reserve (16);

	x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	x86_ret (buf);

	return ptr;
}

void
mono_amd64_tramp_init (void)
{
	create_trampoline_code (MONO_TRAMPOLINE_GENERIC);
	create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);
}
