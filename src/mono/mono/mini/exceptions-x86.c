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
#include <mono/utils/mono-mmap.h>

#include "mini.h"
#include "mini-x86.h"
#include "tasklets.h"
#include "debug-mini.h"

static gpointer signal_exception_trampoline;

gpointer
mono_x86_get_signal_exception_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;

#ifdef TARGET_WIN32
static void (*restore_stack) (void *);

static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

LPTOP_LEVEL_EXCEPTION_FILTER mono_old_win_toplevel_exception_filter;
guint64 mono_win_chained_exception_filter_result;
gboolean mono_win_chained_exception_filter_didrun;

#ifndef PROCESS_CALLBACK_FILTER_ENABLED
#	define PROCESS_CALLBACK_FILTER_ENABLED 1
#endif

#define W32_SEH_HANDLE_EX(_ex) \
	if (_ex##_handler) _ex##_handler(0, ep, sctx)

/*
 * mono_win32_get_handle_stackoverflow (void):
 *
 * Returns a pointer to a method which restores the current context stack
 * and calls handle_exceptions, when done restores the original stack.
 */
static gpointer
mono_win32_get_handle_stackoverflow (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	/* restore_contect (void *sigctx) */
	start = code = mono_global_codeman_reserve (128);

	/* load context into ebx */
	x86_mov_reg_membase (code, X86_EBX, X86_ESP, 4, 4);

	/* move current stack into edi for later restore */
	x86_mov_reg_reg (code, X86_EDI, X86_ESP, 4);

	/* use the new freed stack from sigcontext */
	x86_mov_reg_membase (code, X86_ESP, X86_EBX,  G_STRUCT_OFFSET (struct sigcontext, esp), 4);

	/* get the current domain */
	x86_call_code (code, mono_domain_get);

	/* get stack overflow exception from domain object */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX, G_STRUCT_OFFSET (MonoDomain, stack_overflow_ex), 4);

	/* call mono_arch_handle_exception (sctx, stack_overflow_exception_obj) */
	x86_push_reg (code, X86_EAX);
	x86_push_reg (code, X86_EBX);
	x86_call_code (code, mono_arch_handle_exception);

	/* restore the SEH handler stack */
	x86_mov_reg_reg (code, X86_ESP, X86_EDI, 4);

	/* return */
	x86_ret (code);

	return start;
}

/* Special hack to workaround the fact that the
 * when the SEH handler is called the stack is
 * to small to recover.
 *
 * Stack walking part of this method is from mono_handle_exception
 *
 * The idea is simple; 
 *  - walk the stack to free some space (64k)
 *  - set esp to new stack location
 *  - call mono_arch_handle_exception with stack overflow exception
 *  - set esp to SEH handlers stack
 *  - done
 */
static void 
win32_handle_stack_overflow (EXCEPTION_POINTERS* ep, struct sigcontext *sctx) 
{
	SYSTEM_INFO si;
	DWORD page_size;
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo rji;
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	MonoContext initial_ctx;
	MonoContext ctx;
	guint32 free_stack = 0;
	StackFrameInfo frame;

	/* convert sigcontext to MonoContext (due to reuse of stack walking helpers */
	mono_arch_sigctx_to_monoctx (sctx, &ctx);
	
	/* get our os page size */
	GetSystemInfo(&si);
	page_size = si.dwPageSize;

	/* Let's walk the stack to recover
	 * the needed stack space (if possible)
	 */
	memset (&rji, 0, sizeof (rji));

	initial_ctx = ctx;
	free_stack = (guint8*)(MONO_CONTEXT_GET_BP (&ctx)) - (guint8*)(MONO_CONTEXT_GET_BP (&initial_ctx));

	/* try to free 64kb from our stack */
	do {
		MonoContext new_ctx;

		mono_arch_find_jit_info (domain, jit_tls, &rji, &ctx, &new_ctx, &lmf, NULL, &frame);
		if (!frame.ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (frame.ji != (gpointer)-1) {
			free_stack = (guint8*)(MONO_CONTEXT_GET_BP (&ctx)) - (guint8*)(MONO_CONTEXT_GET_BP (&initial_ctx));
		}

		/* todo: we should call abort if ji is -1 */
		ctx = new_ctx;
	} while (free_stack < 64 * 1024 && frame.ji != (gpointer) -1);

	/* convert into sigcontext to be used in mono_arch_handle_exception */
	mono_arch_monoctx_to_sigctx (&ctx, sctx);

	/* todo: install new stack-guard page */

	/* use the new stack and call mono_arch_handle_exception () */
	restore_stack (sctx);
}

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

	mono_win_chained_exception_filter_didrun = FALSE;
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
	case EXCEPTION_STACK_OVERFLOW:
		win32_handle_stack_overflow (ep, sctx);
		break;
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

	g_free (sctx);

	if (mono_win_chained_exception_filter_didrun)
		res = mono_win_chained_exception_filter_result;

	return res;
}

