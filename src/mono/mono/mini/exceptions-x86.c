/*
 * exceptions-x86.c: exception support for x86
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-x86.h"

#ifdef PLATFORM_WIN32
static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

static LPTOP_LEVEL_EXCEPTION_FILTER old_handler;

#define W32_SEH_HANDLE_EX(_ex) \
	if (_ex##_handler) _ex##_handler((int)sctx)

/*
 * Unhandled Exception Filter
 * Top-level per-process exception handler.
 */
LONG CALLBACK seh_handler(EXCEPTION_POINTERS* ep)
{
	EXCEPTION_RECORD* er;
	CONTEXT* ctx;
	struct sigcontext* sctx;
	LONG res;

	res = EXCEPTION_CONTINUE_EXECUTION;

	er = ep->ExceptionRecord;
	ctx = ep->ContextRecord;
	sctx = g_malloc(sizeof(struct sigcontext));

	/* Copy Win32 context to UNIX style context */
	sctx->eax = ctx->Eax;
	sctx->ebx = ctx->Ebx;
	sctx->ecx = ctx->Ecx;
	sctx->edx = ctx->Edx;
	sctx->ebp = ctx->Ebp;
	sctx->esp = ctx->Esp;
	sctx->esi = ctx->Esi;
	sctx->edi = ctx->Edi;
	sctx->eip = ctx->Eip;

	switch (er->ExceptionCode) {
	case EXCEPTION_ACCESS_VIOLATION:
		W32_SEH_HANDLE_EX(segv);
		break;
	case EXCEPTION_ILLEGAL_INSTRUCTION:
		W32_SEH_HANDLE_EX(ill);
		break;
	case EXCEPTION_INT_DIVIDE_BY_ZERO:
	case EXCEPTION_INT_OVERFLOW:
	case EXCEPTION_FLT_DIVIDE_BY_ZERO:
	case EXCEPTION_FLT_OVERFLOW:
	case EXCEPTION_FLT_UNDERFLOW:
	case EXCEPTION_FLT_INEXACT_RESULT:
		W32_SEH_HANDLE_EX(fpe);
		break;
	default:
		break;
	}

	/* Copy context back */
	ctx->Eax = sctx->eax;
	ctx->Ebx = sctx->ebx;
	ctx->Ecx = sctx->ecx;
	ctx->Edx = sctx->edx;
	ctx->Ebp = sctx->ebp;
	ctx->Esp = sctx->esp;
	ctx->Esi = sctx->esi;
	ctx->Edi = sctx->edi;
	ctx->Eip = sctx->eip;

	return res;
}

void win32_seh_init()
{
	old_handler = SetUnhandledExceptionFilter(seh_handler);
}

void win32_seh_cleanup()
{
	if (old_handler) SetUnhandledExceptionFilter(old_handler);
}

void win32_seh_set_handler(int type, MonoW32ExceptionHandler handler)
{
	switch (type) {
	case SIGFPE:
		fpe_handler = handler;
		break;
	case SIGILL:
		ill_handler = handler;
		break;
	case SIGSEGV:
		segv_handler = handler;
		break;
	default:
		break;
	}
}

#endif /* PLATFORM_WIN32 */


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

/* mono_arch_has_unwind_info:
 *
 * Tests if a function has an DWARF exception table able to restore
 * all caller saved registers. 
 */
gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	struct frame_state state_in;
	struct frame_state *res;

	if (!inited) 
		init_frame_state_for ();
	
	if (!frame_state_for)
		return FALSE;

	g_assert (addr);

	memset (&state_in, 0, sizeof (state_in));

	/* offset 10 is just a guess, but it works for all methods tested */
	if ((res = frame_state_for ((char *)addr + 10, &state_in))) {

		if (res->saved [X86_EBX] == 1 &&
		    res->saved [X86_EDI] == 1 &&
		    res->saved [X86_EBP] == 1 &&
		    res->saved [X86_ESI] == 1)
			return TRUE;
	}

	return FALSE;
}

struct stack_frame
{
  void *next;
  void *return_address;
};

static MonoJitInfo *
x86_unwind_native_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, struct sigcontext *ctx, 
			 struct sigcontext *new_ctx, MonoLMF *lmf, char **trace)
{
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
	return NULL;
}

