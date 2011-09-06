/*------------------------------------------------------------------*/
/* 								    */
/* Name        - exceptions-s390.c				    */
/* 								    */
/* Function    - Exception support for S/390.                       */
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

#define S390_CALLFILTER_INTREGS   	S390_MINIMAL_STACK_SIZE
#define S390_CALLFILTER_FLTREGS		S390_CALLFILTER_INTREGS+(16*sizeof(gulong))
#define S390_CALLFILTER_ACCREGS		S390_CALLFILTER_FLTREGS+(16*sizeof(gdouble))
#define S390_CALLFILTER_SIZE		(S390_CALLFILTER_ACCREGS+(16*sizeof(gulong)))

#define S390_THROWSTACK_ACCPRM		S390_MINIMAL_STACK_SIZE
#define S390_THROWSTACK_FPCPRM		S390_THROWSTACK_ACCPRM+sizeof(gpointer)
#define S390_THROWSTACK_RETHROW		S390_THROWSTACK_FPCPRM+sizeof(gulong)
#define S390_THROWSTACK_INTREGS		S390_THROWSTACK_RETHROW+sizeof(gboolean)
#define S390_THROWSTACK_FLTREGS		S390_THROWSTACK_INTREGS+(16*sizeof(gulong))
#define S390_THROWSTACK_ACCREGS		S390_THROWSTACK_FLTREGS+(16*sizeof(gdouble))
#define S390_THROWSTACK_SIZE		(S390_THROWSTACK_ACCREGS+(16*sizeof(gulong)))

#define SZ_THROW	384

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <ucontext.h>

#include <mono/arch/s390/s390-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-s390.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

gboolean mono_arch_handle_exception (void     *ctx,
									 gpointer obj);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_call_filter                         */
/*                                                                  */
/* Function	- Return a pointer to a method which calls an       */
/*                exception filter. We also use this function to    */
/*                call finally handlers (we pass NULL as @exc       */
/*                object in this case).                             */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	static guint8 *start;
	static int inited = 0;
	guint8 *code;
	int alloc_size, pos, i;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start = mono_global_codeman_reserve (512);

	s390_stm (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lr  (code, s390_r14, STK_BASE);
	alloc_size = S390_ALIGN(S390_CALLFILTER_SIZE, S390_STACK_ALIGNMENT);
	s390_ahi (code, STK_BASE, -alloc_size);
	s390_st  (code, s390_r14, 0, STK_BASE, 0);

	/*------------------------------------------------------*/
	/* save general registers on stack			*/
	/*------------------------------------------------------*/
	s390_stm (code, s390_r0, s390_r13, STK_BASE, S390_CALLFILTER_INTREGS);

	/*------------------------------------------------------*/
	/* save floating point registers on stack		*/
	/*------------------------------------------------------*/
//	pos = S390_CALLFILTER_FLTREGS;
//	for (i = 0; i < 16; ++i) {
//		s390_std (code, i, 0, STK_BASE, pos);
//		pos += sizeof (gdouble);
//	}

	/*------------------------------------------------------*/
	/* save access registers on stack       		*/
	/*------------------------------------------------------*/
//	s390_stam (code, s390_a0, s390_a15, STK_BASE, S390_CALLFILTER_ACCREGS);

	/*------------------------------------------------------*/
	/* Get A(Context)					*/
	/*------------------------------------------------------*/
	s390_lr	  (code, s390_r13, s390_r2);

	/*------------------------------------------------------*/
	/* Get A(Handler Entry Point)				*/
	/*------------------------------------------------------*/
	s390_lr   (code, s390_r0, s390_r3);

	/*------------------------------------------------------*/
	/* Set parameter register with Exception		*/
	/*------------------------------------------------------*/
	s390_lr	  (code, s390_r2, s390_r4);

	/*------------------------------------------------------*/
	/* Load all registers with values from the context	*/
	/*------------------------------------------------------*/
	s390_lm   (code, s390_r3, s390_r12, s390_r13, 
		   G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[3]));
	pos = G_STRUCT_OFFSET(MonoContext, uc_mcontext.fpregs.fprs[0]);
	for (i = 0; i < 16; ++i) {
		s390_ld  (code, i, 0, s390_r13, pos);
		pos += sizeof(gdouble);
	}
	
	/*------------------------------------------------------*/
	/* Point at the copied stack frame and call the filter	*/
	/*------------------------------------------------------*/
	s390_lr   (code, s390_r1, s390_r0);
	s390_basr (code, s390_r14, s390_r1);

	/*------------------------------------------------------*/
	/* Save return value					*/
	/*------------------------------------------------------*/
	s390_lr   (code, s390_r14, s390_r2);

	/*------------------------------------------------------*/
	/* Restore all the regs from the stack 			*/
	/*------------------------------------------------------*/
	s390_lm (code, s390_r0, s390_r13, STK_BASE, S390_CALLFILTER_INTREGS);
//	pos = S390_CALLFILTER_FLTREGS;
//	for (i = 0; i < 16; ++i) {
//		s390_ld (code, i, 0, STK_BASE, pos);
//		pos += sizeof (gdouble);
//	}

	s390_lr   (code, s390_r2, s390_r14);
