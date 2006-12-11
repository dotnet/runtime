/*
 * tramp-mips.c: JIT trampoline code for MIPS
 *
 * Authors:
 *    Mark Mason (mason@broadcom.com)
 *
 * Based on tramp-ppc.c by:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *   Carlos Valiente <yo@virutass.net>
 *
 * (C) 2006 Broadcom
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/mips/mips-codegen.h>

#include "mini.h"
#include "mini-mips.h"

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
	int this_pos = mips_a0;
	MonoDomain *domain = mono_domain_get ();

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = mips_a1;
	    
	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 20);
	mono_domain_unlock (domain);

	mips_load (code, mips_t9, addr);
	mips_addiu (code, this_pos, this_pos, sizeof (MonoObject));
	mips_jr (code, mips_t9);
	mips_nop (code);

	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 20);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

/* Stack size for trampoline function 
 * MIPS_MINIMAL_STACK_SIZE + 16 (args + alignment to mips_magic_trampoline)
 * + MonoLMF + 14 fp regs + 13 gregs + alignment
 * #define STACK (MIPS_MINIMAL_STACK_SIZE + 4 * sizeof (gulong) + sizeof (MonoLMF) + 14 * sizeof (double) + 13 * (sizeof (gulong)))
 * STACK would be 444 for 32 bit darwin
 */

#define STACK (4*4 + 8 + sizeof(MonoLMF) + 32)


/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

/**
 * mips_magic_trampoline:
 * @code: pointer into caller code
 * @method: the method to translate
 * @sp: stack pointer
 *
 * This method is called by the function 'arch_create_jit_trampoline', which in
 * turn is called by the trampoline functions for virtual methods.
 * After having called the JIT compiler to compile the method, it inspects the
 * caller code to find the address of the method-specific part of the
 * trampoline vtable slot for this method, updates it with a fragment that calls
 * the newly compiled code and returns this address of the compiled code to
 * 'arch_create_jit_trampoline' 
 */
static gpointer
mips_magic_trampoline (MonoMethod *method, guint32 *code, char *sp)
{
	char *vtable = NULL;
	gpointer addr;
        MonoJitInfo *ji, *target_ji;
	int reg, offset = 0;
	guint32 base = 0;

	addr = mono_compile_method (method);
	g_assert (addr);

	if (!code)
		return addr;

	/* We can't trampoline across domains */
	ji = mono_jit_info_table_find (mono_domain_get (), code);
	target_ji = mono_jit_info_table_find (mono_domain_get (), addr);
	if (!mono_method_same_domain (ji, target_ji))
		return addr;

#if 0
	g_print ("mips_magic: method code at %p from %p for %s:%s\n",
		 addr, code, method->klass->name, method->name);
#endif
	/* Locate the address of the method-specific trampoline. The call using
	the vtable slot that took the processing flow to 'arch_create_jit_trampoline' 
	looks something like this:

		jal	XXXXYYYY

		lui	t9, XXXX
		addiu	t9, YYYY
		jalr	t9
		nop

	On entry, 'code' points just after one of the above sequences.
	*/
	
	/* The jal case */
	if ((code[-2] >> 26) == 0x03) {
		g_print ("direct patching\n");
		mips_patch ((char*)(code-2), addr);
		return addr;
	}
	
	/* Sanity check: look for the jalr */
	g_assert((code[-2] & 0xfc1f003f) == 0x00000009);

	reg = (code[-2] >> 21) & 0x1f;

	//printf ("mips_magic_trampoline: jalr @ 0x%0x, w/ reg %d\n", code-2, reg);

	/* The lui / addiu / jalr case */
	if ((code [-4] >> 26) == 0x0f && (code [-3] >> 26) == 0x09 && (code [-2] >> 26) == 0) {
		mips_patch ((char*)(code-4), addr);
		return addr;
	}

	//printf ("mips_magic_trampoline: 0x%08x @ 0x%0x\n", *(code-2), code-2);

	/* Probably a vtable lookup */

	/* Walk backwards to find 'lw reg,XX(base)' */
	for(; --code;) {
		guint32 mask = (0x3f << 26) | (0x1f << 16);
		guint32 match = (0x23 << 26) | (reg << 16);
		if((*code & mask) == match) {
			gint16 soff;
			gint reg_offset;

			/* lw reg,XX(base) */
			base = (*code >> 21) & 0x1f;
			soff = (*code & 0xffff);
			if (soff & 0x8000)
				soff |= 0xffff0000;
			offset = soff;
			reg_offset = STACK - sizeof (MonoLMF)
				+ G_STRUCT_OFFSET (MonoLMF, iregs[base]);
			/* o contains now the value of register reg */
			vtable = *((char**) (sp + reg_offset));
#if 0
			g_print ("patching reg is %d, offset %d (vtable %p) @ %p\n",
				 base, offset, vtable, code);
#endif
			break;
		}
	}

	/* this is not done for non-virtual calls, because in that case
	   we won't have an object, but the actual pointer to the 
	   valuetype as the this argument
	 */
	if (method->klass->valuetype && !mono_aot_is_got_entry (code, vtable))
		addr = get_unbox_trampoline (method, addr);

	vtable += offset;
	if (mono_aot_is_got_entry (code, vtable) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable))
		*((gpointer *)vtable) = addr;
	return addr;
}

