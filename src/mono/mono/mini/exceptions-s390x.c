/**
 * \file
 *
 * Function    - Exception support for S/390.
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

#define S390_CALLFILTER_INTREGS   	S390_MINIMAL_STACK_SIZE
#define S390_CALLFILTER_FLTREGS		(S390_CALLFILTER_INTREGS+(16*sizeof(gulong)))
#define S390_CALLFILTER_ACCREGS		(S390_CALLFILTER_FLTREGS+(16*sizeof(gdouble)))
#define S390_CALLFILTER_SIZE		(S390_CALLFILTER_ACCREGS+(16*sizeof(gint32)))

#define S390_THROWSTACK_ACCPRM		S390_MINIMAL_STACK_SIZE				// 160
#define S390_THROWSTACK_FPCPRM		(S390_THROWSTACK_ACCPRM+sizeof(gpointer))	// 168
#define S390_THROWSTACK_RETHROW		(S390_THROWSTACK_FPCPRM+sizeof(gulong))		// 170
#define S390_THROWSTACK_PRESERVE_IPS	(S390_THROWSTACK_RETHROW+sizeof(gulong))  	// 178
#define S390_THROWSTACK_INTREGS		(S390_THROWSTACK_PRESERVE_IPS+sizeof(gulong))	// 180
#define S390_THROWSTACK_FLTREGS		(S390_THROWSTACK_INTREGS+(16*sizeof(gulong)))	// 308
#define S390_THROWSTACK_ACCREGS		(S390_THROWSTACK_FLTREGS+(16*sizeof(gdouble)))	// 430
#define S390_THROWSTACK_SIZE		(S390_THROWSTACK_ACCREGS+(16*sizeof(gint32)))	// 494

#define SZ_THROW	384

#define setup_context(ctx)

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
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-hwcap.h>

#include "mini.h"
#include "mini-s390x.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "mono/utils/mono-tls-inline.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                   P r o t o t y p e s                            */
/*------------------------------------------------------------------*/

static void throw_exception (MonoObject *, unsigned long, unsigned long, 
		 host_mgreg_t *, gdouble *, gint32 *, guint, gboolean, gboolean);
static gpointer mono_arch_get_throw_exception_generic (int, MonoTrampInfo **, 
				int, gboolean, gboolean, gboolean);
static void handle_signal_exception (gpointer);

/*========================= End of Prototypes ======================*/

/*------------------------------------------------------------------*/
/*                 G l o b a l   V a r i a b l e s                  */
/*------------------------------------------------------------------*/

typedef enum {
	by_none,
	by_token
} throwType;

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
	int gr_offset, alloc_size, pos, i;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	g_assert (!aot);

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start = (guint8 *) mono_global_codeman_reserve (512);

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, STK_BASE, S390_CFA_OFFSET);
	s390_stmg (code, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	gr_offset = S390_REG_SAVE_OFFSET - S390_CFA_OFFSET;
	for (i = s390_r6; i <= s390_r15; i++) {
		mono_add_unwind_op_offset (unwind_ops, code, start, i, gr_offset);
		gr_offset += sizeof(uintptr_t);
	}
	s390_lgr  (code, s390_r14, STK_BASE);
	alloc_size = S390_ALIGN(S390_CALLFILTER_SIZE, S390_STACK_ALIGNMENT);
	s390_aghi (code, STK_BASE, -alloc_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, alloc_size + S390_CFA_OFFSET);
	s390_stg  (code, s390_r14, 0, STK_BASE, 0);

	/*------------------------------------------------------*/
	/* save general registers on stack			*/
	/*------------------------------------------------------*/
	s390_stmg (code, s390_r0, STK_BASE, STK_BASE, S390_CALLFILTER_INTREGS);

	/*------------------------------------------------------*/
	/* save floating point registers on stack		*/
	/*------------------------------------------------------*/
	pos = S390_CALLFILTER_FLTREGS;
	for (i = 0; i < 16; ++i) {
		s390_std (code, i, 0, STK_BASE, pos);
		pos += sizeof (gdouble);
	}

	/*------------------------------------------------------*/
	/* save access registers on stack       		*/
	/*------------------------------------------------------*/
	s390_stam (code, s390_a0, s390_a15, STK_BASE, S390_CALLFILTER_ACCREGS);

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
	s390_lmg  (code, s390_r3, s390_r12, s390_r13, 
		   G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[3]));
	pos = G_STRUCT_OFFSET(MonoContext, uc_mcontext.fpregs.fprs[0]);
	for (i = 0; i < 16; ++i) {
		s390_ld  (code, i, 0, s390_r13, pos);
		pos += sizeof(gdouble);
	}

