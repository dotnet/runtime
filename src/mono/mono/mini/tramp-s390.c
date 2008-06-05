/*------------------------------------------------------------------*/
/* 								    */
/* Name        - tramp-s390.c      			  	    */
/* 								    */
/* Function    - JIT trampoline code for S/390.                     */
/* 								    */
/* Name	       - Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com) */
/* 								    */
/* Date        - January, 2004					    */
/* 								    */
/* Derivation  - From exceptions-x86 & exceptions-ppc		    */
/* 	         Paolo Molaro (lupus@ximian.com) 		    */
/* 		 Dietmar Maurer (dietmar@ximian.com)		    */
/* 								    */
/* Copyright   - 2001 Ximian, Inc.				    */
/* 								    */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/

#define GR_SAVE_SIZE		4*sizeof(long)
#define FP_SAVE_SIZE		16*sizeof(double)
#define METHOD_SAVE_OFFSET	S390_MINIMAL_STACK_SIZE
#define CREATE_GR_OFFSET	METHOD_SAVE_OFFSET+sizeof(gpointer)
#define CREATE_FP_OFFSET	CREATE_GR_OFFSET+GR_SAVE_SIZE
#define CREATE_LMF_OFFSET	CREATE_FP_OFFSET+FP_SAVE_SIZE
#define CREATE_STACK_SIZE	(CREATE_LMF_OFFSET+2*sizeof(long)+sizeof(MonoLMF))

/*------------------------------------------------------------------*/
/* Specific trampoline code fragment sizes		                    */
/*------------------------------------------------------------------*/
#define SPECIFIC_TRAMPOLINE_SIZE	64

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/s390/s390-codegen.h>

#include "mini.h"
#include "mini-s390.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

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
/* Name		- mono_arch_get_unbox_trampoline                        */
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

	start = addr;
	if (MONO_TYPE_ISSTRUCT (mono_method_signature (method)->ret))
		this_pos = s390_r3;

	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 28);
	mono_domain_unlock (domain);
    
	s390_basr (code, s390_r13, 0);
	s390_j	  (code, 4);
	s390_word (code, addr);
	s390_l    (code, s390_r1, 0, s390_r13, 4);
	s390_ahi  (code, this_pos, sizeof(MonoObject));
	s390_br   (code, s390_r1);

	g_assert ((code - start) <= 28);

	mono_arch_flush_icache (start, code - start);

	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_patch_callsite                              */
/*                                                                  */
/* Function	- Patch a non-virtual callsite so it calls @addr.       */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	gint32 displace;
	unsigned short opcode;

	opcode = *((unsigned short *) (orig_code - 6));
	/* This should be a 'brasl' instruction */
	g_assert (opcode ==  0xc0e5);
	orig_code    -= 4;
	displace = ((gint32) addr - (gint32) (orig_code - 2)) / 2;
	s390_patch (orig_code, displace);
	mono_arch_flush_icache (orig_code, 4);
}

/*========================= End of Function ========================*/

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_nullify_class_init_trampoline               */
/*                                                                  */
/* Function	- Nullify a call which calls a class init trampoline    */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	char patch[6] = {0x47, 0x00, 0x00, 0x00, 0x07, 0x00};

	code = code - 6;

	memcpy(code, patch, sizeof(patch));
}

/*========================= End of Function ========================*/

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_vcall_slot                              */
/*                                                                  */
/* Function	- This method is called by the arch independent         */
/*            trampoline code to determine the vtable slot used by  */
/*            the call which invoked the trampoline.                */
/*                The calls                                         */
/*                generated by mono for S/390 will look like either:*/
/*                1. l     %r1,xxx(%rx)                             */
/*                   bras  %r14,%r1                                 */
/*                2. brasl %r14,xxxxxx                              */
/*                                                                  */
/* Parameters   - code   - Pointer into caller code                 */
/*                regs   - Register state at the point of the call  */
/*                displacement - Out parameter which will receive   */
/*                the displacement of the vtable slot               */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_vcall_slot (guint8 *code, gpointer *regs, int *displacement)
{
	int reg;
	guchar* base;
	unsigned short opcode;
	char *sp;

	// We are passed sp instead of the register array
	sp = (char*)regs;

	*displacement = 0;

	opcode = *((unsigned short *) (code - 6));
	switch (opcode) {
	case 0x5810 :
		/* This is a bras r14,r1 instruction */
		code    -= 4;
		reg      = *code >> 4;
		*displacement = *((short *)code) & 0x0fff;
		if (reg > 5) 
			base = *((guchar **) (sp + S390_REG_SAVE_OFFSET+
								  sizeof(int)*(reg-6)));
		else
			base = *((guchar **) ((sp - CREATE_STACK_SIZE) + 
								  CREATE_GR_OFFSET +
								  sizeof(int)*(reg-2)));
		return base;
	case 0x581d :
		/* l %r1,OFFSET(%r13,%r7) */
		code    -= 4;
		reg      = *code >> 4;
		*displacement = *((short *)code) & 0x0fff;
		if (reg > 5) 
			base = *((guchar **) (sp + S390_REG_SAVE_OFFSET+
								  sizeof(int)*(reg-6)));
		else
			base = *((guchar **) ((sp - CREATE_STACK_SIZE) + 
								  CREATE_GR_OFFSET +
								  sizeof(int)*(reg-2)));
		base += *((guint32*) (sp + S390_REG_SAVE_OFFSET+
							  sizeof(int)*(s390_r13-6)));
		return base;
	case 0xc0e5 :
		/* This is the 'brasl' instruction */
		return NULL;
	default :
		g_error("Unable to patch instruction prior to %p",code);
	}

	return NULL;
}

