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

#define MONO_CONTEXT_SET_IP(ctx,ip) 					\
	do {								\
		(ctx)->uc_mcontext.gregs[14] = (unsigned long)ip;	\
		(ctx)->uc_mcontext.psw.addr = (unsigned long)ip;	\
	} while (0); 

#define MONO_CONTEXT_SET_BP(ctx,bp) 					\
	do {		 						\
		(ctx)->uc_mcontext.gregs[15] = (unsigned long)bp;	\
		(ctx)->uc_stack.ss_sp	     = (unsigned long)bp;	\
	} while (0); 

#define MONO_CONTEXT_GET_IP(ctx) context_get_ip ((ctx))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[15]))

#define S390_CALLFILTER_INTREGS   	S390_MINIMAL_STACK_SIZE
#define S390_CALLFILTER_FLTREGS		S390_CALLFILTER_INTREGS+(16*sizeof(gulong))
#define S390_CALLFILTER_ACCREGS		S390_CALLFILTER_FLTREGS+(16*sizeof(gdouble))
#define S390_CALLFILTER_SIZE		(S390_CALLFILTER_ACCREGS+(16*sizeof(gulong)))

#define S390_THROWSTACK_ACCPRM		S390_MINIMAL_STACK_SIZE
#define S390_THROWSTACK_FPCPRM		S390_THROWSTACK_ACCPRM+sizeof(gpointer)
#define S390_THROWSTACK_INTREGS		S390_THROWSTACK_FPCPRM+sizeof(gulong)
#define S390_THROWSTACK_FLTREGS		S390_THROWSTACK_INTREGS+(16*sizeof(gulong))
#define S390_THROWSTACK_ACCREGS		S390_THROWSTACK_FLTREGS+(16*sizeof(gdouble))
#define S390_THROWSTACK_SIZE		(S390_THROWSTACK_ACCREGS+(16*sizeof(gulong)))

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
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

typedef struct
{
  void *prev;
  void *unused[5];
  void *regs[8];
  void *return_address;
} MonoS390StackFrame;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

gboolean mono_arch_handle_exception (void     *ctx,
				     gpointer obj, 
				     gboolean test_only);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

/*====================== End of Global Variables ===================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- context_get_ip                                    */
/*                                                                  */
/* Function	- Extract the current instruction address from the  */
/*		  context.                     		 	    */
/*		                               		 	    */
/*------------------------------------------------------------------*/

static inline gpointer 
context_get_ip (MonoContext *ctx) 
{
	gpointer ip;

	ip = (gpointer) ((gint32) (ctx->uc_mcontext.psw.addr) & 0x7fffffff);
	return ip;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_has_unwind_info                         */
/*                                                                  */
/* Function	- Tests if a function has a DWARF exception table   */
/*		  that is able to restore all caller saved registers*/
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_call_filter                              */
/*                                                                  */
/* Function	- Return a pointer to a method which calls an       */
/*                exception filter. We also use this function to    */
/*                call finally handlers (we pass NULL as @exc       */
/*                object in this case).                             */
/*                                                                  */
/*------------------------------------------------------------------*/

static gpointer
arch_get_call_filter (void)
{
	static guint8 start [512];
	static int inited = 0;
	guint8 *code;
	int alloc_size, pos, i;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start;

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
	s390_lm   (code, s390_r3, s390_r12, s390_r13, G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[3]));
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

	g_assert ((code - start) < sizeof(start));
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
		 gulong *int_regs, gdouble *fp_regs, gulong *acc_regs, guint fpc)
{
	static void (*restore_context) (MonoContext *);
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
		mono_ex->stack_trace = NULL;
	}
	mono_arch_handle_exception (&ctx, exc, FALSE);
	setcontext(&ctx);

	g_assert_not_reached ();
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_throw_exception_generic                  */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise exceptions. The returned function has the   */
/*                following signature:                              */
/*                void (*func) (MonoException *exc); or,            */
/*                void (*func) (char *exc_name);                    */
/*                                                                  */
/*------------------------------------------------------------------*/

static gpointer 
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name)
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
mono_arch_get_throw_exception (void)
{
	static guint8 start [256];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE);
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
	static guint8 start [160];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE);
	inited = 1;
	return start;
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- glist_to_array                                    */
/*                                                                  */
/* Function	- Convert a list to a mono array.                   */
/*                                                                  */
/*------------------------------------------------------------------*/

