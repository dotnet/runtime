/*
 * exceptions-arm.c: exception support for ARM
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <ucontext.h>

#include <mono/arch/arm/arm-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-arm.h"

static gboolean arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only);

/*

struct sigcontext {
	unsigned long trap_no;
	unsigned long error_code;
	unsigned long oldmask;
	unsigned long arm_r0;
	unsigned long arm_r1;
	unsigned long arm_r2;
	unsigned long arm_r3;
	unsigned long arm_r4;
	unsigned long arm_r5;
	unsigned long arm_r6;
	unsigned long arm_r7;
	unsigned long arm_r8;
	unsigned long arm_r9;
	unsigned long arm_r10;
	unsigned long arm_fp;
	unsigned long arm_ip;
	unsigned long arm_sp;
	unsigned long arm_lr;
	unsigned long arm_pc;
	unsigned long arm_cpsr;
	unsigned long fault_address;
};

gregs below is this struct
struct user_regs {
	unsigned long int uregs[18];
};

the companion user_fpregs has just 8 double registers
(it's valid for FPA mode, will need changes for VFP)

typedef struct {
	gregset_t gregs;
	fpregset_t fpregs;
} mcontext_t;
	    
typedef struct ucontext {
	unsigned long int uc_flags;
	struct ucontext *uc_link;
	__sigset_t uc_sigmask;
	stack_t uc_stack;
	mcontext_t uc_mcontext;
	long int uc_filler[5];
} ucontext_t;

*/


#define restore_regs_from_context(ctx_reg,ip_reg,tmp_reg) do {	\
		int reg;	\
		ARM_LDR_IMM (code, ip_reg, ctx_reg, G_STRUCT_OFFSET (MonoContext, eip));	\
		ARM_ADD_REG_IMM8 (code, tmp_reg, ctx_reg, G_STRUCT_OFFSET(MonoContext, regs));	\
		ARM_LDMIA (code, tmp_reg, MONO_ARM_REGSAVE_MASK);	\
	} while (0)

/* nothing to do */
#define setup_context(ctx)

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 * The first argument in r0 is the pointer to the context.
 */
static gpointer
arch_get_restore_context (void)
{
	guint8 *code;
	static guint8 start [128];
	static int inited = 0;

	if (inited)
		return start;
	inited = 1;

	code = start;
	restore_regs_from_context (ARMREG_R0, ARMREG_R1, ARMREG_R2);
	/* restore also the stack pointer, FIXME: handle sp != fp */
	ARM_LDR_IMM (code, ARMREG_SP, ARMREG_R0, G_STRUCT_OFFSET (MonoContext, ebp));
	/* jump to the saved IP */
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
	/* never reached */
	ARM_DBRK (code);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
static gpointer
arch_get_call_filter (void)
{
	static guint8 start [320];
	static int inited = 0;
	guint8 *code;
	int alloc_size, pos, i;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start;

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	/* restore all the regs from ctx (in r0), but not sp, the stack pointer */
	restore_regs_from_context (ARMREG_R0, ARMREG_IP, ARMREG_LR);
	/* call handler at eip (r1) and set the first arg with the exception (r2) */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_R2);
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);

	/* epilog */
	ARM_POP_NWB (code, 0xff0 | ((1 << ARMREG_SP) | (1 << ARMREG_PC)));

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;
	gboolean rethrow = eip & 1;

	if (!restore_context)
		restore_context = arch_get_restore_context ();

	eip &= ~1; /* clear the optional rethrow bit */
	/* adjust eip so that it point into the call instruction */
	eip -= 4;

	setup_context (&ctx);

	/*printf ("stack in throw: %p\n", esp);*/
	MONO_CONTEXT_SET_BP (&ctx, esp);
	MONO_CONTEXT_SET_IP (&ctx, eip);
	memcpy (&ctx.regs, int_regs, sizeof (gulong) * 8);
	/* memcpy (&ctx.fregs, fp_regs, sizeof (double) * MONO_SAVED_FREGS); */

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	arch_handle_exception (&ctx, exc, FALSE);
	restore_context (&ctx);
	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception_generic:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (char *exc_name);
 *
 */