#endif

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

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
	static guint8 start [64];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (struct sigcontext *ctx, unsigned long eip) */
	code = start;

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	/* load eip */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 12, 4);
	/* save EBP */
	x86_push_reg (code, X86_EBP);

	/* set new EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBP), 4);
	/* restore registers used by global register allocation (EBX & ESI) */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBX), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESI), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EDI), 4);

	/* call the handler */
	x86_call_reg (code, X86_ECX);

	/* restore EBP */
	x86_pop_reg (code, X86_EBP);

	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	g_assert ((code - start) < 64);
	return start;
}

static void
throw_exception (unsigned long eax, unsigned long ecx, unsigned long edx, unsigned long ebx,
		 unsigned long esi, unsigned long edi, unsigned long ebp, MonoObject *exc,
		 unsigned long eip,  unsigned long esp)
{
	static void (*restore_context) (struct sigcontext *);
	struct sigcontext ctx;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 1;

	/* Pop argument and return address */
	ctx.SC_ESP = esp + (2 * sizeof (gpointer));
	ctx.SC_EIP = eip;
	ctx.SC_EBP = ebp;
	ctx.SC_EDI = edi;
	ctx.SC_ESI = esi;
	ctx.SC_EBX = ebx;
	ctx.SC_EDX = edx;
	ctx.SC_ECX = ecx;
	ctx.SC_EAX = eax;
	
	mono_arch_handle_exception (&ctx, exc, FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

/**
 * mono_arch_get_throw_exception:
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
	static guint8 start [24];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	x86_push_reg (code, X86_ESP);
	x86_push_membase (code, X86_ESP, 4); /* IP */
	x86_push_membase (code, X86_ESP, 12); /* exception */
	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDX);
	x86_push_reg (code, X86_ECX);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, throw_exception);
	/* we should never reach this breakpoint */
	x86_breakpoint (code);

	g_assert ((code - start) < 24);
	return start;
}

/**
 * mono_arch_get_throw_exception_by_name:
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
	static guint8 start [32];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	x86_push_membase (code, X86_ESP, 4); /* exception name */
	x86_push_imm (code, "System");
	x86_push_imm (code, mono_defaults.exception_class->image);
	x86_call_code (code, mono_exception_from_name);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
	/* save the newly create object (overwrite exception name)*/
	x86_mov_membase_reg (code, X86_ESP, 4, X86_EAX, 4);
	x86_jump_code (code, mono_arch_get_throw_exception ());

	g_assert ((code - start) < 32);

	return start;
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
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

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
		int offset;

		*new_ctx = *ctx;

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

		/*
		 * Some managed methods like pinvoke wrappers might have save_lmf set.
		 * In this case, register save/restore code is not generated by the 
		 * JIT, so we have to restore callee saved registers from the lmf.
		 */
		if (ji->method->save_lmf) {
			/* 
			 * We only need to do this if the exception was raised in managed
			 * code, since otherwise the lmf was already popped of the stack.
			 */
			if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
				new_ctx->SC_ESI = (*lmf)->esi;
				new_ctx->SC_EDI = (*lmf)->edi;
				new_ctx->SC_EBX = (*lmf)->ebx;
			}
		}
		else {
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
		}

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		/* Pop EBP and the return address */
		new_ctx->SC_ESP = ctx->SC_EBP + (2 * sizeof (gpointer));
		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->SC_EIP = *((int *)ctx->SC_EBP + 1) - 1;
		new_ctx->SC_EBP = *((int *)ctx->SC_EBP);

		*res = *ji;
		return res;
#ifdef MONO_USE_EXC_TABLES
	} else if ((ji = x86_unwind_native_frame (domain, jit_tls, ctx, new_ctx, *lmf, trace))) {
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

		new_ctx->SC_ESI = (*lmf)->esi;
		new_ctx->SC_EDI = (*lmf)->edi;
		new_ctx->SC_EBX = (*lmf)->ebx;
		new_ctx->SC_EBP = (*lmf)->ebp;
		new_ctx->SC_EIP = (*lmf)->eip;
		/* the lmf is always stored on the stack, so the following
		 * expression points to a stack location which can be used as ESP */
		new_ctx->SC_ESP = (unsigned long)&((*lmf)->eip);

		*lmf = (*lmf)->previous_lmf;

		return res;
		
	}

	return NULL;
}

/**
 * mono_arch_handle_exception:
 *
 * @ctx: saved processor state
 * @obj: the exception object
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	return mono_handle_exception (sigctx, obj, test_only);
}
