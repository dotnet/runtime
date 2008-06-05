/*
 * tramp-hppa.c: JIT trampoline code for hppa
 *
 * Copyright (c) 2007 Randolph Chung
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/hppa/hppa-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>

#include "mini.h"
#include "mini-hppa.h"

/* sizeof (MonoLMF) == 320 + HPPA_STACK_LMF_OFFSET + linkage area (64 bytes) 
 * leave some room to spare
 */
#define TRAMP_STACK_SIZE	512
/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE	128
/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

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
	int this_pos = hppa_r26;
	MonoDomain *domain = mono_domain_get ();

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = hppa_r25;
	    
	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 20);
	mono_domain_unlock (domain);

	hppa_set (code, addr, hppa_r1);
	hppa_ldo (code, sizeof (MonoObject), this_pos, this_pos);
	hppa_bv (code, hppa_r0, hppa_r1);
	hppa_nop (code);

	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 20);
	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *p, guint8 *addr)
{
	guint32 *code = (void *)p;
	/* Search for and patch the calling sequence
	 *
	 * Possibilities are:
	 *
	 * CEE_CALL outputs the following sequence:
	 *
	 * ldil L'<addr>, r1
	 * ldo R'<addr>, r1
	 * bb,>=,n r1, 30, .+16
	 * depwi 0, 31, 2, r1
	 * ldw 4(r1), r19
	 * ldw 0(r1), r1
	 * ble 0(sr4,r1)
	 * copy r31, rp
	 * XXXXXXXXXXXXXXXX      <- code points here
	 */

	/* Go back to the branching insn */

	code -= 2;
	/* We can patch the code only if it is a direct call. In some cases
	 * we can reach here via a reg-indirect call. In that case we can't
	 * patch the callsite
	 */
	if ((code[0] >> 26) == 0x39 && /* ble */
	    (code[-4] >> 26) == 0x31 && /* bb */
	    (code[-6] >> 26) == 0x08) /* ldil */ {
		hppa_patch (&code[-6], addr);
		mono_arch_flush_icache (&code[-6], 8);
	} else {
		printf("Can't patch callsite!\n");
	}
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	g_assert_not_reached ();
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code8, gssize *regs)
{
	guint32 *buf = (guint32 *)((unsigned long)code8 & ~3);
	guint32 *code = buf;

	code -= 2;
	if (code[0] == 0x08000240) /* nop - already nullified */
		return;
	g_assert ((code[0] >> 26) == 0x39); /* ble */
	hppa_nop (code);
	hppa_nop (code);

	mono_arch_flush_icache (buf, 8);

}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	g_assert_not_reached ();
}

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * --------------------
 *  MonoLMF
 *  -------------------
 *  incoming argument registers
 *  -------------------
 *  linkage area
 *  -------------------
 */
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	int i, offset;

	code = buf = mono_global_codeman_reserve (1024);

	/* Trampoline is called with "method" in r20 */
	hppa_stw (buf, hppa_r2, -20, hppa_sp);
	hppa_copy (buf, hppa_sp, hppa_r1);
	hppa_ldo (buf, TRAMP_STACK_SIZE, hppa_sp, hppa_sp);

	/* Save the MonoLMF structure on the stack */
	offset = HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, regs);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i)) {
			hppa_stw (buf, i, offset, hppa_r1);
			offset += sizeof(ulong);
		}
	}
	hppa_copy (buf, hppa_r1, hppa_r3);
	hppa_set (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, fregs), hppa_r1);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_FREG (i)) {
			hppa_fstdx (buf, i, hppa_r1, hppa_r3);
			hppa_ldo (buf, sizeof(double), hppa_r1, hppa_r1);
		}
	}
	/* Save the method info stored in r20 */
	hppa_stw (buf, hppa_r20, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, method), hppa_r3);
	hppa_stw (buf, hppa_r3, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, ebp), hppa_sp);
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		hppa_stw (buf, hppa_r0, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, eip), hppa_r3);
	} else {
		hppa_stw (buf, hppa_r2, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, eip), hppa_r3);
	}

	/* Save the incoming arguments too, before they can trashed by the 
	 * call to the magic trampoline
	 */
	offset = HPPA_STACK_LMF_OFFSET + sizeof (MonoLMF);
	for (i = hppa_r23; i <= hppa_r26; i++) {
		hppa_stw (buf, i, offset, hppa_r3);
		offset += sizeof(ulong);
	}
	hppa_stw (buf, hppa_r28, offset, hppa_r3);
	offset += sizeof(ulong);
	offset = ALIGN_TO (offset, sizeof(double));
	hppa_ldo (buf, offset, hppa_r3, hppa_r1);
	for (i = hppa_fr4; i <= hppa_fr7; i++) {
		hppa_fstds (buf, i, 0, hppa_r1);
		hppa_ldo (buf, sizeof(double), hppa_r1, hppa_r1);
	}

	/* Call mono_get_lmf_addr */
	hppa_set (buf, mono_get_lmf_addr, hppa_r1);
	hppa_depi (buf, 0, 31, 2, hppa_r1);
	hppa_ldw (buf, 0, hppa_r1, hppa_r1);
	hppa_ble (buf, 0, hppa_r1);
	hppa_copy (buf, hppa_r31, hppa_r2);

	/* r28 now points at the (MonoLMF **) for the current thread. */

	/* new_lmf->lmf_addr = lmf_addr */
	hppa_stw (buf, hppa_r28, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, lmf_addr), hppa_r3);

	/* new_lmf->previous_lmf = *lmf_addr */
	hppa_ldw (buf, 0, hppa_r28, hppa_r1);
	hppa_stw (buf, hppa_r1, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, previous_lmf), hppa_r3);
	/* *lmf_addr = new_lmf */
	hppa_ldo (buf, HPPA_STACK_LMF_OFFSET, hppa_r3, hppa_r1);
	hppa_stw (buf, hppa_r1, 0, hppa_r28);

	/* Call mono_magic_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8* tramp) */
	hppa_ldo (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, regs), hppa_r3, hppa_r26);
	hppa_ldw (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, method), hppa_r3, hppa_r24);
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		hppa_copy (buf, hppa_r0, hppa_r25);
	else {
		hppa_ldw (buf, -20, hppa_r3, hppa_r25);
		/* clear the lower two (privilege) bits */
		hppa_depi (buf, 0, 31, 2, hppa_r25);
	}
	hppa_copy (buf, hppa_r0, hppa_r23);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		hppa_set (buf, mono_class_init_trampoline, hppa_r1);
	else
		hppa_set (buf, mono_magic_trampoline, hppa_r1);
	
	hppa_depi (buf, 0, 31, 2, hppa_r1);
	hppa_ldw (buf, 0, hppa_r1, hppa_r1);
	hppa_ble (buf, 0, hppa_r1);
	hppa_copy (buf, hppa_r31, hppa_r2);

	/* Code address is now in r28 */

	/* Unwind LMF */
	hppa_ldw (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, previous_lmf), hppa_r3, hppa_r20);
	hppa_ldw (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, lmf_addr), hppa_r3, hppa_r21);
	hppa_stw (buf, hppa_r20, 0, hppa_r21);

	hppa_copy (buf, hppa_r28, hppa_r20);

	/* Restore arguments */
	offset = HPPA_STACK_LMF_OFFSET + sizeof (MonoLMF);
	for (i = hppa_r23; i <= hppa_r26; i++) {
		hppa_ldw (buf, offset, hppa_r3, i);
		offset += sizeof(ulong);
	}
	hppa_ldw (buf, offset, hppa_r3, hppa_r28);
	offset += sizeof(ulong);
	offset = ALIGN_TO (offset, sizeof(double));
	hppa_ldo (buf, offset, hppa_r3, hppa_r1);
	for (i = hppa_fr4; i <= hppa_fr7; i++) {
		hppa_fldds (buf, 0, hppa_r1, i);
		hppa_ldo (buf, sizeof(double), hppa_r1, hppa_r1);
	}

	/* Restore registers */
	hppa_set (buf, HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, fregs), hppa_r1);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_FREG (i)) {
			hppa_flddx (buf, hppa_r1, hppa_r3, i);
			hppa_ldo (buf, sizeof(double), hppa_r1, hppa_r1);
		}
	}

	hppa_copy (buf, hppa_r3, hppa_r1);
	offset = HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, regs);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i)) {
			hppa_ldw (buf, offset, hppa_r1, i);
			offset += sizeof(ulong);
		}
	}
	/* Jump to the compiled code in hppa_r28 */
	hppa_ldw (buf, -20, hppa_r1, hppa_r2);
	hppa_bv (buf, hppa_r0, hppa_r20);
	hppa_ldo (buf, -TRAMP_STACK_SIZE, hppa_sp, hppa_sp);

	g_assert ((buf - code) <= 1024);
	return code;
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
	   to provide the generic part with the MonoMethod *method pointer. */
	mono_domain_lock (vtable->domain);
	code = buf = mono_code_manager_reserve (vtable->domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (vtable->domain);

	hppa_stw (buf, hppa_r2, -20, hppa_sp);
	hppa_copy (buf, hppa_r3, hppa_r1);
	hppa_copy (buf, hppa_sp, hppa_r3);
	hppa_stwm (buf, hppa_r1, 64, hppa_sp);

	/* mono_class_init_trampoline (regs, code, vtable, tramp) */
	hppa_copy (buf, hppa_r0, hppa_r26);
	hppa_copy (buf, hppa_r2, hppa_r25);
	hppa_set (buf, vtable, hppa_r24);
	hppa_copy (buf, hppa_r0, hppa_r23);

	hppa_set (buf, mono_class_init_trampoline, hppa_r1);
	hppa_depi (buf, 0, 31, 2, hppa_r1);
	hppa_ldw (buf, 0, hppa_r1, hppa_r1);
	hppa_ble (buf, 0, hppa_r1);
	hppa_copy (buf, hppa_r31, hppa_r2);

	hppa_ldw (buf, -20, hppa_r3, hppa_r2);
	hppa_ldo (buf, 64, hppa_r3, hppa_sp);
	hppa_bv (buf, hppa_r0, hppa_r2);
	hppa_ldwm (buf, -64, hppa_sp, hppa_r3);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);

	mono_jit_stats.method_trampolines++;

	return code;
}

static MonoJitInfo*
create_specific_tramp (MonoMethod *method, guint8* tramp, MonoDomain *domain) 
{
	guint8 *code, *buf;
	MonoJitInfo *ji;

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, 20);
	mono_domain_unlock (domain);

	/* Call trampoline, with the "method" pointer in r20 */
	hppa_set (buf, tramp, hppa_r1);
	hppa_ldil (buf, hppa_lsel (method), hppa_r20);
	hppa_bv (buf, hppa_r0, hppa_r1);
	hppa_ldo (buf, hppa_rsel (method), hppa_r20, hppa_r20);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	g_assert ((buf - code) <= 20);

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_start = code;
	ji->code_size = buf - code;

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
 * corresponding vtable entry (see mono_magic_trampoline).
 *
 * A trampoline consists of two parts: a main fragment, shared by all method
 * trampolines, and some code specific to each method, which hard-codes a
 * reference to that method and then calls the main fragment.
 *
 * The main fragment contains a call to 'arm_magic_trampoline', which performs
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
	ji = create_specific_tramp (method, tramp, domain);
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 encoded_offset)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return NULL;
}

guint32
mono_arch_get_rgctx_lazy_fetch_offset (gpointer *regs)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return 0;
}