static gpointer 
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name, gboolean rethrow)
{
	guint8 *code;
	int alloc_size, pos, i;

	code = start;

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	if (by_name) {
		/* r0 has the name of the exception: get the object */
		ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_R0);
		code = mono_arm_emit_load_imm (code, ARMREG_R0, GPOINTER_TO_UINT (mono_defaults.corlib));
		code = mono_arm_emit_load_imm (code, ARMREG_R1, GPOINTER_TO_UINT ("System"));
		code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (mono_exception_from_name));
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
	}

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* caller sp */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, 10 * 4); /* 10 saved regs */
	/* exc is already in place in r0 */
	if (by_name)
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_SP, 9 * 4); /* pos on the stack were lr was saved */
	else
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR); /* caller ip */
	/* FIXME: pointer to the saved fp regs */
	/*pos = alloc_size - sizeof (double) * MONO_SAVED_FREGS;
	ppc_addi (code, ppc_r7, ppc_sp, pos);*/
	/* pointer to the saved int regs */
	ARM_MOV_REG_REG (code, ARMREG_R3, ARMREG_SP); /* the pushed regs */
	/* we encode rethrow in the ip, so we avoid args on the stack */
	ARM_ORR_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, rethrow);

	code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (throw_exception));
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
	/* we should never reach this breakpoint */
	ARM_DBRK (code);
	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	return start;
}

