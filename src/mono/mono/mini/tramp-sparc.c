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
	int this_pos = 4;

	if (!m->signature->ret->byref && MONO_TYPE_ISSTRUCT (m->signature->ret))
		this_pos = 8;
	    
	start = code = g_malloc (32);

	/* This executes in the context of the caller, hence o0 */
	sparc_add_imm (code, 0, sparc_o0, sizeof (MonoObject), sparc_o0);
	sparc_set (code, addr, sparc_g1);
	sparc_jmpl (code, sparc_g1, sparc_g0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - start) <= 32);

	mono_arch_flush_icache (start, code - start);

	return start;
}

/**
 * sparc_magic_trampoline:
 * @m: the method to translate
 * @code: the address of the call instruction
 *
 * This method is called by the trampoline functions for methods. It calls the
 * JIT compiler to compile the method, then patches the calling instruction so
 * further calls will bypass the trampoline. For virtual methods, it finds the
 * address of the vtable slot and updates it.
 */
static gpointer
sparc_magic_trampoline (MonoMethod *m, guint32 *code)
{
	gpointer addr;

	addr = mono_compile_method (m);
	g_assert (addr);

	/* FIXME: patch calling code and vtable */
	if ((sparc_inst_op (*code) == 0x2) && (sparc_inst_op3 (*code) == 0x38)) {
		/* FIXME: is this allways a vcall ? */
		/* indirect call through a vtable */

		if (m->klass->valuetype)
			addr = get_unbox_trampoline (m, addr);
	}

	return addr;
}

static void
sparc_class_init_trampoline (MonoVTable *vtable, guint32 *code)
{
	mono_runtime_class_init (vtable);

	/* FIXME: patch calling code */
}

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp_addr;
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

	code = buf = g_malloc (256);

	/* FIXME: save lmf etc */

	sparc_save_imm (code, sparc_sp, -200, sparc_sp);

	/* We receive the method address in %r1 */
	sparc_mov_reg_reg (code, sparc_g1, sparc_o0);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp_addr = &sparc_class_init_trampoline;
	else
		tramp_addr = &sparc_magic_trampoline;
	sparc_call_simple (code, tramp_addr - code);
	/* set %o1 to caller address in delay slot */
	sparc_mov_reg_reg (code, sparc_i7, sparc_o1);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		sparc_ret (code);
	else
		sparc_jmpl (code, sparc_o0, sparc_g0, sparc_g0);

	/* restore previous frame in delay slot */
	sparc_restore_simple (code);

	g_assert ((code - buf) <= 256);

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

	/* FIXME: flush icache */

	return buf;
}

#define TRAMPOLINE_SIZE (((SPARC_SET_MAX_SIZE >> 2) * 2) + 2)

static MonoJitInfo*
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type)
{
	MonoJitInfo *ji;
	guint32 *code, *buf, *tramp;

	tramp = create_trampoline_code (tramp_type);

	code = buf = g_malloc (TRAMPOLINE_SIZE * 4);

	/* %l0 is caller saved so we can use it */
	sparc_set (code, tramp, sparc_l0);
	sparc_set (code, arg1, sparc_r1);
	sparc_jmpl (code, sparc_l0, sparc_g0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - buf) <= TRAMPOLINE_SIZE);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = buf;
	ji->code_size = (code - buf) * 4;

	mono_jit_stats.method_trampolines++;

	/* FIXME: flush icache */

	return ji;
}	

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	return create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC);
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

	ji = create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC);
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

	ji = create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT);
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
	//x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	//x86_ret (buf);

	return ptr;
}
