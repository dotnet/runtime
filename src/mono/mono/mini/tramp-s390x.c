/**
 * \file
 * Function    - JIT trampoline code for S/390.
 *
 * Name	       - Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com)
 *
 * Date        - January, 2004
 *
 * Derivation  - From exceptions-x86 & exceptions-ppc
 * 	         Paolo Molaro (lupus@ximian.com)
 * 		 Dietmar Maurer (dietmar@ximian.com)
 *
 * Copyright   - 2001 Ximian, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 */

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/

#define LMFReg	s390_r13

/*
 * Method-specific trampoline code fragment sizes		    
 */
#define SPECIFIC_TRAMPOLINE_SIZE	96

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/s390x/s390x-codegen.h>

#include "mini.h"
#include "mini-s390x.h"
#include "mini-runtime.h"
#include "support-s390x.h"
#include "jit-icalls.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

typedef struct {
	guint8	stk[S390_MINIMAL_STACK_SIZE];	/* Standard s390x stack	*/
	guint64	saveFn;				/* Call address		*/
	struct MonoLMF  LMF;			/* LMF			*/
} trampStack_t;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/


/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_unbox_trampoline                    */
/*                                                                  */
/* Function	- Return a pointer to a trampoline which does the   */
/*		  unboxing before calling the method.		    */
/*                                                                  */
/*                When value type methods are called through the    */
/*		  vtable we need to unbox the 'this' argument.	    */
/*		                               		 	    */
/* Parameters   - method - Methd pointer			    */
/*		  addr   - Pointer to native code for method	    */
/*		                               		 	    */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *method, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = s390_r2;
	MonoDomain *domain = mono_domain_get ();
	char trampName[128];

	start = code = mono_domain_code_reserve (domain, 28);

	S390_SET  (code, s390_r1, addr);
	s390_aghi (code, this_pos, sizeof(MonoObject));
	s390_br   (code, s390_r1);

	g_assert ((code - start) <= 28);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, method));

	snprintf(trampName, sizeof(trampName), "%s_unbox_trampoline", method->name);

	mono_tramp_info_register (mono_tramp_info_create (trampName, start, code - start, NULL, NULL), domain);

	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_patch_callsite                          */