void win32_seh_init()
{
	/* install restore stack helper */
	if (!restore_stack)
		restore_stack = mono_win32_get_handle_stackoverflow ();

	mono_old_win_toplevel_exception_filter = SetUnhandledExceptionFilter(seh_handler);
}

void win32_seh_cleanup()
{
	if (mono_old_win_toplevel_exception_filter) SetUnhandledExceptionFilter(mono_old_win_toplevel_exception_filter);
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

#endif /* TARGET_WIN32 */

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	guint8 *start = NULL;
	guint8 *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	/* restore_contect (MonoContext *ctx) */

	start = code = mono_global_codeman_reserve (128);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* get return address, stored in ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, eip), 4);
	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, ebx), 4);
	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (MonoContext, edi), 4);
	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (MonoContext, esi), 4);
	/* restore ESP */
	x86_mov_reg_membase (code, X86_ESP, X86_EAX,  G_STRUCT_OFFSET (MonoContext, esp), 4);
	/* save the return addr to the restored stack */
	x86_push_reg (code, X86_ECX);
	/* restore EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (MonoContext, ebp), 4);
	/* restore ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, ecx), 4);
	/* restore EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, edx), 4);
	/* restore EAX */
	x86_mov_reg_membase (code, X86_EAX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, eax), 4);

	/* jump to the saved IP */
	x86_ret (code);

	nacl_global_codeman_validate(&start, 128, &code);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("restore_context"), start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

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
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	guint8* start;
	guint8 *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	guint kMaxCodeSize = NACL_SIZE (64, 128);

	/* call_filter (MonoContext *ctx, unsigned long eip) */
	start = code = mono_global_codeman_reserve (kMaxCodeSize);

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
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (MonoContext, ebp), 4);
	/* restore registers used by global register allocation (EBX & ESI) */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (MonoContext, ebx), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (MonoContext, esi), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (MonoContext, edi), 4);

	/* align stack and save ESP */
	x86_mov_reg_reg (code, X86_EDX, X86_ESP, 4);
	x86_alu_reg_imm (code, X86_AND, X86_ESP, -MONO_ARCH_FRAME_ALIGNMENT);
	g_assert (MONO_ARCH_FRAME_ALIGNMENT >= 8);
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, MONO_ARCH_FRAME_ALIGNMENT - 8);
	x86_push_reg (code, X86_EDX);

	/* call the handler */
	x86_call_reg (code, X86_ECX);

	/* restore ESP */
	x86_pop_reg (code, X86_ESP);

	/* restore EBP */
	x86_pop_reg (code, X86_EBP);

	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	nacl_global_codeman_validate(&start, kMaxCodeSize, &code);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("call_filter"), start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	g_assert ((code - start) < kMaxCodeSize);
	return start;
}

/*
 * mono_x86_throw_exception:
 *
 *   C function called from the throw trampolines.
 */
