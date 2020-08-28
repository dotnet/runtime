/**
 * \file
 * exception support for x86
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
#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#include <mono/metadata/abi-details.h>
#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-state.h>

#include "mini.h"
#include "mini-x86.h"
#include "mini-runtime.h"
#include "tasklets.h"
#include "aot-runtime.h"
#include "mono/utils/mono-tls-inline.h"

static gpointer signal_exception_trampoline;

gpointer
mono_x86_get_signal_exception_trampoline (MonoTrampInfo **info, gboolean aot);

#ifdef TARGET_WIN32
static void (*restore_stack) (void *);

static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

LPTOP_LEVEL_EXCEPTION_FILTER mono_old_win_toplevel_exception_filter;
gpointer mono_win_vectored_exception_handle;
extern int (*gUnhandledExceptionHandler)(EXCEPTION_POINTERS*);

#ifndef PROCESS_CALLBACK_FILTER_ENABLED
#	define PROCESS_CALLBACK_FILTER_ENABLED 1
#endif

#define W32_SEH_HANDLE_EX(_ex) \
	if (_ex##_handler) _ex##_handler(er->ExceptionCode, &info, ctx)

static LONG CALLBACK seh_unhandled_exception_filter(EXCEPTION_POINTERS* ep)
{
#ifndef MONO_CROSS_COMPILE
	if (mono_old_win_toplevel_exception_filter) {
		return (*mono_old_win_toplevel_exception_filter)(ep);
	}
#endif
	if (mono_dump_start ())
		mono_handle_native_crash (mono_get_signame (SIGSEGV), NULL, NULL, NULL);

	return EXCEPTION_CONTINUE_SEARCH;
}

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

	const int size = 128;

	/* restore_contect (void *sigctx) */
	start = code = mono_global_codeman_reserve (size);

	/* load context into ebx */
	x86_mov_reg_membase (code, X86_EBX, X86_ESP, 4, 4);

	/* move current stack into edi for later restore */
	x86_mov_reg_reg (code, X86_EDI, X86_ESP);

	/* use the new freed stack from sigcontext */
	/* XXX replace usage of struct sigcontext with MonoContext so we can use MONO_STRUCT_OFFSET */
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
	x86_mov_reg_reg (code, X86_ESP, X86_EDI);

	/* return */
	x86_ret (code);

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

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
win32_handle_stack_overflow (EXCEPTION_POINTERS* ep, CONTEXT *sctx)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo rji;
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoLMF *lmf = jit_tls->lmf;		
	MonoContext initial_ctx;
	MonoContext ctx;
	guint32 free_stack = 0;
	StackFrameInfo frame;

	mono_sigctx_to_monoctx (sctx, &ctx);

	/* Let's walk the stack to recover
	 * the needed stack space (if possible)
	 */
	memset (&rji, 0, sizeof (rji));

	initial_ctx = ctx;
	free_stack = (guint8*)(MONO_CONTEXT_GET_BP (&ctx)) - (guint8*)(MONO_CONTEXT_GET_BP (&initial_ctx));

	/* try to free 64kb from our stack */
	do {
		MonoContext new_ctx;

		mono_arch_unwind_frame (domain, jit_tls, &rji, &ctx, &new_ctx, &lmf, NULL, &frame);
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

	mono_monoctx_to_sigctx (&ctx, sctx);

	/* todo: install new stack-guard page */

	/* use the new stack and call mono_arch_handle_exception () */
	restore_stack (sctx);
}

/*
 * Unhandled Exception Filter
 * Top-level per-process exception handler.
 */
static LONG CALLBACK seh_vectored_exception_handler(EXCEPTION_POINTERS* ep)
{
	EXCEPTION_RECORD* er;
	CONTEXT* ctx;
	LONG res;
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoWindowsSigHandlerInfo info = { TRUE, ep };

	/* If the thread is not managed by the runtime return early */
	if (!jit_tls)
		return EXCEPTION_CONTINUE_SEARCH;

	res = EXCEPTION_CONTINUE_EXECUTION;

	er = ep->ExceptionRecord;
	ctx = ep->ContextRecord;

	switch (er->ExceptionCode) {
	case EXCEPTION_STACK_OVERFLOW:
		win32_handle_stack_overflow (ep, ctx);
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
		info.handled = FALSE;
		break;
	}

	if (!info.handled) {
		/* Don't copy context back if we chained exception
		* as the handler may have modfied the EXCEPTION_POINTERS
		* directly. We don't pass sigcontext to chained handlers.
		* Return continue search so the UnhandledExceptionFilter
		* can correctly chain the exception.
		*/
		res = EXCEPTION_CONTINUE_SEARCH;
	}

	return res;
}

