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

/* Stack size for trampoline function */
#define STACK (144 + 8*8)

/* Method-specific trampoline code framgment size */
#define METHOD_TRAMPOLINE_SIZE 64

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
	char *o, *start;
	gpointer addr;
	int reg, offset = 0;

	addr = mono_compile_method(method);
	/*g_print ("method code at %p for %s:%s\n", addr, method->klass->name, method->name);*/
	g_assert(addr);

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
		ppc_patch (code, addr);
		mono_arch_flush_icache (code, 4);
		return addr;
	}
	
	/* Sanity check: instruction must be 'blrl' */
	g_assert(*code == 0x4e800021);
	
	/* OK, we're now at the 'blrl' instruction. Now walk backwards
	till we get to a 'mtlr rA' */
	for(; --code;) {
		if((*code & 0x7c0803a6) == 0x7c0803a6) {
			gint16 soff;
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
			/*g_print ("patching reg is %d\n", reg);*/
			switch(reg) {
				case 0 : o = *((int *) (sp + STACK - 8));   break;
				case 3 : o = *((int *) (sp + STACK - 12));   break;
				case 4 : o = *((int *) (sp + STACK - 16));   break;
				case 5 : o = *((int *) (sp + STACK - 20));   break;
				case 6 : o = *((int *) (sp + STACK - 24));   break;
				case 7 : o = *((int *) (sp + STACK - 28));   break;
				case 8 : o = *((int *) (sp + STACK - 32));   break;
				case 9 : o = *((int *) (sp + STACK - 36));   break;
				case 10: o = *((int *) (sp + STACK - 40));   break;
				case 11: o = *((int *) (sp + STACK - 44));  break;
				case 12: o = *((int *) (sp + STACK - 48));  break;
				case 13: o = *((int *) (sp + STACK - 52));  break;
				case 14: o = *((int *) (sp + STACK - 56));  break;
				case 15: o = *((int *) (sp + STACK - 60));  break;
				case 16: o = *((int *) (sp + STACK - 64));  break;
				case 17: o = *((int *) (sp + STACK - 68));  break;
				case 18: o = *((int *) (sp + STACK - 72));  break;
				case 19: o = *((int *) (sp + STACK - 76));  break;
				case 20: o = *((int *) (sp + STACK - 80));  break;
				case 21: o = *((int *) (sp + STACK - 84));  break;
				case 22: o = *((int *) (sp + STACK - 88));  break;
				case 23: o = *((int *) (sp + STACK - 92));  break;
				case 24: o = *((int *) (sp + STACK - 96));  break;
				case 25: o = *((int *) (sp + STACK - 100));  break;
				case 26: o = *((int *) (sp + STACK - 104));  break;
				case 27: o = *((int *) (sp + STACK - 108));  break;
				case 28: o = *((int *) (sp + STACK - 112));  break;
				case 29: o = *((int *) (sp + STACK - 116));  break;
				case 30: o = *((int *) (sp + STACK - 120)); break;
				case 31: o = *((int *) (sp + STACK - 4));   break;
				default:
					printf("%s: Unexpected register %d\n",
						__FUNCTION__, reg);
					g_assert_not_reached();
			}
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
	/* Finally, replace the method-specific trampoline code (which called
	the generic trampoline code) with a fragment that calls directly the
	compiled method */
	
	start = o;
#if 1
	/* FIXME: make the patching thread safe */
	ppc_b (o, 0);
	ppc_patch (o - 4, addr);
	/*g_print ("patching at %p to %p\n", o, addr);*/
#else
	ppc_stwu (o, ppc_r1, -16, ppc_r1);
	ppc_mflr (o, ppc_r0);
	ppc_stw  (o, ppc_r31, 12, ppc_r1);
	ppc_stw  (o, ppc_r0,  20, ppc_r1);
	ppc_mr   (o, ppc_r31, ppc_r1);
	
	ppc_lis  (o, ppc_r0, (guint32) addr >> 16);
	ppc_ori  (o, ppc_r0, ppc_r0, (guint32) addr & 0xffff);
	ppc_mtlr (o, ppc_r0);
	ppc_blrl (o);
	
	ppc_lwz  (o, ppc_r11, 0,  ppc_r1);
	ppc_lwz  (o, ppc_r0,  4,  ppc_r11);
	ppc_mtlr (o, ppc_r0);
	ppc_lwz  (o, ppc_r31, -4, ppc_r11);
	ppc_mr   (o, ppc_r1, ppc_r11);
	ppc_blr  (o);
#endif	
	mono_arch_flush_icache (start, o - start);
	g_assert(o - start < METHOD_TRAMPOLINE_SIZE);
	
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
		
		/*-----------------------------------------------------------
		STEP 0: First create a non-standard function prologue with a
		stack size big enough to save our registers:
		
			lr		(We'll be calling functions here, so we
					must save it)
			r0		(See ppc_magic_trampoline)
			r1 (sp)		(Stack pointer - must save)
			r3-r10		Function arguments.
			r11-r31		(See ppc_magic_trampoline)
			method in r11	(See ppc_magic_trampoline)
			
		This prologue is non-standard because r0 is not saved here - it
		was saved in the method-specific trampoline code
		-----------------------------------------------------------*/
		
		ppc_stwu (buf, ppc_r1, -STACK, ppc_r1);
		
		/* Save r0 before modifying it - we will need its contents in
		'ppc_magic_trampoline' */
		ppc_stw  (buf, ppc_r0,  STACK - 8,   ppc_r1);
		
		ppc_stw  (buf, ppc_r31, STACK - 4, ppc_r1);
		ppc_mr   (buf, ppc_r31, ppc_r1);
		
		/* Now save our registers. */
		ppc_stw  (buf, ppc_r3,  STACK - 12,  ppc_r1);
		ppc_stw  (buf, ppc_r4,  STACK - 16,  ppc_r1);
		ppc_stw  (buf, ppc_r5,  STACK - 20,  ppc_r1);
		ppc_stw  (buf, ppc_r6,  STACK - 24,  ppc_r1);
		ppc_stw  (buf, ppc_r7,  STACK - 28,  ppc_r1);
		ppc_stw  (buf, ppc_r8,  STACK - 32,  ppc_r1);
		ppc_stw  (buf, ppc_r9,  STACK - 36,  ppc_r1);
		ppc_stw  (buf, ppc_r10, STACK - 40,  ppc_r1);
		/* STACK - 44 contains r11, which is set in the method-specific
		part of the trampoline (see bellow this 'if' block) */
		ppc_stw  (buf, ppc_r12, STACK - 48,  ppc_r1);
		ppc_stw  (buf, ppc_r13, STACK - 52,  ppc_r1);
		ppc_stw  (buf, ppc_r14, STACK - 56,  ppc_r1);
		ppc_stw  (buf, ppc_r15, STACK - 60,  ppc_r1);
		ppc_stw  (buf, ppc_r16, STACK - 64,  ppc_r1);
		ppc_stw  (buf, ppc_r17, STACK - 68,  ppc_r1);
		ppc_stw  (buf, ppc_r18, STACK - 72,  ppc_r1);
		ppc_stw  (buf, ppc_r19, STACK - 76,  ppc_r1);
		ppc_stw  (buf, ppc_r20, STACK - 80,  ppc_r1);
		ppc_stw  (buf, ppc_r21, STACK - 84,  ppc_r1);
		ppc_stw  (buf, ppc_r22, STACK - 88,  ppc_r1);
		ppc_stw  (buf, ppc_r23, STACK - 92,  ppc_r1);
		ppc_stw  (buf, ppc_r24, STACK - 96,  ppc_r1);
		ppc_stw  (buf, ppc_r25, STACK - 100, ppc_r1);
		ppc_stw  (buf, ppc_r26, STACK - 104, ppc_r1);
		ppc_stw  (buf, ppc_r27, STACK - 108, ppc_r1);
		ppc_stw  (buf, ppc_r28, STACK - 112, ppc_r1);
		ppc_stw  (buf, ppc_r29, STACK - 116, ppc_r1);
		ppc_stw  (buf, ppc_r30, STACK - 120, ppc_r1);
		/* Save 'method' pseudo-parameter - the one passed in r11 */
		ppc_stw  (buf, ppc_r11, STACK - 124, ppc_r1);

		/* Save the FP registers */
		offset = 124 + 4 + 8;
		for (i = ppc_f1; i <= ppc_f8; ++i) {
			ppc_stfd  (buf, i, STACK - offset, ppc_r1);
			offset += 8;
		}

		/*----------------------------------------------------------
		STEP 1: call 'mono_get_lmf_addr()' to get the address of our
		LMF. We'll need to restore it after the call to
		'ppc_magic_trampoline' and before the call to the native
		method.
		----------------------------------------------------------*/
				
		/* Calculate the address and make the call. Keep in mind that
		we're using r0, so we'll have to restore it before calling
		'ppc_magic_trampoline' */
		ppc_lis  (buf, ppc_r0, (guint32) mono_get_lmf_addr >> 16);
		ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) mono_get_lmf_addr & 0xffff);
		ppc_mtlr (buf, ppc_r0);
		ppc_blrl (buf);

		/* XXX Update LMF !!! */
		
		/*----------------------------------------------------------
		STEP 2: call 'ppc_magic_trampoline()', who will compile the
		code and fix the method vtable entry for us
		----------------------------------------------------------*/
				
		/* Set arguments */
		
		/* Arg 1: MonoMethod *method. It was put in r11 by the
		method-specific trampoline code, and then saved before the call
		to mono_get_lmf_addr()'. Restore r11, by the way :-) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r3, 0);
		} else
			ppc_lwz  (buf, ppc_r3,  STACK - 124, ppc_r1);
		ppc_lwz  (buf, ppc_r11, STACK - 44,  ppc_r1);
		
		/* Arg 2: code (next address to the instruction that called us) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r4, 0);
		} else
			ppc_lwz  (buf, ppc_r4, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);
		
		/* Arg 3: stack pointer */
		ppc_mr   (buf, ppc_r5, ppc_r1);
		
		/* Calculate call address, restore r0 and call
		'ppc_magic_trampoline'. Return value will be in r3 */
		if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
			ppc_lis  (buf, ppc_r0, (guint32) ppc_class_init_trampoline >> 16);
			ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) ppc_class_init_trampoline & 0xffff);
		} else {
			ppc_lis  (buf, ppc_r0, (guint32) ppc_magic_trampoline >> 16);
			ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) ppc_magic_trampoline & 0xffff);
		}
		ppc_mtlr (buf, ppc_r0);
		ppc_lwz	 (buf, ppc_r0, STACK - 8,  ppc_r1);
		ppc_blrl (buf);
		
		/* OK, code address is now on r3. Move it to r0, so that we
		can restore r3 and use it from r0 later */
		ppc_mr   (buf, ppc_r0, ppc_r3);
		

		/*----------------------------------------------------------
		STEP 3: Restore the LMF
		----------------------------------------------------------*/
		
		/* XXX Do it !!! */
		
		/*----------------------------------------------------------
		STEP 4: call the compiled method
		----------------------------------------------------------*/
		
		/* Restore registers */

		ppc_lwz  (buf, ppc_r3,  STACK - 12,  ppc_r1);
		ppc_lwz  (buf, ppc_r4,  STACK - 16,  ppc_r1);
		ppc_lwz  (buf, ppc_r5,  STACK - 20,  ppc_r1);
		ppc_lwz  (buf, ppc_r6,  STACK - 24,  ppc_r1);
		ppc_lwz  (buf, ppc_r7,  STACK - 28,  ppc_r1);
		ppc_lwz  (buf, ppc_r8,  STACK - 32,  ppc_r1);
		ppc_lwz  (buf, ppc_r9,  STACK - 36,  ppc_r1);
		ppc_lwz  (buf, ppc_r10, STACK - 40,  ppc_r1);
		
		/* Restore the FP registers */
		offset = 124 + 4 + 8;
		for (i = ppc_f1; i <= ppc_f8; ++i) {
			ppc_lfd  (buf, i, STACK - offset, ppc_r1);
			offset += 8;
		}
		/* We haven't touched any of these, so there's no need to
		restore them */
		/*
		ppc_lwz  (buf, ppc_r14, STACK - 56,  ppc_r1);
		ppc_lwz  (buf, ppc_r15, STACK - 60,  ppc_r1);
		ppc_lwz  (buf, ppc_r16, STACK - 64,  ppc_r1);
		ppc_lwz  (buf, ppc_r17, STACK - 68,  ppc_r1);
		ppc_lwz  (buf, ppc_r18, STACK - 72,  ppc_r1);
		ppc_lwz  (buf, ppc_r19, STACK - 76,  ppc_r1);
		ppc_lwz  (buf, ppc_r20, STACK - 80,  ppc_r1);
		ppc_lwz  (buf, ppc_r21, STACK - 84,  ppc_r1);
		ppc_lwz  (buf, ppc_r22, STACK - 88,  ppc_r1);
		ppc_lwz  (buf, ppc_r23, STACK - 92,  ppc_r1);
		ppc_lwz  (buf, ppc_r24, STACK - 96,  ppc_r1);
		ppc_lwz  (buf, ppc_r25, STACK - 100, ppc_r1);
		ppc_lwz  (buf, ppc_r26, STACK - 104, ppc_r1);
		ppc_lwz  (buf, ppc_r27, STACK - 108, ppc_r1);
		ppc_lwz  (buf, ppc_r28, STACK - 112, ppc_r1);
		ppc_lwz  (buf, ppc_r29, STACK - 116, ppc_r1);
		ppc_lwz  (buf, ppc_r30, STACK - 120, ppc_r1);
		*/

		/* Non-standard function epilogue. Instead of doing a proper
		return, we just call the compiled code, so
		that, when it finishes, the method returns here. */
	
