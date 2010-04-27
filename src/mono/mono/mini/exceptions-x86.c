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

#ifdef TARGET_WIN32
static void (*restore_stack) (void *);

static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

static LPTOP_LEVEL_EXCEPTION_FILTER old_handler;

#define W32_SEH_HANDLE_EX(_ex) \
	if (_ex##_handler) _ex##_handler(0, er, sctx)

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

	/* call mono_arch_handle_exception (sctx, stack_overflow_exception_obj, FALSE) */
	x86_push_imm (code, 0);
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
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
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

		mono_arch_find_jit_info_ext (domain, jit_tls, &rji, &ctx, &new_ctx, &lmf, &frame);
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

	return res;
}

void win32_seh_init()
{
	/* install restore stack helper */
	if (!restore_stack)
		restore_stack = mono_win32_get_handle_stackoverflow ();

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

#endif /* TARGET_WIN32 */

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *start = NULL;
	guint8 *code;

	/* restore_contect (MonoContext *ctx) */

	*ji = NULL;

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

	*code_size = code - start;

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
mono_arch_get_call_filter_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8* start;
	guint8 *code;

	*ji = NULL;

	/* call_filter (MonoContext *ctx, unsigned long eip) */
	start = code = mono_global_codeman_reserve (64);

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

	*code_size = code - start;

	g_assert ((code - start) < 64);
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

	mono_handle_exception (&ctx, exc, (gpointer)eip, FALSE);

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

	mono_x86_throw_exception (regs, (MonoObject*)ex, eip, FALSE);
}

/*
 * get_throw_exception:
 *
 *  Generate a call to mono_x86_throw_exception/
 * mono_x86_throw_corlib_exception.
 * If LLVM is true, generate code which assumes the caller is LLVM generated code, 
 * which doesn't push the arguments.
 */
static guint8*
get_throw_exception (const char *name, gboolean rethrow, gboolean llvm, gboolean corlib, guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *start, *code;
	GSList *unwind_ops = NULL;
	int i, stack_size, stack_offset, arg_offsets [5], regs_offset;

	if (ji)
		*ji = NULL;

	start = code = mono_global_codeman_reserve (128);

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
			/* One argument */
			stack_offset = stack_size + 4 + 4;
#ifdef __APPLE__
			/* Pop the alignment added by OP_THROW too */
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
	/* Set arg2 == exc */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size + 4, 4);
	x86_mov_membase_reg (code, X86_ESP, arg_offsets [1], X86_EAX, 4);
	/* Set arg3 == eip */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size, 4);
	x86_mov_membase_reg (code, X86_ESP, arg_offsets [2], X86_EAX, 4);
	if (corlib) {
		/* Set arg4 == offset */
		x86_mov_reg_membase (code, X86_EAX, X86_ESP, stack_size + 8, 4);
		x86_mov_membase_reg (code, X86_ESP, arg_offsets [3], X86_EAX, 4);
	} else {
		/* Set arg4 == rethrow */
		x86_mov_membase_imm (code, X86_ESP, arg_offsets [3], rethrow, 4);
	}
	/* Make the call */
	if (aot) {
		// This can be called from runtime code, which can't guarantee that
		// ebx contains the got address.
		// So emit the got address loading code too
		code = mono_arch_emit_load_got_addr (start, code, NULL, ji);
		code = mono_arch_emit_load_aotconst (start, code, ji, MONO_PATCH_INFO_JIT_ICALL_ADDR, corlib ? "mono_x86_throw_corlib_exception" : "mono_x86_throw_exception");
		x86_call_reg (code, X86_EAX);
	} else {
		x86_call_code (code, corlib ? (gpointer)mono_x86_throw_corlib_exception : (gpointer)mono_x86_throw_exception);
	}
	x86_breakpoint (code);

	g_assert ((code - start) < 128);

	if (code_size)
		*code_size = code - start;

	mono_save_trampoline_xdebug_info (corlib ? "llvm_throw_corlib_exception_trampoline" : "llvm_throw_exception_trampoline", start, code - start, unwind_ops);

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
mono_arch_get_throw_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return get_throw_exception ("throw_exception_trampoline", FALSE, FALSE, FALSE, code_size, ji, aot);
}