void win32_seh_init()
{
	/* install restore stack helper */
	if (!restore_stack)
		restore_stack = (void (*) (void*))mono_win32_get_handle_stackoverflow ();

	mono_old_win_toplevel_exception_filter = SetUnhandledExceptionFilter(seh_unhandled_exception_filter);
	mono_win_vectored_exception_handle = AddVectoredExceptionHandler (1, seh_vectored_exception_handler);
}

void win32_seh_cleanup()
{
	if (mono_old_win_toplevel_exception_filter)
		SetUnhandledExceptionFilter(mono_old_win_toplevel_exception_filter);
	RemoveVectoredExceptionHandler (mono_win_vectored_exception_handle);
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

	const int size = 128;

	start = code = mono_global_codeman_reserve (size);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, ebx), 4);

	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, edi), 4);

	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, esi), 4);

	/* restore EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, edx), 4);

	/*
	 * The context resides on the stack, in the stack frame of the
	 * caller of this function.  The stack pointer that we need to
	 * restore is potentially many stack frames higher up, so the
	 * distance between them can easily be more than the red zone
	 * size.  Hence the stack pointer can be restored only after
	 * we have finished loading everything from the context.
	 */

	/* load ESP into EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, esp), 4);
	/* load return address into ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, eip), 4);
	/* save the return addr to the restored stack - 4 */
	x86_mov_membase_reg (code, X86_EBP, -4, X86_ECX, 4);

	/* load EBP into ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, ebp), 4);
	/* save EBP to the restored stack - 8 */
	x86_mov_membase_reg (code, X86_EBP, -8, X86_ECX, 4);

	/* load EAX into ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, eax), 4);
	/* save EAX to the restored stack - 12 */
	x86_mov_membase_reg (code, X86_EBP, -12, X86_ECX, 4);

	/* restore ECX */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, ecx), 4);

	/* restore ESP - 12 */
	x86_lea_membase (code, X86_ESP, X86_EBP, -12);
	/* restore EAX */
	x86_pop_reg (code, X86_EAX);
	/* restore EBP */
	x86_pop_reg (code, X86_EBP);
	/* jump to the saved IP */
	x86_ret (code);

	if (info)
		*info = mono_tramp_info_create ("restore_context", start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

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
	const int kMaxCodeSize = 64;

	/* call_filter (MonoContext *ctx, unsigned long eip) */
	start = code = mono_global_codeman_reserve (kMaxCodeSize);

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP);
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
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, ebp), 4);
	/* restore registers used by global register allocation (EBX & ESI) */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, ebx), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, esi), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  MONO_STRUCT_OFFSET (MonoContext, edi), 4);

	/* align stack and save ESP */
	x86_mov_reg_reg (code, X86_EDX, X86_ESP);
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

	if (info)
		*info = mono_tramp_info_create ("call_filter", start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	g_assertf ((code - start) <= kMaxCodeSize, "%d %d", (int)(code - start), kMaxCodeSize);
	return start;
}

/*
 * mono_x86_throw_exception:
 *
 *   C function called from the throw trampolines.
 */
void
mono_x86_throw_exception (host_mgreg_t *regs, MonoObject *exc,
						  host_mgreg_t eip, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
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

#ifdef __APPLE__
	/* The OSX ABI specifies 16 byte alignment at call sites */
	g_assert ((ctx.esp % MONO_ARCH_FRAME_ALIGNMENT) == 0);
#endif

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

	/* adjust eip so that it point into the call instruction */
	ctx.eip -= 1;

	mono_handle_exception (&ctx, exc);

	mono_restore_context (&ctx);

	g_assert_not_reached ();
}

void
mono_x86_throw_corlib_exception (host_mgreg_t *regs, guint32 ex_token_index, 
								 host_mgreg_t eip, gint32 pc_offset)
{
	guint32 ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
	MonoException *ex;

	ex = mono_exception_from_token (m_class_get_image (mono_defaults.exception_class), ex_token);

	eip -= pc_offset;

	/* Negate the ip adjustment done in mono_x86_throw_exception () */
	eip += 1;

	mono_x86_throw_exception (regs, (MonoObject*)ex, eip, FALSE, FALSE);
}