#if 1
		/* Restore stack pointer, r31, LR and jump to the code */
		ppc_lwz  (buf, ppc_r1,  0, ppc_r1);
		ppc_lwz  (buf, ppc_r31, -4, ppc_r1);
		ppc_lwz  (buf, ppc_r11, PPC_RET_ADDR_OFFSET, ppc_r1);
		ppc_mtlr (buf, ppc_r11);
		ppc_mtctr (buf, ppc_r0);
		ppc_bcctr (buf, 20, 0);
#else
		ppc_mtlr (buf, ppc_r0);
		ppc_blrl (buf);
		
		/* Restore stack pointer, r31, LR and return to caller */
		ppc_lwz  (buf, ppc_r11,  0, ppc_r1);
		ppc_lwz  (buf, ppc_r31, -4, ppc_r11);
		ppc_mr   (buf, ppc_r1, ppc_r11);
		ppc_lwz  (buf, ppc_r0, 4, ppc_r1);
		ppc_mtlr (buf, ppc_r0);
		ppc_blr  (buf);	
#endif
		
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

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	void *code = mono_compile_method (method);
	/*g_print ("called jump trampoline for %s:%s\n", method->klass->name, method->name);*/
	return mono_jit_info_table_find (mono_domain_get (), code);
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
	guint8 *code, *buf;
	static guint8 *vc = NULL;
	MonoDomain* domain = mono_domain_get ();

	/* previously created trampoline code */
	if (method->info)
		return method->info;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_arch_create_jit_trampoline (mono_marshal_get_synchronized_wrapper (method));

	vc = create_trampoline_code (MONO_TRAMPOLINE_GENERIC);

	/* This is the method-specific part of the trampoline. Its purpose is
	to provide the generic part with the MonoMethod *method pointer. We'll
	use r11 to keep that value, for instance. However, the generic part of
	the trampoline relies on r11 having the same value it had before coming
	here, so we must save it before. */
	//code = buf = g_malloc(METHOD_TRAMPOLINE_SIZE);
	// FIXME: should pass the domain down tot his function
	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	/* Save r11. There's nothing magic in the '44', its just an arbitrary
	position - see above */
	ppc_stw  (buf, ppc_r11, -44,  ppc_r1);
	
	/* Now save LR - we'll overwrite it now */
	ppc_mflr (buf, ppc_r11);
	ppc_stw  (buf, ppc_r11, PPC_RET_ADDR_OFFSET, ppc_r1);
	
	/* Prepare the jump to the generic trampoline code.*/
	ppc_lis  (buf, ppc_r11, (guint32) vc >> 16);
	ppc_ori  (buf, ppc_r11, ppc_r11, (guint32) vc & 0xffff);
	ppc_mtlr (buf, ppc_r11);
	
	/* And finally put 'method' in r11 and fly! */
	ppc_lis  (buf, ppc_r11, (guint32) method >> 16);
	ppc_ori  (buf, ppc_r11, ppc_r11, (guint32) method & 0xffff);
	ppc_blr  (buf);
	
	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);
	
	/* Store trampoline address */
	method->info = code;

	mono_jit_stats.method_trampolines++;

	return code;
}

