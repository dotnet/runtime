/*
 * exceptions-ppc.c: exception support for PowerPC
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
#include <stddef.h>
#include <ucontext.h>

#include <mono/arch/ppc/ppc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-ppc.h"

/*

struct sigcontext {
    int      sc_onstack;     // sigstack state to restore 
    int      sc_mask;        // signal mask to restore 
    int      sc_ir;          // pc 
    int      sc_psw;         // processor status word 
    int      sc_sp;          // stack pointer if sc_regs == NULL 
    void    *sc_regs;        // (kernel private) saved state 
};

struct ucontext {
        int             uc_onstack;
        sigset_t        uc_sigmask;     // signal mask used by this context 
        stack_t         uc_stack;       // stack used by this context 
        struct ucontext *uc_link;       // pointer to resuming context 
        size_t          uc_mcsize;      // size of the machine context passed in 
        mcontext_t      uc_mcontext;    // machine specific context 
};

typedef struct ppc_exception_state {
        unsigned long dar;      // Fault registers for coredump 
        unsigned long dsisr;
        unsigned long exception;// number of powerpc exception taken 
        unsigned long pad0;     // align to 16 bytes 

        unsigned long pad1[4];  // space in PCB "just in case" 
} ppc_exception_state_t;

typedef struct ppc_vector_state {
        unsigned long   save_vr[32][4];
        unsigned long   save_vscr[4];
        unsigned int    save_pad5[4];
        unsigned int    save_vrvalid;                   // VRs that have been saved 
        unsigned int    save_pad6[7];
} ppc_vector_state_t;

typedef struct ppc_float_state {
        double  fpregs[32];

        unsigned int fpscr_pad; // fpscr is 64 bits, 32 bits of rubbish 
        unsigned int fpscr;     // floating point status register 
} ppc_float_state_t;

typedef struct ppc_thread_state {
        unsigned int srr0;      // Instruction address register (PC) 
        unsigned int srr1;      // Machine state register (supervisor) 
        unsigned int r0;
        unsigned int r1;
        unsigned int r2;
	... 
        unsigned int r31;
        unsigned int cr;        // Condition register 
        unsigned int xer;       // User's integer exception register 
        unsigned int lr;        // Link register 
        unsigned int ctr;       // Count register 
        unsigned int mq;        // MQ register (601 only) 

        unsigned int vrsave;    // Vector Save Register 
} ppc_thread_state_t;

struct mcontext {
        ppc_exception_state_t   es;
        ppc_thread_state_t      ss;
        ppc_float_state_t       fs;
        ppc_vector_state_t      vs;
};

typedef struct mcontext  * mcontext_t;

Linux/PPC instead has:
struct sigcontext {
        unsigned long   _unused[4];
        int             signal;
        unsigned long   handler;
        unsigned long   oldmask;
        struct pt_regs  *regs;
};
struct pt_regs {
        unsigned long gpr[32];
        unsigned long nip;
        unsigned long msr;
        unsigned long orig_gpr3;        // Used for restarting system calls 
        unsigned long ctr;
        unsigned long link;
        unsigned long xer;
        unsigned long ccr;
        unsigned long mq;               // 601 only (not used at present) 
                                        // Used on APUS to hold IPL value. 
        unsigned long trap;             // Reason for being here 
        // N.B. for critical exceptions on 4xx, the dar and dsisr
        // fields are overloaded to hold srr0 and srr1. 
        unsigned long dar;              // Fault registers 
        unsigned long dsisr;            // on 4xx/Book-E used for ESR 
        unsigned long result;           // Result of a system call 
};
struct mcontext {
        elf_gregset_t   mc_gregs;
        elf_fpregset_t  mc_fregs;
        unsigned long   mc_pad[2];
        elf_vrregset_t  mc_vregs __attribute__((__aligned__(16)));
};

struct ucontext {
        unsigned long    uc_flags;
        struct ucontext *uc_link;
        stack_t          uc_stack;
        int              uc_pad[7];
        struct mcontext *uc_regs;       // points to uc_mcontext field 
        sigset_t         uc_sigmask;
        // glibc has 1024-bit signal masks, ours are 64-bit 
        int              uc_maskext[30];
        int              uc_pad2[3];
        struct mcontext  uc_mcontext;
};

#define ELF_NGREG       48      // includes nip, msr, lr, etc. 
#define ELF_NFPREG      33      // includes fpscr 

// General registers 
typedef unsigned long elf_greg_t;
typedef elf_greg_t elf_gregset_t[ELF_NGREG];

// Floating point registers 
typedef double elf_fpreg_t;
typedef elf_fpreg_t elf_fpregset_t[ELF_NFPREG];


*/


