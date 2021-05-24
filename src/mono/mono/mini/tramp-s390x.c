/**
 * @file
 *
 * @author Neale Ferguson <neale@sinenomine.net>
 *
 * @section description
 *
 * Function    - JIT trampoline code for S/390.
 *
 * Date        - January, 2004
 *
 * Derivation  - From tramp-x86 & tramp-ppc
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
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/arch/s390x/s390x-codegen.h>

#include "mini.h"
#include "mini-s390x.h"
#include "mini-runtime.h"
#include "jit-icalls.h"
#include "debugger-agent.h"
#include "mono/utils/mono-tls-inline.h"

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

/**
 *                                 
 * @brief Build the unbox trampoline
 *
 * @param[in] Method pointer
 * @param[in] Pointer to native code for method
 *
 * Return a pointer to a trampoline which does the unboxing before 
 * calling the method.
 *
 * When value type methods are called through the  
 * vtable we need to unbox the 'this' argument.	   
 */

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = s390_r2;
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (m);
	char trampName[128];

	start = code = (guint8 *) mono_mem_manager_code_reserve (mem_manager, 28);

	S390_SET  (code, s390_r1, addr);
	s390_aghi (code, this_pos, MONO_ABI_SIZEOF (MonoObject));
	s390_br   (code, s390_r1);

	g_assert ((code - start) <= 28);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));

	snprintf(trampName, sizeof(trampName), "%s_unbox_trampoline", m->name);

	mono_tramp_info_register (mono_tramp_info_create (trampName, start, code - start, NULL, NULL), mem_manager);

	return start;
}

/*========================= End of Function ========================*/

/**
 *                                 
 * @brief Build the SDB trampoline
 *
 * @param[in] Type of trampoline (ss or bp)
 * @param[in] MonoTrampInfo
 * @param[in] Ahead of time indicator
 *
 * Return a trampoline which captures the current context, passes it to
 * mono_debugger_agent_single_step_from_context ()/mono_debugger_agent_breakpoint_from_context (),
 * then restores the (potentially changed) context.
 */

guint8 *
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	int tramp_size = 512;
	int i, framesize, ctx_offset, 
	    gr_offset, fp_offset, ip_offset,
	    sp_offset;
	guint8 *code, *buf;
	void *ep;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	code = buf = (guint8 *)mono_global_codeman_reserve (tramp_size);

	framesize = S390_MINIMAL_STACK_SIZE;

	ctx_offset = framesize;
	framesize += sizeof (MonoContext);

	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	/** 
	 * Create unwind information - On entry s390_r1 has value of method's frame reg
	 */
	s390_stmg (code, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, STK_BASE, S390_CFA_OFFSET);
	gr_offset = S390_REG_SAVE_OFFSET - S390_CFA_OFFSET;
	for (i = s390_r6; i <= s390_r15; i++) {
		mono_add_unwind_op_offset (unwind_ops, code, buf, i, gr_offset);
		gr_offset += sizeof(uintptr_t);
	}
		
	s390_lgr  (code, s390_r0, STK_BASE);
	s390_aghi (code, STK_BASE, -framesize);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, framesize + S390_CFA_OFFSET);
	s390_stg  (code, s390_r0, 0, STK_BASE, 0);

	gr_offset = ctx_offset + G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs);
	sp_offset = ctx_offset + G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[15]);
	ip_offset = ctx_offset + G_STRUCT_OFFSET(MonoContext, uc_mcontext.psw.addr);

	/* Initialize a MonoContext structure on the stack */
	s390_stmg (code, s390_r0, s390_r14, STK_BASE, gr_offset);
	s390_stg  (code, s390_r1, 0, STK_BASE, sp_offset);
	s390_stg  (code, s390_r14, 0, STK_BASE, ip_offset);
	
	fp_offset = ctx_offset + G_STRUCT_OFFSET(MonoContext, uc_mcontext.fpregs.fprs);
	for (i = s390_f0; i < s390_f15; ++i) {
		s390_std (code, i, 0, STK_BASE, fp_offset);
		fp_offset += sizeof(double);
	}

	/* 
	 * Call the single step/breakpoint function in sdb using
	 * the context address as the parameter
	 */
	s390_la (code, s390_r2, 0, STK_BASE, ctx_offset);

	if (single_step) 
		ep = (mini_get_dbg_callbacks())->single_step_from_context;
	else
		ep = (mini_get_dbg_callbacks())->breakpoint_from_context;

	S390_SET  (code, s390_r1, ep);
	s390_basr (code, s390_r14, s390_r1);

	/*
	 * Restore volatiles
	 */ 
	s390_lmg (code, s390_r0, s390_r5, STK_BASE, gr_offset);

	/*
	 * Restore FP registers
	 */ 
	fp_offset = ctx_offset + G_STRUCT_OFFSET(MonoContext, uc_mcontext.fpregs.fprs);
	for (i = s390_f0; i < s390_f15; ++i) {
		s390_ld (code, i, 0, STK_BASE, fp_offset);
		fp_offset += sizeof(double);
	}

	/*
	 * Load the IP from the context to pick up any SET_IP command results
	 */ 
	s390_lg   (code, s390_r14, 0, STK_BASE, ip_offset);

	/*
	 * Restore everything else from the on-entry values
	 */ 
	s390_aghi (code, STK_BASE, framesize);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, S390_CFA_OFFSET);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, STK_BASE);
	s390_lmg  (code, s390_r6, s390_r13, STK_BASE, S390_REG_SAVE_OFFSET);
	for (i = s390_r6; i <= s390_r13; i++)
		mono_add_unwind_op_same_value (unwind_ops, code, buf, i);
	s390_br   (code, s390_r14);

	g_assertf ((code - buf) <= tramp_size, "%d %d", (int)(code - buf), tramp_size);
	mono_arch_flush_icache (code, code - buf);

	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	g_assert (code - buf <= tramp_size);

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