void
mono_x86_throw_exception (mgreg_t *regs, MonoObject *exc, 
						  mgreg_t eip, gboolean rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	ctx.esp = regs [X86_ESP];
	ctx.eip = eip;
	ctx.ebp = regs [X86_EBP];
	ctx.edi = regs [X86_EDI];
	ctx.esi = regs [X86_ESI];
	ctx.ebx = regs [X86_EBX];
	ctx.edx = regs [X86_EDX];
	ctx.ecx = regs [X86_ECX];
	ctx.eax = regs [X86_EAX];

#ifdef __APPLE__
	/* The OSX ABI specifies 16 byte alignment at call sites */
	g_assert ((ctx.esp % MONO_ARCH_FRAME_ALIGNMENT) == 0);
#endif

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}

	if (mono_debug_using_mono_debugger ()) {
		guint8 buf [16], *code;

		mono_breakpoint_clean_code (NULL, (gpointer)eip, 8, buf, sizeof (buf));
		code = buf + 8;

		if (buf [3] == 0xe8) {
			MonoContext ctx_cp = ctx;
			ctx_cp.eip = eip - 5;

			if (mono_debugger_handle_exception (&ctx_cp, exc)) {
				restore_context (&ctx_cp);
				g_assert_not_reached ();
			}
		}
	}

	/* adjust eip so that it point into the call instruction */
	ctx.eip -= 1;

	mono_handle_exception (&ctx, exc);

	restore_context (&ctx);

	g_assert_not_reached ();
}

void
mono_x86_throw_corlib_exception (mgreg_t *regs, guint32 ex_token_index, 
								 mgreg_t eip, gint32 pc_offset)
{
	guint32 ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
	MonoException *ex;

	ex = mono_exception_from_token (mono_defaults.exception_class->image, ex_token);

	eip -= pc_offset;

	/* Negate the ip adjustment done in mono_x86_throw_exception () */
	eip += 1;

	mono_x86_throw_exception (regs, (MonoObject*)ex, eip, FALSE);
}

static void
mono_x86_resume_unwind (mgreg_t *regs, MonoObject *exc, 
						mgreg_t eip, gboolean rethrow)
{
	MonoContext ctx;

	ctx.esp = regs [X86_ESP];
	ctx.eip = eip;
	ctx.ebp = regs [X86_EBP];
	ctx.edi = regs [X86_EDI];
	ctx.esi = regs [X86_ESI];
	ctx.ebx = regs [X86_EBX];
	ctx.edx = regs [X86_EDX];
	ctx.ecx = regs [X86_ECX];
	ctx.eax = regs [X86_EAX];

	mono_resume_unwind (&ctx);
}

/*
 * get_throw_trampoline:
 *
 *  Generate a call to mono_x86_throw_exception/
 * mono_x86_throw_corlib_exception.
 * If LLVM is true, generate code which assumes the caller is LLVM generated code, 
 * which doesn't push the arguments.
 */