static void
mono_x86_resume_unwind (host_mgreg_t *regs, MonoObject *exc, 
						host_mgreg_t eip, gboolean rethrow)
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
get_throw_trampoline (const char *name, gboolean rethrow, gboolean llvm, gboolean corlib, gboolean llvm_abs, gboolean resume_unwind, MonoTrampInfo **info, gboolean aot, gboolean preserve_ips)
{
	guint8 *start, *code, *labels [16];
	int i, stack_size, stack_offset, arg_offsets [5], regs_offset;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	const int kMaxCodeSize = 192;

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

	unwind_ops = mono_arch_get_cie_program ();

	/* Alloc frame */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, stack_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, stack_size + 4);

	arg_offsets [0] = 0;
	arg_offsets [1] = 4;
	arg_offsets [2] = 8;
	arg_offsets [3] = 12;
	arg_offsets [4] = 16;
	regs_offset = 20;

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

	/* Clear fp stack */
	labels [0] = code;
	x86_fnstsw (code);
	x86_shift_reg_imm (code, X86_SHR, X86_EAX, 11);
	x86_alu_reg_imm (code, X86_AND, X86_EAX, 7);
	x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0);
	labels [1] = code;
	x86_branch8 (code, X86_CC_EQ, 0, FALSE);
	x86_fstp (code, 0);
	x86_jump_code (code, labels [0]);
	mono_x86_patch (labels [1], code);

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

		/* Set arg4 == preserve_ips */
		x86_mov_membase_imm (code, X86_ESP, arg_offsets [4], preserve_ips, 4);
	}
	/* Make the call */
	if (aot) {
		// This can be called from runtime code, which can't guarantee that
		// ebx contains the got address.
		// So emit the got address loading code too
		code = mono_arch_emit_load_got_addr (start, code, NULL, &ji);
		code = mono_arch_emit_load_aotconst (start, code, &ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (corlib ? MONO_JIT_ICALL_mono_x86_throw_corlib_exception : MONO_JIT_ICALL_mono_x86_throw_exception));
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, resume_unwind ? (gpointer)(mono_x86_resume_unwind) : (corlib ? (gpointer)mono_x86_throw_corlib_exception : (gpointer)mono_x86_throw_exception));
	}
	x86_breakpoint (code);

	g_assertf ((code - start) <= kMaxCodeSize, "%d %d", (int)(code - start), kMaxCodeSize);

	if (info)
		*info = mono_tramp_info_create (name, start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	return start;
}

/**
 * mono_arch_get_throw_exception:
 * \returns a function pointer which can be used to raise 
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
	return get_throw_trampoline ("throw_exception", FALSE, FALSE, FALSE, FALSE, FALSE, info, aot, FALSE);
}

gpointer 
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("rethrow_exception", TRUE, FALSE, FALSE, FALSE, FALSE, info, aot, FALSE);
}

gpointer 
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("rethrow_preserve_exception", TRUE, FALSE, FALSE, FALSE, FALSE, info, aot, TRUE);
}

/**
 * mono_arch_get_throw_corlib_exception:
 * \returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 */
gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline ("throw_corlib_exception", FALSE, FALSE, TRUE, FALSE, FALSE, info, aot, FALSE);
}

