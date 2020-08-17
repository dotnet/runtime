/**
 * \file
 * exception support for AMD64
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Johan Lorensson (lateralusx.github@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

// Secret password to unlock wcscat_s on mxe, must happen before string.h included
#ifdef __MINGW32__
#define MINGW_HAS_SECURE_API 1
#endif

#include <glib.h>
#include <string.h>
#include <signal.h>
#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-state.h>

#include "mini.h"
#include "mini-amd64.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "tasklets.h"
#include "mono/utils/mono-tls-inline.h"

#ifdef TARGET_WIN32
static void (*restore_stack) (void);
static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

LPTOP_LEVEL_EXCEPTION_FILTER mono_old_win_toplevel_exception_filter;
void *mono_win_vectored_exception_handle;

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

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static gpointer
get_win32_restore_stack (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	const int size = 128;

	/* restore_stack (void) */
	start = code = mono_global_codeman_reserve (size);

	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);

	/* push 32 bytes of stack space for Win64 calling convention */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 32);

	/* restore guard page */
	amd64_mov_reg_imm (code, AMD64_R11, _resetstkoflw);
	amd64_call_reg (code, AMD64_R11);

	/* get jit_tls with context to restore */
	amd64_mov_reg_imm (code, AMD64_R11, mono_tls_get_jit_tls_extern);
	amd64_call_reg (code, AMD64_R11);

	/* move jit_tls from return reg to arg reg */
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RAX, 8);

	/* retrieve pointer to saved context */
	amd64_alu_reg_imm (code, X86_ADD, AMD64_ARG_REG1, MONO_STRUCT_OFFSET (MonoJitTlsData, stack_restore_ctx));

	/* this call does not return */
	amd64_mov_reg_imm (code, AMD64_R11, mono_restore_context);
	amd64_call_reg (code, AMD64_R11);

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	return start;
}
#else
static gpointer
get_win32_restore_stack (void)
{
	// _resetstkoflw unsupported on none desktop Windows platforms.
	return NULL;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

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
	MonoDomain* domain = mono_domain_get ();
	MonoWindowsSigHandlerInfo info = { TRUE, ep };

	/* If the thread is not managed by the runtime return early */
	if (!jit_tls)
		return EXCEPTION_CONTINUE_SEARCH;

	res = EXCEPTION_CONTINUE_EXECUTION;

	er = ep->ExceptionRecord;
	ctx = ep->ContextRecord;

	switch (er->ExceptionCode) {
	case EXCEPTION_STACK_OVERFLOW:
		if (!mono_aot_only && restore_stack) {
			if (mono_arch_handle_exception (ctx, domain->stack_overflow_ex)) {
				/* need to restore stack protection once stack is unwound
				 * restore_stack will restore stack protection and then
				 * resume control to the saved stack_restore_ctx */
				mono_sigctx_to_monoctx (ctx, &jit_tls->stack_restore_ctx);
				ctx->Rip = (guint64)restore_stack;
			}
		} else {
			info.handled = FALSE;
		}
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
	if (!mono_aot_only)
		restore_stack = (void (*) (void))get_win32_restore_stack ();

	mono_old_win_toplevel_exception_filter = SetUnhandledExceptionFilter(seh_unhandled_exception_filter);
	mono_win_vectored_exception_handle = AddVectoredExceptionHandler (1, seh_vectored_exception_handler);
}

void win32_seh_cleanup()
{
	guint32 ret = 0;

	if (mono_old_win_toplevel_exception_filter) SetUnhandledExceptionFilter(mono_old_win_toplevel_exception_filter);

	ret = RemoveVectoredExceptionHandler (mono_win_vectored_exception_handle);
	g_assert (ret);
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

#ifndef DISABLE_JIT
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
	int i, gregs_offset;

	/* restore_contect (MonoContext *ctx) */

	const int size = 256;

	start = code = (guint8 *)mono_global_codeman_reserve (size);

	amd64_mov_reg_reg (code, AMD64_R11, AMD64_ARG_REG1, 8);

	/* Restore all registers except %rip and %r11 */
	gregs_offset = MONO_STRUCT_OFFSET (MonoContext, gregs);
	for (i = 0; i < AMD64_NREG; ++i) {
		if (i != AMD64_RIP && i != AMD64_RSP && i != AMD64_R8 && i != AMD64_R9 && i != AMD64_R10 && i != AMD64_R11)
			amd64_mov_reg_membase (code, i, AMD64_R11, gregs_offset + (i * 8), 8);
	}

	/*
	 * The context resides on the stack, in the stack frame of the
	 * caller of this function.  The stack pointer that we need to
	 * restore is potentially many stack frames higher up, so the
	 * distance between them can easily be more than the red zone
	 * size.  Hence the stack pointer can be restored only after
	 * we have finished loading everything from the context.
	 */
	amd64_mov_reg_membase (code, AMD64_R8, AMD64_R11,  gregs_offset + (AMD64_RSP * 8), 8);
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11,  gregs_offset + (AMD64_RIP * 8), 8);
	amd64_mov_reg_reg (code, AMD64_RSP, AMD64_R8, 8);

	/* jump to the saved IP */
	amd64_jump_reg (code, AMD64_R11);

	g_assertf ((code - start) <= size, "%d %d", (int)(code - start), size);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("restore_context", start, code - start, ji, unwind_ops);

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
	guint8 *start;
	int i, gregs_offset;
	guint8 *code;
	guint32 pos;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	const int kMaxCodeSize = 128;

	start = code = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize);

	/* call_filter (MonoContext *ctx, unsigned long eip) */
	code = start;

	/* Alloc new frame */
	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);

	/* Save callee saved regs */
	pos = 0;
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_CALLEE_SAVED_REG (i)) {
			amd64_push_reg (code, i);
			pos += 8;
		}

	/* Save EBP */
	pos += 8;
	amd64_push_reg (code, AMD64_RBP);

	/* Make stack misaligned, the call will make it aligned again */
	if (! (pos & 8))
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);

	gregs_offset = MONO_STRUCT_OFFSET (MonoContext, gregs);

	/* set new EBP */
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_ARG_REG1, gregs_offset + (AMD64_RBP * 8), 8);
	/* load callee saved regs */
	for (i = 0; i < AMD64_NREG; ++i) {
		if (AMD64_IS_CALLEE_SAVED_REG (i) && i != AMD64_RBP)
			amd64_mov_reg_membase (code, i, AMD64_ARG_REG1, gregs_offset + (i * 8), 8);
	}
	/* load exc register */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_ARG_REG1,  gregs_offset + (AMD64_RAX * 8), 8);

	/* call the handler */
	amd64_call_reg (code, AMD64_ARG_REG2);

	if (! (pos & 8))
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);

	/* restore RBP */
	amd64_pop_reg (code, AMD64_RBP);

	/* Restore callee saved regs */
	for (i = AMD64_NREG; i >= 0; --i)
		if (AMD64_IS_CALLEE_SAVED_REG (i))
			amd64_pop_reg (code, i);

#if TARGET_WIN32
	amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, 0);
	amd64_pop_reg (code, AMD64_RBP);
#else
	amd64_leave (code);
#endif
	amd64_ret (code);

	g_assertf ((code - start) <= kMaxCodeSize, "%d %d", (int)(code - start), kMaxCodeSize);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("call_filter", start, code - start, ji, unwind_ops);

	return start;
}
#endif /* !DISABLE_JIT */

/* 
 * The first few arguments are dummy, to force the other arguments to be passed on
 * the stack, this avoids overwriting the argument registers in the throw trampoline.
 */
void
mono_amd64_throw_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
							guint64 dummy5, guint64 dummy6,
							MonoContext *mctx, MonoObject *exc, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;

	/* mctx is on the caller's stack */
	memcpy (&ctx, mctx, sizeof (MonoContext));

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
	ctx.gregs [AMD64_RIP] --;

	mono_handle_exception (&ctx, exc);
	mono_restore_context (&ctx);
	g_assert_not_reached ();
}

