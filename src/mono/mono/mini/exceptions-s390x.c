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

/* disable this for now */
#undef MONO_USE_EXC_TABLES

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

#include <mono/arch/s390x/s390x-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-s390x.h"

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

#ifdef MONO_USE_EXC_TABLES

/*************************************/
/*    STACK UNWINDING STUFF          */
/*************************************/

/* These definitions are from unwind-dw2.c in glibc 2.2.5 */

/* For x86 */
#define DWARF_FRAME_REGISTERS 17

typedef struct frame_state
{
  void *cfa;
  void *eh_ptr;
  long cfa_offset;
  long args_size;
  long reg_or_offset[DWARF_FRAME_REGISTERS+1];
  unsigned short cfa_reg;
  unsigned short retaddr_column;
  char saved[DWARF_FRAME_REGISTERS+1];
} frame_state;

typedef struct frame_state * (*framesf) (void *, struct frame_state *);

#endif

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

#ifdef MONO_USE_EXC_TABLES

static framesf frame_state_for = NULL;

static gboolean inited = FALSE;

typedef char ** (*get_backtrace_symbols_type) (void *__const *__array, int __size);

static get_backtrace_symbols_type get_backtrace_symbols = NULL;

#endif

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

#ifdef MONO_USE_EXC_TABLES

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- init_frame_state_for                              */
/*                                                                  */
/* Function	- Load the __frame_state_for from libc.             */
/*                There are two versions of __frame_state_for: one  */ 
/*		  in libgcc.a and the other in glibc.so. We need    */
/*                the version from glibc. For more information, see:*/
/*                http://gcc.gnu.org/ml/gcc/2002-08/msg00192.html   */
/*                                                                  */
/*------------------------------------------------------------------*/

static void
init_frame_state_for (void)
{
	GModule *module;

	if ((module = g_module_open ("libc.so.6", G_MODULE_BIND_LAZY))) {
	
		if (!g_module_symbol (module, "__frame_state_for", (gpointer*)&frame_state_for))
			frame_state_for = NULL;

		if (!g_module_symbol (module, "backtrace_symbols", (gpointer*)&get_backtrace_symbols)) {
			get_backtrace_symbols = NULL;
			frame_state_for = NULL;
		}

		g_module_close (module);
	}

	inited = TRUE;
}

/*========================= End of Function ========================*/

#endif

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
#if 0
	struct frame_state state_in;
	struct frame_state *res;

	if (!inited) 
		init_frame_state_for ();
	
	if (!frame_state_for)
		return FALSE;

	g_assert (method->addr);

	memset (&state_in, 0, sizeof (state_in));

	/* offset 10 is just a guess, but it works for all methods tested */
	if ((res = frame_state_for ((char *)method->addr + 10, &state_in))) {

		if (res->saved [X86_EBX] != 1 ||
		    res->saved [X86_EDI] != 1 ||
		    res->saved [X86_EBP] != 1 ||
		    res->saved [X86_ESI] != 1) {
			return FALSE;
		}
		return TRUE;
	} else {
		return FALSE;
	}
#else
	return FALSE;
#endif
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_unwind_native_frame                          */
/*                                                                  */
/* Function	- Use the context to unwind a stack frame.          */
/*                                                                  */
/*------------------------------------------------------------------*/

