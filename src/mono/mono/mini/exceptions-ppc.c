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

static gboolean arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only);

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

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (int)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (int)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_sp))

#ifdef __APPLE__

typedef struct {
	unsigned long sp;
	unsigned long unused1;
	unsigned long lr;
} MonoPPCStackFrame;

#else

typedef struct {
	unsigned long sp;
	unsigned long lr;
} MonoPPCStackFrame;

#endif


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
	restore_regs_from_context (ppc_r3, ppc_r4, ppc_r5);
	/* restore also the stack pointer */
	ppc_lwz (code, ppc_sp, G_STRUCT_OFFSET (MonoContext, sc_sp), ppc_r3);
	//ppc_break (code);
	/* jump to the saved IP */
	ppc_mtctr (code, ppc_r4);
	ppc_bcctr (code, PPC_BR_ALWAYS, 0);
	/* never reached */
	ppc_break (code);

	g_assert ((code - start) < sizeof(start));
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

	g_assert ((code - start) < sizeof(start));
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = arch_get_restore_context ();

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
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name)
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
		ppc_load (code, ppc_r3, mono_defaults.corlib);
		ppc_load (code, ppc_r4, "System");
		ppc_bl (code, 0);
		ppc_patch (code - 4, mono_exception_from_name);
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

	ppc_bl (code, 0);
	ppc_patch (code - 4, throw_exception);
	/* we should never reach this breakpoint */
	ppc_break (code);
	g_assert ((code - start) < size);
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
	static guint8 start [128];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE);
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
	static guint8 start [160];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE);
	inited = 1;
	return start;
}	

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
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	unsigned long *ptr;
	char *p;
	MonoPPCStackFrame *sframe;

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
		sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
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

		*res = *ji;
		return res;
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
			*res = *ji;
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		/*sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr);*/
		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		memcpy (&new_ctx->regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->fregs, (*lmf)->fregs, sizeof (double) * MONO_SAVED_FREGS);
		*lmf = (*lmf)->previous_lmf;

		return res;
		
	}

	return NULL;
}

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	int i, len;
	
	len = mono_array_length (ta);

	res = mono_array_new (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gpointer ip = mono_array_get (ta, gpointer, i);

		ji = mono_jit_info_table_find (domain, ip);
		if (!ji) {
			sf->method = mono_method_get_object (domain, mono_defaults.object_class->methods [0], NULL);
			mono_array_set (res, gpointer, i, sf);
			continue;
		}
		//g_assert (ji != NULL);

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

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoPPCStackFrame *sframe;

	MonoContext ctx, new_ctx;

	setup_context (&ctx);
	setup_context (&new_ctx);

#ifdef __APPLE__
	__asm__ volatile("lwz   %0,0(r1)" : "=r" (sframe));
#else
	__asm__ volatile("lwz   %0,0(1)" : "=r" (sframe));
#endif
	//MONO_CONTEXT_SET_IP (&ctx, sframe->lr);
	MONO_CONTEXT_SET_BP (&ctx, sframe->sp);
	sframe = (MonoPPCStackFrame*)sframe->sp;
	MONO_CONTEXT_SET_IP (&ctx, sframe->lr);

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = do_il_offset ? mono_debug_il_offset_from_address (ji->method, native_offset, domain): -1;

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
		setup_context (&ctx);
	}
}

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
	MonoPPCStackFrame *sframe;

#ifdef __APPLE__
	__asm__ volatile("lwz   %0,0(r1)" : "=r" (sframe));
#else
	__asm__ volatile("lwz   %0,0(1)" : "=r" (sframe));
#endif
	MONO_CONTEXT_SET_BP (&ctx, sframe->sp);
	sframe = (MonoPPCStackFrame*)sframe->sp;
	MONO_CONTEXT_SET_IP (&ctx, sframe->lr);
	/*MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));*/

	skip++;

	do {
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, native_offset, NULL);

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

/*
 * This is the function called from the signal handler
 */