void
mono_arch_exceptions_init (void)
{
	guint8 *tramp;
	MonoTrampInfo *tinfo;

/* 
 * If we're running WoW64, we need to set the usermode exception policy 
 * for SEHs to behave. This requires hotfix http://support.microsoft.com/kb/976038
 * or (eventually) Windows 7 SP1.
 */
#ifdef TARGET_WIN32
	HMODULE kernel32 = LoadLibraryW (L"kernel32.dll");
	if (kernel32) {
		typedef BOOL (WINAPI * SetProcessUserModeExceptionPolicy_t) (DWORD dwFlags);
		typedef BOOL (WINAPI * GetProcessUserModeExceptionPolicy_t) (PDWORD dwFlags);
		GetProcessUserModeExceptionPolicy_t const getter = (GetProcessUserModeExceptionPolicy_t)GetProcAddress (kernel32, "GetProcessUserModeExceptionPolicy");
		if (getter) {
			SetProcessUserModeExceptionPolicy_t const setter = (SetProcessUserModeExceptionPolicy_t)GetProcAddress (kernel32, "SetProcessUserModeExceptionPolicy");
			if (setter) {
				DWORD flags = 0;
				if (getter (&flags))
					setter (flags & ~PROCESS_CALLBACK_FILTER_ENABLED);
			}
		}
	}
#endif

	if (mono_aot_only) {
		signal_exception_trampoline = mono_aot_get_trampoline ("x86_signal_exception_trampoline");
		return;
	}

	// FIXME Macroize.

	/* LLVM needs different throw trampolines */
	tramp = get_throw_trampoline ("llvm_throw_exception_trampoline", FALSE, TRUE, FALSE, FALSE, FALSE, &tinfo, FALSE,  FALSE);
	mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_exception_trampoline, tramp, "llvm_throw_exception_trampoline", NULL, TRUE, NULL);
	mono_tramp_info_register (tinfo, NULL);

	tramp = get_throw_trampoline ("llvm_rethrow_exception_trampoline", TRUE, TRUE, FALSE, FALSE, FALSE, &tinfo, FALSE, FALSE);
	mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_rethrow_exception_trampoline, tramp, "llvm_rethrow_exception_trampoline", NULL, TRUE, NULL);
	mono_tramp_info_register (tinfo, NULL);

	tramp = get_throw_trampoline ("llvm_throw_corlib_exception_trampoline", FALSE, TRUE, TRUE, FALSE, FALSE, &tinfo, FALSE, FALSE);
	mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline, tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE, NULL);
	mono_tramp_info_register (tinfo, NULL);

	tramp = get_throw_trampoline ("llvm_throw_corlib_exception_abs_trampoline", FALSE, TRUE, TRUE, TRUE, FALSE, &tinfo, FALSE, FALSE);
	mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline, tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE, NULL);
	mono_tramp_info_register (tinfo, NULL);

	tramp = get_throw_trampoline ("llvm_resume_unwind_trampoline", FALSE, FALSE, FALSE, FALSE, TRUE, &tinfo, FALSE, FALSE);
	mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline, tramp, "llvm_resume_unwind_trampoline", NULL, TRUE, NULL);
	mono_tramp_info_register (tinfo, NULL);

	signal_exception_trampoline = mono_x86_get_signal_exception_trampoline (&tinfo, FALSE);
	mono_tramp_info_register (tinfo, NULL);
}

/*
 * mono_arch_unwind_frame:
 *
 * See exceptions-amd64.c for docs.
 */
gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		host_mgreg_t regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		regs [X86_EAX] = new_ctx->eax;
		regs [X86_EBX] = new_ctx->ebx;
		regs [X86_ECX] = new_ctx->ecx;
		regs [X86_EDX] = new_ctx->edx;
		regs [X86_ESP] = new_ctx->esp;
		regs [X86_EBP] = new_ctx->ebp;
		regs [X86_ESI] = new_ctx->esi;
		regs [X86_EDI] = new_ctx->edi;
		regs [X86_NREG] = new_ctx->eip;

		gboolean success = mono_unwind_frame ((guint8*)unwind_info, unwind_info_len, (guint8*)ji->code_start,
						   (guint8*)ji->code_start + ji->code_size,
						   (guint8*)ip, NULL, regs, MONO_MAX_IREGS + 1,
						   save_locations, MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

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

		return TRUE;
	} else if (*lmf) {
		g_assert ((((gsize)(*lmf)->previous_lmf) & 2) == 0);

		if ((ji = mini_jit_info_table_find (domain, (gpointer)(uintptr_t)(*lmf)->eip, NULL))) {
			frame->ji = ji;
		} else {
			if (!(*lmf)->method)
				return FALSE;
			frame->method = (*lmf)->method;
		}

		new_ctx->esi = (*lmf)->esi;
		new_ctx->edi = (*lmf)->edi;
		new_ctx->ebx = (*lmf)->ebx;
		new_ctx->ebp = (*lmf)->ebp;
		new_ctx->eip = (*lmf)->eip;

		/* Adjust IP */
		new_ctx->eip --;

		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		/* Check if we are in a trampoline LMF frame */
		if ((gsize)((*lmf)->previous_lmf) & 1) {
			/* lmf->esp is set by the trampoline code */
			new_ctx->esp = (*lmf)->esp;
		}
		else
			/* the lmf is always stored on the stack, so the following
			 * expression points to a stack location which can be used as ESP */
			new_ctx->esp = (gsize)&((*lmf)->eip);

		*lmf = (MonoLMF*)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#if defined(HOST_WATCHOS)
	printf("WARNING: mono_arch_ip_from_context() called!\n");
	return (NULL);
#elif defined(MONO_CROSS_COMPILE)
	g_assert_not_reached ();
	return NULL;
#elif defined(MONO_ARCH_USE_SIGACTION)
	ucontext_t *ctx = (ucontext_t*)sigctx;
	return (gpointer)UCONTEXT_REG_EIP (ctx);
#elif defined(HOST_WIN32)
	return (gpointer)((CONTEXT*)sigctx)->Eip;
#else
	struct sigcontext *ctx = sigctx;
	return (gpointer)ctx->SC_EIP;
#endif
}