static void
mips_class_init_trampoline (void *vtable, guint32 *code, char *sp)
{
	//g_print ("mips_class_init: vtable=%p code=%p sp=%p\n", vtable, code, sp);

	mono_runtime_class_init (vtable);

	/* back up to the jal/jalr instruction */
	code -= 2;

	/* Check for jal/jalr -- and NOP it out */
	if ((((*code)&0xfc000000) == 0x0c000000)
	    || (((*code)&0xfc1f003f) == 0x00000009)) {
		mips_nop (code);
		mono_arch_flush_icache (code-1, 4);
		return;
	} else {
		g_assert_not_reached ();
	}
}

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * --------------------
 *  MonoLMF
 *  -------------------
 *  Saved FP registers 0-13
 *  -------------------
 *  Saved general registers 0-12
 *  -------------------
 *  param area for 3 args to mips_magic_trampoline
 *  -------------------
 *  linkage area
 *  -------------------
 */
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	int i, offset, lmf;

	/* Now we'll create in 'buf' the MIPS trampoline code. This
	   is the trampoline code common to all methods  */
		
	code = buf = mono_global_codeman_reserve (512);
		
	/* Allocate the stack frame, and save the return address */
	mips_addiu (code, mips_sp, mips_sp, -STACK);
	mips_sw (code, mips_ra, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);

	/* offset of MonoLMF from sp */
	lmf = STACK - sizeof (MonoLMF);
	for (i = 0; i < MONO_MAX_IREGS; i++)
		mips_sw (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[i]));
	for (i = 0; i < MONO_MAX_FREGS; i++)
		mips_swc1 (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, fregs[i]));

	/* jump to mono_get_lmf_addr here */
	mips_load (code, mips_t9, mono_get_lmf_addr);
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);

	/* v0 now points at the (MonoLMF **) for the current thread */

	/* we build the MonoLMF structure on the stack - see mini-mips.h
	 * The pointer to the struct is put in mips_s0 (new_lmf).
	 */

	mips_addiu (code, mips_t2, mips_sp, lmf);

	/* new_lmf->lmf_addr = lmf_addr -- useful when unwinding */
	mips_sw (code, mips_v0, mips_t2, G_STRUCT_OFFSET(MonoLMF, lmf_addr));

	/* new_lmf->previous_lmf = *lmf_addr */
	mips_lw (code, mips_at, mips_v0, 0);
	mips_sw (code, mips_at, mips_t2, G_STRUCT_OFFSET(MonoLMF, previous_lmf));

	/* *(lmf_addr) = t2 */
	mips_sw (code, mips_t2, mips_v0, 0);

	/* save method info (it was in t8) */
	mips_lw (code, mips_at, mips_t2, G_STRUCT_OFFSET(MonoLMF, iregs[mips_t8]));
	mips_sw (code, mips_at, mips_t2, G_STRUCT_OFFSET(MonoLMF, method));

	mips_sw (code, mips_sp, mips_t2, G_STRUCT_OFFSET(MonoLMF, ebp));

	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		mips_sw (code, mips_zero, mips_t2, G_STRUCT_OFFSET(MonoLMF, eip));
	} else {
		mips_lw (code, mips_at, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);
		mips_sw (code, mips_at, mips_t2, G_STRUCT_OFFSET(MonoLMF, eip));
	}

	/*
	 * Now we're ready to call mips_magic_trampoline ().
	 */

	/* Arg 1: MonoMethod *method. */
	mips_lw (code, mips_a0, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[mips_t8]));
		
	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		mips_move (code, mips_a1, mips_zero);
	} else {
		mips_lw (code, mips_a1, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);
	}
		
	/* Arg 3: stack pointer so that the magic trampoline can access the
	 * registers we saved above
	 */
	mips_move (code, mips_a2, mips_sp);
		
	/* Now go to the trampoline */
	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		mips_load (code, mips_t9, (guint32)mips_class_init_trampoline);
	} else {
		mips_load (code, mips_t9, (guint32)mips_magic_trampoline);
	}
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);
		
	/* Code address is now in v0, move it to at */
	mips_move (code, mips_at, mips_v0);

	/*
	 * Now unwind the MonoLMF
	 */

	/* t0 = current_lmf->previous_lmf */
	mips_lw (code, mips_t0, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, previous_lmf));
	/* t1 = lmf_addr */
	mips_lw (code, mips_t1, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, lmf_addr));
	/* (*lmf_addr) = previous_lmf */
	mips_sw (code, mips_t0, mips_t1, 0);

	/* Restore the callee-saved & argument registers */
	for (i = 0; i < MONO_MAX_IREGS; i++) {
		if ((MONO_ARCH_CALLEE_SAVED_REGS | MONO_ARCH_CALLEE_REGS | MIPS_ARG_REGS) & (1 << i))
		    mips_lw (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[i]));
	}
	/* XXX - Restore the float registers */

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore ra & stack pointer, and jump to the code */

	mips_lw (code, mips_ra, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);
	mips_addiu (code, mips_sp, mips_sp, STACK);
	mips_jr (code, mips_at);
	mips_nop (code);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
	
	/* Sanity check */
	g_assert ((code - buf) <= 512);

	return buf;
}