/*========================= End of Function ========================*/

gpointer*
mono_arch_get_vcall_slot_addr (guint8* code, gpointer *regs)
{
	gpointer vt;
	int displacement;
	vt = mono_arch_get_vcall_slot (code, regs, &displacement);
	if (!vt)
		return NULL;
	return (gpointer*)((char*)vt + displacement);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_trampoline_code                      */
/*                                                                  */
/* Function	- Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*------------------------------------------------------------------*/

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{

	guint8 *buf, *tramp, *code;
	int i, offset, lmfOffset;

	/* Now we'll create in 'buf' the S/390 trampoline code. This
	   is the trampoline code common to all methods  */
		
	code = buf = mono_global_codeman_reserve (512);
		
	/*-----------------------------------------------------------
	  STEP 0: First create a non-standard function prologue with a
	  stack size big enough to save our registers.
	  -----------------------------------------------------------*/
		
	s390_stm  (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lr	  (buf, s390_r11, s390_r15);
	s390_ahi  (buf, STK_BASE, -CREATE_STACK_SIZE);
	s390_st	  (buf, s390_r11, 0, STK_BASE, 0);
	s390_st	  (buf, s390_r1, 0, STK_BASE, METHOD_SAVE_OFFSET);
	s390_stm  (buf, s390_r2, s390_r5, STK_BASE, CREATE_GR_OFFSET);

	/* Save the FP registers */
	offset = CREATE_FP_OFFSET;
	for (i = s390_f0; i <= s390_f15; ++i) {
		s390_std  (buf, i, 0, STK_BASE, offset);
		offset += 8;
	}

	/*----------------------------------------------------------
	  STEP 1: call 'mono_get_lmf_addr()' to get the address of our
	  LMF. We'll need to restore it after the call to
	  's390_magic_trampoline' and before the call to the native
	  method.
	  ----------------------------------------------------------*/
				
	s390_basr (buf, s390_r13, 0);
	s390_j	  (buf, 4);
	s390_word (buf, mono_get_lmf_addr);
	s390_l    (buf, s390_r1, 0, s390_r13, 4);
	s390_basr (buf, s390_r14, s390_r1);

	/*---------------------------------------------------------------*/
	/* we build the MonoLMF structure on the stack - see mini-s390.h */
	/* Keep in sync with the code in mono_arch_emit_prolog 		 */
	/*---------------------------------------------------------------*/
	lmfOffset = CREATE_STACK_SIZE - sizeof(MonoLMF);
											
	s390_lr    (buf, s390_r13, STK_BASE);
	s390_ahi   (buf, s390_r13, lmfOffset);	
											
	/*---------------------------------------------------------------*/	
	/* Set lmf.lmf_addr = jit_tls->lmf				 */	
	/*---------------------------------------------------------------*/	
	s390_st    (buf, s390_r2, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, lmf_addr));			
											
	/*---------------------------------------------------------------*/	
	/* Get current lmf						 */	
	/*---------------------------------------------------------------*/	
	s390_l     (buf, s390_r0, 0, s390_r2, 0);				
											
	/*---------------------------------------------------------------*/	
	/* Set our lmf as the current lmf				 */	
	/*---------------------------------------------------------------*/	
	s390_st	   (buf, s390_r13, 0, s390_r2, 0);				
											
	/*---------------------------------------------------------------*/	
	/* Have our lmf.previous_lmf point to the last lmf		 */	
	/*---------------------------------------------------------------*/	
	s390_st	   (buf, s390_r0, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, previous_lmf));			
											
	/*---------------------------------------------------------------*/	
	/* save method info						 */	
	/*---------------------------------------------------------------*/	
	s390_l     (buf, s390_r1, 0, STK_BASE, METHOD_SAVE_OFFSET);
	s390_st    (buf, s390_r1, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, method));				
									
	/*---------------------------------------------------------------*/	
	/* save the current SP						 */	
	/*---------------------------------------------------------------*/	
	s390_l     (buf, s390_r1, 0, STK_BASE, 0);
	s390_st	   (buf, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, ebp));	
									
	/*---------------------------------------------------------------*/	
	/* save the current IP						 */	
	/*---------------------------------------------------------------*/	
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		s390_lhi   (buf, s390_r1, 0);
	} else {
		s390_l     (buf, s390_r1, 0, s390_r1, S390_RET_ADDR_OFFSET);
		s390_la    (buf, s390_r1, 0, s390_r1, 0);
	}
	s390_st    (buf, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, eip));	
											
	/*---------------------------------------------------------------*/	
	/* Save general and floating point registers			 */	
	/*---------------------------------------------------------------*/	
	s390_stm   (buf, s390_r2, s390_r12, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, gregs[2]));			
	for (i = 0; i < 16; i++) {						
		s390_std  (buf, i, 0, s390_r13, 				
				   G_STRUCT_OFFSET(MonoLMF, fregs[i]));			
	}

	/*---------------------------------------------------------------*/
	/* STEP 2: call the C trampoline function                        */
	/*---------------------------------------------------------------*/
				
	/* Set arguments */

	/* Arg 1: gssize *regs. We pass sp instead */
	s390_lr   (buf, s390_r2, STK_BASE);
	s390_ahi  (buf, s390_r2, CREATE_STACK_SIZE);
		
	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		s390_lhi (buf, s390_r3, 0);
	} else {
		s390_l  (buf, s390_r3, 0, s390_r11, S390_RET_ADDR_OFFSET);

		/* Mask out bit 31 */
		s390_basr (buf, s390_r13, 0);
		s390_j	  (buf, 4);
		s390_word (buf, (~(1 << 31)));
		s390_n (buf, s390_r3, 0, s390_r13, 4);
	}
		
	/* Arg 3: MonoMethod *method. It was put in r1 by the
	   method-specific trampoline code, and then saved before the call
	   to mono_get_lmf_addr()'. */
	s390_l  (buf, s390_r4, 0, STK_BASE, METHOD_SAVE_OFFSET);

	/* Arg 4: trampoline address. Ignore for now */
		
	/* Calculate call address and call the C trampoline. Return value will be in r2 */
	s390_basr (buf, s390_r13, 0);
	s390_j	  (buf, 4);
	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	s390_word (buf, tramp);
	s390_l    (buf, s390_r1, 0, s390_r13, 4);
	s390_basr (buf, s390_r14, s390_r1);
		
	/* OK, code address is now on r2. Move it to r1, so that we
	   can restore r2 and use it from r1 later */
	s390_lr   (buf, s390_r1, s390_r2);

	/*----------------------------------------------------------
	  STEP 3: Restore the LMF
	  ----------------------------------------------------------*/
	restoreLMF(buf, STK_BASE, CREATE_STACK_SIZE);
	
	/*----------------------------------------------------------
	  STEP 4: call the compiled method
	  ----------------------------------------------------------*/
		
	/* Restore registers */

	s390_lm   (buf, s390_r2, s390_r5, STK_BASE, CREATE_GR_OFFSET);
		
	/* Restore the FP registers */
	offset = CREATE_FP_OFFSET;
	for (i = s390_f0; i <= s390_f15; ++i) {
		s390_ld  (buf, i, 0, STK_BASE, offset);
		offset += 8;
	}

	/* Restore stack pointer and jump to the code - 
	   R14 contains the return address to our caller */
	s390_lr   (buf, STK_BASE, s390_r11);
	s390_lm   (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT || tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
		s390_br (buf, s390_r14);
	else
		s390_br   (buf, s390_r1);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
	
	/* Sanity check */
	g_assert ((buf - code) <= 512);

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_specific_trampoline                  */
/*                                                                  */
/* Function	- Creates the given kind of specific trampoline         */
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
	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, SPECIFIC_TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	s390_basr (buf, s390_r1, 0);
	s390_j	  (buf, 4);
	s390_word (buf, arg1);
	s390_l    (buf, s390_r1, 0, s390_r1, 4);
	displace = (tramp - buf) / 2;
	s390_jcl  (buf, S390_CC_UN, displace);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	/* Sanity check */
	g_assert ((buf - code) <= SPECIFIC_TRAMPOLINE_SIZE);

	if (code_len)
		*code_len = buf - code;
	
	return code;
}	

/*========================= End of Function ========================*/

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