/*
 * handle_exception:
 *
 *   Called by resuming from a signal handler.
 */
static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx;

	memcpy (&ctx, &jit_tls->ex_ctx, sizeof (MonoContext));

	mono_handle_exception (&ctx, (MonoObject*)obj);

	mono_restore_context (&ctx);
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

	const int size = 128;

	start = code = mono_global_codeman_reserve (size);

	/* FIXME no unwind before we push ip */
	/* Caller ip */
	x86_push_reg (code, X86_ECX);

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, X86_ESP, 4);
	mono_add_unwind_op_offset (unwind_ops, code, start, X86_NREG, -4);

	/* Fix the alignment to be what apple expects */
	stack_size = 12;

	x86_alu_reg_imm (code, X86_SUB, X86_ESP, stack_size);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, stack_size + 4);

	/* Arg1 */
	x86_mov_membase_reg (code, X86_ESP, 0, X86_EAX, 4);
	/* Branch to target */
	x86_call_reg (code, X86_EDX);

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	if (info)
		*info = mono_tramp_info_create ("x86_signal_exception_trampoline", start, code - start, ji, unwind_ops);
	else {
		GSList *l;

		for (l = unwind_ops; l; l = l->next)
			g_free (l->data);
		g_slist_free (unwind_ops);
	}

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

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
	ctx->eax = (host_mgreg_t)(gsize)user_data;
	ctx->ecx = ctx->eip;
	ctx->edx = (host_mgreg_t)(gsize)async_cb;

	/*align the stack*/
	ctx->esp = (ctx->esp - 16) & ~15;
	ctx->eip = (host_mgreg_t)(gsize)signal_exception_trampoline;
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
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (ctx, &jit_tls->ex_ctx);

	mctx = jit_tls->ex_ctx;
	mono_setup_async_callback (&mctx, handle_signal_exception, obj);
	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#elif defined (TARGET_WIN32)
	MonoContext mctx;
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);

	mctx = jit_tls->ex_ctx;
	mono_setup_async_callback (&mctx, handle_signal_exception, obj);
	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#else
	MonoContext mctx;

	mono_sigctx_to_monoctx (sigctx, &mctx);

	mono_handle_exception (&mctx, obj);

	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#endif
}

#if defined (MONO_ARCH_USE_SIGACTION) && !defined (MONO_CROSS_COMPILE)
static MonoObject*
restore_soft_guard_pages (void)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	if (jit_tls->stack_ovf_guard_base)
		mono_mprotect (jit_tls->stack_ovf_guard_base, jit_tls->stack_ovf_guard_size, MONO_MMAP_NONE);

	if (jit_tls->stack_ovf_pending) {
		MonoDomain *domain = mono_domain_get ();
		jit_tls->stack_ovf_pending = 0;
		return (MonoObject *) domain->stack_overflow_ex;
	}
	return NULL;
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
	sp = (gpointer*)(mctx->esp);
	sp -= 1;
	/* the return addr */
	sp [0] = (gpointer)(mctx->eip);
	mctx->eip = (gsize)restore_soft_guard_pages;
	mctx->esp = (gsize)sp;
}