static guint8*
get_throw_trampoline (const char *name, gboolean rethrow, gboolean llvm, gboolean corlib, gboolean llvm_abs, gboolean resume_unwind, MonoTrampInfo **info, gboolean aot)
{
	guint8 *start, *code;
	int i, stack_size, stack_offset, arg_offsets [5], regs_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	guint kMaxCodeSize = NACL_SIZE (128, 256);

	start = code = mono_global_codeman_reserve (kMaxCodeSize);

	stack_size = 128;

	/* 
	 * On apple, the stack is misaligned by the pushing of the return address.
	 */
	if (!llvm && corlib)
		/* On OSX, we don't generate alignment code to save space */
		stack_size += 4;
	else
		stack_size += MONO_ARCH_FRAME_ALIGNMENT - 4;

	/*
	 * The stack looks like this:
	 * <pc offset> (only if corlib is TRUE)
	 * <exception object>/<type token>
	 * <return addr> <- esp (unaligned on apple)
	 */

	mono_add_unwind_op_def_cfa (unwind_ops, (guint8*)NULL, (guint8*)NULL, X86_ESP, 4);
	mono_add_unwind_op_offset (unwind_ops, (guint8*)NULL, (guint8*)NULL, X86_NREG, -4);

	/* Alloc frame */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, stack_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, stack_size + 4);

	arg_offsets [0] = 0;
	arg_offsets [1] = 4;
	arg_offsets [2] = 8;
	arg_offsets [3] = 12;
	regs_offset = 16;

	/* Save registers */
	for (i = 0; i < X86_NREG; ++i)
		if (i != X86_ESP)
			x86_mov_membase_reg (code, X86_ESP, regs_offset + (i * 4), i, 4);
	/* Calculate the offset between the current sp and the sp of the caller */
	if (llvm) {
		/* LLVM doesn't push the arguments */
		stack_offset = stack_size + 4;
	} else {
		if (corlib) {
			/* Two arguments */
			stack_offset = stack_size + 4 + 8;
#ifdef __APPLE__
			/* We don't generate stack alignment code on osx to save space */
#endif
		} else {
			/* One argument + stack alignment */
			stack_offset = stack_size + 4 + 4;
#ifdef __APPLE__
			/* Pop the alignment added by OP_THROW too */
			stack_offset += MONO_ARCH_FRAME_ALIGNMENT - 4;
#else
			if (mono_do_x86_stack_align)
				stack_offset += MONO_ARCH_FRAME_ALIGNMENT - 4;
#endif
		}
	}
	/* Save ESP */
	x86_lea_membase (code, X86_EAX, X86_ESP, stack_offset);
	x86_mov_membase_reg (code, X86_ESP, regs_offset + (X86_ESP * 4), X86_EAX, 4);

	/* Set arg1 == regs */
	x86_lea_membase (code, X86_EAX, X86_ESP, regs_offset);
	x86_mov_membase_reg (code, X86_ESP, arg_offsets [0], X86_EAX, 4);
	/* Set arg2 == exc/ex_token_index */
	if (resume_unwind)
		x86_mov_reg_imm (code, X86_EAX, 0);
	else
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size + 4, 4);
	x86_mov_membase_reg (code, X86_ESP, arg_offsets [1], X86_EAX, 4);
	/* Set arg3 == eip */
	if (llvm_abs)
		x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);
	else
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size, 4);
	x86_mov_membase_reg (code, X86_ESP, arg_offsets [2], X86_EAX, 4);
	/* Set arg4 == rethrow/pc_offset */
	if (resume_unwind) {
		x86_mov_membase_imm (code, X86_ESP, arg_offsets [3], 0, 4);
	} else if (corlib) {
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size + 8, 4);
		if (llvm_abs) {
			/* 
			 * The caller is LLVM code which passes the absolute address not a pc offset,
			 * so compensate by passing 0 as 'ip' and passing the negated abs address as
			 * the pc offset.
			 */
			x86_neg_reg (code, X86_EAX);
		}
		x86_mov_membase_reg (code, X86_ESP, arg_offsets [3], X86_EAX, 4);
	} else {
		x86_mov_membase_imm (code, X86_ESP, arg_offsets [3], rethrow, 4);
	}
	/* Make the call */
	if (aot) {
		// This can be called from runtime code, which can't guarantee that
		// ebx contains the got address.
		// So emit the got address loading code too
		code = mono_arch_emit_load_got_addr (start, code, NULL, &ji);
		code = mono_arch_emit_load_aotconst (start, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, corlib ? "mono_x86_throw_corlib_exception" : "mono_x86_throw_exception");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, resume_unwind ? (mono_x86_resume_unwind) : (corlib ? (gpointer)mono_x86_throw_corlib_exception : (gpointer)mono_x86_throw_exception));
	}
	x86_breakpoint (code);

	nacl_global_codeman_validate(&start, kMaxCodeSize, &code);

	g_assert ((code - start) < kMaxCodeSize);

	if (info)
		*info = mono_tramp_info_create (g_strdup (name), start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	return start;
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
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("throw_exception", FALSE, FALSE, FALSE, FALSE, FALSE, info, aot);
}

gpointer 
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("rethrow_exception", TRUE, FALSE, FALSE, FALSE, FALSE, info, aot);
}

/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 */
gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("throw_corlib_exception", FALSE, FALSE, TRUE, FALSE, FALSE, info, aot);
}

