/*
 * tramp-ppc.c: JIT trampoline code for PowerPC
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *   Carlos Valiente <yo@virutass.net>
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/ppc/ppc-codegen.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-ppc.h"

typedef enum {
	MONO_TRAMPOLINE_GENERIC,
	MONO_TRAMPOLINE_JUMP,
	MONO_TRAMPOLINE_CLASS_INIT
} MonoTrampolineType;

/* adapt to mini later... */
#define mono_jit_share_code (1)

/*
 * Address of the x86 trampoline code.  This is used by the debugger to check
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
	int this_pos = 3;

	if (!m->signature->ret->byref && MONO_TYPE_ISSTRUCT (m->signature->ret))
		this_pos = 4;
	    
	start = code = g_malloc (20);

	ppc_load (code, ppc_r0, addr);
	ppc_mtctr (code, ppc_r0);
	ppc_addi (code, this_pos, this_pos, sizeof (MonoObject));
	ppc_bcctr (code, 20, 0);
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 20);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

/* Stack size for trampoline function 
 * PPC_MINIMAL_STACK_SIZE + 16 (args + alignment to ppc_magic_trampoline)
 * + MonoLMF + 14 fp regs + 13 gregs + alignment
 * #define STACK (PPC_MINIMAL_STACK_SIZE + 4 * sizeof (gulong) + sizeof (MonoLMF) + 14 * sizeof (double) + 13 * (sizeof (gulong)))
 * STACK would be 444 for 32 bit darwin
 */
#define STACK (448)

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

/**
 * ppc_magic_trampoline:
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
ppc_magic_trampoline (MonoMethod *method, guint32 *code, char *sp)
{
	char *o = NULL;
	gpointer addr;
        MonoJitInfo *ji, *target_ji;
	int reg, offset = 0;

	addr = mono_compile_method(method);
	/*g_print ("method code at %p for %s:%s\n", addr, method->klass->name, method->name);*/
	g_assert(addr);

	if (!code){
		return addr;
	}

	/* We can't trampoline across domains */
	ji = mono_jit_info_table_find (mono_domain_get (), code);
	target_ji = mono_jit_info_table_find (mono_domain_get (), addr);
	if (!mono_method_same_domain (ji, target_ji))
		return addr;

	/* Locate the address of the method-specific trampoline. The call using
	the vtable slot that took the processing flow to 'arch_create_jit_trampoline' 
	looks something like this:
	
		mtlr rA			; Move rA (a register containing the
					; target address) to LR
		blrl			; Call function at LR
	
	PowerPC instructions are 32-bit long, which means that a 32-bit target
	address cannot be encoded as an immediate value (because we already
	have spent some bits to encode the branch instruction!). That's why a
	'b'ranch to the contents of the 'l'ink 'r'egister (with 'l'ink register
	update) is needed, instead of a simpler 'branch immediate'. This
	complicates our purpose here, because 'blrl' overwrites LR, which holds
	the value we're interested in.
	
	Therefore, we need to locate the 'mtlr rA' instruction to know which
	register LR was loaded from, and then retrieve the value from that
	register */
	
	/* This is the 'blrl' instruction */
	--code;
	
	/*
	 * Note that methods are called also with the bl opcode.
	 */
	if (((*code) >> 26) == 18) {
		/*g_print ("direct patching\n");*/
		ppc_patch ((char*)code, addr);
		mono_arch_flush_icache ((char*)code, 4);
		return addr;
	}
	
	/* Sanity check: instruction must be 'blrl' */
	g_assert(*code == 0x4e800021);
	
	/* OK, we're now at the 'blrl' instruction. Now walk backwards
	till we get to a 'mtlr rA' */
	for(; --code;) {
		if((*code & 0x7c0803a6) == 0x7c0803a6) {
			gint16 soff;
			gint reg_offset;
			/* Here we are: we reached the 'mtlr rA'.
			Extract the register from the instruction */
			reg = (*code & 0x03e00000) >> 21;
			--code;
			/* ok, this is a lwz reg, offset (vtreg) 
			 * it is emitted with:
			 * ppc_emit32 (c, (32 << 26) | ((D) << 21) | ((a) << 16) | (guint16)(d))
			 */
			soff = (*code & 0xffff);
			offset = soff;
			reg = (*code >> 16) & 0x1f;
			g_assert (reg != ppc_r1);
			/*g_print ("patching reg is %d\n", reg);*/
			if (reg >= 13) {
				/* saved in the MonoLMF structure */
				reg_offset = STACK - sizeof (MonoLMF) + G_STRUCT_OFFSET (MonoLMF, iregs);
				reg_offset += (reg - 13) * sizeof (gulong);
			} else {
				/* saved in the stack, see frame diagram below */
				reg_offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double)) - (13 * sizeof (gulong));
				reg_offset += reg * sizeof (gulong);
			}
			/* o contains now the value of register reg */
			o = *((char**) (sp + reg_offset));
			break;
		}
	}

	/* this is not done for non-virtual calls, because in that case
	   we won't have an object, but the actual pointer to the 
	   valuetype as the this argument
	 */
	if (method->klass->valuetype)
		addr = get_unbox_trampoline (method, addr);

	o += offset;
	*((gpointer *)o) = addr;
	return addr;
}