static MonoJitInfo*
create_specific_tramp (MonoMethod *method, guint8* tramp, MonoDomain *domain) {
	guint8 *code, *buf;
	MonoJitInfo *ji;

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, 32);
	mono_domain_unlock (domain);

	/* Prepare the jump to the generic trampoline code
	 * mono_arch_create_trampoline_code() knows we're putting this in t8
	 */
	mips_load (code, mips_t8, method);
	
	/* Now jump to the generic trampoline code */
	mips_load (code, mips_at, tramp);
	mips_jr (code, mips_at);
	mips_nop (code);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);

	g_assert ((code - buf) <= 32);

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_start = buf;
	ji->code_size = code - buf;

	mono_jit_stats.method_trampolines++;

	return ji;
}

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	guint8 *tramp;
	MonoDomain* domain = mono_domain_get ();
	
	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_JUMP);
	return create_specific_tramp (method, tramp, domain);
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. It also replaces the
 * corresponding vtable entry (see mips_magic_trampoline).
 *
 * A trampoline consists of two parts: a main fragment, shared by all method
 * trampolines, and some code specific to each method, which hard-codes a
 * reference to that method and then calls the main fragment.
 *
 * The main fragment contains a call to 'mips_magic_trampoline', which performs
 * call to the JIT compiler and substitutes the method-specific fragment with
 * some code that directly calls the JIT-compiled method.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *tramp;
	MonoJitInfo *ji;
	MonoDomain* domain = mono_domain_get ();
	gpointer code_start;

	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_GENERIC);
	/* FIXME: should pass the domain down to this function */
	ji = create_specific_tramp (method, tramp, domain);
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
	guint8 *code, *buf, *tramp;

	tramp = mono_get_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);

	/* This is the method-specific part of the trampoline. Its purpose is
	to provide the generic part with the MonoMethod *method pointer. We'll
	use r11 to keep that value, for instance. However, the generic part of
	the trampoline relies on r11 having the same value it had before coming
	here, so we must save it before. */
	mono_domain_lock (vtable->domain);
	code = buf = mono_code_manager_reserve (vtable->domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (vtable->domain);

	//g_print ("mips_class_init_tramp buf=%p tramp=%p\n", buf, tramp);

	mips_addiu (code, mips_sp, mips_sp, -MIPS_MINIMAL_STACK_SIZE);
	mips_sw (code, mips_ra, mips_sp, MIPS_MINIMAL_STACK_SIZE + MIPS_RET_ADDR_OFFSET);

	/* Probably need to save/restore a0-a3 here */

	mips_load (code, mips_a0, vtable);
	mips_move (code, mips_a1, mips_ra);
	mips_move (code, mips_a2, mips_zero);

	mips_load (code, mips_t9, mips_class_init_trampoline);
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);

	mips_lw (code, mips_ra, mips_sp, MIPS_MINIMAL_STACK_SIZE + MIPS_RET_ADDR_OFFSET);
	mips_addiu (code, mips_sp, mips_sp, MIPS_MINIMAL_STACK_SIZE);
	mips_jr (code, mips_ra);
	mips_nop (code);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
		
	/* Sanity check */
	g_assert ((code - buf) <= METHOD_TRAMPOLINE_SIZE);
	mono_jit_stats.method_trampolines++;

	return buf;
}

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (void)
{
#if 0
	guint8 *ptr, *buf;

	codeman = mono_global_codeman_reserve (16);
	mips_break (buf, 0xd0);
	mips_jr (buf, mips_ra);
	mips_nop (buf);
	mono_arch_flush_icache (ptr, buf - ptr);

	return ptr;
#else
	return NULL;
#endif
}