/*                                                                  */
/* Function	- Patch a non-virtual callsite so it calls @addr.   */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	gint32 displace;
	unsigned short opcode;

	opcode = *((unsigned short *) (orig_code - 2));
	if (opcode == 0x0dee) {
		/* This should be a 'iihf/iilf' sequence */
		S390_EMIT_CALL((orig_code - 14), addr);
		mono_arch_flush_icache (orig_code - 14, 12);
	} else {
		/* This is the 'brasl' instruction */
		orig_code    -= 4;
		displace = ((gssize) addr - (gssize) (orig_code - 2)) / 2;
		s390_patch_rel (orig_code, displace);
		mono_arch_flush_icache (orig_code, 4);
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_patch_plt_entry.                        */
/*                                                                  */
/* Function	- Patch a PLT entry - unused as yet.                */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_trampoline_code                  */
/*                                                                  */
/* Function	- Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*------------------------------------------------------------------*/

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	char *tramp_name;
	guint8 *buf, *tramp, *code;
	int i, offset, has_caller;
	short *o[1];
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	g_assert (!aot);

	/* Now we'll create in 'buf' the S/390 trampoline code. This
	   is the trampoline code common to all methods  */
		
	code = buf = mono_global_codeman_reserve(512);
		
	if (tramp_type == MONO_TRAMPOLINE_JUMP) 
		has_caller = 0;
	else
		has_caller = 1;

	/*-----------------------------------------------------------
	  STEP 0: First create a non-standard function prologue with a
	  stack size big enough to save our registers.
	  -----------------------------------------------------------*/
		
	s390_stmg (buf, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lgr  (buf, s390_r11, s390_r15);
	s390_aghi (buf, STK_BASE, -sizeof(trampStack_t));
	s390_stg  (buf, s390_r11, 0, STK_BASE, 0);

	/*---------------------------------------------------------------*/
	/* we build the MonoLMF structure on the stack - see mini-s390.h */
	/* Keep in sync with the code in mono_arch_emit_prolog 		 */
	/*---------------------------------------------------------------*/
											
	s390_lgr   (buf, LMFReg, STK_BASE);
	s390_aghi  (buf, LMFReg, G_STRUCT_OFFSET(trampStack_t, LMF));
											
	/*---------------------------------------------------------------*/	
	/* Save general and floating point registers in LMF		 */	
	/*---------------------------------------------------------------*/	
	s390_stmg (buf, s390_r0, s390_r1, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[0]));
	s390_stmg (buf, s390_r2, s390_r5, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[2]));
	s390_mvc  (buf, 10*sizeof(gulong), LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[6]),
		   s390_r11, S390_REG_SAVE_OFFSET);

	offset = G_STRUCT_OFFSET(MonoLMF, fregs[0]);
	for (i = s390_f0; i <= s390_f15; ++i) {
		s390_std  (buf, i, 0, LMFReg, offset);
		offset += sizeof(gdouble);
	}

	/*----------------------------------------------------------
	  STEP 1: call 'mono_get_lmf_addr()' to get the address of our
	  LMF. We'll need to restore it after the call to
	  's390_magic_trampoline' and before the call to the native
	  method.
	  ----------------------------------------------------------*/
				
	S390_SET  (buf, s390_r1, mono_get_lmf_addr);
	s390_basr (buf, s390_r14, s390_r1);
											
	/*---------------------------------------------------------------*/	
	/* Set lmf.lmf_addr = jit_tls->lmf				 */	
	/*---------------------------------------------------------------*/	
	s390_stg   (buf, s390_r2, 0, LMFReg, 				
			    G_STRUCT_OFFSET(MonoLMF, lmf_addr));			
											
	/*---------------------------------------------------------------*/	
	/* Get current lmf						 */	
	/*---------------------------------------------------------------*/	
	s390_lg    (buf, s390_r0, 0, s390_r2, 0);				
											
	/*---------------------------------------------------------------*/	
	/* Set our lmf as the current lmf				 */	
	/*---------------------------------------------------------------*/	
	s390_stg   (buf, LMFReg, 0, s390_r2, 0);				
											
	/*---------------------------------------------------------------*/	
	/* Have our lmf.previous_lmf point to the last lmf		 */	
	/*---------------------------------------------------------------*/	
	s390_stg   (buf, s390_r0, 0, LMFReg, 				
			    G_STRUCT_OFFSET(MonoLMF, previous_lmf));			
											
	/*---------------------------------------------------------------*/	
	/* save method info						 */	
	/*---------------------------------------------------------------*/	
	s390_lg    (buf, s390_r1, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[1]));
	s390_stg   (buf, s390_r1, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, method));				
									
	/*---------------------------------------------------------------*/	
	/* save the current SP						 */	
	/*---------------------------------------------------------------*/	
	s390_lg    (buf, s390_r1, 0, STK_BASE, 0);
	s390_stg   (buf, s390_r1, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, ebp));	
									
	/*---------------------------------------------------------------*/	
	/* save the current IP						 */	
	/*---------------------------------------------------------------*/	
	if (has_caller) {
		s390_lg    (buf, s390_r1, 0, s390_r1, S390_RET_ADDR_OFFSET);
	} else {
		s390_lghi  (buf, s390_r1, 0);
	}
	s390_stg   (buf, s390_r1, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, eip));	
											
	/*---------------------------------------------------------------*/
	/* STEP 2: call the C trampoline function                        */
	/*---------------------------------------------------------------*/
				
	/* Set arguments */

	/* Arg 1: mgreg_t *regs */
	s390_la  (buf, s390_r2, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[0]));
		
	/* Arg 2: code (next address to the instruction that called us) */
	if (has_caller) {
		s390_lg   (buf, s390_r3, 0, s390_r11, S390_RET_ADDR_OFFSET);
	} else {
		s390_lghi (buf, s390_r3, 0);
	}

	/* Arg 3: Trampoline argument */
	s390_lg (buf, s390_r4, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[1]));

	/* Arg 4: trampoline address. */
	S390_SET (buf, s390_r5, buf);
		
	/* Calculate call address and call the C trampoline. Return value will be in r2 */
	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	S390_SET  (buf, s390_r1, tramp);
	s390_basr (buf, s390_r14, s390_r1);
		
	/* OK, code address is now on r2. Save it, so that we
	   can restore r2 and use it later */
	s390_stg  (buf, s390_r2, 0, STK_BASE, G_STRUCT_OFFSET(trampStack_t, saveFn));

	/*----------------------------------------------------------
	  STEP 3: Restore the LMF
	  ----------------------------------------------------------*/
	restoreLMF(buf, STK_BASE, sizeof(trampStack_t));
	
	/* Check for thread interruption */
	S390_SET  (buf, s390_r1, (guint8 *)mono_thread_force_interruption_checkpoint_noraise);
	s390_basr (buf, s390_r14, s390_r1);
	s390_ltgr (buf, s390_r2, s390_r2);
	s390_jz	  (buf, 0); CODEPTR (buf, o[0]);

	/*
	 * Exception case:
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
	S390_SET  (buf, s390_r1, (guint *)mono_get_throw_exception_addr ());
	s390_aghi (buf, STK_BASE, sizeof(trampStack_t));
	s390_lg   (buf, s390_r1, 0, s390_r1, 0); 
	s390_lmg  (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br   (buf, s390_r1);
	PTRSLOT (buf, o[0]);

	/* Reload result */
	s390_lg   (buf, s390_r1, 0, STK_BASE, G_STRUCT_OFFSET(trampStack_t, saveFn));

	/*----------------------------------------------------------
	  STEP 4: call the compiled method
	  ----------------------------------------------------------*/
		
	/* Restore parameter registers */
	s390_lmg (buf, s390_r2, s390_r5, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[2]));
		
	/* Restore the FP registers */
	offset = G_STRUCT_OFFSET(MonoLMF, fregs[0]);
	for (i = s390_f0; i <= s390_f15; ++i) {
		s390_ld  (buf, i, 0, LMFReg, offset);
		offset += sizeof(gdouble);
	}

	/* Restore stack pointer and jump to the code -
	 * R14 contains the return address to our caller 
	 */
	s390_lgr  (buf, STK_BASE, s390_r11);
	s390_lmg  (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN(tramp_type)) {
		s390_lgr (buf, s390_r2, s390_r1);
		s390_br  (buf, s390_r14);
	} else {
		s390_br  (buf, s390_r1);
	}

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));
	
	g_assert (info);
	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, buf - code, ji, unwind_ops);
	g_free (tramp_name);

	/* Sanity check */
	g_assert ((buf - code) <= 512);

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_invalidate_method                       */
/*                                                                  */
/* Function	- 						    */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = ji->code_start;

	S390_SET  (code, s390_r1, func);
	S390_SET  (code, s390_r2, func_arg);
	s390_br   (code, s390_r1);

}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_specific_trampoline              */
/*                                                                  */
/* Function	- Creates the given kind of specific trampoline     */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	gint32 displace;

	tramp = mono_get_trampoline_code (tramp_type);

	/*----------------------------------------------------------*/
	/* This is the method-specific part of the trampoline. Its  */
	/* purpose is to provide the generic part with the          */
	/* MonoMethod *method pointer. We'll use r1 to keep it.     */
	/*----------------------------------------------------------*/
	code = buf = mono_domain_code_reserve (domain, SPECIFIC_TRAMPOLINE_SIZE);

	S390_SET  (buf, s390_r1, arg1);
	displace = (tramp - buf) / 2;
	s390_jg   (buf, displace);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf,
	                     MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE,
	                     (void *) mono_get_generic_trampoline_simple_name (tramp_type)));

	/* Sanity check */
	g_assert ((buf - code) <= SPECIFIC_TRAMPOLINE_SIZE);

	if (code_len)
		*code_len = buf - code;
	
	return code;
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_rgctx_lazy_fetch_trampoline      */
/*                                                                  */
/* Function	- 						    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	gint32 displace;
	int tramp_size,
	    depth, 
	    index, 
	    iPatch = 0,
	    i;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 48 + 16 * depth;
	if (mrgctx)
		tramp_size += 4;
	else
		tramp_size += 12;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));

	if (mrgctx) {
		/* get mrgctx ptr */
		s390_lgr (code, s390_r1, s390_r2);
	} else {
		/* load rgctx ptr from vtable */
		s390_lg (code, s390_r1, 0, s390_r2, MONO_STRUCT_OFFSET(MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		s390_ltgr (code, s390_r1, s390_r1);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [iPatch++] = code;
		s390_jge (code, 0);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0)
			s390_lg (code, s390_r1, 0, s390_r1, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
		else
			s390_lg (code, s390_r1, 0, s390_r1, 0);
		s390_ltgr (code, s390_r1, s390_r1);
		/* if the ptr is null then jump to actual trampoline */
		rgctx_null_jumps [iPatch++] = code;
		s390_jge (code, 0);
	}

	/* fetch slot */
	s390_lg (code, s390_r1, 0, s390_r1, (sizeof (gpointer) * (index  + 1)));
	/* is the slot null? */
	s390_ltgr (code, s390_r1, s390_r1);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [iPatch++] = code;
	s390_jge (code, 0);
	/* otherwise return r1 */
	s390_lgr (code, s390_r2, s390_r1);
	s390_br  (code, s390_r14);

	for (i = 0; i < iPatch; i++) {
		displace = ((uintptr_t) code - (uintptr_t) rgctx_null_jumps[i]) / 2;
		s390_patch_rel ((rgctx_null_jumps [i] + 2), displace);
	}

	g_free (rgctx_null_jumps);

	/* move the rgctx pointer to the VTABLE register */
#if MONO_ARCH_VTABLE_REG != s390_r2
	s390_lgr (code, MONO_ARCH_VTABLE_REG, s390_r2);
#endif

	tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot),
		MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);

	/* jump to the actual trampoline */
	displace = (tramp - code) / 2;
	s390_jg (code, displace);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return(buf);
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name    	- mono_arch_get_static_rgctx_trampoline		    */
/*                                                                  */
/* Function	- Create a trampoline which sets RGCTX_REG to ARG       */
/*		  then jumps to ADDR.				    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_static_rgctx_trampoline (gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	gint32 displace;
	int buf_len;

	MonoDomain *domain = mono_domain_get ();

	buf_len = 32;

	start = code = mono_domain_code_reserve (domain, buf_len);

	S390_SET  (code, MONO_ARCH_RGCTX_REG, arg);
	displace = ((uintptr_t) addr - (uintptr_t) code) / 2;
	s390_jg   (code, displace);
	g_assert ((code - start) < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), domain);

	return(start);
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name	    - mono_arch_get_enter_icall_trampoline.                 */
/*                                                                  */
/* Function - 							    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_enter_icall_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

/*========================= End of Function ========================*/