void
mono_arch_exceptions_init (void)
{
	guint8 *tramp;

/* 
 * If we're running WoW64, we need to set the usermode exception policy 
 * for SEHs to behave. This requires hotfix http://support.microsoft.com/kb/976038
 * or (eventually) Windows 7 SP1.
 */
#ifdef HOST_WIN32
	DWORD flags;
	FARPROC getter;
	FARPROC setter;
	HMODULE kernel32 = LoadLibraryW (L"kernel32.dll");

	if (kernel32) {
		getter = GetProcAddress (kernel32, "GetProcessUserModeExceptionPolicy");
		setter = GetProcAddress (kernel32, "SetProcessUserModeExceptionPolicy");
		if (getter && setter) {
			if (getter (&flags))
				setter (flags & ~PROCESS_CALLBACK_FILTER_ENABLED);
		}
	}
#endif

	if (mono_aot_only) {
		signal_exception_trampoline = mono_aot_get_trampoline ("x86_signal_exception_trampoline");
		return;
	}

	/* LLVM needs different throw trampolines */
	tramp = get_throw_trampoline ("llvm_throw_exception_trampoline", FALSE, TRUE, FALSE, FALSE, FALSE, NULL, FALSE);
	mono_register_jit_icall (tramp, "llvm_throw_exception_trampoline", NULL, TRUE);

	tramp = get_throw_trampoline ("llvm_rethrow_exception_trampoline", FALSE, TRUE, FALSE, FALSE, FALSE, NULL, FALSE);
	mono_register_jit_icall (tramp, "llvm_rethrow_exception_trampoline", NULL, TRUE);

	tramp = get_throw_trampoline ("llvm_throw_corlib_exception_trampoline", FALSE, TRUE, TRUE, FALSE, FALSE, NULL, FALSE);
	mono_register_jit_icall (tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE);

	tramp = get_throw_trampoline ("llvm_throw_corlib_exception_abs_trampoline", FALSE, TRUE, TRUE, TRUE, FALSE, NULL, FALSE);
	mono_register_jit_icall (tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE);

	tramp = get_throw_trampoline ("llvm_resume_unwind_trampoline", FALSE, FALSE, FALSE, FALSE, TRUE, NULL, FALSE);
	mono_register_jit_icall (tramp, "llvm_resume_unwind_trampoline", NULL, TRUE);

	signal_exception_trampoline = mono_x86_get_signal_exception_trampoline (NULL, FALSE);
}

/*
 * mono_arch_find_jit_info:
 *
 * See exceptions-amd64.c for docs.
 */
gboolean
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		gssize regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		frame->type = FRAME_TYPE_MANAGED;

		if (ji->from_aot)
			unwind_info = mono_aot_get_unwind_info (ji, &unwind_info_len);
		else
			unwind_info = mono_get_cached_unwind_info (ji->used_regs, &unwind_info_len);

		regs [X86_EAX] = new_ctx->eax;
		regs [X86_EBX] = new_ctx->ebx;
		regs [X86_ECX] = new_ctx->ecx;
		regs [X86_EDX] = new_ctx->edx;
		regs [X86_ESP] = new_ctx->esp;
		regs [X86_EBP] = new_ctx->ebp;
		regs [X86_ESI] = new_ctx->esi;
		regs [X86_EDI] = new_ctx->edi;
		regs [X86_NREG] = new_ctx->eip;

		mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start, 
						   (guint8*)ji->code_start + ji->code_size,
						   ip, regs, MONO_MAX_IREGS + 1,
						   save_locations, MONO_MAX_IREGS, &cfa);

		new_ctx->eax = regs [X86_EAX];
		new_ctx->ebx = regs [X86_EBX];
		new_ctx->ecx = regs [X86_ECX];
		new_ctx->edx = regs [X86_EDX];
		new_ctx->esp = regs [X86_ESP];
		new_ctx->ebp = regs [X86_EBP];
		new_ctx->esi = regs [X86_ESI];
		new_ctx->edi = regs [X86_EDI];
		new_ctx->eip = regs [X86_NREG];

 		/* The CFA becomes the new SP value */
		new_ctx->esp = (gssize)cfa;

		/* Adjust IP */
		new_ctx->eip --;

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (gpointer)(((gsize)(*lmf)->previous_lmf) & ~3);
		}

		/* Pop arguments off the stack */
		/* 
		 * FIXME: LLVM doesn't push these, we can't use ji->from_llvm as it describes
		 * the callee.
		 */
#ifndef ENABLE_LLVM
		if (ji->has_arch_eh_info)
			new_ctx->esp += mono_jit_info_get_arch_eh_info (ji)->stack_size;