/**
 *
 * @brief Locate the address of the thunk target
 *
 * @param[in] @code - Instruction following the branch and save
 * @returns Address of the thunk code
 *
 * A thunk call is a sequence of:
 * 	lgrl	rx,tgt 	Load address of target 		(.-6)
 * 	basr	rx,rx	Branch and save to that address (.), or,
 * 	br	r1	Jump to target			(.)
 *
 * The target of that code is a thunk which:
 * tgt:	.quad	target		Destination
 */

guint8 *
mono_arch_get_call_target (guint8 *code)
{
	guint8 *thunk;
	guint32 rel;

	/*
	 * Determine thunk address by adding the relative offset
	 * in the lgrl to its location
	 */
	rel = *(guint32 *) ((uintptr_t) code - 4);
	thunk = (guint8 *) ((uintptr_t) code - 6) + (rel * 2);

	return(thunk);
}

/*========================= End of Function ========================*/

/**
 *
 * @brief Patch the callsite
 *
 * @param[in] @method_start - first instruction of method
 * @param[in] @orig_code - Instruction following the branch and save
 * @param[in] @addr - New value for target of call
 *
 * Patch a call. The call is either a 'thunked' call identified by the BASR R14,R14
 * instruction or a direct call
 */

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	guint64 *thunk;

	thunk = (guint64 *) mono_arch_get_call_target(orig_code - 2);
	*thunk = (guint64) addr;
}

/*========================= End of Function ========================*/

/**
 *
 * @brief Patch PLT entry (AOT only)
 *
 * @param[in] @code - Location of PLT
 * @param[in] @got - Global Offset Table
 * @param[in] @regs - Registers at the time
 * @param[in] @addr - Target address
 *
 * Not reached on s390x until we have AOT support
 *
 */

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/**
 *
 * @brief Architecture-specific trampoline creation
 *
 * @param[in] @tramp_type - Type of trampoline
 * @param[out] @info - Pointer to trampoline information
 * @param[in] @aot - AOT indicator
 *
 * Create a generic trampoline
 *
 */

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	const char *tramp_name;
	guint8 *buf, *tramp, *code;
	int i, offset, has_caller;
	short *o[1];
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	g_assert (!aot);

	/* Now we'll create in 'buf' the S/390 trampoline code. This
	   is the trampoline code common to all methods  */
		
	code = buf = (guint8 *) mono_global_codeman_reserve(512);
		
	if (tramp_type == MONO_TRAMPOLINE_JUMP) 
		has_caller = 0;
	else
		has_caller = 1;

	/*-----------------------------------------------------------
	  STEP 0: First create a non-standard function prologue with a
	  stack size big enough to save our registers.
	  -----------------------------------------------------------*/
		
	mono_add_unwind_op_def_cfa (unwind_ops, buf, code, STK_BASE, S390_CFA_OFFSET);
	s390_stmg (buf, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	offset = S390_REG_SAVE_OFFSET - S390_CFA_OFFSET;
	for (i = s390_r6; i <= s390_r15; i++) {
		mono_add_unwind_op_offset (unwind_ops, buf, code, i, offset);
		offset += sizeof(uintptr_t);
	}
		
	s390_lgr  (buf, s390_r11, s390_r15);
	s390_aghi (buf, STK_BASE, -sizeof(trampStack_t));
	mono_add_unwind_op_def_cfa_offset (unwind_ops, buf, code, sizeof(trampStack_t) + S390_CFA_OFFSET);
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
	s390_lg    (buf, s390_r1, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[0]));
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

	/* Arg 1: host_mgreg_t *regs */
	s390_la  (buf, s390_r2, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[0]));
		
	/* Arg 2: code (next address to the instruction that called us) */
	if (has_caller) {
		s390_lg   (buf, s390_r3, 0, s390_r11, S390_RET_ADDR_OFFSET);
	} else {
		s390_lghi (buf, s390_r3, 0);
	}

	/* Arg 3: Trampoline argument */
	s390_lg (buf, s390_r4, 0, LMFReg, G_STRUCT_OFFSET(MonoLMF, gregs[0]));

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
	S390_SET  (buf, s390_r1, (guint *)mono_get_rethrow_preserve_exception_addr ());
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
	mono_add_unwind_op_def_cfa_offset (unwind_ops, buf, code, S390_CFA_OFFSET);
	mono_add_unwind_op_same_value (unwind_ops, buf, code, STK_BASE);
	s390_lmg  (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	for (i = s390_r6; i <= s390_r14; i++)
		mono_add_unwind_op_same_value (unwind_ops, buf, code, i);

	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN(tramp_type)) {
		s390_lgr (buf, s390_r2, s390_r1);
		s390_br  (buf, s390_r14);
	} else {
		s390_br  (buf, s390_r1);
	}

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));
	
	g_assert (info);
	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, code, buf - code, ji, unwind_ops);

	/* Sanity check */
	g_assert ((buf - code) <= 512);

	return code;
}

