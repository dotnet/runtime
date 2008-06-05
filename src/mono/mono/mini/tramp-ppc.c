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

#include "mini.h"
#include "mini-ppc.h"

/*
 * Return the instruction to jump from code to target, 0 if not
 * reachable with a single instruction
 */
static guint32
branch_for_target_reachable (guint8 *branch, guint8 *target)
{
	gint diff = target - branch;
	g_assert ((diff & 3) == 0);
	if (diff >= 0) {
		if (diff <= 33554431)
			return (18 << 26) | (diff);
	} else {
		/* diff between 0 and -33554432 */
		if (diff >= -33554432)
			return (18 << 26) | (diff & ~0xfc000000);
	}
	return 0;
}

/*
 * get_unbox_trampoline:
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
	int this_pos = 3;
	guint32 short_branch;
	MonoDomain *domain = mono_domain_get ();

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 4;
	    
	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 20);
	short_branch = branch_for_target_reachable (code + 4, addr);
	if (short_branch)
		mono_code_manager_commit (domain->code_mp, code, 20, 8);
	mono_domain_unlock (domain);

	if (short_branch) {
		ppc_addi (code, this_pos, this_pos, sizeof (MonoObject));
		ppc_emit32 (code, short_branch);
	} else {
		ppc_load (code, ppc_r0, addr);
		ppc_mtctr (code, ppc_r0);
		ppc_addi (code, this_pos, this_pos, sizeof (MonoObject));
		ppc_bcctr (code, 20, 0);
	}
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 20);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	guint32 *code = (guint32*)code_ptr;
	/* This is the 'blrl' instruction */
	--code;
	
	/*
	 * Note that methods are called also with the bl opcode.
	 */
	if (((*code) >> 26) == 18) {
		/*g_print ("direct patching\n");*/
		ppc_patch ((char*)code, addr);
		mono_arch_flush_icache ((char*)code, 4);
		return;
	}
	
	/* Sanity check: instruction must be 'blrl' */
	g_assert(*code == 0x4e800021);

	/* the thunk-less direct call sequence: lis/ori/mtlr/blrl */
	if ((code [-1] >> 26) == 31 && (code [-2] >> 26) == 24 && (code [-3] >> 26) == 15) {
		ppc_patch ((char*)code, addr);
		return;
	}
	g_assert_not_reached ();
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	g_assert_not_reached ();
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	return;
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	g_assert_not_reached ();
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
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	int i, offset;
	gpointer tramp_handler;

	if(!code) {
		/* Now we'll create in 'buf' the PowerPC trampoline code. This
		 is the trampoline code common to all methods  */
		
		code = buf = mono_global_codeman_reserve (512);
		
		ppc_stwu (buf, ppc_r1, -STACK, ppc_r1);

		/* start building the MonoLMF on the stack */
		offset = STACK - sizeof (double) * MONO_SAVED_FREGS;
		for (i = 14; i < 32; i++) {
			ppc_stfd (buf, i, offset, ppc_r1);
			offset += sizeof (double);
		}
		/* 
		 * now the integer registers.
		 */
		offset = STACK - sizeof (MonoLMF) + G_STRUCT_OFFSET (MonoLMF, iregs);
		ppc_stmw (buf, ppc_r13, ppc_r1, offset);

		/* Now save the rest of the registers below the MonoLMF struct, first 14
		 * fp regs and then the 13 gregs.
		 */
		offset = STACK - sizeof (MonoLMF) - (14 * sizeof (double));
		for (i = 0; i < 14; i++) {
			ppc_stfd (buf, i, offset, ppc_r1);
			offset += sizeof (double);
		}
#define GREGS_OFFSET (STACK - sizeof (MonoLMF) - (14 * sizeof (double)) - (13 * sizeof (gulong)))
		offset = GREGS_OFFSET;
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
		/* save method info (it's stored on the stack, so get it first and put it
		 * in r5 as it's the third argument to the function)
		 */
		ppc_lwz (buf, ppc_r5, GREGS_OFFSET, ppc_r1);
		if ((tramp_type == MONO_TRAMPOLINE_GENERIC) || (tramp_type == MONO_TRAMPOLINE_JUMP))
			ppc_stw (buf, ppc_r5, G_STRUCT_OFFSET(MonoLMF, method), ppc_r11);
		ppc_stw (buf, ppc_sp, G_STRUCT_OFFSET(MonoLMF, ebp), ppc_r11);
		/* save the IP (caller ip) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r0, 0);
		} else {
			ppc_lwz (buf, ppc_r0, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);
		}
		ppc_stw (buf, ppc_r0, G_STRUCT_OFFSET(MonoLMF, eip), ppc_r11);

		/*
		 * Now we're ready to call trampoline (gssize *regs, guint8 *code, gpointer value, guint8 *tramp)
		 * Note that the last argument is unused.
		 */
		/* Arg 1: a pointer to the registers */
		ppc_addi (buf, ppc_r3, ppc_r1, GREGS_OFFSET);
		
		/* Arg 2: code (next address to the instruction that called us) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			ppc_li (buf, ppc_r4, 0);
		} else {
			ppc_lwz  (buf, ppc_r4, STACK + PPC_RET_ADDR_OFFSET, ppc_r1);
		}
		
		/* Arg 3: MonoMethod *method. It was put in r5 already above */
		/*ppc_mr  (buf, ppc_r5, ppc_r5);*/

		tramp_handler = mono_get_trampoline_func (tramp_type);
		ppc_lis  (buf, ppc_r0, (guint32) tramp_handler >> 16);
		ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) tramp_handler & 0xffff);
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
		/* restore iregs */
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
		 * return, we just jump to the compiled code.
		 */
		/* Restore stack pointer and LR and jump to the code */
		ppc_lwz  (buf, ppc_r1,  0, ppc_r1);
		ppc_lwz  (buf, ppc_r11, PPC_RET_ADDR_OFFSET, ppc_r1);
		ppc_mtlr (buf, ppc_r11);
		if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
			ppc_blr (buf);
		} else {
			ppc_bcctr (buf, 20, 0);
		}

		/* Flush instruction cache, since we've generated code */
		mono_arch_flush_icache (code, buf - code);
	
		/* Sanity check */
		g_assert ((buf - code) <= 512);
	}

	return code;
}

#define TRAMPOLINE_SIZE 24
gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	guint32 short_branch;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve_align (domain->code_mp, TRAMPOLINE_SIZE, 4);
	short_branch = branch_for_target_reachable (code + 8, tramp);
	if (short_branch)
		mono_code_manager_commit (domain->code_mp, code, TRAMPOLINE_SIZE, 12);
	mono_domain_unlock (domain);

	if (short_branch) {
		ppc_lis  (buf, ppc_r0, (guint32) arg1 >> 16);
		ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) arg1 & 0xffff);
		ppc_emit32 (buf, short_branch);
	} else {
		/* Prepare the jump to the generic trampoline code.*/
		ppc_lis  (buf, ppc_r0, (guint32) tramp >> 16);
		ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) tramp & 0xffff);
		ppc_mtctr (buf, ppc_r0);
	
		/* And finally put 'arg1' in r0 and fly! */
		ppc_lis  (buf, ppc_r0, (guint32) arg1 >> 16);
		ppc_ori  (buf, ppc_r0, ppc_r0, (guint32) arg1 & 0xffff);
		ppc_bcctr (buf, 20, 0);
	}
	
	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	g_assert ((buf - code) <= TRAMPOLINE_SIZE);
	if (code_len)
		*code_len = buf - code;

	return code;
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