static void
ppc_class_init_trampoline (void *vtable, guint32 *code, char *sp)
{
	mono_runtime_class_init (vtable);

#if 0
	/* This is the 'bl' instruction */
	--code;
	
	if (((*code) >> 26) == 18) {
		ppc_ori (code, 0, 0, 0); /* nop */
		mono_arch_flush_icache (code, 4);
		return;
	} else {
		g_assert_not_reached ();
	}
#endif
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
 *  param area for 3 args to ppc_magic_trampoline
 *  -------------------
 *  linkage area
 *  -------------------
 */
static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	static guint8* generic_jump_trampoline = NULL;
	static guint8 *generic_class_init_trampoline = NULL;
	int i, offset;

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

	if(!code) {
		/* Now we'll create in 'buf' the PowerPC trampoline code. This
		 is the trampoline code common to all methods  */
		
		code = buf = g_malloc(512);
		
		ppc_stwu (buf, ppc_r1, -STACK, ppc_r1);

		/* start building the MonoLMF on the stack */
		offset = STACK - sizeof (double) * MONO_SAVED_FREGS;
		for (i = 14; i < 32; i++) {
			ppc_stfd (buf, i, offset, ppc_r1);
			offset += sizeof (double);
		}
		/* 
		 * now the integer registers. r13 is already saved in the trampoline,
		 * and at this point contains the method to compile, so we skip it.
		 */
		offset = STACK - sizeof (MonoLMF) + G_STRUCT_OFFSET (MonoLMF, iregs) + sizeof (gulong);
		ppc_stmw (buf, ppc_r14, ppc_r1, offset);

		/* Now save the rest of the registers below the MonoLMF struct, first 14
		 * fp regs and then the 13 gregs.
		 */
		offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double));
		for (i = 0; i < 14; i++) {
			ppc_stfd (buf, i, offset, ppc_r1);
			offset += sizeof (double);
		}
		offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double)) - (13 * sizeof (gulong));
		for (i = 0; i < 13; i++) {
			ppc_stw (buf, i, offset, ppc_r1);
			offset += sizeof (gulong);
		}
		/* we got here through a jump to the ctr reg, we must save the lr
		 * in the parent frame (we do it here to reduce the size of the
		 * method-specific trampoline)
		 */
		ppc_mflr (buf, ppc_r0);
		ppc_stw (buf, ppc_r0, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);

		/* ok, now we can continue with the MonoLMF setup, mostly untouched 
		 * from emit_prolog in mini-ppc.c
		 */
		ppc_load (buf, ppc_r0, mono_get_lmf_addr);
		ppc_mtlr (buf, ppc_r0);
		ppc_blrl (buf);
		/* we build the MonoLMF structure on the stack - see mini-ppc.h
		 * The pointer to the struct is put in ppc_r11.
		 */
		ppc_addi (buf, ppc_r11, ppc_sp, STACK - sizeof (MonoLMF));
		ppc_stw (buf, ppc_r3, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r11);
		/* new_lmf->previous_lmf = *lmf_addr */
		ppc_lwz (buf, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		ppc_stw (buf, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r11);
		/* *(lmf_addr) = r11 */
		ppc_stw (buf, ppc_r11, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		/* save method info (it's in r13) */
		ppc_stw (buf, ppc_r13, G_STRUCT_OFFSET(MonoLMF, method), ppc_r11);
		ppc_stw (buf, ppc_sp, G_STRUCT_OFFSET(MonoLMF, ebp), ppc_r11);
		/* save the IP (caller ip) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r0, 0);
		} else {
			ppc_lwz (buf, ppc_r0, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);
		}
		ppc_stw (buf, ppc_r0, G_STRUCT_OFFSET(MonoLMF, eip), ppc_r11);

		/*
		 * Now we're ready to call ppc_magic_trampoline ().
		 */
		/* Arg 1: MonoMethod *method. It was put in r13 */
		ppc_mr  (buf, ppc_r3, ppc_r13);
		
		/* Arg 2: code (next address to the instruction that called us) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r4, 0);
		} else {
			ppc_lwz  (buf, ppc_r4, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);
		}
		
		/* Arg 3: stack pointer so that the magic trampoline can access the
		 * registers we saved above
		 */
		ppc_mr   (buf, ppc_r5, ppc_r1);
		
		if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
			ppc_lis  (buf, ppc_r0, (guint32) ppc_class_init_trampoline >> 16);
			ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) ppc_class_init_trampoline & 0xffff);
		} else {
			ppc_lis  (buf, ppc_r0, (guint32) ppc_magic_trampoline >> 16);
			ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) ppc_magic_trampoline & 0xffff);
		}
		ppc_mtlr (buf, ppc_r0);
		ppc_blrl (buf);
		
		/* OK, code address is now on r3. Move it to the counter reg
		 * so it will be ready for the final jump: this is safe since we
		 * won't do any more calls.
		 */
		ppc_mtctr (buf, ppc_r3);

		/*
		 * Now we restore the MonoLMF (see emit_epilogue in mini-ppc.c)
		 * and the rest of the registers, so the method called will see
		 * the same state as before we executed.
		 * The pointer to MonoLMF is in ppc_r11.
		 */
		ppc_addi (buf, ppc_r11, ppc_r1, STACK - sizeof (MonoLMF));
		/* r5 = previous_lmf */
		ppc_lwz (buf, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r11);
		/* r6 = lmf_addr */
		ppc_lwz (buf, ppc_r6, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r11);
		/* *(lmf_addr) = previous_lmf */
		ppc_stw (buf, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r6);
		/* restore iregs: this time include r13 */
		ppc_lmw (buf, ppc_r13, ppc_r11, G_STRUCT_OFFSET(MonoLMF, iregs));
		/* restore fregs */
		for (i = 14; i < 32; i++) {
			ppc_lfd (buf, i, G_STRUCT_OFFSET(MonoLMF, fregs) + ((i-14) * sizeof (gdouble)), ppc_r11);
		}

		/* restore the volatile registers, we skip r1, of course */
		offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double));
		for (i = 0; i < 14; i++) {
			ppc_lfd (buf, i, offset, ppc_r1);
			offset += sizeof (double);
		}
		offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double)) - (13 * sizeof (gulong));
		ppc_lwz (buf, ppc_r0, offset, ppc_r1);
		offset += 2 * sizeof (gulong);
		for (i = 2; i < 13; i++) {
			ppc_lwz (buf, i, offset, ppc_r1);
			offset += sizeof (gulong);
		}

		/* Non-standard function epilogue. Instead of doing a proper
		 * return, we just hump to the compiled code.
		 */
		/* Restore stack pointer and LR and jump to the code */
		ppc_lwz  (buf, ppc_r1,  0, ppc_r1);
		ppc_lwz  (buf, ppc_r11, PPC_RET_ADDR_OFFSET, ppc_r1);
		ppc_mtlr (buf, ppc_r11);
		ppc_bcctr (buf, 20, 0);

		/* Flush instruction cache, since we've generated code */
		mono_arch_flush_icache (code, buf - code);
	
		/* Sanity check */
		g_assert ((buf - code) <= 512);
	}

	switch (tramp_type) {
	case MONO_TRAMPOLINE_GENERIC:
		mono_generic_trampoline_code = code;
		break;
	case MONO_TRAMPOLINE_JUMP:
		generic_jump_trampoline = code;
		break;
	case MONO_TRAMPOLINE_CLASS_INIT:
		generic_class_init_trampoline = code;
		break;
	}

	return code;
}

