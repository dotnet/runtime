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

#include <mono/arch/ppc/ppc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-ppc.h"

typedef struct sigcontext MonoContext;

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

*/

#ifdef __APPLE__

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (int)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (int)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_sp))

typedef struct {
	unsigned long sp;
	unsigned long unused1;
	unsigned long lr;
} MonoPPCStackFrame;

#else /* sigcontext is different on linux/ppc. See /usr/include/asm/ptrace.h. */

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->regs->nip = (unsigned long)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs->gpr[1] = (unsigned long)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->regs->nip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs->gpr[1]))

typedef struct {
	unsigned long sp;
	unsigned long lr;
} MonoPPCStackFrame;

#endif

/* disbale this for now */
#undef MONO_USE_EXC_TABLES

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

#if 0

static long
get_sigcontext_reg (struct sigcontext *ctx, int dwarf_regnum)
{
	switch (dwarf_regnum) {
	case X86_EAX:
		return ctx->SC_EAX;
	case X86_EBX:
		return ctx->SC_EBX;
	case X86_ECX:
		return ctx->SC_ECX;
	case X86_EDX:
		return ctx->SC_EDX;
	case X86_ESI:
		return ctx->SC_ESI;
	case X86_EDI:
		return ctx->SC_EDI;
	case X86_EBP:
		return ctx->SC_EBP;
	case X86_ESP:
		return ctx->SC_ESP;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

static void
set_sigcontext_reg (struct sigcontext *ctx, int dwarf_regnum, long value)
{
	switch (dwarf_regnum) {
	case X86_EAX:
		ctx->SC_EAX = value;
		break;
	case X86_EBX:
		ctx->SC_EBX = value;
		break;
	case X86_ECX:
		ctx->SC_ECX = value;
		break;
	case X86_EDX:
		ctx->SC_EDX = value;
		break;
	case X86_ESI:
		ctx->SC_ESI = value;
		break;
	case X86_EDI:
		ctx->SC_EDI = value;
		break;
	case X86_EBP:
		ctx->SC_EBP = value;
		break;
	case X86_ESP:
		ctx->SC_ESP = value;
		break;
	case 8:
		ctx->SC_EIP = value;
		break;
	default:
		g_assert_not_reached ();
	}
}

typedef struct frame_state * (*framesf) (void *, struct frame_state *);

static framesf frame_state_for = NULL;

static gboolean inited = FALSE;

typedef char ** (*get_backtrace_symbols_type) (void *__const *__array, int __size);

static get_backtrace_symbols_type get_backtrace_symbols = NULL;

static void
init_frame_state_for (void)
{
	GModule *module;

	/*
	 * There are two versions of __frame_state_for: one in libgcc.a and the
	 * other in glibc.so. We need the version from glibc.
	 * For more info, see this:
	 * http://gcc.gnu.org/ml/gcc/2002-08/msg00192.html
	 */
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

#endif

gboolean  mono_arch_handle_exception (struct sigcontext *ctx, gpointer obj, gboolean test_only);

/* mono_arch_has_unwind_info:
 *
 * Tests if a function has an DWARF exception table able to restore
 * all caller saved registers. 
 */
gboolean
mono_arch_has_unwind_info (MonoMethod *method)
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

struct stack_frame
{
  void *next;
  void *return_address;
};

static MonoJitInfo *
ppc_unwind_native_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, struct sigcontext *ctx, 
			 struct sigcontext *new_ctx, MonoLMF *lmf, char **trace)
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
			//printf ("FRAME %p %p %p\n", frame, MONO_CONTEXT_GET_IP (new_ctx), mono_jit_info_table_find (domain, MONO_CONTEXT_GET_IP (new_ctx)));

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

	//if (!lmf)
	//g_assert_not_reached ();

	if (trace) {
		g_free (*trace);
		*trace = NULL;
	}
#endif
	return NULL;
}

#endif

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
static gpointer
arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

#if 0
	/* restore_contect (struct sigcontext *ctx) */
	/* we do not restore X86_EAX, X86_EDX */

	start = code = g_malloc (1024);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* get return address, stored in EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EIP), 4);
	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBX), 4);
	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EDI), 4);
	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESI), 4);
	/* restore ESP */
	x86_mov_reg_membase (code, X86_ESP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESP), 4);
	/* restore EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBP), 4);

	/* jump to the saved IP */
	x86_jump_reg (code, X86_EDX);