#endif

		return TRUE;
	} else if (*lmf) {

		if (((guint64)(*lmf)->previous_lmf) & 2) {
			/* 
			 * This LMF entry is created by the soft debug code to mark transitions to
			 * managed code done during invokes.
			 */
			MonoLMFExt *ext = (MonoLMFExt*)(*lmf);

			g_assert (ext->debugger_invoke);

			memcpy (new_ctx, &ext->ctx, sizeof (MonoContext));

			*lmf = (gpointer)(((gsize)(*lmf)->previous_lmf) & ~3);

			frame->type = FRAME_TYPE_DEBUGGER_INVOKE;

			return TRUE;
		}
		
		if ((ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->eip, NULL))) {
		} else {
			if (!((guint32)((*lmf)->previous_lmf) & 1))
				/* Top LMF entry */
				return FALSE;
			g_assert_not_reached ();
			/* Trampoline lmf frame */
			frame->method = (*lmf)->method;
		}

		new_ctx->esi = (*lmf)->esi;
		new_ctx->edi = (*lmf)->edi;
		new_ctx->ebx = (*lmf)->ebx;
		new_ctx->ebp = (*lmf)->ebp;
		new_ctx->eip = (*lmf)->eip;

		/* Adjust IP */
		new_ctx->eip --;

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		/* Check if we are in a trampoline LMF frame */
		if ((guint32)((*lmf)->previous_lmf) & 1) {
			/* lmf->esp is set by the trampoline code */
			new_ctx->esp = (*lmf)->esp;

			/* Pop arguments off the stack */
			/* FIXME: Handle the delegate case too ((*lmf)->method == NULL) */
			/* FIXME: Handle the IMT/vtable case too */
#ifndef ENABLE_LLVM
			if ((*lmf)->method) {
				MonoMethod *method = (*lmf)->method;
				MonoJitArgumentInfo *arg_info = g_newa (MonoJitArgumentInfo, mono_method_signature (method)->param_count + 1);

				guint32 stack_to_pop = mono_arch_get_argument_info (NULL, mono_method_signature (method), mono_method_signature (method)->param_count, arg_info);
				new_ctx->esp += stack_to_pop;
			}
#endif
		}
		else
			/* the lmf is always stored on the stack, so the following
			 * expression points to a stack location which can be used as ESP */
			new_ctx->esp = (unsigned long)&((*lmf)->eip);

		*lmf = (gpointer)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

void
mono_arch_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
	mono_sigctx_to_monoctx (sigctx, mctx);
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
	mono_monoctx_to_sigctx (mctx, sigctx);
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#if defined(__native_client__)
	printf("WARNING: mono_arch_ip_from_context() called!\n");
	return (NULL);
#else
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;
	return (gpointer)UCONTEXT_REG_EIP (ctx);
#else
	struct sigcontext *ctx = sigctx;
	return (gpointer)ctx->SC_EIP;
#endif
#endif	/* __native_client__ */
}

/*
 * handle_exception:
 *
 *   Called by resuming from a signal handler.
 */
static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	MonoContext ctx;
	static void (*restore_context) (MonoContext *);

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	memcpy (&ctx, &jit_tls->ex_ctx, sizeof (MonoContext));

	if (mono_debugger_handle_exception (&ctx, (MonoObject *)obj))
		return;

	mono_handle_exception (&ctx, obj);

	restore_context (&ctx);
}

/*
 * mono_x86_get_signal_exception_trampoline:
 *
 *   This x86 specific trampoline is used to call handle_signal_exception.
 */
gpointer
mono_x86_get_signal_exception_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int stack_size;

	start = code = mono_global_codeman_reserve (128);

	/* Caller ip */
	x86_push_reg (code, X86_ECX);

	mono_add_unwind_op_def_cfa (unwind_ops, (guint8*)NULL, (guint8*)NULL, X86_ESP, 4);
	mono_add_unwind_op_offset (unwind_ops, (guint8*)NULL, (guint8*)NULL, X86_NREG, -4);

	/* Fix the alignment to be what apple expects */
	stack_size = 12;

	x86_alu_reg_imm (code, X86_SUB, X86_ESP, stack_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, stack_size + 4);

	/* Arg1 */
	x86_mov_membase_reg (code, X86_ESP, 0, X86_EAX, 4);
	/* Branch to target */
	x86_call_reg (code, X86_EDX);

	g_assert ((code - start) < 128);

	if (info)
		*info = mono_tramp_info_create (g_strdup ("x86_signal_exception_trampoline"), start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	return start;
}