#ifdef __APPLE__
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	struct ucontext *uc = ctx;
	MonoContext mctx;
	gboolean result;
	
	mctx.sc_ir = uc->uc_mcontext->ss.srr0;
	mctx.sc_sp = uc->uc_mcontext->ss.r1;
	memcpy (&mctx.regs, &uc->uc_mcontext->ss.r13, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&mctx.fregs, &uc->uc_mcontext->fs.fpregs [14], sizeof (double) * MONO_SAVED_FREGS);

	result = arch_handle_exception (&mctx, obj, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	uc->uc_mcontext->ss.srr0 = mctx.sc_ir;
	uc->uc_mcontext->ss.r1 = mctx.sc_sp;
	memcpy (&uc->uc_mcontext->ss.r13, &mctx.regs, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&uc->uc_mcontext->fs.fpregs [14], &mctx.fregs, sizeof (double) * MONO_SAVED_FREGS);
	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext->ss.srr0;
}

#else
/* Linux */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	struct ucontext *uc = ctx;
	MonoContext mctx;
	gboolean result;
	
	mctx.sc_ir = uc->uc_mcontext.uc_regs->gregs [PT_NIP];
	mctx.sc_sp = uc->uc_mcontext.uc_regs->gregs [PT_R1];
	memcpy (&mctx.regs, &uc->uc_mcontext.uc_regs->gregs [PT_R13], sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&mctx.fregs, &uc->uc_mcontext.uc_regs->fpregs.fpregs [14], sizeof (double) * MONO_SAVED_FREGS);

	result = arch_handle_exception (&mctx, obj, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	uc->uc_mcontext.uc_regs->gregs [PT_NIP] = mctx.sc_ir;
	uc->uc_mcontext.uc_regs->gregs [PT_R1] = mctx.sc_sp;
	memcpy (&uc->uc_mcontext.uc_regs->gregs [PT_R13], &mctx.regs, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&uc->uc_mcontext.uc_regs->fpregs.fpregs [14], &mctx.fregs, sizeof (double) * MONO_SAVED_FREGS);
	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext.uc_regs->gregs [PT_NIP];
}

#endif

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
	MonoString *initial_stack_trace = NULL;
	GString *trace_str = NULL;
	int frame_count = 0;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_stack_trace = mono_ex->stack_trace;
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
		char *trace = NULL;
		gboolean need_trace = FALSE;
		
		if (test_only && (frame_count < 1000)) {
			need_trace = TRUE;
			if (!trace_str)
				trace_str = g_string_new ("");
		}
		setup_context (&new_ctx);
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      need_trace ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			frame_count ++;
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				if (!initial_stack_trace && (frame_count < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));

					g_string_append (trace_str, trace);
					g_string_append_c (trace_str, '\n');
				}
			}

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
							g_assert (ji->exvar_offset);
							/* need to use the frame pointer (ppc_r31), not r1 (regs start from register r13): methods with clauses always have r31 */
							*((gpointer *)((char *)(ctx->regs [ppc_r31-13]) + ji->exvar_offset)) = obj;
							if (!initial_stack_trace && trace_str) {
								mono_ex->stack_trace = mono_string_new (domain, trace_str->str);
							}
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
							filtered = call_filter (ctx, ei->data.filter, mono_ex);

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (ji->method->klass->image, ei->data.token))) || filtered) {
							if (test_only) {
								if (mono_ex) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips);
								}
								g_list_free (trace_ips);
								g_free (trace);
								if (trace_str)
									g_string_free (trace_str, TRUE);
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							/*printf ("stack for catch: %p\n", MONO_CONTEXT_GET_BP (ctx));*/
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							g_free (trace);
							if (trace_str)
								g_string_free (trace_str, TRUE);
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

		g_free (trace);
			
		*ctx = new_ctx;
		setup_context (ctx);

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
				if (trace_str)
					g_string_free (trace_str, TRUE);
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