static void
altstack_handle_and_restore (MonoContext *ctx, gpointer obj, gboolean stack_ovf)
{
	MonoContext mctx;

	mctx = *ctx;

	mono_handle_exception (&mctx, (MonoObject*)obj);
	if (stack_ovf) {
		MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
		jit_tls->stack_ovf_pending = 1;
		prepare_for_guard_pages (&mctx);
	}
	mono_restore_context (&mctx);
}
#endif

void
mono_arch_handle_altstack_exception (void *sigctx, MONO_SIG_HANDLER_INFO_TYPE *siginfo, gpointer fault_addr, gboolean stack_ovf)
{
#if defined (MONO_ARCH_USE_SIGACTION) && !defined (MONO_CROSS_COMPILE)
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
		glong *sp = (glong*)UCONTEXT_REG_ESP (ctx);
		ji = mini_jit_info_table_find (mono_domain_get (), (gpointer)sp [0], NULL);
		if (ji)
			UCONTEXT_REG_EIP (ctx) = sp [0];
	}
	if (stack_ovf)
		exc = mono_domain_get ()->stack_overflow_ex;
	if (!ji) {
		MonoContext mctx;
		mono_sigctx_to_monoctx (sigctx, &mctx);
		if (mono_dump_start ())
			mono_handle_native_crash (mono_get_signame (SIGSEGV), &mctx, siginfo, sigctx);
		else
			abort ();
	}
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
	sp = (gpointer*)(UCONTEXT_REG_ESP (ctx) & ~15);
	sp = (gpointer*)((char*)sp - frame_size);
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

#if MONO_SUPPORT_TASKLETS && !defined(ENABLE_NETCORE)
MonoContinuationRestore
mono_tasklets_arch_restore (void)
{
	static guint8* saved = NULL;
	guint8 *code, *start;

	if (saved)
		return (MonoContinuationRestore)saved;

	const int size = 48;

	code = start = mono_global_codeman_reserve (size);
	/* the signature is: restore (MonoContinuation *cont, int state, MonoLMF **lmf_addr) */
	/* put cont in edx */
	x86_mov_reg_membase (code, X86_EDX, X86_ESP, 4, 4);
	/* state in eax, so it's setup as the return value */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 8, 4);
	/* lmf_addr in ebx */
	x86_mov_reg_membase(code, X86_EBX, X86_ESP, 0x0C, 4);

	/* setup the copy of the stack */
	x86_mov_reg_membase (code, X86_ECX, X86_EDX, MONO_STRUCT_OFFSET (MonoContinuation, stack_used_size), 4);
	x86_shift_reg_imm (code, X86_SHR, X86_ECX, 2);
	x86_cld (code);
	x86_mov_reg_membase (code, X86_ESI, X86_EDX, MONO_STRUCT_OFFSET (MonoContinuation, saved_stack), 4);
	x86_mov_reg_membase (code, X86_EDI, X86_EDX, MONO_STRUCT_OFFSET (MonoContinuation, return_sp), 4);
	x86_prefix (code, X86_REP_PREFIX);
	x86_movsl (code);

	/* now restore the registers from the LMF */
	x86_mov_reg_membase (code, X86_ECX, X86_EDX, MONO_STRUCT_OFFSET (MonoContinuation, lmf), 4);
	x86_mov_reg_membase (code, X86_EBP, X86_ECX, MONO_STRUCT_OFFSET (MonoLMF, ebp), 4);
	x86_mov_reg_membase (code, X86_ESP, X86_ECX, MONO_STRUCT_OFFSET (MonoLMF, esp), 4);

	/* restore the lmf chain */
	/*x86_mov_reg_membase (code, X86_ECX, X86_ESP, 12, 4);
	x86_mov_membase_reg (code, X86_ECX, 0, X86_EDX, 4);*/

	x86_jump_membase (code, X86_EDX, MONO_STRUCT_OFFSET (MonoContinuation, return_ip));

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));
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
	gssize align = (((gssize)MONO_CONTEXT_GET_SP (ctx)) % MONO_ARCH_FRAME_ALIGNMENT + 4);

	if (align != 0)
		MONO_CONTEXT_SET_SP (ctx, (gsize)MONO_CONTEXT_GET_SP (ctx) - align);

	MONO_CONTEXT_SET_IP (ctx, func);
}

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
	ctx->eip++;
}

void
mono_arch_do_ip_adjustment (MonoContext *ctx)
{
	ctx->eip--;
}