static MonoJitInfo *
s390_unwind_native_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
			  struct sigcontext *ctx, struct sigcontext *new_ctx, 
			  MonoLMF *lmf, char **trace)
{
#if 0
	struct stack_frame *frame;
	gpointer max_stack;
	MonoJitInfo *ji;
	struct frame_state state_in;
	struct frame_state *res;

	if (trace)
		*trace = NULL;

	if (!inited) 
		init_frame_state_for ();

	if (!frame_state_for)
		return FALSE;

	frame = MONO_CONTEXT_GET_BP (ctx);

	max_stack = lmf && lmf->method ? lmf : jit_tls->end_of_stack;

	*new_ctx = *ctx;

	memset (&state_in, 0, sizeof (state_in));

	while ((gpointer)frame->next < (gpointer)max_stack) {
		gpointer ip, addr = frame->return_address;
		void *cfa;
		char *tmp, **symbols;

		if (trace) {
			ip = MONO_CONTEXT_GET_IP (new_ctx);
			symbols = get_backtrace_symbols (&ip, 1);
			if (*trace)
				tmp = g_strdup_printf ("%s\nin (unmanaged) %s", *trace, symbols [0]);
			else
				tmp = g_strdup_printf ("in (unmanaged) %s", symbols [0]);

			free (symbols);
			g_free (*trace);
			*trace = tmp;
		}

		if ((res = frame_state_for (addr, &state_in))) {	
			int i;

			cfa = (gint8*) (get_sigcontext_reg (new_ctx, res->cfa_reg) + res->cfa_offset);
			frame = (struct stack_frame *)((gint8*)cfa - 8);
			for (i = 0; i < DWARF_FRAME_REGISTERS + 1; i++) {
				int how = res->saved[i];
				long val;
				g_assert ((how == 0) || (how == 1));
			
				if (how == 1) {
					val = * (long*) ((gint8*)cfa + res->reg_or_offset[i]);
					set_sigcontext_reg (new_ctx, i, val);
				}
			}
			new_ctx->SC_ESP = (long)cfa;

			if (res->saved [X86_EBX] == 1 &&
			    res->saved [X86_EDI] == 1 &&
			    res->saved [X86_EBP] == 1 &&
			    res->saved [X86_ESI] == 1 &&
			    (ji = mono_jit_info_table_find (domain, frame->return_address))) {
				//printf ("FRAME CFA %s\n", mono_method_full_name (ji->method, TRUE));
				return ji;
			}

		} else {

			MONO_CONTEXT_SET_IP (new_ctx, frame->return_address);
			frame = frame->next;
			MONO_CONTEXT_SET_BP (new_ctx, frame);

			/* stop if !frame or when we detect an unexpected managed frame */
			if (!frame || mono_jit_info_table_find (domain, frame->return_address)) {
				if (trace) {
					g_free (*trace);
					*trace = NULL;
				}
				return NULL;
			}
		}
	}

	if (trace) {
		g_free (*trace);
		*trace = NULL;
	}
#endif
	return NULL;
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

	s390_stmg(code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lgr (code, s390_r14, STK_BASE);
	alloc_size = S390_ALIGN(S390_CALLFILTER_SIZE, S390_STACK_ALIGNMENT);
	s390_aghi(code, STK_BASE, -alloc_size);
	s390_stg (code, s390_r14, 0, STK_BASE, 0);

	/*------------------------------------------------------*/
	/* save general registers on stack			*/
	/*------------------------------------------------------*/
	s390_stmg(code, s390_r0, s390_r13, STK_BASE, S390_CALLFILTER_INTREGS);

	/*------------------------------------------------------*/
	/* Get A(Context)					*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r13, s390_r2);

	/*------------------------------------------------------*/
	/* Get A(Handler Entry Point)				*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r0, s390_r3);

	/*------------------------------------------------------*/
	/* Set parameter register with Exception		*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r2, s390_r4);

	/*------------------------------------------------------*/
	/* Load all registers with values from the context	*/
	/*------------------------------------------------------*/
	s390_lmg  (code, s390_r3, s390_r12, s390_r13, G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[3]));
	pos = G_STRUCT_OFFSET(MonoContext, uc_mcontext.fpregs.fprs[0]);
	for (i = 0; i < 16; ++i) {
		s390_ld  (code, i, 0, s390_r13, pos);
		pos += sizeof(gdouble);
	}
	
	/*------------------------------------------------------*/
	/* Point at the copied stack frame and call the filter	*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r1, s390_r0);
	s390_basr (code, s390_r14, s390_r1);

	/*------------------------------------------------------*/
	/* Save return value					*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r14, s390_r2);

	/*------------------------------------------------------*/
	/* Restore all the regs from the stack 			*/
	/*------------------------------------------------------*/
	s390_lmg  (code, s390_r0, s390_r13, STK_BASE, S390_CALLFILTER_INTREGS);

	s390_lgr  (code, s390_r2, s390_r14);
	s390_aghi (code, s390_r15, alloc_size);
	s390_lmg  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
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

	s390_stmg(code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	alloc_size = S390_ALIGN(S390_THROWSTACK_SIZE, S390_STACK_ALIGNMENT);
	s390_lgr  (code, s390_r14, STK_BASE);
	s390_aghi (code, STK_BASE, -alloc_size);
	s390_stg  (code, s390_r14, 0, STK_BASE, 0);
	if (by_name) {
		s390_lgr  (code, s390_r4, s390_r2);
		s390_bras (code, s390_r13, 6);
		s390_llong(code, mono_defaults.corlib);
		s390_llong(code, "System");
		s390_lg	  (code, s390_r2, 0, s390_r13, 0);
		s390_lg	  (code, s390_r3, 0, s390_r13, 4);
		offset = (guint32) S390_RELATIVE(mono_exception_from_name, code);
		s390_brasl(code, s390_r14, offset);
	}
	/*------------------------------------------------------*/
	/* save the general registers on the stack 		*/
	/*------------------------------------------------------*/
	s390_stmg(code, s390_r0, s390_r13, STK_BASE, S390_THROWSTACK_INTREGS);

	s390_lgr (code, s390_r1, STK_BASE);
	s390_aghi(code, s390_r1, alloc_size);
	/*------------------------------------------------------*/
	/* save the return address in the parameter register    */
	/*------------------------------------------------------*/
	s390_lg  (code, s390_r3, 0, s390_r1, S390_RET_ADDR_OFFSET);

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
	s390_lgr  (code, s390_r4, s390_r1);        /* caller sp */
	/*------------------------------------------------------*/
	/* pointer to the saved int regs 			*/
	/*------------------------------------------------------*/
	s390_la	  (code, s390_r5, 0, STK_BASE, S390_THROWSTACK_INTREGS);
	s390_la   (code, s390_r6, 0, STK_BASE, S390_THROWSTACK_FLTREGS);
	s390_la   (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCREGS);
	s390_stg  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCPRM);
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
	static guint8 start [384];
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
	static guint8 start [384];
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
//		memcpy(new_ctx, ctx, sizeof(*new_ctx));

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
#ifdef MONO_USE_EXC_TABLES
	} else if ((ji = s390_unwind_native_frame (domain, jit_tls, ctx, new_ctx, *lmf, trace))) {
		*res = *ji;
		return res;
#endif
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

/*
		MONO_CONTEXT_SET_BP (ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (ctx, (*lmf)->eip);
*/
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
	MonoException 	*mono_ex;

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

	while (1) {
		MonoContext new_ctx;
		char *trace = NULL;
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      test_only ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				char *tmp, *strace;

				trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));

				if (!mono_ex->stack_trace)
					strace = g_strdup ("");
				else
					strace = mono_string_to_utf8 (mono_ex->stack_trace);
			
				tmp = g_strdup_printf ("%s%s\n", strace, trace);
				g_free (strace);

				mono_ex->stack_trace = mono_string_new (domain, tmp);

				g_free (tmp);
			}

			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start < MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */
						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (ji->method->klass->image, ei->data.token))) ||
						    ((ei->flags == MONO_EXCEPTION_CLAUSE_FILTER &&
						      call_filter (ctx, ei->data.filter, obj)))) {
							if (test_only) {
								if (mono_ex) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips);
								}
								g_list_free (trace_ips);
								g_free (trace);
								return TRUE;
							}
//							memcpy(ctx, &new_ctx, sizeof(new_ctx));
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s - caught at %p with sp %p\n", 
									 i, mono_method_full_name (ji->method, TRUE),
									 ei->handler_start,
									 MONO_CONTEXT_GET_BP(ctx));
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							*((gpointer *)((char *)MONO_CONTEXT_GET_BP (ctx) + ji->exvar_offset)) = obj;
							jit_tls->lmf = lmf;
							g_free (trace);
							return 0;
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

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
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