void
mono_amd64_throw_corlib_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
								   guint64 dummy5, guint64 dummy6,
								   MonoContext *mctx, guint32 ex_token_index, gint64 pc_offset)
{
	guint32 ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
	MonoException *ex;

	ex = mono_exception_from_token (m_class_get_image (mono_defaults.exception_class), ex_token);

	mctx->gregs [AMD64_RIP] -= pc_offset;

	/* Negate the ip adjustment done in mono_amd64_throw_exception () */
	mctx->gregs [AMD64_RIP] += 1;

	mono_amd64_throw_exception (dummy1, dummy2, dummy3, dummy4, dummy5, dummy6, mctx, (MonoObject*)ex, FALSE, FALSE);
}

void
mono_amd64_resume_unwind (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
						  guint64 dummy5, guint64 dummy6,
						  MonoContext *mctx, guint32 dummy7, gint64 dummy8)
{
	/* Only the register parameters are valid */
	MonoContext ctx;

	/* mctx is on the caller's stack */
	memcpy (&ctx, mctx, sizeof (MonoContext));

	mono_resume_unwind (&ctx);
}

#ifndef DISABLE_JIT
/*
 * get_throw_trampoline:
 *
 *  Generate a call to mono_amd64_throw_exception/
 * mono_amd64_throw_corlib_exception.
 */
static gpointer
get_throw_trampoline (MonoTrampInfo **info, gboolean rethrow, gboolean corlib, gboolean llvm_abs, gboolean resume_unwind, const char *tramp_name, gboolean aot, gboolean preserve_ips)
{
	guint8* start;
	guint8 *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int i, stack_size, arg_offsets [16], ctx_offset, regs_offset;
	const int kMaxCodeSize = 256;

#ifdef TARGET_WIN32
	const int dummy_stack_space = 6 * sizeof (target_mgreg_t);	/* Windows expects stack space allocated for all 6 dummy args. */
#else
	const int dummy_stack_space = 0;
#endif

	if (info)
		start = code = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize + MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE);
	else
		start = code = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize);

	/* The stack is unaligned on entry */
	stack_size = ALIGN_TO (sizeof (MonoContext) + 64 + dummy_stack_space, MONO_ARCH_FRAME_ALIGNMENT) + 8;

	code = start;

	if (info)
		unwind_ops = mono_arch_get_cie_program ();

	/* Alloc frame */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, stack_size);
	if (info) {
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, stack_size + 8);
		mono_add_unwind_op_sp_alloc (unwind_ops, code, start, stack_size);
	}

	/*
	 * To hide linux/windows calling convention differences, we pass all arguments on
	 * the stack by passing 6 dummy values in registers.
	 */

	arg_offsets [0] = dummy_stack_space + 0;
	arg_offsets [1] = dummy_stack_space + sizeof (target_mgreg_t);
	arg_offsets [2] = dummy_stack_space + sizeof (target_mgreg_t) * 2;
	arg_offsets [3] = dummy_stack_space + sizeof (target_mgreg_t) * 3;
	ctx_offset = dummy_stack_space + sizeof (target_mgreg_t) * 4;
	regs_offset = ctx_offset + MONO_STRUCT_OFFSET (MonoContext, gregs);

	/* Save registers */
	for (i = 0; i < AMD64_NREG; ++i)
		if (i != AMD64_RSP)
			amd64_mov_membase_reg (code, AMD64_RSP, regs_offset + (i * sizeof (target_mgreg_t)), i, sizeof (target_mgreg_t));
	/* Save RSP */
	amd64_lea_membase (code, AMD64_RAX, AMD64_RSP, stack_size + sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, regs_offset + (AMD64_RSP * sizeof (target_mgreg_t)), X86_EAX, sizeof (target_mgreg_t));
	/* Save IP */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, stack_size, sizeof (target_mgreg_t));
	amd64_mov_membase_reg (code, AMD64_RSP, regs_offset + (AMD64_RIP * sizeof (target_mgreg_t)), AMD64_RAX, sizeof (target_mgreg_t));
	/* Set arg1 == ctx */
	amd64_lea_membase (code, AMD64_RAX, AMD64_RSP, ctx_offset);
	amd64_mov_membase_reg (code, AMD64_RSP, arg_offsets [0], AMD64_RAX, sizeof (target_mgreg_t));
	/* Set arg2 == exc/ex_token_index */
	if (resume_unwind)
		amd64_mov_membase_imm (code, AMD64_RSP, arg_offsets [1], 0, sizeof (target_mgreg_t));
	else
		amd64_mov_membase_reg (code, AMD64_RSP, arg_offsets [1], AMD64_ARG_REG1, sizeof (target_mgreg_t));
	/* Set arg3 == rethrow/pc offset */
	if (resume_unwind) {
		amd64_mov_membase_imm (code, AMD64_RSP, arg_offsets [2], 0, sizeof (target_mgreg_t));
	} else if (corlib) {
		if (llvm_abs)
			/*
			 * The caller doesn't pass in a pc/pc offset, instead we simply use the
			 * caller ip. Negate the pc adjustment done in mono_amd64_throw_corlib_exception ().
			 */
			amd64_mov_membase_imm (code, AMD64_RSP, arg_offsets [2], 1, sizeof  (target_mgreg_t));
		else
			amd64_mov_membase_reg (code, AMD64_RSP, arg_offsets [2], AMD64_ARG_REG2, sizeof (target_mgreg_t));
	} else {
		amd64_mov_membase_imm (code, AMD64_RSP, arg_offsets [2], rethrow, sizeof (target_mgreg_t));

		/* Set arg4 == preserve_ips */
		amd64_mov_membase_imm (code, AMD64_RSP, arg_offsets [3], preserve_ips, sizeof (target_mgreg_t));
	}

	if (aot) {
		MonoJitICallId icall_id;

		if (resume_unwind)
			icall_id = MONO_JIT_ICALL_mono_amd64_resume_unwind;
		else if (corlib)
			icall_id = MONO_JIT_ICALL_mono_amd64_throw_corlib_exception;
		else
			icall_id = MONO_JIT_ICALL_mono_amd64_throw_exception;
		ji = mono_patch_info_list_prepend (ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (icall_id));
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, resume_unwind ? ((gpointer)mono_amd64_resume_unwind) : (corlib ? (gpointer)mono_amd64_throw_corlib_exception : (gpointer)mono_amd64_throw_exception));
	}
	amd64_call_reg (code, AMD64_R11);
	amd64_breakpoint (code);

	mono_arch_flush_icache (start, code - start);

	g_assertf ((code - start) <= kMaxCodeSize, "%d %d", (int)(code - start), kMaxCodeSize);
	g_assert_checked (mono_arch_unwindinfo_validate_size (unwind_ops, MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE));

	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create (tramp_name, start, code - start, ji, unwind_ops);

	return start;
}

/**
 * mono_arch_get_throw_exception:
 * \returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 */
gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (info, FALSE, FALSE, FALSE, FALSE, "throw_exception", aot, FALSE);
}

gpointer 
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (info, TRUE, FALSE, FALSE, FALSE, "rethrow_exception", aot, FALSE);
}

gpointer 
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (info, TRUE, FALSE, FALSE, FALSE, "rethrow_preserve_exception", aot, TRUE);
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
	return get_throw_trampoline (info, FALSE, TRUE, FALSE, FALSE, "throw_corlib_exception", aot, FALSE);
}
#endif /* !DISABLE_JIT */

/*
 * mono_arch_unwind_frame:
 *
 * This function is used to gather information from @ctx, and store it in @frame_info.
 * It unwinds one stack frame, and stores the resulting context into @new_ctx. @lmf
 * is modified if needed.
 * Returns TRUE on success, FALSE otherwise.
 */
gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	int i;

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		host_mgreg_t regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;
		guint8 *epilog = NULL;

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		frame->unwind_info = unwind_info;
		frame->unwind_info_len = unwind_info_len;

		/*
		printf ("%s %p %p\n", ji->d.method->name, ji->code_start, ip);
		mono_print_unwind_info (unwind_info, unwind_info_len);
		*/
		/* LLVM compiled code doesn't have this info */
		if (ji->has_arch_eh_info)
			epilog = (guint8*)ji->code_start + ji->code_size - mono_jinfo_get_epilog_size (ji);
 
		for (i = 0; i < AMD64_NREG; ++i)
			regs [i] = new_ctx->gregs [i];

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, (guint8 *)ji->code_start,
						   (guint8*)ji->code_start + ji->code_size,
						   (guint8 *)ip, epilog ? &epilog : NULL, regs, MONO_MAX_IREGS + 1,
						   save_locations, MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

		for (i = 0; i < AMD64_NREG; ++i)
			new_ctx->gregs [i] = regs [i];
 
		/* The CFA becomes the new SP value */
		new_ctx->gregs [AMD64_RSP] = (host_mgreg_t)(gsize)cfa;

		/* Adjust IP */
		new_ctx->gregs [AMD64_RIP] --;

		return TRUE;
	} else if (*lmf) {
		guint64 rip;

		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		if (((guint64)(*lmf)->previous_lmf) & 4) {
			MonoLMFTramp *ext = (MonoLMFTramp*)(*lmf);

			rip = (guint64)MONO_CONTEXT_GET_IP (ext->ctx);
		} else if ((*lmf)->rsp == 0) {
			/* Top LMF entry */
			return FALSE;
		} else {
			/* 
			 * The rsp field is set just before the call which transitioned to native 
			 * code. Obtain the rip from the stack.
			 */
			rip = *(guint64*)((*lmf)->rsp - sizeof(host_mgreg_t));
		}

		ji = mini_jit_info_table_find (domain, (char *)rip, NULL);
		/*
		 * FIXME: ji == NULL can happen when a managed-to-native wrapper is interrupted
		 * in the soft debugger suspend code, since (*lmf)->rsp no longer points to the
		 * return address.
		 */
		//g_assert (ji);
		if (!ji)
			return FALSE;

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		if (((guint64)(*lmf)->previous_lmf) & 4) {
			MonoLMFTramp *ext = (MonoLMFTramp*)(*lmf);

			/* Trampoline frame */
			for (i = 0; i < AMD64_NREG; ++i)
				new_ctx->gregs [i] = ext->ctx->gregs [i];
			/* Adjust IP */
			new_ctx->gregs [AMD64_RIP] --;
		} else {
			/*
			 * The registers saved in the LMF will be restored using the normal unwind info,
			 * when the wrapper frame is processed.
			 */
			/* Adjust IP */
			rip --;
			new_ctx->gregs [AMD64_RIP] = rip;
			new_ctx->gregs [AMD64_RSP] = (*lmf)->rsp;
			new_ctx->gregs [AMD64_RBP] = (*lmf)->rbp;
			for (i = 0; i < AMD64_NREG; ++i) {
				if (AMD64_IS_CALLEE_SAVED_REG (i) && i != AMD64_RBP)
					new_ctx->gregs [i] = 0;
			}
		}

		*lmf = (MonoLMF *)(((guint64)(*lmf)->previous_lmf) & ~7);

		return TRUE;
	}

	return FALSE;
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

	mono_handle_exception (&ctx, (MonoObject *)obj);

	mono_restore_context (&ctx);
}

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	guint64 sp = ctx->gregs [AMD64_RSP];

	ctx->gregs [AMD64_RDI] = (gsize)user_data;

	/* Allocate a stack frame below the red zone */
	sp -= 128;
	/* The stack should be unaligned */
	if ((sp % 16) == 0)
		sp -= 8;
#ifdef __linux__
	/* Preserve the call chain to prevent crashes in the libgcc unwinder (#15969) */
	*(guint64*)sp = ctx->gregs [AMD64_RIP];
#endif
	ctx->gregs [AMD64_RSP] = sp;
	ctx->gregs [AMD64_RIP] = (gsize)async_cb;
}

/**
 * mono_arch_handle_exception:
 * \param ctx saved processor state
 * \param obj the exception object
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj)
{
#if defined(MONO_ARCH_USE_SIGACTION)
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
#else
	MonoContext mctx;

	mono_sigctx_to_monoctx (sigctx, &mctx);

	mono_handle_exception (&mctx, obj);

	mono_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
#endif
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#if defined(MONO_ARCH_USE_SIGACTION)
	ucontext_t *ctx = (ucontext_t*)sigctx;

	return (gpointer)UCONTEXT_REG_RIP (ctx);
#elif defined(HOST_WIN32)
	return (gpointer)(((CONTEXT*)sigctx)->Rip);
#else
	MonoContext *ctx = (MonoContext*)sigctx;
	return (gpointer)ctx->gregs [AMD64_RIP];
#endif	
}

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
	sp = (gpointer *)(mctx->gregs [AMD64_RSP]);
	sp -= 1;
	/* the return addr */
	sp [0] = (gpointer)(mctx->gregs [AMD64_RIP]);
	mctx->gregs [AMD64_RIP] = (guint64)restore_soft_guard_pages;
	mctx->gregs [AMD64_RSP] = (guint64)sp;
}

static void
altstack_handle_and_restore (MonoContext *ctx, MonoObject *obj, guint32 flags)
{
	MonoContext mctx;
	MonoJitInfo *ji = mini_jit_info_table_find (mono_domain_get (), MONO_CONTEXT_GET_IP (ctx), NULL);
	gboolean stack_ovf = (flags & 1) != 0;
	gboolean nullref = (flags & 2) != 0;

	if (!ji || (!stack_ovf && !nullref)) {
		if (mono_dump_start ())
			mono_handle_native_crash (mono_get_signame (SIGSEGV), ctx, NULL, NULL);
		// if couldn't dump or if mono_handle_native_crash returns, abort
		abort ();
	}

	mctx = *ctx;

	mono_handle_exception (&mctx, obj);
	if (stack_ovf) {
		MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
		jit_tls->stack_ovf_pending = 1;
		prepare_for_guard_pages (&mctx);
	}
	mono_restore_context (&mctx);
}

void
mono_arch_handle_altstack_exception (void *sigctx, MONO_SIG_HANDLER_INFO_TYPE *siginfo, gpointer fault_addr, gboolean stack_ovf)
{
#if defined(MONO_ARCH_USE_SIGACTION)
	MonoException *exc = NULL;
	gpointer *sp;
	MonoJitTlsData *jit_tls = NULL;
	MonoContext *copied_ctx = NULL;
	gboolean nullref = TRUE;

	jit_tls = mono_tls_get_jit_tls ();
	g_assert (jit_tls);

	/* use TLS as temporary storage as we want to avoid
	 * (1) stack allocation on the application stack
	 * (2) calling malloc, because it is not async-safe
	 * (3) using a global storage, because this function is not reentrant
	 *
	 * tls->orig_ex_ctx is used by the stack walker, which shouldn't be running at this point.
	 */
	copied_ctx = &jit_tls->orig_ex_ctx;

	if (!mono_is_addr_implicit_null_check (fault_addr))
		nullref = FALSE;

	if (stack_ovf)
		exc = mono_domain_get ()->stack_overflow_ex;

	/* setup the call frame on the application stack so that control is
	 * returned there and exception handling can continue. we want the call
	 * frame to be minimal as possible, for example no argument passing that
	 * requires allocation on the stack, as this wouldn't be encoded in unwind
	 * information for the caller frame.
	 */
	sp = (gpointer *) ALIGN_DOWN_TO (UCONTEXT_REG_RSP (sigctx), 16);
	sp [-1] = (gpointer)UCONTEXT_REG_RIP (sigctx);
	mono_sigctx_to_monoctx (sigctx, copied_ctx);
	/* at the return from the signal handler execution starts in altstack_handle_and_restore() */
	UCONTEXT_REG_RIP (sigctx) = (unsigned long)altstack_handle_and_restore;
	UCONTEXT_REG_RSP (sigctx) = (unsigned long)(sp - 1);
	UCONTEXT_REG_RDI (sigctx) = (unsigned long)(copied_ctx);
	UCONTEXT_REG_RSI (sigctx) = (guint64)exc;
	UCONTEXT_REG_RDX (sigctx) = (stack_ovf ? 1 : 0) | (nullref ? 2 : 0);
#endif
}