static MonoJitInfo*
create_specific_tramp (MonoMethod *method, guint8* tramp, MonoDomain *domain) {
	guint8 *code, *buf;
	MonoJitInfo *ji;

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, 32);
	mono_domain_unlock (domain);

	/* Save r13 in the place it will have in the on-stack MonoLMF */
	ppc_stw  (buf, ppc_r13, -(MONO_SAVED_FREGS * 8 + MONO_SAVED_GREGS * sizeof (gpointer)),  ppc_r1);
	
	/* Prepare the jump to the generic trampoline code.*/
	ppc_lis  (buf, ppc_r13, (guint32) tramp >> 16);
	ppc_ori  (buf, ppc_r13, ppc_r13, (guint32) tramp & 0xffff);
	ppc_mtctr (buf, ppc_r13);
	
	/* And finally put 'method' in r13 and fly! */
	ppc_lis  (buf, ppc_r13, (guint32) method >> 16);
	ppc_ori  (buf, ppc_r13, ppc_r13, (guint32) method & 0xffff);
	ppc_bcctr (buf, 20, 0);
	
	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	g_assert ((buf - code) <= 32);

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
	
	tramp = create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	return create_specific_tramp (method, tramp, domain);
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. It also replaces the
 * corresponding vtable entry (see ppc_magic_trampoline).
 *
 * A trampoline consists of two parts: a main fragment, shared by all method
 * trampolines, and some code specific to each method, which hard-codes a
 * reference to that method and then calls the main fragment.
 *
 * The main fragment contains a call to 'ppc_magic_trampoline', which performs
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

	tramp = create_trampoline_code (MONO_TRAMPOLINE_GENERIC);
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

	tramp = create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);

	/* This is the method-specific part of the trampoline. Its purpose is
	to provide the generic part with the MonoMethod *method pointer. We'll
	use r11 to keep that value, for instance. However, the generic part of
	the trampoline relies on r11 having the same value it had before coming
	here, so we must save it before. */
	//code = buf = g_malloc(METHOD_TRAMPOLINE_SIZE);
	mono_domain_lock (vtable->domain);
	code = buf = mono_code_manager_reserve (vtable->domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (vtable->domain);

	ppc_mflr (buf, ppc_r4);
	ppc_stw  (buf, ppc_r4, PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_stwu (buf, ppc_sp, -64, ppc_sp);
	ppc_load (buf, ppc_r3, vtable);
	ppc_load (buf, ppc_r5, 0);

	ppc_load (buf, ppc_r0, ppc_class_init_trampoline);
	ppc_mtlr (buf, ppc_r0);
	ppc_blrl (buf);

	ppc_lwz (buf, ppc_r0, 64 + PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_mtlr (buf, ppc_r0);
	ppc_addic (buf, ppc_sp, ppc_sp, 64);
	ppc_blr (buf);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);

	mono_jit_stats.method_trampolines++;

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
	ppc_break (buf);
	if (notification_address)
		*notification_address = buf;
	ppc_blr (buf);

	return ptr;
}