#if 0

/**
 * x86_magic_trampoline:
 * @eax: saved x86 register 
 * @ecx: saved x86 register 
 * @edx: saved x86 register 
 * @esi: saved x86 register 
 * @edi: saved x86 register 
 * @ebx: saved x86 register
 * @code: pointer into caller code
 * @method: the method to translate
 *
 * This method is called by the trampoline functions for virtual
 * methods. It inspects the caller code to find the address of the
 * vtable slot, then calls the JIT compiler and writes the address
 * of the compiled method back to the vtable. All virtual methods 
 * are called with: x86_call_membase (inst, basereg, disp). We always
 * use 32 bit displacement to ensure that the length of the call 
 * instruction is 6 bytes. We need to get the value of the basereg 
 * and the constant displacement.
 */
static gpointer
x86_magic_trampoline (int eax, int ecx, int edx, int esi, int edi, 
		      int ebx, guint8 *code, MonoMethod *m)
{
	guint8 reg;
	gint32 disp;
	char *o;
	gpointer addr;

	addr = mono_compile_method (m);
	g_assert (addr);

	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	if ((code [1] != 0xe8) && (code [3] == 0xff) && ((code [4] & 0x18) == 0x10) && ((code [4] >> 6) == 1)) {
		reg = code [4] & 0x07;
		disp = (signed char)code [5];
	} else {
		if ((code [0] == 0xff) && ((code [1] & 0x18) == 0x10) && ((code [1] >> 6) == 2)) {
			reg = code [1] & 0x07;
			disp = *((gint32*)(code + 2));
		} else if ((code [1] == 0xe8)) {
			*((guint32*)(code + 2)) = (guint)addr - ((guint)code + 1) - 5; 
			return addr;
		} else if ((code [4] == 0xff) && (((code [5] >> 6) & 0x3) == 0) && (((code [5] >> 3) & 0x7) == 2)) {
			/*
			 * This is a interface call: should check the above code can't catch it earlier 
			 * 8b 40 30   mov    0x30(%eax),%eax
			 * ff 10      call   *(%eax)
			 */
			disp = 0;
			reg = code [5] & 0x07;
		} else {
			printf ("Invalid trampoline sequence: %x %x %x %x %x %x %x\n", code [0], code [1], code [2], code [3],
				code [4], code [5], code [6]);
			g_assert_not_reached ();
		}
	}

	switch (reg) {
	case X86_EAX:
		o = (gpointer)eax;
		break;
	case X86_EDX:
		o = (gpointer)edx;
		break;
	case X86_ECX:
		o = (gpointer)ecx;
		break;
	case X86_ESI:
		o = (gpointer)esi;
		break;
	case X86_EDI:
		o = (gpointer)edi;
		break;
	case X86_EBX:
		o = (gpointer)ebx;
		break;
	default:
		g_assert_not_reached ();
	}

	o += disp;

	if (m->klass->valuetype) {
		return *((gpointer *)o) = get_unbox_trampoline (m, addr);
	} else {
		return *((gpointer *)o) = addr;
	}
}