#if 0
	/*------------------------------------------------------*/
	/* We need to preserve current SP before calling filter */
	/* with SP from the context				*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r14, STK_BASE);
	s390_lg	  (code, STK_BASE, 0, s390_r13,
		   G_STRUCT_OFFSET(MonoContext, uc_mcontext.gregs[15]));
	s390_lgr  (code, s390_r13, s390_r14);
#endif

	/*------------------------------------------------------*/
	/* Go call filter   					*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r1, s390_r0);
	s390_basr (code, s390_r14, s390_r1);

	/*------------------------------------------------------*/
	/* Save return value					*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r14, s390_r2);

#if 0
	/*------------------------------------------------------*/
	/* Reload our stack register with value saved in context*/
	/*------------------------------------------------------*/
	s390_lgr  (code, STK_BASE, s390_r13);
#endif

	/*------------------------------------------------------*/
	/* Restore all the regs from the stack 			*/
	/*------------------------------------------------------*/
	s390_lmg  (code, s390_r0, s390_r13, STK_BASE, S390_CALLFILTER_INTREGS);
	pos = S390_CALLFILTER_FLTREGS;
	for (i = 0; i < 16; ++i) {
		s390_ld (code, i, 0, STK_BASE, pos);
		pos += sizeof (gdouble);
	}

	s390_lgr  (code, s390_r2, s390_r14);
	s390_lam  (code, s390_a0, s390_a15, STK_BASE, S390_CALLFILTER_ACCREGS);
	s390_aghi (code, s390_r15, alloc_size);
	s390_lmg  (code, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br   (code, s390_r14);

	g_assert ((code - start) < SZ_THROW); 

	mono_arch_flush_icache(start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("call_filter",
						start, code - start, ji,
						unwind_ops);

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
		 host_mgreg_t *int_regs, gdouble *fp_regs, gint32 *acc_regs,
		 guint fpc, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;
	int iReg;

	memset(&ctx, 0, sizeof(ctx));

	setup_context(&ctx);

	/* adjust eip so that it point into the call instruction */
	ip -= 2;

	for (iReg = 0; iReg < 16; iReg++) {
		ctx.uc_mcontext.gregs[iReg]  	    = int_regs[iReg];
		ctx.uc_mcontext.fpregs.fprs[iReg].d = fp_regs[iReg];
		ctx.uc_mcontext.aregs[iReg]  	    = acc_regs[iReg];
	}

	ctx.uc_mcontext.fpregs.fpc = fpc;

	MONO_CONTEXT_SET_BP (&ctx, sp);
	MONO_CONTEXT_SET_IP (&ctx, ip);
	
	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow && !mono_ex->caught_in_unmanaged) {
			mono_ex->stack_trace = NULL;
			mono_ex->trace_ips = NULL;
		} else if (preserve_ips) {
			mono_ex->caught_in_unmanaged = TRUE;
		}
	}
	mono_error_assert_ok (error);