//	s390_lam  (code, s390_a0, s390_a15, STK_BASE, S390_CALLFILTER_ACCREGS);
	s390_ahi  (code, s390_r15, alloc_size);
	s390_lm   (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br   (code, s390_r14);

	g_assert ((code - start) < SZ_THROW); 
	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- throw_exception.                                  */
/*                                                                  */
/* Function	- Raise an exception based on the parameters passed.*/
/*                                                                  */
/*------------------------------------------------------------------*/

static void
throw_exception (MonoObject *exc, unsigned long ip, unsigned long sp, 
		 gulong *int_regs, gdouble *fp_regs, gulong *acc_regs, 
		 guint fpc, gboolean rethrow)
{
	MonoContext ctx;
	int iReg;
	
	memset(&ctx, 0, sizeof(ctx));

	getcontext(&ctx);

	/* adjust eip so that it point into the call instruction */
	ip -= 6;

	for (iReg = 0; iReg < 16; iReg++) {
		ctx.uc_mcontext.gregs[iReg]  	    = int_regs[iReg];
		ctx.uc_mcontext.fpregs.fprs[iReg].d = fp_regs[iReg];
		ctx.uc_mcontext.aregs[iReg]  	    = acc_regs[iReg];
	}

	ctx.uc_mcontext.fpregs.fpc = fpc;

	MONO_CONTEXT_SET_BP (&ctx, sp);
	MONO_CONTEXT_SET_IP (&ctx, ip);
	
	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_arch_handle_exception (&ctx, exc);
	setcontext(&ctx);

	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- get_throw_exception_generic              	    */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc); or,            */
/*                void (*func) (char *exc_name);                    */
/*                                                                  */
/*------------------------------------------------------------------*/

static gpointer 
get_throw_exception_generic (guint8 *start, int size, 
			     int by_name, gboolean rethrow)
{
	guint8 *code;
	int alloc_size, pos, i, offset;

	code = start;

	s390_stm (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	alloc_size = S390_ALIGN(S390_THROWSTACK_SIZE, S390_STACK_ALIGNMENT);
	s390_lr   (code, s390_r14, STK_BASE);
	s390_ahi  (code, STK_BASE, -alloc_size);
	s390_st   (code, s390_r14, 0, STK_BASE, 0);
	if (by_name) {
		s390_lr   (code, s390_r4, s390_r2);
		s390_bras (code, s390_r13, 6);
		s390_word (code, mono_defaults.corlib);
		s390_word (code, "System");
		s390_l	  (code, s390_r2, 0, s390_r13, 0);
		s390_l	  (code, s390_r3, 0, s390_r13, 4);
		offset = (guint32) S390_RELATIVE(mono_exception_from_name, code);
		s390_brasl(code, s390_r14, offset);
	}
	/*------------------------------------------------------*/
	/* save the general registers on the stack 		*/
	/*------------------------------------------------------*/
	s390_stm (code, s390_r0, s390_r13, STK_BASE, S390_THROWSTACK_INTREGS);

	s390_lr  (code, s390_r1, STK_BASE);
	s390_ahi (code, s390_r1, alloc_size);
	/*------------------------------------------------------*/
	/* save the return address in the parameter register    */
	/*------------------------------------------------------*/
	s390_l   (code, s390_r3, 0, s390_r1, S390_RET_ADDR_OFFSET);

	/*------------------------------------------------------*/
	/* save the floating point registers 			*/
	/*------------------------------------------------------*/
	pos = S390_THROWSTACK_FLTREGS;
	for (i = 0; i < 16; ++i) {
		s390_std (code, i, 0,STK_BASE, pos);
		pos += sizeof (gdouble);
	}
	/*------------------------------------------------------*/
	/* save the access registers         			*/
	/*------------------------------------------------------*/
	s390_stam (code, s390_r0, s390_r15, STK_BASE, S390_THROWSTACK_ACCREGS);

	/*------------------------------------------------------*/
	/* call throw_exception (exc, ip, sp, gr, fr, ar)       */
	/* exc is already in place in r2 			*/
	/*------------------------------------------------------*/
	s390_lr   (code, s390_r4, s390_r1);        /* caller sp */
	/*------------------------------------------------------*/
	/* pointer to the saved int regs 			*/
	/*------------------------------------------------------*/
	s390_la	  (code, s390_r5, 0, STK_BASE, S390_THROWSTACK_INTREGS);
	s390_la   (code, s390_r6, 0, STK_BASE, S390_THROWSTACK_FLTREGS);
	s390_la   (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCREGS);
	s390_st	  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCPRM);
	s390_stfpc(code, STK_BASE, S390_THROWSTACK_FPCPRM);
	s390_lhi  (code, s390_r7, rethrow);
	s390_st	  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_RETHROW);
	offset = (guint32) S390_RELATIVE(throw_exception, code);
	s390_brasl(code, s390_r14, offset);
	/* we should never reach this breakpoint */
	s390_break (code);
	g_assert ((code - start) < size);
	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_throw_exception                          */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc);                */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	static guint8 *start;
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (SZ_THROW);
	get_throw_exception_generic (start, SZ_THROW, FALSE, FALSE);
	inited = 1;
	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_rethrow_exception                        */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc);                */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer 
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	static guint8 *start;
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (SZ_THROW);
	get_throw_exception_generic (start, SZ_THROW, FALSE, TRUE);
	inited = 1;
	return start;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_throw_exception_by_name                  */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise corlib exceptions. The return function has  */