/**
 * mono_arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * corresponding vtable entry (see x86_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;

	/* previously created trampoline code */
	if (method->info)
		return method->info;

	if (!mono_generic_trampoline_code) {
		mono_generic_trampoline_code = buf = g_malloc (256);
		/* save caller save regs because we need to do a call */ 
		x86_push_reg (buf, X86_EDX);
		x86_push_reg (buf, X86_EAX);
		x86_push_reg (buf, X86_ECX);

		/* save LMF begin */

		/* save the IP (caller ip) */
		x86_push_membase (buf, X86_ESP, 16);

		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_reg (buf, X86_EBP);

		/* save method info */
		x86_push_membase (buf, X86_ESP, 32);
		/* get the address of lmf for the current thread */
		x86_call_code (buf, mono_get_lmf_addr);
		/* push lmf */
		x86_push_reg (buf, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (buf, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (buf, X86_EAX, 0, X86_ESP, 4);
		/* save LFM end */

		/* push the method info */
		x86_push_membase (buf, X86_ESP, 44);
		/* push the return address onto the stack */
		x86_push_membase (buf, X86_ESP, 52);

		/* save all register values */
		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_membase (buf, X86_ESP, 64); /* EDX */
		x86_push_membase (buf, X86_ESP, 64); /* ECX */
		x86_push_membase (buf, X86_ESP, 64); /* EAX */

		x86_call_code (buf, x86_magic_trampoline);
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 8*4);

		/* restore LMF start */
		/* ebx = previous_lmf */
		x86_pop_reg (buf, X86_EBX);
		/* edi = lmf */
		x86_pop_reg (buf, X86_EDI);
		/* *(lmf) = previous_lmf */
		x86_mov_membase_reg (buf, X86_EDI, 0, X86_EBX, 4);
		/* discard method info */
		x86_pop_reg (buf, X86_ESI);
		/* restore caller saved regs */
		x86_pop_reg (buf, X86_EBP);
		x86_pop_reg (buf, X86_ESI);
		x86_pop_reg (buf, X86_EDI);
		x86_pop_reg (buf, X86_EBX);
		/* discard save IP */
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);		
		/* restore LMF end */

		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 16);

		/* call the compiled method */
		x86_jump_reg (buf, X86_EAX);

		g_assert ((buf - mono_generic_trampoline_code) <= 256);
	}

	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_jump_code (buf, mono_generic_trampoline_code);
	g_assert ((buf - code) <= 16);

	/* store trampoline address */
	method->info = code;

	//mono_jit_stats.method_trampolines++;

	return code;
}