/**
 * mono_arch_get_rethrow_exception:
 *
 * Returns a function pointer which can be used to rethrow 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer
mono_arch_get_rethrow_exception (void)
{
	static guint8 start [132];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE);
	inited = 1;
	return start;
}
/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, mono_get_exception_arithmetic ()); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint8 start [132];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE);
	inited = 1;
	return start;
}

/**
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, "ArithmeticException"); 
 * x86_call_code (code, arch_get_throw_exception_by_name ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8 start [168];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE, FALSE);
	inited = 1;
	return start;
}	

static MonoArray *
glist_to_array (GList *list, MonoClass *eclass) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, eclass, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
}

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji,
			 MonoContext *ctx, MonoContext *new_ctx, char **trace, MonoLMF **lmf,
			 int *native_offset, gboolean *managed)
{
	return NULL;
}
#if 0
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	unsigned long *ptr;
	char *p;

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
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
		int offset, i;
		gulong *ctx_regs;

		*new_ctx = *ctx;
		setup_context (new_ctx);

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
				tmpaddr = g_strdup_printf ("<0x%05x>", address);
			else
				tmpaddr = g_strdup_printf ("[0x%05x]", iloffset);
		
			fname = mono_method_full_name (ji->method, TRUE);

			if (source_location)
				*trace = g_strdup_printf ("in %s (at %s) %s", tmpaddr, source_location, fname);
			else
				*trace = g_strdup_printf ("in %s %s", tmpaddr, fname);

			g_free (fname);
			g_free (source_location);
			g_free (tmpaddr);
		}
#if ARM_PORT
		sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
#endif
		if (ji->method->save_lmf) {
#if ARM_PORT
			memcpy (&new_ctx->fregs, (char*)sframe->sp - sizeof (double) * MONO_SAVED_FREGS, sizeof (double) * MONO_SAVED_FREGS);
			memcpy (&new_ctx->regs, (char*)sframe->sp - sizeof (double) * MONO_SAVED_FREGS - sizeof (gulong) * MONO_SAVED_GREGS, sizeof (gulong) * MONO_SAVED_GREGS);
#endif
		} else if (ji->used_regs) {
#if ARM_PORT
			/* keep updated with emit_prolog in mini-ppc.c */
			offset = 0;
			/* FIXME handle floating point args 
			for (i = 31; i >= 14; --i) {
				if (ji->used_fregs & (1 << i)) {
					offset += sizeof (double);
					new_ctx->fregs [i - 14] = *(gulong*)((char*)sframe->sp - offset);
				}
			}*/
			for (i = 31; i >= 13; --i) {
				if (ji->used_regs & (1 << i)) {
					offset += sizeof (gulong);
					new_ctx->regs [i - 13] = *(gulong*)((char*)sframe->sp - offset);
				}
			}
		}
		/* the calling IP is in the parent frame */
		sframe = (MonoPPCStackFrame*)sframe->sp;
		/* we substract 4, so that the IP points into the call instruction */
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr - 4);
#endif

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;
		setup_context (new_ctx);

		if (!(*lmf)->method)
			return (gpointer)-1;

		if (trace) {
			char *fname = mono_method_full_name ((*lmf)->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
		
		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

#if ARM_PORT
		/*sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr);*/
		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		memcpy (&new_ctx->regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->fregs, (*lmf)->fregs, sizeof (double) * MONO_SAVED_FREGS);
#endif
		*lmf = (*lmf)->previous_lmf;

		return ji ? ji : res;
	}

	return NULL;
}
#endif

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	struct ucontext *uc = ctx;
	MonoContext mctx;
	gboolean result;

	mctx.eip = uc->uc_mcontext.gregs [ARMREG_PC];
	mctx.ebp = uc->uc_mcontext.gregs [ARMREG_SP];
	memcpy (&mctx.regs, &uc->uc_mcontext.gregs [ARMREG_R4], sizeof (gulong) * 8);
	/* memcpy (&mctx.fregs, &uc->uc_mcontext.uc_regs->fpregs.fpregs [14], sizeof (double) * MONO_SAVED_FREGS);*/

	result = arch_handle_exception (&mctx, obj, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	uc->uc_mcontext.gregs [ARMREG_PC] = mctx.eip;
	uc->uc_mcontext.gregs [ARMREG_SP] = mctx.ebp;
	memcpy (&uc->uc_mcontext.gregs [ARMREG_R4], &mctx.regs, sizeof (gulong) * 8);
	/* memcpy (&uc->uc_mcontext.uc_regs->fpregs.fpregs [14], &mctx.fregs, sizeof (double) * MONO_SAVED_FREGS);*/
	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext.gregs [ARMREG_PC];
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 *
 */
static gboolean
arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer, gpointer) = NULL;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	MonoArray *initial_trace_ips = NULL;
	int frame_count = 0;
	gboolean has_dynamic_methods = FALSE;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_trace_ips = mono_ex->trace_ips;
	} else {
		mono_ex = NULL;
	}


	if (!call_filter)
		call_filter = arch_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		setup_context (&ctx_cp);
		if (mono_jit_trace_calls != NULL)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!arch_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			mono_unhandled_exception (obj);
		}
	}

	memset (&rji, 0, sizeof (rji));

	while (1) {
		MonoContext new_ctx;

		setup_context (&new_ctx);
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			frame_count ++;
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				/* 
				 * Avoid overwriting the stack trace if the exception is
				 * rethrown. Also avoid giant stack traces during a stack
				 * overflow.
				 */
				if (!initial_trace_ips && (frame_count < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));

				}
			}

			if (ji->method->dynamic)
				has_dynamic_methods = TRUE;

			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];
					gboolean filtered = FALSE;

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object int cfg->excvar */
							g_assert (ei->exvar_offset);
							/* need to use the frame pointer (ppc_r31), not r1 (regs start from register r13): methods with clauses always have r31 */
							// ARM_PORT *((gpointer *)((char *)(ctx->regs [ppc_r31-13]) + ei->exvar_offset)) = obj;
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
							filtered = call_filter (ctx, ei->data.filter, mono_ex);

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, ei->data.catch_class)) || filtered) {
							if (test_only) {
								if (mono_ex && !initial_trace_ips) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips, mono_defaults.int_class);
									if (has_dynamic_methods)
										/* These methods could go away anytime, so compute the stack trace now */
										mono_ex->stack_trace = ves_icall_System_Exception_get_trace (mono_ex);
								}
								g_list_free (trace_ips);
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							/*printf ("stack for catch: %p\n", MONO_CONTEXT_GET_BP (ctx));*/
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							return 0;
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							call_filter (ctx, ei->handler_start, NULL);
						}
						
					}
				}
			}
		}

		*ctx = new_ctx;
		setup_context (ctx);

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
			if (!test_only) {
				jit_tls->lmf = lmf;
				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex && !initial_trace_ips) {
					trace_ips = g_list_reverse (trace_ips);
					mono_ex->trace_ips = glist_to_array (trace_ips, mono_defaults.int_class);
					if (has_dynamic_methods)
						/* These methods could go away anytime, so compute the stack trace now */
						mono_ex->stack_trace = ves_icall_System_Exception_get_trace (mono_ex);
				}
				g_list_free (trace_ips);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