#endif
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
	static guint8 start [196];
	static int inited = 0;
	guint8 *code;
	int alloc_size, pos, i;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (struct sigcontext *ctx, unsigned long eip, gpointer exc) */
	code = start;

	ppc_mflr (code, ppc_r0);
	ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);
	alloc_size = PPC_MINIMAL_STACK_SIZE + (sizeof (gulong) * 14 + sizeof (gdouble) * 14);
	// align to PPC_STACK_ALIGNMENT bytes
	if (alloc_size & (PPC_STACK_ALIGNMENT - 1))
		alloc_size += PPC_STACK_ALIGNMENT - (alloc_size & (PPC_STACK_ALIGNMENT - 1));
	ppc_stwu (code, ppc_sp, -alloc_size, ppc_sp);
	/* save all the regs on the stack */
	pos = PPC_MINIMAL_STACK_SIZE;
	ppc_stmw (code, ppc_r13, ppc_sp, pos);
	pos += sizeof (gulong) * 19;
	for (i = 14; i < 32; ++i) {
		ppc_stfd (code, i, pos, ppc_sp);
		pos += sizeof (gdouble);
	}
	/* FIXME: restore all the regs from ctx (in r3) */
	/* call handler at eip (r4) and set the first arg with the exception (r5) */
	ppc_mtctr (code, ppc_r4);
	ppc_mr (code, ppc_r3, ppc_r5);
	ppc_bcctrl (code, PPC_BR_ALWAYS, 0);
	/* restore all the regs from the stack */
	pos = PPC_MINIMAL_STACK_SIZE;
	ppc_lmw (code, ppc_r13, ppc_sp, pos);
	pos += sizeof (gulong) * 19;
	for (i = 14; i < 32; ++i) {
		ppc_lfd (code, i, pos, ppc_sp);
		pos += sizeof (gdouble);
	}
	ppc_lwz (code, ppc_r0, alloc_size + PPC_RET_ADDR_OFFSET, ppc_sp);
	ppc_mtlr (code, ppc_r0);
	ppc_addic (code, ppc_sp, ppc_sp, alloc_size);

	g_assert ((code - start) < sizeof(start));
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	static void (*restore_context) (struct sigcontext *);
	struct sigcontext ctx;

	if (!restore_context)
		restore_context = arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 4;

	MONO_CONTEXT_SET_BP (&ctx, esp);
	MONO_CONTEXT_SET_IP (&ctx, eip);
#if 0
	ctx.SC_ESP = esp;
	ctx.SC_EIP = eip;
	ctx.SC_EBP = ebp;
	ctx.SC_EDI = edi;
	ctx.SC_ESI = esi;
	ctx.SC_EBX = ebx;
	ctx.SC_EDX = edx;
	ctx.SC_ECX = ecx;
	ctx.SC_EAX = eax;