//	mono_arch_handle_exception (&ctx, exc, FALSE);
	mono_handle_exception (&ctx, exc);
	mono_restore_context(&ctx);

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
mono_arch_get_throw_exception_generic (int size, MonoTrampInfo **info, int corlib, 
				       gboolean rethrow, gboolean aot, gboolean preserve_ips)
{
	guint8 *code, *start;
	int gr_offset, alloc_size, pos, i;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	code = start = (guint8 *) mono_global_codeman_reserve(size);

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, STK_BASE, S390_CFA_OFFSET);
	s390_stmg (code, s390_r6, s390_r15, STK_BASE, S390_REG_SAVE_OFFSET);
	gr_offset = S390_REG_SAVE_OFFSET - S390_CFA_OFFSET;
	for (i = s390_r6; i <= s390_r15; i++) {
		mono_add_unwind_op_offset (unwind_ops, code, start, i, gr_offset);
		gr_offset += sizeof(uintptr_t);
	}
	alloc_size = S390_ALIGN(S390_THROWSTACK_SIZE, S390_STACK_ALIGNMENT);
	s390_lgr  (code, s390_r14, STK_BASE);
	s390_aghi (code, STK_BASE, -alloc_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, alloc_size + S390_CFA_OFFSET);
	s390_stg  (code, s390_r14, 0, STK_BASE, 0);
	s390_lgr  (code, s390_r3, s390_r2);
	if (corlib) {
		S390_SET  (code, s390_r1, (guint8 *)mono_exception_from_token);
		S390_SET  (code, s390_r2, (guint8 *)m_class_get_image (mono_defaults.exception_class));
		s390_basr (code, s390_r14, s390_r1);
	}

	/*------------------------------------------------------*/
	/* save the general registers on the stack 		*/
	/*------------------------------------------------------*/
	s390_stmg (code, s390_r0, s390_r13, STK_BASE, S390_THROWSTACK_INTREGS);

	s390_lgr  (code, s390_r1, STK_BASE);
	s390_aghi (code, s390_r1, alloc_size);
	/*------------------------------------------------------*/
	/* save the return address in the parameter register    */
	/*------------------------------------------------------*/
	s390_lg   (code, s390_r3, 0, s390_r1, S390_RET_ADDR_OFFSET);

	/*------------------------------------------------------*/
	/* save the floating point registers 			*/
	/*------------------------------------------------------*/
	pos = S390_THROWSTACK_FLTREGS;
	for (i = 0; i < 16; ++i) {
		s390_std (code, i, 0, STK_BASE, pos);
		pos += sizeof (gdouble);
	}

	/*------------------------------------------------------*/
	/* save the access registers         			*/
	/*------------------------------------------------------*/
	s390_stam (code, s390_r0, s390_r15, STK_BASE, S390_THROWSTACK_ACCREGS);

	/*------------------------------------------------------*/
	/* call throw_exception (tkn, ip, sp, gr, fr, ar, re)   */
	/* - r2 already contains *exc				*/
	/*------------------------------------------------------*/
	s390_lgr  (code, s390_r4, s390_r1);        /* caller sp */

	/*------------------------------------------------------*/
	/* pointer to the saved int regs 			*/
	/*------------------------------------------------------*/
	s390_la	  (code, s390_r5, 0, STK_BASE, S390_THROWSTACK_INTREGS);
	s390_la   (code, s390_r6, 0, STK_BASE, S390_THROWSTACK_FLTREGS);
	s390_la   (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCREGS);
	s390_stg  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_ACCPRM);
	s390_stfpc(code, STK_BASE, S390_THROWSTACK_FPCPRM+4);
	S390_SET  (code, s390_r1, (guint8 *)throw_exception);
	s390_lghi (code, s390_r7, rethrow);
	s390_stg  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_RETHROW);
	s390_lghi (code, s390_r7, preserve_ips);
	s390_stg  (code, s390_r7, 0, STK_BASE, S390_THROWSTACK_PRESERVE_IPS);
	s390_basr (code, s390_r14, s390_r1);
	/* we should never reach this breakpoint */
	s390_break (code);
	g_assert ((code - start) < size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create (corlib ? "throw_corlib_exception" 
                                                      : (rethrow ? "rethrow_exception" 
                                                      : (preserve_ips ? "rethrow_preserve_exception" 
                                                      : "throw_exception")),
						start, code - start, ji, unwind_ops);

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
	g_assert (!aot);
	if (info)
		*info = NULL;

	return (mono_arch_get_throw_exception_generic (SZ_THROW, info, FALSE, FALSE, aot, FALSE));
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_rethrow_preserve_exception                    */
/*                                                                  */
/* Function	- Return a function pointer which can be used to       */
/*                raise exceptions. This preserves the stored ips.  */
/*                The returned function has the                     */
/*                following signature:                              */
/*                void (*func) (MonoException *exc);                */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer 
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	g_assert (!aot);
	if (info)
		*info = NULL;

	return (mono_arch_get_throw_exception_generic (SZ_THROW, info, FALSE, TRUE, aot, TRUE));
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
	g_assert (!aot);
	if (info)
		*info = NULL;

	return (mono_arch_get_throw_exception_generic (SZ_THROW, info, FALSE, TRUE, aot, FALSE));
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- arch_get_corlib_exception                         */
/*                                                                  */
/* Function	- Return a function pointer which can be used to    */
/*                raise corlib exceptions. The return function has  */
/*                the following signature:                          */
/*                void (*func) (guint32 token, guint32 offset)	    */
/*                                                                  */
/*------------------------------------------------------------------*/

gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	g_assert (!aot);
	if (info)
		*info = NULL;

	return (mono_arch_get_throw_exception_generic (SZ_THROW, info, TRUE, FALSE, aot, FALSE));
}	

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_unwind_frame                           */
/*                                                                  */
/* Function	- See exceptions-amd64.c for docs.                  */
/*                                                                  */
/*------------------------------------------------------------------*/