#ifndef DISABLE_JIT
GSList*
mono_amd64_get_exception_trampolines (gboolean aot)
{
	MonoTrampInfo *info;
	GSList *tramps = NULL;

	// FIXME Macro to make one line per trampoline.

	/* LLVM needs different throw trampolines */
	get_throw_trampoline (&info, FALSE, TRUE, FALSE, FALSE, "llvm_throw_corlib_exception_trampoline", aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline;
	tramps = g_slist_prepend (tramps, info);

	get_throw_trampoline (&info, FALSE, TRUE, TRUE, FALSE, "llvm_throw_corlib_exception_abs_trampoline", aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline;
	tramps = g_slist_prepend (tramps, info);

	get_throw_trampoline (&info, FALSE, TRUE, TRUE, TRUE, "llvm_resume_unwind_trampoline", aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline;
	tramps = g_slist_prepend (tramps, info);

	return tramps;
}

#else

GSList*
mono_amd64_get_exception_trampolines (gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* !DISABLE_JIT */

void
mono_arch_exceptions_init (void)
{
	GSList *tramps, *l;
	gpointer tramp;

	if (mono_ee_features.use_aot_trampolines) {

		// FIXME Macro can make one line per trampoline here.
		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline, tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_abs_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline, tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_resume_unwind_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline, tramp, "llvm_resume_unwind_trampoline", NULL, TRUE, NULL);

	} else if (!mono_llvm_only) {
		/* Call this to avoid initialization races */
		tramps = mono_amd64_get_exception_trampolines (FALSE);
		for (l = tramps; l; l = l->next) {
			MonoTrampInfo *info = (MonoTrampInfo *)l->data;

			mono_register_jit_icall_info (info->jit_icall_info, info->code, g_strdup (info->name), NULL, TRUE, NULL);
			mono_tramp_info_register (info, NULL);
		}
		g_slist_free (tramps);
	}
}

// Implies defined(TARGET_WIN32)
#ifdef MONO_ARCH_HAVE_UNWIND_TABLE

static void
mono_arch_unwindinfo_create (gpointer* monoui)
{
	PUNWIND_INFO newunwindinfo;
	*monoui = newunwindinfo = g_new0 (UNWIND_INFO, 1);
	newunwindinfo->Version = 1;
}

void
mono_arch_unwindinfo_add_push_nonvol (PUNWIND_INFO unwindinfo, MonoUnwindOp *unwind_op)
{
	PUNWIND_CODE unwindcode;
	guchar codeindex;

	g_assert (unwindinfo != NULL);

	if (unwindinfo->CountOfCodes >= MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (++unwindinfo->CountOfCodes);
	unwindcode = &unwindinfo->UnwindCode [codeindex];
	unwindcode->UnwindOp = UWOP_PUSH_NONVOL;
	unwindcode->CodeOffset = (guchar)unwind_op->when;
	unwindcode->OpInfo = unwind_op->reg;

	if (unwindinfo->SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");

	unwindinfo->SizeOfProlog = unwindcode->CodeOffset;
}

void
mono_arch_unwindinfo_add_set_fpreg (PUNWIND_INFO unwindinfo, MonoUnwindOp *unwind_op)
{
	PUNWIND_CODE unwindcode;
	guchar codeindex;

	g_assert (unwindinfo != NULL);

	if (unwindinfo->CountOfCodes + 1 >= MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (++unwindinfo->CountOfCodes);
	unwindcode = &unwindinfo->UnwindCode [codeindex];
	unwindcode->UnwindOp = UWOP_SET_FPREG;
	unwindcode->CodeOffset = (guchar)unwind_op->when;

	g_assert (unwind_op->val % 16 == 0);
	unwindinfo->FrameRegister = unwind_op->reg;
	unwindinfo->FrameOffset = unwind_op->val / 16;

	if (unwindinfo->SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");

	unwindinfo->SizeOfProlog = unwindcode->CodeOffset;
}

void
mono_arch_unwindinfo_add_alloc_stack (PUNWIND_INFO unwindinfo, MonoUnwindOp *unwind_op)
{
	PUNWIND_CODE unwindcode;
	guchar codeindex;
	guchar codesneeded;
	guint size;

	g_assert (unwindinfo != NULL);

	size = unwind_op->val;

	if (size < 0x8)
		g_error ("Stack allocation must be equal to or greater than 0x8.");

	if (size <= 0x80)
		codesneeded = 1;
	else if (size <= 0x7FFF8)
		codesneeded = 2;
	else
		codesneeded = 3;

	if (unwindinfo->CountOfCodes + codesneeded > MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (unwindinfo->CountOfCodes += codesneeded);
	unwindcode = &unwindinfo->UnwindCode [codeindex];

	unwindcode->CodeOffset = (guchar)unwind_op->when;

	if (codesneeded == 1) {
		/*The size of the allocation is
		  (the number in the OpInfo member) times 8 plus 8*/
		unwindcode->UnwindOp = UWOP_ALLOC_SMALL;
		unwindcode->OpInfo = (size - 8)/8;
	}
	else {
		if (codesneeded == 3) {
			/*the unscaled size of the allocation is recorded
			  in the next two slots in little-endian format.
			  NOTE, unwind codes are allocated from end to beginning of list so
			  unwind code will have right execution order. List is sorted on CodeOffset
			  using descending sort order.*/
			unwindcode->UnwindOp = UWOP_ALLOC_LARGE;
			unwindcode->OpInfo = 1;
			*((unsigned int*)(&(unwindcode + 1)->FrameOffset)) = size;
		}
		else {
			/*the size of the allocation divided by 8
			  is recorded in the next slot.
			  NOTE, unwind codes are allocated from end to beginning of list so
			  unwind code will have right execution order. List is sorted on CodeOffset
			  using descending sort order.*/
			unwindcode->UnwindOp = UWOP_ALLOC_LARGE;
			unwindcode->OpInfo = 0;
			(unwindcode + 1)->FrameOffset = (gushort)(size/8);
		}
	}

	if (unwindinfo->SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");

	unwindinfo->SizeOfProlog = unwindcode->CodeOffset;
}

static gboolean g_dyn_func_table_inited;

// Dynamic function table used when registering unwind info for OS unwind support.
static GList *g_dynamic_function_table_begin;
static GList *g_dynamic_function_table_end;

// SRW lock (lightweight read/writer lock) protecting dynamic function table.
static SRWLOCK g_dynamic_function_table_lock = SRWLOCK_INIT;

static RtlInstallFunctionTableCallbackPtr g_rtl_install_function_table_callback;
static RtlDeleteFunctionTablePtr g_rtl_delete_function_table;

// If Win8 or Win2012Server or later, use growable function tables instead
// of callbacks. Callback solution will still be fallback on older systems.
static RtlAddGrowableFunctionTablePtr g_rtl_add_growable_function_table;
static RtlGrowFunctionTablePtr g_rtl_grow_function_table;
static RtlDeleteGrowableFunctionTablePtr g_rtl_delete_growable_function_table;

// When using function table callback solution an out of proc module is needed by
// debuggers in order to read unwind info from debug target.
#ifdef _MSC_VER
#define MONO_DAC_MODULE L"mono-2.0-dac-sgen.dll"
#else
#define MONO_DAC_MODULE L"mono-2.0-sgen.dll"
#endif

#define MONO_DAC_MODULE_MAX_PATH 1024

static void
init_table_no_lock (void)
{
	if (g_dyn_func_table_inited == FALSE) {
		g_assert_checked (g_dynamic_function_table_begin == NULL);
		g_assert_checked (g_dynamic_function_table_end == NULL);
		g_assert_checked (g_rtl_install_function_table_callback == NULL);
		g_assert_checked (g_rtl_delete_function_table == NULL);
		g_assert_checked (g_rtl_add_growable_function_table == NULL);
		g_assert_checked (g_rtl_grow_function_table == NULL);
		g_assert_checked (g_rtl_delete_growable_function_table == NULL);

		// Load functions available on Win8/Win2012Server or later. If running on earlier
		// systems the below GetProceAddress will fail, this is expected behavior.
		HMODULE ntdll;
		if (GetModuleHandleEx (0, L"ntdll.dll", &ntdll)) {
			g_rtl_add_growable_function_table = (RtlAddGrowableFunctionTablePtr)GetProcAddress (ntdll, "RtlAddGrowableFunctionTable");
			g_rtl_grow_function_table = (RtlGrowFunctionTablePtr)GetProcAddress (ntdll, "RtlGrowFunctionTable");
			g_rtl_delete_growable_function_table = (RtlDeleteGrowableFunctionTablePtr)GetProcAddress (ntdll, "RtlDeleteGrowableFunctionTable");
		}

		// Fallback on systems not having RtlAddGrowableFunctionTable.
		if (g_rtl_add_growable_function_table == NULL) {
			HMODULE kernel32dll;
			if (GetModuleHandleEx (0, L"kernel32.dll", &kernel32dll)) {
				g_rtl_install_function_table_callback = (RtlInstallFunctionTableCallbackPtr)GetProcAddress (kernel32dll, "RtlInstallFunctionTableCallback");
				g_rtl_delete_function_table = (RtlDeleteFunctionTablePtr)GetProcAddress (kernel32dll, "RtlDeleteFunctionTable");
			}
		}

		g_dyn_func_table_inited = TRUE;
	}
}

void
mono_arch_unwindinfo_init_table (void)
{
	if (g_dyn_func_table_inited == FALSE) {

		AcquireSRWLockExclusive (&g_dynamic_function_table_lock);

		init_table_no_lock ();

		ReleaseSRWLockExclusive (&g_dynamic_function_table_lock);
	}
}

static void
terminate_table_no_lock (void)
{
	if (g_dyn_func_table_inited == TRUE) {
		if (g_dynamic_function_table_begin != NULL) {
			// Free all list elements.
			for (GList *l = g_dynamic_function_table_begin; l; l = l->next) {
				if (l->data) {
					g_free (l->data);
					l->data = NULL;
				}
			}

			//Free the list.
			g_list_free (g_dynamic_function_table_begin);
			g_dynamic_function_table_begin = NULL;
			g_dynamic_function_table_end = NULL;
		}

		g_rtl_delete_growable_function_table = NULL;
		g_rtl_grow_function_table = NULL;
		g_rtl_add_growable_function_table = NULL;

		g_rtl_delete_function_table = NULL;
		g_rtl_install_function_table_callback = NULL;

		g_dyn_func_table_inited = FALSE;
	}
}

void
mono_arch_unwindinfo_terminate_table (void)
{
	if (g_dyn_func_table_inited == TRUE) {

		AcquireSRWLockExclusive (&g_dynamic_function_table_lock);

		terminate_table_no_lock ();

		ReleaseSRWLockExclusive (&g_dynamic_function_table_lock);
	}
}

static GList *
fast_find_range_in_table_no_lock_ex (gsize begin_range, gsize end_range, gboolean *continue_search)
{
	GList *found_entry = NULL;

	// Fast path, look at boundaries.
	if (g_dynamic_function_table_begin != NULL) {
		DynamicFunctionTableEntry *first_entry = (DynamicFunctionTableEntry*)g_dynamic_function_table_begin->data;
		DynamicFunctionTableEntry *last_entry = (g_dynamic_function_table_end != NULL ) ? (DynamicFunctionTableEntry*)g_dynamic_function_table_end->data : first_entry;

		// Sorted in descending order based on begin_range, check first item, that is the entry with highest range.
		if (first_entry != NULL && first_entry->begin_range <= begin_range && first_entry->end_range >= end_range) {
				// Entry belongs to first entry in list.
				found_entry = g_dynamic_function_table_begin;
				*continue_search = FALSE;
		} else {
			if (first_entry != NULL && first_entry->begin_range >= begin_range) {
				if (last_entry != NULL && last_entry->begin_range <= begin_range) {
					// Entry has a range that could exist in table, continue search.
					*continue_search = TRUE;
				}
			}
		}
	}

	return found_entry;
}

static DynamicFunctionTableEntry *
fast_find_range_in_table_no_lock (gsize begin_range, gsize end_range, gboolean *continue_search)
{
	GList *found_entry = fast_find_range_in_table_no_lock_ex (begin_range, end_range, continue_search);
	return (found_entry != NULL) ? (DynamicFunctionTableEntry *)found_entry->data : NULL;
}

static GList *
find_range_in_table_no_lock_ex (const gpointer code_block, gsize block_size)
{
	GList *found_entry = NULL;
	gboolean continue_search = FALSE;

	gsize begin_range = (gsize)code_block;
	gsize end_range = begin_range + block_size;

	// Fast path, check table boundaries.
	found_entry = fast_find_range_in_table_no_lock_ex (begin_range, end_range, &continue_search);
	if (found_entry || continue_search == FALSE)
		return found_entry;

	// Scan table for an entry including range.
	for (GList *node = g_dynamic_function_table_begin; node; node = node->next) {
		DynamicFunctionTableEntry *current_entry = (DynamicFunctionTableEntry *)node->data;
		g_assert_checked (current_entry != NULL);

		// Do we have a match?
		if (current_entry->begin_range == begin_range && current_entry->end_range == end_range) {
			found_entry = node;
			break;
		}
	}

	return found_entry;
}

static DynamicFunctionTableEntry *
find_range_in_table_no_lock (const gpointer code_block, gsize block_size)
{
	GList *found_entry = find_range_in_table_no_lock_ex (code_block, block_size);
	return (found_entry != NULL) ? (DynamicFunctionTableEntry *)found_entry->data : NULL;
}

static GList *
find_pc_in_table_no_lock_ex (const gpointer pc)
{
	GList *found_entry = NULL;
	gboolean continue_search = FALSE;

	gsize begin_range = (gsize)pc;
	gsize end_range = begin_range;

	// Fast path, check table boundaries.
	found_entry = fast_find_range_in_table_no_lock_ex (begin_range, begin_range, &continue_search);
	if (found_entry || continue_search == FALSE)
		return found_entry;

	// Scan table for a entry including range.
	for (GList *node = g_dynamic_function_table_begin; node; node = node->next) {
		DynamicFunctionTableEntry *current_entry = (DynamicFunctionTableEntry *)node->data;
		g_assert_checked (current_entry != NULL);

		// Do we have a match?
		if (current_entry->begin_range <= begin_range && current_entry->end_range >= end_range) {
			found_entry = node;
			break;
		}
	}

	return found_entry;
}

static DynamicFunctionTableEntry *
find_pc_in_table_no_lock (const gpointer pc)
{
	GList *found_entry = find_pc_in_table_no_lock_ex (pc);
	return (found_entry != NULL) ? (DynamicFunctionTableEntry *)found_entry->data : NULL;
}

#ifdef ENABLE_CHECKED_BUILD_UNWINDINFO
static void
validate_table_no_lock (void)
{
	// Validation method checking that table is sorted as expected and don't include overlapped regions.
	// Method will assert on failure to explicitly indicate what check failed.
	if (g_dynamic_function_table_begin != NULL) {
		g_assert_checked (g_dynamic_function_table_end != NULL);

		DynamicFunctionTableEntry *prevoious_entry = NULL;
		DynamicFunctionTableEntry *current_entry = NULL;
		for (GList *node = g_dynamic_function_table_begin; node; node = node->next) {
			current_entry = (DynamicFunctionTableEntry *)node->data;

			g_assert_checked (current_entry != NULL);
			g_assert_checked (current_entry->end_range > current_entry->begin_range);

			if (prevoious_entry != NULL) {
				// List should be sorted in descending order on begin_range.
				g_assert_checked (prevoious_entry->begin_range > current_entry->begin_range);

				// Check for overlapped regions.
				g_assert_checked (prevoious_entry->begin_range >= current_entry->end_range);
			}

			prevoious_entry = current_entry;
		}
	}
}

#else

static void
validate_table_no_lock (void)
{
}
#endif /* ENABLE_CHECKED_BUILD_UNWINDINFO */

// Forward declare.
static PRUNTIME_FUNCTION MONO_GET_RUNTIME_FUNCTION_CALLBACK (DWORD64 ControlPc, IN PVOID Context);

DynamicFunctionTableEntry *
mono_arch_unwindinfo_insert_range_in_table (const gpointer code_block, gsize block_size)
{
	DynamicFunctionTableEntry *new_entry = NULL;

	gsize begin_range = (gsize)code_block;
	gsize end_range = begin_range + block_size;

	AcquireSRWLockExclusive (&g_dynamic_function_table_lock);
	init_table_no_lock ();
	new_entry = find_range_in_table_no_lock (code_block, block_size);
	if (new_entry == NULL && block_size != 0) {
		// Allocate new entry.
		new_entry = g_new0 (DynamicFunctionTableEntry, 1);
		if (new_entry != NULL) {

			// Pre-allocate RUNTIME_FUNCTION array, assume average method size of
			// MONO_UNWIND_INFO_RT_FUNC_SIZE bytes.
			InitializeSRWLock (&new_entry->lock);
			new_entry->handle = NULL;
			new_entry->begin_range = begin_range;
			new_entry->end_range = end_range;
			new_entry->rt_funcs_max_count = (block_size / MONO_UNWIND_INFO_RT_FUNC_SIZE) + 1;
			new_entry->rt_funcs_current_count = 0;
			new_entry->rt_funcs = g_new0 (RUNTIME_FUNCTION, new_entry->rt_funcs_max_count);

			if (new_entry->rt_funcs != NULL) {
				// Check insert on boundaries. List is sorted descending on begin_range.
				if (g_dynamic_function_table_begin == NULL) {
					g_dynamic_function_table_begin = g_list_append (g_dynamic_function_table_begin, new_entry);
					g_dynamic_function_table_end = g_dynamic_function_table_begin;
				} else if (((DynamicFunctionTableEntry *)(g_dynamic_function_table_begin->data))->begin_range < begin_range) {
					// Insert at the head.
					g_dynamic_function_table_begin = g_list_prepend (g_dynamic_function_table_begin, new_entry);
				} else if (((DynamicFunctionTableEntry *)(g_dynamic_function_table_end->data))->begin_range > begin_range) {
					// Insert at tail.
					g_list_append (g_dynamic_function_table_end, new_entry);
					g_dynamic_function_table_end = g_dynamic_function_table_end->next;
				} else {
					//Search and insert at correct position.
					for (GList *node = g_dynamic_function_table_begin; node; node = node->next) {
						DynamicFunctionTableEntry * current_entry = (DynamicFunctionTableEntry *)node->data;
						g_assert_checked (current_entry != NULL);

						if (current_entry->begin_range < new_entry->begin_range) {
							g_dynamic_function_table_begin = g_list_insert_before (g_dynamic_function_table_begin, node, new_entry);
							break;
						}
					}
				}

				// Register dynamic function table entry with OS.
				if (g_rtl_add_growable_function_table != NULL) {
					// Allocate new growable handle table for entry.
					g_assert_checked (new_entry->handle == NULL);
					DWORD result = g_rtl_add_growable_function_table (&new_entry->handle,
										new_entry->rt_funcs, new_entry->rt_funcs_current_count,
										new_entry->rt_funcs_max_count, new_entry->begin_range, new_entry->end_range);
					g_assert (!result);
				} else if (g_rtl_install_function_table_callback != NULL) {
					WCHAR buffer [MONO_DAC_MODULE_MAX_PATH] = { 0 };
					WCHAR *path = buffer;

					// DAC module should be in the same directory as the
					// main executable.
					GetModuleFileNameW (NULL, buffer, G_N_ELEMENTS(buffer));
					path = wcsrchr (buffer, TEXT('\\'));
					if (path != NULL) {
						path++;
						*path = TEXT('\0');
					}

					wcscat_s (buffer, G_N_ELEMENTS(buffer), MONO_DAC_MODULE);
					path = buffer;

					// Register function table callback + out of proc module.
					new_entry->handle = (PVOID)((DWORD64)(new_entry->begin_range) | 3);
					BOOLEAN result = g_rtl_install_function_table_callback ((DWORD64)(new_entry->handle),
									(DWORD64)(new_entry->begin_range), (DWORD)(new_entry->end_range - new_entry->begin_range),
									MONO_GET_RUNTIME_FUNCTION_CALLBACK, new_entry, path);
					g_assert(result);
				} else {
					g_assert_not_reached ();
				}

				// Only included in checked builds. Validates the structure of table after insert.
				validate_table_no_lock ();

			} else {
				g_free (new_entry);
				new_entry = NULL;
			}
		}
	}
	ReleaseSRWLockExclusive (&g_dynamic_function_table_lock);

	return new_entry;
}

static void
remove_range_in_table_no_lock (GList *entry)
{
	if (entry != NULL) {
		if (entry == g_dynamic_function_table_end)
			g_dynamic_function_table_end = entry->prev;

		g_dynamic_function_table_begin = g_list_remove_link (g_dynamic_function_table_begin, entry);
		DynamicFunctionTableEntry *removed_entry = (DynamicFunctionTableEntry *)entry->data;

		g_assert_checked (removed_entry != NULL);
		g_assert_checked (removed_entry->rt_funcs != NULL);

		// Remove function table from OS.
		if (removed_entry->handle != NULL) {
			if (g_rtl_delete_growable_function_table != NULL) {
				g_rtl_delete_growable_function_table (removed_entry->handle);
			} else if (g_rtl_delete_function_table != NULL) {
				g_rtl_delete_function_table ((PRUNTIME_FUNCTION)removed_entry->handle);
			} else {
				g_assert_not_reached ();
			}
		}

		g_free (removed_entry->rt_funcs);
		g_free (removed_entry);

		g_list_free_1 (entry);
	}

	// Only included in checked builds. Validates the structure of table after remove.
	validate_table_no_lock ();
}

void
mono_arch_unwindinfo_remove_pc_range_in_table (const gpointer code)
{
	AcquireSRWLockExclusive (&g_dynamic_function_table_lock);

	GList *found_entry = find_pc_in_table_no_lock_ex (code);

	g_assert_checked (found_entry != NULL || ((DynamicFunctionTableEntry *)found_entry->data)->begin_range == (gsize)code);
	remove_range_in_table_no_lock (found_entry);

	ReleaseSRWLockExclusive (&g_dynamic_function_table_lock);
}

void
mono_arch_unwindinfo_remove_range_in_table (const gpointer code_block, gsize block_size)
{
	AcquireSRWLockExclusive (&g_dynamic_function_table_lock);

	GList *found_entry = find_range_in_table_no_lock_ex (code_block, block_size);

	g_assert_checked (found_entry != NULL || ((DynamicFunctionTableEntry *)found_entry->data)->begin_range == (gsize)code_block);
	remove_range_in_table_no_lock (found_entry);

	ReleaseSRWLockExclusive (&g_dynamic_function_table_lock);
}

PRUNTIME_FUNCTION
mono_arch_unwindinfo_find_rt_func_in_table (const gpointer code, gsize code_size)
{
	PRUNTIME_FUNCTION found_rt_func = NULL;

	gsize begin_range = (gsize)code;
	gsize end_range = begin_range + code_size;

	AcquireSRWLockShared (&g_dynamic_function_table_lock);

	DynamicFunctionTableEntry *found_entry = find_pc_in_table_no_lock (code);

	if (found_entry != NULL) {

		AcquireSRWLockShared (&found_entry->lock);

		g_assert_checked (found_entry->begin_range <= begin_range);
		g_assert_checked (found_entry->end_range >= begin_range && found_entry->end_range >= end_range);
		g_assert_checked (found_entry->rt_funcs != NULL);

		for (int i = 0; i < found_entry->rt_funcs_current_count; ++i) {
			PRUNTIME_FUNCTION current_rt_func = (PRUNTIME_FUNCTION)(&found_entry->rt_funcs [i]);

			// Is this our RT function entry?
			if (found_entry->begin_range + current_rt_func->BeginAddress <= begin_range &&
				found_entry->begin_range + current_rt_func->EndAddress >= end_range) {
				found_rt_func = current_rt_func;
				break;
			}
		}

		ReleaseSRWLockShared (&found_entry->lock);
	}

	ReleaseSRWLockShared (&g_dynamic_function_table_lock);

	return found_rt_func;
}

static PRUNTIME_FUNCTION
mono_arch_unwindinfo_find_pc_rt_func_in_table (const gpointer pc)
{
	return mono_arch_unwindinfo_find_rt_func_in_table (pc, 0);
}

#ifdef ENABLE_CHECKED_BUILD_UNWINDINFO
static void
validate_rt_funcs_in_table_no_lock (DynamicFunctionTableEntry *entry)
{
	// Validation method checking that runtime function table is sorted as expected and don't include overlapped regions.
	// Method will assert on failure to explicitly indicate what check failed.
	g_assert_checked (entry != NULL);
	g_assert_checked (entry->rt_funcs_max_count >= entry->rt_funcs_current_count);
	g_assert_checked (entry->rt_funcs != NULL);

	PRUNTIME_FUNCTION current_rt_func = NULL;
	PRUNTIME_FUNCTION previous_rt_func = NULL;
	for (int i = 0; i < entry->rt_funcs_current_count; ++i) {
		current_rt_func = &(entry->rt_funcs [i]);

		g_assert_checked (current_rt_func->BeginAddress < current_rt_func->EndAddress);
		g_assert_checked (current_rt_func->EndAddress <= current_rt_func->UnwindData);

		if (previous_rt_func != NULL) {
			// List should be sorted in ascending order based on BeginAddress.
			g_assert_checked (previous_rt_func->BeginAddress < current_rt_func->BeginAddress);

			// Check for overlapped regions.
			g_assert_checked (previous_rt_func->EndAddress <= current_rt_func->BeginAddress);
		}

		previous_rt_func = current_rt_func;
	}
}

#else

static void
validate_rt_funcs_in_table_no_lock (DynamicFunctionTableEntry *entry)
{
}
#endif /* ENABLE_CHECKED_BUILD_UNWINDINFO */

PRUNTIME_FUNCTION
mono_arch_unwindinfo_insert_rt_func_in_table (const gpointer code, gsize code_size)
{
	PRUNTIME_FUNCTION new_rt_func = NULL;

	gsize begin_range = (gsize)code;
	gsize end_range = begin_range + code_size;

	AcquireSRWLockShared (&g_dynamic_function_table_lock);

	DynamicFunctionTableEntry *found_entry = find_pc_in_table_no_lock (code);

	if (found_entry != NULL) {

		AcquireSRWLockExclusive (&found_entry->lock);

		g_assert_checked (found_entry->begin_range <= begin_range);
		g_assert_checked (found_entry->end_range >= begin_range && found_entry->end_range >= end_range);
		g_assert_checked (found_entry->rt_funcs != NULL);
		g_assert_checked ((guchar*)code - found_entry->begin_range >= 0);

		gsize code_offset = (gsize)code - found_entry->begin_range;
		gsize entry_count = found_entry->rt_funcs_current_count;
		gsize max_entry_count = found_entry->rt_funcs_max_count;
		PRUNTIME_FUNCTION current_rt_funcs = found_entry->rt_funcs;

		RUNTIME_FUNCTION new_rt_func_data;
		new_rt_func_data.BeginAddress = code_offset;
		new_rt_func_data.EndAddress = code_offset + code_size;

		gsize aligned_unwind_data = ALIGN_TO(end_range, sizeof(host_mgreg_t));
		new_rt_func_data.UnwindData = aligned_unwind_data - found_entry->begin_range;

		g_assert_checked (new_rt_func_data.UnwindData == ALIGN_TO(new_rt_func_data.EndAddress, sizeof (host_mgreg_t)));

		PRUNTIME_FUNCTION new_rt_funcs = NULL;

		// List needs to be sorted in ascending order based on BeginAddress (Windows requirement if list
		// going to be directly reused in OS func tables. Check if we can append to end of existing table without realloc.
		if (entry_count == 0 || (entry_count < max_entry_count) && (current_rt_funcs [entry_count - 1].BeginAddress) < code_offset) {
			new_rt_func = &(current_rt_funcs [entry_count]);
			*new_rt_func = new_rt_func_data;
			entry_count++;
		} else {
			// No easy way out, need to realloc, grow to double size (or current max, if to small).
			max_entry_count = entry_count * 2 > max_entry_count ? entry_count * 2 : max_entry_count;
			new_rt_funcs = g_new0 (RUNTIME_FUNCTION, max_entry_count);

			if (new_rt_funcs != NULL) {
				gsize from_index = 0;
				gsize to_index = 0;

				// Copy from old table into new table. Make sure new rt func gets inserted
				// into correct location based on sort order.
				for (; from_index < entry_count; ++from_index) {
					if (new_rt_func == NULL && current_rt_funcs [from_index].BeginAddress > new_rt_func_data.BeginAddress) {
						new_rt_func = &(new_rt_funcs [to_index++]);
						*new_rt_func = new_rt_func_data;
					}

					if (current_rt_funcs [from_index].UnwindData != 0)
						new_rt_funcs [to_index++] = current_rt_funcs [from_index];
				}

				// If we didn't insert by now, put it last in the list.
				if (new_rt_func == NULL) {
					new_rt_func = &(new_rt_funcs [to_index]);
					*new_rt_func = new_rt_func_data;
				}
			}

			entry_count++;
		}

		// Update the stats for current entry.
		found_entry->rt_funcs_current_count = entry_count;
		found_entry->rt_funcs_max_count = max_entry_count;

		if (new_rt_funcs == NULL && g_rtl_grow_function_table != NULL) {
			// No new table just report increase in use.
			g_assert_checked (found_entry->handle != NULL);
			g_rtl_grow_function_table (found_entry->handle, found_entry->rt_funcs_current_count);
		} else if (new_rt_funcs != NULL && g_rtl_add_growable_function_table != NULL) {
			// New table, delete old table and rt funcs, and register a new one.
			g_assert_checked (g_rtl_delete_growable_function_table != NULL);
			g_rtl_delete_growable_function_table (found_entry->handle);
			found_entry->handle = NULL;
			g_free (found_entry->rt_funcs);
			found_entry->rt_funcs = new_rt_funcs;
			DWORD result = g_rtl_add_growable_function_table (&found_entry->handle,
								found_entry->rt_funcs, found_entry->rt_funcs_current_count,
								found_entry->rt_funcs_max_count, found_entry->begin_range, found_entry->end_range);
			g_assert (!result);
		} else if (new_rt_funcs != NULL && g_rtl_add_growable_function_table == NULL) {
			// No table registered with OS, callback solution in use. Switch tables.
			g_free (found_entry->rt_funcs);
			found_entry->rt_funcs = new_rt_funcs;
		} else if (new_rt_funcs == NULL && g_rtl_grow_function_table == NULL) {
			// No table registered with OS, callback solution in use, nothing to do.
		} else {
			g_assert_not_reached ();
		}

		// Only included in checked builds. Validates the structure of table after insert.
		validate_rt_funcs_in_table_no_lock (found_entry);

		ReleaseSRWLockExclusive (&found_entry->lock);
	}

	ReleaseSRWLockShared (&g_dynamic_function_table_lock);

	return new_rt_func;
}

static PRUNTIME_FUNCTION
MONO_GET_RUNTIME_FUNCTION_CALLBACK ( DWORD64 ControlPc, IN PVOID Context )
{
	return mono_arch_unwindinfo_find_pc_rt_func_in_table ((gpointer)ControlPc);
}

static void
initialize_unwind_info_internal_ex (GSList *unwind_ops, PUNWIND_INFO unwindinfo)
{
	if (unwind_ops != NULL && unwindinfo != NULL) {
		MonoUnwindOp *unwind_op_data;
		gboolean sp_alloced = FALSE;
		gboolean fp_alloced = FALSE;

		// Replay collected unwind info and setup Windows format.
		for (GSList *l = unwind_ops; l; l = l->next) {
			unwind_op_data = (MonoUnwindOp *)l->data;
			switch (unwind_op_data->op) {
				case DW_CFA_offset : {
					// Pushes should go before SP/FP allocation to be compliant with Windows x64 ABI.
					// TODO: DW_CFA_offset can also be used to move saved regs into frame.
					if (unwind_op_data->reg != AMD64_RIP && sp_alloced == FALSE && fp_alloced == FALSE)
						mono_arch_unwindinfo_add_push_nonvol (unwindinfo, unwind_op_data);
					break;
				}
				case DW_CFA_mono_sp_alloc_info_win64 : {
					mono_arch_unwindinfo_add_alloc_stack (unwindinfo, unwind_op_data);
					sp_alloced = TRUE;
					break;
				}
				case DW_CFA_mono_fp_alloc_info_win64 : {
					mono_arch_unwindinfo_add_set_fpreg (unwindinfo, unwind_op_data);
					fp_alloced = TRUE;
					break;
				}
				default :
					break;
			}
		}
	}
}

static PUNWIND_INFO
initialize_unwind_info_internal (GSList *unwind_ops)
{
	PUNWIND_INFO unwindinfo;

	mono_arch_unwindinfo_create ((gpointer*)&unwindinfo);
	initialize_unwind_info_internal_ex (unwind_ops, unwindinfo);

	return unwindinfo;
}

guchar
mono_arch_unwindinfo_get_code_count (GSList *unwind_ops)
{
	UNWIND_INFO unwindinfo = {0};
	initialize_unwind_info_internal_ex (unwind_ops, &unwindinfo);
	return unwindinfo.CountOfCodes;
}

PUNWIND_INFO
mono_arch_unwindinfo_alloc_unwind_info (GSList *unwind_ops)
{
	if (!unwind_ops)
		return NULL;

	return initialize_unwind_info_internal (unwind_ops);
}

void
mono_arch_unwindinfo_free_unwind_info (PUNWIND_INFO unwind_info)
{
	g_free (unwind_info);
}

guint
mono_arch_unwindinfo_init_method_unwind_info (gpointer cfg)
{
	MonoCompile * current_cfg = (MonoCompile *)cfg;
	g_assert (current_cfg->arch.unwindinfo == NULL);
	current_cfg->arch.unwindinfo = initialize_unwind_info_internal (current_cfg->unwind_ops);
	return mono_arch_unwindinfo_get_size (((PUNWIND_INFO)(current_cfg->arch.unwindinfo))->CountOfCodes);
}

void
mono_arch_unwindinfo_install_method_unwind_info (PUNWIND_INFO *monoui, gpointer code, guint code_size)
{
	PUNWIND_INFO unwindinfo, targetinfo;
	guchar codecount;
	guint64 targetlocation;
	if (!*monoui)
		return;

	unwindinfo = *monoui;
	targetlocation = (guint64)&(((guchar*)code)[code_size]);
	targetinfo = (PUNWIND_INFO) ALIGN_TO(targetlocation, sizeof (host_mgreg_t));

	memcpy (targetinfo, unwindinfo, sizeof (UNWIND_INFO) - (sizeof (UNWIND_CODE) * MONO_MAX_UNWIND_CODES));

	codecount = unwindinfo->CountOfCodes;
	if (codecount) {
		memcpy (&targetinfo->UnwindCode [0], &unwindinfo->UnwindCode [MONO_MAX_UNWIND_CODES - codecount],
			sizeof (UNWIND_CODE) * codecount);
	}

#ifdef ENABLE_CHECKED_BUILD_UNWINDINFO
	if (codecount) {
		// Validate the order of unwind op codes in checked builds. Offset should be in descending order.
		// In first iteration previous == current, this is intended to handle UWOP_ALLOC_LARGE as first item.
		int previous = 0;
		for (int current = 0; current < codecount; current++) {
			g_assert_checked (targetinfo->UnwindCode [previous].CodeOffset >= targetinfo->UnwindCode [current].CodeOffset);
			previous = current;
			if (targetinfo->UnwindCode [current].UnwindOp == UWOP_ALLOC_LARGE) {
				if (targetinfo->UnwindCode [current].OpInfo == 0) {
					current++;
				} else {
					current += 2;
				}
			}
		}
	}
#endif /* ENABLE_CHECKED_BUILD_UNWINDINFO */

	mono_arch_unwindinfo_free_unwind_info (unwindinfo);
	*monoui = 0;

	// Register unwind info in table.
	mono_arch_unwindinfo_insert_rt_func_in_table (code, code_size);
}

void
mono_arch_unwindinfo_install_tramp_unwind_info (GSList *unwind_ops, gpointer code, guint code_size)
{
	PUNWIND_INFO unwindinfo = initialize_unwind_info_internal (unwind_ops);
	if (unwindinfo != NULL) {
		mono_arch_unwindinfo_install_method_unwind_info (&unwindinfo, code, code_size);
	}
}

void
mono_arch_code_chunk_new (void *chunk, int size)
{
	mono_arch_unwindinfo_insert_range_in_table (chunk, size);
}

void mono_arch_code_chunk_destroy (void *chunk)
{
	mono_arch_unwindinfo_remove_pc_range_in_table (chunk);
}
#endif /* MONO_ARCH_HAVE_UNWIND_TABLE */

#if MONO_SUPPORT_TASKLETS && !defined(DISABLE_JIT) && !defined(ENABLE_NETCORE)
MonoContinuationRestore
mono_tasklets_arch_restore (void)
{
	static guint8* saved = NULL;
	guint8 *code, *start;
	int cont_reg = AMD64_R9; /* register usable on both call conventions */
	const int kMaxCodeSize = 64;

	if (saved)
		return (MonoContinuationRestore)saved;
	code = start = (guint8 *)mono_global_codeman_reserve (kMaxCodeSize);
	/* the signature is: restore (MonoContinuation *cont, int state, MonoLMF **lmf_addr) */
	/* cont is in AMD64_ARG_REG1 ($rcx or $rdi)
	 * state is in AMD64_ARG_REG2 ($rdx or $rsi)
	 * lmf_addr is in AMD64_ARG_REG3 ($r8 or $rdx)
	 * We move cont to cont_reg since we need both rcx and rdi for the copy
	 * state is moved to $rax so it's setup as the return value and we can overwrite $rsi
 	 */
	amd64_mov_reg_reg (code, cont_reg, MONO_AMD64_ARG_REG1, 8);
	amd64_mov_reg_reg (code, AMD64_RAX, MONO_AMD64_ARG_REG2, 8);
	/* setup the copy of the stack */
	amd64_mov_reg_membase (code, AMD64_RCX, cont_reg, MONO_STRUCT_OFFSET (MonoContinuation, stack_used_size), sizeof (int));
	amd64_shift_reg_imm (code, X86_SHR, AMD64_RCX, 3);
	x86_cld (code);
	amd64_mov_reg_membase (code, AMD64_RSI, cont_reg, MONO_STRUCT_OFFSET (MonoContinuation, saved_stack), sizeof (gpointer));
	amd64_mov_reg_membase (code, AMD64_RDI, cont_reg, MONO_STRUCT_OFFSET (MonoContinuation, return_sp), sizeof (gpointer));
	amd64_prefix (code, X86_REP_PREFIX);
	amd64_movsl (code);

	/* now restore the registers from the LMF */
	amd64_mov_reg_membase (code, AMD64_RCX, cont_reg, MONO_STRUCT_OFFSET (MonoContinuation, lmf), 8);
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_RCX, MONO_STRUCT_OFFSET (MonoLMF, rbp), 8);
	amd64_mov_reg_membase (code, AMD64_RSP, AMD64_RCX, MONO_STRUCT_OFFSET (MonoLMF, rsp), 8);

#ifdef WIN32
	amd64_mov_reg_reg (code, AMD64_R14, AMD64_ARG_REG3, 8);
#else
	amd64_mov_reg_reg (code, AMD64_R12, AMD64_ARG_REG3, 8);
#endif

	/* state is already in rax */
	amd64_jump_membase (code, cont_reg, MONO_STRUCT_OFFSET (MonoContinuation, return_ip));
	g_assertf ((code - start) <= kMaxCodeSize, "%d %d", (int)(code - start), kMaxCodeSize);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	saved = start;
	return (MonoContinuationRestore)saved;
}
#endif /* MONO_SUPPORT_TASKLETS && !defined(DISABLE_JIT) && !defined(ENABLE_NETCORE) */

/*
 * mono_arch_setup_resume_sighandler_ctx:
 *
 *   Setup CTX so execution continues at FUNC.
 */
void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	/* 
	 * When resuming from a signal handler, the stack should be misaligned, just like right after
	 * a call.
	 */
	if ((((guint64)MONO_CONTEXT_GET_SP (ctx)) % 16) == 0)
		MONO_CONTEXT_SET_SP (ctx, (guint64)MONO_CONTEXT_GET_SP (ctx) - 8);
	MONO_CONTEXT_SET_IP (ctx, func);
}

#if (!MONO_SUPPORT_TASKLETS || defined(DISABLE_JIT)) && !defined(ENABLE_NETCORE)
MonoContinuationRestore
mono_tasklets_arch_restore (void)
{
	g_assert_not_reached ();
	return NULL;
}
#endif /* (!MONO_SUPPORT_TASKLETS || defined(DISABLE_JIT)) && !defined(ENABLE_NETCORE) */

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
	ctx->gregs [AMD64_RIP]++;
}

void
mono_arch_do_ip_adjustment (MonoContext *ctx)
{
	ctx->gregs [AMD64_RIP]--;
}