/*                the following signature:                          */
/*                void (*func) (char *exc_name);                    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8 *start;
	static int inited = 0;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (SZ_THROW);
	get_throw_exception_generic (start, SZ_THROW, TRUE, FALSE);
	inited = 1;
	return start;
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_find_jit_info                           */
/*                                                                  */
/* Function	- See exceptions-amd64.c for docs.                      */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	MonoS390StackFrame *sframe;

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		gint32 address;

		frame->type = FRAME_TYPE_MANAGED;

		if (*lmf && (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		sframe = (MonoS390StackFrame *) MONO_CONTEXT_GET_SP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->prev);
		sframe = (MonoS390StackFrame *) sframe->prev;
		MONO_CONTEXT_SET_IP (new_ctx, (guint8*)sframe->return_address - 2);
		memcpy (&new_ctx->uc_mcontext.gregs[6], sframe->regs, (8*sizeof(gint32)));
		return TRUE;
	} else if (*lmf) {
		if (!(*lmf)->method)
			return FALSE;

		ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->eip, NULL);
		if (!ji) {
			// FIXME: This can happen with multiple appdomains (bug #444383)
			return FALSE;
		}

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		memcpy(new_ctx->uc_mcontext.gregs, (*lmf)->gregs, sizeof((*lmf)->gregs));
		memcpy(new_ctx->uc_mcontext.fpregs.fprs, (*lmf)->fregs, sizeof((*lmf)->fregs));

		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip - 2);
		*lmf = (*lmf)->previous_lmf;

		return TRUE;
	}

	return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_handle_exception                        */
/*                                                                  */
/* Function	- Handle an exception raised by the JIT code.	    */
/*                                                                  */
/* Parameters   - ctx       - Saved processor state                 */
/*                obj       - The exception object                  */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_handle_exception (void *uc, gpointer obj)
{
	return mono_handle_exception (uc, obj);
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_ip_from_context                         */
/*                                                                  */
/* Function	- Return the instruction pointer from the context.  */
/*                                                                  */
/* Parameters   - sigctx    - Saved processor state                 */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	return context_get_ip (sigctx);
}


/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_get_restore_context                    */
/*                                                                  */
/* Function	- Return the address of the routine that will rest- */
/*                ore the context.                                  */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	g_assert (!aot);
	if (info)
		*info = NULL;

	return setcontext;
}


/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_is_int_overflow                         */
/*                                                                  */
/* Function	- Inspect the code that raised the SIGFPE signal    */
/*		  to see if the DivideByZero or Arithmetic exception*/
/*		  should be raised.                  		    */
/*		                               			    */
/*------------------------------------------------------------------*/

gboolean
mono_arch_is_int_overflow (void *uc, void *info)
{
	MonoContext *ctx;
	guint8      *code;
	guint32     *operand;
	gboolean    arithExc = TRUE;
	gint	    regNo,
	    	    offset;

	ctx  = (MonoContext *) uc;
	code =  (guint8 *) ((siginfo_t *)info)->si_addr;
	/*----------------------------------------------------------*/
	/* Divide operations are the only ones that will give the   */
	/* divide by zero exception so just check for these ops.    */
	/*----------------------------------------------------------*/
	switch (code[0]) {
		case 0x1d :		/* Divide Register	    */
			regNo = code[1] & 0x0f;	
			if (ctx->uc_mcontext.gregs[regNo] == 0)
				arithExc = FALSE;
		break;
		case 0x5d :		/* Divide		    */
			regNo   = (code[2] & 0xf0 >> 8);	
			offset  = *((guint16 *) code+2) & 0x0fff;
			operand = (guint32*)(ctx->uc_mcontext.gregs[regNo] + offset);
			if (*operand == 0)
				arithExc = FALSE; 
		break;
		case 0xb9 :		/* Divide logical Register? */
			if (code[1] == 0x97) {
				regNo = (code[2] & 0xf0 >> 8);	
				if (ctx->uc_mcontext.gregs[regNo] == 0)
					arithExc = FALSE;
			}
		break;
		case 0xe3 :		/* Divide logical?	    */
			if (code[1] == 0x97) {	
				regNo   = (code[2] & 0xf0 >> 8);	
				offset  = *((guint32 *) code+1) & 0x000fffff;
				operand = (guint32*)(ctx->uc_mcontext.gregs[regNo] + offset);
				if (*operand == 0)
					arithExc = FALSE; 
			}
		break;
		default:
			arithExc = TRUE;
	}
	ctx->uc_mcontext.psw.addr = (guint32)code;
	return (arithExc);
}

/*========================= End of Function ========================*/