gboolean
mono_arch_unwind_frame (MonoJitTlsData *jit_tls, 
			 MonoJitInfo *ji, MonoContext *ctx, 
			 MonoContext *new_ctx, MonoLMF **lmf,
			 host_mgreg_t **save_locations,
			 StackFrameInfo *frame)
{
	gpointer ip = (gpointer) MONO_CONTEXT_GET_IP (ctx);
	guint8 *epilog = NULL;

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		uintptr_t address;
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;
		host_mgreg_t regs[32];

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		address = (char *)ip - (char *)ji->code_start;

		if (ji->has_arch_eh_info)
			epilog = (guint8*)ji->code_start + ji->code_size - mono_jinfo_get_epilog_size (ji);

		memcpy (&regs[0], &ctx->uc_mcontext.gregs, 16 * sizeof(host_mgreg_t));
		memcpy (&regs[16], &ctx->uc_mcontext.fpregs.fprs, 16 * sizeof(host_mgreg_t));
		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start,
						   (guint8 *) ji->code_start + ji->code_size,
						   ip, epilog ? &epilog : NULL, regs, 32, save_locations,
						   MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

		memcpy (&new_ctx->uc_mcontext.gregs, &regs[0], 16 * sizeof(host_mgreg_t));
		memcpy (&new_ctx->uc_mcontext.fpregs.fprs, &regs[16], 16 * sizeof(host_mgreg_t));
		MONO_CONTEXT_SET_IP(new_ctx, regs[14] - 2);
		MONO_CONTEXT_SET_BP(new_ctx, regs[15]);
		MONO_CONTEXT_SET_SP(new_ctx, regs[15]);
	
		return TRUE;
	} else if (*lmf) {

		ji = mini_jit_info_table_find ((gpointer)(*lmf)->eip);
		if (!ji) {
			if (!(*lmf)->method)
				return FALSE;
		
			frame->method = (*lmf)->method;
		}

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		memcpy(new_ctx->uc_mcontext.gregs, (*lmf)->gregs, sizeof((*lmf)->gregs));
		memcpy(new_ctx->uc_mcontext.fpregs.fprs, (*lmf)->fregs, sizeof((*lmf)->fregs));
		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip - 2);
		*lmf = (struct MonoLMF *) (*lmf)->previous_lmf;

		return TRUE;
	}

	return FALSE;
}