/*========================= End of Function ========================*/

/**
 *
 * @brief Architecture-specific method invalidation
 *
 * @param[in] @func - Function to call
 * @param[in] @func_arg - Argument to invalidation function
 *
 * Call an error routine so peple can fix their code
 *
 */

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	/* FIXME: This is not thread safe */
	guint8 *code = (guint8 *) ji->code_start;

	S390_SET  (code, s390_r1, func);
	S390_SET  (code, s390_r2, func_arg);
	s390_br   (code, s390_r1);

}

/*========================= End of Function ========================*/

/**
 *
 * @brief Architecture-specific specific trampoline creation
 *
 * @param[in] @arg1 - Argument to trampoline being created
 * @param[in] @tramp_type - Trampoline type
 * @param[in] @domain - Mono Domain
 * @param[out] @code_len - Length of trampoline created
 *
 * Create the specified kind of trampoline
 *
 */

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	gint64 displace;

	tramp = mono_get_trampoline_code (tramp_type);

	/*----------------------------------------------------------*/
	/* This is the method-specific part of the trampoline. Its  */
	/* purpose is to provide the generic part with the          */
	/* MonoMethod *method pointer. We'll use r1 to keep it.     */
	/*----------------------------------------------------------*/
	code = buf = (guint8 *) mono_mem_manager_code_reserve (mem_manager, SPECIFIC_TRAMPOLINE_SIZE);

	S390_SET  (buf, s390_r0, arg1);
	displace = (tramp - buf) / 2;
	if ((displace >= INT_MIN) && (displace <= INT_MAX))
		s390_jg   (buf, (gint32) displace);
	else {
		S390_SET  (buf, s390_r1, tramp);
		s390_br   (buf, s390_r1);
	}

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

/**
 *
 * @brief Architecture-specific RGCTX lazy fetch trampoline
 *
 * @param[in] @slot - Instance
 * @param[out] @info - Mono Trampoline Information
 * @param[in] @aot - AOT indicator
 *
 * Create the specified kind of trampoline
 *
 */

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	guint8 **rgctx_null_jumps;
	gint64 displace;
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
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (target_mgreg_t);
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

	code = buf = (guint8 *) mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = (guint8 **) g_malloc (sizeof (guint8*) * (depth + 2));

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
	s390_lg (code, s390_r1, 0, s390_r1, (sizeof (target_mgreg_t) * (index  + 1)));
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

	MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();
	tramp = (guint8*)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot),
		MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, NULL);

	/* jump to the actual trampoline */
	displace = (tramp - code) / 2;
	if ((displace >= INT_MIN) && (displace <= INT_MAX))
		s390_jg (code, displace);
	else {
		S390_SET (code, s390_r1, tramp);
		s390_br  (code, s390_r1);
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return(buf);
}	

/*========================= End of Function ========================*/

/**
 *
 * @brief Architecture-specific static RGCTX lazy fetch trampoline
 *
 * @param[in] @arg - Argument to trampoline being created
 * @param[out] @addr - Target
 *
 * Create a trampoline which sets RGCTX_REG to ARG
 *                then jumps to ADDR.
 */

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	gint64 displace;
	int buf_len;

	buf_len = 32;

	start = code = (guint8 *) mono_mem_manager_code_reserve (mem_manager, buf_len);

	S390_SET  (code, MONO_ARCH_RGCTX_REG, arg);
	displace = ((uintptr_t) addr - (uintptr_t) code) / 2;
	if ((displace >= INT_MIN) && (displace <= INT_MAX))
		s390_jg (code, (gint32) displace);
	else {
		S390_SET (code, s390_r1, addr);
		s390_br (code, s390_r1);
	}
	g_assert ((code - start) < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), mem_manager);

	return(start);
}	

/*========================= End of Function ========================*/