void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	/*
	 * Can't pass the obj on the stack, since we are executing on the
	 * same stack. Can't save it into MonoJitTlsData, since it needs GC tracking.
	 * So put it into a register, and branch to a trampoline which
	 * pushes it.
	 */
	ctx->eax = (mgreg_t)user_data;
	ctx->ecx = ctx->eip;
	ctx->edx = (mgreg_t)async_cb;

	/*align the stack*/
	ctx->esp = (ctx->esp - 16) & ~15;
	ctx->eip = (mgreg_t)signal_exception_trampoline;
}

gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj)
{
#if defined(MONO_ARCH_USE_SIGACTION)
	MonoContext mctx;
	ucontext_t *ctx = (ucontext_t*)sigctx;

	/*
	 * Handling the exception in the signal handler is problematic, since the original
	 * signal is disabled, and we could run arbitrary code though the debugger. So
	 * resume into the normal stack and do most work there if possible.
	 */
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);

	/* Pass the ctx parameter in TLS */
	mono_arch_sigctx_to_monoctx (ctx, &jit_tls->ex_ctx);

	mctx = jit_tls->ex_ctx;
	mono_setup_async_callback (&mctx, handle_signal_exception, obj);
	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#elif defined (TARGET_WIN32)
	MonoContext mctx;
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	mono_arch_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);

	mctx = jit_tls->ex_ctx;
	mono_setup_async_callback (&mctx, handle_signal_exception, obj);
	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#else
	MonoContext mctx;

	mono_arch_sigctx_to_monoctx (sigctx, &mctx);

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj))
		return TRUE;

	mono_handle_exception (&mctx, obj);

	mono_arch_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#endif
}

static void
restore_soft_guard_pages (void)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	if (jit_tls->stack_ovf_guard_base)
		mono_mprotect (jit_tls->stack_ovf_guard_base, jit_tls->stack_ovf_guard_size, MONO_MMAP_NONE);
}

/* 
 * this function modifies mctx so that when it is restored, it
 * won't execcute starting at mctx.eip, but in a function that
 * will restore the protection on the soft-guard pages and return back to
 * continue at mctx.eip.
 */
static void
prepare_for_guard_pages (MonoContext *mctx)
{
	gpointer *sp;
	sp = (gpointer)(mctx->esp);
	sp -= 1;
	/* the resturn addr */
	sp [0] = (gpointer)(mctx->eip);
	mctx->eip = (unsigned long)restore_soft_guard_pages;
	mctx->esp = (unsigned long)sp;
}

static void
altstack_handle_and_restore (MonoContext *ctx, gpointer obj, gboolean stack_ovf)
{
	void (*restore_context) (MonoContext *);
	MonoContext mctx;

	restore_context = mono_get_restore_context ();
	mctx = *ctx;

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj)) {
		if (stack_ovf)
			prepare_for_guard_pages (&mctx);
		restore_context (&mctx);
	}

	mono_handle_exception (&mctx, obj);
	if (stack_ovf)
		prepare_for_guard_pages (&mctx);
	restore_context (&mctx);
}

