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
#define CREATE_GR_OFFSET	S390_MINIMAL_STACK_SIZE
#define CREATE_FP_OFFSET	CREATE_GR_OFFSET+GR_SAVE_SIZE
#define CREATE_LMF_OFFSET	CREATE_FP_OFFSET+FP_SAVE_SIZE
#define CREATE_STACK_SIZE	(CREATE_LMF_OFFSET+2*sizeof(long)+sizeof(MonoLMF))
#define METHOD_SAVE_OFFSET	S390_RET_ADDR_OFFSET-4

/*------------------------------------------------------------------*/
/* adapt to mini later...					    */
/*------------------------------------------------------------------*/
#define mono_jit_share_code 	(1)

/*------------------------------------------------------------------*/
/* Method-specific trampoline code fragment sizes		    */
/*------------------------------------------------------------------*/
#define METHOD_TRAMPOLINE_SIZE	64
#define JUMP_TRAMPOLINE_SIZE	64

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
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-s390.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

typedef enum {
	MONO_TRAMPOLINE_GENERIC,
	MONO_TRAMPOLINE_JUMP,
	MONO_TRAMPOLINE_CLASS_INIT
} MonoTrampolineType;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/* Address of the generic trampoline code.  This is used by the     */
/* debugger to check whether a method is a trampoline.		    */
/*------------------------------------------------------------------*/
guint8 *mono_generic_trampoline_code = NULL;

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/


/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_unbox_trampoline                              */
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

static gpointer
get_unbox_trampoline (MonoMethod *method, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = s390_r2;

	start = addr;
	if ((method->klass->valuetype)) {
//	if ((method->klass->valuetype) && 
//	    (method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if ((!method->signature->ret->byref) && 
		    (MONO_TYPE_ISSTRUCT (method->signature->ret)))
			this_pos = s390_r3;
	    
		start = code = g_malloc (28);

		s390_basr (code, s390_r13, 0);
		s390_j	  (code, 4);
		s390_word (code, addr);
		s390_l    (code, s390_r1, 0, s390_r13, 4);
		s390_ahi  (code, this_pos, sizeof(MonoObject));
		s390_br   (code, s390_r1);

		g_assert ((code - start) <= 28);
	}

	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_magic_trampoline                             */
/*                                                                  */
/* Function	- This method is called by the function             */
/*                "arch_create_jit_trampoline", which in turn is    */
/*                called by the trampoline functions for virtual    */
/*                methods. After having called the JIT compiler to  */
/*                compile the method, it inspects the caller code   */
/*                to find the address of the method-specific part   */
/*                of the trampoline vtable slot for this method,    */
/*                updates it with a fragment that calls the newly   */
/*                compiled code and returns this address. The calls */
/*                generated by mono for S/390 will look like either:*/
/*                1. l     %r1,xxx(%rx)                             */
/*                   bras  %r14,%r1                                 */
/*                2. brasl %r14,xxxxxx                              */
/*                                                                  */
/* Parameters   - code   - Pointer into caller code                 */
/*                method - The method to compile                    */
/*                sp     - Stack pointer                            */
/*                                                                  */
/*------------------------------------------------------------------*/