gpointer 
mono_arch_get_rethrow_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return get_throw_exception ("rethow_exception_trampoline", TRUE, FALSE, FALSE, code_size, ji, aot);
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
mono_arch_get_throw_corlib_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return get_throw_exception ("throw_corlib_exception_trampoline", FALSE, FALSE, TRUE, code_size, ji, aot);
}

void
mono_arch_exceptions_init (void)
{
	guint8 *tramp;

	if (mono_aot_only)
		return;

	/* LLVM needs different throw trampolines */
	tramp = get_throw_exception ("llvm_throw_exception_trampoline", FALSE, TRUE, FALSE, NULL, NULL, FALSE);

	mono_register_jit_icall (tramp, "mono_arch_llvm_throw_exception", NULL, TRUE);

	tramp = get_throw_exception ("llvm_throw_corlib_exception_trampoline", FALSE, TRUE, TRUE, NULL, NULL, FALSE);

	mono_register_jit_icall (tramp, "mono_arch_llvm_throw_corlib_exception", NULL, TRUE);
}

/*
 * mono_arch_find_jit_info_ext:
 *
 * See exceptions-amd64.c for docs.
 */
gboolean
mono_arch_find_jit_info_ext (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf, 
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;
	frame->managed = FALSE;

	*new_ctx = *ctx;

	if (ji != NULL) {
		gssize regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		frame->type = FRAME_TYPE_MANAGED;

		if (!ji->method->wrapper_type || ji->method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
			frame->managed = TRUE;

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
						   ip, regs, MONO_MAX_IREGS + 1, &cfa);

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
		 * the caller.
		 */
#ifndef ENABLE_LLVM
		{
			MonoJitArgumentInfo *arg_info = g_newa (MonoJitArgumentInfo, mono_method_signature (ji->method)->param_count + 1);

			guint32 stack_to_pop = mono_arch_get_argument_info (mono_method_signature (ji->method), mono_method_signature (ji->method)->param_count, arg_info);
			new_ctx->esp += stack_to_pop;
		}
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
			/* Trampoline lmf frame */
			frame->method = (*lmf)->method;
		}

		new_ctx->esi = (*lmf)->esi;
		new_ctx->edi = (*lmf)->edi;
		new_ctx->ebx = (*lmf)->ebx;
		new_ctx->ebp = (*lmf)->ebp;
		new_ctx->eip = (*lmf)->eip;

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
			if ((*lmf)->method && (*lmf)->method != MONO_FAKE_IMT_METHOD && (*lmf)->method != MONO_FAKE_VTABLE_METHOD) {
				MonoMethod *method = (*lmf)->method;
				MonoJitArgumentInfo *arg_info = g_newa (MonoJitArgumentInfo, mono_method_signature (method)->param_count + 1);

				guint32 stack_to_pop = mono_arch_get_argument_info (mono_method_signature (method), mono_method_signature (method)->param_count, arg_info);
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

#ifdef __sun
#define REG_EAX EAX
#define REG_EBX EBX
#define REG_ECX ECX
#define REG_EDX EDX
#define REG_EBP EBP
#define REG_ESP ESP
#define REG_ESI ESI
#define REG_EDI EDI
#define REG_EIP EIP
#endif

void
mono_arch_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;
	
	mctx->eax = UCONTEXT_REG_EAX (ctx);
	mctx->ebx = UCONTEXT_REG_EBX (ctx);
	mctx->ecx = UCONTEXT_REG_ECX (ctx);
	mctx->edx = UCONTEXT_REG_EDX (ctx);
	mctx->ebp = UCONTEXT_REG_EBP (ctx);
	mctx->esp = UCONTEXT_REG_ESP (ctx);
	mctx->esi = UCONTEXT_REG_ESI (ctx);
	mctx->edi = UCONTEXT_REG_EDI (ctx);
	mctx->eip = UCONTEXT_REG_EIP (ctx);
#else	
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	mctx->eax = ctx->SC_EAX;
	mctx->ebx = ctx->SC_EBX;
	mctx->ecx = ctx->SC_ECX;
	mctx->edx = ctx->SC_EDX;
	mctx->ebp = ctx->SC_EBP;
	mctx->esp = ctx->SC_ESP;
	mctx->esi = ctx->SC_ESI;
	mctx->edi = ctx->SC_EDI;
	mctx->eip = ctx->SC_EIP;
#endif
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;

	UCONTEXT_REG_EAX (ctx) = mctx->eax;
	UCONTEXT_REG_EBX (ctx) = mctx->ebx;
	UCONTEXT_REG_ECX (ctx) = mctx->ecx;
	UCONTEXT_REG_EDX (ctx) = mctx->edx;
	UCONTEXT_REG_EBP (ctx) = mctx->ebp;
	UCONTEXT_REG_ESP (ctx) = mctx->esp;
	UCONTEXT_REG_ESI (ctx) = mctx->esi;
	UCONTEXT_REG_EDI (ctx) = mctx->edi;
	UCONTEXT_REG_EIP (ctx) = mctx->eip;
#else
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	ctx->SC_EAX = mctx->eax;
	ctx->SC_EBX = mctx->ebx;
	ctx->SC_ECX = mctx->ecx;
	ctx->SC_EDX = mctx->edx;
	ctx->SC_EBP = mctx->ebp;
	ctx->SC_ESP = mctx->esp;
	ctx->SC_ESI = mctx->esi;
	ctx->SC_EDI = mctx->edi;
	ctx->SC_EIP = mctx->eip;
#endif
}	

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;
	return (gpointer)UCONTEXT_REG_EIP (ctx);
#else
	struct sigcontext *ctx = sigctx;
	return (gpointer)ctx->SC_EIP;
#endif	
}

gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;

	mono_arch_sigctx_to_monoctx (sigctx, &mctx);

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj))
		return TRUE;

	mono_handle_exception (&mctx, obj, (gpointer)mctx.eip, test_only);

	mono_arch_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
}

static void
restore_soft_guard_pages (void)
{
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
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
altstack_handle_and_restore (void *sigctx, gpointer obj, gboolean stack_ovf)
{
	void (*restore_context) (MonoContext *);
	MonoContext mctx;

	restore_context = mono_get_restore_context ();
	mono_arch_sigctx_to_monoctx (sigctx, &mctx);

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj)) {
		if (stack_ovf)
			prepare_for_guard_pages (&mctx);
		restore_context (&mctx);
	}

	mono_handle_exception (&mctx, obj, (gpointer)mctx.eip, FALSE);
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
	frame_size = sizeof (ucontext_t) + sizeof (gpointer) * 4;
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
	/* may need to adjust pointers in the new struct copy, depending on the OS */
	memcpy (sp + 4, ctx, sizeof (ucontext_t));
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

	if (saved)
		return (MonoContinuationRestore)saved;
	code = start = mono_global_codeman_reserve (48);
	/* the signature is: restore (MonoContinuation *cont, int state, MonoLMF **lmf_addr) */
	/* put cont in edx */
	x86_mov_reg_membase (code, X86_EDX, X86_ESP, 4, 4);
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

	/* state in eax, so it's setup as the return value */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 8, 4);
	x86_jump_membase (code, X86_EDX, G_STRUCT_OFFSET (MonoContinuation, return_ip));
	g_assert ((code - start) <= 48);
	saved = start;
	return (MonoContinuationRestore)saved;
}
#endif