#endif

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

#if 1
	ppc_mflr (buf, ppc_r4);
	ppc_stw  (buf, ppc_r4, PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_stwu (buf, ppc_sp, -32, ppc_sp);
	ppc_load (buf, ppc_r3, vtable);
	ppc_load (buf, ppc_r5, 0);

	ppc_load (buf, ppc_r0, ppc_class_init_trampoline);
	ppc_mtlr (buf, ppc_r0);
	ppc_blrl (buf);

	ppc_lwz (buf, ppc_r0, 32 + PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_mtlr (buf, ppc_r0);
	ppc_addic (buf, ppc_sp, ppc_sp, 32);
	ppc_blr (buf);
#else
	/* Save r11. There's nothing magic in the '44', its just an arbitrary
	position - see above */
	ppc_stw  (buf, ppc_r11, -44,  ppc_r1);
	
	/* Now save LR - we'll overwrite it now */
	ppc_mflr (buf, ppc_r11);
	ppc_stw  (buf, ppc_r11, 4, ppc_r1);
	ppc_stw  (buf, ppc_r11, PPC_RET_ADDR_OFFSET, ppc_r1);
	
	/* Prepare the jump to the generic trampoline code.*/
	ppc_lis  (buf, ppc_r11, (guint32) tramp >> 16);
	ppc_ori  (buf, ppc_r11, ppc_r11, (guint32) tramp & 0xffff);
	ppc_mtlr (buf, ppc_r11);
	
	/* And finally put 'vtable' in r11 and fly! */
	ppc_lis  (buf, ppc_r11, (guint32) vtable >> 16);
	ppc_ori  (buf, ppc_r11, ppc_r11, (guint32) vtable & 0xffff);
	ppc_blrl  (buf);
	ppc_lwz (buf, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_r1);
	ppc_mtlr (buf, ppc_r0);
	ppc_blr (buf);

#endif

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

