/*
 * tramp-sparc.c: JIT trampoline code for Sparc 64
 *
 * Authors:
 *   Mark Crichton (crichton@gimp.org)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/sparc/sparc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-sparc.h"

typedef enum {
	MONO_TRAMPOLINE_GENERIC,
	MONO_TRAMPOLINE_JUMP,
	MONO_TRAMPOLINE_CLASS_INIT
} MonoTrampolineType;

/* adapt to mini later... */
#define mono_jit_share_code (1)

/*
 * Address of the Sparc trampoline code.  This is used by the debugger to check
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
	int this_pos = 4, reg;

	if (!m->signature->ret->byref && MONO_TYPE_ISSTRUCT (m->signature->ret))
		this_pos = 8;
	    
	start = code = g_malloc (36);

	/* This executes in the context of the caller, hence o0 */
	sparc_add_imm (code, 0, sparc_o0, sizeof (MonoObject), sparc_o0);
#ifdef SPARCV9
	reg = sparc_g4;
#else
	reg = sparc_g1;
#endif
	sparc_set (code, addr, reg);
	sparc_jmpl (code, reg, sparc_g0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - start) <= 36);

	mono_arch_flush_icache (start, code - start);

	return start;
}

/**
 * sparc_magic_trampoline:
 * @m: the method to translate
 * @code: the address of the call instruction
 * @fp: address of the stack frame for the caller
 *
 * This method is called by the trampoline functions for methods. It calls the
 * JIT compiler to compile the method, then patches the calling instruction so
 * further calls will bypass the trampoline. For virtual methods, it finds the
 * address of the vtable slot and updates it.
 */
static gpointer
sparc_magic_trampoline (MonoMethod *m, guint32 *code, guint32 *fp)
{
	gpointer addr;
	gpointer *vtable_slot;

	addr = mono_compile_method (m);
	g_assert (addr);

	/*
	 * Check whenever this is a virtual call, and call an unbox trampoline if
	 * needed.
	 */
	if (mono_sparc_is_virtual_call (code)) {
		if (m->klass->valuetype)
			addr = get_unbox_trampoline (m, addr);

		/* Compute address of vtable slot */
		vtable_slot = mono_sparc_get_vcall_slot_addr (code, fp);
		*vtable_slot = addr;
	}
	else {
		/* Patch calling code */
		if (sparc_inst_op (*code) == 0x1) {
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), addr);

			/* The first part of the condition means an icall without a wrapper */
			if ((!target_ji && m->addr) || mono_method_same_domain (ji, target_ji)) {
				sparc_call_simple (code, (guint8*)addr - (guint8*)code);
			}
		}
	}

	return addr;
}

static void
sparc_class_init_trampoline (MonoVTable *vtable, guint32 *code)
{
	mono_runtime_class_init (vtable);

	/* Patch calling code */
	sparc_nop (code);
}

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp_addr;
	guint32 lmf_offset, method_reg, i;
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

	code = buf = g_malloc (512);

	sparc_save_imm (code, sparc_sp, -608, sparc_sp);

#ifdef SPARCV9
	method_reg = sparc_g4;
#else
	method_reg = sparc_g1;
#endif

#ifdef SPARCV9
	/* Save fp regs since they are not preserved by calls */
	for (i = 0; i < 16; i ++)
		sparc_stdf_imm (code, sparc_f0 + (i * 2), sparc_sp, MONO_SPARC_STACK_BIAS + 320 + (i * 8));
#endif	

	/* We receive the method address in %r1, so save it here */
	sparc_sti_imm (code, method_reg, sparc_sp, MONO_SPARC_STACK_BIAS + 200);

	/* Save lmf since compilation can raise exceptions */
	lmf_offset = MONO_SPARC_STACK_BIAS - sizeof (MonoLMF);

	/* Save the data for the parent (managed) frame */

	/* Save ip */
	sparc_sti_imm (code, sparc_i7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ip));
	/* Save sp */
	sparc_sti_imm (code, sparc_fp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
	/* Save fp */
	/* Load previous fp from the saved register window */
	sparc_flushw (code);
	sparc_ldi_imm (code, sparc_fp, MONO_SPARC_STACK_BIAS + (sparc_i6 - 16) * sizeof (gpointer), sparc_o7);
	sparc_sti_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp));
	/* Save method */
	sparc_sti_imm (code, method_reg, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method));

	sparc_set (code, mono_get_lmf_addr, sparc_o7);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
	sparc_nop (code);

	code = mono_sparc_emit_save_lmf (code, lmf_offset);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp_addr = &sparc_class_init_trampoline;
	else
		tramp_addr = &sparc_magic_trampoline;
	sparc_ldi_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 200, sparc_o0);
	/* pass parent frame address as third argument */
	sparc_mov_reg_reg (code, sparc_fp, sparc_o2);
	sparc_set (code, tramp_addr, sparc_o7);
	/* set %o1 to caller address */
	sparc_mov_reg_reg (code, sparc_i7, sparc_o1);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
	sparc_nop (code);

	/* Save result */
	sparc_sti_imm (code, sparc_o0, sparc_sp, MONO_SPARC_STACK_BIAS + 304);

	/* Restore lmf */
	code = mono_sparc_emit_restore_lmf (code, lmf_offset);

	/* Reload result */
	sparc_ldi_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 304, sparc_o0);

#ifdef SPARCV9
	/* Reload fp regs */
	for (i = 0; i < 16; i ++)
		sparc_lddf_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 320 + (i * 8), sparc_f0 + (i * 2));
#endif	

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		sparc_ret (code);
	else
		sparc_jmpl (code, sparc_o0, sparc_g0, sparc_g0);

	/* restore previous frame in delay slot */
	sparc_restore_simple (code);

/*
{
	gpointer addr;

	sparc_save_imm (code, sparc_sp, -608, sparc_sp);
	addr = code;
	sparc_call_simple (code, 16);
	sparc_nop (code);
	sparc_rett_simple (code);
	sparc_nop (code);

	sparc_save_imm (code, sparc_sp, -608, sparc_sp);
	sparc_ta (code, 1);
	tramp_addr = &sparc_magic_trampoline;
	sparc_call_simple (code, tramp_addr - code);
	sparc_nop (code);
	sparc_rett_simple (code);
	sparc_nop (code);
}
*/

	g_assert ((code - buf) <= 512);

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

	mono_arch_flush_icache (buf, code - buf);

	return buf;
}

#define TRAMPOLINE_SIZE (((SPARC_SET_MAX_SIZE >> 2) * 2) + 2)

static MonoJitInfo*
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain)
{
	MonoJitInfo *ji;
	guint32 *code, *buf, *tramp;

	tramp = create_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE * 4);
	mono_domain_unlock (domain);

	/* We have to use g5 here because there is no other free register */
	sparc_set (code, tramp, sparc_g5);
#ifdef SPARCV9
	sparc_set (code, arg1, sparc_g4);
#else
	sparc_set (code, arg1, sparc_g1);
#endif
	sparc_jmpl (code, sparc_g5, sparc_g0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - buf) <= TRAMPOLINE_SIZE);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = buf;
	ji->code_size = (code - buf) * 4;

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
 * corresponding vtable entry (see sparc_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji;

	/* previously created trampoline code */
	if (method->info)
		return method->info;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_arch_create_jit_trampoline (mono_marshal_get_synchronized_wrapper (method));

	ji = create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, mono_domain_get ());
	method->info = ji->code_start;
	g_free (ji);

	return method->info;
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

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = g_malloc0 (16);
	if (notification_address)
		*notification_address = buf;

	g_assert_not_reached ();

	return ptr;
}