static gpointer
s390_magic_trampoline (MonoMethod *method, guchar *code, char *sp)
{
	gpointer addr;
	gint32 displace;
	int reg, base;
	unsigned short opcode;
	char *fname;
	MonoJitInfo *codeJi, 
		    *addrJi;

	addr = mono_compile_method(method);
	g_assert(addr);


	if (code) {

		/* The top bit needs to be ignored on S/390 */
		(guint32) code &= 0x7fffffff;

		fname  = mono_method_full_name (method, TRUE);
		codeJi = mono_jit_info_table_find (mono_domain_get(), code);
		addrJi = mono_jit_info_table_find (mono_domain_get(), addr);

		opcode = *((unsigned short *) (code - 6));
		switch (opcode) {
			case 0x5810 :
				/* This is a bras r14,r1 instruction */
				code    -= 4;
				reg      = *code >> 4;
				displace = *((short *)code) & 0x0fff;
				if (reg > 5) 
					base = *((int *) (sp + S390_REG_SAVE_OFFSET+
							       sizeof(int)*(reg-6)));
				else
 					base = *((int *) (sp + CREATE_GR_OFFSET+
							       sizeof(int)*(reg-2)));
				addr = get_unbox_trampoline(method, addr);
				if (mono_method_same_domain (codeJi, addrJi)) {
					code = base + displace;
					s390_patch(code, addr);
				}
				break;
			case 0xc0e5 :
				/* This is the 'brasl' instruction */
				code    -= 4;
				displace = ((gint32) addr - (gint32) (code - 2)) / 2;
				if (mono_method_same_domain (codeJi, addrJi)) {
					s390_patch (code, displace);
					mono_arch_flush_icache (code, 4);
				}
				break;
			default :
				g_error("Unable to patch instruction prior to %p",code);
		}
	}


	return addr;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_class_init_trampoline                        */
/*                                                                  */
/* Function	- Initialize a class and then no-op the call to     */
/*                the trampoline.                                   */
/*                                                                  */
/*------------------------------------------------------------------*/

static void
s390_class_init_trampoline (void *vtable, guchar *code, char *sp)
{
	char patch[6] = {0x47, 0x00, 0x00, 0x00, 0x07, 0x00};

	mono_runtime_class_init (vtable);

	code = code - 6;

	memcpy(code, patch, sizeof(patch));
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- create_trampoline_code                            */
/*                                                                  */
/* Function	- Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*------------------------------------------------------------------*/

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{

	guint8 *buf, *code = NULL;
	static guint8* generic_jump_trampoline = NULL;
	static guint8 *generic_class_init_trampoline = NULL;
	int i, offset, lmfOffset;

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
		/* Now we'll create in 'buf' the S/390 trampoline code. This
		 is the trampoline code common to all methods  */
		
		code = buf = g_malloc(512);
		
		/*-----------------------------------------------------------
		STEP 0: First create a non-standard function prologue with a
		stack size big enough to save our registers.
		-----------------------------------------------------------*/
		
		s390_stm  (buf, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
		s390_lr	  (buf, s390_r11, s390_r15);
		s390_ahi  (buf, STK_BASE, -CREATE_STACK_SIZE);
		s390_st	  (buf, s390_r11, 0, STK_BASE, 0);
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
		s390_l     (buf, s390_r1, 0, s390_r11, METHOD_SAVE_OFFSET);
		s390_st    (buf, s390_r1, 0, s390_r13, 				
			    G_STRUCT_OFFSET(MonoLMF, method));				
										
		/*---------------------------------------------------------------*/	
		/* save the current IP						 */	
		/*---------------------------------------------------------------*/	
		s390_l     (buf, s390_r1, 0, STK_BASE, 0);
		s390_st	   (buf, s390_r1, 0, s390_r13, G_STRUCT_OFFSET(MonoLMF, ebp));	
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
		/* STEP 2: call 's390_magic_trampoline()', who will compile the  */
		/* code and fix the method vtable entry for us			 */
		/*---------------------------------------------------------------*/
				
		/* Set arguments */
		
		/* Arg 1: MonoMethod *method. It was put in r11 by the
		method-specific trampoline code, and then saved before the call
		to mono_get_lmf_addr()'. Restore r13, by the way :-) */
		s390_l  (buf, s390_r2, 0, s390_r11, METHOD_SAVE_OFFSET);
		
		/* Arg 2: code (next address to the instruction that called us) */
		if (tramp_type == MONO_TRAMPOLINE_JUMP) {
			s390_lhi (buf, s390_r3, 0);
		} else {
			s390_l  (buf, s390_r3, 0, s390_r11, S390_RET_ADDR_OFFSET);
		}
		
		/* Arg 3: stack pointer */
		s390_lr   (buf, s390_r4, STK_BASE);
		
		/* Calculate call address and call
		's390_magic_trampoline'. Return value will be in r2 */
		s390_basr (buf, s390_r13, 0);
		s390_j	  (buf, 4);
		if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
			s390_word (buf, s390_class_init_trampoline);
		} else {
			s390_word (buf, s390_magic_trampoline);
		}
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
		s390_br   (buf, s390_r1);

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

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_jump_trampoline                  */
/*                                                                  */
/* Function	- Create the designated type of trampoline according*/
/*                to the 'tramp_type' parameter.                    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoJitInfo *
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	guint8 *code, *buf, *tramp = NULL;
	MonoJitInfo *ji;
	MonoDomain *domain = mono_domain_get();
	gint32 displace;

	tramp = create_trampoline_code (MONO_TRAMPOLINE_JUMP);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, METHOD_TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	s390_basr (buf, s390_r13, 0);
	s390_j	  (buf, 4);
	s390_word (buf, method);
	s390_l    (buf, s390_r13, 0, s390_r13, 4);
	displace = (tramp - buf) / 2;
	s390_jcl  (buf, S390_CC_UN, displace);

	mono_arch_flush_icache (code, buf-code);

	g_assert ((buf - code) <= JUMP_TRAMPOLINE_SIZE);
	
	ji 		= g_new0 (MonoJitInfo, 1);
	ji->method 	= method;
	ji->code_start 	= code;
	ji->code_size  	= buf - code;
	
	mono_jit_stats.method_trampolines++;

	return ji;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_jit_trampoline                   */
/*                                                                  */
/* Function	- Creates a trampoline function for virtual methods.*/
/*                If the created code is called it first starts JIT */
/*                compilation and then calls the newly created      */
/*                method. It also replaces the corresponding vtable */
/*                entry (see s390_magic_trampoline).                */
/*                                                                  */
/*                A trampoline consists of two parts: a main        */
/*                fragment, shared by all method trampolines, and   */
/*                and some code specific to each method, which      */
/*                hard-codes a reference to that method and then    */
/*                calls the main fragment.                          */
/*                                                                  */
/*                The main fragment contains a call to              */
/*                's390_magic_trampoline', which performs a call    */
/*                to the JIT compiler and substitutes the method-   */
/*                specific fragment with some code that directly    */
/*                calls the JIT-compiled method.                    */
/*                                                                  */
/* Parameter    - method - Pointer to the method information        */
/*                                                                  */
/* Returns      - A pointer to the newly created code               */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;
	static guint8 *vc = NULL;
	gint32 displace;

	vc = create_trampoline_code (MONO_TRAMPOLINE_GENERIC);

	/* This is the method-specific part of the trampoline. Its purpose is
	to provide the generic part with the MonoMethod *method pointer. We'll
	use r13 to keep that value, for instance. However, the generic part of
	the trampoline relies on r11 having the same value it had before coming
	here, so we must save it before. */
	code = buf = g_malloc(METHOD_TRAMPOLINE_SIZE);

	s390_basr (buf, s390_r13, 0);
	s390_j	  (buf, 4);
	s390_word (buf, method);
	s390_l    (buf, s390_r13, 0, s390_r13, 4);
	displace = (vc - buf) / 2;
	s390_jcl  (buf, S390_CC_UN, displace);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);
	
	mono_jit_stats.method_trampolines++;

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_class_init_trampoline            */
/*                                                                  */
/* Function	- Creates a trampoline function to run a type init- */
/*                ializer. If the trampoline is called, it calls    */
/*                mono_runtime_class_init with the given vtable,    */
/*                then patches the caller code so it does not get   */
/*                called any more.                                  */
/*                                                                  */
/* Parameter    - vtable - The type to initialize                   */
/*                                                                  */
/* Returns      - A pointer to the newly created code               */
/*                                                                  */
/*------------------------------------------------------------------*/

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
	code = buf = g_malloc(METHOD_TRAMPOLINE_SIZE);

	s390_st   (buf, s390_r14, 0, STK_BASE, S390_RET_ADDR_OFFSET);
	s390_ahi  (buf, STK_BASE, -S390_MINIMAL_STACK_SIZE);

	s390_basr (buf, s390_r1, 0);
	s390_j	  (buf, 6);
	s390_word (buf, vtable);
        s390_word (buf, s390_class_init_trampoline);
	s390_lr	  (buf, s390_r3, s390_r14);
	s390_l    (buf, s390_r2, 0, s390_r1, 4);
	s390_lhi  (buf, s390_r4, 0);
	s390_l    (buf, s390_r1, 0, s390_r1, 8);
	s390_basr (buf, s390_r14, s390_r1);

	s390_ahi  (buf, STK_BASE, S390_MINIMAL_STACK_SIZE);
	s390_l    (buf, s390_r14, 0, STK_BASE, S390_RET_ADDR_OFFSET);
	s390_br   (buf, s390_r14);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);
		
	/* Sanity check */
	g_assert ((buf - code) <= METHOD_TRAMPOLINE_SIZE);

	mono_jit_stats.method_trampolines++;

	return code;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_debuger_create_notification_function	    */
/*                                                                  */
/* Function	- This method is only called when running in the    */
/*                Mono debugger. It returns a pointer to the        */
/*                arch specific notification function.              */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = g_malloc0 (16);
	s390_break (buf);
	if (notification_address)
		*notification_address = buf;
	s390_br (buf, s390_r14);

	return ptr;
}

/*========================= End of Function ========================*/