/*========================= End of Function ========================*/

static void
altstack_handle_and_restore (MonoContext *ctx, MONO_SIG_HANDLER_INFO_TYPE *siginfo, gpointer obj, guint32 flags)
{
	MonoContext mctx;
	MonoJitInfo *ji = mini_jit_info_table_find (MONO_CONTEXT_GET_IP (ctx));
	gboolean stack_ovf = (flags & 1) != 0;
	gboolean nullref = (flags & 2) != 0;

	if (!ji || (!stack_ovf && !nullref)) {
		mono_handle_native_crash (mono_get_signame (SIGSEGV), ctx, siginfo);
		/* if couldn't dump or if mono_handle_native_crash returns, abort */
		abort ();
	}

	mctx = *ctx;

	mono_handle_exception (&mctx, obj);
	if (stack_ovf) {
		MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
		jit_tls->stack_ovf_pending = 1;
	}
	mono_restore_context (&mctx);
}

void
mono_arch_handle_altstack_exception (void *sigctx, MONO_SIG_HANDLER_INFO_TYPE *siginfo, gpointer fault_addr, gboolean stack_ovf)
{
#ifdef MONO_ARCH_USE_SIGACTION
	MonoContext *uc = (MonoContext *) sigctx;
	MonoContext *uc_copy;
	MonoException *exc = NULL;
	MonoJitTlsData *jit_tls = NULL;
	gboolean nullref = TRUE;
	uintptr_t sp;

	jit_tls = mono_tls_get_jit_tls();
	g_assert (jit_tls);

	/* use TLS as temporary storage as we want to avoid
	 * (1) stack allocation on the application stack
	 * (2) calling malloc, because it is not async-safe
	 * (3) using a global storage, because this function is not reentrant
	 *
	 * tls->orig_ex_ctx is used by the stack walker, which shouldn't be running at this point.
	 */
	uc_copy = &jit_tls->orig_ex_ctx;

	if (!mono_is_addr_implicit_null_check (fault_addr))
		nullref = FALSE;

	if (stack_ovf)
		exc = mini_get_stack_overflow_ex ();

	/*
	 * Setup the call frame on the application stack so that control is
	 * returned there and exception handling can continue. we want the call
	 * frame to be minimal as possible, for example no argument passing that
	 * requires allocation on the stack, as this wouldn't be encoded in unwind
	 * information for the caller frame.
	 */
	sp = (uintptr_t) (UCONTEXT_REG_Rn(uc, 15));
	sp = sp - S390_MINIMAL_STACK_SIZE;

	mono_sigctx_to_monoctx (uc, uc_copy);
	g_assert ((uintptr_t) mono_arch_ip_from_context (uc) == (uintptr_t) UCONTEXT_IP (uc_copy));

	/* At the return form the signal handler execution starts in altstack_handle_and_restore() */
	UCONTEXT_REG_Rn(uc, 14) = (uintptr_t) UCONTEXT_IP(uc);
	UCONTEXT_IP(uc)         = (uintptr_t) altstack_handle_and_restore;
	UCONTEXT_REG_Rn(uc, 1)  = (uintptr_t) sp;
	UCONTEXT_REG_Rn(uc, S390_FIRST_ARG_REG) = (uintptr_t) uc_copy;
	UCONTEXT_REG_Rn(uc, S390_FIRST_ARG_REG + 1) = (uintptr_t) siginfo;
	UCONTEXT_REG_Rn(uc, S390_FIRST_ARG_REG + 2) = (uintptr_t) exc;
	UCONTEXT_REG_Rn(uc, S390_FIRST_ARG_REG + 3) = (stack_ovf ? 1 : 0) | (nullref ? 2 : 0);
#endif
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- handle_signal_exception                           */
/*                                                                  */
/* Function	- Handle an exception raised by the JIT code.	    */
/*                                                                  */
/* Parameters   - obj       - The exception object                  */
/*                                                                  */
/*------------------------------------------------------------------*/

static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx;

	memcpy (&ctx, &jit_tls->ex_ctx, sizeof (MonoContext));
	mono_handle_exception (&ctx, obj);
	mono_restore_context (&ctx);
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
mono_arch_handle_exception (void *sigctx, gpointer obj)
{
	MonoContext mctx;

	/*
	 * Handling the exception in the signal handler is problematic, since the original
	 * signal is disabled, and we could run arbitrary code though the debugger. So
	 * resume into the normal stack and do most work there if possible.
	 */
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);

	mctx = jit_tls->ex_ctx;
	mono_arch_setup_async_callback (&mctx, handle_signal_exception, obj);
	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_setup_async_callback                    */
/*                                                                  */
/* Function	- Establish the async callback.              	    */
/*                                                                  */
/* Parameters   - ctx       - Context                               */
/*                async_cb  - Callback routine address              */
/*                user_data - Data to be passed to callback         */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	uintptr_t sp = (uintptr_t) MONO_CONTEXT_GET_SP(ctx);

	ctx->uc_mcontext.gregs[2] = (gsize)user_data;

	sp -= S390_MINIMAL_STACK_SIZE;
	*(unsigned long *)sp = (uintptr_t) MONO_CONTEXT_GET_SP(ctx);
	MONO_CONTEXT_SET_BP(ctx, sp);
	MONO_CONTEXT_SET_IP(ctx, (gsize)async_cb);
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
	return ((gpointer) MONO_CONTEXT_GET_IP(((MonoContext *) sigctx)));
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

/**
 * 
 * @brief Setup CTX so execution resumes at FUNC
 *
 * @param[in] Context to be resumed
 * @param[in] Location to be resumed at
 *
 * Set the IP of the passed context to the address so that on resumption
 * we jump to this location
 */

void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	MONO_CONTEXT_SET_IP (ctx, func);
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
	guint64     *operand;
	gboolean    arithExc = TRUE;
	gint	    regNo,
	    	    idxNo,
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
			idxNo   = (code[1] & 0x0f);
			offset  = *((guint16 *) code+2) & 0x0fff;
			operand = (guint64*)(ctx->uc_mcontext.gregs[regNo] + offset);
			if (idxNo != 0)
				operand += ctx->uc_mcontext.gregs[idxNo];
			if (*operand == 0)
				arithExc = FALSE; 
		break;
		case 0xb9 :		/* DL[GR] or DS[GR]         */
			if ((code[1] == 0x97) || (code[1] == 0x87) ||
			    (code[1] == 0x0d) || (code[1] == 0x1d)) {
				regNo = (code[3] & 0x0f);
				if (ctx->uc_mcontext.gregs[regNo] == 0)
					arithExc = FALSE;
			}
		break;
		case 0xe3 :		/* DL[G] | DS[G]  	    */
			if ((code[5] == 0x97) || (code[5] == 0x87) ||
			    (code[5] == 0x0d) || (code[5] == 0x1d)) {
				regNo   = (code[2] & 0xf0 >> 8);	
				idxNo   = (code[1] & 0x0f);
				offset  = (code[2] & 0x0f << 8) + 
					  code[3] + (code[4] << 12);
				operand = (guint64*)(ctx->uc_mcontext.gregs[regNo] + offset);
				if (idxNo != 0)
					operand += ctx->uc_mcontext.gregs[idxNo];
				if (*operand == 0)
					arithExc = FALSE; 
			}
		break;
		default:
			arithExc = TRUE;
	}
	ctx->uc_mcontext.psw.addr = (guint64)code;
	return (arithExc);
}

/*========================= End of Function ========================*/