void
mono_arch_handle_altstack_exception (void *sigctx, gpointer fault_addr, gboolean stack_ovf)
{
#ifdef MONO_ARCH_USE_SIGACTION
	MonoException *exc = NULL;
	ucontext_t *ctx = (ucontext_t*)sigctx;
	MonoJitInfo *ji = mini_jit_info_table_find (mono_domain_get (), (gpointer)UCONTEXT_REG_EIP (ctx), NULL);
	gpointer *sp;
	int frame_size;

	/* if we didn't find a managed method for the ip address and it matches the fault
	 * address, we assume we followed a broken pointer during an indirect call, so
	 * we try the lookup again with the return address pushed on the stack
	 */
	if (!ji && fault_addr == (gpointer)UCONTEXT_REG_EIP (ctx)) {
		glong *sp = (gpointer)UCONTEXT_REG_ESP (ctx);
		ji = mini_jit_info_table_find (mono_domain_get (), (gpointer)sp [0], NULL);
		if (ji)
			UCONTEXT_REG_EIP (ctx) = sp [0];
	}
	if (stack_ovf)
		exc = mono_domain_get ()->stack_overflow_ex;
	if (!ji)
		mono_handle_native_sigsegv (SIGSEGV, sigctx);
	/* setup a call frame on the real stack so that control is returned there
	 * and exception handling can continue.
	 * If this was a stack overflow the caller already ensured the stack pages
	 * needed have been unprotected.
	 * The frame looks like:
	 *   ucontext struct
	 *   test_only arg
	 *   exception arg
	 *   ctx arg
	 *   return ip
	 */
	// FIXME: test_only is no more.
 	frame_size = sizeof (MonoContext) + sizeof (gpointer) * 4;
	frame_size += 15;
	frame_size &= ~15;
	sp = (gpointer)(UCONTEXT_REG_ESP (ctx) & ~15);
	sp = (gpointer)((char*)sp - frame_size);
	/* the incoming arguments are aligned to 16 bytes boundaries, so the return address IP
	 * goes at sp [-1]
	 */
	sp [-1] = (gpointer)UCONTEXT_REG_EIP (ctx);
	sp [0] = sp + 4;
	sp [1] = exc;
	sp [2] = (gpointer)stack_ovf;
	mono_sigctx_to_monoctx (sigctx, (MonoContext*)(sp + 4));
	/* at the return form the signal handler execution starts in altstack_handle_and_restore() */
	UCONTEXT_REG_EIP (ctx) = (unsigned long)altstack_handle_and_restore;
	UCONTEXT_REG_ESP (ctx) = (unsigned long)(sp - 1);
#endif
}

#if MONO_SUPPORT_TASKLETS
MonoContinuationRestore
mono_tasklets_arch_restore (void)
{
	static guint8* saved = NULL;
	guint8 *code, *start;

#ifdef __native_client_codegen__
	g_print("mono_tasklets_arch_restore needs to be aligned for Native Client\n");
#endif
	if (saved)
		return (MonoContinuationRestore)saved;
	code = start = mono_global_codeman_reserve (48);
	/* the signature is: restore (MonoContinuation *cont, int state, MonoLMF **lmf_addr) */
	/* put cont in edx */
	x86_mov_reg_membase (code, X86_EDX, X86_ESP, 4, 4);
        /* state in eax, so it's setup as the return value */
        x86_mov_reg_membase (code, X86_EAX, X86_ESP, 8, 4);

	/* setup the copy of the stack */
	x86_mov_reg_membase (code, X86_ECX, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, stack_used_size), 4);
	x86_shift_reg_imm (code, X86_SHR, X86_ECX, 2);
	x86_cld (code);
	x86_mov_reg_membase (code, X86_ESI, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, saved_stack), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, return_sp), 4);
	x86_prefix (code, X86_REP_PREFIX);
	x86_movsl (code);

	/* now restore the registers from the LMF */
	x86_mov_reg_membase (code, X86_ECX, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, lmf), 4);
	x86_mov_reg_membase (code, X86_EBX, X86_ECX, G_STRUCT_OFFSET (MonoLMF, ebx), 4);
	x86_mov_reg_membase (code, X86_EBP, X86_ECX, G_STRUCT_OFFSET (MonoLMF, ebp), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_ECX, G_STRUCT_OFFSET (MonoLMF, esi), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_ECX, G_STRUCT_OFFSET (MonoLMF, edi), 4);

	/* restore the lmf chain */
	/*x86_mov_reg_membase (code, X86_ECX, X86_ESP, 12, 4);
	x86_mov_membase_reg (code, X86_ECX, 0, X86_EDX, 4);*/

	x86_jump_membase (code, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, return_ip));
	g_assert ((code - start) <= 48);
	saved = start;
	return (MonoContinuationRestore)saved;
}
#endif

/*
 * mono_arch_setup_resume_sighandler_ctx:
 *
 *   Setup CTX so execution continues at FUNC.
 */
void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	int align = (((gint32)MONO_CONTEXT_GET_SP (ctx)) % MONO_ARCH_FRAME_ALIGNMENT + 4);

	if (align != 0)
		MONO_CONTEXT_SET_SP (ctx, (gsize)MONO_CONTEXT_GET_SP (ctx) - align);

	MONO_CONTEXT_SET_IP (ctx, func);
}