#define restore_regs_from_context(ctx_reg,ip_reg,tmp_reg) do {	\
		int reg;	\
		ppc_lwz (code, ip_reg, G_STRUCT_OFFSET (MonoContext, sc_ir), ctx_reg);	\
		ppc_lmw (code, ppc_r13, ctx_reg, G_STRUCT_OFFSET (MonoContext, regs));	\
		for (reg = 0; reg < MONO_SAVED_FREGS; ++reg) {	\
			ppc_lfd (code, (14 + reg), G_STRUCT_OFFSET(MonoLMF, fregs) + reg * sizeof (gdouble), ctx_reg);	\
		}	\
	} while (0)

/* nothing to do */
#define setup_context(ctx)

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 * The first argument in r3 is the pointer to the context.
 */
gpointer
mono_arch_get_restore_context (void)
{
	guint8 *code;
	static guint8 *start = NULL;

	if (start)
		return start;

	code = start = mono_global_codeman_reserve (128);
	restore_regs_from_context (ppc_r3, ppc_r4, ppc_r5);
	/* restore also the stack pointer */
	ppc_lwz (code, ppc_sp, G_STRUCT_OFFSET (MonoContext, sc_sp), ppc_r3);
	//ppc_break (code);
	/* jump to the saved IP */
	ppc_mtctr (code, ppc_r4);
	ppc_bcctr (code, PPC_BR_ALWAYS, 0);
	/* never reached */
	ppc_break (code);

	g_assert ((code - start) < 128);
	mono_arch_flush_icache (start, code - start);
	return start;
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint8 *start = NULL;
	guint8 *code;
	int alloc_size, pos, i;

	if (start)
		return start;

	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start = mono_global_codeman_reserve (320);

	/* save all the regs on the stack */
	pos = 0;
	for (i = 31; i >= 14; --i) {
		pos += sizeof (gdouble);
		ppc_stfd (code, i, -pos, ppc_sp);
	}
	pos += sizeof (gulong) * MONO_SAVED_GREGS;
	ppc_stmw (code, ppc_r13, ppc_sp, -pos);

	ppc_mflr (code, ppc_r0);
	ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);

	alloc_size = PPC_MINIMAL_STACK_SIZE + pos + 64;
	// align to PPC_STACK_ALIGNMENT bytes
	alloc_size += PPC_STACK_ALIGNMENT - 1;
	alloc_size &= ~(PPC_STACK_ALIGNMENT - 1);

	/* allocate stack frame and set link from sp in ctx */
	g_assert ((alloc_size & (PPC_STACK_ALIGNMENT-1)) == 0);
	ppc_lwz (code, ppc_r0, G_STRUCT_OFFSET (MonoContext, sc_sp), ppc_r3);
	ppc_lwzx (code, ppc_r0, ppc_r0, ppc_r0);
	ppc_stwu (code, ppc_r0, -alloc_size, ppc_sp);

	/* restore all the regs from ctx (in r3), but not r1, the stack pointer */
	restore_regs_from_context (ppc_r3, ppc_r6, ppc_r7);
	/* call handler at eip (r4) and set the first arg with the exception (r5) */
	ppc_mtctr (code, ppc_r4);
	ppc_mr (code, ppc_r3, ppc_r5);
	ppc_bcctrl (code, PPC_BR_ALWAYS, 0);

	/* epilog */
	ppc_lwz (code, ppc_r0, alloc_size + PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_mtlr (code, ppc_r0);
	ppc_addic (code, ppc_sp, ppc_sp, alloc_size);
	
	/* restore all the regs from the stack */
	pos = 0;
	for (i = 31; i >= 14; --i) {
		pos += sizeof (double);
		ppc_lfd (code, i, -pos, ppc_sp);
	}
	pos += sizeof (gulong) * MONO_SAVED_GREGS;
	ppc_lmw (code, ppc_r13, ppc_sp, -pos);

	ppc_blr (code);

	g_assert ((code - start) < 320);
	mono_arch_flush_icache (start, code - start);
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs, gboolean rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 4;

	setup_context (&ctx);

	/*printf ("stack in throw: %p\n", esp);*/
	MONO_CONTEXT_SET_BP (&ctx, esp);
	MONO_CONTEXT_SET_IP (&ctx, eip);
	memcpy (&ctx.regs, int_regs, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&ctx.fregs, fp_regs, sizeof (double) * MONO_SAVED_FREGS);

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, (gpointer)eip, FALSE);
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
	pos = 0;
	for (i = 31; i >= 14; --i) {
		pos += sizeof (gdouble);
		ppc_stfd (code, i, -pos, ppc_sp);
	}
	pos += sizeof (gulong) * MONO_SAVED_GREGS;
	ppc_stmw (code, ppc_r13, ppc_sp, -pos);

	ppc_mflr (code, ppc_r0);
	ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);

	alloc_size = PPC_MINIMAL_STACK_SIZE + pos + 64;
	// align to PPC_STACK_ALIGNMENT bytes
	alloc_size += PPC_STACK_ALIGNMENT - 1;
	alloc_size &= ~(PPC_STACK_ALIGNMENT - 1);

	g_assert ((alloc_size & (PPC_STACK_ALIGNMENT-1)) == 0);
	ppc_stwu (code, ppc_sp, -alloc_size, ppc_sp);

	//ppc_break (code);
	if (by_name) {
		ppc_mr (code, ppc_r5, ppc_r3);
		ppc_load (code, ppc_r3, (guint32)mono_defaults.corlib);
		ppc_load (code, ppc_r4, "System");
		ppc_load (code, ppc_r0, mono_exception_from_name);
		ppc_mtctr (code, ppc_r0);
		ppc_bcctrl (code, PPC_BR_ALWAYS, 0);
	}

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* caller sp */
	ppc_lwz (code, ppc_r5, 0, ppc_sp); 
	/* exc is already in place in r3 */
	if (by_name)
		ppc_lwz (code, ppc_r4, PPC_RET_ADDR_OFFSET, ppc_r5); 
	else
		ppc_mr (code, ppc_r4, ppc_r0); /* caller ip */
	/* pointer to the saved fp regs */
	pos = alloc_size - sizeof (double) * MONO_SAVED_FREGS;
	ppc_addi (code, ppc_r7, ppc_sp, pos);
	/* pointer to the saved int regs */
	pos -= sizeof (gulong) * MONO_SAVED_GREGS;
	ppc_addi (code, ppc_r6, ppc_sp, pos);
	ppc_li (code, ppc_r8, rethrow);

	ppc_load (code, ppc_r0, throw_exception);
	ppc_mtctr (code, ppc_r0);
	ppc_bcctrl (code, PPC_BR_ALWAYS, 0);
	/* we should never reach this breakpoint */
	ppc_break (code);
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
	static guint8 *start = NULL;
	static int inited = 0;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (132);
	mono_arch_get_throw_exception_generic (start, 132, FALSE, TRUE);
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
	static guint8 *start = NULL;
	static int inited = 0;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (132);
	mono_arch_get_throw_exception_generic (start, 132, FALSE, FALSE);
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
	static guint8 *start = NULL;
	static int inited = 0;

	if (inited)
		return start;
	start = mono_global_codeman_reserve (168);
	mono_arch_get_throw_exception_generic (start, 168, TRUE, FALSE);
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
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx,
						 MonoContext *new_ctx, MonoLMF **lmf, gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	MonoPPCStackFrame *sframe;

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mono_jit_info_table_find (domain, ip);

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		gint32 address;
		int offset, i;

		*new_ctx = *ctx;
		setup_context (new_ctx);

		if (*lmf && (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_SP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		if (ji->method->save_lmf) {
			memcpy (&new_ctx->fregs, (char*)sframe->sp - sizeof (double) * MONO_SAVED_FREGS, sizeof (double) * MONO_SAVED_FREGS);
			memcpy (&new_ctx->regs, (char*)sframe->sp - sizeof (double) * MONO_SAVED_FREGS - sizeof (gulong) * MONO_SAVED_GREGS, sizeof (gulong) * MONO_SAVED_GREGS);
		} else if (ji->used_regs) {
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

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;
		setup_context (new_ctx);

		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
		} else {
			if (!(*lmf)->method)
				return (gpointer)-1;

			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		/*sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_SP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr);*/
		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		memcpy (&new_ctx->regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->fregs, (*lmf)->fregs, sizeof (double) * MONO_SAVED_FREGS);

		/* FIXME: what about trampoline LMF frames?  see exceptions-x86.c */

		*lmf = (*lmf)->previous_lmf;

		return ji ? ji : res;
	}

	return NULL;
}

/*
 * This is the function called from the signal handler
 */
void
mono_arch_sigctx_to_monoctx (void *ctx, MonoContext *mctx)
{
	os_ucontext *uc = ctx;

	mctx->sc_ir = UCONTEXT_REG_NIP(uc);
	mctx->sc_sp = UCONTEXT_REG_Rn(uc, 1);
	memcpy (&mctx->regs, &UCONTEXT_REG_Rn(uc, 13), sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&mctx->fregs, &UCONTEXT_REG_FPRn(uc, 14), sizeof (double) * MONO_SAVED_FREGS);
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *ctx)
{
	os_ucontext *uc = ctx;

	UCONTEXT_REG_NIP(uc) = mctx->sc_ir;
	UCONTEXT_REG_Rn(uc, 1) = mctx->sc_sp;
	memcpy (&UCONTEXT_REG_Rn(uc, 13), &mctx->regs, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&UCONTEXT_REG_FPRn(uc, 14), &mctx->fregs, sizeof (double) * MONO_SAVED_FREGS);
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	os_ucontext *uc = sigctx;
	return (gpointer)UCONTEXT_REG_NIP(uc);
}

static void
altstack_handle_and_restore (void *sigctx, gpointer obj, gboolean test_only)
{
	void (*restore_context) (MonoContext *);
	MonoContext mctx;

	restore_context = mono_arch_get_restore_context ();
	mono_arch_sigctx_to_monoctx (sigctx, &mctx);
	mono_handle_exception (&mctx, obj, (gpointer)mctx.sc_ir, test_only);
	restore_context (&mctx);
}

void
mono_arch_handle_altstack_exception (void *sigctx, gpointer fault_addr, gboolean stack_ovf)
{
#ifdef MONO_ARCH_USE_SIGACTION
	os_ucontext *uc = (ucontext_t*)sigctx;
	os_ucontext *uc_copy;
	MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context (sigctx));
	gpointer *sp;
	int frame_size;

	if (stack_ovf) {
		const char *method;
		/* we don't do much now, but we can warn the user with a useful message */
		fprintf (stderr, "Stack overflow: IP: %p, SP: %p\n", mono_arch_ip_from_context (sigctx), (gpointer)UCONTEXT_REG_Rn(uc, 1));
		if (ji && ji->method)
			method = mono_method_full_name (ji->method, TRUE);
		else
			method = "Unmanaged";
		fprintf (stderr, "At %s\n", method);
		abort ();
	}
	if (!ji)
		mono_handle_native_sigsegv (SIGSEGV, sigctx);
	/* setup a call frame on the real stack so that control is returned there
	 * and exception handling can continue.
	 * The frame looks like:
	 *   ucontext struct
	 *   ...
	 * 224 is the size of the red zone
	 */
	frame_size = sizeof (ucontext_t) + sizeof (gpointer) * 16 + 224;
	frame_size += 15;
	frame_size &= ~15;
	sp = (gpointer)(UCONTEXT_REG_Rn(uc, 1) & ~15);
	sp = (gpointer)((char*)sp - frame_size);
	/* may need to adjust pointers in the new struct copy, depending on the OS */
	uc_copy = (ucontext_t*)(sp + 16);
	memcpy (uc_copy, uc, sizeof (os_ucontext));
#ifdef __linux__
	uc_copy->uc_mcontext.uc_regs = (gpointer)((char*)uc_copy + ((char*)uc->uc_mcontext.uc_regs - (char*)uc));
#endif
	g_assert (mono_arch_ip_from_context (uc) == mono_arch_ip_from_context (uc_copy));
	/* at the return form the signal handler execution starts in altstack_handle_and_restore() */
	UCONTEXT_REG_LNK(uc) = UCONTEXT_REG_NIP(uc);
	UCONTEXT_REG_NIP(uc) = (unsigned long)altstack_handle_and_restore;
	UCONTEXT_REG_Rn(uc, 1) = (unsigned long)sp;
	UCONTEXT_REG_Rn(uc, PPC_FIRST_ARG_REG) = (unsigned long)(sp + 16);
	UCONTEXT_REG_Rn(uc, PPC_FIRST_ARG_REG + 1) = 0;
	UCONTEXT_REG_Rn(uc, PPC_FIRST_ARG_REG + 2) = 0;
#endif
}

gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;
	gboolean result;

	mono_arch_sigctx_to_monoctx (ctx, &mctx);

	result = mono_handle_exception (&mctx, obj, (gpointer)mctx.sc_ir, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	mono_arch_monoctx_to_sigctx (&mctx, ctx);
	return result;
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