static MonoArray *
glist_to_array (GList *list) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, mono_defaults.int_class, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_find_jit_info                           */
/*                                                                  */
/* Function	- This function is used to gather informatoin from  */
/*                @ctx. It returns the MonoJitInfo of the corres-   */
/*                ponding function, unwinds one stack frame and     */
/*                stores the resulting context into @new_ctx. It    */
/*                also stores a string describing the stack location*/
/*                into @trace (if not NULL), and modifies the @lmf  */
/*                if necessary. @native_offset returns the IP off-  */
/*                set from the start of the function or -1 if that  */
/*                informatoin is not available.                     */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, 
			 MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, 
			 int *native_offset, gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	unsigned long *ptr;
	char *p;
	MonoS390StackFrame *sframe;

	if (prev_ji && 
	    (ip > prev_ji->code_start && 
	    ((guint8 *) ip < ((guint8 *) prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mono_jit_info_table_find (domain, ip);

	if (trace)
		*trace = NULL;

	if (native_offset)
		*native_offset = -1;

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		char *source_location, *tmpaddr, *fname;
		gint32 address, iloffset;
		int offset;

		*new_ctx = *ctx;

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		if (native_offset)
			*native_offset = address;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace) {
			source_location = mono_debug_source_location_from_address (ji->method, address, NULL, domain);
			iloffset = mono_debug_il_offset_from_address (ji->method, address, domain);

			if (iloffset < 0)
				tmpaddr = g_strdup_printf ("<0x%08x>", address);
			else
				tmpaddr = g_strdup_printf ("[0x%08x]", iloffset);
		
			fname = mono_method_full_name (ji->method, TRUE);

			if (source_location)
				*trace = g_strdup_printf ("in %s (at %s) %s", tmpaddr, source_location, fname);
			else
				*trace = g_strdup_printf ("in %s %s", tmpaddr, fname);

			g_free (fname);
			g_free (source_location);
			g_free (tmpaddr);
		}
		sframe = (MonoS390StackFrame *) MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->prev);
		sframe = (MonoS390StackFrame *) sframe->prev;
		MONO_CONTEXT_SET_IP (new_ctx, sframe->return_address);
		memcpy (&new_ctx->uc_mcontext.gregs[6], sframe->regs, (8*sizeof(gint32)));
		*res = *ji;
		return res;
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if (trace)
			*trace = g_strdup_printf ("in (unmanaged) %s", mono_method_full_name ((*lmf)->method, TRUE));
		
		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
			*res = *ji;
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		memcpy(new_ctx->uc_mcontext.gregs, (*lmf)->gregs, sizeof((*lmf)->gregs));
		memcpy(new_ctx->uc_mcontext.fpregs.fprs, (*lmf)->fregs, sizeof((*lmf)->fregs));

		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		*lmf = (*lmf)->previous_lmf;

		return res;
		
	}

	return NULL;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- ves_icall_get_trace                               */
/*                                                                  */
/* Function	- 						    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	int i, len;
	
	if (ta == NULL) {
		return mono_array_new (domain, mono_defaults.stack_frame_class, 0);
	}

	len = mono_array_length (ta);

	res = mono_array_new (domain, mono_defaults.stack_frame_class, 
			      len > skip ? len - skip : 0);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gpointer ip = mono_array_get (ta, gpointer, i);

		ji = mono_jit_info_table_find (domain, ip);
		if (ji == NULL) {
			mono_array_set (res, gpointer, i, sf);
			continue;
		}

		sf->method = mono_method_get_object (domain, ji->method, NULL);
		sf->native_offset = (char *)ip - (char *)ji->code_start;

		sf->il_offset = mono_debug_il_offset_from_address (ji->method, sf->native_offset, domain);

		if (need_file_info) {
			gchar *filename;
			
			filename = mono_debug_source_location_from_address (ji->method, sf->native_offset, &sf->line, domain);

			sf->filename = filename? mono_string_new (domain, filename): NULL;
			sf->column = 0;

			g_free (filename);
		}

		mono_array_set (res, gpointer, i, sf);
	}

	return res;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_jit_walk_stack                               */
/*                                                                  */
/* Function	- 						    */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoContext ctx, new_ctx;

	MONO_CONTEXT_SET_IP (&ctx, __builtin_return_address (0));
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (1));

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, NULL, 
					      &ctx, &new_ctx, NULL, &lmf, 
					      &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = do_il_offset ? mono_debug_il_offset_from_address (ji->method, native_offset, domain): -1;

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- ves_icall_get_frame_info                          */
/*                                                                  */
/* Function	- 						    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	MonoContext ctx, new_ctx;

	MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));

	skip++;

	do {
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, NULL, 
					      &ctx, &new_ctx, NULL, &lmf, 
					      native_offset, NULL);

		ctx = new_ctx;
		
		if (!ji || ji == (gpointer)-1 || MONO_CONTEXT_GET_BP (&ctx) >= jit_tls->end_of_stack)
			return FALSE;

		/* skip all wrappers ??*/
		if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE)
			continue;

		skip--;

	} while (skip >= 0);

	*method = mono_method_get_object (domain, ji->method, NULL);
	*iloffset = mono_debug_il_offset_from_address (ji->method, *native_offset, domain);

	if (need_file_info) {
		gchar *filename;

		filename = mono_debug_source_location_from_address (ji->method, *native_offset, line, domain);

		*file = filename? mono_string_new (domain, filename): NULL;
		*column = 0;

		g_free (filename);
	}

	return TRUE;
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
/*                test_only - Only test if the exception is caught, */
/*                            but don't call handlers               */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_handle_exception (void *uc, gpointer obj, gboolean test_only)
{
	MonoContext	*ctx = uc;
	MonoDomain	*domain = mono_domain_get ();
	MonoJitInfo	*ji, rji;
	static int	(*call_filter) (MonoContext *, gpointer, gpointer) = NULL;
	MonoJitTlsData 	*jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF		*lmf = jit_tls->lmf;		
	GList		*trace_ips = NULL;
	GString		*traceStr  = NULL;
	MonoException 	*mono_ex;
	MonoString	*initialStackTrace = NULL;
	int		frameCount = 0;

	g_assert (ctx != NULL);
	memset(&rji, 0, sizeof(rji));
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		mono_ex->stack_trace = NULL;
	} else {
		mono_ex = NULL;
	}


	if (!call_filter)
		call_filter = arch_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		if (mono_jit_trace_calls != NULL)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!mono_arch_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			mono_unhandled_exception (obj);
		}
	}

	memset (&rji, 0, sizeof(rji));

	while (1) {
		MonoContext new_ctx;
		char *trace = NULL;
		gboolean needTrace = FALSE;

		if (test_only && (frameCount < 1000)) {
			needTrace = TRUE;
			if (!traceStr)
				traceStr = g_string_new ("");
		}	
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      test_only ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			frameCount++;

			if ((test_only) && 
			    (ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) && 
			    (mono_ex)) {
			    	if (!initialStackTrace && (frameCount < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));
					g_string_append   (traceStr, trace);
					g_string_append_c (traceStr, '\n');
				}
			}		
			
			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];
					gboolean filtered = FALSE;

					if (ei->try_start < MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */
						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) ||
						    (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							g_assert (ji->exvar_offset);
							*((gpointer *)((char *)MONO_CONTEXT_GET_BP (ctx) + ji->exvar_offset)) = obj;
							if (!initialStackTrace &&
							    traceStr) {
								mono_ex->stack_trace =  mono_string_new (domain, traceStr->str);
							}
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) 
						      filtered = call_filter (ctx, ei->data.filter, obj);

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) &&
						    (mono_object_isinst (obj, ei->data.catch_class)) || 
						    (filtered)) {
							if (test_only) {
								if (mono_ex) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips);
								}
								g_list_free (trace_ips);
								g_free (trace);
								if (traceStr)
									g_string_free (traceStr, TRUE);

								return TRUE;
							}

							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s - caught at %p with sp %p\n", 
									 i, mono_method_full_name (ji->method, TRUE),
									 ei->handler_start,
									 MONO_CONTEXT_GET_BP(ctx));
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							g_free (trace);
							if (traceStr)
								g_string_free (traceStr, TRUE);
							return FALSE;
						}

						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: finally clause %d of %s handled at: %p using sp: %p\n", 
								i, mono_method_full_name (ji->method, TRUE),
								ei->handler_start,
								MONO_CONTEXT_GET_BP(ctx));
							call_filter (ctx, ei->handler_start, NULL);
						}
						
					}
				}
			}
		}

		g_free (trace);
			
		*ctx = new_ctx;

		if ((ji == (gpointer)-1) || 
		    (MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack)) {
			if (!test_only) {
				jit_tls->lmf = lmf;
				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex) {
					trace_ips = g_list_reverse (trace_ips);
					mono_ex->trace_ips = glist_to_array (trace_ips);
				}
				g_list_free (trace_ips);
				if (traceStr)
					g_string_free (traceStr, TRUE);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
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