#endif	
	mono_arch_handle_exception (&ctx, exc, FALSE);
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

	ppc_mflr (code, ppc_r0);
	ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);
	alloc_size = PPC_MINIMAL_STACK_SIZE + (sizeof (gulong) * 14 + sizeof (gdouble) * 14);
	// align to PPC_STACK_ALIGNMENT bytes
	if (alloc_size & (PPC_STACK_ALIGNMENT - 1))
		alloc_size += PPC_STACK_ALIGNMENT - (alloc_size & (PPC_STACK_ALIGNMENT - 1));
	ppc_stwu (code, ppc_sp, -alloc_size, ppc_sp);
	//ppc_break (code);
	if (by_name) {
		ppc_mr (code, ppc_r5, ppc_r3);
		ppc_load (code, ppc_r3, mono_defaults.corlib);
		ppc_load (code, ppc_r4, "System");
		ppc_bl (code, 0);
		ppc_patch (code - 4, mono_exception_from_name);
	}
	/* save all the regs on the stack */
	pos = PPC_MINIMAL_STACK_SIZE;
	ppc_stmw (code, ppc_r13, ppc_sp, pos);
	pos += sizeof (gulong) * 19;
	for (i = 14; i < 32; ++i) {
		ppc_stfd (code, i, pos, ppc_sp);
		pos += sizeof (gdouble);
	}
	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* exc is already in place in r3 */
	if (by_name)
		ppc_mflr (code, ppc_r4);
	else
		ppc_mr (code, ppc_r4, ppc_r0); /* caller ip */
	ppc_lwz (code, ppc_r5, 0, ppc_sp); /* caller sp */
	/* pointer to the saved int regs */
	pos = PPC_MINIMAL_STACK_SIZE;
	ppc_addi (code, ppc_r6, ppc_sp, pos);
	/* pointer to the saved fp regs */
	pos += sizeof (gulong) * 19;
	ppc_addi (code, ppc_r7, ppc_sp, pos);
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
static MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	unsigned long *ptr;
	char *p;
	MonoPPCStackFrame *sframe;

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
#if 0				
		offset = -1;
		/* restore caller saved registers */
		if (ji->used_regs & X86_EBX_MASK) {
			new_ctx->SC_EBX = *((int *)ctx->SC_EBP + offset);
			offset--;
		}
		if (ji->used_regs & X86_EDI_MASK) {
			new_ctx->SC_EDI = *((int *)ctx->SC_EBP + offset);
			offset--;
		}
		if (ji->used_regs & X86_ESI_MASK) {
			new_ctx->SC_ESI = *((int *)ctx->SC_EBP + offset);
		}

		new_ctx->SC_ESP = ctx->SC_EBP;
		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->SC_EIP = *((int *)ctx->SC_EBP + 1) - 1;
		new_ctx->SC_EBP = *((int *)ctx->SC_EBP);
#endif
		sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr);
		*res = *ji;
		return res;
#ifdef MONO_USE_EXC_TABLES
	} else if ((ji = ppc_unwind_native_frame (domain, jit_tls, ctx, new_ctx, *lmf, trace))) {
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

#if 0
		new_ctx->SC_ESI = (*lmf)->esi;
		new_ctx->SC_EDI = (*lmf)->edi;
		new_ctx->SC_EBX = (*lmf)->ebx;
		new_ctx->SC_EBP = (*lmf)->ebp;
		new_ctx->SC_EIP = (*lmf)->eip;
		/* the lmf is always stored on the stack, so the following
		 * expression points to a stack location which can be used as ESP */
		new_ctx->SC_ESP = (unsigned long)&((*lmf)->eip);
#endif
		sframe = (MonoPPCStackFrame*)MONO_CONTEXT_GET_BP (ctx);
		MONO_CONTEXT_SET_BP (new_ctx, sframe->sp);
		MONO_CONTEXT_SET_IP (new_ctx, sframe->lr);
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
		g_assert (ji != NULL);

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
mono_jit_walk_stack (MonoStackWalk func, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoPPCStackFrame *sframe;

	MonoContext ctx, new_ctx;

#ifdef __APPLE__
	__asm__ volatile("lwz   %0,0(r1)" : "=r" (sframe));
#else
	__asm__ volatile("lwz   %0,0(1)" : "=r" (sframe));
#endif
	MONO_CONTEXT_SET_IP (&ctx, sframe->lr);
	MONO_CONTEXT_SET_BP (&ctx, sframe->sp);

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = mono_debug_il_offset_from_address (ji->method, native_offset, domain);

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
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

	MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));

	skip++;

	do {
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &ctx, &new_ctx, NULL, &lmf, native_offset, NULL);

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

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 *
 */
gboolean
mono_arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer, gpointer) = NULL;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoException *mono_ex;

	g_assert (ctx != NULL);
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
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, ctx, &new_ctx, 
					      test_only ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				char *tmp, *strace;

				trace_ips = g_list_append (trace_ips, MONO_CONTEXT_GET_IP (ctx));

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

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */
						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (ji->method->klass->image, ei->data.token))) ||
						    ((ei->flags == MONO_EXCEPTION_CLAUSE_FILTER &&
						      call_filter (ctx, ei->data.filter, obj)))) {
							if (test_only) {
								if (mono_ex)
									mono_ex->trace_ips = glist_to_array (trace_ips);
								g_list_free (trace_ips);
								g_free (trace);
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
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
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
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
				if (mono_ex)
					mono_ex->trace_ips = glist_to_array (trace_ips);
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

